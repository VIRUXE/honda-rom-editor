using HRE.Core;

namespace HRE.App;

/// <summary>
/// A setting, which is a single value, to read and to change. It is the standard number field
/// of Windows Forms, which brings the arrow keys, the two small buttons and what a screen reader
/// needs: the value, the smallest and the largest it can be. Numbers are read and written here,
/// so that they have a point as the decimal mark whatever the language of Windows, and a comma
/// is understood too.
/// </summary>
public sealed class SettingBox : NumericUpDown {
    private readonly bool ready;
    private bool loading;

    public SettingBox(RomDocument document, ParamDef def) {
        Document = document;
        Def = def;
        var (min, max) = ValueEdit.Range(def);
        AccessibleName = def.Name;
        AccessibleDescription = $"From {Shown(min)} to {Shown(max)}{Unit}";
        DecimalPlaces = ValueEdit.Decimals(def);
        Increment = Exact(ValueEdit.Step(def));
        (Minimum, Maximum) = (Exact(min), Exact(max));
        TextAlign = HorizontalAlignment.Right;
        ready = true;
        Reload();
        document.Changed += Reload;
    }

    /// <summary>Something happened that the user should be told: what the value was set to, or why nothing changed.</summary>
    public event Action<string>? Announced;

    public RomDocument Document { get; }
    public ParamDef Def { get; }

    private string Unit => Def.Unit.Length > 0 ? " " + Def.Unit : "";

    /// <summary>Takes the colours of the theme: at start, and when a Windows contrast theme is turned on or off.</summary>
    public void ApplyTheme() {
        BackColor = DarkTheme.Background;
        ForeColor = DarkTheme.Text;
    }

    /// <summary>Sets the value to what was typed: the nearest step, inside what the setting can be.</summary>
    public void TypeValue(string typed) {
        if (ReadOnly) return;
        if (TableEdit.Parse(typed) is not { } value) {
            Announced?.Invoke($"\"{typed.Trim()}\" is not a number, so nothing was changed. Type a number such as {Shown(ValueEdit.Value(Document.Image, Def))}.");
            return;
        }
        Document.Apply($"Change {Def.Name}", ValueEdit.Set(Def, value));
        var shown = ValueEdit.Text(Document.Image, Def);
        if (!ValueEdit.Fits(Def, value)) {
            var (min, max) = ValueEdit.Range(Def);
            Announced?.Invoke($"It can be set from {Shown(min)} to {Shown(max)}{Unit}. It was set to {shown}{Unit}.");
        } else {
            Announced?.Invoke($"Set to {shown}{Unit}");
        }
    }

    /// <summary>Raises (or, with a negative count, lowers) the value by that many of its smallest step.</summary>
    public void Nudge(int steps) {
        if (ReadOnly || steps == 0) return;
        if (Document.Apply($"{(steps > 0 ? "Raise" : "Lower")} {Def.Name}", ValueEdit.Move(Document.Image, Def, steps))) {
            Announced?.Invoke($"{ValueEdit.Text(Document.Image, Def)}{Unit}");
        } else {
            Announced?.Invoke(steps > 0 ? "Already at the largest value it can be set to." : "Already at the smallest value it can be set to.");
        }
    }

    /// <summary>Takes what is being typed as the value, as Enter would.</summary>
    public void FinishTyping() {
        if (UserEdit) ValidateEditText();
    }

    public override void UpButton() {
        if (UserEdit) ValidateEditText();
        Nudge(1);
    }

    public override void DownButton() {
        if (UserEdit) ValidateEditText();
        Nudge(-1);
    }

    /// <summary>Takes what stands in the field as the value: on Enter, and when the field is left.</summary>
    protected override void ValidateEditText() {
        // The field of the base class is filled while it is being made, before there is a setting to speak of.
        if (!ready) return;
        var typed = Text;
        UserEdit = false;
        // An emptied field is a change of mind, not a value.
        if (typed.Trim() is { Length: > 0 } text && text != ValueEdit.Text(Document.Image, Def)) TypeValue(text);
        UpdateEditText();
    }

    /// <summary>Writes the value into the field.</summary>
    protected override void UpdateEditText() {
        if (!ready) return;
        if (UserEdit) {
            ValidateEditText();
            return;
        }
        ChangingText = true;
        Text = ValueEdit.Text(Document.Image, Def);
    }

    protected override void OnTextBoxKeyPress(object? source, KeyPressEventArgs e) {
        OnKeyPress(e);
        // Digits, the two decimal marks and the sign; what the keyboard sends for Backspace and for commands passes.
        if (!(char.IsAsciiDigit(e.KeyChar) || e.KeyChar is '.' or ',' or '-' || char.IsControl(e.KeyChar))) e.Handled = true;
    }

    protected override void Dispose(bool disposing) {
        if (disposing) Document.Changed -= Reload;
        base.Dispose(disposing);
    }

    private static decimal Exact(float value) => Math.Round((decimal)value, 4);

    private string Shown(float value) => TableEdit.Text(value, ValueEdit.Decimals(Def));

    /// <summary>Takes in the document as it is now: after a change, an undo or a redo.</summary>
    private void Reload() {
        if (loading) return;
        loading = true;
        var value = Exact(ValueEdit.Value(Document.Image, Def));
        // A ROM may hold a value outside what its description allows; it is shown as it is.
        (Minimum, Maximum) = (Math.Min(Minimum, value), Math.Max(Maximum, value));
        UserEdit = false;
        Value = value;
        UpdateEditText();
        loading = false;
    }
}
