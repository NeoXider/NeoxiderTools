# NeoxiderTools Project Summary

**What it is:** a compact map of the `NeoxiderTools` package for developers, maintainers and AI agents. Check it before writing a new mechanic: the runtime API, wrapper, demo or doc you need often exists already.

**Contents:** [package status](#package-status), [structure](#structure), [product layers](#key-product-layers), [reuse-first map](#reuse-first-map), [dependencies](#dependencies), [development rules](#package-development-rules), [demos](#samples), [tests](#tests-and-quality), [canonical documents](#canonical-documents).

---

## Package Status

- **UPM package**: `Assets/Neoxider/package.json` (`com.neoxider.tools`)
- **Current version**: `12.5.1`
- **Unity**: `6000.0+`
- **Main namespace**: `Neo`
- **Size**: 26 module areas plus 17 Tools submodules, compiled as 54 runtime and 6 editor assemblies
- **Install**: `https://github.com/NeoXider/NeoxiderTools.git#upm` ([Getting Started](./Docs/GettingStarted.md#installation))
- **Upgrade guides**: [`Docs/Migration12.md`](./Docs/Migration12.md) (from 11.x, including every 12.x change), [`Docs/Migration11.md`](./Docs/Migration11.md) (from 10.x)
- **Validation**: the 11.0.0 Windows record is [`Verification11.md`](./Verification11.md); no release verification record for 12.x is published, and no audit receipt implies one.
- **Entry points**: [`README.md`](./README.md) for users, [`Docs/README.md`](./Docs/README.md) for module navigation

## Structure

```text
Assets/Neoxider/
  Scripts/      # Runtime modules grouped by domain, one asmdef per module or submodule
  Editor/       # Editor tools and inspectors
  Tests/        # EditMode and PlayMode tests for package runtime/editor-critical flows
  Docs/         # English user-facing documentation
  Prefabs/      # Ready-to-use prefabs
  Resources/    # Settings/assets
  Skill/        # Agent skill for coding assistants
  ThirdParty/   # Bundled Character Movement Fundamentals (MIT)
```

Maintainer documents (plans, TODO, demo scene rules) live in the repository's `dev-docs/` folder and are not part of the package.

## Key Product Layers

- **Core gameplay modules**: `Core` (Level, Resources), `Cards`, `GridSystem`, `Merge`, `Save`, `Settings`, `Shop`, `StateMachine`, `NPC`, `UI`, `Progression`, `Quest`, `Bonus`.
- **Abilities (combat)**: `Neo.Abilities` is a data-driven, Dota-style ability and modifier system and supersedes `Rpg`.
  - Units have properties (`{ Add | Mul | Max }`, aggregated `add -> mul -> max`).
  - Modifiers put buffs, debuffs, DoT, shields and stuns under one lifecycle.
  - Abilities are data, resolved through a validated cast pipeline (states, costs, cooldown and charges, targeting, range) with a typed `CastResult`.
  - An opt-in `AbilitySpatialGrid` handles hundreds of units; event receipts are multiplayer-ready; the UITK Ability Designer edits the data.
- **Grid games**: `FieldGenerator` owns shape, coordinates, cell state and pathfinding; `GridPathfinder` finds paths and movement ranges; `GridPlacementEntry` / `GridPlacementResult` / `PlaceContentFootprint` place multi-cell content; `GridMergeResolver` merges connected groups; `DiceBoardService` runs Dice Merge. Match3, TicTacToe (n-in-a-row) and SlidingMerge (2048 with game-over and spawn events) are separate layers.
- **Generic merge**: `Neo.Merge`, a pure C# connected-group merge engine with no dependency on Unity scenes or grids.
- **Plain C# cores behind components**: `CollectionSet`, `BoxProgress`, `CooldownSchedule`, `SlotSpinMath` (Bonus); `DeckModel`, `HandModel` (Cards); `RpgCharacterStatService`, `RpgCharacterProgressionService`, `RpgModifierMath` (Rpg); `ResourcePoolModel` (Core); `StateMachine<TState>`; `DialogueLinePool`; `IntegerChanceTable`.
- **Time and randomness seams**: `IFrameClock` / `ManualFrameClock` and `INeoClock` / `NeoTrustedTime.AsClock` for injectable time; seeded `RandomExtensions` overloads, `DeckModel(seed)` and `IntegerChanceTable` for reproducible rolls.
- **General-purpose tools**: `Tools/Inventory`, `Tools/Spawner`, `Tools/Move` (including `FreeFlyCameraController`), `Tools/Dialogue`, `Tools/Input`, `Tools/Time`, `Tools/Random`, `Tools/Physics`, `Tools/Draw`.
- **No-code / Inspector workflows**: `Condition`, `Neo.NoCode` (`NoCodeBindText`, `NoCodeFormattedText`, `SetProgress` -> TMP / Slider / Image), module `*NoCodeAction` bridges, `PropertyAttribute` and UnityEvent-driven components. Every NoCode component has a typed C# API.
- **Editor support**: custom inspectors, creation menus, maintenance windows.
- **Optional page module**: `Assets/NeoxiderDemos/NeoxiderPages`, shipped as a demo download.
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
- `com.unity.render-pipelines.universal` only for URP-specific rendering features or 2D lights; the package does not install URP

Versions and status: [`Docs/PackageCompatibility.md`](./Docs/PackageCompatibility.md#package-dependencies).

## Package development rules

- Check this summary and the docs index first: a new mechanic can often be assembled from existing blocks with an adapter.
- Extend existing modules instead of adding duplicates.
- When changing public behaviour, update the module documentation and `CHANGELOG.md`.
- For new user-facing entry points, update `README.md` and `Docs/README.md` first.
- For multi-instance systems, prefer explicit references over implicit auto-find.
- Keep editor-only logic in `Editor/` or in separate `Editor` folders under the correct asmdef.
- Keep rules in plain C# with injectable time and randomness; components and NoCode helpers wrap them.
- Module structure (folders Interfaces, Domain, Data, Components, Bridge, Runtime, Events, Enums; asmdef; namespaces; one type per file) is described in [MODULE_STRUCTURE.md](./MODULE_STRUCTURE.md).

## Samples

The demos are ordinary project folders outside the package; each ships as its own `.unitypackage` on the releases page ([`Docs/Samples.md`](./Docs/Samples.md)):

- `Assets/NeoxiderDemos/Demo`: module demo scenes, example games and `Scenes/SurvivorDemo.unity`, a Vampire-Survivors-style NoCode game with seven weapons, built from library components and UnityEvents on `Neo.Abilities`, with no game scripts. Every scene is saved with its UI and UnityEvent wiring.
- `Assets/NeoxiderDemos/NeoxiderPages`: the optional page navigation module and its demo.
- `Assets/NeoxiderDemos/CharacterMovementFundamentals`: the 3D character controller showcase (slopes, stairs, platforms, gravity rooms) and the Capguy character used by the animated character presets.

## Tests and quality

- Tests live in `Assets/Neoxider/Tests/` (`Edit`, `Play`, `Fixtures`, `Integration`, `Performance`): more than 5,000 EditMode cases plus the PlayMode suites. Counts and skips depend on Unity, optional modules, graphics and the demo layout.
- They cover the critical scenarios of the runtime modules, including `Abilities`, `Save`, `Level`, `Bootstrap`, `Audio`, `Parallax`, `PropertyAttribute`, `Tools`, `Cards`, `GridSystem`, `Merge`, `Dice`, `Rpg`, `Settings`, `Quest`, `Progression`, `StateMachine`, `Network`, `NoCode` and editor regressions, plus zero-allocation guards for warm hot paths.
- `com.unity.test-framework` is not a package dependency; install it in the host to run the tests. How to run them: [`Docs/Testing.md`](./Docs/Testing.md).

## Canonical documents

- User-facing package overview: [`README.md`](./README.md)
- User navigation across modules: [`Docs/README.md`](./Docs/README.md)
- Change history: [`CHANGELOG.md`](./CHANGELOG.md) and, for what an upgrading project notices, [`Docs/Migration12.md`](./Docs/Migration12.md)
- Upgrade guides: [`Docs/Migration12.md`](./Docs/Migration12.md), [`Docs/Migration11.md`](./Docs/Migration11.md)
- Module structure rules (folders, asmdef, namespaces): [`MODULE_STRUCTURE.md`](./MODULE_STRUCTURE.md)
- Documentation standard: [`DOCUMENTATION.md`](./DOCUMENTATION.md) and [`DOCUMENTATION_GUIDELINES.md`](./DOCUMENTATION_GUIDELINES.md)
