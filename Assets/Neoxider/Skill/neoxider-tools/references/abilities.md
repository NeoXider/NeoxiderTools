# Neo.Abilities — data-driven combat (v10 headliner)

`Neo.Abilities` (namespace + asmdef `Neo.Abilities`) is a Dota-derived, data-driven ability/modifier
system: new abilities are authored **entirely in ScriptableObjects**, no code. It **supersedes `Neo.Rpg`**
— for any NEW combat (damage, buffs/debuffs, DoTs, shields, stuns, projectiles, auras, crit/lifesteal)
build on Abilities; touch `Neo.Rpg` only when maintaining an existing Rpg scene (it is slated for removal).
Docs: `Assets/Neoxider/Docs/Abilities/` (README + per-type pages). Source: `Assets/Neoxider/Scripts/Abilities/`
(`Domain/` pure C#, `Data/` ScriptableObjects, `Components/` Unity wrappers, `Bridge/` NoCode).

## The pieces

**Authoring assets (ScriptableObjects)** — create via `Create -> Neoxider -> Abilities`:

| Asset | What it authors |
|---|---|
| `AbilityDefinition` | One ability: targeting (`NoTarget/Self/Unit/Point/Direction`), team filter, costs, cooldown/charges, range, delivery (Instant / homing Projectile + archetype id), cast + impact effect nodes, named `Specials` |
| `ModifierDefinition` | One durable effect — buff/debuff/DoT/shield/stun are ALL modifiers: duration, stack policy (`Independent/Refresh/Stack` + per-stack scaling), property contributions, boolean states, interval tick effects, declarative event reactions (e.g. absorb/retaliate on `take_damage`) |
| `UnitTemplate` | Unit archetype: team, resource pools (`health`, `mana`, …), base properties, granted abilities, start level |
| `AbilityLibrary` | Catalog registering many definitions at once (assign on the hub) |

**Scene components** (`Neoxider/Abilities/...` in Add Component):

| Component | Role / key API |
|---|---|
| `AbilitySystemBehaviour` | Scene hub, singleton `AbilitySystemBehaviour.I` (auto-creates; add manually to assign **Libraries** + **Archetypes**). `AbilitySystem System` (domain entry: `Cast`, `Events`, `GrantAbility`, `SetAbilityLevel`, `Ops`), `bool Paused` (freeze tick for menus/level-up), `AddLibrary(lib)`, `AddArchetype(id, prefab)`, `GetBehaviour(UnitId)`. Implements `IAbilityWorldAdapter` (positions, radius queries, `TryMoveUnit`, spawns via `PoolManager`) |
| `AbilityUnitBehaviour` | A unit's scene presence. Assign **Template**; `AbilityUnit Unit` (domain: `GetProperty`, `HasState`, `Resources`, `SetLevel`), `CurrentHealth/MaxHealth/HealthNormalized/IsAlive`, `ApplyDamage(float)` (pure, no source), `ApplyHeal(float)`, `SetTemplate(...)`/`SetTeamOverride(...)` for pooled spawning. Receipt UnityEvents: `OnDamaged<float>`, `OnHealed<float>`, `OnDied`, `OnModifierApplied/Removed<string>`, `OnAbilityCast<string>` |
| `AbilityCasterBehaviour` | The cast API (`[RequireComponent(AbilityUnitBehaviour)]`). `TryCast(id)`, `TryCastAtUnit(id, unit)`, `TryCastAtPoint(id, point)`, `TryCastTowards(id, dir)` — pick the one matching the ability's Targeting; `GetCooldownNormalized(id)` (1 = just used, 0 = ready); events `OnCastSuccess/OnCastFailed<string>` (`CastFailureReason` name) |
| `AbilityProjectileBehaviour` | Pooled data-driven homing projectile; bind its prefab to the archetype id under the hub's **Archetypes** |

Units exist only between `OnEnable`/`OnDisable`. The minimum damageable unit = `AbilityUnitBehaviour` +
template; add the caster only when it casts. Grant-at-runtime goes through
`AbilitySystemBehaviour.I.System.GrantAbility(unitId, abilityId)` (the caster's list only grants on enable).

## Effect ops (open registry, `AbilityEffectOps`)

Each effect node: `OpId`, `Target` (`Target/Caster/AreaAroundTarget/AreaAroundCaster`), `TeamFilter`,
`Radius`, `MaxTargets`, `Amount`, `DamageType` (`physical`/`magical`/`pure`), `Chance`, op-specific ids.

`damage`, `heal`, `apply_modifier`, `remove_modifier`, `dispel`, `resource_change`, `spawn`, plus motion/
utility atoms: `knockback` (push from caster), `pull` (drag toward caster), `teleport` (blink / pull-to-caster /
`CustomParam="swap"`), `execute` (Amount = fraction of `missing`/`max`/`current` HP per `CustomParam`),
`chain` (nearest-first bounces, `MaxTargets` hops, falloff in `CustomParam`, deterministic). Motion routes
through the world adapter's `TryMoveUnit` seam; `unmovable`/`invulnerable` units stay put. Custom op:
implement `IEffectOperation`, `AbilitySystemBehaviour.I.System.Ops.Register(new MyOp())` once — then it's
authorable in data.

**Live combat properties** read by `DamageService` at damage time (default 0 — contribute via template or
modifier, no wiring): `crit_chance`/`crit_multiplier` (attacker), `evasion_chance` (victim, physical only),
`lifesteal_percent` (attacker, 0..100). Shields: a `shield_hp` property contribution absorbs automatically
before HP (pure damage bypasses).

**Leveled values**: every `Amount` (plus `Radius`, modifier `Duration`) can scale per level and per property
in data — `AmountByLevel[]`, `AmountLevelSource` (`AbilityLevel`/`CasterUnitLevel`/`TargetUnitLevel`),
`AmountScaleProperty` + `AmountScalePerPoint` + `AmountScaleSource`, or `AmountKey` referencing a named
`Specials` entry shared by several nodes. Raise ability level via `System.SetAbilityLevel(unit, id, level)`;
unit level bridges from `Neo.Core.Level.LevelComponent` or `unit.SetLevel(...)`.

## Code-first usage

```csharp
using Neo.Abilities;

// Scene flow: hub + assets already set up in the Inspector.
[SerializeField] private AbilityCasterBehaviour caster;
[SerializeField] private AbilityUnitBehaviour target;

if (!caster.TryCastAtUnit("firebolt", target)) { /* OnCastFailed carried the reason */ }
cooldownImage.fillAmount = caster.GetCooldownNormalized("firebolt");

// Receipts: per-unit UnityEvents, or the global stream:
var system = AbilitySystemBehaviour.I.System;
system.Events.Subscribe(AbilityEvents.TakeDamage, e => SpawnHitNumber(e.Target, e.Amount));
system.Events.SubscribeAny(e => Log(e.EventId, e.Target, e.Amount));   // all events

// Runtime grants / leveling (upgrade cards etc.):
system.GrantAbility(unit.UnitId, "ember");
system.SetAbilityLevel(unit.UnitId, "ember", 2);
```

The domain is pure C# and deterministic (seeded `XorShiftRandom`, injected time — never
`UnityEngine.Random`/`Time`): it runs headless for tests/servers via `new AbilitySystem()` +
`CreateUnit`/`RegisterAbility`/`Cast(CastRequest.AtUnit(...))`/`Tick(dt)`. Area/projectile effects need a
world adapter (the hub in scenes; a fake `IAbilityWorldAdapter` headless). Resource pools reuse
`Neo.Core.Resources.ResourcePoolModel` (`AddPool`, `SetMax`, `Restore`; `SetCurrent(id, value)` is the
direct clamped setter for loads/revives that bypasses the heal gate).

## NoCode trio (designer wiring — see avoid-nocode.md)

`AbilityNoCodeAction` (enum-driven `Execute()` bridge: cast/grant/revoke/level/modifier/damage/heal from
UnityEvents), `AbilityAutoCaster` (auto-cast every ready ability with nearest-target lock-on, `WhenReady`
or `Interval` mode, failure backoff — real logic, fine to reuse from code-first survivor-style games
instead of hand-rolling a fire-when-ready loop), `AbilityCooldownSource` (poll-friendly
`CooldownNormalized`/`ReadyNormalized`/`SecondsRemaining`/`IsReady` for `SetProgress`/`NoCodeBindText`).
When writing C#, call `TryCast*`/`GetCooldownNormalized` directly instead of the action/source bridges.

## Survivor kit — the reference implementation

`Samples~/Demo` ships **SurvivorDemo.unity**: a complete Vampire-Survivors game assembled at runtime from
ONE `SurvivorConfig` ScriptableObject (player/enemy `UnitTemplate`s, auto-cast weapon `AbilityDefinition`s,
slow/DoT/stat `ModifierDefinition`s, upgrade cards, spawn/health ramps, XP curve) on top of Neo.Abilities +
Core resources. **Clip a similar game by authoring new data and swapping the config — never edit the kit
scripts.** Read `Docs/Abilities/SurvivorDemo.md` before building any survivor-like/auto-battler game; copy
its idioms (enemy-as-presentation: MonoBehaviour steers, the ability domain owns health/combat;
config-driven bootstrap; upgrade kinds PermanentModifier/GrantAbility/MaxHealth).

## Pitfalls

- **Spawns/projectiles need an archetype binding** on the hub (`ProjectileArchetypeId`/`ArchetypeId` ->
  prefab) or `RequestSpawn` silently no-ops and impact never happens.
- **`NoTarget` abilities**: the primary target defaults to the caster — use `AreaAroundCaster` (not
  `Target`) for the nuke-around-self pattern, or the caster nukes itself.
- **One hub per world**; a second `AbilitySystemBehaviour` destroys itself.
- With **Auto Tick** off (or `Paused = true`) nothing advances — cooldowns/DoTs freeze until
  `System.Tick(dt)`.
- `AbilityUnitBehaviour.ApplyDamage` is `pure` damage with no source (bypasses armor/resist, no kill
  credit) — route real combat through abilities.
- Regen has two paths: `UnitTemplate.RegenPerSecond` ticks the pool at a fixed rate (works since the
  v10 fix — ignore older docs calling it inert), while the `health_regen`/`mana_regen` **properties** are
  applied per second by `System.Tick` and can be contributed by modifiers. Use the properties when regen
  must scale or be buffable.
