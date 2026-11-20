# The table grid

The cells of a table are shown by [`TableGrid`](../../src/HRE.App/TableGrid.cs). This page records what it is built on, why, what had to be changed in what the platform gives, and how each statement was checked.

## A standard grid with cells drawn by the program

Two ways were open: a control that paints everything itself, or `DataGridView` with its cells painted through `OnCellPainting`.

The [Design principles](https://github.com/VIRUXE/honda-rom-editor/wiki/Design-principles) ask that nothing that carries information is only painted, and that the platform is used in place of hand-made substitutes. An earlier probe ([What .NET 11, WinForms and C# 15 offer](Platform-features.md)) had found that the rows of a painted panel with an accessible object of its own did not reach UI Automation. `DataGridView` was therefore tried first, and it was enough:

| Needed | From `DataGridView` | Done by the editor |
|---|---|---|
| A grid a screen reader can walk | The grid, row, cell and header elements with the grid, table and value patterns | Names: a cell is "RPM 3000, MAP kPa 75", a row "RPM 3000", a column "MAP kPa 75" |
| Arrow keys, Shift and Ctrl selection, Ctrl+A, F2, Enter, Esc | All of it | Nothing |
| A field to type a value in | The editing control, placed over the cell | Its colours and its name; which keys start it |
| Heat colours, selection, focus | Nothing usable: cell styles give one colour per state | All cells and headers are drawn in `OnCellPainting` |
| Where the engine is, counts of readings | Nothing | Drawn over the cells; said in the description of the cell |
| Copy and paste | Copies as text with headers, pastes nothing | Its own copy and paste, so that what is copied can be pasted back |

## What had to be changed

Each of these was seen in a test or a picture before it was changed.

- **Any letter, and + and -, started typing.** `DataGridViewTextBoxCell.KeyEntersEditMode` says yes to almost every key. The grid's cell class says no to all of them, and the grid starts typing itself, from the character and not the key (`OnKeyPress`): a digit, a point or a comma starts a value; + and - raise and lower. Going by the character matters because + is Shift and = on some keyboards and a key of its own on others. F2 still works: the grid handles it apart from this.
- **Tab moved to the next cell**, so a keyboard user could not leave a table of 200 cells without 200 presses. `StandardTab = true` gives Tab back to the page.
- **Page Up, Page Down, Ctrl+Home and Ctrl+End scrolled the page** instead of moving between cells, because a key goes to the parents of a control before the control (`ProcessCmdKey`). The page leaves these keys alone while the focus is in the grid.
- **Clicking a cell made the page jump.** A scrolling panel brings a control that takes the focus into view by its top left corner (`ScrollToControl`), and a table is taller than most windows. The page keeps its place when the grid takes the focus and follows the current cell instead.
- **The grid scrolled inside itself** when its content grew (a longer number, the line for the counts of readings). It now says when its size changes, and the page gives it the room. Scroll bars of its own appear only when the table is wider than the window.
- **Rows were called "Row 1", "Row 2".** A row class of its own names them by their step.
- **The description did not reach UI Automation as help text.** `AccessibleObject.Description` is the description of the older interface; UI Automation's `HelpText` is `AccessibleObject.Help`. The cell gives the same words for both.
- **Copying put headers and quotation marks on the clipboard** in some modes; `ClipboardCopyMode` is off, and the grid writes tab-separated lines, which is what spreadsheets and the tuning programs exchange.

Left as it is: the corner where the headers meet is called "Top Left Header Cell", and the header row "Top Row".

## Typing a number

A comma is taken as the decimal mark as well as a point. The window writes numbers with a point, but the decimal key of the number pad types a comma on many keyboard layouts, and a tuner types on the number pad. When the text has a point, commas are not accepted, so "1,234.5" is not mistaken for anything.

What is typed is stored as the nearest value a cell can hold. A number outside what a cell can hold is set to the nearest end, and the grid says what the ends are.

Cells are written with as many decimals as tell one stored value from the next, three at most: a table that steps by 0.25 shows two. With one decimal, as the pages round, raising a cell from 10.25 to 10.5 would show 10.3 and then 10.5, and 10.2 and 10.3 would be different numbers that look like neighbours.

## Colours

A cell takes a colour between the smallest and the largest value of its table: blue `#6EAFFF`, green `#5FCD82`, yellow `#F0D750`, red `#FF7D69`, blended in between. All are light, so that the text of every cell can be black: the lowest contrast of black on any colour of the scale is 8.4 : 1, at the red end (computed over a thousand steps; a unit test holds every hundredth to 7 : 1). A darker blue and red that were considered first (`#5A8CE6`, `#EB5A46`) leave black text at 6.3 : 1 and 6.1 : 1, under the 7 : 1 the design principles ask for.

Selected cells take the selection colour of the list with white text. The cell the keyboard is on has a frame in the colour of its text while the grid has the focus. Under a Windows contrast theme cells have the colour of the window; the number says it all.

## Where the engine is

Three ways of showing a position between cells were drawn and looked at on the ignition map of a P28 ROM:

1. A ring at the position. It lay on the numbers of the cell it was in.
2. A frame the size of a cell, lying between cells. It crossed the numbers of two to four cells.
3. Frames around the four cells the engine computer blends at that position, each the heavier the more it counts. Nothing is covered, and the weights show what a change to each cell will do. This is what the grid draws: a light line inside a dark one, so that one of the two shows on any cell.

The counts of readings are written small in the upper right corner of each cell, on a line of their own, so that rows are taller while counts are shown.

## How it was checked

- **Unit tests** ([`TableGridTests`](../../tests/HRE.Tests/TableGridTests.cs)): what cells show; typing, raising, lowering, copying and pasting against the bytes of the ROM; names, values and descriptions of the accessibility objects; sizes at the normal and at twice the text size; that the page keeps the grid, its selection and its place while the table changes. A character is sent to the grid's window (`WM_CHAR`) to see that + raises and a digit starts typing.
- **Pictures** with [`tools/HRE.Shots`](../../tools/HRE.Shots), which shows the window without taking the focus: `--cell=row,col` moves to a cell, `--trace=row,col` shows the engine at a position with made-up counts of readings around it, `--zoom=2` doubles the text.
- **UI Automation from a second process.** With the window held up (`--hold=30`), a PowerShell script using `System.Windows.Automation` found the grid and read it:

  ```
  grid: name='Ignition Map (Low Cam)' type=ControlType.DataGrid
  grid pattern: rows=20 cols=10
  cell 12,5: name='RPM 3000, MAP kPa 75' type=ControlType.DataItem value='33.5 readonly=False'
             help='The engine is running here. 220 readings here'
     row header: RPM 3000  column header: MAP kPa 75
  ```

  One thing was odd: `FindFirst` with a condition on the control type did not find the grid among the descendants of the window, while walking the tree with `TreeWalker.ControlViewWalker` did. It was not looked into.

## Not checked

No screen reader was run. No key was pressed on the keyboard and nothing was clicked: the window never had the focus, because the PC was in use. So Ctrl+C and Ctrl+V (which need the Ctrl key to be down), selecting with the mouse, and the scroll bar under a table wider than the window were not exercised; the clipboard itself was not touched by the tests, which call the same code with text. No contrast theme was turned on. These go with the checks by hand in issue #49.
