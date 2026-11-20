# Feature bundle

Features offered by eCtune and CROME Gold, merged and de-duplicated. Where the two tools model the same feature differently, one model is chosen.

Firmware features are tied to the ROM that implements them, so each is written **once, as a build-time toggle** in a source project. For existing `.bin` files, definition packs expose what that ROM already has.

## Editor

| Tier | Features |
|---|---|
| E1 core | Set / add / percent, copy and paste, interpolate, smooth, undo/redo, 2D graph with drag editing, 3D surface, raw / duration / duty-cycle fuel views, ROM notes, snapshots, units, shortcut list, **ROM-to-ROM compare**, validated ranges |
| E2 tables and setup | Rebuild axes, insert / delete column, boost-table fill, whole-table ignition offset, parameter tables, new-ROM wizard, injector / wideband / gear-ratio presets |
| E3 logging and tuning | Gauges, sensor grid with limits, log graphs, cell trace, lambda overlays, playback, auto-adjust with filters and smart fill, live auto-adjust |
| E4 hardware and import | ROM emulators, chip programmers, live tools (TPS calibration, gear learn, timing sync, clear DTC), import of eCtune and CROME files |

## Firmware

| Tier | Features | Model |
|---|---|---|
| F1 foundation | Selectable datalog protocol, editable rev limit and VTEC points, injector size / dead time / battery offset, MAP sensor type and boost columns, closed-loop limits, labelled sensor-check and DTC toggles | |
| F2 tuning | Cranking / post-start / tip-in trims, coolant and intake-air corrections, overall trim, deceleration fuel cut, idle target, wideband closed loop | Target-lambda table |
| F3 motorsport | Launch limit, full-throttle shift, anti-lag, cut type, per-gear limits, shift light, boost cut, gear table and trims | Launch modes plus clutch input; five gears |
| F4 outputs | General-purpose outputs, fan control, A/C cut, VTEC and intake runners on alternate pins | Outputs with condition windows and fuel / ignition adjustment |
| F5 advanced | Electronic boost control, secondary maps, cylinder trims, Alpha-N, extended RPM axis | Closed-loop boost control with gear / RPM targets |
| F6 protection | Overheat limit, lean protection, overboost on MIL, anti-start | |

## Left out

Licensing and ROM passwords, browser-hosted plugin screens and patch scripts, non-Honda protocols, emulators that are no longer made, dyno and dashboard screens, direct-serial wideband links, and defaults that silently switch off factory checks.
