<p align="center"><img src="https://raw.githubusercontent.com/NeoXider/NeoxiderTools/main/Images/readme/01_hero.svg" alt="NeoxiderTools: a gameplay-systems framework for Unity" width="100%" /></p>

# NeoxiderTools — a gameplay-systems framework for Unity

[![Version](https://img.shields.io/badge/version-11.0.0-blue)](./package.json) [![Unity](https://img.shields.io/badge/Unity-6000.0+-green)](./Docs/PackageCompatibility.md) [![Namespace](https://img.shields.io/badge/namespace-Neo-orange)](./Docs/README.md#namespace-policy) [![Tests](https://github.com/NeoXider/NeoxiderTools/actions/workflows/tests.yml/badge.svg)](https://github.com/NeoXider/NeoxiderTools/actions/workflows/tests.yml) [![NoCode](https://img.shields.io/badge/NoCode-ready-8A54D6)](./Docs/NoCode/README.md)

**A modular Unity C# construction library: abilities, shops, saving, quests, cards, grids, NPC AI, UI motion. 25+ modules, 200+ ready components, 200+ extension methods and 1,500+ tests help you assemble reusable game systems from ready parts. Use runtime APIs directly, add scene components, or combine both.**

**`SurvivorDemo`** shows a Vampire-Survivors-style sample assembled from one `SurvivorConfig` data asset on top of `Neo.Abilities`. **Abilities**, **Shop**, **Save**, **Quests**, **RPG**, **Cards**, **Grids**, **Merge**, **NPC AI** and **UI motion** offer C# APIs and scene workflows, with optional Mirror integration for multiplayer. The package includes EditMode and PlayMode coverage; see [Tests](#tests) for how to run it.

[GitHub](https://github.com/NeoXider/NeoxiderTools) · [Documentation](./Docs/README.md) · [Changelog](./CHANGELOG.md) · [Reuse map](./PROJECT_SUMMARY.md) · [Upgrade from 10.x](./Docs/Migration11.md)

> **11.0.0 release preparation — UNVERIFIED.** Package metadata is set to `11.0.0`; final compilation, EditMode/PlayMode execution, and package verification are pending completion of the source changes. Existing projects should start with [Migration to 11](./Docs/Migration11.md).

**По-русски:** [C# API, компоненты и NoCode](./Docs/README.md) · [обновление с 10.x](./Docs/Migration11.md).

---

## One package — four ways to build

<p align="center"><img src="https://raw.githubusercontent.com/NeoXider/NeoxiderTools/main/Images/readme/04_four_ways.svg" alt="No code, clean C# APIs, optional multiplayer, agent-friendly" width="100%" /></p>

| | Who | What you get |
|---|-----|--------------|
| 🧩 | **Beginners — zero code** | Build real gameplay by wiring inspector components: conditions (`NeoCondition`), counters, timers, shops, wheels, slots, equipment, page navigation, click sounds. Start here → [NoCode guide](./Docs/NoCode/GettingStarted.md). |
| 🛠 | **Programmers — clean C# APIs** | Runtime models and services you own from C#, plus **200+ `Neo.Extensions` helpers**. Start with the [C# quick start](./Docs/GettingStarted.md#start-with-a-c-runtime-model). Scene singletons such as `AM.I` are available for modules that expose them. |
| 🌐 | **Multiplayer, optional** | [Mirror](https://github.com/MirrorNetworking/Mirror) bridges (`NetworkPropertySync`, `NetworkReactiveSync`, `NetworkActionRelay`) and one-button Quick Play. The bridges include a solo-mode path; install Mirror when enabling networking. |
| 🤖 | **AI-agent friendly** | A bundled [agent skill](./Skill/neoxider-tools/SKILL.md), `[NeoDoc]`-linked docs, and a machine-readable [reuse map](./PROJECT_SUMMARY.md) let Claude/Codex-style agents build on the package instead of reinventing it. |

---

## Highlight reel

- ⚔️ **Dota-style Abilities + a visual Ability Designer.** Author abilities and modifiers as data assets (`AbilityDefinition`, `ModifierDefinition`, `UnitTemplate`); one validated cast pipeline resolves damage / heal / knockback / pull / teleport / chain / execute, live crit / lifesteal / evasion, and buffs / debuffs / DoT / auras / shields / stuns — with multiplayer-ready receipts.
- 🧛 **A Vampire-Survivors-style sample from one data asset.** The bundled `SurvivorDemo` assembles auto-fire, waves, upgrades and scaling from a single `SurvivorConfig` on top of `Neo.Abilities` — use it as a starting point for your own survivor-like.
- 🧩 **No-code that actually builds games.** Ready components + inspector event wiring + binding scripts let non-coders assemble whole scenes. Study the reference scenes: `adventure`, `Shooter2D`, `ClickerExample`, `DrawExample`.
- 🟢 **A living inspector.** The Neoxider header is a "slime linter": it reflects each component's health — remembers console errors per component, flags missing references and NaN/∞ fields, and watches the Game view in Play Mode.
- 🎁 **Juice out of the box.** `AnimationFly` (coins/gems fly to the HUD with fountain / magnet / scatter presets), tweened toggles and buttons, parallax, and audio managers provide configurable motion and feedback.
- 🧰 **200+ extension methods & 200+ drop-in components.** Random, collections, transforms, string/number formatting, coroutines, movement, cameras, spawners, timers — the repetitive glue is already written. Browse registered component entries and ready-made presets in the **Create Neoxider Object** window; the available entries depend on imported samples and optional modules.

---

## Requirements

- **Unity 6000.0+** (Unity 6). Compatibility notes: [`Docs/PackageCompatibility.md`](./Docs/PackageCompatibility.md).
- **Installed automatically (UPM):** `com.unity.ugui` (ships TextMeshPro on Unity 6), `com.unity.ai.navigation`, `com.unity.inputsystem`.
- **Required (add to your project):** [DOTween](https://assetstore.unity.com/packages/tools/animation/dotween-hotween-v2-27676) and [UniTask](https://github.com/Cysharp/UniTask).
- **Optional:** `Mirror` (multiplayer), `DOTween Pro`, `Odin Inspector`, `Spine Runtime`, [`MarkdownRenderer`](https://github.com/NeoXider/MarkdownRenderer), the [mobile haptics backend](./Docs/Haptics/Haptics.md), URP (only for URP-specific rendering / 2D lights). NeoxiderPages imports without DOTween Pro.

DOTween, UniTask, and the optional integrations above are installed separately. Character Movement Fundamentals runtime/editor code is bundled in `ThirdParty/CharacterMovementFundamentals`; its demo content is an optional sample. See [THIRD-PARTY-NOTICES.md](./THIRD-PARTY-NOTICES.md) for the license and dependency details.

## Install

**Package Manager → `+` → Add package from git URL:**

```text
https://github.com/NeoXider/NeoxiderTools.git?path=Assets/Neoxider
```

This URL follows the remote default branch. For the current 11.0.0 preparation checkout, use the [local-package route](./Docs/GettingStarted.md#option-b--local-path-for-development).

Then install DOTween (Asset Store, run `Tools > Demigiant > DOTween Utility Panel`) and UniTask:

```text
https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask
```

*Manual install:* copy `Assets/Neoxider` into your project (you still need DOTween + UniTask).

## Quick start

1. Install NeoxiderTools + DOTween + UniTask (above).
2. Choose the entry point that matches the feature you are building:

| Workflow | First step |
|----------|------------|
| **C# runtime API** | Follow the [runtime-model quick start](./Docs/GettingStarted.md#start-with-a-c-runtime-model), then use the public APIs in [Core](./Docs/Core/README.md), [Merge](./Docs/Merge/README.md), or [StateMachine](./Docs/StateMachine/README.md). |
| **Scene components** | Follow the [first audio scene](./Docs/GettingStarted.md#your-first-scene-in-5-minutes-audio-manager-am). Add components from `Add Component ▸ Neoxider` or `Neoxider ▸ Windows ▸ Create Neoxider Object`, then assign their dependencies. |
| **Inspector / NoCode** | Follow [NoCode Getting Started](./Docs/NoCode/GettingStarted.md). Bind values and events to typed runtime APIs; keep scene references on components and reusable configuration in assets. |

The package's `Prefabs/-System--.prefab` is available for the existing manager bootstrap. Runtime models such as `ResourcePoolModel`, `MergeResolver`, and `StateMachine<TState>` can be owned by your own code without it. Module setup and dependencies are described in the [Docs index](./Docs/README.md).

**Multiplayer:** install Mirror → add `NeoNetworkManager` + a Transport → enable `isNetworked` on the NoCode components that should replicate. Full walkthrough: [Multiplayer Guide](./Docs/Network/Multiplayer_Guide.md).

---

## How it fits together

<p align="center"><img src="https://raw.githubusercontent.com/NeoXider/NeoxiderTools/main/Images/readme/02_layers.svg" alt="NeoxiderTools layers" width="100%" /></p>

Start with the runtime contract and add scene wrappers, views, or NoCode helpers where the feature needs them. Assembly definitions declare each module's dependencies; module guides distinguish ordinary C# objects from scene-facing components.

<p align="center"><img src="https://raw.githubusercontent.com/NeoXider/NeoxiderTools/main/Images/readme/03_module_shape.svg" alt="Every module has the same shape" width="100%" /></p>

## Module map

| Module | What it covers | Docs |
|--------|-----------------|------|
| ⚔️ **Abilities** *(v10)* | Data-driven Dota-style abilities & modifiers: units, properties, buffs/DoT/shields, cast pipeline, effect-op atoms, multiplayer receipts | [Abilities](./Docs/Abilities/README.md) |
| 🧙 **RPG** | Unified `RpgCharacter`: resources, stats, level/XP, buffs/statuses, melee/ranged/aoe, evade, target selectors, no-code bridges *(superseded by Abilities)* | [RPG](./Docs/Rpg/README.md) |
| 📈 **Progression** | XP, levels, perk tree, unlock tree, persistent meta-progression | [Progression](./Docs/Progression/README.md) |
| 🎛 **Condition** | No-code checks over fields/props/methods/state, AND/OR, UnityEvent outputs | [Condition](./Docs/Condition/README.md) |
| 🔗 **NoCode** | Bind component values to UI & actions, formatted text, progress, action bridges | [NoCode](./Docs/NoCode/README.md) |
| 💾 **Save** | PlayerPrefs + JSON providers, typed helpers, save attributes, scene/global data | [Save](./Docs/Save/README.md) |
| 🛒 **Shop** | Items, bundles, owned/equipped, multi-currency, inventory bridge, save profile | [Shop](./Docs/Shop/README.md) |
| 🎒 **Inventory** | Aggregated & slot-grid inventories, pickups, transfer rules, save integration | [Inventory](./Docs/Tools/Inventory/README.md) |
| 🗺 **Quest** | Quest configs, goals, manager, no-code actions, runtime state | [Quest](./Docs/Quest/README.md) |
| 🔁 **StateMachine** | Runtime FSM + no-code transition predicates | [StateMachine](./Docs/StateMachine/README.md) |
| 🧬 **Reactive** | Serializable reactive `float`/`int`/`bool` properties driving UI without glue | [Reactive](./Docs/Reactive/README.md) |
| 🃏 **Cards** | Deck/hand/board runtime, custom decks/views, poker, Drunkard sample | [Cards](./Docs/Cards/README.md) |
| ⬛ **GridSystem** | Field generation, placement, GridMerge, Dice, Match3, TicTacToe, SlidingMerge | [GridSystem](./Docs/GridSystem/README.md) |
| 🔀 **Merge** | Pure-C# connected-group merge engine for grids, inventories, lists, graphs | [Merge](./Docs/Merge/README.md) |
| 🎰 **Bonus** | Wheel of Fortune, slot machine, daily/cooldown rewards | [Bonus](./Docs/Bonus/README.md) |
| 🤺 **NPC** | Navigation, patrol, chase, animator driver, modular RPG-ready combat | [NPC](./Docs/NPC/README.md) |
| ✨ **UI** | Panels, button & toggle animations, `AnimationFly` reward motion | [UI](./Docs/UI/README.md) |
| **HTML → UITK** | HTML/CSS authoring, native controls, typed actions, adaptive/nav/anim specs and a limited static gradient bake | [Module](./Docs/UI/HtmlToUitk.md), [CLI](https://github.com/NeoXider/NeoxiderTools/blob/main/Tools/HtmlToUitk/README.md) |
| 🔊 **Audio** | Music/SFX manager, one-shot pooling, settings-bound volume | [Audio](./Docs/Audio/README.md) |
| 🌐 **Network** | Optional Mirror wrappers + no-code sync/action bridges + realtime .io toolkit (bootstrap, snapshots, prediction, telemetry) | [Multiplayer Guide](./Docs/Network/Multiplayer_Guide.md), [Realtime IO Guide](./Docs/Network/Realtime_IO_Guide.md) |
| 🖱 **Interaction** | C# contracts, hover/click, 2D/3D physics events, ray modes, and range checks | [Interaction](./Docs/Tools/InteractableObject/README.md) |
| 🧰 **Tools** | 80+ components: 3D character controller (slopes, stairs, platforms, 1st/3rd person), free-fly camera, physics, spawners, timers, input, text, view helpers | [Tools](./Docs/Tools/README.md) |
| ➕ **Extensions** | 200+ C# / Unity API extension methods | [Extensions](./Docs/Extensions/README.md) |

Full navigation lives in the [Docs index](./Docs/README.md); the [Project Summary](./PROJECT_SUMMARY.md) is a compact "what already exists, reuse it" map.

## HTML/CSS → UI Toolkit

The module converts a supported subset of HTML/CSS into native UITK controls: [runtime/editor, typed C# actions and lifecycle](./Docs/UI/HtmlToUitk.md), media width/height/aspect, nav/modal/tabs and animation specs. The [external CLI](https://github.com/NeoXider/NeoxiderTools/blob/main/Tools/HtmlToUitk/README.md) produces UXML/USS/localization and gradient PNGs with a manifest. Static gradient geometry has clear limits; arbitrary CSS/JS and full browser parity are not promised. Historical verification slice (2026-10-14): Unity 6000.3.14f1 EditMode — 239/239, Unity 6000.6.0f1 EditMode — 239/239 and PlayMode — 7/7; failures/skips — 0, exit0. [Checks and limits](https://github.com/NeoXider/NeoxiderTools/blob/main/Docs/Plans/HtmlToUitk-Verification-20261014.md), [remaining TODO](https://github.com/NeoXider/NeoxiderTools/blob/main/Docs/Plans/HtmlToUitk-TODO.md). These results do not verify the current 11.0.0 source; player/mobile and full browser parity are not claimed.

The UPM/manual package includes runtime/editor, but **not the CLI or its .NET DLL**. Conversion needs a full NeoxiderTools checkout, or a separately built CLI, .NET 9 and a configured external CLI path in the editor integration. From the root of a full checkout:

```powershell
dotnet publish Tools/HtmlToUitk/src/HtmlToUitk.Cli/HtmlToUitk.Cli.csproj -c Release -o Tools/HtmlToUitk/bin
dotnet Tools/HtmlToUitk/bin/htmltouitk.dll run Tools/HtmlToUitk/fixtures/production-neutral/neutral.htujob.json
```

The C# `preview`/`compare` commands honestly return nonzero unsupported; the working Python oracle preview/comparison commands are listed in the CLI docs. NuGet install is not required and not claimed.

## Samples

Import via `Package Manager ▸ NeoxiderTools ▸ Samples`. This checkout uses the hidden release source folder `Samples~`. Unity copies imported content to `Assets/Samples/NeoxiderTools/11.0.0/<sample name>/`, for example `Assets/Samples/NeoxiderTools/11.0.0/Demo Scenes/Scenes/SurvivorDemo.unity`.

| Package Manager sample | Source path | Purpose |
|------------------------|-------------|---------|
| **Demo Scenes** | `Samples~/Demo` | Integration and NoCode scenes, plus **`SurvivorDemo`**, configured by `SurvivorConfig` on `Neo.Abilities` ([guide](./Docs/Abilities/SurvivorDemo.md)). |
| **NeoxiderPages** | `Samples~/NeoxiderPages` | Optional page-navigation module (`PM`, `UIPage`, `BtnChangePage`, `UIKit`). |
| **Character Movement Fundamentals** | `Samples~/CharacterMovementFundamentals` | Character-controller scenes for slopes, stairs, platforms, gravity, and a top-down arena ([guide](./Docs/Tools/Move/CharacterController/README.md)). |

During sample development, `Samples/` replaces `Samples~/` so Unity imports the source assets. Release packaging restores `Samples~` and matching `package.json` sample paths. See [Sample scenes](./Docs/Samples.md) for all layouts and scene paths.

Imported copies from earlier versions remain separate project content. Review customized samples using the [migration guide](./Docs/Migration11.md).

## Games built with NeoxiderTools

| Game | Genres | Platform | Link | Notes |
|------|--------|----------|------|-------|
| **Fake Grandkids** *(Внуки понарошку)* | Arcade, Survival | Windows | [MyIndie](https://myindie.ru/games/game/fake-grandkids) | UralGameJam 2026; NeoCondition / Neoxider workflow |

## Tests

EditMode and PlayMode tests under `Tests/` cover save flows, resource/level math, abilities & combat, grid/bonus/UI behavior, networking, and NoCode bindings. Read the [EditMode and isolation guide](./Docs/Testing.md) and [PlayMode guide](./Docs/PlayModeTesting.md) for host prerequisites and scope. The host needs `com.unity.test-framework`; the broad suites also reference integrations such as Mirror. For UPM, `"com.neoxider.tools"` in the host manifest's `testables` list exposes package tests, while host prerequisites and supported test layouts still apply.

Static-reset and Play Mode transition fixtures run in an isolated test host because they can change global state, providers, and editor lifecycle. Current 11.0.0 verification is **UNVERIFIED**. After source preparation is complete, run `python scripts/check_package.py` from a full repository checkout for package integrity checks without Unity. This script is outside the installed UPM package. Record actual pass/fail/skip results for the tested checkout; the workflow badge and historical reports describe their own runs.

## Project layout

```text
Assets/Neoxider/
  Scripts/    Runtime modules (asmdef-separated)
  Editor/     Custom inspectors, windows, tooling
  Tests/      EditMode + PlayMode coverage
  Docs/       Documentation
  Samples~/   UPM sources (Demo, NeoxiderPages, CharacterMovementFundamentals)
  Prefabs/    Ready-to-use prefabs
  Resources/  Settings & package assets
```

`Samples/` is the alternative source location during sample development, replacing `Samples~/` until release packaging.

## Support

Found a bug or have an idea? Open an [issue](https://github.com/NeoXider/NeoxiderTools/issues) or PR. Behavior changes are tracked in the [Changelog](./CHANGELOG.md) and the relevant module docs.
