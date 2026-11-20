"""What the archives under a folder hold, without extracting them: the members that could be ROM images.

Needs 7-Zip; set SEVEN_ZIP to its 7z executable if it is not in the usual place.

usage: list_archives.py <folder> [--all]
"""
import os
import subprocess
import sys

SEVEN_ZIP = os.environ.get("SEVEN_ZIP", r"C:\Program Files\7-Zip\7z.exe")
ROM_EXT = {".bin", ".rom", ".new", ".baserom", ".base", ".cal", ".xdf", ".adx", ".skl", ".kal", ".hex", ".s19", ".eex", ".elf", ".dlf", ".gbs"}
ROM_SIZES = {8192, 16384, 16385, 32768, 32769, 49152, 65536, 131072, 262144, 524288, 1048576}


def members(archive):
    out = subprocess.run([SEVEN_ZIP, "l", "-slt", "-ba", archive], capture_output=True, text=True, encoding="utf-8", errors="replace").stdout
    entry = {}
    for line in out.splitlines() + [""]:
        if not line.strip():
            if entry.get("Path") and entry.get("Attributes", "").find("D") < 0:
                yield entry["Path"], int(entry.get("Size", "0") or 0)
            entry = {}
        elif " = " in line:
            key, value = line.split(" = ", 1)
            entry[key] = value


if __name__ == "__main__":
    root = sys.argv[1]
    show_all = "--all" in sys.argv
    for dirpath, _, files in os.walk(root):
        for name in sorted(files):
            archive = os.path.join(dirpath, name)
            found = list(members(archive))
            roms = [(p, s) for p, s in found if os.path.splitext(p)[1].lower() in ROM_EXT or s in ROM_SIZES]
            print(f"== {os.path.relpath(archive, root)}: {len(found)} members, {len(roms)} ROM-like")
            for path, size in (found if show_all else roms):
                print(f"   {size:9d}  {path}")
