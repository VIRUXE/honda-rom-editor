namespace HRE.Core;

/// <summary>Reads parameters and axes out of a ROM image.</summary>
public static class ParamAccess {
    /// <summary>Whether every byte the parameter occupies lies inside the image.</summary>
    public static bool Contains(RomImage image, ParamDef def) {
        foreach (var span in def.Spans())
            if (span.Start < 0 || span.End > image.Length) return false;
        return true;
    }

    /// <summary>Raw cell of a table.</summary>
    public static byte Cell(RomImage image, ParamDef def, int row, int col) {
        var table = (ParamKind.Table)def.Kind;
        return image[def.Offset + row * table.Cols + col];
    }

    /// <summary>Breakpoint <paramref name="i"/> of an axis.</summary>
    public static ushort AxisValue(RomImage image, Axis axis, int i) =>
        axis.Values is { } values ? values[i] : image.Word(axis.Offset + 2 * i);

    /// <summary>All <paramref name="n"/> breakpoints of an axis.</summary>
    public static float[] AxisValues(RomImage image, Axis axis, int n) {
        var values = new float[n];
        for (var i = 0; i < n; i++) values[i] = AxisValue(image, axis, i);
        return values;
    }

    /// <summary>
    /// The value to store for breakpoint <paramref name="i"/> of an <paramref name="n"/>-point axis
    /// when the user asks for <paramref name="wanted"/>: clamped strictly between its neighbours so
    /// the axis keeps increasing. A fixed axis keeps its current value.
    /// </summary>
    public static ushort ClampAxisValue(RomImage image, Axis axis, int n, int i, ushort wanted) {
        var current = AxisValue(image, axis, i);
        if (axis.IsFixed) return current;
        var lo = i > 0 ? Math.Min(AxisValue(image, axis, i - 1) + 1, ushort.MaxValue) : 0;
        var hi = i + 1 < n ? Math.Max(AxisValue(image, axis, i + 1) - 1, 0) : ushort.MaxValue;
        return lo <= hi ? (ushort)Math.Clamp(wanted, lo, hi) : current;
    }

    /// <summary>The two byte writes that store a big-endian word.</summary>
    public static (int Address, byte Value)[] WordWrites(int offset, ushort value) =>
        [(offset, (byte)(value >> 8)), (offset + 1, (byte)value)];
}
