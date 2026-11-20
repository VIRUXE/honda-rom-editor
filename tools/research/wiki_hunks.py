"""The text replacements that turn one committed revision of a wiki page into another, as JSON.

For bringing the GitHub wiki up to date by hand when docs/wiki cannot be pushed to it: each
pair is [text to find, text to put in its place], and sha256 is that of the page afterwards.

usage: wiki_hunks.py <old revision> <new revision> <Page>
"""
import difflib
import hashlib
import json
import subprocess
import sys

from workspace import REPO

old_rev, new_rev, page = sys.argv[1:4]


def blob(rev):
    return subprocess.run(["git", "-C", REPO, "show", f"{rev}:docs/wiki/{page}.md"], capture_output=True, check=True).stdout.decode("utf-8")


old, new = blob(old_rev), blob(new_rev)
a, b = old.splitlines(keepends=True), new.splitlines(keepends=True)
pairs = []
for tag, i1, i2, j1, j2 in difflib.SequenceMatcher(None, a, b, autojunk=False).get_opcodes():
    if tag == "equal":
        continue
    # One line of context before, so that an insertion has something to hold on to.
    start = max(i1 - 1, 0)
    while start > 0 and old.count("".join(a[start:i2])) != 1:
        start -= 1
    before = "".join(a[start:i2])
    after = "".join(a[start:i1]) + "".join(b[j1:j2])
    assert old.count(before) == 1, before[:60]
    pairs.append([before, after])
check = old
for before, after in pairs:
    check = check.replace(before, after)
assert check == new
print(json.dumps({"pairs": pairs, "sha256": hashlib.sha256(new.encode("utf-8")).hexdigest(), "bytes": len(new.encode("utf-8"))}))
