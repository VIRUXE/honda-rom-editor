"""Which corpus images are the same ROM, and which other image each one is closest to.

Closeness is the share of identical bytes in the first 24 KiB, where the code is. Images of
one firmware type agree in 98 % or more of it; images of different types in a few percent.
Prints the word at 0x23 as well: it differs between the firmware types.

usage: survey.py
"""
import hashlib

from workspace import images


def same(a, b, lo=0, hi=0x6000):
    return sum(1 for i in range(lo, hi) if a[i] == b[i])


if __name__ == "__main__":
    roms = dict(images())
    by_hash = {}
    for name, data in roms.items():
        by_hash.setdefault(hashlib.sha256(data[:0x8000]).hexdigest(), []).append(name)
    print("== identical images ==")
    for digest, names in by_hash.items():
        if len(names) > 1:
            print(digest[:12], names)
    distinct = {names[0]: roms[names[0]] for names in by_hash.values()}
    print("== the two closest other images, by identical bytes in 0000-5FFF ==")
    for name, data in roms.items():
        best = sorted(((same(data, other), n) for n, other in distinct.items() if other[:0x8000] != data[:0x8000]), reverse=True)[:2]
        print(f"{name:80s} w23={data[0x23]:02x}{data[0x24]:02x} " + "  ".join(f"{n}:{count * 100 / 0x6000:.1f}%" for count, n in best))
