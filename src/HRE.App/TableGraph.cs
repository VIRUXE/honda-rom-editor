using HRE.Core;

namespace HRE.App;

/// <summary>
/// A table as lines: one line for each column, one point for each row, evenly spaced as the
/// cells are. The line of the cell the keyboard is on stands out and carries its points, which
/// can be dragged up and down; the other lines are there to compare it with. A table of a single
/// row is one line along its columns.
/// </summary>
public sealed class TableGraph : TableChart {
    /// <summary>Whether a line runs down a column, with a point for each row.</summary>
    private readonly bool alongRows;

    private Drag? drag;

    public TableGraph(TableGrid grid) : base(grid, "Graph") {
        alongRows = Rows > 1;
        Changed();
    }

    private int Points => alongRows ? Rows : Cols;
    private int Lines => alongRows ? Cols : Rows;

    /// <summary>The line that stands out: that of the cell being dragged, or of the cell the keyboard is on.</summary>
    private int ShownLine => Place(drag?.Cell ?? Current).Line;

    /// <summary>Where the point of a cell is in the picture, as it is drawn now.</summary>
    internal PointF PointOf(int row, int col) {
        var plot = PlotIn(Area);
        var cell = new CellRef(row, col);
        return new PointF(plot.X(Place(cell).Point), plot.Y(Shown(cell)));
    }

    /// <summary>
    /// The mouse button went down. On a point, that point's cell becomes the cell of the grid
    /// and a drag begins; on a point of a cell that is selected with others, the selection stays
    /// and all of it is dragged.
    /// </summary>
    internal void Press(Point at) {
        if (CellAt(at) is not { } cell) return;
        if (!Grid[cell.Col, cell.Row].Selected || Grid.SelectedCells.Count == 1) GoTo(cell);
        if (!Grid.ReadOnly) drag = new Drag(cell, at.Y, PlotIn(Area).PerPixel);
        Invalidate();
    }

    /// <summary>The mouse moved with the button down: the dragged cells follow it, shown where they would be stored.</summary>
    internal void DragTo(Point at) {
        if (this.drag is not { } drag) return;
        var (least, most) = TableEdit.Range(Image, Def, drag.Cell.Col);
        var value = Value(drag.Cell);
        drag.Amount = Math.Clamp((drag.StartY - at.Y) * drag.PerPixel, least - value, most - value);
        Invalidate();
    }

    /// <summary>The mouse button went up: what was dragged is changed, as one step.</summary>
    internal void Release() {
        if (this.drag is not { } drag) return;
        this.drag = null;
        // By what the dragged cell moved, to the nearest value it can hold; the others selected with it move as far.
        var amount = TableEdit.Nearest(Image, Def, drag.Cell.Col, Value(drag.Cell) + drag.Amount) - Value(drag.Cell);
        if (amount != 0f) Grid.Add(amount);
        Invalidate();
    }

    /// <summary>A key was pressed in the picture. False when the key means nothing here.</summary>
    internal bool Key(Keys key) {
        var (line, point) = Place(Current);
        switch (key) {
            case Keys.Escape when drag is not null:
                drag = null;
                Invalidate();
                return true;
            case Keys.Left:
                return Go(line, point - 1);
            case Keys.Right:
                return Go(line, point + 1);
            case Keys.Control | Keys.Left:
                return Go(line - 1, point);
            case Keys.Control | Keys.Right:
                return Go(line + 1, point);
            case Keys.Up or Keys.Down:
                if (Grid.CurrentCell is null) Grid.CurrentCell = Grid[0, 0];
                Grid.Nudge(key == Keys.Up ? 1 : -1);
                return true;
            default:
                return false;
        }
    }

    protected override string Describe() {
        var line = ShownLine;
        var (low, high) = (0, 0);
        for (var i = 1; i < Points; i++) {
            if (Value(Cell(line, i)) < Value(Cell(line, low))) low = i;
            if (Value(Cell(line, i)) > Value(Cell(line, high))) high = i;
        }
        var along = alongRows ? Def.RowAxis.Label : Def.ColAxis.Label;
        var current = Current;
        return $"The line of {LineName(line)}. Smallest {ValueText(Value(Cell(line, low)))} at {along} {PointName(low)}; largest {ValueText(Value(Cell(line, high)))} at {along} {PointName(high)}. " +
            $"{Grid.CellName(current.Row, current.Col)} is {ValueText(Value(current))}.";
    }

    protected override void Draw(Graphics g, Rectangle area) {
        var plot = PlotIn(area);
        var shown = ShownLine;
        var (min, max, _, _) = Extent();

        // What the line is, and the unit of its values.
        Write(g, LineName(shown), area.Left, area.Top, DarkTheme.Text);
        if (Def.Unit.Length > 0) Write(g, Def.Unit, area.Right, area.Top, DarkTheme.TextDim, align: 1);

        using (var rule = new Pen(DarkTheme.Border)) {
            var decimals = TableEdit.DecimalsFor(plot.Step);
            for (var value = plot.Low; value <= plot.High + plot.Step / 2f; value += plot.Step) {
                var y = plot.Y(value);
                g.DrawLine(rule, plot.Area.Left, y, plot.Area.Right, y);
                Write(g, TableEdit.Text(value, decimals), plot.Area.Left - Em(0.4f), y - Font.Height / 2f, DarkTheme.TextDim, align: 1);
            }
        }

        // The steps along the bottom: as many as have room, the first always.
        var widest = Enumerable.Range(0, Points).Max(i => Measure(PointName(i)).Width);
        var every = Points > 1 ? Math.Max(1, (int)MathF.Ceiling((widest + Em(0.6f)) / (plot.X(1) - plot.X(0)))) : 1;
        for (var i = 0; i < Points; i += every) Write(g, PointName(i), plot.X(i), plot.Area.Bottom + Em(0.3f), DarkTheme.TextDim, align: 0);
        Write(g, alongRows ? Def.RowAxis.Label : Def.ColAxis.Label, plot.Area.Left + plot.Area.Width / 2f, plot.Area.Bottom + Em(1.4f), DarkTheme.TextDim, align: 0);

        PointF[] LinePoints(int line) => [.. Enumerable.Range(0, Points).Select(i => new PointF(plot.X(i), plot.Y(Shown(Cell(line, i)))))];
        var state = g.Save();
        // A point dragged past the top or the bottom leaves the picture; its value stays written beside it.
        g.SetClip(Rectangle.FromLTRB(area.Left, plot.Area.Top - Em(0.5f), area.Right, plot.Area.Bottom + Em(0.25f)));
        if (Points > 1) {
            using var others = new Pen(DarkTheme.ControlBorder);
            for (var line = 0; line < Lines; line++) {
                if (line != shown) g.DrawLines(others, LinePoints(line));
            }
            using var pen = new Pen(DarkTheme.Text, Math.Max(2f, Font.Height / 9f));
            g.DrawLines(pen, LinePoints(shown));
        }

        var radius = Math.Max(3f, Font.Height / 4.5f);
        var current = drag?.Cell ?? Current;
        using var around = new Pen(DarkTheme.Background, 2f);
        using var ring = new Pen(DarkTheme.Text, Math.Max(2f, Font.Height / 9f));
        foreach (var at in Enumerable.Range(0, Points).Select(i => Cell(shown, i))) {
            var value = Shown(at);
            var (centre, size) = (new PointF(plot.X(Place(at).Point), plot.Y(value)), at == current ? radius * 1.5f : radius);
            var box = new RectangleF(centre.X - size, centre.Y - size, 2 * size, 2 * size);
            using var fill = new SolidBrush(HeatOf(value, min, max));
            g.FillEllipse(fill, box);
            g.DrawEllipse(around, box);
            // A ring around the points of the selected cells; the cell the keyboard is on is larger as well.
            if (Grid[at.Col, at.Row].Selected || at == current) g.DrawEllipse(ring, RectangleF.Inflate(box, 2.5f, 2.5f));
        }
        g.Restore(state);

        // The value of the cell the keyboard is on, or of the one being dragged, beside its point.
        var label = ValueText(Shown(current));
        var point = new PointF(plot.X(Place(current).Point), Math.Clamp(plot.Y(Shown(current)), plot.Area.Top, plot.Area.Bottom));
        var above = point.Y - radius * 1.5f - Em(0.35f) - Font.Height;
        var top = above >= area.Top + Font.Height ? above : point.Y + radius * 1.5f + Em(0.35f);
        var textSize = Measure(label);
        var left = Math.Clamp(point.X - textSize.Width / 2f, plot.Area.Left, Math.Max(plot.Area.Left, area.Right - textSize.Width));
        using (var back = new SolidBrush(DarkTheme.Background)) g.FillRectangle(back, left - 2, top, textSize.Width + 4, textSize.Height);
        Write(g, label, left, top, DarkTheme.Text);
    }

    protected override void OnMouseDown(MouseEventArgs e) {
        base.OnMouseDown(e);
        Select();
        if (e.Button == MouseButtons.Left) Press(e.Location);
    }

    protected override void OnMouseMove(MouseEventArgs e) {
        base.OnMouseMove(e);
        if (drag is null) {
            Cursor = !Grid.ReadOnly && CellAt(e.Location) is not null ? Cursors.SizeNS : Cursors.Default;
        } else if (e.Button.HasFlag(MouseButtons.Left)) {
            DragTo(e.Location);
        } else {
            // The button was let go where this window did not see it.
            drag = null;
            Invalidate();
        }
    }

    protected override void OnMouseUp(MouseEventArgs e) {
        base.OnMouseUp(e);
        if (e.Button == MouseButtons.Left) Release();
    }

    protected override void OnKeyDown(KeyEventArgs e) {
        base.OnKeyDown(e);
        if (!e.Handled) e.Handled = Key(e.KeyData);
    }

    private CellRef Cell(int line, int point) => alongRows ? new CellRef(point, line) : new CellRef(line, point);

    private (int Line, int Point) Place(CellRef cell) => alongRows ? (cell.Col, cell.Row) : (cell.Row, cell.Col);

    private string PointName(int point) => alongRows ? Grid.RowHeader(point) : Grid.ColumnHeader(point);

    private string LineName(int line) => alongRows ? $"{Def.ColAxis.Label} {Grid.ColumnHeader(line)}" : $"{Def.RowAxis.Label} {Grid.RowHeader(line)}";

    /// <summary>What a cell shows in the picture: its value, or while it is dragged the value it would be given.</summary>
    private float Shown(CellRef cell) {
        var value = Value(cell);
        if (this.drag is not { } drag || drag.Amount == 0f) return value;
        var dragged = cell == drag.Cell || (Grid[drag.Cell.Col, drag.Cell.Row].Selected && Grid[cell.Col, cell.Row].Selected);
        return dragged ? TableEdit.Nearest(Image, Def, cell.Col, value + drag.Amount) : value;
    }

    private bool Go(int line, int point) {
        var cell = Cell(Math.Clamp(line, 0, Lines - 1), Math.Clamp(point, 0, Points - 1));
        if (cell != Current || Grid.CurrentCell is null) GoTo(cell);
        return true;
    }

    /// <summary>The cell whose point is under the pointer, the points of the line that stands out before the others; null when none is.</summary>
    private CellRef? CellAt(Point at) {
        var plot = PlotIn(Area);
        var shown = ShownLine;
        var reach = Em(0.6f);
        (CellRef? cell, var nearest) = (null, float.MaxValue);
        for (var line = 0; line < Lines; line++) {
            for (var point = 0; point < Points; point++) {
                var (dx, dy) = (plot.X(point) - at.X, plot.Y(Shown(Cell(line, point))) - at.Y);
                var distance = MathF.Sqrt(dx * dx + dy * dy);
                if (distance > reach) continue;
                // Lines cross and lie on each other: the one that stands out is the one meant.
                if (line != shown) distance += reach;
                if (distance < nearest) (cell, nearest) = (Cell(line, point), distance);
            }
        }
        return cell;
    }

    /// <summary>Where the lines are drawn and what scale they have.</summary>
    private Plot PlotIn(Rectangle area) {
        var (min, max, _, _) = Extent();
        var inner = area.Height - Em(1.6f) - Em(2.7f);
        var (low, high, step) = ChartMath.Scale(min, max, Math.Max(2, inner / Math.Max(Em(1.6f), 1)));
        var decimals = TableEdit.DecimalsFor(step);
        var labels = Math.Max(Measure(TableEdit.Text(low, decimals)).Width, Measure(TableEdit.Text(high, decimals)).Width);
        var left = area.Left + labels + Em(0.8f);
        var plot = Rectangle.FromLTRB(left, area.Top + Em(1.6f), Math.Max(left + 1, area.Right - Em(1f)), Math.Max(area.Top + Em(1.6f) + 1, area.Bottom - Em(2.7f)));
        return new Plot(plot, low, high, step, Points);
    }

    private readonly record struct Plot(Rectangle Area, float Low, float High, float Step, int Points) {
        /// <summary>How much of a value one pixel of height stands for.</summary>
        public float PerPixel => (High - Low) / Area.Height;

        public float X(int point) => Points > 1 ? Area.Left + (float)Area.Width * point / (Points - 1) : Area.Left + Area.Width / 2f;

        public float Y(float value) => Area.Bottom - (value - Low) / (High - Low) * Area.Height;
    }

    /// <summary>A drag under way: the cell whose point was taken, where, the scale at that moment, and how far it has been moved.</summary>
    private sealed class Drag(CellRef cell, int startY, float perPixel) {
        public CellRef Cell => cell;
        public int StartY => startY;
        public float PerPixel => perPixel;
        public float Amount { get; set; }
    }
}
