# SingletonById

**What it is:** a generic `MonoBehaviour` base that keeps one live registered component per string Id (and per
closed component type); a newer instance with the same Id replaces the older one. Script:
`Scripts/Tools/Managers/SingletonById.cs`, namespace `Neo.Tools`, assembly `Neo.Tools.Managers`.

**How to use:**
1. Create a concrete component inheriting `SingletonById<YourType>`.
2. Add it to the scene and set `_id` before activation / `Awake`.
3. Enable `_dontDestroyOnLoad` for a persistent root GameObject when needed.
4. Read it from C# with `YourType.Get(id)`, `YourType.TryGet(id, out YourType value)` or `YourType.Has(id)`.

---

The generic base is not a concrete menu component. Scene scripts or UnityEvent adapters can use its typed C# lookup API; no NoCode bridge is required.

## Inspector fields

| Field | Description |
|-------|-------------|
| `_id` | Case-sensitive registration key; defaults to `Default`. Configure before `Awake` and keep it fixed while registered. |
| `_dontDestroyOnLoad` | Persist across scene loads; defaults to false. Unity requires a root GameObject. |

## Public API and replacement rules

- `Id` is the read-only configured key.
- `Get(string id)` returns the registered component or null; `Has(string id)` checks for a live component.
- `TryGet(string id, out T instance)` returns `true` with a live component; a destroyed entry yields `false` and a
  real `null` (never a destroyed reference).
- `_dontDestroyOnLoad` only applies in Play Mode.
- Null and empty lookup keys return null / false. A component with a null or empty configured Id is not registered; its warning goes through gated `NeoDiagnostics`.
- Lookups do not scan the scene or create objects. An inactive object that has not run `Awake` is not registered; disabling an already registered object does not unregister it.
- A newly awakened instance with the same Id replaces the old one and schedules the **old GameObject** for destruction. The old object's later `OnDestroy` does not remove the replacement.
- Destroying the current instance removes its registration. Overrides of `Awake()` and `OnDestroy()` must call the base implementation.
- Registration dictionaries are separate for each closed `T` and are cleared at runtime subsystem registration. Objects retained without another `Awake` are not automatically re-registered after such a reset.

## C# example

```csharp
using Neo.Tools;
using UnityEngine;

public sealed class CameraSlot : SingletonById<CameraSlot>
{
    [SerializeField] private Camera _camera;
    public Camera Camera => _camera;
}
```

Configure one instance's Id as `Gameplay` in the Inspector, then resolve it without creating a scene dependency implicitly:

```csharp
CameraSlot slot = CameraSlot.Get("Gameplay");
if (slot != null)
{
    Camera camera = slot.Camera;
    // Use the explicitly authored camera for this slot.
}
```

## See Also

- [Managers](./README.md)
- [Singleton](./Singleton.md)
