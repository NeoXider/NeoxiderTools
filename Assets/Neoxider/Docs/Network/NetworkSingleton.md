# NetworkSingleton

**What it is:** the base class for networked global managers (`Scripts/Network/Core/NetworkSingleton.cs`, namespace `Neo.Network`): a singleton that is a Mirror `NetworkBehaviour` when Mirror is installed (so subclasses can use `[SyncVar]`, `[Command]`, `[ClientRpc]`) and a plain `MonoBehaviour` when it is not, so project code does not break when the multiplayer package is absent. Examples: `Money`, `ProgressionManager`, `InventoryManager`.

**How to use:**
1. Derive: `public class ScoreManager : NetworkSingleton<ScoreManager> { ... }` and put it on a scene object (with a `NetworkIdentity` when Mirror is installed).
2. Reach it anywhere with `ScoreManager.I` (or `Instance`); check `ScoreManager.HasInstance` / `TryGetInstance(out ScoreManager manager)` when it may not exist.
3. Guard data-mutating methods with `HasServerAuthority`.

---

## Purpose

Its key trick is **conditional compilation**:
- If Mirror Networking **is installed**, the class inherits from `NetworkBehaviour`, so you can use `[SyncVar]`, `[Command]`, and `[ClientRpc]`.
- If Mirror **is not installed** (or you are building a purely single-player part of the game), it behaves like a plain `MonoBehaviour` with no network dependency.

A plain `Singleton<T>` lives forever in the scene. `NetworkSingleton<T>` registers itself in `Awake` (unless **Set Instance On Awake** is off), destroys a duplicate on the same type (override `DestroyGameObjectOnDuplicateSingleton` to allow several, for example one wallet per save key) and clears its static state on play start.

## API

`NetworkSingleton<T>` is a generic class with no inspector-callable events; it is used from C#.

| Member | Description |
|--------|-------------|
| `static T I` | The singleton instance. Resolved on first access by searching the loaded scenes (inactive objects included, because Mirror keeps scene network objects disabled until a session starts). Returns `null` when none exists. |
| `static T Instance` | Alias of `I`. |
| `static bool HasInstance` | An instance is currently registered. |
| `static bool IsInitialized` | The instance is registered **and** initialized. |
| `static bool TryGetInstance(out T instance)` | Gets the registered instance **without** a scene search. |
| `static void ForgetFailedSearch()` | Forces the next `I` to search the scenes again (see below). |
| `static void DestroyInstance()` | Destroys the instance and clears the reference. |
| `bool HasServerAuthority` | `true` on the server or host (`NetworkServer.active`); always `true` without Mirror. |
| `protected virtual bool DontDestroyOnLoadEnabled`, `SetInstanceOnAwakeEnabled`, `DestroyGameObjectOnDuplicateSingleton` | Behavior switches for subclasses. |
| `protected virtual void Awake()`, `Init()`, `OnDestroy()` | Lifecycle; call `base`. |

### A missing singleton is not remembered for the whole session

`I` searches the scenes when no instance is registered. Before 10.18.0 a failed search was cached until the next play start, so a singleton that appeared later (an additively loaded scene, an object created or enabled one frame after the first caller) was reported missing for ever. Now a miss is trusted only for the rest of the **current frame** and until the next **scene load or unload**; after that `I` searches again. A singleton that registers itself in `Awake` is found immediately regardless. Call `ForgetFailedSearch()` if you create the object by hand and read `I` in the same frame.

> [!TIP]
> Always wrap data-mutating operations (granting money, dealing damage) in an `if (!HasServerAuthority) return;` check. This protects your game from cheaters by preventing clients from mutating important variables locally (the server simply ignores them).

## Example

### A custom counter (Code)

```csharp
using Neo.Network;
#if MIRROR
using Mirror;
#endif

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
        if (!HasServerAuthority)
        {
            return;
        }

        GlobalScore += amount;
    }
}

// anywhere
if (MyScoreManager.HasInstance) MyScoreManager.I.AddScore(10);
```

## See also
- ← [Multiplayer Guide](Multiplayer_Guide.md)
- [NeoNetworkManager](NeoNetworkManager.md)
