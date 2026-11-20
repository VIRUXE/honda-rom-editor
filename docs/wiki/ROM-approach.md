# ROM approach

Older editors treat a ROM as one mutable byte array, find tables through addresses hardcoded per base ROM, and add features by writing pre-assembled bytes at fixed offsets. That breaks as soon as two additions want the same space, and nothing checks that the bytes being replaced are the ones expected.

This editor works two ways.

## Source projects (the main path)

The ROM is assembled from source.

- `project.toml` names the entry file, the output, the ROM size and the available options.
- Features are build-time toggles (`config.inc`); turning one off assembles different code rather than patching bytes.
- Parameters are bound to assembler **symbols**, not addresses (`definitions.toml`), so tables can move when code changes.
- Tuning edits are kept apart from the source as symbol-relative edits (`overlay.toml`) and re-applied on every build.
- The checksum is fixed after each build.

## Definition packs (for existing `.bin` files)

A data file ([[Definition packs]] documents the format, [[Supported ROMs]] lists the ones that ship) that:

- identifies a ROM by fingerprint (size, hash, or masked byte ranges);
- describes its parameters by absolute address, with units and conversions;
- states the checksum location and the range it covers;
- names the datalog protocol the ROM speaks;
- lists **patch recipes**: each step is an address, the bytes expected there, and the new bytes. A recipe whose expected bytes do not match is refused, and applying then reverting a recipe is byte-identical.

Nothing is patched into an existing image beyond verified recipes.
