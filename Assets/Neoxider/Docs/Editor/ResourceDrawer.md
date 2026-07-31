# ResourceDrawer Class

**What it is:** If you mark a field with the `[LoadFromResources]` attribute, `ResourceDrawer` will try to find an asset in the `Resources` folder and assign it to that field, saving you from having to drag it in manually.

**How to use:** see the sections below.

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
- `ProcessResourceAttributes(MonoBehaviour targetObject)`: A static method that scans all fields of `targetObject` for resource-loading attributes. Returns `void`.
