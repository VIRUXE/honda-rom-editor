using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using HRE.App;
using HRE.Definitions;

namespace HRE.Shots;

/// <summary>
/// Saves a picture of the main window, for the documentation and for checking the theme:
/// <c>HRE.Shots out.png [rom.bin [parameter name]]</c>. The window is shown without taking the
/// keyboard focus and is read back from the window manager, so it need not be on top.
/// </summary>
internal static partial class Program {
    private const int ShowNoActivate = 4;
    private const uint RenderFullContent = 2;

    [STAThread]
    private static int Main(string[] args) {
        if (args.Length is < 1 or > 3) {
            Console.Error.WriteLine("usage: HRE.Shots out.png [rom.bin [parameter name]]");
            return 2;
        }
        Application.SetColorMode(SystemColorMode.Dark);
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();

        var (packs, errors) = DefinitionPacks.LoadDirectory(Path.Combine(AppContext.BaseDirectory, "packs"));
        using var form = new MainForm(packs, errors) { StartPosition = FormStartPosition.Manual, Location = new Point(40, 40) };
        if (args.Length > 1) form.OpenRom(args[1]);
        if (args.Length > 2 && !form.SelectParam(args[2])) {
            Console.Error.WriteLine($"no parameter named `{args[2]}`");
            return 1;
        }

        ShowWindow(form.Handle, ShowNoActivate);
        Settle();
        GetWindowRect(form.Handle, out var bounds);
        using var bitmap = new Bitmap(bounds.Right - bounds.Left, bounds.Bottom - bounds.Top, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bitmap)) {
            var dc = g.GetHdc();
            var printed = PrintWindow(form.Handle, dc, RenderFullContent);
            g.ReleaseHdc(dc);
            if (!printed) {
                Console.Error.WriteLine("the window could not be read back");
                return 1;
            }
        }
        bitmap.Save(args[0], ImageFormat.Png);
        Console.WriteLine($"{args[0]}: {bitmap.Width} x {bitmap.Height}");
        return 0;
    }

    /// <summary>Lets the window paint itself, title bar included.</summary>
    private static void Settle() {
        for (var i = 0; i < 20; i++) {
            Application.DoEvents();
            Thread.Sleep(25);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect {
        public int Left, Top, Right, Bottom;
    }

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool ShowWindow(IntPtr window, int command);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetWindowRect(IntPtr window, out Rect bounds);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool PrintWindow(IntPtr window, IntPtr dc, uint flags);
}
