using System.Drawing.Text;

namespace HRE.App;

/// <summary>
/// Symbols from the icon font Windows ships (Segoe Fluent Icons, or Segoe MDL2 Assets on
/// Windows 10: the same code points). Drawn as text, they follow the colour and the size of what
/// is around them. A symbol never stands alone: the words beside it say the same.
/// </summary>
public static class Glyphs {
    public const string Info = "";
    public const string Good = "";
    public const string Warning = "";
    public const string Problem = "";

    private static readonly Lazy<string?> LazyFamily = new(() => {
        using var installed = new InstalledFontCollection();
        var names = installed.Families.Select(f => f.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return new[] { "Segoe Fluent Icons", "Segoe MDL2 Assets" }.FirstOrDefault(names.Contains);
    });

    /// <summary>The icon font this PC has; null when it has neither, and then no symbol is drawn.</summary>
    public static string? Family => LazyFamily.Value;

    public static string For(Severity severity) => severity switch {
        Severity.Info => Info,
        Severity.Good => Good,
        Severity.Warning => Warning,
        Severity.Problem => Problem,
        _ => throw new ArgumentOutOfRangeException(nameof(severity)),
    };

    /// <summary>The icon font at the height of a text font; null when this PC has no icon font.</summary>
    public static Font? FontLike(Font text, float scale = 1f) => Family is { } family ? new Font(family, text.SizeInPoints * scale) : null;

    /// <summary>A symbol as a picture, for the places that take pictures and not text (status fields).</summary>
    public static Bitmap? Render(string glyph, Color color, int pixels) {
        if (Family is not { } family) return null;
        var bitmap = new Bitmap(pixels, pixels);
        using var g = Graphics.FromImage(bitmap);
        using var font = new Font(family, pixels * 0.72f, GraphicsUnit.Pixel);
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        TextRenderer.DrawText(g, glyph, font, new Rectangle(0, 0, pixels, pixels), color,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
        return bitmap;
    }
}
