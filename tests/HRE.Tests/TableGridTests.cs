using System.Drawing;
using System.Windows.Forms;
using HRE.App;
using HRE.Core;

namespace HRE.Tests;

/// <summary>
/// The cells of a table: what they show, what typing, raising, lowering, copying and pasting do
/// to the ROM, and what the grid offers to the keyboard and to a screen reader.
/// </summary>
public class TableGridTests {
    private static void OnUiThread(Action body) => AppShellTests.OnUiThread(body);

    /// <summary>The fuel map of the mini ROM: cells 1..6 in two rows, the columns multiplied by 1, 2 and 4.</summary>
    private static (RomDocument Document, ParamDef Fuel, ParamDef Wide) Mini() {
        var session = AppShellTests.MiniSession();
        return (session.Document, session.Defs[0], session.Defs[1]);
    }

    private static CellRef[] Cells(params (int Row, int Col)[] at) => [.. at.Select(a => new CellRef(a.Row, a.Col))];

    private static byte[] Stored(RomDocument document, int offset, int count) => document.Image.Bytes.Slice(offset, count).ToArray();

    [Fact]
    public void CellsAreWrittenWithAsManyDecimalsAsTellStepsApart() {
        var (document, fuel, wide) = Mini();
        Assert.Equal(0, TableEdit.Decimals(document.Image, fuel));
        Assert.Equal(1, TableEdit.Decimals(document.Image, wide));
        Assert.Equal(2, TableEdit.Decimals(document.Image, fuel with { Scale = 0.25f }));
        Assert.Equal(3, TableEdit.Decimals(document.Image, fuel with { Scale = 0.36105f }));
        Assert.Equal(("12", "24"), (TableEdit.CellText(document.Image, fuel, 0, 2), TableEdit.CellText(document.Image, fuel, 1, 2)));
        // 0x0010 and 0x1234 at half a unit each.
        Assert.Equal(("8", "2330"), (TableEdit.CellText(document.Image, wide, 0, 0), TableEdit.CellText(document.Image, wide, 1, 1)));
        Assert.Equal(["10.25", "10.5", "10", "-0.5", "0"], new[] { 10.25f, 10.5f, 10f, -0.5f, -0.001f }.Select(v => TableEdit.Text(v, 2)));
    }

    [Fact]
    public void TypedNumbersTakeAPointOrAComma() {
        string[] numbers = ["12.5", " 12,5 ", "-3", ".5", "1000"];
        string[] others = ["", "abc", "1.2.3", "1,2.3", "-", "1e5", "NaN"];
        Assert.Equal([12.5f, 12.5f, -3f, 0.5f, 1000f], numbers.Select(t => TableEdit.Parse(t)!.Value));
        Assert.All(others, t => Assert.Null(TableEdit.Parse(t)));
    }

    [Fact]
    public void SettingACellStoresTheNearestStepItCanHold() {
        var (document, fuel, wide) = Mini();
        // Column 2 shows four times what it stores: 22 is nearest to 6 x 4, 26 to 7 x 4 (halves round up).
        document.Apply("set", TableEdit.Set(document.Image, fuel, Cells((0, 2), (1, 2)), 26f));
        Assert.Equal([1, 2, 7, 4, 5, 7], Stored(document, 0x100, 6));
        document.Apply("set", TableEdit.Set(document.Image, fuel, Cells((0, 0)), 9999f));
        document.Apply("set", TableEdit.Set(document.Image, fuel, Cells((0, 1)), -5f));
        Assert.Equal([255, 0], Stored(document, 0x100, 2));
        Assert.True(TableEdit.Fits(document.Image, fuel, 2, 1020f));
        Assert.False(TableEdit.Fits(document.Image, fuel, 2, 1024f));
        Assert.False(TableEdit.Fits(document.Image, fuel, 0, -1f));
        Assert.Equal((0f, 1020f), TableEdit.Range(document.Image, fuel, 2));

        // Sixteen-bit cells, low byte first, at half a unit: 300.5 is 601.
        document.Apply("set", TableEdit.Set(document.Image, wide, Cells((0, 1)), 300.5f));
        Assert.Equal([0x59, 0x02], Stored(document, 0x202, 2));
    }

    [Fact]
    public void RaisingAndLoweringMoveByOneStepAndStopAtTheEnds() {
        var (document, fuel, _) = Mini();
        document.Apply("raise", TableEdit.Step(document.Image, fuel, Cells((0, 0), (1, 2)), 1));
        Assert.Equal([2, 2, 3, 4, 5, 7], Stored(document, 0x100, 6));
        document.Apply("lower", TableEdit.Step(document.Image, fuel, Cells((0, 0), (0, 1)), -3));
        Assert.Equal([0, 0], Stored(document, 0x100, 2));
        // Where a bigger stored value shows as a smaller number, raising still raises what is shown.
        var falling = fuel with { Scale = -1f, Bias = 100f };
        document.Apply("raise", TableEdit.Step(document.Image, falling, Cells((1, 0)), 1));
        Assert.Equal(3, document.Image[0x103]);
    }

    [Fact]
    public void CopiedCellsCanBePastedBack() {
        var (document, fuel, _) = Mini();
        Assert.Equal("1\t4\t12\r\n4\t10\t24", TableEdit.CopyText(document.Image, fuel, Cells((0, 0), (0, 1), (0, 2), (1, 0), (1, 1), (1, 2))));
        // The rectangle around the cells; what is not among them stays empty.
        Assert.Equal("1\t\r\n\t10", TableEdit.CopyText(document.Image, fuel, Cells((0, 0), (1, 1))));
        Assert.Equal("", TableEdit.CopyText(document.Image, fuel, []));

        // A block goes down from the top left cell; what falls outside the table is dropped, what is no number is skipped.
        var (writes, count) = TableEdit.Paste(document.Image, fuel, Cells((0, 1)), "6\t16\t99\r\nx\t20\r\n7\t7\r\n");
        Assert.Equal(3, count);
        document.Apply("paste", writes);
        Assert.Equal([1, 3, 4, 4, 5, 5], Stored(document, 0x100, 6));

        // One number fills the selection.
        (writes, count) = TableEdit.Paste(document.Image, fuel, Cells((0, 0), (1, 0)), "9\n");
        Assert.Equal(2, count);
        document.Apply("paste", writes);
        Assert.Equal([9, 3, 4, 9], Stored(document, 0x100, 4));
        Assert.Equal(0, TableEdit.Paste(document.Image, fuel, Cells((0, 0)), "nothing here").Count);
        Assert.Equal(0, TableEdit.Paste(document.Image, fuel, [], "5").Count);
    }

    [Fact]
    public void TheHeatColoursKeepBlackTextReadable() {
        if (DarkTheme.HighContrast) return; // the colours are then the user's contrast theme, not ours
        for (var t = 0.0; t <= 1.0; t += 0.01) {
            Assert.True(DarkTheme.Contrast(DarkTheme.HeatText, DarkTheme.Heat(t)) >= 7, $"at {t:0.00}");
        }
        Assert.NotEqual(DarkTheme.Heat(0), DarkTheme.Heat(1));
        Assert.Equal(DarkTheme.Heat(0), DarkTheme.Heat(-3));
        Assert.Equal(DarkTheme.Heat(0.5), DarkTheme.Heat(double.NaN));
        // The ring that shows where the engine is: light inside dark, one of which shows on any cell.
        Assert.True(DarkTheme.Contrast(DarkTheme.SelectionText, DarkTheme.Background) >= 7);
    }

    [Fact]
    public void TheGridShowsTheTableAndNamesEveryCell() => OnUiThread(() => {
        var (document, fuel, _) = Mini();
        using var grid = new TableGrid(document, fuel);
        Assert.Equal((2, 3), (grid.RowCount, grid.ColumnCount));
        Assert.Equal("Fuel Map", grid.AccessibleName);
        Assert.Equal("2 rows of RPM by 3 columns of MAP", grid.AccessibleDescription);
        Assert.Equal(["1", "4", "12", "4", "10", "24"], Enumerable.Range(0, 6).Select(i => grid[i % 3, i / 3].Value));
        // The headers say which side of the table they are, for a screen reader that reads them with a cell.
        Assert.Equal(["MAP 30", "MAP 60", "MAP 90"], grid.Columns.Cast<DataGridViewColumn>().Select(c => c.HeaderText));
        Assert.Equal(["RPM 10", "RPM 30"], grid.Rows.Cast<DataGridViewRow>().Select(r => r.HeaderCell.Value));

        var cell = grid[1, 1].AccessibilityObject;
        Assert.Equal(("RPM 30, MAP 60", "10"), (cell.Name, cell.Value));
        Assert.Null(cell.Description);
        Assert.Equal(["RPM 10", "RPM 30"], grid.Rows.Cast<DataGridViewRow>().Select(r => r.AccessibilityObject.Name));
        Assert.True(grid.StandardTab, "Tab leaves the grid for the rest of the page");
        Assert.False(grid.AllowUserToAddRows);

        // Where the engine is and how often it was there are in words too.
        grid.Trace = (0.6f, 1.2f);
        grid.Hits = [0, 1, 0, 0, 7, 0];
        Assert.Equal("The engine is running here. 7 readings here", grid[1, 1].AccessibilityObject.Description);
        Assert.Equal("The engine is running here. 7 readings here", grid[1, 1].AccessibilityObject.Help);
        Assert.Equal("1 reading here", grid[1, 0].AccessibilityObject.Description);
        Assert.Equal("No readings here", grid[0, 0].AccessibilityObject.Description);
        Assert.Throws<ArgumentOutOfRangeException>(() => grid.Hits = [1, 2]);
        grid.Size = grid.ContentSize;
        using var picture = new Bitmap(grid.Width, grid.Height);
        grid.DrawToBitmap(picture, new Rectangle(Point.Empty, grid.Size));
        // The corner where rows and columns meet stays empty; a cell has its heat colour (the first is selected),
        // inside the frame that shows the engine near it.
        Assert.Equal(DarkTheme.Background.ToArgb(), picture.GetPixel(2, 2).ToArgb());
        if (!DarkTheme.HighContrast) Assert.Equal(DarkTheme.Heat(1).ToArgb(), picture.GetPixel(grid.Width - 9, grid.Height - 9).ToArgb());
    });

    [Fact]
    public void TypingRaisingAndPastingChangeTheSelectedCells() => OnUiThread(() => {
        var (document, fuel, _) = Mini();
        using var grid = new TableGrid(document, fuel);
        var said = new List<string>();
        grid.Announced += said.Add;

        // What the field for typing hands over when Enter is pressed.
        grid.CurrentCell = grid[1, 0];
        grid[1, 0].Value = "8";
        Assert.Equal(4, document.Image[0x101]);
        Assert.Equal(("Change Fuel Map", "Set to 8"), (document.UndoLabel, said[^1]));
        Assert.Equal("8", grid[1, 0].Value);

        // With several cells selected, what is typed in one goes into all.
        grid[1, 1].Selected = true;
        grid[1, 0].Value = "6";
        Assert.Equal([3, 3], new[] { document.Image[0x101], document.Image[0x104] });
        Assert.Equal("Set 2 cells to 6", said[^1]);

        grid[1, 0].Value = "six";
        Assert.Equal("\"six\" is not a number, so nothing was changed. Type a number such as 12.5.", said[^1]);
        grid[1, 0].Value = "2000";
        Assert.Equal("A cell here holds 0 to 510. It was set to 510.", said[^1]);
        Assert.Equal([255, 255], new[] { document.Image[0x101], document.Image[0x104] });

        Assert.True(grid.TypeChar('+'));
        Assert.Equal("Already at the largest value a cell here holds.", said[^1]);
        Assert.True(grid.TypeChar('-'));
        Assert.Equal(("Lower Fuel Map", "Lowered 2 cells"), (document.UndoLabel, said[^1]));
        Assert.Equal([254, 254], new[] { document.Image[0x101], document.Image[0x104] });
        grid.ClearSelection();
        grid[2, 1].Selected = true;
        Assert.True(grid.TypeChar('='));
        Assert.Equal((7, "28"), (document.Image[0x105], said[^1]));
        Assert.False(grid.TypeChar('a'));

        grid[1, 1].Selected = true;
        Assert.Equal("508\t28", grid.CopyText());
        grid.Paste("2\t8");
        Assert.Equal((1, 2, "Pasted 2 cells", "Paste into Fuel Map"), (document.Image[0x104], document.Image[0x105], said[^1], document.UndoLabel));
        grid.Paste("");
        Assert.Equal("There are no numbers to paste. Copy cells from a table or a spreadsheet first.", said[^1]);

        // Undoing, which changes the document behind the grid's back, shows at once.
        document.Undo();
        Assert.Equal("28", grid[2, 1].Value);

        grid.ReadOnly = true;
        var before = document.Image;
        grid.Paste("1");
        grid.Nudge(1);
        grid.TypeValue(0, 0, "5");
        Assert.False(grid.TypeChar('+'));
        Assert.Same(before, document.Image);
    });

    [Fact]
    public void ADigitStartsTypingInTheCell() => OnUiThread(() => {
        var (document, fuel, _) = Mini();
        using var form = new Form { ShowInTaskbar = false };
        using var grid = new TableGrid(document, fuel) { Dock = DockStyle.Fill };
        form.Controls.Add(grid);
        _ = form.Handle;
        _ = grid.Handle;
        grid.CurrentCell = grid[1, 1];
        Assert.True(grid.TypeChar('7'));
        Assert.True(grid.IsCurrentCellInEditMode);
        var box = Assert.IsType<DataGridViewTextBoxEditingControl>(grid.EditingControl);
        Assert.Equal(("7", "RPM 30, MAP 60"), (box.Text, box.AccessibleName));
        // While a value is being typed, + and - are characters of it, not commands.
        Assert.False(grid.TypeChar('-'));
        box.Text = "7,9";
        Assert.True(grid.EndEdit());
        Assert.Equal((4, "8"), ((int)document.Image[0x104], (string)grid[1, 1].Value!));

        // The keyboard reaches the same place: a character typed into the grid's window.
        void Type(char c) {
            object[] message = [Message.Create(grid.Handle, 0x0102, c, 0)];
            typeof(Control).GetMethod("WndProc", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(grid, message);
        }
        Type('+');
        Assert.Equal(5, document.Image[0x104]);
        Type('3');
        Assert.Equal("3", grid.EditingControl?.Text);
        Assert.True(grid.CancelEdit() && grid.EndEdit());
        Assert.Equal(5, document.Image[0x104]);

        // F2 is the standard key for changing a cell; letters do not start it.
        Assert.False(grid[1, 1].KeyEntersEditMode(new KeyEventArgs(Keys.A)));
        Assert.Equal(DataGridViewEditMode.EditOnF2, grid.EditMode);
    });

    [Fact]
    public void TheGridIsAsLargeAsItsCellsAndGrowsWithTheText() => OnUiThread(() => {
        var (document, fuel, _) = Mini();
        using var grid = new TableGrid(document, fuel);
        var size = grid.ContentSize;
        Assert.Equal(grid.RowHeadersWidth + 3 * grid.Columns[0].Width, size.Width);
        Assert.Equal(grid.ColumnHeadersHeight + 2 * grid.Rows[0].Height, size.Height);
        Assert.True(grid.Rows[0].Height > grid.Font.Height);
        Assert.Equal(size, grid.FitSize(size.Width));
        // Too little room: as wide as there is, with a scroll bar under the cells.
        Assert.Equal(new Size(size.Width - 10, size.Height + SystemInformation.HorizontalScrollBarHeight), grid.FitSize(size.Width - 10));

        // A longer number widens the columns.
        document.Apply("set", TableEdit.Set(document.Image, fuel, [new CellRef(0, 2)], 1000f));
        Assert.True(grid.ContentSize.Width > size.Width);
        var wide = grid.ContentSize;

        // The counts of readings need a line of their own.
        grid.Hits = new int[6];
        Assert.True(grid.ContentSize.Height > wide.Height);
        grid.Hits = null;
        Assert.Equal(wide, grid.ContentSize);

        using var large = new Font(grid.Font.FontFamily, grid.Font.SizeInPoints * 2f);
        grid.Font = large;
        Assert.True(grid.ContentSize.Width > wide.Width * 3 / 2 && grid.ContentSize.Height > wide.Height * 3 / 2);
    });

    [Fact]
    public void TheTablePageHoldsTheGridAndKeepsItWhileTheTableChanges() => OnUiThread(() => {
        var session = AppShellTests.MiniSession();
        using var view = new PageView { Size = new Size(700, 500) };
        view.Page = Details.Param(session.Document, session.Defs[0]);
        var grid = Assert.IsType<TableGrid>(view.Grid);
        var help = view.Controls.OfType<Label>().Single(l => l.Text == Details.GridHelp);
        // The cells come first: only the name of the table and its group stand above them.
        Assert.True(help.Top >= grid.Bottom, "what can be done with the cells is said under them");
        Assert.All(view.Controls.Cast<Control>().Where(c => c != grid && c.Top < grid.Top), c => Assert.True(c.Text is "Fuel Map" or "Fuel", c.Text));
        Assert.Equal(grid.ContentSize, grid.Size);

        // In the tab order it stands where it is on the page: before the rows and the button under it.
        var order = view.Controls.Cast<Control>().OrderBy(c => c.TabIndex).ToList();
        Assert.True(order.IndexOf(grid) < order.FindIndex(c => c is TextBox { AccessibleName: "Values" }));
        Assert.True(order.IndexOf(grid) < order.FindIndex(c => c is Button));
        // Down the page, or to the right where the pictures stand beside the cells.
        Assert.All(order.Zip(order.Skip(1)), pair => Assert.True(pair.Second.Top >= pair.First.Top || pair.Second.Left >= pair.First.Right, $"`{pair.Second.Text}` is above `{pair.First.Text}`"));

        var heard = new List<string>();
        view.Announced += heard.Add;
        session.Document.Changed += () => view.Revise(Details.Param(session.Document, session.Defs[0]));
        grid.ClearSelection();
        grid[2, 1].Selected = true;
        grid.Nudge(1);
        Assert.Same(grid, view.Grid);
        Assert.False(grid.IsDisposed);
        Assert.True(grid[2, 1].Selected);
        Assert.Equal(["28"], heard);
        // The rest of the page follows, and says what was announced to those who did not hear it.
        Assert.Contains(view.Controls.OfType<TextBox>(), v => v.AccessibleName == "Values" && v.Text == "1 to 28 raw");
        Assert.Contains("28", view.Controls.OfType<Label>().Select(l => l.Text));

        view.ShowTechnical = true;
        Assert.Same(grid, view.Grid);
        // Another table, or a page without one, gets a grid of its own or none.
        view.Page = Details.Param(session.Document, session.Defs[1]);
        Assert.True(grid.IsDisposed);
        Assert.Equal("Wide Map", view.Grid?.AccessibleName);
        view.Page = Details.Param(session.Document, session.Defs[2]);
        Assert.Null(view.Grid);
    });

    [Fact]
    public void ChangingATableInTheWindowLeavesTheFileAsItWasJudged() => OnUiThread(() => {
        var folder = Path.Combine(Path.GetTempPath(), $"hre-grid-{Guid.NewGuid():N}");
        Directory.CreateDirectory(folder);
        try {
            var path = Path.Combine(folder, "mini.bin");
            Checksum.Fix(DefinitionPackTests.MiniRom(), DefinitionPackTests.Mini().Checksum).Save(path);
            using var form = new MainForm([DefinitionPackTests.Mini()], [], new AppSettings());
            form.OpenRom(path);
            Assert.True(form.SelectParam("Fuel Map"));
            var grid = Assert.IsType<TableGrid>(form.Page.Grid);
            grid.TypeValue(0, 0, "40");
            Assert.Same(grid, form.Page.Grid);
            Assert.Equal("4 to 40 raw", AppShellTests.Value(form.Page.Page!, "Values"));
            Assert.True(form.Session!.Document.IsDirty);
            // The checksum is that of the file; a change is not damage, and the file is untouched.
            Assert.Equal(ChecksumState.Valid, form.Session.Checksum);
            Assert.Contains("Checksum correct", form.StatusTexts);
            Assert.Equal(1, File.ReadAllBytes(path)[0x100]);
        } finally {
            Directory.Delete(folder, recursive: true);
        }
    });
}
