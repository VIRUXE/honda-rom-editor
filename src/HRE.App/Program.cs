using HRE.Definitions;

namespace HRE.App;

internal static class Program {
    /// <summary>Starts the editor. A ROM image given on the command line is opened.</summary>
    [STAThread]
    private static void Main(string[] args) {
        Application.SetColorMode(SystemColorMode.Dark);
        ApplicationConfiguration.Initialize();
        var (packs, errors) = DefinitionPacks.LoadDirectory(Path.Combine(AppContext.BaseDirectory, "packs"));
        using var form = new MainForm(packs, errors);
        if (args.Length > 0) form.OpenRom(args[0]);
        Application.Run(form);
    }
}
