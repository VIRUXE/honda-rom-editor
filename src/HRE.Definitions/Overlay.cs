using System.Text;
using HRE.Core;

namespace HRE.Definitions;

/// <summary>One calibration edit: the byte at <c>symbol + offset</c> becomes <c>value</c>.</summary>
public readonly record struct OverlayEdit(string Symbol, uint Offset, byte Value);

/// <summary>The tuner's calibration edits, kept apart from the assembly source (<c>overlay.toml</c>).</summary>
public sealed class Overlay(IReadOnlyList<OverlayEdit> edits) {
    public static Overlay Empty { get; } = new([]);

    public IReadOnlyList<OverlayEdit> Edits { get; } = edits;
}

public enum OverlayErrorKind { UnknownSymbol, OutOfRom }

public sealed class OverlayException(OverlayErrorKind kind, string symbol, uint offset) : Exception(kind == OverlayErrorKind.UnknownSymbol
    ? $"overlay refers to unknown symbol `{symbol}`"
    : $"overlay edit {symbol}+{offset} is outside the ROM") {
    public OverlayErrorKind Kind { get; } = kind;
    public string Symbol { get; } = symbol;
    public uint Offset { get; } = offset;
}

public static class OverlayOps {
    public static Overlay Parse(string text) {
        var edits = new List<OverlayEdit>();
        foreach (var t in TomlReader.Tables(TomlReader.ParseRoot(text), "edit")) {
            edits.Add(new OverlayEdit(TomlReader.String(t, "symbol"), (uint)TomlReader.Integer(t, "offset", 0, uint.MaxValue), (byte)TomlReader.Integer(t, "value", 0, 255)));
        }
        return new Overlay(edits);
    }

    public static string ToToml(Overlay overlay) {
        var sb = new StringBuilder();
        foreach (var e in overlay.Edits) {
            if (sb.Length > 0) sb.Append('\n');
            sb.Append("[[edit]]\n");
            sb.Append("symbol = ").Append(TomlReader.Quote(e.Symbol)).Append('\n');
            sb.Append("offset = ").Append(e.Offset).Append('\n');
            sb.Append("value = ").Append(e.Value).Append('\n');
        }
        return sb.ToString();
    }

    /// <summary>Writes every edit into the image. An unknown symbol or an address outside the ROM is an error.</summary>
    public static RomImage Apply(Overlay overlay, RomImage image, SymbolMap map) {
        var writes = new List<(int, byte)>(overlay.Edits.Count);
        foreach (var e in overlay.Edits) {
            var symbol = map.Get(e.Symbol) ?? throw new OverlayException(OverlayErrorKind.UnknownSymbol, e.Symbol, e.Offset);
            var at = symbol.Value + e.Offset;
            if (symbol.Value < 0 || at >= image.Length) throw new OverlayException(OverlayErrorKind.OutOfRom, e.Symbol, e.Offset);
            writes.Add(((int)at, e.Value));
        }
        return image.With(writes);
    }

    /// <summary>
    /// The edits that turn <paramref name="baseImage"/> into <paramref name="edited"/>, limited to
    /// the bytes parameters cover and expressed relative to their symbols. The checksum byte is
    /// skipped: it is recomputed on every build.
    /// </summary>
    public static Overlay FromDiff(RomImage baseImage, RomImage edited, IEnumerable<ParamDef> defs, int? checksumAt) {
        var edits = new List<OverlayEdit>();
        var seen = new HashSet<(string, uint)>();
        foreach (var (symbol, span) in defs.SelectMany(Parts)) {
            for (var at = span.Start; at < span.End; at++) {
                if (at == checksumAt || at >= baseImage.Length || at >= edited.Length || baseImage[at] == edited[at]) continue;
                var offset = (uint)(at - span.Start);
                // One entry per (symbol, offset): two parameters sharing a span must not duplicate edits.
                if (seen.Add((symbol, offset))) edits.Add(new OverlayEdit(symbol, offset, edited[at]));
            }
        }
        return new Overlay(edits);
    }

    /// <summary>
    /// Merges <paramref name="fresh"/> (a <see cref="FromDiff"/> result over <paramref name="defs"/>,
    /// against <paramref name="rom"/>) into <paramref name="existing"/>. Edits are matched by the
    /// address they resolve to, so an edit written under another name for the same byte is the same
    /// edit. An existing edit:
    /// <list type="bullet">
    /// <item>is replaced when <paramref name="fresh"/> has an edit for its address;</item>
    /// <item>inside a span the definitions cover, is dropped when the ROM byte no longer holds its
    /// value (the cell was edited back to the assembled value) and kept when it still does;</item>
    /// <item>elsewhere, or under a symbol the map does not have, is kept.</item>
    /// </list>
    /// </summary>
    public static Overlay Fold(Overlay existing, Overlay fresh, IEnumerable<ParamDef> defs, SymbolMap map, RomImage rom) {
        var spans = defs.SelectMany(Parts).Select(p => p.Span).ToList();
        var replaced = fresh.Edits.Select(e => Address(e, map)).OfType<long>().ToHashSet();
        var kept = existing.Edits.Where(e => {
            if (Address(e, map) is not { } at) return true;
            if (replaced.Contains(at)) return false;
            if (at <= int.MaxValue && spans.Any(s => s.Contains((int)at))) return at < rom.Length && rom[(int)at] == e.Value;
            return true;
        });
        return new Overlay([.. kept, .. fresh.Edits]);
    }

    /// <summary>Where an edit writes: its symbol's value plus its offset, when the symbol exists and is not negative.</summary>
    private static long? Address(OverlayEdit e, SymbolMap map) =>
        map.Get(e.Symbol) is { Value: >= 0 } symbol ? symbol.Value + e.Offset : null;

    /// <summary>(symbol, span) for a parameter's data and both axes; parts without a symbol cannot be expressed.</summary>
    private static IEnumerable<(string Symbol, ByteRange Span)> Parts(ParamDef d) {
        if (d.Symbol is { } symbol) yield return (symbol, new ByteRange(d.Offset, d.Offset + d.Length));
        if (d.Kind is not ParamKind.Table table) yield break;
        if (d.RowAxis.Symbol is { } rows) yield return (rows, new ByteRange(d.RowAxis.Offset, d.RowAxis.Offset + d.RowAxis.StoredLength(table.Rows)));
        if (d.ColAxis.Symbol is { } cols) yield return (cols, new ByteRange(d.ColAxis.Offset, d.ColAxis.Offset + d.ColAxis.StoredLength(table.Cols)));
        if (d.Multiplier is { Symbol: { } multiplier } m) yield return (multiplier, new ByteRange(m.Offset, m.Offset + table.Cols));
    }
}
