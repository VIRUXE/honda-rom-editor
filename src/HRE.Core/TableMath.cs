namespace HRE.Core;

/// <summary>Where an operating point falls in a table, and what the table reads there.</summary>
public static class TableMath {
    /// <summary>Fractional index of <paramref name="v"/> along increasing breakpoints, clamped to the ends.</summary>
    public static float AxisPos(ReadOnlySpan<float> points, float v) {
        var last = points.Length - 1;
        if (v <= points[0]) return 0f;
        if (v >= points[last]) return last;
        // points[i] <= v < points[i + 1]
        var i = 0;
        while (points[i + 1] <= v) i++;
        return i + (v - points[i]) / (points[i + 1] - points[i]);
    }

    /// <summary>
    /// Fractional (row, col) of an operating point inside a table, clamped to its edges.
    /// Null for non-tables and tables outside the image.
    /// </summary>
    public static (float Row, float Col)? TablePos(RomImage image, ParamDef def, float rowValue, float colValue) {
        if (def.Kind is not ParamKind.Table table || !ParamAccess.Contains(image, def)) return null;
        var rows = ParamAccess.AxisValues(image, def.RowAxis, table.Rows);
        var cols = ParamAccess.AxisValues(image, def.ColAxis, table.Cols);
        return (AxisPos(rows, rowValue), AxisPos(cols, colValue));
    }

    /// <summary>Bilinear table lookup, in display units.</summary>
    public static float? Lookup(RomImage image, ParamDef def, float rowValue, float colValue) {
        var pos = TablePos(image, def, rowValue, colValue);
        if (pos is null) return null;
        var (r, c) = pos.Value;
        var table = (ParamKind.Table)def.Kind;
        int r0 = (int)MathF.Floor(r), c0 = (int)MathF.Floor(c);
        int r1 = Math.Min(r0 + 1, table.Rows - 1), c1 = Math.Min(c0 + 1, table.Cols - 1);
        float fr = r - r0, fc = c - c0;
        float At(int row, int col) => ParamAccess.Cell(image, def, row, col);
        var top = At(r0, c0) + (At(r0, c1) - At(r0, c0)) * fc;
        var bottom = At(r1, c0) + (At(r1, c1) - At(r1, c0)) * fc;
        return def.Display(top + (bottom - top) * fr);
    }
}
