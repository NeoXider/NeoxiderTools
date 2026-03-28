# NeoxiderTools

`NeoxiderTools` is a Unity package with ready-to-use gameplay systems, no-code components, UI helpers, editor utilities, and sample scenes for rapid prototyping and production workflows.

**Version 7.7.25** · Unity 2022.1+ · Namespace `Neo`

- [GitHub](https://github.com/NeoXider/NeoxiderTools)
- [Documentation Index](./Docs/README.md)
- [English Docs Entry](./DocsEn/README.md)
- [Changelog](./CHANGELOG.md)
- [Project Summary](./PROJECT_SUMMARY.md)

## What You Get

- **RPG module** — full RPG runtime with persistent player profile (`RpgStatsManager`), scene combat actors (`RpgCombatant`), HP/levels/buffs/statuses, melee/ranged/aoe attacks (`RpgAttackController` + `RpgAttackDefinition` + `RpgProjectile`), target selectors and attack presets for AI/skills/spells, evade (`RpgEvadeController`), built-in configurable input, and no-code bridges.
- No-code gameplay building blocks such as `NeoCondition`, `Counter`, timers, interaction handlers, and UnityEvent-driven components.
- Reusable runtime modules for inventory, save/load, dialogue, grid systems, cards, shop, progression, state machine, modular NPC navigation/combat composition, audio, and UI.
- Editor helpers, package samples, prefabs, and module-focused documentation.

## Installation

### Unity Package Manager (Git URL)

```text
https://github.com/NeoXider/NeoxiderTools.git?path=Assets/Neoxider
```

Open `Window > Package Manager > + > Add package from git URL` and paste the URL above.

### Manual Install

Copy `Assets/Neoxider` into your Unity project.

## Dependencies

### Installed automatically through UPM

- `com.unity.textmeshpro`
- `com.unity.ai.navigation`

### Required dependencies

- `UniTask` for async-heavy modules such as cards, dialogue, and typed text workflows.
- `DOTween` is required for the package runtime modules.
- `DOTween Pro` is required for the `NeoxiderPages` sample module.
- `Spine Unity Runtime` only if you use Spine-specific integrations.
- `Odin Inspector` is optional. Components work without it.

### Optional docs viewer

For enhanced markdown rendering inside the Inspector you can install `MarkdownRenderer`:

```text
https://github.com/NeoXider/MarkdownRenderer.git
```

Without it, the package still works. Inspector documentation fallbacks remain available.

## Quick Start

1. Import the package by UPM or by copying `Assets/Neoxider`.
2. Add `Assets/Neoxider/Prefabs/--System--.prefab` if your scene uses the built-in managers/UI bootstrap.
3. Add components through `Add Component > Neoxider`.
4. Open the module guide in [Docs/README.md](./Docs/README.md) and start from the module you need.

## Featured Modules

| Module | What it covers | Docs |
|--------|-----------------|------|
| **RPG** | Persistent player profile, scene combatants, HP, levels, buffs, statuses, melee/ranged/aoe, evade, target selectors, attack presets, and no-code bridges | [RPG](./Docs/Rpg/README.md) |
| **Progression** | XP, levels, unlock tree, perk tree, and persistent progression | [Progression](./Docs/Progression/README.md) |
| **Condition** | No-code conditions, field checks, AND/OR logic, and UnityEvent outputs | [NeoCondition](./Docs/Condition/NeoCondition.md) |
| **Tools** | 150+ components for movement, physics, spawners, timers, input, and utility runtime | [Tools](./Docs/Tools/README.md) |
| **Quest** | Quest configs, goals, manager, and runtime quest state | [Quest](./Docs/Quest/README.md) |
| **Reactive** | Serializable reactive properties for `float`, `int`, and `bool` | [Reactive](./Docs/Reactive/README.md) |
| **Save** | `PlayerPrefs`, JSON files, provider-based save flow, scene/global data, save attributes | [Save](./Docs/Save/README.md) |
| **UI** | UI panels, button animations, toggles, and presentation helpers | [UI](./Docs/UI/README.md) |
| **Cards** | MVP architecture, poker, "Drunkard", and deck/hand runtime flow | [Cards](./Docs/Cards/README.md) |
| **GridSystem** | Grid generation, origin anchor, pathfinding, Match3, and TicTacToe | [GridSystem](./Docs/GridSystem.md) |
| **NPC** | NPC navigation, patrol, chase, animator driver, and modular RPG-ready combat | [NPC](./Docs/NPC/README.md) |
| **Editor** | Settings windows, missing-script finder, auto-build, and maintenance tools | [Editor](./Docs/Editor/README.md) |

## Samples

Import samples via `Package Manager > Neoxider Tools > Samples`.

| Sample | Path | Purpose |
|--------|------|---------|
| **Demo Scenes** | `Assets/Neoxider/Samples~/Demo/` | Integration scenes for core modules and gameplay features |
| **NeoxiderPages** | `Assets/Neoxider/Samples~/NeoxiderPages/` | Page-navigation sample module (`PM`, `UIPage`, `BtnChangePage`, `UIKit`) — requires `DOTween Pro` |

## Documentation Notes

- The canonical user-facing navigation lives in [Docs/README.md](./Docs/README.md).
- English onboarding starts in [DocsEn/README.md](./DocsEn/README.md).
- English coverage includes module entry pages plus selected deeper pages for `Save`, `Tools/Managers`, `Tools/InteractableObject`, `Quest`, `UI`, `Shop`, `Cards`, and `Progression`.
- Internal backlog and maintainer-only notes are intentionally not part of the main user docs index.

## Tests

- Baseline `EditMode` tests live in `Assets/Neoxider/Editor/Tests/`.
- The package currently includes coverage for save flows, level helpers, bootstrap order, legacy visibility rules, and RPG combat/runtime behavior.

## Project Layout

```text
Assets/Neoxider/
  Scripts/       # Runtime modules and asmdef-separated code
  Editor/        # Editor tooling
    Tests/       # EditMode tests for package runtime/editor-critical flows
  Docs/          # User-facing documentation (RU)
  DocsEn/        # English onboarding and mirrored docs
  Samples~/      # UPM samples
  Prefabs/       # Ready-to-use prefabs
  Resources/     # Settings and assets
```

## Support

If you find a bug or want to suggest an improvement, open an [issue](https://github.com/NeoXider/NeoxiderTools/issues) or PR in the main repository.
