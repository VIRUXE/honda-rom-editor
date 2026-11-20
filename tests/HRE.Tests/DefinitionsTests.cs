using HRE.Core;
using HRE.Definitions;

namespace HRE.Tests;

public class DefinitionsTests {
    private static SymbolMap Map(params (string Name, long Value)[] symbols) =>
        new(0x8000, "main.asm", [.. symbols.Select(s => new SymbolEntry(s.Name, s.Value, SymbolKind.Label, "main.asm", 1))]);

    private static string Fixture() => File.ReadAllText(Path.Combine(Fixtures.MiniProject, "definitions.toml"));

    private static DefinitionsFile Table(string rows, string cols, string axes) =>
        ParamDefinitions.Parse($"[[param]]\ncategory='a'\nname='t'\nsymbol='t'\nkind='table'\nrows={rows}\ncols={cols}\n{axes}");

    private const string SymbolAxes = "row_axis = { label = 'r', symbol = 'ra' }\ncol_axis = { label = 'c', symbol = 'ca' }\n";

    private const string FixedAxes = """
        [[param]]
        category = 'Fuel'
        name = 'F'
        symbol = 'fuel'
        kind = 'table'
        rows = 2
        cols = 3
        row_axis = { label = 'RPM', values = [500, 1000] }
        col_axis = { label = 'MAP kPa', symbol = 'fuel_map' }

        """;

    [Fact]
    public void ParsesTheFixtureAndResolvesOffsets() {
        var file = ParamDefinitions.Parse(Fixture());
        Assert.Equal(2, file.Params.Count);
        var (defs, problems) = ParamDefinitions.Resolve(file, Map(("tbl_fuel", 0x100), ("tbl_fuel_rpm", 0x106), ("tbl_fuel_map", 0x10A), ("rev_limit", 0x110)));
        Assert.Empty(problems);
        Assert.Equal(0x100, defs[0].Offset);
        Assert.Equal(new ParamKind.Table(2, 3), defs[0].Kind);
        Assert.Equal(Axis.At("RPM", 0x106, "tbl_fuel_rpm"), defs[0].RowAxis);
        Assert.Equal("tbl_fuel", defs[0].Symbol);
        Assert.False(defs[0].Vtec);
        Assert.Equal(new ParamKind.Word(3000, 11000), defs[1].Kind);
        Assert.Equal(0x110, defs[1].Offset);
        Assert.Equal(1f, defs[1].Scale);
        Assert.Null(defs[1].Vtec);
    }

    [Fact]
    public void ResolveReportsMissingSymbols() {
        var (defs, problems) = ParamDefinitions.Resolve(ParamDefinitions.Parse(Fixture()), Map(("tbl_fuel", 0x100), ("tbl_fuel_rpm", 0x106), ("rev_limit", 0x110)));
        Assert.Equal(["tbl_fuel_map"], problems);
        Assert.Single(defs);
        Assert.Equal("Rev Limiter", defs[0].Name);
    }

    [Fact]
    public void OnlyLabelsAndEquatesWithNonNegativeValuesResolve() {
        var file = ParamDefinitions.Parse("[[param]]\ncategory='a'\nname='w'\nsymbol='w'\nkind='word'\n");
        SymbolMap With(long value, SymbolKind kind) => new(0x8000, "m", [new SymbolEntry("w", value, kind, "m", 1)]);
        Assert.Single(ParamDefinitions.Resolve(file, With(0x100, SymbolKind.Equ)).Defs);
        Assert.Equal(["w"], ParamDefinitions.Resolve(file, With(0x100, SymbolKind.Define)).Problems);
        Assert.Equal(["w"], ParamDefinitions.Resolve(file, With(0x100, SymbolKind.Sfr)).Problems);
        Assert.Equal(["w"], ParamDefinitions.Resolve(file, With(-1, SymbolKind.Equ)).Problems);
    }

    [Fact]
    public void UnknownKindAndMissingAxesAreReported() {
        var file = ParamDefinitions.Parse("[[param]]\ncategory='a'\nname='b'\nsymbol='c'\nkind='blob'\n\n[[param]]\ncategory='a'\nname='t'\nsymbol='d'\nkind='table'\nrows=2\ncols=2\n");
        var (defs, problems) = ParamDefinitions.Resolve(file, Map(("c", 1), ("d", 2)));
        Assert.Empty(defs);
        Assert.Equal(["c: unknown kind `blob`", "d: table needs row_axis and col_axis"], problems);
    }

    [Fact]
    public void ImpossibleTableSizesAreReported() {
        var symbols = Map(("t", 0x100), ("ra", 0x200), ("ca", 0x300));
        Assert.Equal(["t: a table needs at least 2 rows and 2 cols, not 0 x 2"], ParamDefinitions.Resolve(Table("0", "2", ""), symbols).Problems);
        Assert.Equal(["t: a table needs at least 2 rows and 2 cols, not 1 x 10"], ParamDefinitions.Resolve(Table("1", "10", SymbolAxes), symbols).Problems);
        var huge = (long.MaxValue / 2).ToString(System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal([$"t: {huge} x 3 cells is too large"], ParamDefinitions.Resolve(Table(huge, "3", SymbolAxes), symbols).Problems);
        var (defs, problems) = ParamDefinitions.Resolve(Table("2", "2", SymbolAxes), symbols);
        Assert.Empty(problems);
        Assert.Single(defs);
    }

    /// <summary>The data and every axis stored in the ROM must end inside it; fixed axes occupy no bytes.</summary>
    [Fact]
    public void SpansPastTheEndOfTheRomAreReported() {
        const string fixedAxes = "row_axis = { label = 'r', values = [1, 2] }\ncol_axis = { label = 'c', values = [1, 2, 3] }\n";
        Assert.Single(ParamDefinitions.Resolve(Table("2", "3", fixedAxes), Map(("t", 0x8000 - 6))).Defs);
        Assert.Equal(["t: data is outside the 0x8000-byte ROM"], ParamDefinitions.Resolve(Table("2", "3", fixedAxes), Map(("t", 0x8000 - 5))).Problems);
        Assert.Equal(["t: row_axis is outside the 0x8000-byte ROM"], ParamDefinitions.Resolve(Table("2", "3", SymbolAxes), Map(("t", 0x100), ("ra", 0x7FFE), ("ca", 0x300))).Problems);
        Assert.Equal(["t: col_axis is outside the 0x8000-byte ROM"], ParamDefinitions.Resolve(Table("2", "3", SymbolAxes), Map(("t", 0x100), ("ra", 0x200), ("ca", 0x7FFC))).Problems);
        var word = ParamDefinitions.Parse("[[param]]\ncategory='a'\nname='w'\nsymbol='w'\nkind='word'\n");
        Assert.Equal(["w: data is outside the 0x8000-byte ROM"], ParamDefinitions.Resolve(word, Map(("w", 0x7FFF))).Problems);
    }

    [Fact]
    public void WordWithMinAboveMaxIsReported() {
        DefinitionsFile Word(int min, int max) => ParamDefinitions.Parse($"[[param]]\ncategory='a'\nname='w'\nsymbol='w'\nkind='word'\nmin={min}\nmax={max}\n");
        Assert.Equal(["w: min 9000 is above max 3000"], ParamDefinitions.Resolve(Word(9000, 3000), Map(("w", 0x100))).Problems);
        var (defs, problems) = ParamDefinitions.Resolve(Word(4000, 4000), Map(("w", 0x100)));
        Assert.Empty(problems);
        Assert.Single(defs);
    }

    [Fact]
    public void FixedAxisSpecParses() {
        var file = ParamDefinitions.Parse(FixedAxes);
        var row = Assert.IsType<AxisSpec.Fixed>(file.Params[0].RowAxis);
        Assert.Equal("RPM", row.Label);
        Assert.Equal([500, 1000], row.Values);
        Assert.Equal(new AxisSpec.AtSymbol("MAP kPa", "fuel_map"), file.Params[0].ColAxis);
        var (defs, problems) = ParamDefinitions.Resolve(file, Map(("fuel", 0x100), ("fuel_map", 0x106)));
        Assert.Empty(problems);
        Assert.True(defs[0].RowAxis.IsFixed);
        Assert.Equal([500, 1000], defs[0].RowAxis.Values);
        Assert.Equal(0x106, defs[0].ColAxis.Offset);
        Assert.False(defs[0].ColAxis.IsFixed);
    }

    /// <summary>An axis is { label, symbol } or { label, values }: giving both, or a misspelt key, is an error.</summary>
    [Fact]
    public void AxisSpecWithBothSymbolAndValuesOrUnknownKeysIsRejected() {
        Assert.Throws<HreFormatException>(() => ParamDefinitions.Parse(FixedAxes.Replace("values = [500, 1000]", "symbol = 'fuel_rpm', values = [500, 1000]")));
        Assert.Throws<HreFormatException>(() => ParamDefinitions.Parse(FixedAxes.Replace("values = [500, 1000]", "values = [500, 1000], symbol = 'fuel_rpm'")));
        Assert.Throws<HreFormatException>(() => ParamDefinitions.Parse(FixedAxes.Replace("symbol = 'fuel_map'", "symbol = 'fuel_map', stride = 2")));
        Assert.Throws<HreFormatException>(() => ParamDefinitions.Parse(FixedAxes.Replace("[500, 1000]", "[500, 70000]")));
    }

    /// <summary>A fixed axis must have one value per row or column, increasing; otherwise the table is skipped.</summary>
    [Fact]
    public void FixedAxisWithWrongLengthOrOrderIsReported() {
        var symbols = Map(("fuel", 0x100), ("fuel_map", 0x106));
        Assert.Equal(["fuel: row_axis has 1 values, the table has 2"], ParamDefinitions.Resolve(ParamDefinitions.Parse(FixedAxes.Replace("[500, 1000]", "[500]")), symbols).Problems);
        Assert.Equal(["fuel: row_axis values must increase"], ParamDefinitions.Resolve(ParamDefinitions.Parse(FixedAxes.Replace("[500, 1000]", "[1000, 500]")), symbols).Problems);
    }

    [Fact]
    public void ScaleAndBiasAcceptIntegersAndFloats() {
        var file = ParamDefinitions.Parse("[[param]]\ncategory='a'\nname='w'\nsymbol='w'\nkind='word'\nscale=0.25\nbias=-6\nignored_key=1\n");
        Assert.Equal(0.25f, file.Params[0].Scale);
        Assert.Equal(-6f, file.Params[0].Bias);
        Assert.Throws<HreFormatException>(() => ParamDefinitions.Parse("[[param]]\ncategory='a'\nname='w'\nkind='word'\n"));
    }

    /// <summary>A key or a table may be given once. The second of two is an error, not the value quietly used.</summary>
    [Theory]
    [InlineData("platform = 'obd0-66k'\nplatform = 'obd1-66207'\n")]
    [InlineData("[[param]]\ncategory='a'\nname='w'\nsymbol='w'\nkind='word'\nkind='table'\n")]
    [InlineData("[[param]]\ncategory='a'\nname='w'\nsymbol='w'\nkind='word'\nrow_axis={label='r',label='s',symbol='x'}\n")]
    public void AKeyDefinedTwiceIsRejected(string text) {
        Assert.Contains("is already defined", Assert.Throws<HreFormatException>(() => ParamDefinitions.Parse(text)).Message);
    }

    [Fact]
    public void SymbolMapReadsTheAssemblerJson() {
        const string json = """
            { "version": 1, "rom_size": 32768, "entry": "main.asm",
              "symbols": [
                { "name": "tbl_fuel", "value": 256, "kind": "Label", "file": "main.asm", "line": 17, "extent": null },
                { "name": "FEATURE_X", "value": 1, "kind": "Define", "file": "config.inc", "line": 2, "extent": null } ],
              "blocks": [ { "start": 0, "end": 280, "file": "main.asm" } ] }
            """;
        var map = SymbolMap.FromJson(json);
        Assert.Equal(0x8000, map.RomSize);
        Assert.Equal(new SymbolEntry("tbl_fuel", 256, SymbolKind.Label, "main.asm", 17), map.Get("tbl_fuel"));
        Assert.Equal(SymbolKind.Define, map.Get("FEATURE_X")!.Kind);
        Assert.Null(map.Get("missing"));
        Assert.Equal([new SourceBlock(0, 280, "main.asm")], map.Blocks);
        Assert.Throws<HreFormatException>(() => SymbolMap.FromJson("{ \"version\": 2 }"));
        Assert.Throws<HreFormatException>(() => SymbolMap.FromJson("not json"));
    }
}
