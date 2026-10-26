# RequireInterface

**Namespace:** `Neo`. **Assembly:** `Neo.PropertyAttribute`.

Apply this field attribute to a serialized Unity Object reference. In the Inspector:

- A `GameObject` must have a component implementing the requested interface.
- A `Component`, `ScriptableObject` or other Object reference must implement the interface itself. An implementing sibling does not make a different component valid.
- An empty reference is allowed. Invalid references are cleared and reported after the field is drawn.
- Mixed values in a multi-object selection are preserved; inspect the targets individually to validate existing mixed references.

## Scene and runtime workflow

```csharp
using Neo;
using UnityEngine;

public interface IDamageable
{
    void TakeDamage(int amount);
}

public sealed class Turret : MonoBehaviour
{
    [SerializeField, RequireInterface(typeof(IDamageable))]
    private GameObject target;

    public void Shoot(int amount)
    {
        if (target != null)
        {
            IDamageable damageable = target.GetComponent<IDamageable>();
            damageable?.TakeDamage(amount);
        }
    }
}
```

Assign a scene GameObject containing an `IDamageable` component. The attribute validates editor assignments; C# assignments and runtime-created objects still require normal contract checks. ScriptableObject configuration should refer to assets rather than scene objects.

For a missing single-reference `[FindInScene]` field, the scene processor checks every stacked `[RequireInterface]` before assigning a candidate. A GameObject may satisfy different interfaces through different components; a Component reference must implement every required interface itself. The first compatible candidate in the existing search order is selected. Existing nonempty references are retained by automatic lookup.

If no candidate satisfies all requirements, the field stays unassigned without being dirtied. The warning is bounded per target/field until a successful assignment; later Inspector draws can still discover a newly available compatible object. This filtering belongs to the single-reference `[FindInScene]` processor, not every injection attribute. See [scene setup](FindInSceneAttribute.md) and the [editor processor contract](../Editor/ComponentDrawer.md).

## Public C# APIs

`new RequireInterface(Type requireType)` retains the interface in `RequireType`. It throws `ArgumentNullException` for null and `ArgumentException` for a non-interface type.

Editor-only helpers on `RequireInterfaceDrawer` support custom inspectors:

| Method | Behavior |
|--------|----------|
| `IsValidProperty(property, targetType)` | Tests whether the property is an Object reference and the type is an interface. |
| `IsReferenceValid(reference, targetType)` | Tests the object/component contract; an empty reference is allowed. |
| `ClearInvalidReference(property, targetType)` | Clears an invalid, non-mixed Object reference and returns whether it changed. Unsupported properties are left intact. |

`ClearInvalidReference` does not log or apply serialized changes. A custom inspector owns its `SerializedObject.Update()` / `ApplyModifiedProperties()` transaction and any feedback. The standard drawer validates after input and preserves the previous GUI tint.

See [PropertyAttribute](README.md).
