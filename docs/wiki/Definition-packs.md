# Definition packs

A definition pack describes an existing ROM image so it can be opened without source: how to recognise it, where its parameters are, where its checksum is, which datalog protocol it speaks and which patches can be applied to it. A pack is one TOML file named `<something>.pack.toml`; packs are plain data and can be shared.

See [[ROM approach]] for where packs fit next to source projects, and [[Supported ROMs]] for the packs that ship with the editor.

## Example

```toml
id = "example-p04"
name = "Example dual-point ROM"
platform = "obd0-66k"
rom_size = 0x8000

[[fingerprint]]
size = 0x8000
sha256 = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef"

[[fingerprint]]
bytes = [
    { address = 0x23, value = [0x96, 0x10], mask = [0xFF, 0xF0] },
    { address = 0x40, value = [0xA5] },
]

[checksum]
kind = "sum8"
start = 0x0000
end = 0x4000
location = 0x3FFF

[datalog]
protocol = "stock"
baud = 9600

[[param]]
category = "Fuel"
name = "Fuel Map"
address = 0x100
kind = "table"
rows = 6
cols = 12
row_axis = { label = "RPM", address = 0x0F0, encoding = "delta-bytes" }
col_axis = { label = "MAP", address = 0x0E4, encoding = "delta-bytes" }
multiplier = { address = 0x148, kind = "exponent" }

[[param]]
category = "Limits"
name = "Rev Limiter"
address = 0x300
kind = "word"
cell = "u16le"
min = 3000
max = 11000
unit = "rpm"

[[patch]]
id = "fast-serial"
name = "Fast serial routine"
doc = "Answers a memory address with the byte stored there."
steps = [
    { address = 0x0500, old = [0x00, 0x00, 0x00], new = [0x03, 0x00, 0x7F] },
    { address = 0x7F00, old = [0xFF, 0xFF, 0xFF, 0xFF], new = [0x21, 0x10, 0x03, 0xFF] },
]
```

## Top-level keys

| Key | Required | Default | Meaning |
|---|---|---|---|
| `id` | yes | | Short stable identifier, unique among packs |
| `name` | yes | | Name shown to the user |
| `doc` | no | `""` | What a newcomer should know about this kind of ROM, in a sentence or two; shown under the name |
| `platform` | no | `obd1-66207` | `obd1-66207`, `obd0-66k` or `obd0-8051` |
| `rom_size` | no | `0x8000` | Size of the ROM the addresses refer to |

## Fingerprints

A pack has one or more `[[fingerprint]]` entries. The pack matches an image when **any** fingerprint matches; a fingerprint matches when **every** condition it gives holds.

| Key | Meaning |
|---|---|
| `size` | The image is exactly this many bytes |
| `sha256` | SHA-256 of the whole image (64 hex digits) |
| `bytes` | A list of `{ address, value, mask }`: the bytes at `address` equal `value`. With a `mask` (same length as `value`) only the bits set in it are compared |

A fingerprint needs a `sha256` or `bytes`; a size alone would claim every image of that length.

An image shorter than `rom_size` never matches a pack, whatever its fingerprints compare: a dump that stops short is not that ROM. A longer one can match, unless a fingerprint states a `size`.

When several packs match, a hash match ranks first, then the fingerprint that compared the most bits. Packs of equal strength are all offered and the user chooses.

## Checksum

`[checksum]` is optional. Without it the whole ROM sums to zero and the correction byte is the last byte.

| Key | Default | Meaning |
|---|---|---|
| `kind` | `sum8` | `sum8`: the covered bytes sum to zero modulo 256. `none`: the ROM does not check itself, nothing is verified or corrected |
| `start` | `0` | First byte of the summed range |
| `end` | `rom_size` | One past the last byte of the summed range |
| `location` | `end - 1` | Address of the correction byte; inside the range |

## Datalog

`[datalog]` is optional. `protocol` names the protocol the ROM's firmware already speaks (see [[Datalog protocol]]); `baud` is given when the protocol does not fix it.

## Parameters

Each `[[param]]` is a table or a 16-bit word at an `address`.

| Key | Applies to | Default | Meaning |
|---|---|---|---|
| `category`, `name` | all | required | Where the parameter appears in the list |
| `doc` | all | `""` | What the parameter is, in plain words for someone who has not met it: shown at the top of its page and when pointing at it. Every parameter of a shipped pack has one |
| `address` | all | required | Address of the data: a number, or where the ROM keeps it (see below) |
| `kind` | all | required | `table` or `word` |
| `rows`, `cols` | table | required | At least 2 x 2; cells are stored row by row. Each is a number, or where the ROM keeps it |
| `cell` | all | `u8` (table), `u16be` (word) | `u8`, `u16be` or `u16le`. A word is `u16be` or `u16le` |
| `min`, `max` | word | `0`, `65535` | Allowed range; equal values make the word read-only |
| `unit`, `scale`, `bias` | all | `""`, `1`, `0` | Displayed value = raw x scale + bias |
| `vtec` | table | | Which cam state the ECU reads this table in |
| `row_axis`, `col_axis` | table | required | See below |
| `multiplier` | table | | See below |

### Values the ROM keeps

A tuning program can give a ROM larger maps and put them somewhere else. The firmware then holds the new column count and the new addresses in its own instructions, and a pack can read them from there instead of stating them. Wherever a pack gives an address, `rows` or `cols`, it may give `{ at = ... }` instead:

| Key | Default | Meaning |
|---|---|---|
| `at` | required | The address the ROM keeps the value at. May itself be `{ at = ... }` |
| `cell` | `u16le` for an address, `u8` for `rows` and `cols` | How the value is stored: `u8`, `u16le` or `u16be` |
| `bias` | `0` | Added to what is read |

```toml
address = { at = 0x1257 }       # the address the firmware loads at 0x1256
cols = { at = 0x1235 }          # the column count it sets at 0x1234
```

A pack that reads a value this way leaves the bytes it reads out of its fingerprint with a `mask`, and compares the instruction around them. One pack then describes every size and position of the maps, and still refuses a ROM whose instructions are not the ones it knows.

Such a parameter is resolved for each image. It is left out, with the reason, when the address lies outside the image or what is read cannot be a table.

### Axes

An axis is an inline table with a `label` and one of:

| Form | `encoding` | Storage |
|---|---|---|
| `{ label, address }` | `words` (default) | One big-endian 16-bit breakpoint each |
| `{ label, address, encoding = "delta-bytes" }` | `delta-bytes` | One byte each: the first is the breakpoint, every later byte is the increase over the breakpoint before it |
| `{ label, address, encoding = "bytes" }` | `bytes` | One byte each. A zero after the first breakpoint stands for 256 and ends the list |
| `{ label, address, encoding = "log-bytes" }` | `log-bytes` | As `bytes`, on a scale that doubles every 64 steps and is linear in between: byte `b` stands for 2^(`b` div 64) x (1 + (`b` mod 64) / 64) |
| `{ label, values = [...] }` | `values` (may be omitted) | Not in the ROM: a fixed list, one value per row or column, increasing; cannot be edited |

A stored axis may add `scale` and `bias`: displayed breakpoint = stored value x `scale` + `bias` (1 and 0 when left out). With `log-bytes` the scale is what byte 0 stands for: `scale = 500` puts 500, 1000, 2000 and 4000 at bytes 0, 64, 128 and 192. A fixed list is already in display units and takes neither.

```toml
row_axis = { label = "RPM", address = 0x700A, encoding = "log-bytes", scale = 500 }
col_axis = { label = "MAP kPa", address = 0x7000, encoding = "bytes", scale = 0.36105, bias = 11.43 }
```

An axis stored as `bytes` at an address may give `ends` instead of knowing its scale: where the ROM keeps, in two cells one after the other, what stored 0 and stored 255 stand for. `ends` is `{ at, cell, bias }` as above, with `u16le` cells by default. The stored value is put on the line between the two ends, and `scale` and `bias` then apply. Tuning programs write the range of the MAP sensor into the ROM this way, so the same pack shows the right pressures whichever sensor a tune was made for:

```toml
# The sensor range, in mbar, is in the four bytes before the axis, each word 32768 above its value.
col_axis = { label = "MAP kPa", address = { at = 0x0A2C }, encoding = "bytes", ends = { at = { at = 0x0A2C, bias = -4 }, bias = -32768 }, scale = 0.1 }
```

Breakpoints always increase. Editing one breakpoint of a delta-encoded axis leaves the others where they are: the step that follows is adjusted to match, and the new value is limited so that both steps still fit one byte. A single-byte breakpoint stays below 256, which only the last one can hold.

Where an operating point falls between two breakpoints is measured in stored units, because that is how the ECU interpolates. On a `log-bytes` axis this differs from interpolating the displayed values whenever two neighbouring breakpoints lie in different octaves.

### Column multipliers

`multiplier = { address, kind }` names one byte per column. Without an `address` the multipliers are the row after the last row of cells, which is where the table lookup of the OBD1 ECUs reads them. Each cell of a column is scaled by its byte before `scale` and `bias` apply:

| `kind` | Factor |
|---|---|
| `linear` (default) | the byte |
| `exponent` | two to the power of the byte |

## Patches

A pack may list `[[patch]]` entries: named changes to the ROM that can be applied and taken out again. A patch is a recipe of steps, and every step states the bytes it expects to find as well as the bytes it writes, so a patch is never written over something it was not made for.

| Key | Required | Meaning |
|---|---|---|
| `id` | yes | Short stable identifier, unique within the pack |
| `name` | yes | Name shown to the user |
| `doc` | no | What the patch does |
| `steps` | yes | One or more steps, as an inline list: `steps = [{ address = ..., old = [...], new = [...] }]` |

Each step has exactly these keys:

| Key | Meaning |
|---|---|
| `address` | Absolute address of the first byte |
| `old` | The bytes expected there before the patch; at least one |
| `new` | The bytes that replace them; as many as `old` |

A long step can run over several lines, because its byte lists can:

```toml
steps = [
    { address = 0x7F00, old = [
        0xFF, 0xFF, 0xFF, 0xFF,
    ], new = [
        0x21, 0x10, 0x03, 0xFF,
    ] },
]
```

Only the inline list is read; `[[patch.steps]]` tables are refused. So is any key in a step other than the three above, because ignoring one could change what the step writes.

### Probing

Before anything is written the image is probed. A patch is in exactly one of three states:

| State | The image holds | What happens |
|---|---|---|
| Not applied | every step's `old` bytes | It can be applied |
| Applied | every step's `new` bytes | It can be reverted |
| Conflict | anything else: a byte that is neither, only some of the steps in place, or an image too short for a step | It is refused, with the address of the first unexpected byte; the image is left as it was |

- Applying a patch and then reverting it gives back the image byte for byte.
- A byte that is the same in `old` and `new` is still compared. A step can repeat bytes unchanged, or change none at all, to pin down code the patch relies on.
- Patching never writes the checksum byte. Correcting the checksum is a separate step, the same one any other edit needs. For an image whose checksum was right, applying a patch, correcting, reverting and correcting again gives back every byte.

### Patches that share bytes

Two patches of a pack may cover the same bytes in two ways only:

- **As alternatives.** Where they overlap, the `new` bytes of each differ from both the `old` and the `new` bytes of the other. While one is applied the other is then always a conflict: it can be neither applied on top nor reverted from underneath until the first is reverted.
- **As shared pins.** Neither changes the shared bytes.

Anything else is rejected when the pack is loaded: a patch that builds on another, one that pins or rewrites bytes another changes, one that repeats a step of another. In each of these, switching one patch could leave the other half in place. A patch that needs another is written as one patch.

### What a pack may not contain

The pack is rejected when a patch:

- has steps that overlap, or a step that reaches past `rom_size`;
- has a step covering the checksum's correction byte, which changes with every edit and so never holds an expected value (this does not apply with `kind = "none"`);
- changes a bit that one of the pack's `bytes` fingerprints compares, because the pack would no longer recognise the ROM once it is patched (pinning such a byte is fine, and so is changing bits a `mask` leaves out);
- changes nothing, because `new` equals `old` in every step;
- repeats the `id` of another patch in the pack, or shares bytes with one in a way the section above does not allow.

Keep steps clear of the bytes a parameter covers as well: once that table is edited, the patch no longer finds the bytes it expects and becomes a conflict.

## Validation

A file that cannot be understood (bad syntax, a missing or contradictory key, an unknown name) is rejected with the reason; a reason inside a patch names the patch and the step. So is a file that defines something twice: a key or a table given twice, or `[[...]]` tables written after an inline list of the same name. The reason gives the line. A parameter that cannot exist in the ROM, such as a span reaching past `rom_size`, is reported and left out; the rest of the pack still loads. So is a parameter that covers the checksum byte or a byte a `bytes` fingerprint compares: editing it would break the checksum, or make the pack stop recognising the ROM.

## Source projects

`definitions.toml` in a source project accepts the same optional keys (`platform`, `cell`, axis `encoding`, `scale` and `bias`, `multiplier`), with a `symbol` wherever a pack has an `address`. Values read from the ROM belong to packs: a source project knows where its tables are. Files written before these keys existed stay valid.
