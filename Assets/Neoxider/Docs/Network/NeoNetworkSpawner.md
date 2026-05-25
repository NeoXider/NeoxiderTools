# NeoNetworkSpawner

**What it is:** a small static helper for spawning and despawning objects in solo or Mirror network mode.

**Where:** `Assets/Neoxider/Scripts/Network/Spawner/NeoNetworkSpawner.cs`.

---

## Purpose

Use `NeoNetworkSpawner` from gameplay scripts that need one call site for offline and multiplayer object creation. In Mirror mode, networked prefabs must be spawned by the active server.

## API

| Method | Use |
|------|-----|
| `Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)` | Instantiates a prefab and calls `NetworkServer.Spawn` when the server is active. |
| `Spawn(GameObject prefab)` | Spawns at zero position with identity rotation. |
| `Despawn(GameObject instance)` | Uses `NetworkServer.Destroy` on server, otherwise `Object.Destroy`. |
| `CanSpawn` | True in solo mode or on the active server. |

## Notes

If Mirror is installed and a `NetworkIdentity` prefab is spawned without an active server, the helper destroys the local instance to avoid client-only ghost objects.

## See Also

- [NeoNetworkManager](NeoNetworkManager.md)
- [NetworkActionRelay](NetworkActionRelay.md)
