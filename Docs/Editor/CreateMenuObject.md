# CreateMenuObject Utility

**What it is:** Instead of manually creating an empty object and adding the needed script to it, or hunting for a prefab in folders, you can simply pick the appropriate menu item.

**How to use:** choose **GameObject > Neoxider > Create Neoxider Object...**, search for a component and click it to create a ready object in the scene. Add `[CreateFromMenu]` to your own component to list it there.

---


## 1. Introduction

`CreateMenuObject` is an editor script that significantly speeds up working with the asset by adding numerous items to the `GameObject -> Neoxider` menu. Each menu item lets you quickly create a ready-to-use `GameObject` in the scene with one of this package's components.

Instead of manually creating an empty object and adding the needed script to it, or hunting for a prefab in folders, you can simply pick the appropriate menu item.

---

## 2. Tool Description

### CreateMenuObject
- **Namespace**: `Neo`
- **File path**: `Assets/Neoxider/Editor/Create/CreateMenuObject.cs`
- **Menu access**:
  - `GameObject/Neoxider/Create Neoxider Object...` — opens the component browser window
  - `GameObject/Neoxider/Presets/...` — ready-made prefabs (System Root, First Person Controller, Simple Weapon, Bullet, Interactive Sphere, Toggle Interactive, Trigger Cube)
  - `Neoxider/Windows/Create Neoxider Object` — the same window, dockable

**Description**
Adds a `Neoxider` submenu to the `GameObject` menu with a large list of objects and prefabs from this asset that are ready to be created.

**Key features**
- **Quick access**: Provides fast access to creating all key components, such as `AM` (Audio Manager), `Money`, `UI`, `VisualToggle`, and many others.
- **Prefab usage**: For some objects (for example, `VisualToggle`, `ButtonPrice`), the utility does not just create an empty object but instantiates a pre-configured prefab.
- **Contextual creation**: The new `GameObject` is created as a child of the right-clicked or selected object (with aligned local transform and a unique sibling name); without a parent it is placed at the Scene view pivot, and in prefab mode it goes under the prefab root.
- **UI safety**: Objects with a `RectTransform` are automatically placed under a `Canvas`; if the scene has none, a Canvas with an EventSystem is created first.
- **Automatic path resolution**: The system automatically determines the path to the prefabs, working both when installed via the Git Package Manager (`Packages/com.neoxider.tools/...`) and with a regular installation (`Assets/Neoxider/...`). No extra configuration required. Since 12.0 the root is taken only from `Editor/Create/CreateMenuObject.cs` itself (scripts with similar names, such as the package tests, can no longer anchor it) and `CreateMenuObject.startPath` always uses forward slashes.
- **Window behaviour**: the **Create Neoxider Object** window keeps its list after a script reload (no Refresh needed), sorts and counts the tree once per search instead of on every repaint, and only repaints on hover when the row under the cursor changes. Colours follow the editor skin, also after switching it while the window is open.

**Public methods**
- This class has no public methods intended to be called from other scripts. All logic is internal and invoked via `MenuItem`.
