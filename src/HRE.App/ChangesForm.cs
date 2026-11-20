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
public sealed class ChangesForm : Form {
    private readonly Font _headingFont;

    public ChangesForm(string fileName, Font textFont) {
        SuspendLayout();
        AutoScaleDimensions = new SizeF(96f, 96f);
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = textFont;
        Text = "Changes not saved";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = MinimizeBox = ShowInTaskbar = false;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(20);
        DarkTheme.ApplyToWindow(this);
        _headingFont = new Font(Font.FontFamily, Font.SizeInPoints * 1.3f, FontStyle.Bold);

        // Everything sizes itself to its text, so larger text and other scalings cannot clip it.
        var wrap = new Size(Font.Height * 24, 0);
        var heading = new Label { Text = $"Save the changes to {fileName}?", Font = _headingFont, AutoSize = true, MaximumSize = wrap, UseMnemonic = false, Margin = new Padding(0, 0, 3, 0) };
        var body = new Label { Text = "If you do not save them, they are lost.", AutoSize = true, MaximumSize = wrap, Margin = new Padding(3, 10, 3, 3) };
        AccessibleDescription = $"{heading.Text} {body.Text}";

        var save = Choice("&Save", ChangesAnswer.Save, "Saves the changes, then goes on");
        var discard = Choice("&Don't save", ChangesAnswer.Discard, "Goes on without saving: the changes are lost");
        var cancel = Choice("Cancel", ChangesAnswer.Cancel, "Goes back to the ROM, with its changes");
        if (!DarkTheme.HighContrast) {
            // The main button in the accent of the program, not of Windows; dark text on it reads best.
            (save.BackColor, save.ForeColor) = (DarkTheme.Accent, Color.Black);
            (discard.BackColor, discard.ForeColor) = (cancel.BackColor, cancel.ForeColor) = (DarkTheme.Card, DarkTheme.Text);
        }
        var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Margin = new Padding(0, Font.Height, 0, 0) };
        buttons.Controls.AddRange([save, discard, cancel]);

        var column = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Dock = DockStyle.Fill };
        column.Controls.AddRange([heading, body, buttons]);
        Controls.Add(column);
        AcceptButton = save;
        CancelButton = cancel;
        ResumeLayout();
    }

    /// <summary>What was chosen. Closing the dialog any other way goes back to the ROM.</summary>
    public ChangesAnswer Answer { get; private set; } = ChangesAnswer.Cancel;

    protected override void Dispose(bool disposing) {
        if (disposing) _headingFont.Dispose();
        base.Dispose(disposing);
    }

    private ThemedButton Choice(string text, ChangesAnswer answer, string description) {
        var button = new ThemedButton { Text = text, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, AccessibleDescription = description, Padding = new Padding(Font.Height / 2, Font.Height / 8, Font.Height / 2, Font.Height / 8) };
        button.Click += (_, _) => {
            Answer = answer;
            DialogResult = DialogResult.OK;
        };
        return button;
    }
}
