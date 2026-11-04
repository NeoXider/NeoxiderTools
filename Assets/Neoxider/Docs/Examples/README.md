# Examples

**What it is:** step-by-step example guides plus a map of the C# entry points behind the **Demo Scenes** sample. The sample scripts live in `Samples~/Demo/Scripts` (assembly `Neo.Rpg.Demo`) and the scene builders in `Samples~/Demo/Editor` (assembly `Neo.Demo.Editor`).

<!-- demo-shots:begin -->
<p align="center"><img src="https://raw.githubusercontent.com/NeoXider/NeoxiderTools/main/Images/demos/ClickerExample.jpg" alt="ClickerExample: a clicker built only from library components" width="49%" /> <img src="https://raw.githubusercontent.com/NeoXider/NeoxiderTools/main/Images/demos/Shooter2D.jpg" alt="Shooter2D: a 2D shooter assembled in the Inspector" width="49%" /> <img src="https://raw.githubusercontent.com/NeoXider/NeoxiderTools/main/Images/demos/adventure.jpg" alt="adventure: a first-person NoCode sandbox" width="49%" /> <img src="https://raw.githubusercontent.com/NeoXider/NeoxiderTools/main/Images/demos/CardGame.jpg" alt="CardGame: the Drunkard card game" width="49%" /></p>

*Demo scenes: `Scenes/_ExampleGame/ClickerExample.unity`, `Scenes/_ExampleGame/Shooter2D.unity`, `Scenes/_ExampleGame/adventure.unity`, `Scenes/_ExampleGame/CardGame.unity`, in the **Demo Scenes** sample (Package Manager > NeoxiderTools > Samples). The scenes are saved with their UI and wiring, so you can inspect them without entering Play Mode.*
<!-- demo-shots:end -->

**Contents:**

| Page / section | What it covers |
|---|---|
| [Anomaly Game](./AnomalyGame.md) | Build an anomaly-survival game from `TimerObject`, `Selector`, `NeoCondition` and `InteractiveObject` without code. |
| [Demo sample scripts](#demo-sample-scripts) | Which runtime APIs each sample controller uses. |
| [C# configuration](#c-configuration) | Driving sample controllers from code. |
| [Owned resources](#owned-resources) | What the sample code creates and releases. |
| [Verification](#verification) | Tests that cover the sample scripts and scenes. |

**Navigation:** [← Docs](../README.md) · [Sample scenes](../Samples.md)

---

## Demo sample scripts

Import **Demo Scenes** from **Package Manager → NeoxiderTools → Samples**. The package stores it in
`Assets/Neoxider/Samples~/Demo`; Unity imports a copy under
`Assets/Samples/NeoxiderTools/<version>/Demo Scenes`. During package development the working root can be
`Assets/Neoxider/Samples/Demo`. Importing the sample makes its `Neo.Rpg.Demo` runtime assembly and the
`Neo.Demo.Editor` builder assembly available. `Neo.Rpg.Demo` contains examples for several modules and
references Mirror as well as the RPG compatibility modules.

Every demo scene is authored by an editor builder and saved with its UI and wiring, so the controllers
below only hold serialized references and public methods that persistent UnityEvent listeners call; they
build no UI at runtime. Game rules stay in the runtime modules. For new combat code, start with
[Abilities](../Abilities/README.md); the RPG examples maintain existing scenes. How the scenes are built and
rebuilt is described in [Sample scenes](../Samples.md#how-demo-scenes-are-made).

| Example | Runtime entry points | Scene workflow |
| --- | --- | --- |
| Condition | `Health.TakeDamage`, `Health.Heal`, `ScoreManager.Add`, `NeoCondition.Evaluate` | `NeoCondition.OnResult` switches the lamps, banners and LOW HP badge through `GameObject.SetActive`. `ConditionDemoUI` provides the damage/heal/score actions, `EvaluateInCode()` (the C# path) and `SetPaused(bool)`. |
| Match3 | `FieldGenerator`, `Match3BoardService`, `GridSystemMatch3DemoUI` | Assign services or call the board view's `Configure`. A live view reconnects when configured with another field and rebuilds when the field is regenerated. |
| TicTacToe | `TicTacToeBoardService.TryMakeMove`, `ResetBoard`, `WinLineLength`, `WinningLine` | Assign the field, board and camera to the view; use the UI to change blockers, test paths and switch to the 5×5 four-in-a-row mode. `Rebuild` replaces the generated cells. |
| Dice Merge | `DiceBoardService`, `DiceMergeDemoController.RestartDemo`, `TryPlaceCurrentPiece`, `ShowHint` | Drag a single die or pair from the tray, tap to rotate, ask for a placement hint and restart after game over. |
| RPG | `RpgCharacter`, `RpgCombatDemoController`, `RpgWorldHealthBars` | Assign combatants and camera explicitly. Health bars use the assigned camera, hide when disabled, and release their generated views when destroyed. |
| Survivor | `SurvivorConfig`, `SurvivorGame` (`RestartRun`, `PickUpgrade`, `GrantLevelUp`), `SurvivorUpgrade` | The scene is authored by `SurvivorDemoBuilder` from a config containing unit templates, an ability library and upgrades. The sample reuses Abilities and PoolManager. |
| Module controllers | `<Module>DemoController` (Audio, Save, Settings, Level, Quest, StateMachine, Parallax, Network, Shop, NPC, Inventory, Light, Selector, ...) | Serialized references to the authored objects plus public listener targets; they log calls and refresh labels from module events. |
| Animation Fly | `AnimationFly.Play`, `AnimationFlyDemoController` | Try world, canvas and screen origins, pooling, motion presets, reward callbacks and the tuning sliders; the service, targets and sliders are saved in the scene. |

## C# configuration

Use the same typed package APIs as the sample buttons; Inspector/NoCode wiring is optional. In an
imported sample, the Condition controller also exposes an amount configuration method:

```csharp
using Neo.Demo.Condition;

// References to Health, ScoreManager and panels are assigned on this scene component.
conditionUi.ConfigureAmounts(damagePerHit: 7, healPerClick: 3, pointsPerClick: 11);
conditionUi.DealDamage();
conditionUi.HealPlayer();
conditionUi.AddScore();
conditionUi.EvaluateInCode();
```

`ConfigureAmounts` clamps negative amounts to zero. Existing scenes retain the defaults of 25 damage,
10 healing and 25 points.

`DiceMergeDemoView.Configure` accepts optional art references. A null sprite, cell prefab, die prefab or
sprite array keeps the corresponding authored reference. With no die prefab, the view creates simple
visible dice using `DiceDieView` and a numeric label. Its existing three-argument `Initialize` API is
retained; the overload with `showValueLabel` can explicitly request a number when no pip sprite exists.
Call `Configure` with both services before `RestartDemo`; an unwired controller reports
`GameOver == true` and `CurrentPiece == null`. Disabling and re-enabling a view refreshes the current board
and tray. Match3 and Dice views disconnect their previous event sources when reconfigured.

`SurvivorConfig.XpForLevel(level)` returns a threshold in `[1, int.MaxValue]`, computes growth without
integer overflow, and treats levels below one as level one. Invalid zero/negative thresholds therefore
cannot trap XP processing in a loop. This is a guard for authored data; configure positive XP values for
the intended progression curve.

## Owned resources

The shipped scenes contain no runtime-generated UI, sprites or materials: the builders refuse to save a
scene that would lose such objects. The controllers therefore only release what they borrow:

- The Level controller accepts unset `LevelManager` event fields: it subscribes only to existing events and
  never creates or replaces the borrowed events or the authored `LevelComponent`.
- `QuestDemoController` creates session-only fallback quests only when its scene was not built, and unregisters
  exactly those configs through `QuestManager.UnregisterQuest(expected)` before destroying them. Saved quest
  state, resource-loaded configs and a replacement config registered under the same ID remain intact; use the
  demo's reset actions when replaying it.
- `SurvivorGame` calls `SpawnUtility.RetirePool(source)` for its exact template identities, returns its
  tracked enemies and orbs, and allows outstanding rentals to be destroyed on their later return. It does not
  delete shared pool roots, so an unrelated pool with the same name survives.

The obsolete runtime frame `NeoDemoShell.Build(title, accent, owner)` still works for your own older demo
controllers. It parents the frame under the given owner and returns a `Context`: call `SetVisible` when
the controller is enabled or disabled and `Dispose` when it is removed. Disposal is idempotent and destroys
only the frame's own Canvas, gradient Sprite and Texture2D; shared cameras, EventSystems and sprites stay
usable. New demos should use a scene builder instead.

Русский: демо-сцены не создают UI и спрайты во время игры — всё сохранено билдером. Контроллеры
освобождают только своё: Level не подменяет события `LevelManager`, Quest снимает с регистрации только
собственные запасные квесты (сохранённый прогресс остаётся), Survivor завершает пулы по точным ссылкам на
свои шаблоны. Устаревший `NeoDemoShell` владеет только созданными им Canvas и градиентом.

## Verification

After importing the sample, run `Audit20261023NeoRpgDemoTests` in Edit Mode and
`Audit20261023NeoRpgDemoLifecycleTests`, `Audit20261023NeoRpgDemoSessionTests`, `SampleRuntimeSceneSmokeTests`,
`SampleScenePlaySmokeTests` and `AudioDemoScenePlayTests` in Play Mode. `SampleScenePlaySmokeTests` loads every
visible sample scene for 12 frames and fails on runtime errors, an unbuilt demo scene (`IsSceneBuilt == false` or an
unbuilt-scene warning), Filled Images without a sprite and a builder scene without its info card; its
`EveryRegisteredSceneBuilder_HasItsSceneSavedInTheVisibleSample` case fails for a builder whose scene was never
saved. `AudioDemoScenePlayTests` presses the authored AudioDemo buttons and removes the controller mid-crossfade.
The builder tests in `Neo.Demo.Editor.Tests` build each scene twice in a temporary copy and check its wiring.
These tests discover the optional assemblies at runtime and skip when the sample has not been imported; no test
asmdef references the hidden release sample. Exercise imported scenes separately to check input, presentation and
optional dependencies. The grid demos read the pointer through `MouseInputCompat` and the Survivor player reads
movement through `MoveInputCompat`, so both work with the Input System only; the Character Movement Fundamentals
example scenes need an Active Input Handling setting of **Both**.
