# NeoxiderSettings

**What it is:** Settings are stored in the `ProjectSettings/NeoxiderSettings.json` file, which lets you keep them in version control and share them with your team.

**How to use:** see the sections below.

---


## 1. Introduction

`NeoxiderSettings` is a static class that serves as the central access point to all asset settings. It is responsible for loading, saving, and providing the data configured in the `Neoxider Settings` window.

Settings are stored in the `ProjectSettings/NeoxiderSettings.json` file, which lets you keep them in version control and share them with your team.

---

## 2. Class Description

### NeoxiderSettings
- **Namespace**: `Neo`
- **File path**: `Assets/Neoxider/Editor/Main/NeoxiderSettings.cs`

**Description**
Static class for managing the Neoxider asset settings.

**Key features**
- **JSON storage**: All settings are saved in a text format that is easy to track.
- **Static access**: Settings are accessible from anywhere in editor code via `NeoxiderSettings.Current`.
- **Settings management**: Provides methods for loading, saving, and resetting settings to their defaults.

**Public properties and methods**
- `Current`: Static property, returns the current `NeoxiderData` settings instance.
- `SceneHierarchy`: Static property, returns a `CreateSceneHierarchy` instance with the hierarchy settings.
- `LoadSettings()`: Static method for loading settings from the file. Returns `void`.
- `SaveSettings()`: Static method for saving the current settings to the file. Returns `void`.
- `ResetToDefaults()`: Static method for resetting all settings to defaults. Returns `void`.
- `OpenSettings()`: Static method that opens the settings window. Invoked via `MenuItem`.
