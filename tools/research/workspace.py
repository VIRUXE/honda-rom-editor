"""Where the ROM corpus is, for the scripts of this folder.

The corpus is not part of the repository. It is looked for in hondaecu/test-roms under the
folder named by HRE_WORKSPACE, or, without that variable, under the folder that holds this
repository: the same rule the tests follow.
"""
import os

REPO = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
WORKSPACE = os.environ.get("HRE_WORKSPACE") or os.path.dirname(REPO)
ROMS = os.path.join(WORKSPACE, "hondaecu", "test-roms")


def read(relative):
    """The bytes of a corpus file, by its path relative to the corpus with forward slashes."""
    with open(os.path.join(ROMS, relative.replace("/", os.sep)), "rb") as f:
        return f.read()


def images(smallest=0x8000, largest=0x8001):
    """(path relative to the corpus, bytes) of every file whose size lies in the given range, in path order.

    Downloaded archives (hondabase/_archives) are skipped: what they hold is under _from-archives.
    """
    found = []
    for directory, _, names in os.walk(ROMS):
        if "_archives" in directory.split(os.sep):
            continue
        for name in names:
            path = os.path.join(directory, name)
            if smallest <= os.path.getsize(path) <= largest:
                found.append(os.path.relpath(path, ROMS).replace(os.sep, "/"))
    for relative in sorted(found, key=str.lower):
        yield relative, read(relative)
