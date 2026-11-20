# Datalog protocol

The protocol is a **per-ROM choice**. Dashes, Bluetooth loggers, display boxes and other tuning software each expect a specific protocol, so a source project selects which datalog routine is assembled into the ROM:

| Choice | Notes |
|---|---|
| `stock` | The factory serial routine is left alone |
| `hre-stream` | Default for new projects; described below |
| `ectune` | eCtune-compatible: one request returns a whole checksummed frame |
| `crome-qd2` | CROME QD2-compatible: one byte per request |
| `crome-qd3` | CROME QD3-compatible: framed, with header and checksum |

For an existing `.bin`, the definition pack says which protocol the ROM already speaks. On the PC side there is one driver per protocol; the logger defaults to the ROM's protocol, can detect it by handshake, and can be overridden.

## What the existing protocols get right and wrong

- A frame-per-request protocol with a checksum, where 16-bit values are read in one step inside the ECU, is the best of the existing designs.
- One byte per request is slow (a USB round trip per byte), has no checksum, and lets the two halves of a 16-bit value come from different moments.
- Only a framed protocol with a header can resynchronise after a dropped byte.
- None of them timestamps samples in the ECU, so sample timing is only as good as USB latency.

## HRE stream v1

| Field | Size | Notes |
|---|---|---|
| Sync | 2 | `A5 5A`; the receiver resynchronises by scanning for it |
| Length | 1 | Payload bytes |
| Sequence | 1 | Wraps; gaps reveal dropped frames |
| Tick | 2 | Free-running ECU timer: the sample's time base |
| Payload | Length | Channels, in the order of a table stored in the ROM |
| Check | 2 | Fletcher-16 |

- The ECU copies all channels into a buffer in one step, then streams the buffer, so every value in a frame belongs to the same moment.
- Commands: `ID` (protocol version and channel-table hash, so mismatched definitions are refused), `STREAM n` (continuous), `ONCE` (one frame), `STOP`.
- At 38400 baud a 32-byte frame takes 8.3 ms; the default rate is 50 Hz.

## Tuning math on the PC

- Each sample is spread over the four surrounding cells by bilinear weight.
- Averages are kept in the lambda domain, with hit count and variance per cell.
- Each lambda reading is paired with the operating point from one exhaust transport delay earlier, using the ECU tick as the clock.
- Samples are filtered on throttle rate of change, coolant temperature, fuel cut and closed-loop state.
