# TODO

Current technical tasks that should stay separate from the changelog. This list does not replace release planning; it records near-term public API improvements and the real follow-ups left by the 12.0.0 quality pass. Items marked done say in one line what shipped; open items are grouped by module. Every open item below was re-checked against the code on 2026-11-04; the items closed by that check are listed in [Closed by the 12.0.0 pass](#closed-by-the-1200-pass).

## Bonus / Slot

- [x] **Allow per-slot-machine symbol weight overrides.** Shipped in 9.11.0: `SlotSymbolWeightOverrides` on `SpinController` (matched by symbol id, definition weights as fallback), **Normalize Weights** in the Inspector `⋮` menu, `PickEconomySymbolId()`, deterministic `PickWeightedId` overloads, and EditMode coverage for disabled override, reordered/changed symbol lists, zero/negative weights, normalization, and weighted selection.

## Shop / UI

- [x] **Add an optional universal category-bar behaviour.** Shipped in 9.11.0: `Neo.UI.CategoryBar` + `CategoryBarItem` (selection state, re-parented selection marker with offset, id/index events, initial/runtime selection, prev/next, disabled entries, Inspector or runtime categories) with the `ShopListViewCategoryBar` adapter; no Shop dependency for generic use.
- [x] **Add an optional furniture/equipment variants panel on top of existing Shop views.** Shipped in 9.11.0: `ShopVariantsPanel` over `ShopListView`/`ShopItem` + optional `EquipmentManager` — unowned/owned/equipped (+ unequip) states through `IShopVariantView` / `ShopVariantStateView`, buy-then-equip, refresh on ownership/equipment changes.
- [x] **Make purchase affordability a reactive Shop view state.** Shipped in 9.11.0: `ButtonPrice.ButtonType.Unaffordable`, `ShopPurchaseButtonView` (balance subscription via `Shop.ResolveCurrencyMoney`, immediate state + `Button.interactable` updates, safe unsubscribe, rebinding), public `Shop.CanAfford(item/id)` + `IMoneyCanSpend`, with EditMode tests.
- [x] **Typed purchase outcome.** Shipped in 12.0.0: `Shop.TryBuy` / `TryBuyBundle` return `ShopPurchaseStatus`; `Buy`/`BuyBundle` delegate to them.

## GridSystem

- [x] Add a generic `GridPlacementService` / rule config on top of the current `FieldGenerator` placement API. Shipped in 9.11.0: `GridPlacementRequest` with `RequireEnabled`, `RequireWalkable`, `RequireUnoccupied`, custom predicate, and overwrite policy; atomic multi-cell placement with failure reasons.
- [x] Non-Mono plain C# `DiceBoard` service. Shipped in 9.12.0: `DiceBoard` core over `FieldGenerator` with C# events; `DiceBoardService` stays the MonoBehaviour wrapper with an unchanged scene API and forwards settings/events into the core.
- [x] **Reusable grid-game building blocks.** Shipped in 12.0.0: allocation-free `GridPathfinder.TryFindPath` / `CollectReachableCells` (movement ranges), Inspector `MovementPreset`, n-in-a-row TicTacToe (`WinLineLength`, `WinningLine`), complete 2048 loop on `SlidingMergeBoardService` (`OnNoMovesLeft`, seeded spawns, custom merge rules), Dice `CanPlaceAnywhere` / `TryFindPlacement`, `FieldObjectSpawner.Unregister` / `PruneDestroyedObjects`.

## Package-wide: split logic cores out of MonoBehaviours

Motivation, from consuming the package in Tropic Mania: several modules could not be reused because
the rules only exist inside a MonoBehaviour that also owns DOTween tweens, `Image`/`TMP_Text`
references, `UnityEvent`s and its own save keys. A project needing the same rules in an engine-free
assembly (deterministic EditMode tests, integer-cents money, injected randomness and clock) has to
reimplement them. `DiceBoard`/`DiceBoardService` above is the pattern to follow: plain C# core with
C# events, MonoBehaviour kept as a thin wrapper with an unchanged scene API.

- [ ] **Long-term principle: logic lives in plain C#, MonoBehaviours are only wrappers.** Nearly all
      game logic should live in plain C# classes (testable cores with C# events and injected
      randomness/clock); `MonoBehaviour` stays as controller/presenter/scene wrapper only. Gradually
      extract logic out of the existing fat components instead of adding new logic into them.
- [ ] **Continue decomposing the god-objects** (line counts as of the 12.0.0 pass):
      - `AM` (~2800) — the largest component; nothing extracted yet. Candidate: a plain C# playback
        session (sessions, backends, observable `AMSettings` snapshots so sliders follow external changes).
      - `SpinController` (~2400) — `SlotSpinMath` (spin price, payout) extracted; next: an injectable random
        seam for plan building (`CheckSpin.SetWin/SetLose` and per-cell picks still use `UnityEngine.Random`;
        needs a shared random abstraction across Bonus and Tools/Random).
      - `RpgCharacter` (~1920, legacy) — `RpgCharacterProfileService`, `RpgCharacterResourceService`,
        `RpgCharacterStatService` (stats, upgrades), `RpgCharacterProgressionService` (level, XP, points),
        `RpgModifierMath`, `RpgEffectShelf` status queries and the optional `RpgCharacterNetworkAdapter` are
        extracted; the facade keeps orchestration (buff apply, profile sequencing, publication, re-entrancy
        guards). Next: the transactional `RpgCharacterState` with an explicit simulation clock (audit
        improvement B).
      - `Selector` (~1850) — bounds, effective index, logical count and the item-activation rule live in
        `SelectorModel`; next: save and Mirror adapters as plain C# services, child-sync helper.
      - `InteractiveObject` (~1590) — query math, target contract, camera resolver,
        `InteractionHitProjector`, `InteractionTargetScope` and the debug ray are extracted; next: keyboard/mouse
        edge state.
      - `AnimationFly` (~1560) — `AnimationFlyMath` and `AnimationFlyVisualPool` extracted; next: the
        coordinate-space resolver (~250 lines) and a tracked flight handle (see UI below).
- [x] **`Bonus/Collection/Collection`** — non-Mono set core. Shipped in 12.0.0: plain C# `CollectionSet`
      (independent sets, C# events, injectable random, own save prefix), exposed read-only as
      `Collection.Set` (`IReadOnlyCollectionSet`) plus `Collection.RandomIndexSource`.
- [x] **`Bonus/Collection/Box`** — accumulate/threshold/claim rules. Shipped in 12.0.0: plain C#
      `BoxProgress` (`long` progress, idempotent claim receipts by claim id, `Capture`/`Restore`) and
      `BoxProgressRules` shared with `Box`; `Box.TargetCollection` replaces the hard `Collection.I` lookup.
- [x] **`Bonus/TimeReward/CooldownReward`** — cooldown rule out of `TimerObject`. Shipped in 12.0.0:
      plain C# `CooldownSchedule` on an injected `INeoClock` (default `NeoTrustedTime`), backlog modes,
      `SaveTo`/`TryLoadFrom`; `CooldownReward` and `TimeReward` share its `CountClaimable` rule.
- [x] **`Tools/Dialogue/DialogueController`** — pooled lines. Shipped in 12.0.0: plain C#
      `DialogueLinePool` + `DialogueLinePlayer` (pick by tag, no back-to-back repeat, time windows,
      first-pick slots, contextual lines pre-empting idle), usable without `DialogueController`.
- [x] **`Tools/Random/ChanceManager`** — integer weight path. Shipped in 12.0.0: `IntegerChanceTable`
      (exact integer weights, O(log n) rolls, injectable unbiased random source).
- [x] **`Extensions/RandomExtensions.Shuffle`** — injectable random. Shipped in 12.0.0:
      `Shuffle(System.Random)`, `Shuffle(Func<int,int>)` and seeded `GetRandomElement` / `GetRandomIndex` /
      `GetRandomWeightedIndex` overloads.
- [x] **Add a clock abstraction.** Shipped in 12.0.0: `IFrameClock` / `UnityFrameClock` / `ManualFrameClock`
      on `Timer.FrameClock` and `TimerObject.FrameClock`; `INeoClock` UTC seam through `TimerObject.UtcClock`,
      `TrustedClock.AsNeoClock()` and `NeoTrustedTime.AsClock` (`Docs/Tools/Time/Clocks.md`).
- [x] **`Save/ISaveProvider` — add `long` support.** Shipped in 12.0.0: `ILongSaveProvider`
      (native in `FileSaveProvider`), `SaveProviderExtensions.GetLong`/`SetLong` (invariant culture, reads
      values written with `SetInt`) and `SaveProvider.GetLong`/`SetLong`.
- [x] **`Audio/AMSettings` — persist mute, and stop force-unmuting on start.** Shipped in 12.0.0: mutes are
      saved under `saveKeyMute` (`PersistMute`); the first binding applies a pre-binding `SetMusic`/`SetEfx`
      request, then the saved mute, then the sources' own state; a replacement `AM` keeps the current mute.

## camelCase public members

`ModulePrinciplesTests.RuntimePublicMembers_UsePascalCase` skips 110 runtime files that predate the PascalCase rule
(`CamelCasePascalViolationAllowedFiles`). New files may not join that list. Migrate one module per change and delete
the module's entries from the list in the same change.

**Recipe per file** (no breaking change, warnings only):

1. A public camelCase field becomes a private serialized field with `[FormerlySerializedAs("oldName")]` plus a
   PascalCase property. Keep an `[Obsolete]` forwarder under the old name, so code still compiles with a warning.
2. A public camelCase method gets a PascalCase name and an `[Obsolete]` forwarder under the old name. Keep the
   forwarder at least until the next major: persistent UnityEvent calls in scenes and prefabs, NeoCondition and
   NoCode bindings, and `[SaveField]` / network member names store the old name as a string.
3. Check that the module's demo scene, prefabs and data assets load the same values (rebuild the scene, run
   `SampleAssetValidationTests` and the module tests), update the module docs and the skill references, and list
   the renamed members in the changelog and the upgrade guide of that release.

**Order** (leaf modules first, shared and networked contracts last):

- [ ] **Step 1, leaf components (57 files):** Animations (`ColorAnimator`, `FloatAnimator`, `Vector3Animator`); Level
      (`LevelButton`, `Map`); Tools/Text (`SetText`, `TimeToText`); Tools/Time (`TimerObject`); Tools/Debug
      (`ErrorLogger`); Tools/Draw (`Drawer`); Tools/View (`LightAnimator`, `MeshEmission`, `Selector`, `StarView`);
      Tools/Components (`Loot`, `ScoreManager`, `TypewriterEffect`); Tools/Move (`AdvancedForceApplier`,
      `CameraRotationController`, `DistanceChecker`, `Follow`, `ConstantMover`, `ConstantRotator`,
      `UniversalRotator`); Tools/Spawner (`PoolManager`, `SimpleSpawner`, `Spawner`); Tools/Input (`MouseEffect`,
      `MouseInputManager`, `MultiKeyEventTrigger`, `SwipeController`); Tools/InteractableObject
      (`InteractiveObject`, `PhysicsEvents2D`, `PhysicsEvents3D`, `ToggleObject`); Tools/Dialogue
      (`DialogueController`, `DialogueUI`); Tools/FakeLeaderboard (`Leaderboard`, `LeaderboardItem`,
      `LeaderboardMove`); Tools/Other (`CameraShake`); Tools/Managers (`GM`); UI (`AnimationFly`, `UI`,
      `VariantView`, `VisualToggle`, `AnchorMove`, `ButtonChangePage`); Cards (`BoardModel`); Core
      (`ResourceEntryInspector`); Extensions (`Shapes`, `LegacyComponentAttribute`); PropertyAttribute
      (`GUIColorAttribute`); NPC (`NpcNavigation`); Save (`FileSaveProvider`); StateMachine (`StateMachine`,
      `StateMachineData`).
- [ ] **Step 2, data assets (15 files):** Bonus slot data (`BetsData`, `LinesData`, `SpriteMultiplayerData`,
      `SpritesData`); Rpg data (`RpgCharacterTemplate`, `RpgProgressionDefinition`, `RpgRegenDefinition`,
      `RpgResourceDefinition`, `RpgResourceModifier`, `RpgStatDefinition`, `RpgStatId`, `RpgStatUpgradeRule`);
      Shop data (`ShopBundleData`, `ShopItemData`); `DialogueData`. `[FormerlySerializedAs]` keeps every saved
      asset; check the package and sample data assets after the import.
- [ ] **Step 3, shared and heavily wired components (24 files):** Audio (`AM`, `AMSettings`, `SettingMixer`);
      Shop (`Money`, `Shop`, `ShopItem`, `ButtonPrice`, `TextMoney`); Bonus (`SpinController`, `CheckSpin`, `Row`,
      `SlotElement`, `SpeedControll`, `VisualSlotLines`, `WinLineRendererPlayback`, `LineRoulett`,
      `WheelMoneyWin`); Rpg (`MeleeWeapon`); Network (`NetworkActionRelay`, `NetworkContextActionRelay`,
      `NetworkEventDispatcher`, `NetworkOwnerFilter`, `NetworkPropertySync`, `NeoNetworkComponent`; every peer must
      run the same version). These carry the most UnityEvent and NoCode references, so add an editor migration
      that rewrites persistent calls before the forwarders are removed.
- [ ] **Step 4, versioned:** `RpgCharacter` keeps its camelCase serialized network compatibility flag until a
      versioned network and save migration.
- **Not migrated:** `UIMeshRigGraphic` (camelCase members are required by `Graphic` / `ILayoutElement`) and the
      injection and `[Button]` attribute types (`ButtonAttribute`, `FindAllInSceneAttribute`,
      `GetComponentAttribute`, `GetComponentsAttribute`, `LoadAllFromResourcesAttribute`,
      `LoadFromResourcesAttribute`) stay on the list for good. The obsolete `AiNavigation`, `TimeReward` and the
      legacy Rpg combat types (`AdvancedAttackCollider`, `AttackExecution`, `Evade`, `Health`) are removed, not
      renamed, when a major release drops them.

## Open follow-ups from the 12.0.0 quality pass

### Abilities

- [ ] Typed projectile delivery host (`IAbilityDeliveryHost`, `AbilityDeliveryRequest`, projectile simulation stepped by the domain clock, swept hits, `DeliveryFailed`); the projectile still uses scene `Time.deltaTime` and point sampling (`Docs/Audits/2026-10-23/Neo.Abilities.plan.md`).
- [ ] Editor catalog validator (`AbilityDefinitionValidator`, `AbilityDiagnostic`, library scope; `Neo.Abilities.Editor.plan.md`).
- [ ] Interned property handles for `AbilityUnit.GetProperty` (string lookups such as `health_regen` per unit per tick) — measure in the Survivor demo first.
- [ ] Pay ability costs through `ResourcePoolModel.TrySpendAll` / `CanSpendAll` (Neo.Core): `AbilitySystem.TryPayCosts` still validates and then pays cost by cost.

### Animations / Parallax / View

- [ ] Parallax: pure layout calculator and frustum-based tile counts for perspective cameras; `RecycleTiles` still reads every tile position on each frame the camera moves.
- [ ] `MeshEmission`: optional `MaterialPropertyBlock` mode instead of cloning its material (SRP Batcher trade-off).

### Bonus

- [ ] `CooldownReward` reads `NeoTrustedTime.UtcNow` directly; only `CooldownSchedule` takes an `INeoClock`, so a scene cannot run a manual clock.
- [ ] Owner decisions: move `CooldownReward` / `TimeReward` persistence onto a `CooldownSchedule` instance (changes the save keys and event order); let `Box` own an integer `BoxProgress` (changes float-progress scenes).
- [ ] `CollectionVisualManager` stays subscribed to a destroyed global `Collection` until it is re-enabled or reassigned (needs a registration event on Singleton).

### Cards

- [ ] Atomic card zones (`CardInstanceId`, `CardZoneModel`, `CardTransferService`; audit plan P01).
- [ ] Card view pooling for draw/return/stack builds (needs a `CardComponent` reset contract).
- [ ] `DeckComponent.Shuffle()` does not reorder the visual stack; `HandComponent` Max Cards 0 means "always full" while `HandModel.Capacity` 0 means unlimited — both need a migration decision.
- [ ] `BoardComponent`: a card moved away during an animated layout stays listed.
- [ ] `ShuffleVisualType.Cut`/`Riffle` are timing placeholders without motion; legacy `CardComparer.ByRank*` / `BySuit*` treat custom cards as equal.

### Condition / NoCode / StateMachine

- [ ] Condition typed core (`ConditionResult`, `ConditionBindingSession`, `IConditionValueSource`) and the versioned member descriptor; the first step shipped (`ConditionEntry.TryEvaluate`, public properties).
- [ ] NoCode typed source session (`IFloatValueSource` / `FloatBindingSession`); the first step shipped (`RefreshBinding`, `UpdateMode`, `SetValues`). Value-type reflective reads still box once per read.

### Editor

- [ ] Missing Scripts scan/plan/apply service API (`Neo.Editor.plan.md`).

### Extensions / Reactive / Haptics / PropertyAttribute

- [ ] Reactive independent observation with retention metadata (`Neo.Reactive.plan.md`).
- [ ] `HapticService` with a backend and clock (`Neo.Haptics.plan.md`).
- [ ] Typed interface-reference contract; the consumer bug is in `Editor/PropertyAttribute/ComponentDrawer.cs`.
- [ ] Versioned string-array codec for `PlayerPrefsUtils` (the old writer must stay byte-compatible).
- [ ] `TryFormatDuration` / `DurationFormatOptions`; making `NumberFormatExtensions.Round` public would let `SetText` cache its per-frame tween string.

### GridSystem / Match3 / Merge

- [ ] Weighted pathfinding (Dijkstra/A* with a cost callback on `GridPathRequest`) for tactics games.
- [ ] Constant-time picking for Hexagonal/Isometric layouts (the fast path covers gapless rectangular grids only).
- [ ] Revision-aware `GridBoard` core with occupancy leases and snapshots (GridSystem plan P2); immutable `MergePlan` + `ApplyIfUnchanged` (Merge plan).
- [ ] TicTacToe: starting-player option, C# events on the service, gravity (Connect-Four) placement.

### Input / Interaction

- [ ] Player/IL2CPP check of the delegate-bound Input System reads at managed stripping High (no `link.xml` entry is expected to be needed; not yet measured in a player build).

### Inventory / Spawner

- [ ] Typed `PoolLease` rental handle (Spawner plan P2; new ownership contract).
- [ ] Inventory instance editing (`TryReplaceInstanceState`, revision, `IInventoryStateSource`; Inventory plan P01).
- [ ] `Spawner` pooled admissions: O(n) `SpawnedObjects.Remove` (plus a prune every 10th admission) because the public list is mutable; a read-only view needs an API decision. Random spawn delays allocate a `WaitForSeconds` each.
- [ ] `InventoryDropper` instantiates drops instead of pooling them; per-mutation before/after count dictionaries need a `MutationReceipt` lifecycle redesign.

### Network

- [ ] Typed `ReplicationBinding<T>` core for `NetworkPropertySync` (boxes on every poll and write).
- [ ] `NeoNetworkTelemetry` overlay is still IMGUI; offer a uGUI/UI Toolkit readout.
- [ ] Owner decision: `Prefabs/-System--.prefab` (its `Money` object) and `Prefabs/Tools/First Person Controller.prefab` carry a Mirror `NetworkIdentity`, so a project without Mirror shows a missing script on them. Removing it changes multiplayer setups that rely on it; an alternative is a Mirror-only variant prefab.

### NPC / Shop

- [ ] Agent-type-aware `INpcPathPlanner` with typed failures and retry/backoff (NPC plan §4); navigation `CaptureState`/`RestoreState` (§5).
- [ ] Typed shop transactions and receipts across modules (`IShopGrantParticipant`, transactional inventory delivery).
- [ ] `ShopVariantsPanel` still calls `GetComponentsInChildren` per row on each render (1-2 renders per purchase; caching needs hierarchy invalidation).
- [ ] Owner decision: `NpcRpgCombatBrain.OnHoldingPosition` fires every decision tick.

### Progression / Quest

- [ ] Plain C# `QuestRuntime` / `ProgressionSession` with stable objective ids and v2 snapshots (Quest plan §4, Progression plan §3).
- [ ] Progression save coalescing (each XP/perk reward calls `SaveProvider.Save()`); needs an owner decision on crash safety.
- [ ] Durable per-reward claim ledger; a policy for corrupt loaded quest status/count values.
- [ ] `LevelCurveDefinition` reward walk is O(entries²) for a non-ascending track (ascending tracks are walked in one pass).

### Rpg (legacy)

- [ ] Durable `BaseMax` / profile v3 (audit improvement A).
- [ ] `RpgCombatMath`: `[Obsolete]` warning pointing to the allocation-free `RpgModifierMath` (owner decision).

### Save / Settings / Level

- [ ] Result-bearing save operations and explicit sessions (`ITransactionalSaveProvider`, `ISaveSession`; Save plan).
- [ ] `DeleteKey`/`DeleteAll` on both providers do not raise `OnKeyChanged`.
- [ ] Instance-based settings controller with an Apply/Cancel session; `SceneFlowController` operation factory; separate map-progress model (Level plan P2/P3).

### Time / Dialogue / Random

- [ ] Plain C# `TimerState` / `TimerOptions` / `TimerSnapshot` core (`Neo.Tools.Time.plan.md` §3).
- [ ] Typewriter: a deterministic session/tokenizer model (grapheme-aware reveal, full TMP tag semantics); the rate cap is fixed.

### Tools (Move / Physics / Draw / Components / View)

- [ ] Shared `MotionPlane`/`MotionMath` core for the mouse/keyboard movers and owner-token input blocks across controllers (Move plan S1/S2); `KeyboardMover`, `PlayerController2DPhysics` / `3DPhysics` and `NeoCharacterInput` could read movement through `MoveInputCompat.ReadMove`.
- [ ] Drawer: incremental `StrokeBuilder` (each appended point re-smooths the whole stroke).
- [ ] MagneticField: independent discovery/force cadence (with Use Fixed Update off the total force depends on frame rate).
- [ ] NoCode gaps found by the Physics 3D demo: `ImpulseZone` pushes only on enter (an opt-in repeat-while-inside mode); `ExplosiveForce` passes upwards modifier 0 (an optional serialized `upwardsModifier`); no NoCode transform-reset component to restack a scene.
- [ ] Exact integer backend for `Counter`/`RandomRange` beyond 2^24 (save-format and network migration).
- [ ] `ErrorLogger`: the character budget shipped (`MaxCharacters`); a plain C# `DebugLogBuffer` core is still open.
- [ ] Selector: the public `IncludedIndices` snapshot is rebuilt O(pool) per selection change.
- [ ] `UnityLifecycleEvents` pays Update/FixedUpdate/LateUpdate calls even with every emit flag off (a component split changes the serialized layout).
- [ ] Procedural/impulse camera shake on `TransformAnimationEvaluator` (needs a bridge asmdef).
- [ ] `SingletonById` re-registration with domain reload and scene reload both disabled: needs the four-combination Unity run. `AnimatorParameterDriver` retries `GetComponent` on every call while no Animator exists.

### UI

- [ ] AnimationFly: tracked flight handle (`Status`, `Cancel`, `Finished`) — a flight interrupted by disabling or destroying the object leaves its result incomplete.
- [ ] Shared pause lease coordinator in `Neo.Tools.Managers` (GM ownership).

### Samples and demo scenes

- [ ] **Modules and components without a demo scene:** Tools/FakeLeaderboard (`Leaderboard`; `SubmitScore` now makes it wireable), Tools/Debug (`ErrorLogger`, `NeoDebugOverlay`), `CameraShake`, Haptics, Core resources (`HealthComponent` / `ResourcePoolModel` on their own), Shop equipment and variants (`EquipmentManager`, `ShopVariantsPanel`), `SwipeController`, `StarView`, the Move toolkit beyond Follow and CameraConstraint (`FreeFlyCameraController`, `UniversalRotator`, `AdvancedForceApplier`, `DistanceChecker`), and a Cards sandbox (custom sorting, poker evaluator, custom cards).
- [ ] `QuestDemoView` is `[Obsolete]` and used by no scene: delete it in the next major.
- [ ] Character Movement Fundamentals: `ShowcaseScene.unity` keeps a hand-authored `StandaloneInputModule` and the CMF scripts read `UnityEngine.Input`, so its example scenes need Active Input Handling = Both. The CameraAspectRatio backdrop uses a NeoxiderPages sprite (empty with only Demo Scenes imported; the builder warns).
- [ ] `Assets/Samples/CharacterMovementFundamentals` is an older committed import that shares all 133 GUIDs with `Samples~/CharacterMovementFundamentals`: delete or move it before renaming `Samples~` to `Samples` (duplicate GUIDs), or decide to keep it as a separate test fixture.
- [ ] NeoxiderPages: the Game page's pause icon and the Pause page's Resume/Menu buttons are not wired in the hand-authored prefabs (the info card covers the flow); `UIPage.EndActive` called directly does not update PM state; `PM.RefreshEditorPreview`'s `autoSelectEditorPage` branch never selects anything (pre-existing).
- [ ] Glyphs missing from the static TMP atlas (LiberationSans SDF): builder strings `CardGameBuilder` (U+2190), `NoCodeActionsDemoBuilder` (U+2460-2463), `NoCodeBindingDemoBuilder` (U+2191), U+2192 in `SurvivorDemoBuilder`, `VampireSurvivorDemoBuilder` and `AnimationFlyDemoBuilder` "World → Wallet" (change together with `UIDemoBuildersTests` `FindButton("Btn_World → Wallet")`), and '→' written into TMP texts at Play time by about 20 runtime demo scripts (`Samples~/Demo/Scripts/Shell/*DemoController.cs`, `GridSystemMatch3DemoUI`, `ProgressionDemoUI`, `RpgCharacterQuickDemo`, `AnimationFlyDemoController`).
- [ ] Missing builder tests in `Neo.Demo.Editor.Tests`: `PagesUIBuilder` (no Demo-assembly component, the four persistent calls, 1080x1920 match-width scaler, `NeoDemo.Pages.*` keys), `ExampleGameBuildersTests` for the three example-game builders (info card, no `StandaloneInputModule`, demo keys, idempotent second pass), and `ProgressionQuestDemoBuildersTests` checking that `PlayerInventory` / `Progression_Managers` are retired. `PagesEditorGUI` multi-object writes are IMGUI and untested.
- [ ] Sample tests: remove `SampleRuntimeSceneSmokeTests.CoreModuleDemoScenes_LoadAndTickWithoutRuntimeErrors` once `SampleScenePlaySmokeTests` has run green (it covers the same scenes); add `KnownSkips` entries only for scenes the first run proves host-dependent; run the Input System-only skip list on such a host; deduplicate the GUID regexes and hidden-meta scan of `SamplePlayScenes.cs` and `SampleAssetValidationTests.cs` once a shared test asmdef exists. `CountHiddenSampleScriptSlots` is a YAML approximation that can only over-count hidden slots.
- [ ] Demo tuning measured only in models: the Physics 3D pad (a Kick that lands mid-fall can stop a ball, about 1% after 12 kicks; Kick revives it), the magnet at the strength maximum, and the info-card layout of AnimatorsDemo, UIComponentsDemo, BonusRewardsDemo, Physics3DDemo, the example games and the portrait Pages card. Check them in the post-rebuild captures and a Play run.
- [ ] Move `DemoSaveKeys` from `Editor/Builders/ExampleGames` into `Editor/Kit` (a plain move with its `.meta`).

### Release verification still owed

- [x] Demo rebuild: all 46 scenes built, a second pass reports 0 created and 0 removed; scenes, data assets, materials and folder metas are committed (2026-11-04).
- [x] Suites on Unity 6000.3.14f1 with the samples visible: EditMode 5739 passed / 0 failed, PlayMode 951 passed / 0 failed, `Neo.Demo.Editor.Tests` 102/102, Performance 140/140 (recorded in `Docs/Audits/2026-11-02/Performance/MeasuredResults-12.0.md`).
- [x] Every rebuilt scene captured (Edit and Play Mode, portrait Pages); the captures illustrate the READMEs and module guides.
- [x] Multiplayer: the dedicated-server matrix (server + two client players over Telepathy) passed all 54 cases; every assembly compiles without `MIRROR`.
- [x] Commit hygiene: line-ending-only ProjectSettings and TMP fallback changes left out; the `Docs/Audits/2026-11-02/Evidence` zips are committed like the 2026-11-01 ones.
- [x] Unity 6000.6 compile of all 71 assemblies (covers the `UNITY_6000_5_OR_NEWER` branches).
- [ ] An Input System-only host run, an interactive focused Game view for the cursor and EOF cases, an IL2CPP player build, and the clean-consumer validation host with `-neoValidationExpectedPackageVersion 12.0.0`.
- [ ] HtmlToUitk: the repository <https://github.com/NeoXider/HtmlToUitk> is public but has no license yet (owner's decision).

## Closed by the 12.0.0 pass

Verified against the code on 2026-11-04 and removed from the open lists above:

- Abilities: the spatial grid is patched in place on `TryMoveUnit` and unregistration; the Neo.Core batch payment (`ResourcePoolModel.TrySpendAll`) shipped.
- Audio docs: the remaining **Purpose:** headers were converted to What it is / How to use.
- Cards: `BoardComponent` reuses its layout buffers (batch placement 14.8 µs per card, measured).
- Condition / StateMachine: `NeoConditionEditor` draws **Use Unscaled Time**; NoCode transitions are routed per `StateData` source, logging is per machine (`DebugLogEnabled`), and sorted transition lists are cached; `RpgConditionAdapter.Refresh()` exists.
- Editor: unresolved `[FindInScene]` / `[LoadFromResources]` lookups retry only after a change or every 0.5 s; `QuestConfigEditor` lists `ValidateDefinition()` issues; the mascot click opens the component's documentation page; the StateMachine, StateData, Quest Flow, Scene Saver and UI Mesh Rig adapter inspectors cache per repaint, and Scene Saver's per-save log is opt-in.
- GridSystem: Match3 has `RandomIndexProvider` and an allocation-free resolve and shuffle path.
- Input: `MouseInputCompat.TryGetScrollDelta` / `TryGetPointerDelta` and the touch fallback; the gated **Enable Hold** warning (`MouseEffect.RequiresHoldEvents`); `DefaultIndexedMinimumHits` = 8 confirmed by the small-hits benchmark (both paths within 0.03 ms below 8 hits); the ancestor-keyed index is not needed (large hierarchy: default 0.27 ms vs forced index 1.56 ms).
- Inventory / Spawner: a destroyed `PoolManager` retires its pools; Auto Save bursts coalesce through `InventoryComponent.DeferAutoSave()`.
- Network: `NeoLobbyPlayer.PlayerId` with an `[Obsolete]` `ConnectionId`; `NeoNetworkState.IsAuthorized` delegates to `NetworkAuthorityPolicy`; `NeoNetworkDiscovery.IsSearching`.
- Shop: buy-button unsubscribe uses `UnityEventDelegateCache.UnsubscribeAll()`; `ShopPurchaseButtonView.Refresh` uses the id index (no O(n²) lookups).
- Rpg: one network snapshot per simulation frame (publication scope); `RpgProjectile` uses `RpgPhysicsQueryBuffers`; RPG hit sorting no longer allocates a comparison delegate per cast.
- Save / Level: `SaveProviderSettingsComponent` runs at `[DefaultExecutionOrder(-1000)]`; `LevelManager.ResetProgress(mapId)` / `TryResetProgress` / `ResetAllProgress`; `LevelComponent` publishes XP before level and notifies each state once per call.
- Time / Dialogue / Random: `DialogueController.RevealMode` reveals through `maxVisibleCharacters`; the typewriter keeps its rate above one character per frame; `ChanceData.OnIdGenerated` is `[Obsolete]`.
- Tools: `Follow.TeleportToTarget` snaps only the followed channels; `CameraConstraint.IsAtEdge` reports only constrained axes (documented); `CameraAspectRatioScaler` has C# properties for every Inspector setting; `ErrorLogger.MaxCharacters`; Selector `SetRandom` / `SetRandomUnique` no longer allocate.
- UI: `UI.SetPageAnim` / `SetOnePageAnim` (**Use Unscaled Time**) and `ButtonChangePage` (unscaled tweens) work at time scale 0; `UIMeshRigSpriteRenderer` uploads indices only when the topology changes; `Neo.UI.FakeLoad` completes a one-shot revisit on the next frame and resumes after deactivation; `ButtonScale` releases to the scale at press start; `VariantView` applies its state in `Awake`; `AnchorMove.Apply` / `SetX` / `SetY` / `SetAnchor` and `CategoryBar.DisableEntry` / `EnableEntry` are UnityEvent targets.
- Bonus / Tools: `CooldownReward.SetRewardAvailableNow` raises `OnRewardAvailable`; `Box.ResetProgress()`; `Leaderboard.SubmitScore(int|float)`; `MagneticField.SetModeIndex` and `ImpulseZone.SetDirectionIndex`; the animators, `TimerObject` and `Neo.UI.FakeLoad` initialize their UnityEvents.
- Samples: the unused sample scripts are deleted (`QuestDemoView` is obsolete); `AnimatorsDemoController` drives the authored AnimatorsDemo scene; `SurvivorPlayerController` reads `MoveInputCompat`; builders replace `StandaloneInputModule` (the example games included); package prefabs no longer reference sample art; every sample scene uses `NeoDemo.*` save keys (`SampleSceneSaveKeyTests` has no known offenders left); `RpgWorldHealthBars` no longer allocates per frame or depends on the scene search; new demo scenes for Animations, UI components, Bonus rewards and Physics 3D; the NoCode example games and the Pages UI scene have info cards; the demo kit helpers (`ctx.OwnListeners`, `DemoSceneKit.Step`, `ctx.Retire`, `DemoPanel.ValueRow`, `DemoFrameOptions.UpgradeLegacyInputModule`) are in the kit.

## See Also

- [Ideas](IDEAS.md)
- [Demo scene builders](DemoSceneBuilders.md)
- [GridSystem](../Assets/Neoxider/Docs/GridSystem/README.md)
