# Corpus

The ROM images the packs and their tests are checked against. They are kept outside the repository, in `hondaecu/test-roms` of the workspace; `tests/fixtures/corpus/expected-packs.tsv` lists every 32 KiB file there with the pack it is expected to be.

## Sources

| Folder | What | Files |
|---|---|---|
| `hondabase/` | [github.com/hondabase/files-archive](https://github.com/hondabase/files-archive) at commit `85892fc1f508a82693073003d2e86f515f55a60d` (2026-09-13). The archive holds 515 files, 278 MB, most of them manuals, programs and photos | see below |
| `editors/crome/` | The five `.NEW` templates CROME 1.7.6 installs: P28 (304), P30 (203), P30G (209), P72 (273) and the Gold base v0.94 | 5 |
| `editors/ectune/` | The base ROM eCtune 1.3 starts every new calibration from, saved as a `.bin` | 1 |
| top level | `P28-ERMbaseline.bin`, a baseline for Uberdata from icelord.net, and one HTS tune kept as an emulator fixture | 2 |

From hondabase, in two passes:

1. Every file of 32,768 or 32,769 bytes: 185 files, 183 distinct images. Listed in `hondabase/MANIFEST.tsv` with size, 8-bit sum and SHA-256.
2. The rest of the ROM material (2026-11-20), listed in `hondabase/MANIFEST-more.tsv`: 30 images of other sizes, and the ROM files inside three kinds of archive (below). Files were fetched with `tools/research/fetch_hondabase.py`, which checks each against its git blob SHA-1 and resolves Git LFS pointers against their SHA-256.

| Archive | Taken from it |
|---|---|
| `ecu-roms/DPFI-ROM-DevKit.zip` | All 41 files: 13 P04 and 4 PM5 ROMs, annotated P04 listings, data-area notes, an editor and a ROM debugger |
| `uberdata/*.7z` (five releases) | The 31 base ROMs they ship |
| the archive's TunerPro bundle of definitions and ROMs (`xdf&bin (2).zip`) | It covers many makes; only its Acura part was taken: 18 Legend and NSX ROMs and one definition file |

The HTS, CROME, TurboEdit and OBD0Edit archives hold single installers and no separate ROM files; they were not kept.

## What the 259 images of 32 KiB are

| Pack | Images |
|---|---|
| `obd1-p30` | 38 |
| `obd1-p28` | 34 |
| `obd1-p72` | 20 |
| `crome-p30-boost` | 23 |
| `crome-p30-boost-rev` | 5 |
| `crome-p30-boost1` | 2 |
| `crome-p72-boost` | 4 |
| `uberdata-p72` | 7 |
| `crome-gold` | 2 |
| `ectune-273` | 1 |
| none | 123 |

The 123 that no pack describes, by what the archive calls them:

| Images | What |
|---|---|
| 21 | TurboEdit tunes (OBD0) |
| 18 | The dual-point kit: P04 and PM5 ROMs (OBD0) and two images of debugger code |
| 17 | Acura Legend and NSX |
| 24 | ROMs named after OBD0 and other early parts: PK2 (6), PR4 (4), PR3 (3), PW0 (2), PT3 (2), PT6 (2), PM6, PM7, PR5, PH3, PJ0 |
| 22 | ROMs named after other OBD1 parts: P13 (5), P14 (4), P07 (4), P0C (2), P12 (2), P39 (2), P08, P0A, P11 |
| 7 | HTS tunes |
| 6 | Others named after a tune or an engine (`ng6001`, `ng60_04`, `d16a8_stock`, `innova_P51A`, `STOCK-PM6`, `STOCK-PR3`) |
| 4 | P74, two BRE tunes, one Hondata S200 image |
| 3 | Haltech base maps, which are not ECU code |
| 1 | One of the HTS tunes a second time, kept as an emulator fixture |

The names are the archive's. Several are wrong about the ROM inside: `pr3-j01-205.bin`, `pr4-USDM-232.bin` and `int233stock.bin` are P30-type OBD1 ROMs, and `p75-import-only.bin` is the 274 ROM. The packs go by the code, not the name.

## How the images were sorted

`tools/research/survey.py` compares the first 24 KiB of every image with every other. Images of one firmware type share 98 % or more of those bytes; images of different types share a few percent. The clusters this gives are the firmware types of [OBD1 firmware types](OBD1-firmware-types.md). The word at `0x0023` differs between them (`03CB` P30 type, `03D6` P28 type, `0367` P72 type), but it is a consequence, not a definition: it is part of an instruction.

`tools/research/layout.py` then reads, from each image's own code, how many rows and columns its main maps have and where they and their axes are. It finds the instructions by their bytes with the operands left open, so it works on any image of the three types wherever a tuning program has put the maps. Its groups were the starting point for every pack.

## What the odd files taught

| File | What it is | Consequence |
|---|---|---|
| `ecu-roms/P28-230.bin` and nine more | 32,769 bytes: one stray byte after the ROM | Fingerprints state no size |
| `ecu-roms/368-d14a2-p1j-stock.bin` | 32,860 bytes: a P28-type ROM followed by a line of text from whoever read it out | A longer file is judged by the ROM it starts with |
| `xenocron/crome/d/final-notes.bin` | 32,923 bytes: a tune followed by the notes CROME lets its author keep with the ROM | The same; an editor must not lose what follows the ROM |
| `ecu-roms/OKI_P30.BIN` | 65,536 bytes: the 209 ROM twice, as it is written to a 64 KiB chip | Identified by its first half |
| `hts/bins/B20 Unknown Joey.BIN` | 65,536 bytes: `FF` in the lower half, a P30-type ROM in the upper half | Not recognised yet |
| `ecu-roms/*MCU*.bin` (three) | 32,000 bytes: dumps that stop short | An image shorter than the ROM never matches a pack: correcting its checksum would reach past its end |
| `ecu-roms/Civ*.bin`, `Int*.bin` and two `*66507*` | 49,152 bytes: OBD2 ROMs | Out of scope |
| `xenocron/hondata/s300/*.skl`, `kpro/EP3.kal` | Hondata calibration files | See the Hondata notes when they exist |

Five images that packs identify do not sum to zero: 327, 332 and 215 as archived, `P28-ERMbaseline.bin`, and the tune `cheoh.bin`. Every tune with enlarged maps sums to zero.
