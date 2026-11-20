# Application

The editor is one window: a menu bar, the parameter tree on the left, the detail view on the right and a status bar. It has one theme, black, and no setting to change it.

![The main window with a fuel map selected](https://raw.githubusercontent.com/VIRUXE/honda-rom-editor/main/docs/assets/shell.png)

Run it with `dotnet run --project src/HRE.App`, or start `HondaRomEditor.exe`. A ROM image given on the command line is opened.

## Opening a ROM

**File > Open ROM** (Ctrl+O) reads a `.bin` file and looks for the [definition pack](Definition-packs) whose fingerprint matches it. The packs are the `*.pack.toml` files in the `packs` folder next to the program; the start page lists the ones that were loaded and reports any that could not be read.

- A recognised ROM fills the tree with the parameters its pack finds in it.
- A ROM no pack recognises still opens: the window shows its size and SHA-256 and says that nothing describes its tables.
- A file that cannot be read is reported in the detail view; the ROM that was open stays open.

At this stage a ROM is opened for reading. The table grid, editing and saving are the next steps of the same milestone.

## Parameter tree

The ROM is the root; under it the categories, in the order the pack lists them, and under each its parameters. What is selected decides what the detail view shows.

| Selected | Detail view |
|---|---|
| The ROM | File, size and SHA-256; the pack that recognised it, the platform, whether the checksum is correct and what it covers, the datalog protocol; its parameters by category; the state of each patch the pack knows (applied, not applied, conflict); parameters of the pack that were left out, and why |
| A category | Its parameters and what kind each is |
| A table | Rows and columns, address range, cell width, unit and scaling, column multipliers, the VTEC state it is read in, the range of its values; for each axis its label, number of breakpoints, how and where it is stored, and its range |
| A value | Address, byte order, unit and scaling, the value as shown and as stored, the range it may take |

**View > Expand all** and **Collapse all** open and close the categories.

## Status bar

| Field | Meaning |
|---|---|
| Path | The file that is open; with no ROM open, how many definition packs were loaded |
| ROM | The name of the pack that recognised the ROM, or "Not recognised" |
| Parameters | How many parameters the pack finds in this image |
| Checksum | "Checksum correct" or "Checksum wrong" by the pack's rule; "No checksum" for ROMs that do not check themselves; "Checksum unknown" when no pack says where it is |

## Theme

True black background, light text, one teal accent. All colours come from one palette, and a unit test holds them to WCAG contrast ratios. What WinForms draws by itself in its dark colour mode and what the editor draws instead is written down in the research note [The black theme](https://github.com/VIRUXE/honda-rom-editor/blob/main/docs/research/Dark-theme.md).
