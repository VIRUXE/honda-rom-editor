namespace HRE.App;

/// <summary>
/// The one look of the application: true black, light text, a teal accent. When the user has
/// turned on a Windows contrast theme, every colour comes from that theme instead: the system's
/// accessibility setting wins over the application's look.
/// </summary>
public static class DarkTheme {
    private static readonly Lazy<Icon> LazyIcon = new(() => {
        using var stream = typeof(DarkTheme).Assembly.GetManifestResourceStream("HRE.App.app.ico")!;
        return new Icon(stream);
    });

    private static readonly Color[] HeatStops = [
        Color.FromArgb(0x6E, 0xAF, 0xFF),
        Color.FromArgb(0x5F, 0xCD, 0x82),
        Color.FromArgb(0xF0, 0xD7, 0x50),
        Color.FromArgb(0xFF, 0x7D, 0x69),
    ];

    /// <summary>Whether a Windows contrast theme is on.</summary>
    public static bool HighContrast => SystemInformation.HighContrast;

    public static Color Background => HighContrast ? SystemColors.Window : Color.Black;

    /// <summary>The menu bar and the status bar.</summary>
    public static Color Surface => HighContrast ? SystemColors.Control : Color.FromArgb(0x14, 0x14, 0x14);

    /// <summary>Notices and other blocks that stand out from the page.</summary>
    public static Color Card => HighContrast ? SystemColors.Window : Color.FromArgb(0x14, 0x14, 0x14);

    /// <summary>Lines that only divide.</summary>
    public static Color Border => HighContrast ? SystemColors.WindowText : Color.FromArgb(0x2A, 0x2A, 0x2A);

    /// <summary>Lines that show where a control is or can be grabbed.</summary>
    public static Color ControlBorder => HighContrast ? SystemColors.WindowText : Color.FromArgb(0x6E, 0x6E, 0x6E);

    public static Color Text => HighContrast ? SystemColors.WindowText : Color.FromArgb(0xE6, 0xE6, 0xE6);
    public static Color TextDim => HighContrast ? SystemColors.WindowText : Color.FromArgb(0x9A, 0xA0, 0xA6);
    public static Color Accent => HighContrast ? SystemColors.HotTrack : Color.FromArgb(0x00, 0x96, 0xAA);

    /// <summary>The fill of the selected item, and of the menu item the pointer or the keyboard is on.</summary>
    public static Color Selection => HighContrast ? SystemColors.Highlight : Color.FromArgb(0x00, 0x4B, 0x55);

    public static Color SelectionText => HighContrast ? SystemColors.HighlightText : Color.FromArgb(0xFF, 0xFF, 0xFF);

    public static Color Good => HighContrast ? SystemColors.WindowText : Color.FromArgb(0x3C, 0xBE, 0x50);
    public static Color Warning => HighContrast ? SystemColors.WindowText : Color.FromArgb(0xF0, 0xC8, 0x28);
    public static Color Bad => HighContrast ? SystemColors.WindowText : Color.FromArgb(0xEB, 0x3C, 0x2D);

    /// <summary>The text on a cell of a table, whatever its heat colour.</summary>
    public static Color HeatText => HighContrast ? SystemColors.WindowText : Color.Black;

    public static Icon AppIcon => LazyIcon.Value;

    /// <summary>
    /// The colour of a table cell from the smallest value of its table (0) to the largest (1):
    /// blue, green, yellow, red, all light enough for black text. Under a contrast theme cells
    /// have the colour of the window: the number says it all.
    /// </summary>
    public static Color Heat(double t) {
        if (HighContrast) return SystemColors.Window;
        var at = Math.Clamp(double.IsNaN(t) ? 0.5 : t, 0, 1) * (HeatStops.Length - 1);
        var i = Math.Min((int)at, HeatStops.Length - 2);
        var (a, b, f) = (HeatStops[i], HeatStops[i + 1], at - i);
        int Mix(int x, int y) => (int)Math.Round(x + (y - x) * f);
        return Color.FromArgb(Mix(a.R, b.R), Mix(a.G, b.G), Mix(a.B, b.B));
    }

    public static Color SeverityColor(Severity severity) => severity switch {
        Severity.Info => Accent,
        Severity.Good => Good,
        Severity.Warning => Warning,
        Severity.Problem => Bad,
        _ => throw new ArgumentOutOfRangeException(nameof(severity)),
    };

    /// <summary>The renderer of menus and the status bar. Under a contrast theme WinForms' own renderer knows best.</summary>
    public static ToolStripRenderer CreateRenderer() => HighContrast ? new ToolStripProfessionalRenderer() : new Renderer();

    /// <summary>Icon, colours and title bar of a window. Call again when the contrast theme changes.</summary>
    public static void ApplyToWindow(Form form) {
        form.Icon = AppIcon;
        form.BackColor = Background;
        form.ForeColor = Text;
        // Windows 11 lets a window choose these; under a contrast theme they are the system's.
        form.FormCaptionBackColor = HighContrast ? Color.Empty : Background;
        form.FormCaptionTextColor = HighContrast ? Color.Empty : Text;
        form.FormBorderColor = HighContrast ? Color.Empty : Border;
    }

    /// <summary>The body font of pages and lists: larger than the system font of menus, and enlarged by the user's text size.</summary>
    public static Font BodyFont(Font system, double zoom) => new(system.FontFamily, (float)(system.SizeInPoints * 10.5 / 9.0 * zoom));

    /// <summary>The font of menus and the status bar at the user's text size.</summary>
    public static Font BarFont(Font system, double zoom) => new(system.FontFamily, (float)(system.SizeInPoints * zoom));

    /// <summary>How readable one colour is on another: the WCAG contrast ratio, from 1 (none) to 21.</summary>
    public static double Contrast(Color a, Color b) {
        var (la, lb) = (Luminance(a), Luminance(b));
        return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
    }

    private static double Luminance(Color c) {
        static double Linear(byte channel) {
            var v = channel / 255.0;
            return v <= 0.03928 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);
        }
        return 0.2126 * Linear(c.R) + 0.7152 * Linear(c.G) + 0.0722 * Linear(c.B);
    }

    private sealed class Renderer() : ToolStripProfessionalRenderer(new Colors()) {
        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e) {
            // Status fields carry their own colour; disabled items are left to WinForms.
            if (e.Item.Enabled && e.Item is not ToolStripStatusLabel) e.TextColor = e.Item.Selected || e.Item.Pressed ? SelectionText : Text;
            base.OnRenderItemText(e);
        }

        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e) {
            if (e.Item is { Enabled: true }) e.ArrowColor = Text;
            base.OnRenderArrow(e);
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e) {
            // Menus keep their outline; the menu bar and the status bar get one hairline towards the content.
            if (e.ToolStrip is ToolStripDropDown) {
                base.OnRenderToolStripBorder(e);
                return;
            }
            using var pen = new Pen(Border);
            var y = e.ToolStrip is StatusStrip ? 0 : e.ToolStrip.Height - 1;
            e.Graphics.DrawLine(pen, 0, y, e.ToolStrip.Width, y);
        }
    }

    private sealed class Colors : ProfessionalColorTable {
        public Colors() => UseSystemColors = false;

        public override Color MenuStripGradientBegin => Surface;
        public override Color MenuStripGradientEnd => Surface;
        public override Color StatusStripGradientBegin => Surface;
        public override Color StatusStripGradientEnd => Surface;
        public override Color ToolStripDropDownBackground => Surface;
        public override Color ImageMarginGradientBegin => Surface;
        public override Color ImageMarginGradientMiddle => Surface;
        public override Color ImageMarginGradientEnd => Surface;
        public override Color CheckBackground => Selection;
        public override Color CheckSelectedBackground => Selection;
        public override Color CheckPressedBackground => Selection;
        public override Color MenuBorder => ControlBorder;
        public override Color MenuItemBorder => Accent;
        public override Color MenuItemSelected => Selection;
        public override Color MenuItemSelectedGradientBegin => Selection;
        public override Color MenuItemSelectedGradientEnd => Selection;
        public override Color MenuItemPressedGradientBegin => Selection;
        public override Color MenuItemPressedGradientMiddle => Selection;
        public override Color MenuItemPressedGradientEnd => Selection;
        public override Color SeparatorDark => Border;
        public override Color SeparatorLight => Border;
    }
}
