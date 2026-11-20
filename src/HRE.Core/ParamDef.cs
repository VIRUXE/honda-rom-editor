namespace HRE.Core;

/// <summary>Half-open byte range <c>[Start, End)</c> inside a ROM.</summary>
public readonly record struct ByteRange(int Start, int End) {
    public int Length => End - Start;
    public bool Contains(int address) => address >= Start && address < End;
}

public abstract record ParamKind {
    /// <summary>2D map stored row-major, one byte per cell.</summary>
    public sealed record Table(int Rows, int Cols) : ParamKind;

    /// <summary>Big-endian 16-bit scalar. <c>Min == Max</c> makes it read-only.</summary>
    public sealed record Word(ushort Min, ushort Max) : ParamKind;
}

/// <summary>
/// A table axis: one big-endian 16-bit breakpoint per row or column stored in the ROM at
/// <see cref="Offset"/>, or a fixed list of breakpoints for ROMs that do not store them that way.
/// Breakpoints increase.
/// </summary>
public sealed record Axis(string Label, string? Symbol, int Offset, IReadOnlyList<ushort>? Values) {
    /// <summary>Placeholder for parameters that are not tables.</summary>
    public static Axis None { get; } = new("", null, 0, null);

    public static Axis At(string label, int offset, string? symbol = null) => new(label, symbol, offset, null);

    public static Axis Fixed(string label, IReadOnlyList<ushort> values) => new(label, null, 0, values);

    /// <summary>Fixed breakpoints are not in the ROM and cannot be edited.</summary>
    public bool IsFixed => Values is not null;
}

public sealed record ParamDef {
    public required string Category { get; init; }
    public required string Name { get; init; }

    /// <summary>Symbol the offset was resolved from, when the definition came from a project.</summary>
    public string? Symbol { get; init; }

    public required int Offset { get; init; }
    public required ParamKind Kind { get; init; }
    public string Unit { get; init; } = "";

    /// <summary>Displayed value = raw * Scale + Bias.</summary>
    public float Scale { get; init; } = 1f;

    public float Bias { get; init; }
    public Axis RowAxis { get; init; } = Axis.None;
    public Axis ColAxis { get; init; } = Axis.None;

    /// <summary>For cam-specific maps: which VTEC state the ECU reads this table in.</summary>
    public bool? Vtec { get; init; }

    /// <summary>Bytes of data (cells, or the two bytes of a word).</summary>
    public int Length => Kind switch {
        ParamKind.Table t => t.Rows * t.Cols,
        _ => 2,
    };

    public float Display(float raw) => raw * Scale + Bias;

    /// <summary>
    /// Byte ranges this parameter occupies: its data and, for tables, the axes stored in the ROM
    /// (fixed axes occupy nothing).
    /// </summary>
    public IReadOnlyList<ByteRange> Spans() {
        var spans = new List<ByteRange>(3) { new(Offset, Offset + Length) };
        if (Kind is ParamKind.Table t) {
            if (!RowAxis.IsFixed) spans.Add(new(RowAxis.Offset, RowAxis.Offset + 2 * t.Rows));
            if (!ColAxis.IsFixed) spans.Add(new(ColAxis.Offset, ColAxis.Offset + 2 * t.Cols));
        }
        return spans;
    }
}
