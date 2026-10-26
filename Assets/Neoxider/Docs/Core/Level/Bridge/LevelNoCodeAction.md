# LevelNoCodeAction

**What it is:** a UnityEvent-friendly bridge in `Neo.Core.Level.Bridge` (namespace `Neo.Core.Level`) that drives a `LevelComponent` through its `ILevelProvider` runtime API.

## Setup

1. Add the component via **Add Component → Neoxider → Core → Level NoCode Action**.
2. Assign `Level Provider`, or leave it empty to use a `LevelComponent` on the same GameObject.
3. Choose an `Action Type` and fill the matching amount.
4. Call `Execute()` from a `Button`, animation event, or any other UnityEvent source.

The explicit reference takes priority over the sibling fallback. Resolution runs on every execution, including after a provider is added or an explicit reference is destroyed. It does not search parents, children, or the scene, and does not resolve arbitrary `ILevelProvider` implementations.

Execution is explicit: the component does not invoke an action automatically in Start or Update. A [NeoCondition](../../../Condition/NeoCondition.md) `OnTrue` event can call `Execute()`; configure that condition against the provider's value members as described in [LevelConditionAdapter](LevelConditionAdapter.md#scene-and-nocode-conditions).

## Supported actions

| Action Type | Description |
|-------------|-------------|
| `AddXp` | Adds `Xp Amount` XP through `ILevelProvider.AddXp`. |
| `SetLevel` | Sets the level to `Target Level` through `ILevelProvider.SetLevel`. |

## Events

| Event | When it is raised |
|-------|-------------------|
| `OnSuccess` | A supported action ran on a resolved provider, including an action that left the level unchanged. |
| `OnLevelUp(int)` | After `OnSuccess`, if the action changed the provider level. The payload is the resulting level captured immediately after the provider call. |

`OnLevelUp` retains its existing name and also reports downward changes made by `SetLevel`. An `OnSuccess` listener may change the provider again; that later change does not suppress, replace, or create the level event for the completed action. If an `OnSuccess` listener executes another action synchronously, that nested execution completes its own events before the outer level event.

Missing providers and unknown action values produce no events. A zero XP amount still counts as a successful invocation when a provider is present. Exceptions thrown by provider or UnityEvent callbacks propagate to the caller.

## API for UnityEvent wiring

- `Execute()` — runs the configured action.
- `SetXpAmount(int)` / `SetTargetLevel(int)` — 1-arg setters so a UnityEvent can supply a value before `Execute()`.
- `XpAmount`, `TargetLevel`, `ActionType`, `LevelProvider` — public accessors for runtime configuration.

`XpAmount`/`SetXpAmount` clamp negative values to zero. `TargetLevel`/`SetTargetLevel` clamp values below one to one. The provider applies its own level cap and XP curve; an event reports the resolved level, which may differ from the requested level.

## C# usage

```csharp
using Neo.Core.Level;

// action and playerLevel are existing scene references.
action.LevelProvider = playerLevel;
action.ActionType = LevelNoCodeActionType.AddXp;
action.XpAmount = 250;
action.Execute();
```

For gameplay code that does not need bridge events, call `ILevelProvider.AddXp(amount)` or `SetLevel(level)` directly. The bridge owns no subscriptions or persistent resources; listeners registered by other code remain that caller's responsibility.

## See Also

- [Level bridges](README.md)
- [LevelConditionAdapter](LevelConditionAdapter.md)
- [LevelComponent](../Components/LevelComponent.md)
- [Core module](../../README.md)
