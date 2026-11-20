using HRE.Core;
using Tomlyn.Model;

namespace HRE.Definitions;

/// <summary>
/// A table axis as written in <c>definitions.toml</c>: breakpoints stored in the ROM at a symbol
/// (16-bit big-endian words), or a fixed list of values. Exactly one of the two.
/// </summary>
public abstract record AxisSpec(string Label) {
    public sealed record AtSymbol(string Label, string Symbol) : AxisSpec(Label);

    public sealed record Fixed(string Label, IReadOnlyList<ushort> Values) : AxisSpec(Label);
}

/// <summary>One <c>[[param]]</c> entry of <c>definitions.toml</c>, bound to an assembler symbol.</summary>
public sealed record ParamSpec {
    public required string Category { get; init; }
    public required string Name { get; init; }
    public required string Symbol { get; init; }
    public required string Kind { get; init; }
    public long Rows { get; init; }
    public long Cols { get; init; }
    public ushort Min { get; init; }
    public ushort Max { get; init; } = ushort.MaxValue;
    public string Unit { get; init; } = "";
    public float Scale { get; init; } = 1f;
    public float Bias { get; init; }
    public bool? Vtec { get; init; }
    public AxisSpec? RowAxis { get; init; }
    public AxisSpec? ColAxis { get; init; }
}

public sealed record DefinitionsFile(IReadOnlyList<ParamSpec> Params);

/// <summary>Parameter definitions bound to assembler symbols (<c>definitions.toml</c>).</summary>
public static class Definitions {
    public static DefinitionsFile Parse(string text) {
        var specs = new List<ParamSpec>();
        foreach (var t in TomlReader.Tables(TomlReader.ParseRoot(text), "param")) {
            specs.Add(new ParamSpec {
                Category = TomlReader.String(t, "category"),
                Name = TomlReader.String(t, "name"),
                Symbol = TomlReader.String(t, "symbol"),
                Kind = TomlReader.String(t, "kind"),
                Rows = TomlReader.Integer(t, "rows", 0, long.MaxValue, 0),
                Cols = TomlReader.Integer(t, "cols", 0, long.MaxValue, 0),
                Min = (ushort)TomlReader.Integer(t, "min", 0, ushort.MaxValue, 0),
                Max = (ushort)TomlReader.Integer(t, "max", 0, ushort.MaxValue, ushort.MaxValue),
                Unit = TomlReader.String(t, "unit", ""),
                Scale = TomlReader.Number(t, "scale", 1f),
                Bias = TomlReader.Number(t, "bias", 0f),
                Vtec = TomlReader.OptionalBool(t, "vtec"),
                RowAxis = ParseAxis(t, "row_axis"),
                ColAxis = ParseAxis(t, "col_axis"),
            });
        }
        return new DefinitionsFile(specs);
    }

    /// <summary>An axis is <c>{ label, symbol }</c> or <c>{ label, values }</c>; both, or any other key, is an error.</summary>
    private static AxisSpec? ParseAxis(TomlTable param, string key) {
        if (!param.TryGetValue(key, out var value)) return null;
        if (value is not TomlTable axis) throw new HreFormatException($"`{key}` must be a table");
        var label = TomlReader.String(axis, "label");
        var keys = axis.Keys.Where(k => k != "label").ToList();
        if (keys is ["symbol"]) return new AxisSpec.AtSymbol(label, TomlReader.String(axis, "symbol"));
        if (keys is ["values"] && axis["values"] is TomlArray values) {
            return new AxisSpec.Fixed(label, [.. values.Select(v => (ushort)TomlReader.AsInteger(v, $"{key}.values", 0, ushort.MaxValue))]);
        }
        throw new HreFormatException($"`{key}` must be {{ label, symbol }} or {{ label, values }}");
    }

    /// <summary>
    /// Resolves every parameter whose symbols exist. Problems are reported as
    /// <c>"&lt;symbol&gt;: &lt;reason&gt;"</c> (or the bare name of a missing symbol) and that
    /// parameter is left out. A parameter is also left out when its size is impossible: a table
    /// smaller than 2 x 2 or too large, a word with min above max, or any span (data or stored
    /// axis) reaching past the map's ROM size.
    /// </summary>
    public static (IReadOnlyList<ParamDef> Defs, IReadOnlyList<string> Problems) Resolve(DefinitionsFile file, SymbolMap map) {
        var defs = new List<ParamDef>();
        var problems = new List<string>();
        foreach (var p in file.Params) {
            ParamKind kind;
            switch (p.Kind) {
                case "table" when p.Rows < 2 || p.Cols < 2:
                    problems.Add($"{p.Symbol}: a table needs at least 2 rows and 2 cols, not {p.Rows} x {p.Cols}");
                    continue;
                case "table" when p.Rows > int.MaxValue || p.Cols > int.MaxValue || p.Rows * p.Cols > int.MaxValue:
                    problems.Add($"{p.Symbol}: {p.Rows} x {p.Cols} cells is too large");
                    continue;
                case "table":
                    kind = new ParamKind.Table((int)p.Rows, (int)p.Cols);
                    break;
                case "word" when p.Min > p.Max:
                    problems.Add($"{p.Symbol}: min {p.Min} is above max {p.Max}");
                    continue;
                case "word":
                    kind = new ParamKind.Word(p.Min, p.Max);
                    break;
                default:
                    problems.Add($"{p.Symbol}: unknown kind `{p.Kind}`");
                    continue;
            }
            if (Lookup(map, p.Symbol, problems) is not { } offset) continue;
            Axis rowAxis = Axis.None, colAxis = Axis.None;
            if (kind is ParamKind.Table table) {
                if (p.RowAxis is null || p.ColAxis is null) {
                    problems.Add($"{p.Symbol}: table needs row_axis and col_axis");
                    continue;
                }
                var row = ResolveAxis(p.RowAxis, table.Rows, "row_axis", p.Symbol, map, problems);
                var col = ResolveAxis(p.ColAxis, table.Cols, "col_axis", p.Symbol, map, problems);
                if (row is null || col is null) continue;
                (rowAxis, colAxis) = (row, col);
            }
            var def = new ParamDef {
                Category = p.Category,
                Name = p.Name,
                Symbol = p.Symbol,
                Offset = offset,
                Kind = kind,
                Unit = p.Unit,
                Scale = p.Scale,
                Bias = p.Bias,
                RowAxis = rowAxis,
                ColAxis = colAxis,
                Vtec = p.Vtec,
            };
            if (CheckSpans(def, map.RomSize) is { } reason) problems.Add($"{p.Symbol}: {reason}");
            else defs.Add(def);
        }
        return (defs, problems);
    }

    /// <summary>A symbol that is a label or EQU with a non-negative value; otherwise its name is reported.</summary>
    private static int? Lookup(SymbolMap map, string symbol, List<string> problems) {
        if (map.Get(symbol) is { Value: >= 0 and <= int.MaxValue, Kind: SymbolKind.Label or SymbolKind.Equ } entry) return (int)entry.Value;
        problems.Add(symbol);
        return null;
    }

    private static Axis? ResolveAxis(AxisSpec spec, int n, string which, string owner, SymbolMap map, List<string> problems) {
        switch (spec) {
            case AxisSpec.AtSymbol s:
                return Lookup(map, s.Symbol, problems) is { } offset ? Axis.At(s.Label, offset, s.Symbol) : null;
            case AxisSpec.Fixed f when f.Values.Count != n:
                problems.Add($"{owner}: {which} has {f.Values.Count} values, the table has {n}");
                return null;
            case AxisSpec.Fixed f when !f.Values.Zip(f.Values.Skip(1)).All(pair => pair.First < pair.Second):
                problems.Add($"{owner}: {which} values must increase");
                return null;
            case AxisSpec.Fixed f:
                return Axis.Fixed(f.Label, f.Values);
            default:
                throw new InvalidOperationException("unknown axis spec");
        }
    }

    /// <summary>Null when every byte the parameter occupies lies inside the ROM; the reason otherwise.</summary>
    private static string? CheckSpans(ParamDef d, int romSize) {
        string Outside(string what) => $"{what} is outside the 0x{romSize:x}-byte ROM";
        bool Fits(long start, long length) => start + length <= romSize;
        if (!Fits(d.Offset, d.Length)) return Outside("data");
        if (d.Kind is ParamKind.Table table) {
            if (!d.RowAxis.IsFixed && !Fits(d.RowAxis.Offset, 2L * table.Rows)) return Outside("row_axis");
            if (!d.ColAxis.IsFixed && !Fits(d.ColAxis.Offset, 2L * table.Cols)) return Outside("col_axis");
        }
        return null;
    }
}
