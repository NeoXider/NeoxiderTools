# UIPage

**What it is:** UI page component for `PM` (Page Manager). Stores `PageId`, popup mode, and compatibility settings for open/close behavior. Script `Samples~/NeoxiderPages/Runtime/Scripts/Page/UIPage.cs`, namespace `Neo.Pages`.

**How to use:**

1. Add `UIPage` (**Add Component → Neoxider → Pages → UIPage**) to the root GameObject of a page, under the `PM` object.
2. Assign `Page Id`.
3. Enable `Popup` if the page should open above other pages.
4. Enable `Ignore On Exclusive Change` if the page should not be closed during exclusive page switches.
5. For custom animation, derive from `UIPage` (see [Custom animated pages](#custom-animated-pages)) or add a project-specific component next to it.

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
| `StartActive()` | Enable the page (a disable/enable cycle, so `OnEnable` of the page content runs again). Virtual. |
| `EndActive()` | Close the page. If the GameObject is already inactive in the hierarchy, only `SetActive(false)` runs. Virtual. |
| `SetActive(bool)` | Directly enable/disable the page GameObject. |
| `OpenThisPage()` | Opens this page through `UIKit.ShowPage` (also the **Open** button in the Inspector). |
| `HasShowAnimation`, `ShowAnimationDuration`, `HideAnimationDuration` | Animation timing reported to `PM`; `false`/`0` for the base page. Virtual. |
| `WaitForShowAnimation()`, `WaitForHideAnimation()` | Coroutines that wait for those durations (realtime). |

## Custom animated pages

The base page switches instantly. An animated page derives from `UIPage`, plays its tween in an override of
`StartActive()`/`EndActive()` and reports the timing, so `PM` keeps the outgoing page visible until the incoming one
has finished showing:

```csharp
public class FadePage : UIPage
{
    [SerializeField] private float _fade = 0.25f;

    public override bool HasShowAnimation => _fade > 0f;
    public override float ShowAnimationDuration => _fade;

    public override void StartActive()
    {
        base.StartActive();
        // start the fade-in tween here
    }
}
```

## Compatibility

Legacy fields `_playBackward` and `_onlyPlayBackward` are migrated automatically:

- `_onlyPlayBackward = true` → `BackwardOnly`;
- `_playBackward = true` → `ForwardAndBackward`;
- `_playBackward = false` → `ForwardOnly`.

## See Also

- [PM](./PM.md)
- [BtnChangePage](./BtnChangePage.md)



