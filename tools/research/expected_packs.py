"""Rewrites tests/fixtures/corpus/expected-packs.tsv: the pack each 32 KiB corpus image is.

Reads the fingerprints of packs/*.pack.toml and matches them with this script's own code, so
the list is a second opinion on the editor's matching. The tests fail when a 32 KiB file of
the corpus is missing from the list. Review the difference before committing it.

usage: expected_packs.py            rewrite the list
       expected_packs.py --near     also print images that miss a pack by one to four byte ranges
"""
import collections
import glob
import os
import sys
import tomllib

from workspace import REPO, images

OUT = os.path.join(REPO, "tests", "fixtures", "corpus", "expected-packs.tsv")


def load_packs():
    packs = []
    for path in sorted(glob.glob(os.path.join(REPO, "packs", "*.pack.toml"))):
        with open(path, "rb") as f:
            pack = tomllib.load(f)
        packs.append((pack["id"], pack.get("rom_size", 0x8000), pack["fingerprint"]))
    return packs


def misses(data, fingerprint):
    """The byte ranges of a fingerprint the image does not hold."""
    if "size" in fingerprint and fingerprint["size"] != len(data):
        return fingerprint.get("bytes", []) or [fingerprint]
    wrong = []
    for entry in fingerprint.get("bytes", []):
        address, value = entry["address"], entry["value"]
        mask = entry.get("mask", [0xFF] * len(value))
        if address + len(value) > len(data) or any((data[address + i] ^ value[i]) & mask[i] for i in range(len(value))):
            wrong.append(entry)
    return wrong


def identify(data, packs):
    return [pack_id for pack_id, rom_size, fingerprints in packs if len(data) >= rom_size and any("bytes" in f and not misses(data, f) for f in fingerprints)]


if __name__ == "__main__":
    packs = load_packs()
    rows = []
    for relative, data in images():
        hits = identify(data, packs)
        assert len(hits) <= 1, (relative, hits)
        rows.append((relative, hits[0] if hits else "-"))
        if "--near" in sys.argv and not hits:
            for pack_id, _, fingerprints in packs:
                for fingerprint in fingerprints:
                    wrong = misses(data, fingerprint)
                    if 0 < len(wrong) <= 4:
                        print(f"near {pack_id}: {relative}: " + ", ".join(f"0x{e['address']:04x}" for e in wrong))
    with open(OUT, "w", encoding="utf-8", newline="\n") as f:
        f.write("# The pack each image of the test corpus is identified as, or - when no pack describes it.\n")
        f.write("# Paths are relative to hondaecu/test-roms in the workspace. Every 32 KiB image there is listed.\n")
        f.write("pack\tpath\n")
        for relative, pack in rows:
            f.write(f"{pack}\t{relative}\n")
    print(len(rows), "images:", dict(collections.Counter(pack for _, pack in rows)))
