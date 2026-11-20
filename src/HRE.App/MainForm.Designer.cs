#nullable disable

namespace HRE.App;

partial class MainForm {
    /// <summary>Required designer variable.</summary>
    private System.ComponentModel.IContainer components = null;

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent() {
        this._menu = new System.Windows.Forms.MenuStrip();
        this._fileMenu = new System.Windows.Forms.ToolStripMenuItem();
        this._openItem = new System.Windows.Forms.ToolStripMenuItem();
        this._recentMenu = new System.Windows.Forms.ToolStripMenuItem();
        this._closeItem = new System.Windows.Forms.ToolStripMenuItem();
        this._fileSeparator1 = new System.Windows.Forms.ToolStripSeparator();
        this._saveItem = new System.Windows.Forms.ToolStripMenuItem();
        this._saveAsItem = new System.Windows.Forms.ToolStripMenuItem();
        this._fileSeparator2 = new System.Windows.Forms.ToolStripSeparator();
        this._exitItem = new System.Windows.Forms.ToolStripMenuItem();
        this._editMenu = new System.Windows.Forms.ToolStripMenuItem();
        this._undoItem = new System.Windows.Forms.ToolStripMenuItem();
        this._redoItem = new System.Windows.Forms.ToolStripMenuItem();
        this._viewMenu = new System.Windows.Forms.ToolStripMenuItem();
        this._technicalItem = new System.Windows.Forms.ToolStripMenuItem();
        this._viewSeparator = new System.Windows.Forms.ToolStripSeparator();
        this._largerItem = new System.Windows.Forms.ToolStripMenuItem();
        this._smallerItem = new System.Windows.Forms.ToolStripMenuItem();
        this._normalItem = new System.Windows.Forms.ToolStripMenuItem();
        this._helpMenu = new System.Windows.Forms.ToolStripMenuItem();
        this._gettingStartedItem = new System.Windows.Forms.ToolStripMenuItem();
        this._supportedRomsItem = new System.Windows.Forms.ToolStripMenuItem();
        this._reportItem = new System.Windows.Forms.ToolStripMenuItem();
        this._helpSeparator = new System.Windows.Forms.ToolStripSeparator();
        this._aboutItem = new System.Windows.Forms.ToolStripMenuItem();
        this._status = new System.Windows.Forms.StatusStrip();
        this._statusFile = new System.Windows.Forms.ToolStripStatusLabel();
        this._statusChanges = new System.Windows.Forms.ToolStripStatusLabel();
        this._statusKind = new System.Windows.Forms.ToolStripStatusLabel();
        this._statusChecksum = new System.Windows.Forms.ToolStripStatusLabel();
        this._split = new System.Windows.Forms.SplitContainer();
        this._tree = new System.Windows.Forms.TreeView();
        this._page = new HRE.App.PageView();
        this._menu.SuspendLayout();
        this._status.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this._split)).BeginInit();
        this._split.Panel1.SuspendLayout();
        this._split.Panel2.SuspendLayout();
        this._split.SuspendLayout();
        this.SuspendLayout();
        //
        // _menu
        //
        this._menu.AccessibleName = "Menu";
        this._menu.AllowClickThrough = true;
        this._menu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._fileMenu,
            this._editMenu,
            this._viewMenu,
            this._helpMenu});
        this._menu.Location = new System.Drawing.Point(0, 0);
        this._menu.Name = "_menu";
        this._menu.Padding = new System.Windows.Forms.Padding(4, 2, 0, 2);
        this._menu.Size = new System.Drawing.Size(1180, 24);
        this._menu.TabIndex = 2;
        //
        // _fileMenu
        //
        this._fileMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._openItem,
            this._recentMenu,
            this._closeItem,
            this._fileSeparator1,
            this._saveItem,
            this._saveAsItem,
            this._fileSeparator2,
            this._exitItem});
        this._fileMenu.Name = "_fileMenu";
        this._fileMenu.Text = "&File";
        //
        // _openItem
        //
        this._openItem.AccessibleDescription = "Choose a ROM file to open";
        this._openItem.Name = "_openItem";
        this._openItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
        this._openItem.Text = "&Open...";
        this._openItem.Click += new System.EventHandler(this.OpenItem_Click);
        //
        // _recentMenu
        //
        this._recentMenu.AccessibleDescription = "Open a file you opened lately";
        this._recentMenu.Name = "_recentMenu";
        this._recentMenu.Text = "&Recent files";
        //
        // _closeItem
        //
        this._closeItem.AccessibleDescription = "Close the file that is open";
        this._closeItem.Name = "_closeItem";
        this._closeItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.W)));
        this._closeItem.Text = "&Close";
        this._closeItem.Click += new System.EventHandler(this.CloseItem_Click);
        //
        // _fileSeparator1
        //
        this._fileSeparator1.Name = "_fileSeparator1";
        //
        // _saveItem
        //
        this._saveItem.AccessibleDescription = "Save the changes to the file, with the checksum corrected";
        this._saveItem.Name = "_saveItem";
        this._saveItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
        this._saveItem.Text = "&Save";
        this._saveItem.Click += new System.EventHandler(this.SaveItem_Click);
        //
        // _saveAsItem
        //
        this._saveAsItem.AccessibleDescription = "Save the ROM as another file, and go on working in that one";
        this._saveAsItem.Name = "_saveAsItem";
        this._saveAsItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) | System.Windows.Forms.Keys.S)));
        this._saveAsItem.Text = "Save &as...";
        this._saveAsItem.Click += new System.EventHandler(this.SaveAsItem_Click);
        //
        // _fileSeparator2
        //
        this._fileSeparator2.Name = "_fileSeparator2";
        //
        // _exitItem
        //
        this._exitItem.AccessibleDescription = "Close the program";
        this._exitItem.Name = "_exitItem";
        this._exitItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.F4)));
        this._exitItem.Text = "E&xit";
        this._exitItem.Click += new System.EventHandler(this.ExitItem_Click);
        //
        // _editMenu
        //
        this._editMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._undoItem,
            this._redoItem});
        this._editMenu.Name = "_editMenu";
        this._editMenu.Text = "&Edit";
        //
        // _undoItem
        //
        this._undoItem.AccessibleDescription = "Take back the last change";
        this._undoItem.Name = "_undoItem";
        this._undoItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Z)));
        this._undoItem.Text = "&Undo";
        this._undoItem.Click += new System.EventHandler(this.UndoItem_Click);
        //
        // _redoItem
        //
        this._redoItem.AccessibleDescription = "Make the change again that was taken back";
        this._redoItem.Name = "_redoItem";
        this._redoItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Y)));
        this._redoItem.Text = "&Redo";
        this._redoItem.Click += new System.EventHandler(this.RedoItem_Click);
        //
        // _viewMenu
        //
        this._viewMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._technicalItem,
            this._viewSeparator,
            this._largerItem,
            this._smallerItem,
            this._normalItem});
        this._viewMenu.Name = "_viewMenu";
        this._viewMenu.Text = "&View";
        //
        // _technicalItem
        //
        this._technicalItem.AccessibleDescription = "Show or hide addresses, hashes and other details of how things are stored";
        this._technicalItem.Name = "_technicalItem";
        this._technicalItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.T)));
        this._technicalItem.Text = "&Technical details";
        this._technicalItem.Click += new System.EventHandler(this.TechnicalItem_Click);
        //
        // _viewSeparator
        //
        this._viewSeparator.Name = "_viewSeparator";
        //
        // _largerItem
        //
        this._largerItem.AccessibleDescription = "Make the text of the window larger";
        this._largerItem.Name = "_largerItem";
        this._largerItem.ShortcutKeyDisplayString = "Ctrl++";
        this._largerItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Oemplus)));
        this._largerItem.Text = "&Larger text";
        this._largerItem.Click += new System.EventHandler(this.LargerItem_Click);
        //
        // _smallerItem
        //
        this._smallerItem.AccessibleDescription = "Make the text of the window smaller";
        this._smallerItem.Name = "_smallerItem";
        this._smallerItem.ShortcutKeyDisplayString = "Ctrl+-";
        this._smallerItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.OemMinus)));
        this._smallerItem.Text = "&Smaller text";
        this._smallerItem.Click += new System.EventHandler(this.SmallerItem_Click);
        //
        // _normalItem
        //
        this._normalItem.AccessibleDescription = "Bring the text back to its normal size";
        this._normalItem.Name = "_normalItem";
        this._normalItem.ShortcutKeyDisplayString = "Ctrl+0";
        this._normalItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.D0)));
        this._normalItem.Text = "&Normal text size";
        this._normalItem.Click += new System.EventHandler(this.NormalItem_Click);
        //
        // _helpMenu
        //
        this._helpMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._gettingStartedItem,
            this._supportedRomsItem,
            this._reportItem,
            this._helpSeparator,
            this._aboutItem});
        this._helpMenu.Name = "_helpMenu";
        this._helpMenu.Text = "&Help";
        //
        // _gettingStartedItem
        //
        this._gettingStartedItem.AccessibleDescription = "Read how the window works, in the browser";
        this._gettingStartedItem.Name = "_gettingStartedItem";
        this._gettingStartedItem.ShortcutKeys = System.Windows.Forms.Keys.F1;
        this._gettingStartedItem.Text = "&Getting started";
        this._gettingStartedItem.Click += new System.EventHandler(this.GettingStartedItem_Click);
        //
        // _supportedRomsItem
        //
        this._supportedRomsItem.AccessibleDescription = "Read which ROMs this version can open, in the browser";
        this._supportedRomsItem.Name = "_supportedRomsItem";
        this._supportedRomsItem.Text = "&Which ROMs can be opened";
        this._supportedRomsItem.Click += new System.EventHandler(this.SupportedRomsItem_Click);
        //
        // _reportItem
        //
        this._reportItem.AccessibleDescription = "Tell the makers what went wrong, in the browser";
        this._reportItem.Name = "_reportItem";
        this._reportItem.Text = "&Report a problem";
        this._reportItem.Click += new System.EventHandler(this.ReportItem_Click);
        //
        // _helpSeparator
        //
        this._helpSeparator.Name = "_helpSeparator";
        //
        // _aboutItem
        //
        this._aboutItem.AccessibleDescription = "See the version of the program";
        this._aboutItem.Name = "_aboutItem";
        this._aboutItem.Text = "&About Honda ROM Editor";
        this._aboutItem.Click += new System.EventHandler(this.AboutItem_Click);
        //
        // _status
        //
        this._status.AccessibleName = "Status";
        this._status.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._statusFile,
            this._statusChanges,
            this._statusKind,
            this._statusChecksum});
        this._status.Location = new System.Drawing.Point(0, 738);
        this._status.Name = "_status";
        this._status.ShowItemToolTips = true;
        this._status.Size = new System.Drawing.Size(1180, 22);
        this._status.SizingGrip = false;
        this._status.TabIndex = 1;
        //
        // _statusFile
        //
        this._statusFile.Name = "_statusFile";
        this._statusFile.Spring = true;
        this._statusFile.Text = "No file open";
        this._statusFile.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        //
        // _statusChanges
        //
        this._statusChanges.Available = false;
        this._statusChanges.Name = "_statusChanges";
        this._statusChanges.Padding = new System.Windows.Forms.Padding(14, 0, 0, 0);
        this._statusChanges.Text = "Changes not saved";
        this._statusChanges.ToolTipText = "Save them with File > Save (Ctrl+S)";
        //
        // _statusKind
        //
        this._statusKind.Name = "_statusKind";
        this._statusKind.Padding = new System.Windows.Forms.Padding(14, 0, 0, 0);
        this._statusKind.ToolTipText = "The kind of ROM this file was recognised as";
        //
        // _statusChecksum
        //
        this._statusChecksum.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
        this._statusChecksum.Name = "_statusChecksum";
        this._statusChecksum.Padding = new System.Windows.Forms.Padding(14, 0, 0, 0);
        this._statusChecksum.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
        //
        // _split
        //
        this._split.AccessibleName = "Divider between the list and the details";
        this._split.Dock = System.Windows.Forms.DockStyle.Fill;
        this._split.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
        this._split.Location = new System.Drawing.Point(0, 24);
        this._split.Name = "_split";
        //
        // _split.Panel1
        //
        this._split.Panel1.Controls.Add(this._tree);
        this._split.Panel1.Padding = new System.Windows.Forms.Padding(0, 8, 0, 0);
        //
        // _split.Panel2
        //
        this._split.Panel2.Controls.Add(this._page);
        this._split.Size = new System.Drawing.Size(1180, 714);
        this._split.SplitterDistance = 280;
        this._split.TabIndex = 0;
        this._split.Paint += new System.Windows.Forms.PaintEventHandler(this.PaintDivider);
        //
        // _tree
        //
        this._tree.AccessibleName = "Tables and settings";
        this._tree.BorderStyle = System.Windows.Forms.BorderStyle.None;
        this._tree.Dock = System.Windows.Forms.DockStyle.Fill;
        this._tree.DrawMode = System.Windows.Forms.TreeViewDrawMode.OwnerDrawText;
        this._tree.FullRowSelect = true;
        this._tree.HideSelection = false;
        this._tree.Location = new System.Drawing.Point(0, 8);
        this._tree.Name = "_tree";
        this._tree.ShowLines = false;
        this._tree.ShowNodeToolTips = true;
        this._tree.ShowPlusMinus = false;
        this._tree.ShowRootLines = false;
        this._tree.Size = new System.Drawing.Size(280, 706);
        this._tree.TabIndex = 0;
        this._tree.BeforeCollapse += new System.Windows.Forms.TreeViewCancelEventHandler(this.Tree_BeforeCollapse);
        this._tree.DrawNode += new System.Windows.Forms.DrawTreeNodeEventHandler(this.DrawNode);
        this._tree.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.Tree_AfterSelect);
        this._tree.GotFocus += new System.EventHandler(this.Tree_FocusChanged);
        this._tree.LostFocus += new System.EventHandler(this.Tree_FocusChanged);
        //
        // _page
        //
        this._page.Dock = System.Windows.Forms.DockStyle.Fill;
        this._page.Location = new System.Drawing.Point(0, 0);
        this._page.Name = "_page";
        this._page.Size = new System.Drawing.Size(896, 714);
        this._page.TabIndex = 0;
        //
        // MainForm
        //
        this.AllowDrop = true;
        this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
        this.ClientSize = new System.Drawing.Size(1180, 760);
        // Docking goes by z-order: the control added last takes its edge first.
        this.Controls.Add(this._split);
        this.Controls.Add(this._status);
        this.Controls.Add(this._menu);
        this.MainMenuStrip = this._menu;
        this.MinimumSize = new System.Drawing.Size(560, 380);
        this.Name = "MainForm";
        this.Text = "Honda ROM Editor";
        this._menu.ResumeLayout(false);
        this._menu.PerformLayout();
        this._status.ResumeLayout(false);
        this._status.PerformLayout();
        this._split.Panel1.ResumeLayout(false);
        this._split.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)(this._split)).EndInit();
        this._split.ResumeLayout(false);
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    #endregion

    private System.Windows.Forms.MenuStrip _menu;
    private System.Windows.Forms.ToolStripMenuItem _fileMenu;
    private System.Windows.Forms.ToolStripMenuItem _openItem;
    private System.Windows.Forms.ToolStripMenuItem _recentMenu;
    private System.Windows.Forms.ToolStripMenuItem _closeItem;
    private System.Windows.Forms.ToolStripSeparator _fileSeparator1;
    private System.Windows.Forms.ToolStripMenuItem _saveItem;
    private System.Windows.Forms.ToolStripMenuItem _saveAsItem;
    private System.Windows.Forms.ToolStripSeparator _fileSeparator2;
    private System.Windows.Forms.ToolStripMenuItem _exitItem;
    private System.Windows.Forms.ToolStripMenuItem _editMenu;
    private System.Windows.Forms.ToolStripMenuItem _undoItem;
    private System.Windows.Forms.ToolStripMenuItem _redoItem;
    private System.Windows.Forms.ToolStripMenuItem _viewMenu;
    private System.Windows.Forms.ToolStripMenuItem _technicalItem;
    private System.Windows.Forms.ToolStripSeparator _viewSeparator;
    private System.Windows.Forms.ToolStripMenuItem _largerItem;
    private System.Windows.Forms.ToolStripMenuItem _smallerItem;
    private System.Windows.Forms.ToolStripMenuItem _normalItem;
    private System.Windows.Forms.ToolStripMenuItem _helpMenu;
    private System.Windows.Forms.ToolStripMenuItem _gettingStartedItem;
    private System.Windows.Forms.ToolStripMenuItem _supportedRomsItem;
    private System.Windows.Forms.ToolStripMenuItem _reportItem;
    private System.Windows.Forms.ToolStripSeparator _helpSeparator;
    private System.Windows.Forms.ToolStripMenuItem _aboutItem;
    private System.Windows.Forms.StatusStrip _status;
    private System.Windows.Forms.ToolStripStatusLabel _statusFile;
    private System.Windows.Forms.ToolStripStatusLabel _statusChanges;
    private System.Windows.Forms.ToolStripStatusLabel _statusKind;
    private System.Windows.Forms.ToolStripStatusLabel _statusChecksum;
    private System.Windows.Forms.SplitContainer _split;
    private System.Windows.Forms.TreeView _tree;
    private PageView _page;
}
