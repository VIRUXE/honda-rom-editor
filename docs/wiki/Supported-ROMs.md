# Supported ROMs

An existing `.bin` is opened through a definition pack ([[Definition packs]] documents the format). The packs that ship with the editor are in [`packs/`](https://github.com/VIRUXE/honda-rom-editor/tree/main/packs).

## Factory firmware

| Pack | Describes | Maps | Checksum byte |
|---|---|---|---|
| `obd1-p30` | Factory OBD1 firmware of the P30 type. Checked on ROM numbers 202, 203, 205, 209, 215, 219, 224, 230, 231, 232, 233, 235, 237 and 240, which include the P30 (203, 209, 240) and the P28 with ROM number 230 | 20 x 10, both cams | `0x7FFF` |
| `obd1-p28` | Factory OBD1 firmware of the P28 type. Checked on ROM numbers 243, 244, 245, 248, 249, 251, 259, 301, 303, 304, 305, 311, 314, 315, 317, 318, 321, 327 and 368, which include the P28 (304, 315, 317, 327) | 20 x 10, both cams | `0x7FFF` |
| `obd1-p72` | Factory OBD1 firmware of the P72 type. Checked on ROM numbers 269, 270, 273, 274 and 332 | 20 x 10 low cam, 15 x 10 high cam | `0x003B` |

The three factory packs go by the firmware a ROM runs, not by the part number on the ECU: the same code was shipped under many part numbers, and one part number can carry either type (a P28 is of the P30 type with ROM number 230 and of the P28 type with 304).

## ROMs changed by a tuning program

| Pack | Describes | Maps | Checksum byte |
|---|---|---|---|
| `crome-p30-boost` | P30 type with CROME's Boost Tools+: more MAP columns for boost, the maps moved to free space | 20 rows, 16 to 24 columns, wherever the ROM has them | `0x7FFF` |
| `crome-p30-boost-rev` | The same with CROME's Rev Tools, which gives the high-cam maps an rpm axis that reaches 11000 rpm | as above | `0x7FFF` |
| `crome-p30-boost1` | P30 type with the first version of CROME's Boost Tools | 20 x 16 | `0x7FFF` |
| `crome-p72-boost` | P72 type with CROME's Boost Tools+ | 20 rows low cam, 15 high cam, 16 to 24 columns, wherever the ROM has them | `0x7FFF` |
| `uberdata-p72` | The boost base ROMs that Uberdata ships: P72 type with a MAP code of twice the factory range | 20 x 15 low cam, 15 x 15 high cam | none: these ROMs skip the check |
| `crome-gold` | The CROME Gold base ROM, v0.94 | 20 x 24, both cams | `0x003B` |
| `ectune-273` | ROMs eCtune builds on its eCt-273 firmware, v0.0.65 | 20 x 24, both cams, two sets | none: the firmware does not check one |

### ROMs made with CROME

CROME does not sign the ROMs it saves. What it leaves depends on what was done:

- **Edited tables only.** The ROM is still the firmware it started from, with other table data and a corrected checksum. It is recognised as that firmware, by one of the three factory packs, and nothing in it says which program made it.
- **An add-on that changes the firmware.** Boost Tools, Boost Tools+ and Rev Tools each rewrite a few instructions, always the same ones. A pack compares those instructions, so such a ROM is recognised for what it carries. Boost Tools+ lets the tuner choose the number of boost columns and places the maps accordingly; the pack reads the column count and the addresses from the instructions that hold them, so one pack covers every size.
- **The Gold base.** A firmware of its own, recognised by its own pack.

A ROM with an add-on that changes the map lookups in a way no pack knows is reported as unknown rather than guessed at.

## How a ROM is recognised

Each pack's fingerprint is the handful of instructions with which the firmware finds its main maps and axes, and the ones that turn engine speed and manifold pressure into the codes the axes are written in. Table data is never compared. That has three consequences:

- **A tuned ROM is still recognised.** Every cell, multiplier and breakpoint of every map can change. The tests rewrite all of them in every image of the corpus, identify the result again and find the same maps in it.
- **A ROM is never read from the wrong place.** Where the instructions are not the ones a pack knows, the pack does not match and the ROM is reported as unknown. Where a pack reads an address from the ROM, a parameter that would land on the firmware's own code or on the checksum byte is left out.
- **No ROM matches two packs.** Every two packs disagree on at least one byte that both compare.

A file that is longer than the ROM is judged by the ROM it starts with: notes a tuning program appended, or the ROM written twice to fill a 64 KiB chip, do not hide it. A dump that stops short of 32 KiB is not recognised, because it could be neither checked nor saved as that ROM.

## What a pack exposes

The four main maps of each set: fuel and ignition, low cam and high cam.

| | Stored | Shown as |
|---|---|---|
| Fuel cell | one byte, with one multiplier byte per column after the last row | cell x multiplier / 4: the ECU uses cell x multiplier, and a quarter of it is the figure CROME and eCtune show |
| Ignition cell | one byte | degrees before top dead centre: byte x 0.25 - 6 |
| Low-cam RPM axis | one byte per row, doubling every 64 steps from 500 rpm | rpm |
| High-cam RPM axis | one byte per row, linear | rpm: byte x 35.21 in the factory firmware, x 43.08 with Rev Tools, in the CROME Gold base and in eCt-273 |
| MAP axis | one byte per column | kPa. See below |

Axes are read from the ROM, because every ROM has its own: two factory ROMs of the same type rarely share an RPM axis. The last breakpoint of an axis is stored as zero and stands for 256.

What a MAP column stands for depends on the firmware:

| ROM | The stored byte is | Shown as |
|---|---|---|
| Factory firmware | a code: twice the sensor reading above 24 | kPa for the factory sensor, 11 to 104 |
| Uberdata boost bases | the sensor reading above 24, so the code reaches twice as far | kPa for the factory sensor, 11 to 196 |
| CROME Boost Tools and Boost Tools+ | the raw sensor reading | kPa, from the range of the MAP sensor that the tune carries next to the axis: the factory sensor, a 2.5 or 3 bar one, or any other |
| CROME Gold base, eCt-273 | a code whose scale depends on the sensor set in the ROM | the stored code, until [issue 41](https://github.com/VIRUXE/honda-rom-editor/issues/41) |

In the 24-column ROMs a tune uses as many columns as it needs and repeats the last breakpoint in the others. In ROMs of the P72 type for engines without VTEC the high-cam maps and their axis are empty.

Limiters, VTEC points, idle and the other scalars are not in the packs yet.

## Not covered

- A 64 KiB file with the ROM in its upper half.
- Other OBD1 firmware (the P13 and P14 type, non-VTEC P07, the Acura Legend and NSX, ROMs made by other tuning suites) and every OBD0 ROM; see [[OBD0]].

## How the packs are checked

Against a corpus of 259 images of 32 KiB: factory ROMs, tunes made with several programs, the base images tuning programs hand out, and files that are not OBD1 ROMs at all. A list states for each one which pack it is, or that it is none.

- 136 images are identified, each by exactly one pack: 92 of factory firmware (38 of the P30 type, 34 of the P28 type, 20 of the P72 type), 34 with CROME's boost add-ons, 7 Uberdata boost bases, both CROME Gold images and the eCt-273 base. Every one of the 48 CROME tunes in the corpus is among them. The other 123 match no pack: OBD0 ROMs, other OBD1 firmware, files that are not ROMs.
- Files of other sizes are judged as described above: a factory ROM with text after it, one written twice into 64 KiB and a boosted tune with its author's notes after the ROM are identified; dumps of 32000 bytes are not.
- On every image, every parameter resolves and lies inside the image, no map shares cells with another, and every axis read from the image is ordered and spans engine speeds and pressures an engine can see.
- Every image whose bytes sum to zero passes its pack's checksum, and correcting one that does not changes only the checksum byte.
- The P28 with ROM number 230 reads the same through its pack as through its source project: the same map addresses, the same axes in rpm and kPa, the factory column multipliers, 16.5 degrees at idle.
- The P28 with ROM number 304 reads as CROME shows the template it ships: low-cam fuel from 4 to 600, and the same rpm axes, except that CROME reads the closing zero of an axis as 255 where the ECU counts 256.
- A boosted ROM keeps the factory columns and adds boost columns after them. Read through the sensor range the tune carries, its first ten columns come out at the pressures of the factory axis, which the factory pack derives from the firmware instead. The Uberdata bases agree with the factory P72 axis in the same way.

The corpus is not part of the repository; see [[Testing without hardware]].
