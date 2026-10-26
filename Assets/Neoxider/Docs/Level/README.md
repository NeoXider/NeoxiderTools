# Level module

Level flow, scene loading, level buttons, and progress. Scripts in `Scripts/Level/`. Use this page as the English module entry.

Demo in the release layout: `Assets/Neoxider/Samples~/Demo/Scenes/Level/LevelFlowDemo.unity`, controller `Demo/Scripts/Shell/LevelDemoController.cs`. Active sample development uses `Assets/Neoxider/Samples/` instead. Import **Demo Scenes** through Package Manager to copy the sample into `Assets/Samples/NeoxiderTools/<version>/Demo Scenes/`; `Samples~` is not compiled in place.

## Runtime API

`LevelManager` manages zero-based level selection and a separate progress frontier per `Map`. `Map.level` / `MaxLevel` identify the next uncompleted level; they are also the number of recorded completions. In a looping map, selection wraps while progress keeps increasing.

```csharp
using Neo.Level;

LevelManager levels = LevelManager.I;
levels.SetLastLevel();              // Select the saved frontier (wrapped for looping maps).
levels.SetLevel(2);                 // Select directly from ordinary C#.
// After successful gameplay:
levels.SaveLevel();                 // Record completion only at the current frontier.
levels.SetLastLevel();              // Select the newly unlocked level.
```

Reference `Neo.Level` from a custom assembly. Level selection does not load a Unity scene; use `OnChangeLevel` to drive your level view or call `SceneFlowController` explicitly. The manager can run without level buttons. Empty map slots are skipped during initialization and map discovery, and cannot be selected.

## Scene and Inspector workflow

Configure maps and the save prefix on `LevelManager`; optional `LevelButton` children provide the map UI and `TextLevel` displays current or maximum progress. Buttons use visual IDs **0 = locked, 1 = current frontier, 2 = passed**. Inspector UnityEvents call the same public methods shown above. `SceneFlowController` independently handles loading, progress, manual activation, pause, and quit.

In 11.0, `LevelButton` labels widen their one-based addition, and `TextLevel` integer animations finish with exact digits through the existing `SetText` API. Intermediate text/`currentNum` remain approximate floats. `TextLevel` combines the base tween enable/disable hooks with its own source subscription and deferred wait; see [TextLevel](TextLevel.md) for lifecycle and offset details.

## Component documentation

| Page | Description |
|------|-------------|
| [LevelManager](./LevelManager.md) | Level selection and map progress |
| [LevelButton](./LevelButton.md) | UI button for level selection |
| [TextLevel](./TextLevel.md) | UI text bound to current or max level |
| [Map](./Map.md) | Serialized map progress entry |
| [SceneFlowController](./SceneFlowController.md) | Scene transition control |

## See also

- [Save](../Save/README.md)
- [UI](../UI/README.md)
