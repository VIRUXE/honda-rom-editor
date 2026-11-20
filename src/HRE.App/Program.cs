using HRE.Definitions;

namespace HRE.App;

/// <summary>How the application sets itself up before its first window. The picture tool starts the same way.</summary>
public static class AppStartup {
    /// <summary>Dark colour mode, the current look of the standard controls, per-monitor scaling.</summary>
    public static void Configure() {
        Application.SetColorMode(SystemColorMode.Dark);
        Application.SetDefaultVisualStylesMode(VisualStylesMode.Net11);
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
    }

    /// <summary>The definition packs that ship next to the program.</summary>
    public static (IReadOnlyList<DefinitionPack> Packs, IReadOnlyList<ProjectException> Errors) LoadPacks() =>
        DefinitionPacks.LoadDirectory(Path.Combine(AppContext.BaseDirectory, "packs"));
}

internal static class Program {
    /// <summary>Starts the editor. A ROM file given on the command line is opened.</summary>
    [STAThread]
    private static void Main(string[] args) {
        AppStartup.Configure();
        var (packs, errors) = AppStartup.LoadPacks();
        var settingsPath = SettingsFile.DefaultPath;
        var settings = SettingsFile.Load(settingsPath);
        using var form = new MainForm(packs, errors, settings);
        // A fault in a command is told in the window, which stays up: nothing here can damage a file.
        Application.ThreadException += (_, e) => form.ReportFault(e.Exception);
        if (args is [{ Length: > 0 } path, ..]) form.OpenRom(path);
        Application.Run(form);
        SettingsFile.Save(settingsPath, settings);
    }
}
