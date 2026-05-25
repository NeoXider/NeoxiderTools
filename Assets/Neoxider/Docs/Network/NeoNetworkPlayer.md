# NeoNetworkPlayer

**What it is:** a player helper that works as a Mirror `NetworkBehaviour` when Mirror is installed and as a plain `MonoBehaviour` in solo mode.

**Where:** `Assets/Neoxider/Scripts/Network/Player/NeoNetworkPlayer.cs`, menu `Neoxider/Network/NeoNetworkPlayer`.

---

## Purpose

`NeoNetworkPlayer` separates local-only and remote-only object setup for player prefabs. Typical local-only objects are cameras, input handlers, and player UI. Remote-only objects are nameplates or visuals that should be hidden for the owning player.

## Inspector

| Field | Description |
|------|-------------|
| `_localOnlyObjects` | Enabled for the local player, disabled for remote players. |
| `_remoteOnlyObjects` | Disabled for the local player, enabled for remote players. |
| `_onLocalPlayerStarted` | Fired after local-player setup. |
| `_onRemotePlayerStarted` | Fired after remote-player setup. |

## Runtime Behavior

- In Mirror mode, local setup runs from `OnStartLocalPlayer`.
- Remote setup runs for non-local clients from `OnStartClient`.
- In solo mode, local setup runs in `Start`.
- Child `AudioListener` components are enabled only for the local player.

## See Also

- [NeoNetworkManager](NeoNetworkManager.md)
- [NeoLobbyPlayer](NeoLobbyPlayer.md)
