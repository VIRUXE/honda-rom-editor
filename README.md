# Honda ROM Editor

A ROM editor and datalogger for Honda OBD0 and OBD1 engine computers, written in C# for Windows.

**Status: early development.** The core ROM model and project file formats exist; there is no
application window yet. Do not flash anything produced by this code into a car.

## What it is meant to do

- Edit fuel, ignition and option parameters with a table grid, 2D/3D graphs, undo/redo and
  ROM-to-ROM compare.
- Work two ways:
  - **Source projects** — the ROM is assembled from source, features are build-time toggles, and
    tuning edits are stored separately as symbol-relative overlays.
  - **Definition packs** — existing `.bin` files are opened through data files that identify the
    ROM and describe its tables, with verified patch recipes.
- Datalog with a selectable protocol per ROM, so the ROM can match whatever logger, dash or
  emulator is connected.
- Talk to ROM emulators (Moates Ostrich, One ROM, CobraRTP) for live tuning.
- Be testable end to end without an ECU, against an emulated one.

## Building

Requires the .NET 11 SDK.

```
dotnet build HondaRomEditor.slnx
dotnet test HondaRomEditor.slnx
```

## Layout

| Project | Contents |
|---|---|
| `src/HRE.Core` | ROM image, checksum, parameter model, table math, undo/redo document |
| `src/HRE.Definitions` | Project manifest, parameter definitions, feature flags, calibration overlay, symbol map |
| `tests/HRE.Tests` | Unit tests |

## Licence

Public domain, under [The Unlicense](LICENSE).
