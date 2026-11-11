# ButtonChangePage

**What it is:** `Neo.UI.ButtonChangePage` (`Scripts/UI/Simple/ButtonChangePage.cs`) is a `MonoBehaviour` that
combines pointer/submit input, optional press feedback, and navigation through the [UI](./UI.md) page manager
(`Neo.UI.UI.I`). It can also serve as an event button when page switching is disabled.

**How to use:**
1. Add the component to a button object (a same-object `Button`/`Selectable` is optional).
2. Set `_idPage` and the switching mode; wire extra actions into `OnClick`.
3. From code call `ExecuteClick()` or the direct `SetPage(int)` / `SetOnePage(int)` / `SetPageAnim(int)`.

---

## Setup

1. Add `Neoxider > UI > ButtonChangePage` to the button object.
2. Optionally use a same-object `Selectable` (such as `Button`); it is resolved in `Awake`.
3. Enable `_canSwitchPage` and choose `_idPage`, `_onePage`, and `_useAnimPage` for navigation.
4. Configure `OnClick` for additional actions. It is emitted after an accepted click/navigation request.

`ExecuteClick()` is the C# entry point shared by left pointer clicks and submit input. `Interactable`
combines the legacy `intecactable` switch with the linked Selectable's effective state, including parent
`CanvasGroup.interactable`. A group that disables interaction also blocks `ExecuteClick` and press feedback.

```csharp
button.Interactable = true;
button.ExecuteClick();
```

`SetPage(int)`, `SetOnePage(int)`, and `SetPageAnim(int)` are direct navigation methods for C# or UnityEvent
wiring. They forward to `UI.I` independently of the click gate and do nothing while no page manager exists. The misspelled `intecactable` field remains
available for serialized compatibility; prefer the `Interactable` property from code.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `intecactable` | Legacy click switch (misspelled for serialized compatibility); use the `Interactable` property from code. |
| `_imageTarget` | Optional Image reference; resolved from the same object when empty or destroyed. |
| `_selectable` | Optional Selectable whose effective interactability gates clicks; resolved from the same object when empty or destroyed. |
| `_canSwitchPage` | When off, the component is a plain event button. |
| `_idPage` | Page index passed to `UI.I`. |
| `_onePage` | Use `SetOnePage` (re-enable one page) instead of `SetPage`. |
| `_useAnimPage` | Use the animated `SetPageAnim` transition. Its delays follow the page manager's `Use Unscaled Time` (default on), so it also works from a paused menu. |
| `_useAnimImage` | Scale the button down while pressed (DOTween, unscaled time). |
| `_timeAnimImage` | Press/release tween length in seconds. |
| `_scaleAnim` | Relative press scale offset (`-0.15` = 85% of the base scale captured in `Awake`, applied to every axis, so a non-uniform scale keeps its proportions). |
| `OnClick` | Raised after an accepted click or submit. |

## See Also

- [Module Root](../README.md)
