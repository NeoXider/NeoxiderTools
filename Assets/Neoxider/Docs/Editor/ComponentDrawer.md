# ComponentDrawer Class

**What it is:** For example, if you mark a field with the `[GetComponent]` attribute, it is `ComponentDrawer` that performs `GetComponent()` and assigns the found component to that field right in the editor, saving you from having to do it manually.

**How to use:** see the sections below.

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
- `ProcessComponentAttributes(MonoBehaviour targetObject)`: A static method that scans all fields of `targetObject` for component-lookup attributes. Returns `void`.
