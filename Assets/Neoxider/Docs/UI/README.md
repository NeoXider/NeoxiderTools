# UI module

Reusable UI helpers: pages, buttons, animations, toggles, and presentation. Scripts in `Scripts/UI/`. Full per-component pages are linked below.

Scene loading, Quit/Restart/Pause, and progress UI live in the Level module: use `SceneFlowController`.

## Entry pages

| Page | Description |
|------|-------------|
| [UI](./UI.md) | Page manager, switching modes, and events |

## Typical use cases

- Button press feedback and simple UI animation (ButtonScale, ButtonShake)
- Page/state transitions (UI, ButtonChangePage)
- Toggle-style state visualization (VisualToggle, VariantView)
- Text and value presentation (see Tools/Text)

## docs (per-component)

| Page | Description |
|------|-------------|
 · Overview
| [UI](./UI.md), [AnchorMove](./AnchorMove.md), [ButtonScale](./ButtonScale.md), [ButtonShake](./ButtonShake.md) | Core UI |
| [VisualToggle](./VisualToggle.md), [VariantView](./VariantView.md), [AnimationFly](./AnimationFly.md) | Toggles and animation |
| [PausePage](./PausePage.md), [FakeLoad](./FakeLoad.md) | UI flow helpers |

## See also

- [NeoxiderPages](../NeoxiderPages/README.md) — Page-navigation sample
- [Tools/Text](../Tools/Text/README.md) — Text helpers
