# What .NET 11, WinForms and C# 15 offer

The interface is to use what the platform offers in place of hand-made substitutes ([Design principles](https://github.com/VIRUXE/honda-rom-editor/wiki/Design-principles)). This page records what was looked at, how, and what came of it.

## How it was checked

On .NET SDK 11.0.100 (C# 15, Windows Desktop reference pack 11.0.0), Windows 11 build 26300, one monitor at 100 % scaling:

- The public API of `System.Windows.Forms` in the 10.0 and 11.0 reference packs was compared by reflection, and every `[Experimental]` attribute listed.
- Each API named below was used in a probe project compiled with warnings as errors, and most were run. Windows were shown without activating them and read back with `PrintWindow`, as [`tools/HRE.Shots`](../../tools/HRE.Shots) does.
- What a screen reader is given was read from the `AccessibilityObject` of each control and, from a second process, from the UI Automation tree.

Not done: no screen reader was run, no real drag and drop was performed, and no Windows setting was changed (contrast theme, text size, scaling), since all of these take over the PC. Statements about those rest on the documentation and on reading the WinForms source at tag `v11.0.0`.

## WinForms

| What | State on .NET 11 | In the editor |
|---|---|---|
| `Application.SetColorMode(SystemColorMode.Dark)` | Stable (no diagnostic) | Used: the base under the black palette. See [The black theme](Dark-theme.md) |
| `Application.SetDefaultVisualStylesMode(VisualStylesMode.Net11)` | New in 11, stable | Used: rounded buttons and text fields in the current Windows look. Sizes differ from the classic look, so nothing is laid out by coordinates. The default button and check marks take the accent colour of Windows; the editor gives its main buttons its own |
| `Form.FormCaptionBackColor`, `FormCaptionTextColor`, `FormBorderColor`, `FormCornerPreference` | Stable | Used for the black title bar, in place of calling `DwmSetWindowAttribute` |
| `Control.AccessibleName`, `AccessibleRole`, `AccessibleDescription` | Stable | Used on every control that needs more than its text |
| `AccessibleObject.RaiseAutomationNotification` | Stable; returned true in the probe | Used to announce what an action did (file opened, file refused, text size) |
| Custom `AccessibleObject` on a painted control | Works for the older accessibility interface; the rows of a painted panel did not reach UI Automation in the probe | Not used. The page is made of standard controls instead, which are exposed without further code |
| `ToolStripItem.SelectedChanged`, `ToolStrip.AllowClickThrough` | Stable (since 9) | Used: the status bar says what the highlighted command does; the first click on an inactive window reaches the menu |
| `ControlMutationExtensions.SuspendPainting` | New in 11, stable | Used when the list is rebuilt; it cannot be left unbalanced by an exception, unlike `BeginUpdate` and `EndUpdate` |
| `FileDialog.ClientGuid` | Stable | Used: Windows keeps the folder of the Open dialog apart from the dialogs to come (save, import, datalog) |
| Dropping files: `AllowDrop`, `DataFormats.FileDrop`, `TryGetData<T>` | Stable. `IAsyncDropTarget` is the only experimental WinForms API left (WFO5003) | Used with the stable API. `GetData` is deprecated in favour of `TryGetData` |
| `ToolTip`, `TreeView.ShowNodeToolTips`, `StatusStrip.ShowItemToolTips` | Stable, dark | Used on the entries of the list and the fields of the status bar |
| Icon fonts (Segoe Fluent Icons; Segoe MDL2 Assets on Windows 10, same code points) | Fonts of Windows, drawn as text | Used for the symbols of notices and of the status bar. They take any colour and scale with the text. Each has words beside it |
| `SystemIcons.GetStockIcon` | Stable | Not used: multi-colour icons cannot be tinted and sit badly on black |
| `TaskDialog` | Stable, but white under the dark colour mode | Not used |
| `ListView` groups | Group headers unreadable under the dark colour mode | Not used |
| `DataGridView` | Can be made black (`EnableHeadersVisualStyles = false` plus header styles); brings keyboard handling and accessibility with it | Used for the table grid, with every cell drawn by the editor. See [The table grid](Table-grid.md) for what it gives and what had to be changed |
| `Application.SystemVisualSettings` (text scale, contrast, focus metrics, animation) | New in 11, stable; values were read, changes not observed | Not used yet. High contrast is read through `SystemInformation.HighContrast` and `SystemEvents.UserPreferenceChanged`, which behave the same on older versions |
| Command binding (`ToolStripItem.Command`, `ButtonBase.Command`) | Stable | Not used: with a handful of commands, each raised from one or two places, it would add types without removing code. To be looked at again with a toolbar |
| `Control.InvokeAsync`, `Form.ShowDialogAsync` | Stable | Not needed to read a 32 KB file. For the datalogger and other work off the window's thread |
| Jump lists, taskbar progress | Nothing built in; needs COM interop | Not used. Recent files are in the File menu and on the start page |
| Trimming, native AOT | Refused for WinForms (NETSDK1175) | Not used. A self-contained single file publishes; it was not started |

Two rules for new controls that the compiler enforces: every settable public property of a control needs `[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]` (WFO1000 is an error), and a control with scroll bars of its own sets `ControlStyles.ApplyThemingImplicitly`.

### Scaling

`AutoScaleMode.Dpi` does nothing unless `AutoScaleDimensions` states the DPI the numbers were written for, between `SuspendLayout` and `ResumeLayout`. The first version of the window left it out, so its sizes did not scale. Several properties are not scaled by WinForms in any case (`SplitContainer.SplitterDistance` and `Panel1MinSize`, `TreeView.ItemHeight`, paddings of status fields): the editor sets them through `LogicalToDeviceUnits` or derives them from the font. Fonts created by the application are not rescaled either: the page derives its title and heading fonts from its own `Font` whenever that changes.

`Application.SetDefaultFont` does not reach `MenuStrip` and `StatusStrip`; the editor sets their fonts itself.

## .NET

| What | In the editor |
|---|---|
| `System.Text.Json` source generation | Used for the settings file: no reflection, and the shape of the file is checked by the compiler |
| `Environment.SpecialFolder.ApplicationData` | Used for the settings folder. `Application.UserAppDataPath` is avoided: its path contains the version, so every release would start without settings |
| `Convert.ToHexStringLower`, `ArgumentOutOfRangeException.ThrowIf...` | Already in use |

Documented, not checked here: .NET 11 does not start on x64 processors without the x86-64-v2 instruction set, which matters for old workshop laptops and belongs on a download page.

## C# 15

No project sets `LangVersion`, so all of them compile as C# 15.

| Feature | In the editor |
|---|---|
| `field` in a property (C# 14) | Used for the properties of the page control that rebuild the page when set |
| Collection expressions, primary constructors, required members, list patterns | In use throughout |
| Switch expressions that name every member and throw otherwise | Used for every enum that decides wording or colour, so that a new state cannot be given no words |
| `closed` hierarchies, `union` types (C# 15) | Not used yet. `closed` suits the block types of a page and the kinds of parameter; to be done with the table grid, when the kinds of parameter grow |
| Extension members (C# 14), collection expression arguments (C# 15) | No place where they make the code clearer |

Build settings, in `Directory.Build.props`: warnings are errors, `AnalysisLevel` is `latest-recommended`, code style is enforced in the build. Turning them on found ten places, among them number formatting that would have followed the user's locale.
