# ROMs changed by tuning programs

What CROME, Uberdata and eCtune leave in a ROM, read off the images of the [corpus](Corpus.md), and how the packs recognise each. Addresses and factory code are those of [OBD1 firmware types](OBD1-firmware-types.md).

## CROME

### A tune without add-ons

CROME does not mark the ROMs it saves. Of the 48 CROME tunes in the corpus, 14 differ from a factory image only in table data and in one byte that brings the sum back to zero: `0x7FFF` on the P30 and P28 types. They are recognised as the factory firmware they are (12 of the P30 type, one P28, one P72).

When CROME is given notes to keep with a ROM it writes them after the 32 KiB (`final-notes.bin`, 32,923 bytes).

### Boost Tools+ on the P30 type

28 tunes. The add-on gives the main maps more MAP columns, moves them and their axes to free space, and makes the lookups use the raw MAP reading. Compared with the factory code:

| Address | Factory | With Boost Tools+ |
|---|---|---|
| `0x0829` to `0x083F` | Takes the reading, checks it against limits and calls the MAP code routine (`... 32 21 4F`) | `F5 A3 C6 cc CD 11 C6 00 CF 04 77 00 CB 09 C5 98 18 F4 35 CB 08 FF FF`: loads the 8-bit reading from RAM `A3h` and compares it with a cut-off `cc` the tune sets |
| `0x0A13`, `0x0A28` | `F5 A7`: the column search takes the MAP code | `F5 A3`: it takes the raw reading |
| `0x0A17`, `0x0A2C` | `00 70` | address of the new MAP axis |
| `0x0A1D`, `0x0A32` | `08` | columns - 2 |
| `0x09DC`, `0x09F4` | `0A 70`, `1E 70` | addresses of the moved rpm axes |
| `0x0B3A`, `0x1235` | `0A` | the column count |
| map operands (`0x0B4A`, `0x0B56`, `0x1245`, `0x1257`) | factory addresses | addresses of the moved maps |

Everything else in the lookups is unchanged, including the 20 rows and the two routines. The instructions are the factory ones with other operands, which is what lets one pack read every layout: it compares the instructions and reads the operands.

Six layouts occur in the corpus:

| Columns | MAP axis | Low-cam fuel | Tunes |
|---|---|---|---|
| 16 | `0x6E72` | `0x6EAA` | 20 |
| 16 | `0x70F2` | `0x712A` | 4 |
| 18 | `0x704C` | `0x7086` | 1 |
| 20 | `0x6FA6` | `0x6FE2` | 1 |
| 22 | `0x6F00` | `0x6F3E` | 1 |
| 24 | `0x6E5A` | `0x6E9A` | 1 |

From 24 columns down to 16 at `0x70F2` the block starts 166 bytes later for every two columns fewer: 83 bytes per column, which is one MAP breakpoint, two fuel columns of 21 bytes and two ignition columns of 20. The 16-column layout at `0x6E72` is the fixed one of the earlier versions of the add-on.

**The sensor range.** The five bytes before the MAP axis hold a sensor type (1, 2 or 3) and two little-endian words: the pressures, in mbar, that readings 0 and 255 stand for, each stored 32768 above its value.

| Type | Words | Range, mbar | Tunes |
|---|---|---|---|
| 1 | `C5 7F`, `F6 86` | -59 to 1782 | 15 |
| 2 | `CF 7F`, `9F 8B` | -49 to 2975 | 7 |
| 3 | `46 80`, `1E 8A` | 70 to 2590 | 5 |
| 3 | `51 7E`, `04 8F` | -431 to 3844 | 1 |

Type 1 is the factory sensor: the same 7.221 mbar per count and -59 mbar at zero as on the factory axis. A boosted ROM keeps the factory columns and adds boost columns after them, and this gives a check that does not depend on the stored range being read correctly: in `jeffsciv23.bin` the first ten columns, read through the stored range, come out within 0.2 kPa of the factory axis of the 203 ROM read through the factory MAP code. The tests hold this.

### Rev Tools

Five of the 28 also carry a change to the high-cam rpm lookup: `0x09F3` holds `03 04 79`, a jump to `0x7904`. The routine there divides 43520 (`MOV er0, #0AA00h`) by the crank period instead of 53248, loads the high-cam rpm axis (operand at `0x7922`) and returns into the factory code. The axis then stands for b x 1875000 / 43520 = b x 43.08 rpm, 11029 rpm at the last breakpoint. These are `crome-p30-boost-rev`. No image in the corpus has Rev Tools without Boost Tools.

### The first Boost Tools

Two tunes (`11psi-FooK.bin`, `Stevefinal.bin`) carry an earlier form: `0x0A13` and `0x0A28` take the raw reading as above, but `0x0829` keeps the factory code with one limit changed (`C6 FF` at `0x082E`), the layout is the fixed 16 columns at `0x6E72`, and the two range words are stored without the offset of 32768: `19 00`, `E4 06`, 25 to 1764 mbar. These are `crome-p30-boost1`.

### Boost Tools+ on the P72 type

Four tunes, with 16, 20 and 24 columns. The same changes at the P72 addresses: `F5 BB` and the cut-off at `0x0841`, `F5 BB` at `0x0A9F` and `0x0AB4`, one MAP axis for fuel and ignition, the sensor block before it. The high-cam maps keep 15 rows. The check of the ROM sum is jumped over (`03 2A 2A` at `0x2A1D`), yet all four sum to zero with `0x7FFF` as the correcting byte: the enlarged maps end one byte before it.

### The Gold base

Two images: the template CROME installs (v0.94) and `crome gold.bin` from the archive, whose code is 97.5 % the same.

- It is P30-type firmware with features built in: 27,574 of its 32,768 bytes are those of the 203 ROM.
- The main maps always have 24 columns: fuel at `0x6E9A` and `0x7092` (with the multiplier row), ignition at `0x728A` and `0x746A`, the MAP axis at `0x6E5A`, the rpm axes at `0x6E72` and `0x6E86`. The fuel set-up is at `0x11F9` instead of `0x1234`.
- A tune uses as many columns as it needs; the unused ones repeat the last breakpoint, and the list still ends in zero. The base uses ten.
- The high-cam row search takes the byte at RAM `2FEh`, which a routine at `0x7946` computes as 43520 / period: b x 43.08 rpm, as with Rev Tools.
- The column search does not take the factory MAP code: a routine reached from `0x0829` looks the reading up in a table at `0x6374`. How that table follows the sensor chosen in the editor is not worked out, so the pack shows MAP columns as stored codes (issue 41).
- The option byte at `0x6011` is `FF`, so a wrong sum is ignored. Both images still sum to zero, and they differ in `0x003A` and `0x003B`: the correction is there.
- The serial receive routine at `0x01FD` is the same in both images for its first 12 bytes and differs after that. Which datalog protocol each speaks is not worked out, so the pack names none.

## Uberdata

The base ROMs Uberdata ships are factory images under other names: `p30.bin` is the 203 ROM, `p28.bin` the 304, `StockGSR.bin` the 273, `StockLS.bin` and `p75-import-only.bin` the 274, `p06.bin` the 301. `P28-ERMbaseline.bin` is P72-type code with the factory layout and a sum that is not zero.

Four more (`Blake1`, `Blake2`, `ross`, `test`) are boost bases on the P72 type:

- 15 columns at fixed addresses: low-cam fuel `0x6770` (20 x 15), high-cam fuel `0x71F0` (15 x 15), low-cam ignition `0x6B90`, high-cam ignition `0x7060`. The axes stay at the factory addresses; the MAP axis at `0x7C4A` now has 15 bytes and serves fuel and ignition.
- The MAP code routine at `0x4DCB` has its doubling removed: the `ROLB A` (`33`) is a return (`01`). The code is the reading above 24, at half the factory resolution and twice the range: kPa = b x 0.7221 + 11.43, up to 196 kPa. The factory upper limit on the reading is removed as well (`00 00` at `0x0848`).
- The check of the ROM sum is jumped over, and these images do not sum to zero.

The scale is confirmed by the data: the first nine MAP breakpoints of `Blake1.bin` are half the factory ones (`0C 18 28 40 58 68 70 78` against `18 30 50 80 B0 D0 E0 F0`), and read with the halved scale they are the factory pressures to within 0.01 kPa. These are `uberdata-p72`.

What else Uberdata changes in a ROM (its launch control and other options) has not been looked at.

## eCtune

One image: the base eCtune 1.3 starts every calibration from, which the program reports as eCt-273 v0.0.65.

- Maps of 20 x 24 in two sets. Each set-up loads the address of the first set and skips or takes a second load depending on a flag, so both sets are visible in the code: fuel `0x6EAD` and `0x70A5` (low and high cam) or `0x765F` and `0x7857`; ignition `0x729D` and `0x747D` or `0x7A4F` and `0x7C2F`. Each set has its own axes: MAP `0x6E59` or `0x6E17`, low-cam rpm `0x6E71` or `0x6E2F`, high-cam rpm `0x6E85` or `0x6E43`.
- As in the Gold base, unused columns repeat the last breakpoint; the base uses 15.
- The high-cam axis is 43520 / period (`0x078A`): b x 43.08 rpm.
- The last bytes of the image are a trailer; `0x7FF7` to `0x7FF9` hold `00 00 41`, and 41h is 65. The pack compares them, so another version of the firmware is not taken for this one.
- The image sums to 6Bh: this firmware does not check its sum.
- The MAP axis is shown as stored codes, as for the Gold base (issue 41). The bytes at `0x60FC` to `0x6100` are `02`, `BA 7F`, `FE 86`: a type byte and two words that, read as Boost Tools writes them, are -70 and 1790 mbar. Whether the firmware and the program use them that way in every sensor mode is not verified.

The seven HTS tunes of the archive hold maps at the same addresses, with the code four bytes later (set-ups at `0x0B23` and `0x117D`) and a trailer ending in `HONDATUNESUITE`. They are a different build, and no pack claims them.

## How to repeat this

```
python tools/research/layout.py           # the layouts, read from each image's code
python tools/research/expected_packs.py --near
rasm66k dasm "New turbo civic map.bin" rev.asm   # then look at 09F3 and 7904
python tools/research/dump.py jeffsciv23.bin 6e6d 1 21   # sensor block and MAP axis
```
