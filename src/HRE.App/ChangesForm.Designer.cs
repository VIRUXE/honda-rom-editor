#nullable disable

namespace HRE.App;

partial class ChangesForm {
    /// <summary>Required designer variable.</summary>
    private System.ComponentModel.IContainer components = null;

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent() {
        this._column = new System.Windows.Forms.FlowLayoutPanel();
        this._heading = new System.Windows.Forms.Label();
        this._body = new System.Windows.Forms.Label();
        this._buttons = new System.Windows.Forms.FlowLayoutPanel();
        this._save = new HRE.App.ThemedButton();
        this._discard = new HRE.App.ThemedButton();
        this._cancel = new HRE.App.ThemedButton();
        this._column.SuspendLayout();
        this._buttons.SuspendLayout();
        this.SuspendLayout();
        //
        // _column
        //
        this._column.AutoSize = true;
        this._column.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
        this._column.Controls.Add(this._heading);
        this._column.Controls.Add(this._body);
        this._column.Controls.Add(this._buttons);
        this._column.Dock = System.Windows.Forms.DockStyle.Fill;
        this._column.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
        this._column.Location = new System.Drawing.Point(20, 20);
        this._column.Name = "_column";
        this._column.TabIndex = 0;
        this._column.WrapContents = false;
        //
        // _heading
        //
        this._heading.AutoSize = true;
        this._heading.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0);
        this._heading.Name = "_heading";
        this._heading.TabIndex = 0;
        this._heading.Text = "Save the changes to tune.bin?";
        this._heading.UseMnemonic = false;
        //
        // _body
        //
        this._body.AutoSize = true;
        this._body.Margin = new System.Windows.Forms.Padding(3, 10, 3, 3);
        this._body.Name = "_body";
        this._body.TabIndex = 1;
        this._body.Text = "If you do not save them, they are lost.";
        //
        // _buttons
        //
        this._buttons.AutoSize = true;
        this._buttons.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
        this._buttons.Controls.Add(this._save);
        this._buttons.Controls.Add(this._discard);
        this._buttons.Controls.Add(this._cancel);
        this._buttons.Margin = new System.Windows.Forms.Padding(0, 16, 0, 0);
        this._buttons.Name = "_buttons";
        this._buttons.TabIndex = 2;
        //
        // _save
        //
        this._save.AccessibleDescription = "Saves the changes, then goes on";
        this._save.AutoSize = true;
        this._save.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
        this._save.Name = "_save";
        this._save.TabIndex = 0;
        this._save.Text = "&Save";
        this._save.Click += new System.EventHandler(this.Save_Click);
        //
        // _discard
        //
        this._discard.AccessibleDescription = "Goes on without saving: the changes are lost";
        this._discard.AutoSize = true;
        this._discard.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
        this._discard.Name = "_discard";
        this._discard.TabIndex = 1;
        this._discard.Text = "&Don't save";
        this._discard.Click += new System.EventHandler(this.Discard_Click);
        //
        // _cancel
        //
        this._cancel.AccessibleDescription = "Goes back to the ROM, with its changes";
        this._cancel.AutoSize = true;
        this._cancel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
        this._cancel.Name = "_cancel";
        this._cancel.TabIndex = 2;
        this._cancel.Text = "Cancel";
        this._cancel.Click += new System.EventHandler(this.Cancel_Click);
        //
        // ChangesForm
        //
        this.AcceptButton = this._save;
        this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
        this.AutoSize = true;
        this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
        this.CancelButton = this._cancel;
        this.Controls.Add(this._column);
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Name = "ChangesForm";
        this.Padding = new System.Windows.Forms.Padding(20);
        this.ShowInTaskbar = false;
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = "Changes not saved";
        this._column.ResumeLayout(false);
        this._column.PerformLayout();
        this._buttons.ResumeLayout(false);
        this._buttons.PerformLayout();
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    #endregion

    private System.Windows.Forms.FlowLayoutPanel _column;
    private System.Windows.Forms.Label _heading;
    private System.Windows.Forms.Label _body;
    private System.Windows.Forms.FlowLayoutPanel _buttons;
    private ThemedButton _save;
    private ThemedButton _discard;
    private ThemedButton _cancel;
}
