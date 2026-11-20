<p align="center">
  <img src="docs/assets/banner.svg" alt="Honda ROM Editor" width="100%">
</p>

<p align="center">
  <a href="https://dotnet.microsoft.com/download/dotnet/11.0"><img alt=".NET 11" src="https://img.shields.io/badge/.NET-11-512BD4?style=for-the-badge&logo=dotnet&logoColor=white"></a>
  <img alt="C#" src="https://img.shields.io/badge/C%23-latest-239120?style=for-the-badge&logo=csharp&logoColor=white">
  <img alt="Windows" src="https://img.shields.io/badge/Windows-WinForms-0078D4?style=for-the-badge&logo=windows11&logoColor=white">
  <img alt="Theme: black" src="https://img.shields.io/badge/theme-black%20only-000000?style=for-the-badge">
  <a href="LICENSE"><img alt="Licence: Unlicense" src="https://img.shields.io/badge/licence-Unlicense-eb3c2d?style=for-the-badge"></a>
</p>

<p align="center">
  <a href="https://github.com/VIRUXE/honda-rom-editor/milestones"><img alt="Milestones" src="https://img.shields.io/github/milestones/closed/VIRUXE/honda-rom-editor?style=flat-square&label=milestones%20done&color=3cbe50"></a>
  <a href="https://github.com/VIRUXE/honda-rom-editor/issues"><img alt="Open issues" src="https://img.shields.io/github/issues/VIRUXE/honda-rom-editor?style=flat-square&color=f0c828"></a>
  <img alt="Last commit" src="https://img.shields.io/github/last-commit/VIRUXE/honda-rom-editor?style=flat-square&color=0096aa">
  <a href="https://github.com/VIRUXE/honda-rom-editor/wiki"><img alt="Wiki" src="https://img.shields.io/badge/docs-wiki-9aa0a6?style=flat-square"></a>
</p>

<p align="center">
  <b>A ROM editor and datalogger for Honda OBD0 and OBD1 engine computers.</b><br>
  Build ROMs from source, open existing ones safely, log with the protocol your hardware speaks.
</p>

> [!WARNING]
> **Early development.** The core, the file formats and the build pipeline work; there is no
> application window yet. Do not flash anything produced by this code into a car.

---

## Highlights

<table>
<tr>
<td width="50%" valign="top">

### ROMs built from source
The ROM is assembled, not patched. Features are build-time switches, tables are bound to
**symbols** instead of addresses, and your tuning lives in a separate overlay that is re-applied
on every build.

</td>
<td width="50%" valign="top">

### Existing `.bin` files, safely
Definition packs identify a ROM by fingerprint and describe its tables as data, reading from the
ROM itself where a tuning program has enlarged or moved them. Patch recipes state the bytes they
expect to replace — and refuse to run when they don't match.

</td>
</tr>
<tr>
<td width="50%" valign="top">

### Pick your datalog protocol
Dashes, Bluetooth loggers and tuning tools each expect a specific protocol, so you choose which
one the ROM speaks — including a new framed, timestamped stream built for accurate tuning.

</td>
<td width="50%" valign="top">

### Tuning math done properly
Bilinear cell weighting, lambda-domain averaging and exhaust transport-delay compensation, so a
reading lands on the cell that actually produced it.

</td>
</tr>
<tr>
<td width="50%" valign="top">

### Live tuning hardware
Moates Ostrich, One ROM and CobraRTP emulators behind one device layer, recognised by how they
answer rather than by COM port number.

</td>
<td width="50%" valign="top">

### No ECU required
Everything is tested against a virtual ECU that runs the real ROM and serves its serial port —
no engine, bench harness or emulator needed.

</td>
</tr>
</table>

## Roadmap

| | Milestone | What it delivers |
|:-:|---|---|
| <img alt="Done" src="https://img.shields.io/badge/-done-3cbe50?style=flat-square"> | **M1** Core and formats | ROM image, checksum, parameter model, undo/redo, project file formats |
| <img alt="Done" src="https://img.shields.io/badge/-done-3cbe50?style=flat-square"> | **M2** Build | Source project → ROM image through the assembler, overlay applied, checksum fixed |
| <img alt="In progress" src="https://img.shields.io/badge/-in%20progress-d29922?style=flat-square"> | **M3** Definition packs | Open existing `.bin` files; verified patch recipes. The pack format, patch recipes and packs for the factory P28, P30 and P72 firmware, ROMs with CROME's boost add-ons, Uberdata's boost bases, the CROME Gold base and eCtune's base are in; pressure units on the MAP axis of the last two are next |
| <img alt="Planned" src="https://img.shields.io/badge/-planned-30363d?style=flat-square"> | **M4** App shell | Black-themed window, table grid, 3D surface, undo/redo, save |
| <img alt="Planned" src="https://img.shields.io/badge/-planned-30363d?style=flat-square"> | **M5** Virtual ECU serial port | Emulated ECU serving its real serial routines over TCP |
| <img alt="Planned" src="https://img.shields.io/badge/-planned-30363d?style=flat-square"> | **M6** Legacy datalog drivers | eCtune-compatible, CROME QD2 / QD3-compatible |
| <img alt="Planned" src="https://img.shields.io/badge/-planned-30363d?style=flat-square"> | **M7** Selectable ROM protocols | Choose the protocol a ROM speaks; the HRE stream protocol |
| <img alt="Planned" src="https://img.shields.io/badge/-planned-30363d?style=flat-square"> | **M8** Tuning pipeline | Cell tracing, lambda accumulation, live display, playback |
| <img alt="Planned" src="https://img.shields.io/badge/-planned-30363d?style=flat-square"> | **M9** Emulator drivers | Ostrich 1.0 / 2.0, One ROM, CobraRTP |
| <img alt="Planned" src="https://img.shields.io/badge/-planned-30363d?style=flat-square"> | **M10** OBD0 | P04 and the dual-point ECUs first, then PR3 / PW0 and the 8051 family |
| <img alt="Planned" src="https://img.shields.io/badge/-planned-30363d?style=flat-square"> | **M11** Feature bundle | Editor and firmware features, tier by tier |
| <img alt="Planned" src="https://img.shields.io/badge/-planned-30363d?style=flat-square"> | **M12** Hardware verification | The emulator checks repeated on real devices |

Progress lives on the [project board](https://github.com/users/VIRUXE/projects/4).

## Getting started

```powershell
git clone https://github.com/VIRUXE/honda-rom-editor
cd honda-rom-editor
dotnet build HondaRomEditor.slnx
dotnet test HondaRomEditor.slnx
```

Requires the **.NET 11 SDK**. Building a source project also needs the `rasm66k` assembler: put it
on the `PATH`, next to the application, or point `HRE_RASM66K` at it. Tests that need it, or files
outside this repository, are skipped when those are missing.

## What's inside

| Project | Contents |
|---|---|
| [`HRE.Core`](src/HRE.Core) | Immutable ROM image, checksum, parameter model, table math, undo/redo document |
| [`HRE.Definitions`](src/HRE.Definitions) | Project manifest, parameter definitions, feature flags, calibration overlay, symbol map; definition packs and patch recipes |
| [`HRE.Build`](src/HRE.Build) | Runs the assembler and turns a source project into a ROM image |
| [`packs`](packs) | Definition packs for the common OBD1 ROMs |
| [`HRE.Tests`](tests/HRE.Tests) | Unit, build and workspace tests |

## Documentation

| | |
|---|---|
| [Architecture](https://github.com/VIRUXE/honda-rom-editor/wiki/Architecture) | Projects and conventions |
| [ROM approach](https://github.com/VIRUXE/honda-rom-editor/wiki/ROM-approach) | Source projects and definition packs |
| [Definition packs](https://github.com/VIRUXE/honda-rom-editor/wiki/Definition-packs) | The pack file format: fingerprints, checksum, parameters, patches |
| [Supported ROMs](https://github.com/VIRUXE/honda-rom-editor/wiki/Supported-ROMs) | Which existing ROMs open, how they are recognised and how that is checked |
| [Datalog protocol](https://github.com/VIRUXE/honda-rom-editor/wiki/Datalog-protocol) | Selectable protocols and the HRE stream |
| [Hardware](https://github.com/VIRUXE/honda-rom-editor/wiki/Hardware) | Supported ROM emulators |
| [OBD0](https://github.com/VIRUXE/honda-rom-editor/wiki/OBD0) | Which OBD0 ECUs, and how |
| [Feature bundle](https://github.com/VIRUXE/honda-rom-editor/wiki/Feature-bundle) | Every planned editor and firmware feature |
| [Testing without hardware](https://github.com/VIRUXE/honda-rom-editor/wiki/Testing-without-hardware) | How each area is verified |
| [Research notes](docs/research/README.md) | How we know: the ROM corpus, the firmware types, what tuning programs leave in a ROM |

## Licence

Public domain, under [The Unlicense](LICENSE).
