# ButtonAttribute

**Namespace:** `Neo`. **Assembly:** `Neo.PropertyAttribute`.

Mark a method with `[Button]` to expose an Inspector action. The method remains an ordinary C# API:

```csharp
using Neo;
using UnityEngine;

public sealed class CounterPreview : MonoBehaviour
{
    [SerializeField] private int count;

    [Button("Add", PlayModeOnly = true)]
    public void Add(int amount = 1)
    {
        count += amount;
    }
}
```

## Inspector workflow

Select the component and open its Actions section. The `Neo.Editor.NeoCustomEditor` fallback inspector discovers attributed methods on MonoBehaviours, including scripts outside the `Neo` namespace. A custom editor replacing that fallback must integrate the action renderer itself. The method-only attribute is not attached to a serialized field; a `PropertyDrawer` alone cannot discover and display all method actions.

The main action renderer supports `int`, `float`, `string`, `bool`, Unity Object references, enums, `Vector2`, `Vector3` and `Color` parameters. Optional parameter defaults initialize the controls. Unsupported signatures need a supported wrapper method.

Use `PlayModeOnly = true` for runtime state changes. Edit Mode actions should be safe authoring operations. This option disables the Inspector button outside Play Mode; it does not guard direct method calls.

## Public C# API

`ButtonAttribute(string buttonName = null, float width = 120)` exposes `ButtonName` and `Width`. An empty name uses the method name. `PlayModeOnly` is a named, writable option and defaults to false. Width is metadata; the current main Actions card layout stretches its button to the available Inspector width.

Call `counter.Add(3)` directly for gameplay. The attribute does not schedule or invoke the method at runtime.

See [PropertyAttribute](README.md) and [drawer metadata helpers](InternalTypes.md).
