# Architecture

| Project | Contents |
|---|---|
| `HRE.Core` | Immutable ROM image, checksum, parameter and axis model, table math, undo/redo document |
| `HRE.Definitions` | Project manifest, parameter definitions, feature flags, calibration overlay, symbol map; definition packs and patch recipes |
| `HRE.Build` | Runs the assembler as an external tool and turns a source project into a ROM image |
| `HRE.Comms` | Raw-byte links (serial, TCP, scripted), datalog protocol drivers, ROM emulator drivers |
| `HRE.Datalog` | Channel maps, sample pipeline, cell accumulator, filters, log files |
| `HRE.App` | WinForms application: main window, the list, pages built from standard controls, settings, the black theme |
| `HRE.Tests` | xUnit tests |
| `tools/HRE.Shots` | Saves a picture of the main window, for the documentation and for checking the theme |
| `tools/research` | Scripts that survey the ROM corpus |

## Conventions

- Stateless logic lives in static classes (`Checksum`, `TableMath`, `OverlayOps`, `ConfigInc`, frame codecs). Instances exist only for things with state or a lifetime: the ROM document, links, drivers, devices, forms.
- No mutable static state. A ROM image is immutable; every edit produces a new image and an undo step.
- Interfaces only where there are several real implementations (links, datalog drivers, ROM emulators).
- Opening braces stay on the line that opens the block (see `.editorconfig`).
- The interface follows the [[Design principles]]. One black theme, no setting; colours come from `DarkTheme` only, which hands out the user's contrast theme when Windows has one on.
- The window holds no logic and no wording of its own: `RomSession.Open` says what came of opening a file, `Details` builds every page and so holds every sentence, `PageView` shows a page as standard controls. All three are tested without showing a window.
- Warnings are errors, and the recommended analyzers run in every build (`Directory.Build.props`).
- The assembler and the virtual ECU are separate command-line tools, called as processes.
