# Survivor Demo

**What it is** — a complete, playable Vampire-Survivors-style game built entirely on
[Neo.Abilities](./README.md) and the Core resource/level systems. You move a hero around an
arena while endless waves close in; your weapons auto-fire, kills drop XP orbs, and every level-up
lets you pick one of three upgrade cards. Spawn rate and enemy health escalate the longer you
survive. The game is **data**: a single `SurvivorConfig` asset holds the player, the enemy spawn
table, the abilities, the upgrades and the tuning, so swapping that asset produces a different
survivor game on the same code. That is the point of the kit: it is a template you clip by editing
data, not code.

**How to use** — import the **Demo Scenes** sample via Package Manager (the package ships it hidden in
`Samples~`), then open `<Demo sample>/Scenes/SurvivorDemo.unity` and press Play. Everything the game
needs is a saved scene object, so you can read the whole wiring in the Hierarchy and Inspector
**without entering Play Mode**: the `Survivor Game` object (config + references), `[Demo] Services`
(ability hub, info card data), `[Demo] World` (arena, player, inactive pooled templates),
`[Demo] Pool Manager`, `[Demo] Camera` and `[Demo] HUD`. Nothing is assembled at runtime.

The active release source is `Assets/Neoxider/Samples~/Demo/Scenes/SurvivorDemo.unity`. During sample development it becomes `Assets/Neoxider/Samples/Demo/Scenes/SurvivorDemo.unity`. Package Manager imports it into the consuming project, for example `Assets/Samples/NeoxiderTools/10.19.0/Demo Scenes/Scenes/SurvivorDemo.unity`; use the installed version's folder. Importing does not make `Samples~` itself visible as a regular Unity asset folder.

## How to open and play

1. Import the Demo sample (Package Manager → Neoxider Tools → Samples), then open
   `<Demo sample>/Scenes/SurvivorDemo.unity`.
2. Press Play.
3. Move with **WASD** / arrow keys (Unity's `Horizontal` / `Vertical` axes).
4. Weapons **auto-fire** — you never aim or press a fire button. You start with **Magic Bolt**, a
   homing bolt that strikes the nearest enemy.
5. Walk over the golden **XP orbs** dropped by dead enemies (they are magnetized to you when close).
6. On **level up** the game pauses and shows three random **upgrade cards**; click one to continue.
   **Level up now** (top right) opens the picker immediately, **Restart run** starts over.
7. When your health hits zero you get a **game-over** screen with your time, kills, and level; click
   **Play again** to restart the same scene instantly (no reload).

A tutorial toast at the top fades out after ~6 seconds:
`WASD move · weapons auto-fire · collect XP · level up to choose upgrades`. The info card in the
bottom-right corner names the objects to inspect; collapse it with its **-** button.

## Kit architecture

Everything lives in the Demo sample under `Scripts/Survivor/` (namespace `Neo.Samples.Survivor`).
The scene is authored by the editor builder `Editor/Builders/Abilities/SurvivorDemoBuilder.cs`
(menu **Neoxider/Demo/Rebuild Active Demo Scene**), which reads the config and saves every object.

| Scene object | What it is |
|--------------|------------|
| `Survivor Game` | `SurvivorGame` with the **Config** and the authored references (hub, HUD, camera, player, templates). |
| `[Demo] Services/Ability System` | `AbilitySystemBehaviour`: the config's library, the `bolt` projectile archetype bound to its template, **Spatial Cell Size** 3 (grid radius queries for hundreds of chasers). |
| `[Demo] World/Arena` | Neon frame + floor sprites sized from `ArenaExtent`. |
| `[Demo] World/Player` | `AbilityUnitBehaviour` (player `UnitTemplate`) + `SurvivorPlayerController`. |
| `[Demo] World/Templates` | Inactive pooled templates: one `Enemy_<unit template>` per enemy type, `XpOrb`, one `Projectile_<archetype>` per projectile archetype. |
| `[Demo] Pool Manager` | `PoolManager` (a scene root, so its DontDestroyOnLoad promotion is clean). |
| `[Demo] HUD` | Overlay canvas with `SurvivorHudView`, the upgrade cards (`Btn_Upgrade 1..3` → `SurvivorGame.PickUpgrade(i)`), `Btn_Play again` / `Btn_Restart run` → `SurvivorGame.RestartRun`, `Btn_Level up now` → `SurvivorGame.GrantLevelUp`, and the info card. |

| Script | Role |
|--------|------|
| `SurvivorGame` | Game loop over the authored scene: registers upgrade content, maps each config enemy type to its template (by unit template), then runs spawning, XP/level-up, upgrades, camera follow and game-over. Restarts re-register the player from its `UnitTemplate` (disable/enable), so health, abilities, modifiers and cooldowns reset without recreating objects. Public: `RestartRun`, `PickUpgrade(int)`, `GrantLevelUp`, `AddXp`, `Level`, `Kills`, `IsLevelUpOpen`. |
| `SurvivorConfig` | **The whole game as data** (`ScriptableObject`). Library, player template + color, starting abilities, enemy spawn table, upgrade pool, arena/camera size, spawn ramp, health ramp, XP curve, pickup/player radius. `CreateAssetMenu`: *Neoxider/Survivor Demo/Config*. |
| `SurvivorUpgrade` | One level-up option (`ScriptableObject`): title, description, accent color, a `SurvivorUpgradeKind`, and the payload (modifier / ability / health bonus) plus `MaxTimes`. `CreateAssetMenu`: *Neoxider/Survivor Demo/Upgrade*. |
| `SurvivorUpgradeKind` | Enum of the three upgrade behaviors: `PermanentModifier`, `GrantAbility`, `MaxHealth`. |
| `SurvivorEnemyType` | One spawn-table entry (`[Serializable]`, inlined in the config): unit template + color, radius, contact DPS, XP reward, spawn weight, unlock time. |
| `SurvivorPlayerController` | The player avatar. WASD movement (read through `MoveInputCompat`, so it works with the Input System only) scaled by the unit's `move_speed` property, plus the auto-caster: fires every granted ability the moment it comes off cooldown (unit-targeted abilities lock onto the nearest enemy, the rest self-cast). |
| `SurvivorEnemy` | A pooled chaser. Steers toward the player at its unit `move_speed`, freezes while stunned/rooted/frozen, deals continuous contact damage, drops an XP orb on death. Pure presentation + steering; the ability domain owns its health and combat. |
| `SurvivorXpOrb` | A pooled XP pickup. Idles until the player is within magnet range, homes in, and is collected on contact. |
| `SurvivorHudView` | Drives the authored HUD: XP bar, level, timer, kills, health bar, ability cooldown pips, the fading tutorial toast, the level-up cards and the game-over screen. Labels change only when their value changes, so the running HUD allocates nothing per frame. |
| `SurvivorUI` / `SurvivorArt` | Shared runtime helpers still used by other demos; the authored scene uses the DemoKit sprite assets (`Sprites/DemoKit`) instead of procedural textures. |

After changing the config's **enemy list** or adding a **projectile archetype**, rebuild the scene
(**Neoxider/Demo/Rebuild Active Demo Scene**) so the matching templates and archetype bindings are
authored. Stats, upgrades, abilities and tuning are read at Play time and need no rebuild.

## The data behind the demo

The shipped assets live in the Demo sample under `Survivor/Data/`. Reading them shows exactly
what a survivor game is made of.

**Player** — `unit_player` (team 1 "Hero"): `health` 100, `mana` 100, `move_speed` 5. Starting
ability: `magic_bolt`.

**Enemies** (`SurvivorConfig.Enemies`, three archetypes on team 2):

| Type | Template | HP | Move | Contact DPS | XP | Weight | Unlocks at |
|------|----------|----|------|-------------|----|--------|-----------|
| Grunt | `unit_grunt` | 20 | 2.2 | 10 | 1 | 2.0 | 0 s |
| Stinger | `unit_fast` | 12 | 3.6 | 8 | 1 | 1.2 | 20 s |
| Brute | `unit_tank` | 70 | 1.5 | 16 | 3 | 0.7 | 50 s |

Spawn type is a weighted random over the entries whose `UnlockTime` has passed, so the roster and
its mix widen as the run goes on.

**Abilities** (`SurvivorLibrary`, four auto-cast weapons):

| Ability | Targeting | Cooldown | Effect |
|---------|-----------|----------|--------|
| `magic_bolt` | Unit (nearest, range 11) | 0.75 s | Homing projectile (archetype `bolt`, speed 15), 10 magical on hit |
| `nova` | NoTarget (self) | 2.6 s | 14 magical to all enemies within radius 3.4 of the player |
| `frost_ring` | NoTarget (self) | 3.4 s | 7 magical + `chill` (slow) within radius 3.8 |
| `ember` | NoTarget (self) | 3.2 s | 5 magical + `burn` (DoT) within radius 3.2 |

**Modifiers** (7 in the library — the upgrade payloads):

| Modifier | Kind | Effect |
|----------|------|--------|
| `swift` | permanent buff | `move_speed` **Add** +0.8 |
| `might` | permanent buff | `outgoing_damage_mul` **Add** +0.22 (×1.22 damage) |
| `haste` | permanent buff | `cooldown_reduction_percent` **Add** +12 |
| `regen` | permanent buff | `health_regen` **Add** +3 / s |
| `ward` | permanent buff | `incoming_damage_mul` **Mul** ×0.85 (-15% taken) |
| `chill` | 1.6 s debuff | `move_speed` **Mul** ×0.5 (enemy slow) |
| `burn` | 3 s debuff, DoT | 4 magical every 0.5 s |

**Upgrades** (`SurvivorConfig.Upgrades`, nine cards, 3 offered per level-up):

| Card | Kind | Payload | Repeatable |
|------|------|---------|------------|
| Swift | PermanentModifier | `swift` | unlimited |
| Might | PermanentModifier | `might` | unlimited |
| Haste | PermanentModifier | `haste` | unlimited |
| Regeneration | PermanentModifier | `regen` | unlimited |
| Ward | PermanentModifier | `ward` | unlimited |
| Vitality | MaxHealth | +25 max HP, full heal | unlimited |
| Ember Burst | GrantAbility | `ember` | once |
| Frost Ring | GrantAbility | `frost_ring` | once |
| Arcane Nova | GrantAbility | `nova` | once |

Stat cards stack (each pick applies the modifier again); weapon cards are `MaxTimes = 1` and stop
being offered once owned. Tuning (`SurvivorConfig`): arena extent 9, camera size 7, spawn interval
ramps 1.1 s -> 0.25 s over 180 s, enemy health +50% per minute, XP for level *n* = `5 + 4*(n-1)`,
pickup radius 2.2.

## Clip your own survivor game

You never edit a script. Build a new game by authoring data and pointing the config at it:

1. **Duplicate the config.** Copy `SurvivorConfig.asset` (or create a fresh one via
   *Create -> Neoxider -> Survivor Demo -> Config*). This asset *is* your game.
2. **Author units.** Create [UnitTemplate](./UnitTemplate.md) assets for your hero and each enemy
   (a `health` pool, `move_speed` base property; the hero also needs `mana` if any weapon has a
   cost). Put the hero on one team id and enemies on another.
3. **Author weapons.** Create [AbilityDefinition](./AbilityDefinition.md) assets. Unit-targeted
   abilities (e.g. a homing bolt) auto-aim at the nearest enemy; NoTarget abilities self-cast as
   area bursts around the hero. For projectiles set a Projectile delivery + an archetype id (the
   scene builder authors a glowing projectile template for every archetype the library and the
   upgrades reference).
4. **Author modifiers.** Create [ModifierDefinition](./ModifierDefinition.md) assets for stat buffs
   (permanent, Duration 0) and for enemy debuffs like slows/DoTs used by your abilities.
5. **Collect them.** Add every ability and modifier to an [AbilityLibrary](./AbilityLibrary.md) and
   assign it to the config's **Library**. (Upgrade payloads are also force-registered even if you
   forget to add them to the library.)
6. **Author upgrades.** Create `SurvivorUpgrade` assets (*Create -> Neoxider -> Survivor Demo ->
   Upgrade*), one per level-up option, each with a `Kind` (see below) and its payload.
7. **Wire the config lists.** Fill in `PlayerTemplate`, `StartingAbilities` (ability ids), the
   `Enemies` spawn table (`SurvivorEnemyType` rows), and the `Upgrades` pool. Tune arena, spawn
   ramp, health ramp and XP curve to taste.
8. **Point the scene at it.** Assign your config to the `SurvivorGame` component, run
   **Neoxider/Demo/Rebuild Active Demo Scene** once (templates and archetypes follow the config),
   and press Play.

### The three upgrade kinds

`SurvivorUpgrade.Kind` (`SurvivorUpgradeKind`) decides what picking the card does:

- **PermanentModifier** — applies `Modifier` to the hero every time the card is chosen, so it
  **stacks** (Swift twice = +1.6 move speed). Use for stat growth via property contributions.
- **GrantAbility** — registers and grants `Ability` to the hero as a new auto-cast weapon; the
  ability list and the HUD cooldown pips update. Already-owned grant cards are filtered out of the
  offer, so set `MaxTimes = 1`.
- **MaxHealth** — raises the hero's maximum `health` pool by `HealthBonus` and heals to full.

`MaxTimes` (0 = unlimited) caps how often any card can be offered/taken. Each level-up offers three
distinct eligible cards at random.

## How it exercises Neo.Abilities and Core

The demo is a working tour of the ability system rather than a bespoke combat script:

- **Units and teams** — hero and enemies are `AbilityUnit`s created from [UnitTemplate](./UnitTemplate.md)s
  via `AbilityUnitBehaviour`, on opposing teams. Ability `TeamFilter = Enemies` makes weapons hit
  the right side automatically.
- **Properties (Add / Mul)** — movement, damage, cooldown and mitigation are all driven by unit
  properties, so upgrades and debuffs "just work": Swift contributes `move_speed` **Add**, Ward
  contributes `incoming_damage_mul` **Mul**, Might contributes `outgoing_damage_mul` **Add**, Haste
  contributes `cooldown_reduction_percent`. The player controller reads `move_speed` directly for
  its speed; `DamageService` reads the damage multipliers.
- **Modifiers, slows and DoTs** — `burn` is a tick-effect DoT; `chill` is a timed `move_speed` **Mul**
  slow. Enemies read their own `move_speed` and honor `Stunned`/`Rooted`/`Frozen` states, so any
  disabling modifier you add stops them without extra code.
- **Cast pipeline** — the auto-caster only fires slots whose cooldown/charges are ready and lets the
  system validate targeting and range; a homing bolt casts `AtUnit` on the nearest enemy in range,
  area weapons cast `NoTarget`.
- **Homing projectiles** — `magic_bolt` uses a Projectile delivery + archetype id; the hub's
  **Archetypes** list binds it to the authored `Projectile_bolt` template (`AbilityProjectileBehaviour`)
  and the system spawns/steers it.
  See [AbilityProjectileBehaviour](./AbilityProjectileBehaviour.md).
- **Area effects** — `nova`, `frost_ring`, and `ember` use `AreaAroundCaster` selectors with a radius,
  the same effect-node model documented in the [Abilities README](./README.md).
- **Receipts -> HUD** — the HUD reads live unit state (health, per-slot `NormalizedCooldown`) to
  drive the health bar and cooldown pips, illustrating the event/state-as-receipt pattern.

Core systems it leans on:

- **Health via `ResourcePoolModel`** — every unit's `health` (and the hero's `mana`) is a
  `Neo.Core.Resources.ResourcePoolModel` pool. Contact damage flows through `DamageService` into the
  pool; the enemy health ramp and the Vitality upgrade call `Resources.SetMax` + `Restore` on it.
  See [Core / Resources](../Core/README.md).
- **Level / XP curve concept** — leveling is a simple linear curve authored in the config
  (`XpForLevel(n) = BaseXp + XpPerLevel*(n-1)`): collect XP to a threshold, level up, offer upgrades.
  It mirrors the idea behind Core's [Level](../Core/Level.md) /
  [LevelCurveDefinition](../Core/LevelCurveDefinition.md); swap the two config numbers to reshape the
  pacing.

## What to reuse in your own game

- **The authored, config-driven scene.** `SurvivorDemoBuilder` shows how to turn one
  `ScriptableObject` into a saved, inspectable scene (camera, arena, ability hub + library +
  archetypes, pooled templates, HUD with persistent UnityEvents), while `SurvivorGame` only runs the
  loop over serialized references.
- **The auto-caster.** `SurvivorPlayerController.AutoCast` is a compact, reusable recipe for
  fire-when-ready weapons that pick unit vs. self targeting from the blueprint — drop it into any
  Neo.Abilities project (or use the NoCode [AbilityAutoCaster](./AbilityAutoCaster.md)).
- **Enemy-as-presentation.** `SurvivorEnemy` keeps steering/contact in the MonoBehaviour and lets the
  ability domain own health, damage and disabling states — the recommended split for Neo.Abilities
  actors.
- **Spatial grid for crowds.** The hub's **Spatial Cell Size** turns projectile hits, area effects and
  auto-cast searches into grid lookups; results match the linear scan.

## See also

- [Abilities README](./README.md) — the system this demo is built on
- [UnitTemplate](./UnitTemplate.md) · [AbilityDefinition](./AbilityDefinition.md) ·
  [ModifierDefinition](./ModifierDefinition.md) · [AbilityLibrary](./AbilityLibrary.md)
- [AbilitySystemBehaviour](./AbilitySystemBehaviour.md) ·
  [AbilityUnitBehaviour](./AbilityUnitBehaviour.md) ·
  [AbilityProjectileBehaviour](./AbilityProjectileBehaviour.md)
- [Core / Resources](../Core/README.md) · [Core / Level](../Core/Level.md)
