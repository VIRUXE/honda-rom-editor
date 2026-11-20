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
        this.menu = new System.Windows.Forms.MenuStrip();
        this.fileMenu = new System.Windows.Forms.ToolStripMenuItem();
        this.openItem = new System.Windows.Forms.ToolStripMenuItem();
        this.recentMenu = new System.Windows.Forms.ToolStripMenuItem();
        this.closeItem = new System.Windows.Forms.ToolStripMenuItem();
        this.fileSeparator1 = new System.Windows.Forms.ToolStripSeparator();
        this.saveItem = new System.Windows.Forms.ToolStripMenuItem();
        this.saveAsItem = new System.Windows.Forms.ToolStripMenuItem();
        this.fileSeparator2 = new System.Windows.Forms.ToolStripSeparator();
        this.exitItem = new System.Windows.Forms.ToolStripMenuItem();
        this.editMenu = new System.Windows.Forms.ToolStripMenuItem();
        this.undoItem = new System.Windows.Forms.ToolStripMenuItem();
        this.redoItem = new System.Windows.Forms.ToolStripMenuItem();
        this.viewMenu = new System.Windows.Forms.ToolStripMenuItem();
        this.technicalItem = new System.Windows.Forms.ToolStripMenuItem();
        this.viewSeparator = new System.Windows.Forms.ToolStripSeparator();
        this.largerItem = new System.Windows.Forms.ToolStripMenuItem();
        this.smallerItem = new System.Windows.Forms.ToolStripMenuItem();
        this.normalItem = new System.Windows.Forms.ToolStripMenuItem();
        this.helpMenu = new System.Windows.Forms.ToolStripMenuItem();
        this.gettingStartedItem = new System.Windows.Forms.ToolStripMenuItem();
        this.supportedRomsItem = new System.Windows.Forms.ToolStripMenuItem();
        this.reportItem = new System.Windows.Forms.ToolStripMenuItem();
        this.helpSeparator = new System.Windows.Forms.ToolStripSeparator();
        this.aboutItem = new System.Windows.Forms.ToolStripMenuItem();
        this.status = new System.Windows.Forms.StatusStrip();
        this.statusFile = new System.Windows.Forms.ToolStripStatusLabel();
        this.statusChanges = new System.Windows.Forms.ToolStripStatusLabel();
        this.statusKind = new System.Windows.Forms.ToolStripStatusLabel();
        this.statusChecksum = new System.Windows.Forms.ToolStripStatusLabel();
        this.split = new System.Windows.Forms.SplitContainer();
        this.tree = new System.Windows.Forms.TreeView();
        this.page = new HRE.App.PageView();
        this.menu.SuspendLayout();
        this.status.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this.split)).BeginInit();
        this.split.Panel1.SuspendLayout();
        this.split.Panel2.SuspendLayout();
        this.split.SuspendLayout();
        this.SuspendLayout();
        //
        // menu
        //
        this.menu.AccessibleName = "Menu";
        this.menu.AllowClickThrough = true;
        this.menu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileMenu,
            this.editMenu,
            this.viewMenu,
            this.helpMenu});
        this.menu.Location = new System.Drawing.Point(0, 0);
        this.menu.Name = "menu";
        this.menu.Padding = new System.Windows.Forms.Padding(4, 2, 0, 2);
        this.menu.Size = new System.Drawing.Size(1180, 24);
        this.menu.TabIndex = 2;
        //
        // fileMenu
        //
        this.fileMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openItem,
            this.recentMenu,
            this.closeItem,
            this.fileSeparator1,
            this.saveItem,
            this.saveAsItem,
            this.fileSeparator2,
            this.exitItem});
        this.fileMenu.Name = "fileMenu";
        this.fileMenu.Text = "&File";
        //
        // openItem
        //
        this.openItem.AccessibleDescription = "Choose a ROM file to open";
        this.openItem.Name = "openItem";
        this.openItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
        this.openItem.Text = "&Open...";
        this.openItem.Click += new System.EventHandler(this.OpenItem_Click);
        //
        // recentMenu
        //
        this.recentMenu.AccessibleDescription = "Open a file you opened lately";
        this.recentMenu.Name = "recentMenu";
        this.recentMenu.Text = "&Recent files";
        //
        // closeItem
        //
        this.closeItem.AccessibleDescription = "Close the file that is open";
        this.closeItem.Name = "closeItem";
        this.closeItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.W)));
        this.closeItem.Text = "&Close";
        this.closeItem.Click += new System.EventHandler(this.CloseItem_Click);
        //
        // fileSeparator1
        //
        this.fileSeparator1.Name = "fileSeparator1";
        //
        // saveItem
        //
        this.saveItem.AccessibleDescription = "Save the changes to the file, with the checksum corrected";
        this.saveItem.Name = "saveItem";
        this.saveItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
        this.saveItem.Text = "&Save";
        this.saveItem.Click += new System.EventHandler(this.SaveItem_Click);
        //
        // saveAsItem
        //
        this.saveAsItem.AccessibleDescription = "Save the ROM as another file, and go on working in that one";
        this.saveAsItem.Name = "saveAsItem";
        this.saveAsItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) | System.Windows.Forms.Keys.S)));
        this.saveAsItem.Text = "Save &as...";
        this.saveAsItem.Click += new System.EventHandler(this.SaveAsItem_Click);
        //
        // fileSeparator2
        //
        this.fileSeparator2.Name = "fileSeparator2";
        //
        // exitItem
        //
        this.exitItem.AccessibleDescription = "Close the program";
        this.exitItem.Name = "exitItem";
        this.exitItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.F4)));
        this.exitItem.Text = "E&xit";
        this.exitItem.Click += new System.EventHandler(this.ExitItem_Click);
        //
        // editMenu
        //
        this.editMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.undoItem,
            this.redoItem});
        this.editMenu.Name = "editMenu";
        this.editMenu.Text = "&Edit";
        //
        // undoItem
        //
        this.undoItem.AccessibleDescription = "Take back the last change";
        this.undoItem.Name = "undoItem";
        this.undoItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Z)));
        this.undoItem.Text = "&Undo";
        this.undoItem.Click += new System.EventHandler(this.UndoItem_Click);
        //
        // redoItem
        //
        this.redoItem.AccessibleDescription = "Make the change again that was taken back";
        this.redoItem.Name = "redoItem";
        this.redoItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Y)));
        this.redoItem.Text = "&Redo";
        this.redoItem.Click += new System.EventHandler(this.RedoItem_Click);
        //
        // viewMenu
        //
        this.viewMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.technicalItem,
            this.viewSeparator,
            this.largerItem,
            this.smallerItem,
            this.normalItem});
        this.viewMenu.Name = "viewMenu";
        this.viewMenu.Text = "&View";
        //
        // technicalItem
        //
        this.technicalItem.AccessibleDescription = "Show or hide addresses, hashes and other details of how things are stored";
        this.technicalItem.Name = "technicalItem";
        this.technicalItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.T)));
        this.technicalItem.Text = "&Technical details";
        this.technicalItem.Click += new System.EventHandler(this.TechnicalItem_Click);
        //
        // viewSeparator
        //
        this.viewSeparator.Name = "viewSeparator";
        //
        // largerItem
        //
        this.largerItem.AccessibleDescription = "Make the text of the window larger";
        this.largerItem.Name = "largerItem";
        this.largerItem.ShortcutKeyDisplayString = "Ctrl++";
        this.largerItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Oemplus)));
        this.largerItem.Text = "&Larger text";
        this.largerItem.Click += new System.EventHandler(this.LargerItem_Click);
        //
        // smallerItem
        //
        this.smallerItem.AccessibleDescription = "Make the text of the window smaller";
        this.smallerItem.Name = "smallerItem";
        this.smallerItem.ShortcutKeyDisplayString = "Ctrl+-";
        this.smallerItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.OemMinus)));
        this.smallerItem.Text = "&Smaller text";
        this.smallerItem.Click += new System.EventHandler(this.SmallerItem_Click);
        //
        // normalItem
        //
        this.normalItem.AccessibleDescription = "Bring the text back to its normal size";
        this.normalItem.Name = "normalItem";
        this.normalItem.ShortcutKeyDisplayString = "Ctrl+0";
        this.normalItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.D0)));
        this.normalItem.Text = "&Normal text size";
        this.normalItem.Click += new System.EventHandler(this.NormalItem_Click);
        //
        // helpMenu
        //
        this.helpMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.gettingStartedItem,
            this.supportedRomsItem,
            this.reportItem,
            this.helpSeparator,
            this.aboutItem});
        this.helpMenu.Name = "helpMenu";
        this.helpMenu.Text = "&Help";
        //
        // gettingStartedItem
        //
        this.gettingStartedItem.AccessibleDescription = "Read how the window works, in the browser";
        this.gettingStartedItem.Name = "gettingStartedItem";
        this.gettingStartedItem.ShortcutKeys = System.Windows.Forms.Keys.F1;
        this.gettingStartedItem.Text = "&Getting started";
        this.gettingStartedItem.Click += new System.EventHandler(this.GettingStartedItem_Click);
        //
        // supportedRomsItem
        //
        this.supportedRomsItem.AccessibleDescription = "Read which ROMs this version can open, in the browser";
        this.supportedRomsItem.Name = "supportedRomsItem";
        this.supportedRomsItem.Text = "&Which ROMs can be opened";
        this.supportedRomsItem.Click += new System.EventHandler(this.SupportedRomsItem_Click);
        //
        // reportItem
        //
        this.reportItem.AccessibleDescription = "Tell the makers what went wrong, in the browser";
        this.reportItem.Name = "reportItem";
        this.reportItem.Text = "&Report a problem";
        this.reportItem.Click += new System.EventHandler(this.ReportItem_Click);
        //
        // helpSeparator
        //
        this.helpSeparator.Name = "helpSeparator";
        //
        // aboutItem
        //
        this.aboutItem.AccessibleDescription = "See the version of the program";
        this.aboutItem.Name = "aboutItem";
        this.aboutItem.Text = "&About Honda ROM Editor";
        this.aboutItem.Click += new System.EventHandler(this.AboutItem_Click);
        //
        // status
        //
        this.status.AccessibleName = "Status";
        this.status.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.statusFile,
            this.statusChanges,
            this.statusKind,
            this.statusChecksum});
        this.status.Location = new System.Drawing.Point(0, 738);
        this.status.Name = "status";
        this.status.ShowItemToolTips = true;
        this.status.Size = new System.Drawing.Size(1180, 22);
        this.status.SizingGrip = false;
        this.status.TabIndex = 1;
        //
        // statusFile
        //
        this.statusFile.Name = "statusFile";
        this.statusFile.Spring = true;
        this.statusFile.Text = "No file open";
        this.statusFile.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        //
        // statusChanges
        //
        this.statusChanges.Available = false;
        this.statusChanges.Name = "statusChanges";
        this.statusChanges.Padding = new System.Windows.Forms.Padding(14, 0, 0, 0);
        this.statusChanges.Text = "Changes not saved";
        this.statusChanges.ToolTipText = "Save them with File > Save (Ctrl+S)";
        //
        // statusKind
        //
        this.statusKind.Name = "statusKind";
        this.statusKind.Padding = new System.Windows.Forms.Padding(14, 0, 0, 0);
        this.statusKind.ToolTipText = "The kind of ROM this file was recognised as";
        //
        // statusChecksum
        //
        this.statusChecksum.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
        this.statusChecksum.Name = "statusChecksum";
        this.statusChecksum.Padding = new System.Windows.Forms.Padding(14, 0, 0, 0);
        this.statusChecksum.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
        //
        // split
        //
        this.split.AccessibleName = "Divider between the list and the details";
        this.split.Dock = System.Windows.Forms.DockStyle.Fill;
        this.split.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
        this.split.Location = new System.Drawing.Point(0, 24);
        this.split.Name = "split";
        //
        // split.Panel1
        //
        this.split.Panel1.Controls.Add(this.tree);
        this.split.Panel1.Padding = new System.Windows.Forms.Padding(0, 8, 0, 0);
        //
        // split.Panel2
        //
        this.split.Panel2.Controls.Add(this.page);
        this.split.Size = new System.Drawing.Size(1180, 714);
        this.split.SplitterDistance = 280;
        this.split.TabIndex = 0;
        this.split.Paint += new System.Windows.Forms.PaintEventHandler(this.PaintDivider);
        //
        // tree
        //
        this.tree.AccessibleName = "Tables and settings";
        this.tree.BorderStyle = System.Windows.Forms.BorderStyle.None;
        this.tree.Dock = System.Windows.Forms.DockStyle.Fill;
        this.tree.DrawMode = System.Windows.Forms.TreeViewDrawMode.OwnerDrawText;
        this.tree.FullRowSelect = true;
        this.tree.HideSelection = false;
        this.tree.Location = new System.Drawing.Point(0, 8);
        this.tree.Name = "tree";
        this.tree.ShowLines = false;
        this.tree.ShowNodeToolTips = true;
        this.tree.ShowPlusMinus = false;
        this.tree.ShowRootLines = false;
        this.tree.Size = new System.Drawing.Size(280, 706);
        this.tree.TabIndex = 0;
        this.tree.BeforeCollapse += new System.Windows.Forms.TreeViewCancelEventHandler(this.Tree_BeforeCollapse);
        this.tree.DrawNode += new System.Windows.Forms.DrawTreeNodeEventHandler(this.DrawNode);
        this.tree.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.Tree_AfterSelect);
        this.tree.GotFocus += new System.EventHandler(this.Tree_FocusChanged);
        this.tree.LostFocus += new System.EventHandler(this.Tree_FocusChanged);
        //
        // page
        //
        this.page.Dock = System.Windows.Forms.DockStyle.Fill;
        this.page.Location = new System.Drawing.Point(0, 0);
        this.page.Name = "page";
        this.page.Size = new System.Drawing.Size(896, 714);
        this.page.TabIndex = 0;
        //
        // MainForm
        //
        this.AllowDrop = true;
        this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
        this.ClientSize = new System.Drawing.Size(1180, 760);
        // Docking goes by z-order: the control added last takes its edge first.
        this.Controls.Add(this.split);
        this.Controls.Add(this.status);
        this.Controls.Add(this.menu);
        this.MainMenuStrip = this.menu;
        this.MinimumSize = new System.Drawing.Size(560, 380);
        this.Name = "MainForm";
        this.Text = "Honda ROM Editor";
        this.menu.ResumeLayout(false);
        this.menu.PerformLayout();
        this.status.ResumeLayout(false);
        this.status.PerformLayout();
        this.split.Panel1.ResumeLayout(false);
        this.split.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)(this.split)).EndInit();
        this.split.ResumeLayout(false);
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    #endregion

    private System.Windows.Forms.MenuStrip menu;
    private System.Windows.Forms.ToolStripMenuItem fileMenu;
    private System.Windows.Forms.ToolStripMenuItem openItem;
    private System.Windows.Forms.ToolStripMenuItem recentMenu;
    private System.Windows.Forms.ToolStripMenuItem closeItem;
    private System.Windows.Forms.ToolStripSeparator fileSeparator1;
    private System.Windows.Forms.ToolStripMenuItem saveItem;
    private System.Windows.Forms.ToolStripMenuItem saveAsItem;
    private System.Windows.Forms.ToolStripSeparator fileSeparator2;
    private System.Windows.Forms.ToolStripMenuItem exitItem;
    private System.Windows.Forms.ToolStripMenuItem editMenu;
    private System.Windows.Forms.ToolStripMenuItem undoItem;
    private System.Windows.Forms.ToolStripMenuItem redoItem;
    private System.Windows.Forms.ToolStripMenuItem viewMenu;
    private System.Windows.Forms.ToolStripMenuItem technicalItem;
    private System.Windows.Forms.ToolStripSeparator viewSeparator;
    private System.Windows.Forms.ToolStripMenuItem largerItem;
    private System.Windows.Forms.ToolStripMenuItem smallerItem;
    private System.Windows.Forms.ToolStripMenuItem normalItem;
    private System.Windows.Forms.ToolStripMenuItem helpMenu;
    private System.Windows.Forms.ToolStripMenuItem gettingStartedItem;
    private System.Windows.Forms.ToolStripMenuItem supportedRomsItem;
    private System.Windows.Forms.ToolStripMenuItem reportItem;
    private System.Windows.Forms.ToolStripSeparator helpSeparator;
    private System.Windows.Forms.ToolStripMenuItem aboutItem;
    private System.Windows.Forms.StatusStrip status;
    private System.Windows.Forms.ToolStripStatusLabel statusFile;
    private System.Windows.Forms.ToolStripStatusLabel statusChanges;
    private System.Windows.Forms.ToolStripStatusLabel statusKind;
    private System.Windows.Forms.ToolStripStatusLabel statusChecksum;
    private System.Windows.Forms.SplitContainer split;
    private System.Windows.Forms.TreeView tree;
    private PageView page;
}
