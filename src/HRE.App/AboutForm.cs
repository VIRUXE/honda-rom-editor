using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace HRE.App;

/// <summary>The About dialog: name, version, what the program is today and where to report a problem.</summary>
public sealed class AboutForm : Form {
    private readonly Font _nameFont;

    public AboutForm(string appName, string reportUrl) {
        var version = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0] ?? "";
        var details = $"{appName} {version}, {RuntimeInformation.FrameworkDescription}, {RuntimeInformation.OSDescription}";

        SuspendLayout();
        AutoScaleDimensions = new SizeF(96f, 96f);
        AutoScaleMode = AutoScaleMode.Dpi;
        Text = $"About {appName}";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = MinimizeBox = ShowInTaskbar = false;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(20);
        DarkTheme.ApplyToWindow(this);
        _nameFont = new Font(Font.FontFamily, Font.SizeInPoints * 1.8f, FontStyle.Bold);

        // Everything sizes itself to its text, so larger text and other scalings cannot clip it.
        var wrap = new Size(LogicalToDeviceUnits(380), 0);
        var link = new LinkLabel { Text = "Report a problem or ask a question", AutoSize = true, Margin = new Padding(3, 10, 3, 3), AccessibleDescription = reportUrl };
        if (!DarkTheme.HighContrast) (link.LinkColor, link.ActiveLinkColor, link.VisitedLinkColor) = (DarkTheme.Accent, DarkTheme.Text, DarkTheme.Accent);
        link.LinkClicked += (_, _) => Process.Start(new ProcessStartInfo(reportUrl) { UseShellExecute = true })?.Dispose();

        var copy = new ThemedButton { Text = "&Copy version details", AutoSize = true, AccessibleDescription = "Copies the versions of the program, .NET and Windows, to paste into a problem report" };
        copy.Click += (_, _) => Clipboard.SetText(details);
        var close = new ThemedButton { Text = "Close", AutoSize = true, DialogResult = DialogResult.OK };
        if (!DarkTheme.HighContrast) {
            // The main button in the accent of the program, not of Windows; dark text on it reads best.
            (close.BackColor, close.ForeColor) = (DarkTheme.Accent, Color.Black);
            (copy.BackColor, copy.ForeColor) = (DarkTheme.Card, DarkTheme.Text);
        }
        var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, AutoSize = true, Dock = DockStyle.Fill, Margin = new Padding(0, 16, 0, 0) };
        buttons.Controls.AddRange([close, copy]);

        var column = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Dock = DockStyle.Fill };
        column.Controls.AddRange([
            new Label { Text = appName, Font = _nameFont, AutoSize = true, UseMnemonic = false, Margin = new Padding(0, 0, 3, 0) },
            new Label { Text = $"Version {version}", ForeColor = DarkTheme.TextDim, AutoSize = true },
            new Label { Text = "For the ROMs of Honda OBD0 and OBD1 engine computers.", AutoSize = true, MaximumSize = wrap, Margin = new Padding(3, 12, 3, 3) },
            new Label { Text = $"Early version. {Details.EarlyVersion}", AutoSize = true, MaximumSize = wrap },
            link,
            buttons,
        ]);
        Controls.Add(column);
        AcceptButton = CancelButton = close;
        ResumeLayout();
    }

    protected override void Dispose(bool disposing) {
        if (disposing) _nameFont.Dispose();
        base.Dispose(disposing);
    }
}
