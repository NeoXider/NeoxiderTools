# Message handlers, ready broadcast and event channel

**What it is:** the plumbing every game re-writes around Mirror messages (`Scripts/Network/Realtime/NetClientHandlers.cs`, `NetServerHandlers.cs`, `NetReadyBroadcast.cs`, `NetEventChannel.cs`, namespace `Neo.Network.Realtime`, needs Mirror): handler registries that survive reconnects, a send helper that only reaches connections that can receive, and a typed reliable event channel.

**How to use:**
1. Create a `NetClientHandlers` (client messages) and/or `NetServerHandlers` (commands from clients), `Add<T>(handler)` your handlers and call `RegisterNow()` from `Awake`.
2. Send to clients with `NetReadyBroadcast.ToReadyClients(message)` (or a `NetEventChannel<T>` for events); never with `NetworkServer.SendToReady`.
3. Dispose the registries when their owner is destroyed.

---

## The two Mirror behaviors this fixes

1. **A message for a type with no handler disconnects the receiver**, and frames start flowing while the client is still connecting, so a handler must exist before the connection does.
2. **`NetworkClient.Shutdown` / `NetworkServer.Shutdown` clear every handler**, so after "stop, start again" (a reconnect, host-stop-host) they are gone and the first message disconnects the peer.

## NetClientHandlers / NetServerHandlers

| Member | Description |
|--------|-------------|
| `Add<T>(Action<T> handler, bool requireAuthentication = false)` (client) | Adds a handler for the message type `T`. Registers immediately when the set is already live. |
| `Add<T>(Action<NetworkConnectionToClient, T> handler, bool requireAuthentication = true)` (server) | Same for commands from clients; `connection` is the sender. |
| `RegisterNow()` | Registers every handler. Idempotent. Call from `Awake`: an `Update`-gated registration loses the race against the first frames. |
| `UnregisterAll()` | Drops every handler. |
| `Tick()` | Optional per-frame call for projects **without** a `NeoNetworkManager`: re-registers when a session (re)starts. |
| `IsRegistered`, `Count` | State. |
| `Dispose()` | Unregisters and stops following the manager. |

With a `NeoNetworkManager` in the scene nothing else is needed: the registries re-register from `NeoNetworkManager.ClientSessionStarted` / `ServerSessionStarted` (raised from `OnStartClient` / `OnStartServer`, before any message can arrive). Registration uses Mirror's `ReplaceHandler`, so registering twice never logs a "replacing handler" warning. Pass `followNeoManager: false` to the constructor to opt out.

```csharp
public sealed class MoveCommands : MonoBehaviour
{
    private readonly NetServerHandlers _server = new NetServerHandlers();

    private void Awake()
    {
        _server.Add<MoveInput>(OnMove);       // survives host -> stop -> host
        _server.RegisterNow();
    }

    private void OnDestroy() => _server.Dispose();

    private void OnMove(NetworkConnectionToClient sender, MoveInput input) { /* validate, apply */ }
}
```

## NetReadyBroadcast

Static server-side send helpers. A client that has not finished the ready handshake has no spawned objects yet; `isReady` flips on `ReadyMessage` but the spawn burst only goes out once the connection owns a player, so "ready **and** `identity != null`" is the real "can receive" signal (`NeoNetworkState.IsConnectionSpawned`). `NetworkServer.SendToReady` only checks the first flag.

| Member | Description |
|--------|-------------|
| `bool CanReceive(NetworkConnectionToClient conn)` | Ready and owns a player. |
| `int ToReadyClients<T>(T message, int channelId = Channels.Reliable, bool includeLocalHost = true)` | Sends to every such connection; returns how many. `includeLocalHost: false` skips the host's own loopback connection for events the authority already acted on. |
| `bool SendTo<T>(NetworkConnectionToClient conn, T message, int channelId = Channels.Reliable)` | One connection, if it can receive. |
| `int CountReady(bool includeLocalHost = true)` | How many connections a broadcast would reach. |

## NetEventChannel&lt;T&gt;

A typed reliable event: the authority publishes, remote clients receive; the host never receives its own event back.

| Member | Description |
|--------|-------------|
| `NetEventChannel(NetClientHandlers handlers, int channelId = Channels.Reliable)` | Adds its client handler to the registry. |
| `int Publish(T message)` | Authority: sends to every ready, spawned **remote** client; returns how many. No-op without a server. |
| `bool PublishTo(NetworkConnectionToClient conn, T message)` | One remote connection. |
| `event Action<T> Received` | Raised on remote clients only, never on the host. A throwing listener is logged. |
| `int PublishedCount`, `int ReceivedCount` | Counters. |

Act locally first, then publish:

```csharp
OnKill(killer, victim);                                      // host and server update their own state
_kills.Publish(new KillEvent { Killer = killer, Victim = victim });   // remote clients get the feed line
```

## See also
- [NeoNetworkManager](../NeoNetworkManager.md) (events, `IsConnectionSpawned`)
- [NetFraming](./NetFraming.md)
- [Realtime IO Guide](../Realtime_IO_Guide.md)
- [Realtime README](./README.md)
