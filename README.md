# NeoxiderTools for Unity

[![Version](https://img.shields.io/badge/version-10.0.0-blue)]() [![Unity](https://img.shields.io/badge/Unity-6000.0+-green)]() [![Namespace](https://img.shields.io/badge/namespace-Neo-orange)]()

`NeoxiderTools` is a Unity package for building gameplay systems quickly without hiding the code. It combines inspector-driven no-code components, reusable runtime modules, editor tooling, samples, and documented extension points.

Use it when you want production-oriented building blocks for prototypes, jam games, and larger Unity projects: conditions, save/load, shop, RPG combat, inventory, quests, state machines, UI helpers, networking bridges, and many small tools that remove repetitive glue code.

## One package — four ways to build

- 🧩 **Beginners — no code required.** Wire real gameplay in the inspector: conditions, counters, timers, shops, slots, wheels, equipment, page navigation, click sounds. Start with the [NoCode guide](Assets/Neoxider/Docs/NoCode/GettingStarted.md).
- 🛠 **Professionals — clean C# APIs.** Production-grade modules (Save, Shop, RPG, StateMachine, Reactive, Pooling) with concise APIs, `TypeName.I` singletons, and **300+ extension methods** in `Neo.Extensions`.
- 🌐 **Multiplayer out of the box.** Optional Mirror integration: NoCode sync bridges (`NetworkPropertySync`, `NetworkReactiveSync`, `NetworkActionRelay`) and one-button LAN Quick Play. Everything still compiles and runs solo without Mirror.
- 🤖 **AI-agent development.** A bundled [agent skill](Assets/Neoxider/Skill/neoxider-tools/SKILL.md), `[NeoDoc]`-linked docs, and a machine-readable [reuse map](Assets/Neoxider/PROJECT_SUMMARY.md) let coding agents (Claude, Codex) build games on top of the package instead of reinventing it.

## Quick links

- [Package docs index](Assets/Neoxider/Docs/README.md)
- [Package README](Assets/Neoxider/README.md)
- [Project summary: existing modules and reuse map](Assets/Neoxider/PROJECT_SUMMARY.md)
- [Changelog](Assets/Neoxider/CHANGELOG.md)
- [Multiplayer guide](Assets/Neoxider/Docs/Network/Multiplayer_Guide.md)

## Install

Install **NeoxiderTools** plus the required third-party packages below. In Unity: `Window > Package Manager > + > Add package from git URL` (or import from Asset Store for DOTween).

### NeoxiderTools (Git URL)

```text
https://github.com/NeoXider/NeoxiderTools.git?path=Assets/Neoxider
```

### DOTween (required)

Install from the [Unity Asset Store — DOTween (HOTween v2)](https://assetstore.unity.com/packages/tools/animation/dotween-hotween-v2-27676), then run setup via `Tools > Demigiant > DOTween Utility Panel`.

### UniTask (required)

```text
https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask
```

### Manual install (NeoxiderTools only)

Copy `Assets/Neoxider` into your project. You still need DOTween and UniTask installed separately.

## Requirements

- Unity `6000.0+`.
- Installed automatically by UPM with NeoxiderTools: `com.unity.textmeshpro`, `com.unity.ai.navigation`, `com.unity.inputsystem`, `com.unity.ugui`.
- **Required (host project):** [DOTween](https://assetstore.unity.com/packages/tools/animation/dotween-hotween-v2-27676), [UniTask](https://github.com/Cysharp/UniTask) — see [Install](#install).
- **Optional:** `DOTween Pro` (NeoxiderPages sample), `Mirror`, `Spine Unity Runtime`, `Odin Inspector`, `MarkdownRenderer`.
- URP is optional. The package no longer depends on `com.unity.render-pipelines.universal`; add URP yourself only if your project uses URP-specific rendering features or 2D lights.

## What is inside

| Area | What it solves | Docs |
|------|----------------|------|
| Condition | Inspector-driven checks over fields, properties, methods, GameObject state, AND/OR, events | [Condition](Assets/Neoxider/Docs/Condition/README.md) |
| NoCode | Bind component values to UI and actions without writing one-off view scripts | [NoCode](Assets/Neoxider/Docs/NoCode/README.md) |
| Abilities | Data-driven Dota-style abilities and modifiers: units, properties, buffs/debuffs/DoT/shields, cast pipeline, effect ops, multiplayer-ready receipts; v10 successor to RPG | [Abilities](Assets/Neoxider/Docs/Abilities/README.md) |
| RPG | Universal resources, stats, buffs, statuses, progression, combat, targets, multiplayer-ready APIs (superseded by Abilities in v10) | [RPG](Assets/Neoxider/Docs/Rpg/README.md) |
| Shop | Items, bundles, owned/equipped state, multi-currency, inventory bridge, save profile | [Shop](Assets/Neoxider/Docs/Shop/README.md) |
| Inventory | Aggregated and slot-grid inventories, pickups, transfer rules, save integration | [Inventory](Assets/Neoxider/Docs/Tools/Inventory/README.md) |
| Save | PlayerPrefs and JSON-backed provider flow, typed helpers, save attributes | [Save](Assets/Neoxider/Docs/Save/README.md) |
| Progression | XP, levels, perk tree, unlock tree, persistent progression | [Progression](Assets/Neoxider/Docs/Progression/README.md) |
| Quest | Quest configs, goals, manager, no-code actions, runtime state | [Quest](Assets/Neoxider/Docs/Quest/README.md) |
| StateMachine | Runtime state machine plus no-code transition predicates | [StateMachine](Assets/Neoxider/Docs/StateMachine/README.md) |
| Merge | Pure C# connected-group merge engine for grids, inventories, lists, and custom graphs | [Merge](Assets/Neoxider/Docs/Merge/README.md) |
| GridSystem | Field generation, grid merge adapter, Dice, Match3, TicTacToe, SlidingMerge | [GridSystem](Assets/Neoxider/Docs/GridSystem/README.md) |
| Cards | Deck/hand/board runtime, custom decks/views, poker, Drunkard sample | [Cards](Assets/Neoxider/Docs/Cards/README.md) |
| UI | UI panels, button animations, toggles, `AnimationFly` reward motion | [UI](Assets/Neoxider/Docs/UI/README.md) |
| Network | Optional Mirror wrappers and no-code network action/sync bridges | [Network guide](Assets/Neoxider/Docs/Network/Multiplayer_Guide.md) |
| Tools | Movement, free-fly camera, physics, timers, spawners, interaction, text, managers, view helpers | [Tools](Assets/Neoxider/Docs/Tools/README.md) |
| Extensions | 300+ C# and Unity API extension methods | [Extensions](Assets/Neoxider/Docs/Extensions/README.md) |

## No-code example: NeoCondition

`NeoCondition` lets designers wire gameplay logic from the Inspector:

- Check fields, properties, GameObject state, or single-argument methods.
- Compare against constants or another object.
- Use `AND`, `OR`, and inversion.
- Trigger `OnTrue`, `OnFalse`, `OnResult`, and `OnInvertedResult` UnityEvents.

Example: `Money.CanSpend(100) == true` can enable a Buy button, while `== false` can show a not-enough-money hint.

Read more: [NeoCondition docs](Assets/Neoxider/Docs/Condition/NeoCondition.md).

## First scene checklist

1. Install NeoxiderTools, DOTween, and UniTask (see [Install](#install)).
2. Add `Assets/Neoxider/Prefabs/--System--.prefab` if your scene uses built-in managers or UI bootstrap.
3. Add components through `Add Component > Neoxider`.
4. Start with one module guide: [Shop](Assets/Neoxider/Docs/Shop/README.md), [RPG](Assets/Neoxider/Docs/Rpg/README.md), [Condition](Assets/Neoxider/Docs/Condition/README.md), or [Tools](Assets/Neoxider/Docs/Tools/README.md).
5. For multiplayer, install Mirror first and follow the [Multiplayer guide](Assets/Neoxider/Docs/Network/Multiplayer_Guide.md).

## Samples

During active development samples are visible under `Assets/Neoxider/Samples/`. Before release packaging they are moved to the UPM sample source path `Assets/Neoxider/Samples~/`, then imported by Package Manager into `Assets/Samples/NeoxiderTools/<version>/<sample>/`.

| Sample | Purpose |
|--------|---------|
| Demo | Integration scenes for core systems and gameplay modules, including `SurvivorDemo` — a complete Vampire-Survivors-style game assembled from one `SurvivorConfig` data asset on top of `Neo.Abilities` |
| NeoxiderPages | Optional page-navigation sample with `PM`, `UIPage`, `BtnChangePage`, and UIKit helpers |

## Tests

Package tests live under `Assets/Neoxider/Tests/`:

- `Edit` for edit-mode and pure logic coverage.
- `Play` / `PlayMode` for runtime and scene behavior.
- `Editor` for editor-specific package checks.

Run them from Unity Test Runner. The package expects `com.unity.test-framework` in the host project when tests are used.

## Project layout

```text
Assets/Neoxider/
  Scripts/       Runtime modules and asmdef-separated code
  Editor/        Custom inspectors, windows, and editor utilities
  Tests/         EditMode and PlayMode package tests
  Docs/          Documentation
  Samples/       Active development samples
  Samples~/      UPM sample source path before release packaging
  Prefabs/       Ready-to-use prefabs
  Resources/     Settings and package assets
```

## Games using NeoxiderTools

| Game | Genre | Platform | Link | Notes |
|------|-------|----------|------|-------|
| Fake Grandkids | Arcade, Survival | Windows | [MyIndie](https://myindie.ru/games/game/fake-grandkids) | UralGameJam 2026; inspector-driven Neoxider workflow |

## Support

Open an issue or pull request in the repository. Keep public behavior changes documented in [CHANGELOG.md](Assets/Neoxider/CHANGELOG.md) and the relevant module docs.
