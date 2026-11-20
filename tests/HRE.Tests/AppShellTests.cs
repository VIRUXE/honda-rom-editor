using System.Drawing;
using System.Windows.Forms;
using HRE.App;
using HRE.Core;
using HRE.Definitions;

namespace HRE.Tests;

/// <summary>The application shell: opening a ROM, the parameter tree, the detail pages, the theme and the main window.</summary>
public class AppShellTests {
    private static readonly DefinitionPack[] Packs = [DefinitionPackTests.Mini()];

    private static RomSession MiniSession(RomImage? image = null) => RomSession.Open(@"C:\roms\mini.bin", image ?? DefinitionPackTests.MiniRom(), Packs);

    private static string Value(DetailPage page, string label) => page.Sections.SelectMany(s => s.Rows).Single(r => r.Label == label).Value;

    /// <summary>Runs on a thread of its own that may own windows; what it throws is thrown here.</summary>
    private static void OnUiThread(Action body) {
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
        Assert.Equal("mini", session.Pack?.Id);
        Assert.Empty(session.OtherMatches);
        Assert.Equal(["Fuel Map", "Wide Map", "Rev Limiter"], session.Defs.Select(d => d.Name));
        Assert.Empty(session.Problems);
        Assert.Equal("mini.bin", session.FileName);
        Assert.False(session.Document.IsDirty);
    }

    [Fact]
    public void TheChecksumIsJudgedByThePack() {
        var rom = DefinitionPackTests.MiniRom();
        Assert.Equal(ChecksumState.Wrong, MiniSession(rom).Checksum);
        Assert.Equal(ChecksumState.Valid, MiniSession(Checksum.Fix(rom, Packs[0].Checksum)).Checksum);
        var unchecked_ = Packs[0] with { Checksum = new ChecksumSpec(ChecksumKind.None, 0, 0, 0) };
        Assert.Equal(ChecksumState.None, RomSession.Open("mini.bin", rom, [unchecked_]).Checksum);
    }

    [Fact]
    public void AnUnrecognisedRomOpensWithoutParameters() {
        var session = MiniSession(DefinitionPackTests.Blank);
        Assert.Null(session.Pack);
        Assert.Empty(session.Defs);
        Assert.Equal(ChecksumState.Unknown, session.Checksum);
        var page = Details.Rom(session);
        Assert.Equal("No definition pack recognises this ROM.", page.Subtitle);
        Assert.Equal("32768 bytes (0x8000)", Value(page, "Size"));
    }

    [Fact]
    public void PacksThatMatchEquallyWellAreReported() {
        var twin = Packs[0] with { Id = "twin", Name = "Twin" };
        var session = RomSession.Open("mini.bin", DefinitionPackTests.MiniRom(), [Packs[0], twin]);
        Assert.Equal("mini", session.Pack?.Id);
        Assert.Equal(["twin"], session.OtherMatches.Select(p => p.Id));
        Assert.Contains(Details.Rom(session).Sections, s => s.Heading == "Also matches");
    }

    [Fact]
    public void TheTreeKeepsTheOrderOfTheDefinitions() {
        var defs = MiniSession().Defs;
        var extra = defs[0] with { Name = "Fuel Trim" };
        var tree = ParamTree.Build([.. defs, extra]);
        Assert.Equal(["Fuel", "Ignition", "Limits"], tree.Select(c => c.Name));
        Assert.Equal(["Fuel Map", "Fuel Trim"], tree[0].Params.Select(p => p.Name));
        Assert.Empty(ParamTree.Build([]));
    }

    [Fact]
    public void TheRomPageSaysWhatTheRomIs() {
        var session = MiniSession();
        var page = Details.Rom(session);
        Assert.Equal("mini.bin", page.Title);
        Assert.Equal("Mini test ROM", page.Subtitle);
        Assert.Equal(@"C:\roms\mini.bin", Value(page, "File"));
        Assert.Equal(session.Image.Sha256(), Value(page, "SHA-256"));
        Assert.Equal("OBD0, OKI 66k family", Value(page, "Platform"));
        Assert.Equal("Wrong: bytes 0x0000 to 0x3FFF (16384 bytes) sum to zero, corrected at 0x3FFF", Value(page, "Checksum"));
        Assert.Equal("stock, 9600 baud", Value(page, "Datalog"));
        Assert.Equal("Fuel Map", Value(page, "Fuel"));
        Assert.Equal("Not applied", Value(page, "Fast serial routine"));

        var patched = MiniSession(PatchEngine.Apply(Packs[0].Patches[0], session.Image));
        Assert.Equal("Applied", Value(Details.Rom(patched), "Fast serial routine"));
    }

    [Fact]
    public void ATablePageSaysWhereTheTableIsAndWhatItHolds() {
        var session = MiniSession();
        var page = Details.Param(session.Image, session.Defs[0]);
        Assert.Equal(("Fuel Map", "Fuel"), (page.Title, page.Subtitle));
        Assert.Equal("Table, 2 rows x 3 columns", Value(page, "Kind"));
        Assert.Equal("0x0100 to 0x0105 (6 bytes)", Value(page, "Address"));
        Assert.Equal("Cell x 2^byte, 0x0120 to 0x0122 (3 bytes)", Value(page, "Column multiplier"));
        // Cells 1..6, the columns multiplied by 1, 2 and 4.
        Assert.Equal("1 to 24 raw", Value(page, "Values"));

        var rows = page.Sections.Single(x => x.Heading == "Row axis").Rows.ToDictionary(r => r.Label, r => r.Value);
        Assert.Equal("RPM", rows["Label"]);
        Assert.Equal("Bytes, each the step from the breakpoint before, 0x00F0 to 0x00F1 (2 bytes)", rows["Stored"]);
        Assert.Equal("10 to 30", rows["Range"]);

        var wide = Details.Param(session.Image, session.Defs[1]);
        Assert.Equal("16-bit, low byte first", Value(wide, "Cell"));
        Assert.Equal("stored x 0.5", Value(wide, "Shown as"));
        Assert.Equal("Not in the ROM: fixed by the definition", wide.Sections.Single(x => x.Heading == "Column axis").Rows.Single(r => r.Label == "Stored").Value);
    }

    [Fact]
    public void AValuePageShowsTheValue() {
        var session = MiniSession();
        var page = Details.Param(session.Image, session.Defs[2]);
        Assert.Equal("Value", Value(page, "Kind"));
        Assert.Equal("0x0300 to 0x0301 (2 bytes)", Value(page, "Address"));
        Assert.Equal("9000 rpm (stored 9000)", Value(page, "Value"));
        Assert.Equal("3000 to 11000, as stored", Value(page, "Allowed"));
    }

    [Fact]
    public void ACategoryPageListsItsParameters() {
        var page = Details.Category(ParamTree.Build(MiniSession().Defs)[0]);
        Assert.Equal(("Fuel", "1 parameter"), (page.Title, page.Subtitle));
        Assert.Equal("Table, 2 x 3 raw", Value(page, "Fuel Map"));
        Assert.Equal("Value rpm", Value(Details.Category(ParamTree.Build(MiniSession().Defs)[2]), "Rev Limiter"));
    }

    [Fact]
    public void TheStartPageListsThePacksAndTheOnesThatFailed() {
        var page = Details.Start(Packs, [new ProjectException(ProjectErrorKind.Format, "bad.pack.toml", "missing `id`")]);
        Assert.Equal("Mini test ROM", Value(page, "mini"));
        Assert.Contains(page.Sections, s => s.Heading == "Packs that could not be read" && s.Rows.Count == 1);
        Assert.Single(Details.Start(Packs, []).Sections);
        Assert.Contains("No definition packs", Details.Start([], []).Sections[0].Rows[0].Value);
    }

    [Fact]
    public void TheThemeIsBlackAndReadable() {
        Assert.Equal(Color.Black.ToArgb(), DarkTheme.Background.ToArgb());
        Assert.Equal(21, DarkTheme.Contrast(Color.White, Color.Black), 3);
        // WCAG: 7 for body text, 4.5 for the rest that must be read, 3 for what is merely seen.
        Assert.True(DarkTheme.Contrast(DarkTheme.Text, DarkTheme.Background) >= 7);
        Assert.True(DarkTheme.Contrast(DarkTheme.Text, DarkTheme.Selection) >= 7);
        Assert.True(DarkTheme.Contrast(DarkTheme.Text, DarkTheme.Hover) >= 7);
        Assert.True(DarkTheme.Contrast(DarkTheme.TextDim, DarkTheme.Surface) >= 4.5);
        Assert.All([DarkTheme.Accent, DarkTheme.Good, DarkTheme.Warning, DarkTheme.Bad], c => Assert.True(DarkTheme.Contrast(c, DarkTheme.Surface) >= 4.5, c.ToString()));
        Assert.True(DarkTheme.Contrast(DarkTheme.TextDisabled, DarkTheme.Surface) >= 3);
    }

    [Fact]
    public void TheWindowShowsTheRomItOpens() => OnUiThread(() => {
        var path = Path.Combine(Path.GetTempPath(), $"hre-shell-{Guid.NewGuid():N}.bin");
        DefinitionPackTests.MiniRom().Save(path);
        try {
            using var form = new MainForm(Packs, []);
            Assert.Equal("Honda ROM Editor", form.Text);
            Assert.Empty(form.Tree.Nodes);
            Assert.Equal(["No ROM open. 1 definition pack loaded."], form.StatusTexts);
            Assert.Equal("Honda ROM Editor", form.Detail.Page?.Title);

            form.OpenRom(path);
            var name = Path.GetFileName(path);
            Assert.Equal($"{name} - Honda ROM Editor", form.Text);
            var root = Assert.Single(form.Tree.Nodes.Cast<TreeNode>());
            Assert.Equal(name, root.Text);
            Assert.Equal(["Fuel", "Ignition", "Limits"], root.Nodes.Cast<TreeNode>().Select(n => n.Text));
            Assert.Equal(["Fuel Map"], root.Nodes[0].Nodes.Cast<TreeNode>().Select(n => n.Text));
            Assert.Equal([path, "Mini test ROM", "3 parameters", "Checksum wrong"], form.StatusTexts);
            Assert.Equal(name, form.Detail.Page?.Title);

            Assert.True(form.SelectParam("Rev Limiter"));
            Assert.Equal("Rev Limiter", form.Detail.Page?.Title);
            Assert.False(form.SelectParam("No such parameter"));

            form.CloseRom();
            Assert.Null(form.Session);
            Assert.Empty(form.Tree.Nodes);
            Assert.Equal("Honda ROM Editor", form.Text);
        } finally {
            File.Delete(path);
        }
    });

    [Fact]
    public void AFileThatCannotBeReadIsReportedInTheWindow() => OnUiThread(() => {
        using var form = new MainForm(Packs, []);
        var missing = Path.Combine(Path.GetTempPath(), $"hre-missing-{Guid.NewGuid():N}.bin");
        form.OpenRom(missing);
        Assert.Null(form.Session);
        Assert.Equal("This file could not be opened.", form.Detail.Page?.Subtitle);
        Assert.Equal(["No ROM open. 1 definition pack loaded."], form.StatusTexts);

        // The ROM that is open stays open.
        var path = Path.Combine(Path.GetTempPath(), $"hre-shell-{Guid.NewGuid():N}.bin");
        DefinitionPackTests.MiniRom().Save(path);
        try {
            form.OpenRom(path);
            form.OpenRom(missing);
            Assert.Equal(path, form.Session?.Path);
            Assert.Equal("This file could not be opened.", form.Detail.Page?.Subtitle);
            Assert.Single(form.Tree.Nodes);
            Assert.True(form.SelectParam("Fuel Map"));
            Assert.Equal("Fuel Map", form.Detail.Page?.Title);
        } finally {
            File.Delete(path);
        }
    });
}
