namespace HondaROMEditor {
	partial class MainForm {
		/// <summary>
		///  Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		///  Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing) {
			if (disposing && (components != null)) {
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		///  Required method for Designer support - do not modify
		///  the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() {
			components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
			StatusStrip = new StatusStrip();
			ReportingStatusLabel = new ToolStripStatusLabel();
			MenuStrip = new MenuStrip();
			FileMenuItem = new ToolStripMenuItem();
			EditMenuItem = new ToolStripMenuItem();
			HelpMenuItem = new ToolStripMenuItem();
			AboutMenuItem = new ToolStripMenuItem();
			ToolStrip = new ToolStrip();
			NavigateBackwardButton = new ToolStripSplitButton();
			NavigateForwardButton = new ToolStripButton();
			toolStripSeparator1 = new ToolStripSeparator();
			NewRomButton = new ToolStripButton();
			OpenRomButton = new ToolStripButton();
			SaveRomButton = new ToolStripButton();
			RomParametersButton = new ToolStripButton();
			toolStripSeparator2 = new ToolStripSeparator();
			UndoButton = new ToolStripSplitButton();
			RedoButton = new ToolStripSplitButton();
			toolStripSeparator3 = new ToolStripSeparator();
			ChipDeviceButton = new ToolStripButton();
			LiveDataButton = new ToolStripButton();
			DeviceSettingsButton = new ToolStripButton();
			AppSettingsButton = new ToolStripButton();
			SubmitBugButton = new ToolStripButton();
			DonateButton = new ToolStripButton();
			SplitContainer = new SplitContainer();
			RomSplitContainer = new SplitContainer();
			RomSettingsTableLayout = new TableLayoutPanel();
			RomParametersSearchBox = new TextBox();
			RomParametersTree = new TreeView();
			panel1 = new Panel();
			ReportingSplitContainer = new SplitContainer();
			ErrorListView = new ListView();
			ErrorList_Severity = new ColumnHeader();
			ErrorList_Description = new ColumnHeader();
			ErrorList_Location = new ColumnHeader();
			imageList1 = new ImageList(components);
			DebugLogListBox = new ListBox();
			StatusStrip.SuspendLayout();
			MenuStrip.SuspendLayout();
			ToolStrip.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)SplitContainer).BeginInit();
			SplitContainer.Panel1.SuspendLayout();
			SplitContainer.Panel2.SuspendLayout();
			SplitContainer.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)RomSplitContainer).BeginInit();
			RomSplitContainer.Panel1.SuspendLayout();
			RomSplitContainer.Panel2.SuspendLayout();
			RomSplitContainer.SuspendLayout();
			RomSettingsTableLayout.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)ReportingSplitContainer).BeginInit();
			ReportingSplitContainer.Panel1.SuspendLayout();
			ReportingSplitContainer.Panel2.SuspendLayout();
			ReportingSplitContainer.SuspendLayout();
			SuspendLayout();
			// 
			// StatusStrip
			// 
			StatusStrip.Font = new Font("Segoe UI Semibold", 8.25F, FontStyle.Bold);
			StatusStrip.Items.AddRange(new ToolStripItem[] { ReportingStatusLabel });
			StatusStrip.LayoutStyle = ToolStripLayoutStyle.HorizontalStackWithOverflow;
			StatusStrip.Location = new Point(0, 566);
			StatusStrip.Name = "StatusStrip";
			StatusStrip.ShowItemToolTips = true;
			StatusStrip.Size = new Size(932, 22);
			StatusStrip.TabIndex = 0;
			// 
			// ReportingStatusLabel
			// 
			ReportingStatusLabel.ActiveLinkColor = SystemColors.ControlText;
			ReportingStatusLabel.BorderSides = ToolStripStatusLabelBorderSides.Right;
			ReportingStatusLabel.LinkBehavior = LinkBehavior.HoverUnderline;
			ReportingStatusLabel.LinkColor = SystemColors.ControlText;
			ReportingStatusLabel.Name = "ReportingStatusLabel";
			ReportingStatusLabel.Size = new Size(113, 17);
			ReportingStatusLabel.Text = "0 Errors, 0 Warnings";
			ReportingStatusLabel.VisitedLinkColor = SystemColors.ControlText;
			ReportingStatusLabel.Click += ReportingStatusLabel_Click;
			// 
			// MenuStrip
			// 
			MenuStrip.Items.AddRange(new ToolStripItem[] { FileMenuItem, EditMenuItem, HelpMenuItem });
			MenuStrip.LayoutStyle = ToolStripLayoutStyle.HorizontalStackWithOverflow;
			MenuStrip.Location = new Point(0, 0);
			MenuStrip.Name = "MenuStrip";
			MenuStrip.Size = new Size(932, 24);
			MenuStrip.TabIndex = 1;
			MenuStrip.Text = "menuStrip1";
			// 
			// FileMenuItem
			// 
			FileMenuItem.Name = "FileMenuItem";
			FileMenuItem.Size = new Size(37, 20);
			FileMenuItem.Text = "File";
			// 
			// EditMenuItem
			// 
			EditMenuItem.Name = "EditMenuItem";
			EditMenuItem.Size = new Size(39, 20);
			EditMenuItem.Text = "Edit";
			// 
			// HelpMenuItem
			// 
			HelpMenuItem.DropDownItems.AddRange(new ToolStripItem[] { AboutMenuItem });
			HelpMenuItem.Name = "HelpMenuItem";
			HelpMenuItem.Size = new Size(44, 20);
			HelpMenuItem.Text = "Help";
			// 
			// AboutMenuItem
			// 
			AboutMenuItem.Name = "AboutMenuItem";
			AboutMenuItem.Size = new Size(107, 22);
			AboutMenuItem.Text = "About";
			AboutMenuItem.Click += AboutMenuItem_Click;
			// 
			// ToolStrip
			// 
			ToolStrip.Font = new Font("Segoe UI Semibold", 8.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
			ToolStrip.ImageScalingSize = new Size(48, 48);
			ToolStrip.Items.AddRange(new ToolStripItem[] { NavigateBackwardButton, NavigateForwardButton, toolStripSeparator1, NewRomButton, OpenRomButton, SaveRomButton, RomParametersButton, toolStripSeparator2, UndoButton, RedoButton, toolStripSeparator3, ChipDeviceButton, LiveDataButton, DeviceSettingsButton, AppSettingsButton, SubmitBugButton, DonateButton });
			ToolStrip.Location = new Point(0, 24);
			ToolStrip.Name = "ToolStrip";
			ToolStrip.Size = new Size(932, 68);
			ToolStrip.TabIndex = 2;
			ToolStrip.Text = "toolStrip1";
			// 
			// NavigateBackwardButton
			// 
			NavigateBackwardButton.Enabled = false;
			NavigateBackwardButton.Image = (Image)resources.GetObject("NavigateBackwardButton.Image");
			NavigateBackwardButton.ImageTransparentColor = Color.Magenta;
			NavigateBackwardButton.Name = "NavigateBackwardButton";
			NavigateBackwardButton.Size = new Size(72, 65);
			NavigateBackwardButton.Text = "Backward";
			NavigateBackwardButton.TextImageRelation = TextImageRelation.ImageAboveText;
			NavigateBackwardButton.ToolTipText = "Navigate Backward";
			// 
			// NavigateForwardButton
			// 
			NavigateForwardButton.Enabled = false;
			NavigateForwardButton.Image = (Image)resources.GetObject("NavigateForwardButton.Image");
			NavigateForwardButton.ImageTransparentColor = Color.Magenta;
			NavigateForwardButton.Name = "NavigateForwardButton";
			NavigateForwardButton.Size = new Size(53, 65);
			NavigateForwardButton.Text = "Forward";
			NavigateForwardButton.TextImageRelation = TextImageRelation.ImageAboveText;
			NavigateForwardButton.ToolTipText = "Navigate Forward";
			// 
			// toolStripSeparator1
			// 
			toolStripSeparator1.Name = "toolStripSeparator1";
			toolStripSeparator1.Size = new Size(6, 68);
			// 
			// NewRomButton
			// 
			NewRomButton.Image = (Image)resources.GetObject("NewRomButton.Image");
			NewRomButton.ImageTransparentColor = Color.Magenta;
			NewRomButton.Name = "NewRomButton";
			NewRomButton.Size = new Size(52, 65);
			NewRomButton.Text = "New";
			NewRomButton.TextImageRelation = TextImageRelation.ImageAboveText;
			NewRomButton.ToolTipText = "New ROM File";
			NewRomButton.Click += NewRomButton_Click;
			// 
			// OpenRomButton
			// 
			OpenRomButton.Image = (Image)resources.GetObject("OpenRomButton.Image");
			OpenRomButton.ImageTransparentColor = Color.Magenta;
			OpenRomButton.Name = "OpenRomButton";
			OpenRomButton.Size = new Size(52, 65);
			OpenRomButton.Text = "Open";
			OpenRomButton.TextImageRelation = TextImageRelation.ImageAboveText;
			OpenRomButton.ToolTipText = "Open ROM File";
			// 
			// SaveRomButton
			// 
			SaveRomButton.Enabled = false;
			SaveRomButton.Image = (Image)resources.GetObject("SaveRomButton.Image");
			SaveRomButton.ImageTransparentColor = Color.Magenta;
			SaveRomButton.Name = "SaveRomButton";
			SaveRomButton.Size = new Size(52, 65);
			SaveRomButton.Text = "Save";
			SaveRomButton.TextImageRelation = TextImageRelation.ImageAboveText;
			SaveRomButton.ToolTipText = "Save ROM File";
			// 
			// RomParametersButton
			// 
			RomParametersButton.CheckOnClick = true;
			RomParametersButton.Enabled = false;
			RomParametersButton.Image = (Image)resources.GetObject("RomParametersButton.Image");
			RomParametersButton.ImageTransparentColor = Color.Magenta;
			RomParametersButton.Name = "RomParametersButton";
			RomParametersButton.Size = new Size(68, 65);
			RomParametersButton.Text = "Parameters";
			RomParametersButton.TextImageRelation = TextImageRelation.ImageAboveText;
			RomParametersButton.ToolTipText = "ROM Parameters";
			RomParametersButton.Click += RomSettingsButton_Click;
			// 
			// toolStripSeparator2
			// 
			toolStripSeparator2.Name = "toolStripSeparator2";
			toolStripSeparator2.Size = new Size(6, 68);
			// 
			// UndoButton
			// 
			UndoButton.Enabled = false;
			UndoButton.Image = (Image)resources.GetObject("UndoButton.Image");
			UndoButton.ImageTransparentColor = Color.Magenta;
			UndoButton.Name = "UndoButton";
			UndoButton.Size = new Size(64, 65);
			UndoButton.Text = "Undo";
			UndoButton.TextImageRelation = TextImageRelation.ImageAboveText;
			// 
			// RedoButton
			// 
			RedoButton.Enabled = false;
			RedoButton.Image = (Image)resources.GetObject("RedoButton.Image");
			RedoButton.ImageTransparentColor = Color.Magenta;
			RedoButton.Name = "RedoButton";
			RedoButton.Size = new Size(64, 65);
			RedoButton.Text = "Redo";
			RedoButton.TextImageRelation = TextImageRelation.ImageAboveText;
			// 
			// toolStripSeparator3
			// 
			toolStripSeparator3.Name = "toolStripSeparator3";
			toolStripSeparator3.Size = new Size(6, 68);
			// 
			// ChipDeviceButton
			// 
			ChipDeviceButton.Enabled = false;
			ChipDeviceButton.Image = (Image)resources.GetObject("ChipDeviceButton.Image");
			ChipDeviceButton.ImageTransparentColor = Color.Magenta;
			ChipDeviceButton.Name = "ChipDeviceButton";
			ChipDeviceButton.Size = new Size(71, 65);
			ChipDeviceButton.Text = "Chip Device";
			ChipDeviceButton.TextImageRelation = TextImageRelation.ImageAboveText;
			// 
			// LiveDataButton
			// 
			LiveDataButton.Enabled = false;
			LiveDataButton.Image = (Image)resources.GetObject("LiveDataButton.Image");
			LiveDataButton.ImageTransparentColor = Color.Magenta;
			LiveDataButton.Name = "LiveDataButton";
			LiveDataButton.Size = new Size(58, 65);
			LiveDataButton.Text = "Live Data";
			LiveDataButton.TextImageRelation = TextImageRelation.ImageAboveText;
			// 
			// DeviceSettingsButton
			// 
			DeviceSettingsButton.Enabled = false;
			DeviceSettingsButton.Image = (Image)resources.GetObject("DeviceSettingsButton.Image");
			DeviceSettingsButton.ImageTransparentColor = Color.Magenta;
			DeviceSettingsButton.Name = "DeviceSettingsButton";
			DeviceSettingsButton.Size = new Size(89, 65);
			DeviceSettingsButton.Text = "Device Settings";
			DeviceSettingsButton.TextImageRelation = TextImageRelation.ImageAboveText;
			// 
			// AppSettingsButton
			// 
			AppSettingsButton.Alignment = ToolStripItemAlignment.Right;
			AppSettingsButton.Image = (Image)resources.GetObject("AppSettingsButton.Image");
			AppSettingsButton.ImageTransparentColor = Color.Magenta;
			AppSettingsButton.Name = "AppSettingsButton";
			AppSettingsButton.Size = new Size(52, 65);
			AppSettingsButton.Text = "Settings";
			AppSettingsButton.TextImageRelation = TextImageRelation.ImageAboveText;
			AppSettingsButton.ToolTipText = "Application Settings";
			// 
			// SubmitBugButton
			// 
			SubmitBugButton.Alignment = ToolStripItemAlignment.Right;
			SubmitBugButton.Image = (Image)resources.GetObject("SubmitBugButton.Image");
			SubmitBugButton.ImageTransparentColor = Color.Magenta;
			SubmitBugButton.Name = "SubmitBugButton";
			SubmitBugButton.Size = new Size(70, 65);
			SubmitBugButton.Text = "Submit Bug";
			SubmitBugButton.TextImageRelation = TextImageRelation.ImageAboveText;
			// 
			// DonateButton
			// 
			DonateButton.Alignment = ToolStripItemAlignment.Right;
			DonateButton.Image = (Image)resources.GetObject("DonateButton.Image");
			DonateButton.ImageTransparentColor = Color.Magenta;
			DonateButton.Name = "DonateButton";
			DonateButton.Size = new Size(52, 65);
			DonateButton.Text = "Donate";
			DonateButton.TextImageRelation = TextImageRelation.ImageAboveText;
			// 
			// SplitContainer
			// 
			SplitContainer.Dock = DockStyle.Fill;
			SplitContainer.FixedPanel = FixedPanel.Panel2;
			SplitContainer.Location = new Point(0, 92);
			SplitContainer.Name = "SplitContainer";
			SplitContainer.Orientation = Orientation.Horizontal;
			// 
			// SplitContainer.Panel1
			// 
			SplitContainer.Panel1.Controls.Add(RomSplitContainer);
			SplitContainer.Panel1MinSize = 300;
			// 
			// SplitContainer.Panel2
			// 
			SplitContainer.Panel2.Controls.Add(ReportingSplitContainer);
			SplitContainer.Panel2Collapsed = true;
			SplitContainer.Panel2MinSize = 100;
			SplitContainer.Size = new Size(932, 474);
			SplitContainer.SplitterDistance = 300;
			SplitContainer.SplitterWidth = 10;
			SplitContainer.TabIndex = 3;
			// 
			// RomSplitContainer
			// 
			RomSplitContainer.BorderStyle = BorderStyle.FixedSingle;
			RomSplitContainer.Dock = DockStyle.Fill;
			RomSplitContainer.FixedPanel = FixedPanel.Panel1;
			RomSplitContainer.Location = new Point(0, 0);
			RomSplitContainer.Margin = new Padding(0);
			RomSplitContainer.Name = "RomSplitContainer";
			// 
			// RomSplitContainer.Panel1
			// 
			RomSplitContainer.Panel1.Controls.Add(RomSettingsTableLayout);
			RomSplitContainer.Panel1.Padding = new Padding(15);
			RomSplitContainer.Panel1Collapsed = true;
			RomSplitContainer.Panel1MinSize = 160;
			// 
			// RomSplitContainer.Panel2
			// 
			RomSplitContainer.Panel2.Controls.Add(panel1);
			RomSplitContainer.Panel2.Padding = new Padding(15);
			RomSplitContainer.Panel2MinSize = 500;
			RomSplitContainer.Size = new Size(932, 474);
			RomSplitContainer.SplitterDistance = 230;
			RomSplitContainer.TabIndex = 2;
			// 
			// RomSettingsTableLayout
			// 
			RomSettingsTableLayout.AutoSize = true;
			RomSettingsTableLayout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			RomSettingsTableLayout.CellBorderStyle = TableLayoutPanelCellBorderStyle.Single;
			RomSettingsTableLayout.ColumnCount = 1;
			RomSettingsTableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			RomSettingsTableLayout.Controls.Add(RomParametersSearchBox, 0, 0);
			RomSettingsTableLayout.Controls.Add(RomParametersTree, 0, 1);
			RomSettingsTableLayout.Dock = DockStyle.Fill;
			RomSettingsTableLayout.Location = new Point(15, 15);
			RomSettingsTableLayout.Margin = new Padding(0);
			RomSettingsTableLayout.Name = "RomSettingsTableLayout";
			RomSettingsTableLayout.RowCount = 2;
			RomSettingsTableLayout.RowStyles.Add(new RowStyle());
			RomSettingsTableLayout.RowStyles.Add(new RowStyle());
			RomSettingsTableLayout.Size = new Size(198, 68);
			RomSettingsTableLayout.TabIndex = 0;
			// 
			// RomParametersSearchBox
			// 
			RomParametersSearchBox.BorderStyle = BorderStyle.FixedSingle;
			RomParametersSearchBox.Dock = DockStyle.Fill;
			RomParametersSearchBox.Location = new Point(31, 11);
			RomParametersSearchBox.Margin = new Padding(30, 10, 30, 10);
			RomParametersSearchBox.Name = "RomParametersSearchBox";
			RomParametersSearchBox.PlaceholderText = "Search Parameters...";
			RomParametersSearchBox.Size = new Size(136, 23);
			RomParametersSearchBox.TabIndex = 2;
			RomParametersSearchBox.TextAlign = HorizontalAlignment.Center;
			RomParametersSearchBox.WordWrap = false;
			RomParametersSearchBox.KeyDown += RomSettingsSearchBox_KeyDown;
			// 
			// RomParametersTree
			// 
			RomParametersTree.Dock = DockStyle.Fill;
			RomParametersTree.ItemHeight = 18;
			RomParametersTree.Location = new Point(6, 50);
			RomParametersTree.Margin = new Padding(5);
			RomParametersTree.Name = "RomParametersTree";
			RomParametersTree.ShowNodeToolTips = true;
			RomParametersTree.Size = new Size(186, 386);
			RomParametersTree.TabIndex = 1;
			// 
			// panel1
			// 
			panel1.Dock = DockStyle.Fill;
			panel1.Location = new Point(15, 15);
			panel1.Name = "panel1";
			panel1.Size = new Size(900, 442);
			panel1.TabIndex = 1;
			// 
			// ReportingSplitContainer
			// 
			ReportingSplitContainer.BorderStyle = BorderStyle.FixedSingle;
			ReportingSplitContainer.Dock = DockStyle.Fill;
			ReportingSplitContainer.FixedPanel = FixedPanel.Panel2;
			ReportingSplitContainer.Location = new Point(0, 0);
			ReportingSplitContainer.Name = "ReportingSplitContainer";
			// 
			// ReportingSplitContainer.Panel1
			// 
			ReportingSplitContainer.Panel1.Controls.Add(ErrorListView);
			// 
			// ReportingSplitContainer.Panel2
			// 
			ReportingSplitContainer.Panel2.Controls.Add(DebugLogListBox);
			ReportingSplitContainer.Size = new Size(150, 46);
			ReportingSplitContainer.SplitterDistance = 25;
			ReportingSplitContainer.TabIndex = 0;
			// 
			// ErrorListView
			// 
			ErrorListView.Activation = ItemActivation.OneClick;
			ErrorListView.BorderStyle = BorderStyle.None;
			ErrorListView.Columns.AddRange(new ColumnHeader[] { ErrorList_Severity, ErrorList_Description, ErrorList_Location });
			ErrorListView.Dock = DockStyle.Fill;
			ErrorListView.FullRowSelect = true;
			ErrorListView.Location = new Point(0, 0);
			ErrorListView.Name = "ErrorListView";
			ErrorListView.Size = new Size(23, 44);
			ErrorListView.SmallImageList = imageList1;
			ErrorListView.TabIndex = 1;
			ErrorListView.UseCompatibleStateImageBehavior = false;
			ErrorListView.View = View.Details;
			// 
			// ErrorList_Severity
			// 
			ErrorList_Severity.Text = "Severity";
			// 
			// ErrorList_Description
			// 
			ErrorList_Description.Text = "Description";
			ErrorList_Description.Width = 500;
			// 
			// ErrorList_Location
			// 
			ErrorList_Location.Text = "Location";
			ErrorList_Location.TextAlign = HorizontalAlignment.Center;
			ErrorList_Location.Width = 150;
			// 
			// imageList1
			// 
			imageList1.ColorDepth = ColorDepth.Depth32Bit;
			imageList1.ImageStream = (ImageListStreamer)resources.GetObject("imageList1.ImageStream");
			imageList1.TransparentColor = Color.Transparent;
			imageList1.Images.SetKeyName(0, "info_20dp_E8EAED_FILL0_wght200_GRAD0_opsz20.png");
			// 
			// DebugLogListBox
			// 
			DebugLogListBox.BorderStyle = BorderStyle.None;
			DebugLogListBox.Dock = DockStyle.Fill;
			DebugLogListBox.FormattingEnabled = true;
			DebugLogListBox.HorizontalScrollbar = true;
			DebugLogListBox.Location = new Point(0, 0);
			DebugLogListBox.Name = "DebugLogListBox";
			DebugLogListBox.Size = new Size(119, 44);
			DebugLogListBox.TabIndex = 0;
			// 
			// MainForm
			// 
			AutoScaleDimensions = new SizeF(7F, 15F);
			AutoScaleMode = AutoScaleMode.Font;
			AutoSize = true;
			ClientSize = new Size(932, 588);
			Controls.Add(SplitContainer);
			Controls.Add(ToolStrip);
			Controls.Add(StatusStrip);
			Controls.Add(MenuStrip);
			Icon = (Icon)resources.GetObject("$this.Icon");
			MainMenuStrip = MenuStrip;
			Name = "MainForm";
			StartPosition = FormStartPosition.CenterScreen;
			Text = "Honda ROM Editor (OBD1)";
			Load += MainForm_Load;
			Shown += MainForm_Shown;
			HelpRequested += MainForm_HelpRequested;
			StatusStrip.ResumeLayout(false);
			StatusStrip.PerformLayout();
			MenuStrip.ResumeLayout(false);
			MenuStrip.PerformLayout();
			ToolStrip.ResumeLayout(false);
			ToolStrip.PerformLayout();
			SplitContainer.Panel1.ResumeLayout(false);
			SplitContainer.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)SplitContainer).EndInit();
			SplitContainer.ResumeLayout(false);
			RomSplitContainer.Panel1.ResumeLayout(false);
			RomSplitContainer.Panel1.PerformLayout();
			RomSplitContainer.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)RomSplitContainer).EndInit();
			RomSplitContainer.ResumeLayout(false);
			RomSettingsTableLayout.ResumeLayout(false);
			RomSettingsTableLayout.PerformLayout();
			ReportingSplitContainer.Panel1.ResumeLayout(false);
			ReportingSplitContainer.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)ReportingSplitContainer).EndInit();
			ReportingSplitContainer.ResumeLayout(false);
			ResumeLayout(false);
			PerformLayout();
		}

		#endregion

		private StatusStrip StatusStrip;
		private MenuStrip MenuStrip;
		private ToolStripMenuItem FileMenuItem;
		private ToolStripMenuItem EditMenuItem;
		private ToolStripMenuItem HelpMenuItem;
		private ToolStrip ToolStrip;
		private ToolStripSeparator toolStripSeparator1;
		private ToolStripSplitButton NavigateBackwardButton;
		private ToolStripButton NavigateForwardButton;
		private ToolStripButton NewRomButton;
		private ToolStripButton OpenRomButton;
		private ToolStripButton SaveRomButton;
		private ToolStripSeparator toolStripSeparator2;
		private ToolStripSplitButton UndoButton;
		private ToolStripSplitButton RedoButton;
		private ToolStripSeparator toolStripSeparator3;
		private ToolStripButton DeviceSettingsButton;
		private ToolStripButton RomParametersButton;
		private ToolStripButton AppSettingsButton;
		private ToolStripButton ChipDeviceButton;
		private ToolStripButton LiveDataButton;
		private ToolStripButton SubmitBugButton;
		private ToolStripButton DonateButton;
		private ToolStripMenuItem AboutMenuItem;
		private SplitContainer SplitContainer;
		private TreeView RomParametersTree;
		private SplitContainer RomSplitContainer;
		private SplitContainer ReportingSplitContainer;
		public ListBox DebugLogListBox;
		public ListView ErrorListView;
		private ColumnHeader ErrorList_Severity;
		private ColumnHeader ErrorList_Description;
		private ColumnHeader ErrorList_Location;
		private ImageList imageList1;
		private TextBox RomParametersSearchBox;
		private TableLayoutPanel RomSettingsTableLayout;
		private ToolStripStatusLabel ReportingStatusLabel;
		private Panel panel1;
	}
}
