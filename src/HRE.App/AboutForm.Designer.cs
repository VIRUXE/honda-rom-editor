#nullable disable

namespace HRE.App;

partial class AboutForm {
    /// <summary>Required designer variable.</summary>
    private System.ComponentModel.IContainer components = null;

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent() {
        this.column = new System.Windows.Forms.FlowLayoutPanel();
        this.nameLabel = new System.Windows.Forms.Label();
        this.versionLabel = new System.Windows.Forms.Label();
        this.purposeLabel = new System.Windows.Forms.Label();
        this.earlyLabel = new System.Windows.Forms.Label();
        this.reportLink = new System.Windows.Forms.LinkLabel();
        this.buttons = new System.Windows.Forms.FlowLayoutPanel();
        this.close = new HRE.App.ThemedButton();
        this.copy = new HRE.App.ThemedButton();
        this.column.SuspendLayout();
        this.buttons.SuspendLayout();
        this.SuspendLayout();
        //
        // column
        //
        this.column.AutoSize = true;
        this.column.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
        this.column.Controls.Add(this.nameLabel);
        this.column.Controls.Add(this.versionLabel);
        this.column.Controls.Add(this.purposeLabel);
        this.column.Controls.Add(this.earlyLabel);
        this.column.Controls.Add(this.reportLink);
        this.column.Controls.Add(this.buttons);
        this.column.Dock = System.Windows.Forms.DockStyle.Fill;
        this.column.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
        this.column.Location = new System.Drawing.Point(20, 20);
        this.column.Name = "column";
        this.column.TabIndex = 0;
        this.column.WrapContents = false;
        //
        // nameLabel
        //
        this.nameLabel.AutoSize = true;
        this.nameLabel.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0);
        this.nameLabel.Name = "nameLabel";
        this.nameLabel.TabIndex = 0;
        this.nameLabel.Text = "Honda ROM Editor";
        this.nameLabel.UseMnemonic = false;
        //
        // versionLabel
        //
        this.versionLabel.AutoSize = true;
        this.versionLabel.Name = "versionLabel";
        this.versionLabel.TabIndex = 1;
        this.versionLabel.Text = "Version";
        //
        // purposeLabel
        //
        this.purposeLabel.AutoSize = true;
        this.purposeLabel.Margin = new System.Windows.Forms.Padding(3, 12, 3, 3);
        this.purposeLabel.MaximumSize = new System.Drawing.Size(380, 0);
        this.purposeLabel.Name = "purposeLabel";
        this.purposeLabel.TabIndex = 2;
        this.purposeLabel.Text = "For the ROMs of Honda OBD0 and OBD1 engine computers.";
        //
        // earlyLabel
        //
        this.earlyLabel.AutoSize = true;
        this.earlyLabel.MaximumSize = new System.Drawing.Size(380, 0);
        this.earlyLabel.Name = "earlyLabel";
        this.earlyLabel.TabIndex = 3;
        this.earlyLabel.Text = "Early version.";
        //
        // reportLink
        //
        this.reportLink.AutoSize = true;
        this.reportLink.Margin = new System.Windows.Forms.Padding(3, 10, 3, 3);
        this.reportLink.Name = "reportLink";
        this.reportLink.TabIndex = 4;
        this.reportLink.TabStop = true;
        this.reportLink.Text = "Report a problem or ask a question";
        this.reportLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.ReportLink_LinkClicked);
        //
        // buttons
        //
        this.buttons.AutoSize = true;
        this.buttons.Controls.Add(this.close);
        this.buttons.Controls.Add(this.copy);
        this.buttons.Dock = System.Windows.Forms.DockStyle.Fill;
        this.buttons.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
        this.buttons.Margin = new System.Windows.Forms.Padding(0, 16, 0, 0);
        this.buttons.Name = "buttons";
        this.buttons.TabIndex = 5;
        //
        // close
        //
        this.close.AutoSize = true;
        this.close.DialogResult = System.Windows.Forms.DialogResult.OK;
        this.close.Name = "close";
        this.close.TabIndex = 0;
        this.close.Text = "Close";
        //
        // copy
        //
        this.copy.AccessibleDescription = "Copies the versions of the program, .NET and Windows, to paste into a problem report";
        this.copy.AutoSize = true;
        this.copy.Name = "copy";
        this.copy.TabIndex = 1;
        this.copy.Text = "&Copy version details";
        this.copy.Click += new System.EventHandler(this.Copy_Click);
        //
        // AboutForm
        //
        this.AcceptButton = this.close;
        this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
        this.AutoSize = true;
        this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
        this.CancelButton = this.close;
        this.Controls.Add(this.column);
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Name = "AboutForm";
        this.Padding = new System.Windows.Forms.Padding(20);
        this.ShowInTaskbar = false;
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = "About Honda ROM Editor";
        this.column.ResumeLayout(false);
        this.column.PerformLayout();
        this.buttons.ResumeLayout(false);
        this.buttons.PerformLayout();
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    #endregion

    private System.Windows.Forms.FlowLayoutPanel column;
    private System.Windows.Forms.Label nameLabel;
    private System.Windows.Forms.Label versionLabel;
    private System.Windows.Forms.Label purposeLabel;
    private System.Windows.Forms.Label earlyLabel;
    private System.Windows.Forms.LinkLabel reportLink;
    private System.Windows.Forms.FlowLayoutPanel buttons;
    private ThemedButton close;
    private ThemedButton copy;
}
