# NeoNetworkComponent

**What it is:** abstract base class (`NetworkBehaviour` / `MonoBehaviour`) for every networked NoCode component. Removes duplicated boilerplate (rate-limiting, late-join, dispatch). Path: `Scripts/Network/Core/NeoNetworkComponent.cs`, namespace `Neo.Network`.

**How to use:**
1. Derive your component from `NeoNetworkComponent` instead of `NetworkBehaviour`.
2. Declare `[SyncVar]` fields for authoritative state.
3. Override `ApplyNetworkState()` to restore state on late-join.
4. Call `RateLimitCheck()` as the first line of every `[Command]`.
5. Use `ShouldDispatchToServer()` / `ShouldBroadcastRpc()` instead of hand-written checks.

---

## Fields

| Field | Type | Description |
|-------|------|--------------|
| `isNetworked` | `bool` | When true, state is replicated over the network. When false, runs locally. |

## Methods

| Method | Returns | Description |
|--------|---------|--------------|
| `RateLimitCheck()` | `bool` | Returns `true` if the command arrived too soon (< `NetworkRateLimit`). Call at the start of every `[Command]`. |
| `ApplyNetworkState()` | `void` | Override: applies SyncVar values to local state. Called from `OnStartClient` for non-server clients. |
| `ShouldDispatchToServer()` | `bool` | Returns `true` if the current node is a pure client (should send a Cmd to the server). |
| `ShouldBroadcastRpc()` | `bool` | Returns `true` if the current node is the server (should broadcast an Rpc). |

## Properties

| Property | Type | Description |
|----------|------|--------------|
| `NetworkRateLimit` | `float` (virtual) | Minimum interval between commands (default 0.05s). Override for a custom value. |

## Example

```csharp
public class MyCounter : NeoNetworkComponent
{
#if MIRROR
    [SyncVar] private int _syncValue;

    protected override void ApplyNetworkState()
    {
        _localValue = _syncValue;
        UpdateUI();
    }

    [Command(requiresAuthority = false)]
    private void CmdSet(int val, NetworkConnectionToClient sender = null)
    {
        if (RateLimitCheck()) return;
        _syncValue = val;
        _localValue = val;
        RpcSet(val);
    }

    [ClientRpc]
    private void RpcSet(int val) { if (isServer) return; _localValue = val; UpdateUI(); }
#endif

    public void Set(int val)
    {
        if (ShouldDispatchToServer()) { CmdSet(val); return; }
        _localValue = val; UpdateUI();
        if (ShouldBroadcastRpc()) { _syncValue = val; RpcSet(val); }
    }
}
```

## See also
- [NetworkSingleton](NetworkSingleton.md) — base class for singleton managers
- [NoCode Network Spec](NoCode_Network_Spec.md) — conventions (Rule 11)

## RateLimitCheck note

The limit is tracked **per component instance** (one timer per object on the server), not per client.
On scene objects with `requiresAuthority = false` commands, frequent commands from one client can
drop legitimate commands from others. Per-owner objects are unaffected.
