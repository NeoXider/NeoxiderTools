# NeoxiderTools

`NeoxiderTools` is a Unity package with ready-to-use gameplay systems, no-code components, UI helpers, editor utilities, and sample scenes for rapid prototyping and production workflows.

**Version 7.5.0** · Unity 2022.1+ · Namespace `Neo`

- [GitHub](https://github.com/NeoXider/NeoxiderTools)
- [Documentation Index](./Docs/README.md)
- [English Docs Entry](./DocsEn/README.md)
- [Changelog](./CHANGELOG.md)
- [Project Summary](./PROJECT_SUMMARY.md)

## What You Get

- No-code gameplay building blocks such as `NeoCondition`, `Counter`, timers, interaction handlers, and UnityEvent-driven components.
- Reusable runtime modules for inventory, save/load, dialogue, grid systems, cards, shop, state machine, NPC navigation, audio, and UI.
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

### Used by specific modules

- `UniTask` for async-heavy modules such as cards, dialogue, and typed text workflows.
- `DOTween` for modules with tween-based animation helpers.
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
| **Condition** | No-code rules, method calls with arguments, AND/OR logic, UnityEvent outputs | [NeoCondition](./Docs/Condition/NeoCondition.md) |
| **Tools** | Inventory, spawner, movement, dialogue, time, input, utility components | [Tools](./Docs/Tools/README.md) |
| **Save** | Scene saves, provider-based saves, global data | [Save](./Docs/Save/README.md) |
| **UI** | UI behaviors, animations, pages, toggles | [UI](./Docs/UI/README.md) |
| **Cards** | Decks, hands, presenter/view workflow, async animation | [Cards](./Docs/Cards/README.md) |
| **GridSystem** | Shape/origin/pathfinding grid workflows, Match3, TicTacToe | [GridSystem](./Docs/GridSystem.md) |
| **Editor** | Inspector extensions, builders, maintenance tools | [Editor](./Docs/Editor/README.md) |

## Samples

Import samples via `Package Manager > Neoxider Tools > Samples`.

| Sample | Path | Purpose |
|--------|------|---------|
| **Demo Scenes** | `Assets/Neoxider/Samples~/Demo/` | Integration scenes for core modules and gameplay features |
| **NeoxiderPages** | `Assets/Neoxider/Samples~/NeoxiderPages/` | Optional page-navigation sample module (`PM`, `UIPage`, `BtnChangePage`, `UIKit`) |

## Documentation Notes

- The canonical user-facing navigation lives in [Docs/README.md](./Docs/README.md).
- English onboarding starts in [DocsEn/README.md](./DocsEn/README.md).
- Internal backlog and maintainer-only notes are intentionally not part of the main user docs index.

## Project Layout

```text
Assets/Neoxider/
  Scripts/       # Runtime modules and asmdef-separated code
  Editor/        # Editor tooling
  Docs/          # User-facing documentation (RU)
  DocsEn/        # English onboarding and mirrored docs
  Samples~/      # UPM samples
  Prefabs/       # Ready-to-use prefabs
  Resources/     # Settings and assets
```

## Support

If you find a bug or want to suggest an improvement, open an [issue](https://github.com/NeoXider/NeoxiderTools/issues) or PR in the main repository.
