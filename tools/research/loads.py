"""The instructions of a listing that load a table address in a given range into X1, X2 or DP.

The listing is one written by `rasm66k dasm <rom> <listing>`.

usage: loads.py <listing> <lowest address, hex> <highest address, hex> [lines of context]
"""
import re
import sys

path, lo, hi = sys.argv[1], int(sys.argv[2], 16), int(sys.argv[3], 16)
context = int(sys.argv[4]) if len(sys.argv) > 4 else 0
lines = [l.rstrip("\n") for l in open(path, encoding="utf-8", errors="replace")]
code = [l for l in lines if re.search(r"; [0-9A-F]{4} \d ", l)]
pat = re.compile(r"MOV\s+(X1|DP|X2), #(?:tbl_|label_|0)([0-9a-f]{4})h?\b")
for i, l in enumerate(code):
    m = pat.search(l)
    if m and lo <= int(m.group(2), 16) <= hi:
        for k in range(max(0, i - context), min(len(code), i + context + 1)):
            print(("> " if k == i else "  ") + code[k][:108])
        if context:
            print()
