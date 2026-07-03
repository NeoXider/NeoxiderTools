# Npc Combat Scenarios

**What it is:** ready-made recipes for building automatic NPCs based on `NpcNavigation`, `NpcRpgCombatBrain`, `RpgTargetSelector`, and `RpgAttackController`.

This page serves as a practical cookbook: what to add to the object, what to configure, and how to get the enemy type you need without a separate large script.

---

## Common Base for All Scenarios

A basic combat NPC usually gets:

1. `NavMeshAgent`
2. `NpcNavigation`
3. `NpcAnimatorDriver` if needed
4. `RpgCharacter`
5. `RpgTargetSelector`
6. `RpgAttackController`
7. `NpcRpgCombatBrain`

Asset configs:

1. `RpgAttackDefinition`
2. `RpgAttackPreset`
3. `NpcCombatPreset`

Overall flow:

1. `RpgTargetSelector` finds a target
2. `NpcRpgCombatBrain` decides whether to chase the target or whether it can already attack
3. `NpcNavigation` brings the NPC to the required distance
4. `RpgAttackController` executes the attack according to the preset

## Scenario 1. Automatic Melee NPC

Suitable for enemies that should walk up to the player on their own and hit at close range.

### Component setup

- `NpcNavigation`
- The mode can be left as `Patrol` or `Combined` if the NPC should walk between points first
- `RpgAttackController`
- `Enable Built-in Input`: `false`
- `NpcRpgCombatBrain`
- `Auto Acquire Target`: `true`

### `RpgAttackDefinition` setup

- `DeliveryType`: `Direct` or `Area`
- `HitMode`: `Damage`
- `Range`: `2 - 3`
- `Radius`: `0.25 - 1.25`
- `Cooldown`: as balance requires
- `TargetLayers`: the player/target layer

### `RpgAttackPreset` setup

- `RequireTarget`: `true`
- `UseSelectorComponentWhenAvailable`: `true`
- `AimAtTarget`: `true`
- `TargetQuery.SelectionMode`: usually `Nearest`
- `TargetQuery.Range`: slightly more than the aggro distance, e.g. `10 - 15`

### `NpcCombatPreset` setup

- `Preferred Attack Distance`: `1.5 - 2.2`
- `Lose Target Distance`: `10 - 15`
- `Run While Chasing`: `true`
- `Stop Movement Inside Attack Range`: `true`
- `Face Target Before Attack`: `true`

### Result

The NPC will find the nearest target on its own, run up, stop at close range, and start attacking automatically on cooldown.

## Scenario 2. Automatic Ranged NPC

Suitable for archers, shooters, mages, and enemies with a projectile attack.

### `RpgAttackDefinition` setup

- `DeliveryType`: `Projectile`
- `ProjectilePrefab`: assign a prefab with `RpgProjectile`
- `Range`: `8 - 20`
- `Radius`: `0` or a small splash
- `Cooldown`: as balance requires
- `ProjectileSpeed`: depending on the weapon type
- `TargetLayers`: the player/target layer

### `RpgAttackPreset` setup

- `RequireTarget`: `true`
- `AimAtTarget`: `true`
- `TargetQuery.SelectionMode`: usually `Nearest`
- `TargetQuery.Range`: greater than or equal to the attack's working distance

### `NpcCombatPreset` setup

- `Preferred Attack Distance`: `6 - 12`
- `Lose Target Distance`: `15 - 25`
- `Run While Chasing`: `true`
- `Stop Movement Inside Attack Range`: `true`
- `Face Target Before Attack`: `true`

### Result

The NPC will only approach to the required distance, then stop and attack the target from afar, without ramming into it like a melee enemy.

## Scenario 3. Patrol -> Aggro -> Attack

Suitable for enemies that patrol an area and enter combat when the player appears.

### `NpcNavigation` setup

- `Mode`: `Combined`
- `Patrol Points` or `Patrol Zone`: assign
- `Combined Target`: can be left empty if combat will get its target through `RpgTargetSelector`

### `NpcCombatPreset` setup

- `Auto Restore Navigation Mode`: `true`

### Result

The NPC patrols, then `NpcRpgCombatBrain` switches it into the combat follow/attack flow. When the target is lost, the previous navigation mode is restored and the NPC returns to its patrol behavior.

## Scenario 4. Stationary Turret / Mage

Suitable for turrets, casters, or guards that don't need to move toward the target.

### `NpcCombatPreset` setup

- `Preferred Attack Distance`: to match the ability's range
- `Lose Target Distance`: to match the area of interest
- `Run While Chasing`: `false`
- `Stop Movement Inside Attack Range`: `true`

### Additionally

- If no movement is needed at all, don't give the NPC a scenario that requires chasing the target
- You can use a large `TargetQuery.Range` so the turret picks a target within its zone

### Result

The NPC stays in place, automatically selects a target, and attacks without movement-heavy behavior.

## Scenario 5. Prioritizing the Weakest/Most Wounded

Suitable for skills, healers, support NPCs, and special enemies.

### `RpgTargetSelector` setup

In `RpgTargetQuery` you can choose:

- `LowestCurrentHp`
- `LowestHpPercent`
- `HighestLevel`
- `Random`

### Result

The behavior changes without a new script. The same `NpcRpgCombatBrain` keeps working, but the target is selected using a different strategy.

## Practical Tips

- For NPCs, almost always disable `Enable Built-in Input` on `RpgAttackController`
- For melee and ranged NPCs, tweak `RpgAttackDefinition`, `RpgAttackPreset`, and `NpcCombatPreset` first, not the code
- If an NPC needs quick testing in the Inspector, use the `[Button]` methods `SelectTarget()`, `EvaluateNow()`, and `ForceAttack()`
- If you need even more logic, it's better to add new small components on top of the brain/preset rather than growing one giant controller
