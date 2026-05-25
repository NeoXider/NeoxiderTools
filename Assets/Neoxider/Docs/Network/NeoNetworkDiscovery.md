# NeoNetworkDiscovery

**What it is:** a NoCode LAN discovery wrapper around Mirror `NetworkDiscovery`.

**Where:** `Assets/Neoxider/Scripts/Network/Lobby/NeoNetworkDiscovery.cs`, menu `Neoxider/Network/Neo Network Discovery`.

---

## Purpose

Use this component to advertise a host on the local network and to discover available servers from clients. It keeps a server list and exposes discovery lifecycle as `UnityEvent`s.

## Events

| Event | When it fires |
|------|---------------|
| `OnServerFound` | A new LAN server response is received. |
| `OnServerListUpdated` | The internal server dictionary changes. |
| `OnAdvertisingStarted` | Server advertising starts. |
| `OnDiscoveryStarted` | Client discovery starts. |

## API

| Method | Use |
|------|-----|
| `StartAdvertising()` | Advertises this host on LAN. |
| `StartDiscovery()` | Clears the list and searches for LAN servers. |
| `StopDiscovery()` | Stops discovery. |
| `ConnectToServer(string address)` | Connects to the given address. |
| `ConnectToFirstServer()` | Quick-joins the first discovered server. |

## Setup

1. Add Mirror `NetworkDiscovery` to the network object.
2. Add `NeoNetworkDiscovery` to the same object.
3. Enable auto advertise/discover if the scene should manage discovery automatically.
4. Wire `OnServerFound` or `OnServerListUpdated` to UI.

## See Also

- [NeoLobbyManager](NeoLobbyManager.md)
- [NeoNetworkManager](NeoNetworkManager.md)
