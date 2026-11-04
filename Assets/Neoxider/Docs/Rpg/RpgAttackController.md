# RpgAttackController

**What it is:** a `MonoBehaviour` (`Scripts/Rpg/Components/RpgAttackController.cs`, namespace `Neo.Rpg`) that
casts melee, ranged and area attacks described by `RpgAttackDefinition`, with costs, cooldowns, cast delay,
presets and optional built-in input.

**How to use:**
1. Add it to the actor, fill `Attacks` (and optionally `Presets`), assign `Character Source` when the
   `RpgCharacter` is not on this object or a parent.
2. Call `UsePrimaryAttack()`, `TryUseAttack(id, out reason)` or `TryUsePreset(...)` from code, UnityEvents,
   `RpgAutoAttackController` or `RpgNoCodeAction`.
3. Disable `Enable Built-in Input` on NPCs; keep it for a player who attacks with the mouse/key binding.

**Navigation:** [← RPG](./README.md)

---

## Supported delivery types

| Mode | Runtime behavior |
|------|------------------|
| `Direct` | Raycast (radius 0) or SphereCast / CircleCast forward; hits are applied nearest first |
| `Area` | OverlapSphere / OverlapCircle at the impact point |
| `Projectile` | Spawns `RpgProjectile` with the same definition |

`MaxTargets` caps receivers per cast; with casts the nearest receivers win (3D results are sorted by
distance, 2D results are sorted the same way). Queries reuse per-controller buffers that grow instead of
truncating, so warm casts allocate nothing.

## API

- `UsePrimaryAttack()`
- `UsePrimaryPreset()`
- `TryUseAttack(...)`
- `TryUsePreset(...)`
- `CanUseAttack(...)` / `CanUsePreset(...)`
- `GetRemainingCooldown(...)`
- `GetEffectiveCooldown(RpgAttackDefinition)` - cooldown the next use would start
- `ScaleCooldownByAttackSpeed` - see below
- `IsCasting`, `EnableBuiltInInput`

## Attack speed

Enable **Scale Cooldown By Attack Speed** (`ScaleCooldownByAttackSpeed`) to divide each new cooldown by the
source `RpgCharacter.GetAttackSpeedMultiplier()` (`AttackSpeedPercent` buffs; +100% halves the cooldown,
the multiplier is floored at 0.01). Cooldowns already running are not rescaled. The option is off by default.

```csharp
attackController.ScaleCooldownByAttackSpeed = true;
hero.ApplyBuffById("Frenzy"); // AttackSpeedPercent = 50 -> next cooldown is 1/1.5 of the authored value
```

## Built-in input

- Enabled by default. The press is checked first; network authority is only resolved on a press.
- Primary attack uses left mouse button by default.
- Binding can use either `MouseButton` or `KeyCode`.
- Disable `Enable Built-in Input` for NPC and AI actors.

## AI / skills / spells

Use `RpgAttackPreset` together with `RpgTargetSelector` when the controller should resolve targets automatically instead of relying only on forward casts.

## Runtime contracts

- When `RpgAttackPreset.RequireTarget` is enabled, target resolution happens before `CostAmount` is spent, so a failed target lookup does not consume resources.
- Direct and area attacks resolve targets through `IRpgCombatReceiver` and deduplicate hits by receiver within one physics query, even when a target has multiple child colliders.
- Projectile prefab and network spawn authority are checked before spending resources or starting cooldown.
- Inactive controllers reject casts. Disabling the component or its GameObject cancels a pending cast
  and clears `IsCasting`; reactivation permits another cast after the existing cooldown expires.
  Cancellation retains the cost and cooldown already accepted when the cast began.
- `OnAttackStarted` runs while the caster is busy, so a listener cannot recursively start another cast.
  A listener that deactivates the caster also cancels its pending resolution.

For C#, use `CanUseAttack(definition, out reason)` or `TryUsePreset(preset, target, out reason)`.
For scene buttons, select an attack/preset in `RpgNoCodeAction`, assign this controller, and wire
`Execute()`. Rejections are available through the bridge's `OnFailed` and the controller's failure event.
