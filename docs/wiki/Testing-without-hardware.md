# Testing without hardware

Everything is testable with no ECU, engine simulator or emulator attached.

| Area | How it is tested |
|---|---|
| ROM model and file formats | Unit tests; round trips of real project files |
| Build | The P28-230 source project must build to an image with the SHA-256 recorded in its manifest |
| Definition packs | Fingerprints and table reads checked against a corpus of ROM images; every patch recipe applied then reverted byte-identically |
| Application window | Every page and every sentence is built by code that needs no window and is unit-tested, including the rule that plain sections hold no addresses and that every problem offers a way on; the window and its pages are created in tests without being shown, and checked for names, roles, tab order, overlap and growth at twice the text size; the palette is checked for contrast |
| Datalog drivers | Scripted byte transcripts, including bad checksums and resynchronisation |
| End-to-end logging | A virtual ECU runs the real ROM and serves its serial port over TCP; RPM, load and throttle are set through a control channel |
| Tuning math | Synthetic logs with a known transport delay and lambda error that the pipeline must recover |
| ROM emulators | Fake device streams per profile: identify, block write and read back, live write, verify |

Tests that need files outside the repository (ROM source projects, ROM images) look for them in the folder next to the repository, or in `HRE_WORKSPACE`, and are skipped when the files are absent.

The last milestone repeats the emulator checks on real hardware.
