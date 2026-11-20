# Design principles

Every part of the interface is held to these rules, and a change that touches the interface is checked against the list at the end.

The interface must be **accessible**, **modern** and **easy for first-time users** who are not technical, and it must **use what the platform offers** (.NET, WinForms, the newest C#) in place of hand-made substitutes.

## Who it is for

Someone with a file read from the chip of their car's engine computer. They may never have seen hexadecimal and may not know what a ROM, a checksum or a table is. They want to see what the file is, later change it, without breaking the car. People who know all this are served by the same window: the technical detail is one command away, never in the way.

## Wording

1. Say what a thing means before how it is stored. "How much fuel the engine gets at each engine speed and engine load" comes first; the address comes last.
2. One word per thing, everywhere:

   | In the window | Not |
   |---|---|
   | ROM, ROM file | image, binary, dump |
   | Kind of ROM | definition pack, pack id, platform |
   | Table; setting (a single value) | parameter |
   | Steps (of a row or a column) | breakpoints |
   | Checksum, explained where it first appears as "a self-check built into the ROM" | |
   | Add-on, on or off | patch, applied |

   The names the community uses for tables ("Fuel Map (Low Cam)") stay as they are: tutorials and forums use them. Each gets a description under it.
3. Addresses, hexadecimal, hashes, byte counts of encodings, identifiers and chip names appear only in sections marked technical, which are shown with **View > Technical details** (Ctrl+T) or the button at the foot of a page.
4. A problem says what happened and what to do, in at most three short sentences, and offers the way on as a button. It never shows the system's own message outside the technical details, never says "error", "invalid" or "failed", and never blames the user.
5. Every number has its unit and is rounded for reading: whole numbers from a thousand up, one decimal below. Sizes are written with their unit and a thousands separator ("32,768 bytes", "1 byte").
6. The interface is in English with fixed number formats: a point as the decimal mark, as the tuning programs and tutorials write table values.
7. Sentence case. A button names what it does ("Open a file...", "Choose another file..."); three dots only when the command asks for more before acting.
8. The program says what it cannot do yet, where the user would look for it.

## Behaviour

- The next step is always on the screen as a button, with the keyboard focus on it at start. Nothing important is only in a menu.
- Opening a file never changes it. A file that cannot be opened leaves what was open as it was, and the explanation appears above the page.
- A file is recognised by the program; the user is never asked what kind of ROM it is.
- What can go wrong with a file is told apart: missing, a folder, in use, not allowed, empty, too small, too large, the right size but unknown.
- The window remembers where it was, the width of the list, the text size, whether technical details are shown, and the files opened lately.
- A fault in the program is told in the window, which keeps running.

Rules already fixed for editing and saving, which come later:

- Every change is one step that can be undone, named in the menu. Changes are not confirmed one by one.
- The checksum is corrected when saving, without asking.
- Saving never destroys the file that was opened without the user choosing to, and writes through a temporary file so that a crash cannot leave half a ROM.
- The program asks only before what cannot be undone, and the button that is safe is the default.

## Accessibility

- **Keyboard.** Every command is reachable without a mouse: access keys on every menu and item, shortcuts for the common ones, Tab through the page, F6 between the list and the page, arrow keys on the divider. The focus is always visible.
- **Screen readers.** The window is made of standard controls, each with a name: text as labels, values as read-only fields named after their row, actions as buttons, notices as groups with their heading as the name. Nothing that carries information is only painted. What an action did is announced.
- **Contrast.** Text at least 7 : 1 on its background, dimmed and coloured text at least 4.5 : 1, anything that shows a state or the edge of a control at least 3 : 1. A unit test holds the palette to this.
- **Colour is never the only signal.** A state is words, and a symbol beside them; colour comes third.
- **Text size.** View > Larger text enlarges everything up to twice the size, and pages grow taller instead of clipping. Sizes are derived from the font, so display scaling and Windows' own text size carry through.
- **Windows contrast themes.** When the user turns one on, every colour comes from that theme. The program has one look and no theme setting; the system's accessibility setting is not a theme of the program and wins over it.
- **No motion** that the user did not cause.

## Look

- True black, light text, one teal accent, used for the one main button of a page, the selection and links.
- A page is one reading column, no wider than is comfortable to read, starting at the top left.
- Notices stand on a slightly lighter card with a coloured edge and a symbol: information, good, warning, problem.
- Symbols come from the icon font of Windows; they follow the text size and the colour around them. No emoji.
- Standard controls in their current Windows look; custom drawing only where a standard control cannot do the job (the selection of the list; later the table grid).

## What the platform offers, and what is used

See the research note [What .NET 11, WinForms and C# 15 offer](https://github.com/VIRUXE/honda-rom-editor/blob/main/docs/research/Platform-features.md) for what was checked on the installed SDK and why each thing is or is not used.

## Checklist for a change to the interface

- [ ] Can it be done with the keyboard alone, and is the focus visible at each step?
- [ ] Does every new control have a name a screen reader can read, and is every new piece of information in a control and not only painted?
- [ ] Is every new string in the window's words, with technical detail in a technical section?
- [ ] Does every new problem say what happened and what to do, with a button for the way on?
- [ ] Do new colours come from `DarkTheme`, with their pairs added to the contrast test?
- [ ] Are all sizes derived from the font or scaled from 96 dpi, with no fixed pixel positions?
- [ ] Is it right at twice the text size (`HRE.Shots --zoom=2`)?
- [ ] Are the wording, the names and the roles covered by tests, and the pictures in `docs/assets` retaken?

Some things can only be judged by a person at the machine and are checked before a release: reading the window with Narrator, a display at 150 % scaling, a Windows contrast theme, and Windows' text size setting.
