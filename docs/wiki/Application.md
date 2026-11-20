# Application

The editor is one window: a menu bar, a list of what the open ROM holds, a page that describes what is selected, and a status bar. It is built to the [[Design principles]]: usable with the keyboard alone and with a screen reader, in plain words, with the technical detail one command away.

![The main window with a fuel map selected: what the table is, how to read it, and its cells in colours from blue for the smallest value to red for the largest](https://raw.githubusercontent.com/VIRUXE/honda-rom-editor/main/docs/assets/shell.png)

Run it with `dotnet run --project src/HRE.App`, or start `HondaRomEditor.exe`. A ROM file given on the command line is opened.

> Early version. It opens ROM files and lets you change the values of their tables. It cannot undo a change or save the file yet.

## Starting

![The start page: a button to open a file, the note about this early version, what a ROM is, which ROMs can be opened](https://raw.githubusercontent.com/VIRUXE/honda-rom-editor/main/docs/assets/start.png)

With no file open the page has the whole window. It offers, top to bottom: the button **Open a file...**, which has the keyboard focus; the files opened lately; what this version cannot do yet; what a ROM is; and the kinds of ROM this version can open.

## Opening a file

Three ways lead to the same thing: **File > Open...** (Ctrl+O) or the button on the start page; dropping a file anywhere on the window; **File > Recent files** or the list on the start page.

The program finds out by itself what kind of ROM the file is (see [[Supported ROMs]]). It is never asked of the user.

A file that is not opened leaves the window as it was and is explained above the page, with a button for the way on:

| What is wrong | What the window says |
|---|---|
| The file is gone | "... is no longer there. It may have been moved, renamed or deleted." It is also taken off the recent files |
| It is a folder | "... is a folder, not a file. Choose the ROM file inside it." |
| Another program holds it | "Another program is using ... Close it there, then try again." |
| Windows refuses | "Windows did not let this program read ..." |
| It is empty | "... is empty. The copy, the download or the read from the chip did not finish." |
| It is smaller than a ROM | "... is too small to be a ROM", with both sizes |
| It is far larger than a ROM | "... is too large to be a ROM". Such a file is not read at all |
| The right size, but no known ROM | "This version does not know the ROM in ...", with a link to the list of ROMs that open. The file's SHA-256 is in the technical details, for a report |

Opening never changes the file.

## The list

**Overview** comes first; under it the groups (Fuel, Ignition) with their tables and settings, in the order the description of the ROM gives them. The groups are always open. Pointing at an entry shows what it is.

| Selected | The page shows |
|---|---|
| Overview | What kind of ROM the file was recognised as; whether its checksum is correct, with what a checksum is; its size; what it holds, group by group; its add-ons and whether each is on |
| A group | Its tables and settings, each with its description |
| A table | What the table is, in a sentence; what its rows and columns stand for, how many steps each has and from where to where they run; the range of its values; whether the engine computer uses it with VTEC on or off; and its cells, to read and to change |
| A setting | Its value with its unit, and from where to where it can be set |

## The cells of a table

Under **Values** a table shows its cells: one row for each step of the rows (engine speed, for the fuel and ignition maps), one column for each step of the columns (engine load). The steps are written along the top and down the left side. Each cell has a colour between blue, for the smallest value of the table, and red, for the largest, so that the shape of a table shows at a glance; the number is always there too.

| To | Do |
|---|---|
| Move | Arrow keys; Home and End within a row; Ctrl+Home and Ctrl+End to the first and the last cell. Or click a cell |
| Select more cells | Hold Shift while moving, or drag with the mouse. Ctrl+A selects all. Ctrl with a click adds single cells |
| Change the selected cells | Type a number and press Enter. Esc leaves them as they were. F2 changes the number that is there |
| Raise or lower them | + and -. Each press is one step, the smallest change the cell can take |
| Copy and paste | Ctrl+C and Ctrl+V. What is copied is text, one line per row with tabs between values, so it goes to and from a spreadsheet. One copied number is pasted into every selected cell; a block is laid down from the top left selected cell |

A number may be typed with a point or with a comma as its decimal mark. It is stored as the nearest value the cell can hold; a number that is too large or too small becomes the largest or the smallest the cell can hold, and the line under the cells says so. That line says what the last action did.

While a number is being typed, + and - are part of the number. To type a negative number, press F2 first.

Cells are written with as many decimals as tell one step from the next: a table that moves in steps of 0.25 shows two.

The page follows the table: the range under **How to read it** is that of the cells as they are now. A screen reader reads a cell as its place and its value ("RPM 3000, MAP kPa 75: 33.5").

The grid can also show where the engine is running, by framing the cells around that point, and how many logged readings fell in each cell. Both wait for the datalogger.

## Technical details

**View > Technical details** (Ctrl+T), or the button at the foot of a page, shows on every page what only matters to someone working on the bytes: the full path, the SHA-256, the identifier of the ROM's description, the platform, the checksum rule, addresses, cell widths, scaling and how the steps of rows and columns are stored. The choice is remembered.

Every value on a page is a field that can be reached with Tab, selected and copied.

## Text size

**View > Larger text** (Ctrl++), **Smaller text** (Ctrl+-) and **Normal text size** (Ctrl+0) set the size of all text, from the normal size up to twice that. The list widens with it and pages grow taller. The choice is remembered.

## Keyboard

| Key | Does |
|---|---|
| Alt, then the underlined letter | Opens a menu; every command has a letter |
| Ctrl+O, Ctrl+W | Open a file, close it |
| F6, Shift+F6 | Moves between the list and the page |
| Tab, Shift+Tab | Moves through the fields and buttons of the page, and to the divider, which the arrow keys move |
| Page Up, Page Down, Ctrl+Home, Ctrl+End | Scroll the page. In the cells of a table they move between cells |
| Arrow keys, Shift, a number, Enter, Esc, F2, +, -, Ctrl+A, Ctrl+C, Ctrl+V | In the cells of a table: see [The cells of a table](#the-cells-of-a-table) |
| Ctrl+T | Technical details |
| Ctrl++, Ctrl+-, Ctrl+0 | Text size |
| F1 | Opens this page |

While a command is highlighted in a menu, the status bar says what it does.

## Status bar

| Field | Meaning |
|---|---|
| File | The name of the open file; the full path when pointed at. "No file open" otherwise |
| Kind of ROM | What the file was recognised as |
| Checksum | "Checksum correct", "Checksum wrong" or "No checksum" (some kinds of ROM have none), with a symbol and a colour that say the same. It is the checksum of the file as it was opened: changing a table does not make it wrong |

## What is remembered

The place and size of the window, the width of the list, the text size, whether technical details are shown, and the eight files opened last. They are kept in `%AppData%\HondaRomEditor\settings.json`; deleting the file brings the defaults back. A place that is no longer on any screen is not restored.

## Windows contrast themes

When a Windows contrast theme is on, the window takes all its colours from it. Otherwise it has one look, black, and no setting to change it.

## Help menu

**Getting started** (F1) opens this page, **Which ROMs can be opened** opens [[Supported ROMs]], **Report a problem** opens the issue list. **About** shows the version and copies the versions of the program, .NET and Windows for a report.
