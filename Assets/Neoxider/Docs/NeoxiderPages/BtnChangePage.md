# BtnChangePage (NeoxiderPages)

**What it is:** a UI button that drives **PM** page navigation. It handles pointer clicks, an optional press-scale animation, an optional game-state change, and can auto-label a `TMP_Text`.

**Add:** Add Component → Neoxider → Pages → BtnChangePage (on a UI element with a `Graphic`/raycast target).

---

## Actions

The **Action** field selects what the click does:

| Action | Behavior |
|--------|----------|
| `OpenPage` | Opens **Target Page Id** via `PM.ChangePage` (exclusive or popup, per the target `UIPage`). |
| `Cancel` | Returns to the previous page via `PM.SwitchToPreviousPage()`. |
| `CloseCurrent` | Closes the current page via `PM.CloseCurrentPage()`. |

## Fields

- **Intecactable** — when off, clicks and press animation are ignored.
- **Image Target** — the `Image` used for press feedback (auto-filled from the same GameObject).
- **Target Page Id** — page to open when Action is `OpenPage`.
- **Can Switch Page** — when off, the click runs the state/`OnClick` only and does not change pages.
- **Execute State** — a `GameState.State` run before switching (`Menu`, `Start`, `Restart`, `Pause`, `Resume`, `Win`, `Lose`, `End`, or `None`).
- **Use Anim Image** / **Time Anim Image** / **Scale Anim** — press-scale animation settings (unscaled time).
- **Change Text** / **Text Page** — when enabled, the `TMP_Text` is auto-labeled (`Cancel`, `Close`, or the target page display name).

## Events

- **On Click** — invoked after the state change and page switch.

## See also

- [PM](./PM.md)
- [UIPage](./UIPage.md)
- [NeoxiderPages README](./README.md)
