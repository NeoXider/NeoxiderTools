# SceneFlowController

**Purpose:** A convenient wrapper around Unity's standard `SceneManager`. It supports synchronous, asynchronous, and additive scene loading. It allows you to easily set up a UI loading screen (Progress Bar, Text) directly in the Inspector without writing loading coroutines.

## Setup

1. Add `Add Component > Neoxider > Level > SceneFlowController` to an object (e.g., a Play button or Scene Manager).
2. Select the `_loadMode` (usually `Async` for large scenes).
3. Specify the `_sceneBuildIndex` or `_sceneName`.
4. Configure the UI references (Slider, Text, Panel) to show progress.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `_loadMode` | `Sync` (freezes the game), `Async` (background loading), `AsyncManual` (waits for activation command), `Additive` (loads on top). |
| `_sceneBuildIndex` | Scene index in Build Settings (if name is not used). |
| `_sceneName` | Scene name (used if `_useSceneName` = true). |
| `_activateOnReady` | For `Async` mode. If `true`, the scene switches instantly when 100% loaded. |
| `_loadOnStart` | Automatically start loading when this object awakens. |
| `_progressPanel` | A `GameObject` (loading screen) that is auto-enabled at the start and disabled at the end of the load. |
| `_sliderProgress`, `_imageProgress` | UI elements to display the loading bar. |
| `_textProgress`, `_textMeshProgress` | Text components for percentage display (`_progressTextFormat` = Percent). |

## Usage

```csharp
// If configured in the Inspector, just call without arguments:
sceneFlowController.LoadScene();

// Or pass the name/index directly:
sceneFlowController.LoadScene("Level_1");

// Quick restart of the active scene:
sceneFlowController.Restart();
```

## See Also
- [LevelManager](LevelManager.md) - Logical level progression.
- [Module Root](../README.md)
