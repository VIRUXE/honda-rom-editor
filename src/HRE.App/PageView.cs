using System.ComponentModel;

namespace HRE.App;

/// <summary>
/// Shows a <see cref="DetailPage"/> as real controls, so that a screen reader can read it, the
/// keyboard can walk it and its values can be copied: labels for text, read-only fields for
/// values, buttons for actions. It lays them out in one reading column and scrolls when the page
/// is taller than the view. All sizes come from the font, so the page follows the display
/// scaling and the user's text size.
/// </summary>
public sealed class PageView : Panel {
    private const TextFormatFlags Wrapped = TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl | TextFormatFlags.NoPrefix;

    /// <summary>Each part places its controls at (x, y) in the given width and returns the height it took.</summary>
    private readonly List<Func<int, int, int, int>> _parts = [];

    private Font? _titleFont;
    private Font? _headingFont;
    private Font? _glyphFont;
    private DetailPage? _page;
    private TableGrid? _grid;
    private Label? _gridNote;
    private bool _arranging;

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

    /// <summary>The table on the page has something to tell: what an action did, or why it did nothing.</summary>
    public event Action<string>? Announced;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public DetailPage? Page {
        get => _page;
        set {
            _page = value;
            AutoScrollPosition = Point.Empty;
            Rebuild();
        }
    }

    /// <summary>The cells of the table on the page; null when the page shows no table.</summary>
    public TableGrid? Grid => _grid;

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
        _page = page;
        Rebuild();
        AutoScrollPosition = new Point(0, scrolled);
    }

    /// <summary>Creates the controls of the page anew: after the page, the colours or the font changed.</summary>
    public void Rebuild() {
        SuspendLayout();
        // The table of the page stays when the new page shows the same one: it has the selection and may have the focus.
        var kept = Page?.Blocks.OfType<PageBlock.Grid>().FirstOrDefault() is { } shown && _grid is { } grid && ReferenceEquals(grid.Document, shown.Document) && grid.Def == shown.Def ? grid : null;
        foreach (var old in Controls.Cast<Control>().ToArray()) {
            if (old != kept) old.Dispose();
        }
        _grid = kept;
        _gridNote = null;
        _parts.Clear();
        _titleFont?.Dispose();
        _headingFont?.Dispose();
        _glyphFont?.Dispose();
        _titleFont = new Font(Font.FontFamily, Font.SizeInPoints * 1.9f, FontStyle.Bold);
        _headingFont = new Font(Font, FontStyle.Bold);
        _glyphFont = Glyphs.FontLike(Font, 1.25f);
        BackColor = DarkTheme.Background;
        ForeColor = DarkTheme.Text;

        if (Page is { } page) {
            // Large bold type sits a little to the right of its box; pull it back in line with the text under it.
            var title = NewLabel(page.Title, _titleFont, DarkTheme.Text);
            _parts.Add((x, y, width) => Fit(title, x - Em(0.2f), y, width));
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
                    case PageBlock.Grid table:
                        AddGrid(table);
                        break;
                }
            }
            if (page.Blocks.Any(b => b is PageBlock.Section { Technical: true })) {
                var toggle = NewButton(ShowTechnical ? "Hide technical details" : "Show technical details", primary: false);
                toggle.Click += (_, _) => TechnicalToggled?.Invoke();
                _parts.Add((x, y, _) => {
                    toggle.Location = new Point(x, y + Em(1f));
                    return toggle.Height + Em(1f);
                });
            }
        }
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
        if (step == 0 || _grid?.ContainsFocus == true) return base.ProcessCmdKey(ref msg, keyData);
        AutoScrollPosition = new Point(0, Math.Max(0, -AutoScrollPosition.Y + step));
        return true;
    }

    /// <summary>
    /// Where to scroll when a control takes the focus. The table is taller than most views, and
    /// bringing its top into view would move it away under the pointer: the page follows the
    /// cell instead (<see cref="RevealCell"/>).
    /// </summary>
    protected override Point ScrollToControl(Control activeControl) =>
        activeControl == _grid ? DisplayRectangle.Location : base.ScrollToControl(activeControl);

    protected override void Dispose(bool disposing) {
        if (disposing) {
            _titleFont?.Dispose();
            _headingFont?.Dispose();
            _glyphFont?.Dispose();
        }
        base.Dispose(disposing);
    }

    /// <summary>A length in units of the line height, which follows scaling and text size.</summary>
    private int Em(float lines) => (int)Math.Round(Font.Height * lines);

    /// <summary>Places every part, top to bottom, in a column no wider than is comfortable to read.</summary>
    private void Arrange() {
        if (_arranging || _parts.Count == 0) return;
        _arranging = true;
        SuspendLayout();
        var margin = Em(1.25f);
        var width = Math.Clamp(ClientSize.Width - 2 * margin, Em(10f), Em(42f));
        var y = margin + AutoScrollPosition.Y;
        foreach (var part in _parts) y += part(margin, y, width);
        AutoScrollMargin = new Size(0, margin);
        ResumeLayout();
        _arranging = false;
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

    private Button NewButton(string text, bool primary) {
        var button = new Button {
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
        _parts.Add((x, y, width) => Em(gapBefore) + Fit(label, x, y + Em(gapBefore), width));
    }

    private void AddHeading(string text) => AddText(text, _headingFont, DarkTheme.Text, 1.1f);

    private void AddNotice(PageBlock.Notice notice) {
        var box = new NoticeBox(notice, _headingFont!, _glyphFont);
        Controls.Add(box);
        _parts.Add((x, y, width) => {
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
        _parts.Add((x, y, width) => {
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
        AddHeading(block.Heading);
        // Above the cells: a table is taller than most windows, and what can be done with it is not to be scrolled for.
        AddText(Details.GridHelp, null, DarkTheme.TextDim, 0.2f);
        if (_grid is null) {
            _grid = new TableGrid(block.Document, block.Def);
            _grid.Announced += text => {
                _gridNote?.Text = text;
                Arrange();
                Announced?.Invoke(text);
            };
            _grid.ContentSizeChanged += () => {
                Arrange();
                RevealCell();
            };
            _grid.CurrentCellChanged += (_, _) => RevealCell();
            _grid.Enter += (_, _) => RevealCell();
            Controls.Add(_grid);
        } else {
            _grid.ApplyTheme();
        }
        // The tab order is the order of the controls: the table comes after what was created before it.
        Controls.SetChildIndex(_grid, Controls.Count - 1);
        var grid = _grid;
        // What the last action did, for those who do not hear it announced. It keeps its line, so nothing moves when it speaks.
        var note = _gridNote = NewLabel("", null, DarkTheme.Text);
        _parts.Add((x, y, width) => {
            // The table may be wider than the reading column, up to the width of the view.
            var size = grid.FitSize(ClientSize.Width - x - Em(1.25f));
            grid.SetBounds(x, y + Em(0.5f), size.Width, size.Height);
            var noteHeight = Math.Max(Fit(note, x, y + Em(0.5f) + size.Height + Em(0.4f), width), Em(1f));
            return Em(0.5f) + size.Height + Em(0.4f) + noteHeight;
        });
    }

    /// <summary>Scrolls the page so that the cell the keyboard is on is in view.</summary>
    private void RevealCell() {
        if (_arranging || _grid is not { CurrentCellAddress: { X: >= 0, Y: >= 0 } at } grid) return;
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
        _parts.Add((x, y, width) => {
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
        private readonly Label? _glyph;
        private readonly Label _heading;
        private readonly Label _body;
        private readonly Color _edge;

        public NoticeBox(PageBlock.Notice notice, Font headingFont, Font? glyphFont) {
            _edge = DarkTheme.SeverityColor(notice.Severity);
            BackColor = DarkTheme.Card;
            AccessibleRole = notice.Severity is Severity.Warning or Severity.Problem ? AccessibleRole.Alert : AccessibleRole.Grouping;
            AccessibleName = notice.Heading;
            AccessibleDescription = notice.Body;
            if (glyphFont is not null) {
                // The symbol repeats what the heading says; a screen reader has no use for it.
                _glyph = new Label { Text = Glyphs.For(notice.Severity), Font = glyphFont, ForeColor = _edge, UseMnemonic = false, AccessibleRole = AccessibleRole.None };
                Controls.Add(_glyph);
            }
            _heading = new Label { Text = notice.Heading, Font = headingFont, ForeColor = DarkTheme.Text, UseMnemonic = false };
            _body = new Label { Text = notice.Body, ForeColor = DarkTheme.Text, UseMnemonic = false };
            Controls.Add(_heading);
            Controls.Add(_body);
        }

        /// <summary>Places the symbol and the two texts for the given width. Returns the height the notice needs.</summary>
        public int Arrange(int width) {
            var em = Font.Height;
            var (pad, edge) = (em * 3 / 4, Math.Max(em / 5, 3));
            var textLeft = edge + pad + (_glyph is null ? 0 : em * 2);
            _glyph?.SetBounds(edge + pad - em / 4, pad - em / 8, em * 2, em * 3 / 2);
            var y = pad + Fit(_heading, textLeft, pad, width - textLeft - pad);
            y += em / 6 + Fit(_body, textLeft, y + em / 6, width - textLeft - pad);
            return y + pad;
        }

        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            using var brush = new SolidBrush(_edge);
            e.Graphics.FillRectangle(brush, 0, 0, Math.Max(Font.Height / 5, 3), Height);
            if (!DarkTheme.HighContrast) return;
            using var pen = new Pen(DarkTheme.Border);
            e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        }
    }
}
