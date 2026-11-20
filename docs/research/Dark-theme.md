# The black theme on .NET 11 WinForms

What the dark colour mode of WinForms gives for free on .NET 11, what it does not, and what the editor therefore does itself. Checked on .NET SDK 11.0.100, Windows 11 (build 26300), at 100 % scaling, with Windows itself in dark mode.

## How it was checked

Windows were shown without activating them and read back from the window manager (`PrintWindow` with the full-content flag), so the pictures show what the desktop compositor draws, title bar included. [`tools/HRE.Shots`](../../tools/HRE.Shots) does this for the editor's main window:

```powershell
dotnet run --project tools/HRE.Shots -- [--technical] [--zoom=2] out.png [rom.bin [parameter name]]
```

The stock controls were looked at the same way, in throwaway forms holding one of each and nothing else but `Application.SetColorMode(SystemColorMode.Dark)` before the first window.

## Stock controls under `SystemColorMode.Dark`

| Control | What it looks like |
|---|---|
| Form | Dark grey background (about `#202020`), dark grey title bar with light text |
| `MenuStrip`, its drop-downs, `StatusStrip` | Dark grey, light text; disabled items dimmed |
| `TreeView` | Dark grey, light text, dark scroll bar; the selected row is only slightly lighter than the rest, hard to see |
| `Button`, `TextBox`, `ComboBox`, `CheckBox`, `ListBox`, `ProgressBar`, `LinkLabel` | Dark, with light text and borders |
| `TabControl` | Dark tabs and page |
| Scroll bars of a scrolling `Panel` | Dark |
| `ToolTip` | Dark grey, light text |
| The system file dialog | Dark (with Windows in dark mode; it is dark grey and cannot be recoloured) |
| `DataGridView` | Dark cells; the column and row headers stay white until `EnableHeadersVisualStyles` is set to false and the header styles are given colours, after which they are dark too |
| `ListView` with groups | The group headers come out nearly unreadable (dark blue on dark grey) |
| `TaskDialog` | Stays white: it does not follow the colour mode |
| `NumericUpDown` | Dark field with a light border; the two buttons stand side by side at its right end. It takes `BackColor` and `ForeColor` |
| The default button of a window (`AcceptButton`, or the button that has the focus) | Filled with the accent colour of Windows (purple on the PC it was checked on), whatever `BackColor` it was given |

So the colour mode is a sound base. It is grey, though, not black, and it has no accent colour; and two things do not follow it at all.

## What the editor does

- **Black, not grey.** Every surface gets its colour from one palette (`DarkTheme`). Setting `BackColor` and `ForeColor` is enough for forms, panels, labels, text fields, the list and buttons.
- **Menus and the status bar** are drawn by a `ToolStripProfessionalRenderer` with a colour table from the same palette; without it they keep the grey of the colour mode. The highlighted menu item gets the selection fill and an outline in the accent: the fill alone is too faint on black to show where the keyboard is. Status fields keep the colour they are given; the first version of the renderer overrode it, so the red of "Checksum wrong" was never drawn.
- **Title bar and window border** are set through `Form.FormCaptionBackColor`, `FormCaptionTextColor` and `FormBorderColor`, which are stable on .NET 11 and can be set before the window exists. The first version called `DwmSetWindowAttribute` itself; the properties do the same and need no unsafe code. Windows 11 only; what they do on Windows 10 was not checked.
- **The list's selection** is drawn by the editor (`TreeViewDrawMode.OwnerDrawText`): a fill across the whole row, a bar in the accent at its left edge, and an outline while the list has the keyboard focus. The bar is there because the fill is only 2.1 : 1 against black.
- **No dialog boxes of the system** for messages: `TaskDialog` would be a white window over a black one. A problem is told on the page instead, which also keeps it readable for as long as the user needs.
- **The one question, in a window of the editor.** Before changes are lost the editor asks whether to save them; that is `ChangesForm`, a small window in the palette, for the same reason (`HRE.Shots --ask` takes its picture).
- **Buttons keep their colour.** `ThemedButton` overrides `Button.NotifyDefault` to pass `false` on, so the button Enter presses is not repainted in the accent of Windows: the main button of a page or a dialog is in the editor's teal whether or not it has the focus. The focus still shows as the outline of the button.
- **No `ListView` groups.** The list stays a `TreeView`.
- **Pages are standard controls**, not painted text: see [What .NET 11, WinForms and C# 15 offer](Platform-features.md).

## Windows contrast themes

When `SystemInformation.HighContrast` is true, `DarkTheme` hands out the colours of the user's contrast theme (`SystemColors.Window`, `WindowText`, `Highlight`, `HighlightText`, `HotTrack`) instead of its own, leaves menus to WinForms' own renderer and the title bar to Windows, and the window re-colours itself when the setting changes (`SystemEvents.UserPreferenceChanged`). This follows from the code and the documentation: **no contrast theme was switched on to look at it**, because that changes the settings of the PC it was developed on.

## Contrast

The palette is checked by a unit test against the WCAG contrast ratio: at least 7 for text on the background, the bars and the cards, and for the text of the selected row; at least 4.5 for dimmed text, for the colours of notices and for the dark text of the main button on the accent; at least 3 for the accent bar of the selected row, the outline of the highlighted menu item and the divider.

Measured on the first version of the window, before these were added: selected row fill on black 2.1 : 1, highlighted menu item 1.2 : 1, divider 1.5 : 1. Light text on the accent would be 2.8 : 1, which is why the main button has dark text.

## Not checked

- Scaling other than 100 %, and monitors with different scaling. A change of DPI was only simulated.
- Windows 10.
- A Windows contrast theme, and Windows' own text size setting.
- The window with the keyboard focus in it: pictures are taken without activating it.
