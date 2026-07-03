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
- **Menu access**: `Tools/Neoxider/Find & Remove Missing Scripts`

**Description**
Creates a Unity editor window for finding and removing `Missing Script` references from all objects across all scenes and prefabs in the project. Uses an architecture that separates logic from GUI rendering.

**Key features**
- **Two search modes**: 
  - Quick search in the current scene only
  - Full scan of all scenes and prefabs in the project
- **Interactive list**: Displays found problems as a list. Clicking a list item pings the problematic `GameObject`.
- **Targeted removal**: Lets you remove the missing script from a single specific object.
- **Bulk removal**: Lets you remove all found missing scripts with a single button.
- **Safety**: Uses the `Undo` system for all removal operations.
- **Professional architecture**: GUI rendering is extracted into a separate class, `FindAndRemoveMissingScriptsWindowGUI`.

**Public methods**
- `ShowWindow()`: Static method that opens the tool window. Invoked via `MenuItem`.
