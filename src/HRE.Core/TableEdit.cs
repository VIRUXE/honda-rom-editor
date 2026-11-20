using System.Globalization;

namespace HRE.Core;

/// <summary>A cell of a table.</summary>
public readonly record struct CellRef(int Row, int Col);

/// <summary>
/// Table cells as the user reads and writes them: the text of a cell, the number a text stands
/// for, and the byte writes that set, raise, lower or paste cells. Values are in display units;
/// what is stored is rounded to the nearest step and kept inside what a cell can hold.
/// </summary>
public static class TableEdit {
    /// <summary>More decimals than this tell nothing a tuner can use.</summary>
    private const int MostDecimals = 3;

    private static readonly CultureInfo Format = CultureInfo.InvariantCulture;

    /// <summary>
    /// How many decimals the cells of a table are written with: as few as tell two neighbouring
    /// stored values apart, so that raising a cell by one step always changes what is shown.
    /// </summary>
    public static int Decimals(RomImage image, ParamDef def) {
        var table = (ParamKind.Table)def.Kind;
        var step = float.MaxValue;
        for (var col = 0; col < table.Cols; col++) {
            var size = MathF.Abs(def.Scale * ParamAccess.ColumnFactor(image, def, col));
            if (size > 0f) step = MathF.Min(step, size);
        }
        if (step == float.MaxValue) return 0;
        for (var decimals = 0; decimals < MostDecimals; decimals++) {
            var scaled = step * MathF.Pow(10f, decimals);
            if (MathF.Abs(scaled - MathF.Round(scaled)) < 0.001f) return decimals;
        }
        return MostDecimals;
    }

    /// <summary>A number with at most <paramref name="decimals"/> decimals, a point as the decimal mark, no trailing zeros.</summary>
    public static string Text(float value, int decimals) {
        var text = Math.Round(value, decimals, MidpointRounding.AwayFromZero).ToString("0." + new string('#', decimals), Format);
        return text == "-0" ? "0" : text;
    }

    /// <summary>What a cell shows.</summary>
    public static string CellText(RomImage image, ParamDef def, int row, int col) =>
        Text(ParamAccess.CellDisplay(image, def, row, col), Decimals(image, def));

    /// <summary>
    /// The number a typed or pasted text stands for; null when it is not a number. A comma is
    /// taken as the decimal mark too: it is what the number pad types on many keyboards.
    /// </summary>
    public static float? Parse(string text) {
        var trimmed = text.Trim();
        if (!trimmed.Contains('.')) trimmed = trimmed.Replace(',', '.');
        return float.TryParse(trimmed, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, Format, out var value) && float.IsFinite(value) ? value : null;
    }

    /// <summary>The smallest and the largest value a cell of column <paramref name="col"/> can show.</summary>
    public static (float Min, float Max) Range(RomImage image, ParamDef def, int col) {
        var factor = ParamAccess.ColumnFactor(image, def, col);
        var (a, b) = (def.Display(0f), def.Display(ParamAccess.CellMax(def) * factor));
        return (MathF.Min(a, b), MathF.Max(a, b));
    }

    /// <summary>Whether a cell of column <paramref name="col"/> can hold <paramref name="display"/>, give or take half a step.</summary>
    public static bool Fits(RomImage image, ParamDef def, int col, float display) {
        var raw = Stored(image, def, col, display);
        return raw is null || (raw >= -0.5 && raw <= ParamAccess.CellMax(def) + 0.5);
    }

    /// <summary>The byte writes that set every one of <paramref name="cells"/> to <paramref name="display"/>.</summary>
    public static List<(int Address, byte Value)> Set(RomImage image, ParamDef def, IEnumerable<CellRef> cells, float display) {
        var writes = new List<(int, byte)>();
        foreach (var cell in cells) AddSet(writes, image, def, cell, display);
        return writes;
    }

    /// <summary>
    /// The byte writes that raise (or, with a negative count, lower) every one of
    /// <paramref name="cells"/> by <paramref name="steps"/> of the smallest step it can take.
    /// "Raise" is what the user sees: with a negative scale the stored value goes down.
    /// </summary>
    public static List<(int Address, byte Value)> Step(RomImage image, ParamDef def, IEnumerable<CellRef> cells, int steps) {
        var writes = new List<(int, byte)>();
        var direction = def.Scale < 0f ? -steps : steps;
        foreach (var cell in cells) {
            writes.AddRange(ParamAccess.CellWrites(def, cell.Row, cell.Col, ParamAccess.Cell(image, def, cell.Row, cell.Col) + direction));
        }
        return writes;
    }

    /// <summary>
    /// The cells as text for the clipboard: the rectangle around them, one line per row, values
    /// separated by tabs, as spreadsheets and the tuning programs exchange them. A cell of the
    /// rectangle that is not among <paramref name="cells"/> is left empty.
    /// </summary>
    public static string CopyText(RomImage image, ParamDef def, IReadOnlyCollection<CellRef> cells) {
        if (cells.Count == 0) return "";
        var chosen = cells.ToHashSet();
        var (top, bottom) = (cells.Min(c => c.Row), cells.Max(c => c.Row));
        var (left, right) = (cells.Min(c => c.Col), cells.Max(c => c.Col));
        var decimals = Decimals(image, def);
        var lines = new List<string>();
        for (var row = top; row <= bottom; row++) {
            var values = new List<string>();
            for (var col = left; col <= right; col++) {
                values.Add(chosen.Contains(new CellRef(row, col)) ? Text(ParamAccess.CellDisplay(image, def, row, col), decimals) : "");
            }
            lines.Add(string.Join('\t', values));
        }
        return string.Join("\r\n", lines);
    }

    /// <summary>
    /// The byte writes that paste clipboard text. A single number goes into every one of
    /// <paramref name="cells"/>. A block of numbers is laid down from <paramref name="cells"/>'
    /// top left corner; what falls outside the table, and what is not a number, is left out.
    /// <c>Count</c> is how many cells were given a value.
    /// </summary>
    public static (List<(int Address, byte Value)> Writes, int Count) Paste(RomImage image, ParamDef def, IReadOnlyCollection<CellRef> cells, string text) {
        var writes = new List<(int, byte)>();
        if (cells.Count == 0) return (writes, 0);
        var table = (ParamKind.Table)def.Kind;
        var block = text.ReplaceLineEndings("\n").TrimEnd('\n').Split('\n').Select(line => line.Split('\t').Select(Parse).ToArray()).ToArray();
        if (block is [[{ } single]]) {
            foreach (var cell in cells) AddSet(writes, image, def, cell, single);
            return (writes, cells.Count);
        }
        var (top, left) = (cells.Min(c => c.Row), cells.Min(c => c.Col));
        var count = 0;
        for (var r = 0; r < block.Length && top + r < table.Rows; r++) {
            for (var c = 0; c < block[r].Length && left + c < table.Cols; c++) {
                if (block[r][c] is not { } value) continue;
                AddSet(writes, image, def, new CellRef(top + r, left + c), value);
                count++;
            }
        }
        return (writes, count);
    }

    /// <summary>What a cell would have to store to show <paramref name="display"/>, unrounded; null when nothing it stores changes what it shows.</summary>
    private static double? Stored(RomImage image, ParamDef def, int col, float display) {
        var factor = ParamAccess.ColumnFactor(image, def, col);
        return factor == 0f || def.Scale == 0f ? null : (display - def.Bias) / (double)def.Scale / factor;
    }

    private static void AddSet(List<(int, byte)> writes, RomImage image, ParamDef def, CellRef cell, float display) {
        if (Stored(image, def, cell.Col, display) is not { } stored) return;
        var raw = (int)Math.Clamp(Math.Round(stored, MidpointRounding.AwayFromZero), 0, ParamAccess.CellMax(def));
        writes.AddRange(ParamAccess.CellWrites(def, cell.Row, cell.Col, raw));
    }
}
