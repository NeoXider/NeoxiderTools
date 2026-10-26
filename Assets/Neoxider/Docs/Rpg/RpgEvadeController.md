# RpgEvadeController

**What it is:** an RPG-native evade ability with cooldown and optional invulnerability.

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
