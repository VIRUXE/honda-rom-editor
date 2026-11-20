# Supported ROMs

An existing `.bin` is opened through a definition pack ([[Definition packs]] documents the format). The packs that ship with the editor are in [`packs/`](https://github.com/VIRUXE/honda-rom-editor/tree/main/packs).

| Pack | Describes | Maps | Checksum byte |
|---|---|---|---|
| `obd1-p30` | Factory OBD1 firmware of the P30 type. Checked on ROM numbers 202, 203, 205, 209, 215, 219, 224, 230, 231, 232, 233, 235, 237 and 240, which include the P30 (203, 209, 240) and the P28 with ROM number 230 | 20 x 10, both cams | `0x7FFF` |
| `obd1-p28` | Factory OBD1 firmware of the P28 type. Checked on ROM numbers 243, 244, 245, 248, 249, 251, 259, 301, 303, 304, 305, 311, 314, 315, 317, 318, 321 and 327, which include the P28 (304, 315, 317, 327) | 20 x 10, both cams | `0x7FFF` |
| `obd1-p72` | Factory OBD1 firmware of the P72 type. Checked on ROM numbers 269, 270, 273, 274 and 332 | 20 x 10 low cam, 15 x 10 high cam | `0x003B` |
| `crome-gold` | The CROME Gold base ROM, v0.94 | 20 x 24, both cams | `0x003B` |
| `ectune-273` | ROMs eCtune builds on its eCt-273 firmware, v0.0.65 | 20 x 24, both cams, two sets | none: the firmware does not check one |

The three factory packs go by the firmware a ROM runs, not by the part number on the ECU: the same code was shipped under many part numbers, and one part number can carry either type (a P28 is of the P30 type with ROM number 230 and of the P28 type with 304).

## How a ROM is recognised

Each pack's fingerprint is the handful of instructions with which the firmware finds its main maps and axes, and the ones that turn engine speed and manifold pressure into the codes the axes are written in. Table data is never compared. That has three consequences:

- **A tuned ROM is still recognised.** Every cell, multiplier and breakpoint of every map can change. The tests rewrite all of them in every image of the corpus and identify the result again.
- **A ROM is never read from the wrong place.** Where a tuning program has enlarged or moved the maps, the instructions name other addresses and sizes, so the pack does not match and the ROM is reported as unknown.
- **No ROM matches two packs.** Every two packs disagree on at least one byte that both compare.

A file with a stray byte after the 32 KiB ROM is recognised as well.

## What a pack exposes

The four main maps of each set: fuel and ignition, low cam and high cam.

| | Stored | Shown as |
|---|---|---|
| Fuel cell | one byte, with one multiplier byte per column after the last row | cell x multiplier / 4: the ECU uses cell x multiplier, and a quarter of it is the figure CROME and eCtune show |
| Ignition cell | one byte | degrees before top dead centre: byte x 0.25 - 6 |
| Low-cam RPM axis | one byte per row, doubling every 64 steps from 500 rpm | rpm |
| High-cam RPM axis | one byte per row, linear | rpm: byte x 35.21 in the factory firmware, x 43.08 in the CROME Gold base and in eCt-273 |
| MAP axis | one byte per column | kPa for the factory sensor; the stored code in the CROME Gold base and in eCt-273, where the pressure a code stands for depends on the MAP sensor set in the ROM |

Axes are read from the ROM, because every ROM has its own: two factory ROMs of the same type rarely share an RPM axis. The last breakpoint of an axis is stored as zero and stands for 256.

In the 24-column ROMs a tune uses as many columns as it needs and repeats the last breakpoint in the others. In ROMs of the P72 type for engines without VTEC the high-cam maps and their axis are empty.

Limiters, VTEC points, idle and the other scalars are not in the packs yet.

## Not covered

- **ROMs whose maps a tuning program enlarged or moved**, such as stock ROMs given extra boost columns: 34 of the 48 CROME tunes in the test corpus. They are tracked in [issue 40](https://github.com/VIRUXE/honda-rom-editor/issues/40).
- Other OBD1 firmware (the P13 and P14 type, non-VTEC P07, ROMs made by other tuning suites) and every OBD0 ROM; see [[OBD0]].

## How the packs are checked

Against a corpus of 193 images: factory ROMs, tunes made with several programs, the base images tuning programs hand out, and files that are not OBD1 ROMs at all. A list states for each one which pack it is, or that it is none.

- 71 images are identified, each by exactly one pack: 33 of the P30 type, 25 of the P28 type, 10 of the P72 type, both CROME Gold images and the eCt-273 base. The other 122 match no pack, among them all 34 tunes with enlarged maps.
- Every parameter lies inside its image, and every axis read from an image is ordered and spans engine speeds and pressures an engine can see.
- Every image whose bytes sum to zero passes its pack's checksum, and correcting one that does not changes only the checksum byte.
- The P28 with ROM number 230 reads the same through its pack as through its source project: the same map addresses, the same axes in rpm and kPa, the factory column multipliers, 16.5 degrees at idle.
- The P28 with ROM number 304 reads as CROME shows the template it ships: low-cam fuel from 4 to 600, and the same rpm axes, except that CROME reads the closing zero of an axis as 255 where the ECU counts 256.

The corpus is not part of the repository; see [[Testing without hardware]].
