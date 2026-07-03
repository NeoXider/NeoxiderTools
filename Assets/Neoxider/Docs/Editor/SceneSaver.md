# Scene Saver Utility

**What it is:** The utility creates a clone of the scene in a separate `Assets/Scenes/AutoSaves` folder without touching the main scene file. This provides safe and unobtrusive backups.

**How to use:** see the sections below.

---


## 1. Introduction

`SceneSaver` is a background editor utility that automatically saves a backup copy of the currently open scene at specified intervals. The main purpose of this tool is to prevent loss of work in case of a Unity crash or an accidental close without saving.

The utility creates a clone of the scene in a separate `Assets/Scenes/AutoSaves` folder without touching the main scene file. This provides safe and unobtrusive backups.

---

## 2. Tool Description

### SceneSaver
- **Namespace**: `(global)`
- **File path**: `Assets/Neoxider/Editor/Scene/SceneSaver.cs`
- **GUI class**: `SceneSaverGUI` (`Assets/Neoxider/Editor/GUI/SceneSaverGUI.cs`)
- **Menu access**: `Tools/Neoxider/Scene Saver`

**Description**
An editor script that automatically saves backup copies of the active scene. Uses an architecture that separates logic from GUI rendering.

**Key features**
- **Background operation**: Uses `EditorApplication.update` to check the time and trigger saving without interrupting your work.
- **Configurable interval**: In the settings window, you can specify how often (in minutes) a backup should be made.
- **Safe saving**: Creates a copy of the scene with the `_AutoSave` suffix in a separate folder, never overwriting your main file.
- **Smart saving**: By default, saves the scene only if it has unsaved changes (`isDirty`).
- **Professional architecture**: GUI rendering is extracted into a separate class, `SceneSaverGUI`.

**Public methods**
- `ShowWindow()`: Static method that opens the utility settings window. Invoked via `MenuItem`.
