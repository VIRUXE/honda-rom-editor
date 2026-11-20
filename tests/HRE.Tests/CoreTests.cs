using HRE.Core;

namespace HRE.Tests;

public class CoreTests {
    /// <summary>A 20 x 10 table at 0x6000 with its RPM axis at 0x6500 and MAP axis at 0x6528.</summary>
    private static readonly ParamDef Fuel = new() {
        Category = "Fuel",
        Name = "Fuel Map",
        Offset = 0x6000,
        Kind = new ParamKind.Table(20, 10),
        RowAxis = Axis.At("RPM", 0x6500),
        ColAxis = Axis.At("MAP kPa", 0x6528),
    };

    private static RomImage Demo() {
        var data = new byte[0x8000];
        Array.Fill(data, (byte)0xFF);
        for (var r = 0; r < 20; r++) WriteWord(data, 0x6500 + 2 * r, (ushort)(500 + 400 * r));
        for (var c = 0; c < 10; c++) WriteWord(data, 0x6528 + 2 * c, (ushort)(20 + 9 * c));
        for (var r = 0; r < 20; r++) {
            for (var c = 0; c < 10; c++) data[0x6000 + r * 10 + c] = (byte)(r + 2 * c);
        }
        return Checksum.Fix(new RomImage(data));
    }

    private static void WriteWord(byte[] data, int at, ushort v) {
        data[at] = (byte)(v >> 8);
        data[at + 1] = (byte)v;
    }

    [Fact]
    public void FixChecksumRepairsAnEdit() {
        var rom = Demo();
        Assert.True(Checksum.IsValid(rom));
        var edited = rom.With(0x6000, (byte)(rom[0x6000] + 7));
        Assert.False(Checksum.IsValid(edited));
        Assert.True(Checksum.IsValid(Checksum.Fix(edited)));
    }

    [Fact]
    public void ChecksumCanCoverPartOfTheImage() {
        var data = new byte[0x8000];
        Array.Fill(data, (byte)0xFF, 0x4000, 0x4000);
        data[0x10] = 0x37;
        var rom = Checksum.Fix(new RomImage(data), 0x3FFF, 0, 0x4000);
        Assert.True(Checksum.IsValid(rom, 0, 0x4000));
        Assert.Equal(0xC9, rom[0x3FFF]);
        Assert.Throws<ArgumentOutOfRangeException>(() => Checksum.Fix(rom, 0x4000, 0, 0x4000));
    }

    [Fact]
    public void WordsAreBigEndian() {
        var rom = Demo().With(ParamAccess.WordWrites(0x6400, 0x1234));
        Assert.Equal(new byte[] { 0x12, 0x34 }, rom.Bytes.Slice(0x6400, 2).ToArray());
        Assert.Equal(0x1234, rom.Word(0x6400));
    }

    [Fact]
    public void ImagesAreImmutable() {
        var rom = Demo();
        var copy = rom.ToArray();
        copy[0] = 0x55;
        Assert.NotEqual(0x55, rom[0]);
        Assert.Same(rom, rom.With(0, rom[0]));
        Assert.Throws<ArgumentOutOfRangeException>(() => rom.With([(0x8000, (byte)1)]));
    }

    [Fact]
    public void AxesAreReadFromTheRom() {
        var rom = Demo();
        Assert.Equal([500f, 900f, 1300f], ParamAccess.AxisValues(rom, Fuel.RowAxis, 3));
        Assert.Equal(101, ParamAccess.AxisValue(rom, Fuel.ColAxis, 9));
        Assert.True(ParamAccess.Contains(rom, Fuel));
        Assert.Equal([new ByteRange(0x6000, 0x60C8), new ByteRange(0x6500, 0x6528), new ByteRange(0x6528, 0x653C)], Fuel.Spans());
    }

    [Fact]
    public void ClampAxisValueKeepsBreakpointsIncreasing() {
        var rom = Demo();
        Assert.Equal(1000, ParamAccess.ClampAxisValue(rom, Fuel.RowAxis, 20, 1, 1000));
        Assert.Equal(1299, ParamAccess.ClampAxisValue(rom, Fuel.RowAxis, 20, 1, 9999));
        Assert.Equal(501, ParamAccess.ClampAxisValue(rom, Fuel.RowAxis, 20, 1, 0));
        Assert.Equal(0, ParamAccess.ClampAxisValue(rom, Fuel.RowAxis, 20, 0, 0));
        Assert.Equal(9000, ParamAccess.ClampAxisValue(rom, Fuel.RowAxis, 20, 19, 9000));
    }

    [Fact]
    public void FixedAxisReadsItsValuesAndOccupiesNoBytes() {
        var rom = Demo();
        var def = Fuel with {
            RowAxis = Axis.Fixed("RPM", [.. Enumerable.Range(0, 20).Select(r => (ushort)(500 + 400 * r))]),
            ColAxis = Axis.Fixed("MAP kPa", [11, 29, 40, 52, 63, 75, 87, 92, 98, 104]),
        };
        Assert.Equal(1300, ParamAccess.AxisValue(rom, def.RowAxis, 2));
        Assert.Equal([11f, 29f, 40f], ParamAccess.AxisValues(rom, def.ColAxis, 3));
        Assert.Equal(29, ParamAccess.ClampAxisValue(rom, def.ColAxis, 10, 1, 33));
        Assert.Equal([new ByteRange(0x6000, 0x60C8)], def.Spans());
    }

    [Fact]
    public void AxisPosIsFractionalAndClamped() {
        float[] points = [1000, 2000, 4000];
        Assert.Equal(0f, TableMath.AxisPos(points, 500));
        Assert.Equal(0f, TableMath.AxisPos(points, 1000));
        Assert.Equal(0.25f, TableMath.AxisPos(points, 1250));
        Assert.Equal(1f, TableMath.AxisPos(points, 2000));
        Assert.Equal(1.5f, TableMath.AxisPos(points, 3000));
        Assert.Equal(2f, TableMath.AxisPos(points, 9000));
    }

    [Fact]
    public void LookupInterpolatesBilinearly() {
        var rom = Demo();
        var def = Fuel with { Scale = 0.5f, Bias = 1f };
        // Cell (r, c) holds r + 2c, so the surface is a plane and interpolation is exact.
        Assert.Equal(def.Display(0), TableMath.Lookup(rom, def, 500, 20));
        Assert.Equal(def.Display(0.5f + 2 * 0.5f), TableMath.Lookup(rom, def, 700, 24.5f));
        Assert.Equal(def.Display(19 + 18), TableMath.Lookup(rom, def, 99999, 99999));
        Assert.Null(TableMath.Lookup(rom, def with { Kind = new ParamKind.Word(0, 9) }, 500, 20));
        Assert.Null(TableMath.Lookup(new RomImage(new byte[16]), def, 500, 20));
    }

    [Fact]
    public void DocumentUndoesAndRedoesWholeSteps() {
        var doc = new RomDocument(Demo());
        var original = doc.Image;
        var changes = 0;
        doc.Changed += () => changes++;

        Assert.False(doc.Apply("no-op", [(0x6000, original[0x6000])]));
        Assert.False(doc.IsDirty);

        Assert.True(doc.Apply("raise two cells", [(0x6000, 50), (0x6001, 60), (0x6002, original[0x6002])]));
        Assert.True(doc.IsDirty);
        Assert.Equal("raise two cells", doc.UndoLabel);
        Assert.Equal(2, doc.Image.Bytes.Slice(0x6000, 3).ToArray().Zip(original.Bytes.Slice(0x6000, 3).ToArray()).Count(p => p.First != p.Second));

        Assert.True(doc.Undo());
        Assert.Equal(original.Bytes.ToArray(), doc.Image.Bytes.ToArray());
        Assert.False(doc.IsDirty);
        Assert.Equal("raise two cells", doc.RedoLabel);

        Assert.True(doc.Redo());
        Assert.Equal(50, doc.Image[0x6000]);
        Assert.Equal(60, doc.Image[0x6001]);
        Assert.False(doc.Redo());
        Assert.Equal(3, changes);
    }

    [Fact]
    public void DocumentTracksTheSavedState() {
        var doc = new RomDocument(Demo());
        doc.Apply("a", [(0x6000, 1)]);
        doc.MarkSaved();
        Assert.False(doc.IsDirty);
        doc.Undo();
        Assert.True(doc.IsDirty);
        doc.Apply("b", [(0x6000, 2)]);
        Assert.True(doc.IsDirty);
        Assert.False(doc.CanRedo);
        Assert.Throws<ArgumentOutOfRangeException>(() => doc.Apply("outside", [(0x8000, 1)]));

        // Saving corrects the checksum on the way out: the document takes that in without a step to undo.
        var changes = 0;
        doc.Changed += () => changes++;
        var written = Checksum.Fix(doc.Image);
        doc.MarkSaved(written);
        Assert.Equal((false, 1, "b"), (doc.IsDirty, changes, doc.UndoLabel));
        Assert.Same(written, doc.Image);
        doc.MarkSaved(new RomImage(written.Bytes));
        Assert.Equal(1, changes);
        doc.Undo();
        Assert.Equal((true, written[^1]), (doc.IsDirty, doc.Image[doc.Image.Length - 1]));
        Assert.Throws<ArgumentOutOfRangeException>(() => doc.MarkSaved(new RomImage(new byte[4])));
    }
}
