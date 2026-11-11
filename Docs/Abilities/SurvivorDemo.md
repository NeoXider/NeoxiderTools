# Survivor Demo

**What it is:** a complete Vampire-Survivors-style 2D game built only from library components and UnityEvents, with no game scripts, like the Shooter2D reference game. You move a wizard through endless waves while seven weapons fire by themselves; kills drop XP gems, each level-up offers three cards, a boss arrives at 2:00 and surviving 3:00 wins. Sprites, animations, particles, sounds and music are wired the same way, and the same modules work in 3D.

**How to use:**
1. Download the Demo `.unitypackage` (`Assets/NeoxiderDemos/Demo`).
2. Open `Assets/NeoxiderDemos/Demo/Scenes/SurvivorDemo.unity` and press Play.
3. Read the wiring in the Hierarchy and Inspector: everything is a saved scene object, visible without Play Mode.
4. Change the game by editing the scene and the assets in `Survivor/Data`. There is no scene-generation script.

---

## How to play

- **WASD / arrows** move. Weapons fire by themselves.
- **XP gems** fly to you once you are within the pickup radius; each is 1 XP.
- On **level-up** the game pauses and shows three different cards; click one. A weapon card gives the weapon, and picking it again levels it up, to 5. The other cards are permanent bonuses.
- The bar at the bottom left shows every weapon with its level and cooldown.
- The timer counts to **3:00**: survive it for **VICTORY**; dying shows **YOU FELL**. **Play again** restarts.
- The top-right **+10 XP** button opens a level-up at once; **Restart run** reloads the scene.

## The weapons: seven behaviours from one constructor

Every weapon is an ability asset in `Survivor/Data`, plus a template in the scene when it has a body. None has code of its own.

| Weapon | Behaviour | How the ability is built | What levels change |
|--------|-----------|--------------------------|--------------------|
| **Magic Bolt** (start) | Homing bolts at the nearest foes. | Unit target; a `projectile` effect with the `AreaAroundCaster` selector launches one bolt per selected foe, and the payload `bolt_hit` deals the damage. | Damage, cooldown and **Max Targets By Level**: 1, 1, 2, 2, 3 bolts. |
| **Holy Aura** | A ring around the hero that burns and pushes back. | No target; a `zone` cast effect with the `Caster` selector follows the hero and pulses `aura_tick` (damage + `knockback`) every 0.5 s. The zone lasts as long as the cooldown, so it never drops. | Radius and damage. |
| **Spinning Blades** | Three blades orbit the hero. | No target, `Projectile` delivery; the template is an `AbilityProjectileBehaviour` with **Motion** Orbit, three **Hit Points** and a **Rehit Interval**. | Damage and cooldown: at level 5 the blades are almost permanent. |
| **Chain Lightning** | Strikes a random foe and jumps on. | Unit target, a `chain` effect with a falloff per hop. A second auto-caster with **Target Pick** Random aims it. A second `chain` node with the `AreaAroundCaster` selector starts at the nearest foe; **Chance By Level** 0, 0, 1, 1, 1 switches it on. | Damage, cooldown and **Max Targets By Level**: 3 to 8 hops. From level 3 a second bolt strikes the nearest foe. |
| **Burning Ground** | Sets the ground on fire under a random foe. | Point target; a `zone` effect fixed at the point pulses `fire_tick`. Aimed by the Random auto-caster. | Radius, damage and **Duration By Level**. |
| **Ice Spear** | A fast spear that pierces a whole line. | Direction target, `Projectile` delivery; the template has **Max Hits** 99. | Damage and cooldown. |
| **Frost Nova** | Freezes everything around the hero. | No target; area damage plus `apply_modifier` `frozen` (the `stunned` state). | Radius, damage and the freeze: **Duration By Level** 1 to 2 s. |

Cooldowns come from **Cooldown By Level** on each ability. A weapon card is a button with one `AbilityNoCodeAction` set to `LevelUpAbility` (**Max Level** 5): the first pick grants the weapon, the next ones raise its level.

## How it is assembled

| Scene object | What it holds |
|--------------|---------------|
| `[Demo] World/Player` | The hero: movement, unit, casters, XP magnet. See [Player](#player). |
| `[Demo] World/Player/Random Aim` | A second `AbilityAutoCaster` with **Target Pick** Random, for lightning and burning ground. |
| `[Demo] World/Templates/Enemy_*` (inactive) | The five enemy types. See [Enemies](#enemies). |
| `[Demo] World/Templates/XpOrb` (inactive) | Sprites, a trigger `CircleCollider2D` and a `Despawner` that spawns `Fx_Pickup`. The player's magnet moves and collects it. |
| `[Demo] World/Templates/Projectile_*`, `Orbit_blades` (inactive) | `AbilityProjectileBehaviour` templates (**Plane** ScreenXY): the homing bolt, the piercing spear, the enemy spit and the orbiting blades. |
| `[Demo] World/Templates/Zone_*` (inactive) | `AbilityZoneView` templates for the aura and the burning ground: they scale to the zone radius, follow the anchor and return to the pool when the zone ends. |
| `[Demo] World/Templates/Fx_*` (inactive) | Pooled effects: `ParticleSystem`, `Despawner` with **Despawn After Seconds**, `UnityLifecycleEvents` for the sound. |
| `[Demo] World/Enemy Spawner`, `[Demo] World/Boss Spawner` | Two `Spawner` components. See [Spawners](#spawners). |
| `[Demo] Services/Run` | XP, kills, timer, scene flow and the upgrade draft. See [Run services](#run-services). |
| `[Demo] Services/Ability System` | `AbilitySystemBehaviour` with the library and the archetypes `bolt`, `spear`, `spit`, `blades`, `zone_aura`, `zone_fire`; an `AbilityCuePlayer` with the cues `frost`, `zap` and `hit`. |
| `[Demo] Services/Audio`, `[Demo] Audio` | One `PlayAudio` per sound, and the package `-AM` prefab with the music. |
| `[Demo] HUD` | Bars, texts, cards, overlays and the weapon loadout. See [HUD](#hud). |
| `[Demo] Camera Rig` | `Follow` (TwoD, exponential smoothing) on the player; the child camera carries the shake. |

### Player

- `KeyboardMover` (XY), `AbilityUnitBehaviour` (`unit_player`) and `AbilityCasterBehaviour`.
- An `AbilityAutoCaster` with **Target Pick** Nearest for the bolt, aura, blades, nova and spear.
- `MagneticField` (TwoD, Kinematic, reach = the player radius).
- `OnMoveSpeedChanged -> KeyboardMover.SetSpeed`.
- `MagneticField.OnObjectReached -> LevelComponent.AddXp(1)` and `Despawner.DespawnOther(gem)`.

### Enemies

Each `Enemy_*` template has:

- `AbilityUnitBehaviour` (`unit_grunt`, `unit_fast`, `unit_tank`, `unit_spitter` or `unit_boss`), `AbilityCasterBehaviour` and `AbilityAutoCaster`;
- `Follow` (TwoD, target = player) with `OnMoveSpeedChanged -> Follow.SetPositionSpeed`, so a freeze stops them;
- `Loot` (pooled gems) and a `Despawner` that spawns `Fx_Death`;
- `OnDied -> Loot.DropLoot`, `Kills.Add(1)`, `Despawner.Despawn`.

The Spitter keeps its distance with `Follow` **Stopping Distance** and casts a straight `spit` projectile. The Ogre King is a big unit with a heavy bite.

### Spawners

- **Enemy Spawner:** the enemy templates with **Prefab Weights** and **Prefab Unlock Seconds**; **Around Target** = player, on a ring outside the view; **Delay Over Time** ramps from 1.1 s to 0.25 s over 180 s; **Max Active** 160; object pool.
- **Boss Spawner:** one template, **Prefab Unlock Seconds** 120, **Spawn Limit** 1.

### Run services

`[Demo] Services/Run` holds:

- `XP Level`: a `LevelComponent` with the curve asset `SurvivorXpCurve`;
- `Kills`: a `Counter`;
- `Survival Timer`: a `TimerObject`, 180 s;
- `Scene Flow`: a `SceneFlowController`;
- `Pickup Collector`: a `Despawner`;
- `Upgrade Draft`: a `ChanceSystemBehaviour` with one entry per card, **Draw Count** 3, and **Max Picks** 5 on weapon cards.

### HUD

- XP and HP bars (`SetProgress`), level and kill texts (`NoCodeFormattedText`), the timer (`NoCodeBindText` + `TimeToText`), the cards and the overlays.
- `Loadout`: one slot per weapon with an `AbilityCooldownSource`, a `SetProgress` on `CooldownNormalized` and a `NoCodeFormattedText` on `Level`.
- Weapon cards: a `Level` text with a `NoCodeFormattedText` on the slot's `Level` and `NextLevel` shows the level a pick gives, for example `Lv 2 > 3`.

### Run flow

All as persistent UnityEvents:

- `LevelComponent.OnLevelUp` -> `UpgradeOverlay.SetActive(true)`, `SceneFlowController.Pause(true)`, `ChanceSystemBehaviour.GenerateUniqueVoid()`, the level-up sound and burst.
- `ChanceSystemBehaviour.OnDrawStarted` hides every card; `Events By Index[i]` shows card *i*.
- Card button -> `AbilityNoCodeAction.Execute` (`LevelUpAbility` or `ApplyModifier`; Vitality also `Heal`), `ConsumePick(i)`, hide the overlay, `Pause(false)`.
- Player `OnDied` -> defeat overlay, pause, `StopSpawn` on both spawners. `TimerObject.OnTimerCompleted` -> victory overlay, pause, `StopSpawn`.
- **Play again** / **Restart run** -> `SceneFlowController.Pause(false)`, `SceneFlowController.Restart()`.

## Look and feel, also without scripts

| Feedback | How it is wired |
|----------|-----------------|
| Idle "breathing" | `TransformAnimator` (scale pulse, random start time) on each `Body` sprite; the gem also floats, the blades spin. |
| Hit reaction | `AbilityUnitBehaviour.OnDamaged -> TransformAnimator.Shake()` and `SpriteFlash.Flash()` on the body: it shakes and flashes white. The hero's `OnDamaged` also shakes the child camera of the rig, which has its own `TransformAnimator`. |
| Weapon effects | Ability data names a cue: **Cast Cue** `frost` on Frost Nova, **Impact Cue** `zap` on Chain Lightning and `hit` on the bolt, spear and blades. `AbilityCuePlayer` spawns the pooled effect at the caster or at every damaged unit. |
| Zones | The aura and the fire are `AbilityZoneView` templates named by the zone effect's archetype id. |
| Enemy death, gem pickup | `Despawner` **Spawn Prefab On Despawn** on the enemy and gem templates. |
| Effect lifetime | Every effect template has a `Despawner` with **Despawn After Seconds** and returns to the pool by itself. |
| Sounds of pooled objects | `UnityLifecycleEvents.OnEnable -> PlayAudio.AudioPlay`. **Min Interval** on those `PlayAudio` components keeps twenty deaths at once from stacking one sound. |
| Other sounds | `OnLevelUp`, the timer, the hero's `OnDied` and every button call `AudioPlay` directly. |
| Level-up burst | `LevelComponent.OnLevelUp -> ParticleSystem.Play` on `Player/LevelUp Burst`. It uses unscaled time, so it plays while the game is paused for the cards. |
| Ground | One tiled sprite (`SpriteRenderer` **Draw Mode** Tiled). |

The art is in `Assets/NeoxiderDemos/Demo/Sprites/Survivor` (sprites and particle materials); the sounds and the music loop are in `Demo/Survivor/Audio`. Swap a sprite or a clip in the Inspector to restyle the game.

## The data

The tuning lives in the scene components (the spawner table, the draft entries, the timer) and in the Neo.Abilities assets in `Survivor/Data`:

| Asset | Content |
|-------|---------|
| `unit_player` | `health` 100, `mana` 100, `move_speed` 5; grants `magic_bolt`. |
| `unit_grunt` / `unit_fast` / `unit_tank` | 20 / 12 / 70 HP, move 2.2 / 3.6 / 1.5; melee `bite_*` every 0.5 s. Spawn weight 2 / 1.2 / 0.7, unlocked at 0 / 20 / 50 s. |
| `unit_spitter` | 16 HP, move 1.9, stops 5.5 away and casts `spit` (a straight projectile, 7 damage). Weight 0.9, unlocked at 40 s. |
| `unit_boss` | Ogre King: 450 HP, move 1.25, `bite_boss` 14 damage. Arrives once at 120 s and drops 14 gems. |
| `magic_bolt`, `aura`, `blades`, `lightning`, `fire_pool`, `spear`, `frost_ring` | The seven weapons, each with per-level arrays. |
| `bolt_hit`, `aura_tick`, `fire_tick` | Payload abilities: what a bolt, an aura pulse and a fire pulse do to the units they reach. |
| `frozen` | The freeze: the `stunned` state with **Duration By Level**. |
| `swift`, `might`, `haste`, `regen`, `ward`, `vitality` | Permanent player modifiers applied by cards (independent stacks). |
| `SurvivorXpCurve` | Cumulative XP table: level *n* costs `5 + 4*(n-1)` XP. |

## Clip your own survivor game

- **Tune:** change the numbers in the unit, ability and modifier assets, or in the scene components.
- **Add a weapon:** create an ability asset from the effects above (`projectile`, `zone`, `chain`, `knockback`, `apply_modifier`, an area or cone selector) and add it to `SurvivorLibrary`. List its id in one of the hero's auto-casters, then duplicate a weapon card and a loadout slot. A weapon with a body also needs a template in the hub's **Archetypes**.
- **Add an enemy:** create a `UnitTemplate` with an ability and duplicate an enemy template in the scene. Set its `UnitTemplate` and sprite, and add it to the spawner's **Prefabs**, **Prefab Weights** and **Prefab Unlock Seconds**.
- **Add an effect:** duplicate an `Fx_*` template, add a cue to `AbilityCuePlayer` and write its id into the ability's **Cast Cue** or **Impact Cue**.
- **Make it 3D:** `KeyboardMover` XZ, spawner **Ring Plane** XZ, magnet **Space** ThreeD, `Follow` ThreeD, projectiles and zone views with **Plane** GroundXZ.

## See also

- [Neo.Abilities](./README.md), [AbilityDefinition](./AbilityDefinition.md),
  [AbilityProjectileBehaviour](./AbilityProjectileBehaviour.md), [AbilityAutoCaster](./AbilityAutoCaster.md),
  [AbilityCuePlayer](./AbilityCuePlayer.md), [AbilityNoCodeAction](./AbilityNoCodeAction.md),
  [AbilityCooldownSource](./AbilityCooldownSource.md)
- [Spawner](../Tools/Spawner/Spawner.md), [Despawner](../Tools/Spawner/Despawner.md),
  [MagneticField](../Tools/Physics/MagneticField.md),
  [ChanceSystemBehaviour](../Tools/Random/ChanceSystemBehaviour.md),
  [LevelComponent](../Core/Level/Components/LevelComponent.md), [Loot](../Tools/Components/Loot.md)
- [Demo scenes](../Samples.md)
