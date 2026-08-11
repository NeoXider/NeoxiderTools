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
- Tab/category selection with a movable selected marker (CategoryBar)
- Text and value presentation (see Tools/Text)
- Deformable Canvas sprites with Animator-friendly control points (UI Mesh Rig)

## docs (per-component)

| Page | Description |
|------|-------------|
 · Overview
| [UI](./UI.md), [AnchorMove](./AnchorMove.md), [ButtonScale](./ButtonScale.md), [ButtonShake](./ButtonShake.md) | Core UI |
| [VisualToggle](./VisualToggle.md), [VariantView](./VariantView.md), [AnimationFly](./AnimationFly.md) | Toggles and animation |
| [CategoryBar](./CategoryBar.md) | Generic category/tab bar with selection state and marker |
| [PausePage](./PausePage.md), [FakeLoad](./FakeLoad.md) | UI flow helpers |
| [UI Mesh Rig](./UIMeshRig.md) | Editable uGUI mesh deformation, setup/pose tools, Animator workflow |

## See also

- [NeoxiderPages](../NeoxiderPages/README.md) — Page-navigation sample
- [Tools/Text](../Tools/Text/README.md) — Text helpers
