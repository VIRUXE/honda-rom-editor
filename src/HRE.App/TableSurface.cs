using HRE.Core;

namespace HRE.App;

/// <summary>
/// A table as a surface seen from above and to one side: each cell is a corner of the surface,
/// as high as its value and in the colour it has in the cells. It can be turned and tilted, with
/// the mouse or the arrow keys, to look at it from any side. The cell the keyboard is on in the
/// grid is marked on it, and a click on a corner moves the grid there.
/// </summary>
public sealed class TableSurface : TableChart {
    private const float StartTurn = 30f;
    private const float StartTilt = 30f;
    private const float LowestTilt = 5f;
    private const float HighestTilt = 85f;

    private (Point At, float Turn, float Tilt, bool Moved)? _press;

    public TableSurface(TableGrid grid) : base(grid, "Surface") => Changed();

    /// <summary>How far the surface is turned around its upright axis, in degrees from 0 to 360.</summary>
    internal float Turn { get; private set; } = StartTurn;

    /// <summary>From how far above the ground the surface is seen, in degrees.</summary>
    internal float Tilt { get; private set; } = StartTilt;

    /// <summary>Turns and tilts the surface by so many degrees. It is never seen from under or from straight above.</summary>
    internal void TurnBy(float turn, float tilt) => TurnTo(Turn + turn, Tilt + tilt);

    /// <summary>Where the corner of a cell is in the picture.</summary>
    internal PointF PointOf(int row, int col) => new View(this, Area).Corner(row, col);

    /// <summary>A click: the grid moves to the cell whose corner is nearest, when one is near.</summary>
    internal void ClickAt(Point at) {
        var view = new View(this, Area);
        (CellRef? cell, var nearest) = (null, (float)Em(0.7f));
        for (var row = 0; row < Rows; row++) {
            for (var col = 0; col < Cols; col++) {
                var corner = view.Corner(row, col);
                var distance = MathF.Sqrt((corner.X - at.X) * (corner.X - at.X) + (corner.Y - at.Y) * (corner.Y - at.Y));
                if (distance < nearest) (cell, nearest) = (new CellRef(row, col), distance);
            }
        }
        if (cell is { } found) GoTo(found);
    }

    /// <summary>A key was pressed in the picture. False when the key means nothing here.</summary>
    internal bool Key(Keys key) {
        switch (key) {
            // As with the mouse: the side that faces the viewer goes the way the arrow points.
            case Keys.Left:
                TurnBy(15f, 0f);
                return true;
            case Keys.Right:
                TurnBy(-15f, 0f);
                return true;
            case Keys.Up:
                TurnBy(0f, 5f);
                return true;
            case Keys.Down:
                TurnBy(0f, -5f);
                return true;
            case Keys.Home:
                TurnTo(StartTurn, StartTilt);
                return true;
            default:
                return false;
        }
    }

    protected override string Describe() => $"The whole table as a surface, each cell as high as its value. {ExtentWords()}";

    protected override void Draw(Graphics g, Rectangle area) {
        var view = new View(this, area);
        var (min, max, _, _) = Extent();
        var line = DarkTheme.HighContrast ? DarkTheme.Text : DarkTheme.Background;

        // The four corners of the base, and which of them is nearest: what stands there is in front of the surface.
        (int Row, int Col)[] corners = [(0, 0), (0, Cols - 1), (Rows - 1, Cols - 1), (Rows - 1, 0)];
        var near = Enumerable.Range(0, 4).MaxBy(i => view.Near(corners[i].Row, corners[i].Col));
        using var frame = new Pen(DarkTheme.ControlBorder);
        g.DrawPolygon(frame, [.. corners.Select(c => view.Floor(c.Row, c.Col))]);
        for (var i = 0; i < 4; i++) {
            if (i != near) g.DrawLine(frame, view.Floor(corners[i].Row, corners[i].Col), view.Top(corners[i].Row, corners[i].Col));
        }

        // Far before near, so that what is nearer covers what is farther.
        var faces = new List<(float Near, int Row, int Col)>();
        for (var row = 0; row + 1 < Rows; row++) {
            for (var col = 0; col + 1 < Cols; col++) {
                faces.Add(((view.Near(row, col) + view.Near(row + 1, col + 1)) / 2f, row, col));
            }
        }
        using var edge = new Pen(line);
        foreach (var (_, row, col) in faces.OrderBy(f => f.Near)) {
            CellRef[] cells = [new(row, col), new(row, col + 1), new(row + 1, col + 1), new(row + 1, col)];
            using var fill = new SolidBrush(DarkTheme.HighContrast ? DarkTheme.Background : HeatOf(cells.Average(Value), min, max));
            PointF[] shape = [.. cells.Select(c => view.Corner(c.Row, c.Col))];
            g.FillPolygon(fill, shape);
            g.DrawPolygon(edge, shape);
        }

        // In front of the surface: the nearest corner of the base and the two sides that meet there.
        var (nearest, before, after) = (corners[near], corners[(near + 3) % 4], corners[(near + 1) % 4]);
        g.DrawLine(frame, view.Floor(nearest.Row, nearest.Col), view.Corner(nearest.Row, nearest.Col));
        g.DrawLine(frame, view.Floor(nearest.Row, nearest.Col), view.Floor(before.Row, before.Col));
        g.DrawLine(frame, view.Floor(nearest.Row, nearest.Col), view.Floor(after.Row, after.Col));

        // The selected cells and the cell the keyboard is on: light inside dark, which shows on any colour.
        var radius = Math.Max(2.5f, Font.Height / 6f);
        using var dark = new SolidBrush(DarkTheme.Background);
        using var light = new SolidBrush(DarkTheme.Text);
        void Mark(CellRef cell, float size) {
            var at = view.Corner(cell.Row, cell.Col);
            g.FillEllipse(dark, at.X - size - 2f, at.Y - size - 2f, 2 * size + 4f, 2 * size + 4f);
            g.FillEllipse(light, at.X - size, at.Y - size, 2 * size, 2 * size);
        }
        var selected = Grid.SelectedRefs;
        if (selected.Count > 1) {
            foreach (var cell in selected) Mark(cell, radius);
        }
        Mark(Current, radius * 1.7f);

        // The steps of the two sides that meet at the nearest corner, written outside the base.
        var nearFloor = view.Floor(nearest.Row, nearest.Col);
        foreach (var far in new[] { before, after }) {
            var farFloor = view.Floor(far.Row, far.Col);
            var alongRows = far.Col == nearest.Col;
            var axis = alongRows ? Def.RowAxis : Def.ColAxis;
            string Step(int row, int col) => alongRows ? Grid.RowHeader(row) : Grid.ColumnHeader(col);
            // To the left of a side that runs to the left, to the right of one that runs to the right.
            var other = far == before ? after : before;
            var side = farFloor.X < nearFloor.X - 1f || (MathF.Abs(farFloor.X - nearFloor.X) <= 1f && view.Floor(other.Row, other.Col).X > nearFloor.X) ? 1 : -1;
            var gap = side > 0 ? -Em(0.3f) : Em(0.3f);
            Write(g, Step(nearest.Row, nearest.Col), nearFloor.X + gap, nearFloor.Y + Em(0.3f), DarkTheme.TextDim, side);
            Write(g, Step(far.Row, far.Col), farFloor.X + gap, farFloor.Y + Em(0.8f), DarkTheme.TextDim, side);
            Write(g, axis.Label, (nearFloor.X + farFloor.X) / 2f + gap, (nearFloor.Y + farFloor.Y) / 2f + Em(0.7f), DarkTheme.Text, side);
        }

        // How high: the smallest and the largest value, at the foot and the head of the corner farthest to the left.
        var left = corners.MinBy(c => view.Floor(c.Row, c.Col).X);
        var (foot, head) = (view.Floor(left.Row, left.Col), view.Top(left.Row, left.Col));
        Write(g, ValueText(min), foot.X - Em(0.4f), foot.Y - Font.Height * 0.6f, DarkTheme.TextDim, align: 1);
        Write(g, Def.Unit.Length > 0 ? $"{ValueText(max)} {Def.Unit}" : ValueText(max), head.X - Em(0.4f), head.Y - Font.Height * 0.6f, DarkTheme.TextDim, align: 1);
    }

    protected override void OnMouseDown(MouseEventArgs e) {
        base.OnMouseDown(e);
        Select();
        if (e.Button == MouseButtons.Left) _press = (e.Location, Turn, Tilt, false);
    }

    protected override void OnMouseMove(MouseEventArgs e) {
        base.OnMouseMove(e);
        if (_press is not { } press || !e.Button.HasFlag(MouseButtons.Left)) {
            _press = null;
            return;
        }
        var (dx, dy) = (e.X - press.At.X, e.Y - press.At.Y);
        // A hand that clicks moves a little; that is not yet a turn.
        if (!press.Moved && Math.Abs(dx) + Math.Abs(dy) < SystemInformation.DragSize.Width) return;
        _press = press with { Moved = true };
        // The width of the picture is a turn of 180 degrees, wherever it is taken hold of.
        TurnTo(press.Turn - dx * 180f / Math.Max(Width, 1), press.Tilt + dy * 180f / Math.Max(Width, 1));
    }

    protected override void OnMouseUp(MouseEventArgs e) {
        base.OnMouseUp(e);
        if (_press is { Moved: false } press && e.Button == MouseButtons.Left) ClickAt(press.At);
        _press = null;
    }

    protected override void OnKeyDown(KeyEventArgs e) {
        base.OnKeyDown(e);
        if (!e.Handled) e.Handled = Key(e.KeyData);
    }

    private void TurnTo(float turn, float tilt) {
        Turn = (turn % 360f + 360f) % 360f;
        Tilt = Math.Clamp(tilt, LowestTilt, HighestTilt);
        Invalidate();
    }

    /// <summary>The surface as it is seen now, placed in an area: where each corner falls.</summary>
    private readonly struct View {
        private readonly TableSurface _owner;
        private readonly float _min;
        private readonly float _max;
        private readonly float _scale;
        private readonly PointF _origin;

        public View(TableSurface owner, Rectangle area) {
            _owner = owner;
            (_min, _max, _, _) = owner.Extent();
            // Room for what is written around it; the base keeps its size while it is turned.
            var (side, above, below) = (owner.Em(2.5f), owner.Em(0.8f), owner.Em(2.4f));
            var (up, flat) = MathF.SinCos(owner.Tilt * MathF.PI / 180f);
            var (across, depth, height) = (MathF.Sqrt(2f), MathF.Sqrt(2f) * up, ChartMath.SurfaceHeight * flat);
            _scale = Math.Max(1f, Math.Min((area.Width - 2 * side) / across, (area.Height - above - below) / (depth + height)));
            var free = area.Height - above - below - (depth + height) * _scale;
            _origin = new PointF(area.Left + area.Width / 2f, area.Top + above + free / 2f + (height + depth / 2f) * _scale);
        }

        /// <summary>Where the corner of a cell is, at the height of its value.</summary>
        public PointF Corner(int row, int col) {
            var value = _owner.Value(new CellRef(row, col));
            return At(row, col, _max > _min ? (value - _min) / (_max - _min) * ChartMath.SurfaceHeight : ChartMath.SurfaceHeight / 2f);
        }

        /// <summary>Where a cell is on the base, under its corner.</summary>
        public PointF Floor(int row, int col) => At(row, col, 0f);

        /// <summary>Where a cell would be if it held the largest value of the table.</summary>
        public PointF Top(int row, int col) => At(row, col, ChartMath.SurfaceHeight);

        /// <summary>How near a cell is to the viewer; what is nearer covers what is farther.</summary>
        public float Near(int row, int col) => Project(row, col, 0f).Near;

        private PointF At(int row, int col, float z) {
            var (x, y, _) = Project(row, col, z);
            return new PointF(_origin.X + x * _scale, _origin.Y + y * _scale);
        }

        /// <summary>The first row is at the back and the first column on the left, as in the cells laid flat.</summary>
        private (float X, float Y, float Near) Project(int row, int col, float z) => ChartMath.Project(
            _owner.Cols > 1 ? (float)col / (_owner.Cols - 1) - 0.5f : 0f,
            _owner.Rows > 1 ? (float)row / (_owner.Rows - 1) - 0.5f : 0f,
            z, _owner.Turn, _owner.Tilt);
    }
}
