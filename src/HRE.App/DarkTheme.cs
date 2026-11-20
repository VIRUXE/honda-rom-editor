using System.Runtime.InteropServices;

namespace HRE.App;

/// <summary>The one theme of the application: true black, light text, a teal accent.</summary>
public static partial class DarkTheme {
    public static Color Background { get; } = Color.Black;

    /// <summary>Menus, the status bar and other surfaces that sit on the background.</summary>
    public static Color Surface { get; } = Color.FromArgb(0x0C, 0x0C, 0x0C);

    /// <summary>What the pointer is over.</summary>
    public static Color Hover { get; } = Color.FromArgb(0x1E, 0x1E, 0x1E);

    public static Color Border { get; } = Color.FromArgb(0x2A, 0x2A, 0x2A);
    public static Color Text { get; } = Color.FromArgb(0xE6, 0xE6, 0xE6);
    public static Color TextDim { get; } = Color.FromArgb(0x9A, 0xA0, 0xA6);
    public static Color TextDisabled { get; } = Color.FromArgb(0x6E, 0x6E, 0x6E);
    public static Color Accent { get; } = Color.FromArgb(0x00, 0x96, 0xAA);

    /// <summary>The selected item: the accent, darkened so light text stays readable on it.</summary>
    public static Color Selection { get; } = Color.FromArgb(0x00, 0x4B, 0x55);

    public static Color Good { get; } = Color.FromArgb(0x3C, 0xBE, 0x50);
    public static Color Warning { get; } = Color.FromArgb(0xF0, 0xC8, 0x28);
    public static Color Bad { get; } = Color.FromArgb(0xEB, 0x3C, 0x2D);

    public static ToolStripRenderer CreateRenderer() => new Renderer();

    /// <summary>Black title bar and window border, where Windows lets an application choose them (Windows 11).</summary>
    public static void ApplyToWindow(Form form) {
        SetWindowColor(form.Handle, DwmCaptionColor, Background);
        SetWindowColor(form.Handle, DwmBorderColor, Border);
        SetWindowColor(form.Handle, DwmTextColor, Text);
    }

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

    private const int DwmBorderColor = 34;
    private const int DwmCaptionColor = 35;
    private const int DwmTextColor = 36;

    private static void SetWindowColor(IntPtr window, int attribute, Color color) {
        var colorRef = color.R | color.G << 8 | color.B << 16;
        // Older Windows versions do not know these attributes and say so; the window keeps its dark default.
        _ = DwmSetWindowAttribute(window, attribute, ref colorRef, sizeof(int));
    }

    [LibraryImport("dwmapi.dll")]
    private static partial int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int size);

    private sealed class Renderer() : ToolStripProfessionalRenderer(new Colors()) {
        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e) {
            e.TextColor = e.Item.Enabled ? Text : TextDisabled;
            base.OnRenderItemText(e);
        }

        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e) {
            e.ArrowColor = e.Item is { Enabled: true } ? Text : TextDisabled;
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
        public override Color MenuBorder => Border;
        public override Color MenuItemBorder => Hover;
        public override Color MenuItemSelected => Hover;
        public override Color MenuItemSelectedGradientBegin => Hover;
        public override Color MenuItemSelectedGradientEnd => Hover;
        public override Color MenuItemPressedGradientBegin => Hover;
        public override Color MenuItemPressedGradientMiddle => Hover;
        public override Color MenuItemPressedGradientEnd => Hover;
        public override Color SeparatorDark => Border;
        public override Color SeparatorLight => Border;
    }
}
