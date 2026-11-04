# BtnChangePage (NeoxiderPages)

**What it is:** a UI button that drives **PM** page navigation. It handles pointer clicks, an optional press-scale animation, an optional game-state change, and can auto-label a `TMP_Text`.

**How to use:**
1. **Add Component → Neoxider → Pages → BtnChangePage** on a UI element with a `Graphic`/raycast target. The Action starts as `OpenPage` with no target page; pick one in the Inspector.
2. Choose the **Action** and, for `OpenPage`, the **Target Page Id**.
3. Optionally set **Execute State** to change the game state on click (for example `Restart`).

Script: `Samples~/NeoxiderPages/Runtime/Scripts/Page/BtnChangePage.cs`, namespace `Neo.Pages`.

---

## Actions

The **Action** field selects what the click does:

| Action | Behavior |
|--------|----------|
| `OpenPage` | Opens **Target Page Id** via `PM.ChangePage` (exclusive or popup, per the target `UIPage`). |
| `Cancel` | Returns to the previous page via `PM.SwitchToPreviousPage()`. |
| `CloseCurrent` | Closes the current page via `PM.CloseCurrentPage()`. |

Only the primary pointer button reacts (left mouse button, touch, pen); right and middle clicks are ignored. Without
a `PM` in the scene a click runs the game state and `OnClick` but skips the page action (a warning through
`NeoDiagnostics`).

**Legacy action values.** Old scenes and prefabs can carry a value of the retired `UIKit.Page` enum (for example
`12` = Pause) in the Action field. Such a button keeps its old behavior: it opens **Target Page Id** (nothing without
one). The first click logs one warning per button even with `NeoDiagnostics` warnings off, and the Inspector shows
the value with a **Use Open Page** fix button. `HasLegacyAction` / `BtnChangePage.IsLegacyAction(value)` detect it
from code.

## Fields

- **Interactable** (serialized as `intecactable`) — when off, clicks and press animation are ignored.
- **Image Target** — the button's `Image`, auto-filled from the same GameObject (a reference for project extensions; the press animation scales the whole button).
- **Target Page Id** — page to open when Action is `OpenPage`.
- **Can Switch Page** — when off, the click runs the state/`OnClick` only and does not change pages.
- **Execute State** — a `GameState.State` run before switching (`Menu`, `Start`, `Restart`, `Pause`, `Resume`, `Win`, `Lose`, `End`, or `None`).
- **Use Anim Image** / **Time Anim Image** / **Scale Anim** — press-scale animation settings (unscaled time); the scale is relative to the button's own scale on every axis.
- **Change Text** / **Text Page** — when enabled, the `TMP_Text` is auto-labeled (`Cancel`, `Close`, or the target page display name).

## Events

- **On Click** — invoked after the state change and page switch.

## API

| Member | Purpose |
|--------|---------|
| `ChangePage()` | Runs the Action now (open / back / close) on the scene's `PM`. |
| `Interactable` | Same as the **Interactable** field. |
| `ButtonAction` | The **Action** (`OpenPage`, `Cancel`, `CloseCurrent`). |
| `TargetPageId` | Page opened by `OpenPage` (and by a legacy action value). |
| `HasLegacyAction` / `IsLegacyAction(Action)` | True for a value outside `Action` left by the old `UIKit.Page` enum. |

## See also

- [PM](./PM.md)
- [UIPage](./UIPage.md)
- [NeoxiderPages README](./README.md)
