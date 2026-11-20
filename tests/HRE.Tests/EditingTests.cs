using System.Drawing;
using System.Windows.Forms;
using HRE.App;
using HRE.Core;
using HRE.Definitions;

namespace HRE.Tests;

/// <summary>
/// Working on a ROM from opening to saving: undoing and redoing, changing a setting, saving with
/// the checksum corrected and the first state of the file kept, and the one question the
/// program asks before changes are lost.
/// </summary>
public class EditingTests {
    private static readonly DefinitionPack[] Packs = [DefinitionPackTests.Mini()];

    private static void OnUiThread(Action body) => AppShellTests.OnUiThread(body);

    /// <summary>A folder of its own holding the mini ROM as <c>mini.bin</c>, its checksum wrong as the fixture has it; removed afterwards.</summary>
    private static void WithMiniFile(Action<string, string> body) {
        var folder = Path.Combine(Path.GetTempPath(), $"hre-edit-{Guid.NewGuid():N}");
        Directory.CreateDirectory(folder);
        try {
            var path = Path.Combine(folder, "mini.bin");
            DefinitionPackTests.MiniRom().Save(path);
            body(folder, path);
        } finally {
            foreach (var file in Directory.GetFiles(folder)) File.SetAttributes(file, FileAttributes.Normal);
            Directory.Delete(folder, recursive: true);
        }
    }

    private static RomSession Open(string path) => Assert.IsType<OpenResult.Opened>(RomSession.Open(path, Packs)).Session;

    private static byte[] Raise(RomSession session) {
        session.Document.Apply("Raise Fuel Map", TableEdit.Step(session.Image, session.Defs[0], [new CellRef(0, 0)], 1));
        return session.Image.ToArray();
    }

    [Fact]
    public void ASettingIsReadAndWrittenInItsOwnUnits() {
        var session = AppShellTests.MiniSession();
        var (image, limiter) = (session.Image, session.Defs[2]);
        Assert.True(ValueEdit.CanChange(limiter));
        Assert.Equal(("9000", (3000f, 11000f), 0, 1f), (ValueEdit.Text(image, limiter), ValueEdit.Range(limiter), ValueEdit.Decimals(limiter), ValueEdit.Step(limiter)));
        // Low byte first: 9500 is 0x251C.
        Assert.Equal([(0x300, (byte)0x1C), (0x301, (byte)0x25)], ValueEdit.Set(limiter, 9500.4f));
        Assert.Equal(((ushort)11000, (ushort)3000), (ValueEdit.Stored(limiter, 20000f), ValueEdit.Stored(limiter, -5f)));
        Assert.True(ValueEdit.Fits(limiter, 11000.4f));
        Assert.False(ValueEdit.Fits(limiter, 11001f));
        Assert.Equal([(0x300, (byte)0x29), (0x301, (byte)0x23)], ValueEdit.Move(image, limiter, 1));

        // A quarter of a unit per step: two decimals, and what is typed goes to the nearest step.
        var fine = limiter with { Scale = 0.25f, Bias = 10f };
        Assert.Equal(("2260", 2, (760f, 2760f)), (ValueEdit.Text(image, fine), ValueEdit.Decimals(fine), ValueEdit.Range(fine)));
        Assert.Equal((ushort)4001, ValueEdit.Stored(fine, 1010.3f));
        // Where a bigger stored value shows as a smaller number, raising still raises what is shown.
        var falling = limiter with { Scale = -1f, Bias = 20000f };
        Assert.Equal((9000f, 17000f), ValueEdit.Range(falling));
        Assert.Equal([(0x300, (byte)0x27), (0x301, (byte)0x23)], ValueEdit.Move(image, falling, 1));

        var locked = limiter with { Kind = new ParamKind.Word(9000, 9000) };
        Assert.False(ValueEdit.CanChange(locked));
        Assert.Empty(ValueEdit.Set(locked, 5000f));
        Assert.Empty(ValueEdit.Move(image, locked, 1));
    }

    [Fact]
    public void SavingCorrectsTheChecksumAndKeepsTheFileAsItFirstWas() => WithMiniFile((folder, path) => {
        var original = File.ReadAllBytes(path);
        var session = Open(path);
        Assert.Equal(ChecksumState.Wrong, session.Checksum);
        var changed = Raise(session);

        var first = Assert.IsType<SaveResult.Saved>(session.Save(path));
        Assert.Equal((path, path + ".bak", true), (first.Path, first.Backup, first.ChecksumCorrected));
        Assert.Equal(original, File.ReadAllBytes(path + ".bak"));
        var written = File.ReadAllBytes(path);
        Assert.Equal(changed[0x100], written[0x100]);
        Assert.True(Checksum.IsValid(new RomImage(written), Packs[0].Checksum));
        // The document is what the file holds, the corrected checksum included, and that is no step to undo.
        Assert.Equal(written, session.Image.ToArray());
        Assert.Equal((ChecksumState.Valid, false, "Raise Fuel Map"), (session.Checksum, session.Document.IsDirty, session.Document.UndoLabel));
        Assert.Equal(["mini.bin", "mini.bin.bak"], Directory.GetFiles(folder).Select(Path.GetFileName).Order());

        // The copy is made once: it stays the file as it first was.
        Raise(session);
        var second = Assert.IsType<SaveResult.Saved>(session.Save(path));
        Assert.Equal((null, true), (second.Backup, second.ChecksumCorrected));
        Assert.Equal(original, File.ReadAllBytes(path + ".bak"));
        Assert.Equal(session.Image.ToArray(), File.ReadAllBytes(path));
        // Nothing changed since: the same bytes go out again.
        Assert.False(Assert.IsType<SaveResult.Saved>(session.Save(path)).ChecksumCorrected);

        // Under another name: that file becomes the file of the session, and the first is left as it is.
        var before = File.ReadAllBytes(path);
        Raise(session);
        var other = Path.Combine(folder, "tuned.bin");
        var third = Assert.IsType<SaveResult.Saved>(session.Save(other));
        Assert.Equal((other, null, other, "tuned.bin"), (third.Path, third.Backup, session.Path, session.FileName));
        Assert.Equal(before, File.ReadAllBytes(path));
        Assert.Equal(session.Image.ToArray(), File.ReadAllBytes(other));
        Assert.Equal(ChecksumState.Valid, Open(other).Checksum);
        Assert.DoesNotContain(Directory.GetFiles(folder), f => f.EndsWith(".tmp", StringComparison.Ordinal));
    });

    [Fact]
    public void AFileThatCannotBeWrittenIsLeftAloneAndTheChangesStay() => WithMiniFile((folder, path) => {
        var original = File.ReadAllBytes(path);
        var session = Open(path);
        var changed = Raise(session);
        SaveProblem Problem(string to) => Assert.IsType<SaveResult.Refused>(session.Save(to)).Problem;

        using (File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None)) Assert.Equal(SaveProblem.InUse, Problem(path));
        File.SetAttributes(path, FileAttributes.ReadOnly);
        Assert.Equal(SaveProblem.Denied, Problem(path));
        File.SetAttributes(path, FileAttributes.Normal);
        Assert.Equal(SaveProblem.BadName, Problem(""));
        Assert.Equal(SaveProblem.BadName, Problem(folder));
        var nowhere = Assert.IsType<SaveResult.Refused>(session.Save(Path.Combine(folder, "no-such-folder", "mini.bin")));
        Assert.Equal((SaveProblem.Unwritable, "mini.bin"), (nowhere.Problem, nowhere.FileName));
        Assert.False(string.IsNullOrEmpty(nowhere.SystemMessage));

        Assert.Equal(original, File.ReadAllBytes(path));
        Assert.Equal(["mini.bin"], Directory.GetFiles(folder).Select(Path.GetFileName));
        Assert.Equal((path, true, ChecksumState.Wrong), (session.Path, session.Document.IsDirty, session.Checksum));
        Assert.Equal(changed, session.Image.ToArray());
    });

    [Fact]
    public void EverySaveProblemIsToldInPlainWordsWithAWayOn() {
        foreach (var problem in Enum.GetValues<SaveProblem>()) {
            var blocks = Details.Problem(new SaveResult.Refused(problem, @"C:\roms\tune.bin", "Access to the path is denied."));
            var notice = Assert.IsType<PageBlock.Notice>(blocks[0]);
            Assert.Equal(Severity.Problem, notice.Severity);
            // What happened, that nothing is lost, and what to do; never the machine's words for it.
            Assert.Contains("Your changes are still here.", notice.Body);
            Assert.DoesNotContain("denied.", notice.Heading + notice.Body);
            Assert.DoesNotMatch("(?i)error|invalid|failed|exception", notice.Heading + notice.Body);
            var actions = Assert.Single(blocks.OfType<PageBlock.Actions>()).Items;
            Assert.Equal(new PageAction("Save as...", PageCommand.SaveAs, Primary: true), actions[0]);
            Assert.Equal(PageCommand.Dismiss, actions[^1].Command);
            var technical = Assert.Single(blocks.OfType<PageBlock.Section>());
            Assert.True(technical.Technical);
            Assert.Equal([@"C:\roms\tune.bin", "Access to the path is denied."], technical.Rows.Select(r => r.Value));
        }
        Assert.Equal("Another program is using tune.bin", ((PageBlock.Notice)Details.Problem(new SaveResult.Refused(SaveProblem.InUse, @"C:\roms\tune.bin"))[0]).Heading);

        var saved = new SaveResult.Saved(@"C:\roms\tune.bin", @"C:\roms\tune.bin.bak", true);
        Assert.Equal("Saved tune.bin. The checksum was corrected. The file as it was before is kept as tune.bin.bak.", Details.Said(saved));
        Assert.Equal("Saved tune.bin.", Details.Said(saved with { Backup = null, ChecksumCorrected = false }));
        var kept = Assert.IsType<PageBlock.Notice>(Details.KeptCopy(saved)[0]);
        Assert.Equal((Severity.Good, "Saved tune.bin"), (kept.Severity, kept.Heading));
        Assert.Contains("tune.bin.bak", kept.Body);
    }

    [Fact]
    public void TheWindowUndoesRedoesAndSaves() => OnUiThread(() => WithMiniFile((folder, path) => {
        using var form = new MainForm(Packs, [], new AppSettings());
        var menu = form.MainMenuStrip!.Items.Cast<ToolStripMenuItem>().SelectMany(t => t.DropDownItems.OfType<ToolStripMenuItem>()).ToList();
        ToolStripMenuItem Command(Keys shortcut) => menu.Single(i => i.ShortcutKeys == shortcut);
        var (undo, redo, save, saveAs) = (Command(Keys.Control | Keys.Z), Command(Keys.Control | Keys.Y), Command(Keys.Control | Keys.S), Command(Keys.Control | Keys.Shift | Keys.S));
        Assert.Equal((false, false, false, false), (undo.Enabled, redo.Enabled, save.Enabled, saveAs.Enabled));
        Assert.False(form.Save());

        form.OpenRom(path);
        Assert.Equal((false, false, true, true), (undo.Enabled, redo.Enabled, save.Enabled, saveAs.Enabled));
        Assert.True(form.SelectParam("Fuel Map"));
        var grid = form.Page.Grid!;
        grid.ClearSelection();
        grid[0, 0].Selected = true;
        grid.TypeValue(0, 0, "40");

        // A change shows in the title and the status bar, and the command that takes it back names it.
        Assert.Equal("mini.bin (changes not saved) - Honda ROM Editor", form.Text);
        Assert.Equal(["mini.bin", "Changes not saved", "Mini test ROM", "Checksum wrong"], form.StatusTexts);
        Assert.Equal(("&Undo Change Fuel Map", true, "&Redo", false), (undo.Text, undo.Enabled, redo.Text, redo.Enabled));

        undo.PerformClick();
        Assert.Equal((1, "mini.bin - Honda ROM Editor"), (form.Session!.Image[0x100], form.Text));
        Assert.Equal(("&Undo", false, "&Redo Change Fuel Map", true), (undo.Text, undo.Enabled, redo.Text, redo.Enabled));
        Assert.Equal("1", grid[0, 0].Value);
        // What the command did is on the line under the cells, for those who do not hear it.
        Assert.Contains("Undone: Change Fuel Map", form.Page.Controls.OfType<Label>().Select(l => l.Text));
        redo.PerformClick();
        Assert.Equal((40, "40"), ((int)form.Session.Image[0x100], (string)grid[0, 0].Value!));
        Assert.Contains("Redone: Change Fuel Map", form.Page.Controls.OfType<Label>().Select(l => l.Text));

        // The first save puts the file as it was aside and says so above the page; the grid stays.
        save.PerformClick();
        Assert.Equal(40, File.ReadAllBytes(path)[0x100]);
        Assert.Equal(1, File.ReadAllBytes(path + ".bak")[0x100]);
        Assert.Equal("mini.bin - Honda ROM Editor", form.Text);
        Assert.Equal(["mini.bin", "Mini test ROM", "Checksum correct"], form.StatusTexts);
        Assert.Equal("Saved mini.bin", Assert.IsType<PageBlock.Notice>(form.Page.Page!.Blocks[0]).Heading);
        Assert.Same(grid, form.Page.Grid);
        // Saved is not the end of undoing.
        Assert.True(undo.Enabled);

        grid.Nudge(1);
        Assert.True(form.Save());
        Assert.IsType<PageBlock.Grid>(form.Page.Page!.Blocks[0]);
        Assert.Equal(41, File.ReadAllBytes(path)[0x100]);

        // Under another name: the window goes on in that file.
        var other = Path.Combine(folder, "tuned.bin");
        grid.Nudge(1);
        Assert.True(form.SaveTo(other));
        Assert.Equal(("tuned.bin - Honda ROM Editor", other), (form.Text, form.Session.Path));
        Assert.Equal((42, 41), (File.ReadAllBytes(other)[0x100], File.ReadAllBytes(path)[0x100]));
        Assert.Contains("tuned.bin", form.StatusTexts);

        // A file that cannot be written is explained above the page, and the changes stay.
        grid.Nudge(1);
        using (File.Open(other, FileMode.Open, FileAccess.Read, FileShare.None)) Assert.False(form.Save());
        Assert.Equal("Another program is using tuned.bin", Assert.IsType<PageBlock.Notice>(form.Page.Page!.Blocks[0]).Heading);
        Assert.Contains("Save as...", form.Page.Controls.OfType<Button>().Select(b => b.Text));
        Assert.Equal("tuned.bin (changes not saved) - Honda ROM Editor", form.Text);
        Assert.Equal(43, form.Session.Image[0x100]);
    }));

    [Fact]
    public void WhileANumberIsTypedUndoBelongsToTheTyping() => OnUiThread(() => WithMiniFile((folder, path) => {
        using var form = new MainForm(Packs, [], new AppSettings());
        _ = form.Handle;
        form.OpenRom(path);
        Assert.True(form.SelectParam("Fuel Map"));
        var grid = form.Page.Grid!;
        _ = grid.Handle;
        grid.TypeValue(0, 0, "40");
        grid.CurrentCell = grid[1, 1];
        Assert.True(grid.TypeChar('7'));
        form.Undo();
        form.Redo();
        Assert.Equal((40, "Change Fuel Map", true), (form.Session!.Image[0x100], form.Session.Document.UndoLabel, grid.IsCurrentCellInEditMode));

        // Saving takes what is being typed with it.
        Assert.True(form.Save());
        // The column shows twice what it stores: 7 is nearest to 4 x 2.
        Assert.Equal((40, 4), (File.ReadAllBytes(path)[0x100], File.ReadAllBytes(path)[0x104]));
    }));

    [Fact]
    public void ChangesAreNotLostWithoutBeingAskedFor() => OnUiThread(() => WithMiniFile((folder, path) => {
        var other = Path.Combine(folder, "other.bin");
        File.Copy(path, other);
        using var form = new MainForm(Packs, [], new AppSettings());
        _ = form.Handle;
        var asked = new List<string>();
        var answer = ChangesAnswer.Cancel;
        form.AskAboutChanges = name => {
            asked.Add(name);
            return answer;
        };
        void Change() {
            Assert.True(form.SelectParam("Fuel Map"));
            var grid = form.Page.Grid!;
            grid.ClearSelection();
            grid[0, 0].Selected = true;
            grid.Nudge(1);
        }

        // Nothing changed, nothing asked.
        form.OpenRom(path);
        form.OpenRom(other);
        Assert.True(form.CloseRom());
        Assert.Empty(asked);

        // Going back keeps the ROM and its changes, whichever way it was about to be left.
        form.OpenRom(path);
        Change();
        Assert.False(form.CloseRom());
        form.OpenRom(other);
        form.Close();
        Assert.Equal(["mini.bin", "mini.bin", "mini.bin"], asked);
        Assert.False(form.IsDisposed);
        Assert.Equal((path, true), (form.Session?.Path, form.Session?.Document.IsDirty));
        // A file that would not open anyway is no reason to ask.
        form.OpenRom(Path.Combine(folder, "gone.bin"));
        Assert.Equal(3, asked.Count);

        // Saving first, then leaving.
        answer = ChangesAnswer.Save;
        form.OpenRom(other);
        Assert.Equal((other, 2), (form.Session?.Path, File.ReadAllBytes(path)[0x100]));

        // A save that does not go through keeps the ROM open.
        Change();
        using (File.Open(other, FileMode.Open, FileAccess.Read, FileShare.None)) Assert.False(form.CloseRom());
        Assert.Equal(other, form.Session?.Path);

        // Giving the changes up.
        answer = ChangesAnswer.Discard;
        Assert.True(form.CloseRom());
        Assert.Equal(1, File.ReadAllBytes(other)[0x100]);
        form.OpenRom(path);
        Change();
        form.Close();
        Assert.True(form.IsDisposed);
        Assert.Equal(2, File.ReadAllBytes(path)[0x100]);
    }));

    [Fact]
    public void TheQuestionOffersSavingFirstAndGoingBackOnEsc() => OnUiThread(() => {
        using var font = new Font(SystemFonts.MessageBoxFont!.FontFamily, 9f);
        using var ask = new ChangesForm("mini.bin", font);
        var buttons = ask.Controls.Cast<Control>().SelectMany(c => c.Controls.Cast<Control>()).SelectMany(c => c.Controls.OfType<Button>()).ToList();
        Assert.Equal(["&Save", "&Don't save", "Cancel"], buttons.Select(b => b.Text));
        Assert.All(buttons, b => Assert.False(string.IsNullOrEmpty(b.AccessibleDescription), b.Text));
        // Enter saves: nothing is lost by pressing a key too soon.
        Assert.Same(buttons[0], ask.AcceptButton);
        Assert.Same(buttons[2], ask.CancelButton);
        Assert.Equal("Save the changes to mini.bin? If you do not save them, they are lost.", ask.AccessibleDescription);
        // Closed without a choice, it goes back to the ROM.
        Assert.Equal(ChangesAnswer.Cancel, ask.Answer);
        // The dialog is not shown here, so the buttons are pressed from the inside.
        void Press(Button button) => typeof(Control).GetMethod("OnClick", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(button, [EventArgs.Empty]);
        Press(buttons[1]);
        Assert.Equal(ChangesAnswer.Discard, ask.Answer);
        Press(buttons[0]);
        Assert.Equal(ChangesAnswer.Save, ask.Answer);
    });

    [Fact]
    public void ASettingIsChangedInAFieldThatNamesItsRange() => OnUiThread(() => {
        var session = AppShellTests.MiniSession();
        var (document, limiter) = (session.Document, session.Defs[2]);
        using var box = new SettingBox(document, limiter);
        var said = new List<string>();
        box.Announced += said.Add;
        Assert.Equal(("9000", 3000m, 11000m, 1m), (box.Text, box.Minimum, box.Maximum, box.Increment));
        Assert.Equal(("Rev Limiter", "From 3000 to 11000 rpm"), (box.AccessibleName, box.AccessibleDescription));

        // What Enter hands over.
        box.Text = "9500";
        Assert.Equal((9500, "Change Rev Limiter", "Set to 9500 rpm"), (ParamAccess.Word(document.Image, limiter), document.UndoLabel, said[^1]));
        box.Text = "20000";
        Assert.Equal(("11000", "It can be set from 3000 to 11000 rpm. It was set to 11000 rpm."), (box.Text, said[^1]));
        box.UpButton();
        Assert.Equal("Already at the largest value it can be set to.", said[^1]);
        box.DownButton();
        Assert.Equal(("10999", "10999 rpm", "Lower Rev Limiter"), (box.Text, said[^1], document.UndoLabel));
        box.Text = "fast";
        Assert.Equal(("10999", "\"fast\" is not a number, so nothing was changed. Type a number such as 10999."), (box.Text, said[^1]));
        // An emptied field is a change of mind.
        var count = said.Count;
        box.Text = "";
        box.FinishTyping();
        Assert.Equal(("10999", count), (box.Text, said.Count));

        // Undoing, which changes the document behind the field, shows at once.
        document.Undo();
        Assert.Equal(("11000", 11000m), (box.Text, box.Value));

        // A point as the decimal mark whatever the language of Windows; a comma is understood.
        using var fine = new SettingBox(document, limiter with { Scale = 0.25f });
        Assert.Equal(("2750", 2, 0.25m), (fine.Text, fine.DecimalPlaces, fine.Increment));
        fine.Text = "1000,3";
        Assert.Equal(("1000.25", 4001), (fine.Text, ParamAccess.Word(document.Image, limiter)));

        // A value outside what the description allows is shown as it is.
        var odd = new RomDocument(document.Image.With(ParamAccess.WordWrites(limiter, 12000)));
        using var outside = new SettingBox(odd, limiter);
        Assert.Equal(("12000", 12000m), (outside.Text, outside.Maximum));

        box.ReadOnly = true;
        var before = document.Image;
        box.TypeValue("5000");
        box.Nudge(1);
        Assert.Same(before, document.Image);
    });

    [Fact]
    public void TheSettingPageStartsWithItsFieldAndKeepsItWhileTheValueChanges() => OnUiThread(() => {
        var session = AppShellTests.MiniSession();
        var limiter = session.Defs[2];
        var page = Details.Param(session.Document, limiter);
        Assert.IsType<PageBlock.Setting>(page.Blocks[0]);
        Assert.Equal(("3000 to 11000 rpm", "1 rpm"), (AppShellTests.Value(page, "Range"), AppShellTests.Value(page, "Step")));

        using var view = new PageView { Size = new Size(700, 500) };
        view.Page = page;
        var box = Assert.IsType<SettingBox>(view.Setting);
        Assert.Null(view.Grid);
        // Only the name of the setting and its group stand above the field; its unit stands beside it.
        Assert.All(view.Controls.Cast<Control>().Where(c => c != box && c.Bottom <= box.Top), c => Assert.True(c.Text is "Rev Limiter" or "Limits", c.Text));
        var unit = view.Controls.OfType<Label>().Single(l => l.Text == "rpm");
        Assert.True(unit.Left >= box.Right && unit.Top < box.Bottom && unit.Bottom > box.Top);
        Assert.True(view.Controls.OfType<Label>().Single(l => l.Text == Details.SettingHelp).Top >= box.Bottom);
        Assert.True(view.FocusFirst() || !view.Visible);
        Assert.Equal(0, view.Controls.Cast<Control>().Where(c => c.TabStop).Min(c => c.TabIndex) - box.TabIndex);

        var heard = new List<string>();
        view.Announced += heard.Add;
        session.Document.Changed += () => view.Revise(Details.Param(session.Document, limiter));
        box.Text = "8000";
        Assert.Same(box, view.Setting);
        Assert.False(box.IsDisposed);
        Assert.Equal(["Set to 8000 rpm"], heard);
        Assert.Contains("Set to 8000 rpm", view.Controls.OfType<Label>().Select(l => l.Text));
        view.Say("Undone: Change Rev Limiter");
        Assert.Contains("Undone: Change Rev Limiter", view.Controls.OfType<Label>().Select(l => l.Text));

        // A value that is fixed is told, not offered.
        var locked = limiter with { Kind = new ParamKind.Word(9000, 9000) };
        view.Page = Details.Param(session.Document, locked);
        Assert.True(box.IsDisposed);
        Assert.Null(view.Setting);
        Assert.Contains(view.Controls.OfType<TextBox>(), v => v.AccessibleName == "Can be changed" && v.Text == "No: this value is fixed");
    });
}
