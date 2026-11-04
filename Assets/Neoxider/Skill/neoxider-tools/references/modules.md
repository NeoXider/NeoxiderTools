# NeoxiderTools — module inventory, managers, attributes

Root namespace `Neo`; each module is `Neo.<Module>` with its own `.asmdef`. Verify signatures against
`Assets/Neoxider/Scripts/<Module>/` and the docs at `Assets/Neoxider/Docs/<Module>/` before using.

## Module inventory
| Module | Namespace | Purpose |
|---|---|---|
| Abilities | `Neo.Abilities` | **v10 combat core** — data-driven Dota-style abilities/modifiers (`AbilityDefinition`, `ModifierDefinition`, `UnitTemplate`, `AbilityLibrary`; `AbilitySystemBehaviour`/`AbilityUnitBehaviour`/`AbilityCasterBehaviour`). Supersedes Rpg — see abilities.md |
| Animations | `Neo.Animations` | `ColorAnimator`, `FloatAnimator`, `Vector3Animator`, `TransformAnimator` — animators with a manual clock (`ManualUpdate` + `Tick(dt)`); `AnimationUtils.ApplyToRenderer` animates emission through a `MaterialPropertyBlock` |
| Audio | `Neo.Audio` | `AM` (audio manager; music crossfade, ambience layer `PlayAmbience`/`AmbienceVolume` since 10.16), `AMSettings` (persists volumes and Music/Efx/Master/UI mutes — `PersistVolume`/`PersistMute`; a `SetMusic`/`SetEfx` call in a consumer's `Awake` is kept, binding no longer forces sound on), `SettingMixer`, `PlayAudio`, `RandomMusicController` |
| Haptics | `Neo.Haptics` | `Haptics.Play(HapticType)` — nine named feels, master switch `EnabledProvider`, repeat throttle; real iOS/Android feels with optional `com.tsyk5.mobilehapticfeedback` (`NEO_MOBILE_HAPTICS`), coarse `Handheld.Vibrate` fallback without it (10.15+) |
| Bonus | `Neo.Bonus` | Slot (`SpinController`, `Row`, `SlotElement`), roulette (`LineRoulett`, `WheelMoneyWin`), `Box`, `ItemCollection`, `Collection`; plain C# cores `CollectionSet`, `BoxProgress`, `CooldownSchedule`, `SlotSpinMath` |
| Cards | `Neo.Cards` | Deck/hand/poker eval, model–presenter card system; seeded shuffles (`DeckModel(seed)`), stable custom sorting (`SortAsync(comparer)`, `CardComparer.ByValue*`), allocation-free `PokerHandEvaluator.EvaluateStrength` |
| Condition | `Neo.Condition` | `NeoCondition` no-code evaluator — **avoid in code** (see avoid-nocode.md); typed `ConditionEntry.TryEvaluate`, `NeoCondition.UseUnscaledTime` |
| Core | `Neo.Core.*` | `HealthComponent`, resources, `Level/` loading helpers |
| Extensions | `Neo.Extensions` | static helper methods — see extensions.md |
| GridSystem | `Neo.GridSystem` | `FieldGenerator`, `FieldSpawner`, `GridPathfinder` (allocation-free `TryFindPath`, `CollectReachableCells`), `GridSlotAllocator`, `GridShapeMask`, match3/merge/dice/tic-tac-toe (n-in-a-row)/sliding-merge (2048) |
| Level | `Neo.Level` | `LevelManager`, `LevelButton`, `SceneFlowController`, `Map` |
| Merge | `Neo.Merge` | merge mechanic (`GridMergeResolver` etc.) |
| Network | `Neo.Network` | Mirror-optional: `NetworkSingleton<T>`, `NeoNetworkManager`, lobby, `NetworkReactivePropertyBridge`; realtime .io toolkit `Neo.Network.Realtime` (`SnapshotTimeline`, `SnapshotBuffer<T>`, `LocalPredictionModel`, `NetEventChannel<T>`, ...), `NeoNetworkBootstrap` (CLI), `NeoNetworkTelemetry`; transport-free `NetworkAuthorityPolicy` (assembly `Neo.Network.Contracts`) |
| NoCode | `Neo.NoCode` | inspector binding components — **avoid in code**; each has a typed C# surface (`RefreshBinding`, `UpdateMode`, `PollIntervalSeconds`, `SetValues`, `SetRange`, `ForceRewrite`) |
| NPC | `Neo.NPC` | `NpcNavigation`, `NpcTargetFinder`, `NpcAnimatorDriver`, combat |
| Parallax | `Neo.Parallax` | `ParallaxLayer` 2D parallax |
| Progression | `Neo.Progression` | `ProgressionManager`, XP/perk nodes, premium track; C# events `NodeUnlocked`/`PerkPurchased`/`PremiumActivated` |
| PropertyAttribute | `Neo` (+ global inject) | inspector attributes — see below |
| Quest | `Neo.Quest` | `QuestManager` (`Notify(type, targetId, amount)`, `NotifyKill/Collect/ReachPoint/Talk`, `AddObjectiveProgress`, `TryAcceptQuest`, C# events), `QuestConfig` (`ValidateDefinition()`), objectives |
| Reactive | `Neo.Reactive` | `ReactiveProperty<T>`, `ReactivePropertyFloat/Int/Bool` |
| Rpg | `Neo.Rpg` | **LEGACY** (superseded by Abilities in v10, slated for removal) — local/offline `RpgCharacter`, attack/projectile/contact-damage, stats, buffs; plain-C# state lives in `RpgCharacterResourceService`, `RpgCharacterStatService` (stats, upgrades), `RpgCharacterProgressionService` (level, XP, points) and `RpgModifierMath` (multipliers); extend damage with `IRpgDamageModifier` (`AddDamageModifier`) and observe it with `ApplyDamage(RpgDamageInfo)` / `DamageApplied`. The complete base dependency closure is free of Mirror/`Neo.Network` implementations. `RpgConditionAdapter` lives in `Neo.Rpg.ConditionBridge`; optional Mirror transport lives in `Neo.Rpg.Network`. Add those assembly references only when used. `RpgNoCodeAction` remains in the base assembly for serialized UnityEvent compatibility. Maintain existing Rpg scenes only; never start new combat on it |
| Save | `Neo.Save` | `SaveManager`, `SaveableBehaviour`, `[SaveField]`, `SaveProvider` (`GetLong`/`SetLong` for 64-bit values; `FileSaveProvider` skips unchanged flushes, `HasUnsavedChanges`) |
| Settings | `Neo.Settings` | `GameSettingsComponent` (`HasPendingDeferredSave`), `SettingsView`, graphics presets |
| Shop | `Neo.Shop` | `Money` (NetworkSingleton), `Shop` (typed `TryBuy`/`TryBuyBundle` → `ShopPurchaseStatus`), `ShopItem`, `ButtonPrice`; optional Inventory integration lives in `Neo.Shop.Bridges` |
| StateMachine | `Neo.StateMachine` | generic `StateMachine<T>`, `IState`, `StateTransition`, `StateMachineBehaviour<TState>` (code wrapper), `StateMachineBehaviourBase` (NoCode `StateMachineData` wrapper); allocation-free ticks |
| Tools | `Neo.Tools` | `Singleton<T>`, `GM`, `EM`, `PoolManager`/`NeoObjectPool`, `Spawner`, timers and clock seams (`IFrameClock`, `INeoClock`), `IntegerChanceTable`, `DialogueLinePool`, movement/camera/physics/debug helpers, `Counter`, `ScoreManager` |
| UI | `Neo.UI` | `AnimationFly` (fly-to-target), `AnchorMove`, `ButtonChangePage`, `CategoryBar`, `VariantView`, `ButtonScale`/`ButtonShake`/`FakeLoad` (unscaled time by default), UI Mesh Rig |

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
| `[Button]` / `[Button("Label")]` / `[Button(width:200)]` | method | shows an inspector button; method params render as fields. Add `PlayModeOnly = true` to runtime or state-mutating actions; omit it only for edit-safe authoring/preview operations. Runs on every selected object (static methods once) and records Undo in Edit Mode. **Works on ANY MonoBehaviour (global namespace too)** via the global fallback inspector `NeoCustomEditor` (`[CustomEditor(typeof(MonoBehaviour), true, isFallback=true)]`). Don't write a custom `Editor` for test/cheat buttons — it overrides the fallback and hides the `[Button]`s. |
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
removing boilerplate `GetComponent` calls in `Awake`. Assignments are recorded with Undo (prefab overrides
keep them), and an unresolved field warns once per object and field.

## Component creation & editor menus
Package components carry `[AddComponentMenu("Neoxider/<Module>/<Name>")]` (Add Component search) and
`[CreateFromMenu("Neoxider/<Module>/<Name>")]` (GameObject create menu; may instantiate a prefab). When
adding a manager to a scene programmatically, prefer the existing component; access at runtime via `.I`.

Editor menus (v10) all live under one top-level **`Neoxider/`** menu: `Neoxider/Windows/...` (Ability
Designer, Dialogue Editor, Prefab To Sprite, Create Neoxider Object), `Neoxider/Tools/...` (Scene Saver,
Texture Max Size, Save Project Zip, missing-script repair, Fix Editor Assembly References), plus Network,
Samples, Settings, `Neoxider/Visual Settings`, and `Neoxider/Health Check`. `Visual Settings` has an
Animation section (`Animate inspector` master switch, `Animate in Play Mode`, `Max FPS`; also
`CustomEditorSettings.EnableAnimations` / `AnimateInPlayMode` / `AnimationFps`): tell a user whose editor feels
slow to switch inspector animation off. In a custom `CustomEditorBase` editor use `EnsureRepaint()` for decoration
only and `EnsureLiveRepaint()` for data that must stay current, and never call id-taking IMGUI
(`GUI.BeginGroup/Box/Button`, `EditorGUI.LabelField`, `GUILayout.*`) inside an
`Event.current.type == EventType.Repaint` branch - it shifts the control ids after it and breaks presses and text selection. Hierarchy right-click:
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
- Samples: `Samples~/Demo` (SurvivorDemo + one builder-authored demo scene per module + example games),
  `Samples~/NeoxiderPages` (`PM`, `UIPage`, `BtnChangePage`).
- Not in this package: the HTML → UI Toolkit converter moved to `com.neoxider.htmltouitk`
  (https://github.com/NeoXider/HtmlToUitk.git).
