# Spawner

**What it is:** Scene `MonoBehaviour` (a `NeoNetworkComponent`) that produces prefabs in a loop or in waves, with random delays/rotation, spawn points, 2D/3D spawn areas with deny zones, pooling and optional Mirror replication. `Scripts/Tools/Spawner/Spawner.cs`, namespace `Neo.Tools`.

**How to use:**
1. Add `Spawner`, fill **Prefabs** and choose **Spawn Mode** (`Loop` or `Waves`).
2. Optionally assign **Spawn Points** or a **Spawn Area** collider, and **Deny Areas**.
3. Enable **Spawn On Awake**, or call `StartSpawn()`/`StopSpawn()` from UnityEvents or C#; subscribe to `OnObjectSpawned` / `OnWaveStarted`.

---

## Fields (Inspector)

| Field | Description |
|-------|-------------|
| **Prefabs** | Array of prefabs. One is randomly selected per spawn. |
| **Use Object Pool** | Use `PoolManager`, or automatic fallback pooling when no manager exists. Disable for standard `Instantiate`. |
| **Destroy Delay** | Delay in seconds before auto-destroying (despawning) the object. `0` = do not destroy. |
| **Spawn Limit** | Maximum automatic spawn attempts in `Loop` mode. Failed/null prefab attempts also consume this limit. `0` = infinite. |
| **Min / Max Spawn Delay** | Minimum and maximum time delay between spawning consecutive objects. |
| **Spawn Mode** | `Loop` (continuous spawn) or `Waves` (bursts with pauses). |
| **Base Wave Count** | Number of objects to spawn in the first wave. |
| **Count Per Wave** | How many *more* objects to spawn in each subsequent wave. |
| **Time Between Waves** | Pause duration between waves (in seconds). |
| **Spawn Points** | Array of spawn points. Empty → spawn from the spawner's own transform; with several, a **random** point is chosen per spawn (position and rotation come from the same point). A `Spawn Area` collider takes priority over points. |
| **Spawn Area / Spawn Area 2D** | Optional `Collider` / `Collider2D` — spawn a random point inside the collider bounds instead of using `Spawn Points`. Takes priority over `Spawn Points` when assigned. |
| **Prefab Weights** | Relative weight per prefab, in the order of **Prefabs**. Empty or missing entries count as 1; 0 never spawns. |
| **Prefab Unlock Seconds** | Seconds after `StartSpawn` before each prefab may spawn (tougher enemies join later). Missing entries count as 0. While nothing is unlocked the loop waits. |
| **Delay Over Time** | Multiplier on the spawn delay over seconds since `StartSpawn` (x = seconds, y = multiplier). Empty = 1; a curve falling to 0.3 spawns three times as often. |
| **Max Active** | The loop waits while this many spawned objects are active. 0 = no cap. |
| **Around Target** / **Ring Min/Max Radius** / **Ring Plane** | Spawn on a ring around a transform (the player in a survivor game), uniform over the ring area, in `XY` (2D) or `XZ` (3D). Wins over spawn points; a spawn area collider still wins over the ring; deny zones re-roll ring points too. |

C#: `PickPrefabIndex()`, `ElapsedSpawnSeconds`, `AroundTarget`, `MaxActive`, `SetRing(min, max, plane)`, `SetSelection(weights, unlockSeconds)`, `SetDelayOverTime(curve)`. The math is the pure `SpawnPlanner` (`PickWeighted`, `RingPoint`, `DelayMultiplier`), usable from game code and covered by `SpawnPlannerTests`.
| **Deny Areas / Deny Areas 2D** | `Collider[]` / `Collider2D[]` — zones where spawning is forbidden. A candidate position inside any deny zone is rejected and re-rolled (see `IsPositionAllowed`/`Max Rejection Tries` below). Only applies when resolving a position via `Spawn Area`/`Spawn Area 2D`. |
| **Max Rejection Tries** | How many times to re-roll a candidate that landed inside a deny zone before giving up and using the last candidate anyway (`Min(1)`). |
| **Max Waves** | Maximum number of waves in `Waves` mode. `0` = infinite waves. |
| **Spawn On Awake** | Starts spawning automatically from `Start()`. The serialized field name is retained for compatibility. |
| **Parent Transform** | Optional parent for spawned objects. `null` = spawn without a parent: unparented in the active scene through `PoolManager`, under the `[SpawnUtility Pools]` root on the fallback pool path, or at the scene root without pooling. |

## API

| Method / Property | Description |
|-------------------|-------------|
| `void StartSpawn()` | Starts one automatic production generation (loop or waves). A callback may Stop/Start it immediately; only the newest generation publishes its coroutine handle and continues. |
| `IEnumerator SpawnObjects()` | Manual coroutine entry. Callers starting this enumerator themselves must own and cancel that coroutine before restarting; `StopSpawn()` directly cancels only the routine started by `StartSpawn()`. |
| `void StopSpawn()` | Cancels the current automatic spawn coroutine immediately. Pending delayed despawns and existing objects remain. A later `StartSpawn()` begins one new loop. |
| `GameObject SpawnRandomObject()` | Instantly spawns a random prefab from the list and returns it. |
| `GameObject SpawnById(int prefabId, Vector3 position)` | Instantly spawns a specific prefab by index at the given position. |
| `GameObject SpawnObject(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)` | Direct typed spawn path with an explicit prefab, world pose and parent. Null prefab returns null. |
| `Transform ResolveSpawnPoint()` | Returns the point for the current spawn: a random non-null entry from `Spawn Points`, or the spawner's own transform when the list is empty. |
| `Vector3 GetSpawnPosition()` | Resolves the position for the current spawn: a random point inside `Spawn Area`/`Spawn Area 2D` (re-rolled against deny zones) when assigned, otherwise the active spawn point's position. Allocation-free. |
| `bool IsPositionAllowed(Vector3 position)` | Returns `false` if the position is inside any configured `Deny Areas`/`Deny Areas 2D` collider. With no deny zones configured, always returns `true`. |
| `void Clear()` | Stops spawning and destroys (or pools) all objects previously created by this spawner. |
| `int GetActiveObjectCount()` | Prunes destroyed/returned/reissued rentals and counts the current objects active in the hierarchy. Allocation-free. |
| `List<GameObject> SpawnedObjects` | Current tracked object list; pooled instances are deduplicated by object and tracked by spawn generation (a reused rental appears once). Counting and periodic spawn cleanup prune stale entries. Treat it as read-only. |

`Clear()` returns only the generations issued to this spawner. If another spawner has already reused an old pooled instance, that newer rental remains active. Release callbacks may spawn new objects; those new objects remain tracked after the clear. An expired delay from an older generation cannot remove or despawn the newer rental.

Spawn loops stop on disable and require `StartSpawn()` to resume. Delayed despawns run on this component: disabling its GameObject or destroying the spawner cancels those Unity coroutines, so keep the spawner active until they finish or call `Clear()` explicitly. Runtime Inspector buttons are Play Mode only.

In 11.0, Stop/Start, Clear or disable during a spawn/wave callback invalidates the old producer immediately, including the synchronous first `StartCoroutine` step. A superseded producer cannot overwrite the newer handle, consume its limit, increment its wave, or clear its running flag. Its remaining callback/RPC tail is suppressed after invalidation; already-created objects remain tracked, and Stop still preserves their independent expiry. Public manually-started `SpawnObjects()` enumerators retain caller-owned coroutine cancellation.

Fixed waits reuse one `WaitForSeconds` per spawner: equal **Min / Max Spawn Delay**, **Time Between Waves** and **Destroy Delay** allocate the wait once instead of on every spawn (a random delay still needs a new wait per spawn). Timing is unchanged.

Capsule areas honor X/Y/Z direction, center, rotation and collider scale. Box/Sphere (3D) and Box/Circle (2D) areas are also supported. Deny-zone retries are bounded: if every candidate is rejected, the last candidate is still used as configured.

With Mirror networking enabled, clients request server-authoritative spawns and host events fire once. Returning a registered server rental to its pool invokes Mirror unspawn after release callbacks and before storage, including through `Clear()` and delayed despawn. Remote client pooling requires Mirror's own spawn/unspawn handlers. A local return on a remote client does not request authoritative removal.

## Unity Events

| Event | Arguments | Description |
|-------|-----------|-------------|
| `OnObjectSpawned` | `GameObject` | Fired every time an object is successfully spawned. |
| `OnWaveStarted` | `int waveIndex` | Fired when a new wave starts, passing the wave number. |
| `OnWaveObjectSpawned` | `GameObject, int` | Fired when an object is spawned during a wave (passes the object and wave index). |

## Examples

### No-Code Example (Inspector)
Set the component to `Waves` mode, `Base Wave Count = 3`, and `Count Per Wave = 2`. Enable `Spawn On Awake`. Link a UI text script to the `OnWaveStarted` event to display "Wave N Started!" on the screen.

### Code Example
```csharp
[SerializeField] private Spawner _enemySpawner;

public void StartBossFight()
{
    // Instantly spawn the first enemy in the center
    _enemySpawner.SpawnById(0, Vector3.zero);
    
    // Start infinite backup spawning
    _enemySpawner.StartSpawn();
}
```

## See Also
- [PoolManager](PoolManager.md)
- [Despawner](Despawner.md)
- [SimpleSpawner](SimpleSpawner.md)
- [Tools/Spawner](README.md)
