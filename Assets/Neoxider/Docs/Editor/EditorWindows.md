# Editor Windows

**What it is:** The Editor Windows module provides an architecture for creating Unity editor windows with a separation between logic and GUI rendering.

**How to use:** see the sections below.

---


The Editor Windows module provides an architecture for creating Unity editor windows with a separation between logic and GUI rendering.

## Architecture

All editor windows use a separation-of-concerns pattern:
- **EditorWindow** classes contain only the window management logic
- **EditorWindowGUI** classes contain all GUI rendering

This provides:
- A clean architecture with separation of concerns
- Easy testing and maintenance
- Reusable GUI components

## Base Class

### EditorWindowGUI

Abstract base class for all window GUI renderers.

```csharp
public abstract class EditorWindowGUI
{
    public abstract void OnGUI(EditorWindow window);
}
```

## Available Windows

### NeoxiderSettingsWindow

**Menu path:** `Neoxider → Settings`

A window for managing global Neoxider settings:
- General settings (attribute search, folder validation)
- Project folder structure
- Scene hierarchy settings

**GUI class:** `NeoxiderSettingsWindowGUI`

### SceneSaver

**Menu path:** `Neoxider → Tools → Scene Saver`

A utility for automatically saving scene backups:
- Configurable save interval
- Automatic background saving
- Saving even if the scene has not changed

**GUI class:** `SceneSaverGUI`

### FindAndRemoveMissingScriptsWindow

**Menu path:** `Neoxider → Tools → Find & Remove Missing Scripts`

A window for finding and removing Missing Scripts:
- Search across all scenes and prefabs
- Visual list of found objects
- Bulk or individual removal

**GUI class:** `FindAndRemoveMissingScriptsWindowGUI`

### TextureMaxSizeChanger

**Menu path:** `Neoxider → Tools → Texture Max Size`

A tool for bulk-changing the maximum texture size:
- Filtering by texture type
- Processing progress bar
- Confirmation before applying

**GUI class:** `TextureMaxSizeChangerGUI`

## Creating a New Window

### Step 1: Create a GUI class

```csharp
using UnityEditor;
using Neo.Editor.GUI;

namespace Neo.Editor.GUI
{
    public class MyWindowGUI : EditorWindowGUI
    {
        public override void OnGUI(EditorWindow window)
        {
            EditorGUILayout.LabelField("My Window");
            // Your GUI rendering
        }
    }
}
```

### Step 2: Create an EditorWindow class

```csharp
using UnityEditor;
using Neo.Editor.GUI;

namespace Neo
{
    public class MyWindow : EditorWindow
    {
        private MyWindowGUI _gui;

        [MenuItem("Neoxider/Windows/My Window")]
        public static void ShowWindow()
        {
            GetWindow<MyWindow>("My Window");
        }

        private void OnEnable()
        {
            _gui = new MyWindowGUI();
        }

        private void OnGUI()
        {
            _gui?.OnGUI(this);
        }
    }
}
```

## Architecture Benefits

1. **Separation of concerns**: Window logic is separated from rendering
2. **Testability**: GUI classes can be tested independently
3. **Reusability**: GUI components can be used across different windows
4. **Clean code**: EditorWindow classes contain minimal code
5. **Professional structure**: Follows Unity development best practices
