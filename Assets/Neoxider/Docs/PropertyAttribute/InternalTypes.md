# PropertyAttribute drawers and documentation metadata

The attributes are available in players. The three drawers below compile only with `UNITY_EDITOR`; guard custom-inspector calls accordingly.

| Type | Public helpers / responsibility |
|------|---------------------------------|
| `ButtonAttributeDrawer` | Legacy property-drawer adapter; `FindButtonMethod(Type)`, `GetButtonText(MethodInfo, ButtonAttribute)`, `IsButtonEnabled(ButtonAttribute, bool)`, `GetDefaultValue(Type)`. Method action discovery/rendering in ordinary MonoBehaviour inspectors belongs to `Neo.Editor.CustomEditorBase`. |
| `GUIColorAttributeDrawer` | Draws the field and its expanded children with a scoped GUI tint. `GetColor(GUIColorAttribute, Color)` returns the configured color or the fallback. |
| `RequireInterfaceDrawer` | Draws and validates Object references. `IsValidProperty`, `IsReferenceValid` and `ClearInvalidReference` let custom inspectors use the same checks. See [contract details](RequireInterfaceAttribute.md). |

`Neo.Editor.ComponentDrawer` also uses `IsReferenceValid` to check every stacked interface requirement before assigning a missing single-reference `[FindInScene]` field. See [component assignment](../Editor/ComponentDrawer.md) and [NeoCustomEditor](../Editor/NeoCustomEditor.md) for authoring integration. The method action renderer belongs to [CustomEditorBase](../Editor/CustomEditorBase.md).

`FindButtonMethod(null)` returns null; otherwise the legacy helper returns the first directly attributed method in metadata-token order among methods returned by reflection. It does not enumerate private methods declared on base classes. `GetButtonText` prefers a nonempty custom name, then the method name, then an empty string. These helpers do not invoke actions.

## NeoDocAttribute

`[NeoDoc("PropertyAttribute/README.md")]` associates a component class with documentation relative to the package `Docs/` folder. `Neo.Editor` owns the Inspector documentation UI and file lookup.

The C# constructor `NeoDocAttribute(string docPathRelativeToDocs)` exposes `DocPath`. It trims surrounding whitespace, replaces backslashes with forward slashes and removes leading slashes. Null becomes an empty string. Normalization does not establish that a document exists. The attribute is class-only and is not inherited.

```csharp
[Neo.NeoDoc("PropertyAttribute/README.md")]
public sealed class DocumentedComponent : UnityEngine.MonoBehaviour
{
}
```

See [PropertyAttribute](README.md).
