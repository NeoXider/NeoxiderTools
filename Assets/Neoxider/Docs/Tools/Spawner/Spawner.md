# Spawner

**Purpose:** A flexible object spawner component. Supports pooling (`PoolManager`), infinite loops, wave-based spawning (Waves), random delays, random rotation, and area spawning within a collider (2D/3D).

## Fields (Inspector)

| Field | Description |
|-------|-------------|
| **Prefabs** | Array of prefabs. One is randomly selected per spawn. |
| **Use Object Pool** | Use `PoolManager` (if present) instead of standard `Instantiate`. |
| **Destroy Delay** | Delay in seconds before auto-destroying (despawning) the object. `0` = do not destroy. |
| **Spawn Limit** | Maximum objects to spawn in `Loop` mode. `0` = infinite. |
| **Min / Max Spawn Delay** | Minimum and maximum time delay between spawning consecutive objects. |
| **Spawn Mode** | `Loop` (continuous spawn) or `Waves` (bursts with pauses). |
| **Base Wave Count** | Number of objects to spawn in the first wave. |
| **Count Per Wave** | How many *more* objects to spawn in each subsequent wave. |
| **Time Between Waves** | Pause duration between waves (in seconds). |
| **Spawn Points** | Array of spawn points. Empty → spawn from the spawner's own transform; with several, a **random** point is chosen per spawn (position and rotation come from the same point). A `Spawn Area` collider takes priority over points. |

## API

| Method / Property | Description |
|-------------------|-------------|
| `void StartSpawn()` | Starts the automatic spawn coroutine (loop or waves). |
| `void StopSpawn()` | Stops the automatic spawn. Modifies `isSpawning`. |
| `GameObject SpawnRandomObject()` | Instantly spawns a random prefab from the list and returns it. |
| `GameObject SpawnById(int prefabId, Vector3 position)` | Instantly spawns a specific prefab by index at the given position. |
| `Transform ResolveSpawnPoint()` | Returns the point for the current spawn: a random non-null entry from `Spawn Points`, or the spawner's own transform when the list is empty. |
| `void Clear()` | Stops spawning and destroys (or pools) all objects previously created by this spawner. |

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

## Planned (TODO)
- **Deny zones:** `_denyAreas` (`Collider[]`) and `_denyAreas2D` (`Collider2D[]`) fields + `_maxRejectionTries`. When resolving a position (especially a random point inside `Spawn Area`), a candidate inside any deny zone is rejected and re-rolled up to `_maxRejectionTries`; plus `bool IsPositionAllowed(Vector3)`. Idea: "spawn in zone A (points/area) but NOT in sub-region B". Today "where allowed" is set by spawn points/area; "where forbidden" is planned.

## See Also
- [PoolManager](PoolManager.md)
- [Despawner](Despawner.md)
- [SimpleSpawner](SimpleSpawner.md)
- ← [Tools/Spawner](../README.md)
