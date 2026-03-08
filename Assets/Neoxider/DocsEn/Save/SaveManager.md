# SaveManager

## Overview
`SaveManager` is the runtime core of the `Save` module. It discovers components that implement `ISaveableComponent`, collects fields marked with `[SaveField]`, serializes them into a shared JSON container, and restores them on load.

- **Namespace**: `Neo.Save`
- **Path**: `Assets/Neoxider/Scripts/Save/SaveManager.cs`

## How to use
1. Add a single `SaveManager` component to your scene.
2. Inherit saveable scene components from [`SaveableBehaviour`](./SaveableBehaviour.md) or implement `ISaveableComponent` manually.
3. Mark fields with `[SaveField]`.
4. Implement [`ISaveIdentityProvider`](./ISaveIdentityProvider.md) when the default scene-based identity is not enough.
5. Call `SaveManager.Save()` or `SaveManager.Load()` manually when you need explicit checkpoints.

## What it does
- Registers all active and inactive `MonoBehaviour` instances that implement `ISaveableComponent`.
- Caches only fields marked with `[SaveField]`.
- Writes a single JSON container through `SaveProvider`.
- Calls `OnDataLoaded()` after successful restore.
- Re-registers newly loaded scene objects and removes stale registrations before save/load passes.

## Stable component identity
Current versions no longer use `GetInstanceID()` for persistent save keys.

The manager now resolves identities in this order:
- custom `SaveIdentity` from `ISaveIdentityProvider`;
- otherwise a scene-based identity from [`SaveIdentityUtility`](./SaveIdentityUtility.md);
- then prefixes that identity with the component type full name.

This makes save keys stable across application restarts as long as the object keeps the same scene placement or provides its own identity.

## Public API
- `bool IsLoad`
- `void Register(MonoBehaviour monoObj)`
- `void Unregister(MonoBehaviour monoObj)`
- `void Save()`
- `void Load(List<MonoBehaviour> componentsToLoad = null)`
- `void Save(MonoBehaviour monoObj, bool isSave = false)`
- `void Load(MonoBehaviour monoObj)`

## Typical workflow
1. A `SaveableBehaviour` registers itself in `OnEnable()`.
2. `SaveManager` scans `[SaveField]` fields.
3. `Init()` loads all registered components.
4. `OnApplicationQuit()` saves all registered components.
5. When a new scene loads, only newly discovered saveable components are loaded.

## Notes
- Use explicit save calls for important gameplay events, not just on quit.
- Prefer `ISaveIdentityProvider` for runtime-spawned objects or objects that may move inside the hierarchy.
- If you restructure scene hierarchies after release, verify compatibility with older save files.

## Example
```csharp
using Neo.Save;
using UnityEngine;

public class PlayerStats : SaveableBehaviour, ISaveIdentityProvider
{
    [SaveField("health")] [SerializeField] private int _health = 100;
    [SaveField("coins")] [SerializeField] private int _coins;

    public string SaveIdentity => "player-main";

    public override void OnDataLoaded()
    {
        Debug.Log($"Loaded stats: health={_health}, coins={_coins}");
    }
}
```

## See also
- [`SaveableBehaviour`](./SaveableBehaviour.md)
- [`ISaveIdentityProvider`](./ISaveIdentityProvider.md)
- [`SaveIdentityUtility`](./SaveIdentityUtility.md)
- [`README`](./README.md)
