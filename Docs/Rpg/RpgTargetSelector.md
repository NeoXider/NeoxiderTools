# RpgTargetSelector

**What it is:** a reusable `MonoBehaviour` target selector for AI, skill logic and spell casting
(`Scripts/Rpg/Components/RpgTargetSelector.cs`). It searches colliders around an origin with an
`RpgTargetQuery` and picks one `IRpgCombatReceiver`.

**How to use:**
1. Add it to the actor, configure `Query` (range, layers, 2D/3D, filters, selection mode) and optionally
   `Origin` / `Character Source`.
2. Call `SelectTarget()` / `TrySelectTarget(out target)` from code, or press the Inspector button.
3. Assign it on an `RpgAttackController` and use an `RpgAttackPreset` with
   `UseSelectorComponentWhenAvailable` to aim presets automatically.

**Navigation:** [← RPG](./README.md)

---

## Supports

- `Nearest`
- `Farthest`
- `LowestCurrentHp`
- `HighestCurrentHp`
- `LowestHpPercent`
- `HighestLevel`
- `Random`

## Inspector testing

- `SelectTarget()` has `[Button]`
- `ClearTarget()` has `[Button]`

## Runtime behavior

`SelectTarget()` returns the selected collider's GameObject; `TrySelectTarget(out target)` returns
whether selection succeeded. Candidates use `IRpgCombatReceiver`, including custom implementations.
With `Ignore Self`, all colliders resolving to the source receiver are excluded, including child
hurtboxes. Multiple colliders resolving to the same receiver contribute only one candidate.

Selection reuses shared physics/candidate buffers and allocates nothing once warm. The query can be
changed from code:

```csharp
selector.Query.SelectionMode = RpgTargetSelectionMode.LowestHpPercent;
selector.Query.Range = 12f;
selector.Query.Use2D = false;
GameObject target = selector.SelectTarget();
```

Assign `Character Source` when the query origin is outside the character hierarchy. In scene workflows,
connect the selected-target event to the consuming AI/skill view, or assign the selector on an attack
controller and use a preset. `ClearTarget()` releases the selection and resets `HasTargetState`.
