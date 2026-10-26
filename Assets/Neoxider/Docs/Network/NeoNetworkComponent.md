# NeoNetworkComponent

**What it is:** abstract base class (`NetworkBehaviour` / `MonoBehaviour`) for every networked NoCode component. Removes duplicated boilerplate (rate-limiting, late-join, dispatch). Path: `Scripts/Network/Core/NeoNetworkComponent.cs`, namespace `Neo.Network`.

**How to use:**
1. Derive your component from `NeoNetworkComponent` instead of `NetworkBehaviour`.
2. Declare `[SyncVar]` fields for authoritative state.
3. Override `ApplyNetworkState()` to restore state on late-join.
4. Validate networking, policy and payload first, then call `RateLimitCheck(sender)` before mutation.
5. Use `ShouldDispatchToServer()` / `ShouldBroadcastRpc()` instead of hand-written checks.

---

## Fields

| Field | Type | Description |
|-------|------|--------------|
| `isNetworked` | `bool` | When true, state is replicated over the network. When false, runs locally. |

## Methods

| Method | Returns | Description |
|--------|---------|--------------|
| `RateLimitCheck()` | `bool` | Shared local/server command window; returns true when the command arrived too soon. |
| `RateLimitCheck(NetworkConnectionToClient sender)` | `bool` | Independent window for each remote sender; null/exact host connection use the shared local window. Mirror only. |
| `ApplyNetworkState()` | `void` | Override: applies SyncVar values to local state. Called from `OnStartClient` for non-server clients. |
| `ShouldDispatchToServer()` | `bool` | Returns `true` if the current node is a pure client (should send a Cmd to the server). |
| `ShouldBroadcastRpc()` | `bool` | Returns `true` if the current node is the server (should broadcast an Rpc). |

## Properties

| Property | Type | Description |
|----------|------|--------------|
| `NetworkRateLimit` | `float` (virtual) | Minimum interval between commands (default 0.05s). Override for a custom value. |

## Example

```csharp
using Neo.Network;
#if MIRROR
using Mirror;
#endif

public class MyCounter : NeoNetworkComponent
{
    private int _localValue;
    private void UpdateUI() { /* render _localValue */ }
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
        if (!isNetworked || !NeoNetworkState.IsAuthorized(gameObject, sender, NetworkAuthorityMode.OwnerOnly)
            || RateLimitCheck(sender)) return;
        _syncValue = val;
        _localValue = val;
        RpcSet(val);
    }

    [ClientRpc]
    private void RpcSet(int val) { if (isServer) return; _localValue = val; UpdateUI(); }
#endif

    public void Set(int val)
    {
#if MIRROR
        if (ShouldDispatchToServer()) { CmdSet(val); return; }
#endif
        _localValue = val; UpdateUI();
#if MIRROR
        if (ShouldBroadcastRpc()) { _syncValue = val; RpcSet(val); }
#endif
    }
}
```

## See also
- [NetworkSingleton](NetworkSingleton.md) — base class for singleton managers
- [NoCode Network Spec](NoCode_Network_Spec.md) — conventions (Rule 11)

## RateLimitCheck note

Both overloads use unscaled double time, so a real-time window can reopen during gameplay pause.
Prefer the sender overload for shared scene objects. Each remote connection gets an independent
window per component; channels on that component share their sender's window. Invalid/local-only
ingress should be rejected before budget consumption. The parameterless overload intentionally
retains one shared local/server window. This limiter does not authorize a caller: use a named
[authority policy](Contracts.md) and retain game-specific validation separately.
