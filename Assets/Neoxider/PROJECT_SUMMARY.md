# NeoxiderTools Project Summary

**What it is:** a concise map of the `NeoxiderTools` package for developers, maintainers, and AI agents. Goal: quickly understand which ready-made runtime APIs, MonoBehaviour wrappers, sample scenes, and docs already exist before writing a new mechanic.

**Contents:** [package status](#package-status), [structure](#structure), [product layers](#key-product-layers), [reuse-first map](#reuse-first-map), [12.0 notes](#120-notes), [dependencies](#dependencies), [development rules](#package-development-rules), [samples](#samples), [tests](#tests-and-quality).

---

## Package Status

- **UPM package**: `Assets/Neoxider/package.json` (`com.neoxider.tools`)
- **Current version**: `12.0.0`
- **Upgrade guides**: [`Docs/Migration12.md`](./Docs/Migration12.md) (from 11.x), [`Docs/Migration11.md`](./Docs/Migration11.md) (from 10.x)
- **Validation**: the 11.0.0 Windows record is [`Verification11.md`](./Verification11.md); release verification of the 12.0.0 source is pending and is not implied by any audit receipt.
- **Unity**: `6000.0+`
- **Main namespace**: `Neo`
- **Size**: 26 module areas plus 17 Tools submodules, compiled as 54 runtime and 6 editor assemblies
- **Main user entry point**: [`README.md`](./README.md)
- **Main documentation index**: [`Docs/README.md`](./Docs/README.md)
- **Package tests**: `Assets/Neoxider/Tests/`

## Structure

```text
Assets/Neoxider/
  Scripts/      # Runtime modules grouped by domain, one asmdef per module or submodule
  Editor/       # Editor tools and inspectors
  Tests/        # EditMode and PlayMode tests for package runtime/editor-critical flows
  Docs/         # English user-facing documentation
  Samples~/     # UPM samples (Demo, NeoxiderPages, CharacterMovementFundamentals) — hidden from Unity, imported via Package Manager
  Prefabs/      # Ready-to-use prefabs
  Resources/    # Settings/assets
  Skill/        # Agent skill for coding assistants
  ThirdParty/   # Bundled Character Movement Fundamentals (MIT)
```

Maintainer documents (plans, TODO, demo builder guide) live in the repository's `dev-docs/` folder and are not part of the package.

## Key Product Layers

- **Core gameplay modules**: `Core` (Level, Resources), `Cards`, `GridSystem`, `Merge`, `Save`, `Settings`, `Shop`, `StateMachine`, `NPC`, `UI`, `Progression`, `Quest`, `Bonus`.
- **Abilities (combat)**: `Neo.Abilities` — a data-driven, Dota-style ability/modifier system. Units with properties (`{ Add | Mul | Max }`, aggregated `add -> mul -> max`), modifiers that unify buffs/debuffs/DoT/shields/stuns under one lifecycle, data-authored abilities resolved through a validated cast pipeline (states, costs, cooldown/charges, targeting, range) with a typed `CastResult`, an opt-in `AbilitySpatialGrid` for hundreds of units, multiplayer-ready event receipts, and a UITK Ability Designer. Supersedes `Rpg`.
- **Grid games**: `FieldGenerator` for shape/coordinates/cell state/pathfinding, `GridPathfinder` for paths and movement ranges, `GridPlacementEntry` / `GridPlacementResult` / `PlaceContentFootprint` for multi-cell placement, `GridMergeResolver` for connected-group merge, `DiceBoardService` for Dice Merge, plus the Match3, TicTacToe (n-in-a-row) and SlidingMerge (2048 with game-over and spawn events) layers.
- **Generic merge**: `Neo.Merge` - a pure C# connected-group merge engine with no dependency on Unity scene/Grid.
- **Plain C# cores behind components**: `CollectionSet`, `BoxProgress`, `CooldownSchedule`, `SlotSpinMath` (Bonus); `DeckModel`, `HandModel` (Cards); `RpgCharacterStatService`, `RpgCharacterProgressionService`, `RpgModifierMath` (Rpg); `ResourcePoolModel` (Core); `StateMachine<TState>`; `DialogueLinePool`; `IntegerChanceTable`.
- **Time and randomness seams**: `IFrameClock` / `ManualFrameClock` and `INeoClock` / `NeoTrustedTime.AsClock` for injectable time; seeded `RandomExtensions` overloads, `DeckModel(seed)` and `IntegerChanceTable` for reproducible rolls.
- **General-purpose tools**: `Tools/Inventory`, `Tools/Spawner`, `Tools/Move` (including `FreeFlyCameraController`), `Tools/Dialogue`, `Tools/Input`, `Tools/Time`, `Tools/Random`, `Tools/Physics`, `Tools/Draw`.
- **No-code / Inspector workflows**: `Condition`, **`Neo.NoCode`** (`NoCodeBindText`, `NoCodeFormattedText`, `SetProgress` -> TMP / Slider / Image), module `*NoCodeAction` bridges, `PropertyAttribute`, UnityEvent-driven components. Every NoCode component has a typed C# API.
- **Editor support**: custom inspectors, creation menus, maintenance windows.
- **Optional sample module**: `Samples~/NeoxiderPages`.
- **Moved out**: the HTML/CSS → UI Toolkit converter is the separate [HtmlToUitk](https://github.com/NeoXider/HtmlToUitk) package (`com.neoxider.htmltouitk`).

## Reuse-first map

Before implementing something new, check these ready-made building blocks:

| If you need | Already exists | Where to look |
|------------|----------|--------------|
| Grid, shape mask, coordinates, disabled/walkable/occupied cells | `FieldGenerator`, `GridShapeMask`, `FieldDebugDrawer` | [`Docs/GridSystem/README.md`](./Docs/GridSystem/README.md) |
| Paths, movement ranges, flood fill on a grid | `GridPathfinder.TryFindPath`, `CollectReachableCells`, `FieldGenerator.CollectNeighbors` | [`Docs/GridSystem/GridPathfinder.md`](./Docs/GridSystem/GridPathfinder.md) |
| Multi-cell placement of shapes/items | `GridPlacementEntry`, `GridPlacementResult`, `FieldGenerator.CanPlaceContentFootprint`, `PlaceContentFootprint` | [`Docs/GridSystem/FieldGenerator.md`](./Docs/GridSystem/FieldGenerator.md) |
| Merging identical connected elements | `Neo.Merge.MergeResolver`, `MergeRequest<TItem,TValue>`, `GridMergeRequest.Increment(...)` | [`Docs/Merge/README.md`](./Docs/Merge/README.md) |
| Dice Merge / drop-and-merge on a grid | `DicePiece`, `DicePieceGenerator`, `DiceBoardService` (`CanPlaceAnywhere`, `TryFindPlacement`) | [`Docs/GridSystem/Dice/README.md`](./Docs/GridSystem/Dice/README.md) |
| Limited hands, stalls, draft tray, market row | `HandModel.Capacity`, `TryAdd(...)`, `RemainingCapacity`, `AddRangeUntilFull(...)` | [`Docs/Cards/README.md`](./Docs/Cards/README.md) |
| Reproducible deals, custom card sorting | `DeckModel(seed)`, `DeckComponent.UseFixedShuffleSeed`, `HandComponent.SortAsync(IComparer<CardData>)`, `CardComparer` | [`Docs/Cards/README.md`](./Docs/Cards/README.md) |
| Match3, TicTacToe / Gomoku, 2048-like movement | `Match3BoardService`, `TicTacToeBoardService` (`WinLineLength`), `SlidingMergeBoardService` | [`Docs/GridSystem/README.md`](./Docs/GridSystem/README.md) |
| Flying rewards/coins between world/canvas points | `AnimationFly.Play(AnimationFlyRequest)`, sprite/prefab visuals, reward timing, fountain/magnet/scatter motion presets | [`Docs/UI/AnimationFly.md`](./Docs/UI/AnimationFly.md) |
| Saving scene objects and global/profile data | `SaveManager`, `SaveProvider`, `GlobalSave`, `SaveableBehaviour` | [`Docs/Save/README.md`](./Docs/Save/README.md) |
| 64-bit saved values (money in cents, big counters) | `SaveProvider.GetLong`/`SetLong`, `ILongSaveProvider` | [`Docs/Save/SaveProvider.md`](./Docs/Save/SaveProvider.md) |
| Money, shop, multi-currency | `Money`, `IMoneySpend`, `Shop.TryBuy` → `ShopPurchaseStatus`, `ShopItemData.CurrencyOverrideSaveKey` | [`Docs/Shop/README.md`](./Docs/Shop/README.md) |
| HP/Mana/resources, levels and XP | `HealthComponent`, `ResourcePoolModel`, `LevelComponent`, `LevelCurveDefinition` | [`Docs/Core/README.md`](./Docs/Core/README.md) |
| Data-driven abilities/modifiers: buffs, debuffs, DoT, shields, cast pipeline (successor to Rpg) | `AbilityDefinition`, `ModifierDefinition`, `UnitTemplate`, `AbilityUnitBehaviour`, `AbilityCasterBehaviour` | [`Docs/Abilities/README.md`](./Docs/Abilities/README.md) |
| RPG combat, projectiles, buffs/statuses, shields/armour hooks (superseded by Abilities) | `RpgCharacter`, `IRpgDamageModifier`, `RpgAttackController`, `RpgProjectile`, `RpgCombatMath` | [`Docs/Rpg/README.md`](./Docs/Rpg/README.md) |
| Inspector/no-code conditions and actions | `NeoCondition`, `ConditionEntryPredicate`, module NoCode bridges | [`Docs/Condition/README.md`](./Docs/Condition/README.md) |
| Collections, piggy banks, cooldown rewards in plain C# | `CollectionSet`, `BoxProgress`, `CooldownSchedule` | [`Docs/Bonus/README.md`](./Docs/Bonus/README.md) |
| Injectable frame time and UTC | `IFrameClock`, `ManualFrameClock`, `INeoClock`, `NeoTrustedTime.AsClock` | [`Docs/Tools/Time/Clocks.md`](./Docs/Tools/Time/Clocks.md) |
| Exact integer odds | `IntegerChanceTable` | [`Docs/Tools/Random/IntegerChanceTable.md`](./Docs/Tools/Random/IntegerChanceTable.md) |
| Bark lines by tag | `DialogueLinePool`, `DialogueLinePlayer` | [`Docs/Tools/Dialogue/DialogueLinePool.md`](./Docs/Tools/Dialogue/DialogueLinePool.md) |
| Quest credit from enemy/pickup prefabs | `QuestManager.Notify*`, `QuestNoCodeAction` Notify actions by Target Id | [`Docs/Quest/README.md`](./Docs/Quest/README.md) |
| Debug/spectator movement, pooling, timers, input helpers | `Tools/Move`, `Tools/Spawner`, `Tools/Time`, `Tools/Input` | [`Docs/Tools/README.md`](./Docs/Tools/README.md) |

## 12.0 notes

The full list is in [`CHANGELOG.md`](./CHANGELOG.md) and [`Docs/Migration12.md`](./Docs/Migration12.md).

- The HTML/CSS → UI Toolkit converter left the package (`com.neoxider.htmltouitk`).
- Demo scenes are authored in the scene by editor builders (`Samples~/Demo/Editor/Builders`) instead of runtime bootstraps; `NeoDemoShell` is an obsolete fallback.
- Hot paths of most modules were reworked to avoid per-frame and per-call allocations once warm; zero-allocation tests use Unity's GC.Alloc recorder. Explicit `Performance` tests print `NEO_PERF` lines.
- Plain C# cores were extracted from components (Bonus, Cards, Rpg, GridSystem), and time/random seams were added (clocks, seeded overloads, integer odds).
- Behaviour changes worth knowing: `AMSettings` persists mute, `ButtonScale`/`ButtonShake`/`FakeLoad` default to unscaled time, `AnimationFly.Play` copies its request, NPC chase events fire once per chase, `StringExtension.ToFloat` reads the invariant culture first.

## Dependencies

### Via UPM

- `com.unity.ai.navigation`
- `com.unity.inputsystem`
- `com.unity.ugui` (ships TextMeshPro on Unity 6)

### By use case

- `UniTask` for async-heavy modules (Audio, Cards, Dialogue, Time, Tools/Components)
- `DOTween` for tween-based animations and UI
- `Mirror` only for networking (`Neo.Network` compiles and runs solo without it)
- `Spine Unity Runtime` only for Spine integrations
- `MarkdownRenderer` optionally for improved `.md` viewing in the inspector
- `com.neoxider.htmltouitk` only for HTML/CSS → UI Toolkit conversion
- `com.unity.render-pipelines.universal` only for projects that need URP-specific rendering features or 2D lights; the package does not install URP

## Package development rules

- Check this summary and the docs index first: the needed building block often already exists, and a new mechanic can be assembled with an adapter instead of rewriting from scratch.
- Reuse existing modules and extend them instead of adding duplicates.
- When changing public behavior, update the module documentation and `CHANGELOG.md`.
- For new user-facing entry points, update `README.md` and `Docs/README.md` first.
- For multi-instance systems, prefer explicit references over implicit auto-find.
- Keep editor-only logic in `Editor/` or in separate `Editor` folders under the correct `asmdef`.
- Keep rules in plain C# with injectable time and randomness; components and NoCode helpers wrap them.
- **Module structure:** folders (Interfaces, Domain, Data, Components, Bridge, Runtime, Events, Enums), asmdef, namespaces, and the "one type - one file" principle are described in [MODULE_STRUCTURE.md](./MODULE_STRUCTURE.md).

## Samples

- `Samples~/Demo` contains the module demo scenes, the example games and `Scenes/SurvivorDemo.unity` — a complete Vampire-Survivors-style game driven by one `SurvivorConfig` data asset on top of `Neo.Abilities`. Every module demo (Audio, Save, Settings, Level, StateMachine, NoCode, Parallax, Quest, Progression, Inventory, Abilities, Grid, Shop, NPC, Network, Tools, `UI/AnimationFlyDemo` and more) is saved with its UI and UnityEvent wiring and is authored by an editor builder in `Samples~/Demo/Editor/Builders`; **Neoxider > Demo > Rebuild Active Demo Scene** rebuilds one. See [`Docs/Samples.md`](./Docs/Samples.md).
- `Samples~/NeoxiderPages` contains the optional page navigation sample module.
- `Samples~/CharacterMovementFundamentals` holds the 3D character controller showcase (slopes, stairs, platforms, gravity rooms) and the Capguy character the animated character presets reference.
- During sample development the folder is renamed to `Samples`; before release packaging all three sample roots return to the UPM paths `Samples~/Demo`, `Samples~/NeoxiderPages` and `Samples~/CharacterMovementFundamentals`, as specified in `package.json.samples`.

## Tests and quality

- `com.unity.test-framework` is **not** a package dependency — install it in the host project to run the tests.
- Package tests live in `Assets/Neoxider/Tests/` (`Edit`, `Play`, `Fixtures`, `Integration`) — more than 5,000 EditMode cases plus the PlayMode suites. Current counts and skips are reported by Test Runner and depend on Unity, optional modules, graphics, and sample layout. Demo builders are tested in `Neo.Demo.Editor.Tests` inside the Demo sample.
- Covered are the critical scenarios of the runtime modules, including `Abilities`, `Save`, `Level`, `Bootstrap`, `Audio`, `Parallax`, `PropertyAttribute`, `Tools`, `Cards`, `GridSystem`, `Merge`, `Dice`, `Rpg`, `Settings`, `Quest`, `Progression`, `StateMachine`, `Network`, `NoCode` and editor regressions, plus zero-allocation guards for warm hot paths.

## Canonical documents

- User-facing package overview: [`README.md`](./README.md)
- User navigation across modules: [`Docs/README.md`](./Docs/README.md)
- Change history: [`CHANGELOG.md`](./CHANGELOG.md)
- Upgrade guides: [`Docs/Migration12.md`](./Docs/Migration12.md), [`Docs/Migration11.md`](./Docs/Migration11.md)
- Module structure rules (folders, asmdef, namespaces): [`MODULE_STRUCTURE.md`](./MODULE_STRUCTURE.md)
- Documentation standard: [`DOCUMENTATION.md`](./DOCUMENTATION.md) and [`DOCUMENTATION_GUIDELINES.md`](./DOCUMENTATION_GUIDELINES.md)
