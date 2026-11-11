# Demo Scenes

**What it is:** the canonical page for the three demo downloads: where to get them, what each scene shows and how the scenes are made. Demos are not part of the package; each is a separate `.unitypackage` on the [releases page](https://github.com/NeoXider/NeoxiderTools/releases).

**How to use:**
1. Install the package ([Getting Started](./GettingStarted.md#installation)) and the [prerequisites](#host-and-sample-prerequisites): TMP Essential Resources and 2D Sprite.
2. Download `NeoxiderTools.Demo.v<ver>.unitypackage` and import it with **Assets → Import Package → Custom Package…**. Add `NeoxiderPages.v<ver>.unitypackage` and `NeoxiderTools.CharacterMovementDemo.v<ver>.unitypackage` for the example games.
3. Open a scene, for example `Assets/NeoxiderDemos/Demo/Scenes/Audio/AudioDemo.unity`. Select a button and read its **On Click** list in Edit Mode, then press Play.
4. To restore a scene you changed, re-import its download ([how demo scenes are made](#how-demo-scenes-are-made)).

**Navigation:** [← Docs](./README.md) · [Getting Started](./GettingStarted.md) · [Upgrading to 12](./Migration12.md)

---

## The three demos

| Download | Imported into | Contents |
|----------|---------------|----------|
| `NeoxiderTools.Demo.v<ver>.unitypackage` | `Assets/NeoxiderDemos/Demo/` | Module demo scenes, example games and `SurvivorDemo` ([guide](./Abilities/SurvivorDemo.md)) |
| `NeoxiderPages.v<ver>.unitypackage` | `Assets/NeoxiderDemos/NeoxiderPages/` | Optional page navigation (`PM`, `UIPage`, `BtnChangePage`, UIKit helpers; [guide](./NeoxiderPages/README.md)). Works on its own. |
| `NeoxiderTools.CharacterMovementDemo.v<ver>.unitypackage` | `Assets/NeoxiderDemos/CharacterMovementFundamentals/` | Character-controller showcase and special movement scenes ([guide](./Tools/Move/CharacterController/README.md)) |

The repository keeps the same folders, always visible to Unity. Scene paths below are relative to `Assets/NeoxiderDemos/Demo/`.

- Import all three before opening the example games: `_ExampleGame/CardGame.unity` uses the PM prefab and `UIPage` from NeoxiderPages, and the 9-Sliced background of `Tools/CameraAspectRatio.unity` uses its `background_game.png` (without it the background is empty, the scene still works).
- Module demos such as InventoryUseDemo, AbilitiesNoCodeDemo and AudioDemo need only the Demo download. Package prefabs need no demo: their art ships under `Assets/Neoxider/Art` and `ThirdParty/CharacterMovementFundamentals/Art` ([moved assets](./Migration12.md#package-assets-moved-out-of-the-samples)).
- Up to 12.1 the demos were Package Manager samples in `Assets/Samples/NeoxiderTools/<version>/<sample name>/`. Such a copy has the same GUIDs as the downloads: delete it before importing.

## Host and sample prerequisites

The UPM dependencies install the runtime and editor APIs, not every project resource the demos use. DOTween Core and UniTask are required as for any host ([Getting Started](GettingStarted.md)); the Pages demo also imports without DOTween Pro.

| Prerequisite | Scope and setup |
| --- | --- |
| TextMesh Pro Essential Resources | **Window → Text Mesh Pro → Import TMP Essential Resources** before opening text-based Demo or Pages scenes; TMP code in uGUI does not install these assets. |
| `com.unity.2d.sprite` 1.0.0 | For the Demo scenes that use Unity's default 2D sprites; not a dependency of the runtime modules. |
| Mirror | Optional; network gameplay needs it. Network demo scripts compile without it through the `#if MIRROR` / solo fallback of `Neo.Network`; check the solo build without Mirror's files, not only without its define. |

## How demo scenes are made

Every demo scene is saved complete: UI, configured components, data assets and persistent UnityEvent listeners are in the `.unity` file, and nothing is assembled at Play start. Most buttons call the module component directly (for example `SaveManager.FlushNow`, `InventoryHand.SetSlotIndex` or an `*NoCodeAction.Execute`); a small `*DemoController` only logs and refreshes labels.

The demos ship no scene-generation scripts. Edit a demo scene like any of your own; re-import the download to get the original back.

- Objects named `[Demo] <Key>` (`[Demo] Frame`, `[Demo] Services`, `[Demo] World`, `[Demo] Info Card`, ...) are the demo's own UI and helpers; objects without the prefix are the game or module on show.
- The collapsible info card names the components to inspect and the equivalent C# calls; the log feed lists events as they fire.
- `NeoDemoShell` and `ModuleDemoSceneInfo.Show` are obsolete fallbacks for your own older controllers; no shipped scene uses them.
- `_ExampleGame/ClickerExample`, `Shooter2D`, `adventure` and `Scenes/SurvivorDemo.unity` are NoCode reference games. `SurvivorDemo` has seven weapons and no game scripts.

**Save keys.** Demos save only under their own keys, so they never touch your game's saves under the library defaults: `NeoDemo.<Demo>.<Data>` (for example `NeoDemo.Clicker.Money` / `.Shop`, `NeoDemo.Shooter2D.Money`, `NeoDemo.Adventure.Inventory`, `NeoDemo.Progression.Level` / `.Profile` / `.Inventory`, `NeoDemo.Bonus.*`, `NeoDemo.Pages.*`), the older `Neo.Demo.*` prefixes, or their own files such as `neo_save_demo.json`. `SampleSceneSaveKeyTests` fails when a demo saves under a library default key.

## Module demo scenes

| Module | Scene | What it shows |
|--------|-------|---------------|
| Abilities | `Scenes/Abilities/AbilitiesNoCodeDemo.unity` | `AbilityNoCodeAction` buttons with results in the log, `SetProgress` HP and cooldown bars, an Auto Zap toggle bound to `AbilityAutoCaster`, unit and caster events. |
| Abilities | `Scenes/Abilities/AbilitiesTriggersDemo.unity` | Six lessons on a 2D arena: thorns, attack enhancer, mark for the next hits, stacking poison, formation zone, auto-defense shield; a "why did this fire" panel reads `AbilitySystem.Trace`. |
| Animations | `Scenes/Animations/AnimatorsDemo.unity` | `Vector3Animator` (scale), `ColorAnimator`, `TransformAnimator` (rotate, bob, pulse, Shake impulse) and `FloatAnimator` (card bar), bound by Inspector UnityEvents; Play, Pause, Resume and Stop with a shared AnimationType and speed. |
| Audio | `Scenes/Audio/AudioDemo.unity` | `AM` sound entries (ids, multi-clip sets, random pitch, UI channel), Loop and Shuffle music pools through `MusicControl`, `PlayAudioBtn`, `AMSettings` volumes and mutes on `Neo.Demo.Audio.*` keys. |
| Condition | `Scenes/Condition/NeoConditionDemo.unity` | `NeoCondition.OnResult` switching lamps, banners and a LOW HP badge through `GameObject.SetActive`; HP and score through `NoCodeFormattedText` and `SetProgress`; `NeoCondition.Evaluate()` from C#; checks that run while paused. |
| GridSystem | `Scenes/GridSystem/GridSystemMatch3Demo.unity`, `GridSystemTicTacToeDemo.unity`, `GridSystemDiceMergeDemo.unity` | `GridShapeMask` shapes, `GridPathfinder` path and reach highlights, movement presets, 5×5 four-in-a-row TicTacToe with a win-line highlight and an O bot, Dice placement hints and game over; an Edit Mode board preview. |
| Inventory | `Scenes/Inventory/InventoryUseDemo.unity` | Slot grid views whose buttons call `InventoryHand.SetSlotIndex`; `InventoryTotalCountText` / `InventoryItemCountText`; the use flow `InventoryHand` → `InventoryUseAction` → HP restore. |
| Level | `Scenes/Level/LevelFlowDemo.unity` | `LevelComponent` with the `LevelFlowDemoCurve` asset, `LevelNoCodeAction` and C# buttons; `LevelManager` with a finite and an infinite map, a `LevelButton` grid and a `Map.IsComplete` badge. |
| Network | `Scenes/Network/NetworkDemo.unity` | `NeoNetworkManager` + Telepathy, `NetworkActionRelay` channels and scopes, a replicated `RpgCharacter` boss, LAN discovery, telemetry rows. Needs Mirror for networking; compiles without it. |
| NoCode | `Scenes/NoCode/NoCodeBindingDemo.unity` | One Counter driving `NoCodeBindText`, `NoCodeFormattedText` and `SetProgress` in the Reactive, Poll and Once update modes. |
| NoCode | `Scenes/NoCode/NoCodeActionsDemo.unity` | Level, Progression, Quest and Rpg NoCode actions calling typed runtime APIs; result events write the labels and log. |
| NPC | `Scenes/Ai/AiMoveExample.unity` | `NpcNavigation` agents: Chaser, Guard (`SetMode`), Patroller and Sentry (Combined). |
| Parallax | `Scenes/Parallax/ParallaxDemo.unity` | Seven `ParallaxLayer` bands from -1 (locked to the camera) to +0.4 (foreground), auto-scrolling clouds, camera jumps. |
| Progression | `Scenes/Progression_Demo.unity` | XP and levels from C#, reward-track perk points, unlock and perk trees and premium through `ProgressionNoCodeAction`, `NoCodeBindText` labels. |
| Quest | `Scenes/Quests/QuestDemo.unity` | `QuestManager` journal from C#, `QuestNoCodeAction` notifications by Target Id, a Money reward on **On Quest Completed**; save key `NeoDemo.Quests`. |
| RPG | `Scenes/RpgCharacterQuickDemo.unity`, `Scenes/RpgCombatNpcDemo.unity` | `RpgCharacter` HUD bindings, `RpgNoCodeAction` buttons, `ApplyDamage` with a source, `DamageApplied`, an `IRpgDamageModifier` guard; auto combat, NPC deaths and wave restore. |
| Save | `Scenes/Save/SaveDemo.unity` | `[SaveField]` values through `SaveManager`, a 64-bit wallet through `SaveProvider.SetLong` / `GetLong`, `FileSaveProvider.HasUnsavedChanges` (a second Save writes nothing); its own `neo_save_demo.json` in `persistentDataPath`. |
| Settings | `Scenes/Settings/SettingsDemo.unity` | `GameSettingsComponent` with a Deferred save (`HasPendingDeferredSave`); keys prefixed `Neo.Demo.Settings.`. |
| Shop | `Scenes/Shop/Shop.unity` | Storefront cards with `ShopItem`, `ButtonPrice` and purchase states, a `TextMoney` wallet, typed `TryBuy` → `ShopPurchaseStatus`. |
| StateMachine | `Scenes/StateMachine/StateMachineDemo.unity` | A C# `StateMachine<IState>` agent next to a NoCode guard (`StateMachineBehaviourBase` with `StateMachineData` / `StateData` assets). |
| UI | `Scenes/UI/AnimationFlyDemo.unity` | `AnimationFly` from world, canvas and screen origins, sprite and prefab visuals, the `Bonus Prefab List` path, reward callbacks, tuning sliders, live readouts. |
| UI | `Scenes/UI/UIComponentsDemo.unity` | `CategoryBar` tabs switching a `VariantView` page, a `VariantView` badge, `AnchorMove`, `FakeLoad`, `ButtonShake`, a `PausePage` overlay (scaled and unscaled clocks), no code. |
| UI | `UI Mesh Rig/UIMeshRigDemo.unity` | One rig geometry through uGUI, UI Toolkit, world MeshFilter/MeshRenderer and SpriteRenderer outputs. |
| Bonus | `Scenes/Bonuses/SlotExample.unity`, `Scenes/Bonuses/WheelFortuneExample.unity` | Slot and wheel with a session wallet, bets, paylines and a paytable; wheel prizes through `WheelMoneyWin`. |
| Bonus | `Scenes/Bonuses/BonusRewardsDemo.unity` | A reward loop with no demo script ([details](#bonusrewardsdemo-and-physics3ddemo)). |
| Tools | `Scenes/Tools/AttributeExample.unity` | `[FindInScene]`, `[GetComponent]`, `[RequireInterface]` and `[Button]` resolved in Edit Mode next to the same lookups in C#; a second target for multi-object `[Button]` actions. |
| Tools | `Scenes/Tools/CameraAspectRatio.unity` | `CameraAspectRatioScaler` scale modes, design resolution and reference size; phone, tablet and ultrawide previews. |
| Tools | `Scenes/Tools/CameraConstraint.unity` | `CameraConstraint` bounds, edge checks, zoom and padding. |
| Tools | `Scenes/Tools/ChanceSystemExample.unity` | `ChanceSystemBehaviour` events (NoCode), `ChanceManager` sampling from C#, exact `IntegerChanceTable` jackpot odds (1 in 1,000,000). |
| Tools | `Scenes/Tools/Dialogue.unity` | A `DialogueController` box with typewriter and auto-advance; a `DialogueLinePlayer` bark panel (idle lines, a contextual interrupt, first-only greetings). |
| Tools | `Scenes/Tools/DrawExample.unity` | `Drawer` strokes with smoothing, width, auto-delete and live colliders. |
| Tools | `Scenes/Tools/MouseEffectorExample.unity` | `MouseInputManager` (hold enabled) with `MouseEffect` spawn triggers. |
| Tools | `Scenes/Tools/Move/Follow.unity` | `Follow` with a draggable target, a rail follower with position limits, a turret with a 2D aim limit. |
| Tools | `Scenes/Tools/Physics/Physics3DDemo.unity` | Magnet, impulse zone and explosive force ([details](#bonusrewardsdemo-and-physics3ddemo)). |
| Tools | `Scenes/Tools/View/Light.unity`, `Scenes/Tools/View/Selector.unity` | Five `LightAnimator`s with play/pause, speed, manual `Tick` and unscaled time; three wired `Selector`s (index, fill, fill with offset). |
| NeoxiderPages | `Assets/NeoxiderDemos/NeoxiderPages/Demo/Scenes/UI.unity` | `PM` with its `UIPage` pages, audio and system prefabs; a plain-uGUI `[Demo] Pages Info` card (so Pages imports alone) with Pause popup, GM Win, Back and Close buttons. |

### BonusRewardsDemo and Physics3DDemo

`Scenes/Bonuses/BonusRewardsDemo.unity`:

- a `Collection` of six pizzas with `CollectionVisualManager` slots and an `ItemCollectionInfo` details row;
- a `Box` chest filled by a `CooldownReward` gift that grants a random locked pizza;
- the legacy `TimeReward` paying a session-only `Money` wallet, and a reset; every key contains `NeoDemo.Bonus.`.

`Scenes/Tools/Physics/Physics3DDemo.unity`, every control a persistent listener:

- `MagneticField` pulsing between pull and push (`ToggleMode`) in a ring of steel balls;
- an `ImpulseZone` over an elastic plate that keeps three balls bouncing; Kick calls `ApplyImpulseToObject`;
- a manual `ExplosiveForce` mine (`Explode()`, `Explode(18)`) under a crate pyramid.

## Example games and integration scenes

| Scene | What to look at |
|-------|-----------------|
| `Scenes/SurvivorDemo.unity` | A Vampire-Survivors-style NoCode game with seven weapons, built from library components and UnityEvents on `Neo.Abilities`, with no game scripts ([guide](./Abilities/SurvivorDemo.md)). Has a **+10 XP** button. |
| `Scenes/VampireSurvivorMCP.unity` | RPG-based survivor game with an authored HUD, spawner counters, player damage, a death overlay and reset. Esc frees the cursor. |
| `Scenes/_ExampleGame/CardGame.unity` | Drunkard card game; the `[Demo] Guide` adds an info card, a live `DrunkardGame` log and a "Same deal every restart" toggle (`DeckComponent.UseFixedShuffleSeed`). Needs NeoxiderPages. |
| `Scenes/_ExampleGame/ClickerExample.unity`, `Shooter2D.unity`, `adventure.unity` | NoCode reference games: ready components, Inspector wiring and binding scripts only, each with an info card. ClickerExample is portrait (1080x1920). |

## Modules without a demo scene

Their pages show the setup and C# calls instead:

- Tools/FakeLeaderboard (`Leaderboard`): `UpdatePlayerScore(int, bool)` cannot be a persistent UnityEvent target yet.
- Tools/Debug (`ErrorLogger`, `NeoDebugOverlay`), `CameraShake` and Haptics.
- Core resources alone (`HealthComponent`, `ResourcePoolModel`); the RPG demos show `RpgCharacter`.
- Shop equipment and variants (`EquipmentManager`, `ShopVariantsPanel`).
- `SwipeController`, `StarView`, and Move tools beyond Follow and CameraConstraint (`FreeFlyCameraController`, `UniversalRotator`, `AdvancedForceApplier`, `DistanceChecker`).
- A Cards sandbox (custom sorting, `PokerHandEvaluator`, custom cards); CardGame shows Drunkard only.

Open demo work: [dev-docs/TODO.md](https://github.com/NeoXider/NeoxiderTools/blob/main/dev-docs/TODO.md).

## Release archives

Every [release](https://github.com/NeoXider/NeoxiderTools/releases) carries the package and each demo as separate archives:

| Archive | Contents |
|---|---|
| `NeoxiderTools.v<ver>.unitypackage` | The package, imported into `Assets/Neoxider`; same files as the `upm` branch. |
| `NeoxiderTools.Demo.v<ver>.unitypackage` | Module demo scenes, imported into `Assets/NeoxiderDemos/Demo`. |
| `NeoxiderPages.v<ver>.unitypackage` | Page navigation and its demo, imported into `Assets/NeoxiderDemos/NeoxiderPages`. |
| `NeoxiderTools.CharacterMovementDemo.v<ver>.unitypackage` | Character controller scenes, imported into `Assets/NeoxiderDemos/CharacterMovementFundamentals`. |

Install the package first, then the demos you need. `python scripts/build_distribution.py unitypackages` builds the archives from a committed revision. The 11.0.0 archives (`NeoxiderTools.Core` without samples; `NeoxiderTools` with all three trees under `Assets/Neoxider/Samples`) and their `Assets/Neoxider/SharedAssets` staging are described in [Verification 11](../Verification11.md); do not mix their GUID copies with the current downloads.

## Troubleshooting and automated checks

- **Missing scripts in Character Movement Fundamentals scenes** although the script GUIDs resolve: the Library holds stale script data, for example after updating from a version where the controller scripts lived in the demo. Right-click `Assets/Neoxider/ThirdParty/CharacterMovementFundamentals/Scripts` (or the package's `ThirdParty` folder) and choose **Reimport**; the assets are fine.
- **Input handling.** The Character Movement Fundamentals scenes need Active Input Handling = Both; SurvivorDemo runs with either setting.
- **Automated checks.** `SampleAssetValidationTests` (missing scripts and MonoScript GUIDs, `NetworkBehaviour` without `NetworkIdentity`, broken `TerrainData.treePrototypes`), `SampleScenePlaySmokeTests` (every scene loads and ticks 12 frames cleanly, with its info card), `AudioDemoScenePlayTests`, `SampleSceneSaveKeyTests` and `PackageSampleIsolationTests` (no package asset references a demo-only GUID). An absent demo tree is skipped. Details: [Testing](./Testing.md#sample-validation).

## По-русски

Демо — отдельные архивы на странице релизов; импортируйте их после пакета, они ставятся в `Assets/NeoxiderDemos/<имя>`. Нужны TMP Essential Resources и пакет 2D Sprite; Mirror опционален. Сцены сохранены целиком: UI, компоненты и UnityEvent-связи видны в Inspector без запуска, генераторов сцен нет, исходную сцену возвращает повторный импорт. Объекты `[Demo] ...` — UI и помощники демо, без префикса — показываемые модули. Демо сохраняют данные под своими ключами `NeoDemo.<Demo>.<Data>`. Копию сэмплов 12.1 и раньше из `Assets/Samples/NeoxiderTools/<версия>/` удалите перед импортом: GUID совпадают. Модули без демо-сцены: [Modules without a demo scene](#modules-without-a-demo-scene).
