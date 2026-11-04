# RpgEvadeController

**What it is:** an RPG-native evade ability with cooldown and optional invulnerability
(`MonoBehaviour`, `Scripts/Rpg/Components/RpgEvadeController.cs`).

**How to use:**
1. Add it to the actor; assign `Character` when the `RpgCharacter` is not on this object or a parent.
2. Set evade duration, cooldown and `Grant Invulnerability`.
3. Press the binding (LeftShift by default), call `TryStartEvade()`, or use `RpgNoCodeAction` with `StartEvade`.
4. Bind `IsEvadingState` / `RemainingCooldownState` to animation and UI.

**Navigation:** [← RPG](./README.md)

---

## Main use cases

- Roll / dash / dodge
- Invulnerability frames
- No-code evade buttons through `RpgNoCodeAction`

## Built-in input

- Enabled by default.
- Default binding: `LeftShift`.
- Binding can use either `MouseButton` or `KeyCode`.
- Disable `Enable Built-in Input` for NPC and AI actors.

## C# and scene API

Call `TryStartEvade()` and inspect its boolean result; `CanEvade`, `IsEvading`, and
`GetRemainingCooldown()` expose readiness. Scene buttons can call `RpgNoCodeAction.Execute` with
`StartEvade` and an assigned controller. Movement/dash animation is supplied by the scene; this
component owns timings and the optional invulnerability window.

Each evade owns one invulnerability lock on the receiver that granted it. Expiry or disabling the
component/GameObject releases that lock exactly once, preserving locks from shields or other skills.
Changing `Grant Invulnerability` during an active evade does not leak the acquired lock.
Disabling cancels pending completion, clears reactive evading state, and raises the finished event for
an active evade. The accepted cooldown remains; `ResetCooldown()` clears it explicitly.

`IsEvadingState` and `RemainingCooldownState` publish both values once on the first frame after the
component is enabled, so bound UI starts in sync. After that they notify only when their value changes
(every frame while the cooldown counts down, never while idle). Earlier versions re-notified both states
every frame.
