using HRE.Core;
using HRE.Definitions;

namespace HRE.Tests;

public class OverlayTests {
    private static SymbolEntry Sym(string name, long value, SymbolKind kind = SymbolKind.Label) => new(name, value, kind, "m", 1);

    private static SymbolMap Map(params SymbolEntry[] extra) => new(0x8000, "m", [Sym("tbl", 0x100), Sym("word", 0x200), .. extra]);

    private static readonly ParamDef[] Defs = [
        new() {
            Category = "Fuel",
            Name = "T",
            Symbol = "tbl",
            Offset = 0x100,
            Kind = new ParamKind.Table(2, 3),
            RowAxis = Axis.At("r", 0x106, "ax"),
            ColAxis = Axis.At("c", 0x10A, "ay"),
        },
        new() { Category = "L", Name = "W", Symbol = "word", Offset = 0x200, Kind = new ParamKind.Word(0, 9) },
    ];

    private static RomImage Blank() => new(new byte[0x8000]);

    private static OverlayEdit E(string symbol, uint offset, byte value) => new(symbol, offset, value);

    [Fact]
    public void TomlRoundTrip() {
        var overlay = new Overlay([E("tbl", 4, 200), E("word", 1, 7)]);
        var text = OverlayOps.ToToml(overlay);
        Assert.Equal("[[edit]]\nsymbol = \"tbl\"\noffset = 4\nvalue = 200\n\n[[edit]]\nsymbol = \"word\"\noffset = 1\nvalue = 7\n", text);
        Assert.Equal(overlay.Edits, OverlayOps.Parse(text).Edits);
        Assert.Empty(OverlayOps.Parse("").Edits);
        Assert.Equal("", OverlayOps.ToToml(Overlay.Empty));
    }

    [Fact]
    public void ParseRejectsBadEdits() {
        Assert.Throws<HreFormatException>(() => OverlayOps.Parse("[[edit]]\nsymbol = \"a\"\noffset = 0\nvalue = 256\n"));
        Assert.Throws<HreFormatException>(() => OverlayOps.Parse("[[edit]]\nsymbol = \"a\"\noffset = -1\nvalue = 1\n"));
        Assert.Throws<HreFormatException>(() => OverlayOps.Parse("[[edit]]\noffset = 0\nvalue = 1\n"));
        Assert.Throws<HreFormatException>(() => OverlayOps.Parse("[[edit]"));
    }

    [Fact]
    public void ApplyWritesSymbolRelativeBytes() {
        var image = OverlayOps.Apply(new Overlay([E("tbl", 4, 200), E("word", 1, 7)]), Blank(), Map());
        Assert.Equal(200, image[0x104]);
        Assert.Equal(7, image[0x201]);
    }

    [Fact]
    public void ApplyRejectsUnknownSymbolsAndAddressesOutsideTheRom() {
        var unknown = Assert.Throws<OverlayException>(() => OverlayOps.Apply(new Overlay([E("gone", 0, 1)]), Blank(), Map()));
        Assert.Equal(OverlayErrorKind.UnknownSymbol, unknown.Kind);
        Assert.Equal("gone", unknown.Symbol);
        var outside = Assert.Throws<OverlayException>(() => OverlayOps.Apply(new Overlay([E("word", 0x8000, 1)]), Blank(), Map()));
        Assert.Equal(OverlayErrorKind.OutOfRom, outside.Kind);
        var negative = Assert.Throws<OverlayException>(() => OverlayOps.Apply(new Overlay([E("neg", 5, 1)]), Blank(), Map(Sym("neg", -1, SymbolKind.Equ))));
        Assert.Equal(OverlayErrorKind.OutOfRom, negative.Kind);
    }

    [Fact]
    public void FromDiffStaysInsideParameterSpansAndSkipsTheChecksum() {
        var edited = Blank().With([
            (0x101, 5),       // table cell (1 into tbl)
            (0x107, 9),       // row axis byte: symbol "ax", offset 1
            (0x200, 1),       // word high byte
            (0x300, 0xEE),    // outside every span: ignored
            (0x7FFF, 0x42),   // checksum byte: ignored
        ]);
        var overlay = OverlayOps.FromDiff(Blank(), edited, Defs, 0x7FFF);
        Assert.Equal([E("tbl", 1, 5), E("ax", 1, 9), E("word", 0, 1)], overlay.Edits);
    }

    /// <summary>Folds against a ROM whose bytes are <paramref name="baseImage"/> with <paramref name="fresh"/> applied.</summary>
    private static IReadOnlyList<OverlayEdit> Fold(OverlayEdit[] existing, OverlayEdit[] fresh, SymbolMap map, RomImage baseImage) {
        var freshOverlay = new Overlay(fresh);
        var rom = OverlayOps.Apply(freshOverlay, baseImage, map);
        return OverlayOps.Fold(new Overlay(existing), freshOverlay, Defs, map, rom).Edits;
    }

    [Fact]
    public void FoldReplacesCoveredEditsAndKeepsTheRest() {
        OverlayEdit[] existing = [
            E("tbl", 1, 5),    // edited back to the assembled value (0): absent from fresh, dropped
            E("tbl", 2, 6),    // edited again: replaced
            E("gone", 0, 7),   // no such symbol: kept
            E("tbl", 40, 8),   // beyond the 2 x 3 table and its axes: kept
        ];
        var folded = Fold(existing, [E("tbl", 2, 9), E("word", 1, 3)], Map(), Blank());
        Assert.Equal([E("gone", 0, 7), E("tbl", 40, 8), E("tbl", 2, 9), E("word", 1, 3)], folded);
    }

    /// <summary>
    /// Coverage is by address: an edit written under another symbol for the same bytes is replaced
    /// by the fresh edit, not kept beside it.
    /// </summary>
    [Fact]
    public void FoldMatchesEditsByAddressAcrossAliases() {
        var folded = Fold([E("alias", 2, 6), E("alias", 1, 4)], [E("tbl", 2, 9)], Map(Sym("alias", 0x100)), Blank());
        Assert.Equal([E("tbl", 2, 9)], folded);
    }

    /// <summary>
    /// An edit whose byte the ROM still holds is kept even when it now equals the assembled value:
    /// the tuner's value stays pinned if the source changes again.
    /// </summary>
    [Fact]
    public void FoldKeepsAnEditWhoseByteStillMatches() {
        var folded = Fold([E("tbl", 3, 7)], [], Map(), Blank().With(0x103, 7));
        Assert.Equal([E("tbl", 3, 7)], folded);
    }
}
