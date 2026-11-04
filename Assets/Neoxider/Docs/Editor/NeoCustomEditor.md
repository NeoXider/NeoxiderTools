# NeoCustomEditor Editor

**What it is:** the fallback inspector for every `MonoBehaviour`: it draws the Neoxider look for `Neo.*` components, turns `[Button]` methods into actions and runs the `[GetComponent]` / `[FindInScene]` / `[LoadFromResources]` lookups for any script.

**How to use:** nothing to register - Unity picks it for any component without its own editor. An explicit `[CustomEditor]` for a type always wins over it; derive from `CustomEditorBase` (or `NeoCustomEditor`) to keep the shared look.

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
- **Global effect**: The editor is registered for `MonoBehaviour` with `editorForChildClasses = true` and `isFallback = true`, so it covers every script that has no dedicated editor. Under `MIRROR`, exact-type subclasses (`CounterNeoEditor`, `SpawnerNeoEditor`, ...) keep the Neoxider look for components that derive from `NetworkBehaviour`.
- **Cheap for foreign scripts**: the attribute lookups resolve their attributed fields once per type; a script without such attributes costs a dictionary lookup per Inspector event and allocates nothing.
- **Central hub**: Serves as the entry point that invokes the logic from `ComponentDrawer` and `ResourceDrawer` to process the corresponding attributes - for every selected object when several are selected. Lookups that found nothing are throttled on this path (retried after a hierarchy or project change, at most every 0.5 s otherwise), so an unresolved `[FindInScene]` or `[LoadFromResources]` no longer rescans the scene or the Resources on every IMGUI event.
- **Extensibility**: Built on the `CustomEditorBase` base class, making it easy to add new functionality.

**Public methods**
- This class has no public methods intended to be called from other scripts. It extends the functionality of the Unity inspector.

## Assembly reference repair

**Neoxider > Tools > Fix Editor Assembly References** repairs missing runtime references in `Neo.Editor.asmdef`. Existing references are recognized by assembly name or GUID. The tool excludes editor/test assemblies and candidates with an explicit `includePlatforms` list, so it does not pull the test assembly back into the assembly it tests. Explicit optional editor dependencies remain a maintainer decision. The operation preserves other asmdef fields and is idempotent; it does not replace proper references in your own game assembly.
