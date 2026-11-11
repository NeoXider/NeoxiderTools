# Loot

**What it is:** a `MonoBehaviour` that instantiates a random number of random prefabs around its transform, either
when the object is destroyed during gameplay or on demand. Script: `Scripts/Tools/Components/Loot.cs`, namespace
`Neo.Tools`, assembly `Neo.Tools.Components`.

**How to use:**
1. Add **Neoxider / Tools / Loot** to the object that drops items (an enemy, a chest).
2. Assign `lootItems`, set `dropChance` (0–1) and the `minCount`/`maxCount` range.
3. Set `dropRadius` and choose **Scatter Plane**: `XZ` for 3D, `XY` for 2D.
4. Keep **Spawn On Destroy** for automatic drops, or turn it off and call `DropLoot()` from a death event.

---

## Fields

| Field | Description |
|-------|-------------|
| `lootItems` | Prefabs to pick from; every dropped item is chosen uniformly. Null entries are skipped (they reduce the spawned count). |
| `dropChance` | Probability (0–1) that one `DropLoot()` call drops anything. |
| `spawnOnDestroy` | Drop automatically in `OnDestroy` during gameplay. |
| `minCount` / `maxCount` | Item count range per successful drop; both endpoints are included. Keep them ordered. |
| `dropRadius` | Scatter radius around the transform (0 = exact position). |
| `_scatterPlane` (`ScatterPlane`) | `XZ` (default): horizontal scatter, Y stays at the owner's height. `XY`: 2D scatter, Z stays at the owner's depth. |
| `_useObjectPool` (`UseObjectPool`) | Take drops from the shared `PoolManager` pool instead of `Instantiate`, so frequent drops (XP orbs, coins) make no garbage. Return them with a `Despawner` or `PoolManager.Release`. |

## Destroy-time drops

**Spawn On Destroy** only drops for gameplay destruction:

- never in Edit Mode (editor cleanup must not create scene objects);
- never while the owning scene unloads (`LoadScene`, `UnloadSceneAsync`): objects spawned from `OnDestroy` there
  would leak into the remaining scenes;
- never while the application quits (including leaving Play Mode in the Editor).

Objects in the `DontDestroyOnLoad` scene still drop when they are destroyed during gameplay.

## C# and UnityEvents

```csharp
Loot loot = GetComponent<Loot>();
loot.spawnOnDestroy = false;
loot.ScatterPlane = LootScatterPlane.XY; // 2D game
loot.DropLoot(); // Explicit drop, for example after a confirmed enemy death.
```

`DropLoot()` is public and can also be wired to a UnityEvent. It uses `Instantiate`; spawned objects are independent
scene objects, so their cleanup belongs to the consuming game. Empty or null prefab arrays produce no drops. Explicit
calls remain available outside Play Mode for deliberate authoring code.

## See also

- [Components](README.md)
