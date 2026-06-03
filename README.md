# NeoxiderTools for Unity

[Russian](README_RU.md) | [English](README.md)

[![Version](https://img.shields.io/badge/version-9.1.0-blue)]() [![Unity](https://img.shields.io/badge/Unity-2022.1+-green)]() [![Namespace](https://img.shields.io/badge/namespace-Neo-orange)]()

`NeoxiderTools` is a Unity package for building gameplay systems quickly without hiding the code. It combines inspector-driven no-code components, reusable runtime modules, editor tooling, samples, and documented extension points.

Use it when you want production-oriented building blocks for prototypes, jam games, and larger Unity projects: conditions, save/load, shop, RPG combat, inventory, quests, state machines, UI helpers, networking bridges, and many small tools that remove repetitive glue code.

For a fast map of existing systems before building something from scratch, start with the [Project summary](Assets/Neoxider/PROJECT_SUMMARY.md).

## Quick links

- [Package docs index](Assets/Neoxider/DocsEn/README.md)
- [Russian docs index](Assets/Neoxider/Docs/README.md)
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

- Unity `2022.1+`.
- Installed automatically by UPM with NeoxiderTools: `com.unity.textmeshpro`, `com.unity.ai.navigation`, `com.unity.inputsystem`.
- **Required (host project):** [DOTween](https://assetstore.unity.com/packages/tools/animation/dotween-hotween-v2-27676), [UniTask](https://github.com/Cysharp/UniTask) — see [Install](#install).
- **Optional:** `DOTween Pro` (NeoxiderPages sample), `Mirror`, `Spine Unity Runtime`, `Odin Inspector`, `MarkdownRenderer`.
- URP is optional. The package no longer depends on `com.unity.render-pipelines.universal`; add URP yourself only if your project uses URP-specific rendering features or 2D lights.

## What is inside

| Area | What it solves | Docs |
|------|----------------|------|
| Condition | Inspector-driven checks over fields, properties, methods, GameObject state, AND/OR, events | [Condition](Assets/Neoxider/DocsEn/Condition/README.md) |
| NoCode | Bind component values to UI and actions without writing one-off view scripts | [NoCode](Assets/Neoxider/Docs/NoCode/README.md) |
| RPG | Universal resources, stats, buffs, statuses, progression, combat, targets, multiplayer-ready APIs | [RPG](Assets/Neoxider/DocsEn/Rpg/README.md) |
| Shop | Items, bundles, owned/equipped state, multi-currency, inventory bridge, save profile | [Shop](Assets/Neoxider/DocsEn/Shop/README.md) |
| Inventory | Aggregated and slot-grid inventories, pickups, transfer rules, save integration | [Inventory](Assets/Neoxider/DocsEn/Tools/Inventory/README.md) |
| Save | PlayerPrefs and JSON-backed provider flow, typed helpers, save attributes | [Save](Assets/Neoxider/DocsEn/Save/README.md) |
| Progression | XP, levels, perk tree, unlock tree, persistent progression | [Progression](Assets/Neoxider/DocsEn/Progression/README.md) |
| Quest | Quest configs, goals, manager, no-code actions, runtime state | [Quest](Assets/Neoxider/DocsEn/Quest/README.md) |
| StateMachine | Runtime state machine plus no-code transition predicates | [StateMachine](Assets/Neoxider/DocsEn/StateMachine/README.md) |
| Merge | Pure C# connected-group merge engine for grids, inventories, lists, and custom graphs | [Merge](Assets/Neoxider/DocsEn/Merge/README.md) |
| GridSystem | Field generation, grid merge adapter, Dice, Match3, TicTacToe, SlidingMerge | [GridSystem](Assets/Neoxider/DocsEn/GridSystem/README.md) |
| Network | Optional Mirror wrappers and no-code network action/sync bridges | [Network guide](Assets/Neoxider/Docs/Network/Multiplayer_Guide.md) |
| Tools | Movement, free-fly camera, physics, timers, spawners, interaction, text, managers, view helpers | [Tools](Assets/Neoxider/DocsEn/Tools/README.md) |
| Extensions | 300+ C# and Unity API extension methods | [Extensions](Assets/Neoxider/DocsEn/Extensions/README.md) |

## No-code example: NeoCondition

`NeoCondition` lets designers wire gameplay logic from the Inspector:

- Check fields, properties, GameObject state, or single-argument methods.
- Compare against constants or another object.
- Use `AND`, `OR`, and inversion.
- Trigger `OnTrue`, `OnFalse`, `OnResult`, and `OnInvertedResult` UnityEvents.

Example: `Money.CanSpend(100) == true` can enable a Buy button, while `== false` can show a not-enough-money hint.

Read more: [NeoCondition docs](Assets/Neoxider/DocsEn/Condition/NeoCondition.md).

## First scene checklist

1. Install NeoxiderTools, DOTween, and UniTask (see [Install](#install)).
2. Add `Assets/Neoxider/Prefabs/--System--.prefab` if your scene uses built-in managers or UI bootstrap.
3. Add components through `Add Component > Neoxider`.
4. Start with one module guide: [Shop](Assets/Neoxider/DocsEn/Shop/README.md), [RPG](Assets/Neoxider/DocsEn/Rpg/README.md), [Condition](Assets/Neoxider/DocsEn/Condition/README.md), or [Tools](Assets/Neoxider/DocsEn/Tools/README.md).
5. For multiplayer, install Mirror first and follow the [Multiplayer guide](Assets/Neoxider/Docs/Network/Multiplayer_Guide.md).

## Samples

During active development samples are visible under `Assets/Neoxider/Samples/`. Before release packaging they are moved to the UPM sample source path `Assets/Neoxider/Samples~/`, then imported by Package Manager into `Assets/Samples/NeoxiderTools/<version>/<sample>/`.

| Sample | Purpose |
|--------|---------|
| Demo | Integration scenes for core systems and gameplay modules |
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
  Docs/          Russian documentation
  DocsEn/        English documentation
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
