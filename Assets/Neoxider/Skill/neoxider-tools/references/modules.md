# NeoxiderTools — module inventory, managers, attributes

Root namespace `Neo`; each module is `Neo.<Module>` with its own `.asmdef`. Verify signatures against
`Assets/Neoxider/Scripts/<Module>/` and the docs at `Assets/Neoxider/Docs/<Module>/` before using.

## Module inventory
| Module | Namespace | Purpose |
|---|---|---|
| Abilities | `Neo.Abilities` | **v10 combat core** — data-driven Dota-style abilities/modifiers (`AbilityDefinition`, `ModifierDefinition`, `UnitTemplate`, `AbilityLibrary`; `AbilitySystemBehaviour`/`AbilityUnitBehaviour`/`AbilityCasterBehaviour`). Supersedes Rpg — see abilities.md |
| Animations | `Neo.Animations` | `ColorAnimator`, `FloatAnimator`, `Vector3Animator` — value animators |
| Audio | `Neo.Audio` | `AM` (audio manager), `AMSettings`, `SettingMixer`, `PlayAudio`, `RandomMusicController` |
| Bonus | `Neo.Bonus` | Slot (`SpinController`, `Row`, `SlotElement`), roulette (`LineRoulett`, `WheelMoneyWin`), `Box`, `ItemCollection` |
| Cards | `Neo.Cards` | Deck/hand/poker eval, model–presenter card system |
| Condition | `Neo.Condition` | `NeoCondition` no-code evaluator — **avoid in code** (see avoid-nocode.md) |
| Core | `Neo.Core.*` | `HealthComponent`, resources, `Level/` loading helpers |
| Extensions | `Neo.Extensions` | static helper methods — see extensions.md |
| GridSystem | `Neo.GridSystem` | `FieldGenerator`, `FieldSpawner`, `GridPathfinder`, `GridSlotAllocator`, match3/merge/dice/tic-tac-toe |
| Level | `Neo.Level` | `LevelManager`, `LevelButton`, `SceneFlowController`, `Map` |
| Merge | `Neo.Merge` | merge mechanic (`GridMergeResolver` etc.) |
| Network | `Neo.Network` | Mirror-optional: `NetworkSingleton<T>`, `NeoNetworkManager`, lobby, `NetworkReactiveProperty` |
| NoCode | `Neo.NoCode` | inspector binding components — **avoid in code** |
| NPC | `Neo.NPC` | `NpcNavigation`, `NpcTargetFinder`, `NpcAnimatorDriver`, combat |
| Parallax | `Neo.Parallax` | `ParallaxLayer` 2D parallax |
| Progression | `Neo.Progression` | `ProgressionManager`, XP/perk nodes |
| PropertyAttribute | `Neo` (+ global inject) | inspector attributes — see below |
| Quest | `Neo.Quest` | `QuestManager`, `QuestConfig`, objectives |
| Reactive | `Neo.Reactive` | `ReactiveProperty<T>`, `ReactivePropertyFloat/Int/Bool` |
| Rpg | `Neo.Rpg` | **LEGACY** (superseded by Abilities in v10, slated for removal) — `RpgCharacter`, attack/projectile/contact-damage, stats, buffs. Maintain existing Rpg scenes only; never start new combat on it |
| Save | `Neo.Save` | `SaveManager`, `SaveableBehaviour`, `[SaveField]`, `SaveProvider` |
| Settings | `Neo.Settings` | `GameSettingsComponent`, `SettingsView`, graphics presets |
| Shop | `Neo.Shop` | `Money` (NetworkSingleton), `Shop`, `ShopItem`, `ButtonPrice` |
| StateMachine | `Neo.StateMachine` | generic `StateMachine<T>`, `IState`, `StateTransition`, `StateMachineBehaviourBase` |
| Tools | `Neo.Tools` | `Singleton<T>`, `GM`, `EM`, `PoolManager`/`NeoObjectPool`, `Spawner`, movement/camera/physics/debug helpers, `Counter`, `ScoreManager` |
| UI | `Neo.UI` | `AnimationFly` (fly-to-target), `AnchorMove`, `ButtonChangePage` |

## Singleton managers — access via `.I`
Base: `Singleton<T>` in `Neo.Tools` (`Scripts/Tools/Managers/Singleton.cs`). Members: `static T I`,
`static T Instance` (alias), `static bool HasInstance`, `static bool TryGetInstance(out T)`. Network ones use
`NetworkSingleton<T>` (`Neo.Network`).

| Class | Access | Module |
|---|---|---|
| `AM` | `AM.I` | Audio |
| `AMSettings` | `AMSettings.I` | Audio |
| `GM` | `GM.I` | Tools (game state) |
| `EM` | `EM.I` (+ static shortcuts like `EM.GameStart()`) | Tools (event hub) |
| `SaveManager` | `SaveManager.I` | Save (auto-loads on Awake) |
| `PoolManager` | `PoolManager.I` (or static `PoolManager.Get(...)`) | Tools |
| `LevelManager` | `LevelManager.I` | Level |
| `ScoreManager` | `ScoreManager.I` | Tools |
| `QuestManager` | `QuestManager.I` | Quest |
| `ProgressionManager` | `ProgressionManager.I` | Progression |
| `GameSettingsComponent` | `GameSettingsComponent.I` | Settings |
| `AnimationFly` | `AnimationFly.I` | UI |
| `InventoryComponent` | `InventoryComponent.I` | Tools.Inventory |
| `Money` | `Money.I` (NetworkSingleton) | Shop |
| `Bootstrap` | `Bootstrap.I` | Tools |
| `AbilitySystemBehaviour` | `AbilitySystemBehaviour.I` (auto-creates on first access) | Abilities |

`RpgCharacter` is intentionally NOT a singleton — many per scene; get it with `GetComponent<RpgCharacter>()`.

## Property attributes (high-value idioms)
Namespace `Neo` (the inject family is in the global namespace).

| Attribute | On | Effect |
|---|---|---|
| `[Button]` / `[Button("Label")]` / `[Button(width:200)]` | method | shows an inspector button; method params render as fields. **Works on ANY MonoBehaviour (global namespace too)** via the global fallback inspector `NeoCustomEditor` (`[CustomEditor(typeof(MonoBehaviour), true, isFallback=true)]`). Don't write a custom `Editor` for test/cheat buttons — it overrides the fallback and hides the `[Button]`s. |
| `[GUIColor(r,g,b,a?)]` | field | tints the field background |
| `[RequireInterface(typeof(IFoo))]` | Object/GameObject field | constrains reference to objects implementing the interface; stackable with `[FindInScene]` |
| `[GetComponent]` / `[GetComponent(true)]` | field | auto-assign from same GO / incl. children |
| `[GetComponents]` / `[GetComponents(true)]` | array/list field | auto-assign all matching |
| `[FindInScene]` | field | auto-find first matching component in scene |
| `[FindAllInScene]` / `[FindAllInScene(sortMode)]` | array/list field | auto-find all matching |
| `[LoadFromResources("path")]` | field | load asset from Resources |
| `[LoadAllFromResources("path")]` | array/list field | load all of type from Resources |
| `[NeoDoc("Module/File.md")]` | class | links to `Assets/Neoxider/Docs/Module/File.md` (your authoritative doc pointer) |

Inject attributes populate `[SerializeField]` references automatically in the editor (OnValidate/drawer),
removing boilerplate `GetComponent` calls in `Awake`.

## Component creation & editor menus
Package components carry `[AddComponentMenu("Neoxider/<Module>/<Name>")]` (Add Component search) and
`[CreateFromMenu("Neoxider/<Module>/<Name>")]` (GameObject create menu; may instantiate a prefab). When
adding a manager to a scene programmatically, prefer the existing component; access at runtime via `.I`.

Editor menus (v10) all live under one top-level **`Neoxider/`** menu: `Neoxider/Windows/...` (Ability
Designer, Dialogue Editor, Prefab To Sprite, Create Neoxider Object), `Neoxider/Tools/...` (Scene Saver,
Texture Max Size, Save Project Zip, missing-script repair, Fix Editor Assembly References), plus Network,
Samples, Settings, `Neoxider/Visual Settings`, and `Neoxider/Health Check`. Hierarchy right-click:
`GameObject/Neoxider/Create Neoxider Object...`, `GameObject/Neoxider/Presets/...` (System Root, First
Person Controller, Simple Weapon, Bullet, Interactive Sphere, Toggle Interactive, Trigger Cube), and
Create/Sort Scene Hierarchy.

## Dependencies & setup
- Unity 6 (`6000.0`)+. Bundled deps: `com.unity.ai.navigation` 1.1.7, `com.unity.inputsystem` 1.14.2,
  `com.unity.ugui` 1.0.0 (TMP ships inside ugui on Unity 6 — no separate textmeshpro dependency).
- Optional (NOT bundled; gate features): **Mirror** (`#if MIRROR` → `NetworkSingleton`, `Money` net,
  `NeoNetworkManager`, lobby, `NetworkReactiveProperty`); **DOTween** (tween paths in animation modules);
  **Spine** (`SpineController`); **Odin** (some editor drawers). All Mirror code is `#if MIRROR`-guarded; the
  package compiles without it.
- Install: `"com.neoxider.tools": "https://github.com/NeoXider/NeoxiderTools.git"` in `Packages/manifest.json`.
- Samples: `Samples~/Demo` (SurvivorDemo + demo-shell scenes + Condition/Tools/GridSystem/Shop demos),
  `Samples~/NeoxiderPages` (`PM`, `UIPage`, `BtnChangePage`).
