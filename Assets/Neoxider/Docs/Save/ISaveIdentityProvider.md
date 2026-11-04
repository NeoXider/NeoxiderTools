# ISaveIdentityProvider

**What it is:** an interface (`Scripts/Save/ISaveIdentityProvider.cs`, namespace `Neo.Save`) that lets a component
provide its own stable save identity instead of relying on the default scene-based identity.

**How to use:** implement `SaveIdentity` on a saveable component (for example a [SaveableBehaviour](SaveableBehaviour.md))
and return a deterministic, unique key such as `player-main`.

---

## Contract
- `string SaveIdentity { get; }`

`SaveManager` checks this interface first. When the property returns a non-empty value, that value becomes the identity part of the save key.

## When to use it
Use this interface when:
- an object is spawned dynamically but should restore the same data every session;
- the object may move inside the hierarchy;
- you need an explicit migration path for older save files;
- a domain-specific key is clearer than a scene path.

## Recommendations
- Keep the value deterministic and stable.
- Avoid `GetInstanceID()`, `Guid.NewGuid()` at runtime, or any per-session random value.
- Prefer readable identifiers such as `player-main`, `settings-ui`, or `quest-log`.

## Example
```csharp
using Neo.Save;
using UnityEngine;

public class PlayerSaveAnchor : SaveableBehaviour, ISaveIdentityProvider
{
    [SaveField("xp")] [SerializeField] private int _xp;

    public string SaveIdentity => "player-main";
}
```

## See also
- [`SaveIdentityUtility`](./SaveIdentityUtility.md)
- [`SaveManager`](./SaveManager.md)
- [`README`](./README.md)
