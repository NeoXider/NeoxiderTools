# UIPage

**Purpose:** UI page component for `PM` (Page Manager). Stores `PageId`, popup mode, and open/close animation settings.

## Setup

1. Add `UIPage` to the root GameObject of a page.
2. Assign `Page Id`.
3. Enable `Popup` if the page should open above other pages.
4. Enable `Ignore On Exclusive Change` if the page should not be closed during exclusive page switches.
5. For animation, add `DOTweenAnimation` and assign it to `Animation`.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `Page Id` | Page identifier used by `PM`. |
| `Popup` | Opens above current pages without deactivating them. |
| `Ignore On Exclusive Change` | `PM` does not deactivate this page during regular exclusive switches. |
| `Animation` | `DOTweenAnimation` played on show/hide. |
| `Animation Mode` | When the animation is played: `ForwardOnly`, `BackwardOnly`, `ForwardAndBackward`. |

## Animation Mode

| Mode | Behavior |
|------|----------|
| `ForwardOnly` | `StartActive()` enables the page and restarts the forward animation from the beginning. `EndActive()` disables the page immediately. |
| `BackwardOnly` | `StartActive()` only enables the page. `EndActive()` restarts the reverse animation from the end, then disables the page. |
| `ForwardAndBackward` | Opening restarts the forward animation from the beginning; closing restarts the reverse animation from the end. |

Page animation is forced to unscaled time (`DOTweenAnimation.isIndependentUpdate = true`) and `autoKill = false`, so it works during pause/menu flows and can be restarted reliably.

On exclusive switches via `PM` (e.g. Menu → Shop), when the **incoming** page has a show animation, other pages (except `Popup` and `Ignore On Exclusive Change`) are hidden **after** the show tween completes (`WaitForShowAnimation`), then `EndActive()` runs — no empty backdrop. `Popup` pages are never deactivated on exclusive switches; opening a popup still goes through `ActivePage` and leaves other pages untouched.

## API

| Method | Purpose |
|--------|---------|
| `StartActive()` | Enable the page and play the opening animation according to `Animation Mode`. |
| `EndActive()` | Close the page and play the reverse animation according to `Animation Mode`. If the GameObject is already inactive in the hierarchy, only `SetActive(false)` runs — no coroutine (avoids Unity's inactive coroutine error). |
| `SetActive(bool)` | Directly enable/disable the page GameObject. |

## Compatibility

Legacy fields `_playBackward` and `_onlyPlayBackward` are migrated automatically:

- `_onlyPlayBackward = true` → `BackwardOnly`;
- `_playBackward = true` → `ForwardAndBackward`;
- `_playBackward = false` → `ForwardOnly`.

## See Also

- [PM](./PM.md)
- [BtnChangePage](./BtnChangePage.md)
