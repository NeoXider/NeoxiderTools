# NeoxiderSettings

**What it is:** the static access point to the package-wide editor settings (attribute search, project folder layout, scene hierarchy template), stored in `ProjectSettings/NeoxiderSettings.json` so the team shares them through version control.

**How to use:** edit them in **Neoxider > Settings**, or from editor code read `NeoxiderSettings.Current` and call `NeoxiderSettings.SaveSettings()` after changing it.

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
- `SceneHierarchy`: Static property, returns a `CreateSceneHierarchy` instance with the hierarchy settings. Its template is stored in the same JSON file (`sceneHierarchyJson`) by `SaveSettings()` and restored when the settings load, also when this property is the first settings access after an editor restart (for example the **Create Scene Hierarchy** menu item); files written by older versions simply keep the template defaults.
- `LoadSettings()`: Static method for loading settings from the file. Returns `void`.
- `SaveSettings()`: Static method for saving the current settings to the file. Returns `void`. It only writes the JSON file: `ProjectSettings/` is not an asset folder, so no `AssetDatabase.Refresh` is triggered (earlier versions refreshed the whole project on every save).
- `FindMissingFolders()`: Returns the configured folders that do not exist on disk - the root folder first, then each entry of `folders`. Empty when the structure is complete.
- `EnableAttributeSearch`: Whether the Inspector runs the `[GetComponent]` / `[FindInScene]` / `[LoadFromResources]` lookups.
- `ResetToDefaults()`: Static method for resetting all settings to defaults. Returns `void`.
- `OpenSettings()`: Static method that opens the settings window. Invoked via `MenuItem`.

**Example**

```csharp
NeoxiderSettings.Current.enableAttributeSearch = false;
NeoxiderSettings.SaveSettings();

foreach (string folder in NeoxiderSettings.FindMissingFolders())
{
    UnityEngine.Debug.Log("Missing: " + folder);
}
```
