# NeoxiderTools Docs (English)

This folder is the English entry point for `NeoxiderTools`.

Use it as the main navigation page for package modules, utility submodules, and optional samples.

## Start Here

- [Package README](../README.md)
- [No-Code UI binding (Neo.NoCode)](../Docs/NoCode/README.md) — `NoCodeBindText`, `SetProgress` (RU)
- [Russian docs index](../Docs/README.md) (includes [No-Code strategy / non-goals for visual scripting](../Docs/NO_CODE_AUDIT.md))
- [Tools index](./Tools/README.md)
- [Condition](./Condition/README.md)
- [Save](./Save/README.md)
- [UI](./UI/README.md)
- [GridSystem](./GridSystem.md)

## Coverage Notes

- Every top-level module has an English entry page in this tree.
- Selected deeper English pages are available for `Save`, `Tools/Managers`, `Tools/InteractableObject`, `Quest`, `UI`, `Shop`, `Cards`, `Progression`, and `Animations`.
- When a detailed page is still RU-only, this index keeps a direct link to the Russian documentation.
- Full coverage audit documents are **local-only**: place files such as `COVERAGE_AUDIT.md` under `Local/Audits/` at the repository root (ignored by git). See [`/Local/README.md`](../../../Local/README.md).

## Module Index

| Module | What it covers | English docs |
|--------|----------------|--------------|
| **NoCode** | Bind floats to `SetText`/TMP and Slider/Image via reflection-safe resolver (RU hub) | [`../Docs/NoCode/README.md`](../Docs/NoCode/README.md) |
| **Animations** | Float, Color, and `Vector3` animation helpers | [`Animations/README.md`](./Animations/README.md) |
| **Audio** | `AudioManager`, mixer helpers, random music, and audio utilities | [`Audio/README.md`](./Audio/README.md) |
| **Bonus** | Slots, wheel rewards, collections, and timed rewards | [`Bonus/README.md`](./Bonus/README.md) |
| **Cards** | MVP architecture, poker, "Drunkard", and card runtime flow | [`Cards/README.md`](./Cards/README.md) |
| **Condition** | No-code conditions, field checks, AND/OR logic, and event outputs | [`Condition/README.md`](./Condition/README.md) |
| **Editor** | Settings windows, missing-script finder, auto-build, and maintenance tools | [`Editor/README.md`](./Editor/README.md) |
| **Extensions** | 300+ extension methods for C# and Unity APIs | [`Extensions/README.md`](./Extensions/README.md) |
| **GridSystem** | Grid generation, origin anchor, pathfinding, Match3, and TicTacToe | [`GridSystem.md`](./GridSystem.md) |
| **Level** | Level manager, scene progression, and level map flow | [`Level/README.md`](./Level/README.md) |
| **NPC** | NPC navigation, patrol, chase, animator driver, and modular RPG-ready combat | [`NPC/README.md`](./NPC/README.md) |
| **Parallax** | Parallax layers and related visual components | [`Parallax/README.md`](./Parallax/README.md) |
| **PropertyAttribute** | `[Button]`, `[GUIColor]`, inject attributes, and inspector helpers | [`PropertyAttribute/README.md`](./PropertyAttribute/README.md) |
| **Quest** | Quest configs, manager, objectives, and runtime quest state | [`Quest/README.md`](./Quest/README.md) |
| **Progression** | XP, levels, unlock tree, perk tree, and persistent progression | [`Progression/README.md`](./Progression/README.md) |
| **RPG** | HP, levels, buffs, statuses, melee/ranged/aoe, evade, target selectors, attack presets, and no-code bridges | [`Rpg/README.md`](./Rpg/README.md) |
| **Reactive** | Serializable reactive properties for `float`, `int`, and `bool` | [`Reactive/README.md`](./Reactive/README.md) |
| **Save** | `PlayerPrefs`, JSON files, provider API, scene/global saves, and save attributes | [`Save/README.md`](./Save/README.md) |
| **Settings** | `GameSettings`, scene service, `SettingsView` UI | [`Settings/README.md`](./Settings/README.md) |
| **Shop** | Shop flow, currency, and purchases | [`Shop/README.md`](./Shop/README.md) |
| **StateMachine** | Code + no-code runtime state machine with visual editor workflow | [`StateMachine/README.md`](./StateMachine/README.md) |
| **Tools** | 150+ components for movement, physics, spawners, timers, input, and utilities | [`Tools/README.md`](./Tools/README.md) |
| **UI** | UI panels, button animations, toggles, and presentation helpers | [`UI/README.md`](./UI/README.md) |

## `Tools` submodules

- [Components](./Tools/Components/README.md)
- [Dialogue](./Tools/Dialogue/README.md)
- [Input](./Tools/Input/README.md)
- [Inventory](./Tools/Inventory/README.md)
- [InteractableObject](./Tools/InteractableObject/README.md)
- [Managers](./Tools/Managers/README.md)
- [Move](./Tools/Move/README.md)
- [Physics](./Tools/Physics/README.md)
- [Random](./Tools/Random/README.md)
- [Spawner](./Tools/Spawner/README.md)
- [Text](./Tools/Text/README.md)
- [Time](./Tools/Time/README.md)
- [View](./Tools/View/README.md)
- [Debug](./Tools/Debug/README.md)
- [Draw](./Tools/Draw/README.md)
- [FakeLeaderboard](./Tools/FakeLeaderboard/README.md)
- [Other](./Tools/Other/README.md)

## Samples and add-ons

- [NeoxiderPages](./NeoxiderPages/README.md)
- [UI Extension](./UI%20Extension/README.md)

## Notes

- Inspector-integrated documentation via `[NeoDoc("...")]` still points to the canonical Russian `Docs/` tree.
- English pages mirror the Russian structure where possible, but some deep pages may still be RU-only.
