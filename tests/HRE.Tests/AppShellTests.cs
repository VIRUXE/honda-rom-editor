using System.Drawing;
using System.Windows.Forms;
using HRE.App;
using HRE.Core;
using HRE.Definitions;

namespace HRE.Tests;

/// <summary>
/// The application window: opening a file, the list, the wording of every page, the theme, the
/// settings, and what the window offers to the keyboard and to a screen reader.
/// </summary>
public class AppShellTests {
    private static readonly DefinitionPack[] Packs = [DefinitionPackTests.Mini()];

    internal static RomSession MiniSession(RomImage? image = null) => RomSession.Recognise(@"C:\roms\mini.bin", image ?? DefinitionPackTests.MiniRom(), Packs)!;

    private static IEnumerable<PageBlock.Section> Sections(DetailPage page) => page.Blocks.OfType<PageBlock.Section>();

    private static IEnumerable<PageBlock.Notice> Notices(DetailPage page) => page.Blocks.OfType<PageBlock.Notice>();

    internal static string Value(DetailPage page, string label) => Sections(page).SelectMany(s => s.Rows).Single(r => r.Label == label).Value;

    /// <summary>Everything a page says outside its technical sections.</summary>
    private static string PlainText(IEnumerable<PageBlock> blocks) => string.Join("\n", blocks.SelectMany<PageBlock, string>(b => b switch {
        PageBlock.Paragraph p => [p.Value],
        PageBlock.Notice n => [n.Heading, n.Body],
        PageBlock.Section { Technical: false } s => [s.Heading, .. s.Rows.SelectMany(r => new[] { r.Label, r.Value })],
        PageBlock.Actions a => [a.Heading ?? "", .. a.Items.Select(i => i.Label)],
        _ => [],
    }));

    /// <summary>A folder of its own for files a test makes; removed afterwards.</summary>
    private static void InTempFolder(Action<string> body) {
        var folder = Path.Combine(Path.GetTempPath(), $"hre-shell-{Guid.NewGuid():N}");
        Directory.CreateDirectory(folder);
        try {
            body(folder);
        } finally {
            Directory.Delete(folder, recursive: true);
        }
    }

    /// <summary>Runs on a thread of its own that may own windows; what it throws is thrown here.</summary>
    internal static void OnUiThread(Action body) {
        Exception? failure = null;
        var thread = new Thread(() => {
            try {
                body();
            } catch (Exception e) {
                failure = e;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (failure is not null) throw new Xunit.Sdk.XunitException(failure.ToString());
    }

    [Fact]
    public void ARecognisedRomGetsItsPackAndParameters() {
        var session = MiniSession();
        Assert.Equal("mini", session.Pack.Id);
        Assert.Empty(session.OtherMatches);
        Assert.Equal(["Fuel Map", "Wide Map", "Rev Limiter"], session.Defs.Select(d => d.Name));
        Assert.Empty(session.Problems);
        Assert.Equal("mini.bin", session.FileName);
        Assert.False(session.Document.IsDirty);
        Assert.Null(RomSession.Recognise("blank.bin", DefinitionPackTests.Blank, Packs));
    }

    [Fact]
    public void TheChecksumIsJudgedByThePack() {
        var rom = DefinitionPackTests.MiniRom();
        Assert.Equal(ChecksumState.Wrong, MiniSession(rom).Checksum);
        Assert.Equal(ChecksumState.Valid, MiniSession(Checksum.Fix(rom, Packs[0].Checksum)).Checksum);
        var unchecked_ = Packs[0] with { Checksum = new ChecksumSpec(ChecksumKind.None, 0, 0, 0) };
        Assert.Equal(ChecksumState.None, RomSession.Recognise("mini.bin", rom, [unchecked_])!.Checksum);
    }

    [Fact]
    public void OpeningSaysWhyAFileWasNotOpened() => InTempFolder(folder => {
        OpenProblem Problem(string path) => Assert.IsType<OpenResult.Refused>(RomSession.Open(path, Packs)).Problem;
        string Make(string name, byte[] bytes) {
            var path = Path.Combine(folder, name);
            File.WriteAllBytes(path, bytes);
            return path;
        }

        Assert.Equal(OpenProblem.NotFound, Problem(Path.Combine(folder, "missing.bin")));
        Assert.Equal(OpenProblem.NotFound, Problem(Path.Combine(folder, "no-such-folder", "missing.bin")));
        Assert.Equal(OpenProblem.IsFolder, Problem(folder));
        Assert.Equal(OpenProblem.BadName, Problem(""));
        Assert.Equal(OpenProblem.Empty, Problem(Make("empty.bin", [])));
        Assert.Equal(OpenProblem.TooSmall, Problem(Make("short.bin", new byte[100])));
        Assert.Equal(OpenProblem.TooLarge, Problem(Make("video.bin", new byte[RomSession.LargestFile + 1])));

        var unknown = Assert.IsType<OpenResult.Refused>(RomSession.Open(Make("blank.bin", new byte[0x8000]), Packs));
        Assert.Equal((OpenProblem.UnknownRom, 0x8000L), (unknown.Problem, unknown.Size));
        Assert.Equal(DefinitionPackTests.Blank.Sha256(), unknown.Sha256);
        Assert.Equal("blank.bin", unknown.FileName);

        var good = Make("mini.bin", DefinitionPackTests.MiniRom().ToArray());
        using (File.Open(good, FileMode.Open, FileAccess.Read, FileShare.None)) Assert.Equal(OpenProblem.InUse, Problem(good));
        var opened = Assert.IsType<OpenResult.Opened>(RomSession.Open(good, Packs));
        Assert.Equal((good, "mini"), (opened.Session.Path, opened.Session.Pack.Id));
    });

    [Fact]
    public void EveryProblemIsToldInPlainWordsWithAWayOn() {
        foreach (var problem in Enum.GetValues<OpenProblem>()) {
            var blocks = Details.Problem(new OpenResult.Refused(problem, @"C:\roms\odd.bin", 100, "ab12", "Access to the path is denied."), 0x8000);
            var notice = Assert.IsType<PageBlock.Notice>(blocks[0]);
            Assert.Equal(Severity.Problem, notice.Severity);
            Assert.True(notice.Heading.Length > 0 && notice.Body.Length > 0, problem.ToString());
            // What went wrong and what to do, never the machine's words for it.
            var plain = PlainText(blocks);
            Assert.DoesNotContain("0x", plain);
            Assert.DoesNotContain("ab12", plain);
            Assert.DoesNotContain("denied.", plain);
            Assert.DoesNotMatch("(?i)error|invalid|failed|exception", plain);

            var actions = Assert.Single(blocks.OfType<PageBlock.Actions>()).Items;
            Assert.Equal((PageCommand.OpenFile, true), (actions[0].Command, actions[0].Primary));
            Assert.Equal(PageCommand.Dismiss, actions[^1].Command);
            var technical = Assert.Single(blocks.OfType<PageBlock.Section>());
            Assert.True(technical.Technical);
            Assert.Contains(technical.Rows, r => r.Value == @"C:\roms\odd.bin");
        }
        var small = Details.Problem(new OpenResult.Refused(OpenProblem.TooSmall, @"C:\roms\short.bin", 1), 0x8000);
        Assert.Equal("It holds 1 byte; the ROMs this version opens hold 32,768 bytes. It may be another kind of file, or the read from the chip stopped short.", ((PageBlock.Notice)small[0]).Body);
        var unknown = Details.Problem(new OpenResult.Refused(OpenProblem.UnknownRom, @"C:\roms\blank.bin", 0x8000), 0x8000);
        Assert.Contains(unknown.OfType<PageBlock.Actions>().Single().Items, a => a.Command == PageCommand.OpenLink && a.Argument == Details.SupportedRomsUrl);
    }

    [Fact]
    public void TheListKeepsTheOrderOfTheDefinitions() {
        var defs = MiniSession().Defs;
        var extra = defs[0] with { Name = "Fuel Trim" };
        var tree = ParamTree.Build([.. defs, extra]);
        Assert.Equal(["Fuel", "Ignition", "Limits"], tree.Select(c => c.Name));
        Assert.Equal(["Fuel Map", "Fuel Trim"], tree[0].Params.Select(p => p.Name));
        Assert.Empty(ParamTree.Build([]));
    }

    [Fact]
    public void TheRomPageSaysWhatTheFileIsBeforeHowItIsStored() {
        var session = MiniSession();
        var page = Details.Rom(session);
        Assert.Equal(("mini.bin", "Recognised as Mini test ROM"), (page.Title, page.Subtitle));
        Assert.Equal((Severity.Warning, "The checksum of this file is wrong"), (Notices(page).Single().Severity, Notices(page).Single().Heading));
        Assert.Equal("Mini test ROM", Value(page, "Kind of ROM"));
        Assert.Equal("32,768 bytes", Value(page, "Size", technical: false));
        Assert.Equal("Wrong", Value(page, "Checksum"));
        Assert.Equal("Fuel Map", Value(page, "Fuel"));
        Assert.Equal("Off", Value(page, "Fast serial routine"));

        // Addresses, hashes and encodings are there, and only where they are asked for.
        Assert.DoesNotContain("0x", PlainText(page.Blocks));
        Assert.DoesNotContain(session.Image.Sha256(), PlainText(page.Blocks));
        var technical = Sections(page).Single(s => s.Heading == "Technical details");
        Assert.True(technical.Technical);
        var rows = technical.Rows.ToDictionary(r => r.Label, r => r.Value);
        Assert.Equal(@"C:\roms\mini.bin", rows["File"]);
        Assert.Equal("32,768 bytes (0x8000)", rows["Size"]);
        Assert.Equal(session.Image.Sha256(), rows["SHA-256"]);
        Assert.Equal("mini", rows["Description"]);
        Assert.Equal("OBD0, OKI 66k family", rows["Platform"]);
        Assert.Equal("Bytes 0x0000 to 0x3FFF (16,384 bytes) sum to zero, corrected at 0x3FFF", rows["Checksum rule"]);
        Assert.Equal("stock, 9600 baud", rows["Datalog protocol"]);

        var mended = MiniSession(PatchEngine.Apply(Packs[0].Patches[0], Checksum.Fix(session.Image, Packs[0].Checksum)));
        Assert.Equal("On", Value(Details.Rom(mended), "Fast serial routine"));
        Assert.Equal(Severity.Good, Notices(Details.Rom(MiniSession(Checksum.Fix(session.Image, Packs[0].Checksum)))).Single().Severity);

        var longer = new RomImage([.. session.Image.ToArray(), 0]);
        Assert.Equal("32,769 bytes. The ROM is the first 32,768 bytes; the rest is left alone.", Value(Details.Rom(MiniSession(longer)), "Size", technical: false));
    }

    private static string Value(DetailPage page, string label, bool technical) =>
        Sections(page).Where(s => s.Technical == technical).SelectMany(s => s.Rows).Single(r => r.Label == label).Value;

    [Fact]
    public void PacksThatMatchEquallyWellAreReported() {
        var twin = Packs[0] with { Id = "twin", Name = "Twin" };
        var session = RomSession.Recognise("mini.bin", DefinitionPackTests.MiniRom(), [Packs[0], twin])!;
        Assert.Equal("mini", session.Pack.Id);
        Assert.Equal(["twin"], session.OtherMatches.Select(p => p.Id));
        Assert.Contains(Notices(Details.Rom(session)), n => n.Heading == "This file could also be Twin");
    }

    [Fact]
    public void ATablePageSaysWhatTheTableIsAndHowToReadIt() {
        var session = MiniSession();
        var described = session.Defs[0] with { Doc = "How much fuel the engine gets." };
        var page = Details.Param(session.Document, described);
        Assert.Equal(("Fuel Map", "Fuel"), (page.Title, page.Subtitle));
        Assert.Equal(new PageBlock.Paragraph("How much fuel the engine gets."), page.Blocks[0]);
        Assert.Equal("RPM: 2 steps, from 10 to 30", Value(page, "Rows"));
        Assert.Equal("MAP: 3 steps, from 30 to 90", Value(page, "Columns"));
        // Cells 1..6, the columns multiplied by 1, 2 and 4.
        Assert.Equal("1 to 24 raw", Value(page, "Values"));
        Assert.DoesNotContain("0x", PlainText(page.Blocks));

        Assert.Equal("0x0100 to 0x0105 (6 bytes)", Value(page, "Address"));
        Assert.Equal("2 x 3, 8-bit", Value(page, "Cells"));
        Assert.Equal("Cell x 2^byte, 0x0120 to 0x0122 (3 bytes)", Value(page, "Column multiplier"));
        Assert.Equal("Bytes, each the step from the one before, 0x00F0 to 0x00F1 (2 bytes)", Value(page, "Row steps"));

        var wide = Details.Param(session.Document, session.Defs[1]);
        Assert.IsNotType<PageBlock.Paragraph>(wide.Blocks[0]);
        Assert.Equal("2 x 2, 16-bit, low byte first", Value(wide, "Cells"));
        Assert.Equal("stored x 0.5", Value(wide, "Shown as"));
        Assert.Equal("Not in the ROM: fixed by the description", Value(wide, "Column steps"));
        Assert.Equal("While VTEC is on", Value(Details.Param(session.Document, session.Defs[0] with { Vtec = true }), "Used"));
    }

    [Fact]
    public void ASettingPageShowsTheValueAndWhatItMayBe() {
        var session = MiniSession();
        var page = Details.Param(session.Document, session.Defs[2]);
        Assert.Equal("9000 rpm", Value(page, "Value"));
        Assert.Equal("From 3000 to 11000 rpm", Value(page, "Can be set"));
        Assert.Equal("9000, 16-bit, low byte first", Value(page, "Stored"));
        var locked = session.Defs[2] with { Kind = new ParamKind.Word(9000, 9000) };
        Assert.Equal("No: this value is fixed", Value(Details.Param(session.Document, locked), "Can be changed"));
    }

    [Fact]
    public void AGroupPageListsWhatItHolds() {
        var groups = ParamTree.Build(MiniSession().Defs);
        var page = Details.Category(groups[0]);
        Assert.Equal(("Fuel", "1 table"), (page.Title, page.Subtitle));
        Assert.Equal("Table, 2 rows by 3 columns, in raw", Value(page, "Fuel Map"));
        Assert.Equal("Single value, in rpm", Value(Details.Category(groups[2]), "Rev Limiter"));
        Assert.Equal("1 setting", Details.Category(groups[2]).Subtitle);
        Assert.Equal("2 tables and 1 setting", Details.Count(MiniSession().Defs));
        Assert.Equal("Nothing to show", Details.Count([]));
        Assert.Equal(("1 byte", "32,768 bytes"), (Details.Bytes(1), Details.Bytes(0x8000)));
    }

    [Fact]
    public void TheStartPageOffersTheNextStepFirst() {
        var page = Details.Start(Packs, [], []);
        Assert.Equal(new PageAction("Open a file...", PageCommand.OpenFile, Primary: true), Assert.IsType<PageBlock.Actions>(page.Blocks[0]).Items.Single());
        Assert.Contains(Notices(page), n => n.Severity == Severity.Info && n.Body == Details.EarlyVersion);
        // The kinds of ROM by name; the identifiers of their descriptions are nobody's business here.
        Assert.Equal(["Mini test ROM"], Sections(page).Single(s => s.Heading == "ROMs this version can open").Rows.Select(r => r.Value));
        Assert.DoesNotContain(page.Blocks.OfType<PageBlock.Actions>(), a => a.Heading is not null);

        var recent = Details.Start(Packs, [], [@"C:\roms\a.bin", @"D:\b.bin"]).Blocks.OfType<PageBlock.Actions>().Single(a => a.Heading == "Recent files");
        Assert.Equal([(@"a.bin   (C:\roms)", @"C:\roms\a.bin"), (@"b.bin   (D:\)", @"D:\b.bin")], recent.Items.Select(i => (i.Label, i.Argument!)));
        Assert.All(recent.Items, i => Assert.Equal(PageCommand.OpenRecent, i.Command));

        var broken = Details.Start(Packs, [new ProjectException(ProjectErrorKind.Format, "bad.pack.toml", "missing `id`")], []);
        Assert.Contains(Notices(broken), n => n.Severity == Severity.Warning && n.Heading == "1 ROM description could not be loaded");
        Assert.Contains(Sections(broken), s => s.Technical && s.Rows.Count == 1);
        Assert.Equal(Severity.Problem, Assert.IsType<PageBlock.Notice>(Details.Start([], [], []).Blocks[0]).Severity);
    }

    [Fact]
    public void EveryShippedParameterIsDescribedInPlainWords() {
        var packs = ShippedPackTests.Packs();
        Assert.All(packs.SelectMany(p => p.Params), p => {
            Assert.True(p.Doc.Length > 40 && p.Doc.EndsWith('.'), $"{p.Name}: `{p.Doc}`");
            Assert.DoesNotContain("0x", p.Doc);
        });
        Assert.All(packs.Where(p => p.Id.StartsWith("obd1-", StringComparison.Ordinal)), p => Assert.Contains("not the part number", p.Doc));

        var text = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "fixtures", "packs", "mini.pack.toml")).Replace("name = \"Fuel Map\"", "name = \"Fuel Map\"\ndoc = \"What it is.\"");
        var (defs, _) = DefinitionPacks.Parse(text).Resolve(DefinitionPackTests.MiniRom());
        Assert.Equal(["What it is.", "", ""], defs.Select(d => d.Doc));
    }

    [Fact]
    public void TheThemeIsBlackAndReadable() {
        if (DarkTheme.HighContrast) return; // the colours are then the user's contrast theme, not ours
        Assert.Equal(Color.Black.ToArgb(), DarkTheme.Background.ToArgb());
        Assert.Equal(21, DarkTheme.Contrast(Color.White, Color.Black), 3);
        // WCAG: 7 for body text, 4.5 for the other text, 3 for what shows a state or where a control is.
        Color[] surfaces = [DarkTheme.Background, DarkTheme.Surface, DarkTheme.Card];
        Assert.All(surfaces, s => Assert.True(DarkTheme.Contrast(DarkTheme.Text, s) >= 7, s.ToString()));
        Assert.All(surfaces, s => Assert.True(DarkTheme.Contrast(DarkTheme.TextDim, s) >= 4.5, s.ToString()));
        Assert.True(DarkTheme.Contrast(DarkTheme.SelectionText, DarkTheme.Selection) >= 7);
        Assert.True(DarkTheme.Contrast(Color.Black, DarkTheme.Accent) >= 4.5, "the text of the main button");
        Assert.All(Enum.GetValues<Severity>(), severity => Assert.All(surfaces, s =>
            Assert.True(DarkTheme.Contrast(DarkTheme.SeverityColor(severity), s) >= 4.5, $"{severity} on {s}")));
        Assert.True(DarkTheme.Contrast(DarkTheme.Accent, DarkTheme.Background) >= 3, "the bar of the selected row");
        Assert.True(DarkTheme.Contrast(DarkTheme.Accent, DarkTheme.Surface) >= 3, "the outline of the highlighted menu item");
        Assert.True(DarkTheme.Contrast(DarkTheme.ControlBorder, DarkTheme.Background) >= 3, "the divider");
    }

    [Fact]
    public void SettingsSurviveARoundTripAndABrokenFile() => InTempFolder(folder => {
        var path = Path.Combine(folder, "sub", "settings.json");
        Assert.Equal((1.0, false, 0), (SettingsFile.Load(path).TextZoom, SettingsFile.Load(path).ShowTechnical, SettingsFile.Load(path).RecentFiles.Count));

        var settings = new AppSettings { TextZoom = 1.5, ShowTechnical = true, ListWidth = 320, Window = new WindowPlace(10, 20, 900, 600, true) };
        for (var i = 0; i < AppSettings.MostRecentFiles + 3; i++) settings.Remember($@"C:\roms\{i}.bin");
        settings.Remember(@"c:\ROMS\5.bin");
        Assert.Equal(AppSettings.MostRecentFiles, settings.RecentFiles.Count);
        Assert.Equal([@"c:\ROMS\5.bin", @"C:\roms\10.bin", @"C:\roms\9.bin"], settings.RecentFiles.Take(3));
        settings.Forget(@"C:\roms\10.bin");
        Assert.DoesNotContain(@"C:\roms\10.bin", settings.RecentFiles);

        Assert.True(SettingsFile.Save(path, settings));
        var read = SettingsFile.Load(path);
        Assert.Equal((1.5, true, 320, settings.Window), (read.TextZoom, read.ShowTechnical, read.ListWidth, read.Window));
        Assert.Equal(settings.RecentFiles, read.RecentFiles);

        File.WriteAllText(path, "{ \"TextZoom\": 40, \"ListWidth\": 3, \"RecentFiles\": [\"\", \"a\", \"A\"] }");
        var mended = SettingsFile.Load(path);
        Assert.Equal((AppSettings.LargestZoom, 160), (mended.TextZoom, mended.ListWidth));
        Assert.Equal(["a"], mended.RecentFiles);
        File.WriteAllText(path, "not json");
        Assert.Equal(1.0, SettingsFile.Load(path).TextZoom);
    });

    [Fact]
    public void TheWindowShowsTheFileItOpens() => OnUiThread(() => InTempFolder(folder => {
        var path = Path.Combine(folder, "mini.bin");
        DefinitionPackTests.MiniRom().Save(path);
        var settings = new AppSettings();
        using var form = new MainForm(Packs, [], settings);
        var split = Assert.IsType<SplitContainer>(form.Tree.Parent!.Parent);

        Assert.Equal("Honda ROM Editor", form.Text);
        Assert.True(split.Panel1Collapsed, "with no file open the page has the whole window");
        Assert.Equal(["No file open"], form.StatusTexts);
        Assert.Equal("Honda ROM Editor", form.Page.Page?.Title);

        form.OpenRom(path);
        Assert.Equal("mini.bin - Honda ROM Editor", form.Text);
        Assert.False(split.Panel1Collapsed);
        Assert.Equal(["Overview", "Fuel", "Ignition", "Limits"], form.Tree.Nodes.Cast<TreeNode>().Select(n => n.Text));
        Assert.Equal(["Fuel Map"], form.Tree.Nodes[1].Nodes.Cast<TreeNode>().Select(n => n.Text));
        Assert.All(form.Tree.Nodes.Cast<TreeNode>().SelectMany(n => n.Nodes.Cast<TreeNode>().Prepend(n)), n => Assert.False(string.IsNullOrEmpty(n.ToolTipText), n.Text));
        Assert.Equal(["mini.bin", "Mini test ROM", "Checksum wrong"], form.StatusTexts);
        Assert.Equal("Recognised as Mini test ROM", form.Page.Page?.Subtitle);
        Assert.Equal([path], settings.RecentFiles);

        Assert.True(form.SelectParam("Rev Limiter"));
        Assert.Equal("Rev Limiter", form.Page.Page?.Title);
        Assert.False(form.SelectParam("No such parameter"));

        // A file that cannot be opened is explained above the page; the open ROM stays.
        form.OpenRom(Path.Combine(folder, "gone.bin"));
        Assert.Equal(path, form.Session?.Path);
        Assert.Equal("gone.bin is no longer there", Assert.IsType<PageBlock.Notice>(form.Page.Page!.Blocks[0]).Heading);
        Assert.Equal("Rev Limiter", form.Page.Page.Title);
        Assert.Equal("mini.bin - Honda ROM Editor", form.Text);
        Assert.True(form.SelectParam("Fuel Map"));
        Assert.IsNotType<PageBlock.Notice>(form.Page.Page!.Blocks[0]);

        form.ReportFault(new InvalidOperationException("boom"));
        Assert.Equal("Something went wrong in Honda ROM Editor", Assert.IsType<PageBlock.Notice>(form.Page.Page!.Blocks[0]).Heading);

        form.CloseRom();
        Assert.Null(form.Session);
        Assert.Empty(form.Tree.Nodes);
        Assert.True(split.Panel1Collapsed);
        Assert.Equal("Honda ROM Editor", form.Text);
        Assert.Contains(form.Page.Page!.Blocks.OfType<PageBlock.Actions>(), a => a.Heading == "Recent files");
    }));

    [Fact]
    public void EveryCommandCanBeFoundReadAndReachedFromTheKeyboard() => OnUiThread(() => {
        using var form = new MainForm(Packs, [], new AppSettings());
        Assert.Equal("Tables and settings", form.Tree.AccessibleName);
        Assert.Equal("Details", form.Page.AccessibleName);
        var menu = form.MainMenuStrip!;
        Assert.Equal("Menu", menu.AccessibleName);
        Assert.Equal(["&File", "&View", "&Help"], menu.Items.Cast<ToolStripMenuItem>().Select(i => i.Text));

        static char AccessKey(ToolStripItem item) => char.ToUpperInvariant(item.Text![item.Text!.IndexOf('&') + 1]);
        foreach (ToolStripMenuItem top in menu.Items) {
            var items = top.DropDownItems.OfType<ToolStripMenuItem>().ToList();
            Assert.All(items, i => Assert.Contains('&', i.Text!));
            Assert.Equal(items.Count, items.Select(AccessKey).Distinct().Count());
            // Each says what it does: read out by a screen reader, shown in the status bar.
            Assert.All(items, i => Assert.False(string.IsNullOrEmpty(i.AccessibleDescription), i.Text));
        }
        var shortcuts = menu.Items.Cast<ToolStripMenuItem>().SelectMany(t => t.DropDownItems.OfType<ToolStripMenuItem>()).Select(i => i.ShortcutKeys).Where(k => k != Keys.None).ToList();
        Assert.Equal(shortcuts.Count, shortcuts.Distinct().Count());
        Assert.Contains(Keys.Control | Keys.O, shortcuts);
        Assert.Contains(Keys.F1, shortcuts);
    });

    [Fact]
    public void ThePageIsMadeOfControlsAScreenReaderAndTheKeyboardCanUse() => OnUiThread(() => {
        var session = MiniSession();
        using var view = new PageView { Size = new Size(700, 500) };
        view.Page = Details.Rom(session) with { Blocks = [.. Details.Problem(new OpenResult.Refused(OpenProblem.NotFound, @"C:\roms\gone.bin"), 0x8000), .. Details.Rom(session).Blocks] };

        var labels = view.Controls.OfType<Label>().Select(l => l.Text).ToList();
        Assert.Contains("mini.bin", labels);
        Assert.Contains("About this file", labels);
        Assert.DoesNotContain("Technical details", labels);

        // Values are fields: reached with Tab, named after their row, selectable and copyable, never editable.
        var values = view.Controls.OfType<TextBox>().ToList();
        Assert.Contains(values, v => v.AccessibleName == "Kind of ROM" && v.Text == "Mini test ROM");
        Assert.All(values, v => Assert.True(v.ReadOnly && v.TabStop && !string.IsNullOrEmpty(v.AccessibleName), v.Text));

        var notices = view.Controls.OfType<Panel>().ToList();
        Assert.Contains(notices, n => n.AccessibleName == "gone.bin is no longer there" && n.AccessibleRole == AccessibleRole.Alert && n.AccessibleDescription!.Length > 0);
        Assert.Contains(notices, n => n.AccessibleName == "The checksum of this file is wrong" && n.AccessibleRole == AccessibleRole.Alert);

        var buttons = view.Controls.OfType<Button>().Select(b => b.Text).ToList();
        Assert.Equal(["Choose another file...", "Dismiss", "Show technical details"], buttons);
        PageAction? pressed = null;
        view.ActionInvoked += action => pressed = action;
        view.Controls.OfType<Button>().First().PerformClick();
        Assert.Equal(PageCommand.OpenFile, pressed?.Command);

        // Nothing overlaps, and the tab order runs down the page.
        var placed = view.Controls.Cast<Control>().OrderBy(c => c.TabIndex).ToList();
        Assert.All(placed, c => Assert.True(c.Width > 0 && c.Height > 0, c.Text));
        Assert.All(placed.Zip(placed.Skip(1)), pair => Assert.True(pair.Second.Top >= pair.First.Top, $"`{pair.Second.Text}` is above `{pair.First.Text}`"));
        var fields = placed.Where(c => c is TextBox or Button or Panel).ToList();
        Assert.All(fields.SelectMany((a, i) => fields.Skip(i + 1).Select(b => (a, b))), pair =>
            Assert.False(pair.a.Bounds.IntersectsWith(pair.b.Bounds), $"`{pair.a.Text}` and `{pair.b.Text}` overlap"));

        var toggled = 0;
        view.TechnicalToggled += () => toggled++;
        view.Controls.OfType<Button>().Last().PerformClick();
        Assert.Equal(1, toggled);
        view.ShowTechnical = true;
        Assert.Contains("Technical details", view.Controls.OfType<Label>().Select(l => l.Text));
        Assert.Contains(view.Controls.OfType<TextBox>(), v => v.AccessibleName == "SHA-256" && v.Text == session.Image.Sha256());
        Assert.Equal("Hide technical details", view.Controls.OfType<Button>().Last().Text);

        // Larger text makes a taller page, not a clipped one.
        var before = view.Controls.Cast<Control>().Max(c => c.Bottom);
        using var large = new Font(view.Font.FontFamily, view.Font.SizeInPoints * 2f);
        view.Font = large;
        Assert.True(view.Controls.Cast<Control>().Max(c => c.Bottom) > before * 3 / 2);
        Assert.All(view.Controls.OfType<TextBox>(), v => Assert.True(v.Height >= large.Height, v.AccessibleName));
    });
}
