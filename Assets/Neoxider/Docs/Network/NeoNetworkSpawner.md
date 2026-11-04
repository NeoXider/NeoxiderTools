# NeoNetworkSpawner

**What it is:** a static spawn helper (`Scripts/Network/Spawner/NeoNetworkSpawner.cs`, namespace `Neo.Network`) that
instantiates a prefab and, while a Mirror server is active, spawns it on every client with
`NetworkServer.Spawn`; despawning uses `NetworkServer.Destroy`. Without a session it falls back to plain
`Instantiate` / `Destroy`, so the same call works in single player.

**How to use:**
1. Register the networked prefab with the network manager (Registered Spawnable Prefabs).
2. On the authority (server, host or single player) call `NeoNetworkSpawner.Spawn(prefab, position, rotation)`.
3. Remove the object with `NeoNetworkSpawner.Despawn(instance)`; check `NeoNetworkSpawner.CanSpawn` before
   exposing a spawn action to players.

---

## API

| Member | Description |
|--------|-------------|
| `GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)` | Instantiates and, with an active server, network-spawns. Returns `null` for a null prefab. A prefab with a `NetworkIdentity` spawned without an active server (a pure client) is destroyed again and `null` is returned, so no client-only ghost appears. |
| `GameObject Spawn(GameObject prefab)` | Same at the origin with no rotation. |
| `void Despawn(GameObject instance)` | `NetworkServer.Destroy` with an active server, otherwise `Object.Destroy`. Null is ignored. |
| `bool CanSpawn` | `true` on a server/host and in single player (no Mirror). |

```csharp
if (NeoNetworkSpawner.CanSpawn)
{
    GameObject pickup = NeoNetworkSpawner.Spawn(pickupPrefab, spawnPoint.position, Quaternion.identity);
}
```

## Notes

- Call it on the authoritative side only; a client request goes through a command, a
  [NetworkActionRelay](./NetworkActionRelay.md) channel with **Server Only** scope, or a dedicated component.
- Keep durable game state in dedicated networked components after the object is spawned.

## Related docs

- [NeoNetworkManager](./NeoNetworkManager.md)
- [NetworkActionRelay](./NetworkActionRelay.md)
- [Multiplayer Guide](./Multiplayer_Guide.md)
