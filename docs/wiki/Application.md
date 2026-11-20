# Application

The editor is one window: a menu bar, a list of what the open ROM holds, a page for what is selected, and a status bar. A table's page is its cells, with two pictures of the table beside them, a surface and a graph, and what is said of the table under them. What is changed can be undone step by step and is saved with the checksum corrected. It is built to the [[Design principles]]: usable with the keyboard alone and with a screen reader, in plain words, with the technical detail one command away.

![The main window with a fuel map selected: its cells first, in colours from blue for the smallest value to red for the largest; beside them the table as a surface and as a graph; under them the keys, what the table is and how to read it](https://raw.githubusercontent.com/VIRUXE/honda-rom-editor/main/docs/assets/shell.png)

Run it with `dotnet run --project src/HRE.App`, or start `HondaRomEditor.exe`. A ROM file given on the command line is opened.

> Early version. It opens ROM files, lets you change their tables and settings, and saves them with the checksum corrected. It cannot log the engine or work with an emulator yet.

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

Opening never changes the file. When the ROM that is open has changes that are not saved, the program first asks what to do with them (see [Saving](#saving)); a file that would not open anyway is explained without that question.

## The list

**Overview** comes first; under it the groups (Fuel, Ignition) with their tables and settings, in the order the description of the ROM gives them. The groups are always open. Pointing at an entry shows what it is.

| Selected | The page shows |
|---|---|
| Overview | What kind of ROM the file was recognised as; whether its checksum is correct, with what a checksum is; its size; what it holds, group by group; its add-ons and whether each is on |
| A group | Its tables and settings, each with its description |
| A table | Its cells, to read and to change, straight under its name. Beside them, or under them in a narrow window, the table as a surface and as a graph. Under them: the keys that work in the cells; what the table is, in a sentence; what its rows and columns stand for, how many steps each has and from where to where they run; the range of its values; whether the engine computer uses it with VTEC on or off |
| A setting | Its value in a field, to read and to change, with its unit beside it. Under it: the keys that work in the field; what the setting is; from where to where it can be set, and in which steps. A value the ROM's description marks as fixed is shown, not offered for change |

## The cells of a table

A table's page starts with its cells, and they take the keyboard focus when the page is reached with F6: one row for each step of the rows (engine speed, for the fuel and ignition maps), one column for each step of the columns (engine load). The steps are written along the top and down the left side. Each cell has a colour between blue, for the smallest value of the table, and red, for the largest, so that the shape of a table shows at a glance; the number is always there too.

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

## The surface and the graph

Two pictures of the table stand beside its cells, where a change to a cell shows in them at once. In a window too narrow for that they stand under the cells. They hold nothing of their own: they draw the cells, mark the cell the keyboard is on, and change cells through the grid, so every change made in a picture is the same step, undone the same way, as one made in the cells.

**The surface** is the whole table seen from above and to one side. Each cell is a corner of it, as high as its value and in the colour it has in the cells; the first row is at the back and the first column on the left, as if the cells were laid flat. The two sides at the front carry the steps they run between, and the upright at the left the smallest and the largest value. The cell the keyboard is on is marked with a dot, and so are the selected cells.

| To | Do |
|---|---|
| Turn and tilt it | Drag it with the mouse, or press the arrow keys. It is never seen from underneath |
| Go back to the view it starts with | Home |
| Move to a cell | Click its corner |

**The graph** shows one column of the table as a line, with a point for each row: the column of the cell the keyboard is on, named above the line. The other columns are drawn faintly behind it, to compare it with. The points are spaced evenly, as the cells are, and each has the colour of its cell; the value of the cell the keyboard is on is written beside its point. A table of a single row is drawn as its one line, along its columns.

| To | Do |
|---|---|
| Change a cell | Drag its point up or down. While it is dragged the point shows the value it will be stored as, and the number beside it follows. Esc lets go without a change |
| Change several cells together | Select them in the cells, then drag the point of one of them: the others move by the same amount |
| Raise or lower the cell a step | The up and down arrow keys |
| Move along the line | The left and right arrow keys, or click a point |
| Go to the next column | Ctrl with the left and right arrow keys, or click a point of another line |

A point cannot be dragged past what its cell can hold. A drag is one step for undo, however many cells it moved.

A screen reader reads each picture by its name ("Graph of Fuel Map (Low Cam)") and what it shows in words: where the smallest and the largest values are, and the cell the keyboard is on. The values themselves are read in the cells.

## A setting

A setting is a single value, such as a rev limit. Its page starts with the value in a field, which takes the keyboard focus when the page is reached with F6.

| To | Do |
|---|---|
| Set it | Type a number and press Enter, or leave the field. Emptying the field leaves the value as it was |
| Raise or lower it | The up and down arrow keys, or the two small buttons in the field. Each press is one step, the smallest change the setting can take |

As in a table, a point or a comma may be typed as the decimal mark, the number is stored as the nearest step, and a number outside what the setting can be becomes the nearest it can be; the line under the field says so. A screen reader reads the field as the name of the setting, its value, and the smallest and largest it can be.

## Undo and redo

Every change is one step: typing a value into one cell or into fifty, a press of + or -, a paste, a new value of a setting. **Edit > Undo** (Ctrl+Z) takes the last step back and **Edit > Redo** (Ctrl+Y) makes it again; both name the step in the menu ("Undo Change Fuel Map (Low Cam)"). The steps go back to the moment the file was opened, and saving does not end them. What the command did is written on the line under the cells or the field.

While a number is being typed into a cell, Ctrl+Z takes back the typing, not the last step.

## Saving

**File > Save** (Ctrl+S) writes the ROM to its file. **File > Save as...** (Ctrl+Shift+S) asks for a name, writes the ROM there, and goes on working in that file; the first file stays as it is.

- The checksum is corrected on the way, without asking. A file that was opened with a wrong checksum has a correct one after a save.
- The first time the program replaces a file, it keeps the file as it was next to it, under the same name with `.bak` added (`tune.bin.bak`), and says so above the page. That copy is made once and never overwritten: it stays the file as it was before this program first changed it. To go back to it, rename it.
- The ROM is written to a temporary file that then takes the place of the ROM file in one step, so a crash or a power cut cannot leave half a ROM.
- What is being typed into a cell or a field is taken as typed and saved with the rest.

While there are changes that are not saved, the title bar says "(changes not saved)" after the name of the file and the status bar says "Changes not saved".

A file that cannot be written is explained above the page, with the button **Save as...**; the changes stay in the window:

| What is wrong | What the window says |
|---|---|
| Another program holds the file | "Another program is using ... Close the file there and save again, or save under another name." |
| Windows refuses: the file is read-only, or the folder cannot be written to | "Windows did not let this program write ..." |
| The name is not one Windows accepts | "That is not a file name Windows accepts" |
| Anything else | "... could not be saved", with what Windows said in the technical details |

Closing the file, opening another one and closing the window ask first when there are changes that are not saved: "Save the changes to ...?", with **Save**, **Don't save** and **Cancel**. Enter saves, Esc goes back to the ROM. When the save does not go through, the ROM stays open. This is the only question the program asks.

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
| Ctrl+S, Ctrl+Shift+S | Save; save as another file |
| Ctrl+Z, Ctrl+Y | Undo, redo |
| F6, Shift+F6 | Moves between the list and the page |
| Tab, Shift+Tab | Moves through the cells, the pictures, the fields and the buttons of the page, and to the divider, which the arrow keys move |
| Page Up, Page Down, Ctrl+Home, Ctrl+End | Scroll the page. In the cells of a table they move between cells |
| Arrow keys, Shift, a number, Enter, Esc, F2, +, -, Ctrl+A, Ctrl+C, Ctrl+V | In the cells of a table: see [The cells of a table](#the-cells-of-a-table) |
| Arrow keys, Home | In the surface of a table: turn and tilt it, and bring back the view it starts with |
| Arrow keys, Ctrl with left and right, Esc | In the graph of a table: see [The surface and the graph](#the-surface-and-the-graph) |
| A number, Enter, the up and down arrow keys | In the field of a setting: see [A setting](#a-setting) |
| Ctrl+T | Technical details |
| Ctrl++, Ctrl+-, Ctrl+0 | Text size |
| F1 | Opens this page |

While a command is highlighted in a menu, the status bar says what it does.

## Status bar

| Field | Meaning |
|---|---|
| File | The name of the open file; the full path when pointed at. "No file open" otherwise |
| Changes | "Changes not saved", while there are any |
| Kind of ROM | What the file was recognised as |
| Checksum | "Checksum correct", "Checksum wrong" or "No checksum" (some kinds of ROM have none), with a symbol and a colour that say the same. It is the checksum of the file, as it was opened or last saved: changing a table does not make it wrong, and saving makes it correct |

## What is remembered

The place and size of the window, the width of the list, the text size, whether technical details are shown, and the eight files opened last. They are kept in `%AppData%\HondaRomEditor\settings.json`; deleting the file brings the defaults back. A place that is no longer on any screen is not restored.

## Windows contrast themes

When a Windows contrast theme is on, the window takes all its colours from it. Otherwise it has one look, black, and no setting to change it.

## Help menu

**Getting started** (F1) opens this page, **Which ROMs can be opened** opens [[Supported ROMs]], **Report a problem** opens the issue list. **About** shows the version and copies the versions of the program, .NET and Windows for a report.
