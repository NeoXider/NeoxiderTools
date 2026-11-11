<p align="center"><img src="https://raw.githubusercontent.com/NeoXider/NeoxiderTools/main/Images/readme/01_hero.svg" alt="NeoxiderTools: a gameplay-systems framework for Unity" width="100%" /></p>

# NeoxiderTools — a gameplay-systems framework for Unity

[![Version](https://img.shields.io/badge/version-12.5.0-blue)](./package.json) [![Unity](https://img.shields.io/badge/Unity-6000.0+-green)](./Docs/PackageCompatibility.md) [![Namespace](https://img.shields.io/badge/namespace-Neo-orange)](./Docs/README.md#namespace-policy) [![License](https://img.shields.io/badge/license-PolyForm%20Small%20Business-lightgrey)](./LICENSE.md) [![NoCode](https://img.shields.io/badge/NoCode-ready-8A54D6)](./Docs/NoCode/README.md)

A modular Unity C# library of ready game systems: abilities, shops, saving, quests, cards, grids, NPC AI, UI motion and more. Use the runtime APIs from code, add scene components, or wire them in the Inspector without code; multiplayer through Mirror is optional.

`SurvivorDemo` shows the result: a Vampire-Survivors-style NoCode game with seven weapons, built from library components and UnityEvents, with no game scripts.

[GitHub](https://github.com/NeoXider/NeoxiderTools) · [Documentation](./Docs/README.md) · [Changelog](./CHANGELOG.md) · [Reuse map](./PROJECT_SUMMARY.md) · [Upgrade from 11.x](./Docs/Migration12.md) · [Upgrade from 10.x](./Docs/Migration11.md)

> **Upgrading from 11.x?** Read [Upgrading to 12](./Docs/Migration12.md): the HTML → UI Toolkit converter moved to [its own package](https://github.com/NeoXider/HtmlToUitk), a few APIs became obsolete, and the demos are separate downloads.

**По-русски:** [установка](./Docs/GettingStarted.md#installation) · [C# API, компоненты и NoCode](./Docs/README.md) · [демо-сцены](./Docs/Samples.md) · [обновление с 11.x](./Docs/Migration12.md) · [обновление с 10.x](./Docs/Migration11.md).

---

## One package — four ways to build

<p align="center"><img src="https://raw.githubusercontent.com/NeoXider/NeoxiderTools/main/Images/readme/04_four_ways.svg" alt="No code, clean C# APIs, optional multiplayer, agent-friendly" width="100%" /></p>

| | Who | What you get |
|---|-----|--------------|
| 🧩 | **Beginners — zero code** | Gameplay from Inspector components: `NeoCondition`, counters, timers, shops, wheels, slots, equipment, page navigation, click sounds. Start with the [NoCode guide](./Docs/NoCode/GettingStarted.md). |
| 🛠 | **Programmers — clean C# APIs** | Runtime models and services you own from C#, plus 200+ `Neo.Extensions` helpers. Start with the [C# quick start](./Docs/GettingStarted.md#start-with-a-c-runtime-model). Scene singletons such as `AM.I` are optional. |
| 🌐 | **Multiplayer, optional** | [Mirror](https://github.com/MirrorNetworking/Mirror) bridges (`NetworkPropertySync`, `NetworkReactiveSync`, `NetworkActionRelay`) with a solo-mode path, and one-button Quick Play. |
| 🤖 | **AI-agent friendly** | A bundled [agent skill](./Skill/neoxider-tools/SKILL.md), `[NeoDoc]`-linked docs and a machine-readable [reuse map](./PROJECT_SUMMARY.md), so coding agents build on the package instead of rewriting it. |

---

<!-- demo-gallery:begin -->

## See it running

Captures taken in the Unity Editor (several in Play Mode) of scenes from the Demo download (`Assets/NeoxiderDemos/Demo`).

<table>
<tr>
<td width="33%" align="center"><a href="./Docs/Abilities/README.md"><img src="https://raw.githubusercontent.com/NeoXider/NeoxiderTools/main/Images/demos/SurvivorDemo.jpg" alt="Survivor arena: Seven weapons, waves and a boss, no game scripts" width="100%" /></a><br /><b>Survivor arena</b><br /><sub>Seven weapons, waves and a boss, no game scripts</sub></td>
<td width="33%" align="center"><a href="./Docs/GridSystem/README.md"><img src="https://raw.githubusercontent.com/NeoXider/NeoxiderTools/main/Images/demos/GridSystemMatch3Demo.jpg" alt="Match3 board: Shaped boards, paths and match rules" width="100%" /></a><br /><b>Match3 board</b><br /><sub>Shaped boards, paths and match rules</sub></td>
<td width="33%" align="center"><a href="./Docs/Bonus/README.md"><img src="https://raw.githubusercontent.com/NeoXider/NeoxiderTools/main/Images/demos/BonusRewardsDemo.jpg" alt="Rewards and collection: Collection, chest and cooldown gifts, no script" width="100%" /></a><br /><b>Rewards and collection</b><br /><sub>Collection, chest and cooldown gifts, no script</sub></td>
</tr>
<tr>
<td width="33%" align="center"><a href="./Docs/Tools/Physics/README.md"><img src="https://raw.githubusercontent.com/NeoXider/NeoxiderTools/main/Images/demos/Physics3DDemo.jpg" alt="Physics 3D: Magnet, impulse zone and explosive force" width="100%" /></a><br /><b>Physics 3D</b><br /><sub>Magnet, impulse zone and explosive force</sub></td>
<td width="33%" align="center"><a href="./Docs/UI/README.md"><img src="https://raw.githubusercontent.com/NeoXider/NeoxiderTools/main/Images/demos/UIComponentsDemo.jpg" alt="UI components: Tabs, variants, loader, shake and pause" width="100%" /></a><br /><b>UI components</b><br /><sub>Tabs, variants, loader, shake and pause</sub></td>
<td width="33%" align="center"><a href="./Docs/Cards/README.md"><img src="https://raw.githubusercontent.com/NeoXider/NeoxiderTools/main/Images/demos/CardGame.jpg" alt="Card game: Deck, hand and board components" width="100%" /></a><br /><b>Card game</b><br /><sub>Deck, hand and board components</sub></td>
</tr>
<tr>
<td width="33%" align="center"><a href="./Docs/Bonus/README.md"><img src="https://raw.githubusercontent.com/NeoXider/NeoxiderTools/main/Images/demos/SlotExample.jpg" alt="Slot machine: Bets, paylines and a paytable" width="100%" /></a><br /><b>Slot machine</b><br /><sub>Bets, paylines and a paytable</sub></td>
<td width="33%" align="center"><a href="./Docs/Shop/README.md"><img src="https://raw.githubusercontent.com/NeoXider/NeoxiderTools/main/Images/demos/Shop.jpg" alt="Shop: Storefront, purchase states and a wallet" width="100%" /></a><br /><b>Shop</b><br /><sub>Storefront, purchase states and a wallet</sub></td>
<td width="33%" align="center"><a href="./Docs/UI/README.md"><img src="https://raw.githubusercontent.com/NeoXider/NeoxiderTools/main/Images/demos/AnimationFlyDemo.jpg" alt="Reward fly: Coins flying from world, canvas and screen" width="100%" /></a><br /><b>Reward fly</b><br /><sub>Coins flying from world, canvas and screen</sub></td>
</tr>
<tr>
<td width="33%" align="center"><a href="./Docs/NoCode/README.md"><img src="https://raw.githubusercontent.com/NeoXider/NeoxiderTools/main/Images/demos/NoCodeActionsDemo.jpg" alt="NoCode actions: Level, Progression, Quest and RPG without code" width="100%" /></a><br /><b>NoCode actions</b><br /><sub>Level, Progression, Quest and RPG without code</sub></td>
<td width="33%" align="center"><a href="./Docs/Progression/README.md"><img src="https://raw.githubusercontent.com/NeoXider/NeoxiderTools/main/Images/demos/Progression_Demo.jpg" alt="Progression: XP, perk and unlock trees, premium track" width="100%" /></a><br /><b>Progression</b><br /><sub>XP, perk and unlock trees, premium track</sub></td>
<td width="33%" align="center"><a href="./Docs/UI/README.md"><img src="https://raw.githubusercontent.com/NeoXider/NeoxiderTools/main/Images/demos/UIMeshRigDemo.jpg" alt="UI Mesh Rig: One rig in uGUI, UI Toolkit and world space" width="100%" /></a><br /><b>UI Mesh Rig</b><br /><sub>One rig in uGUI, UI Toolkit and world space</sub></td>
</tr>
</table>

<!-- demo-gallery:end -->

## Highlight reel

- ⚔️ **Dota-style Abilities with a visual Ability Designer.** Abilities, modifiers and units are data assets, resolved by one cast pipeline with typed results and multiplayer-ready receipts.
- 🎚️ **Scalable ability parameters.** Cooldown, cost, range, charges, radius, duration and modifier strength scale with ability level, stats or the resource a cast spent.
- 🧛 **A survivor game with no game scripts.** `SurvivorDemo` wires seven weapons, weapon levels, waves, a boss and XP cards from library components; start your own survivor-like from it.
- 🧩 **NoCode that builds whole games.** Study the reference scenes `adventure`, `Shooter2D`, `ClickerExample` and `DrawExample`.
- 🔍 **Demo scenes you can read in the Inspector.** Each demo is saved with its UI and UnityEvent wiring, so you see how a button reaches a module API without entering Play Mode.
- 🧪 **Testable cores.** Plain C# rules, injectable clocks (`IFrameClock`, `INeoClock`), seeded shuffles and exact integer odds (`IntegerChanceTable`) make games reproducible in tests, replays and servers.
- 🟢 **A living inspector.** The Neoxider header reflects each component's health: console errors per component, missing references, NaN/∞ fields, and the Game view in Play Mode.
- 🎁 **Juice out of the box.** `AnimationFly` sends coins and gems to the HUD with fountain, magnet and scatter presets, next to tweened toggles and buttons, parallax and audio managers.
- 🧰 **200+ drop-in components.** Movement, cameras, spawners, timers and more; browse them in the **Create Neoxider Object** window, whose entries depend on the installed optional modules and demos.

---

## Requirements

- **Unity 6000.0+** (Unity 6); see [compatibility](./Docs/PackageCompatibility.md).
- **Installed by UPM:** `com.unity.ugui` (ships TextMeshPro on Unity 6), `com.unity.ai.navigation`, `com.unity.inputsystem`.
- **Required in your project:** [DOTween](https://assetstore.unity.com/packages/tools/animation/dotween-hotween-v2-27676) and [UniTask](https://github.com/Cysharp/UniTask).
- **Optional:** `Mirror` (multiplayer), `DOTween Pro`, `Odin Inspector`, `Spine Runtime`, [`MarkdownRenderer`](https://github.com/NeoXider/MarkdownRenderer), [HtmlToUitk](https://github.com/NeoXider/HtmlToUitk) (HTML/CSS → UI Toolkit), the [mobile haptics backend](./Docs/Haptics/Haptics.md), URP (only for URP-specific rendering or 2D lights). NeoxiderPages imports without DOTween Pro.
- **Demos:** TMP Essential Resources and `com.unity.2d.sprite` ([demo prerequisites](./Docs/Samples.md#host-and-sample-prerequisites)).

Character Movement Fundamentals runtime and editor code is bundled in `ThirdParty/CharacterMovementFundamentals`; its demo scenes are a separate download. License and dependency details: [THIRD-PARTY-NOTICES.md](./THIRD-PARTY-NOTICES.md).

## Install

**Package Manager → `+` → Add package from git URL:**

```text
https://github.com/NeoXider/NeoxiderTools.git#upm
```

Then install DOTween from the Asset Store (run `Tools > Demigiant > DOTween Utility Panel`) and UniTask:

```text
https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask#2.5.11
```

The `upm` branch holds only the package, a few megabytes. Pinning a version, the local-package route, a manual copy of `Assets/Neoxider` and the `NeoxiderTools.v<ver>.unitypackage` archive are described in [Getting Started](./Docs/GettingStarted.md#installation).

## Quick start

1. Install NeoxiderTools, DOTween and UniTask (above).
2. Choose the entry point for the feature you are building:

| Workflow | First step |
|----------|------------|
| **C# runtime API** | The [runtime-model quick start](./Docs/GettingStarted.md#start-with-a-c-runtime-model), then [Core](./Docs/Core/README.md), [Merge](./Docs/Merge/README.md) or [StateMachine](./Docs/StateMachine/README.md). |
| **Scene components** | The [first audio scene](./Docs/GettingStarted.md#your-first-scene-in-5-minutes-audio-manager-am). Add components from `Add Component ▸ Neoxider` or `Neoxider ▸ Windows ▸ Create Neoxider Object`, then assign their dependencies. |
| **Inspector / NoCode** | [NoCode Getting Started](./Docs/NoCode/GettingStarted.md). Keep scene references on components and reusable configuration in assets. |

The package's `Prefabs/-System--.prefab` holds the existing manager bootstrap. Runtime models such as `ResourcePoolModel`, `MergeResolver` and `StateMachine<TState>` work without it.

**Multiplayer:** install Mirror, add `NeoNetworkManager` and a Transport, then enable `isNetworked` on the NoCode components that should replicate. Walkthrough: [Multiplayer Guide](./Docs/Network/Multiplayer_Guide.md).

---

## How it fits together

<p align="center"><img src="https://raw.githubusercontent.com/NeoXider/NeoxiderTools/main/Images/readme/02_layers.svg" alt="NeoxiderTools layers" width="100%" /></p>

Start with a module's runtime contract, then add scene wrappers, views or NoCode helpers where the feature needs them. Assembly definitions declare each module's dependencies.

<p align="center"><img src="https://raw.githubusercontent.com/NeoXider/NeoxiderTools/main/Images/readme/03_module_shape.svg" alt="Every module has the same shape" width="100%" /></p>

## Module map

| | Module | What it covers | Docs |
|:-:|--------|-----------------|------|
| <img src="https://raw.githubusercontent.com/NeoXider/NeoxiderTools/main/Images/modules/thumbs/abilities.jpg" alt="Abilities module concept art" width="96" /> | ⚔️ **Abilities** | Data-driven Dota-style abilities and modifiers: buffs, DoT, shields, a typed cast pipeline, level/stat/resource scaling, orbit, zone and chain behaviours, summons, a scene validator | [Abilities](./Docs/Abilities/README.md) |
| <img src="https://raw.githubusercontent.com/NeoXider/NeoxiderTools/main/Images/modules/thumbs/rpg.jpg" alt="RPG module concept art" width="96" /> | 🧙 **RPG** | `RpgCharacter`: resources, stats, level/XP, buffs, damage modifiers, melee/ranged/aoe, evade, no-code bridges *(superseded by Abilities)* | [RPG](./Docs/Rpg/README.md) |
| <img src="https://raw.githubusercontent.com/NeoXider/NeoxiderTools/main/Images/modules/thumbs/core.jpg" alt="Core module concept art" width="96" /> | ❤️ **Core** | Resource pools (HP/mana), health component, levels and XP curves | [Core](./Docs/Core/README.md) |
| <img src="https://raw.githubusercontent.com/NeoXider/NeoxiderTools/main/Images/modules/thumbs/progression.jpg" alt="Progression module concept art" width="96" /> | 📈 **Progression** | XP, levels, perk tree, unlock tree, premium track, persistent meta-progression | [Progression](./Docs/Progression/README.md) |
| <img src="https://raw.githubusercontent.com/NeoXider/NeoxiderTools/main/Images/modules/thumbs/condition.jpg" alt="Condition module concept art" width="96" /> | 🎛 **Condition** | No-code checks over fields, properties, methods and state, AND/OR, UnityEvent outputs | [Condition](./Docs/Condition/README.md) |
| <img src="https://raw.githubusercontent.com/NeoXider/NeoxiderTools/main/Images/modules/thumbs/nocode.jpg" alt="NoCode module concept art" width="96" /> | 🔗 **NoCode** | Bind component values to UI and actions, formatted text, progress, action bridges, each with a typed C# API | [NoCode](./Docs/NoCode/README.md) |
| <img src="https://raw.githubusercontent.com/NeoXider/NeoxiderTools/main/Images/modules/thumbs/save.jpg" alt="Save module concept art" width="96" /> | 💾 **Save** | PlayerPrefs and JSON providers, typed helpers incl. 64-bit values, save attributes, scene/global data | [Save](./Docs/Save/README.md) |
| <img src="https://raw.githubusercontent.com/NeoXider/NeoxiderTools/main/Images/modules/thumbs/settings.jpg" alt="Settings module concept art" width="96" /> | ⚙️ **Settings** | Game settings service with deferred saving and UI bindings | [Settings](./Docs/Settings/README.md) |
| <img src="https://raw.githubusercontent.com/NeoXider/NeoxiderTools/main/Images/modules/thumbs/shop.jpg" alt="Shop module concept art" width="96" /> | 🛒 **Shop** | Items, bundles, typed purchase results, owned/equipped, multi-currency, inventory bridge, save profile | [Shop](./Docs/Shop/README.md) |
| <img src="https://raw.githubusercontent.com/NeoXider/NeoxiderTools/main/Images/modules/thumbs/inventory.jpg" alt="Inventory module concept art" width="96" /> | 🎒 **Inventory** | Aggregated and slot-grid inventories, pickups, transfer rules, selection events, save integration | [Inventory](./Docs/Tools/Inventory/README.md) |
| <img src="https://raw.githubusercontent.com/NeoXider/NeoxiderTools/main/Images/modules/thumbs/quest.jpg" alt="Quest module concept art" width="96" /> | 🗺 **Quest** | Quest configs, goals, manager, kill/collect/reach/talk notifications, no-code actions, runtime state | [Quest](./Docs/Quest/README.md) |
| <img src="https://raw.githubusercontent.com/NeoXider/NeoxiderTools/main/Images/modules/thumbs/level.jpg" alt="Level module concept art" width="96" /> | 🚪 **Level** | Level manager, scene loading, level map flow | [Level](./Docs/Level/README.md) |
| <img src="https://raw.githubusercontent.com/NeoXider/NeoxiderTools/main/Images/modules/thumbs/statemachine.jpg" alt="StateMachine module concept art" width="96" /> | 🔁 **StateMachine** | Runtime FSM + no-code transition predicates | [StateMachine](./Docs/StateMachine/README.md) |
| <img src="https://raw.githubusercontent.com/NeoXider/NeoxiderTools/main/Images/modules/thumbs/reactive.jpg" alt="Reactive module concept art" width="96" /> | 🧬 **Reactive** | Serializable reactive `float`/`int`/`bool` properties that drive UI without glue code | [Reactive](./Docs/Reactive/README.md) |
| <img src="https://raw.githubusercontent.com/NeoXider/NeoxiderTools/main/Images/modules/thumbs/cards.jpg" alt="Cards module concept art" width="96" /> | 🃏 **Cards** | Deck/hand/board runtime, seeded shuffling, stable custom sorting, custom decks and views, poker, Drunkard example | [Cards](./Docs/Cards/README.md) |
| <img src="https://raw.githubusercontent.com/NeoXider/NeoxiderTools/main/Images/modules/thumbs/gridsystem.jpg" alt="GridSystem module concept art" width="96" /> | ⬛ **GridSystem** | Field generation, pathfinding and movement ranges, placement, GridMerge, Dice, Match3, TicTacToe / n-in-a-row, SlidingMerge (2048) | [GridSystem](./Docs/GridSystem/README.md) |
| <img src="https://raw.githubusercontent.com/NeoXider/NeoxiderTools/main/Images/modules/thumbs/merge.jpg" alt="Merge module concept art" width="96" /> | 🔀 **Merge** | Pure C# connected-group merge engine for grids, inventories, lists, graphs | [Merge](./Docs/Merge/README.md) |
| <img src="https://raw.githubusercontent.com/NeoXider/NeoxiderTools/main/Images/modules/thumbs/bonus.jpg" alt="Bonus module concept art" width="96" /> | 🎰 **Bonus** | Wheel of Fortune, slot machine, collections, daily/cooldown rewards, each with a plain C# core | [Bonus](./Docs/Bonus/README.md) |
| <img src="https://raw.githubusercontent.com/NeoXider/NeoxiderTools/main/Images/modules/thumbs/npc.jpg" alt="NPC module concept art" width="96" /> | 🤺 **NPC** | Navigation, patrol, chase, staggered re-pathing, animator driver, modular RPG-ready combat | [NPC](./Docs/NPC/README.md) |
| <img src="https://raw.githubusercontent.com/NeoXider/NeoxiderTools/main/Images/modules/thumbs/ui.jpg" alt="UI module concept art" width="96" /> | ✨ **UI** | Panels, button and toggle animations, `AnimationFly` reward motion, UI Mesh Rig | [UI](./Docs/UI/README.md) |
| <img src="https://raw.githubusercontent.com/NeoXider/NeoxiderTools/main/Images/modules/thumbs/animations.jpg" alt="Animations module concept art" width="96" /> | 🎞 **Animations** | Float, color, `Vector3` and transform animators with a manual clock | [Animations](./Docs/Animations/README.md) |
| <img src="https://raw.githubusercontent.com/NeoXider/NeoxiderTools/main/Images/modules/thumbs/audio.jpg" alt="Audio module concept art" width="96" /> | 🔊 **Audio** | Music/SFX manager, one-shot pooling, settings-bound volume and mute saved between launches | [Audio](./Docs/Audio/README.md) |
| <img src="https://raw.githubusercontent.com/NeoXider/NeoxiderTools/main/Images/modules/thumbs/haptics.jpg" alt="Haptics module concept art" width="96" /> | 📳 **Haptics** | Named vibration feels, settings switch, repeat throttle | [Haptics](./Docs/Haptics/Haptics.md) |
| <img src="https://raw.githubusercontent.com/NeoXider/NeoxiderTools/main/Images/modules/thumbs/parallax.jpg" alt="Parallax module concept art" width="96" /> | 🏞 **Parallax** | Camera-bound parallax layers with seamless recycling | [Parallax](./Docs/Parallax/README.md) |
| <img src="https://raw.githubusercontent.com/NeoXider/NeoxiderTools/main/Images/modules/thumbs/network.jpg" alt="Network module concept art" width="96" /> | 🌐 **Network** | Optional Mirror wrappers, no-code sync and action bridges, a realtime .io toolkit (bootstrap, snapshots, prediction, telemetry) | [Multiplayer Guide](./Docs/Network/Multiplayer_Guide.md), [Realtime IO Guide](./Docs/Network/Realtime_IO_Guide.md) |
| <img src="https://raw.githubusercontent.com/NeoXider/NeoxiderTools/main/Images/modules/thumbs/interaction.jpg" alt="Interaction module concept art" width="96" /> | 🖱 **Interaction** | C# contracts, hover/click, 2D/3D physics events, ray modes and range checks | [Interaction](./Docs/Tools/InteractableObject/README.md) |
| <img src="https://raw.githubusercontent.com/NeoXider/NeoxiderTools/main/Images/modules/thumbs/tools.jpg" alt="Tools module concept art" width="96" /> | 🧰 **Tools** | 80+ components: 3D character controller (slopes, stairs, platforms, 1st/3rd person), free-fly camera, physics, spawners, timers and clocks, random tables, dialogue, input, text, view helpers | [Tools](./Docs/Tools/README.md) |
| <img src="https://raw.githubusercontent.com/NeoXider/NeoxiderTools/main/Images/modules/thumbs/extensions.jpg" alt="Extensions module concept art" width="96" /> | ➕ **Extensions** | 200+ C# and Unity API extension methods, including seeded random helpers | [Extensions](./Docs/Extensions/README.md) |
| <img src="https://raw.githubusercontent.com/NeoXider/NeoxiderTools/main/Images/modules/thumbs/editor.jpg" alt="Editor module concept art" width="96" /> | 🛠 **Editor** | Living inspector, `[Button]` and inject attributes, Create Neoxider Object window, maintenance tools | [Editor](./Docs/Editor/README.md) |

Full navigation is in the [Docs index](./Docs/README.md); the [Project Summary](./PROJECT_SUMMARY.md) is a compact "what already exists" map.

## HTML/CSS → UI Toolkit

The converter is now the separate [HtmlToUitk](https://github.com/NeoXider/HtmlToUitk) package (`com.neoxider.htmltouitk`); see [Upgrading to 12](./Docs/Migration12.md#htmltouitk-moved-to-its-own-package).

## Demos

Demos are separate downloads on the [releases page](https://github.com/NeoXider/NeoxiderTools/releases): `NeoxiderTools.Demo`, `NeoxiderPages` and `NeoxiderTools.CharacterMovementDemo`. Install the package first; each demo imports into `Assets/NeoxiderDemos/<name>`. [Demo scenes](./Docs/Samples.md) lists every scene and its prerequisites. If you imported samples from 12.1 or earlier, read [Upgrading to 12](./Docs/Migration12.md#samples-and-demo-scenes) first.

## Games built with NeoxiderTools

| Game | Genres | Platform | Link | Notes |
|------|--------|----------|------|-------|
| **Fake Grandkids** *(Внуки понарошку)* | Arcade, Survival | Windows | [MyIndie](https://myindie.ru/games/game/fake-grandkids) | UralGameJam 2026; NeoCondition / Neoxider workflow |

## Tests

More than 5,000 EditMode cases plus the PlayMode suites live under `Tests/`. How to run them, host prerequisites and scope: [Testing](./Docs/Testing.md).

## Project layout

```text
Assets/Neoxider/
  Scripts/    Runtime modules (asmdef-separated)
  Editor/     Custom inspectors, windows, tooling
  Tests/      EditMode + PlayMode coverage
  Docs/       Documentation
  Prefabs/    Ready-to-use prefabs
  Resources/  Settings & package assets
```

Demo scenes live outside the package, in `Assets/NeoxiderDemos/` of the repository.

## License

Starting with 11.0.1, NeoxiderTools is released under the [PolyForm Small Business License 1.0.0](./LICENSE.md).
It is free for personal and non-commercial use and for small businesses: fewer than 100 people and less than
1,000,000 USD (2019) revenue in the prior tax year. Larger companies need a commercial license: contact
neoxider@gmail.com. Earlier releases keep the license they shipped with. Bundled third-party code keeps its own
license, see [third-party notices](./THIRD-PARTY-NOTICES.md).

## Support

Found a bug or have an idea? Open an [issue](https://github.com/NeoXider/NeoxiderTools/issues) or PR. Behaviour changes are tracked in the [Changelog](./CHANGELOG.md) and the relevant module docs.
