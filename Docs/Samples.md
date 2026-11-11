# Demo Scenes

**What it is:** the index of the three demo downloads: what each scene shows, how the scenes are authored and rebuilt, and where they live. Demos are not part of the package; each is a separate `.unitypackage` on the [releases page](https://github.com/NeoXider/NeoxiderTools/releases).

**How to use:**
1. Install the package ([Getting Started](./GettingStarted.md)) and prepare the host project: [prerequisites](#host-and-sample-prerequisites) (TMP Essential Resources, 2D Sprite).
2. Download and import `NeoxiderTools.Demo.v<ver>.unitypackage`. Import `NeoxiderPages.v<ver>.unitypackage` and `NeoxiderTools.CharacterMovementDemo.v<ver>.unitypackage` too if you want the example games.
3. Open a scene, for example `Assets/NeoxiderDemos/Demo/Scenes/Audio/AudioDemo.unity`. The UI, the configured components and every button's UnityEvent are already in the scene: inspect them in Edit Mode, then press Play.
4. To restore a demo scene after editing it, use **Neoxider > Demo > Rebuild Active Demo Scene** ([how demo scenes are made](#how-demo-scenes-are-made)).

**Navigation:** [← Docs](./README.md) · [Getting Started](./GettingStarted.md) · [Upgrading to 12](./Migration12.md)

---

## Demo roots

| Download | Root |
|----------|------|
| `NeoxiderTools.Demo.v<ver>.unitypackage` | `Assets/NeoxiderDemos/Demo/` |
| `NeoxiderPages.v<ver>.unitypackage` | `Assets/NeoxiderDemos/NeoxiderPages/` |
| `NeoxiderTools.CharacterMovementDemo.v<ver>.unitypackage` | `Assets/NeoxiderDemos/CharacterMovementFundamentals/` |

The repository keeps the same folders, always visible to Unity, so demo development needs no renaming. The paths in the scene tables below are relative to `Assets/NeoxiderDemos/Demo/`.

Up to 12.1 the demos were Package Manager samples, copied into `Assets/Samples/NeoxiderTools/<version>/<sample name>/`. Such a copy shares asset GUIDs with the downloads: delete it before importing a demo `.unitypackage`.

## Host and sample prerequisites

For the complete NeoxiderTools host, DOTween **Core** and UniTask are the existing required dependencies;
follow [Getting Started](GettingStarted.md). The standalone Pages sample keeps its own no-DOTween contract.
The package's declared UPM dependencies install the runtime/editor APIs, but not all project resources used by samples:

| Prerequisite | Scope / setup |
| --- | --- |
| TextMesh Pro Essential Resources | Import the official **Window → Text Mesh Pro → Import TMP Essential Resources** into the consuming project before opening text-based Demo/Pages scenes. TMP code in uGUI does not itself install these font/material/shader assets. |
| `com.unity.2d.sprite` 1.0.0 | Install for the Demo scenes that reference Unity's default 2D sprite assets. This is a sample/editor resource prerequisite, not a new dependency of every runtime module. |
| Mirror | Optional. Network gameplay/transport validation requires the matching installed backend; solo-mode compilation must be checked separately without its payload/symbols. |

Network demo scripts are written to compile without Mirror installed: Mirror-specific code in Demo Scenes uses the same `#if MIRROR` / solo-mode fallback pattern as `Neo.Network`.

**По-русски:** для текстовых Demo/Pages импортируйте официальные TMP Essential Resources; для всей
коллекции Demo также нужен пакет 2D Sprite с его стандартными ассетами. Это отдельные ресурсы хоста,
а не доказательство успешного импорта демо. Mirror остаётся опциональным; его отсутствие нельзя
проверить только выключением символа при оставленных файлах.

## The three demos

| Download | Folder | What it contains |
|----------|--------|------------------|
| **NeoxiderTools.Demo** | `Assets/NeoxiderDemos/Demo` | Module demo scenes, example games and `SurvivorDemo` ([guide](./Abilities/SurvivorDemo.md)), plus the editor scene builders that author them. |
| **NeoxiderPages** | `Assets/NeoxiderDemos/NeoxiderPages` | Optional page navigation: `PM`, `UIPage`, `BtnChangePage`, and UIKit helpers ([guide](./NeoxiderPages/README.md)). Imports and runs on its own. |
| **NeoxiderTools.CharacterMovementDemo** | `Assets/NeoxiderDemos/CharacterMovementFundamentals` | Character-controller showcase, special movement scenes, and their supporting art/audio ([guide](./Tools/Move/CharacterController/README.md)). |

For the full collection, import **all three demos** before opening the example games. Some scenes reference assets in a sibling demo:

- `_ExampleGame/CardGame.unity` uses the PM prefab and `UIPage` from **NeoxiderPages**.
- The **9-Sliced** background of `Tools/CameraAspectRatio.unity` uses `background_game.png` from **NeoxiderPages**. With only Demo Scenes imported the background stays empty; the scene remains usable.

Module demos such as InventoryUseDemo, AbilitiesNoCodeDemo and AudioDemo need only **Demo Scenes**. The package prefabs themselves (slot, shop item, bullet, NPC, the bundled Character Movement Fundamentals controller) no longer need any sample: their art ships in the package under `Assets/Neoxider/Art` and `ThirdParty/CharacterMovementFundamentals/Art` ([moved assets](./Migration12.md#package-assets-moved-out-of-the-samples)).

## How demo scenes are made

Every demo scene is saved complete: UI, configured module components, data assets and persistent UnityEvent listeners are in the `.unity` file. Nothing is assembled at Play start, so the Inspector shows exactly how a button reaches a module API. This is also the NoCode path: most buttons call the module component directly (for example `SaveManager.FlushNow`, `InventoryHand.SetSlotIndex` or an `*NoCodeAction.Execute`), and a small `*DemoController` only logs and refreshes labels.

Each scene is produced by an editor **builder**: a static method in `Demo/Editor/Builders/<Module>/<Scene>Builder.cs` (editor assembly `Neo.Demo.Editor`) that authors the scene through a small kit. In a built scene:

- objects named `[Demo] <Key>` (`[Demo] Frame`, `[Demo] Services`, `[Demo] World`, `[Demo] Info Card`, ...) are generated by the builder;
- objects without the `[Demo]` prefix are hand-authored (the game in CardGame or VampireSurvivorMCP, the world of the RPG demos) and are never removed by a rebuild;
- the collapsible info card names the components to inspect and the equivalent C# calls; the log feed lists the events as they fire.

The **Neoxider > Demo** menu appears while the Demo download is in the project (`Assets/NeoxiderDemos/Demo`), because the builders live in the demo:

| Menu item | Effect |
|---|---|
| **Rebuild Active Demo Scene** | Runs the builder of the open scene, validates it and saves it. |
| **Rebuild All Demo Scenes** | Same for every scene that has a builder; open scenes are restored afterwards. |
| **List Scene Builders** | Logs every registered builder and its scene. |
| **Author Info Card In Active Scene** / **Author Missing Info Cards (All Demo Scenes)** | Adds the authored info card to scenes without a builder. |

A rebuild is idempotent. It recreates everything under the `[Demo]` roots and deletes objects you added there, keeps hand-authored objects, and does not save a scene that fails validation (missing scripts, listeners without a target, runtime-generated sprites). When a builder's scene does not exist yet, the rebuild creates it first (an empty scene at the builder path, missing folders included). Put your own changes on hand-authored objects, in a copy of the scene, or in the builder itself. Batch mode uses `-executeMethod Neo.Demo.Editor.DemoSceneBatch.RebuildAll` with an optional `-neoDemoScenes "SaveDemo;AudioDemo"` filter; the full builder guide is in the repository's [dev-docs/DemoSceneBuilders.md](https://github.com/NeoXider/NeoxiderTools/blob/main/dev-docs/DemoSceneBuilders.md).

The runtime frame `NeoDemoShell` and `ModuleDemoSceneInfo.Show` are obsolete fallbacks for your own older demo controllers; no shipped scene uses them. `_ExampleGame/ClickerExample`, `Shooter2D` and `adventure` are hand-authored NoCode reference games; their builders (`Editor/Builders/ExampleGames`) only add the info card, upgrade the input module and set demo-only save keys, and never rebuild the game itself.

**Save keys.** Demos persist only under their own keys, `NeoDemo.<Demo>.<Data>` (for example `NeoDemo.Clicker.Money` / `.Shop`, `NeoDemo.Shooter2D.Money`, `NeoDemo.Adventure.Inventory`, `NeoDemo.Progression.Level` / `.Profile` / `.Inventory`, `NeoDemo.Bonus.*`, `NeoDemo.Pages.*`), the older `Neo.Demo.*` prefixes, or their own files (`neo_save_demo.json`). Running a demo in your project never reads or overwrites your game's saves under the library default keys; `SampleSceneSaveKeyTests` fails when a sample scene or prefab persists under such a key.

## Module demo scenes

| Module | Scene | What it shows |
|--------|-------|---------------|
| Abilities | `Scenes/Abilities/AbilitiesNoCodeDemo.unity` | `AbilityNoCodeAction` buttons (results in the log), `SetProgress` HP and cooldown bars, an Auto Zap toggle bound to `AbilityAutoCaster`, unit and caster events. |
| Abilities | `Scenes/Abilities/AbilitiesTriggersDemo.unity` | Six lessons on a 2D arena: thorns (reflect), attack enhancer, mark for the next hits, stacking poison, a formation zone and an auto-defense shield; the "why did this fire" panel reads `AbilitySystem.Trace`. |
| Animations | `Scenes/Animations/AnimatorsDemo.unity` | `Vector3Animator` (scale), `ColorAnimator` (colour), `TransformAnimator` (rotate, bob, pulse, Shake impulse) and `FloatAnimator` (card bar) on real targets; value bindings are Inspector UnityEvents; Play/Pause/Resume/Stop, a shared AnimationType and speed. Menu **Neoxider > Demo > Open Animators Demo**. |
| Audio | `Scenes/Audio/AudioDemo.unity` | `AM` sound entries (ids, multi-clip sets, random pitch, UI channel), Loop/Shuffle music pools through `MusicControl`, `PlayAudioBtn`, and `AMSettings` persisted volumes and mutes on demo-only keys (`Neo.Demo.Audio.*`). |
| Condition | `Scenes/Condition/NeoConditionDemo.unity` | `NeoCondition.OnResult` switching lamps, banners and a LOW HP badge through `GameObject.SetActive`; HP and score through `NoCodeFormattedText` and `SetProgress`; `NeoCondition.Evaluate()` from C# and conditions that keep checking while paused. |
| GridSystem | `Scenes/GridSystem/GridSystemMatch3Demo.unity`, `GridSystemTicTacToeDemo.unity`, `GridSystemDiceMergeDemo.unity` | Shapes from a `GridShapeMask`, `GridPathfinder` path and reach highlights, movement presets, a 5×5 four-in-a-row TicTacToe mode with a winning-line highlight and an O bot, Dice placement hints and game over. A board preview is visible in Edit Mode. |
| Inventory | `Scenes/Inventory/InventoryUseDemo.unity` | Slot grid views; each slot button calls `InventoryHand.SetSlotIndex`; `InventoryTotalCountText` / `InventoryItemCountText`; use flow `InventoryHand` → `InventoryUseAction` → HP restore. |
| Level | `Scenes/Level/LevelFlowDemo.unity` | `LevelComponent` with the `LevelFlowDemoCurve` asset and `LevelNoCodeAction` buttons next to C# buttons; `LevelManager` with a finite and an infinite map, a `LevelButton` grid and a `Map.IsComplete` badge. |
| Network | `Scenes/Network/NetworkDemo.unity` | `NeoNetworkManager` + Telepathy, `NetworkActionRelay` channels and scopes, a replicated `RpgCharacter` boss, LAN discovery and telemetry rows. Needs Mirror for networking; compiles without it. |
| NoCode | `Scenes/NoCode/NoCodeBindingDemo.unity` | One Counter driving `NoCodeBindText`, `NoCodeFormattedText` and `SetProgress`, with the Reactive / Poll / Once update modes side by side. |
| NoCode | `Scenes/NoCode/NoCodeActionsDemo.unity` | Level / Progression / Quest / Rpg NoCode actions calling typed runtime APIs; result events write the labels and the log. |
| NPC | `Scenes/Ai/AiMoveExample.unity` | `NpcNavigation` agents: Chaser, Guard (`SetMode`), Patroller and Sentry (Combined). |
| Parallax | `Scenes/Parallax/ParallaxDemo.unity` | Seven camera-bound `ParallaxLayer` bands from -1 (locked to the camera) to +0.4 (foreground), auto-scrolling clouds and camera jumps. |
| Progression | `Scenes/Progression_Demo.unity` | XP and levels from C#, reward-track perk points, unlock and perk trees and premium through `ProgressionNoCodeAction`, `NoCodeBindText` labels. |
| Quest | `Scenes/Quests/QuestDemo.unity` | `QuestManager` journal from C#, `QuestNoCodeAction` notifications by Target Id, a Money reward wired to **On Quest Completed**; save key `NeoDemo.Quests`. |
| RPG | `Scenes/RpgCharacterQuickDemo.unity`, `Scenes/RpgCombatNpcDemo.unity` | `RpgCharacter` HUD bindings and `RpgNoCodeAction` buttons, `ApplyDamage` with a source, `DamageApplied`, an `IRpgDamageModifier` guard; the combat scene shows auto combat, NPC deaths and wave restore. |
| Save | `Scenes/Save/SaveDemo.unity` | `[SaveField]` values through `SaveManager`, a 64-bit wallet through `SaveProvider.SetLong`/`GetLong`, and `FileSaveProvider.HasUnsavedChanges` (a second Save writes nothing). Uses its own `neo_save_demo.json` in `persistentDataPath`. |
| Settings | `Scenes/Settings/SettingsDemo.unity` | `GameSettingsComponent` with a Deferred save (`HasPendingDeferredSave`); keys use the `Neo.Demo.Settings.` prefix. |
| Shop | `Scenes/Shop/Shop.unity` | Storefront cards with `ShopItem`, `ButtonPrice` and purchase states, a `TextMoney` wallet, typed `TryBuy` → `ShopPurchaseStatus`. |
| StateMachine | `Scenes/StateMachine/StateMachineDemo.unity` | A C# `StateMachine<IState>` agent next to a NoCode guard (`StateMachineBehaviourBase` with saved `StateMachineData` / `StateData` assets). |
| UI | `Scenes/UI/AnimationFlyDemo.unity` | `AnimationFly` from world, canvas and screen origins, sprite and prefab visuals, the `Bonus Prefab List` path, reward callbacks, tuning sliders and live readouts. |
| UI | `Scenes/UI/UIComponentsDemo.unity` | `CategoryBar` tabs switching a `VariantView` page, a `VariantView` badge, `AnchorMove`, `FakeLoad`, `ButtonShake` and a `PausePage` overlay (scaled vs unscaled clocks), all wired without code. |
| UI | `UI Mesh Rig/UIMeshRigDemo.unity` | One rig geometry through uGUI, UI Toolkit, world MeshFilter/MeshRenderer and SpriteRenderer outputs. |
| Bonus | `Scenes/Bonuses/SlotExample.unity`, `Scenes/Bonuses/WheelFortuneExample.unity` | Slot and wheel with a session wallet, bets, paylines and a paytable; wheel prizes paid through `WheelMoneyWin`. |
| Bonus | `Scenes/Bonuses/BonusRewardsDemo.unity` | A reward loop without a demo script: a `Collection` of six pizzas with `CollectionVisualManager` slots and an `ItemCollectionInfo` details row, a `Box` chest filled by a `CooldownReward` gift that grants a random locked pizza, the legacy `TimeReward` paying into a session-only `Money` wallet, and a reset; every save key contains `NeoDemo.Bonus.`. |
| Tools | `Scenes/Tools/AttributeExample.unity` | Inspector attributes (`[FindInScene]`, `[GetComponent]`, `[RequireInterface]`, `[Button]`) resolved in Edit Mode, compared with the same lookups in C#; a second target for multi-object `[Button]` actions. |
| Tools | `Scenes/Tools/CameraAspectRatio.unity` | `CameraAspectRatioScaler` scale modes, design resolution and reference size, with phone/tablet/ultrawide previews. |
| Tools | `Scenes/Tools/CameraConstraint.unity` | `CameraConstraint` bounds, edge checks, zoom and padding. |
| Tools | `Scenes/Tools/ChanceSystemExample.unity` | `ChanceSystemBehaviour` events (NoCode), `ChanceManager` sampling from C#, and exact `IntegerChanceTable` jackpot odds (1 in 1,000,000). |
| Tools | `Scenes/Tools/Dialogue.unity` | A `DialogueController` dialogue box with typewriter and auto-advance controls, and a `DialogueLinePlayer` bark panel (idle lines, a contextual interrupt, first-only greetings). |
| Tools | `Scenes/Tools/DrawExample.unity` | `Drawer` strokes with smoothing, width, auto-delete and live-collider controls. |
| Tools | `Scenes/Tools/MouseEffectorExample.unity` | `MouseInputManager` (hold enabled) with `MouseEffect` spawn triggers. |
| Tools | `Scenes/Tools/Move/Follow.unity` | `Follow` with a draggable target, a rail follower with position limits and a turret with a 2D aim limit. |
| Tools | `Scenes/Tools/Physics/Physics3DDemo.unity` | `MagneticField` pulsing between pull and push (`ToggleMode`) inside a ring of steel balls, an `ImpulseZone` trigger above an elastic plate that keeps three balls bouncing (Kick calls `ApplyImpulseToObject`), and a manual `ExplosiveForce` mine (`Explode()`, `Explode(18)`) under a crate pyramid; every control is a persistent listener. |
| Tools | `Scenes/Tools/View/Light.unity`, `Scenes/Tools/View/Selector.unity` | Five `LightAnimator`s with play/pause, speed, manual `Tick` and unscaled time; three wired `Selector`s (index, fill, fill with offset). |
| NeoxiderPages | `NeoxiderPages/Demo/Scenes/UI.unity` (in the NeoxiderPages sample) | `PM` with its `UIPage` pages, audio and system prefabs; the `[Demo] Pages Info` card (plain uGUI, so the Pages sample still imports alone) explains PM and has Pause popup / GM Win / Back / Close buttons. |

## Example games and integration scenes

| Scene | What to look at |
|-------|-----------------|
| `Scenes/SurvivorDemo.unity` | A Vampire-Survivors-style run assembled from one `SurvivorConfig` on top of `Neo.Abilities` ([guide](./Abilities/SurvivorDemo.md)). Includes a **Level up now** button. After changing the config's enemies or projectile archetypes, rebuild the scene. |
| `Scenes/VampireSurvivorMCP.unity` | RPG-based survivor game with an authored HUD, spawner counters, player damage, a death overlay and reset. Press Esc to free the cursor. |
| `Scenes/_ExampleGame/CardGame.unity` | Drunkard card game; the `[Demo] Guide` adds an info card, a live `DrunkardGame` event log and a "Same deal every restart" toggle (`DeckComponent.UseFixedShuffleSeed`). Needs NeoxiderPages. |
| `Scenes/_ExampleGame/ClickerExample.unity`, `Shooter2D.unity`, `adventure.unity` | Hand-authored NoCode reference games: ready components, Inspector event wiring and binding scripts only. Each has an info card naming the key objects and their wiring. ClickerExample is a portrait (1080x1920) game. |

## Modules without a demo scene

These modules and components have no scene yet; their pages show the setup and the C# calls instead:

- Tools/FakeLeaderboard (`Leaderboard`): `UpdatePlayerScore(int, bool)` cannot be a persistent UnityEvent target yet.
- Tools/Debug (`ErrorLogger`, `NeoDebugOverlay`), `CameraShake` and Haptics.
- Core resources on their own (`HealthComponent`, `ResourcePoolModel`); the RPG demos show `RpgCharacter` instead.
- Shop equipment and variants (`EquipmentManager`, `ShopVariantsPanel`).
- `SwipeController`, `StarView`, and the Move toolkit beyond Follow and CameraConstraint (`FreeFlyCameraController`, `UniversalRotator`, `AdvancedForceApplier`, `DistanceChecker`).
- A Cards sandbox (custom sorting, `PokerHandEvaluator`, custom cards); CardGame shows the Drunkard game only.

The open demo work is tracked in the repository's [dev-docs/TODO.md](https://github.com/NeoXider/NeoxiderTools/blob/main/dev-docs/TODO.md).

## Release archives

Since 12.2.0 every release carries the package and each demo as separate archives (`NeoxiderTools.v<ver>`, `NeoxiderTools.Demo.v<ver>`, `NeoxiderPages.v<ver>`, `NeoxiderTools.CharacterMovementDemo.v<ver>`), built by `python scripts/build_distribution.py unitypackages` from a committed revision. The 11.0.0 archives (`NeoxiderTools.Core` without samples and `NeoxiderTools` with all three trees under `Assets/Neoxider/Samples`) and their `Assets/Neoxider/SharedAssets` staging are described in [Verification 11](../Verification11.md); do not combine their GUID copies with the new downloads.

## Automated checks

`SampleAssetValidationTests` checks demo prefabs and scenes under `Assets/NeoxiderDemos` for missing scripts, missing MonoScript GUIDs, `NetworkBehaviour` components without `NetworkIdentity`, and broken `TerrainData.treePrototypes`; a demo tree that is absent is skipped, a reference that resolves nowhere still fails. The [testing guide](./Testing.md#sample-validation) describes scene ownership and isolation for these checks.

If the Character Movement Fundamentals scenes or prefabs show missing scripts although their script GUIDs resolve (for example after updating from a version where the controller scripts lived in the demo), the project's Library holds stale script data: right-click `Assets/Neoxider/ThirdParty/CharacterMovementFundamentals/Scripts` (or the package's `ThirdParty` folder) and choose **Reimport**. The asset files themselves are fine.

`SampleScenePlaySmokeTests` (Play Mode) loads every demo scene, ticks 12 frames and fails on error, assert or exception logs, Filled Images without a sprite, a demo controller that reports an unbuilt scene, and a builder-owned scene without its authored info card; it also fails when a registered builder has no saved scene. `AudioDemoScenePlayTests` presses the authored AudioDemo buttons. `SampleSceneSaveKeyTests` rejects library default save keys in demo scenes and prefabs, and `PackageSampleIsolationTests` rejects package assets that reference a GUID shipped only with the demos. The Character Movement Fundamentals scenes report Ignored in Input System-only projects (they need Active Input Handling = Both).

The builders have their own tests in `Neo.Demo.Editor.Tests` (`Assets/NeoxiderDemos/Demo/Editor/Tests`). Each builds a temporary copy of its scene twice and checks the wiring, that hand-authored objects survive and that the second pass creates and removes nothing.

## По-русски

Демо-сцены сохранены целиком: UI, настроенные компоненты и UnityEvent-связи видны в Inspector без запуска игры. Каждую сцену собирает редакторный билдер из сэмпла (`Demo/Editor/Builders`); пересборка — **Neoxider > Demo > Rebuild Active Demo Scene**, а отсутствующую сцену билдера пересборка создаёт сама. Новые сцены: Animations (`AnimatorsDemo`), UI Components, Bonus Rewards и Physics 3D; у игр ClickerExample, Shooter2D, adventure и у сцены Pages `UI.unity` есть инфо-карточка. Демо сохраняют данные только под своими ключами `NeoDemo.<Demo>.<Data>`. Модули без демо-сцены перечислены в разделе [Modules without a demo scene](#modules-without-a-demo-scene). Объекты `[Demo] ...` создаёт билдер и пересоздаёт при пересборке, объекты без префикса — ручные и сохраняются. Меню доступно, пока сэмпл Demo виден Unity (папка `Assets/NeoxiderDemos/Demo`).
