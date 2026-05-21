# Neoxider — a powerful toolkit collection for Unity

![Neoxider Cover](Images/neoxider_cover_cosmic.png)

[🇷🇺 Русский](README_RU.md) | [🇺🇸 English](README.md)

[![Version](https://img.shields.io/badge/version-8.4.1-blue)]() [![Unity](https://img.shields.io/badge/Unity-2022.1+-green)]() [![Namespace](https://img.shields.io/badge/namespace-Neo-orange)]()

> **EN:** Ready-to-use Unity tools that integrate easily into your project. 150+ modules for fast game development without unnecessary complexity.
>
> **RU:** Готовые решения для Unity, которые легко интегрируются в ваш проект. Более 150 модулей для быстрой разработки игр без лишних сложностей.

**Neoxider** is an ecosystem of ready-to-use Unity tools, built by developers for developers. Easy to configure through the Inspector, no deep code diving required, yet fully transparent and extensible. Perfect for prototyping and production projects.

**Neoxider** — экосистема готовых инструментов для Unity, созданная разработчиками для разработчиков. Легко настраивается через Inspector, не требует глубокого погружения в код, но остаётся полностью прозрачной и расширяемой.

📖 **[English docs →](Assets/Neoxider/DocsEn/README.md)** · 📖 **[Полная документация (RU) →](Assets/Neoxider/Docs/README.md)** · 📌 **[PROJECT_SUMMARY →](Assets/Neoxider/PROJECT_SUMMARY.md)** · 📝 **[Changelog →](Assets/Neoxider/CHANGELOG.md)**

**Documentation (EN):** [DocsEn/README.md](Assets/Neoxider/DocsEn/README.md) — English entry point for top-level modules and key pages; when a deep page is not yet translated, the index links to the corresponding Russian section. **Documentation (RU):** [Docs/README.md](Assets/Neoxider/Docs/README.md) — canonical index of all modules. Tools submodules and samples (NeoxiderPages, UI Extension) are included in both indexes.

**Multiplayer:** the **Neo.Network** module — a wrapper around **Mirror** (optional build). Guide: [Multiplayer_Guide.md](Assets/Neoxider/Docs/Network/Multiplayer_Guide.md) · NoCode rules: [NoCode_Network_Spec.md](Assets/Neoxider/Docs/Network/NoCode_Network_Spec.md).

---

## 📑 Table of Contents

- [No-Code conditions — NeoCondition](#no-code-conditions--neocondition)
- [What makes Neoxider special](#what-makes-neoxider-special)
- [Demo Scenes](#demo-scenes)
- [Games built with NeoxiderTools](#games-built-with-neoxidertools) — games powered by the ecosystem
- [Demo Games](#demo-games)
- [Quick Start](#quick-start)
- [Multiplayer Quick Start](#multiplayer-quick-start)
- [Module Table](#module-table)
  - [Condition](#condition--no-code-conditions) · [Tools](#tools--tools) · [UI](#ui--interface) · [Bonus](#bonus--bonus-systems) · [Shop](#shop--store) · [Save](#save--saves) · [Quest](#quest--quests) · [Cards](#cards--card-games) · [StateMachine](#statemachine--state-machine) · [Animations](#animations--animations) · [Audio](#audio--sound) · [Extensions](#extensions--c-extensions) · [Editor](#editor--editor-tools) · [Level](#level--levels) · [NPC](#npc) · [Parallax](#parallax) · [GridSystem](#gridsystem) · [PropertyAttribute](#propertyattribute) · [Reactive](#reactive)
- [Top Modules](#top-modules)
- [Installation via UPM](#installation-via-upm) — [Dependencies](#dependencies), [Main package](#main-package), [Manual installation](#manual-installation)
- [Installing Demo Scenes and NeoxiderPages](#installing-demo-scenes-and-neoxiderpages)
- [FAQ](#faq)
- [Support and contribution](#support-and-contribution)

---

## No-Code conditions — NeoCondition

Design complex game logic **without writing a single line of code**. The `NeoCondition` component lets you, directly in the Inspector:

- **Check any data** — HP, score, object state, any public field or property of any component
- **Combine conditions** — AND/OR logic, inversion (NOT), multiple checks in one component
- **React to changes** — `OnTrue`, `OnFalse`, `OnResult` events connect to any objects via UnityEvent
- **Inspect GameObject properties** — `activeSelf`, `tag`, `layer` and others — no extra components needed
- **Work with future objects** — find objects by name, set up conditions for prefabs before they spawn via Prefab Preview
- **Pick a check mode** — Interval, EveryFrame, Manual; the Only On Change filter excludes redundant firings

> **Example:** "When `Health.Hp <= 0` — show Game Over" — one setup in the Inspector, zero lines in code.

📖 [NeoCondition documentation →](Assets/Neoxider/Docs/Condition/NeoCondition.md)

---

## What makes Neoxider special

- **Production-ready** — every subsystem ships with examples, documentation, and thoughtful integrations
- **No-Code where it counts** — most components are configured through the Inspector and UnityEvent, yet remain extensible
- **Hybrid approach** — No-Code + Code for maximum flexibility
- **Modularity** — isolation via Assembly Definition Files, import only the modules you need
- **Extensibility** — inheritance, interfaces, public API on every component
- **Automatic saving** — powerful save attribute module; many scripts persist data automatically
- **Inline documentation** — every module has its own README in `Assets/Neoxider/Docs/`

> Pay special attention to the **Extensions** module if you love writing code — 300+ extension methods for C# and the Unity API.
> Many scripts also support code-driven workflows: Singleton, ChanceSystem, Timer, and more.

---

<img width="464" height="522" alt="image" src="https://github.com/user-attachments/assets/fbb02b88-fed6-4445-bf19-079382966628" />

## Demo Scenes
![image](https://github.com/user-attachments/assets/90c98f0c-aae2-4837-81ed-b18a10b65ed5)

## Games built with NeoxiderTools

> **EN:** Shipping titles and jams that build on **NeoxiderTools** (including inspector-driven workflows).
> **RU:** Реальные релизы и демо, где **NeoxiderTools** — основа геймплея (в т.ч. no-code в Inspector).

| Game · Игра | Genres · Жанры | Platform | Link · Ссылка | Notes · Примечание |
|-------------|----------------|----------|---------------|-------------------|
| [**Fake Grandkids (Внуки понарошку: пенсия прилагается)**](https://myindie.ru/games/game/fake-grandkids) | Arcade, Survival | Windows | [MyIndie — store page · страница](https://myindie.ru/games/game/fake-grandkids) | RU; **UralGameJam 2026**; logic via NeoCondition and the NeoxiderTools ecosystem · v7.8.0 |

## Demo Games
<img width="354" height="623" alt="2025-11-02_22-31-20" src="https://github.com/user-attachments/assets/56c255c1-5e96-410c-b212-ea865ea4521f" />
<img width="372" height="623" alt="image" src="https://github.com/user-attachments/assets/6d16edff-dd20-47bb-90f1-c3fc0e913d68" />
<img width="345" height="703" alt="image" src="https://github.com/user-attachments/assets/2c45a361-201b-499f-b77f-c90b3f02c757" />

---

## Quick Start

1. **Install dependencies** — Unity 2022+ (recommended)
2. **Import** the `Assets/Neoxider` folder into your project (or via [UPM](#installation-via-upm))
3. **Add the system prefab** `Prefabs/--System--.prefab` to the scene — event and UI managers
4. **Drag components** from the Inspector — most work without code via UnityEvent
5. **Read the docs** — open the README in `Docs/` for the module you need

## Multiplayer Quick Start

1. Install **Mirror** in the project (see [Mirror](https://github.com/MirrorNetworking/Mirror) / Package Manager).
2. Add **`NeoNetworkManager`** and a Mirror transport (e.g. **Telepathy**) to the scene.
3. For NoCode, keep the player configured directly in the scene: add a `NetworkIdentity`, enable **Use Scene Player Template**, assign the player in **Scene Player Template**, and leave **Player Prefab** empty.
4. Start a session from UI or code: `NeoNetworkManager.Singleton.StartHost()` / `StartClient()` (details in the guide).
5. For NoCode components, enable **`isNetworked`** wherever replication is required, and follow **[Multiplayer_Guide.md](Assets/Neoxider/Docs/Network/Multiplayer_Guide.md)** and **[NoCode_Network_Spec.md](Assets/Neoxider/Docs/Network/NoCode_Network_Spec.md)**.

## Tests

- Baseline `EditMode` tests live in `Assets/Neoxider/Editor/Tests/`.
- They cover critical scenarios for `Save`, `Level`, `Bootstrap`, and legacy/editor behaviors.
- To run inside Unity, use `Test Runner` or the `com.unity.test-framework` package.

---

## Module Table

| Module | Description |
|--------|-------------|
| ⚙️ [**Condition**](#condition--no-code-conditions) | No-Code conditions: field checks, AND/OR logic, events |
| 🛠️ [**Tools**](#tools--tools) | 150+ components: movement, physics, spawners, timers, input |
| 🖼️ [**UI**](#ui--interface) | UI panels, button animations, toggles |
| 🎁 [**Bonus**](#bonus--bonus-systems) | Slots, wheel of fortune, collections, time-based rewards |
| 🛒 [**Shop**](#shop--store) | Shop, currency, purchases |
| 💾 [**Save**](#save--saves) | PlayerPrefs, JSON files, `[SaveField]` attribute |
| 📜 [**Quest**](#quest--quests) | Quest configs, manager, objectives, runtime state |
| 📈 [**Progression**](Assets/Neoxider/Docs/Progression/README.md) | XP, levels, unlock tree, perk tree, and persistent progression |
| 🃏 [**Cards**](#cards--card-games) | MVP architecture, Poker, "Drunkard" |
| 🤖 [**StateMachine**](#statemachine--state-machine) | Code + No-Code, visual editor |
| ✨ [**Animations**](#animations--animations) | Float, Color, Vector3 animations |
| 🎵 [**Audio**](#audio--sound) | AudioManager, mixer, random music |
| 🔌 [**Extensions**](#extensions--c-extensions) | 300+ extension methods |
| 🛠️ [**Editor**](#editor--editor-tools) | Settings windows, missing-script finder, auto-build |
| 🗺️ [**Level**](#level--levels) | Level manager, level map |
| 🚶 [**NPC**](#npc) | NPC navigation, patrol, chase, and animator driver |
| 🌌 [**Parallax**](#parallax) | Parallax layers |
| 🔲 [**GridSystem**](#gridsystem) | Grid generation, origin anchor, pathfinding, Match3/TicTacToe |
| 🏷️ [**PropertyAttribute**](#propertyattribute) | `[Button]`, `[GUIColor]`, inject attributes |
| ⚡ [**Reactive**](#reactive) | Reactive serializable `float`, `int`, `bool` properties |
| 🌐 [**Network**](#network--multiplayer) | Multiplayer on Mirror: `NeoNetworkManager`, NoCode sync (`NetworkPropertySync`, `NetworkActionRelay`, **`NetworkContextActionRelay`**), lobby/discovery |

---

## Modules

### Condition — No-Code conditions

- **NeoCondition** — check any fields/properties of components and GameObjects via the Inspector
- **AND/OR logic**, inversion (NOT), multiple conditions in one component
- **Source Mode** — read data from components or from properties of the GameObject itself (`activeSelf`, `tag`, `layer`)
- **Find By Name** — locate scene objects by name with caching
- **Wait For Object + Prefab Preview** — configure conditions for prefabs before they spawn
- Events: `OnTrue`, `OnFalse`, `OnResult(bool)`, `OnInvertedResult(bool)`

📖 [Documentation →](Assets/Neoxider/Docs/Condition/NeoCondition.md)

### Tools — Tools

The largest category — the basic "bricks" for building games:

| Submodule | Components |
|-----------|------------|
| **Components** | Counter, Health, ScoreManager, DialogueManager, Loot, TypewriterEffect, AttackSystem |
| **Input** | SwipeController, MouseInputManager, MouseEffect, MultiKeyEventTrigger |
| **Movement** | MovementToolkit, Follow, CameraConstraint, DistanceChecker |
| **Physics** | ExplosiveForce, ImpulseZone, MagneticField |
| **Spawner** | ObjectPool, Spawner, SimpleSpawner |
| **Managers** | Singleton, GM, EM, Bootstrap |
| **Random** | ChanceManager, ChanceSystemBehaviour |
| **Time** | Timer, TimerObject |
| **Debug** | ErrorLogger, FPS |
| **Draw** | Drawer (lines, colliders) |
| **FakeLeaderboard** | Leaderboard, LeaderboardItem |
| **InteractableObject** | InteractiveObject, PhysicsEvents2D/3D |

📖 [Documentation →](Assets/Neoxider/Docs/Tools/README.md) | [Physics →](Assets/Neoxider/Docs/Tools/Physics/README.md)

### UI — Interface

- **UI** — UI panel (page) manager
- **ButtonScale / ButtonShake** — button animations
- **AnimationFly** — "flying element" animation
- **VisualToggle** — universal visual-state toggle
- **VariantView** — visual state management

📖 [Documentation →](Assets/Neoxider/Docs/UI/README.md)

### Bonus — Bonus systems

- **Slot** — slot machine
- **WheelFortune** — wheel of fortune
- **Collection** — collection system
- **TimeReward** — time-based rewards
- **LineRoulett** — linear roulette

📖 [Documentation →](Assets/Neoxider/Docs/Bonus/README.md)

### Shop — Store

- **Shop** — central controller
- **ShopItem** — visual representation of an item
- **Money** — currency system
- **ButtonPrice** — button with price
- **TextMoney** — UI display of money

📖 [Documentation →](Assets/Neoxider/Docs/Shop/README.md)

### Save — Saves

- **SaveProvider** — static API (like PlayerPrefs)
- **ISaveProvider** — interface for custom providers
- **SaveManager** — core of the system
- **GlobalSave** — global storage
- **SaveableBehaviour** — base class for saveable components

📖 [Documentation →](Assets/Neoxider/Docs/Save/README.md)

### Quest — Quests

- **QuestConfig** — quest ScriptableObject: ID, title, description, objectives, start conditions
- **QuestManager** — quest acceptance, progress tracking, events, and Condition Context
- **QuestState** — quest runtime state and objective progress
- **QuestNoCodeAction** — universal no-code bridge for UnityEvent
- **NotifyKill / NotifyCollect** — increment counter objectives without manually walking state

📖 [Documentation →](Assets/Neoxider/Docs/Quest/README.md)

### Cards — Card games

- **MVP architecture**: Model, View, Presenter
- **CardComponent, DeckComponent, HandComponent, BoardComponent**
- **Poker** submodule with hand combinations
- **DrunkardGame** — ready-made "Drunkard" game

📖 [Documentation →](Assets/Neoxider/Docs/Cards/README.md)

### StateMachine — State machine

- Code implementation via the `IState` interface
- No-Code configuration via ScriptableObject
- Predicate system for complex transition conditions
- Visual editor in the Inspector

📖 [Documentation →](Assets/Neoxider/Docs/StateMachine/README.md)

### Animations — Animations

- **FloatAnimator** — float value animation
- **ColorAnimator** — color animation
- **Vector3Animator** — vector animation

📖 [Documentation →](Assets/Neoxider/Docs/Animations/README.md)

### Audio — Sound

- **AMSettings** — audio manager settings
- **RandomMusicController** — random music controller
- **SettingMixer** — mixer control
- **AudioSimple** — simplified playback system

📖 [Documentation →](Assets/Neoxider/Docs/Audio/README.md)

### Extensions — C# extensions

300+ extension methods:
- **Transform** — position, rotation, scale, hierarchy
- **Collections** — ForEach, Shuffle, GetRandom, FindDuplicates
- **String** — CamelCase, Truncate, Bold, Rainbow, Gradient
- **Random** — Chance, WeightedIndex, RandomColor
- **Coroutine** — Delay, WaitUntil, RepeatUntil
- **Color, Audio, Screen, Layout** and much more

📖 [Documentation →](Assets/Neoxider/Docs/Extensions/README.md)

### Editor — Editor tools

- **NeoxiderSettingsWindow** — global settings window
- **FindAndRemoveMissingScripts** — locate missing scripts
- **TextureMaxSizeChanger** — bulk texture resizing
- **SaveProjectZip** — project backups
- **AutoBuildName** — automatic build naming
- **NeoUpdateChecker** — auto-check for updates via GitHub

📖 [Documentation →](Assets/Neoxider/Docs/Editor/README.md)

### Level — Levels

- **LevelManager** — level manager
- **LevelButton** — level button
- **Map** — level map

### NPC

- **NpcNavigation** — NPC movement with patrol and chase logic
- **NpcAnimatorDriver** — sync movement state with Animator
- Used together with movement/nav workflows and animation bridges

📖 [Documentation →](Assets/Neoxider/Docs/NPC/README.md)

### Parallax

- **ParallaxLayer** — parallax with preview, gaps, and randomization

### GridSystem

- **FieldGenerator** — grid/field generator
- **FieldCell** — grid cell
- **FieldSpawner** — spawn objects on the field
- **GridShapeMask + Origin** — arbitrary shapes and a build anchor for the field
- **GridPathfinder** — pathfinding with diagnostics for unreachable paths
- **Match3 / TicTacToe** — applied gameplay layers + demo scenes

### PropertyAttribute

- `[Button]` — Inspector buttons from methods
- `[GUIColor]` — color styling for fields
- `[RequireInterface]` — interface validation
- Inject attributes: `[GetComponent]`, `[FindInScene]`, `[LoadFromResources]`

📖 [Documentation →](Assets/Neoxider/Docs/PropertyAttribute/README.md)

### Reactive

- **ReactivePropertyFloat** — serializable reactive `float` value
- **ReactivePropertyInt** — serializable reactive `int` value
- **ReactivePropertyBool** — serializable reactive `bool` value
- **SetValueWithoutNotify / ForceNotify** — control notifications during loading and manual sync

📖 [Documentation →](Assets/Neoxider/Docs/Reactive/README.md)

### Network / Multiplayer

- **Neo.Network** — optional **Mirror** integration; without Mirror the same scenarios compile as local `MonoBehaviour`
- **NeoNetworkManager**, **NetworkPropertySync**, **NetworkActionRelay**, **`NetworkContextActionRelay`**, **NetworkOwnerFilter**, lobby/discovery wrappers — replication patterns from the Inspector
- **Authority** — `NetworkAuthorityMode` for scene objects; see the NoCode specification

📖 [Multiplayer Guide →](Assets/Neoxider/Docs/Network/Multiplayer_Guide.md) · 📖 [NoCode Network Spec →](Assets/Neoxider/Docs/Network/NoCode_Network_Spec.md)

---

## Top Modules

- **NeoCondition** — No-Code conditions: check any data and build logic entirely in the Inspector
- **Counter** — universal counter with arithmetic, events, and auto-save
- **SpineController** — Spine facade with UnityEvent wrappers and auto-fill
- **ParallaxLayer** — parallax with preview and automatic tile recycling
- **DialogueManager** — dialogues with characters, portraits, and per-line events
- **ChanceManager** — declarative probability system for loot and roulettes
- **ObjectPool / Spawner** — extensible pool with waves and random prefab selection
- **MovementToolkit** — movement controllers (keyboard, mouse, 2D/3D, follow cameras)
- **Physics** — ExplosiveForce, ImpulseZone, MagneticField with custom modes
- **Timer / TimerObject** — timers with pause, replay, and progress events

---

## Installation via UPM

### Dependencies

| Package | Installation method |
|---------|---------------------|
| **Input System** (`com.unity.inputsystem`) | Recommended for Input / swipe / new input modules; already listed in the project template's `Packages/manifest.json`. In the Neoxider UPM package it is listed as a dependency for version compatibility. |
| **UniTask** | Git URL: `https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask` |
| **DOTween** | [Asset Store](https://assetstore.unity.com/packages/tools/animation/dotween-hotween-v2-27676) |
| **DOTween Pro** (for NeoxiderPages) | Asset Store — required for the NeoxiderPages sample module |
| **Mirror** (for `Neo.Network`) | [Mirror](https://github.com/MirrorNetworking/Mirror) / Asset Store — optional; needed only for networked scenarios |

### Main package

```
https://github.com/NeoXider/NeoxiderTools.git?path=Assets/Neoxider
```

Window -> Package Manager -> **+** -> Add package from git URL.

If you need a specific version, append a tag to the URL (for example, `#5.5.2`):

```
https://github.com/NeoXider/NeoxiderTools.git?path=Assets/Neoxider#5.5.2
```

### Manual installation

Copy the `Assets/Neoxider` folder into your Unity project.

---

## Installing Demo Scenes and NeoxiderPages

After installing the main package via UPM, additional modules are available through the **Package Manager**:

1. **Window -> Package Manager** -> find **Neoxider Tools** (In Project)
2. On the right panel, at the bottom — the **Samples** section
3. Click **Import** next to the module you need:
   - **Demo Scenes** — demo scenes and usage examples
   - **NeoxiderPages** — pages and screens module (PageManager, UIPage, UIKit), requires **DOTween Pro**

Files are copied to `Assets/Samples/Neoxider Tools/<version>/`.

> Alternatively: download the `.unitypackage` from [Releases](https://github.com/NeoXider/NeoxiderTools/releases)

**Quick page calls:**

```csharp
UIKit.ShowPage("PageEnd");
// or
PM.I.ChangePageByName("PageEnd");
```

`PageSubscriber` automatically resolves `PageId` by standard names: `PageGame`, `PageWin`, `PageLose`, `PageEnd` (configurable in the Inspector).

---

## FAQ

**Can I use it selectively?** Yes — import only the folders you need; dependencies are documented in each module's README.

**Are there example scenes?** Yes, in the `Demo` folder — minimal scenes for every major module.

**Does it work with 3D?** Most systems — yes. Exception: purely 2D solutions like `ParallaxLayer`.

---

## Support and contribution

Neoxider is actively evolving. Found a bug or want to suggest a module — open an issue/PR. All changes are documented in the [Changelog](Assets/Neoxider/CHANGELOG.md).

Happy developing!
