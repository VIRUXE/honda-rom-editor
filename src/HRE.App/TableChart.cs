using System.Drawing.Drawing2D;
using HRE.Core;

namespace HRE.App;

/// <summary>
/// A picture of a table, beside its cells: the base of the graph and of the surface. A picture
/// holds nothing of its own. It draws the cells of the <see cref="TableGrid"/> it was made for,
/// marks the cell and the selection of that grid, and changes cells through it, so that the two
/// can never disagree and every change is the same undoable step as in the cells. What it shows
/// is in the cells for a screen reader; the picture itself says in words where the smallest and
/// the largest values are.
/// </summary>
public abstract class TableChart : Panel {
    protected TableChart(TableGrid grid, string kind) {
        Grid = grid;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
        TabStop = true;
        AccessibleRole = AccessibleRole.Chart;
        AccessibleName = $"{kind} of {grid.Def.Name}";
        grid.Document.Changed += Changed;
        grid.CurrentCellChanged += OnGridMoved;
        grid.SelectionChanged += OnGridMoved;
    }

    /// <summary>Something happened that a screen reader should tell: which cell the picture moved to.</summary>
    public event Action<string>? Announced;

    protected TableGrid Grid { get; }
    protected RomImage Image => Grid.Document.Image;
    protected ParamDef Def => Grid.Def;
    protected int Rows => Grid.RowCount;
    protected int Cols => Grid.ColumnCount;

    /// <summary>The cell the keyboard is on in the grid; the first cell when it is on none.</summary>
    protected CellRef Current => Grid.CurrentCellAddress is { X: >= 0, Y: >= 0 } at ? new CellRef(at.Y, at.X) : new CellRef(0, 0);

    /// <summary>Where the picture is drawn: the control without its frame and a little room inside it.</summary>
    protected Rectangle Area => Rectangle.Inflate(ClientRectangle, -Em(0.6f), -Em(0.5f));

    /// <summary>Takes in the table as it is now: after a change, an undo, a redo, or a move in the cells.</summary>
    public void Changed() {
        AccessibleDescription = Describe();
        Invalidate();
    }

    /// <summary>Draws the picture inside <paramref name="area"/>.</summary>
    protected abstract void Draw(Graphics g, Rectangle area);

    /// <summary>What the picture shows, in a sentence or two, for a screen reader.</summary>
    protected abstract string Describe();

    /// <summary>A length in units of the line height, which follows scaling and text size.</summary>
    protected int Em(float lines) => (int)Math.Round(Font.Height * lines);

    protected float Value(CellRef cell) => ParamAccess.CellDisplay(Image, Def, cell.Row, cell.Col);

    protected string ValueText(float value) => TableEdit.Text(value, TableEdit.Decimals(Image, Def));

    /// <summary>The smallest and the largest value of the table, and the cells that hold them.</summary>
    protected (float Min, float Max, CellRef Lowest, CellRef Highest) Extent() {
        var (min, max, lowest, highest) = (float.MaxValue, float.MinValue, default(CellRef), default(CellRef));
        for (var row = 0; row < Rows; row++) {
            for (var col = 0; col < Cols; col++) {
                var value = Value(new CellRef(row, col));
                if (value < min) (min, lowest) = (value, new CellRef(row, col));
                if (value > max) (max, highest) = (value, new CellRef(row, col));
            }
        }
        return (min, max, lowest, highest);
    }

    /// <summary>"Smallest 12 at RPM 500, MAP 30; largest ...": where the ends of the table are.</summary>
    protected string ExtentWords() {
        var (min, max, lowest, highest) = Extent();
        return $"Smallest {ValueText(min)} at {Grid.CellName(lowest.Row, lowest.Col)}; largest {ValueText(max)} at {Grid.CellName(highest.Row, highest.Col)}.";
    }

    /// <summary>The colour of a value, or of a mark on it, as the cells have it. Under a contrast theme marks take the colour of text.</summary>
    protected static Color HeatOf(float value, float min, float max) =>
        DarkTheme.HighContrast ? DarkTheme.Text : DarkTheme.Heat(max > min ? (value - min) / (max - min) : 0.5);

    /// <summary>Moves the grid to a cell, and tells a screen reader which it is and what it holds.</summary>
    protected void GoTo(CellRef cell) {
        Grid.CurrentCell = Grid[cell.Col, cell.Row];
        Announced?.Invoke($"{Grid.CellName(cell.Row, cell.Col)}: {ValueText(Value(cell))}");
    }

    protected Size Measure(string text) => TextRenderer.MeasureText(text, Font, Size.Empty, TextFlags);

    /// <summary>Writes a text with its top at <paramref name="y"/>: starting at x (align -1), centred on it (0) or ending at it (1).</summary>
    protected void Write(Graphics g, string text, float x, float y, Color color, int align = -1) {
        var width = Measure(text).Width;
        var left = align < 0 ? x : align == 0 ? x - width / 2f : x - width;
        // Never over the frame, whatever the picture asks for.
        left = Math.Clamp(left, 2f, Math.Max(2f, ClientSize.Width - width - 2f));
        TextRenderer.DrawText(g, text, Font, new Point((int)MathF.Round(left), (int)MathF.Round(y)), color, TextFlags);
    }

    protected override void OnPaint(PaintEventArgs e) {
        var g = e.Graphics;
        g.Clear(DarkTheme.Background);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        Draw(g, Area);
        g.SmoothingMode = SmoothingMode.None;
        // The frame says where the picture is; it is heavier and lighter while the keyboard is in it.
        var width = Focused ? Math.Max(2, Font.Height / 9) : 1;
        using var pen = new Pen(Focused ? DarkTheme.Text : DarkTheme.ControlBorder, width) { Alignment = PenAlignment.Inset };
        g.DrawRectangle(pen, 0, 0, ClientSize.Width - (width > 1 ? 0 : 1), ClientSize.Height - (width > 1 ? 0 : 1));
    }

    protected override void OnGotFocus(EventArgs e) {
        base.OnGotFocus(e);
        Invalidate();
    }

    protected override void OnLostFocus(EventArgs e) {
        base.OnLostFocus(e);
        Invalidate();
    }

    protected override void OnFontChanged(EventArgs e) {
        base.OnFontChanged(e);
        Invalidate();
    }

    protected override AccessibleObject CreateAccessibilityInstance() => new Accessible(this);

    /// <summary>The arrow keys work inside the picture; Tab leaves it.</summary>
    protected override bool IsInputKey(Keys keyData) =>
        (keyData & Keys.KeyCode) is Keys.Left or Keys.Right or Keys.Up or Keys.Down || base.IsInputKey(keyData);

    protected override void Dispose(bool disposing) {
        if (disposing) {
            Grid.Document.Changed -= Changed;
            Grid.CurrentCellChanged -= OnGridMoved;
            Grid.SelectionChanged -= OnGridMoved;
        }
        base.Dispose(disposing);
    }

    private const TextFormatFlags TextFlags = TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine;

    private void OnGridMoved(object? sender, EventArgs e) => Changed();

    /// <summary>
    /// What a screen reader is given: that the picture takes the keyboard, which a panel does
    /// not say of itself, and its description as help text too, since screen readers differ in
    /// which of the two they read.
    /// </summary>
    private sealed class Accessible(TableChart owner) : ControlAccessibleObject(owner) {
        public override string? Help => owner.AccessibleDescription;

        public override AccessibleStates State => base.State | AccessibleStates.Focusable | (owner.Focused ? AccessibleStates.Focused : AccessibleStates.None);
    }
}

/// <summary>The arithmetic of the pictures: the scale of a graph, and where a point of a surface falls on the screen.</summary>
public static class ChartMath {
    /// <summary>How high the largest value of a table stands over the smallest, in sides of the base of the surface.</summary>
    public const float SurfaceHeight = 0.6f;

    /// <summary>
    /// A scale for values from <paramref name="min"/> to <paramref name="max"/>: lines a round
    /// step apart (1, 2 or 5 times a power of ten), at most <paramref name="most"/> of them between
    /// the two ends, which are the lines just under the smallest and just over the largest value.
    /// </summary>
    public static (float Low, float High, float Step) Scale(float min, float max, int most) {
        // A table of one value still gets a scale around it.
        if (!(max > min)) (min, max) = (min - 1f, min + 1f);
        var rough = (double)(max - min) / Math.Max(most, 1);
        var magnitude = Math.Pow(10, Math.Floor(Math.Log10(rough)));
        var step = magnitude * 10;
        foreach (var round in (double[])[1, 2, 5]) {
            if (round * magnitude >= rough) {
                step = round * magnitude;
                break;
            }
        }
        return ((float)(Math.Floor(min / step) * step), (float)(Math.Ceiling(max / step) * step), (float)step);
    }

    /// <summary>
    /// Where a point of the surface falls on the screen, seen from far away. The base of the
    /// surface is a square of side 1 around the origin, with x to the right, y towards the
    /// viewer and z up. It is turned by <paramref name="turn"/> degrees around the upright axis
    /// and seen from <paramref name="tilt"/> degrees above the ground. Screen x grows to the
    /// right and screen y downwards; <c>Near</c> grows towards the viewer, and what is nearer
    /// hides what is farther.
    /// </summary>
    public static (float X, float Y, float Near) Project(float x, float y, float z, float turn, float tilt) {
        var (sin, cos) = MathF.SinCos(turn * MathF.PI / 180f);
        var (up, flat) = MathF.SinCos(tilt * MathF.PI / 180f);
        var (across, towards) = (x * cos - y * sin, x * sin + y * cos);
        return (across, towards * up - z * flat, towards);
    }
}
