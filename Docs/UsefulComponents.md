# Useful Neoxider Components (Add Component / GameObject)

**What it is:** a catalog of the scene components the package registers under **Add Component → Neoxider** and in the **Create Neoxider Object** window, grouped by category, with the module page to read next.

**How to use:**
1. **Add Component** → type `Neoxider` in the search field, or open the **Neoxider** category. The menu path everywhere is **Neoxider/** (not Neo).
2. Or open **GameObject → Neoxider → Create Neoxider Object...** (also **Neoxider → Windows → Create Neoxider Object**) to create an object with the component, from a prefab when one is assigned.
3. Every component has a documentation link in its Inspector header (`[NeoDoc]`); the tables below link the module entry pages.

**Navigation:** [← Docs](./README.md)

---

## Gameplay modules

| Category | Components | Docs |
|----------|------------|------|
| **Neoxider/Abilities** | Ability System, Ability Unit, Ability Caster, Ability Auto Caster, Ability Cooldown Source, Ability Projectile, Ability NoCode Action | [Abilities](./Abilities/README.md) |
| **Neoxider/Core** | Health Component, Level Component, Level Condition Adapter, Level NoCode Action | [Core](./Core/README.md) |
| **Neoxider/RPG** | RpgCharacter, RpgAttackController, RpgAutoAttackController, RpgEvadeController, RpgProjectile, RpgTargetSelector, RpgContactDamage, RpgDeathHandler, RpgResourceBinding, RpgStatBinding, RpgConditionAdapter, RpgNoCodeAction, RpgStatsDamageableBridge; **RPG/Network**: Rpg Character / Rpg Attack Network Adapter | [Rpg](./Rpg/README.md) |
| **Neoxider/Progression** | ProgressionManager, ProgressionNoCodeAction, ProgressionConditionAdapter | [Progression](./Progression/README.md) |
| **Neoxider/Quest** | QuestManager, QuestNoCodeAction, QuestContext | [Quest](./Quest/README.md) |
| **Neoxider/Shop** | Money, Shop, ShopItem, ShopListView, ShopListViewCategoryBar, ShopCategoryButton, ShopCategorySelector, ShopPurchaseButtonView, ShopVariantStateView, ShopVariantsPanel, EquipmentManager | [Shop](./Shop/README.md) |
| **Neoxider/Bonus** | CooldownReward, ResourceRegen, WheelFortune, WheelMoneyWin, LineRoulett, SpinController, Row, SlotElement, Collection, CollectionVisualManager, Box, ItemCollection, ItemCollectionInfo | [Bonus](./Bonus/README.md) |
| **Neoxider/Cards** | DeckComponent, HandComponent, BoardComponent, CardComponent, CardViewUniversal, DeckView, HandView, DrunkardGame | [Cards](./Cards/README.md) |
| **Neoxider/GridSystem** | FieldGenerator, FieldSpawner, FieldObjectSpawner, FieldDebugDrawer, GridCellMarker, GridGameBuilder; **Dice** DiceBoardService; **Match3** Match3BoardService; **SlidingMerge** SlidingMergeBoardService; **TicTacToe** TicTacToeBoardService | [GridSystem](./GridSystem/README.md) |
| **Neoxider/NPC** | NpcNavigation, NpcTargetFinder, NpcAnimatorDriver; **NPC/Combat** NpcRpgCombatBrain | [NPC](./NPC/README.md) |
| **Neoxider/Level** | LevelManager, LevelButton, TextLevel, SceneFlowController (scene loading with progress, Quit/Restart/Pause/Proceed) | [Level](./Level/README.md) |
| **Neoxider/Save** | SaveManager, SaveProviderSettingsComponent, PlayerData (example) | [Save](./Save/README.md) |
| **Neoxider/Settings** | GameSettingsComponent, SettingsView | [Settings](./Settings/README.md) |

## Logic, NoCode and presentation

| Category | Components | Docs |
|----------|------------|------|
| **Neoxider/Condition** | NeoCondition (no-code checks on component fields, AND/OR, events) | [Condition](./Condition/README.md) |
| **Neoxider/NoCode** | NoCodeBindText, NoCodeFormattedText, SetProgress | [NoCode](./NoCode/README.md) |
| **Neoxider/UI** | VisualToggle, ButtonPrice, ButtonScale, ButtonShake, ButtonChangePage, CategoryBar, CategoryBarItem, VariantView, AnchorMove, FakeLoad, UI (simple page container), AnimationFly, UI Mesh Rig (Graphic, Sprite Renderer, UI Toolkit Host, World Renderer, Mesh Rig Point, Mesh Rig Point Motion) | [UI](./UI/README.md) |
| **Neoxider/Animations** | FloatAnimator, ColorAnimator, Vector3Animator, TransformAnimator | [Animations](./Animations/README.md) |
| **Neoxider/Audio** | AM (audio manager), AMSettings, AudioControl, MusicControl, PlayAudio, PlayAudioBtn, SettingMixer, Audio Focus Mute | [Audio](./Audio/README.md) |
| **Neoxider/Parallax** | ParallaxLayer | [Parallax](./Parallax/README.md) |
| **Neoxider/Network** | NeoNetworkManager, NeoNetworkBootstrap, NeoNetworkPlayer, NetworkPlayerName, Neo Network Discovery, NeoNetworkTelemetry, Network Action Relay, Network Context Action Relay, Network Property Sync, Network Reactive Sync, Network Owner Filter, Neo Lobby Manager / Player / Presenter | [Network](./Network/README.md) |

## Tools

In **Add Component**, most Tools components sit directly under **Neoxider/Tools**; Physics, Dialogue and Inventory have their own submenus. The **Create Neoxider Object** window groups them into subfolders:

| Create window folder | Components | Docs |
|----------------------|------------|------|
| **Movement** | Character Controller, PlayerController2D/3D Physics, PlayerController2D/3D AnimatorDriver, Follow, DistanceChecker, CameraConstraint, CameraRotationController, FreeFlyCameraController, CursorLockController, ScreenPositioner, AdvancedForceApplier, MouseMover2D/3D, ConstantMover, ConstantRotator, KeyboardMover, UniversalRotator | [Move](./Tools/Move/README.md) |
| **Physics** | ExplosiveForce, ImpulseZone, MagneticField, PhysicsEvents2D, PhysicsEvents3D | [Physics](./Tools/Physics/README.md) |
| **Spawner** | PoolManager, Spawner, SimpleSpawner (plus Despawner at the Tools root) | [Spawner](./Tools/Spawner/README.md) |
| **Components** | Counter, ModifyCounterByKey, ScoreManager, TextScore, Loot, RandomRange, TypewriterEffectComponent, AnimatorParameterDriver, UnityLifecycleEvents; legacy Health, Evade, AttackExecution, AdvancedAttackCollider (use [RPG](./Rpg/README.md) or [Abilities](./Abilities/README.md) instead) | [Components](./Tools/Components/README.md) |
| **Dialogue** | DialogueController, DialogueUI, DialogueLinePlayer (bark lines). Editor: **Neoxider → Windows → Dialogue Editor** | [Dialogue](./Tools/Dialogue/README.md) |
| **Inventory** | InventoryComponent, InventoryHand, InventoryDropper, PickableItem, HandView, InventoryView, InventorySlotGridView, InventorySlotView, InventoryItemView, InventoryItemCountText, InventoryTotalCountText, InventoryPickupBridge, InventoryUseAction, ShopInventoryGrantBridge | [Inventory](./Tools/Inventory/README.md) |
| **Input** | MouseInputManager, MouseEffect, SwipeController, MultiKeyEventTrigger | [Input](./Tools/Input/README.md) |
| **Interact** | InteractiveObject, ToggleObject (plus InteractionRayProvider at the Tools root) | [Interaction](./Tools/InteractableObject/README.md) |
| **View** | Selector, SelectorItem, StarView, BillboardUniversal, LightAnimator, MeshEmission, ImageFillAmountAnimator, ZPositionAdjuster, SpriteFlash | [View](./Tools/View/README.md) |
| **Time** | TimerObject (plus GameTimeController at the Tools root) | [Time](./Tools/Time/README.md) |
| **Text** | SetText, TimeToText | [Text](./Tools/Text/README.md) |
| **Random** | ChanceSystemBehaviour | [Random](./Tools/Random/README.md) |
| **Debug** | FPS, ErrorLogger, NeoDebugOverlay | [Debug](./Tools/Debug/README.md) |
| **Managers** | Bootstrap, GM, EM | [Managers](./Tools/Managers/README.md) |
| **Camera** | CameraAspectRatioScaler | [CameraAspectRatioScaler](./Tools/CameraAspectRatioScaler.md) |
| **Other** | CameraShake, Drawer, PausePage, RevertAmount, SpineController, UpdateChilds, AiNavigation (legacy, use NpcNavigation) | [Other](./Tools/Other/README.md) |
| **State Machine** | StateMachineBehaviour (generic C# states), State Machine Behaviour (NoCode `StateMachineData`) | [StateMachine](./StateMachine/README.md) |
| **FakeLeaderboard** | Leaderboard, LeaderboardMove, LeaderboardItem | [FakeLeaderboard](./Tools/FakeLeaderboard/README.md) |
| **Network** | Network Event Dispatcher | [NetworkEventDispatcher](./Tools/Network/NetworkEventDispatcher.md) |

The NeoxiderPages sample adds **Neoxider/Pages** (PM, UIPage, BtnChangePage, PageSubscriber, FakeLoad, ToggleAudio); see [NeoxiderPages](./NeoxiderPages/README.md).

---

## GameObject → Neoxider (Quick Creation)

**GameObject → Neoxider → Create Neoxider Object...** opens a list of components marked with the `[CreateFromMenu]` attribute. Selecting an entry creates an object with the component; if a prefab is specified and found in the package, the object is created from the prefab, otherwise an empty object with the component is created (fallback).

There is also a menu of ready-made prefab presets: **GameObject → Neoxider → Presets** (System Root, character controllers for first/third person, top-down, side-scroller and click-to-move, Simple Weapon, Bullet, Moving Platform, Interactive Sphere, Toggle Interactive, Trigger Cube). It is convenient for starting a scene with ready objects without searching the Project window.

**Correspondence with paths and documentation:**
- The window's top-level categories (UI, Tools, Bonus, Shop, Audio, ...) are color-highlighted; Tools has the subfolders listed above.
- Prefab paths in the package follow the script folder structure where possible (e.g., `Prefabs/UI/`, `Prefabs/Tools/`, `Prefabs/Bonus/`). When installed from Git or as a package, prefabs are loaded from the package root; if a prefab is not found, only an object with the component is created.
- The list is built via reflection from all types with the `[CreateFromMenu]` attribute, so entries from imported samples and optional modules appear when they are present.

To add a new entry, put `[CreateFromMenu("Neoxider/Category/Name", PrefabPath = "Prefabs/...")]` on the class (PrefabPath is optional).
