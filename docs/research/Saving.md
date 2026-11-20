# Saving a ROM file

What was checked and decided for **File > Save** (issue #11), and how to repeat it.

## What saving has to guarantee

From the [design principles](../wiki/Design-principles.md): the checksum is corrected without asking; a crash cannot leave half a ROM; the file that was opened is never destroyed without the user choosing to; the program asks only before what cannot be undone.

## How the file is written

1. The checksum is corrected in a copy of the image, with the rule of the ROM's description (`Checksum.Fix`). A description without a checksum leaves the image as it is.
2. The bytes go to `<name>.tmp` in the same folder.
3. `File.Replace(temporary, file, backup)` puts the temporary file in the place of the ROM file in one step of the file system. When the file does not exist yet (Save as, a new name), `File.Move` does.
4. Only then does the session take the new path, judge the checksum again and mark the document as saved. When any step is refused, the temporary file is deleted and nothing else has changed.

The corrected checksum byte is taken into the document without an undo step (`RomDocument.MarkSaved(written)`): the document always equals the file after a save, and undoing a change afterwards takes back the change, not the correction. The byte is corrected again at the next save.

## What `File.Replace` reports

Probed with PowerShell 7 (.NET 10.0.15) on Windows 11 build 26300, a three-byte file, its replacement and a backup name in one folder:

| State of the file to replace | Result |
|---|---|
| Not held | Replaced; the old file is at the backup name |
| Held by a program that shares nothing, or shares reading only | `IOException`, `HResult` `0x80070020` (sharing violation). The file is as it was, no backup is made, the temporary file is still there |
| Held by a program that shares reading, writing and deleting | Replaced, with the backup |
| Read-only attribute | `UnauthorizedAccessException`, `0x80070005`. File as it was, no backup, temporary file still there |

The editor's tests repeat the first, the second and the last case on .NET 11 through `RomSession.Save` (`EditingTests`). The editor also takes `0x80070021` (lock violation) and `0x80070497` (`ERROR_UNABLE_TO_REMOVE_REPLACED`, which the Windows documentation of `ReplaceFile` names for a file that cannot be replaced) as "another program is using the file"; neither was seen in the probe.

To repeat: write a file and a replacement, open the file with `[IO.File]::Open(path, 'Open', 'Read', 'None')`, call `[IO.File]::Replace(replacement, path, backup, $true)` and read the exception's `HResult`.

## The copy of the first state

The first time the editor replaces a file, the file as it was is kept next to it as `<name>.bak`. A `.bak` that is already there is never overwritten.

Considered and not done:

- **A new backup at every save**, as text editors do. After the second session the backup would be the first session's tune, and the ROM as it came from the chip would be gone. For a tuner that ROM is the one that cannot be made again.
- **Asking before the first save.** The principles keep questions for what cannot be undone; with the copy, replacing the file can be undone.
- **No backup.** Saving would then destroy the opened file on the user's first Ctrl+S.

The save that makes the copy says so in a notice above the page; later saves only change the title and the status bar, so that nothing moves the table while it is being tuned.

## The question before changes are lost

Closing the file, opening another one and closing the window ask "Save the changes to ...?" when there are changes: **Save**, **Don't save**, **Cancel**. Enter saves and Esc goes back, so no single key loses work. A file that would not open anyway does not trigger the question: it is opened first, and the question comes only when it would take the place of the open ROM.

The question is a window of the editor, not `MessageBox` or `TaskDialog`, which stay light under the black theme (see [The black theme](Dark-theme.md)). `tools/HRE.Shots --ask out.png` takes its picture.

## Ctrl+Z while a number is typed

Menu shortcuts are handled before the focused control sees the key, so Ctrl+Z would undo the last change to the ROM while the user is correcting a number in a cell. The Undo command therefore hands the key to the cell's text field while a cell is being edited (`TextBox.Undo`), and Redo does nothing then.
