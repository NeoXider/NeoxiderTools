# AbilitySystemBehaviour

**What it is:** the scene hub of the ability system. It owns the single `AbilitySystem` instance for the world, ticks it each frame, implements the world adapter (positions and radius queries over registered units), and spawns archetype prefabs (projectiles, zones, summons) through `PoolManager`.

**How to use:** you usually do not add it manually — it auto-creates itself the first time anything touches `AbilitySystemBehaviour.I`. Add it explicitly only when you want to assign **Libraries** and **Archetypes** in the Inspector, or to control ticking.

- Namespace: `Neo.Abilities`
- File: `Assets/Neoxider/Scripts/Abilities/Components/AbilitySystemBehaviour.cs`
- Component menu: `Neoxider/Abilities/Ability System`
- Implements `IAbilityWorldAdapter`. Runs at `DefaultExecutionOrder(-500)` so it initializes before units.

## Fields (Inspector)

| Field | Type | Description |
|-------|------|-------------|
| **Libraries** | `List<AbilityLibrary>` | Ability/modifier catalogs registered on Awake. |
| **Archetypes** | `List<SpawnArchetypeEntry>` | Archetype id -> prefab bindings for projectiles, zones, and summons. |
| **Auto Tick** | bool | Advance the system every frame with `Time.deltaTime`. Disable for manual or server-driven ticking. |

**SpawnArchetypeEntry** — `{ Id, Prefab }`. The id must match an ability's `ProjectileArchetypeId` or a `spawn` op's `ArchetypeId`.

## Key API

| Member | Description |
|--------|-------------|
| `static AbilitySystemBehaviour I` | Scene singleton; auto-created on first access. |
| `AbilitySystem System` | The owned domain system (initialized on demand). Entry point for `Cast`, `Events`, `Modifiers`, `Ops`, catalogs, and units. |
| `bool Paused { get; set; }` | Suspends the auto-tick (modifiers, cooldowns, regen freeze) for menus and pause screens. Manual `System.Tick` calls are unaffected. |
| `void AddLibrary(AbilityLibrary library)` | Registers an additional catalog at runtime (DLC, mods, generated content). |
| `void AddArchetype(string id, GameObject prefab)` | Adds an archetype -> prefab binding at runtime. |
| `AbilityUnitBehaviour GetBehaviour(UnitId id)` | The scene component for a unit id, if it has a scene presence. |
| `bool TryGetPosition(UnitId, out Vector3)` | World adapter: position of a registered unit. |
| `void QueryUnitsInRadius(Vector3, float, List<UnitId>)` | World adapter: unit ids within a radius (used by area effects and projectiles). |
| `bool TryMoveUnit(UnitId, Vector3)` | World adapter: sets a registered unit's transform position (the motion seam for `knockback` / `pull` / `teleport`). Returns `false` for units without a scene presence. |
| `void RequestSpawn(SpawnRequest)` | World adapter: resolves the archetype prefab and spawns it via `PoolManager`, then calls `ISpawnedAbilityEntity.OnSpawned`. |

## Example

**Inspector setup:** add one `AbilitySystem` object, assign your [AbilityLibrary](./AbilityLibrary.md) to **Libraries**, and bind `{ Id = "fireball_projectile", Prefab = <your projectile prefab> }` under **Archetypes**. Give the projectile prefab an [AbilityProjectileBehaviour](./AbilityProjectileBehaviour.md).

**Code (access the system, drive a manual tick):**

```csharp
AbilitySystem system = AbilitySystemBehaviour.I.System;

// Global receipt stream (UI, logging, network replication).
system.Events.SubscribeAny(e => Debug.Log($"{e.EventId} on {e.Target} ({e.Amount})"));

// Manual ticking (Auto Tick disabled):
system.Tick(Time.deltaTime);
```

## Pitfalls

- **One hub per world.** A second instance destroys itself in `Awake`. Do not keep two.
- **Spawns need an archetype binding.** If `ProjectileArchetypeId` / a `spawn` `ArchetypeId` has no matching entry, `RequestSpawn` returns silently and impact never happens.
- **Only registered units have positions.** Area effects and projectile hits use `AbilityUnitBehaviour` transforms; a code-only unit with no behaviour is invisible to radius queries.
- With **Auto Tick** off, nothing advances — call `System.Tick(dt)` yourself, or cooldowns, durations, and DoTs freeze.

## See also

- [AbilityUnitBehaviour](./AbilityUnitBehaviour.md) — units register into the hub
- [AbilityLibrary](./AbilityLibrary.md) — what **Libraries** holds
- [AbilityProjectileBehaviour](./AbilityProjectileBehaviour.md) — the spawned projectile
- Back: [Abilities module](./README.md)
