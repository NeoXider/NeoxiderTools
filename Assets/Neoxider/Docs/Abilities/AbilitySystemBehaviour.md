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
| **Auto Tick** | bool | Advance the system every frame with `Time.deltaTime` (through `AbilitySystem.Advance`). Disable for manual or server-driven ticking. |
| **Time Mode** | `AbilityTimeMode` | `Variable` (default): one domain step of `Time.deltaTime` per frame. `Fixed Ticks`: whole ticks of 1 / **Ticks Per Second** (`FixedStepTimeSource`), the same step every time, for replays, lockstep and deterministic tests. |
| **Ticks Per Second** | int | Tick rate of `Fixed Ticks`. |
| **Trace Enabled** (Diagnostics) | bool | Records `AbilitySystem.Trace` from the start: why reactions and effects fired ([trace](./TriggersAndHooks.md#trace-why-did-this-fire)). |
| **Trace Capacity** (Diagnostics) | int | Entries kept by the trace ring buffer (default 256). |
| **Spatial Cell Size** (Performance) | float | `0` (default) = every radius query scans all registered units, always exact. `> 0` = a per-frame spatial grid of unit positions with this cell size (world units) serves radius queries. Use it with hundreds of units/projectiles; about the typical hit/area radius (2-5) works well. |

**SpawnArchetypeEntry** — `{ Id, Prefab }`. The id must match an ability's `ProjectileArchetypeId`, a `spawn` op's `ArchetypeId` or a `zone` / `projectile` op's `ArchetypeId`.

### Zone visuals

Domain zones (`zone` op, `AbilitySystem.SpawnZone`) with an `ArchetypeId` ask the hub to spawn that prefab with `SpawnRequest.ZoneId`, `Duration`, `Magnitude` = radius and `TargetUnit` = the followed anchor. Put an **AbilityZoneView** (`Neoxider/Abilities/Ability Zone View`) on the prefab: it scales X/Z to the radius (**Mesh Radius** = the mesh radius at scale 1, 0.5 for Unity's cylinder), follows the anchor, applies **Offset**, and returns to the pool on `zone_expired` or when its lifetime ends. Gameplay stays in the domain zone; the view is presentation only.

## Key API

| Member | Description |
|--------|-------------|
| `static AbilitySystemBehaviour I` | Scene singleton; auto-created on first access. |
| `AbilitySystem System` | The owned domain system (initialized on demand). Entry point for `Cast`, `Events`, `Modifiers`, `Ops`, catalogs, and units. |
| `bool Paused { get; set; }` | Suspends the auto-tick (modifiers, cooldowns, regen freeze) for menus and pause screens. Manual `System.Tick` calls are unaffected. |
| `void AddLibrary(AbilityLibrary library)` | Registers an additional catalog at runtime (DLC, mods, generated content). |
| `void AddArchetype(string id, GameObject prefab)` | Adds an archetype -> prefab binding at runtime. |
| `AbilityUnitBehaviour GetBehaviour(UnitId id)` | The scene component for a unit id, if it has a scene presence. |
| `int RegisteredUnitCount` | Number of units with a registered scene presence. |
| `float SpatialCellSize { get; set; }` | Runtime access to **Spatial Cell Size**. Negative or non-finite values become `0` (linear scan). |
| `void InvalidateSpatialCache()` | Makes the next radius query re-read every unit position. Call it after teleporting units by script when queries in the same frame must see the new positions (grid mode only). |
| `bool TryGetPosition(UnitId, out Vector3)` | World adapter: position of a registered unit (always the live transform). |
| `void QueryUnitsInRadius(Vector3, float, List<UnitId>)` | World adapter: appends unit ids within a radius (used by area effects, projectiles and auto-casters). The radius is inclusive and tested with `AbilitySpatialGrid.IsWithinRadius` (exact double-precision distance, so far-away units and huge radii such as `float.MaxValue` behave the same on every platform). Grid and linear modes return the same ids in the same order (the hub's registry enumeration order, which matches registration order only until a unit unregisters) for the same positions and any radius. A null list is ignored. |
| `bool TryMoveUnit(UnitId, Vector3)` | World adapter: sets a registered unit's transform position (the motion seam for `knockback` / `pull` / `teleport`) and moves the unit inside the current spatial grid snapshot, so the next query sees the new position without a rebuild. Returns `false` for units without a scene presence. |
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

// Survivor-scale scenes: serve projectile/area queries from a per-frame grid.
AbilitySystemBehaviour.I.SpatialCellSize = 3f;
```

## Performance

- Radius queries read cached unit transforms. With **Spatial Cell Size** at `0`, every projectile, area effect and auto-caster query is `O(registered units)`; with hundreds of projectiles that is the dominant per-frame cost.
- With **Spatial Cell Size** `> 0` the hub snapshots unit positions once per frame (on the first query) into an [`AbilitySpatialGrid`](./README.md#spatial-queries-for-many-units). Queries then visit only nearby cells. Moves made through the ability system (`TryMoveUnit`, motion ops) and unregistered units (kills, disabled or pooled enemies) update the snapshot in place, so the next query in the same frame already sees them without rebuilding the whole grid. A newly registered unit makes the next query rebuild the snapshot once, which keeps grid results in the linear scan's order. A unit moved by another script later in the same frame is found at its new position from the next frame, or after `InvalidateSpatialCache()`.
- Radius queries (both modes, including the grid rebuild after `InvalidateSpatialCache()` and the in-place update after `TryMoveUnit`), the domain and the event bus do not allocate per cast (instant or projectile), hit, tick or event once their buffers are warm; neither does an `AbilityAutoCaster` pass that casts. `AbilityHotPathAllocationTests` checks these paths with Unity's `GC.Alloc` profiler counter. `RequestSpawn` is the exception: it gets prefab instances from `PoolManager`, which instantiates when a pool is empty, so give projectile and zone archetypes a pool with a suitable initial size.

## Pitfalls

- **One hub per world.** A second instance destroys itself in `Awake`. Do not keep two.
- **Spawns need an archetype binding.** If `ProjectileArchetypeId` / a `spawn` `ArchetypeId` has no matching entry, `RequestSpawn` returns silently and impact never happens.
- **Only registered units have positions.** Area effects and projectile hits use `AbilityUnitBehaviour` transforms; a code-only unit with no behaviour is invisible to radius queries.
- **Grid mode is one frame behind for external moves.** Enable **Spatial Cell Size** only when that is acceptable (typical for hit detection), or call `InvalidateSpatialCache()` after scripted teleports.
- With **Auto Tick** off, nothing advances — call `System.Tick(dt)` yourself, or cooldowns, durations, and DoTs freeze.

## See also

- [AbilityUnitBehaviour](./AbilityUnitBehaviour.md) — units register into the hub
- [AbilityLibrary](./AbilityLibrary.md) — what **Libraries** holds
- [AbilityProjectileBehaviour](./AbilityProjectileBehaviour.md) — the spawned projectile
- Back: [Abilities module](./README.md)
