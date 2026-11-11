# ComponentDrawer Class

**What it is:** For example, if you mark a field with the `[GetComponent]` attribute, it is `ComponentDrawer` that performs `GetComponent()` and assigns the found component to that field right in the editor, saving you from having to do it manually.

**How to use:** mark a field with `[GetComponent]`, `[GetComponents]`, `[FindInScene]` or `[FindAllInScene]`, keep **Enable Attribute Search** on in **Neoxider > Settings**, then select the component: the Inspector fills the empty field. From a custom Editor call `ComponentDrawer.ProcessComponentAttributes(component)`.

---


## 1. Introduction

`ComponentDrawer` is a static helper class that is part of the `NeoCustomEditor` custom inspector. Its job is to implement the logic for attributes that automatically find and assign component references.

For example, if you mark a field with the `[GetComponent]` attribute, it is `ComponentDrawer` that performs `GetComponent()` and assigns the found component to that field right in the editor, saving you from having to do it manually.

---

## 2. Class Description

### ComponentDrawer
- **Namespace**: `Neo.Editor`
- **File path**: `Assets/Neoxider/Editor/PropertyAttribute/ComponentDrawer.cs`

**Description**
A static class implementing the logic for automatic component lookup attributes (such as `[GetComponent]`, `[FindInScene]` and their variants that find all matching components).

**Key features**
- **Automatic assignment**: Finds components and assigns them to fields if they are empty (null).
- **Collection support**: Can populate not only single fields but also arrays or lists (`List<>`).
- **Multiple search sources**:
  - On the same `GameObject` (`[GetComponent]`, `[GetComponents]`)
  - In all child objects (`SearchInChildren = true`)
  - Across the entire scene (`[FindInScene]`, `[FindAllInScene]`)

**Public methods**
- `ProcessComponentAttributes(MonoBehaviour targetObject)`: Fills every null field and empty collection of `targetObject` that carries a component-lookup attribute. Returns `void`; a null target or disabled attribute search is a no-op.

**Behaviour details**
- **Cost**: the attributed fields of each type are resolved once per domain reload. The Inspector calls this method on every IMGUI event of every MonoBehaviour it shows, so a type without lookup attributes costs one dictionary lookup and allocates nothing; assigned fields are only read.
- **Undo and prefabs**: every assignment (single reference or collection) is recorded with Undo, so it can be undone and is kept as a prefab-instance override.
- **Diagnostics**: a field whose type is not a `Component`/`GameObject`, or a `[FindInScene]` without a compatible candidate, is reported once per object and field until it resolves, instead of on every repaint.
- **Inspector throttle**: the Neo inspector calls the lookups through `InjectionLookupThrottle`. A lookup that found nothing is not repeated on every IMGUI event: it runs again after the next hierarchy change (a newly added object is still found on the next draw) or after 0.5 s at the latest. A direct call of `ProcessComponentAttributes` is never throttled.
- **Multi-object selection**: with several objects selected the inspector processes every selected object, not only the first one.

## Collection authoring and C# use

`[GetComponents]`, `[GetComponents(true)]`, and `[FindAllInScene]` populate null or empty arrays and lists with their declared element type. For example, `BoxCollider[]` receives a `BoxCollider[]` and `List<BoxCollider>` receives a `List<BoxCollider>`. Generic collection interfaces accepted by the lookup are backed by `List<T>`. Non-empty collections retain their existing references. When the lookup finds nothing and the collection is already empty, the field is left as is: no Undo record and no dirty flag, so merely viewing the inspector does not mark the scene as modified.

In a scene component, declare fields such as `[GetComponents(true)] public List<Rigidbody> Bodies = new();` with `using System.Collections.Generic;`. Select the component in the Inspector to populate it. Attribute search must be enabled in **Neoxider > Settings**. Collection assignments participate in Undo.

A custom Editor can call `Neo.Editor.ComponentDrawer.ProcessComponentAttributes(component)` after its serialized edits are applied. For runtime initialization, use the typed Unity API directly, for example `Rigidbody[] bodies = component.GetComponentsInChildren<Rigidbody>();`.

## Scene lookup with interface requirements

For a null single-reference `[FindInScene]` field, lookup checks every stacked `[RequireInterface(typeof(IMyInterface))]` before assignment. A `Component` reference must itself implement all required interfaces. A `GameObject` must contain components satisfying each requirement; different components may satisfy different interfaces. The first compatible candidate in the existing scene search order is assigned with Undo. Search scope and ordering are unchanged.

Existing explicit references remain untouched by automatic lookup. If no candidate satisfies every requirement, the field stays null, is not dirtied, and the diagnostic is emitted once for that object/field until a successful assignment. Subsequent Inspector draws can still find a compatible component added later. This avoids repeatedly assigning and clearing an incompatible reference. Manual Inspector assignment retains the normal `RequireInterface` validation.

`ProcessComponentAttributes(component)` applies the same filtering when called from custom Editor C#. It is an authoring helper; runtime composition should pass an explicitly typed dependency through its normal initialization API.

Since 11.0 `[FindInScene]` checks every `RequireInterface` before assigning, and automatic lookup never replaces a non-empty manual reference.
