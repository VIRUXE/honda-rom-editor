using HRE.Core;
using Tomlyn.Model;

namespace HRE.Definitions;

/// <summary>
/// A table axis as written in a definitions file: breakpoints stored in the ROM at a symbol or an
/// address (in the given encoding), or a fixed list of values. Exactly one of the three.
/// </summary>
public abstract record AxisSpec(string Label) {
    /// <summary>Displayed breakpoint = stored value x Scale + Bias. A fixed list is already in display units.</summary>
    public float Scale { get; init; } = 1f;

    public float Bias { get; init; }

    public sealed record AtSymbol(string Label, string Symbol, AxisEncoding Encoding = AxisEncoding.Words) : AxisSpec(Label);

    public sealed record AtAddress(string Label, RomValue Address, AxisEncoding Encoding = AxisEncoding.Words) : AxisSpec(Label) {
        /// <summary>
        /// Where the ROM keeps, in two cells one after the other, what stored 0 and stored 255
        /// stand for. The stored value is put on that line before <see cref="AxisSpec.Scale"/>
        /// and <see cref="AxisSpec.Bias"/> apply.
        /// </summary>
        public RomValue.At? Ends { get; init; }
    }

    public sealed record Fixed(string Label, IReadOnlyList<ushort> Values) : AxisSpec(Label);
}

/// <summary>
/// Per-column multiplier bytes of a table: at a symbol, at an address, or, with neither, in the
/// row after the last row of cells.
/// </summary>
public sealed record MultiplierSpec(string? Symbol, RomValue? Address, MultiplierKind Kind);

/// <summary>
/// One <c>[[param]]</c> entry, located by an assembler symbol (source projects) or by an address
/// (definition packs). Exactly one of <see cref="Symbol"/> and <see cref="Address"/> is set. The
/// address, and the rows and columns of a table, are given outright or read from the ROM.
/// </summary>
public sealed record ParamSpec {
    public required string Category { get; init; }
    public required string Name { get; init; }

    /// <summary>What the parameter is, in plain words.</summary>
    public string Doc { get; init; } = "";

    public string? Symbol { get; init; }
    public RomValue? Address { get; init; }
    public required string Kind { get; init; }
    public RomValue Rows { get; init; } = 0;
    public RomValue Cols { get; init; } = 0;
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
            var address = t.TryGetValue("address", out var where) ? RomValue.Parse(where, "address", CellFormat.U16LittleEndian, 0, int.MaxValue) : null;
            if (symbol is null && address is null) throw new HreFormatException("missing `symbol`: a parameter needs a `symbol` or an `address`");
            if (symbol is not null && address is not null) throw new HreFormatException("a parameter has a `symbol` or an `address`, not both");
            specs.Add(new ParamSpec {
                Category = TomlReader.String(t, "category"),
                Name = TomlReader.String(t, "name"),
                Doc = TomlReader.String(t, "doc", ""),
                Symbol = symbol,
                Address = address,
                Kind = TomlReader.String(t, "kind"),
                Rows = t.TryGetValue("rows", out var rows) ? RomValue.Parse(rows, "rows", CellFormat.U8, 0, long.MaxValue) : 0,
                Cols = t.TryGetValue("cols", out var cols) ? RomValue.Parse(cols, "cols", CellFormat.U8, 0, long.MaxValue) : 0,
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
    /// with an optional <c>encoding</c> (<c>words</c>, <c>delta-bytes</c>, <c>bytes</c>,
    /// <c>log-bytes</c>, or <c>values</c> for a fixed list). A stored axis may give a
    /// <c>scale</c> and a <c>bias</c>; one stored as <c>bytes</c> at an address may give
    /// <c>ends</c>, where the ROM keeps what stored 0 and stored 255 stand for. Two locations, an
    /// encoding that contradicts the location, or any other key, is an error.
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
            "bytes" => AxisEncoding.Bytes,
            "log-bytes" => AxisEncoding.LogBytes,
            _ => null,
        };
        var scale = TomlReader.Number(axis, "scale", 1f);
        // A zero scale would show every breakpoint as the same value.
        if (scale == 0f) throw new HreFormatException($"`{key}.scale` must not be 0");
        var bias = TomlReader.Number(axis, "bias", 0f);
        keys.RemoveAll(k => k is "scale" or "bias");
        if (stored is { } e && keys is ["symbol"]) return new AxisSpec.AtSymbol(label, TomlReader.String(axis, "symbol"), e) { Scale = scale, Bias = bias };
        RomValue.At? ends = null;
        if (axis.TryGetValue("ends", out var range)) {
            // The two ends are what stored 0 and stored 255 stand for, so they need plain bytes.
            if (stored != AxisEncoding.Bytes) throw new HreFormatException($"`{key}.ends` goes with encoding bytes");
            ends = (range is TomlTable ? RomValue.Parse(range, $"{key}.ends", CellFormat.U16LittleEndian, 0, 0) : null) as RomValue.At
                ?? throw new HreFormatException($"`{key}.ends` must be {{ at = ... }}, the address the ROM keeps the two ends at");
            keys.Remove("ends");
        }
        if (stored is { } f && keys is ["address"]) {
            return new AxisSpec.AtAddress(label, RomValue.Parse(axis["address"], $"{key}.address", CellFormat.U16LittleEndian, 0, int.MaxValue), f) { Scale = scale, Bias = bias, Ends = ends };
        }
        throw new HreFormatException($"`{key}` must be {{ label, symbol }}, {{ label, address }} or {{ label, values }}, with encoding words, delta-bytes, bytes, log-bytes or values to match; only a stored axis has a scale and a bias, and only one at an address has ends");
    }

    /// <summary>
    /// <c>multiplier = { symbol | address, kind }</c>; <c>kind</c> is <c>linear</c> (the default)
    /// or <c>exponent</c>. With neither a symbol nor an address the multipliers are the row after
    /// the last row of cells.
    /// </summary>
    private static MultiplierSpec? ParseMultiplier(TomlTable param) {
        if (!param.TryGetValue("multiplier", out var value)) return null;
        if (value is not TomlTable m) throw new HreFormatException("`multiplier` must be a table");
        var symbol = TomlReader.OptionalString(m, "symbol");
        var address = m.TryGetValue("address", out var where) ? RomValue.Parse(where, "multiplier.address", CellFormat.U16LittleEndian, 0, int.MaxValue) : null;
        if (symbol is not null && address is not null) throw new HreFormatException("`multiplier` has a `symbol` or an `address`, not both");
        var kind = TomlReader.String(m, "kind", "linear") switch {
            "linear" => MultiplierKind.Linear,
            "exponent" => MultiplierKind.Exponent,
            var other => throw new HreFormatException($"unknown multiplier kind `{other}`; expected linear or exponent"),
        };
        return new MultiplierSpec(symbol, address, kind);
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
    /// reaching past <paramref name="romSize"/>. What a file says the ROM keeps is read from
    /// <paramref name="image"/>; a parameter that needs it is left out when there is no image or
    /// the value lies outside it.
    /// </summary>
    public static (IReadOnlyList<ParamDef> Defs, IReadOnlyList<string> Problems) Resolve(IReadOnlyList<ParamSpec> specs, SymbolMap? map, int romSize, RomImage? image = null) {
        var defs = new List<ParamDef>();
        var problems = new List<string>();
        foreach (var p in specs) {
            if (ResolveOne(p, map, romSize, image, problems) is { } def) defs.Add(def);
        }
        return (defs, problems);
    }

    private static ParamDef? ResolveOne(ParamSpec p, SymbolMap? map, int romSize, RomImage? image, List<string> problems) {
        long? Read(RomValue value, string what) {
            if (value.Read(image) is { } read) return read;
            problems.Add(image is null ? $"{p.Id}: {what} is read from the ROM, and there is no image to read it from" : $"{p.Id}: {what} is read from an address outside the image");
            return null;
        }

        int? Locate(string? symbol, RomValue? address, string what) {
            if (symbol is not null) {
                if (map?.Get(symbol) is { Value: >= 0 and <= int.MaxValue, Kind: SymbolKind.Label or SymbolKind.Equ } entry) return (int)entry.Value;
                problems.Add(symbol);
                return null;
            }
            if (Read(address!, what) is not { } at) return null;
            if (at is >= 0 and <= int.MaxValue) return (int)at;
            problems.Add($"{p.Id}: {what} read from the ROM is {at}, not an address");
            return null;
        }

        Axis? ResolveAxis(AxisSpec spec, int n, string which) {
            switch (spec) {
                case AxisSpec.AtSymbol s:
                    return Locate(s.Symbol, null, which) is { } offset ? Axis.At(s.Label, offset, s.Symbol, s.Encoding) with { Scale = s.Scale, Bias = s.Bias } : null;
                case AxisSpec.AtAddress a:
                    if (Locate(null, a.Address, which) is not { } at) return null;
                    var (scale, bias) = (a.Scale, a.Bias);
                    if (a.Ends is { } ends) {
                        if (Read(ends, $"{which} ends") is not { } low || Read(ends.Next(), $"{which} ends") is not { } high) return null;
                        if (low == high) {
                            problems.Add($"{p.Id}: {which} ends are both {low}");
                            return null;
                        }
                        // Stored 0 stands for the low end and stored 255 for the high one.
                        (scale, bias) = ((high - low) / 255f * a.Scale, low * a.Scale + a.Bias);
                    }
                    return Axis.At(a.Label, at, null, a.Encoding) with { Scale = scale, Bias = bias };
                case AxisSpec.Fixed f when f.Values.Count != n:
                    problems.Add($"{p.Id}: {which} has {f.Values.Count} values, the table has {n}");
                    return null;
                case AxisSpec.Fixed f when !f.Values.Zip(f.Values.Skip(1)).All(pair => pair.First < pair.Second):
                    problems.Add($"{p.Id}: {which} values must increase");
                    return null;
                case AxisSpec.Fixed f:
                    return Axis.Fixed(f.Label, f.Values);
                default:
                    throw new InvalidOperationException("unknown axis spec");
            }
        }

        ParamKind kind;
        switch (p.Kind) {
            case "table":
                if (Read(p.Rows, "rows") is not { } rows || Read(p.Cols, "cols") is not { } cols) return null;
                if (rows < 2 || cols < 2) {
                    problems.Add($"{p.Id}: a table needs at least 2 rows and 2 cols, not {rows} x {cols}");
                    return null;
                }
                if (rows > int.MaxValue || cols > int.MaxValue || rows * cols > int.MaxValue / 2) {
                    problems.Add($"{p.Id}: {rows} x {cols} cells is too large");
                    return null;
                }
                kind = new ParamKind.Table((int)rows, (int)cols);
                break;
            case "word" when p.Min > p.Max:
                problems.Add($"{p.Id}: min {p.Min} is above max {p.Max}");
                return null;
            case "word" when p.Cell == CellFormat.U8:
                problems.Add($"{p.Id}: a word is u16be or u16le, not u8");
                return null;
            case "word":
                kind = new ParamKind.Word(p.Min, p.Max);
                break;
            default:
                problems.Add($"{p.Id}: unknown kind `{p.Kind}`");
                return null;
        }
        if (Locate(p.Symbol, p.Address, "address") is not { } offset) return null;
        var cell = p.Cell ?? (kind is ParamKind.Table ? CellFormat.U8 : CellFormat.U16BigEndian);
        Axis rowAxis = Axis.None, colAxis = Axis.None;
        ColumnMultiplier? multiplier = null;
        if (kind is ParamKind.Table table) {
            if (p.RowAxis is null || p.ColAxis is null) {
                problems.Add($"{p.Id}: table needs row_axis and col_axis");
                return null;
            }
            var row = ResolveAxis(p.RowAxis, table.Rows, "row_axis");
            var col = ResolveAxis(p.ColAxis, table.Cols, "col_axis");
            if (row is null || col is null) return null;
            (rowAxis, colAxis) = (row, col);
            if (p.Multiplier is { Symbol: null, Address: null } follows) {
                // The row after the last: where the lookup of these ECUs finds one multiplier per column.
                multiplier = new ColumnMultiplier(offset + table.Rows * table.Cols * (cell == CellFormat.U8 ? 1 : 2), follows.Kind);
            } else if (p.Multiplier is { } m) {
                if (Locate(m.Symbol, m.Address, "multiplier") is not { } at) return null;
                multiplier = new ColumnMultiplier(at, m.Kind, m.Symbol);
            }
        } else if (p.Multiplier is not null) {
            problems.Add($"{p.Id}: only a table has a multiplier");
            return null;
        }
        var def = new ParamDef {
            Category = p.Category,
            Name = p.Name,
            Doc = p.Doc,
            Symbol = p.Symbol,
            Offset = offset,
            Kind = kind,
            Cell = cell,
            Unit = p.Unit,
            Scale = p.Scale,
            Bias = p.Bias,
            RowAxis = rowAxis,
            ColAxis = colAxis,
            Multiplier = multiplier,
            Vtec = p.Vtec,
        };
        if (CheckSpans(def, romSize) is not { } reason) return def;
        problems.Add($"{p.Id}: {reason}");
        return null;
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
