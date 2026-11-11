# Survivor Demo

**What it is** — a complete, playable Vampire-Survivors-style 2D game built **only from library components and
UnityEvents**, like the Shooter2D reference game: no game scripts. You move a wizard while endless waves close in;
weapons fire by themselves, kills drop XP gems, every level-up offers three upgrade cards, and surviving three
minutes wins the run. The look is wired the same way: sprites, squash and shake animations, particles, sounds and
music. The same modules work in 3D (XZ plane options on the spawner, magnet, mover and projectiles).

**How to use** — download the Demo `.unitypackage` (`Assets/NeoxiderDemos/Demo`), open
`Assets/NeoxiderDemos/Demo/Scenes/SurvivorDemo.unity` and press Play. Everything is a saved scene object, so the whole
wiring can be read in the Hierarchy and Inspector **without entering Play Mode**. There is no scene-generation
script: change the game by editing the scene.

## How to play

- **WASD / arrows** move. Weapons auto-fire at the nearest enemy (`magic_bolt`) or around you (nova, frost ring,
  ember).
- **XP gems** fly to you once you are within the pickup radius; each is 1 XP.
- On **level-up** the game pauses and shows three different cards; click one.
- The timer counts to **3:00**: survive it for **VICTORY**; dying shows **YOU FELL**. **Play again** restarts.
- The top-right **+10 XP** button opens a level-up at once; **Restart run** reloads the scene.

## How it is assembled

| Scene object | Components and wiring |
|--------------|-----------------------|
| `[Demo] World/Player` | `KeyboardMover` (XY) + `AbilityUnitBehaviour` (`unit_player`) + `AbilityCasterBehaviour` + `AbilityAutoCaster` + `MagneticField` (TwoD, Kinematic, reach = player radius). `OnMoveSpeedChanged -> KeyboardMover.SetSpeed` (Swift and slows change the speed). `MagneticField.OnObjectReached -> LevelComponent.AddXp(1)` and `Despawner.DespawnOther(gem)`. |
| `[Demo] World/Templates/Enemy_*` (inactive) | `AbilityUnitBehaviour` (`unit_grunt` / `unit_fast` / `unit_tank`, each granting its `bite_*` ability) + `AbilityCasterBehaviour` + `AbilityAutoCaster` (bites the player in range) + `Follow` (TwoD, target = player; `OnMoveSpeedChanged -> Follow.SetPositionSpeed`, so `chill` slows them and stuns stop them) + `Loot` (pooled, drops `XpReward` gems) + `Despawner` (spawns `Fx_Death`). `OnDied -> Loot.DropLoot`, `Kills.Add(1)`, `Despawner.Despawn`. |
| `[Demo] World/Templates/XpOrb` (inactive) | Sprites + a trigger `CircleCollider2D` + `Despawner` (spawns `Fx_Pickup`). No game script: the player's magnet moves and collects it. |
| `[Demo] World/Templates/Projectile_bolt` (inactive) | `AbilityProjectileBehaviour` (**Plane** ScreenXY) + a particle trail; the `bolt` archetype of the ability hub. |
| `[Demo] World/Templates/Fx_*` (inactive) | Pooled effect templates: `ParticleSystem` sparks and a ring, `Despawner` with **Despawn After Seconds**, `UnityLifecycleEvents` for the sound. |
| `[Demo] World/Enemy Spawner` | `Spawner`: the enemy templates with **Prefab Weights** and **Prefab Unlock Seconds**, **Around Target** = player on a ring outside the view, **Delay Over Time** ramping 1.1 s to 0.25 s over 180 s, **Max Active** 160, object pool. |
| `[Demo] Services/Run` | `XP Level` (`LevelComponent`, custom curve asset `SurvivorXpCurve`, no save), `Kills` (`Counter`, no save), `Survival Timer` (`TimerObject`, 180 s), `Scene Flow` (`SceneFlowController`: pause and restart), `Pickup Collector` (`Despawner`) and `Upgrade Draft` (`ChanceSystemBehaviour`, one entry per card, **Draw Count** 3, weapon cards with **Max Picks** 1). |
| `[Demo] Services/Ability System` | `AbilitySystemBehaviour`: the library, the `bolt` projectile archetype and the `fx_nova` / `fx_frost` / `fx_ember` effect archetypes, spatial grid cell 3. |
| `[Demo] Services/Audio` | One `PlayAudio` per sound (`Sfx Shoot`, `Sfx Enemy Death`, `Sfx Pickup`, `Sfx Level Up`, ...). |
| `[Demo] Audio` | The package `-AM` prefab with one startup music entry. |
| `[Demo] HUD` | XP bar and HP bar (`SetProgress` on `LevelComponent.XpProgress01` and `AbilityUnitBehaviour.HealthNormalized`), level and kills texts (`NoCodeFormattedText`), timer (`NoCodeBindText` + `TimeToText`), the cards, the defeat and victory overlays. |
| `[Demo] Camera Rig` | `Follow` (TwoD, exponential smoothing) on the player; the child camera carries the shake. |

**Run flow, all as persistent UnityEvents:**

- `LevelComponent.OnLevelUp` -> `UpgradeOverlay.SetActive(true)`, `SceneFlowController.Pause(true)`,
  `ChanceSystemBehaviour.GenerateUniqueVoid()`, the level-up sound and burst.
- `ChanceSystemBehaviour.OnDrawStarted` hides every card; `Events By Index[i]` shows card *i*.
- Card button -> `AbilityNoCodeAction.Execute` (`GrantAbility` or `ApplyModifier`; Vitality also `Heal`),
  `ConsumePick(i)`, hide the overlay, `Pause(false)`.
- Player `OnDied` -> defeat overlay, pause, `Spawner.StopSpawn`. `TimerObject.OnTimerCompleted` -> victory overlay,
  pause, `StopSpawn`.
- **Play again** / **Restart run** -> `SceneFlowController.Pause(false)`, `SceneFlowController.Restart()`.

## Look and feel, also without scripts

| Feedback | How it is wired |
|----------|-----------------|
| Idle "breathing" | `TransformAnimator` (scale pulse, random start time) on each `Body` sprite; the gem also floats. |
| Hit reaction | `AbilityUnitBehaviour.OnDamaged -> TransformAnimator.Shake()` on the body. The hero's `OnDamaged` also shakes the camera: the rig follows, the child camera has its own `TransformAnimator`. |
| Weapon effects | Each area weapon has a `spawn` cast effect in its ability asset (`fx_nova`, `fx_frost`, `fx_ember`, target = caster). The ability hub maps the archetype id to a pooled effect template, so the effect is part of the ability data. |
| Enemy death, gem pickup | `Despawner` **Spawn Prefab On Despawn** on the enemy and gem templates. |
| Effect lifetime | Every effect template has a `Despawner` with **Despawn After Seconds** and returns to the pool by itself. |
| Sounds | Pooled templates play their `PlayAudio` with `UnityLifecycleEvents.OnEnable -> PlayAudio.AudioPlay` (bolt shot, death, pickup, weapon bursts); **Min Interval** on those `PlayAudio` components keeps twenty simultaneous deaths from stacking one sound. `OnLevelUp`, the timer, the hero's `OnDied` and every button call `AudioPlay` directly. |
| Level-up burst | `LevelComponent.OnLevelUp -> ParticleSystem.Play` on `Player/LevelUp Burst`. It uses unscaled time, so it plays while the game is paused for the cards. |
| Projectile trail | A world-space `ParticleSystem` (rate over distance) on the bolt template. |
| Ground | One tiled sprite (`SpriteRenderer` **Draw Mode** Tiled). |

The art is in `Assets/NeoxiderDemos/Demo/Sprites/Survivor` (sprites and two particle materials); the sounds and the music loop are in
`Demo/Survivor/Audio` (generated with Stable Audio 3; the button click is the shared demo clip). Swap a sprite or a clip in
the Inspector to restyle the game.

## The data

The tuning lives in the scene components (the spawner table, the draft entries, the timer) and in Neo.Abilities
assets in `Survivor/Data`:

| Asset | Content |
|-------|---------|
| `unit_player` | `health` 100, `mana` 100, `move_speed` 5; grants `magic_bolt` (homing projectile, 10 magical, 0.75 s). |
| `unit_grunt` / `unit_fast` / `unit_tank` | 20 / 12 / 70 HP, move 2.2 / 3.6 / 1.5; grant `bite_grunt` / `bite_fast` / `bite_tank` (instant melee every 0.5 s: 5 / 4 / 8 damage). In the spawner: weight 2 / 1.2 / 0.7, unlocked at 0 / 20 / 50 s; in `Loot`: 1 / 1 / 3 XP gems. |
| `nova`, `frost_ring`, `ember` | Self-centred area weapons granted by cards (frost applies `chill`, ember applies `burn`), each with a `spawn` cast effect for its visual. |
| `swift`, `might`, `haste`, `regen`, `ward`, `vitality` | Permanent player modifiers applied by cards (independent stacks). |
| `SurvivorXpCurve` | Cumulative XP table: level *n* costs `5 + 4*(n-1)` XP. |

## Clip your own survivor game

- Change the numbers in the unit, ability and modifier assets, or in the scene components.
- Add an enemy: create a `UnitTemplate` with a bite ability, duplicate an enemy template in the scene, set its
  `UnitTemplate` and sprite, and add it to the spawner's **Prefabs**, **Prefab Weights** and
  **Prefab Unlock Seconds**.
- Add an upgrade: a card button with an `AbilityNoCodeAction`, a new entry in the `Upgrade Draft` and its
  `Events By Index` element.
- Add a weapon effect: duplicate an `Fx_*` template, register it in the ability hub's **Archetypes** and add a
  `spawn` cast effect with that id to the ability.
- Make it 3D: `KeyboardMover` XZ, spawner **Ring Plane** XZ, magnet **Space** ThreeD, `Follow` ThreeD and projectiles
  with **Plane** GroundXZ.

## What it verifies

`SurvivorDemoGameplayTests` (Play Mode) has two cases. `SavedScene_IsWiredFromLibraryComponentsWithoutGameScripts`
reads the saved scene: the wiring is persistent UnityEvents between library components, and the info card is the only
component that does not come from the package. `Autopilot_KillsLevelsUpTakesCardsAndWinsTheRun` plays the scene with
an autopilot that kites and picks cards: it has to kill more than 20 enemies, reach level 4, take three cards and
win the 180 s run. With `-neoSurvivorFrames <dir>` the test also writes JPEG frames and the mixed audio for a
gameplay video.

## See also

- [Neo.Abilities](./README.md), [AbilityUnitBehaviour](./AbilityUnitBehaviour.md),
  [AbilityProjectileBehaviour](./AbilityProjectileBehaviour.md)
- [Spawner](../Tools/Spawner/Spawner.md), [Despawner](../Tools/Spawner/Despawner.md),
  [MagneticField](../Tools/Physics/MagneticField.md),
  [ChanceSystemBehaviour](../Tools/Random/ChanceSystemBehaviour.md),
  [LevelComponent](../Core/Level/Components/LevelComponent.md), [Loot](../Tools/Components/Loot.md)
- [Demo scenes](../Samples.md)
