# NetworkSingleton

A drop-in replacement for the standard `Singleton<T>` component, built to adapt automatically to a multiplayer environment.

## Purpose

`NetworkSingleton<T>` is the base class global managers derive from (e.g. `ProgressionManager`, `WorldTimeManager`, `InventoryManager`).

Its key trick is **conditional compilation**:
- If Mirror Networking **is installed** in the project, the class automatically inherits from `NetworkBehaviour`, so you can use `[SyncVar]`, `[Command]`, and `[ClientRpc]`.
- If Mirror **is not installed** (or you're building a purely single-player part of the game), it behaves like a plain `MonoBehaviour` with no network dependency — so your project code doesn't break when the multiplayer package is absent.

## Key difference from `Singleton<T>`

A plain `Singleton<T>` lives forever in the scene. In multiplayer, global managers are often duplicated per connection (e.g. an Inventory Manager may exist once per connected player). `NetworkSingleton<T>` resolves these collisions automatically (distinguishing the authority client copy from the server copy), guaranteeing that static access via `Instance` refers to the *local copy* for the current player, not someone else's.

## API

Since `NetworkSingleton<T>` is a generic class, it exposes no inspector-callable events — it's used purely from C#.

| Method / Property | Description |
|-------|----------|
| **`Singleton`** | (Property) Returns the active component instance in the scene. |
| **`HasServerAuthority()`** | Checks whether the calling code owns the object on the server. In solo mode (no Mirror), always returns `true`. |
| **`IsServer()`** | Checks whether this script is running on the server. In solo mode, always returns `true`. |

> [!TIP]
> Always wrap data-mutating operations (granting money, dealing damage) in a `if (!HasServerAuthority()) return;` check. This protects your game from cheaters by preventing clients from mutating important variables locally (the server simply ignores them).

## Example

### A custom counter (Code)

```csharp
using Neo.Network;
using Mirror;

// Our global manager.
public class MyScoreManager : NetworkSingleton<MyScoreManager>
{
#if MIRROR
    // This value auto-updates on every client,
    // but ONLY the server is allowed to change it!
    [SyncVar] 
#endif
    public int GlobalScore;

    public void AddScore(int amount)
    {
        // Check: are we allowed to edit the score?
        if (!HasServerAuthority()) 
        {
            Debug.LogWarning("Only the server can add score!");
            return;
        }

        GlobalScore += amount;
    }
}
```

## See also
- ← [Multiplayer Guide](Multiplayer_Guide.md)
