namespace HRE.App;

/// <summary>
/// A button that keeps the colours it was given. Windows paints the default button of a window,
/// which is the one Enter presses, in the accent colour of the system, over the colour of the
/// program; this one does not take that look. The focus still shows as its outline.
/// </summary>
public sealed class ThemedButton : Button {
    public override void NotifyDefault(bool value) => base.NotifyDefault(false);
}
