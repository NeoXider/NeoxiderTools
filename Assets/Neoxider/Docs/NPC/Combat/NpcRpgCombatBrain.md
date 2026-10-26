# NpcRpgCombatBrain

**Purpose:** Composes `NpcNavigation`, `RpgTargetSelector`, and `RpgAttackController` into an automatic combat NPC. Each `Decision Interval` it evaluates a stateless decision (acquire / clear / chase / hold / attack) via `NpcCombatDecisionCore` and drives the other components accordingly.

> Legacy: this brain predates `Neo.Abilities`, which supersedes it for new projects. It is kept working for existing RPG-preset setups; prefer the ability system for new combat.

## Setup

- Add via `Add Component -> Neoxider/NPC/Combat/NpcRpgCombatBrain` on an NPC that also has `NpcNavigation`, `RpgTargetSelector`, `RpgAttackController`, and (optionally) `RpgCharacter`.
- Assign an `NpcCombatPreset` (which references an `RpgAttackPreset`).
- References are auto-resolved from the same GameObject in `Awake`; use the `AutoResolveReferences` button to re-resolve in the editor.

## Key Fields (Inspector)

| Field | Default | Description |
|-------|---------|-------------|
| `_isActive` | `true` | Master switch for the decision loop. |
| `_preset` | none | `NpcCombatPreset` driving distances and attack behaviour. |
| `_navigation` | (this GO) | `NpcNavigation` used for chasing. |
| `_targetSelector` | (this GO) | `RpgTargetSelector` used to acquire targets. |
| `_attackController` | (this GO) | `RpgAttackController` that executes the attack preset. |
| `_character` | (this GO) | `RpgCharacter` (`IRpgCombatReceiver`) queried for `CanPerformActions`. |
| `_lookOrigin` | this transform | Transform rotated to face the target before attacking. |
| `_autoAcquireTarget` | `true` | Acquire a target automatically when none is set. |
| `_disableAttackControllerInput` | `true` | Turns off the attack controller's built-in input so only the brain fires attacks. |
| `_clearTargetOnDisable` | `true` | Clear the target (and restore nav mode) when the brain is disabled. |
| `_decisionInterval` | `0.15` | Seconds between brain evaluations. |

## Public API

- `AutoResolveReferences()` — fill missing component references, including references to destroyed components that have been replaced on the same GameObject.
- `EvaluateNow()` — force one immediate evaluation.
- `AcquireTarget()` — select a target via the selector (returns the `GameObject`).
- `ClearCombatTarget()` — clear the target and restore captured navigation mode, follow target, and active state when automatic restoration is enabled.
- `ForceAttack()` — try the preset attack against the current target (returns success); missing controller/preset dependencies return false and emit `_onAttackFailed`.
- Read-only: `CurrentTarget`, `HasTarget`.

`AutoResolveReferences` is an edit-safe authoring button. The other action buttons are enabled only
in Play Mode. Their typed C# methods are still callable directly; button gating does not replace
application-level lifecycle control.

## Navigation restoration

With `Auto Restore Navigation Mode` enabled, the brain captures navigation before its first chase
or movement stop. This also covers targets acquired directly inside attack range. Clearing the target
or disabling the brain restores the captured mode, original follow target, and previous `IsActive`
value, so a previously paused NPC stays paused. A capture already made is restored even if the preset
reference changes before clearing. Run/speed settings and patrol progress are not part of this snapshot.

```csharp
NpcRpgCombatBrain brain = GetComponent<NpcRpgCombatBrain>();
brain.AutoResolveReferences();
brain.EvaluateNow();
// Later, leave combat and restore the captured navigation settings.
brain.ClearCombatTarget();
```

Use `Neo.NPC.Combat` for the brain type. For Inspector assembly, assign the preset and scene components,
then use the target/decision/attack events for presentation or additional typed component actions.

## Events

`_onTargetAcquired(GameObject)`, `_onTargetLost(GameObject)`, `_onChaseStarted`, `_onHoldingPosition`, `_onAttackTriggered`, `_onAttackFailed(string)`, `_onDecisionChanged(string)`.

## See Also

- [Module Root](../README.md)
- [NpcCombatPreset](./NpcCombatPreset.md)
- [NpcCombatScenarios](./NpcCombatScenarios.md)
