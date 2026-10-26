# ButtonChangePage

`Neo.UI.ButtonChangePage` combines pointer/submit input, optional press feedback, and navigation through
`Neo.UI.UI.I`. It can also serve as an event button when page switching is disabled.

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
wiring. They forward to `UI.I` independently of the click gate. The misspelled `intecactable` field remains
available for serialized compatibility; prefer the `Interactable` property from code.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `OnClick` | On Click. |
| `_idPage` | Id Page. |
| `_imageTarget` | Image Target. |
| `_onePage` | One Page. |
| `_scaleAnim` | Scale Anim. |
| `_timeAnimImage` | Time Anim Image. |
| `_useAnimPage` | Use Anim Page. |
| `intecactable` | Intecactable. |

## See Also

- [Module Root](../README.md)
