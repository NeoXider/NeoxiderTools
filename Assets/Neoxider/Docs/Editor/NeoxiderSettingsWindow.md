# Neoxider Settings Window

**What it is:** This is the central hub for customizing the asset to fit your project's needs.

**How to use:** see the sections below.

---


## 1. Introduction

`NeoxiderSettingsWindow` is an editor window that provides a user interface for all of the asset's global settings. Through this window you can configure the project folder structure, the standard scene hierarchy, and other common parameters.

This is the central hub for customizing the asset to fit your project's needs.

---

## 2. Tool Description

### NeoxiderSettingsWindow
- **Namespace**: `Neo`
- **File path**: `Assets/Neoxider/Editor/Main/NeoxiderSettingsWindow.cs`
- **GUI class**: `NeoxiderSettingsWindowGUI` (`Assets/Neoxider/Editor/GUI/NeoxiderSettingsWindowGUI.cs`)
- **Menu access**: `Neoxider/Settings`

**Description**
Creates a window in the Unity editor for configuring all Neoxider asset settings. Uses an architecture that separates logic from GUI rendering.

**Key features**
- **Folder structure setup**: Lets you define a standard folder structure for the project and automatically create any missing folders.
- **Scene hierarchy setup**: Lets you edit the list of objects created by the `CreateSceneHierarchy` utility.
- **Centralized management**: All settings are gathered in one place for convenience.
- **Save and reset**: Settings can be saved or reset to their default values.
- **Professional architecture**: GUI rendering is extracted into a separate class, `NeoxiderSettingsWindowGUI`.

**Public methods**
- `ShowWindow()`: A static method that opens the settings window. Invoked via `MenuItem`.
