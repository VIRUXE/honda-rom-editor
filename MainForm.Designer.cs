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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
			StatusStrip = new StatusStrip();
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
			RomSettingsButton = new ToolStripButton();
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
			MenuStrip.SuspendLayout();
			ToolStrip.SuspendLayout();
			SuspendLayout();
			// 
			// StatusStrip
			// 
			StatusStrip.LayoutStyle = ToolStripLayoutStyle.HorizontalStackWithOverflow;
			StatusStrip.Location = new Point(0, 681);
			StatusStrip.Name = "StatusStrip";
			StatusStrip.ShowItemToolTips = true;
			StatusStrip.Size = new Size(1093, 22);
			StatusStrip.TabIndex = 0;
			// 
			// MenuStrip
			// 
			MenuStrip.Items.AddRange(new ToolStripItem[] { FileMenuItem, EditMenuItem, HelpMenuItem });
			MenuStrip.Location = new Point(0, 0);
			MenuStrip.Name = "MenuStrip";
			MenuStrip.Size = new Size(1093, 24);
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
			ToolStrip.ImageScalingSize = new Size(48, 48);
			ToolStrip.Items.AddRange(new ToolStripItem[] { NavigateBackwardButton, NavigateForwardButton, toolStripSeparator1, NewRomButton, OpenRomButton, SaveRomButton, RomSettingsButton, toolStripSeparator2, UndoButton, RedoButton, toolStripSeparator3, ChipDeviceButton, LiveDataButton, DeviceSettingsButton, AppSettingsButton, SubmitBugButton, DonateButton });
			ToolStrip.Location = new Point(0, 24);
			ToolStrip.Name = "ToolStrip";
			ToolStrip.Size = new Size(1093, 70);
			ToolStrip.TabIndex = 2;
			ToolStrip.Text = "toolStrip1";
			// 
			// NavigateBackwardButton
			// 
			NavigateBackwardButton.Enabled = false;
			NavigateBackwardButton.Image = (Image)resources.GetObject("NavigateBackwardButton.Image");
			NavigateBackwardButton.ImageTransparentColor = Color.Magenta;
			NavigateBackwardButton.Name = "NavigateBackwardButton";
			NavigateBackwardButton.Size = new Size(74, 67);
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
			NavigateForwardButton.Size = new Size(54, 67);
			NavigateForwardButton.Text = "Forward";
			NavigateForwardButton.TextImageRelation = TextImageRelation.ImageAboveText;
			NavigateForwardButton.ToolTipText = "Navigate Forward";
			// 
			// toolStripSeparator1
			// 
			toolStripSeparator1.Name = "toolStripSeparator1";
			toolStripSeparator1.Size = new Size(6, 70);
			// 
			// NewRomButton
			// 
			NewRomButton.Image = (Image)resources.GetObject("NewRomButton.Image");
			NewRomButton.ImageTransparentColor = Color.Magenta;
			NewRomButton.Name = "NewRomButton";
			NewRomButton.Size = new Size(86, 67);
			NewRomButton.Text = "New ROM File";
			NewRomButton.TextImageRelation = TextImageRelation.ImageAboveText;
			// 
			// OpenRomButton
			// 
			OpenRomButton.Image = (Image)resources.GetObject("OpenRomButton.Image");
			OpenRomButton.ImageTransparentColor = Color.Magenta;
			OpenRomButton.Name = "OpenRomButton";
			OpenRomButton.Size = new Size(91, 67);
			OpenRomButton.Text = "Open ROM File";
			OpenRomButton.TextImageRelation = TextImageRelation.ImageAboveText;
			// 
			// SaveRomButton
			// 
			SaveRomButton.Enabled = false;
			SaveRomButton.Image = (Image)resources.GetObject("SaveRomButton.Image");
			SaveRomButton.ImageTransparentColor = Color.Magenta;
			SaveRomButton.Name = "SaveRomButton";
			SaveRomButton.Size = new Size(86, 67);
			SaveRomButton.Text = "Save ROM File";
			SaveRomButton.TextImageRelation = TextImageRelation.ImageAboveText;
			// 
			// RomSettingsButton
			// 
			RomSettingsButton.Enabled = false;
			RomSettingsButton.Image = (Image)resources.GetObject("RomSettingsButton.Image");
			RomSettingsButton.ImageTransparentColor = Color.Magenta;
			RomSettingsButton.Name = "RomSettingsButton";
			RomSettingsButton.Size = new Size(83, 67);
			RomSettingsButton.Text = "ROM Settings";
			RomSettingsButton.TextImageRelation = TextImageRelation.ImageAboveText;
			// 
			// toolStripSeparator2
			// 
			toolStripSeparator2.Name = "toolStripSeparator2";
			toolStripSeparator2.Size = new Size(6, 70);
			// 
			// UndoButton
			// 
			UndoButton.Enabled = false;
			UndoButton.Image = (Image)resources.GetObject("UndoButton.Image");
			UndoButton.ImageTransparentColor = Color.Magenta;
			UndoButton.Name = "UndoButton";
			UndoButton.Size = new Size(64, 67);
			UndoButton.Text = "Undo";
			UndoButton.TextImageRelation = TextImageRelation.ImageAboveText;
			// 
			// RedoButton
			// 
			RedoButton.Enabled = false;
			RedoButton.Image = (Image)resources.GetObject("RedoButton.Image");
			RedoButton.ImageTransparentColor = Color.Magenta;
			RedoButton.Name = "RedoButton";
			RedoButton.Size = new Size(64, 67);
			RedoButton.Text = "Redo";
			RedoButton.TextImageRelation = TextImageRelation.ImageAboveText;
			// 
			// toolStripSeparator3
			// 
			toolStripSeparator3.Name = "toolStripSeparator3";
			toolStripSeparator3.Size = new Size(6, 70);
			// 
			// ChipDeviceButton
			// 
			ChipDeviceButton.Enabled = false;
			ChipDeviceButton.Image = (Image)resources.GetObject("ChipDeviceButton.Image");
			ChipDeviceButton.ImageTransparentColor = Color.Magenta;
			ChipDeviceButton.Name = "ChipDeviceButton";
			ChipDeviceButton.Size = new Size(74, 67);
			ChipDeviceButton.Text = "Chip Device";
			ChipDeviceButton.TextImageRelation = TextImageRelation.ImageAboveText;
			// 
			// LiveDataButton
			// 
			LiveDataButton.Enabled = false;
			LiveDataButton.Image = (Image)resources.GetObject("LiveDataButton.Image");
			LiveDataButton.ImageTransparentColor = Color.Magenta;
			LiveDataButton.Name = "LiveDataButton";
			LiveDataButton.Size = new Size(59, 67);
			LiveDataButton.Text = "Live Data";
			LiveDataButton.TextImageRelation = TextImageRelation.ImageAboveText;
			// 
			// DeviceSettingsButton
			// 
			DeviceSettingsButton.Enabled = false;
			DeviceSettingsButton.Image = (Image)resources.GetObject("DeviceSettingsButton.Image");
			DeviceSettingsButton.ImageTransparentColor = Color.Magenta;
			DeviceSettingsButton.Name = "DeviceSettingsButton";
			DeviceSettingsButton.Size = new Size(91, 67);
			DeviceSettingsButton.Text = "Device Settings";
			DeviceSettingsButton.TextImageRelation = TextImageRelation.ImageAboveText;
			// 
			// AppSettingsButton
			// 
			AppSettingsButton.Alignment = ToolStripItemAlignment.Right;
			AppSettingsButton.Image = (Image)resources.GetObject("AppSettingsButton.Image");
			AppSettingsButton.ImageTransparentColor = Color.Magenta;
			AppSettingsButton.Name = "AppSettingsButton";
			AppSettingsButton.Size = new Size(53, 67);
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
			SubmitBugButton.Size = new Size(73, 67);
			SubmitBugButton.Text = "Submit Bug";
			SubmitBugButton.TextImageRelation = TextImageRelation.ImageAboveText;
			// 
			// DonateButton
			// 
			DonateButton.Alignment = ToolStripItemAlignment.Right;
			DonateButton.Image = (Image)resources.GetObject("DonateButton.Image");
			DonateButton.ImageTransparentColor = Color.Magenta;
			DonateButton.Name = "DonateButton";
			DonateButton.Size = new Size(52, 67);
			DonateButton.Text = "Donate";
			DonateButton.TextImageRelation = TextImageRelation.ImageAboveText;
			// 
			// MainForm
			// 
			AutoScaleDimensions = new SizeF(7F, 15F);
			AutoScaleMode = AutoScaleMode.Font;
			ClientSize = new Size(1093, 703);
			Controls.Add(ToolStrip);
			Controls.Add(StatusStrip);
			Controls.Add(MenuStrip);
			Icon = (Icon)resources.GetObject("$this.Icon");
			MainMenuStrip = MenuStrip;
			Name = "MainForm";
			StartPosition = FormStartPosition.CenterScreen;
			Text = "Honda ROM Editor (OBD1)";
			Shown += MainForm_Shown;
			MenuStrip.ResumeLayout(false);
			MenuStrip.PerformLayout();
			ToolStrip.ResumeLayout(false);
			ToolStrip.PerformLayout();
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
		private ToolStripButton RomSettingsButton;
		private ToolStripButton AppSettingsButton;
		private ToolStripButton ChipDeviceButton;
		private ToolStripButton LiveDataButton;
		private ToolStripButton SubmitBugButton;
		private ToolStripButton DonateButton;
		private ToolStripMenuItem AboutMenuItem;
	}
}
