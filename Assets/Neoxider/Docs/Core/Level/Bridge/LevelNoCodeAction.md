# LevelNoCodeAction

**What it is:** a UnityEvent-friendly bridge from `Scripts/Core/Level/Bridge/LevelNoCodeAction.cs` that drives a `LevelComponent` (or any `ILevelProvider`) without writing code.

## Setup

1. Add the component via **Add Component → Neoxider → Core → Level NoCode Action**.
2. Assign `Level Provider`, or leave it empty to use a `LevelComponent` on the same GameObject.
3. Choose an `Action Type` and fill the matching amount.
4. Call `Execute()` from a `Button`, animation event, or any other UnityEvent source.

## Supported actions

| Action Type | Description |
|-------------|-------------|
| `AddXp` | Adds `Xp Amount` XP through `ILevelProvider.AddXp`. |
| `SetLevel` | Sets the level to `Target Level` through `ILevelProvider.SetLevel`. |

## Events

| Event | When it is raised |
|-------|-------------------|
| `OnSuccess` | The configured action ran. |
| `OnLevelUp(int)` | The action changed the provider level (either action can move it); the payload is the new level. |

## API for UnityEvent wiring

- `Execute()` — runs the configured action.
- `SetXpAmount(int)` / `SetTargetLevel(int)` — 1-arg setters so a UnityEvent can supply a value before `Execute()`.
- `XpAmount`, `TargetLevel`, `ActionType`, `LevelProvider` — public accessors for runtime configuration.

## See Also

- [LevelComponent](../Components/LevelComponent.md)
- [Module Root](../../README.md)
