# ButtonAttribute

**What it is:** `[Button]` (`Scripts/PropertyAttribute/ButtonAttribute.cs`, namespace `Neo`, assembly
`Neo.PropertyAttribute`) turns a method into an Inspector action button. The method stays an ordinary C# API.

**How to use:**
1. Reference `Neo.PropertyAttribute` from your assembly definition and add `using Neo;`.
2. Put `[Button]` (optionally with a label and `PlayModeOnly = true`) on a method of a MonoBehaviour.
3. Select the component and press the button in its **Actions** section.

---


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

Use `PlayModeOnly = true` for runtime state changes. Edit Mode actions should be safe authoring operations. This option disables the Inspector button outside Play Mode (its tooltip says so); it does not guard direct method calls.

A method without parameters is drawn as one button whose tooltip shows the C# signature. A method with parameters adds a "N params" chip next to its button; the chip unfolds one row per parameter, and **Reset** restores the declared defaults. Entered values are kept for the inspector session.

With several objects selected, an instance action runs on each selected object (a static one runs once); one failing object does not stop the others. In Edit Mode every call is recorded with Undo first, so serialized changes the action makes can be undone and mark the object dirty.

## Public C# API

`ButtonAttribute(string buttonName = null, float width = 120)` exposes `ButtonName` and `Width`. An empty name uses the method name. `PlayModeOnly` is a named, writable option and defaults to false. Width is metadata; the current main Actions card layout stretches its button to the available Inspector width.

Call `counter.Add(3)` directly for gameplay. The attribute does not schedule or invoke the method at runtime.

See [PropertyAttribute](README.md) and [drawer metadata helpers](InternalTypes.md).
