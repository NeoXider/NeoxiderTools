![NeoxiderTools Cover](./Images/cover_1_nodes.png)

# NeoxiderTools

`NeoxiderTools` is a Unity package with ready-to-use gameplay systems, no-code components, UI helpers, editor utilities, and sample scenes for rapid prototyping and production workflows.

[![Version](https://img.shields.io/badge/version-9.13.1-blue)]() [![Unity](https://img.shields.io/badge/Unity-6000.0+-green)]() [![Namespace](https://img.shields.io/badge/namespace-Neo-orange)]()

- [GitHub](https://github.com/NeoXider/NeoxiderTools)
- [Documentation Index](./Docs/README.md)
- [Changelog](./CHANGELOG.md)
- [Project Summary: existing modules and reuse map](./PROJECT_SUMMARY.md)

## One package — four ways to build

| | Who | What you get |
|---|-----|--------------|
| 🧩 | **Beginners — no code required** | Build real gameplay by wiring inspector components: conditions (`NeoCondition`), counters, timers, shops, wheels, slots, equipment, page navigation, click sounds. Start here: [NoCode guide](./Docs/NoCode/GettingStarted.md). |
| 🛠 | **Professionals — clean C# APIs** | Production-grade modules (Save, Shop, RPG, StateMachine, Reactive, Pooling) driven through concise APIs and singletons (`AM.I`, `SaveManager.I`), plus **300+ extension methods** in `Neo.Extensions` so gameplay code reads like a spec. |
| 🌐 | **Multiplayer out of the box** | Optional [Mirror](https://github.com/MirrorNetworking/Mirror) integration: NoCode sync bridges (`NetworkPropertySync`, `NetworkReactiveSync`, `NetworkActionRelay`), lobby/discovery with one-button Quick Play. Everything compiles and runs solo without Mirror installed. |
| 🤖 | **AI-agent development** | A bundled [agent skill](./Skill/neoxider-tools/SKILL.md), `[NeoDoc]`-linked documentation, and a machine-readable [reuse map](./PROJECT_SUMMARY.md) let Claude/Codex-style coding agents build full games on top of the package instead of reinventing it. |

---

## Table of Contents
- [What You Get](#what-you-get)
- [Requirements](#requirements)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Multiplayer quick start](#multiplayer-quick-start)
- [Games built with NeoxiderTools](#games-built-with-neoxidertools)
- [Featured Modules](#featured-modules)
- [Samples](#samples)
- [Documentation](#documentation)
- [Tests](#tests)
- [Project Layout](#project-layout)
- [Support](#support)

---

## What You Get

Start with the [Project Summary](./PROJECT_SUMMARY.md) when you need a compact map of what already exists in the package and what should be reused instead of rebuilt.

- **RPG module** - unified `RpgCharacter` runtime for players, NPCs, mobs, pets, and destructibles: universal resources/stats, level/XP/upgrades, buffs/statuses, regen, save/load, Mirror multiplayer sync, melee/ranged/aoe attacks (`RpgAttackController` + `RpgAttackDefinition` + `RpgProjectile`), target selectors, evade (`RpgEvadeController`), UI bindings, and no-code bridges.
- **Grid/Merge modules** - `FieldGenerator`, reusable multi-cell placement, `Neo.Merge`, `GridMergeResolver`, `DiceBoardService`, Match3, TicTacToe, and SlidingMerge services for grid games and board-like systems.
- **UI reward motion** - `AnimationFly` has a typed request/result API for prefab or sprite visuals, world/canvas conversions, pooling, and reward timing callbacks.
- **Cards runtime rails** - `HandModel` supports unlimited hands by default and optional finite capacity through `Capacity`, `TryAdd(...)`, `RemainingCapacity`, and `AddRangeUntilFull(...)`.
- **Dice value pools** - `DicePieceGenerator` keeps the original merge pool through `CreateDefaultPool()`, adds `CreateD6Pool()`, and exposes `CreateSequentialPool(minValue, maxValue)` for custom numbered dice.
- No-code gameplay building blocks such as `NeoCondition`, `Counter`, timers, interaction handlers, and UnityEvent-driven components.
- Reusable runtime modules for inventory, save/load, dialogue, grid systems, cards, shop, progression, state machine, modular NPC navigation/combat composition, audio, and UI.
- **Multiplayer (optional Mirror)** — `Neo.Network` NoCode bridges (`NetworkPropertySync`, `NetworkActionRelay`, lobby/discovery wrappers). Without Mirror, the same scripts compile for offline/solo flows.
- **Free-fly tooling** — `FreeFlyCameraController` provides Unity Scene View style debug/spectator camera movement in `Tools/Move`, with RMB gating by default.
- Editor helpers, package samples, prefabs, and module-focused documentation.

## Installation

### Unity Package Manager (Git URL)

```text
https://github.com/NeoXider/NeoxiderTools.git?path=Assets/Neoxider
```

Open `Window > Package Manager > + > Add package from git URL` and paste the URL above.

### Manual Install

Copy `Assets/Neoxider` into your Unity project.

## Requirements

- **Unity 6000.0+**
- Current compatibility notes: [`Docs/PackageCompatibility.md`](./Docs/PackageCompatibility.md)
- **Automatic via UPM**: `com.unity.textmeshpro`, `com.unity.ai.navigation`, `com.unity.inputsystem`, `com.unity.ugui`
- **Required (3rd party)**:
  - `UniTask` (for async-heavy modules such as cards, dialogue)
  - `DOTween` (for package runtime modules)
- **Optional**:
  - `DOTween Pro` (optional for project-specific UI animation workflows)
  - `Spine Unity Runtime` (only for Spine integrations)
  - `Odin Inspector` (components work perfectly without it)
  - `MarkdownRenderer` (install via `https://github.com/NeoXider/MarkdownRenderer.git` for enhanced markdown in Inspector)
  - **Mirror** (required only for `Neo.Network` / multiplayer; see [Multiplayer_Guide](./Docs/Network/Multiplayer_Guide.md))
  - **URP** (`com.unity.render-pipelines.universal`) only if your project uses URP-specific rendering features or 2D lights; the package no longer installs URP automatically.
- None of the above are bundled inside this package — see [THIRD-PARTY-NOTICES.md](./THIRD-PARTY-NOTICES.md) for what's referenced, why, and license pointers.

## Quick Start

1. Import the package by UPM or by copying `Assets/Neoxider`.
2. Add `Assets/Neoxider/Prefabs/--System--.prefab` if your scene uses the built-in managers/UI bootstrap.
3. Add components through `Add Component > Neoxider`.
4. Open the module guide in [Docs/README.md](./Docs/README.md) and start from the module you need.

## Multiplayer quick start

1. Install **Mirror** (see [Mirror](https://github.com/MirrorNetworking/Mirror)).
2. Add **`NeoNetworkManager`** + a Mirror **Transport** to the scene.
3. For NoCode, keep the player configured in the scene: add `NetworkIdentity`, enable **Use Scene Player Template**, assign the player to **Scene Player Template**, and leave **Player Prefab** empty.
4. Call `NeoNetworkManager.Singleton.StartHost()` / `StartClient()` from UI or code (details in the guide).
5. Enable **`isNetworked`** on NoCode components that should replicate; read **[Multiplayer_Guide.md](./Docs/Network/Multiplayer_Guide.md)** and **[NoCode_Network_Spec.md](./Docs/Network/NoCode_Network_Spec.md)**.

## Games built with NeoxiderTools

Shipping and jam titles that use this package for gameplay (no-code + modules). **Add new games** in the showcase table: [GitHub — root README](https://github.com/NeoXider/NeoxiderTools#games-built-with-neoxidertools) · [local monorepo](../../README.md#games-built-with-neoxidertools).

| Game | Genres | Platform | Link | Notes |
|------|--------|----------|------|-------|
| **Fake Grandkids** *(Внуки понарошку: пенсия прилагается)* | Arcade, Survival | Windows | [MyIndie](https://myindie.ru/games/game/fake-grandkids) | RU; **UralGameJam 2026**; NeoCondition / Neoxider workflow |

**Template for a new row** (copy into the root README table):

```markdown
| [**Game Title**](https://page-url) | Genre A, Genre B | Windows | [Store / Page](https://page-url) | Language; jam; short note |
```

## Featured Modules

| Module | What it covers | Docs |
|--------|-----------------|------|
| **RPG** | Persistent player profile, scene combatants, HP, levels, buffs, statuses, melee/ranged/aoe, evade, target selectors, attack presets, and no-code bridges | [RPG](./Docs/Rpg/README.md) |
| **Progression** | XP, levels, unlock tree, perk tree, and persistent progression | [Progression](./Docs/Progression/README.md) |
| **Condition** | No-code conditions, field checks, AND/OR logic, and UnityEvent outputs | [NeoCondition](./Docs/Condition/NeoCondition.md) |
| **Tools** | 150+ components for movement, free-fly cameras, physics, spawners, timers, input, and utility runtime | [Tools](./Docs/Tools/README.md) |
| **Quest** | Quest configs, goals, manager, and runtime quest state | [Quest](./Docs/Quest/README.md) |
| **Reactive** | Serializable reactive properties for `float`, `int`, and `bool` | [Reactive](./Docs/Reactive/README.md) |
| **Save** | `PlayerPrefs`, JSON files, provider-based save flow, scene/global data, save attributes | [Save](./Docs/Save/README.md) |
| **UI** | UI panels, button animations, toggles, and presentation helpers | [UI](./Docs/UI/README.md) |
| **Cards** | MVP architecture, poker, "Drunkard", and deck/hand runtime flow | [Cards](./Docs/Cards/README.md) |
| **Merge** | Pure C# connected-group merge engine for grids, inventories, lists, and custom graphs | [Merge](./Docs/Merge/README.md) |
| **GridSystem** | Grid generation, origin anchor, pathfinding, placement, GridMerge, Dice, Match3, TicTacToe, and SlidingMerge | [GridSystem](./Docs/GridSystem/README.md) |
| **NPC** | NPC navigation, patrol, chase, animator driver, and modular RPG-ready combat | [NPC](./Docs/NPC/README.md) |
| **Network** | Mirror-based multiplayer: `NeoNetworkManager`, NoCode sync (`NetworkPropertySync`, `NetworkActionRelay`), lobby/discovery | [Multiplayer_Guide](./Docs/Network/Multiplayer_Guide.md) · [NoCode_Network_Spec](./Docs/Network/NoCode_Network_Spec.md) |
| **Editor** | Settings windows, missing-script finder, auto-build, and maintenance tools | [Editor](./Docs/Editor/README.md) |

## Samples

Import samples via `Package Manager > NeoxiderTools > Samples`.

| Sample | Path | Purpose |
|--------|------|---------|
| **Demo Scenes** | dev: `Assets/Neoxider/Samples/Demo/`; UPM source: `Assets/Neoxider/Samples~/Demo/`; imported: `Assets/Samples/NeoxiderTools/<version>/Demo Scenes/` | Integration scenes for core modules and gameplay features |
| **NeoxiderPages** | dev: `Assets/Neoxider/Samples/NeoxiderPages/`; UPM source: `Assets/Neoxider/Samples~/NeoxiderPages/`; imported: `Assets/Samples/NeoxiderTools/<version>/NeoxiderPages/` | Page-navigation sample module (`PM`, `UIPage`, `BtnChangePage`, `UIKit`) |

## Documentation

- The canonical user-facing navigation lives in [Docs/README.md](./Docs/README.md).
- The index keeps one canonical entry per module and routes detailed pages through that module entry.
- Compatibility notes: [PackageCompatibility](./Docs/PackageCompatibility.md).

## Tests

- Package tests live in `Assets/Neoxider/Tests/` (`Edit`, `Play`, and `PlayMode`), with editor-only tests under `Assets/Neoxider/Tests/Editor/`.
- The package currently includes coverage for save flows, level helpers, bootstrap order, legacy visibility rules, and RPG combat/runtime behavior.

## Project Layout

```text
Assets/Neoxider/
  Scripts/       # Runtime modules and asmdef-separated code
  Editor/        # Editor tooling
  Tests/         # EditMode and PlayMode tests for package runtime/editor-critical flows
  Docs/          # User-facing documentation
  Samples/       # Active development samples and smoke scenes
  Samples~/      # UPM sample source path before release packaging
  Prefabs/       # Ready-to-use prefabs
  Resources/     # Settings and assets
```

## Support

If you find a bug or want to suggest an improvement, open an [issue](https://github.com/NeoXider/NeoxiderTools/issues) or PR in the main repository.
