# NetworkOwnerFilter

**What it is:** a `NetworkBehaviour` / `MonoBehaviour` component that filters an action by network role (LocalPlayer, Server, Everyone). Path: `Scripts/Network/Core/NetworkOwnerFilter.cs`, namespace `Neo.Network`.

**How to use:**
1. Add `NetworkOwnerFilter` to a GameObject.
2. Pick a **Mode** (LocalPlayerOnly, ServerOnly, Everyone).
3. Wire a trigger to `Filter()`, and the action to `onAllowed`.

---

## Fields

| Field | Description |
|------|----------|
| **Mode** | Filter mode: `LocalPlayerOnly`, `ServerOnly`, `Everyone`. |
| **On Allowed** | UnityEvent — fired when the current role passes the filter. |
| **On Denied** | UnityEvent — fired when the current role does NOT pass the filter. |

## Modes (OwnerFilterMode)

| Mode | Description |
|-------|-------------|
| **LocalPlayerOnly** | Passes only if the current client owns the object (isLocalPlayer / isOwned). |
| **ServerOnly** | Passes only if the current environment is the server (or host). |
| **Everyone** | Always passes (a no-op, useful for readability in a chain). |

## API

| Method | Description |
|-------|-------------|
| **Filter()** | Checks the role and fires `onAllowed` or `onDenied`. Wire this to a trigger. |
| **IsAllowed()** | Returns `true`/`false` without firing events. For use from code. |

## Examples

### Only the local player opens their inventory
```
Button OnClick() → NetworkOwnerFilter.Filter()
                    ├── onAllowed → InventoryUI.Show()
                    └── onDenied → (nothing)
```
Mode = `LocalPlayerOnly`. Only the prefab's owner sees their own inventory.

### Only the server spawns enemies
```
Timer.OnInterval() → NetworkOwnerFilter.Filter()
                     ├── onAllowed → Spawner.Spawn()
                     └── onDenied → (nothing)
```
Mode = `ServerOnly`. Enemies are created on the server only; clients receive them via Mirror.

### Chained with NetworkActionRelay
```
InteractiveObject.OnInteract()
  → NetworkOwnerFilter.Filter() [LocalPlayerOnly]
      → onAllowed → NetworkActionRelay.Trigger() [AllClients]
          → onTriggered → Door.Open()
```
The filter guarantees only the owner sends the Command, and the Relay broadcasts to everyone.

## Without Mirror (Offline)
In solo mode, `Filter()` always fires `onAllowed`.

## See also
- [NetworkActionRelay](NetworkActionRelay.md) — networked action broadcast
- [NoCode Network Spec](NoCode_Network_Spec.md) — conventions
