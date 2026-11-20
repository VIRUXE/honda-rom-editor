namespace HRE.Core;

/// <summary>Half-open byte range <c>[Start, End)</c> inside a ROM.</summary>
public readonly record struct ByteRange(int Start, int End) {
    public int Length => End - Start;
    public bool Contains(int address) => address >= Start && address < End;
}

/// <summary>The ECU family a ROM runs on.</summary>
public enum RomPlatform {
    /// <summary>OBD1, OKI MSM66207 (<c>obd1-66207</c>).</summary>
    Obd1Oki66207,

    /// <summary>OBD0 ECUs built on an OKI 66k-family CPU (<c>obd0-66k</c>).</summary>
    Obd0Oki66k,

    /// <summary>OBD0 ECUs built on an 8051-family CPU (<c>obd0-8051</c>).</summary>
    Obd0I8051,
}

/// <summary>How one table cell, or a 16-bit scalar, is stored.</summary>
public enum CellFormat {
    /// <summary>One byte per cell.</summary>
    U8,

    /// <summary>Two bytes per cell, high byte first.</summary>
    U16BigEndian,

    /// <summary>Two bytes per cell, low byte first.</summary>
    U16LittleEndian,
}

/// <summary>How the breakpoints of an axis stored in the ROM are encoded.</summary>
public enum AxisEncoding {
    /// <summary>One big-endian 16-bit word per breakpoint.</summary>
    Words,

    /// <summary>One byte per breakpoint, each the increase over the one before; the first is the breakpoint itself.</summary>
    DeltaBytes,
}

/// <summary>How a per-column multiplier byte scales the cells of its column.</summary>
public enum MultiplierKind {
    /// <summary>Cell value times the byte.</summary>
    Linear,

    /// <summary>Cell value times two to the power of the byte.</summary>
    Exponent,
}

/// <summary>One multiplier byte per table column, stored in the ROM at <see cref="Offset"/>.</summary>
public sealed record ColumnMultiplier(int Offset, MultiplierKind Kind, string? Symbol = null);

public abstract record ParamKind {
    /// <summary>2D map stored row-major; the cell width is the parameter's <see cref="ParamDef.Cell"/>.</summary>
    public sealed record Table(int Rows, int Cols) : ParamKind;

    /// <summary>16-bit scalar. <c>Min == Max</c> makes it read-only.</summary>
    public sealed record Word(ushort Min, ushort Max) : ParamKind;
}

/// <summary>
/// A table axis: breakpoints stored in the ROM at <see cref="Offset"/> in the given
/// <see cref="Encoding"/>, or a fixed list of breakpoints for ROMs that do not store them.
/// Breakpoints increase.
/// </summary>
public sealed record Axis(string Label, string? Symbol, int Offset, IReadOnlyList<ushort>? Values, AxisEncoding Encoding = AxisEncoding.Words) {
    /// <summary>Placeholder for parameters that are not tables.</summary>
    public static Axis None { get; } = new("", null, 0, null);

    public static Axis At(string label, int offset, string? symbol = null, AxisEncoding encoding = AxisEncoding.Words) =>
        new(label, symbol, offset, null, encoding);

    public static Axis Fixed(string label, IReadOnlyList<ushort> values) => new(label, null, 0, values);

    /// <summary>Fixed breakpoints are not in the ROM and cannot be edited.</summary>
    public bool IsFixed => Values is not null;

    /// <summary>Bytes an axis of <paramref name="n"/> breakpoints occupies in the ROM; none when fixed.</summary>
    public int StoredLength(int n) => IsFixed ? 0 : Encoding == AxisEncoding.Words ? 2 * n : n;
}

public sealed record ParamDef {
    public required string Category { get; init; }
    public required string Name { get; init; }

    /// <summary>Symbol the offset was resolved from, when the definition came from a project.</summary>
    public string? Symbol { get; init; }

    public required int Offset { get; init; }
    public required ParamKind Kind { get; init; }

    /// <summary>
    /// Storage of a table's cells. For a word only the byte order matters: little-endian when
    /// <see cref="CellFormat.U16LittleEndian"/>, big-endian otherwise.
    /// </summary>
    public CellFormat Cell { get; init; } = CellFormat.U8;

    public string Unit { get; init; } = "";

    /// <summary>Displayed value = raw * Scale + Bias (raw already scaled by the column multiplier, if any).</summary>
    public float Scale { get; init; } = 1f;

    public float Bias { get; init; }
    public Axis RowAxis { get; init; } = Axis.None;
    public Axis ColAxis { get; init; } = Axis.None;

    /// <summary>Per-column multiplier bytes of a table, when the ROM has them.</summary>
    public ColumnMultiplier? Multiplier { get; init; }

    /// <summary>For cam-specific maps: which VTEC state the ECU reads this table in.</summary>
    public bool? Vtec { get; init; }

    /// <summary>Bytes per table cell.</summary>
    public int CellBytes => Cell == CellFormat.U8 ? 1 : 2;

    public bool LittleEndian => Cell == CellFormat.U16LittleEndian;

    /// <summary>Bytes of data (cells, or the two bytes of a word).</summary>
    public int Length => Kind switch {
        ParamKind.Table t => t.Rows * t.Cols * CellBytes,
        _ => 2,
    };

    public float Display(float raw) => raw * Scale + Bias;

    /// <summary>
    /// Byte ranges this parameter occupies: its data and, for tables, the axes and column
    /// multipliers stored in the ROM (fixed axes occupy nothing).
    /// </summary>
    public IReadOnlyList<ByteRange> Spans() {
        var spans = new List<ByteRange>(4) { new(Offset, Offset + Length) };
        if (Kind is ParamKind.Table t) {
            if (!RowAxis.IsFixed) spans.Add(new(RowAxis.Offset, RowAxis.Offset + RowAxis.StoredLength(t.Rows)));
            if (!ColAxis.IsFixed) spans.Add(new(ColAxis.Offset, ColAxis.Offset + ColAxis.StoredLength(t.Cols)));
            if (Multiplier is { } m) spans.Add(new(m.Offset, m.Offset + t.Cols));
        }
        return spans;
    }
}
