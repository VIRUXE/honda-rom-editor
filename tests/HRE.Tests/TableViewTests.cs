using System.Drawing;
using System.Windows.Forms;
using HRE.App;
using HRE.Core;

namespace HRE.Tests;

/// <summary>
/// The two pictures of a table, the graph and the surface: what they draw, what dragging a point
/// and the keys do to the ROM and to the grid they belong to, what they say to a screen reader,
/// and where the page puts them.
/// </summary>
public class TableViewTests {
    private static void OnUiThread(Action body) => AppShellTests.OnUiThread(body);

    /// <summary>The fuel map of the mini ROM: 1, 4, 12 over 4, 10, 24, in steps of 1, 2 and 4.</summary>
    private static (RomDocument Document, ParamDef Fuel) Mini() {
        var session = AppShellTests.MiniSession();
        return (session.Document, session.Defs[0]);
    }

    private static byte[] Stored(RomDocument document) => document.Image.Bytes.Slice(0x100, 6).ToArray();

    private static bool Drawn(Control control) {
        using var picture = new Bitmap(control.Width, control.Height);
        control.DrawToBitmap(picture, new Rectangle(Point.Empty, control.Size));
        var background = DarkTheme.Background.ToArgb();
        // Inside the frame, something other than the background.
        return Enumerable.Range(4, control.Width - 8).Any(x => Enumerable.Range(4, control.Height - 8).Any(y => picture.GetPixel(x, y).ToArgb() != background));
    }

    [Fact]
    public void AddingToCellsLandsOnValuesTheyCanHold() {
        var (document, fuel) = Mini();
        // The third column moves in steps of 4.
        Assert.Equal(24f, TableEdit.Nearest(document.Image, fuel, 2, 22f));
        Assert.Equal(1020f, TableEdit.Nearest(document.Image, fuel, 2, 5000f));
        Assert.Equal(0f, TableEdit.Nearest(document.Image, fuel, 0, -3f));
        // 1 + 3 is 4; 12 + 3 is nearest to 16.
        document.Apply("add", TableEdit.Add(document.Image, fuel, [new CellRef(0, 0), new CellRef(0, 2)], 3f));
        Assert.Equal([4, 2, 4, 4, 5, 6], Stored(document));
        document.Apply("add", TableEdit.Add(document.Image, fuel, [new CellRef(0, 0), new CellRef(1, 0)], -100f));
        Assert.Equal([0, 2, 4, 0, 5, 6], Stored(document));
        Assert.Empty(TableEdit.Add(document.Image, fuel, [], 5f));
    }

    [Fact]
    public void TheScaleOfAGraphHasRoundSteps() {
        Assert.Equal((-10f, 50f, 10f), ChartMath.Scale(-5f, 42.75f, 7));
        Assert.Equal((0f, 600f, 200f), ChartMath.Scale(4f, 600f, 4));
        Assert.Equal((0f, 30f, 5f), ChartMath.Scale(1f, 28f, 6));
        // A table of one value still has a scale around it.
        Assert.Equal((2f, 4f, 0.5f), ChartMath.Scale(3f, 3f, 5));
        var (low, high, step) = ChartMath.Scale(0.013f, 0.021f, 4);
        Assert.True(low <= 0.013f && high >= 0.021f && step > 0f && (high - low) / step <= 5.01f);
    }

    [Fact]
    public void WhatThePicturesDrawShowsOnTheBackground() {
        if (DarkTheme.HighContrast) return; // the colours are then the user's contrast theme, not ours
        // The points of the graph and the faces of the surface, in every colour a cell can take.
        for (var t = 0.0; t <= 1.0; t += 0.01) {
            Assert.True(DarkTheme.Contrast(DarkTheme.Heat(t), DarkTheme.Background) >= 3, $"at {t:0.00}");
        }
        // The line that stands out and the marks of the cells; the lines behind it and the frame; what is written.
        Assert.True(DarkTheme.Contrast(DarkTheme.Text, DarkTheme.Background) >= 7);
        Assert.True(DarkTheme.Contrast(DarkTheme.ControlBorder, DarkTheme.Background) >= 3);
        Assert.True(DarkTheme.Contrast(DarkTheme.TextDim, DarkTheme.Background) >= 4.5);
    }

    [Fact]
    public void TheSurfaceIsSeenFromAboveAndTurnsAroundItsUprightAxis() {
        static void Near((float X, float Y, float Near) expected, (float X, float Y, float Near) actual) {
            Assert.Equal(expected.X, actual.X, 3);
            Assert.Equal(expected.Y, actual.Y, 3);
            Assert.Equal(expected.Near, actual.Near, 3);
        }
        // Not turned: what is towards the viewer is lower on the screen, by the sine of the tilt.
        Near((0f, 0.25f, 0.5f), ChartMath.Project(0f, 0.5f, 0f, 0f, 30f));
        Near((0.5f, 0f, 0f), ChartMath.Project(0.5f, 0f, 0f, 0f, 30f));
        // Up is up, by the cosine of the tilt, and is no nearer.
        Near((0f, -0.866f, 0f), ChartMath.Project(0f, 0f, 1f, 0f, 30f));
        // A quarter turn brings the right side to the front.
        Near((0f, 0.25f, 0.5f), ChartMath.Project(0.5f, 0f, 0f, 90f, 30f));
        // From straight above there is no height to see.
        Near((0f, 0.5f, 0.5f), ChartMath.Project(0f, 0.5f, 1f, 0f, 90f));
    }

    [Fact]
    public void TheGraphShowsTheLineOfTheCellAndMovesAlongIt() => OnUiThread(() => {
        var (document, fuel) = Mini();
        using var grid = new TableGrid(document, fuel);
        using var graph = new TableGraph(grid) { Size = new Size(400, 300) };
        var heard = new List<string>();
        graph.Announced += heard.Add;
        Assert.Equal(("Graph of Fuel Map", AccessibleRole.Chart), (graph.AccessibleName, graph.AccessibleRole));
        Assert.True(graph.TabStop);
        // A screen reader is told that it takes the keyboard, and hears its description as help text too.
        Assert.True(graph.AccessibilityObject.State.HasFlag(AccessibleStates.Focusable));

        grid.CurrentCell = grid[1, 0];
        Assert.Equal("The line of MAP 60. Smallest 4 at RPM 10; largest 10 at RPM 30. RPM 10, MAP 60 is 4.", graph.AccessibleDescription);
        Assert.Equal(graph.AccessibleDescription, graph.AccessibilityObject.Help);
        // The larger value is higher up; the points of a line are spread over its width.
        Assert.True(graph.PointOf(1, 1).Y < graph.PointOf(0, 1).Y);
        Assert.True(graph.PointOf(1, 1).X > graph.PointOf(0, 1).X + 100);
        Assert.True(Drawn(graph));

        // Left and right along the line, with Ctrl to the next line; the grid goes with it.
        Assert.True(graph.Key(Keys.Right));
        Assert.Equal((new Point(1, 1), "RPM 30, MAP 60: 10"), (grid.CurrentCellAddress, heard[^1]));
        Assert.True(graph.Key(Keys.Right));
        Assert.Equal((new Point(1, 1), 1), (grid.CurrentCellAddress, heard.Count));
        Assert.True(graph.Key(Keys.Control | Keys.Right));
        Assert.Equal((new Point(2, 1), "RPM 30, MAP 90: 24"), (grid.CurrentCellAddress, heard[^1]));
        Assert.StartsWith("The line of MAP 90.", graph.AccessibleDescription);
        Assert.True(graph.Key(Keys.Left));
        Assert.Equal(new Point(2, 0), grid.CurrentCellAddress);

        // Up and down raise and lower the cell a step, as + and - do in the cells.
        var said = new List<string>();
        grid.Announced += said.Add;
        Assert.True(graph.Key(Keys.Up));
        Assert.Equal((4, "16", "Raise Fuel Map"), (document.Image[0x102], said[^1], document.UndoLabel));
        Assert.True(graph.Key(Keys.Down));
        Assert.Equal(3, document.Image[0x102]);
        Assert.EndsWith("RPM 10, MAP 90 is 12.", graph.AccessibleDescription);
        Assert.False(graph.Key(Keys.A));
        Assert.False(graph.Key(Keys.Escape));
    });

    [Fact]
    public void DraggingAPointChangesItsCellAsOneStep() => OnUiThread(() => {
        var (document, fuel) = Mini();
        using var grid = new TableGrid(document, fuel);
        using var graph = new TableGraph(grid) { Size = new Size(400, 300) };
        var said = new List<string>();
        grid.Announced += said.Add;
        grid.CurrentCell = grid[2, 0];
        // How many pixels a unit is, from two points 12 apart.
        var perUnit = (graph.PointOf(0, 2).Y - graph.PointOf(1, 2).Y) / 12f;
        Point Raised(PointF from, float by) => new((int)from.X, (int)MathF.Round(from.Y - by * perUnit));

        // Taking a point makes its cell the cell of the grid.
        var point = graph.PointOf(1, 2);
        graph.Press(Point.Round(point));
        Assert.Equal(new Point(2, 1), grid.CurrentCellAddress);
        // While it is dragged the point shows where it would be stored, and nothing is changed yet.
        var before = document.Image;
        graph.DragTo(Raised(point, 9f));
        Assert.Equal(point.Y - 8f * perUnit, graph.PointOf(1, 2).Y, 1f);
        Assert.Same(before, document.Image);
        graph.Release();
        Assert.Equal((8, "Change Fuel Map", "Set to 32"), (document.Image[0x105], document.UndoLabel, said[^1]));
        document.Undo();
        Assert.Equal(6, document.Image[0x105]);

        // Esc lets go of the point without a change; so does a drag of less than half a step.
        graph.Press(Point.Round(graph.PointOf(1, 2)));
        graph.DragTo(Raised(point, 12f));
        Assert.True(graph.Key(Keys.Escape));
        graph.Release();
        graph.Press(Point.Round(graph.PointOf(1, 2)));
        graph.DragTo(Raised(point, 1f));
        graph.Release();
        Assert.Equal((6, false), (document.Image[0x105], document.CanUndo));

        // A point cannot be dragged past what its cell can hold.
        graph.Press(Point.Round(graph.PointOf(1, 2)));
        graph.DragTo(new Point((int)point.X, 100000));
        graph.Release();
        Assert.Equal((0, "Set to 0"), ((int)document.Image[0x105], said[^1]));
        document.Undo();

        // The cells selected with the one that is dragged go with it, each to a value it can hold.
        grid.CurrentCell = grid[0, 0];
        grid[0, 1].Selected = true;
        var first = graph.PointOf(0, 0);
        graph.Press(Point.Round(first));
        Assert.Equal(2, grid.SelectedCells.Count);
        graph.DragTo(Raised(first, 3f));
        graph.Release();
        Assert.Equal([4, 2, 3, 7, 5, 6], Stored(document));
        Assert.Equal("Raised 2 cells by 3", said[^1]);

        // Away from every point nothing is taken; a table that may not be changed is not dragged.
        graph.Press(new Point(5, 5));
        graph.DragTo(new Point(5, 200));
        graph.Release();
        grid.ReadOnly = true;
        graph.Press(Point.Round(graph.PointOf(1, 1)));
        graph.DragTo(Raised(graph.PointOf(1, 1), 6f));
        graph.Release();
        Assert.Equal([4, 2, 3, 7, 5, 6], Stored(document));
        Assert.Equal(new Point(1, 1), grid.CurrentCellAddress);
    });

    [Fact]
    public void TheSurfaceTurnsAndFollowsTheGrid() => OnUiThread(() => {
        var (document, fuel) = Mini();
        using var grid = new TableGrid(document, fuel);
        using var surface = new TableSurface(grid) { Size = new Size(400, 300) };
        var heard = new List<string>();
        surface.Announced += heard.Add;
        Assert.Equal(("Surface of Fuel Map", AccessibleRole.Chart), (surface.AccessibleName, surface.AccessibleRole));
        Assert.Equal("The whole table as a surface, each cell as high as its value. Smallest 1 at RPM 10, MAP 30; largest 24 at RPM 30, MAP 90.", surface.AccessibleDescription);
        Assert.True(Drawn(surface));

        // The first row is at the back, the first column on the left.
        Assert.True(surface.PointOf(1, 0).Y > surface.PointOf(0, 0).Y);
        Assert.True(surface.PointOf(0, 2).X > surface.PointOf(0, 0).X);

        // The arrow keys turn and tilt it; Home brings back the view it starts with.
        var (turn, tilt) = (surface.Turn, surface.Tilt);
        var corner = surface.PointOf(1, 2);
        Assert.True(surface.Key(Keys.Right));
        Assert.Equal((turn + 345f) % 360f, surface.Turn);
        Assert.True(surface.PointOf(1, 2).X > corner.X, "the side that faces the viewer goes to the right");
        Assert.True(surface.Key(Keys.Left) && surface.Key(Keys.Left));
        Assert.Equal(turn + 15f, surface.Turn);
        Assert.True(surface.Key(Keys.Up));
        Assert.Equal(tilt + 5f, surface.Tilt);
        Assert.True(surface.Key(Keys.Home));
        Assert.Equal((turn, tilt), (surface.Turn, surface.Tilt));
        Assert.Equal(corner, surface.PointOf(1, 2));
        // It is never seen from under, nor from straight above; a full turn is no turn.
        surface.TurnBy(360f, 500f);
        Assert.Equal((turn, 85f), (surface.Turn, surface.Tilt));
        surface.TurnBy(-720f - turn, -500f);
        Assert.Equal((0f, 5f), (surface.Turn, surface.Tilt));
        Assert.False(surface.Key(Keys.A));
        Assert.True(surface.Key(Keys.Home));

        // A click on a corner moves the grid to its cell; a click on nothing moves nothing.
        surface.ClickAt(Point.Round(surface.PointOf(1, 2)));
        Assert.Equal((new Point(2, 1), "RPM 30, MAP 90: 24"), (grid.CurrentCellAddress, heard[^1]));
        surface.ClickAt(new Point(3, 3));
        Assert.Equal((new Point(2, 1), 1), (grid.CurrentCellAddress, heard.Count));

        // A change to the table shows: the corner of a raised cell stands higher.
        var was = surface.PointOf(0, 0);
        document.Apply("set", TableEdit.Set(document.Image, fuel, [new CellRef(0, 0)], 20f));
        Assert.True(surface.PointOf(0, 0).Y < was.Y);
        Assert.StartsWith("The whole table as a surface, each cell as high as its value. Smallest 4 at", surface.AccessibleDescription);
    });

    [Fact]
    public void TheTablePageShowsThePicturesBesideTheCellsOrUnderThem() => OnUiThread(() => {
        var session = AppShellTests.MiniSession();
        using var view = new PageView { Size = new Size(900, 600) };
        view.Page = Details.Param(session.Document, session.Defs[0]);
        var (grid, surface, graph) = (view.Grid!, Assert.IsType<TableSurface>(view.Surface), Assert.IsType<TableGraph>(view.Graph));
        // Wide enough: beside the cells, the surface over the graph, and what they are for under all of it.
        Assert.True(surface.Left > grid.Right && graph.Left == surface.Left);
        Assert.Equal(grid.Top, surface.Top);
        Assert.True(graph.Top > surface.Bottom);
        Assert.True(surface.Height >= surface.Font.Height * 12 && surface.Width >= surface.Font.Height * 17);
        var help = view.Controls.OfType<Label>().Single(l => l.Text == Details.ChartHelp(session.Defs[0]));
        Assert.True(help.Top >= graph.Bottom);
        Assert.Contains("Drag a point up or down to change its cell", help.Text);
        Assert.Contains("Home turns it back", help.Text);
        // The keyboard reaches them after the cells.
        Assert.True(grid.TabIndex < surface.TabIndex && surface.TabIndex < graph.TabIndex);

        // Too narrow for that: under the cells, one over the other.
        view.Size = new Size(330, 600);
        Assert.True(surface.Top > grid.Bottom && graph.Top > surface.Bottom);
        Assert.Equal((grid.Left, grid.Left), (surface.Left, graph.Left));
        Assert.True(surface.Right <= view.ClientSize.Width);
        view.Size = new Size(900, 600);
        Assert.True(surface.Left > grid.Right);

        // They stay while the table changes, the surface as it was turned.
        surface.TurnBy(40f, 0f);
        session.Document.Changed += () => view.Revise(Details.Param(session.Document, session.Defs[0]));
        grid.CurrentCell = grid[0, 0];
        grid.Nudge(1);
        Assert.Same(surface, view.Surface);
        Assert.Same(graph, view.Graph);
        Assert.False(surface.IsDisposed || graph.IsDisposed);
        Assert.Equal(70f, surface.Turn);
        view.ShowTechnical = true;
        Assert.Same(graph, view.Graph);

        // Another table has pictures of its own; a setting has none.
        view.Page = Details.Param(session.Document, session.Defs[1]);
        Assert.True(surface.IsDisposed && graph.IsDisposed);
        Assert.Equal(("Surface of Wide Map", "Graph of Wide Map"), (view.Surface?.AccessibleName, view.Graph?.AccessibleName));
        view.Page = Details.Param(session.Document, session.Defs[2]);
        Assert.Null(view.Surface);
        Assert.Null(view.Graph);
    });

    [Fact]
    public void ATableOfOneRowIsOneLineAndHasNoSurface() => OnUiThread(() => {
        var session = AppShellTests.MiniSession();
        var row = session.Defs[0] with { Name = "Row", Kind = new ParamKind.Table(1, 3) };
        using var view = new PageView { Size = new Size(900, 600) };
        view.Page = Details.Param(session.Document, row);
        Assert.Null(view.Surface);
        var graph = Assert.IsType<TableGraph>(view.Graph);
        Assert.Equal("The line of RPM 10. Smallest 1 at MAP 30; largest 12 at MAP 90. RPM 10, MAP 30 is 1.", graph.AccessibleDescription);
        Assert.True(graph.Key(Keys.Right));
        Assert.Equal(new Point(1, 0), view.Grid!.CurrentCellAddress);
        var help = Details.ChartHelp(row);
        Assert.Contains("the row of the cell you are on as a line, with a point for each column", help);
        Assert.DoesNotContain("surface", help);
        Assert.DoesNotContain("Ctrl", help);
        Assert.True(Drawn(graph));
    });
}
