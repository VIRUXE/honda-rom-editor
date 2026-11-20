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
        this._column = new System.Windows.Forms.FlowLayoutPanel();
        this._nameLabel = new System.Windows.Forms.Label();
        this._versionLabel = new System.Windows.Forms.Label();
        this._purposeLabel = new System.Windows.Forms.Label();
        this._earlyLabel = new System.Windows.Forms.Label();
        this._reportLink = new System.Windows.Forms.LinkLabel();
        this._buttons = new System.Windows.Forms.FlowLayoutPanel();
        this._close = new HRE.App.ThemedButton();
        this._copy = new HRE.App.ThemedButton();
        this._column.SuspendLayout();
        this._buttons.SuspendLayout();
        this.SuspendLayout();
        //
        // _column
        //
        this._column.AutoSize = true;
        this._column.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
        this._column.Controls.Add(this._nameLabel);
        this._column.Controls.Add(this._versionLabel);
        this._column.Controls.Add(this._purposeLabel);
        this._column.Controls.Add(this._earlyLabel);
        this._column.Controls.Add(this._reportLink);
        this._column.Controls.Add(this._buttons);
        this._column.Dock = System.Windows.Forms.DockStyle.Fill;
        this._column.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
        this._column.Location = new System.Drawing.Point(20, 20);
        this._column.Name = "_column";
        this._column.TabIndex = 0;
        this._column.WrapContents = false;
        //
        // _nameLabel
        //
        this._nameLabel.AutoSize = true;
        this._nameLabel.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0);
        this._nameLabel.Name = "_nameLabel";
        this._nameLabel.TabIndex = 0;
        this._nameLabel.Text = "Honda ROM Editor";
        this._nameLabel.UseMnemonic = false;
        //
        // _versionLabel
        //
        this._versionLabel.AutoSize = true;
        this._versionLabel.Name = "_versionLabel";
        this._versionLabel.TabIndex = 1;
        this._versionLabel.Text = "Version";
        //
        // _purposeLabel
        //
        this._purposeLabel.AutoSize = true;
        this._purposeLabel.Margin = new System.Windows.Forms.Padding(3, 12, 3, 3);
        this._purposeLabel.MaximumSize = new System.Drawing.Size(380, 0);
        this._purposeLabel.Name = "_purposeLabel";
        this._purposeLabel.TabIndex = 2;
        this._purposeLabel.Text = "For the ROMs of Honda OBD0 and OBD1 engine computers.";
        //
        // _earlyLabel
        //
        this._earlyLabel.AutoSize = true;
        this._earlyLabel.MaximumSize = new System.Drawing.Size(380, 0);
        this._earlyLabel.Name = "_earlyLabel";
        this._earlyLabel.TabIndex = 3;
        this._earlyLabel.Text = "Early version.";
        //
        // _reportLink
        //
        this._reportLink.AutoSize = true;
        this._reportLink.Margin = new System.Windows.Forms.Padding(3, 10, 3, 3);
        this._reportLink.Name = "_reportLink";
        this._reportLink.TabIndex = 4;
        this._reportLink.TabStop = true;
        this._reportLink.Text = "Report a problem or ask a question";
        this._reportLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.ReportLink_LinkClicked);
        //
        // _buttons
        //
        this._buttons.AutoSize = true;
        this._buttons.Controls.Add(this._close);
        this._buttons.Controls.Add(this._copy);
        this._buttons.Dock = System.Windows.Forms.DockStyle.Fill;
        this._buttons.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
        this._buttons.Margin = new System.Windows.Forms.Padding(0, 16, 0, 0);
        this._buttons.Name = "_buttons";
        this._buttons.TabIndex = 5;
        //
        // _close
        //
        this._close.AutoSize = true;
        this._close.DialogResult = System.Windows.Forms.DialogResult.OK;
        this._close.Name = "_close";
        this._close.TabIndex = 0;
        this._close.Text = "Close";
        //
        // _copy
        //
        this._copy.AccessibleDescription = "Copies the versions of the program, .NET and Windows, to paste into a problem report";
        this._copy.AutoSize = true;
        this._copy.Name = "_copy";
        this._copy.TabIndex = 1;
        this._copy.Text = "&Copy version details";
        this._copy.Click += new System.EventHandler(this.Copy_Click);
        //
        // AboutForm
        //
        this.AcceptButton = this._close;
        this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
        this.AutoSize = true;
        this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
        this.CancelButton = this._close;
        this.Controls.Add(this._column);
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Name = "AboutForm";
        this.Padding = new System.Windows.Forms.Padding(20);
        this.ShowInTaskbar = false;
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = "About Honda ROM Editor";
        this._column.ResumeLayout(false);
        this._column.PerformLayout();
        this._buttons.ResumeLayout(false);
        this._buttons.PerformLayout();
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    #endregion

    private System.Windows.Forms.FlowLayoutPanel _column;
    private System.Windows.Forms.Label _nameLabel;
    private System.Windows.Forms.Label _versionLabel;
    private System.Windows.Forms.Label _purposeLabel;
    private System.Windows.Forms.Label _earlyLabel;
    private System.Windows.Forms.LinkLabel _reportLink;
    private System.Windows.Forms.FlowLayoutPanel _buttons;
    private ThemedButton _close;
    private ThemedButton _copy;
}
