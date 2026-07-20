<div align="center">

<img src="Images/neoxider_cover_cosmic.png" alt="NeoxiderTools — Unity Game Development Library" width="620" />

# NeoxiderTools — assemble whole games from ready modules

**A batteries-included Unity toolkit: 20+ gameplay modules, a no-code inspector layer, and 200+ extension methods — so you _assemble_ games instead of re-writing the same glue every project.**

[![Version](https://img.shields.io/badge/version-10.1.0-blue)]()
[![Unity](https://img.shields.io/badge/Unity-6000.0+-green)]()
[![Namespace](https://img.shields.io/badge/namespace-Neo-orange)]()
[![Tests](https://img.shields.io/badge/tests-1000%2B%20green-brightgreen)]()
[![NoCode](https://img.shields.io/badge/NoCode-ready-8A54D6)]()

### [🚀 Install](#-install) · [📚 Documentation](Assets/Neoxider/Docs/README.md) · [🧩 Modules](#-what-is-inside) · [🎮 Samples](#-samples) · [🗺 Reuse map](Assets/Neoxider/PROJECT_SUMMARY.md) · [📝 Changelog](Assets/Neoxider/CHANGELOG.md)

</div>

---

Prototype a jam game in an afternoon or ship a full title. Drop in ready systems — Dota-style **Abilities**, **Shop**, **Save**, **Quests**, **RPG**, **Cards**, **Grids**, **Merge**, **NPC AI**, **UI motion** — wire them in the Inspector *or* drive them from clean C# APIs, then go online with one optional package. Backed by **1,000+ automated tests** (978 EditMode + 109 PlayMode, all green).

📖 **Full package README — highlight reel, module map, quick start: [Assets/Neoxider/README.md](Assets/Neoxider/README.md)**

## 📑 Contents

- [One package — four ways to build](#one-package--four-ways-to-build)
- [🚀 Install](#-install)
- [Requirements](#requirements)
- [🧩 What is inside](#-what-is-inside)
- [No-code example: NeoCondition](#no-code-example-neocondition)
- [First scene checklist](#first-scene-checklist)
- [🎮 Samples](#-samples)
- [Tests](#tests)
- [Project layout](#project-layout)
- [Games using NeoxiderTools](#games-using-neoxidertools)
- [Support](#support)

## One package — four ways to build

- 🧩 **Beginners — no code required.** Wire real gameplay in the inspector: conditions, counters, timers, shops, slots, wheels, equipment, page navigation, click sounds. Start with the [NoCode guide](Assets/Neoxider/Docs/NoCode/GettingStarted.md).
- 🛠 **Professionals — clean C# APIs.** Production-grade modules (Save, Shop, RPG, StateMachine, Reactive, Pooling) with concise APIs, `TypeName.I` singletons, and **200+ extension methods** in `Neo.Extensions`.
- 🌐 **Multiplayer out of the box.** Optional Mirror integration: NoCode sync bridges (`NetworkPropertySync`, `NetworkReactiveSync`, `NetworkActionRelay`) and one-button LAN Quick Play. Everything still compiles and runs solo without Mirror.
- 🤖 **AI-agent development.** A bundled [agent skill](Assets/Neoxider/Skill/neoxider-tools/SKILL.md), `[NeoDoc]`-linked docs, and a machine-readable [reuse map](Assets/Neoxider/PROJECT_SUMMARY.md) let coding agents (Claude, Codex) build games on top of the package instead of reinventing it.

<div align="center">
<img src="Assets/Neoxider/Images/cover_1_nodes.png" alt="Neoxider modules wire together" width="440" />
</div>

## 🚀 Install

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
- **Required (host project):** [DOTween](https://assetstore.unity.com/packages/tools/animation/dotween-hotween-v2-27676), [UniTask](https://github.com/Cysharp/UniTask) — see [Install](#-install).
- **Optional:** `DOTween Pro` (NeoxiderPages sample), `Mirror`, `Spine Unity Runtime`, `Odin Inspector`, `MarkdownRenderer`.
- URP is optional. The package no longer depends on `com.unity.render-pipelines.universal`; add URP yourself only if your project uses URP-specific rendering features or 2D lights.

## 🧩 What is inside

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
| Extensions | 200+ C# and Unity API extension methods | [Extensions](Assets/Neoxider/Docs/Extensions/README.md) |

## No-code example: NeoCondition

`NeoCondition` lets designers wire gameplay logic from the Inspector:

- Check fields, properties, GameObject state, or single-argument methods.
- Compare against constants or another object.
- Use `AND`, `OR`, and inversion.
- Trigger `OnTrue`, `OnFalse`, `OnResult`, and `OnInvertedResult` UnityEvents.

Example: `Money.CanSpend(100) == true` can enable a Buy button, while `== false` can show a not-enough-money hint.

Read more: [NeoCondition docs](Assets/Neoxider/Docs/Condition/NeoCondition.md).

## First scene checklist

1. Install NeoxiderTools, DOTween, and UniTask (see [Install](#-install)).
2. Add `Assets/Neoxider/Prefabs/--System--.prefab` if your scene uses built-in managers or UI bootstrap.
3. Add components through `Add Component > Neoxider`.
4. Start with one module guide: [Shop](Assets/Neoxider/Docs/Shop/README.md), [RPG](Assets/Neoxider/Docs/Rpg/README.md), [Condition](Assets/Neoxider/Docs/Condition/README.md), or [Tools](Assets/Neoxider/Docs/Tools/README.md).
5. For multiplayer, install Mirror first and follow the [Multiplayer guide](Assets/Neoxider/Docs/Network/Multiplayer_Guide.md).

## 🎮 Samples

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
