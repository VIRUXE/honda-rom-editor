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
        this.column = new System.Windows.Forms.FlowLayoutPanel();
        this.heading = new System.Windows.Forms.Label();
        this.body = new System.Windows.Forms.Label();
        this.buttons = new System.Windows.Forms.FlowLayoutPanel();
        this.save = new HRE.App.ThemedButton();
        this.discard = new HRE.App.ThemedButton();
        this.cancel = new HRE.App.ThemedButton();
        this.column.SuspendLayout();
        this.buttons.SuspendLayout();
        this.SuspendLayout();
        //
        // column
        //
        this.column.AutoSize = true;
        this.column.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
        this.column.Controls.Add(this.heading);
        this.column.Controls.Add(this.body);
        this.column.Controls.Add(this.buttons);
        this.column.Dock = System.Windows.Forms.DockStyle.Fill;
        this.column.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
        this.column.Location = new System.Drawing.Point(20, 20);
        this.column.Name = "column";
        this.column.TabIndex = 0;
        this.column.WrapContents = false;
        //
        // heading
        //
        this.heading.AutoSize = true;
        this.heading.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0);
        this.heading.Name = "heading";
        this.heading.TabIndex = 0;
        this.heading.Text = "Save the changes to tune.bin?";
        this.heading.UseMnemonic = false;
        //
        // body
        //
        this.body.AutoSize = true;
        this.body.Margin = new System.Windows.Forms.Padding(3, 10, 3, 3);
        this.body.Name = "body";
        this.body.TabIndex = 1;
        this.body.Text = "If you do not save them, they are lost.";
        //
        // buttons
        //
        this.buttons.AutoSize = true;
        this.buttons.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
        this.buttons.Controls.Add(this.save);
        this.buttons.Controls.Add(this.discard);
        this.buttons.Controls.Add(this.cancel);
        this.buttons.Margin = new System.Windows.Forms.Padding(0, 16, 0, 0);
        this.buttons.Name = "buttons";
        this.buttons.TabIndex = 2;
        //
        // save
        //
        this.save.AccessibleDescription = "Saves the changes, then goes on";
        this.save.AutoSize = true;
        this.save.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
        this.save.Name = "save";
        this.save.TabIndex = 0;
        this.save.Text = "&Save";
        this.save.Click += new System.EventHandler(this.Save_Click);
        //
        // discard
        //
        this.discard.AccessibleDescription = "Goes on without saving: the changes are lost";
        this.discard.AutoSize = true;
        this.discard.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
        this.discard.Name = "discard";
        this.discard.TabIndex = 1;
        this.discard.Text = "&Don't save";
        this.discard.Click += new System.EventHandler(this.Discard_Click);
        //
        // cancel
        //
        this.cancel.AccessibleDescription = "Goes back to the ROM, with its changes";
        this.cancel.AutoSize = true;
        this.cancel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
        this.cancel.Name = "cancel";
        this.cancel.TabIndex = 2;
        this.cancel.Text = "Cancel";
        this.cancel.Click += new System.EventHandler(this.Cancel_Click);
        //
        // ChangesForm
        //
        this.AcceptButton = this.save;
        this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
        this.AutoSize = true;
        this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
        this.CancelButton = this.cancel;
        this.Controls.Add(this.column);
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Name = "ChangesForm";
        this.Padding = new System.Windows.Forms.Padding(20);
        this.ShowInTaskbar = false;
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = "Changes not saved";
        this.column.ResumeLayout(false);
        this.column.PerformLayout();
        this.buttons.ResumeLayout(false);
        this.buttons.PerformLayout();
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    #endregion

    private System.Windows.Forms.FlowLayoutPanel column;
    private System.Windows.Forms.Label heading;
    private System.Windows.Forms.Label body;
    private System.Windows.Forms.FlowLayoutPanel buttons;
    private ThemedButton save;
    private ThemedButton discard;
    private ThemedButton cancel;
}
