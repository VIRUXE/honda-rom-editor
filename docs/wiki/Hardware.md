# Hardware

ROM emulators sit behind one device abstraction (identify, read, write, live write, verify; optional banks, trace and datalog port) with a profile per device. Devices are recognised by their reply to the identify command and by USB port description, never by COM number.

| Device | Link | Notes |
|---|---|---|
| Moates Ostrich 1.0 / 2.0 | Serial, 921600 baud (115200 fallback), 8-bit sum checksum | Bulk read/write, live byte write, banks, speed switch |
| One ROM with its Ostrich-compatible plugin | Two USB serial ports: control, and a datalog passthrough fixed at 38400 baud | No speed switch, no address trace, bank 0 only. Edits go live at once and are saved to flash 3 s after the last one; a save can stall the datalog stream for up to about a second |
| CobraRTP | Serial, 921600 baud; datalogging on a separate port | Driven with the standard Ostrich protocol. To be confirmed on a real unit |

Address tracing (the emulator reporting which ROM addresses the ECU reads) is a stretch goal, to be confirmed on a real Ostrich.

Public protocol references: the Moates hardware protocol sheet, [mstrand/ostrich2](https://github.com/mstrand/ostrich2), [keith-daigle/moates](https://github.com/keith-daigle/moates), [piersfinlayson/one-rom](https://github.com/piersfinlayson/one-rom) and [one-rom-ostrich](https://github.com/piersfinlayson/one-rom-ostrich).
