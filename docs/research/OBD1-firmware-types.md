# OBD1 firmware types

How the factory OBD1 firmware for the OKI MSM66207 keeps its main maps, worked out from the ROM images of the [corpus](Corpus.md). The packs `obd1-p30`, `obd1-p28` and `obd1-p72` are built on this page.

## Three kinds of code

Sorted by their code (see [Corpus](Corpus.md)), the factory images with two cam profiles fall into three types. Within a type the code is 98 % or more identical; the calibration differs. The types are named after the ECU each is best known from.

| Type | ROM numbers seen | Notes |
|---|---|---|
| P30 | 202, 203, 205, 209, 215, 219, 224, 230, 231, 232, 233, 235, 237, 240 | The earlier firmware. Includes the P28 with ROM number 230 |
| P28 | 243, 244, 245, 248, 249, 251, 259, 301, 303, 304, 305, 311, 314, 315, 317, 318, 321, 327, 368 | The later firmware |
| P72 | 269, 270, 273, 274, 332 | 15 rows on the high cam. Images for engines without VTEC leave the high-cam maps and their axis empty |

The part number on the ECU does not tell the type: a P28 is of the P30 type with ROM number 230 and of the P28 type with 304, and P08 and P29 images appear under both.

## How a main map is read

All three types read every main map through the same two routines.

**Set-up.** The caller loads the column count into `r0`, the row count into `r1`, the column index and fraction, the row index and fraction, and the address of the map into `X1`. In bytes: `98 cc 99 rr`, four index loads, `60 lo hi`. The fuel code loads the high-cam map first and replaces it by the low-cam one when the low cam is in use. The ignition code of the P30 type does the same; that of the P28 and P72 types loads the low-cam map first.

**The 2-D lookup.** Cell (row, column) is at `map + columns x row + column`, one byte, row by row. The routine interpolates the four cells around the operating point. With the flag `PSWL.5` set it first multiplies each cell by the byte at `map + columns x rows + column`: one multiplier per column, in the row after the last. Fuel lookups set the flag (`A3 1D` before the call), ignition lookups clear it (`A3 0D`). So a fuel map occupies `columns x rows + columns` bytes and an ignition map `columns x rows`.

**The breakpoint search.** An axis is a list of increasing bytes. The search starts from the index found last time, limited by a count the caller gives, and walks until the next byte is larger than the input or is zero: after the first entry a zero ends the list and counts as 256. It returns the index and a 16-bit fraction towards the next breakpoint, so the ECU interpolates between breakpoints in stored units.

Where these are in each type:

| | P30 type | P28 type | P72 type |
|---|---|---|---|
| 2-D lookup | `0x5010` | `0x59E4` | `0x4EA6` |
| Breakpoint search | `0x4FDE` | `0x59B2` | `0x4E74` |
| Fuel set-up | `0x1234` | `0x12FC` | `0x1266` |
| Fuel map, high cam (operand at) | `0x7104` (`0x1245`) | `0x7122` (`0x130D`) | `0x7D6C` (`0x1277`), 15 rows |
| Fuel map, low cam (operand at) | `0x7032` (`0x1257`) | `0x7050` (`0x1324`) | `0x7C9A` (`0x128B`), 20 rows |
| Ignition set-up | `0x0B39` | `0x0B67` | `0x0B85`, continued at `0x64DA` |
| Ignition map, high cam (operand at) | `0x7316` (`0x0B4A`) | `0x73AC` (`0x0B92`) | `0x7ED4` (`0x64E9`), 15 rows |
| Ignition map, low cam (operand at) | `0x724E` (`0x0B56`) | `0x72E4` (`0x0B7B`) | `0x7E0C` (`0x64DF`), 20 rows |
| Low-cam rpm axis, 20 bytes (operand at) | `0x700A` (`0x09DC`) | `0x7014` (`0x0A0D`) | `0x7C68` (`0x0A50`) |
| High-cam rpm axis (operand at) | `0x701E`, 20 bytes (`0x09F4`) | `0x7028`, 20 bytes (`0x0A25`) | `0x7C7C`, 15 bytes (`0x0A68`) |
| MAP axis for ignition, 10 bytes (operand at) | `0x7000` (`0x0A17`) | `0x7000` (`0x0A51`) | `0x7C54` (`0x0AA3`) |
| MAP axis for fuel, 10 bytes (operand at) | `0x7000` (`0x0A2C`) | `0x7000` (`0x0A66`) | `0x7C4A` (`0x0AB8`) |

Maps are 20 rows by 10 columns, except the high-cam maps of the P72 type, which have 15 rows.

Not in the packs, and not analysed further:

- Each type has shorter alternative tables that the same set-up switches to under some conditions, with 11 rows: fuel at `0x71D6` and ignition at `0x73DE` in the P30 type; fuel at `0x71F4` and `0x726C`, ignition at `0x7474` and `0x74E2` in the P28 type. The P72 type has alternative ignition maps at `0x7AEC` and `0x7BB4`.
- Each type sets up a third pair of maps of the same shape: at `0x1676` (maps `0x7514` and `0x744C`) in the P30 type, at `0x1793` (`0x7618`, `0x7550`) in the P28 type, at `0x16EE` (`0x7F6A`, 15 x 10) in the P72 type.
- The P28 type has a second MAP axis at `0x700A` and a third rpm axis at `0x703C`; the P72 type a third MAP axis at `0x7C5E` and a third rpm axis at `0x7C8B`.

## What the axis bytes stand for

| Axis | The byte is | In engineering units | Code |
|---|---|---|---|
| Low-cam rpm | a code that doubles every 64 steps | rpm = 500 x 2^(b div 64) x (1 + (b mod 64) / 64) | P30 type `0x07AA`, P28 type `0x07D3`, P72 type `0x07C7` |
| High-cam rpm | 53248 / crank period | rpm = b x 1875000 / 53248 = b x 35.21 | `MOV er0, #0D000h` then `DIV`: P30 type `0x0808`, P28 type `0x0830`, P72 type `0x0824` |
| MAP | twice the 8-bit sensor reading above 24 | kPa = (b / 2 + 24) x 0.7221 - 5.9 = b x 0.36105 + 11.43 | P30 type `0x4F21`, P28 type `0x58F8`, P72 type `0x4DCB` |

**Low-cam rpm.** The crank period is 1875000 / rpm. The code divides 480000 by the period and halves it, after halving the dividend once for each octave above 500 rpm and adding 64 per octave: within an octave the code is linear in rpm, and each octave starts at a multiple of 64. The octave limits are periods 1875, 937, 468 and 234 (1000, 2000, 4000 and 8000 rpm): a table at `0x6D08` in the P30 type, immediates in the other two. The factory axes confirm it: every one has breakpoints at `40h`, `80h` and `C0h`, the 1000, 2000 and 4000 rpm marks.

**MAP.** The routine subtracts 24 from the reading and doubles it, with a ninth bit of the reading shifted in, and saturates at 255. The sensor scale, 0.7221 kPa per count with -5.9 kPa at zero, is the one tuning programs for these ECUs use for the factory sensor; it puts 101 kPa at 2.9 V. It is the one number on this page that does not come out of the ROM.

Checked two ways: the P28 with ROM number 230, read with these conversions, gives the axes its source project derived independently (500 to 8000 rpm, 0 to 9014 rpm, 11 to 104 kPa); and the P28 with ROM number 304 gives the axes CROME shows for the template it ships, apart from the last breakpoint, which CROME reads as 255 where the search counts 256 (7936 and 8979 rpm against 8000 and 9014).

Every image has its own axes. Among the P30-type images of the corpus that keep the factory layout there are 11 different low-cam rpm axes and 9 high-cam ones, most of them in factory images. That is why the packs read the axes from the ROM.

## What the cells stand for

- **Ignition**: degrees before top dead centre = byte x 0.25 - 6. The idle cells of the factory maps are `5Ah`, 16.5 degrees.
- **Fuel**: the ECU uses cell x column multiplier, then scales the result further. CROME and eCtune both show a quarter of that product (4 to 600 for the low-cam map of the 304 ROM), and so do the packs. Factory multipliers run from 1 in the first column to about 10 in the last.

## Checksum

The firmware sums all 32768 bytes, 64 at a time in the background, and expects zero.

| | P30 type | P28 type | P72 type |
|---|---|---|---|
| On a mismatch it reads the option byte at | `0x6011` (code at `0x285A`) | `0x60FB` (`0x2BA9`) | `0x547E` (`0x2A1D`) |

If that byte is zero, as in the factory images, the ECU stores a fault code and resets; otherwise the mismatch is ignored. Tuning programs disable the check either by setting the byte or by replacing the read with a jump over the reset (`03 2A 2A` at `0x2A1D` in the P72 type).

Which byte can absorb a correction:

- `0x7FFF` is unused (`FF`) in the P30 and P28 types, and it is the byte CROME corrects.
- In the P72 type the tables run to `0x7FFF`. The four bytes after the vector table, `0x0038` to `0x003B`, hold an identifier that the firmware only copies to RAM at start-up (code at `0x258F`, `0x289C` and `0x2736` in the three types). Tools that edit P72-type ROMs correct the sum in `0x003B`, and so does the pack.

The factory images balance their sum somewhere in those identifier bytes: `0x0039` to `0x003B` differ from one ROM number to the next while `0x7FFF` stays `FF`.

## How to repeat this

```
python tools/research/survey.py          # the clusters
python tools/research/layout.py          # rows, columns and addresses, read from each image's code
python tools/research/layout.py --axes   # the axis bytes and multipliers per layout
rasm66k dasm 203stock.bin p30.asm
python tools/research/loads.py p30.asm 7000 77ff 4
```
