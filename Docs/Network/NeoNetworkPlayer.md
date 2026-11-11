# NeoNetworkPlayer

**What it is:** a player component (`Scripts/Network/Player/NeoNetworkPlayer.cs`, namespace `Neo.Network`) that
separates the local player from remote copies: it switches local-only and remote-only objects, keeps a single
active `AudioListener` and raises events when the object starts as the local or a remote player. With Mirror it
is a `NetworkBehaviour`; without Mirror it is a `MonoBehaviour` that always reports the local player.

**How to use:**
1. Add a Mirror `NetworkIdentity` and `NeoNetworkPlayer` to the player prefab (or the scene player template).
2. Put the camera, input and HUD objects into **Local Only Objects** and name tags into **Remote Only Objects**.
3. Bind **On Local Player Started** / **On Remote Player Started** for anything else that differs per role.

---

## Fields

| Field | Description |
|-------|-------------|
| **Local Only Objects** | Enabled for the local player, disabled on remote copies (camera, input, HUD). |
| **Remote Only Objects** | Disabled for the local player, enabled on remote copies (name tag above the head). |
| **Debug Lifecycle Log** | Logs which role started. |

## API

| Member | Description |
|--------|-------------|
| `bool IsLocalPlayer` | This copy is controlled here (`isLocalPlayer`); always `true` without Mirror. |
| `bool HasAuthority` | This peer owns the object (`isOwned`); always `true` without Mirror. |
| `UnityEvent OnLocalPlayerStarted` | Raised from `OnStartLocalPlayer` (from `Start` without Mirror). |
| `UnityEvent OnRemotePlayerStarted` | Raised from `OnStartClient` on copies that are not the local player. |

Every child `AudioListener` is enabled for the local player and disabled on remote copies, even when it is not
listed in **Local Only Objects**, so a remote player's camera never adds a second listener.

> Objects under **Local Only Objects** are inactive on remote copies. Do not target them with replicated
> actions (for example a [NetworkContextActionRelay](./NetworkContextActionRelay.md) target) that remote players
> must see.

## Related docs

- [NeoNetworkManager](./NeoNetworkManager.md)
- [NetworkOwnerFilter](./NetworkOwnerFilter.md)
- [NetworkPlayerName](./NetworkPlayerName.md)
- [Multiplayer Guide](./Multiplayer_Guide.md)
