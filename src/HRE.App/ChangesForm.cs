namespace HRE.App;

/// <summary>What to do with changes that are not saved, before the ROM they are in is left.</summary>
public enum ChangesAnswer {
    Save,
    Discard,

    /// <summary>Stay: the ROM is not left after all.</summary>
    Cancel,
}

/// <summary>
/// The one question the program asks: whether to save the changes before a ROM is closed.
/// Losing them cannot be undone, so Save is the button Enter presses, and Esc goes back.
/// </summary>
/// <remarks>
/// The controls are laid out in <c>ChangesForm.Designer.cs</c>, which the Visual Studio designer
/// edits; the file name, the fonts, the sizes that follow them and the colours are applied here.
/// </remarks>
public sealed partial class ChangesForm : Form {
    private readonly Font headingFont;

    public ChangesForm(string fileName, Font textFont) {
        InitializeComponent();
        SuspendLayout();
        Font = textFont;
        DarkTheme.ApplyToWindow(this);
        headingFont = new Font(Font.FontFamily, Font.SizeInPoints * 1.3f, FontStyle.Bold);

        // Everything sizes itself to its text, so larger text and other scalings cannot clip it.
        var wrap = new Size(Font.Height * 24, 0);
        heading.Text = $"Save the changes to {fileName}?";
        heading.Font = headingFont;
        heading.MaximumSize = body.MaximumSize = wrap;
        AccessibleDescription = $"{heading.Text} {body.Text}";

        var padding = new Padding(Font.Height / 2, Font.Height / 8, Font.Height / 2, Font.Height / 8);
        save.Padding = discard.Padding = cancel.Padding = padding;
        if (!DarkTheme.HighContrast) {
            // The main button in the accent of the program, not of Windows; dark text on it reads best.
            (save.BackColor, save.ForeColor) = (DarkTheme.Accent, Color.Black);
            (discard.BackColor, discard.ForeColor) = (cancel.BackColor, cancel.ForeColor) = (DarkTheme.Card, DarkTheme.Text);
        }
        buttons.Margin = new Padding(0, Font.Height, 0, 0);
        ResumeLayout();
    }

    /// <summary>What was chosen. Closing the dialog any other way goes back to the ROM.</summary>
    public ChangesAnswer Answer { get; private set; } = ChangesAnswer.Cancel;

    protected override void Dispose(bool disposing) {
        if (disposing) {
            components?.Dispose();
            headingFont.Dispose();
        }
        base.Dispose(disposing);
    }

    private void Save_Click(object? sender, EventArgs e) => Choose(ChangesAnswer.Save);

    private void Discard_Click(object? sender, EventArgs e) => Choose(ChangesAnswer.Discard);

    private void Cancel_Click(object? sender, EventArgs e) => Choose(ChangesAnswer.Cancel);

    private void Choose(ChangesAnswer answer) {
        Answer = answer;
        DialogResult = DialogResult.OK;
    }
}
