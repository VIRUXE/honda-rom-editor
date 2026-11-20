# The black theme on .NET 11 WinForms

What the dark colour mode of WinForms gives for free on .NET 11, what it does not, and what the editor therefore draws itself. Checked on .NET SDK 11.0.100, Windows 11 (build 26300), at 100 % scaling.

## How it was checked

Windows were shown without activating them and read back from the window manager (`PrintWindow` with the full-content flag), so the pictures show what the desktop compositor draws, title bar included. [`tools/HRE.Shots`](../../tools/HRE.Shots) does this for the editor's main window:

```powershell
dotnet run --project tools/HRE.Shots -- out.png [rom.bin [parameter name]]
```

The stock controls were looked at the same way, in a throwaway form holding one of each and nothing else but `Application.SetColorMode(SystemColorMode.Dark)` before the first window.

## Stock controls under `SystemColorMode.Dark`

| Control | What it looks like |
|---|---|
| Form | Dark grey background (about `#202020`), dark grey title bar with light text |
| `MenuStrip`, its drop-downs, `StatusStrip` | Dark grey, light text; disabled items dimmed |
| `TreeView` | Dark grey, light text, dark scroll bar; the selected row is only slightly lighter than the rest, hard to see |
| `Button`, `TextBox`, `ComboBox`, `CheckBox`, `ListBox`, `ProgressBar` | Dark, with light text and borders |
| `TabControl` | Dark tabs and page |
| Scroll bars of a scrolling `Panel` | Dark |
| `DataGridView` | Dark cells, but the column and row headers stay white |

So the colour mode is a sound base: nothing is left light except the headers of `DataGridView`. It is grey, though, not black, and it has no accent colour.

## What the editor adds

- **Black, not grey.** Every surface gets its colour from one palette (`DarkTheme`): black background, a near-black surface for the menu and status bars, light text. Setting `BackColor` and `ForeColor` is enough for forms, panels, the tree and buttons.
- **Menus and the status bar** are drawn by a `ToolStripProfessionalRenderer` with a colour table from the same palette; without it they keep the grey of the colour mode. The renderer also sets the text colour of every item, enabled or disabled, from the palette.
- **Title bar and window border.** The colour mode makes the title bar dark grey. Windows 11 lets a window choose its caption, caption text and border colours (`DwmSetWindowAttribute`, attributes 35, 36 and 34), which makes them black. Older Windows versions are documented to refuse these attributes; the window then keeps the dark grey.
- **The tree's selection** is drawn by the editor (`TreeViewDrawMode.OwnerDrawText`). With `FullRowSelect` the control still paints its own grey highlight over the indent, next to the editor's, so full-row selection is off and the highlight runs from the text to the right edge.
- **Read-only text pages** (the detail view) are painted directly with `TextRenderer`, double-buffered, on a scrolling panel.
- **No `DataGridView`.** Its white headers are one more reason for the table grid to be a control of its own.

## Contrast

The palette is checked by a unit test against the WCAG contrast ratio: at least 7 for body text on the background, on the selection and on the hover colour; at least 4.5 for dimmed text and for the accent and status colours on the bar surface; at least 3 for disabled text. The first choice for disabled text (`#5C5C5C`) failed that test and was lightened to `#6E6E6E`.

## Not checked

- Scaling other than 100 %, and monitors with different scaling.
- Windows 10, where the title bar colours cannot be set.
- The system file dialog: Windows draws it, not the application.
- The detail view with a page long enough to scroll.
