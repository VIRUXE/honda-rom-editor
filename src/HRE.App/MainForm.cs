using System.Diagnostics;
using System.Windows.Forms.Automation;
using HRE.Core;
using HRE.Definitions;
using Microsoft.Win32;

namespace HRE.App;

/// <summary>The main window: menus, the list of tables and settings, the page that describes what is selected, the status bar.</summary>
/// <remarks>
/// The controls are laid out in <c>MainForm.Designer.cs</c>, which the Visual Studio designer
/// edits; what depends on the settings, the theme or the screen is applied here at run time.
/// </remarks>
public sealed partial class MainForm : Form {
    private const string AppName = "Honda ROM Editor";
    private const string GettingStartedUrl = "https://github.com/VIRUXE/honda-rom-editor/wiki/Application";
    private const string ReportUrl = "https://github.com/VIRUXE/honda-rom-editor/issues";
    private const double ZoomStep = 0.1;

    /// <summary>Lets Windows remember the folder of this dialog apart from the other file dialogs to come.</summary>
    private static readonly Guid OpenRomDialog = new("6f6f2a55-7c0b-4a5e-9a57-1d2f7f1c8e01");

    private static readonly Guid SaveRomDialog = new("6f6f2a55-7c0b-4a5e-9a57-1d2f7f1c8e02");

    private readonly IReadOnlyList<DefinitionPack> _packs;
    private readonly IReadOnlyList<ProjectException> _packErrors;
    private readonly AppSettings _settings;

    private IReadOnlyList<PageBlock>? _problem;
    private Font? _bodyFont;
    private Font? _groupFont;
    private Font? _barFont;

    public MainForm(IReadOnlyList<DefinitionPack> packs, IReadOnlyList<ProjectException> packErrors, AppSettings settings) {
        _packs = packs;
        _packErrors = packErrors;
        _settings = settings;

        AskAboutChanges = AskInDialog;
        InitializeComponent();
        // The name is the program's: the designer holds it too, in the title and the About command.
        Text = AppName;
        _aboutItem.Text = $"&About {AppName}";
        foreach (ToolStripMenuItem top in _menu.Items) {
            foreach (var item in top.DropDownItems.OfType<ToolStripMenuItem>()) Describe(item);
        }

        PlaceWindow();
        _split.SplitterWidth = LogicalToDeviceUnits(7);
        _split.Panel1MinSize = LogicalToDeviceUnits(160);

        _page.ActionInvoked += RunPageAction;
        _page.TechnicalToggled += ToggleTechnical;
        _page.Announced += Announce;
        _page.ShowTechnical = _settings.ShowTechnical;
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;

        ApplyTheme();
        ApplyTextSize();
        ShowSession();
    }

    /// <summary>The ROM that is open; null when none is.</summary>
    public RomSession? Session { get; private set; }

    internal TreeView Tree => _tree;
    internal PageView Page => _page;
    internal IEnumerable<string> StatusTexts => new[] { _statusFile, _statusChanges, _statusKind, _statusChecksum }.Where(l => l.Available).Select(l => l.Text ?? "");

    /// <summary>
    /// Asks what to do with the changes to the named file, which are not saved, before its ROM is
    /// left. A dialog asks the user; a test answers in its place.
    /// </summary>
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    internal Func<string, ChangesAnswer> AskAboutChanges { get; set; }

    /// <summary>
    /// Opens a ROM file, replacing the one that is open. A file that is missing, unreadable or
    /// not a ROM this version knows is explained at the top of the page, and what was open stays
    /// open. So it does when the open ROM has changes and the user chooses to go back to them.
    /// </summary>
    public void OpenRom(string path) {
        switch (RomSession.Open(path, _packs)) {
            case OpenResult.Opened opened:
                if (!LeaveRom()) break;
                Session = opened.Session;
                // What a page says of a table follows the table as it is changed; so do the commands that undo and save.
                Session.Document.Changed += () => {
                    _page.Revise(BuildPage());
                    ShowEditState();
                };
                _problem = null;
                _settings.Remember(Session.Path);
                ShowSession();
                Announce($"Opened {Session.FileName}. Recognised as {Session.Pack.Name}. {ChecksumWords(Session.Checksum)}.");
                break;
            case OpenResult.Refused refused:
                if (refused.Problem == OpenProblem.NotFound) _settings.Forget(refused.Path);
                // The start page has its own button to open a file; one is enough.
                _problem = Details.Problem(refused, _packs.Count == 0 ? 0 : _packs.Min(p => p.RomSize), offerOpen: Session is not null);
                ShowRecentFiles();
                ShowPage();
                _page.FocusFirst();
                if (_problem[0] is PageBlock.Notice notice) Announce($"{notice.Heading}. {notice.Body}");
                break;
        }
    }

    /// <summary>Closes the open ROM. Returns false when it has changes and the user chose to go back to them.</summary>
    public bool CloseRom() {
        if (!LeaveRom()) return false;
        Session = null;
        _problem = null;
        ShowSession();
        return true;
    }

    /// <summary>Saves the open ROM to its file. Returns false when nothing was saved; the page then says why.</summary>
    public bool Save() => Session is { } session && SaveTo(session.Path);

    /// <summary>Asks for a file name and saves the open ROM there. Returns false when nothing was saved.</summary>
    public bool SaveAs() {
        if (Session is not { } session) return false;
        using var dialog = new SaveFileDialog {
            Title = "Save the ROM as",
            Filter = "ROM files (*.bin)|*.bin|All files (*.*)|*.*",
            DefaultExt = "bin",
            AddExtension = true,
            OverwritePrompt = true,
            InitialDirectory = Path.GetDirectoryName(session.Path),
            FileName = session.FileName,
            ClientGuid = SaveRomDialog,
        };
        return dialog.ShowDialog(this) == DialogResult.OK && SaveTo(dialog.FileName);
    }

    /// <summary>Takes back the last change.</summary>
    public void Undo() {
        // While a number is being typed into a cell, the key belongs to the typing.
        if (_page.Grid is { IsCurrentCellInEditMode: true, EditingControl: TextBox typing }) {
            typing.Undo();
            return;
        }
        if (Session?.Document is not { UndoLabel: { } label } document) return;
        document.Undo();
        Tell($"Undone: {label}");
    }

    /// <summary>Makes the change again that was taken back last.</summary>
    public void Redo() {
        if (_page.Grid is { IsCurrentCellInEditMode: true }) return;
        if (Session?.Document is not { RedoLabel: { } label } document) return;
        document.Redo();
        Tell($"Redone: {label}");
    }

    /// <summary>
    /// Saves the open ROM to the given file, which becomes the file of the window. A file that
    /// cannot be written is explained at the top of the page, and the changes stay.
    /// </summary>
    internal bool SaveTo(string path) {
        if (Session is not { } session) return false;
        _page.FinishTyping();
        switch (session.Save(path)) {
            case SaveResult.Saved saved:
                _settings.Remember(saved.Path);
                _problem = saved.Backup is null ? null : Details.KeptCopy(saved);
                ShowRecentFiles();
                ShowEditState();
                ShowStatus();
                _page.Revise(BuildPage());
                Tell(Details.Said(saved));
                return true;
            case SaveResult.Refused refused:
                _problem = Details.Problem(refused);
                ShowPage();
                _page.FocusFirst();
                if (_problem[0] is PageBlock.Notice notice) Announce($"{notice.Heading}. {notice.Body}");
                return false;
            default:
                return false;
        }
    }

    /// <summary>Selects a table or a setting in the list by name. Returns false when the open ROM has none of that name.</summary>
    public bool SelectParam(string name) {
        var node = _tree.Nodes.Cast<TreeNode>().SelectMany(Descendants).FirstOrDefault(n => n.Tag is ParamDef def && def.Name == name);
        if (node is null) return false;
        _tree.SelectedNode = node;
        node.EnsureVisible();
        ShowPage();
        return true;
    }

    /// <summary>Tells the user that the program itself went wrong, at the top of the page, and keeps running.</summary>
    public void ReportFault(Exception fault) {
        _problem = Details.Fault(fault);
        ShowPage();
        Announce("Something went wrong in Honda ROM Editor. Your files were not changed.");
    }

    protected override void OnShown(EventArgs e) {
        base.OnShown(e);
        if (Session is null) _page.FocusFirst();
    }

    protected override void OnDragEnter(DragEventArgs drgevent) {
        base.OnDragEnter(drgevent);
        drgevent.Effect = drgevent.Data?.GetDataPresent(DataFormats.FileDrop) == true ? DragDropEffects.Copy : DragDropEffects.None;
    }

    protected override void OnDragDrop(DragEventArgs drgevent) {
        base.OnDragDrop(drgevent);
        if (drgevent.Data is { } data && data.TryGetData(DataFormats.FileDrop, out string[]? files) && files is [var first, ..]) OpenRom(first);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData) {
        // F6 moves between the list and the page, as in Explorer.
        if (keyData is not (Keys.F6 or (Keys.Shift | Keys.F6))) return base.ProcessCmdKey(ref msg, keyData);
        if (!_split.Panel1Collapsed && !_tree.ContainsFocus) _tree.Select();
        else _page.FocusFirst();
        return true;
    }

    protected override void OnDpiChanged(DpiChangedEventArgs e) {
        base.OnDpiChanged(e);
        ApplyTextSize();
        ShowStatus();
    }

    protected override void OnFormClosing(FormClosingEventArgs e) {
        base.OnFormClosing(e);
        if (e.Cancel) return;
        if (!LeaveRom()) {
            e.Cancel = true;
            return;
        }
        var bounds = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
        _settings.Window = new WindowPlace(bounds.X, bounds.Y, bounds.Width, bounds.Height, WindowState == FormWindowState.Maximized);
        if (!_split.Panel1Collapsed) _settings.ListWidth = (int)(_split.SplitterDistance * 96 / DeviceDpi / _settings.TextZoom);
    }

    protected override void Dispose(bool disposing) {
        if (disposing) {
            components?.Dispose();
            SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
            _bodyFont?.Dispose();
            _groupFont?.Dispose();
            _barFont?.Dispose();
        }
        base.Dispose(disposing);
    }

    private static IEnumerable<TreeNode> Descendants(TreeNode node) => node.Nodes.Cast<TreeNode>().SelectMany(Descendants).Prepend(node);

    private static string ChecksumWords(ChecksumState state) => state switch {
        ChecksumState.Valid => "Checksum correct",
        ChecksumState.Wrong => "Checksum wrong",
        ChecksumState.None => "No checksum",
        _ => throw new ArgumentOutOfRangeException(nameof(state)),
    };

    private static void OpenLink(string url) => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true })?.Dispose();

    /// <summary>
    /// Says what a command does: to screen readers, and in the status bar while the command is
    /// highlighted. The description is the item's accessible description, which the designer holds.
    /// </summary>
    private void Describe(ToolStripMenuItem item, string? description = null) {
        if (description is not null) item.AccessibleDescription = description;
        item.SelectedChanged += (_, _) => {
            if (item.Selected) _statusFile.Text = item.AccessibleDescription;
            else ShowStatus();
        };
    }

    private void OpenItem_Click(object? sender, EventArgs e) => ChooseRom();

    private void CloseItem_Click(object? sender, EventArgs e) => CloseRom();

    private void SaveItem_Click(object? sender, EventArgs e) => Save();

    private void SaveAsItem_Click(object? sender, EventArgs e) => SaveAs();

    private void ExitItem_Click(object? sender, EventArgs e) => Close();

    private void UndoItem_Click(object? sender, EventArgs e) => Undo();

    private void RedoItem_Click(object? sender, EventArgs e) => Redo();

    private void TechnicalItem_Click(object? sender, EventArgs e) => ToggleTechnical();

    private void LargerItem_Click(object? sender, EventArgs e) => SetZoom(_settings.TextZoom + ZoomStep);

    private void SmallerItem_Click(object? sender, EventArgs e) => SetZoom(_settings.TextZoom - ZoomStep);

    private void NormalItem_Click(object? sender, EventArgs e) => SetZoom(1.0);

    private void GettingStartedItem_Click(object? sender, EventArgs e) => OpenLink(GettingStartedUrl);

    private void SupportedRomsItem_Click(object? sender, EventArgs e) => OpenLink(Details.SupportedRomsUrl);

    private void ReportItem_Click(object? sender, EventArgs e) => OpenLink(ReportUrl);

    private void AboutItem_Click(object? sender, EventArgs e) => ShowAbout();

    private void Tree_AfterSelect(object? sender, TreeViewEventArgs e) {
        _problem = null;
        ShowPage();
    }

    /// <summary>The groups stay open: there is nothing to gain from hiding a handful of rows.</summary>
    private void Tree_BeforeCollapse(object? sender, TreeViewCancelEventArgs e) => e.Cancel = true;

    /// <summary>The selected row is outlined only while the list has the focus.</summary>
    private void Tree_FocusChanged(object? sender, EventArgs e) => _tree.Invalidate();

    /// <summary>Starts where the window was last time, when that place is still on a screen; otherwise centred, no larger than the screen.</summary>
    private void PlaceWindow() {
        if (_settings.Window is { } place && Screen.AllScreens.Any(s => s.WorkingArea.IntersectsWith(new Rectangle(place.X, place.Y, place.Width, place.Height)))) {
            StartPosition = FormStartPosition.Manual;
            Bounds = new Rectangle(place.X, place.Y, place.Width, place.Height);
            if (place.Maximised) WindowState = FormWindowState.Maximized;
            return;
        }
        var area = Screen.PrimaryScreen?.WorkingArea.Size ?? new Size(1280, 720);
        Size = new Size(Math.Min(LogicalToDeviceUnits(1180), area.Width * 9 / 10), Math.Min(LogicalToDeviceUnits(760), area.Height * 9 / 10));
        StartPosition = FormStartPosition.CenterScreen;
    }

    private void ChooseRom() {
        using var dialog = new OpenFileDialog {
            Title = "Open a ROM file",
            Filter = "ROM files (*.bin)|*.bin|All files (*.*)|*.*",
            CheckFileExists = true,
            ClientGuid = OpenRomDialog,
        };
        if (dialog.ShowDialog(this) == DialogResult.OK) OpenRom(dialog.FileName);
    }

    private void RunPageAction(PageAction action) {
        switch (action.Command) {
            case PageCommand.OpenFile:
                ChooseRom();
                break;
            case PageCommand.OpenRecent:
                OpenRom(action.Argument!);
                break;
            case PageCommand.OpenLink:
                OpenLink(action.Argument!);
                break;
            case PageCommand.SaveAs:
                SaveAs();
                break;
            case PageCommand.Dismiss:
                _problem = null;
                ShowPage();
                if (!_page.FocusFirst()) _tree.Select();
                break;
        }
    }

    private void ToggleTechnical() {
        _settings.ShowTechnical = !_settings.ShowTechnical;
        _technicalItem.Checked = _settings.ShowTechnical;
        _page.ShowTechnical = _settings.ShowTechnical;
        Announce(_settings.ShowTechnical ? "Technical details shown" : "Technical details hidden");
    }

    private void SetZoom(double zoom) {
        _settings.TextZoom = Math.Clamp(Math.Round(zoom, 1), AppSettings.SmallestZoom, AppSettings.LargestZoom);
        ApplyTextSize();
        Announce($"Text size {_settings.TextZoom:P0}");
    }

    /// <summary>Rebuilds the list, the page, the status bar and the title for the current session.</summary>
    private void ShowSession() {
        using (_tree.SuspendPainting()) {
            _tree.Nodes.Clear();
            if (Session is { } session) {
                var overview = new TreeNode("Overview") { Tag = session, ToolTipText = "What this file is" };
                _tree.Nodes.Add(overview);
                foreach (var category in ParamTree.Build(session.Defs)) {
                    var group = new TreeNode(category.Name) { Tag = category, ToolTipText = Details.Count(category.Params) };
                    foreach (var def in category.Params) {
                        group.Nodes.Add(new TreeNode(def.Name) { Tag = def, ToolTipText = def.Doc.Length > 0 ? def.Doc : Details.KindText(def) });
                    }
                    _tree.Nodes.Add(group);
                }
                _tree.ExpandAll();
                _tree.SelectedNode = overview;
            }
        }
        // With no file open there is nothing to list: the page has the whole window.
        _split.Panel1Collapsed = Session is null;
        ShowEditState();
        ShowRecentFiles();
        ShowStatus();
        ShowPage();
    }

    /// <summary>
    /// Whether the open ROM may be left: it has no changes, or the user saved them or gave them
    /// up. What is being typed counts as a change.
    /// </summary>
    private bool LeaveRom() {
        _page.FinishTyping();
        if (Session is not { Document.IsDirty: true } session) return true;
        return AskAboutChanges(session.FileName) switch {
            ChangesAnswer.Save => Save(),
            ChangesAnswer.Discard => true,
            _ => false,
        };
    }

    private ChangesAnswer AskInDialog(string fileName) {
        using var ask = new ChangesForm(fileName, _barFont ?? Font);
        ask.ShowDialog(this);
        return ask.Answer;
    }

    /// <summary>
    /// Brings what depends on the changes in line with them: the commands that undo, redo and
    /// save, the title and the status bar.
    /// </summary>
    private void ShowEditState() {
        var document = Session?.Document;
        // A name with an ampersand in it must not make an access key.
        static string Named(string command, string? step) => step is null ? command : $"{command} {step.Replace("&", "&&")}";
        _undoItem.Text = Named("&Undo", document?.UndoLabel);
        _redoItem.Text = Named("&Redo", document?.RedoLabel);
        _undoItem.Enabled = document?.CanUndo == true;
        _redoItem.Enabled = document?.CanRedo == true;
        _closeItem.Enabled = _saveItem.Enabled = _saveAsItem.Enabled = Session is not null;
        var changed = document?.IsDirty == true;
        _statusChanges.Available = changed;
        Text = Session is null ? AppName : $"{Session.FileName}{(changed ? " (changes not saved)" : "")} - {AppName}";
    }

    /// <summary>Shows the page of what is selected, under the problem to report when there is one.</summary>
    private void ShowPage() => _page.Page = BuildPage();

    private DetailPage BuildPage() {
        var page = Session is not { } session ? Details.Start(_packs, _packErrors, _settings.RecentFiles) : _tree.SelectedNode?.Tag switch {
            ParamDef def => Details.Param(session.Document, def),
            ParamCategory category => Details.Category(category),
            _ => Details.Rom(session),
        };
        return _problem is null ? page : page with { Blocks = [.. _problem, .. page.Blocks] };
    }

    private void ShowRecentFiles() {
        _recentMenu.DropDownItems.Clear();
        var number = 1;
        foreach (var path in _settings.RecentFiles) {
            var item = new ToolStripMenuItem($"&{number++}  {Path.GetFileName(path)}   ({Path.GetDirectoryName(path)})");
            item.Click += (_, _) => OpenRom(path);
            Describe(item, $"Open {path}");
            _recentMenu.DropDownItems.Add(item);
        }
        if (_settings.RecentFiles.Count == 0) _recentMenu.DropDownItems.Add(new ToolStripMenuItem("No recent files") { Enabled = false });
    }

    private void ShowStatus() {
        var session = Session;
        _statusKind.Available = _statusChecksum.Available = session is not null;
        _statusFile.ForeColor = DarkTheme.Text;
        if (session is null) {
            _statusFile.Text = "No file open";
            _statusFile.AccessibleName = _statusFile.ToolTipText = null;
            return;
        }
        _statusFile.Text = session.FileName;
        _statusFile.ToolTipText = session.Path;
        _statusFile.AccessibleName = $"File: {session.Path}";
        _statusKind.Text = session.Pack.Name;
        _statusKind.ForeColor = DarkTheme.Text;
        _statusChanges.ForeColor = DarkTheme.SeverityColor(Severity.Warning);
        _statusKind.AccessibleName = $"Kind of ROM: {session.Pack.Name}";

        var (severity, tip) = session.Checksum switch {
            ChecksumState.Valid => (Severity.Good, "The self-check built into the ROM is correct: the file is intact. It is corrected each time the ROM is saved"),
            ChecksumState.Wrong => (Severity.Warning, "The self-check built into the ROM is wrong: the file may be damaged"),
            ChecksumState.None => (Severity.Info, "This kind of ROM has no self-check"),
            _ => throw new ArgumentOutOfRangeException(nameof(session)),
        };
        _statusChecksum.Text = ChecksumWords(session.Checksum);
        _statusChecksum.ToolTipText = tip;
        _statusChecksum.ForeColor = session.Checksum == ChecksumState.None ? DarkTheme.TextDim : DarkTheme.SeverityColor(severity);
        var old = _statusChecksum.Image;
        _statusChecksum.Image = Glyphs.Render(Glyphs.For(severity), _statusChecksum.ForeColor, LogicalToDeviceUnits((int)(16 * _settings.TextZoom)));
        old?.Dispose();
    }

    /// <summary>Applies the user's text size to the window: the list and the page in the body size, the bars in the system size.</summary>
    private void ApplyTextSize() {
        var (body, group, bar) = (_bodyFont, _groupFont, _barFont);
        _bodyFont = DarkTheme.BodyFont(Font, _settings.TextZoom);
        _groupFont = new Font(_bodyFont, FontStyle.Bold);
        _barFont = DarkTheme.BarFont(Font, _settings.TextZoom);
        _tree.Font = _page.Font = _bodyFont;
        _menu.Font = _status.Font = _barFont;
        _tree.ItemHeight = _bodyFont.Height + LogicalToDeviceUnits(14);
        _tree.Indent = _bodyFont.Height;
        // The list grows with its text, as far as the window allows.
        var wanted = LogicalToDeviceUnits((int)(_settings.ListWidth * _settings.TextZoom));
        _split.SplitterDistance = Math.Clamp(wanted, _split.Panel1MinSize, Math.Max(_split.Panel1MinSize, _split.Width / 2));
        _status.ImageScalingSize = new Size(LogicalToDeviceUnits((int)(16 * _settings.TextZoom)), LogicalToDeviceUnits((int)(16 * _settings.TextZoom)));
        ShowStatus();
        _largerItem.Enabled = _settings.TextZoom < AppSettings.LargestZoom;
        _smallerItem.Enabled = _settings.TextZoom > AppSettings.SmallestZoom;
        body?.Dispose();
        group?.Dispose();
        bar?.Dispose();
    }

    /// <summary>Gives every part of the window its colours: at start, and when the user turns a Windows contrast theme on or off.</summary>
    private void ApplyTheme() {
        DarkTheme.ApplyToWindow(this);
        _menu.Renderer = _status.Renderer = DarkTheme.CreateRenderer();
        foreach (var strip in new ToolStrip[] { _menu, _status }) {
            strip.BackColor = DarkTheme.Surface;
            strip.ForeColor = DarkTheme.Text;
        }
        foreach (ToolStripMenuItem top in _menu.Items) {
            top.DropDown.BackColor = DarkTheme.Surface;
            top.DropDown.ForeColor = DarkTheme.Text;
            // No command has a picture; only the View menu has a tick.
            if (top.DropDown is ToolStripDropDownMenu drop) (drop.ShowImageMargin, drop.ShowCheckMargin) = (false, top.DropDownItems.Contains(_technicalItem));
        }
        _technicalItem.Checked = _settings.ShowTechnical;
        _tree.BackColor = _split.BackColor = _split.Panel1.BackColor = _split.Panel2.BackColor = DarkTheme.Background;
        _tree.ForeColor = DarkTheme.Text;
        _page.Rebuild();
        ShowStatus();
        Invalidate(true);
    }

    private void OnUserPreferenceChanged(object? sender, UserPreferenceChangedEventArgs e) {
        if (IsHandleCreated && e.Category is UserPreferenceCategory.Accessibility or UserPreferenceCategory.Color or UserPreferenceCategory.General) BeginInvoke(ApplyTheme);
    }

    /// <summary>Tells a screen reader what just happened; it is also what a sighted user reads on the page.</summary>
    private void Announce(string text) {
        if (IsHandleCreated) AccessibilityObject.RaiseAutomationNotification(AutomationNotificationKind.ActionCompleted, AutomationNotificationProcessing.MostRecent, text);
    }

    /// <summary>Tells what a command did: to a screen reader, and on the line under the table or the setting.</summary>
    private void Tell(string text) {
        _page.Say(text);
        Announce(text);
    }

    /// <summary>The line down the middle of the divider, which is wide enough to grab.</summary>
    private void PaintDivider(object? sender, PaintEventArgs e) {
        if (_split.Panel1Collapsed) return;
        using var pen = new Pen(DarkTheme.ControlBorder);
        var x = _split.SplitterRectangle.Left + _split.SplitterRectangle.Width / 2;
        e.Graphics.DrawLine(pen, x, 0, x, _split.Height);
    }

    /// <summary>
    /// Draws a row of the list: the selected one filled, with an accent bar that shows on black
    /// where the fill alone is faint, and outlined while the list has the keyboard focus.
    /// </summary>
    private void DrawNode(object? sender, DrawTreeNodeEventArgs e) {
        if (e.Node is not { } node || e.Bounds.IsEmpty) return;
        var selected = (e.State & TreeNodeStates.Selected) != 0;
        var row = new Rectangle(0, e.Bounds.Top, _tree.ClientSize.Width, e.Bounds.Height);
        var bar = LogicalToDeviceUnits(4);
        using (var back = new SolidBrush(selected ? DarkTheme.Selection : DarkTheme.Background)) e.Graphics.FillRectangle(back, row);
        if (selected) {
            using var accent = new SolidBrush(DarkTheme.HighContrast ? DarkTheme.SelectionText : DarkTheme.Accent);
            e.Graphics.FillRectangle(accent, 0, row.Top, bar, row.Height);
            if (_tree.Focused) {
                using var pen = new Pen(DarkTheme.SelectionText);
                e.Graphics.DrawRectangle(pen, bar + 1, row.Top + 1, row.Width - bar - 3, row.Height - 3);
            }
        }
        // Group names in bold, so that they read as headings of what is under them.
        var font = node.Tag is ParamCategory ? _groupFont ?? _tree.Font : _tree.Font;
        var left = bar + LogicalToDeviceUnits(10) + node.Level * _tree.Indent;
        var text = new Rectangle(left, row.Top, Math.Max(row.Width - left - bar, 1), row.Height);
        TextRenderer.DrawText(e.Graphics, node.Text, font, text, selected ? DarkTheme.SelectionText : DarkTheme.Text,
            TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
    }

    private void ShowAbout() {
        using var about = new AboutForm(AppName, ReportUrl);
        about.ShowDialog(this);
    }
}
