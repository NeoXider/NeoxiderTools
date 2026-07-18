# NetworkEventDispatcher

**What it is:** a NoCode network event bridge that broadcasts a local UnityEvent through Mirror when networking is active.

**Where:** `Assets/Neoxider/Scripts/Network/Core/NetworkEventDispatcher.cs`, menu `Neoxider/Tools/Network/Network Event Dispatcher`.

---

## Purpose

Use `NetworkEventDispatcher` when a local event such as a UI click, trigger, or interaction should invoke the same UnityEvent on all connected clients. In offline mode it simply invokes the event locally.

## Inspector

| Field | Description |
|------|-------------|
| `AuthorityMode` | Defines who may dispatch the event over the network. Default `None`. |
| `onNetworkEvent` | Parameterless event fired locally/offline, on the server, and through RPC on clients. |
| `onNetworkIntEvent` | Fired on all clients by `DispatchGlobalInt(int)`. |
| `onNetworkFloatEvent` | Fired on all clients by `DispatchGlobalFloat(float)`. |
| `onNetworkStringEvent` | Fired on all clients by `DispatchGlobalString(string)`. |

## API

| Method | Use |
|------|-----|
| `DispatchGlobalEvent()` | Broadcasts the parameterless `onNetworkEvent` to everyone. Entry point for UnityEvents and UI buttons. |
| `DispatchGlobalInt(int)` | Broadcasts an int payload (`onNetworkIntEvent`). |
| `DispatchGlobalFloat(float)` | Broadcasts a float payload (`onNetworkFloatEvent`). |
| `DispatchGlobalString(string)` | Broadcasts a string payload (`onNetworkStringEvent`). |

## Runtime Behavior

- Server/host invokes locally and sends a client RPC.
- Client-only mode sends a command to the server.
- Offline mode invokes `onNetworkEvent` directly.

## See Also

- [NetworkActionRelay](../../Network/NetworkActionRelay.md)
- [NetworkContextActionRelay](../../Network/NetworkContextActionRelay.md)

## Rate Limit (9.6.2)

All dispatch commands are protected by `RateLimitCheck(sender)` from `NeoNetworkComponent` (by default no
more than once per 0.05 s **per connection**, so one spamming client cannot starve others): a client cannot
spam the global broadcast. Overly frequent calls are silently dropped by the server.
