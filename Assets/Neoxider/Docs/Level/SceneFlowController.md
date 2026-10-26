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
| `_activateOnReady` | For `Async` mode. If `true`, Unity activates the scene automatically. If `false`, call `ProceedScene()` as in manual mode. |
| `_loadOnStart` | Automatically start loading on `Start`. |
| `_progressPanel` | A `GameObject` (loading screen) that is auto-enabled at the start and disabled at the end of the load. |
| `_sliderProgress`, `_imageProgress` | UI elements to display the loading bar. |
| `_textProgress`, `_textMeshProgress` | Text components for percentage display (`_progressTextFormat` = Percent). |
| `_progressTextFormat` | `Percent` displays the prefix and raw Unity percentage. `Plain` displays the prefix alone. |
| `_readyToProceedText` | Text shown while a manual load waits for activation; retained across waiting frames. |

## Usage

```csharp
// If configured in the Inspector, just call without arguments:
sceneFlowController.LoadScene();

// Or pass the name/index directly:
sceneFlowController.LoadScene("Level_1");

// Quick restart of the active scene:
sceneFlowController.Restart();

// Manual activation, also callable from a UI Button's Inspector event:
sceneFlowController.OnReadyToProceed.AddListener(ShowContinueButton);
sceneFlowController.ProceedScene();

bool hasPendingLoad = sceneFlowController.IsLoading;
```

`LoadScene()` uses the name only when `_useSceneName` is enabled and the name is nonempty; otherwise it uses `_sceneBuildIndex`. `LoadScene(int)` and `LoadScene(string)` use the explicit target. `LoadMode`, `SceneBuildIndex`, and `SceneName` are writable C# properties. `Pause(bool)` sets time scale to zero or one; `Quit()` requests application exit.

Only one asynchronous load can be owned by a controller at a time. Further requests, including sync/additive requests, are ignored while `IsLoading` is true. A null loader result or an exception releases the request state so another call can retry. Async calls on an inactive GameObject are ignored because Unity cannot start its coroutine. `SceneLoader` is an injectable `ISceneFlowSceneLoader`; assigning null restores Unity's loader. Restore it after using a custom test/application loader because it is shared by all controllers.

Async events are `OnLoadStarted`, `OnProgress(float)`, `OnReadyToProceed` (once for a manual request), and `OnLoadCompleted`. Progress follows raw Unity values from 0 to 1; manual loading normally waits at 0.9 until `ProceedScene()`. Completion publishes 1 before clearing the panel and firing `OnLoadCompleted`. Sync and additive loads call the loader directly and do not emit these async events.

Keep the controller on an active object outside a loading panel that will be hidden. Deactivating its GameObject or destroying the component releases a pending manual activation, clears `IsLoading`, and hides the panel. Unity scene loading cannot be canceled through this component: the native operation may still finish and activate its scene, without a completion notification from the stopped controller. Disabling only the component leaves its running coroutine active, following Unity coroutine behavior.

## See Also
- [LevelManager](LevelManager.md) - Logical level progression.
- [Level module](README.md)
