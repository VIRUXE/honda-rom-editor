using System.ComponentModel;
using HRE.Core;

namespace HRE.App;

/// <summary>
/// Shows a <see cref="DetailPage"/> as real controls, so that a screen reader can read it, the
/// keyboard can walk it and its values can be copied: labels for text, read-only fields for
/// values, buttons for actions. It lays them out in one reading column and scrolls when the page
/// is taller than the view. All sizes come from the font, so the page follows the display
/// scaling and the user's text size.
/// </summary>
public sealed class PageView : Panel {
    /// <summary>The least width at which a picture of a table can be read, and its least height, in lines of text.</summary>
    private const float ChartWidth = 18f;

    private const float ChartHeight = 13f;

    private const TextFormatFlags Wrapped = TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl | TextFormatFlags.NoPrefix;

    /// <summary>Each part places its controls at (x, y) in the given width and returns the height it took.</summary>
    private readonly List<Func<int, int, int, int>> parts = [];

    private Font? titleFont;
    private Font? headingFont;
    private Font? glyphFont;
    private DetailPage? page;
    private TableGrid? grid;
    private TableGraph? graph;
    private TableSurface? surface;
    private SettingBox? setting;
    private Label? note;
    private bool arranging;

    public PageView() {
        AutoScroll = true;
        DoubleBuffered = true;
        AccessibleName = "Details";
        BackColor = DarkTheme.Background;
        ForeColor = DarkTheme.Text;
    }

    /// <summary>A button of the page was pressed.</summary>
    public event Action<PageAction>? ActionInvoked;

    /// <summary>The button that shows or hides the technical sections was pressed.</summary>
    public event Action? TechnicalToggled;

    /// <summary>The table or the setting on the page has something to tell: what an action did, or why it did nothing.</summary>
    public event Action<string>? Announced;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public DetailPage? Page {
        get => page;
        set {
            page = value;
            AutoScrollPosition = Point.Empty;
            Rebuild();
        }
    }

    /// <summary>The cells of the table on the page; null when the page shows no table.</summary>
    public TableGrid? Grid => grid;

    /// <summary>The graph of the table on the page; null when the page shows no table.</summary>
    public TableGraph? Graph => graph;

    /// <summary>The surface of the table on the page; null when the page shows no table, or one of a single row or column.</summary>
    public TableSurface? Surface => surface;

    /// <summary>The field of the setting on the page; null when the page shows no setting that can be changed.</summary>
    public SettingBox? Setting => setting;

    /// <summary>
    /// Writes what an action did on the line under the table or the setting, for those who do not
    /// hear it announced. A page without either has no such line.
    /// </summary>
    public void Say(string text) {
        if (note is null) return;
        note.Text = text;
        Arrange();
    }

    /// <summary>
    /// Takes what is being typed into a cell or into the setting as its value, as Enter would.
    /// Called before the document is saved or left, so that nothing typed is lost.
    /// </summary>
    public void FinishTyping() {
        grid?.EndEdit();
        setting?.FinishTyping();
    }

    /// <summary>Whether the technical sections of a page are shown.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool ShowTechnical {
        get;
        set {
            if (field == value) return;
            field = value;
            Rebuild();
        }
    }

    /// <summary>Gives the keyboard focus to the first thing on the page that takes it. False when nothing does.</summary>
    public bool FocusFirst() {
        var first = Controls.Cast<Control>().Where(c => c.TabStop && c.CanSelect).OrderBy(c => c.TabIndex).FirstOrDefault();
        first?.Select();
        return first is not null;
    }

    /// <summary>
    /// Shows the page again after what it describes changed. The view stays where it was scrolled
    /// to, and the table keeps its selection and the keyboard focus.
    /// </summary>
    public void Revise(DetailPage page) {
        var scrolled = -AutoScrollPosition.Y;
        this.page = page;
        Rebuild();
        AutoScrollPosition = new Point(0, scrolled);
    }

    /// <summary>Creates the controls of the page anew: after the page, the colours or the font changed.</summary>
    public void Rebuild() {
        SuspendLayout();
        // The table or the setting of the page stays when the new page shows the same one: it has the selection and may have the focus.
        var keptGrid = Page?.Blocks.OfType<PageBlock.Grid>().FirstOrDefault() is { } table && this.grid is { } grid && ReferenceEquals(grid.Document, table.Document) && grid.Def == table.Def ? grid : null;
        var keptSetting = Page?.Blocks.OfType<PageBlock.Setting>().FirstOrDefault() is { } value && setting is { } box && ReferenceEquals(box.Document, value.Document) && box.Def == value.Def ? box : null;
        // What the line under it said last stays too.
        var said = keptGrid is not null || keptSetting is not null ? note?.Text ?? "" : "";
        // The pictures of a table stay with its cells: the surface is as it was turned.
        if (keptGrid is null) (graph, surface) = (null, null);
        foreach (var old in Controls.Cast<Control>().ToArray()) {
            if (old != keptGrid && old != keptSetting && old != graph && old != surface) old.Dispose();
        }
        this.grid = keptGrid;
        setting = keptSetting;
        note = null;
        parts.Clear();
        titleFont?.Dispose();
        headingFont?.Dispose();
        glyphFont?.Dispose();
        titleFont = new Font(Font.FontFamily, Font.SizeInPoints * 1.9f, FontStyle.Bold);
        headingFont = new Font(Font, FontStyle.Bold);
        glyphFont = Glyphs.FontLike(Font, 1.25f);
        BackColor = DarkTheme.Background;
        ForeColor = DarkTheme.Text;

        if (Page is { } page) {
            // Large bold type sits a little to the right of its box; pull it back in line with the text under it.
            var title = NewLabel(page.Title, titleFont, DarkTheme.Text);
            parts.Add((x, y, width) => Fit(title, x - Em(0.2f), y, width));
            if (page.Subtitle.Length > 0) AddText(page.Subtitle, null, DarkTheme.TextDim, 0);
            foreach (var block in page.Blocks) {
                switch (block) {
                    case PageBlock.Paragraph paragraph:
                        AddText(paragraph.Value, null, DarkTheme.Text, 0.6f);
                        break;
                    case PageBlock.Notice notice:
                        AddNotice(notice);
                        break;
                    case PageBlock.Section section when ShowTechnical || !section.Technical:
                        AddSection(section);
                        break;
                    case PageBlock.Actions actions:
                        AddActions(actions);
                        break;
                    case PageBlock.Grid cells:
                        AddGrid(cells);
                        break;
                    case PageBlock.Setting setting:
                        AddSetting(setting);
                        break;
                }
            }
            if (page.Blocks.Any(b => b is PageBlock.Section { Technical: true })) {
                var toggle = NewButton(ShowTechnical ? "Hide technical details" : "Show technical details", primary: false);
                toggle.Click += (_, _) => TechnicalToggled?.Invoke();
                parts.Add((x, y, _) => {
                    toggle.Location = new Point(x, y + Em(1f));
                    return toggle.Height + Em(1f);
                });
            }
        }
        note?.Text = said;
        var index = 0;
        foreach (Control control in Controls) control.TabIndex = index++;
        ResumeLayout();
        Arrange();
    }

    protected override void OnClientSizeChanged(EventArgs e) {
        base.OnClientSizeChanged(e);
        Arrange();
    }

    protected override void OnFontChanged(EventArgs e) {
        base.OnFontChanged(e);
        Rebuild();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData) {
        // The page scrolls from the keyboard wherever the focus is inside it.
        var step = keyData switch {
            Keys.PageDown => ClientSize.Height * 9 / 10,
            Keys.PageUp => -ClientSize.Height * 9 / 10,
            Keys.Control | Keys.End => int.MaxValue / 2,
            Keys.Control | Keys.Home => -int.MaxValue / 2,
            _ => 0,
        };
        // In the table these keys move between cells, and the page follows the cell.
        if (step == 0 || grid?.ContainsFocus == true) return base.ProcessCmdKey(ref msg, keyData);
        AutoScrollPosition = new Point(0, Math.Max(0, -AutoScrollPosition.Y + step));
        return true;
    }

    /// <summary>
    /// Where to scroll when a control takes the focus. The table is taller than most views, and
    /// bringing its top into view would move it away under the pointer: the page follows the
    /// cell instead (<see cref="RevealCell"/>).
    /// </summary>
    protected override Point ScrollToControl(Control activeControl) =>
        // A picture taken hold of with the mouse must not move away under the pointer either.
        activeControl == grid || (activeControl is TableChart && MouseButtons != MouseButtons.None) ? DisplayRectangle.Location : base.ScrollToControl(activeControl);

    protected override void Dispose(bool disposing) {
        if (disposing) {
            titleFont?.Dispose();
            headingFont?.Dispose();
            glyphFont?.Dispose();
        }
        base.Dispose(disposing);
    }

    /// <summary>A length in units of the line height, which follows scaling and text size.</summary>
    private int Em(float lines) => (int)Math.Round(Font.Height * lines);

    /// <summary>Places every part, top to bottom, in a column no wider than is comfortable to read.</summary>
    private void Arrange() {
        if (arranging || parts.Count == 0) return;
        arranging = true;
        SuspendLayout();
        var margin = Em(1.25f);
        var width = Math.Clamp(ClientSize.Width - 2 * margin, Em(10f), Em(42f));
        var y = margin + AutoScrollPosition.Y;
        foreach (var part in parts) y += part(margin, y, width);
        AutoScrollMargin = new Size(0, margin);
        ResumeLayout();
        arranging = false;
    }

    /// <summary>Sizes a control to its wrapped text in the given width, and places it. Returns its height.</summary>
    private static int Fit(Control control, int x, int y, int width) {
        const int Slack = 8; // what labels and text fields keep free around their text
        var height = TextRenderer.MeasureText(control.Text, control.Font, new Size(Math.Max(width - Slack, 1), int.MaxValue), Wrapped).Height + 2;
        control.SetBounds(x, y, width, height);
        return height;
    }

    private Label NewLabel(string text, Font? font, Color color) {
        var label = new Label { Text = text, ForeColor = color, AutoSize = false, UseMnemonic = false };
        if (font is not null) label.Font = font;
        Controls.Add(label);
        return label;
    }

    /// <summary>A value that can be reached with Tab, selected and copied.</summary>
    private TextBox NewValue(string name, string text) {
        var box = new TextBox {
            Text = text,
            AccessibleName = name,
            ReadOnly = true,
            Multiline = true,
            WordWrap = true,
            BorderStyle = BorderStyle.None,
            VisualStylesMode = VisualStylesMode.Classic,
            BackColor = DarkTheme.Background,
            ForeColor = DarkTheme.Text,
        };
        box.Enter += (_, _) => box.SelectAll();
        box.Leave += (_, _) => box.DeselectAll();
        Controls.Add(box);
        return box;
    }

    private ThemedButton NewButton(string text, bool primary) {
        var button = new ThemedButton {
            Text = text,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(Em(0.5f), Em(0.15f), Em(0.5f), Em(0.15f)),
            UseMnemonic = false,
        };
        if (!DarkTheme.HighContrast) {
            // Dark text on the accent: light text on it would be too faint to read.
            button.BackColor = primary ? DarkTheme.Accent : DarkTheme.Card;
            button.ForeColor = primary ? Color.Black : DarkTheme.Text;
        }
        Controls.Add(button);
        return button;
    }

    private void AddText(string text, Font? font, Color color, float gapBefore) {
        var label = NewLabel(text, font, color);
        parts.Add((x, y, width) => Em(gapBefore) + Fit(label, x, y + Em(gapBefore), width));
    }

    private void AddHeading(string text) => AddText(text, headingFont, DarkTheme.Text, 1.1f);

    private void AddNotice(PageBlock.Notice notice) {
        var box = new NoticeBox(notice, headingFont!, glyphFont);
        Controls.Add(box);
        parts.Add((x, y, width) => {
            box.SetBounds(x, y + Em(0.8f), width, box.Arrange(width));
            return box.Height + Em(0.8f);
        });
    }

    private void AddSection(PageBlock.Section section) {
        AddHeading(section.Heading);
        var rows = new List<(Label? Label, Control Value)>();
        foreach (var row in section.Rows) {
            if (row.Label.Length > 0) rows.Add((NewLabel(row.Label, null, DarkTheme.TextDim), NewValue(row.Label, row.Value)));
            // Running text is read, not copied; in a technical section even a bare line is worth copying.
            else rows.Add((null, section.Technical ? NewValue(section.Heading, row.Value) : NewLabel(row.Value, null, DarkTheme.Text)));
        }
        parts.Add((x, y, width) => {
            var gap = Em(0.75f);
            var widest = rows.Where(r => r.Label is not null).Select(r => TextRenderer.MeasureText(r.Label!.Text, Font).Width).DefaultIfEmpty(0).Max();
            var labelWidth = Math.Min(widest + gap, width / 3);
            var height = Em(0.35f);
            foreach (var (label, value) in rows) {
                var taken = label is null
                    ? Fit(value, x, y + height, width)
                    : Math.Max(Fit(label, x, y + height, labelWidth), Fit(value, x + labelWidth + gap, y + height, width - labelWidth - gap));
                height += taken + Em(0.3f);
            }
            return height;
        });
    }

    private void AddGrid(PageBlock.Grid block) {
        if (this.grid is null) {
            this.grid = new TableGrid(block.Document, block.Def);
            this.grid.Announced += text => {
                Say(text);
                Announced?.Invoke(text);
            };
            this.grid.ContentSizeChanged += () => {
                Arrange();
                RevealCell();
            };
            this.grid.CurrentCellChanged += (_, _) => RevealCell();
            this.grid.Enter += (_, _) => RevealCell();
            Controls.Add(this.grid);
            var table = (ParamKind.Table)block.Def.Kind;
            if (table.Rows > 1 && table.Cols > 1) surface = new TableSurface(this.grid);
            if (table.Rows * table.Cols > 0) graph = new TableGraph(this.grid);
            foreach (var chart in Charts()) {
                // Which cell a picture moved to is for a screen reader; the cells show it to the eye.
                chart.Announced += text => Announced?.Invoke(text);
                Controls.Add(chart);
            }
        } else {
            this.grid.ApplyTheme();
        }
        // The tab order is the order of the controls: the table comes after what was created before it.
        Controls.SetChildIndex(this.grid, Controls.Count - 1);
        var grid = this.grid;
        // What the last action did, for those who do not hear it announced. It keeps its line, so nothing moves when it speaks.
        var note = this.note = NewLabel("", null, DarkTheme.Text);
        var charts = Charts().ToArray();
        foreach (var chart in charts) Controls.SetChildIndex(chart, Controls.Count - 1);
        parts.Add((x, y, width) => {
            var (gap, top) = (Em(0.8f), y + Em(0.8f));
            // The table may be wider than the reading column, up to the width of the view.
            var room = ClientSize.Width - x - Em(1.25f);
            var size = grid.FitSize(room);
            grid.SetBounds(x, top, size.Width, size.Height);
            // The pictures stand beside the cells, where a change shows in them at once, when the view is wide enough.
            var column = Math.Max(size.Width, Em(12f));
            var beside = charts.Length > 0 && room - column - 2 * gap >= Em(ChartWidth);
            var noteHeight = Math.Max(Fit(note, x, grid.Bottom + Em(0.4f), beside ? column : width), Em(1f));
            var bottom = grid.Bottom + Em(0.4f) + noteHeight;
            if (beside) {
                // As tall together as the cells, each no lower than can be read.
                var each = Math.Max((bottom - top - (charts.Length - 1) * gap) / charts.Length, Em(ChartHeight));
                for (var i = 0; i < charts.Length; i++) {
                    charts[i].SetBounds(x + column + 2 * gap, top + i * (each + gap), Math.Min(room - column - 2 * gap, Em(2 * ChartWidth)), each);
                }
                bottom = Math.Max(bottom, charts[^1].Bottom);
            } else {
                // Under the cells: side by side where both have room, one over the other where not.
                var across = room >= charts.Length * Em(ChartWidth) + (charts.Length - 1) * gap;
                var each = across ? Math.Min((room - (charts.Length - 1) * gap) / charts.Length, Em(2 * ChartWidth)) : Math.Max(Math.Min(room, Em(2 * ChartWidth)), Em(10f));
                var under = bottom + gap;
                for (var i = 0; i < charts.Length; i++) {
                    charts[i].SetBounds(across ? x + i * (each + gap) : x, across ? under : under + i * (Em(1.25f * ChartHeight) + gap), each, Em(1.25f * ChartHeight));
                }
                bottom = charts.Length > 0 ? charts[^1].Bottom : bottom;
            }
            return bottom - y;
        });
        // Under the cells: nothing but the name of the table stands between the tuner and the table.
        AddText(Details.GridHelp, null, DarkTheme.TextDim, 0.2f);
        if (charts.Length > 0) AddText(Details.ChartHelp(block.Def), null, DarkTheme.TextDim, 0.4f);
    }

    /// <summary>The pictures of the table on the page, top to bottom.</summary>
    private IEnumerable<TableChart> Charts() => new TableChart?[] { surface, graph }.OfType<TableChart>();

    private void AddSetting(PageBlock.Setting block) {
        if (setting is null) {
            setting = new SettingBox(block.Document, block.Def);
            setting.Announced += text => {
                Say(text);
                Announced?.Invoke(text);
            };
            Controls.Add(setting);
        }
        setting.ApplyTheme();
        Controls.SetChildIndex(setting, Controls.Count - 1);
        var box = setting;
        var unit = block.Def.Unit.Length > 0 ? NewLabel(block.Def.Unit, null, DarkTheme.TextDim) : null;
        var note = this.note = NewLabel("", null, DarkTheme.Text);
        parts.Add((x, y, width) => {
            // Room for the largest value it can take, and for the two small buttons.
            var widest = TextRenderer.MeasureText(box.Maximum.ToString("F" + box.DecimalPlaces, System.Globalization.CultureInfo.InvariantCulture), Font).Width;
            box.SetBounds(x, y + Em(0.8f), Math.Min(widest + Em(3f), width), box.PreferredHeight);
            if (unit is not null) {
                var size = TextRenderer.MeasureText(unit.Text, Font);
                unit.SetBounds(box.Right + Em(0.5f), box.Top + (box.Height - size.Height) / 2, size.Width + 8, size.Height);
            }
            var noteHeight = Math.Max(Fit(note, x, box.Bottom + Em(0.4f), width), Em(1f));
            return Em(0.8f) + box.Height + Em(0.4f) + noteHeight;
        });
        AddText(Details.SettingHelp, null, DarkTheme.TextDim, 0.2f);
    }

    /// <summary>Scrolls the page so that the cell the keyboard is on is in view.</summary>
    private void RevealCell() {
        // A cell chosen in a picture is looked at there: the page stays where it is.
        if (Charts().Any(c => c.Focused)) return;
        if (arranging || this.grid is not { CurrentCellAddress: { X: >= 0, Y: >= 0 } at } grid) return;
        var cell = grid.GetCellDisplayRectangle(at.X, at.Y, cutOverflow: false);
        if (cell.IsEmpty) return;
        cell.Offset(grid.Location);
        var margin = Em(0.5f);
        var move = cell.Top - margin < 0 ? cell.Top - margin : cell.Bottom + margin > ClientSize.Height ? cell.Bottom + margin - ClientSize.Height : 0;
        if (move != 0) AutoScrollPosition = new Point(0, Math.Max(0, -AutoScrollPosition.Y + move));
    }

    private void AddActions(PageBlock.Actions actions) {
        if (actions.Heading is { } heading) AddHeading(heading);
        var buttons = new List<Button>();
        foreach (var action in actions.Items) {
            var button = NewButton(action.Label, action.Primary);
            if (action.Argument is { } argument) button.AccessibleDescription = argument;
            button.Click += (_, _) => ActionInvoked?.Invoke(action);
            buttons.Add(button);
        }
        // A list under a heading runs down the page; a few buttons side by side otherwise.
        var stacked = actions.Heading is not null;
        parts.Add((x, y, width) => {
            var gap = Em(0.5f);
            var (left, top, rowHeight) = (x, y + Em(0.8f), 0);
            foreach (var button in buttons) {
                if (left > x && (stacked || left + button.Width > x + width)) (left, top, rowHeight) = (x, top + rowHeight + gap, 0);
                button.Location = new Point(left, top);
                left += button.Width + gap;
                rowHeight = Math.Max(rowHeight, button.Height);
            }
            return top + rowHeight - y;
        });
    }

    /// <summary>A notice: a coloured edge, a symbol, a heading and a sentence, on a card.</summary>
    private sealed class NoticeBox : Panel {
        private readonly Label? glyph;
        private readonly Label heading;
        private readonly Label body;
        private readonly Color edge;

        public NoticeBox(PageBlock.Notice notice, Font headingFont, Font? glyphFont) {
            edge = DarkTheme.SeverityColor(notice.Severity);
            BackColor = DarkTheme.Card;
            AccessibleRole = notice.Severity is Severity.Warning or Severity.Problem ? AccessibleRole.Alert : AccessibleRole.Grouping;
            AccessibleName = notice.Heading;
            AccessibleDescription = notice.Body;
            if (glyphFont is not null) {
                // The symbol repeats what the heading says; a screen reader has no use for it.
                glyph = new Label { Text = Glyphs.For(notice.Severity), Font = glyphFont, ForeColor = edge, UseMnemonic = false, AccessibleRole = AccessibleRole.None };
                Controls.Add(glyph);
            }
            heading = new Label { Text = notice.Heading, Font = headingFont, ForeColor = DarkTheme.Text, UseMnemonic = false };
            body = new Label { Text = notice.Body, ForeColor = DarkTheme.Text, UseMnemonic = false };
            Controls.Add(heading);
            Controls.Add(body);
        }

        /// <summary>Places the symbol and the two texts for the given width. Returns the height the notice needs.</summary>
        public int Arrange(int width) {
            var em = Font.Height;
            var (pad, edge) = (em * 3 / 4, Math.Max(em / 5, 3));
            var textLeft = edge + pad + (glyph is null ? 0 : em * 2);
            glyph?.SetBounds(edge + pad - em / 4, pad - em / 8, em * 2, em * 3 / 2);
            var y = pad + Fit(heading, textLeft, pad, width - textLeft - pad);
            y += em / 6 + Fit(body, textLeft, y + em / 6, width - textLeft - pad);
            return y + pad;
        }

        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            using var brush = new SolidBrush(edge);
            e.Graphics.FillRectangle(brush, 0, 0, Math.Max(Font.Height / 5, 3), Height);
            if (!DarkTheme.HighContrast) return;
            using var pen = new Pen(DarkTheme.Border);
            e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        }
    }
}
