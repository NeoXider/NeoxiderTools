# AbilitySystemBehaviour

**What it is:** the scene hub of the ability system (`Scripts/Abilities/Components/AbilitySystemBehaviour.cs`, namespace `Neo.Abilities`, menu `Neoxider/Abilities/Ability System`). It owns the world's single `AbilitySystem`, ticks it every frame, implements the world adapter `IAbilityWorldAdapter` over registered units and spawns archetype prefabs through `PoolManager`. It runs at `DefaultExecutionOrder(-500)`, so it initializes before units.

**How to use:**
1. Add one hub to the scene. Without one, it creates itself the first time anything touches `AbilitySystemBehaviour.I`, but then you cannot assign anything in the Inspector.
2. Assign your [AbilityLibrary](./AbilityLibrary.md) assets to **Libraries**.
3. Bind archetype ids to prefabs under **Archetypes** for projectiles, zones and summons.
4. For hundreds of units, set **Spatial Cell Size**.

---

## Fields (Inspector)

| Field | Type | Description |
|-------|------|-------------|
| **Libraries** | `List<AbilityLibrary>` | Ability and modifier catalogs registered on Awake. |
| **Archetypes** | `List<SpawnArchetypeEntry>` | Archetype id to prefab bindings for projectiles, zones and summons. |
| **Auto Tick** | bool | Advance the system every frame with `Time.deltaTime` through `AbilitySystem.Advance`. Disable for manual or server-driven ticking. |
| **Time Mode** | `AbilityTimeMode` | `Variable` (default): one step of `Time.deltaTime` per frame. `Fixed Ticks`: whole ticks of 1 / **Ticks Per Second** (`FixedStepTimeSource`), for replays, lockstep and deterministic tests. |
| **Ticks Per Second** | int | Tick rate of `Fixed Ticks`. |
| **Trace Enabled** (Diagnostics) | bool | Records `AbilitySystem.Trace` from the start: why reactions and effects fired ([trace](./TriggersAndHooks.md#trace-why-did-this-fire)). |
| **Trace Capacity** (Diagnostics) | int | Entries kept by the trace ring buffer (default 256). |
| **Spatial Cell Size** (Performance) | float | `0` (default): every radius query scans all registered units. `> 0`: a per-frame spatial grid with this cell size in world units serves radius queries. About the typical hit or area radius (2-5) works well. |

**SpawnArchetypeEntry** is `{ Id, Prefab, Role, Projectile, Zone }`. The id must match an ability's **Projectile Archetype Id**, or the `ArchetypeId` of a `spawn`, `zone` or `projectile` effect.

### Archetypes without components on the prefab

The **Role** of an archetype entry decides what the hub does with the prefab:

| Role | The prefab | What the hub does |
|------|------------|-------------------|
| `As Is` (default) | Has its own component (`AbilityProjectileBehaviour`, `AbilityZoneView`, `AbilitySummonBehaviour`) or needs none, like a plain effect. | Spawns it unchanged. |
| `Projectile` | Any visual: a sprite, a model, a particle effect. | Adds `AbilityProjectileBehaviour` to each instance and applies the entry's **Projectile** settings: speed, lifetime, hit radius, pierce, plane, orbit. |
| `Zone View` | Any visual. | Adds `AbilityZoneView` and applies the entry's **Zone** settings: plane, mesh radius, offset. |

A prefab that already has the component keeps its own settings. So all projectile and zone tuning can live in one list, and one visual prefab can serve several archetypes with different settings.

### Zone visuals

A domain zone (`zone` op, `AbilitySystem.SpawnZone`) with an `ArchetypeId` asks the hub to spawn that prefab with `SpawnRequest.ZoneId`, `Duration`, `Magnitude` = radius and `TargetUnit` = the followed anchor. Put an **AbilityZoneView** (`Neoxider/Abilities/Ability Zone View`) on the prefab:

- **Plane** `GroundXZ` scales X and Z for a 3D floor disc; `ScreenXY` scales X and Y for a 2D sprite.
- **Mesh Radius** is the mesh radius at scale 1: 0.5 for Unity's cylinder.
- **Offset** is applied on top of the position; the view follows the anchor.
- The view returns to the pool on `zone_expired` or when its lifetime ends.

Gameplay stays in the domain zone; the view is presentation only.

## Key API

| Member | Description |
|--------|-------------|
| `static AbilitySystemBehaviour I` | Scene singleton; created on first access. |
| `static AbilitySystemBehaviour InstanceOrNull` | The hub if one exists, without creating it. |
| `AbilitySystem System` | The owned domain system, initialized on demand: `Cast`, `Events`, `Modifiers`, `Ops`, catalogs and units. |
| `bool Paused { get; set; }` | Suspends the auto-tick for menus and pause screens: modifiers, cooldowns and regeneration freeze. Manual `System.Tick` calls still work. |
| `void AddLibrary(AbilityLibrary library)` | Registers another catalog at runtime: DLC, mods, generated content. |
| `void AddArchetype(string id, GameObject prefab)` | Adds an archetype binding at runtime. `AddArchetype(SpawnArchetypeEntry)` also takes a role and settings. |
| `AbilityUnitBehaviour GetBehaviour(UnitId id)` | The scene component of a unit id, if it has a scene presence. |
| `int RegisteredUnitCount` | Number of units with a registered scene presence. |
| `float SpatialCellSize { get; set; }` | Runtime access to **Spatial Cell Size**. Negative or non-finite values become `0`. |
| `void InvalidateSpatialCache()` | Makes the next radius query re-read every position (grid mode only). Call it after moving units by script when queries in the same frame must see the new positions. |
| `bool TryGetPosition(UnitId, out Vector3)` | World adapter: the live transform position of a registered unit. |
| `void QueryUnitsInRadius(Vector3, float, List<UnitId>)` | World adapter: appends the ids of units within the radius, inclusive. Grid and linear modes return the same ids in the same order. A null list is ignored. See [Performance](#performance). |
| `bool TryMoveUnit(UnitId, Vector3)` | World adapter: sets a registered unit's position (the seam of `knockback`, `pull`, `teleport`) and moves it inside the current grid snapshot. Returns `false` for units without a scene presence. |
| `void RequestSpawn(SpawnRequest)` | World adapter: resolves the archetype prefab, spawns it through `PoolManager` and calls `ISpawnedAbilityEntity.OnSpawned`. |

### Getting what an ability spawned

| Member | Description |
|--------|-------------|
| `event Action<SpawnRequest, GameObject> EntitySpawned` | Raised after the hub spawned an object for an ability. The request carries the owner, ability id, position, direction, target unit and cast id. |
| **On Entity Spawned** (`UnityEvent<GameObject>`) | The same moment for the Inspector. |
| `void AdoptSpawned(SpawnRequest request, GameObject instance)` | Hands the hub an object your own code created (a custom pool, Addressables, a network spawn): the role is applied, the entity is told about the request and `EntitySpawned` is raised. |

```csharp
AbilitySystemBehaviour hub = AbilitySystemBehaviour.InstanceOrNull;
hub.EntitySpawned += (request, spawned) =>
{
    if (request.AbilityId == "summon_wolf")
    {
        spawned.GetComponent<MyWolfBrain>().Follow(hub.GetBehaviour(request.Owner).transform);
    }
};
```

For events that are not spawns (a cast, a hit, a kill), subscribe through `hub.System.Events` or use an [AbilityEventListener](./AbilityEventListener.md).

## Example

**Inspector:** add one `AbilitySystem` object, assign your [AbilityLibrary](./AbilityLibrary.md) to **Libraries**, and bind `{ Id = "fireball_projectile", Prefab = <your projectile prefab> }` under **Archetypes**. Give the projectile prefab an [AbilityProjectileBehaviour](./AbilityProjectileBehaviour.md), or set the entry's Role to `Projectile`.

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

- **Linear mode** (Spatial Cell Size `0`): every projectile, area effect and auto-caster query costs `O(registered units)`. With hundreds of projectiles that is the main per-frame cost.
- **Grid mode** (Spatial Cell Size `> 0`): the first query of a frame snapshots unit positions into an `AbilitySpatialGrid`, and queries visit only nearby cells. Moves through the ability system (`TryMoveUnit`, motion ops) and unregistered units (kills, disabled or pooled enemies) update the snapshot in place. A newly registered unit makes the next query rebuild it once, which keeps grid results in the linear order. A unit moved by another script later in the frame is found at its new position from the next frame, or after `InvalidateSpatialCache()`.
- **Same answers in both modes.** Queries use `AbilitySpatialGrid.IsWithinRadius`: inclusive, exact distance in double precision, so far-away units and huge radii such as `float.MaxValue` behave the same on every platform. Both modes return the same ids in the hub's registry order, which matches registration order until a unit unregisters.
- **No allocation once warm:** radius queries in both modes, grid rebuilds and in-place updates, the domain, the event bus and an `AbilityAutoCaster` pass that casts. `RequestSpawn` is the exception: `PoolManager` instantiates when a pool is empty, so give projectile and zone archetypes a pool with a suitable initial size.

### Spatial grid in a custom adapter

A custom `IAbilityWorldAdapter`, for example on a headless server, can use the same grid:

```csharp
public sealed class ArenaWorldAdapter : IAbilityWorldAdapter
{
    private readonly AbilitySpatialGrid _grid = new AbilitySpatialGrid(cellSize: 3f);
    private readonly Dictionary<UnitId, Vector3> _positions = new Dictionary<UnitId, Vector3>();
    private bool _dirty = true;

    // Your movement code calls this. A known unit is moved inside the grid; a new unit changes the fill
    // order, so the grid is refilled lazily on the next query.
    public void SetPosition(UnitId unit, Vector3 position)
    {
        bool known = _positions.ContainsKey(unit);
        _positions[unit] = position;
        if (!known || _dirty || !_grid.Move(unit, position))
        {
            _dirty = true;
        }
    }

    public void QueryUnitsInRadius(Vector3 point, float radius, List<UnitId> results)
    {
        if (_dirty)
        {
            _grid.Clear();
            foreach (KeyValuePair<UnitId, Vector3> pair in _positions)
            {
                _grid.Add(pair.Key, pair.Value);
            }

            _dirty = false;
        }

        _grid.Query(point, radius, results); // insertion order, inclusive radius, no allocation when warm
    }

    public bool TryGetPosition(UnitId unit, out Vector3 position)
    {
        return _positions.TryGetValue(unit, out position);
    }

    public bool TryMoveUnit(UnitId unit, Vector3 newPosition)
    {
        if (!_positions.ContainsKey(unit))
        {
            return false;
        }

        SetPosition(unit, newPosition);
        return true;
    }

    public void RequestSpawn(SpawnRequest request)
    {
    }
}
```

The grid returns ids in insertion order. An adapter that fills it in the same order as its linear scan keeps area results, and therefore replays, identical. A cell size about the typical query radius works well; a query whose radius spans more cells than there are units falls back to a plain scan.

| `AbilitySpatialGrid` member | Description |
|-----------------------------|-------------|
| `new AbilitySpatialGrid(float cellSize = 4f)` | Creates an empty grid. |
| `CellSize` | Changing it clears the grid. |
| `Count` | Entries added since `Clear`, removed ones included. |
| `Clear()` | Empties the grid. |
| `int Add(UnitId, Vector3)` | Adds an entry and returns its insertion index. |
| `Query(Vector3 point, float radius, List<UnitId> results)` | Appends matching ids; does not clear `results`. |
| `bool Move(UnitId, Vector3)`, `bool Remove(UnitId)` | Relocate or remove an added unit between fills, keeping every other entry's order. They return `false` for an id not added since the last `Clear`, or already removed. Cost: O(1) plus one cell; the first call after a fill indexes the ids once, O(`Count`). |
| `static bool IsWithinRadius(position, center, radius)` | The membership test: inclusive, a negative radius acts like its absolute value, NaN never matches, distance in double precision. |
| `static double SqrDistance(a, b)` | That squared distance as a `double`, finite for finite inputs. The auto-caster, capped areas and `chain` rank the nearest candidates with it. |

Use `IsWithinRadius` in a custom linear path too, so both paths agree for far-away coordinates and huge radii. A float `(a - b).sqrMagnitude <= r * r` overflows to infinity beyond about `1.8e19`, and whether `r * r` overflows depends on whether the runtime keeps float intermediates in double precision.

```csharp
// Linear fallback that returns exactly what the grid returns:
foreach (KeyValuePair<UnitId, Vector3> pair in _positions)
{
    if (AbilitySpatialGrid.IsWithinRadius(pair.Value, point, radius))
    {
        results.Add(pair.Key);
    }
}
```

## Pitfalls

- **One hub per world.** A second instance destroys itself in `Awake`.
- **Spawns need an archetype binding.** If an archetype id has no matching entry, `RequestSpawn` returns silently and the impact never happens.
- **Only registered units have positions.** Area effects and projectile hits use `AbilityUnitBehaviour` transforms; a code-only unit is invisible to radius queries.
- **Grid mode is one frame behind for external moves.** Use it when that is acceptable, which is typical for hit detection, or call `InvalidateSpatialCache()` after scripted teleports.
- **With Auto Tick off nothing advances.** Call `System.Tick(dt)` yourself, or cooldowns, durations and DoTs freeze.

## See also

- [AbilityUnitBehaviour](./AbilityUnitBehaviour.md) — units register into the hub
- [AbilityLibrary](./AbilityLibrary.md) — what **Libraries** holds
- [AbilityProjectileBehaviour](./AbilityProjectileBehaviour.md) — the spawned projectile
- Back: [Abilities module](./README.md)
