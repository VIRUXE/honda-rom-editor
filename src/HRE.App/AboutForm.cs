using System.Reflection;

namespace HRE.App;

/// <summary>The About dialog: name, version, what the program is and where it lives.</summary>
public sealed class AboutForm : Form {
    public AboutForm(string appName, string projectUrl) {
        Text = $"About {appName}";
        AutoScaleMode = AutoScaleMode.Dpi;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = MinimizeBox = ShowInTaskbar = false;
        BackColor = DarkTheme.Background;
        ForeColor = DarkTheme.Text;
        ClientSize = new Size(420, 190);

        var version = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0] ?? "";
        var close = new Button {
            Text = "Close",
            DialogResult = DialogResult.OK,
            FlatStyle = FlatStyle.Flat,
            BackColor = DarkTheme.Surface,
            ForeColor = DarkTheme.Text,
            Size = new Size(88, 28),
            Location = new Point(308, 146),
        };
        close.FlatAppearance.BorderColor = DarkTheme.Border;
        close.FlatAppearance.MouseOverBackColor = DarkTheme.Hover;

        Controls.AddRange([
            new Label { Text = appName, Font = new Font("Segoe UI Semibold", 16f), AutoSize = true, Location = new Point(22, 20) },
            new Label { Text = $"Version {version}", ForeColor = DarkTheme.TextDim, AutoSize = true, Location = new Point(24, 58) },
            new Label { Text = "A ROM editor and datalogger for Honda OBD0 and OBD1 engine computers.", Size = new Size(372, 36), Location = new Point(24, 86) },
            new Label { Text = projectUrl, ForeColor = DarkTheme.Accent, AutoSize = true, Location = new Point(24, 124) },
            close,
        ]);
        AcceptButton = CancelButton = close;
    }

    protected override void OnHandleCreated(EventArgs e) {
        base.OnHandleCreated(e);
        DarkTheme.ApplyToWindow(this);
    }
}
