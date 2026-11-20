using HRE.Core;
using HRE.Definitions;

namespace HRE.Tests;

/// <summary>Axes stored one byte per breakpoint, linear or on the log scale, with a scale and a bias.</summary>
public class ByteAxisTests {
    /// <summary>A 2 x 4 table at 0x20 with its row axis at 0x10 and its column axis at 0x18.</summary>
    private static RomImage Rom(byte[] rows, byte[] cols) {
        var data = new byte[0x40];
        rows.CopyTo(data, 0x10);
        cols.CopyTo(data, 0x18);
        new byte[] { 10, 20, 30, 40, 50, 60, 70, 80 }.CopyTo(data, 0x20);
        return new RomImage(data);
    }

    private static Axis Bytes(int offset, float scale = 1f, float bias = 0f) =>
        Axis.At("x", offset, null, AxisEncoding.Bytes) with { Scale = scale, Bias = bias };

    private static Axis Log(int offset, float scale = 1f) => Axis.At("x", offset, null, AxisEncoding.LogBytes) with { Scale = scale };

    [Fact]
    public void AZeroAfterTheFirstBreakpointIs256() {
        var rom = Rom([0x00, 0x00], [0x00, 0x30, 0xF0, 0x00]);
        var axis = Bytes(0x18);
        Assert.Equal([0f, 48f, 240f, 256f], ParamAccess.AxisStoredValues(rom, axis, 4));
        Assert.Equal(0, ParamAccess.AxisValue(rom, axis, 0));
        Assert.Equal(256, ParamAccess.AxisValue(rom, axis, 3));
        Assert.Equal(4, axis.StoredLength(4));
    }

    [Fact]
    public void ScaleAndBiasGiveTheDisplayedBreakpoints() {
        var rom = Rom([0x00, 0x00], [0x00, 0x30, 0xF0, 0x00]);
        var axis = Bytes(0x18, 0.5f, 10f);
        Assert.Equal([10f, 34f, 130f, 138f], ParamAccess.AxisValues(rom, axis, 4));
        Assert.Equal(48f, axis.Stored(34f));
        // The stored values are not scaled.
        Assert.Equal(48, ParamAccess.AxisValue(rom, axis, 1));
    }

    [Fact]
    public void TheLogScaleDoublesEvery64Steps() {
        var rom = Rom([0x00, 0x00], [0x00, 0x53, 0xC7, 0x00]);
        var axis = Log(0x18, 500f);
        // 0x53 = 64 + 19: 500 x 2 x (1 + 19/64); 0xC7 = 3 x 64 + 7: 500 x 8 x (1 + 7/64).
        Assert.Equal([500f, 1296.875f, 4437.5f, 8000f], ParamAccess.AxisValues(rom, axis, 4));
        Assert.Equal([0f, 83f, 199f, 256f], ParamAccess.AxisStoredValues(rom, axis, 4));
        foreach (var stored in new[] { 0f, 1f, 63f, 64f, 83f, 128f, 199.5f, 255f, 256f }) Assert.Equal(stored, axis.Stored(axis.Display(stored)), 3);
        // Below the first octave the first octave's line carries on.
        Assert.Equal(-32f, axis.Stored(250f), 3);
    }

    /// <summary>The ECU interpolates in stored units, so the position on a log axis is measured there.</summary>
    [Fact]
    public void ATablePositionIsMeasuredInStoredUnits() {
        var rom = Rom([0x20, 0xA0], [0x00, 0x30, 0xF0, 0x00]);
        var def = new ParamDef {
            Category = "c",
            Name = "n",
            Offset = 0x20,
            Kind = new ParamKind.Table(2, 4),
            RowAxis = Log(0x10, 500f),
            ColAxis = Bytes(0x18, 0.5f, 10f),
        };
        // Rows at 0x20 (750 rpm) and 0xA0 (3000 rpm). 1500 rpm is stored 0x60: halfway in steps,
        // though only a third of the way in rpm.
        var pos = TableMath.TablePos(rom, def, 1500f, 34f);
        Assert.NotNull(pos);
        Assert.Equal(0.5f, pos.Value.Row, 3);
        Assert.Equal(1f, pos.Value.Col, 3);
        Assert.Equal(40f, TableMath.Lookup(rom, def, 1500f, 34f)!.Value, 3);
        // Clamped at both ends.
        Assert.Equal((0f, 3f), TableMath.TablePos(rom, def, 100f, 500f));
    }

    [Fact]
    public void EditingKeepsABreakpointBetweenItsNeighbours() {
        var rom = Rom([0x00, 0x00], [0x10, 0x30, 0xF0, 0x00]);
        var axis = Bytes(0x18);
        Assert.Equal(0x31, ParamAccess.ClampAxisValue(rom, axis, 4, 2, 0));
        Assert.Equal(0xFF, ParamAccess.ClampAxisValue(rom, axis, 4, 2, 9999));
        Assert.Equal(0x2F, ParamAccess.ClampAxisValue(rom, axis, 4, 0, 9999));
        Assert.Equal(0, ParamAccess.ClampAxisValue(rom, axis, 4, 0, 0));
        // Only the last breakpoint reaches 256, and it is stored as zero.
        Assert.Equal(256, ParamAccess.ClampAxisValue(rom, axis, 4, 3, 9999));
        Assert.Equal(0xF1, ParamAccess.ClampAxisValue(rom, axis, 4, 3, 0));
        Assert.Equal([(0x1B, (byte)0x00)], ParamAccess.AxisWrites(rom, axis, 4, 3, 256));
        Assert.Equal([(0x1B, (byte)0xF8)], ParamAccess.AxisWrites(rom, axis, 4, 3, 0xF8));
        var moved = rom.With(ParamAccess.AxisWrites(rom, axis, 4, 1, 0x40));
        Assert.Equal([16f, 64f, 240f, 256f], ParamAccess.AxisStoredValues(moved, axis, 4));
    }

    /// <summary>The first breakpoint is the only one whose zero means zero, so it cannot be 256.</summary>
    [Fact]
    public void TheFirstBreakpointStopsAt255() {
        var rom = Rom([0x00, 0x00], [0x10, 0x00, 0x00, 0x00]);
        Assert.Equal(255, ParamAccess.ClampAxisValue(rom, Bytes(0x18), 2, 0, 9999));
    }

    /// <summary>An axis padded with repeats, as ROMs with spare columns have, reads without failing and cannot be made worse.</summary>
    [Fact]
    public void APaddedAxisStaysWhereItIs() {
        var rom = Rom([0x00, 0x00], [0x18, 0x98, 0x98, 0x98, 0x00]);
        var axis = Bytes(0x18);
        Assert.Equal([24f, 152f, 152f, 152f, 256f], ParamAccess.AxisStoredValues(rom, axis, 5));
        // No value lies strictly between 0x98 and 0x98.
        Assert.Equal(0x98, ParamAccess.ClampAxisValue(rom, axis, 5, 2, 0xA0));
        Assert.Same(rom, rom.With(ParamAccess.AxisWrites(rom, axis, 5, 2, 0xA0)));
        // A value on the repeated breakpoint belongs to the last column that holds it, as in the ECU's search.
        Assert.Equal(3f, TableMath.AxisPos(ParamAccess.AxisStoredValues(rom, axis, 5), 152f));
    }

    private static string Table(string rowAxis, string colAxis = "{ label = 'c', values = [1, 2] }") => DefinitionPackTests.Minimal() + $"""

        [[param]]
        category = "c"
        name = "n"
        address = 0x100
        kind = "table"
        rows = 2
        cols = 2
        row_axis = {rowAxis}
        col_axis = {colAxis}
        """;

    [Fact]
    public void APackNamesTheEncodingScaleAndBias() {
        var pack = DefinitionPacks.Parse(Table(
            "{ label = 'RPM', address = 0x10, encoding = 'log-bytes', scale = 500 }",
            "{ label = 'MAP kPa', address = 0x20, encoding = 'bytes', scale = 0.5, bias = 10.0 }"));
        Assert.Empty(pack.Resolve(DefinitionPackTests.Blank).Problems);
        var def = Assert.Single(pack.Resolve(DefinitionPackTests.Blank).Defs);
        Assert.Equal(Axis.At("RPM", 0x10, null, AxisEncoding.LogBytes) with { Scale = 500f }, def.RowAxis);
        Assert.Equal(Axis.At("MAP kPa", 0x20, null, AxisEncoding.Bytes) with { Scale = 0.5f, Bias = 10f }, def.ColAxis);
        Assert.Equal([new ByteRange(0x100, 0x104), new ByteRange(0x10, 0x12), new ByteRange(0x20, 0x22)], def.Spans());
    }

    [Fact]
    public void AScaleAlsoAppliesToWordsAndDeltaBytes() {
        var pack = DefinitionPacks.Parse(Table(
            "{ label = 'r', address = 0x10, scale = 2 }",
            "{ label = 'c', address = 0x20, encoding = 'delta-bytes', scale = 4, bias = 1 }"));
        var def = Assert.Single(pack.Resolve(DefinitionPackTests.Blank).Defs);
        var data = new byte[0x8000];
        new byte[] { 0x01, 0x00, 0x02, 0x00 }.CopyTo(data, 0x10);    // words 256, 512
        new byte[] { 3, 5 }.CopyTo(data, 0x20);                       // 3, 8
        var rom = new RomImage(data);
        Assert.Equal([512f, 1024f], ParamAccess.AxisValues(rom, def.RowAxis, 2));
        Assert.Equal([13f, 33f], ParamAccess.AxisValues(rom, def.ColAxis, 2));
    }

    [Theory]
    [InlineData("{ label = 'r', address = 0x10, encoding = 'bytes', scale = 0 }", "must not be 0")]
    [InlineData("{ label = 'r', values = [1, 2], scale = 2 }", "only a stored axis has a scale")]
    [InlineData("{ label = 'r', values = [1, 2], bias = 1 }", "only a stored axis has a scale")]
    [InlineData("{ label = 'r', address = 0x10, encoding = 'nibbles' }", "encoding words, delta-bytes, bytes, log-bytes or values")]
    [InlineData("{ label = 'r', address = 0x10, scale = 'big' }", "must be a number")]
    public void RejectsAnAxisItCannotRead(string axis, string reason) {
        var e = Assert.Throws<HreFormatException>(() => DefinitionPacks.Parse(Table(axis)));
        Assert.Contains(reason, e.Message);
    }

    [Fact]
    public void AByteAxisPastTheRomIsReportedAndLeftOut() {
        var pack = DefinitionPacks.Parse(Table("{ label = 'r', address = 0x7FFF, encoding = 'bytes' }"));
        Assert.Empty(pack.Resolve(DefinitionPackTests.Blank).Defs);
        Assert.Equal(["n: row_axis is outside the 0x8000-byte ROM"], pack.Resolve(DefinitionPackTests.Blank).Problems);
    }
}
