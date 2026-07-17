# NeoxiderTools Project Summary

A concise map of the `NeoxiderTools` package for developers, maintainers, and AI agents. Goal: quickly understand which ready-made runtime APIs, MonoBehaviour wrappers, sample scenes, and docs already exist before writing a new mechanic.

## Package Status

- **UPM package**: `Assets/Neoxider/package.json`
- **Current version**: `10.0.0`
- **Unity**: `6000.0+`
- **Main namespace**: `Neo`
- **Main user entry point**: [`README.md`](./README.md)
- **Main documentation index**: [`Docs/README.md`](./Docs/README.md)
- **English entry point**: [`Docs/README.md`](./Docs/README.md)
- **Package tests**: `Assets/Neoxider/Tests/`

## Structure

```text
Assets/Neoxider/
  Scripts/      # Runtime modules grouped by domain
  Editor/       # Editor tools and inspectors
  Tests/        # EditMode and PlayMode tests for package runtime/editor-critical flows
  Docs/         # English user-facing documentation
  Samples/      # Active development samples and smoke scenes
  Samples~/     # UPM sample source path before release packaging
  Prefabs/      # Ready-to-use prefabs
  Resources/    # Settings/assets
```

## Key Product Layers

- **Core gameplay modules**: `Core` (Level, Resources), `Cards`, `GridSystem`, `Merge`, `Save`, `Shop`, `StateMachine`, `NPC`, `UI`, `Progression`.
- **Abilities (v10 combat)**: `Neo.Abilities` — a data-driven, Dota-style ability/modifier system. Units with properties (`{ Add | Mul | Max }`, aggregated `add -> mul -> max`), modifiers that unify buffs/debuffs/DoT/shields/stuns under one lifecycle, data-authored abilities resolved through a validated cast pipeline (states, costs, cooldown/charges, targeting, range), multiplayer-ready event receipts, and a UITK Ability Designer. Supersedes `Rpg` in v10.
- **Grid games**: `FieldGenerator` for shape/coordinates/cell state/pathfinding, `GridPlacementEntry` / `GridPlacementResult` / `PlaceContentFootprint` for multi-cell placement, `GridMergeResolver` for connected-group merge, `DiceBoardService` for Dice Merge, plus the Match3, TicTacToe, and SlidingMerge layers.
- **Generic merge**: `Neo.Merge` - a pure C# connected-group merge engine with no dependency on Unity scene/Grid.
- **General-purpose tools**: `Tools/Inventory`, `Tools/Spawner`, `Tools/Move` (including `FreeFlyCameraController`), `Tools/Dialogue`, `Tools/Input`, `Tools/Time`.
- **No-code / Inspector workflows**: `Condition`, **`Neo.NoCode`** (`NoCodeBindText`, `SetProgress` -> TMP / Slider / Image), `PropertyAttribute`, UnityEvent-driven components.
- **Editor support**: custom inspectors, creation menus, maintenance windows.
- **Optional sample module**: `Samples~/NeoxiderPages`.

## Reuse-first map

Before implementing something new, check these ready-made building blocks:

| If you need | Already exists | Where to look |
|------------|----------|--------------|
| Grid, shape mask, coordinates, disabled/walkable/occupied cells | `FieldGenerator`, `GridShapeMask`, `FieldDebugDrawer` | [`Docs/GridSystem/README.md`](./Docs/GridSystem/README.md) |
| Multi-cell placement of shapes/items | `GridPlacementEntry`, `GridPlacementResult`, `FieldGenerator.CanPlaceContentFootprint`, `PlaceContentFootprint` | [`Docs/GridSystem/FieldGenerator.md`](./Docs/GridSystem/FieldGenerator.md) |
| Merging identical connected elements | `Neo.Merge.MergeResolver`, `MergeRequest<TItem,TValue>`, `GridMergeRequest.Increment(...)` | [`Docs/Merge/README.md`](./Docs/Merge/README.md) |
| Dice Merge / drop-and-merge on a grid | `DicePiece`, `DicePieceGenerator`, `DiceBoardService` | [`Docs/GridSystem/Dice/README.md`](./Docs/GridSystem/Dice/README.md) |
| Limited hands, stalls, draft tray, market row | `HandModel.Capacity`, `TryAdd(...)`, `RemainingCapacity`, `AddRangeUntilFull(...)` | [`Docs/Cards/README.md`](./Docs/Cards/README.md) |
| Match3, TicTacToe, 2048-like movement | `Match3BoardService`, `TicTacToeBoardService`, `SlidingMergeBoardService` | [`Docs/GridSystem/README.md`](./Docs/GridSystem/README.md) |
| Flying rewards/coins between world/canvas points | `AnimationFly.Play(AnimationFlyRequest)`, sprite/prefab visuals, reward timing, fountain/magnet/scatter motion presets | [`Docs/UI/AnimationFly.md`](./Docs/UI/AnimationFly.md) |
| Saving scene objects and global/profile data | `SaveManager`, `SaveProvider`, `GlobalSave`, `SaveableBehaviour` | [`Docs/Save/README.md`](./Docs/Save/README.md) |
| Money, shop, multi-currency | `Money`, `IMoneySpend`, `Shop`, `ShopItemData.CurrencyOverrideSaveKey` | [`Docs/Shop/README.md`](./Docs/Shop/README.md) |
| HP/Mana/resources, levels and XP | `HealthComponent`, `ResourcePoolModel`, `LevelComponent`, `LevelCurveDefinition` | [`Docs/Core/README.md`](./Docs/Core/README.md) |
| Data-driven abilities/modifiers: buffs, debuffs, DoT, shields, cast pipeline (v10 successor to Rpg) | `AbilityDefinition`, `ModifierDefinition`, `UnitTemplate`, `AbilityUnitBehaviour`, `AbilityCasterBehaviour` | [`Docs/Abilities/README.md`](./Docs/Abilities/README.md) |
| RPG combat, projectiles, buffs/statuses (superseded by Abilities in v10) | `RpgCharacter`, `RpgAttackController`, `RpgProjectile`, `RpgCombatMath` | [`Docs/Rpg/README.md`](./Docs/Rpg/README.md) |
| Inspector/no-code conditions and actions | `NeoCondition`, `ConditionEntryPredicate`, module NoCode bridges | [`Docs/Condition/README.md`](./Docs/Condition/README.md) |
| Debug/spectator movement, pooling, timers, input helpers | `Tools/Move`, `Tools/Spawner`, `Tools/Time`, `Tools/Input` | [`Docs/Tools/README.md`](./Docs/Tools/README.md) |

## Recent stabilization notes

- `GridSystem`, `Merge`, and `Dice` already have reusable placement/merge APIs, configurable dice rules, cascade-limit reporting, consistent board notifications, and an active Dice Merge sample.
- `DicePieceGenerator` supports `CreateDefaultPool()` for the original 1-5 merge pool, `CreateD6Pool()` for classic 1-6 faces, and `CreateSequentialPool(minValue, maxValue)` for custom numbered dice/progression pools.
- `Cards` supports finite runtime hands via `HandModel.Capacity`; `Capacity = 0` keeps the old unlimited behavior, while `TryAdd(...)` / `AddRangeUntilFull(...)` provide an error-free path for UI overflow flows.
- `AnimationFly` already supports typed request/result, prefab or sprite visuals, world/canvas coordinate conversion, pooling/disable-on-complete, reward timing callbacks, and reusable motion presets for fountain, magnet, fountain+magnet, and scatter reward effects.
- `SaveManager.Save()` preserves shared container read-modify-write and does not delete data of unloaded scene objects.
- `Core`/`RPG` fixes covered edge cases around XP-backed level sync, duplicate death/resource events, regen-from-zero, target resolution, projectile hits, buff stacks and persistence.
- `Shop`/`Money` reject negative spends and avoid optimistic client-only network success before authority confirms balance.

## Dependencies

### Via UPM

- `com.unity.textmeshpro`
- `com.unity.ai.navigation`
- `com.unity.inputsystem`
- `com.unity.ugui`

### By use case

- `UniTask` for async-heavy modules
- `DOTween` for tween-based animations and UI
- `Spine Unity Runtime` only for Spine integrations
- `MarkdownRenderer` optionally for improved `.md` viewing in the inspector
- `com.unity.render-pipelines.universal` only for projects that need URP-specific rendering features or 2D lights; the package no longer installs URP automatically

## Package development rules

- Check this summary and the docs index first: the needed building block often already exists, and a new mechanic can be assembled with an adapter instead of rewriting from scratch.
- Reuse existing modules and extend them instead of adding duplicates.
- When changing public behavior, update the module documentation and `CHANGELOG.md`.
- For new user-facing entry points, update `README.md` and `Docs/README.md` first.
- For multi-instance systems, prefer explicit references over implicit auto-find.
- Keep editor-only logic in `Editor/` or in separate `Editor` folders under the correct `asmdef`.
- **Module structure:** folders (Interfaces, Domain, Data, Components, Bridge, Runtime, Events, Enums), asmdef, namespaces, and the "one type - one file" principle are described in [MODULE_STRUCTURE.md](./MODULE_STRUCTURE.md).

## Samples

- `Samples/Demo` contains active developer scenes for package modules, including `Scenes/SurvivorDemo.unity` — a complete Vampire-Survivors-style game driven by one `SurvivorConfig` data asset on top of `Neo.Abilities` — and module demo shells built on the shared `NeoDemoShell` frame (Audio, Save, Settings, LevelFlow, StateMachine, NoCode, Parallax, Quest), plus `Scenes/UI/AnimationFlyDemo.unity` for manually testing the fly effect.
- `Samples/NeoxiderPages` contains the active optional page navigation sample module.
- Before release packaging, these sample roots are moved to the UPM paths `Samples~/Demo` and `Samples~/NeoxiderPages`, as specified in `package.json.samples`.

## Tests and quality

- The package includes `com.unity.test-framework`.
- Package tests live in `Assets/Neoxider/Tests/` (`Edit`, `Play`, `PlayMode`, `Editor`).
- Currently covered are the critical scenarios of `Abilities`, `Save`, `Level`, `Bootstrap`, `Audio`, `Parallax`, `PropertyAttribute`, `Tools/Move`, `Cards`, `GridSystem`, `Merge`, `Dice`, `Rpg`, `Settings`, `Quest`, `Progression`, `StateMachine`, and some legacy/editor regressions.

## Canonical documents

- User-facing package overview: [`README.md`](./README.md)
- User navigation across modules: [`Docs/README.md`](./Docs/README.md)
- English entry point: [`Docs/README.md`](./Docs/README.md)
- Change history: [`CHANGELOG.md`](./CHANGELOG.md)
- Module structure rules (folders, asmdef, namespaces): [`MODULE_STRUCTURE.md`](./MODULE_STRUCTURE.md)
