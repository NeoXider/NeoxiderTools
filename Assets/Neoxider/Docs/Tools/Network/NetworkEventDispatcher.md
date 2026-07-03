# NetworkEventDispatcher

**What it is:** a NoCode network event bridge that broadcasts a local UnityEvent through Mirror when networking is active.

**Where:** `Assets/Neoxider/Scripts/Network/Core/NetworkEventDispatcher.cs`, menu `Neoxider/Tools/Network/Network Event Dispatcher`.

---

## Purpose

Use `NetworkEventDispatcher` when a local event such as a UI click, trigger, or interaction should invoke the same UnityEvent on all connected clients. In offline mode it simply invokes the event locally.

## Inspector

| Field | Description |
|------|-------------|
| `AuthorityMode` | Defines who may dispatch the event over the network. |
| `onNetworkEvent` | Event fired locally/offline, on the server, and through RPC on clients. |

## API

| Method | Use |
|------|-----|
| `DispatchGlobalEvent()` | Entry point for UnityEvents and UI buttons. |

## Runtime Behavior

- Server/host invokes locally and sends a client RPC.
- Client-only mode sends a command to the server.
- Offline mode invokes `onNetworkEvent` directly.

## See Also

- [NetworkActionRelay](../../Network/NetworkActionRelay.md)
- [NetworkContextActionRelay](../../Network/NetworkContextActionRelay.md)

## Rate Limit (9.6.2)

`CmdDispatchEvent` is protected by `RateLimitCheck()` from `NeoNetworkComponent` (by default no more than
once per 0.05 s per object): a client cannot spam the global broadcast. Overly frequent calls
are silently dropped by the server.
