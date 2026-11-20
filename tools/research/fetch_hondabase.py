"""Fetches files of github.com/hondabase/files-archive that are not in the corpus yet.

usage: fetch_hondabase.py <tree.json> <out-dir> <list-file>

<tree.json> is the recursive tree of the archive at COMMIT (gh api repos/hondabase/files-archive/
git/trees/COMMIT?recursive=1). <list-file> names one archive path per line. Each file is checked
against its git blob SHA-1; a Git LFS pointer is resolved and the object checked against the
SHA-256 and size the pointer states. Nothing is overwritten.
"""
import hashlib
import json
import os
import sys
import urllib.parse
import urllib.request

COMMIT = "85892fc1f508a82693073003d2e86f515f55a60d"
RAW = "https://raw.githubusercontent.com/hondabase/files-archive/" + COMMIT + "/"
MEDIA = "https://media.githubusercontent.com/media/hondabase/files-archive/" + COMMIT + "/"


def get(url):
    request = urllib.request.Request(url, headers={"User-Agent": "rom-corpus-fetch"})
    with urllib.request.urlopen(request, timeout=120) as response:
        return response.read()


def blob_sha1(data):
    return hashlib.sha1(b"blob %d\0" % len(data) + data).hexdigest()


def main():
    tree_path, out_dir, list_path = sys.argv[1:4]
    tree = {e["path"]: e for e in json.load(open(tree_path, encoding="utf-8"))["tree"] if e["type"] == "blob"}
    wanted = [line.rstrip("\n") for line in open(list_path, encoding="utf-8") if line.strip()]
    for path in wanted:
        entry = tree[path]
        target = os.path.join(out_dir, path.replace("/", os.sep))
        if os.path.exists(target):
            print("have    ", path)
            continue
        quoted = urllib.parse.quote(path)
        data = get(RAW + quoted)
        if blob_sha1(data) != entry["sha"]:
            raise SystemExit(f"{path}: blob SHA-1 does not match the tree")
        kind = "blob"
        if len(data) < 200 and data.startswith(b"version https://git-lfs.github.com/spec/v1"):
            fields = dict(line.split(" ", 1) for line in data.decode("ascii").splitlines() if " " in line)
            oid, size = fields["oid"].removeprefix("sha256:"), int(fields["size"])
            data = get(MEDIA + quoted)
            if len(data) != size or hashlib.sha256(data).hexdigest() != oid:
                raise SystemExit(f"{path}: LFS object does not match its pointer")
            kind = "lfs"
        os.makedirs(os.path.dirname(target), exist_ok=True)
        with open(target, "wb") as f:
            f.write(data)
        print(f"{kind:8s}", len(data), path)


if __name__ == "__main__":
    main()
