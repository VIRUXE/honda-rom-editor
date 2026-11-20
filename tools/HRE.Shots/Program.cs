using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using HRE.App;

namespace HRE.Shots;

/// <summary>
/// Saves a picture of the main window, for the documentation and for checking the theme:
/// <c>HRE.Shots [options] out.png [rom.bin [parameter name]]</c>. The window is shown without
/// taking the keyboard focus and is read back from the window manager, so it need not be on top.
/// It starts from default settings and saves none. Options: <c>--technical</c> shows the
/// technical details; <c>--zoom=1.5</c> sets the text size; <c>--cell=row,col</c> moves to a cell
/// of the table, which scrolls it into view; <c>--trace=row,col</c> shows the engine running at
/// that position between cells, with made-up counts of readings around it; <c>--size=1180x1300</c>
/// sets the size of the window, which may be taller than the screen; <c>--hold=seconds</c>
/// keeps the window up after the picture, to read what it gives a screen reader from another process.
/// </summary>
internal static partial class Program {
    private const int ShowNoActivate = 4;
    private const uint RenderFullContent = 2;

    [STAThread]
    private static int Main(string[] all) {
        var settings = new AppSettings();
        var args = new List<string>();
        (int Row, int Col)? cell = null;
        (float Row, float Col)? trace = null;
        var hold = 0.0;
        Size? size = null;
        static float[] Pair(string arg) => [.. arg[(arg.IndexOf('=') + 1)..].Split(',').Select(v => float.Parse(v, System.Globalization.CultureInfo.InvariantCulture))];
        foreach (var arg in all) {
            if (arg == "--technical") settings.ShowTechnical = true;
            else if (arg.StartsWith("--zoom=", StringComparison.Ordinal)) settings.TextZoom = double.Parse(arg["--zoom=".Length..], System.Globalization.CultureInfo.InvariantCulture);
            else if (arg.StartsWith("--cell=", StringComparison.Ordinal)) cell = ((int)Pair(arg)[0], (int)Pair(arg)[1]);
            else if (arg.StartsWith("--trace=", StringComparison.Ordinal)) trace = (Pair(arg)[0], Pair(arg)[1]);
            else if (arg.StartsWith("--hold=", StringComparison.Ordinal)) hold = Pair(arg)[0];
            else if (arg.StartsWith("--size=", StringComparison.Ordinal)) size = new Size((int)Pair(arg.Replace('x', ','))[0], (int)Pair(arg.Replace('x', ','))[1]);
            else args.Add(arg);
        }
        if (args.Count is < 1 or > 3) {
            Console.Error.WriteLine("usage: HRE.Shots [--technical] [--zoom=1.5] [--cell=row,col] [--trace=row,col] [--size=WxH] [--hold=seconds] out.png [rom.bin [parameter name]]");
            return 2;
        }
        AppStartup.Configure();
        var (packs, errors) = AppStartup.LoadPacks();
        using var form = new MainForm(packs, errors, settings) { StartPosition = FormStartPosition.Manual, Location = new Point(40, 40) };
        if (size is { } wanted) {
            form.MaximumSize = wanted;
            form.Size = wanted;
        }
        if (args.Count > 1) form.OpenRom(args[1]);
        if (args.Count > 2 && !form.SelectParam(args[2])) {
            Console.Error.WriteLine($"no parameter named `{args[2]}`");
            return 1;
        }

        ShowWindow(form.Handle, ShowNoActivate);
        Settle();
        if (form.Page.Grid is { } grid) {
            if (cell is { } at) grid.CurrentCell = grid[at.Col, at.Row];
            if (trace is { } where) {
                grid.Trace = where;
                // Readings as a log would leave them: many where the engine is, fewer around it.
                grid.Hits = [.. Enumerable.Range(0, grid.RowCount * grid.ColumnCount).Select(i => {
                    var (dr, dc) = (i / grid.ColumnCount - where.Row, i % grid.ColumnCount - where.Col);
                    return (int)(240 * MathF.Exp(-(dr * dr + dc * dc) / 3f));
                })];
            }
            Settle();
        }
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
        for (var until = DateTime.UtcNow.AddSeconds(hold); DateTime.UtcNow < until;) Settle();
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
