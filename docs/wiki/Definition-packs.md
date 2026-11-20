# Definition packs

A definition pack describes an existing ROM image so it can be opened without source: how to recognise it, where its parameters are, where its checksum is and which datalog protocol it speaks. A pack is one TOML file named `<something>.pack.toml`; packs are plain data and can be shared.

See [[ROM approach]] for where packs fit next to source projects.

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
col_axis = { label = "MAP", address = 0x0F6, encoding = "delta-bytes" }
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
```

## Top-level keys

| Key | Required | Default | Meaning |
|---|---|---|---|
| `id` | yes | | Short stable identifier, unique among packs |
| `name` | yes | | Name shown to the user |
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

Each `[[param]]` is a table or a 16-bit word at an absolute `address`.

| Key | Applies to | Default | Meaning |
|---|---|---|---|
| `category`, `name` | all | required | Where the parameter appears in the tree |
| `address` | all | required | Absolute address of the data |
| `kind` | all | required | `table` or `word` |
| `rows`, `cols` | table | required | At least 2 x 2; cells are stored row by row |
| `cell` | all | `u8` (table), `u16be` (word) | `u8`, `u16be` or `u16le`. A word is `u16be` or `u16le` |
| `min`, `max` | word | `0`, `65535` | Allowed range; equal values make the word read-only |
| `unit`, `scale`, `bias` | all | `""`, `1`, `0` | Displayed value = raw x scale + bias |
| `vtec` | table | | Which cam state the ECU reads this table in |
| `row_axis`, `col_axis` | table | required | See below |
| `multiplier` | table | | See below |

### Axes

An axis is an inline table with a `label` and one of:

| Form | `encoding` | Storage |
|---|---|---|
| `{ label, address }` | `words` (default) | One big-endian 16-bit breakpoint each |
| `{ label, address, encoding = "delta-bytes" }` | `delta-bytes` | One byte each: the first is the breakpoint, every later byte is the increase over the breakpoint before it |
| `{ label, values = [...] }` | `values` (may be omitted) | Not in the ROM: a fixed list, one value per row or column, increasing; cannot be edited |

Breakpoints always increase. Editing one breakpoint of a delta-encoded axis leaves the others where they are: the step that follows is adjusted to match, and the new value is limited so that both steps still fit one byte.

### Column multipliers

`multiplier = { address, kind }` names one byte per column. Each cell of a column is scaled by its byte before `scale` and `bias` apply:

| `kind` | Factor |
|---|---|
| `linear` (default) | the byte |
| `exponent` | two to the power of the byte |

## Validation

A file that cannot be understood (bad syntax, a missing or contradictory key, an unknown name) is rejected with the reason. A parameter that cannot exist in the ROM, such as a span reaching past `rom_size`, is reported and left out; the rest of the pack still loads.

## Source projects

`definitions.toml` in a source project accepts the same optional keys (`platform`, `cell`, axis `encoding`, `multiplier`), with a `symbol` wherever a pack has an `address`. Files written before these keys existed stay valid.
