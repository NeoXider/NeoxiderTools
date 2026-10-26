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

### Lobby demo authoring (MIRROR)

**Menu:** `Neoxider → Network → Create Lobby Demo Scene`, available when Mirror is enabled.

The command first offers to save modified scenes. Cancel leaves all loaded work intact. Cancelling the destination dialog also creates no scene or prefab. Choose an **unused `.unity` path inside Assets**; an existing scene or other destination asset is refused. After confirmation it creates the lobby scene and a uniquely named `<SceneName>_Assets` directory beside it. Existing assets are not overwritten.

Lobby and gameplay scenes start as copied persistent assets from the package's Editor template, then receive their camera, light and UI. The command does not need to save an unrelated untitled scene to create them. The menu keeps its normal Single-scene replacement after the save/continue decision; the internal additive authoring path retains the other open scenes. Failed population removes only newly created output and restores the prior active scene/selection while those objects still exist; Single replacement already authorized by the user does not recreate discarded unsaved work.

The saved manager references a real `NeoLobbyPlayer` prefab through `roomPlayerPrefab` and a separate `NetworkIdentity` game-player prefab through `playerPrefab`. Host, Join, Ready and Leave use persistent callbacks to the scene's `NeoLobbyPresenter`. The address field and discovery output feed `SetAddress`; presenter status/player-count text outputs use the existing `SetText` adapter. These links survive saving and reopening the scene.

The generated assets directory also contains `LobbyGame.unity`, a minimal gameplay scene with its own persistent Leave control. `RoomScene`/`offlineScene`/`onlineScene` point to the lobby and `GameplayScene` to this scene. Add both scenes to the project's **Build Profiles** and check the Transport before testing the ready-to-game transition. KCP is assigned when available; otherwise the tool requests manual Transport setup. The generated game-player prefab is a minimal network identity to replace or extend for the game. Presenters live on scene canvases and resolve the surviving manager after transitions. Actual multi-peer behavior requires separate Play Mode/product verification.

Editor C# can call the preserved `Neo.Editor.LobbyDemoSceneCreator.CreateLobbyDemoScene()` entry behind `UNITY_EDITOR && MIRROR`; it opens the same save dialogs. Runtime C# can assign `NeoLobbyPresenter.Manager`, call `SetAddress(string)`, `HostLobby()`, `JoinLobby()`, `ToggleReady()` and `LeaveLobby()` directly. Inspector button wiring is optional convenience over these typed APIs.

This command writes to the chosen project path, not the package sample folders. Developer `Assets/Neoxider/Samples`, release `Assets/Neoxider/Samples~`, and imported `Assets/Samples/NeoxiderTools/<version>/<sample name>` layouts remain unchanged.

В 11.0 отмена защищает открытые сцены; выберите новый путь `.unity` внутри Assets — существующие assets не перезаписываются. Обе сцены создаются из постоянного Editor-шаблона без сохранения посторонних untitled-сцен. Prefab-ссылки и события presenter сохраняются в assets. Перед запуском добавьте обе сцены в Build Profiles и проверьте Transport.

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
using Neo.Editor.Windows;

namespace Neo.Editor.Windows
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
using Neo.Editor.Windows;

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
