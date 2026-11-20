# OBD0

"OBD0" covers two processor families:

- **OKI 66k family** — P04 and the other dual-point injection ECUs, PR3, PW0. The existing assembler handles their code.
- **8051 family** — PM6, PM7, PR4 and related. Definition packs only, until an 8051 toolchain exists.

| Tier | ROMs | Support |
|---|---|---|
| O1 | P04 and the dual-point injection ECUs | Definition pack, then a source project and a board model in the virtual ECU |
| O2 | PR3, PW0 | Definition packs (17x15 fuel and ignition tables) |
| O3 | PM6, PM7, PR4 | Definition packs only |

## What P04 needs from the model

- Axes stored as cumulative deltas in front of the cells.
- A multiplier exponent per fuel column.
- A checksum over the first 16 KiB only; the rest of the 32 KiB image is blank.
- Tables: three 6x12 fuel, three 5x7 small fuel, one 8x15 ignition, three 6x7 correction.
- The factory serial routine answers an address with the byte stored there, which is enough for basic logging.

Related work: [DPEdit](https://codeberg.org/K1ngPrawn/dpedit), an editor for these ECUs.
