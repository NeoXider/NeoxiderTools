# NeoCharacterSprint

**What it is:** A `MonoBehaviour` that adds sprint to CMF's `AdvancedWalkerController`, which ships with a single movement speed. It reads `NeoCharacterInput.IsRunHeld` and swaps the controller's speed while sprint is held. `Scripts/Tools/Move/CharacterController/NeoCharacterSprint.cs`, namespace `Neo.Tools`.

**How to use:**
1. Add it to the character root next to `AdvancedWalkerController` and [NeoCharacterInput](./NeoCharacterInput.md) — both references are resolved automatically in `Awake`.
2. Set the walk speed on `AdvancedWalkerController.movementSpeed` as usual; this component scales from it.
3. Tune `Sprint Speed Multiplier`, and `Speed Lerp Rate` if you want the transition to ramp instead of snap.

---

## Fields

### References

| Field | Type | Purpose |
|-------|------|---------|
| `Controller` | `AdvancedWalkerController` | Auto-found on this GameObject. |
| `Input` | `NeoCharacterInput` | Auto-found on this GameObject. Sprint never engages when missing. |

### Speed

| Field | Type | Purpose |
|-------|------|---------|
| `Sprint Speed Multiplier` | `float`, min 1 | Multiplies the authored walk speed while sprinting. Default `1.7`. |
| `Speed Lerp Rate` | `float`, min 0 | Speed change per second. `0` (default) switches instantly. |
| `Require Grounded` | `bool` | Only sprint while grounded. On by default, so a sprint press mid-air does not add air speed. |

### Events

| Event | Fires |
|-------|-------|
| `On Sprint Start` | The frame sprint becomes active. |
| `On Sprint Stop` | The frame sprint stops. |

Both are parameterless `UnityEvent`s — useful for footstep audio rate, FOV kick or a stamina drain.

---

## Properties

| Property | Type | Meaning |
|----------|------|---------|
| `IsSprinting` | `bool` | Whether sprint is currently applied. |
| `WalkSpeed` | `float` | The walk speed captured from the controller in `Awake`. |
| `SprintSpeed` | `float` | `WalkSpeed * Sprint Speed Multiplier`. |

## Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `RefreshWalkSpeed()` | `void` | Re-reads the walk speed from the controller. Call after changing `AdvancedWalkerController.movementSpeed` at runtime so sprint scales from the new value. |
| `SetWalkSpeed(float)` | `void` | Overrides the walk speed sprint scales from. Negative values are clamped to `0`. |

---

## Where the walk speed lives

`AdvancedWalkerController.movementSpeed` stays the single source of truth for walking: it is captured once in `Awake` and this component writes back to that same field every frame. Two consequences:

- Changing `movementSpeed` from other code at runtime (a slow debuff, a swimming state) is overwritten on the next `Update` unless you go through `SetWalkSpeed` or call `RefreshWalkSpeed` afterwards.
- Removing or disabling this component leaves the controller at whatever speed was last written. Call `SetWalkSpeed`/`RefreshWalkSpeed`, or disable it while not sprinting.

## See also

- [CharacterController overview](./README.md)
- [NeoCharacterInput](./NeoCharacterInput.md)
