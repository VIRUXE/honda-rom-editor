# The surface and the graph

A table's page shows two pictures of the table beside its cells: [`TableSurface`](../../src/HRE.App/TableSurface.cs), the whole table as a surface that can be turned, and [`TableGraph`](../../src/HRE.App/TableGraph.cs), one column as a line whose points can be dragged. Both derive from [`TableChart`](../../src/HRE.App/TableChart.cs). This page records how they are made, why, and how each statement was checked.

## Painted by the editor

WinForms on .NET has no chart control (the one of .NET Framework was not carried over) and nothing that draws in three dimensions. A table has a few hundred cells, so both pictures are drawn with GDI+ in `OnPaint`, double-buffered. The [Design principles](https://github.com/VIRUXE/honda-rom-editor/wiki/Design-principles) allow custom drawing where a standard control cannot do the job; the price is that a painted picture tells a screen reader nothing by itself. What was done about that is under [What a screen reader is given](#what-a-screen-reader-is-given).

## One table, shown three times

A picture holds no values and no selection of its own. It is made for a `TableGrid` and

- draws the cells of that grid's document, again on every change, undo and redo;
- marks the grid's current cell and its selected cells;
- moves by setting the grid's current cell;
- changes cells by calling the grid (`Nudge` for a step, `Add` for a drag).

So the cells and the pictures cannot disagree, a change made in a picture is the same undo step with the same name as one made in the cells, and what the change did is announced and written on the line under the cells by the code that already did that.

When the table changes, the page is built again around the grid, which stays (see [The table grid](Table-grid.md)). The pictures stay with it, so the surface is still turned the way it was.

## The surface

Each cell is a corner of the surface: a table of 20 rows and 10 columns is 19 by 9 faces. The base is a square whatever the numbers of rows and columns, and the largest value stands 0.6 of its side above the smallest. Rows and columns are evenly spaced, as in the cells.

**Projection.** Parallel, with no perspective: `ChartMath.Project` turns a point around the upright axis and looks at it from an angle above the ground.

```
across  = x cos(turn) - y sin(turn)
towards = x sin(turn) + y cos(turn)
screen x = across
screen y = towards sin(tilt) - z cos(tilt)
```

**What covers what.** Faces are drawn from the farthest to the nearest, ordered by `towards` at the middle of the face. For a surface that has one height at each place, seen from above without perspective, that order is right for every face: a face can only be covered by one that is nearer on the ground. It is why the tilt stops at 5 and 85 degrees and never goes under the ground. Of the frame, the base and three uprights are drawn before the surface and the upright and the two sides at the nearest corner after it.

**Colour.** A face has the heat colour of the mean of its four cells, with a thin line of the background between faces, as between cells. Under a Windows contrast theme the faces have the colour of the window and the lines the colour of text: a drawing in lines, with what is behind still hidden.

**Size.** The scale is taken from the diagonal of the base, so the surface does not grow and shrink while it is turned; it does change with the tilt.

**Turning.** With the mouse, the width of the picture is half a turn. The arrow keys turn by 15 degrees and tilt by 5. Both go the same way: the side that faces the viewer follows the hand or the arrow. A press that does not move farther than Windows' own threshold for a drag is a click, and moves the grid to the cell whose corner is nearest, when one is within reach.

Seen in pictures on the ignition and fuel maps of a factory P28 ROM, at the starting view (30 degrees turned, 30 degrees above the ground) and at 300 and 50 degrees.

## The graph

**Which way the lines run.** One line for each column, one point for each row. On the main maps that is one line per engine load over engine speed, the way a fuel or ignition curve is usually looked at. A table of a single row is one line along its columns.

**Spacing.** Points are evenly spaced, like the cells, not placed by the value of their step. The steps of the factory maps are close together at low engine speed and far apart at the top; placed by value, the first eight points of a low-cam map would share a fifth of the width and could not be taken hold of one by one.

**Scale.** `ChartMath.Scale` picks lines a round step apart (1, 2 or 5 times a power of ten), with the ends just outside the smallest and the largest value of the whole table, so every line is on the same scale.

**Taking a point.** The point under the pointer is the nearest within 0.6 of a line of text. Lines cross and lie on each other, so the points of the line that stands out are preferred to those of the others.

**Dragging.** Nothing is written while a point is dragged. The picture shows each dragged cell at the value it would be stored as (`TableEdit.Nearest`), so the point moves in the steps the cell has and the number beside it is the one that will be there. On release the cells are changed in one step: the dragged cell by the amount it moved to its nearest value, the other selected cells by the same amount, each to the nearest value it can hold. Writing on every movement of the mouse would have filled the undo history with one step per pixel.

The scale of the drag is fixed when the point is taken, and the amount is kept inside what the dragged cell can hold, so a point can be pulled past the top of the picture and its value still follows the hand.

## Where they stand

Beside the cells, the surface over the graph, when at least 18 lines of text of width are left beside the grid; then a change to a cell shows in the pictures without scrolling. Otherwise under the cells, side by side when both fit and one over the other when not. All sizes are in lines of text, so at twice the text size the pictures go under the cells of a 10-column map on a 1900 pixel window, and are twice as large.

Two things had to be kept from moving the page while a picture is used:

- The page follows the grid's current cell. A cell chosen in a picture would scroll the page back to the cells and take the picture away from under the pointer; the page now stays while a picture has the keyboard.
- A scrolling panel brings a control that takes the focus into view. For a picture taken hold of with the mouse that would shift it under the pointer at the start of a drag; the page stays while a mouse button is down.

## What a screen reader is given

Read from a second process with `System.Windows.Automation`, the window held up with `HRE.Shots --hold`:

| The picture is | UI Automation sees |
|---|---|
| A `Control` with `AccessibleName`, `AccessibleRole` and `AccessibleDescription` set | A pane without a name. None of the three arrives |
| A `Panel` with the same three | A pane with its name; no help text; not focusable from the keyboard, though it takes the focus |
| A `Panel` whose accessible object also returns the description as `Help` and adds `Focusable` to `State` | Name, help text and keyboard focusable, as below |

```
name='Surface of Fuel Map (Low Cam)' type=ControlType.Pane focusable=True
  help='The whole table as a surface, each cell as high as its value. Smallest 4 at RPM 500, MAP kPa 11.4; largest 600 at RPM 5000, MAP kPa 103.9.'
name='Graph of Fuel Map (Low Cam)' type=ControlType.Pane focusable=True
  help='The line of MAP kPa 63.4. Smallest 252 at RPM 500; largest 364.5 at RPM 5000. RPM 2000, MAP kPa 63.4 is 274.5.'
```

This agrees with the earlier finding that a painted control with accessible objects of its own did not reach UI Automation ([What .NET 11, WinForms and C# 15 offer](Platform-features.md)), and narrows it: a plain `Control` is not given a UI Automation provider at all, while the standard controls, `Panel` among them, are. The control type stays "pane" whatever role is set.

The values themselves are not repeated in the pictures: they are the cells of the grid, which a screen reader walks. Moving in a picture announces the cell it moved to and its value.

## How it was checked

- **Unit tests** ([`TableViewTests`](../../tests/HRE.Tests/TableViewTests.cs)): the projection and the scale as numbers; where points and corners fall; the keys; pressing, dragging and releasing a point against the bytes of the ROM, the undo step and what is announced; dragging several cells; that nothing is written during a drag or by a drag of less than half a step; names, descriptions and help text; where the page puts the pictures at three widths; that they stay while the table changes.
- **Pictures** with [`tools/HRE.Shots`](../../tools/HRE.Shots): `--show=graph` or `--show=surface` scrolls a picture into view, `--turn=turn,tilt` turns the surface. Taken at 1500, 1300 and 1000 pixels of width and at twice the text size.
- **UI Automation from a second process**, as above.

## Not checked

The window never had the focus, because the PC was in use: no point was dragged and nothing was turned with a real mouse, and no key was pressed in the window. The tests call the same code the mouse and the keys reach. No screen reader was run and no contrast theme was turned on. These go with the checks by hand in issue #49.
