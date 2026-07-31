# NeoCustomEditor Editor

**What it is:** It combines several features:

**How to use:** see the sections below.

---


## 1. Introduction

`NeoCustomEditor` is the main editor script that replaces the standard inspector for all components inherited from `MonoBehaviour`. Its goal is to significantly extend the inspector's capabilities and automate routine tasks.

It combines several features:
1.  Rendering methods as buttons (inherited from `CustomEditorBase`).
2.  Automatic assignment of component references via attributes (using `ComponentDrawer`).
3.  Automatic loading of assets from `Resources` folders via attributes (using `ResourceDrawer`).

---

## 2. Class Description

### NeoCustomEditor
- **Namespace**: `Neo.Editor`
- **File path**: `Assets/Neoxider/Editor/PropertyAttribute/NeoCustomEditor.cs`

**Description**
A custom editor for all `MonoBehaviour`s that activates all the attribute magic, such as `[Button]`, `[GetComponent]`, and `[LoadFromResources]`.

**Key features**
- **Global effect**: Since the editor is created for `MonoBehaviour` with the `editorForChildClasses = true` flag, it works for all your scripts automatically.
- **Central hub**: Serves as the entry point that invokes the logic from `ComponentDrawer` and `ResourceDrawer` to process the corresponding attributes.
- **Extensibility**: Built on the `CustomEditorBase` base class, making it easy to add new functionality.

**Public methods**
- This class has no public methods intended to be called from other scripts. It extends the functionality of the Unity inspector.
