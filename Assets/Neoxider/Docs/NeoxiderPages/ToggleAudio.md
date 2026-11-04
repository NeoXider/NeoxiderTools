# ToggleAudio (NeoxiderPages)

**What it is:** links a UI toggle (`VisualToggle`) to the music or sound-effects switch of `AMSettings`. Script `Samples~/NeoxiderPages/Runtime/Scripts/Core/ToggleAudio.cs`, namespace `Neo.Pages`.

**How to use:**
1. Keep `AM` and [AMSettings](../Audio/AMSettings.md) in the scene (for example through the system prefab).
2. **Add Component → Neoxider → Pages → ToggleAudio** next to a `VisualToggle` (the field fills itself in the editor).
3. Tick **Is Music** for the music switch; leave it off for sound effects.

---

## Fields

- **UI Toggle View** — reference to the `VisualToggle`.
- **Is Music** — control music (`true`) or sound effects (`false`).

## Behaviour

From `Start` on (no `GM` needed), a toggle change calls `AMSettings.SetMusic(bool)` or `SetEfx(bool)` through the `UIKit` `Audio` facade. The toggle is synced from the settings before the listener is attached, so its initial state never overwrites the saved choice. Every later enable refreshes the toggle from `AMSettings`; without `AMSettings` in the scene the toggle is left as it is. With **Persist Mute** on in `AMSettings` (the default), the player's choice is saved and restored on the next launch.

## See also

- [AMSettings](../Audio/AMSettings.md)
- [NeoxiderPages README](./README.md)
