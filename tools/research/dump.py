"""Byte grids from a ROM image.

usage: dump.py <image> <address, hex> <rows> <cols> [<address> <rows> <cols> ...]
"""
import sys

data = open(sys.argv[1], "rb").read()
args = sys.argv[2:]
for k in range(0, len(args), 3):
    addr, rows, cols = int(args[k], 16), int(args[k + 1]), int(args[k + 2])
    print(f"-- {addr:04x} {rows}x{cols}")
    for r in range(rows):
        print("   " + " ".join(f"{data[addr + r * cols + c]:02x}" for c in range(cols)))
