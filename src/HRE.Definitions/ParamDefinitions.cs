using HRE.Core;
using Tomlyn.Model;

namespace HRE.Definitions;

/// <summary>
/// A table axis as written in a definitions file: breakpoints stored in the ROM at a symbol or an
/// absolute address (in the given encoding), or a fixed list of values. Exactly one of the three.
/// </summary>
public abstract record AxisSpec(string Label) {
    public sealed record AtSymbol(string Label, string Symbol, AxisEncoding Encoding = AxisEncoding.Words) : AxisSpec(Label);

    public sealed record AtAddress(string Label, int Address, AxisEncoding Encoding = AxisEncoding.Words) : AxisSpec(Label);

    public sealed record Fixed(string Label, IReadOnlyList<ushort> Values) : AxisSpec(Label);
}

/// <summary>Per-column multiplier bytes of a table, at a symbol or an absolute address (exactly one).</summary>
public sealed record MultiplierSpec(string? Symbol, int? Address, MultiplierKind Kind);

/// <summary>
/// One <c>[[param]]</c> entry, located by an assembler symbol (source projects) or by an absolute
/// address (definition packs). Exactly one of <see cref="Symbol"/> and <see cref="Address"/> is set.
/// </summary>
public sealed record ParamSpec {
    public required string Category { get; init; }
    public required string Name { get; init; }
    public string? Symbol { get; init; }
    public int? Address { get; init; }
    public required string Kind { get; init; }
    public long Rows { get; init; }
    public long Cols { get; init; }
    public ushort Min { get; init; }
    public ushort Max { get; init; } = ushort.MaxValue;

    /// <summary>Null means the default for the kind: one byte per table cell, a big-endian word.</summary>
    public CellFormat? Cell { get; init; }

    public string Unit { get; init; } = "";
    public float Scale { get; init; } = 1f;
    public float Bias { get; init; }
    public bool? Vtec { get; init; }
    public AxisSpec? RowAxis { get; init; }
    public AxisSpec? ColAxis { get; init; }
    public MultiplierSpec? Multiplier { get; init; }

    /// <summary>What problems are reported under: the symbol, or the name of an address-located parameter.</summary>
    public string Id => Symbol ?? Name;
}

public sealed record DefinitionsFile(IReadOnlyList<ParamSpec> Params) {
    /// <summary>The ECU family, when the file states one (<c>platform</c>).</summary>
    public RomPlatform? Platform { get; init; }
}

/// <summary>Parameter definitions (<c>definitions.toml</c>, and the <c>[[param]]</c> entries of a definition pack).</summary>
public static class ParamDefinitions {
    public static DefinitionsFile Parse(string text) {
        var root = TomlReader.ParseRoot(text);
        return new DefinitionsFile(ParseParams(root)) { Platform = ParsePlatform(root) };
    }

    /// <summary>The optional top-level <c>platform</c> key.</summary>
    internal static RomPlatform? ParsePlatform(TomlTable root) => TomlReader.OptionalString(root, "platform") switch {
        null => null,
        "obd1-66207" => RomPlatform.Obd1Oki66207,
        "obd0-66k" => RomPlatform.Obd0Oki66k,
        "obd0-8051" => RomPlatform.Obd0I8051,
        var other => throw new HreFormatException($"unknown platform `{other}`; expected obd1-66207, obd0-66k or obd0-8051"),
    };

    internal static IReadOnlyList<ParamSpec> ParseParams(TomlTable root) {
        var specs = new List<ParamSpec>();
        foreach (var t in TomlReader.Tables(root, "param")) {
            var symbol = TomlReader.OptionalString(t, "symbol");
            var address = TomlReader.OptionalInteger(t, "address", 0, int.MaxValue);
            if (symbol is null && address is null) throw new HreFormatException("missing `symbol`: a parameter needs a `symbol` or an `address`");
            if (symbol is not null && address is not null) throw new HreFormatException("a parameter has a `symbol` or an `address`, not both");
            specs.Add(new ParamSpec {
                Category = TomlReader.String(t, "category"),
                Name = TomlReader.String(t, "name"),
                Symbol = symbol,
                Address = (int?)address,
                Kind = TomlReader.String(t, "kind"),
                Rows = TomlReader.Integer(t, "rows", 0, long.MaxValue, 0),
                Cols = TomlReader.Integer(t, "cols", 0, long.MaxValue, 0),
                Min = (ushort)TomlReader.Integer(t, "min", 0, ushort.MaxValue, 0),
                Max = (ushort)TomlReader.Integer(t, "max", 0, ushort.MaxValue, ushort.MaxValue),
                Cell = TomlReader.OptionalString(t, "cell") switch {
                    null => null,
                    "u8" => CellFormat.U8,
                    "u16be" => CellFormat.U16BigEndian,
                    "u16le" => CellFormat.U16LittleEndian,
                    var other => throw new HreFormatException($"unknown cell `{other}`; expected u8, u16be or u16le"),
                },
                Unit = TomlReader.String(t, "unit", ""),
                Scale = TomlReader.Number(t, "scale", 1f),
                Bias = TomlReader.Number(t, "bias", 0f),
                Vtec = TomlReader.OptionalBool(t, "vtec"),
                RowAxis = ParseAxis(t, "row_axis"),
                ColAxis = ParseAxis(t, "col_axis"),
                Multiplier = ParseMultiplier(t),
            });
        }
        return specs;
    }

    /// <summary>
    /// An axis is <c>{ label, symbol }</c>, <c>{ label, address }</c> or <c>{ label, values }</c>,
    /// with an optional <c>encoding</c> (<c>words</c>, <c>delta-bytes</c>, or <c>values</c> for a
    /// fixed list). Two locations, an encoding that contradicts the location, or any other key, is
    /// an error.
    /// </summary>
    private static AxisSpec? ParseAxis(TomlTable param, string key) {
        if (!param.TryGetValue(key, out var value)) return null;
        if (value is not TomlTable axis) throw new HreFormatException($"`{key}` must be a table");
        var label = TomlReader.String(axis, "label");
        var encoding = TomlReader.OptionalString(axis, "encoding");
        var keys = axis.Keys.Where(k => k is not ("label" or "encoding")).ToList();
        if (keys is ["values"] && axis["values"] is TomlArray values && encoding is null or "values") {
            return new AxisSpec.Fixed(label, [.. values.Select(v => (ushort)TomlReader.AsInteger(v, $"{key}.values", 0, ushort.MaxValue))]);
        }
        AxisEncoding? stored = encoding switch {
            null or "words" => AxisEncoding.Words,
            "delta-bytes" => AxisEncoding.DeltaBytes,
            _ => null,
        };
        if (stored is { } e && keys is ["symbol"]) return new AxisSpec.AtSymbol(label, TomlReader.String(axis, "symbol"), e);
        if (stored is { } f && keys is ["address"]) return new AxisSpec.AtAddress(label, (int)TomlReader.Integer(axis, "address", 0, int.MaxValue), f);
        throw new HreFormatException($"`{key}` must be {{ label, symbol }}, {{ label, address }} or {{ label, values }}, with encoding words, delta-bytes or values to match");
    }

    /// <summary><c>multiplier = { symbol | address, kind }</c>; <c>kind</c> is <c>linear</c> (the default) or <c>exponent</c>.</summary>
    private static MultiplierSpec? ParseMultiplier(TomlTable param) {
        if (!param.TryGetValue("multiplier", out var value)) return null;
        if (value is not TomlTable m) throw new HreFormatException("`multiplier` must be a table");
        var symbol = TomlReader.OptionalString(m, "symbol");
        var address = TomlReader.OptionalInteger(m, "address", 0, int.MaxValue);
        if ((symbol is null) == (address is null)) throw new HreFormatException("`multiplier` needs a `symbol` or an `address`, not both");
        var kind = TomlReader.String(m, "kind", "linear") switch {
            "linear" => MultiplierKind.Linear,
            "exponent" => MultiplierKind.Exponent,
            var other => throw new HreFormatException($"unknown multiplier kind `{other}`; expected linear or exponent"),
        };
        return new MultiplierSpec(symbol, (int?)address, kind);
    }

    /// <summary>Resolves the parameters of a source project against its symbol map.</summary>
    public static (IReadOnlyList<ParamDef> Defs, IReadOnlyList<string> Problems) Resolve(DefinitionsFile file, SymbolMap map) =>
        Resolve(file.Params, map, map.RomSize);

    /// <summary>
    /// Resolves every parameter that can be located. Problems are reported as
    /// <c>"&lt;id&gt;: &lt;reason&gt;"</c> (or the bare name of a missing symbol; without a
    /// <paramref name="map"/> every symbol is missing) and that parameter is left out. A parameter
    /// is also left out when its size is impossible: a table smaller than 2 x 2 or too large, a
    /// word with min above max or one-byte cells, or any span (data, stored axis or multipliers)
    /// reaching past <paramref name="romSize"/>.
    /// </summary>
    public static (IReadOnlyList<ParamDef> Defs, IReadOnlyList<string> Problems) Resolve(IReadOnlyList<ParamSpec> specs, SymbolMap? map, int romSize) {
        var defs = new List<ParamDef>();
        var problems = new List<string>();
        foreach (var p in specs) {
            ParamKind kind;
            switch (p.Kind) {
                case "table" when p.Rows < 2 || p.Cols < 2:
                    problems.Add($"{p.Id}: a table needs at least 2 rows and 2 cols, not {p.Rows} x {p.Cols}");
                    continue;
                case "table" when p.Rows > int.MaxValue || p.Cols > int.MaxValue || p.Rows * p.Cols > int.MaxValue / 2:
                    problems.Add($"{p.Id}: {p.Rows} x {p.Cols} cells is too large");
                    continue;
                case "table":
                    kind = new ParamKind.Table((int)p.Rows, (int)p.Cols);
                    break;
                case "word" when p.Min > p.Max:
                    problems.Add($"{p.Id}: min {p.Min} is above max {p.Max}");
                    continue;
                case "word" when p.Cell == CellFormat.U8:
                    problems.Add($"{p.Id}: a word is u16be or u16le, not u8");
                    continue;
                case "word":
                    kind = new ParamKind.Word(p.Min, p.Max);
                    break;
                default:
                    problems.Add($"{p.Id}: unknown kind `{p.Kind}`");
                    continue;
            }
            if (Locate(map, p.Symbol, p.Address, problems) is not { } offset) continue;
            Axis rowAxis = Axis.None, colAxis = Axis.None;
            ColumnMultiplier? multiplier = null;
            if (kind is ParamKind.Table table) {
                if (p.RowAxis is null || p.ColAxis is null) {
                    problems.Add($"{p.Id}: table needs row_axis and col_axis");
                    continue;
                }
                var row = ResolveAxis(p.RowAxis, table.Rows, "row_axis", p.Id, map, problems);
                var col = ResolveAxis(p.ColAxis, table.Cols, "col_axis", p.Id, map, problems);
                if (row is null || col is null) continue;
                (rowAxis, colAxis) = (row, col);
                if (p.Multiplier is { } m) {
                    if (Locate(map, m.Symbol, m.Address, problems) is not { } at) continue;
                    multiplier = new ColumnMultiplier(at, m.Kind, m.Symbol);
                }
            } else if (p.Multiplier is not null) {
                problems.Add($"{p.Id}: only a table has a multiplier");
                continue;
            }
            var def = new ParamDef {
                Category = p.Category,
                Name = p.Name,
                Symbol = p.Symbol,
                Offset = offset,
                Kind = kind,
                Cell = p.Cell ?? (kind is ParamKind.Table ? CellFormat.U8 : CellFormat.U16BigEndian),
                Unit = p.Unit,
                Scale = p.Scale,
                Bias = p.Bias,
                RowAxis = rowAxis,
                ColAxis = colAxis,
                Multiplier = multiplier,
                Vtec = p.Vtec,
            };
            if (CheckSpans(def, romSize) is { } reason) problems.Add($"{p.Id}: {reason}");
            else defs.Add(def);
        }
        return (defs, problems);
    }

    /// <summary>
    /// The address itself, or the value of a symbol that is a label or EQU with a non-negative
    /// value; otherwise the symbol's name is reported.
    /// </summary>
    private static int? Locate(SymbolMap? map, string? symbol, int? address, List<string> problems) {
        if (symbol is null) return address;
        if (map?.Get(symbol) is { Value: >= 0 and <= int.MaxValue, Kind: SymbolKind.Label or SymbolKind.Equ } entry) return (int)entry.Value;
        problems.Add(symbol);
        return null;
    }

    private static Axis? ResolveAxis(AxisSpec spec, int n, string which, string owner, SymbolMap? map, List<string> problems) {
        switch (spec) {
            case AxisSpec.AtSymbol s:
                return Locate(map, s.Symbol, null, problems) is { } offset ? Axis.At(s.Label, offset, s.Symbol, s.Encoding) : null;
            case AxisSpec.AtAddress a:
                return Axis.At(a.Label, a.Address, null, a.Encoding);
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
            if (!Fits(d.RowAxis.Offset, d.RowAxis.StoredLength(table.Rows))) return Outside("row_axis");
            if (!Fits(d.ColAxis.Offset, d.ColAxis.StoredLength(table.Cols))) return Outside("col_axis");
            if (d.Multiplier is { } m && !Fits(m.Offset, table.Cols)) return Outside("multiplier");
        }
        return null;
    }
}
