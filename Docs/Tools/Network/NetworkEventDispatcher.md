# NetworkEventDispatcher

**What it is:** a NoCode network event bridge (`Neo.Tools.NetworkEventDispatcher`,
`Scripts/Network/Core/NetworkEventDispatcher.cs`) that broadcasts a local UnityEvent through Mirror when networking
is active. In offline mode it simply invokes the event locally.

**How to use:**
1. Add it to an object with a `NetworkIdentity` (menu `Neoxider/Tools/Network/Network Event Dispatcher`) and tick
   **Is Networked**.
2. Wire a local trigger (UI click, trigger, interaction) to `DispatchGlobalEvent()` or a payload variant.
3. Bind `onNetworkEvent` (or the int/float/string events) to what every peer should run.

---

## Purpose

Use `NetworkEventDispatcher` when a local event such as a UI click, trigger, or interaction should invoke the same
UnityEvent on all connected clients. For several named channels, scopes or per-channel payloads use
[NetworkActionRelay](../../Network/NetworkActionRelay.md).

## Inspector

| Field | Description |
|------|-------------|
| `Is Networked` | Off keeps the dispatcher local-only; the server then also rejects remote dispatch commands. |
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
| `AuthorityMode` | `None` / `OwnerOnly` / `ServerOnly` sender policy (C# property). |

```csharp
NetworkEventDispatcher alarm = siren.GetComponent<NetworkEventDispatcher>();
alarm.onNetworkIntEvent.AddListener(level => SetAlarmLevel(level));
alarm.DispatchGlobalInt(3);
```

## Runtime Behavior

- Server/host invokes locally and sends a client RPC (the host does not receive its own RPC twice).
- Client-only mode sends a command to the server.
- Offline mode, or **Is Networked** off, invokes the event directly.
- The server rejects a dispatch command while its own **Is Networked** flag is off, before the sender's rate
  budget is spent, so an opted-out dispatcher cannot be driven by a queued or crafted command.

## Rate Limit

All dispatch commands are protected by `RateLimitCheck(sender)` from `NeoNetworkComponent` (by default no
more than once per 0.05 s **per connection**, so one spamming client cannot starve others): a client cannot
spam the global broadcast. Overly frequent calls are silently dropped by the server.

## See Also

- [NetworkActionRelay](../../Network/NetworkActionRelay.md)
- [NetworkContextActionRelay](../../Network/NetworkContextActionRelay.md)
- [NeoNetworkComponent](../../Network/NeoNetworkComponent.md)
