# NpcRpgCombatBrain

**What it is:** a `MonoBehaviour` (`Scripts/NPC/Combat/NpcRpgCombatBrain.cs`, namespace `Neo.NPC.Combat`) that
composes `NpcNavigation`, `RpgTargetSelector`, and `RpgAttackController` into an automatic combat NPC. Each
`Decision Interval` it evaluates a stateless decision (acquire / clear / chase / hold / attack) via
`NpcCombatDecisionCore` and drives the other components accordingly.

**How to use:**
1. Put it on an NPC with `NpcNavigation`, `RpgTargetSelector`, `RpgAttackController` (and optionally `RpgCharacter`).
2. Assign an `NpcCombatPreset`.
3. React through its events (Inspector or the `On*` properties from C#).

> Legacy: this brain predates `Neo.Abilities`, which supersedes it for new projects. It is kept working for existing RPG-preset setups; prefer the ability system for new combat.

---

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
| `_character` | (this GO) | `RpgCharacter` (`IRpgCombatReceiver`) queried for `CanPerformActions`. Without it, any `IRpgCombatReceiver` on the GameObject is used; a found receiver is cached, a missing one is looked up again on later decisions (so a receiver added after `Awake` still gates actions). |
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
- Read-only: `CurrentTarget`, `HasTarget`, `CurrentDecision` (last `NpcCombatDecisionCore.Decision`, null before the first evaluation).
- `IsActive` (get/set) — master switch of the decision loop; turning it off does not clear the target.
- `Preset` (get/set) — swap the `NpcCombatPreset` at runtime.
- Events for C#: `OnTargetAcquired`, `OnTargetLost`, `OnChaseStarted`, `OnHoldingPosition`, `OnAttackTriggered`,
  `OnAttackFailed`, `OnDecisionChanged` return the same UnityEvents the Inspector shows.

```csharp
NpcRpgCombatBrain brain = GetComponent<NpcRpgCombatBrain>();
brain.OnChaseStarted.AddListener(PlayAggroBark);
brain.OnAttackFailed.AddListener(reason => Debug.Log(reason));
brain.Preset = enragedPreset;
```

## Chase continuation

The decision runs every `Decision Interval`, but a continuing chase only fixes what drifted: the follow target is
re-assigned only when it differs, the mode only when it is not `FollowTarget`, and `Resume` only when navigation is
paused. `SetRunning` and `OnChaseStarted` run once when a chase starts (after a hold/attack/clear or for a different
target). Before this, every tick re-assigned the same target, which restarted the NavMesh path and raised
`onTargetChanged` / `onSpeedChanged` / `OnChaseStarted` again. `OnHoldingPosition` still fires on each holding tick.

Re-path cadence of a continuing chase:

- **`Auto Update Path` on (default):** `NpcNavigation` re-paths toward the moving target every `Path Update Interval`
  (0.5 s by default, staggered across NPCs), no longer every `Decision Interval`. Lower `Path Update Interval` for
  faster pursuit.
- **`Auto Update Path` off:** `NpcNavigation` never re-paths on its own, so the brain calls
  `NpcNavigation.RefreshPath()` on each decision where the target moved.
- A chase restarted after a hold also refreshes the path immediately, so the NPC does not resume walking toward
  where the target stood before the hold.

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

| Inspector field | C# property | When |
|-----------------|-------------|------|
| `_onTargetAcquired(GameObject)` | `OnTargetAcquired` | A new target was selected. |
| `_onTargetLost(GameObject)` | `OnTargetLost` | The previous target was cleared. |
| `_onChaseStarted` | `OnChaseStarted` | A chase started (once per chase / target). |
| `_onHoldingPosition` | `OnHoldingPosition` | Each decision tick that holds position. |
| `_onAttackTriggered` | `OnAttackTriggered` | The attack controller accepted the preset attack. |
| `_onAttackFailed(string)` | `OnAttackFailed` | An attack attempt was rejected (reason). |
| `_onDecisionChanged(string)` | `OnDecisionChanged` | The decision changed (readable summary). |

## See Also

- [Module Root](../README.md)
- [NpcCombatPreset](./NpcCombatPreset.md)
- [NpcCombatScenarios](./NpcCombatScenarios.md)
