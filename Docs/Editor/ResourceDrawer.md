# ResourceDrawer Class

**What it is:** If you mark a field with the `[LoadFromResources]` attribute, `ResourceDrawer` will try to find an asset in the `Resources` folder and assign it to that field, saving you from having to drag it in manually.

**How to use:** mark a field with `[LoadFromResources("Path/Asset")]` or a collection with `[LoadAllFromResources("Folder")]`, keep **Enable Attribute Search** on in **Neoxider > Settings**, then select the component. From a custom Editor call `ResourceDrawer.ProcessResourceAttributes(component)`.

---


## 1. Introduction

`ResourceDrawer` is a static helper class, similar to `ComponentDrawer`, but designed to work with assets in `Resources` folders. It is part of the `NeoCustomEditor` custom inspector and implements the logic for attributes that automatically load assets.

If you mark a field with the `[LoadFromResources]` attribute, `ResourceDrawer` will try to find an asset in the `Resources` folder and assign it to that field, saving you from having to drag it in manually.

---

## 2. Class Description

### ResourceDrawer
- **Namespace**: `Neo.Editor`
- **File path**: `Assets/Neoxider/Editor/PropertyAttribute/ResourceDrawer.cs`

**Description**
A static class implementing the logic for attributes that automatically load assets from `Resources` folders (for example, `[LoadFromResources]`, `[LoadAllFromResources]`).

**Key features**
- **Automatic loading**: Finds assets and assigns them to fields if they are empty (null).
- **Search by path and type**: Can look up an asset by a specific path inside the `Resources` folder, or simply find the first asset of the required type.
- **Collection support**: Can populate arrays or lists (`List<>`) with all found assets.

**Public methods**
- `ProcessResourceAttributes(MonoBehaviour targetObject)`: Loads every missing `[LoadFromResources]` reference and every empty `[LoadAllFromResources]` collection of `targetObject`. Returns `void`; a null target or disabled attribute search is a no-op.

**Behaviour details**
- **Cost**: attributed fields are resolved once per type, so components without these attributes cost one dictionary lookup per Inspector event and allocate nothing.
- **Undo**: single assignments and collections are recorded with Undo (prefab instances keep them as overrides).
- **Missing resources**: when nothing matches, the warning `... not found at Resources path` is logged once per object and field until a resource is found, instead of on every repaint. The Inspector retries the lookup after the next project change (an asset added afterwards is still picked up) and at most every 0.5 s otherwise, instead of running `Resources.FindObjectsOfTypeAll` on every IMGUI event. A direct call of `ProcessResourceAttributes` is never throttled.

## Typed collections

`[LoadAllFromResources("Items")] public List<Sprite> Icons = new();` loads matching assets from Resources-relative path `Items` when the inspected field is null or empty. Arrays retain their declared element type, and generic collection interfaces use a `List<T>` implementation. Already populated fields are preserved. Collection assignments participate in Undo.

The Inspector calls `ResourceDrawer.ProcessResourceAttributes(component)` when attribute search is enabled. A custom Editor may call the same API explicitly; it belongs to the Editor-only assembly. Runtime code can call `Sprite[] icons = Resources.LoadAll<Sprite>("Items");` and initialize its own services without requiring this Inspector workflow.
