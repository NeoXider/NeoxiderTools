# Survivor Demo

**What it is** — a complete, playable Vampire-Survivors-style game built entirely on
[Neo.Abilities](./README.md) and the Core resource/level systems. You move a hero around an
arena while endless waves close in; your weapons auto-fire, kills drop XP orbs, and every level-up
lets you pick one of three upgrade cards. Spawn rate and enemy health escalate the longer you
survive. The whole game — arena, camera, player, enemies, projectiles, HUD — is assembled **in code
at runtime from a single data asset**, so swapping that asset produces a different survivor game on
the same code. That is the point of the kit: it is a template you clip by editing data, not code.

**How to use** — open `Assets/Neoxider/Samples/Demo/Scenes/SurvivorDemo.unity` and press Play.
The scene contains exactly one GameObject ("Survivor Game") with a single `SurvivorGame` component
and a `SurvivorConfig` assigned to it — nothing else. Everything visible is generated on Start.

## How to open and play

1. Open `Assets/Neoxider/Samples/Demo/Scenes/SurvivorDemo.unity`.
2. Press Play.
3. Move with **WASD** / arrow keys (Unity's `Horizontal` / `Vertical` axes).
4. Weapons **auto-fire** — you never aim or press a fire button. You start with **Magic Bolt**, a
   homing bolt that strikes the nearest enemy.
5. Walk over the golden **XP orbs** dropped by dead enemies (they are magnetized to you when close).
6. On **level up** the game pauses and shows three random **upgrade cards**; click one to continue.
7. When your health hits zero you get a **game-over** screen with your time, kills, and level; click
   **Play Again** to restart the same scene instantly (no reload).

A tutorial toast at the top fades out after ~6 seconds:
`WASD move · weapons auto-fire · collect XP · level up to choose upgrades`.

## Kit architecture

Everything lives in `Assets/Neoxider/Samples/Demo/Scripts/Survivor/` (namespace
`Neo.Samples.Survivor`). The scene holds **one component + one config**; the rest is built at runtime.

| Script | Role |
|--------|------|
| `SurvivorGame` | Bootstrap + game loop. On `Start` it builds the PoolManager, camera, arena, ability system, enemy/orb templates and HUD from the config, then runs spawning, XP/level-up, camera follow and game-over. The only component you place in a scene. |
| `SurvivorConfig` | **The whole game as data** (`ScriptableObject`). Library, player template + color, starting abilities, enemy spawn table, upgrade pool, arena/camera size, spawn ramp, health ramp, XP curve, pickup/player radius. Swap it and you have a different game. `CreateAssetMenu`: *Neoxider/Survivor Demo/Config*. |
| `SurvivorUpgrade` | One level-up option (`ScriptableObject`): title, description, accent color, a `SurvivorUpgradeKind`, and the payload (modifier / ability / health bonus) plus `MaxTimes`. `CreateAssetMenu`: *Neoxider/Survivor Demo/Upgrade*. |
| `SurvivorUpgradeKind` | Enum of the three upgrade behaviors: `PermanentModifier`, `GrantAbility`, `MaxHealth`. |
| `SurvivorEnemyType` | One spawn-table entry (`[Serializable]`, inlined in the config): unit template + color, radius, contact DPS, XP reward, spawn weight, unlock time. |
| `SurvivorPlayerController` | The player avatar. WASD movement scaled by the unit's `move_speed` property, plus the auto-caster: fires every granted ability the moment it comes off cooldown (unit-targeted abilities lock onto the nearest enemy, the rest self-cast). |
| `SurvivorEnemy` | A pooled chaser. Steers toward the player at its unit `move_speed`, freezes while stunned/rooted/frozen, deals continuous contact damage, drops an XP orb on death. Pure presentation + steering; the ability domain owns its health and combat. |
| `SurvivorXpOrb` | A pooled XP pickup. Idles until the player is within magnet range, homes in, and is collected on contact. |
| `SurvivorHud` | Builds and drives the HUD in code (no prefabs): XP/level bar, timer, kills, health bar, ability cooldown pips, the fading tutorial toast, the level-up card picker and the game-over screen. |
| `SurvivorUI` | Small uGUI construction helpers (rounded panels, bars, labels, buttons) and the shared color palette used by the HUD. |
| `SurvivorArt` | Procedural sprites (soft disc, ring, radial glow, 9-sliced rounded rect), cached and shared, so the demo needs **zero imported art assets**. |

Because `SurvivorGame` reads all content from `SurvivorConfig`, the scene is deliberately empty of
game objects — no player prefab, no enemy prefabs, no canvas. This is what makes the kit
"clip-and-reskin": the code is fixed, the game is the data.

## The data behind the demo

The shipped assets live in `Assets/Neoxider/Samples/Demo/Survivor/Data/`. Reading them shows exactly
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
   kit auto-builds a glowing projectile template for every archetype the library references).
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
8. **Point the scene at it.** Assign your config to the `SurvivorGame` component and press Play.

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
- **Homing projectiles** — `magic_bolt` uses a Projectile delivery + archetype id; the kit registers
  an `AbilityProjectileBehaviour` template for each archetype and the system spawns/steers it.
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

- **The config-driven bootstrap pattern.** `SurvivorGame.BuildOnce()` shows how to stand up a whole
  playable scene (camera, arena, ability system + library + archetypes, pooled templates, canvas)
  from one `ScriptableObject` — a clean base for any "assemble the game from data" sample.
- **The auto-caster.** `SurvivorPlayerController.AutoCast` is a compact, reusable recipe for
  fire-when-ready weapons that pick unit vs. self targeting from the blueprint — drop it into any
  Neo.Abilities project.
- **Enemy-as-presentation.** `SurvivorEnemy` keeps steering/contact in the MonoBehaviour and lets the
  ability domain own health, damage and disabling states — the recommended split for Neo.Abilities
  actors.
- **Prefab-free HUD + art.** `SurvivorHud`, `SurvivorUI`, and `SurvivorArt` build a full themed UI and
  all sprites in code, so the sample imports nothing. Handy when you want a self-contained demo or a
  quick debug overlay.

## See also

- [Abilities README](./README.md) — the system this demo is built on
- [UnitTemplate](./UnitTemplate.md) · [AbilityDefinition](./AbilityDefinition.md) ·
  [ModifierDefinition](./ModifierDefinition.md) · [AbilityLibrary](./AbilityLibrary.md)
- [AbilitySystemBehaviour](./AbilitySystemBehaviour.md) ·
  [AbilityUnitBehaviour](./AbilityUnitBehaviour.md) ·
  [AbilityProjectileBehaviour](./AbilityProjectileBehaviour.md)
- [Core / Resources](../Core/README.md) · [Core / Level](../Core/Level.md)
