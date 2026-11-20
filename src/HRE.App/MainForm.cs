using System.Diagnostics;
using HRE.Core;
using HRE.Definitions;

namespace HRE.App;

/// <summary>The main window: menus, the parameter tree, the detail view and the status bar.</summary>
public sealed class MainForm : Form {
    private const string AppName = "Honda ROM Editor";
    private const string WikiUrl = "https://github.com/VIRUXE/honda-rom-editor/wiki";
    private const string ProjectUrl = "https://github.com/VIRUXE/honda-rom-editor";

    /// <summary>Space between the fields of the status bar.</summary>
    private static readonly Padding StatusGap = new(14, 0, 0, 0);

    private readonly IReadOnlyList<DefinitionPack> _packs;
    private readonly IReadOnlyList<ProjectException> _packErrors;

    private readonly TreeView _tree = new() {
        Dock = DockStyle.Fill,
        BorderStyle = BorderStyle.None,
        BackColor = DarkTheme.Background,
        ForeColor = DarkTheme.Text,
        LineColor = DarkTheme.Border,
        HideSelection = false,
        ShowLines = false,
        ShowRootLines = true,
        DrawMode = TreeViewDrawMode.OwnerDrawText,
    };

    private readonly DetailView _detail = new() { Dock = DockStyle.Fill };
    private readonly ToolStripStatusLabel _statusFile = new() { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
    private readonly ToolStripStatusLabel _statusPack = new() { Padding = StatusGap };
    private readonly ToolStripStatusLabel _statusParams = new() { Padding = StatusGap };
    private readonly ToolStripStatusLabel _statusChecksum = new() { Padding = StatusGap };
    private readonly ToolStripMenuItem _closeItem;
    private readonly ToolStripMenuItem _expandItem;
    private readonly ToolStripMenuItem _collapseItem;

    public MainForm(IReadOnlyList<DefinitionPack> packs, IReadOnlyList<ProjectException> packErrors) {
        _packs = packs;
        _packErrors = packErrors;

        Text = AppName;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(1180, 720);
        MinimumSize = new Size(640, 400);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = DarkTheme.Background;
        ForeColor = DarkTheme.Text;

        var renderer = DarkTheme.CreateRenderer();
        _closeItem = Item("&Close ROM", Keys.Control | Keys.W, CloseRom);
        _expandItem = Item("&Expand all", Keys.None, _tree.ExpandAll);
        _collapseItem = Item("&Collapse all", Keys.None, CollapseToCategories);
        var menu = new MenuStrip { Renderer = renderer, BackColor = DarkTheme.Surface, ForeColor = DarkTheme.Text, Padding = new Padding(4, 2, 0, 2) };
        menu.Items.Add(TopMenu("&File",
            Item("&Open ROM...", Keys.Control | Keys.O, ChooseRom),
            _closeItem,
            new ToolStripSeparator(),
            Item("E&xit", Keys.Alt | Keys.F4, Close)));
        menu.Items.Add(TopMenu("&View", _expandItem, _collapseItem));
        menu.Items.Add(TopMenu("&Help",
            Item("&Documentation", Keys.F1, () => OpenLink(WikiUrl)),
            Item("&Project on GitHub", Keys.None, () => OpenLink(ProjectUrl)),
            new ToolStripSeparator(),
            Item($"&About {AppName}", Keys.None, ShowAbout)));

        var status = new StatusStrip { Renderer = renderer, BackColor = DarkTheme.Surface, ForeColor = DarkTheme.Text, SizingGrip = false };
        status.Items.AddRange([_statusFile, _statusPack, _statusParams, _statusChecksum]);

        var split = new SplitContainer {
            Dock = DockStyle.Fill,
            BackColor = DarkTheme.Border,
            FixedPanel = FixedPanel.Panel1,
            SplitterWidth = 1,
            Panel1MinSize = 160,
            TabStop = false,
        };
        split.Panel1.BackColor = DarkTheme.Background;
        split.Panel1.Padding = new Padding(6, 8, 0, 0);
        split.Panel2.BackColor = DarkTheme.Background;
        split.Panel1.Controls.Add(_tree);
        split.Panel2.Controls.Add(_detail);

        // Docking goes by z-order: the control added last takes its edge first.
        Controls.Add(split);
        Controls.Add(status);
        Controls.Add(menu);
        MainMenuStrip = menu;
        split.SplitterDistance = 280;

        _tree.ItemHeight = _tree.ItemHeight + 6;
        _tree.AfterSelect += (_, _) => ShowSelection();
        _tree.DrawNode += DrawNode;

        ShowSession();
    }

    /// <summary>The ROM that is open; null when none is.</summary>
    public RomSession? Session { get; private set; }

    internal TreeView Tree => _tree;
    internal DetailView Detail => _detail;
    internal IEnumerable<string> StatusTexts => new[] { _statusFile, _statusPack, _statusParams, _statusChecksum }.Where(l => l.Available).Select(l => l.Text ?? "");

    /// <summary>
    /// Opens a ROM image, replacing the one that is open. A file that cannot be read is reported
    /// in the detail view and leaves what was open as it is.
    /// </summary>
    public void OpenRom(string path) {
        try {
            Session = RomSession.Open(path, _packs);
        } catch (Exception e) when (e is IOException or UnauthorizedAccessException) {
            _detail.Page = Details.Failure(path, e.Message);
            return;
        }
        ShowSession();
    }

    public void CloseRom() {
        Session = null;
        ShowSession();
    }

    /// <summary>Selects a parameter in the tree by name. Returns false when the open ROM has none of that name.</summary>
    public bool SelectParam(string name) {
        var node = _tree.Nodes.Cast<TreeNode>().SelectMany(Descendants).FirstOrDefault(n => n.Tag is ParamDef def && def.Name == name);
        if (node is null) return false;
        _tree.SelectedNode = node;
        node.EnsureVisible();
        ShowSelection();
        return true;
    }

    protected override void OnHandleCreated(EventArgs e) {
        base.OnHandleCreated(e);
        DarkTheme.ApplyToWindow(this);
    }

    private static IEnumerable<TreeNode> Descendants(TreeNode node) => node.Nodes.Cast<TreeNode>().SelectMany(Descendants).Prepend(node);

    private static ToolStripMenuItem TopMenu(string text, params ToolStripItem[] items) {
        var menu = new ToolStripMenuItem(text) { ForeColor = DarkTheme.Text };
        menu.DropDown.BackColor = DarkTheme.Surface;
        menu.DropDown.ForeColor = DarkTheme.Text;
        menu.DropDownItems.AddRange(items);
        return menu;
    }

    private static ToolStripMenuItem Item(string text, Keys shortcut, Action run) {
        var item = new ToolStripMenuItem(text) { ForeColor = DarkTheme.Text };
        if (shortcut != Keys.None) item.ShortcutKeys = shortcut;
        item.Click += (_, _) => run();
        return item;
    }

    private void ChooseRom() {
        using var dialog = new OpenFileDialog {
            Title = "Open ROM",
            Filter = "ROM images (*.bin)|*.bin|All files (*.*)|*.*",
            CheckFileExists = true,
        };
        if (dialog.ShowDialog(this) == DialogResult.OK) OpenRom(dialog.FileName);
    }

    /// <summary>Rebuilds the tree, the detail view, the status bar and the title for the current session.</summary>
    private void ShowSession() {
        _tree.BeginUpdate();
        _tree.Nodes.Clear();
        if (Session is { } session) {
            var root = new TreeNode(session.FileName) { Tag = session };
            foreach (var category in ParamTree.Build(session.Defs)) {
                var node = new TreeNode(category.Name) { Tag = category };
                foreach (var def in category.Params) node.Nodes.Add(new TreeNode(def.Name) { Tag = def });
                root.Nodes.Add(node);
            }
            _tree.Nodes.Add(root);
            _tree.ExpandAll();
            _tree.SelectedNode = root;
        }
        _tree.EndUpdate();

        var open = Session is not null;
        _closeItem.Enabled = _expandItem.Enabled = _collapseItem.Enabled = open;
        Text = open ? $"{Session!.FileName} - {AppName}" : AppName;
        ShowStatus();
        if (open) ShowSelection();
        else _detail.Page = Details.Start(_packs, _packErrors);
    }

    private void ShowSelection() {
        if (Session is not { } session) return;
        _detail.Page = _tree.SelectedNode?.Tag switch {
            ParamDef def => Details.Param(session.Image, def),
            ParamCategory category => Details.Category(category),
            _ => Details.Rom(session),
        };
    }

    private void ShowStatus() {
        var session = Session;
        _statusPack.Available = _statusParams.Available = _statusChecksum.Available = session is not null;
        if (session is null) {
            var packs = _packs.Count == 1 ? "1 definition pack" : $"{_packs.Count} definition packs";
            _statusFile.Text = _packErrors.Count == 0 ? $"No ROM open. {packs} loaded." : $"No ROM open. {packs} loaded, {_packErrors.Count} could not be read.";
            return;
        }
        _statusFile.Text = session.Path;
        _statusPack.Text = session.Pack?.Name ?? "Not recognised";
        _statusPack.ForeColor = session.Pack is null ? DarkTheme.Warning : DarkTheme.Text;
        _statusParams.Text = session.Defs.Count == 1 ? "1 parameter" : $"{session.Defs.Count} parameters";
        (_statusChecksum.Text, _statusChecksum.ForeColor) = session.Checksum switch {
            ChecksumState.Valid => ("Checksum correct", DarkTheme.Good),
            ChecksumState.Wrong => ("Checksum wrong", DarkTheme.Bad),
            ChecksumState.None => ("No checksum", DarkTheme.TextDim),
            _ => ("Checksum unknown", DarkTheme.TextDim),
        };
    }

    /// <summary>Collapses the categories and keeps the ROM node open, so the categories stay in view.</summary>
    private void CollapseToCategories() {
        foreach (TreeNode root in _tree.Nodes) {
            foreach (TreeNode category in root.Nodes) category.Collapse();
        }
    }

    /// <summary>Draws a node's text: the selection in the theme's colour, the ROM and the categories dimmer than the parameters.</summary>
    private void DrawNode(object? sender, DrawTreeNodeEventArgs e) {
        if (e.Node is not { } node || e.Bounds.IsEmpty) return;
        var selected = (e.State & TreeNodeStates.Selected) != 0;
        var row = new Rectangle(e.Bounds.Left - 2, e.Bounds.Top, _tree.ClientSize.Width - e.Bounds.Left + 2, e.Bounds.Height);
        using (var back = new SolidBrush(selected ? DarkTheme.Selection : DarkTheme.Background)) e.Graphics.FillRectangle(back, row);
        var color = selected || node.Tag is ParamDef ? DarkTheme.Text : DarkTheme.TextDim;
        var text = new Rectangle(e.Bounds.Left + 2, e.Bounds.Top, row.Width - 4, e.Bounds.Height);
        TextRenderer.DrawText(e.Graphics, node.Text, _tree.Font, text, color, TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
    }

    private void ShowAbout() {
        using var about = new AboutForm(AppName, ProjectUrl);
        about.ShowDialog(this);
    }

    private static void OpenLink(string url) => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true })?.Dispose();
}
