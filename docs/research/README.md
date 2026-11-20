# Research notes

What was found out while building the editor, written down so that it can be checked and repeated. The wiki says what the editor does; these pages say how we know.

| Page | Subject |
|---|---|
| [Corpus](Corpus.md) | The ROM images everything is checked against: where they come from, what they are, what the odd files taught |
| [OBD1 firmware types](OBD1-firmware-types.md) | The three kinds of factory firmware, how the main maps are looked up, how the axes are encoded, where the checksum is |
| [ROMs changed by tuning programs](Tuning-program-ROMs.md) | What CROME, Uberdata and eCtune leave in a ROM, and how each is recognised |
| [The black theme](Dark-theme.md) | What the dark colour mode of WinForms gives on .NET 11, what it does not, and what the editor does itself |
| [What .NET 11, WinForms and C# 15 offer](Platform-features.md) | Which platform features were checked on the installed SDK, and which the editor uses or leaves alone, and why |

## Rules for these pages

- A page states what was observed, on which images, and how to see it again. A conclusion that was not checked on an image says so.
- Addresses are ROM addresses in hex. "The corpus" is the set of images described in [Corpus](Corpus.md); it is not part of the repository.
- Findings come from the ROM images themselves, from running the programs that made them, and from published documentation.

## Tools

The scripts in [`tools/research`](../../tools/research) read the corpus (see `workspace.py` for where it is looked for) and need only Python 3.11 or later.

| Script | Prints |
|---|---|
| `survey.py` | Images that are the same ROM, and the two images each one is closest to in its first 24 KiB |
| `layout.py` | Images grouped by what their code says about the main maps: rows x columns, map and axis addresses. With `--axes`, the distinct axis bytes and fuel column multipliers per layout |
| `loads.py <listing> <lo> <hi>` | The instructions of a disassembly listing that load a table address in a range |
| `dump.py <image> <address> <rows> <cols>` | Byte grids |
| `expected_packs.py` | Rewrites the list of which pack each corpus image is (`tests/fixtures/corpus/expected-packs.tsv`); `--near` also lists near misses |
| `fetch_hondabase.py`, `list_archives.py` | Fetch files of the hondabase archive with their checksums verified; list what archives hold |
| `wiki_hunks.py` | The text replacements that bring a wiki page up to date |

[`tools/HRE.Shots`](../../tools/HRE.Shots) saves a picture of the editor's main window without bringing it to the front: `dotnet run --project tools/HRE.Shots -- [--technical] [--zoom=2] out.png [rom.bin [parameter name]]`. `make_icon.py` draws the application icon (needs Pillow).

Listings are made with the `rasm66k` disassembler: `rasm66k dasm <rom> <listing>`.
