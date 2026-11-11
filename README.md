<div align="center">

# NeoxiderTools

**Build games from reusable Unity C# systems.**

A modular construction library: use runtime APIs directly, then add scene components and optional Inspector / NoCode workflows.

[![Version](https://img.shields.io/badge/version-12.4.0-blue)](Assets/Neoxider/package.json)
[![License](https://img.shields.io/badge/license-PolyForm%20Small%20Business-lightgrey)](Assets/Neoxider/LICENSE.md)
[![Unity](https://img.shields.io/badge/Unity-6000.0+-green)](Assets/Neoxider/Docs/PackageCompatibility.md)
[![Namespace](https://img.shields.io/badge/namespace-Neo-orange)](Assets/Neoxider/Docs/README.md#namespace-policy)
[![NoCode](https://img.shields.io/badge/NoCode-ready-8A54D6)](Assets/Neoxider/Docs/NoCode/README.md)

### [See it running](#-see-it-running) · [Install](#-install) · [Start](#quick-start) · [Modules](#-what-is-inside) · [Demos](#-demos) · [Documentation](Assets/Neoxider/Docs/README.md) · [Upgrade from 11.x](Assets/Neoxider/Docs/Migration12.md)

</div>

> **Upgrading from 11.x?** Read [Upgrading to 12](Assets/Neoxider/Docs/Migration12.md): the HTML → UI Toolkit converter moved to [its own package](https://github.com/NeoXider/HtmlToUitk), a few APIs became obsolete, and the demos are separate downloads. Coming from 10.x, read [Migration to 11](Assets/Neoxider/Docs/Migration11.md) first.

**По-русски:** [установка](Assets/Neoxider/Docs/GettingStarted.md#installation) · [C# API, компоненты и NoCode](Assets/Neoxider/Docs/README.md) · [демо-сцены](Assets/Neoxider/Docs/Samples.md) · [обновление с 11.x](Assets/Neoxider/Docs/Migration12.md) · [обновление с 10.x](Assets/Neoxider/Docs/Migration11.md).

<p align="center"><img src="Images/readme/01_hero-rendered-v1.png" alt="Concept render of independent grid, card, ability and resource modules joined into a reusable game toolkit" width="100%" /></p>

*Concept artwork for the construction library; not an Editor or gameplay capture.*

<!-- demo-gallery:begin -->

## 🎬 See it running

Captures taken in the Unity Editor (several in Play Mode) of scenes from the Demo download (`Assets/NeoxiderDemos/Demo`). Click a picture to open the module guide.

<table>
<tr>
<td width="33%" align="center"><a href="Assets/Neoxider/Docs/Abilities/README.md"><img src="Images/demos/SurvivorDemo.jpg" alt="Survivor arena: a NoCode game from library components" width="100%" /></a><br /><b>Survivor arena</b><br /><sub>Seven weapons, waves and a boss, no game scripts</sub></td>
<td width="33%" align="center"><a href="Assets/Neoxider/Docs/GridSystem/README.md"><img src="Images/demos/GridSystemMatch3Demo.jpg" alt="Match3 board: Shaped boards, paths and match rules" width="100%" /></a><br /><b>Match3 board</b><br /><sub>Shaped boards, paths and match rules</sub></td>
<td width="33%" align="center"><a href="Assets/Neoxider/Docs/Bonus/README.md"><img src="Images/demos/BonusRewardsDemo.jpg" alt="Rewards and collection: Collection, chest and cooldown gifts, no script" width="100%" /></a><br /><b>Rewards and collection</b><br /><sub>Collection, chest and cooldown gifts, no script</sub></td>
</tr>
<tr>
<td width="33%" align="center"><a href="Assets/Neoxider/Docs/Tools/Physics/README.md"><img src="Images/demos/Physics3DDemo.jpg" alt="Physics 3D: Magnet, impulse zone and explosive force" width="100%" /></a><br /><b>Physics 3D</b><br /><sub>Magnet, impulse zone and explosive force</sub></td>
<td width="33%" align="center"><a href="Assets/Neoxider/Docs/UI/README.md"><img src="Images/demos/UIComponentsDemo.jpg" alt="UI components: Tabs, variants, loader, shake and pause" width="100%" /></a><br /><b>UI components</b><br /><sub>Tabs, variants, loader, shake and pause</sub></td>
<td width="33%" align="center"><a href="Assets/Neoxider/Docs/Cards/README.md"><img src="Images/demos/CardGame.jpg" alt="Card game: Deck, hand and board components" width="100%" /></a><br /><b>Card game</b><br /><sub>Deck, hand and board components</sub></td>
</tr>
<tr>
<td width="33%" align="center"><a href="Assets/Neoxider/Docs/Bonus/README.md"><img src="Images/demos/SlotExample.jpg" alt="Slot machine: Bets, paylines and a paytable" width="100%" /></a><br /><b>Slot machine</b><br /><sub>Bets, paylines and a paytable</sub></td>
<td width="33%" align="center"><a href="Assets/Neoxider/Docs/Shop/README.md"><img src="Images/demos/Shop.jpg" alt="Shop: Storefront, purchase states and a wallet" width="100%" /></a><br /><b>Shop</b><br /><sub>Storefront, purchase states and a wallet</sub></td>
<td width="33%" align="center"><a href="Assets/Neoxider/Docs/UI/README.md"><img src="Images/demos/AnimationFlyDemo.jpg" alt="Reward fly: Coins flying from world, canvas and screen" width="100%" /></a><br /><b>Reward fly</b><br /><sub>Coins flying from world, canvas and screen</sub></td>
</tr>
<tr>
<td width="33%" align="center"><a href="Assets/Neoxider/Docs/NoCode/README.md"><img src="Images/demos/NoCodeActionsDemo.jpg" alt="NoCode actions: Level, Progression, Quest and RPG without code" width="100%" /></a><br /><b>NoCode actions</b><br /><sub>Level, Progression, Quest and RPG without code</sub></td>
<td width="33%" align="center"><a href="Assets/Neoxider/Docs/Progression/README.md"><img src="Images/demos/Progression_Demo.jpg" alt="Progression: XP, perk and unlock trees, premium track" width="100%" /></a><br /><b>Progression</b><br /><sub>XP, perk and unlock trees, premium track</sub></td>
<td width="33%" align="center"><a href="Assets/Neoxider/Docs/UI/README.md"><img src="Images/demos/UIMeshRigDemo.jpg" alt="UI Mesh Rig: One rig in uGUI, UI Toolkit and world space" width="100%" /></a><br /><b>UI Mesh Rig</b><br /><sub>One rig in uGUI, UI Toolkit and world space</sub></td>
</tr>
</table>

<!-- demo-gallery:end -->

## Quick start

1. [Install NeoxiderTools](#-install), plus DOTween and UniTask in the host project.
2. Choose one feature and one entry point:

| Start with | Use it for | Guide |
|------------|------------|-------|
| **C# runtime API** | Own models and services from your code, such as `ResourcePoolModel`, `MergeResolver`, `IntegerChanceTable` or `StateMachine<TState>`; add views when needed | [Runtime-model example](Assets/Neoxider/Docs/GettingStarted.md#start-with-a-c-runtime-model) |
| **Scene components** | Add a component from `Add Component > Neoxider` or `Neoxider > Windows > Create Neoxider Object`, then assign its dependencies | [First audio scene](Assets/Neoxider/Docs/GettingStarted.md#your-first-scene-in-5-minutes-audio-manager-am) |
| **Inspector / NoCode** | Connect values and events to the same typed APIs | [NoCode guide](Assets/Neoxider/Docs/NoCode/GettingStarted.md) |
| **A working demo** | Open a ready scene and inspect how it is wired | [Demo scenes](Assets/Neoxider/Docs/Samples.md) |
| **Multiplayer** | Install Mirror first, then follow the guide | [Multiplayer guide](Assets/Neoxider/Docs/Network/Multiplayer_Guide.md) |

3. Find the next building block in the [module map](#-what-is-inside). Keep reusable configuration in assets and scene references on components.

The package's `Prefabs/-System--.prefab` holds the existing manager bootstrap (`Assets/Neoxider/Prefabs/-System--.prefab` in a manual install); runtime models work without it. For an existing project, start with the [behaviour changes in 12](Assets/Neoxider/Docs/Migration12.md#behaviour-changes-to-review). Wider context: [package README](Assets/Neoxider/README.md), [reuse map](Assets/Neoxider/PROJECT_SUMMARY.md), [changelog](Assets/Neoxider/CHANGELOG.md).

<!-- whats-new:begin -->
## ⚡ What's new in 12

- **Faster where games spend their frames.** Measured in the Unity 6000.3 Editor ([full record](Docs/Audits/2026-11-02/Performance/MeasuredResults-12.0.md)):

  | Case | 11.x | 12.0 |
  |------|------|------|
  | 1,000 interactable objects, one frame | 3,366 ms | 5.5 ms |
  | Input read (8 keys + stick) | 141 µs, 68 allocations | 3.7 µs, 0 |
  | Match3 valid-swap search, 32×32 board | 74.5 ms, 4,978 allocations | 0.87 ms, 0 |
  | NoCode StateMachine, 128 states | 17.3 µs | 0.55 µs |
  | Card placement on a 128-card board | 113 µs per card | 15 µs per card |

- **Demo scenes you can read without Play Mode.** Every module demo is saved with its UI and UnityEvent wiring; four new scenes (Animations, UI Components, Bonus Rewards, Physics 3D) and info cards on the NoCode example games.
- **More NoCode targets.** One-argument UnityEvent methods where an enum or a second argument used to block wiring (`SetModeIndex`, `SetDirectionIndex`, `AnchorMove.SetX`, `Leaderboard.SubmitScore`, `Box.ResetProgress`, `CategoryBar.DisableEntry`).
- **Abilities as a constructor.** Cooldown, cost, range, charges, radius, duration and modifier strength scale with the ability level, with stats or with the resource a cast spent. Projectiles home, fly straight or orbit; a plain prefab becomes a projectile or a zone; cues, summons and outside code plug in as components ([Abilities](Assets/Neoxider/Docs/Abilities/README.md)).
- **A survivor game with no game scripts.** `SurvivorDemo` is assembled from library components and data: seven weapons with different behaviour, upgrade cards, waves and a boss ([how it is assembled](Assets/Neoxider/Docs/Abilities/SurvivorDemo.md)).
- **Hundreds of fixes**, each with a test: more than 5,800 EditMode and 1,000 PlayMode cases. See the [changelog](Assets/Neoxider/CHANGELOG.md) and [Upgrading to 12](Assets/Neoxider/Docs/Migration12.md).

<!-- whats-new:end -->

## 📑 Contents

<details>
<summary>Navigate the README</summary>

- [See it running](#-see-it-running)
- [Quick start](#quick-start)
- [What's new in 12](#-whats-new-in-12)
- [Install](#-install) and [requirements](#requirements)
- [Module map](#-what-is-inside)
- [How it fits together](#how-it-fits-together)
- [Four ways to build](#one-package--four-ways-to-build)
- [NoCode example](#no-code-example-neocondition)
- [Demos](#-demos)
- [HTML/CSS → UI Toolkit](#htmlcss--ui-toolkit)
- [Tests](#tests)
- [Project layout](#project-layout)
- [Games](#games-using-neoxidertools) and [support](#support)

</details>

## 🚀 Install

In Unity: `Window > Package Manager > + > Add package from git URL`.

```text
https://github.com/NeoXider/NeoxiderTools.git#upm
```

Then install the two required third-party packages:

- **DOTween:** from the [Unity Asset Store — DOTween (HOTween v2)](https://assetstore.unity.com/packages/tools/animation/dotween-hotween-v2-27676), then run `Tools > Demigiant > DOTween Utility Panel`.
- **UniTask:** add it from git URL:

  ```text
  https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask#2.5.11
  ```

The `upm` branch holds only the package, a few megabytes. Pinning a version, the local-package route for source development and a manual copy of `Assets/Neoxider` are in [Getting Started](Assets/Neoxider/Docs/GettingStarted.md#installation).

## Requirements

- Unity `6000.0+`.
- Installed by UPM with NeoxiderTools: `com.unity.ai.navigation`, `com.unity.inputsystem`, `com.unity.ugui` (which includes TextMeshPro on Unity 6).
- **Required in the host project:** [DOTween](https://assetstore.unity.com/packages/tools/animation/dotween-hotween-v2-27676) and [UniTask](https://github.com/Cysharp/UniTask).
- **Optional:** `DOTween Pro`, `Mirror`, `Spine Unity Runtime`, `Odin Inspector`, `MarkdownRenderer`, [HtmlToUitk](https://github.com/NeoXider/HtmlToUitk) (HTML/CSS → UI Toolkit) and the [mobile haptics backend](Assets/Neoxider/Docs/Haptics/Haptics.md). NeoxiderPages imports without DOTween Pro.
- URP is optional: the package does not depend on `com.unity.render-pipelines.universal`. Add it only for URP-specific rendering features or 2D lights.
- Demos also need **TMP Essential Resources** and `com.unity.2d.sprite` ([demo prerequisites](Assets/Neoxider/Docs/Samples.md#host-and-sample-prerequisites)).

Character Movement Fundamentals runtime and editor code is bundled under `ThirdParty/CharacterMovementFundamentals`; its demo scenes are a separate download. Licenses and external dependencies: [third-party notices](Assets/Neoxider/THIRD-PARTY-NOTICES.md). Pinned versions, Unity SDK compatibility, the DOTween Core/Pro upgrade constraint and the optional MCP setup: [Dependencies](Assets/Neoxider/Docs/Dependencies.md).

## 🧩 What is inside

26 module areas plus 17 Tools submodules, compiled as 54 runtime and 6 editor assemblies with explicit references, so a game pulls in only what it uses.

| | Area | What it solves | Docs |
|:-:|------|----------------|------|
| <img src="Images/modules/thumbs/abilities.jpg" alt="Abilities module concept art" width="96" /> | Abilities | Data-driven Dota-style abilities and modifiers: buffs, DoT, shields, a typed cast pipeline, level/stat/resource scaling, orbit, zone and chain behaviours, summons, a scene validator; successor to RPG | [Abilities](Assets/Neoxider/Docs/Abilities/README.md) |
| <img src="Images/modules/thumbs/rpg.jpg" alt="RPG module concept art" width="96" /> | RPG | Resources, stats, buffs, statuses, progression, combat, damage modifiers, targets (superseded by Abilities) | [RPG](Assets/Neoxider/Docs/Rpg/README.md) |
| <img src="Images/modules/thumbs/core.jpg" alt="Core module concept art" width="96" /> | Core | Resource pools, health, levels and XP curves | [Core](Assets/Neoxider/Docs/Core/README.md) |
| <img src="Images/modules/thumbs/condition.jpg" alt="Condition module concept art" width="96" /> | Condition | Inspector-driven checks over fields, properties, methods, GameObject state, AND/OR, events | [Condition](Assets/Neoxider/Docs/Condition/README.md) |
| <img src="Images/modules/thumbs/nocode.jpg" alt="NoCode module concept art" width="96" /> | NoCode | Bind component values to UI and actions without one-off view scripts; every component has a typed C# API | [NoCode](Assets/Neoxider/Docs/NoCode/README.md) |
| <img src="Images/modules/thumbs/shop.jpg" alt="Shop module concept art" width="96" /> | Shop | Items, bundles, typed purchase results, owned/equipped state, multi-currency, inventory bridge, save profile | [Shop](Assets/Neoxider/Docs/Shop/README.md) |
| <img src="Images/modules/thumbs/inventory.jpg" alt="Inventory module concept art" width="96" /> | Inventory | Aggregated and slot-grid inventories, pickups, transfer rules, selection events, save integration | [Inventory](Assets/Neoxider/Docs/Tools/Inventory/README.md) |
| <img src="Images/modules/thumbs/save.jpg" alt="Save module concept art" width="96" /> | Save | PlayerPrefs and JSON-backed providers, typed helpers including 64-bit values, save attributes | [Save](Assets/Neoxider/Docs/Save/README.md) |
| <img src="Images/modules/thumbs/settings.jpg" alt="Settings module concept art" width="96" /> | Settings | Game settings service, deferred saving, UI bindings | [Settings](Assets/Neoxider/Docs/Settings/README.md) |
| <img src="Images/modules/thumbs/progression.jpg" alt="Progression module concept art" width="96" /> | Progression | XP, levels, perk tree, unlock tree, premium track, persistent progression | [Progression](Assets/Neoxider/Docs/Progression/README.md) |
| <img src="Images/modules/thumbs/quest.jpg" alt="Quest module concept art" width="96" /> | Quest | Quest configs, goals, manager, kill/collect/reach/talk notifications, no-code actions, runtime state | [Quest](Assets/Neoxider/Docs/Quest/README.md) |
| <img src="Images/modules/thumbs/level.jpg" alt="Level module concept art" width="96" /> | Level | Level manager, scene loading, level map flow | [Level](Assets/Neoxider/Docs/Level/README.md) |
| <img src="Images/modules/thumbs/statemachine.jpg" alt="StateMachine module concept art" width="96" /> | StateMachine | Runtime state machine plus no-code transition predicates | [StateMachine](Assets/Neoxider/Docs/StateMachine/README.md) |
| <img src="Images/modules/thumbs/reactive.jpg" alt="Reactive module concept art" width="96" /> | Reactive | Serializable reactive `float`/`int`/`bool` properties | [Reactive](Assets/Neoxider/Docs/Reactive/README.md) |
| <img src="Images/modules/thumbs/merge.jpg" alt="Merge module concept art" width="96" /> | Merge | Pure C# connected-group merge engine for grids, inventories, lists and custom graphs | [Merge](Assets/Neoxider/Docs/Merge/README.md) |
| <img src="Images/modules/thumbs/gridsystem.jpg" alt="GridSystem module concept art" width="96" /> | GridSystem | Field generation, pathfinding and movement ranges, grid merge adapter, Dice, Match3, TicTacToe / n-in-a-row, SlidingMerge (2048) | [GridSystem](Assets/Neoxider/Docs/GridSystem/README.md) |
| <img src="Images/modules/thumbs/cards.jpg" alt="Cards module concept art" width="96" /> | Cards | Deck/hand/board runtime, seeded shuffling, stable custom sorting, custom decks and views, poker, Drunkard example | [Cards](Assets/Neoxider/Docs/Cards/README.md) |
| <img src="Images/modules/thumbs/bonus.jpg" alt="Bonus module concept art" width="96" /> | Bonus | Wheel of Fortune, slot machine, collections, daily/cooldown rewards, each with a plain C# core | [Bonus](Assets/Neoxider/Docs/Bonus/README.md) |
| <img src="Images/modules/thumbs/npc.jpg" alt="NPC module concept art" width="96" /> | NPC | Navigation, patrol, chase, animator driver, modular RPG-ready combat | [NPC](Assets/Neoxider/Docs/NPC/README.md) |
| <img src="Images/modules/thumbs/ui.jpg" alt="UI module concept art" width="96" /> | UI | UI panels, button animations, toggles, `AnimationFly` reward motion, UI Mesh Rig | [UI](Assets/Neoxider/Docs/UI/README.md) |
| <img src="Images/modules/thumbs/animations.jpg" alt="Animations module concept art" width="96" /> | Animations | Float, color, `Vector3` and transform animators with a manual clock | [Animations](Assets/Neoxider/Docs/Animations/README.md) |
| <img src="Images/modules/thumbs/audio.jpg" alt="Audio module concept art" width="96" /> | Audio | Music/SFX manager, one-shot pooling, settings-bound volume and mute that persist between launches | [Audio](Assets/Neoxider/Docs/Audio/README.md) |
| <img src="Images/modules/thumbs/haptics.jpg" alt="Haptics module concept art" width="96" /> | Haptics | Named vibration feels with a settings switch and repeat throttle | [Haptics](Assets/Neoxider/Docs/Haptics/Haptics.md) |
| <img src="Images/modules/thumbs/parallax.jpg" alt="Parallax module concept art" width="96" /> | Parallax | Camera-bound parallax layers with seamless recycling | [Parallax](Assets/Neoxider/Docs/Parallax/README.md) |
| <img src="Images/modules/thumbs/network.jpg" alt="Network module concept art" width="96" /> | Network | Optional Mirror wrappers, no-code network action and sync bridges, a realtime .io toolkit (bootstrap, snapshots, prediction, telemetry) | [Network](Assets/Neoxider/Docs/Network/README.md), [Realtime IO guide](Assets/Neoxider/Docs/Network/Realtime_IO_Guide.md) |
| <img src="Images/modules/thumbs/interaction.jpg" alt="Interaction module concept art" width="96" /> | Interaction | C# interaction contracts, hover/click, 2D/3D physics events, ray modes and range checks | [Interaction](Assets/Neoxider/Docs/Tools/InteractableObject/README.md) |
| <img src="Images/modules/thumbs/tools.jpg" alt="Tools module concept art" width="96" /> | Tools | Movement, free-fly camera, physics, timers and injectable clocks, exact-odds random tables, dialogue and bark lines, spawners, text, managers, view helpers | [Tools](Assets/Neoxider/Docs/Tools/README.md) |
| <img src="Images/modules/thumbs/extensions.jpg" alt="Extensions module concept art" width="96" /> | Extensions | 200+ C# and Unity API extension methods, including seeded random helpers | [Extensions](Assets/Neoxider/Docs/Extensions/README.md) |
| <img src="Images/modules/thumbs/editor.jpg" alt="Editor module concept art" width="96" /> | Editor | Living inspector, `[Button]` and inject attributes, creation windows, maintenance tools | [Editor](Assets/Neoxider/Docs/Editor/README.md) |

## How it fits together

<p align="center"><img src="Images/readme/02_layers-rendered-v1.png" alt="Concept render of foundation modules, gameplay modules, removable authoring adapters and optional network peers" width="100%" /></p>

**Foundation → gameplay → adapters.** Start with a module's runtime API, then add scene wrappers, views or NoCode helpers where your game needs them. Assembly definitions declare the dependencies; each module guide describes its own.

<p align="center"><img src="Images/readme/03_module-rendered-v1.png" alt="Concept render of a runtime core with removable scene and event adapters, documentation and a separate sample board" width="100%" /></p>

Time-dependent rules take an injectable clock (`IFrameClock`, `INeoClock`) and random rolls take a seed or source, so the same rules run in tests, replays and servers.

## One package — four ways to build

<p align="center"><img src="Images/readme/04_four_ways-rendered-v1.png" alt="Four concept workstations for C# APIs, component and Inspector authoring, optional multiplayer, and coding-agent guidance" width="100%" /></p>

*Left to right: C# APIs, component/Inspector authoring, optional multiplayer, and coding-agent guidance.*

- 🛠 **Programmers — clean C# APIs.** Use runtime models and services directly, with 200+ extension methods in `Neo.Extensions`. Scene singletons such as `AM.I` are optional.
- 🧩 **Components and optional NoCode.** Configure conditions, counters, timers, shops, slots, wheels, equipment, page navigation and click sounds in the Inspector.
- 🌐 **Multiplayer, if you need it.** Optional Mirror NoCode sync bridges (`NetworkPropertySync`, `NetworkReactiveSync`, `NetworkActionRelay`) with a solo-mode path, and one-button LAN Quick Play.
- 🤖 **AI-agent development.** A bundled [agent skill](Assets/Neoxider/Skill/neoxider-tools/SKILL.md), `[NeoDoc]`-linked docs and a machine-readable [reuse map](Assets/Neoxider/PROJECT_SUMMARY.md) let coding agents (Claude, Codex) build on the package instead of rewriting it.

## No-code example: NeoCondition

`NeoCondition` wires gameplay logic in the Inspector:

- Check fields, properties, GameObject state, or single-argument methods.
- Compare against constants or another object.
- Combine with `AND`, `OR` and inversion.
- On dispatch it emits `OnResult(result)` and `OnInvertedResult(!result)`, then `OnTrue` or `OnFalse`. **Only On Change** suppresses dispatch for an unchanged result.

Example: a condition calling `CanSpend(100f)` on a `Money` component can drive an affordability hint, while the purchase itself goes through the Shop API and the configured server authority. Read more: [NeoCondition docs](Assets/Neoxider/Docs/Condition/NeoCondition.md).

## 🎮 Demos

<!-- samples-shots:begin -->
<p align="center"><img src="Images/demos/SurvivorDemo.jpg" alt="SurvivorDemo: the survivor arena with waves and upgrades" width="49%" /> <img src="Images/demos/ShowcaseScene.jpg" alt="Character Movement Fundamentals showcase: slopes, stairs, platforms and gravity zones" width="49%" /></p>

*SurvivorDemo (Demo download) and the Character Movement Fundamentals showcase, captured in Play Mode.*
<!-- samples-shots:end -->

Demos are separate downloads on the [releases page](https://github.com/NeoXider/NeoxiderTools/releases): `NeoxiderTools.Demo`, `NeoxiderPages` and `NeoxiderTools.CharacterMovementDemo`. Install the package first; each demo imports into `Assets/NeoxiderDemos/<name>`, and this repository keeps them in the same folders. `SurvivorDemo` (`Assets/NeoxiderDemos/Demo/Scenes/SurvivorDemo.unity`) is a NoCode game with seven weapons and no game scripts ([guide](Assets/Neoxider/Docs/Abilities/SurvivorDemo.md)). Every scene, its prerequisites and how the scenes are made: [Demo scenes](Assets/Neoxider/Docs/Samples.md). Samples imported from 12.1 or earlier: see [Upgrading to 12](Assets/Neoxider/Docs/Migration12.md#samples-and-demo-scenes).

## HTML/CSS → UI Toolkit

The converter is now the separate [HtmlToUitk](https://github.com/NeoXider/HtmlToUitk) package (`com.neoxider.htmltouitk`); see [Upgrading to 12](Assets/Neoxider/Docs/Migration12.md#htmltouitk-moved-to-its-own-package).

## Tests

Package tests live under `Assets/Neoxider/Tests/`: `Edit` (`Neo.Editor.Tests`), `Play` (`Neo.Tests.Play`), `Fixtures`, `Integration` (Mirror network) and `Performance`. How to run them, host prerequisites and isolation rules: [Testing](Assets/Neoxider/Docs/Testing.md). Without Unity, `python scripts/check_package.py` checks package integrity.

## Project layout

```text
Assets/Neoxider/
  Scripts/       Runtime modules and asmdef-separated code
  Editor/        Custom inspectors, windows, and editor utilities
  Tests/         EditMode and PlayMode package tests
  Docs/          Documentation
  Prefabs/       Ready-to-use prefabs
  Resources/     Settings and package assets
  Skill/         Agent skill for coding assistants
Assets/NeoxiderDemos/  Demo scenes (Demo, NeoxiderPages, CharacterMovementFundamentals), separate downloads
dev-docs/        Maintainer documents (repository only, not in the package)
```

`python scripts/build_distribution.py upm` writes the package to the `upm` branch; `python scripts/build_distribution.py unitypackages` writes the release archives.

## Games using NeoxiderTools

| Game | Genre | Platform | Link | Notes |
|------|-------|----------|------|-------|
| Fake Grandkids | Arcade, Survival | Windows | [MyIndie](https://myindie.ru/games/game/fake-grandkids) | UralGameJam 2026; inspector-driven Neoxider workflow |

## License

Starting with 11.0.1, NeoxiderTools is released under the [PolyForm Small Business License 1.0.0](Assets/Neoxider/LICENSE.md).
It is free for personal and non-commercial use and for small businesses: fewer than 100 people and less than
1,000,000 USD (2019) revenue in the prior tax year. Larger companies need a commercial license: contact
neoxider@gmail.com. Earlier releases keep the license they shipped with. Bundled third-party code keeps its own
license, see [third-party notices](Assets/Neoxider/THIRD-PARTY-NOTICES.md).

## Support

Open an issue or pull request in the repository. Keep public behaviour changes documented in [CHANGELOG.md](Assets/Neoxider/CHANGELOG.md) and the relevant module docs.
