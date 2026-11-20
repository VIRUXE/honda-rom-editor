using HRE.Core;
using HRE.Definitions;

namespace HRE.Tests;

/// <summary>Packs that say where the ROM keeps an address, a size or the ends of an axis.</summary>
public class RomValueTests {
    /// <summary>
    /// A pack for a ROM whose code holds the address of a table (at 0x11), its column count (at
    /// 0x21) and the address of its column axis (at 0x31), with the range of the sensor behind
    /// that axis in the four bytes before the axis.
    /// </summary>
    private const string Pack = """
        id = 'x'
        name = 'X'

        [[fingerprint]]
        bytes = [
            { address = 0x10, value = [0x60, 0, 0], mask = [0xFF, 0, 0] },
            { address = 0x20, value = [0x98, 0], mask = [0xFF, 0] },
            { address = 0x30, value = [0x60, 0, 0], mask = [0xFF, 0, 0] },
        ]

        [[param]]
        category = 'Fuel'
        name = 'Fuel'
        address = { at = 0x11 }
        kind = 'table'
        rows = 2
        cols = { at = 0x21 }
        row_axis = { label = 'RPM', values = [1000, 2000] }
        col_axis = { label = 'MAP kPa', address = { at = 0x31 }, encoding = 'bytes', ends = { at = { at = 0x31, bias = -4 }, bias = -32768 }, scale = 0.1 }
        multiplier = { kind = 'linear' }
        """;

    /// <summary>The ROM of <see cref="Pack"/> with its table at <paramref name="table"/> and its axis at <paramref name="axis"/>.</summary>
    private static RomImage Rom(int table, int cols, int axis, int low = -50, int high = 2500) {
        var data = new byte[0x8000];
        void Put(int address, int value) {
            if (address < data.Length) data[address] = (byte)value;
        }
        var (lo, hi) = (low + 0x8000, high + 0x8000);
        new[] { (byte)lo, (byte)(lo >> 8), (byte)hi, (byte)(hi >> 8) }.CopyTo(data, axis - 4);
        for (var c = 0; c < cols; c++) Put(axis + c, c == cols - 1 ? 0 : 20 + 30 * c);
        for (var i = 0; i < 2 * cols; i++) Put(table + i, i + 1);
        for (var c = 0; c < cols; c++) Put(table + 2 * cols + c, c + 2);
        // The code last: a table the test puts on top of it must not change where the code points.
        (data[0x10], data[0x11], data[0x12]) = (0x60, (byte)table, (byte)(table >> 8));
        (data[0x20], data[0x21]) = (0x98, (byte)cols);
        (data[0x30], data[0x31], data[0x32]) = (0x60, (byte)axis, (byte)(axis >> 8));
        return new RomImage(data);
    }

    [Fact]
    public void TheSamePackReadsTwoRomsFromWhereEachKeepsItsTable() {
        var pack = DefinitionPacks.Parse(Pack);
        var small = Rom(table: 0x1000, cols: 3, axis: 0x0F00);
        var large = Rom(table: 0x2000, cols: 6, axis: 0x1F00, low: 70, high: 2590);
        Assert.NotNull(Fingerprints.Match(pack, small));
        Assert.NotNull(Fingerprints.Match(pack, large));

        var (defs, problems) = pack.Resolve(small);
        Assert.Empty(problems);
        var fuel = Assert.Single(defs);
        Assert.Equal((0x1000, new ParamKind.Table(2, 3)), (fuel.Offset, fuel.Kind));
        Assert.Equal(0x0F00, fuel.ColAxis.Offset);
        // The multipliers are the row after the last.
        Assert.Equal(new ColumnMultiplier(0x1006, MultiplierKind.Linear), fuel.Multiplier);
        Assert.Equal([new ByteRange(0x1000, 0x1006), new ByteRange(0x0F00, 0x0F03), new ByteRange(0x1006, 0x1009)], fuel.Spans());
        // Stored 0 is -50 mbar and stored 255 is 2500 mbar; shown in kPa.
        Assert.Equal(-5f, fuel.ColAxis.Display(0f), 3);
        Assert.Equal(250f, fuel.ColAxis.Display(255f), 3);
        Assert.Equal([15f, 45f, 251f], ParamAccess.AxisValues(small, fuel.ColAxis, 3).Select(v => MathF.Round(v)));
        Assert.Equal(6f * 4f, ParamAccess.CellDisplay(small, fuel, 1, 2));

        var wide = Assert.Single(pack.Resolve(large).Defs);
        Assert.Equal((0x2000, new ParamKind.Table(2, 6)), (wide.Offset, wide.Kind));
        Assert.Equal(0x1F00, wide.ColAxis.Offset);
        Assert.Equal(new ColumnMultiplier(0x200C, MultiplierKind.Linear), wide.Multiplier);
        Assert.Equal(7f, wide.ColAxis.Display(0f), 3);
        Assert.Equal(259f, wide.ColAxis.Display(255f), 3);
    }

    [Fact]
    public void AValueReadsACellInItsFormatPlusABias() {
        var rom = new RomImage([0x34, 0x12, 0x80, 0x02, 0x00]);
        RomValue.At At(int address, CellFormat cell, long bias = 0) => new(address, cell, bias);
        Assert.Equal(7, new RomValue.Constant(7).Read(null));
        Assert.Equal(0x34, At(0, CellFormat.U8).Read(rom));
        Assert.Equal(0x1234, At(0, CellFormat.U16LittleEndian).Read(rom));
        Assert.Equal(0x3412, At(0, CellFormat.U16BigEndian).Read(rom));
        Assert.Equal(0x1234 - 4, At(0, CellFormat.U16LittleEndian, -4).Read(rom));
        // The address of a value can be read from the ROM too: the word at 3 is 2, where 0x80 is.
        Assert.Equal(0x80, new RomValue.At(At(3, CellFormat.U16LittleEndian), CellFormat.U8, 0).Read(rom));
        Assert.Equal(0x0280, At(0, CellFormat.U16LittleEndian).Next().Read(rom));
        // Nothing to read from, or nothing there.
        Assert.Null(At(0, CellFormat.U8).Read(null));
        Assert.Null(At(5, CellFormat.U8).Read(rom));
        Assert.Null(At(4, CellFormat.U16LittleEndian).Read(rom));
        Assert.Null(new RomValue.At(At(0, CellFormat.U16LittleEndian), CellFormat.U8, 0).Read(rom));
    }

    [Fact]
    public void WhatCannotBeReadIsReportedAndLeftOut() {
        var pack = DefinitionPacks.Parse(Pack);
        // No image: a source project resolves by symbol and has none.
        Assert.Equal(["Fuel: cols is read from the ROM, and there is no image to read it from"], ParamDefinitions.Resolve(pack.Params, null, 0x8000).Problems);
        // A table that the ROM says has one column, and one it says lies past the end.
        Assert.Equal(["Fuel: a table needs at least 2 rows and 2 cols, not 2 x 1"], pack.Resolve(Rom(0x1000, 1, 0x0F00)).Problems);
        Assert.Equal(["Fuel: data is outside the 0x8000-byte ROM"], pack.Resolve(Rom(0x7FFE, 3, 0x0F00)).Problems);
        // An axis whose two ends are the same value has no scale.
        Assert.Equal(["Fuel: col_axis ends are both 100"], pack.Resolve(Rom(0x1000, 3, 0x0F00, low: 100, high: 100)).Problems);
        // The image is shorter than the address the pack reads from.
        var shortPack = DefinitionPacks.Parse(Pack.Replace("cols = { at = 0x21 }", "cols = { at = 0x9000 }"));
        Assert.Equal(["Fuel: cols is read from an address outside the image"], shortPack.Resolve(Rom(0x1000, 3, 0x0F00)).Problems);
    }

    /// <summary>A table the ROM points at must not be the code that points at it, nor the checksum byte.</summary>
    [Fact]
    public void AParameterMayNotCoverWhatThePackComparesOrCorrects() {
        var pack = DefinitionPacks.Parse(Pack);
        Assert.Equal(["Fuel: covers the byte at 0x10, which a fingerprint compares"], pack.Resolve(Rom(0x000C, 3, 0x0F00)).Problems);
        Assert.Empty(pack.Resolve(Rom(0x000C, 3, 0x0F00)).Defs);
        // The bytes a mask leaves out are free: they are the operands the table is found through.
        Assert.Empty(DefinitionPacks.Parse(Pack.Replace("address = { at = 0x11 }", "address = 0x11")).Resolve(Rom(0x1000, 2, 0x0F00)).Problems);
        Assert.Equal(["Fuel: covers the checksum byte at 0x7fff"], pack.Resolve(Rom(0x7FF7, 3, 0x0F00)).Problems);
        // A ROM that does not check itself has no such byte.
        var unchecked_ = DefinitionPacks.Parse(Pack + "\n[checksum]\nkind = 'none'\n");
        Assert.Empty(unchecked_.Resolve(Rom(0x7FF7, 3, 0x0F00)).Problems);
    }

    private static string Table(string change) => DefinitionPackTests.Minimal() + """

        [[param]]
        category = 'c'
        name = 'n'
        address = 0x100
        kind = 'table'
        rows = 2
        cols = 2
        row_axis = { label = 'r', values = [1, 2] }
        col_axis = { label = 'c', address = 0x200, encoding = 'bytes' }
        """.Replace("address = 0x100", change.StartsWith("address") ? change : "address = 0x100")
           .Replace("rows = 2", change.StartsWith("rows") ? change : "rows = 2")
           .Replace("col_axis = { label = 'c', address = 0x200, encoding = 'bytes' }", change.StartsWith("col_axis") ? change : "col_axis = { label = 'c', address = 0x200, encoding = 'bytes' }")
        + (change.StartsWith("multiplier") ? "\n" + change + "\n" : "\n");

    [Theory]
    [InlineData("address = { cell = 'u8' }", "`address` must be a number or { at = ... }")]
    [InlineData("address = { at = 0x10, size = 2 }", "unknown key `size`")]
    [InlineData("address = { at = 0x10, cell = 'u32' }", "unknown cell `u32`")]
    [InlineData("address = { at = -1 }", "`address.at` = -1 is outside")]
    [InlineData("rows = { at = 'x' }", "`rows.at` must be an integer")]
    [InlineData("col_axis = { label = 'c', address = 0x200, ends = { at = 0x1FC } }", "`col_axis.ends` goes with encoding bytes")]
    [InlineData("col_axis = { label = 'c', address = 0x200, encoding = 'bytes', ends = 0x1FC }", "`col_axis.ends` must be { at = ... }")]
    [InlineData("col_axis = { label = 'c', values = [1, 2], ends = { at = 0x1FC } }", "`col_axis.ends` goes with encoding bytes")]
    [InlineData("col_axis = { label = 'c', symbol = 's', encoding = 'bytes', ends = { at = 0x1FC } }", "only one at an address has ends")]
    [InlineData("multiplier = { symbol = 's', address = 0x300 }", "`multiplier` has a `symbol` or an `address`, not both")]
    public void RejectsAValueItCannotRead(string change, string reason) {
        var e = Assert.Throws<HreFormatException>(() => DefinitionPacks.Parse(Table(change)));
        Assert.Contains(reason, e.Message);
    }

    [Fact]
    public void ACellFormatAndABiasCanBeStated() {
        var pack = DefinitionPacks.Parse(Table("address = { at = 0x10, cell = 'u16be', bias = 0x20 }"));
        var data = new byte[0x8000];
        (data[0x10], data[0x11]) = (0x01, 0x00);
        var def = Assert.Single(pack.Resolve(new RomImage(data)).Defs);
        Assert.Equal(0x120, def.Offset);
        Assert.Equal(new RomValue.At(0x10, CellFormat.U16BigEndian, 0x20), pack.Params[0].Address);
        Assert.Equal<RomValue>(2, pack.Params[0].Rows);
    }
}
