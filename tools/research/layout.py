"""What the code of each OBD1 image says about its main maps, and the images grouped by it.

The firmware sets up every main-map lookup the same way: column count, row count, the
indexes, then the address of the map. The breakpoint lists are loaded just before the
breakpoint search is called. This script finds those instructions by their bytes, whatever
the operands, and prints for each image: where each set-up is, rows x columns, the map
addresses, and the address and search limit of each rpm and MAP axis.

usage: layout.py            groups of images with the same layout
       layout.py --axes     per layout, the distinct axis bytes and fuel column multipliers
"""
import re
import sys

from workspace import images

IDX = rb"[\xC3\xC4].\x4A[\xB3\xB4].\x79[\xC3\xC4].\x4B[\xB3\xB4].\x4B"
MAIN = re.compile(rb"\x98(.)\x99(.)" + IDX + rb"\x60(..)", re.S)
LOW = re.compile(rb"[\xC3\xC4].\x4B[\xB3\xB4].\x4B\x60(..)", re.S)
RPM_AXIS = re.compile(rb"\x60(..)[\xC3\xC4].\x4B[\xC4\xC5].\x4A\x9E(.)", re.S)
MAP_AXIS = re.compile(rb"\x60(..)[\xC3\xC4].\x4B\x9E(.)\xA3\x0C\x32", re.S)


def word(b):
    return b[0] | b[1] << 8


def scan(data):
    """main: (address of the set-up, columns, rows, first map, second map or None); rpm, map: (address of the load, axis address, search limit)."""
    mains = []
    for m in MAIN.finditer(data):
        low = LOW.search(data, m.end(), m.end() + 48)
        mains.append((m.start(), m.group(1)[0], m.group(2)[0], word(m.group(3)), word(low.group(1)) if low else None))
    return {
        "main": mains,
        "rpm": [(m.start(), word(m.group(1)), m.group(2)[0]) for m in RPM_AXIS.finditer(data)],
        "map": [(m.start(), word(m.group(1)), m.group(2)[0]) for m in MAP_AXIS.finditer(data)],
    }


def describe(data):
    s = scan(data)
    parts = [f"@{at:04x} {rows}x{cols} first={first:04x} second={second if second is None else format(second, '04x')}" for at, cols, rows, first, second in s["main"]]
    parts.append("rpm " + " ".join(f"@{at:04x}:{a:04x}/{n}" for at, a, n in s["rpm"]))
    parts.append("map " + " ".join(f"@{at:04x}:{a:04x}/{n}" for at, a, n in s["map"]))
    return " | ".join(parts)


def hexs(b):
    return " ".join(f"{x:02x}" for x in b)


def axes(roms):
    """Per fuel-map layout: the distinct axis bytes and column multipliers, with how many images hold each."""
    seen = {}
    for name, data in roms.items():
        s = scan(data)
        fuel = next((m for m in s["main"] if m[0] in (0x1234, 0x12FC, 0x1266, 0x11F9)), None)
        if fuel is None or not s["rpm"] or not s["map"] or fuel[4] is None:
            continue
        at, cols, rows, first, second = fuel
        entry = seen.setdefault((at, rows, cols, first, second), {"n": 0, "map": {}, "rpm": {}, "mult": {}})
        entry["n"] += 1
        for where, address, limit in s["map"]:
            entry["map"].setdefault((where, hexs(data[address:address + limit + 2])), []).append(name)
        for where, address, limit in s["rpm"]:
            entry["rpm"].setdefault((where, hexs(data[address:address + limit + 2])), []).append(name)
        # The P72 type sets 15 rows first and 20 for the low-cam map.
        for label, base, height in (("first", first, rows), ("second", second, 20 if at == 0x1266 else rows)):
            entry["mult"].setdefault((label, hexs(data[base + height * cols:base + height * cols + cols])), []).append(name)
    for (at, rows, cols, first, second), entry in seen.items():
        print(f"== fuel@{at:04x} {rows}x{cols} first={first:04x} second={second:04x}: {entry['n']} images")
        for kind in ("map", "rpm", "mult"):
            for (where, text), names in sorted(entry[kind].items(), key=lambda kv: (str(kv[0][0]), -len(kv[1]))):
                where = f"@{where:04x}" if isinstance(where, int) else where
                print(f"   {kind} {where} x{len(names):2d}  {text}" + (f"   <- {names[0]}" if len(names) <= 2 else ""))


if __name__ == "__main__":
    roms = dict(images(0x8000, 0x10000))
    if "--axes" in sys.argv:
        axes(roms)
    else:
        groups = {}
        for name, data in roms.items():
            groups.setdefault(describe(data), []).append(name)
        for description, names in sorted(groups.items(), key=lambda kv: -len(kv[1])):
            print(f"[{len(names)}] {description}")
            for name in names:
                print("     ", name)
