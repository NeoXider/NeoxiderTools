# Sample Scenes

Current active sample path during development: `Assets/Neoxider/Samples/Demo/`.

Before release/UPM packaging the folder is moved back to `Assets/Neoxider/Samples~/Demo/`, and `Assets/Neoxider/package.json` must point to `Samples~/...`.

After importing through Unity Package Manager, Unity copies the sample into the project-level path `Assets/Samples/NeoxiderTools/<version>/<sample name>/...`. For the current version the expected imported path is `Assets/Samples/NeoxiderTools/9.5.2/Demo Scenes/...`.

Network demo scripts must compile without Mirror installed. Mirror-specific code in imported Demo Scenes is optional and must be wrapped with the same `#if MIRROR` / solo-mode fallback pattern used by `Neo.Network`.

These scenes are smoke coverage and manual entry points for modules. They do not replace tests: public C# contracts stay covered by EditMode/PlayMode tests, while scenes show the minimal MonoBehaviour wrapper setup.

## Required Smoke Scenes

| Module | Scene | What it checks |
|--------|-------|----------------|
| Audio | `Scenes/Audio/AudioDemo.unity` | `AM`, `AudioSource`, base scene entry |
| Level | `Scenes/Level/LevelFlowDemo.unity` | `LevelManager` as a scene wrapper |
| Network | `Scenes/Network/NetworkDemo.unity` | `NeoNetworkManager` + Mirror transport |
| NoCode | `Scenes/NoCode/NoCodeBindingDemo.unity` | scene-only binding wrapper |
| Parallax | `Scenes/Parallax/ParallaxDemo.unity` | `ParallaxLayer` + visual template |
| Save | `Scenes/Save/SaveDemo.unity` | `SaveProviderSettingsComponent` + `SaveManager` |
| Settings | `Scenes/Settings/SettingsDemo.unity` | `GameSettingsComponent` defaults |
| StateMachine | `Scenes/StateMachine/StateMachineDemo.unity` | `StateMachineBehaviourBase` lifecycle entry |
| UI / AnimationFly | `Scenes/UI/AnimationFlyDemo.unity` | fly-effect coordinate spaces, sprite/prefab visuals, pooling, reward callbacks, and labeled sliders for count/timing/arc/scale/rotation |

## Gameplay And Integration Scenes

| Area | Scenes | Manual check |
|------|--------|--------------|
| GridSystem | `Scenes/GridSystem/GridSystemMatch3Demo.unity`, `Scenes/GridSystem/GridSystemTicTacToeDemo.unity` | board, rules, view binding |
| Progression | `Scenes/Progression_Demo.unity` | XP, level growth, progress bar |
| Quest | `Scenes/Quests/QuestDemo.unity` | quest flow and scene reload |
| RPG | `Scenes/RpgCharacterQuickDemo.unity`, `Scenes/RpgCombatNpcDemo.unity` | `RpgCombatNpcDemo` must show HUD, auto combat, player damage, NPC death, and automatic wave restore |
| Bonus | `Scenes/Bonuses/SlotExample.unity`, `Scenes/Bonuses/WheelFortuneExample.unity` | slot/wheel runtime loop |
| Shop | `Scenes/Shop/Shop.unity` | typed shop API |
| Tools | `Scenes/Tools/**` | isolated runtime/editor wrappers |
| Example games | `Scenes/_ExampleGame/**`, `Scenes/VampireSurvivorMCP.unity` | `VampireSurvivorMCP` must show HUD, spawner counters, player damage, death overlay, and reset |

## Automated Checks

`SampleAssetValidationTests` checks sample prefabs/scenes for missing scripts, missing MonoScript GUIDs, `NetworkBehaviour` components without `NetworkIdentity`, and broken `TerrainData.treePrototypes`. It resolves the active root from `Assets/Neoxider/Samples`, `Assets/Neoxider/Samples~`, or the imported root `Assets/Samples/NeoxiderTools`.

`SampleSceneCoverageTests` supports all three sample-root formats. For active development/imported samples it opens the required smoke scenes and verifies that they are not empty, contain `ModuleDemoSceneInfo`, and have no missing scripts. For hidden `Samples~`, Unity does not compile sample-only scripts, so the test validates `.unity` YAML instead: scene presence, `ModuleDemoSceneInfo`, RPG demo controllers, `RpgCharacter`, `Spawner`, and all referenced MonoScript GUIDs resolved through asset/package metadata.
