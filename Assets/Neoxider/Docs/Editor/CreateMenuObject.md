# CreateMenuObject Utility

**What it is:** Instead of manually creating an empty object and adding the needed script to it, or hunting for a prefab in folders, you can simply pick the appropriate menu item.

**How to use:** see the sections below.

---


## 1. Introduction

`CreateMenuObject` is an editor script that significantly speeds up working with the asset by adding numerous items to the `GameObject -> Neoxider` menu. Each menu item lets you quickly create a ready-to-use `GameObject` in the scene with one of this package's components.

Instead of manually creating an empty object and adding the needed script to it, or hunting for a prefab in folders, you can simply pick the appropriate menu item.

---

## 2. Tool Description

### CreateMenuObject
- **Namespace**: `Neo`
- **File path**: `Assets/Neoxider/Editor/Create/CreateMenuObject.cs`
- **Menu access**: `GameObject/Neoxider/...`

**Description**
Adds a `Neoxider` submenu to the `GameObject` menu with a large list of objects and prefabs from this asset that are ready to be created.

**Key features**
- **Quick access**: Provides fast access to creating all key components, such as `AM` (Audio Manager), `Money`, `UI`, `VisualToggle`, and many others.
- **Prefab usage**: For some objects (for example, `VisualToggle`, `ButtonPrice`), the utility does not just create an empty object but instantiates a pre-configured prefab.
- **Contextual creation**: The new `GameObject` is created as a child of the currently selected object in the hierarchy.
- **Automatic path resolution**: The system automatically determines the path to the prefabs, working both when installed via the Git Package Manager (`Packages/com.neoxider.tools/...`) and with a regular installation (`Assets/Neoxider/...`). No extra configuration required.

**Public methods**
- This class has no public methods intended to be called from other scripts. All logic is internal and invoked via `MenuItem`.
