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

## Public API (buttons)

- `AutoResolveReferences()` — fill missing component references.
- `EvaluateNow()` — force one immediate evaluation.
- `AcquireTarget()` — select a target via the selector (returns the `GameObject`).
- `ClearCombatTarget()` — clear the target and restore navigation mode.
- `ForceAttack()` — try the preset attack against the current target (returns success).
- Read-only: `CurrentTarget`, `HasTarget`.

## Events

`_onTargetAcquired(GameObject)`, `_onTargetLost(GameObject)`, `_onChaseStarted`, `_onHoldingPosition`, `_onAttackTriggered`, `_onAttackFailed(string)`, `_onDecisionChanged(string)`.

## See Also

- [Module Root](../README.md)
- [NpcCombatPreset](./NpcCombatPreset.md)
- [NpcCombatScenarios](./NpcCombatScenarios.md)
