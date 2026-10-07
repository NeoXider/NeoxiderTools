# NeoNetworkBootstrap

**What it is:** a MonoBehaviour (`Scripts/Network/Bootstrap/NeoNetworkBootstrap.cs`, namespace `Neo.Network`) that decides how a process starts and nothing else: it reads the command line (or the page URL on WebGL), resolves a pure `NeoStartupIntent` (`Scripts/Network/Bootstrap/NeoStartupIntent.cs`), prepares the transport and calls exactly one of `StartAsServer()` / `StartAsHost()` / `StartAsClient()` on the [`NeoNetworkManager`](./NeoNetworkManager.md). It also gives a headless process what an unattended dedicated server needs: no cameras, audio or UI, a frame cap, vSync off, run-in-background, and a periodic status line.

**How to use:**
1. Put `NeoNetworkBootstrap` on an object in the first scene, next to the `NeoNetworkManager`.
2. Leave **Start From Command Line** on. With no networking switches nothing happens and the game stays single player.
3. Launch a dedicated server: `Game.exe -batchmode -nographics -server -port 7777 -maxplayers 10`. Launch a client: `Game.exe -client -address play.example.com -port 7777 -name Neo`.
4. For menu buttons call `StartAsHost()` / `StartAsClient(address)` / `StopNetwork()` from a `UnityEvent` or from code.
5. Optionally register game-specific switches (`-botfill`, `-autoplay`) in **Custom Value / Flag Switches** and read them from `Intent`.

---

## Command-line switches

Switch names are case-insensitive. A value can follow after a space, after `=`, and `--name` works like `-name`.

| Switch | Meaning |
|--------|---------|
| `-host` | Start as host (server + local client). |
| `-server` | Start as dedicated server (no local player). **Command line only.** |
| `-client` | Start as a client. |
| `-address <host>` | Server address for a client (default `localhost`). |
| `-port <n>` | Listen / connect port, clamped to 1..65535. Applied to any `PortTransport` (Telepathy, KCP, SimpleWeb, Multiplex). Without it the transport keeps its own port. |
| `-maxplayers <n>` | Capacity (Mirror `maxConnections`). |
| `-name <text>` | Player display name, available as `Intent.DisplayName`. |
| `-batchmode`, `-nographics` | Unity's own switches. Only used to know that the process is headless. |

Role precedence is **host > server > client**, so an explicit `-host` beats a stray `-server`. Unknown switches are ignored (Unity adds many of its own and a game must never crash on them). A value that is missing or not a number keeps the default; a value that looks like another switch (`-address -host`) is not swallowed.

```text
Game.exe -batchmode -nographics -server -port 7777 -maxplayers 10     # dedicated server
Game.exe -host -name Neo                                              # host that plays
Game.exe -client -address 192.168.0.5 -port 7777 -name=Bob            # client
```

## The pure parser

`NeoStartupCommandLine.Parse` takes the raw arguments and returns an immutable `NeoStartupIntent`; no Unity, no scene, no Mirror session, so the whole decision table is unit-testable.

```csharp
NeoStartupIntent intent = NeoStartupCommandLine.Parse(
    new[] { "-server", "-port", "7778", "-botfill", "6" },
    new NeoStartupDefaults(address: "localhost", port: 7777, maxPlayers: 10),
    customValueSwitches: new[] { "-botfill" },
    customFlagSwitches: new[] { "-autoplay" });

intent.Mode;                         // NeoStartupMode.DedicatedServer
intent.Port;                         // 7778
intent.GetCustomInt("botfill", 4);   // 6
intent.HasCustom("autoplay");        // false
```

| Member of `NeoStartupIntent` | Meaning |
|------------------------------|---------|
| `Mode` | `Solo`, `Host`, `DedicatedServer`, `Client`. |
| `Headless` | Unity was launched with `-batchmode` or `-nographics`. |
| `Address`, `Port`, `MaxPlayers`, `DisplayName` | Resolved values. `Port` and `MaxPlayers` of `0` mean "not specified" (`HasPort`, `HasMaxPlayers`). |
| `IsNetworked`, `IsAuthorityPeer`, `SimulatesLocally`, `SuppressesLocalPresentation` | Convenience flags per mode. |
| `HasCustom(name)`, `TryGetCustom(name, out value)`, `GetCustomInt`, `GetCustomFloat` | Custom switches registered with the parser. Names are matched without dashes, case-insensitive. |
| `WithMode(mode)`, `WithAddress(address)` | Copies with another role / address (a UI button after boot). |

## Dedicated server rules

- **A dedicated server only starts from the command line.** `StartNetwork(intent)` with `DedicatedServer` is refused (and logged) unless the command line asked for it: a "Start dedicated server" button would blank the player's own screen.
- It suppresses local presentation: every `Camera`, `AudioListener`, `Canvas` and `UIDocument` is disabled (also in scenes loaded later), plus the behaviours listed in **Dedicated Server Suppressed Behaviours**. Presenters can also check the static `NeoNetworkBootstrap.LocalPresentationSuppressed` and skip work instead of being destroyed.

## Headless runtime

For a headless process (batch mode or dedicated server):

| Setting | Why |
|---------|-----|
| **Headless Target Frame Rate** (default 60) | A batch-mode server otherwise spins a core at thousands of frames per second. `0` leaves Mirror's own cap (its send rate). The manager's `ConfigureHeadlessFrameRate` override respects the cap, so Mirror does not reset it on server start. |
| **Disable VSync When Headless** | The cap is the only limiter. |

For **any networked peer**: **Run In Background When Networked** sets `Application.runInBackground = true`. An unfocused client or host otherwise stops simulating and looks like a dead peer to the server (and to a verify script).

## Status line

An authority peer logs one line every **Status Log Interval Seconds** (default 10, first one after 3 s, `0` turns it off), by default only in batch mode or on a dedicated server:

```text
[Server] status: mode=DedicatedServer connections=3 ready=3 spawned=3 fps=60 uptime=120s humans=3 bots=4 mobs=120
```

`connections`, `ready` and `spawned` (ready and owns a player) come from Mirror; add game metrics with `bootstrap.StatusExtraProvider = () => $"humans={n}";`. `bootstrap.BuildStatusLine()` returns the same string for your own HUD or log.

## WebGL

A browser tab cannot open raw TCP/UDP sockets and has no command line.

- **Transport:** assign a `SimpleWebTransport` (WebSocket, ships with Mirror) to **WebGL Transport**; WebGL builds then use it instead of the manager's own transport. A server that must accept browsers **and** desktop clients puts both transports (for example Telepathy/KCP and SimpleWeb) into Mirror's `MultiplexTransport`; browsers need `wss://` (TLS) when the page is served over HTTPS.
- **Start-up:** with **Read URL Query On WebGL** the bootstrap reads `-client`, `-address`, `-port`, `-name` from the page URL: `https://game.example.com/?client&address=srv.example.com&port=7778&name=Bob`. Only a **client** can be started this way; `?server` or `?host` in a link is ignored.

The WebGL path is a small, deliberate code path that has not been exercised in a WebGL player build in this repository (the transport and the parser are tested, the player build is not).

## Methods and properties

| Member | Description |
|--------|-------------|
| `bool StartNetwork(NeoStartupIntent intent)` | Starts the role. Returns `true` when a session was started by this call; `false` for solo, a refused dedicated server, a missing manager or a session that already runs. Idempotent. |
| `bool StartAs(NeoStartupMode mode)` | Same, reusing the resolved port, capacity and name. Host or client. |
| `void StartAsHost()`, `StartAsClient()`, `StartAsClient(string address)`, `StopNetwork()` | `UnityEvent`-friendly wrappers. |
| `string BuildStatusLine()` | The status string. |
| `Func<string> StatusExtraProvider` | Game metrics appended to the status line. |
| `NeoStartupIntent Intent`, `NeoStartupMode StartupMode`, `bool HasStartedNetwork` | State. |
| `static NeoStartupIntent Resolve(string[] args[, defaults, valueSwitches, flagSwitches])` | The parser without a component, for tests and tools. |
| `static bool LocalPresentationSuppressed`, `HasInstance`, `OwnsFrameRate`; `static NeoNetworkBootstrap Active` | Static state read by presenters and the manager. |
| `UnityEvent OnNetworkStartedEvent`, `event Action<NeoStartupIntent> IntentResolved` | Notifications. |

A second bootstrap in the scene disables itself and logs an error; the first keeps ownership.

## See also
- [NeoNetworkManager](./NeoNetworkManager.md)
- [Realtime IO Guide](./Realtime_IO_Guide.md)
- [Multiplayer Guide](./Multiplayer_Guide.md)
