# UI

**What it is:** `Neo.UI.UI` (`Scripts/UI/Simple/UI.cs`) is a lightweight page manager `MonoBehaviour` built around enabling and disabling `GameObject` screens. It centralizes page switching and supports instant, delayed, and animation-driven transitions; the active instance is available as `UI.I`.

**How to use:**
1. Add `Neoxider > UI > UI` to the root of your screens and fill `_pages` (or enable `_setChild` to collect the children of `_parentPages`).
2. Set `startId` to the page shown on load (`-1` = none).
3. Switch pages with `SetPage(int)` from code, UnityEvents or [ButtonChangePage](./ButtonChangePage.md).

---

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
| `SetPageDelay(int id)` | Switches after `_timeDelay` seconds. |
| `SetPageAnim(int id)` | Starts an animated page transition: restarts the curtain `_animator`, switches after `_timeDelay`, hides the curtain after another `_timeDelay`. |
| `SetOnePageAnim(int id)` | Animated reactivation of one page. |
| `UseUnscaledTime` | Reads or switches the time mode of the three delayed calls above. |
| `SetCurrentPage(bool active)` | Shows/hides the currently selected page without changing the selection. |
| `SetCurrtentPage(bool active)` | Misspelled legacy alias of `SetCurrentPage`, kept for existing UnityEvent wiring. |

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `_pages` | Page objects; the index is the page id. |
| `_setChild` / `_parentPages` | Tick `_setChild` to refill `_pages` from the children of `_parentPages` (or this object). |
| `id` | Current page (`-1` = none). |
| `startId` | Page activated in `Awake` (`-1` keeps the authored active states). |
| `_timeDelay` | Delay of `SetPageDelay`, `SetPageAnim` and `SetOnePageAnim`, in seconds. Default `1.5`. |
| `_animator` | Optional transition curtain restarted by the animated switches. |
| `_useUnscaledTime` | Count the delays in unscaled time, so delayed and animated switches still finish while `Time.timeScale` is `0` (pause menus). Default on. |

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
- `UI.I` is assigned in `Awake` and cleared when its owning component is destroyed. Destroying an older manager does not clear a newer one.
- An empty page list is supported. `SetPage(index)` still records `id` and emits `OnChangePage`; there is simply no matching page to activate.
- `startId = -1` preserves the authored active states on startup. `SetPage(-1)` explicitly hides every page.
- For button feedback and visual effects, combine it with `ButtonScale`, `ButtonShake`, `VisualToggle`, or other UI helpers from the same module.
- Under a [PausePage](./PausePage.md) (time scale `0`) the delayed and animated switches still finish with the
  default unscaled time. The curtain `Animator` has its own clock: set its **Update Mode** to **Unscaled Time**
  too, or the curtain freezes while the page switch still happens.
- Upgrade note: `_useUnscaledTime` is new and defaults to on. Before it, `SetPageAnim`, `SetOnePageAnim` and
  `SetPageDelay` waited in scaled time and never switched while the game was paused. Untick `Use Unscaled Time`
  or set `UseUnscaledTime = false` to keep scaled delays (slow motion included); `SetPageDelay` then uses
  `Invoke` as before.

```csharp
using Neo.UI;

// The same calls can be assigned to a Button's On Click event.
UI.I.SetPage(2);
UI.I.SetCurrentPage(false);
```

## See also

- [README](./README.md)
- [UI docs](./README.md)
- [Tools/Text](../Tools/Text/README.md)
