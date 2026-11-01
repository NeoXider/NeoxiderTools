# Sample Scenes

Samples ship hidden in `Samples~`; import them via **Package Manager → NeoxiderTools → Samples**. The current package metadata is `11.0.1`.

> **11.0.0.** Windows verification recorded on 26–27 October 2026 includes fresh P31 UPM imports of all three samples and **20/20 imported Play** cases per SDK, with DOTween Pro absent; **28/28 Edit** retains P30 evidence through unchanged graph identity. The tested Editors are Unity `6000.3.14f1` and `6000.6.0f1`. CardGame sizing/gameplay acceptance includes automated checks and three manual OS-click rounds. See [verification scope](../Verification11.md) and [Migration to 11](./Migration11.md). Use the tagged release for a pinned installation.

## Sample roots

| Layout | Root | Use |
|--------|------|-----|
| **Current release source** | `Assets/Neoxider/Samples~/` | Hidden UPM sample sources; `package.json.samples[].path` points to `Samples~/...`. |
| **Development source** | `Assets/Neoxider/Samples/` | Alternative active source folder while editing sample scenes/scripts in Unity. Restore `Samples~` and check package paths before release. |
| **Imported project copy** | `Assets/Samples/NeoxiderTools/11.0.1/<sample name>/` | Unity copies the selected sample here; it does not copy it back into the package. |

Only one source layout needs to be active. For a UPM installation the hidden source is inside the installed package, while the imported project copy still appears under `Assets/Samples/NeoxiderTools/<version>/`.

### Legacy release archives: choose one

| Archive | Legacy manifest / visible content |
|---|---|
| `NeoxiderTools.Core.v11.0.0.unitypackage` | `samples: []`; no sample trees. |
| `NeoxiderTools.v11.0.0.unitypackage` | Complete/Core plus `Samples/Demo`, `Samples/NeoxiderPages`, `Samples/CharacterMovementFundamentals` under Assets/Neoxider. |

Required prefab textures/materials/animations are staged under `Assets/Neoxider/SharedAssets` in both legacy variants. Their asset bytes and GUIDs are preserved; serialized references remain valid. The source `Samples~` tree is not moved. The legacy Pages Shine material uses built-in `UI/Default` instead of a missing optional shader; its former proprietary glint effect is not supplied. An inactive missing particle-mask property was removed while preserving its active texture and tint.

These are archive-specific variants; main/source UPM retains `Samples~`. Legacy imports do not install UPM
dependencies automatically. Do not combine duplicate UPM/legacy GUID copies. The separate
`HtmlToUitk.Cli.v11.0.0.zip` supplies the converter; Unity archives do not contain Tools. Actual final
export/import and publication status is in [Verification 11](../Verification11.md), not these intended layouts.

The `demo` branch does not require renaming the release source root. Earlier imported versions remain separate project copies; compare customized content before adopting the 11.0.0 sample copy.

| Package Manager sample | Package source | What it contains |
|------------------------|----------------|------------------|
| **Demo Scenes** | `Samples~/Demo` | Module smoke scenes, integration examples, and `SurvivorDemo` ([guide](./Abilities/SurvivorDemo.md)). |
| **NeoxiderPages** | `Samples~/NeoxiderPages` | Optional page navigation: `PM`, `UIPage`, `BtnChangePage`, and UIKit helpers ([guide](./NeoxiderPages/README.md)). |
| **Character Movement Fundamentals** | `Samples~/CharacterMovementFundamentals` | Character-controller showcase, special movement scenes, and their supporting art/audio ([guide](./Tools/Move/CharacterController/README.md)). |

For the full integration collection, import **all three samples** before opening the legacy example
games. These scenes can reference assets in sibling samples: for example, `CardGame.unity` uses the PM
prefab from **NeoxiderPages**. Individual module demos such as InventoryUseDemo, AbilitiesNoCodeDemo and
AudioDemo do not require the legacy example-game setup. Pages remains optional for projects that do not
use page navigation or those integration scenes.

**По-русски:** для всей коллекции интеграционных сцен импортируйте все три сэмпла. В старых примерах
есть перекрёстные ссылки: например, CardGame использует PM из NeoxiderPages. Модульные демо инвентаря,
способностей и аудио можно изучать отдельно. При автоматической проверке ссылки проверяются после
импорта всей коллекции, без пропуска отсутствующих ассетов.

For example, import **Demo Scenes**, then open `Assets/Samples/NeoxiderTools/11.0.1/Demo Scenes/Scenes/Audio/AudioDemo.unity`. The paths in the smoke/integration tables below are relative to that **Demo Scenes** root, or to `Assets/Neoxider/Samples~/Demo/` in the source checkout. Replace `Samples~` with `Samples` when using the development layout.

Network demo scripts must compile without Mirror installed. Mirror-specific code in imported Demo Scenes is optional and must be wrapped with the same `#if MIRROR` / solo-mode fallback pattern used by `Neo.Network`.

These scenes are manual entry points and smoke scenarios for modules. Public C# contract tests cover a different scope; the scene tables describe intended checks rather than an exhaustive execution log. The observed manual subset and its limits are listed below.

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
| UI / Mesh Rig | `UI Mesh Rig/UIMeshRigDemo.unity` | shared geometry through uGUI, UI Toolkit, and world MeshFilter/MeshRenderer outputs |

## Gameplay And Integration Scenes

| Area | Scenes | Manual check |
|------|--------|--------------|
| Abilities | `Scenes/Abilities/AbilitiesNoCodeDemo.unity` | damage/heal results, cooldown rejection, level-scaled Zap, optional auto-cast and Reset |
| Inventory | `Scenes/Inventory/InventoryUseDemo.unity` | catch → nine-slot hotbar → exact-instance podium/recall; selected CELL consumption restores HP |
| Ai | `Scenes/Ai/AiMoveExample.unity` | AI movement example |
| Condition | `Scenes/Condition/NeoConditionDemo.unity` | condition rules demo |
| GridSystem | `Scenes/GridSystem/GridSystemMatch3Demo.unity`, `Scenes/GridSystem/GridSystemTicTacToeDemo.unity`, `Scenes/GridSystem/GridSystemDiceMergeDemo.unity` | board, rules, view binding |
| NoCode | `Scenes/NoCode/NoCodeActionsDemo.unity` | NoCode actions calling typed runtime APIs |
| Progression | `Scenes/Progression_Demo.unity` | XP, level growth, progress bar |
| Quest | `Scenes/Quests/QuestDemo.unity` | quest flow and scene reload |
| RPG | `Scenes/RpgCharacterQuickDemo.unity`, `Scenes/RpgCombatNpcDemo.unity` | `RpgCombatNpcDemo` must show HUD, auto combat, player damage, NPC death, and automatic wave restore |
| Bonus | `Scenes/Bonuses/SlotExample.unity`, `Scenes/Bonuses/WheelFortuneExample.unity` | slot/wheel runtime loop |
| Shop | `Scenes/Shop/Shop.unity` | typed shop API |
| Tools | `Scenes/Tools/**` | isolated runtime/editor wrappers |
| Example games | `Scenes/_ExampleGame/**`, `Scenes/VampireSurvivorMCP.unity`, `Scenes/SurvivorDemo.unity` | `VampireSurvivorMCP` must show HUD, spawner counters, player damage, death overlay, and reset |

## Recorded manual subset

Eleven scenes have been examined; this is not acceptance of every sample or every interaction. The
[verification record](../Verification11.md) retains the tested inputs and evidence.

| Scenario | Observed result / limit |
|---|---|
| Abilities | HP **80**, then **95**; Zap **12 / 30**. The corrected panel and filled HP display were observed. |
| Inventory | Deposit/place/recall retained the same item UID; one **E** press consumed one CELL and changed HP **45 → 70**. |
| Save | **100 → 110 → Load 100** observed. |
| Quest | Three kills awarded **25 gold**. |
| TicTacToe / Survivor | **X** win; survivor death and restart observed. |
| Audio | ON/OFF UI state observed; audible output was not established. |
| Dice | Tap rotation observed; OS-level dragging remains **INCONCLUSIVE**. |
| CardGame | Three OS-click rounds completed and the tutorial advanced. Piles at scale **0.7** fit Free Aspect and 16:9. Automated P31 scene clicks require exactly one round through the actual EventSystem top-graphic raycast and `ExecuteHierarchy`. |

По-русски: это проверенные сценарии ограниченного набора сцен, не приёмка всего каталога. В CardGame
завершены три раунда OS-кликами; слышимость аудио и OS-level drag в Dice не подтверждены.

## Automated Checks

`SampleAssetValidationTests` checks sample prefabs/scenes for missing scripts, missing MonoScript GUIDs, `NetworkBehaviour` components without `NetworkIdentity`, and broken `TerrainData.treePrototypes`. It resolves the active root from `Assets/Neoxider/Samples`, `Assets/Neoxider/Samples~`, or the imported root `Assets/Samples/NeoxiderTools`. The [testing guide](./Testing.md) describes scene ownership and isolation for these checks.

`SampleSceneCoverageTests` supports all three sample-root formats. Visible development/imported scenes can be inspected through disposable preview scenes; hidden `Samples~` content is checked as source/YAML because Unity does not compile its sample-only scripts. [PlayMode testing](./PlayModeTesting.md) explains which scene checks require visible compiled samples and which cases skip when they are absent. Keep pass/fail/skip separate; recorded imported-sample and manual scopes, source qualifications and limitations are listed in [Verification 11](../Verification11.md).
