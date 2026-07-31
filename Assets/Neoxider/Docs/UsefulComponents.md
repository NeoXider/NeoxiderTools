# Useful Neoxider Components (Add Component / GameObject)

**What it is:** Components that are convenient to add via **Add Component** (the **Neoxider/…** menu) or via **GameObject → Neoxider/…** when creating an object.

**How to use:** see the sections below.

---


Components that are convenient to add via **Add Component** (the **Neoxider/…** menu) or via **GameObject → Neoxider/…** when creating an object.

---

## How to Add

- **Add Component** → type `Neoxider` in the search field or pick the **Neoxider** entry in the category list.
- **GameObject** → **Neoxider** → submenus by category (UI, Bonus, Tools, Shop, Audio, etc.) — some entries create an object with the component and optionally a prefab.

The menu path everywhere is **Neoxider/** (not Neo).

---

## By Category (Add Component → Neoxider/…)

### Neoxider/UI
| Component | Summary |
|-----------|--------|
| **VisualToggle** | Toggles visibility (GameObject, Renderer, CanvasGroup, etc.) via events. |
| **ButtonPrice** | Displays a price, disables the button when currency is insufficient. For a points/stars indicator, see **Selector** (fill mode). |
| **UI** (Simple) | Simple container for buttons/pages. |
| **VariantView**, **AnchorMove** | Display variants, anchor-based offset. |
| **ButtonScale**, **ButtonShake** | Button press animations. |
| **FakeLoad** (UI) | Simulated loading for screens. |
| **ButtonChangePage** | Switches a page/panel on click. |
| **PausePage** | Pause page (in Tools). |

### Neoxider/Tools (subcategories in the Create Neoxider Object window)

In the **Create Neoxider Object** window and in menu paths, Tools is split into subfolders:

| Subcategory | Components |
|--------------|------------|
| **Physics** | ExplosiveForce, ImpulseZone, MagneticField, PhysicsEvents2D, PhysicsEvents3D |
| **Movement** | Follow, DistanceChecker, CameraConstraint, CameraRotationController, FreeFlyCameraController, CursorLockController, PlayerController2D/3D Physics, PlayerController2D/3D AnimatorDriver, ScreenPositioner, AdvancedForceApplier, MouseMover2D/3D, ConstantMover, ConstantRotator, KeyboardMover, UniversalRotator |
| **Spawner** | Spawner, SimpleSpawner, Despawner (removal/return to pool, spawn on despawn, OnDespawn) |
| **Components** | RpgStatsDamageableBridge, Counter, Loot, TextScore, TypewriterEffectComponent; *Health, Evade, AttackExecution, AdvancedAttackCollider — legacy, see [RPG](./Rpg/README.md) (`RpgCharacter`, `RpgAttackController`, `RpgEvadeController`)* |
| **Dialogue** | DialogueController, DialogueUI. Editor: a button in the inspector or **Neoxider → Windows → Dialogue Editor**. |
| **Inventory** | InventoryComponent, InventoryHand, InventoryDropper, PickableItem, HandView. Hand: Hand Anchor + Selector; HandView on the prefab — offsets and scale in the hand; use with E, drop with G via Dropper; hand scale — HandScaleMode (Relative by default). |
| **Input** | SwipeController, MultiKeyEventTrigger, MouseEffect |
| **View** | Selector, StarView, BillboardUniversal, LightAnimator, MeshEmission, ImageFillAmountAnimator, ZPositionAdjuster |
| **Debug** | FPS, ErrorLogger |
| **Time** | TimerObject |
| **Text** | SetText, TimeToText |
| **Interact** | InteractiveObject, ToggleObject |
| **Random** | ChanceSystemBehaviour |
| **Other** | AiNavigation, CameraShake, Drawer, RevertAmount, SpineController, UpdateChilds, PausePage |
| **State Machine** | StateMachineBehaviour, State Machine Behaviour |
| **FakeLeaderboard** | LeaderboardMove, LeaderboardItem |
| **Managers** | Bootstrap |
| **Camera** | CameraAspectRatioScaler |

### Neoxider/Shop
| Component | Summary |
|-----------|--------|
| **Money** | Currency singleton (storage, events). |
| **ShopItem**, **Shop** | Shop item and the shop itself. |
| **ButtonPrice** | Button with a price (in UI). |

### Neoxider/Audio
| Component | Summary |
|-----------|--------|
| **AM** (Audio Manager) | Scene audio management. |
| **AMSettings**, **SettingMixer** | Settings and mixer. |
| **PlayAudio**, **PlayAudioBtn** | Playback on event / on click. |
| **AudioControl** | UI volume control. |

### Neoxider/Bonus
| Component | Summary |
|-----------|--------|
| **CooldownReward**, **TimeReward** | Cooldown-based / time-based reward. |
| **WheelFortune**, **LineRoulett** | Wheel of fortune, line roulette. |
| **Box**, **ItemCollection**, **ItemCollectionInfo** | Collections and boxes. |
| **SpinController**, **Row**, **SlotElement** | Slot mechanics. |

### Neoxider/Level
| Component | Summary |
|-----------|--------|
| **SceneFlowController** | Scene loading (by id/name/fields), progress (text, Slider, Image), events, Quit/Restart/Pause/Proceed. |
| **LevelManager** | Level and map manager. |
| **LevelButton** | Level select/start button. |

### Neoxider/Save
| Component | Summary |
|-----------|--------|
| **SaveProviderSettingsComponent** | Save provider settings component. |
| **PlayerData** (Example) | Example player data. |

### Neoxider/Condition
| Component | Summary |
|-----------|--------|
| **NeoCondition** | No-Code conditions on component fields, AND/OR, events. |

### Neoxider/Animations
| Component | Summary |
|-----------|--------|
| **ColorAnimator**, **FloatAnimator**, **Vector3Animator** | Color, float, Vector3 animation. |

### Neoxider/GridSystem, Neoxider/Parallax, Neoxider/NPC
| Component | Summary |
|-----------|--------|
| **FieldGenerator**, **FieldSpawner**, **FieldObjectSpawner** | Grid generation and spawning. |
| **ParallaxLayer** | Parallax layer. |
| **NpcNavigation** | NPC navigation. |

---

## GameObject → Neoxider (Quick Creation)

**GameObject → Neoxider → Create Neoxider Object…** opens a list of components marked with the `[CreateFromMenu]` attribute. Selecting an entry creates an object with the component; if a prefab is specified and found in the package, the object is created from the prefab, otherwise an empty object with the component is created (fallback).

There is also a menu of ready-made prefab presets: **GameObject → Neoxider → Presets**.  
This section is convenient for quickly starting a scene with ready objects (e.g., **Simple Weapon**, **First Person Controller**, interactive prefabs) without manually searching in Project.

**Correspondence with paths and documentation:**
- The menu path (MenuPath) matches **Add Component** and the sections above: Neoxider/UI, Neoxider/Tools, Neoxider/Bonus, Neoxider/Shop, Neoxider/Audio, etc.
- Prefab paths in the package follow the script folder structure where possible (e.g., `Prefabs/UI/`, `Prefabs/Tools/`, `Prefabs/Bonus/`). When installed from Git or as a package, prefabs are loaded from the package root; if a prefab is not found, only an object with the component is created.

**Available entries:** the list is built via reflection from all types with the `[CreateFromMenu]` attribute. Top-level categories (UI, Tools, Bonus, Shop, Audio, etc.) are color-highlighted in the window for quick recognition. **Tools** has subfolders: Physics, Movement, Spawner, Components, Dialogue, Input, View, Debug, Time, Text, Interact, Random, Other, State Machine, FakeLeaderboard, Managers, Camera. An entry's path matches Add Component (e.g., Neoxider/Tools/Movement/PlayerController3DPhysics). Some entries have a prefab assigned — then an object is created from the prefab; otherwise an empty object with the component is created.

To add a new entry, put `[CreateFromMenu("Neoxider/Category/Name", PrefabPath = "Prefabs/...")]` on the class (PrefabPath is optional).
