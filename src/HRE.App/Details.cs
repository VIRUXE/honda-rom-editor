using System.Globalization;
using HRE.Core;
using HRE.Definitions;

namespace HRE.App;

/// <summary>How serious a notice is. It picks the symbol and the colour; the words always say it too.</summary>
public enum Severity { Info, Good, Warning, Problem }

/// <summary>What a button on a page does.</summary>
public enum PageCommand { OpenFile, OpenRecent, OpenLink, Dismiss }

/// <summary>A button on a page. <see cref="Argument"/> is the file or the address the command works on.</summary>
public sealed record PageAction(string Label, PageCommand Command, string? Argument = null, bool Primary = false);

/// <summary>One line of a section: a label and its value. An empty label makes the value a line of its own.</summary>
public sealed record DetailRow(string Label, string Value);

/// <summary>One piece of a page, top to bottom.</summary>
public abstract record PageBlock {
    /// <summary>A sentence or two of running text.</summary>
    public sealed record Paragraph(string Value) : PageBlock;

    /// <summary>Something the reader should not miss: a symbol, a heading and a sentence.</summary>
    public sealed record Notice(Severity Severity, string Heading, string Body) : PageBlock;

    /// <summary>
    /// Rows under a heading. A technical section holds what only matters to someone who works
    /// on the bytes (addresses, hashes, encodings); it is shown on request.
    /// </summary>
    public sealed record Section(string Heading, IReadOnlyList<DetailRow> Rows, bool Technical = false) : PageBlock;

    /// <summary>Buttons, under an optional heading.</summary>
    public sealed record Actions(IReadOnlyList<PageAction> Items, string? Heading = null) : PageBlock;

    /// <summary>The cells of a table, to read and to change, under a heading.</summary>
    public sealed record Grid(string Heading, RomDocument Document, ParamDef Def) : PageBlock;
}

/// <summary>What the page view shows: a title, a line under it and blocks.</summary>
public sealed record DetailPage(string Title, string Subtitle, IReadOnlyList<PageBlock> Blocks);

/// <summary>
/// Builds every page of the window, and so holds its wording. Rules: say what a thing means
/// before how it is stored; one word per thing; a problem says what happened and what to do;
/// addresses, hashes and encodings go in technical sections.
/// </summary>
public static class Details {
    public const string SupportedRomsUrl = "https://github.com/VIRUXE/honda-rom-editor/wiki/Supported-ROMs";

    /// <summary>What can be done in the cells of a table, said above them: the keys are not to be guessed.</summary>
    public const string GridHelp = "Move with the arrow keys; hold Shift to select more cells. Type a number and press Enter to change the selected cells, or press + and - to raise and lower them a step. Ctrl+C copies, Ctrl+V pastes.";

    /// <summary>What this version can and cannot do yet.</summary>
    public const string EarlyVersion = "It opens ROM files and lets you change the values of their tables. It cannot undo a change or save the file yet.";

    private static readonly CultureInfo Format = CultureInfo.InvariantCulture;

    public static DetailPage Start(IReadOnlyList<DefinitionPack> packs, IReadOnlyList<ProjectException> packErrors, IReadOnlyList<string> recentFiles) {
        var blocks = new List<PageBlock>();
        if (packs.Count == 0) {
            blocks.Add(new PageBlock.Notice(Severity.Problem, "The ROM descriptions are missing", "Without them no file can be opened. They belong in the folder named packs next to the program. Install the program again."));
        }
        blocks.Add(new PageBlock.Actions([new PageAction("Open a file...", PageCommand.OpenFile, Primary: true)]));
        blocks.Add(new PageBlock.Paragraph("Or drag a file from a folder and drop it anywhere in this window."));
        if (recentFiles.Count > 0) {
            blocks.Add(new PageBlock.Actions([.. recentFiles.Select(path => new PageAction(RecentLabel(path), PageCommand.OpenRecent, path))], "Recent files"));
        }
        blocks.Add(new PageBlock.Notice(Severity.Info, "Early version", EarlyVersion));
        blocks.Add(new PageBlock.Section("New here?", [
            new("", "A ROM is the program and the settings of an engine computer (ECU). It is read from the chip inside the ECU into a file, usually a .bin file of 32 KB."),
            new("", "Open such a file to see what it is and which fuel and ignition tables it holds."),
        ]));
        if (packs.Count > 0) blocks.Add(new PageBlock.Section("ROMs this version can open", [.. packs.Select(p => new DetailRow("", p.Name))]));
        if (packErrors.Count > 0) {
            blocks.Add(new PageBlock.Notice(Severity.Warning,
                packErrors.Count == 1 ? "1 ROM description could not be loaded" : $"{packErrors.Count} ROM descriptions could not be loaded",
                "ROMs of that kind will not be recognised. The others open as usual."));
            blocks.Add(new PageBlock.Section("Descriptions that could not be loaded", [.. packErrors.Select(e => new DetailRow("", e.Message))], Technical: true));
        }
        return new DetailPage("Honda ROM Editor", "See what is inside the ROM of a Honda engine computer.", blocks);
    }

    /// <summary>What to show when a file was not opened: what happened, what to do, and the way on.</summary>
    public static IReadOnlyList<PageBlock> Problem(OpenResult.Refused refused, int romSize, bool offerOpen = true) {
        var name = refused.FileName;
        var held = refused.Size is { } size ? $"It holds {Bytes(size)}; the ROMs this version opens hold {Bytes(romSize)}." : "";
        var (heading, body) = refused.Problem switch {
            OpenProblem.NotFound => ($"{name} is no longer there", "It may have been moved, renamed or deleted."),
            OpenProblem.IsFolder => ($"{name} is a folder, not a file", "Choose the ROM file inside it."),
            OpenProblem.InUse => ($"Another program is using {name}", "Close it there, then try again."),
            OpenProblem.Denied => ($"Windows did not let this program read {name}", "Check that you are allowed to open it."),
            OpenProblem.BadName => ("That is not a file name Windows accepts", "Choose the file with File > Open."),
            OpenProblem.Unreadable => ($"{name} could not be read", "Windows gave the reason shown under the technical details."),
            OpenProblem.Empty => ($"{name} is empty", "The copy, the download or the read from the chip did not finish. Get the file again."),
            OpenProblem.TooSmall => ($"{name} is too small to be a ROM", $"{held} It may be another kind of file, or the read from the chip stopped short."),
            OpenProblem.TooLarge => ($"{name} is too large to be a ROM", $"{held} It is another kind of file."),
            OpenProblem.UnknownRom => ($"This version does not know the ROM in {name}", "It matches none of the ROMs this version can open, so its tables cannot be shown. The file was not changed."),
            _ => throw new ArgumentOutOfRangeException(nameof(refused)),
        };
        var actions = new List<PageAction>();
        if (offerOpen) actions.Add(new("Choose another file...", PageCommand.OpenFile, Primary: true));
        if (refused.Problem == OpenProblem.UnknownRom) actions.Add(new("Which ROMs can be opened?", PageCommand.OpenLink, SupportedRomsUrl));
        actions.Add(new("Dismiss", PageCommand.Dismiss));

        var technical = new List<DetailRow> { new("File", refused.Path) };
        if (refused.Size is { } length) technical.Add(new("Size", $"{Bytes(length)} (0x{length:X})"));
        if (refused.Sha256 is { } hash) technical.Add(new("SHA-256", hash));
        if (refused.SystemMessage is { } message) technical.Add(new("Windows said", message));
        return [
            new PageBlock.Notice(Severity.Problem, heading, body.Trim()),
            new PageBlock.Actions(actions),
            new PageBlock.Section("About the file that was not opened", technical, Technical: true),
        ];
    }

    /// <summary>What to show when the program itself went wrong.</summary>
    public static IReadOnlyList<PageBlock> Fault(Exception fault) => [
        new PageBlock.Notice(Severity.Problem, "Something went wrong in Honda ROM Editor", "Your files were not changed. You can keep working; if it happens again, please report it with the technical details."),
        new PageBlock.Actions([new("Dismiss", PageCommand.Dismiss)]),
        new PageBlock.Section("What went wrong", [new("", fault.ToString())], Technical: true),
    ];

    public static DetailPage Rom(RomSession session) {
        var (pack, image) = (session.Pack, session.Image);
        var blocks = new List<PageBlock>();
        if (pack.Doc.Length > 0) blocks.Add(new PageBlock.Paragraph(pack.Doc));
        switch (session.Checksum) {
            case ChecksumState.Valid:
                blocks.Add(new PageBlock.Notice(Severity.Good, "This file is intact", "Its checksum is correct. The checksum is a self-check built into the ROM: it shows that no byte was lost or changed by accident."));
                break;
            case ChecksumState.Wrong:
                blocks.Add(new PageBlock.Notice(Severity.Warning, "The checksum of this file is wrong", "The checksum is a self-check built into the ROM. The file may be damaged, or it was changed by a program that did not correct the checksum."));
                break;
        }
        if (session.OtherMatches.Count > 0) {
            blocks.Add(new PageBlock.Notice(Severity.Info, $"This file could also be {string.Join(" or ", session.OtherMatches.Select(p => p.Name))}",
                $"It fits more than one kind of ROM equally well. It is shown as {pack.Name}."));
        }

        var extra = image.Length - pack.RomSize;
        var about = new List<DetailRow> {
            new("Kind of ROM", pack.Name),
            new("Size", extra > 0 ? $"{Bytes(image.Length)}. The ROM is the first {Bytes(pack.RomSize)}; the rest is left alone." : Bytes(image.Length)),
            new("Checksum", session.Checksum switch {
                ChecksumState.Valid => "Correct",
                ChecksumState.Wrong => "Wrong",
                ChecksumState.None => "This kind of ROM has none",
                _ => throw new ArgumentOutOfRangeException(nameof(session)),
            }),
        };
        blocks.Add(new PageBlock.Section("About this file", about));

        var categories = ParamTree.Build(session.Defs);
        if (categories.Count > 0) {
            blocks.Add(new PageBlock.Section("What is in this file", [.. categories.Select(c => new DetailRow(c.Name, string.Join(", ", c.Params.Select(p => p.Name))))]));
        }
        if (pack.Patches.Count > 0) {
            blocks.Add(new PageBlock.Section("Add-ons", [.. pack.Patches.Select(p => new DetailRow(p.Name, PatchEngine.Probe(p, image) switch {
                PatchState.Applied => "On",
                PatchState.NotApplied => "Off",
                PatchState.Conflict => "Cannot tell: this part of the ROM is not what the add-on expects, so it is left alone",
                _ => throw new ArgumentOutOfRangeException(nameof(session)),
            }))]));
        }
        if (session.Problems.Count > 0) blocks.Add(new PageBlock.Section("Not shown", [.. session.Problems.Select(p => new DetailRow("", p))], Technical: true));

        var checksum = pack.Checksum;
        var technical = new List<DetailRow> {
            new("File", session.Path),
            new("Size", $"{Bytes(image.Length)} (0x{image.Length:X})"),
            new("SHA-256", image.Sha256()),
            new("Description", pack.Id),
            new("Platform", PlatformName(pack.Platform)),
            new("Checksum rule", checksum.Kind == ChecksumKind.None
                ? "None"
                : $"Bytes {Range(checksum.Start, checksum.End - checksum.Start)} sum to zero, corrected at 0x{checksum.Location:X4}"),
        };
        if (pack.Datalog is { } datalog) technical.Add(new("Datalog protocol", datalog.Baud is { } baud ? $"{datalog.Protocol}, {baud} baud" : datalog.Protocol));
        blocks.Add(new PageBlock.Section("Technical details", technical, Technical: true));
        return new DetailPage(session.FileName, $"Recognised as {pack.Name}", blocks);
    }

    public static DetailPage Category(ParamCategory category) =>
        new(category.Name, Count(category.Params), [
            new PageBlock.Section("In this group", [.. category.Params.Select(p => new DetailRow(p.Name, p.Doc.Length > 0 ? p.Doc : KindText(p)))]),
        ]);

    public static DetailPage Param(RomDocument document, ParamDef def) {
        var image = document.Image;
        var blocks = new List<PageBlock>();
        if (def.Doc.Length > 0) blocks.Add(new PageBlock.Paragraph(def.Doc));
        var technical = new List<DetailRow>();
        if (def.Symbol is { } symbol) technical.Add(new("Symbol", symbol));
        technical.Add(new("Address", Range(def.Offset, def.Length)));
        switch (def.Kind) {
            case ParamKind.Table table:
                var (min, max) = TableRange(image, def, table);
                var reading = new List<DetailRow> {
                    new("Rows", AxisText(image, def.RowAxis, table.Rows)),
                    new("Columns", AxisText(image, def.ColAxis, table.Cols)),
                    new("Values", $"{Plain(min)} to {Plain(max)}{UnitSuffix(def)}"),
                };
                if (def.Vtec is { } vtec) reading.Add(new("Used", vtec ? "While VTEC is on" : "While VTEC is off"));
                blocks.Add(new PageBlock.Section("How to read it", reading));
                blocks.Add(new PageBlock.Grid(def.Unit.Length > 0 ? $"Values, in {def.Unit}" : "Values", document, def));
                technical.Add(new("Cells", $"{table.Rows} x {table.Cols}, {CellName(def.Cell)}"));
                AddScaling(technical, def);
                if (def.Multiplier is { } m) {
                    technical.Add(new("Column multiplier", $"{(m.Kind == MultiplierKind.Linear ? "Cell x byte" : "Cell x 2^byte")}, {Range(m.Offset, table.Cols)}"));
                }
                technical.Add(new("Row steps", AxisStorage(def.RowAxis, table.Rows)));
                technical.Add(new("Column steps", AxisStorage(def.ColAxis, table.Cols)));
                break;
            case ParamKind.Word word:
                var raw = ParamAccess.Word(image, def);
                var value = new List<DetailRow> { new("Value", $"{Plain(def.Display(raw))}{UnitSuffix(def)}") };
                value.Add(word.Min == word.Max
                    ? new("Can be changed", "No: this value is fixed")
                    : new("Can be set", $"From {Plain(def.Display(word.Min))} to {Plain(def.Display(word.Max))}{UnitSuffix(def)}"));
                blocks.Add(new PageBlock.Section("What it is set to", value));
                technical.Add(new("Stored", $"{raw}, 16-bit, {(def.LittleEndian ? "low byte first" : "high byte first")}"));
                AddScaling(technical, def);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(def));
        }
        blocks.Add(new PageBlock.Section("Technical details", technical, Technical: true));
        return new DetailPage(def.Name, def.Category, blocks);
    }

    /// <summary>"2 tables", "1 table and 1 setting": what a list of parameters holds, in the window's words.</summary>
    public static string Count(IReadOnlyCollection<ParamDef> defs) {
        var tables = defs.Count(d => d.Kind is ParamKind.Table);
        var settings = defs.Count - tables;
        string[] parts = [
            tables == 0 ? "" : tables == 1 ? "1 table" : $"{tables} tables",
            settings == 0 ? "" : settings == 1 ? "1 setting" : $"{settings} settings",
        ];
        var said = parts.Where(p => p.Length > 0).ToList();
        return said.Count == 0 ? "Nothing to show" : string.Join(" and ", said);
    }

    /// <summary>What kind of thing a parameter is, in a few words: for lists and tool tips.</summary>
    public static string KindText(ParamDef def) => def.Kind switch {
        ParamKind.Table t => $"Table, {t.Rows} rows by {t.Cols} columns{(def.Unit.Length > 0 ? $", in {def.Unit}" : "")}",
        ParamKind.Word => def.Unit.Length > 0 ? $"Single value, in {def.Unit}" : "Single value",
        _ => throw new ArgumentOutOfRangeException(nameof(def)),
    };

    /// <summary>A whole number of bytes with its unit: "1 byte", "32,768 bytes".</summary>
    public static string Bytes(long count) => count == 1 ? "1 byte" : $"{count.ToString("N0", Format)} bytes";

    private static string RecentLabel(string path) => $"{Path.GetFileName(path)}   ({Path.GetDirectoryName(path)})";

    private static string AxisText(RomImage image, Axis axis, int n) {
        if (n == 0) return axis.Label;
        var values = ParamAccess.AxisValues(image, axis, n);
        return $"{axis.Label}: {n} steps, from {Plain(values[0])} to {Plain(values[^1])}";
    }

    private static string AxisStorage(Axis axis, int n) => axis.IsFixed
        ? "Not in the ROM: fixed by the description"
        : $"{EncodingName(axis.Encoding)}, {Range(axis.Offset, axis.StoredLength(n))}";

    private static void AddScaling(List<DetailRow> rows, ParamDef def) {
        if (def.Scale == 1f && def.Bias == 0f) return;
        var bias = def.Bias == 0f ? "" : def.Bias > 0f ? $" + {Exact(def.Bias)}" : $" - {Exact(-def.Bias)}";
        rows.Add(new("Shown as", $"stored x {Exact(def.Scale)}{bias}"));
    }

    private static (float Min, float Max) TableRange(RomImage image, ParamDef def, ParamKind.Table table) {
        var (min, max) = (float.MaxValue, float.MinValue);
        for (var row = 0; row < table.Rows; row++) {
            for (var col = 0; col < table.Cols; col++) {
                var value = ParamAccess.CellDisplay(image, def, row, col);
                (min, max) = (Math.Min(min, value), Math.Max(max, value));
            }
        }
        return table.Rows * table.Cols == 0 ? (0f, 0f) : (min, max);
    }

    private static string PlatformName(RomPlatform platform) => platform switch {
        RomPlatform.Obd1Oki66207 => "OBD1, OKI MSM66207",
        RomPlatform.Obd0Oki66k => "OBD0, OKI 66k family",
        RomPlatform.Obd0I8051 => "OBD0, 8051 family",
        _ => throw new ArgumentOutOfRangeException(nameof(platform)),
    };

    private static string CellName(CellFormat cell) => cell switch {
        CellFormat.U8 => "8-bit",
        CellFormat.U16BigEndian => "16-bit, high byte first",
        CellFormat.U16LittleEndian => "16-bit, low byte first",
        _ => throw new ArgumentOutOfRangeException(nameof(cell)),
    };

    private static string EncodingName(AxisEncoding encoding) => encoding switch {
        AxisEncoding.Words => "16-bit words",
        AxisEncoding.DeltaBytes => "Bytes, each the step from the one before",
        AxisEncoding.Bytes => "Bytes",
        AxisEncoding.LogBytes => "Bytes on a logarithmic scale",
        _ => throw new ArgumentOutOfRangeException(nameof(encoding)),
    };

    /// <summary>A byte range the way a hex editor shows it: first and last address.</summary>
    private static string Range(int start, int length) =>
        length <= 0 ? $"0x{start:X4}" : $"0x{start:X4} to 0x{start + length - 1:X4} ({Bytes(length)})";

    private static string UnitSuffix(ParamDef def) => def.Unit.Length > 0 ? " " + def.Unit : "";

    /// <summary>A number for reading: whole from a thousand up, one decimal below.</summary>
    private static string Plain(float value) => value.ToString(Math.Abs(value) >= 1000f ? "0" : "0.#", Format);

    /// <summary>A number as the definition states it, for the technical rows.</summary>
    private static string Exact(float value) => value.ToString("0.####", Format);
}
