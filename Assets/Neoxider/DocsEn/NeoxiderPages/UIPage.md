# UIPage

**Purpose:** UI page component for `PM` (Page Manager). Stores `PageId`, popup mode, and compatibility settings for open/close behavior.

## Setup

1. Add `UIPage` to the root GameObject of a page.
2. Assign `Page Id`.
3. Enable `Popup` if the page should open above other pages.
4. Enable `Ignore On Exclusive Change` if the page should not be closed during exclusive page switches.
5. For custom animation, add a project-specific component next to `UIPage`.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `Page Id` | Page identifier used by `PM`. |
| `Popup` | Opens above current pages without deactivating them. |
| `Ignore On Exclusive Change` | `PM` does not deactivate this page during regular exclusive switches. |
| `Animation Mode` | Compatibility setting for older scenes and future project extensions. The base sample does not require tween components. |

## Animation Mode

| Mode | Behavior |
|------|----------|
| `ForwardOnly` | Compatibility with older settings. In the base sample, the page is enabled/disabled immediately. |
| `BackwardOnly` | Compatibility with older settings. In the base sample, the page is enabled/disabled immediately. |
| `ForwardAndBackward` | Compatibility with older settings. In the base sample, the page is enabled/disabled immediately. |

On exclusive switches via `PM` (for example Menu -> Shop), the incoming page is enabled and outgoing pages are closed through `EndActive()`. `Ignore On Exclusive Change` pages are left untouched. `Popup` pages are closed by default when an exclusive non-popup page opens (`PM.closePopupsOnExclusivePageChange = true`). Disable that PM option when popups must survive exclusive page switches. Opening a popup still goes through `ChangePage` -> `ActivePage` and leaves the background page untouched.

## API

| Method | Purpose |
|--------|---------|
| `StartActive()` | Enable the page. |
| `EndActive()` | Close the page. If the GameObject is already inactive in the hierarchy, only `SetActive(false)` runs. |
| `SetActive(bool)` | Directly enable/disable the page GameObject. |

## Compatibility

Legacy fields `_playBackward` and `_onlyPlayBackward` are migrated automatically:

- `_onlyPlayBackward = true` → `BackwardOnly`;
- `_playBackward = true` → `ForwardAndBackward`;
- `_playBackward = false` → `ForwardOnly`.

## See Also

- [PM](./PM.md)
- [BtnChangePage](./BtnChangePage.md)



