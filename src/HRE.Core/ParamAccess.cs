namespace HRE.Core;

/// <summary>Reads parameters and axes out of a ROM image, and produces the byte writes that change them.</summary>
public static class ParamAccess {
    /// <summary>Whether every byte the parameter occupies lies inside the image.</summary>
    public static bool Contains(RomImage image, ParamDef def) {
        foreach (var span in def.Spans())
            if (span.Start < 0 || span.End > image.Length) return false;
        return true;
    }

    /// <summary>Raw cell of a table.</summary>
    public static int Cell(RomImage image, ParamDef def, int row, int col) {
        var at = CellAddress(def, row, col);
        return def.Cell == CellFormat.U8 ? image[at] : Word(image, at, def.LittleEndian);
    }

    /// <summary>Largest raw value a cell of the table can hold.</summary>
    public static int CellMax(ParamDef def) => def.Cell == CellFormat.U8 ? byte.MaxValue : ushort.MaxValue;

    /// <summary>The byte writes that store <paramref name="value"/> in a table cell, clamped to what the cell can hold.</summary>
    public static (int Address, byte Value)[] CellWrites(ParamDef def, int row, int col, int value) {
        var at = CellAddress(def, row, col);
        var raw = Math.Clamp(value, 0, CellMax(def));
        return def.Cell == CellFormat.U8 ? [(at, (byte)raw)] : WordWrites(at, (ushort)raw, def.LittleEndian);
    }

    /// <summary>
    /// What a cell of column <paramref name="col"/> is multiplied by before scale and bias: 1 for a
    /// table without column multipliers.
    /// </summary>
    public static float ColumnFactor(RomImage image, ParamDef def, int col) {
        if (def.Multiplier is not { } m) return 1f;
        var raw = image[m.Offset + col];
        return m.Kind == MultiplierKind.Linear ? raw : MathF.Pow(2f, raw);
    }

    /// <summary>A table cell in display units.</summary>
    public static float CellDisplay(RomImage image, ParamDef def, int row, int col) =>
        def.Display(Cell(image, def, row, col) * ColumnFactor(image, def, col));

    /// <summary>The raw value of a word parameter.</summary>
    public static ushort Word(RomImage image, ParamDef def) => Word(image, def.Offset, def.LittleEndian);

    /// <summary>The two byte writes that store a word parameter.</summary>
    public static (int Address, byte Value)[] WordWrites(ParamDef def, ushort value) => WordWrites(def.Offset, value, def.LittleEndian);

    /// <summary>The largest value a single-byte breakpoint holds: a stored zero after the first breakpoint.</summary>
    private const int ByteAxisEnd = 256;

    /// <summary>Breakpoint <paramref name="i"/> of an axis, as stored.</summary>
    public static ushort AxisValue(RomImage image, Axis axis, int i) {
        if (axis.Values is { } values) return values[i];
        switch (axis.Encoding) {
            case AxisEncoding.Words:
                return image.Word(axis.Offset + 2 * i);
            case AxisEncoding.Bytes or AxisEncoding.LogBytes:
                var stored = image[axis.Offset + i];
                return stored == 0 && i > 0 ? (ushort)ByteAxisEnd : stored;
            default:
                var sum = 0;
                for (var k = 0; k <= i; k++) sum += image[axis.Offset + k];
                return (ushort)Math.Min(sum, ushort.MaxValue);
        }
    }

    /// <summary>All <paramref name="n"/> breakpoints of an axis, as stored.</summary>
    public static float[] AxisStoredValues(RomImage image, Axis axis, int n) {
        var values = new float[n];
        for (var i = 0; i < n; i++) values[i] = AxisValue(image, axis, i);
        return values;
    }

    /// <summary>All <paramref name="n"/> breakpoints of an axis, in display units.</summary>
    public static float[] AxisValues(RomImage image, Axis axis, int n) {
        var values = new float[n];
        for (var i = 0; i < n; i++) values[i] = axis.Display(AxisValue(image, axis, i));
        return values;
    }

    /// <summary>
    /// The value to store for breakpoint <paramref name="i"/> of an <paramref name="n"/>-point axis
    /// when the user asks for <paramref name="wanted"/> (both as stored): clamped strictly between
    /// its neighbours so the axis keeps increasing, and to what the encoding can hold. With delta
    /// bytes each step still fits one byte; with single bytes only the last breakpoint can be 256.
    /// A fixed axis keeps its current value.
    /// </summary>
    public static ushort ClampAxisValue(RomImage image, Axis axis, int n, int i, ushort wanted) {
        var current = AxisValue(image, axis, i);
        if (axis.IsFixed) return current;
        int? prev = i > 0 ? AxisValue(image, axis, i - 1) : null;
        int? next = i + 1 < n ? AxisValue(image, axis, i + 1) : null;
        var lo = prev is { } p ? Math.Min(p + 1, ushort.MaxValue) : 0;
        var hi = next is { } q ? Math.Max(q - 1, 0) : ushort.MaxValue;
        switch (axis.Encoding) {
            case AxisEncoding.DeltaBytes:
                hi = Math.Min(hi, (prev ?? 0) + byte.MaxValue);
                if (next is { } after) lo = Math.Max(lo, after - byte.MaxValue);
                break;
            case AxisEncoding.Bytes or AxisEncoding.LogBytes:
                // A stored zero means 256 everywhere but in the first breakpoint.
                hi = Math.Min(hi, i > 0 ? ByteAxisEnd : byte.MaxValue);
                break;
        }
        return lo <= hi ? (ushort)Math.Clamp(wanted, lo, hi) : current;
    }

    /// <summary>
    /// The byte writes that set breakpoint <paramref name="i"/> of an <paramref name="n"/>-point
    /// axis to <paramref name="wanted"/> (as stored), clamped as <see cref="ClampAxisValue"/> does.
    /// The other breakpoints keep their values: with delta bytes the following step is adjusted
    /// to match. A fixed axis has no writes.
    /// </summary>
    public static (int Address, byte Value)[] AxisWrites(RomImage image, Axis axis, int n, int i, ushort wanted) {
        if (axis.IsFixed) return [];
        var value = ClampAxisValue(image, axis, n, i, wanted);
        if (axis.Encoding == AxisEncoding.Words) return WordWrites(axis.Offset + 2 * i, value);
        // 256 is stored as zero.
        if (axis.Encoding is AxisEncoding.Bytes or AxisEncoding.LogBytes) return [(axis.Offset + i, (byte)value)];
        var prev = i > 0 ? AxisValue(image, axis, i - 1) : 0;
        var own = (axis.Offset + i, (byte)(value - prev));
        return i + 1 < n ? [own, (axis.Offset + i + 1, (byte)(AxisValue(image, axis, i + 1) - value))] : [own];
    }

    /// <summary>The two byte writes that store a 16-bit word, big-endian unless stated.</summary>
    public static (int Address, byte Value)[] WordWrites(int offset, ushort value, bool littleEndian = false) => littleEndian
        ? [(offset, (byte)value), (offset + 1, (byte)(value >> 8))]
        : [(offset, (byte)(value >> 8)), (offset + 1, (byte)value)];

    private static int CellAddress(ParamDef def, int row, int col) {
        var table = (ParamKind.Table)def.Kind;
        return def.Offset + (row * table.Cols + col) * def.CellBytes;
    }

    private static ushort Word(RomImage image, int at, bool littleEndian) =>
        littleEndian ? (ushort)(image[at + 1] << 8 | image[at]) : image.Word(at);
}
