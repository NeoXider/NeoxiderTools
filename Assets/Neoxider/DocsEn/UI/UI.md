# UI

`UI` is a lightweight page manager built around enabling and disabling `GameObject` screens. It centralizes page switching and supports instant, delayed, and animation-driven transitions. File: `Assets/Neoxider/Scripts/UI/Simple/UI.cs`, namespace: `Neo.UI`.

## What it does

- Keeps a page list in one place.
- Activates one page by index.
- Supports delayed switching.
- Supports animated transitions when an `Animator` is configured.
- Can repopulate its page list from child objects.

## Main API

| API | Description |
|-----|-------------|
| `SetPage()` | Activates the page stored in the public `id` field. |
| `SetPage(int id)` | Activates one page and disables the others. |
| `SetOnePage(int id)` | Re-enables the page to restart state or animation. |
| `SetPageDelay(int id)` | Switches after the configured delay. |
| `SetPageAnim(int id)` | Starts an animated page transition. |
| `SetOnePageAnim(int id)` | Animated reactivation of one page. |
| `SetCurrtentPage(bool active)` | Toggles the currently active page. |

## Events

- `OnChangePage(int id)` fires when the active page changes.
- `OnStartPage()` fires when page `0` becomes active.

## Typical use

1. Add `UI` to a scene object.
2. Populate the page array manually or from child objects.
3. Choose the startup page.
4. Trigger `SetPage(...)` from buttons, gameplay code, or animation flow.

## Notes

- This component is intentionally simple and works best for page-like UI sections.
- For button feedback and visual effects, combine it with `ButtonScale`, `ButtonShake`, `VisualToggle`, or other UI helpers from the same module.

## See also

- [README](./README.md)
- [Russian UI docs](../../Docs/UI/README.md)
- [Tools/Text](../Tools/Text/README.md)
