using System.ComponentModel;
using System.Runtime.InteropServices;
using HRE.Core;

namespace HRE.App;

/// <summary>
/// The cells of a table, to read and to change. It is the standard grid of Windows Forms, which
/// brings the keyboard and what a screen reader needs, with the cells drawn here: each in a
/// colour that runs from the smallest value of the table to the largest, the selection, where the
/// engine is running and how often it has been in each cell.
/// </summary>
public sealed class TableGrid : DataGridView {
    private readonly ParamKind.Table _table;
    private RomImage? _shown;
    private Font? _hitFont;
    private int _decimals;
    private float _min;
    private float _max;

    public TableGrid(RomDocument document, ParamDef def) {
        Document = document;
        Def = def;
        _table = (ParamKind.Table)def.Kind;

        AccessibleName = def.Name;
        AccessibleDescription = $"{_table.Rows} rows of {def.RowAxis.Label} by {_table.Cols} columns of {def.ColAxis.Label}";
        VirtualMode = true;
        AllowUserToAddRows = AllowUserToDeleteRows = AllowUserToOrderColumns = AllowUserToResizeColumns = AllowUserToResizeRows = false;
        SelectionMode = DataGridViewSelectionMode.CellSelect;
        MultiSelect = true;
        EditMode = DataGridViewEditMode.EditOnF2;
        // Tab leaves the grid for the next thing on the page; the arrow keys move inside it.
        StandardTab = true;
        // Copying is done here, so that what is copied can be pasted back.
        ClipboardCopyMode = DataGridViewClipboardCopyMode.Disable;
        ShowCellToolTips = false;
        BorderStyle = BorderStyle.None;
        CellBorderStyle = DataGridViewCellBorderStyle.None;
        ColumnHeadersBorderStyle = RowHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        EnableHeadersVisualStyles = false;
        ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing;
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
        AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
        DoubleBuffered = true;

        for (var col = 0; col < _table.Cols; col++) {
            Columns.Add(new DataGridViewColumn(new Cell()) { SortMode = DataGridViewColumnSortMode.NotSortable, Resizable = DataGridViewTriState.False, ValueType = typeof(string) });
        }
        RowTemplate = new Row();
        RowCount = _table.Rows;
        ApplyTheme();
        Reload();
        document.Changed += Reload;
    }

    /// <summary>Something happened that the user should be told: what an action did, or why it did nothing.</summary>
    public event Action<string>? Announced;

    /// <summary>The size at which every cell is in view changed: a longer number, another font, the counts of readings.</summary>
    public event Action? ContentSizeChanged;

    public RomDocument Document { get; }
    public ParamDef Def { get; }

    /// <summary>
    /// Where the engine is running, as a position between the cells (see <see cref="TableMath.TablePos"/>);
    /// null when nothing is being logged. The cells around it are framed, the nearest most
    /// heavily, and the nearest says so in its description.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public (float Row, float Col)? Trace {
        get;
        set {
            field = value;
            Invalidate();
        }
    }

    /// <summary>
    /// How many logged readings fell in each cell, row by row; null when nothing was logged. The
    /// count is written in the corner of its cell and told in its description.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public IReadOnlyList<int>? Hits {
        get;
        set {
            if (value is not null) ArgumentOutOfRangeException.ThrowIfNotEqual(value.Count, _table.Rows * _table.Cols);
            var resize = (field is null) != (value is null);
            field = value;
            if (resize) Measure();
            Invalidate();
        }
    }

    /// <summary>The size at which every cell is in view.</summary>
    public Size ContentSize {
        get {
            Reload();
            return new Size(RowHeadersWidth + Columns.GetColumnsWidth(DataGridViewElementStates.Visible), ColumnHeadersHeight + Rows.GetRowsHeight(DataGridViewElementStates.Visible));
        }
    }

    /// <summary>The cells an action works on: the selected ones.</summary>
    public IReadOnlyList<CellRef> SelectedRefs => [.. SelectedCells.Cast<DataGridViewCell>().Select(c => new CellRef(c.RowIndex, c.ColumnIndex)).Order(Comparer<CellRef>.Create((a, b) => (a.Row, a.Col).CompareTo((b.Row, b.Col))))];

    /// <summary>The size to give the grid where no more than <paramref name="width"/> is free: all of it, or as wide as there is room with a scroll bar under it.</summary>
    public Size FitSize(int width) {
        var content = ContentSize;
        return content.Width <= width ? content : new Size(Math.Max(width, RowHeadersWidth + Columns[0].Width), content.Height + SystemInformation.HorizontalScrollBarHeight);
    }

    /// <summary>Takes the colours of the theme: at start, and when a Windows contrast theme is turned on or off.</summary>
    public void ApplyTheme() {
        BackgroundColor = GridColor = DarkTheme.Background;
        // The cells are drawn here; these colours are what the field for typing a value takes.
        DefaultCellStyle.BackColor = ColumnHeadersDefaultCellStyle.BackColor = RowHeadersDefaultCellStyle.BackColor = DarkTheme.Background;
        DefaultCellStyle.ForeColor = DarkTheme.Text;
        DefaultCellStyle.SelectionBackColor = DarkTheme.Selection;
        DefaultCellStyle.SelectionForeColor = DarkTheme.SelectionText;
        DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        Invalidate();
    }

    /// <summary>The name of a cell: where it is, in the words of the two sides of the table.</summary>
    public string CellName(int row, int col) => $"{Def.RowAxis.Label} {RowHeader(row)}, {Def.ColAxis.Label} {ColumnHeader(col)}";

    /// <summary>What the overlays say about a cell, in words; empty when they say nothing.</summary>
    public string CellNote(int row, int col) {
        var notes = new List<string>();
        if (Trace is { } trace && (int)MathF.Round(trace.Row) == row && (int)MathF.Round(trace.Col) == col) notes.Add("The engine is running here");
        if (Hits is { } hits) notes.Add(hits[row * _table.Cols + col] switch { 0 => "No readings here", 1 => "1 reading here", var n => $"{n} readings here" });
        return string.Join(". ", notes);
    }

    /// <summary>Sets the cell at (row, col) to what was typed; when that cell is one of several selected, all of them.</summary>
    public void TypeValue(int row, int col, string typed) {
        if (ReadOnly) return;
        if (TableEdit.Parse(typed) is not { } value) {
            Announced?.Invoke($"\"{typed.Trim()}\" is not a number, so nothing was changed. Type a number such as 12.5.");
            return;
        }
        var cells = SelectedRefs is { Count: > 1 } selected && selected.Contains(new CellRef(row, col)) ? selected : [new CellRef(row, col)];
        Document.Apply($"Change {Def.Name}", TableEdit.Set(Document.Image, Def, cells, value));
        var shown = TableEdit.CellText(Document.Image, Def, row, col);
        if (!TableEdit.Fits(Document.Image, Def, col, value)) {
            var (min, max) = TableEdit.Range(Document.Image, Def, col);
            Announced?.Invoke($"A cell here holds {TableEdit.Text(min, _decimals)} to {TableEdit.Text(max, _decimals)}. It was set to {shown}.");
        } else {
            Announced?.Invoke(cells.Count == 1 ? $"Set to {shown}" : $"Set {cells.Count} cells to {shown}");
        }
    }

    /// <summary>Raises (or, with a negative count, lowers) the selected cells by that many of the smallest step a cell can take.</summary>
    public void Nudge(int steps) {
        var cells = SelectedRefs;
        if (ReadOnly || cells.Count == 0 || steps == 0) return;
        var up = steps > 0;
        if (!Document.Apply($"{(up ? "Raise" : "Lower")} {Def.Name}", TableEdit.Step(Document.Image, Def, cells, steps))) {
            Announced?.Invoke(up ? "Already at the largest value a cell here holds." : "Already at the smallest value a cell here holds.");
        } else if (cells.Count == 1) {
            Announced?.Invoke(TableEdit.CellText(Document.Image, Def, cells[0].Row, cells[0].Col));
        } else {
            Announced?.Invoke($"{(up ? "Raised" : "Lowered")} {cells.Count} cells");
        }
    }

    /// <summary>The selected cells as text for the clipboard.</summary>
    public string CopyText() => TableEdit.CopyText(Document.Image, Def, SelectedRefs);

    /// <summary>Pastes clipboard text at the selection: one number into every selected cell, a block from the top left selected cell.</summary>
    public void Paste(string text) {
        if (ReadOnly) return;
        var (writes, count) = TableEdit.Paste(Document.Image, Def, SelectedRefs, text);
        if (count == 0) {
            Announced?.Invoke("There are no numbers to paste. Copy cells from a table or a spreadsheet first.");
            return;
        }
        Document.Apply($"Paste into {Def.Name}", writes);
        Announced?.Invoke(count == 1 ? "Pasted 1 cell" : $"Pasted {count} cells");
    }

    /// <summary>
    /// A character typed while no cell is being edited: a digit starts typing a value, + and -
    /// raise and lower the selection. False when the character means nothing here.
    /// </summary>
    internal bool TypeChar(char typed) {
        if (ReadOnly || IsCurrentCellInEditMode || CurrentCell is null) return false;
        switch (typed) {
            // On keyboards where + is Shift and =, the bare key does the same.
            case '+' or '=':
                Nudge(1);
                return true;
            case '-':
                Nudge(-1);
                return true;
            case '.' or ',' or (>= '0' and <= '9'):
                if (BeginEdit(false) && EditingControl is TextBox box) {
                    box.Text = typed.ToString();
                    box.SelectionStart = 1;
                }
                return true;
            default:
                return false;
        }
    }

    protected override void OnCellValueNeeded(DataGridViewCellValueEventArgs e) {
        base.OnCellValueNeeded(e);
        e.Value = TableEdit.CellText(Document.Image, Def, e.RowIndex, e.ColumnIndex);
    }

    protected override void OnCellValuePushed(DataGridViewCellValueEventArgs e) {
        base.OnCellValuePushed(e);
        TypeValue(e.RowIndex, e.ColumnIndex, e.Value?.ToString() ?? "");
    }

    protected override void OnKeyPress(KeyPressEventArgs e) {
        base.OnKeyPress(e);
        if (!e.Handled) e.Handled = TypeChar(e.KeyChar);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData) {
        if (IsCurrentCellInEditMode) return base.ProcessCmdKey(ref msg, keyData);
        switch (keyData) {
            case Keys.Control | Keys.C:
                UseClipboard(() => {
                    var text = CopyText();
                    if (text.Length == 0) return;
                    Clipboard.SetText(text);
                    Announced?.Invoke(SelectedCells.Count == 1 ? "Copied 1 cell" : $"Copied {SelectedCells.Count} cells");
                });
                return true;
            case Keys.Control | Keys.V:
                UseClipboard(() => Paste(Clipboard.GetText()));
                return true;
            default:
                return base.ProcessCmdKey(ref msg, keyData);
        }
    }

    protected override void OnEditingControlShowing(DataGridViewEditingControlShowingEventArgs e) {
        base.OnEditingControlShowing(e);
        e.CellStyle.BackColor = DarkTheme.Background;
        e.CellStyle.ForeColor = DarkTheme.Text;
        if (e.Control is not TextBox box) return;
        box.TextAlign = HorizontalAlignment.Center;
        box.AccessibleName = CurrentCell is { } cell ? CellName(cell.RowIndex, cell.ColumnIndex) : Def.Name;
    }

    protected override void OnFontChanged(EventArgs e) {
        base.OnFontChanged(e);
        Measure();
    }

    protected override void OnGotFocus(EventArgs e) {
        base.OnGotFocus(e);
        Invalidate();
    }

    protected override void OnLostFocus(EventArgs e) {
        base.OnLostFocus(e);
        Invalidate();
    }

    protected override void OnCurrentCellChanged(EventArgs e) {
        base.OnCurrentCellChanged(e);
        // The headers of the current cell's row and column stand out.
        Invalidate();
    }

    protected override void OnCellPainting(DataGridViewCellPaintingEventArgs e) {
        base.OnCellPainting(e);
        if (e.Handled || e.Graphics is not { } g) return;
        e.Handled = true;
        using (var gap = new SolidBrush(DarkTheme.Background)) g.FillRectangle(gap, e.CellBounds);
        if (e.RowIndex < 0 && e.ColumnIndex < 0) return;
        if (e.RowIndex < 0 || e.ColumnIndex < 0) {
            var current = e.RowIndex < 0 ? CurrentCellAddress.X == e.ColumnIndex : CurrentCellAddress.Y == e.RowIndex;
            var text = e.RowIndex < 0 ? ColumnHeader(e.ColumnIndex) : RowHeader(e.RowIndex);
            TextRenderer.DrawText(g, text, Font, e.CellBounds, current ? DarkTheme.Text : DarkTheme.TextDim, Centred);
            return;
        }

        var selected = (e.State & DataGridViewElementStates.Selected) != 0;
        var back = selected ? DarkTheme.Selection : DarkTheme.Heat(Warmth(e.RowIndex, e.ColumnIndex));
        var fore = selected ? DarkTheme.SelectionText : DarkTheme.HeatText;
        // One pixel of the background stays between cells.
        var box = new Rectangle(e.CellBounds.X, e.CellBounds.Y, e.CellBounds.Width - 1, e.CellBounds.Height - 1);
        using (var fill = new SolidBrush(back)) g.FillRectangle(fill, box);

        var value = box;
        if (Hits is { } hits && _hitFont is { } hitFont) {
            var count = hits[e.RowIndex * _table.Cols + e.ColumnIndex];
            // Clear of the frame that shows where the engine is.
            var band = new Rectangle(box.X, box.Y + Font.Height / 5, box.Width - Font.Height * 2 / 5, hitFont.Height);
            if (count > 0) TextRenderer.DrawText(g, count.ToString(System.Globalization.CultureInfo.InvariantCulture), hitFont, band, fore, TextFormatFlags.Right | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
            value = Rectangle.FromLTRB(box.Left, box.Top + hitFont.Height, box.Right, box.Bottom);
        }
        TextRenderer.DrawText(g, e.FormattedValue?.ToString() ?? "", Font, value, fore, Centred);

        if (CurrentCellAddress == new Point(e.ColumnIndex, e.RowIndex) && ContainsFocus) {
            var width = Math.Max(2, Font.Height / 10);
            using var pen = new Pen(fore, width) { Alignment = System.Drawing.Drawing2D.PenAlignment.Inset };
            g.DrawRectangle(pen, box.X, box.Y, box.Width, box.Height);
        }
    }

    protected override void OnPaint(PaintEventArgs e) {
        base.OnPaint(e);
        if (Trace is not { } trace || _table.Rows == 0 || _table.Cols == 0) return;
        // A position between steps lies between cells: the engine computer blends the four around
        // it. Each is framed, the heavier the more it counts. The frames stay clear of the numbers.
        var (width, height) = (Columns[0].Width, Rows[0].Height);
        var (row, col) = (Math.Clamp(trace.Row, 0f, _table.Rows - 1), Math.Clamp(trace.Col, 0f, _table.Cols - 1));
        var (top, left) = ((int)row, (int)col);
        var stroke = Math.Max(2f, Font.Height / 9f);
        var g = e.Graphics;
        var state = g.Save();
        g.SetClip(Rectangle.FromLTRB(RowHeadersWidth, ColumnHeadersHeight, ClientSize.Width, ClientSize.Height));
        foreach (var (r, c, share) in new[] {
            (top, left, (1 - (row - top)) * (1 - (col - left))), (top, left + 1, (1 - (row - top)) * (col - left)),
            (top + 1, left, (row - top) * (1 - (col - left))), (top + 1, left + 1, (row - top) * (col - left)),
        }) {
            if (share < 0.02f || r >= _table.Rows || c >= _table.Cols) continue;
            var box = new Rectangle(RowHeadersWidth - HorizontalScrollingOffset + c * width, ColumnHeadersHeight - VerticalScrollingOffset + r * height, width - 1, height - 1);
            var line = (int)MathF.Round(stroke * (0.5f + 2f * share));
            // Dark around light: one of the two shows on any cell.
            using var outer = new Pen(DarkTheme.Background, line + 2) { Alignment = System.Drawing.Drawing2D.PenAlignment.Inset };
            using var inner = new Pen(DarkTheme.SelectionText, line) { Alignment = System.Drawing.Drawing2D.PenAlignment.Inset };
            g.DrawRectangle(outer, box);
            g.DrawRectangle(inner, Rectangle.Inflate(box, -1, -1));
        }
        g.Restore(state);
    }

    protected override void Dispose(bool disposing) {
        if (disposing) {
            Document.Changed -= Reload;
            _hitFont?.Dispose();
        }
        base.Dispose(disposing);
    }

    private const TextFormatFlags Centred = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine;

    private string RowHeader(int row) => AxisText(Def.RowAxis, row);

    private string ColumnHeader(int col) => AxisText(Def.ColAxis, col);

    /// <summary>A step of a row or a column, rounded for reading: whole from a thousand up, one decimal below.</summary>
    private string AxisText(Axis axis, int i) {
        var value = axis.Display(ParamAccess.AxisValue(Document.Image, axis, i));
        return TableEdit.Text(value, MathF.Abs(value) >= 1000f ? 0 : 1);
    }

    /// <summary>Where a cell lies between the smallest value of the table (0) and the largest (1).</summary>
    private double Warmth(int row, int col) =>
        _max > _min ? (ParamAccess.CellDisplay(Document.Image, Def, row, col) - _min) / (_max - _min) : 0.5;

    /// <summary>Takes in the document as it is now: after a change, an undo or a redo.</summary>
    private void Reload() {
        if (ReferenceEquals(_shown, Document.Image)) return;
        _shown = Document.Image;
        _decimals = TableEdit.Decimals(_shown, Def);
        (_min, _max) = (float.MaxValue, float.MinValue);
        for (var row = 0; row < _table.Rows; row++) {
            for (var col = 0; col < _table.Cols; col++) {
                var value = ParamAccess.CellDisplay(_shown, Def, row, col);
                (_min, _max) = (MathF.Min(_min, value), MathF.Max(_max, value));
            }
        }
        for (var col = 0; col < _table.Cols; col++) Columns[col].HeaderText = $"{Def.ColAxis.Label} {ColumnHeader(col)}";
        for (var row = 0; row < _table.Rows; row++) Rows[row].HeaderCell.Value = $"{Def.RowAxis.Label} {RowHeader(row)}";
        Measure();
        Invalidate();
    }

    /// <summary>Sizes rows, columns and headers to the font and to the widest text they hold.</summary>
    private void Measure() {
        if (_shown is not { } image) return;
        var before = (RowHeadersWidth, ColumnHeadersHeight, Columns[0].Width, Rows[0].Height);
        _hitFont?.Dispose();
        _hitFont = Hits is null ? null : new Font(Font.FontFamily, Font.SizeInPoints * 0.72f);
        int Widest(IEnumerable<string> texts) => texts.DefaultIfEmpty("").Max(t => TextRenderer.MeasureText(t, Font, Size.Empty, TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine).Width);
        var pad = Font.Height * 3 / 4;
        var cells = Enumerable.Range(0, _table.Rows).SelectMany(row => Enumerable.Range(0, _table.Cols).Select(col => TableEdit.CellText(image, Def, row, col)));
        var columnWidth = Widest(cells.Concat(Enumerable.Range(0, _table.Cols).Select(ColumnHeader))) + pad;
        var rowHeight = Font.Height + Font.Height / 2 + (_hitFont?.Height ?? 0);
        foreach (DataGridViewColumn column in Columns) column.Width = columnWidth;
        RowTemplate.Height = rowHeight;
        foreach (DataGridViewRow row in Rows) row.Height = rowHeight;
        RowHeadersWidth = Widest(Enumerable.Range(0, _table.Rows).Select(RowHeader)) + pad;
        ColumnHeadersHeight = Font.Height + Font.Height / 2;
        if (before != (RowHeadersWidth, ColumnHeadersHeight, columnWidth, rowHeight)) ContentSizeChanged?.Invoke();
    }

    /// <summary>Another program can hold the clipboard for a moment; that is told, not thrown.</summary>
    private void UseClipboard(Action action) {
        try {
            action();
        } catch (ExternalException) {
            Announced?.Invoke("Another program is using the clipboard. Try again.");
        }
    }

    /// <summary>A cell that starts typing only through <see cref="TypeChar"/>, and tells a screen reader where it is and what the overlays show.</summary>
    private sealed class Cell : DataGridViewTextBoxCell {
        public override bool KeyEntersEditMode(KeyEventArgs e) => false;

        protected override AccessibleObject CreateAccessibilityInstance() => new Accessible(this);

        private sealed class Accessible(Cell owner) : DataGridViewTextBoxCellAccessibleObject(owner) {
            public override string? Name => owner.DataGridView is TableGrid grid ? grid.CellName(owner.RowIndex, owner.ColumnIndex) : base.Name;

            public override string? Description => Note ?? base.Description;

            // Screen readers differ in which of the two they read; both say the same.
            public override string? Help => Note ?? base.Help;

            private string? Note => owner.DataGridView is TableGrid grid && grid.CellNote(owner.RowIndex, owner.ColumnIndex) is { Length: > 0 } note ? note : null;
        }
    }

    /// <summary>A row that is called by its step ("RPM 3000"), not by its number.</summary>
    private sealed class Row : DataGridViewRow {
        protected override AccessibleObject CreateAccessibilityInstance() => new Accessible(this);

        private sealed class Accessible(Row owner) : DataGridViewRowAccessibleObject(owner) {
            public override string Name => owner.DataGridView is TableGrid grid && owner.Index >= 0 ? $"{grid.Def.RowAxis.Label} {grid.RowHeader(owner.Index)}" : base.Name;
        }
    }
}
