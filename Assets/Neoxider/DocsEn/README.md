# NeoxiderTools Docs (English)

This folder is the English entry point for `NeoxiderTools`.

Use it as the main navigation page for package modules, utility submodules, and optional samples.

## Start Here

- [Package README](../README.md)
- [Russian docs index](../Docs/README.md)
- [Tools index](./Tools/README.md)
- [Condition](./Condition/README.md)
- [Save](./Save/README.md)
- [UI](./UI/README.md)
- [GridSystem](./GridSystem.md)

## Coverage Notes

- Every top-level module has an English entry page in this tree.
- Selected deeper English pages are available for `Save`, `Tools/Managers`, `Tools/InteractableObject`, `Quest`, `UI`, `Shop`, `Cards`, `Progression`, and `Animations`.
- When a detailed page is still RU-only, this index keeps a direct link to the Russian documentation.
- Full coverage audit: [COVERAGE_AUDIT.md](./COVERAGE_AUDIT.md)

## Module Index

| Module | What it covers | English docs |
|--------|----------------|--------------|
| **Animations** | Float, color, and `Vector3` animation helpers | [`Animations/README.md`](./Animations/README.md) |
| **Audio** | Audio manager, mixer helpers, click playback, music helpers | [`Audio/README.md`](./Audio/README.md) |
| **Bonus** | Collections, slot systems, wheel rewards, time rewards | [`Bonus/README.md`](./Bonus/README.md) |
| **Cards** | Card game runtime, deck/hand/view flow, poker helpers | [`Cards/README.md`](./Cards/README.md) |
| **Condition** | Inspector-driven conditions and UnityEvent outputs | [`Condition/README.md`](./Condition/README.md) |
| **Editor** | Editor windows, maintenance tools, creators, build helpers | [`Editor/README.md`](./Editor/README.md) |
| **Extensions** | C# and Unity extension methods | [`Extensions/README.md`](./Extensions/README.md) |
| **GridSystem** | Grid generation, origin, pathfinding, Match3, TicTacToe | [`GridSystem.md`](./GridSystem.md) |
| **Level** | Level flow, buttons, scene progression helpers | [`Level/README.md`](./Level/README.md) |
| **NPC** | NPC navigation and animation helpers | [`NPC/README.md`](./NPC/README.md) |
| **Parallax** | Parallax layers and related components | [`Parallax/README.md`](./Parallax/README.md) |
| **PropertyAttribute** | Inspector attributes, validation, inject helpers | [`PropertyAttribute/README.md`](./PropertyAttribute/README.md) |
| **Quest** | Quest configs, manager, triggers, quest state flow | [`Quest/README.md`](./Quest/README.md) |
| **Progression** | XP, levels, unlock tree, perk tree, and persistent progression | [`Progression/README.md`](./Progression/README.md) |
| **RPG** | Persistent profile with `Auto Save`, combat actors, melee/ranged/aoe attacks, target selectors, presets for AI/skills/spells, built-in input, evade, buffs, and statuses | [`Rpg/README.md`](./Rpg/README.md) |
| **Reactive** | Serializable reactive properties for `float`, `int`, `bool` | [`Reactive/README.md`](./Reactive/README.md) |
| **Save** | Provider API, scene saves, global save data | [`Save/README.md`](./Save/README.md) |
| **Shop** | Shop controller, money flow, purchase UI helpers | [`Shop/README.md`](./Shop/README.md) |
| **StateMachine** | Runtime state machine and no-code editor flow | [`StateMachine/README.md`](./StateMachine/README.md) |
| **Tools** | Inventory, movement, spawner, dialogue, input, time, view, misc utilities | [`Tools/README.md`](./Tools/README.md) |
| **UI** | Buttons, pages, visual toggles, presentation helpers | [`UI/README.md`](./UI/README.md) |

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
