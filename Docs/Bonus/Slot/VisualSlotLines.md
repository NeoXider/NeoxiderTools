# VisualSlotLines

**What it is:** a serializable helper that turns payline highlight objects on and off. Script:
`Scripts/Bonus/Slot/VisualSlotLines.cs`, namespace `Neo.Bonus`. [SpinController](SpinController.md) owns one as
its **Line Slot** field.

**How to use:**
1. Create one GameObject per payline (a line sprite, an image or a frame) and assign them to
   **Line Slot > Lines** on `SpinController`, in the same order as the [LinesData](Data/LinesData.md) lines.
2. The controller shows the active lines while you change the line count, hides all lines when a spin starts,
   and shows the winning lines after a win.
3. Call `LineActiv(int[] ids)` or `LineActiv(bool)` yourself when you drive lines from your own UI.

---

## Members

| Member | Description |
|--------|-------------|
| `GameObject[] lines` | Highlight object per payline index. |
| `void LineActiv(int[] idList)` | Hides every line, then activates the listed indices; out-of-range ids and null entries are skipped. |
| `void LineActiv(bool activ)` | Shows or hides all lines. |

The number of line objects also caps the line count the controller offers (`AddLine`): with objects assigned, the
maximum is the smaller of the valid payline definitions and `lines.Length`.

## See also

- [SpinController](SpinController.md), [LinesData](Data/LinesData.md)
- [Slot module](README.md)
