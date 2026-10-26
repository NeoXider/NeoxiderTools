# RPG condition bridge

`RpgConditionAdapter` exposes RPG predicates through `Neo.Condition.IConditionEvaluator` and Inspector events. This is the integration guide for the optional `Neo.Rpg.ConditionBridge` assembly. See the [RPG overview](README.md) for the character and combat APIs.

## C# API

Custom assemblies that use `RpgConditionAdapter` must reference `Neo.Rpg.ConditionBridge`. Also reference `Neo.Condition` when using `IConditionEvaluator`, as in the example below, and `Neo.Rpg` when using its character or evaluation-mode types. The component remains in the `Neo.Rpg` namespace; the character lives in `Neo.Rpg.Components`. The evaluation-mode enum is provided by the base `Neo.Rpg` assembly.

Given an Inspector-configured `RpgConditionAdapter adapter` and a candidate `GameObject actor`:

```csharp
using Neo.Condition;
using Neo.Rpg;

IConditionEvaluator condition = adapter;
bool passes = condition.Evaluate(actor);
bool localPasses = adapter.EvaluateCurrent();
bool latestResult = adapter.LastResult;
```

`Evaluate(actor)` resolves the character in this order:

1. The explicitly assigned `Character` field.
2. A character on `actor` itself.
3. A character in `actor`'s active parent hierarchy (Unity's default parent lookup).

It does not search children or the whole scene. An absent or destroyed assigned character permits context fallback. A null context still works with an assigned character. `EvaluateCurrent()` uses the adapter's own GameObject as context; repeated calls read current state without caching a previous hierarchy lookup.

`CanEvade` and `AttackReady` query their explicitly assigned controllers. These modes work without a character in the adapter's context hierarchy: each controller applies its own readiness rules. Controllers are not automatically found through the context or character.

The assigned controller must be enabled on an active GameObject. A disabled controller or an inactive hierarchy produces `false` before inversion, even when its cooldown is ready. For C# setup without built-in input, set `EnableBuiltInInput = false`, finish configuration, then activate the controller's hierarchy before checking readiness.

## Predicate configuration

| Mode | Compared value / required configuration |
| --- | --- |
| `HpAtLeast` | Current HP >= `Threshold` |
| `HpPercentAtLeast` | HP percentage >= `Threshold`, expressed as 0-100 |
| `LevelAtLeast` | Character level >= `Level Threshold` |
| `IsDead` | Character death state |
| `HasBuff`, `HasStatus` | Active effect matching `Buff Id` or `Status Id` |
| `CanPerformActions`, `IsInvulnerable` | Character's corresponding state |
| `CanEvade` | Assigned `Evade Controller` reports `CanEvade` |
| `AttackReady` | Assigned `Attack Controller` can use `Attack Id` |
| `ResourceAtLeast`, `ResourceBelow` | Selected `Resource` current value >= or < `Threshold` |
| `ResourcePercentAtLeast`, `ResourcePercentBelow` | Selected resource percentage >= or < `Threshold`, expressed as 0-100 |
| `StatAtLeast`, `StatBelow` | Selected `Stat` value >= or < `Threshold` |
| `UpgradePointsAtLeast` | Free upgrade points >= `Level Threshold` |
| `UpgradeLevelAtLeast` | Selected stat's upgrade count >= `Level Threshold` |
| `XpAtLeast` | Current XP >= `Threshold` |

`AtLeast` includes equality; `Below` excludes it. Percentages use the character's normalized resource value and divide the configured threshold by 100. The adapter does not clamp runtime thresholds.

`Resource` and `Stat` use `RpgStatId`, supporting presets and custom IDs. A nonblank legacy `_resourceId` remains supported: its trimmed value takes precedence over `Resource`. An empty or whitespace-only legacy ID uses the typed field. Missing resource/stat values follow `RpgCharacter`'s zero-value fallback; a missing resource or a resource with nonpositive maximum has zero percent. Such values can satisfy a `Below` or zero-threshold predicate.

## Results and events

For character modes, an unavailable character produces `false` before inversion. For controller modes, an unavailable controller produces `false` before inversion. Unknown evaluation modes also produce `false`. `Invert` negates this result, including a result caused by a missing dependency.

Every completed evaluation, including a missing-dependency result, follows this order:

1. Compute the result and apply `Invert`.
2. Update `LastResult`.
3. Invoke `On Evaluated` with that result.
4. Invoke exactly one of `On True` and `On False`.
5. Return that evaluation's result.

Events fire on every call, including repeated equal results. A listener may start another evaluation: each call retains its own result and matching branch event. `LastResult` holds the most recently computed result, which can belong to a nested call when the outer call returns. Avoid wiring unconditional evaluation loops in event handlers.

## Scene / NoCode workflow

1. Add **Neoxider > RPG > RpgConditionAdapter** to a scene object.
2. Assign `Character` or arrange a character on the evaluation context or a parent.
3. Select the mode and configure its threshold, resource/stat selector or effect ID. For a controller mode, assign an enabled controller on an active GameObject; for `AttackReady`, also set an attack ID registered with that controller.
4. Wire `On Evaluated`, `On True`, and `On False` to scene responses as needed. Calling `EvaluateCurrent()` from its Inspector button or C# evaluates the adapter object's context.
5. To use it through `NeoCondition`, select the adapter as the component source, read its parameterless `EvaluateCurrent()` boolean method, and compare it with `true`. This evaluates the adapter's own context; custom C# consumers can pass another context to `IConditionEvaluator.Evaluate`.

Keep scene references on the component. ScriptableObject assets used by the character and controllers contain reusable configuration and do not need references back to this adapter.
