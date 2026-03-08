# UI module

Reusable UI helpers: pages, buttons, animations, toggles, and presentation. Scripts in `Scripts/UI/`. Full per-component docs are in Russian.

## Entry pages

| Page | Description |
|------|-------------|
| [UI](./UI.md) | Page manager, switching modes, and events |
| [Russian UI docs](../../Docs/UI/README.md) | Full Russian per-component documentation |

## Typical use cases

- Button press feedback and simple UI animation (ButtonScale, ButtonShake)
- Page/state transitions (UI, ButtonChangePage)
- Toggle-style state visualization (VisualToggle, VariantView)
- Text and value presentation (see Tools/Text)

## Russian docs (per-component)

| Page | Description |
|------|-------------|
| [UI README](../../Docs/UI/README.md) | Overview |
| [UI](../../Docs/UI/UI.md), [AnchorMove](../../Docs/UI/AnchorMove.md), [ButtonScale](../../Docs/UI/ButtonScale.md), [ButtonShake](../../Docs/UI/ButtonShake.md) | Core UI |
| [VisualToggle](../../Docs/UI/VisualToggle.md), [VariantView](../../Docs/UI/VariantView.md), [AnimationFly](../../Docs/UI/AnimationFly.md) | Toggles and animation |
| [PausePage](../../Docs/UI/PausePage.md), [FakeLoad](../../Docs/UI/FakeLoad.md), [UIReady](../../Docs/UI/UIReady.md) | Flow helpers (`UIReady` is deprecated; use `SceneFlowController`) |

## See also

- [NeoxiderPages](../NeoxiderPages/README.md) — Page-navigation sample
- [Tools/Text](../Tools/Text/README.md) — Text helpers
