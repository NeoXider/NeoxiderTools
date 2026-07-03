# Neo Network Manager

Module for automatic multiplayer session management (host/client) without hand-written networking code. It's the central hub of NeoxiderTools multiplayer, built on top of Mirror Networking.

## Purpose

`NeoNetworkManager` manages the lifecycle of a network game. It lets you start a host (server + client) or connect a client to a server. Fully No-Code compatible: every start method can be wired to a UI button via UnityEvents.

If the Mirror package is removed, the component simply deactivates, letting the game run solo without compile errors.

## Fields

| Field | Description |
|------|----------|
| **[Network Info]** | General network settings block (inherited from Mirror). |
| **Network Address** | IP address the client connects to (defaults to `localhost` / `127.0.0.1`). |
| **[Spawn Info]** | Resource block for network initialization. |
| **Player Prefab** | Player prefab spawned automatically on connect. Must have a `NetworkIdentity`. |
| **Registered Spawnable Prefabs** | Every object (enemies, projectiles, etc.) the server can spawn over the network. |

## Scene Player Template for NoCode

For NoCode projects, the player can live directly in the scene: cameras, UI, UnityEvents, and bindings already wired. Enable **Use Scene Player Template** on `NeoNetworkManager` for this.

| Field | Description |
|------|----------|
| **Use Scene Player Template** | Uses a scene object as the player template instead of a regular Mirror `Player Prefab`. |
| **Scene Player Template** | The scene object with a `NetworkIdentity` and player components. Every NoCode reference is wired on it. |
| **Disable Scene Player Template** | Disables the original template at runtime so only network copies stay active. On by default. |

How it works:

1. The scene player stays a template only.
2. `NeoNetworkManager` disables the template when the network starts.
3. When a player connects, the server temporarily clears the template's `sceneId` before cloning, creates a runtime copy without a `sceneId`, assigns a stable runtime `assetId`, and calls `NetworkServer.AddPlayerForConnection`.
4. Clients register a Mirror spawn handler with the same stable id and build their own copy from the local scene template.

This sidesteps the main problem of a prefab-only approach: NoCode references don't need to be rewired on a prefab asset. Important: every client/build must have the same scene with the same `NeoNetworkManager` and the same `Scene Player Template` assigned.

A regular `Player Prefab` still works fine when the player is a pure prefab asset with no scene-level NoCode references.

## API

Methods are designed to be called from UI buttons or your own scripts.

| Method | Description |
|-------|----------|
| **StartHost()** | Starts the game as a Host. The app becomes both Server and Client (local multiplayer lobby). Callable from code or a UnityEvent. |
| **StartClient()** | Connects to a server at the configured `Network Address`. |
| **StopHost()** | Stops the local server and disconnects every client. |
| **StopClient()** | Disconnects the player from the current server. |

## Examples

### Scene setup (No-Code)
1. Add a "Create Game" and a "Join" button to your Canvas.
2. Create an empty scene object, add `NeoNetworkManager` and a base `Telepathy Transport`.
3. On the "Create Game" button, in the Inspector, wire `OnClick()`: drag in `NeoNetworkManager` and pick `NeoNetworkManager -> StartHost()`.
4. On the "Join" button pick `NeoNetworkManager -> StartClient()`.
Done — your main menu can now form a lobby.

### From a script (Code)
```csharp
using Neo.Network;
using UnityEngine;

public class MyMatchmaker : MonoBehaviour
{
    public void CreateLobby()
    {
        // Check the singleton is available to avoid a NullReference
        if (NeoNetworkManager.Singleton != null)
        {
            NeoNetworkManager.Singleton.StartHost();
        }
    }
}
```

## See also
- ← [Multiplayer Guide](Multiplayer_Guide.md)
- [Official Mirror documentation](https://mirror-networking.gitbook.io/docs)
