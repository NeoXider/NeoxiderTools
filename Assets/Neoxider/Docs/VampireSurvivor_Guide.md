# Building a Vampire Survivors Game

**What it is:** a step-by-step guide for assembling a Vampire-Survivors-style game (auto-firing weapons, endless enemy waves, XP and level-up upgrades) from NeoxiderTools modules. The recommended path is the data-driven [Neo.Abilities](./Abilities/README.md) module (`Scripts/Abilities/`); the [Survivor Demo](./Abilities/SurvivorDemo.md) is the complete reference game built this way.

**How to use:**
1. Open the [Survivor Demo](./Abilities/SurvivorDemo.md) once to see the finished loop.
2. Follow steps 1-6 below to build your own scene from components, or clip the demo by editing its data assets.
3. Before scaling to hundreds of enemies, apply the [performance checklist](#step-6-performance-for-hundreds-of-units).

---

## Contents
- [Step 1: Ability system and data](#step-1-ability-system-and-data)
- [Step 2: Player and auto-firing weapons](#step-2-player-and-auto-firing-weapons)
- [Step 3: Enemies and waves](#step-3-enemies-and-waves)
- [Step 4: Health bars and HUD](#step-4-health-bars-and-hud)
- [Step 5: Experience and level-up upgrades](#step-5-experience-and-level-up-upgrades)
- [Step 6: Performance for hundreds of units](#step-6-performance-for-hundreds-of-units)
- [Alternative: the RPG component path](#alternative-the-rpg-component-path)
- [See also](#see-also)

---

## Step 1: Ability system and data

1. Create the weapons as [AbilityDefinition](./Abilities/AbilityDefinition.md) assets (`Assets > Create > Neoxider > Abilities > Ability`):
   - **Magic bolt** — Targeting `Unit`, Delivery `Projectile`, Projectile Archetype Id `bolt`, Cooldown `0.8`, Impact Effect `damage` 20.
   - **Garlic aura** — Targeting `NoTarget`, Delivery `Instant`, Cooldown `1`, Impact Effect `damage`, Target `AreaAroundCaster`, Team Filter `Enemies`, Radius `2.5`, Amount `8`.
2. Create buffs/debuffs as [ModifierDefinition](./Abilities/ModifierDefinition.md) assets (slows, burns, permanent upgrade modifiers such as `{ outgoing_damage_mul, Mul, 1.2 }`).
3. Put them into an [AbilityLibrary](./Abilities/AbilityLibrary.md).
4. Add an [AbilitySystemBehaviour](./Abilities/AbilitySystemBehaviour.md) to the scene, assign the library under **Libraries**, and bind `{ Id = "bolt", Prefab = <projectile prefab> }` under **Archetypes**. The projectile prefab carries an [AbilityProjectileBehaviour](./Abilities/AbilityProjectileBehaviour.md).

## Step 2: Player and auto-firing weapons

1. Create a [UnitTemplate](./Abilities/UnitTemplate.md) for the player: Team `1`, a `health` pool, base `move_speed`.
2. On the player object add [AbilityUnitBehaviour](./Abilities/AbilityUnitBehaviour.md) (assign the template), [AbilityCasterBehaviour](./Abilities/AbilityCasterBehaviour.md) (drag in the starting weapons) and [AbilityAutoCaster](./Abilities/AbilityAutoCaster.md) (mode `WhenReady`).
3. Move the player with your own controller or a NeoxiderTools mover such as `PlayerController3DPhysics` / `PlayerController2DPhysics`; read `Unit.GetProperty(AbilityProperties.MoveSpeed)` if upgrades should change speed.

Unit-targeted weapons lock onto the nearest enemy, `NoTarget` weapons fire around the player — each one the moment its cooldown ends.

## Step 3: Enemies and waves

1. Create an enemy `UnitTemplate`: Team `2`, a `health` pool, base `move_speed`.
2. Enemy prefab: `AbilityUnitBehaviour` (enemy template) plus a chaser script or `NpcNavigation` for movement. Contact damage can be an ability on a short cooldown, or a direct `DamageService.ApplyDamage` call from your contact script.
3. Spawn enemies with `Spawner` (`Neo.Tools`, **Spawn Mode** `Waves`) or `PoolManager.Get`. Pooled enemies re-register as new domain units on every enable — set the template with `SetTemplate` before activation if one prefab serves several enemy types.
4. React to deaths through the unit's **On Died** event or `System.Events.Subscribe(AbilityEvents.Death, ...)` to drop XP and count kills.

## Step 4: Health bars and HUD

- Read `AbilityUnitBehaviour.HealthNormalized` for bars, or bind it with `SetProgress` (NoCode).
- Ability cooldown pips: [AbilityCooldownSource](./Abilities/AbilityCooldownSource.md) exposes `CooldownNormalized` / `ReadyNormalized` for NoCode bindings.
- World-space bars above enemies: a small canvas per enemy with `BillboardUniversal` (**Billboard Mode** `AwayFromCamera`, **Ignore Y** on) keeps them readable from a top-down camera.

## Step 5: Experience and level-up upgrades

1. Track XP with `LevelComponent` (`Neo.Core.Level`) or `ProgressionManager`; grant XP from the death handler of Step 3.
2. On level-up set `AbilitySystemBehaviour.I.Paused = true` (cooldowns, modifiers and auto-casters freeze) and show the upgrade cards; also pause scene time (`Time.timeScale = 0`) if projectiles and enemy movement must stop.
3. Apply the chosen upgrade from code:
   - new weapon: `system.RegisterAbility(definition.Blueprint); system.GrantAbility(unitId, definition.Id);`
   - weapon level: `system.SetAbilityLevel(unitId, abilityId, level)` with leveled values on the effect nodes;
   - stat upgrade: `system.ApplyModifier(modifierId, unitId, unitId)` with a permanent modifier.
4. Unpause.

The Survivor Demo implements exactly these three upgrade kinds (`PermanentModifier`, `GrantAbility`, `MaxHealth`) — see [Clip your own survivor game](./Abilities/SurvivorDemo.md#clip-your-own-survivor-game).

## Step 6: Performance for hundreds of units

- **Spatial grid.** On the [AbilitySystemBehaviour](./Abilities/AbilitySystemBehaviour.md) set **Spatial Cell Size** to about the typical hit/area radius (for example `3`). Projectile hit tests, area effects, `chain` hops and auto-caster searches then visit only nearby units instead of every registered unit. Knockback and kills update the grid in place, so a crowded frame does not rebuild it before every hit test. See [Spatial queries for many units](./Abilities/README.md#spatial-queries-for-many-units).
- **Pool everything that spawns.** Projectiles already go through `PoolManager`; spawn enemies and XP orbs from pools as well.
- **Prefer modifiers over per-enemy scripts.** DoTs, slows and auras run inside the domain tick without per-frame allocation; hundreds of ticking modifiers cost no garbage.
- **Avoid per-hit allocations in your listeners.** Subscribe once (not per spawn) and avoid string formatting in `take_damage` handlers; the bus itself allocates nothing per event.
- **Retry delays.** Keep **Failed Retry Delay** on auto-casters above zero so out-of-range or out-of-mana weapons do not retry every frame.

## Alternative: the RPG component path

The older `Neo.Rpg` components can also drive a survivor game when you prefer physics-based hits:

- Player: `RpgCharacter` (stats and resources) with `RpgAttackController` + `RpgAutoAttackController`; ranged weapons use an `RpgAttackDefinition` with Delivery `Projectile` (spawns `RpgProjectile`), auras use `AuraWeapon`.
- Enemies: `RpgCharacter`, `NpcNavigation` for chasing, `RpgContactDamage` for touch damage.
- Health UI: `RpgResourceBinding` + `SetProgress`, text via `NoCodeFormattedText`.

The Demo Scenes sample has a reference scene for this path, `Scenes/VampireSurvivorMCP.unity` (see [Sample scenes](./Samples.md#example-games-and-integration-scenes)).

New projects should start from Neo.Abilities: it is data-driven, deterministic, network-ready by id and covered by allocation guards.

## See also
- [Abilities module](./Abilities/README.md)
- [Survivor Demo](./Abilities/SurvivorDemo.md)
- [RPG Module Index](./Rpg/README.md)
- [Tools Module Index](./Tools/README.md)
- [Progression System](./Progression/README.md)
- ← [Back to the main table of contents](./README.md)
