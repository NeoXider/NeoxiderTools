# FindInSceneAttribute

**Namespace:** `Neo`. **Assembly:** `Neo.PropertyAttribute`.

`[FindInScene]` marks a Component or GameObject field for scene lookup by its field type.

The attribute has no configuration. The `Neo.Editor` Inspector processor assigns a candidate only when the reference is missing and attribute search is enabled. Selection among multiple candidates is not an application-level ordering contract. Prefer an explicit reference when identity matters.

## Scene authoring with interface requirements

```csharp
using Neo;
using UnityEngine;

public interface ISceneTarget { }
public interface ISceneSelectable { }

public sealed class SceneTargetBinding : MonoBehaviour
{
    [SerializeField, FindInScene]
    [RequireInterface(typeof(ISceneTarget))]
    [RequireInterface(typeof(ISceneSelectable))]
    private GameObject target;
}
```

Add components implementing both requirements to an eligible scene GameObject, enable attribute search and select the binding component. In 11.0, lookup tests every requirement before writing the field. For a GameObject, different components may satisfy different interfaces. If the field instead holds a Component, that Component must itself implement all interfaces; an implementing sibling does not make it a valid candidate.

The first compatible object in the existing scene search order is assigned with Undo. An already assigned nonempty reference is not replaced by automatic lookup. With no compatible candidate the field stays unassigned and is not dirtied. A diagnostic is emitted once per target/field until a successful assignment; later Inspector draws still retry lookup. The change preserves scene search scope and ordering. Collection and other injection attributes retain their own lookup contracts.

## Custom Editor and runtime C# use

`Neo.Editor.ComponentDrawer.ProcessComponentAttributes(component)` uses the same filtering from a custom Editor. Apply the Editor's serialized edits before calling that authoring helper. It does not initialize dependencies in a player: runtime discovery requires an explicit C# query or initialization method. See [ComponentDrawer](../Editor/ComponentDrawer.md) for editor integration.

See [FindAllInScene](FindAllInSceneAttribute.md), [RequireInterface](RequireInterfaceAttribute.md) and the [shared workflow](README.md).
