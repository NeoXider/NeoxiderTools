# Find & Remove Missing Scripts Utility

**What it is:** A Unity editor tool that helps find and fix one of the most common problems in large projects — missing scripts (`Missing Script`). This happens when a script...

**How to use:** see the sections below.

---


## 1. Introduction

This is a Unity editor tool that helps find and fix one of the most common problems in large projects — missing scripts (`Missing Script`). This happens when a script attached to a `GameObject` is deleted or renamed, leaving behind an empty, non-functional reference.

This window scans all scenes and prefabs in the project, builds a list of objects with such problems, and lets you automatically remove these missing references, cleaning the project of errors.

---

## 2. Tool Description

### FindAndRemoveMissingScriptsWindow
- **Namespace**: `(global)`
- **File path**: `Assets/Neoxider/Editor/FindAndRemoveMissingScriptsWindow.cs`
- **GUI class**: `FindAndRemoveMissingScriptsWindowGUI` (`Assets/Neoxider/Editor/GUI/FindAndRemoveMissingScriptsWindowGUI.cs`)
- **Menu access**: `Neoxider/Tools/Find & Remove Missing Scripts`

**Description**
Creates a Unity editor window for finding and removing `Missing Script` references from all objects across all scenes and prefabs in the project. Uses an architecture that separates logic from GUI rendering.

**Key features**
- **Two search modes**: 
  - Quick search in the current scene only
  - Full scan of all scenes and prefabs in the project
- **Interactive list**: Displays found problems as a list. Clicking a list item pings the problematic `GameObject`.
- **Targeted removal**: Lets you remove the missing script from a single specific object.
- **Bulk removal**: Lets you remove all found missing scripts with a single button.
- **Scene preservation**: Closed scenes are scanned in temporary preview scenes. Loaded scenes are inspected in place, including their unsaved state; scanning never replaces the open scene setup.
- **Persistence**: Repairs to closed scene assets and prefabs are saved. Already open scenes stay open and dirty, with Undo available; use normal Save Scene to persist those repairs.
- **Professional architecture**: GUI rendering is extracted into a separate class, `FindAndRemoveMissingScriptsWindowGUI`.

**Public methods**
- `ShowWindow()`: Static method that opens the tool window. Invoked via `MenuItem`.

## Repair workflow

1. Choose current-scene search or a full scan. A new search resets previous progress. Closing the window stops its update callback and clears the progress bar.
2. Click a result to select it. A closed scene opens additively, preserving other loaded scenes. Duplicate object names and names containing `/` are addressed by their hierarchy indices, rather than ambiguous name lookup.
3. Remove one result or all results. Closed scenes are opened additively for repair, saved, and closed again. Prefab contents are loaded, saved, and released. Failed repairs remain in the results; a scene whose save fails remains open with the repair available.
4. Save any scenes that were already open when repaired. This avoids implicitly saving unrelated edits. Asset repairs are immediate file changes; the window does not offer Undo for a saved closed scene or prefab.

Rescan after restructuring a hierarchy: results describe the hierarchy at scan time. The public C# entry point remains `FindAndRemoveMissingScriptsWindow.ShowWindow()`; scanning and repair are implementation details of the GUI controller.
