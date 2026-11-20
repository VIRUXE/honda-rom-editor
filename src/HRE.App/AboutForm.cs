using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace HRE.App;

/// <summary>The About dialog: name, version, what the program is today and where to report a problem.</summary>
/// <remarks>
/// The controls are laid out in <c>AboutForm.Designer.cs</c>, which the Visual Studio designer
/// edits; the name, the version, the fonts and the colours are applied here.
/// </remarks>
public sealed partial class AboutForm : Form {
    private readonly Font nameFont;
    private readonly string reportUrl;
    private readonly string details;

    public AboutForm(string appName, string reportUrl) {
        var version = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0] ?? "";
        details = $"{appName} {version}, {RuntimeInformation.FrameworkDescription}, {RuntimeInformation.OSDescription}";
        this.reportUrl = reportUrl;

        InitializeComponent();
        SuspendLayout();
        Text = $"About {appName}";
        DarkTheme.ApplyToWindow(this);
        nameFont = new Font(Font.FontFamily, Font.SizeInPoints * 1.8f, FontStyle.Bold);

        // Everything sizes itself to its text, so larger text and other scalings cannot clip it.
        var wrap = new Size(LogicalToDeviceUnits(380), 0);
        nameLabel.Text = appName;
        nameLabel.Font = nameFont;
        versionLabel.Text = $"Version {version}";
        versionLabel.ForeColor = DarkTheme.TextDim;
        purposeLabel.MaximumSize = earlyLabel.MaximumSize = wrap;
        earlyLabel.Text = $"Early version. {Details.EarlyVersion}";
        reportLink.AccessibleDescription = reportUrl;
        if (!DarkTheme.HighContrast) {
            (reportLink.LinkColor, reportLink.ActiveLinkColor, reportLink.VisitedLinkColor) = (DarkTheme.Accent, DarkTheme.Text, DarkTheme.Accent);
            // The main button in the accent of the program, not of Windows; dark text on it reads best.
            (close.BackColor, close.ForeColor) = (DarkTheme.Accent, Color.Black);
            (copy.BackColor, copy.ForeColor) = (DarkTheme.Card, DarkTheme.Text);
        }
        ResumeLayout();
    }

    protected override void Dispose(bool disposing) {
        if (disposing) {
            components?.Dispose();
            nameFont.Dispose();
        }
        base.Dispose(disposing);
    }

    private void ReportLink_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs e) =>
        Process.Start(new ProcessStartInfo(reportUrl) { UseShellExecute = true })?.Dispose();

    private void Copy_Click(object? sender, EventArgs e) => Clipboard.SetText(details);
}
