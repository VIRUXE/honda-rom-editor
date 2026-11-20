using System.Diagnostics;
using System.Windows.Forms.Automation;
using HRE.Core;
using HRE.Definitions;
using Microsoft.Win32;

namespace HRE.App;

/// <summary>The main window: menus, the list of tables and settings, the page that describes what is selected, the status bar.</summary>
public sealed class MainForm : Form {
    private const string AppName = "Honda ROM Editor";
    private const string GettingStartedUrl = "https://github.com/VIRUXE/honda-rom-editor/wiki/Application";
    private const string ReportUrl = "https://github.com/VIRUXE/honda-rom-editor/issues";
    private const double ZoomStep = 0.1;

    /// <summary>Lets Windows remember the folder of this dialog apart from the other file dialogs to come.</summary>
    private static readonly Guid OpenRomDialog = new("6f6f2a55-7c0b-4a5e-9a57-1d2f7f1c8e01");

    private readonly IReadOnlyList<DefinitionPack> _packs;
    private readonly IReadOnlyList<ProjectException> _packErrors;
    private readonly AppSettings _settings;

    private readonly MenuStrip _menu = new() { AccessibleName = "Menu", Padding = new Padding(4, 2, 0, 2), AllowClickThrough = true };
    private readonly StatusStrip _status = new() { AccessibleName = "Status", SizingGrip = false, ShowItemToolTips = true };
    private readonly SplitContainer _split = new() { Dock = DockStyle.Fill, FixedPanel = FixedPanel.Panel1, TabStop = true, AccessibleName = "Divider between the list and the details" };
    private readonly PageView _page = new() { Dock = DockStyle.Fill };

    private readonly TreeView _tree = new() {
        Dock = DockStyle.Fill,
        AccessibleName = "Tables and settings",
        BorderStyle = BorderStyle.None,
        HideSelection = false,
        FullRowSelect = true,
        ShowLines = false,
        ShowPlusMinus = false,
        ShowRootLines = false,
        ShowNodeToolTips = true,
        DrawMode = TreeViewDrawMode.OwnerDrawText,
    };

    private readonly ToolStripStatusLabel _statusFile = new() { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
    private readonly ToolStripStatusLabel _statusKind = new() { Padding = new Padding(14, 0, 0, 0), ToolTipText = "The kind of ROM this file was recognised as" };
    private readonly ToolStripStatusLabel _statusChecksum = new() { Padding = new Padding(14, 0, 0, 0), ImageAlign = ContentAlignment.MiddleLeft, TextImageRelation = TextImageRelation.ImageBeforeText };
    private readonly ToolStripMenuItem _recentMenu = new("&Recent files");
    private readonly ToolStripMenuItem _closeItem;
    private readonly ToolStripMenuItem _technicalItem;
    private readonly ToolStripMenuItem _largerItem;
    private readonly ToolStripMenuItem _smallerItem;

    private IReadOnlyList<PageBlock>? _problem;
    private Font? _bodyFont;
    private Font? _groupFont;
    private Font? _barFont;

    public MainForm(IReadOnlyList<DefinitionPack> packs, IReadOnlyList<ProjectException> packErrors, AppSettings settings) {
        _packs = packs;
        _packErrors = packErrors;
        _settings = settings;

        SuspendLayout();
        AutoScaleDimensions = new SizeF(96f, 96f);
        AutoScaleMode = AutoScaleMode.Dpi;
        Text = AppName;
        MinimumSize = new Size(560, 380);
        AllowDrop = true;

        _closeItem = Item("&Close", Keys.Control | Keys.W, "Close the file that is open", CloseRom);
        _technicalItem = Item("&Technical details", Keys.Control | Keys.T, "Show or hide addresses, hashes and other details of how things are stored", ToggleTechnical);
        _largerItem = Item("&Larger text", Keys.Control | Keys.Oemplus, "Make the text of the window larger", () => SetZoom(_settings.TextZoom + ZoomStep));
        _largerItem.ShortcutKeyDisplayString = "Ctrl++";
        _smallerItem = Item("&Smaller text", Keys.Control | Keys.OemMinus, "Make the text of the window smaller", () => SetZoom(_settings.TextZoom - ZoomStep));
        _smallerItem.ShortcutKeyDisplayString = "Ctrl+-";
        var normalItem = Item("&Normal text size", Keys.Control | Keys.D0, "Bring the text back to its normal size", () => SetZoom(1.0));
        normalItem.ShortcutKeyDisplayString = "Ctrl+0";
        Describe(_recentMenu, "Open a file you opened lately");

        _menu.Items.Add(TopMenu("&File",
            Item("&Open...", Keys.Control | Keys.O, "Choose a ROM file to open", ChooseRom),
            _recentMenu,
            _closeItem,
            new ToolStripSeparator(),
            Item("E&xit", Keys.Alt | Keys.F4, "Close the program", Close)));
        _menu.Items.Add(TopMenu("&View", _technicalItem, new ToolStripSeparator(), _largerItem, _smallerItem, normalItem));
        _menu.Items.Add(TopMenu("&Help",
            Item("&Getting started", Keys.F1, "Read how the window works, in the browser", () => OpenLink(GettingStartedUrl)),
            Item("&Which ROMs can be opened", Keys.None, "Read which ROMs this version can open, in the browser", () => OpenLink(Details.SupportedRomsUrl)),
            Item("&Report a problem", Keys.None, "Tell the makers what went wrong, in the browser", () => OpenLink(ReportUrl)),
            new ToolStripSeparator(),
            Item($"&About {AppName}", Keys.None, "See the version of the program", ShowAbout)));
        _status.Items.AddRange([_statusFile, _statusKind, _statusChecksum]);

        _split.Panel1.Padding = new Padding(0, 8, 0, 0);
        _split.Panel1.Controls.Add(_tree);
        _split.Panel2.Controls.Add(_page);
        _split.Paint += PaintDivider;

        // Docking goes by z-order: the control added last takes its edge first.
        Controls.Add(_split);
        Controls.Add(_status);
        Controls.Add(_menu);
        MainMenuStrip = _menu;
        ResumeLayout();

        PlaceWindow();
        _split.SplitterWidth = LogicalToDeviceUnits(7);
        _split.Panel1MinSize = LogicalToDeviceUnits(160);

        _tree.AfterSelect += (_, _) => {
            _problem = null;
            ShowPage();
        };
        // The groups stay open: there is nothing to gain from hiding a handful of rows.
        _tree.BeforeCollapse += (_, e) => e.Cancel = true;
        _tree.DrawNode += DrawNode;
        _tree.GotFocus += (_, _) => _tree.Invalidate();
        _tree.LostFocus += (_, _) => _tree.Invalidate();
        _page.ActionInvoked += RunPageAction;
        _page.TechnicalToggled += ToggleTechnical;
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
    internal IEnumerable<string> StatusTexts => new[] { _statusFile, _statusKind, _statusChecksum }.Where(l => l.Available).Select(l => l.Text ?? "");

    /// <summary>
    /// Opens a ROM file, replacing the one that is open. A file that is missing, unreadable or
    /// not a ROM this version knows is explained at the top of the page, and what was open stays open.
    /// </summary>
    public void OpenRom(string path) {
        switch (RomSession.Open(path, _packs)) {
            case OpenResult.Opened opened:
                Session = opened.Session;
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

    public void CloseRom() {
        Session = null;
        _problem = null;
        ShowSession();
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
        var bounds = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
        _settings.Window = new WindowPlace(bounds.X, bounds.Y, bounds.Width, bounds.Height, WindowState == FormWindowState.Maximized);
        if (!_split.Panel1Collapsed) _settings.ListWidth = (int)(_split.SplitterDistance * 96 / DeviceDpi / _settings.TextZoom);
    }

    protected override void Dispose(bool disposing) {
        if (disposing) {
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

    private static ToolStripMenuItem TopMenu(string text, params ToolStripItem[] items) {
        var menu = new ToolStripMenuItem(text);
        menu.DropDownItems.AddRange(items);
        return menu;
    }

    private ToolStripMenuItem Item(string text, Keys shortcut, string description, Action run) {
        var item = new ToolStripMenuItem(text);
        if (shortcut != Keys.None) item.ShortcutKeys = shortcut;
        item.Click += (_, _) => run();
        Describe(item, description);
        return item;
    }

    /// <summary>Says what a command does: to screen readers, and in the status bar while the command is highlighted.</summary>
    private void Describe(ToolStripMenuItem item, string description) {
        item.AccessibleDescription = description;
        item.SelectedChanged += (_, _) => {
            if (item.Selected) _statusFile.Text = description;
            else ShowStatus();
        };
    }

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
        _closeItem.Enabled = Session is not null;
        Text = Session is null ? AppName : $"{Session.FileName} - {AppName}";
        ShowRecentFiles();
        ShowStatus();
        ShowPage();
    }

    /// <summary>Shows the page of what is selected, under the problem to report when there is one.</summary>
    private void ShowPage() {
        var page = Session is not { } session ? Details.Start(_packs, _packErrors, _settings.RecentFiles) : _tree.SelectedNode?.Tag switch {
            ParamDef def => Details.Param(session.Image, def),
            ParamCategory category => Details.Category(category),
            _ => Details.Rom(session),
        };
        _page.Page = _problem is null ? page : page with { Blocks = [.. _problem, .. page.Blocks] };
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
        _statusKind.AccessibleName = $"Kind of ROM: {session.Pack.Name}";

        var (severity, tip) = session.Checksum switch {
            ChecksumState.Valid => (Severity.Good, "The self-check built into the ROM is correct: the file is intact"),
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
