using System.ComponentModel;

namespace HRE.App;

/// <summary>Shows a <see cref="DetailPage"/>: title, sections, and rows of label and value. Scrolls when the page is taller than the view.</summary>
public sealed class DetailView : Panel {
    private const TextFormatFlags Wrapped = TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding;
    private const TextFormatFlags SingleLine = TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding;

    private readonly Font _titleFont = new("Segoe UI Semibold", 16f);
    private readonly Font _headingFont = new("Segoe UI Semibold", 9f);
    private DetailPage? _page;

    public DetailView() {
        DoubleBuffered = true;
        ResizeRedraw = true;
        AutoScroll = true;
        BackColor = DarkTheme.Background;
        ForeColor = DarkTheme.Text;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public DetailPage? Page {
        get => _page;
        set {
            _page = value;
            AutoScrollPosition = Point.Empty;
            Relayout();
        }
    }

    protected override void OnClientSizeChanged(EventArgs e) {
        base.OnClientSizeChanged(e);
        Relayout();
    }

    protected override void OnPaint(PaintEventArgs e) {
        base.OnPaint(e);
        if (_page is null) return;
        LayoutPage(e.Graphics, AutoScrollPosition.Y, draw: true);
    }

    protected override void Dispose(bool disposing) {
        if (disposing) {
            _titleFont.Dispose();
            _headingFont.Dispose();
        }
        base.Dispose(disposing);
    }

    private int Scaled(int logical) => logical * DeviceDpi / 96;

    private void Relayout() {
        if (!IsHandleCreated || _page is null) {
            AutoScrollMinSize = Size.Empty;
        } else {
            using var g = CreateGraphics();
            AutoScrollMinSize = new Size(0, LayoutPage(g, 0, draw: false));
        }
        Invalidate();
    }

    protected override void OnHandleCreated(EventArgs e) {
        base.OnHandleCreated(e);
        Relayout();
    }

    /// <summary>Lays the page out from <paramref name="top"/> down, drawing it when asked. Returns the height of the page.</summary>
    private int LayoutPage(Graphics g, int top, bool draw) {
        var page = _page!;
        var margin = Scaled(24);
        var width = Math.Max(ClientSize.Width - 2 * margin, Scaled(120));
        var labelWidth = Math.Min(Scaled(170), width / 3);
        var gap = Scaled(16);
        var y = top + margin;

        y += DrawText(g, page.Title, _titleFont, DarkTheme.Text, margin, y, width, draw, SingleLine);
        if (page.Subtitle.Length > 0) y += Scaled(2) + DrawText(g, page.Subtitle, Font, DarkTheme.TextDim, margin, y + Scaled(2), width, draw, Wrapped);
        y += Scaled(14);

        foreach (var section in page.Sections) {
            if (draw) {
                using var pen = new Pen(DarkTheme.Border);
                g.DrawLine(pen, margin, y, margin + width, y);
            }
            y += Scaled(12);
            if (section.Heading is { } heading) {
                y += DrawText(g, heading.ToUpperInvariant(), _headingFont, DarkTheme.Accent, margin, y, width, draw, SingleLine) + Scaled(8);
            }
            foreach (var row in section.Rows) {
                int height;
                if (row.Label.Length == 0) {
                    height = DrawText(g, row.Value, Font, DarkTheme.Text, margin, y, width, draw, Wrapped);
                } else {
                    var labelHeight = DrawText(g, row.Label, Font, DarkTheme.TextDim, margin, y, labelWidth, draw, Wrapped);
                    var valueHeight = DrawText(g, row.Value, Font, DarkTheme.Text, margin + labelWidth + gap, y, width - labelWidth - gap, draw, Wrapped);
                    height = Math.Max(labelHeight, valueHeight);
                }
                y += height + Scaled(6);
            }
            y += Scaled(10);
        }
        return y - top + margin;
    }

    /// <summary>Measures a piece of text, drawing it when asked. Returns its height.</summary>
    private static int DrawText(Graphics g, string text, Font font, Color color, int x, int y, int width, bool draw, TextFormatFlags flags) {
        var size = TextRenderer.MeasureText(g, text, font, new Size(width, int.MaxValue), flags);
        if (draw) TextRenderer.DrawText(g, text, font, new Rectangle(x, y, width, size.Height), color, flags | TextFormatFlags.Left | TextFormatFlags.Top);
        return size.Height;
    }
}
