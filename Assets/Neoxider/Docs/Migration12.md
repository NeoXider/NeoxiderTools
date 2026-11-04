# Upgrading to NeoxiderTools 12

**What it is:** the upgrade guide from 11.x to 12.0.0. It lists what was removed (the HTML/CSS → UI Toolkit converter), the APIs that became `[Obsolete]` with their replacements, source-compatibility notes, and the behaviour changes that an existing project can notice without changing any code. Release notes with every change live in the [changelog](../CHANGELOG.md).

**How to use:**
1. If your project uses the HTML/CSS → UI Toolkit converter or its runtime (`UitkScreen`, `UitkRouter`, `BreakpointWatcher`, `UitkAnimator`), install the new package first: [HtmlToUitk moved to its own package](#htmltouitk-moved-to-its-own-package).
2. Update NeoxiderTools, let it compile, and fix the new obsolete warnings with the [replacement table](#obsolete-apis-and-replacements).
3. Read the [behaviour changes](#behaviour-changes-to-review) for the modules your game uses. The ones marked **check** most often need a scene or code adjustment.
4. Re-import the samples you use: the demo scenes are now [authored in the scene](#samples-and-demo-scenes). If your own scenes reference sample art that package prefabs used, read [package assets moved out of the samples](#package-assets-moved-out-of-the-samples).

**Navigation:** [← Docs](./README.md) · [Upgrading from 10.x to 11](./Migration11.md) · [Changelog](../CHANGELOG.md)

---

## At a glance

- **Removed:** the `Neo.HtmlToUitk.*` assemblies, their tests, the Velora showcase and the converter CLI. They live in the separate `com.neoxider.htmltouitk` package now.
- **Not removed:** no other public type, member or serialized field of the package runtime was removed or renamed. In the Demo sample, `RpgCharacterQuickDemo` dropped its `_configureOnAwake` field on purpose, and the unused sample scripts `RpgDemoUI`, `ConditionDemoSetup`, `HealthTextDisplay`, `Network/TestStart` and the obsolete `SurvivorHud` were deleted. New fields are additive and keep their previous behaviour by default, except where a section below says the default changed on purpose (Audio mute persistence, unscaled UI feedback, NPC path staggering).
- **Obsolete (warnings only):** `VariantView.currentStateIndex`, `Map.GetCopmplete()`, `Neo.Editor.NeoEditorAutoRegister`, `ChanceData.OnIdGenerated`, and in the Demo sample `NeoDemoShell`, `ModuleDemoSceneInfo.Show` and `QuestDemoView`.
- **Moved:** package prefabs no longer use art that ships only in `Samples~`; the package carries its own copies under `Assets/Neoxider/Art` and `ThirdParty/CharacterMovementFundamentals/Art` ([moved assets](#package-assets-moved-out-of-the-samples)).
- **Samples:** every module demo scene is saved with its UI and wiring. Nothing is built at Play start, and an editor builder can rebuild each scene from the **Neoxider > Demo** menu.

## HtmlToUitk moved to its own package

The HTML/CSS → UI Toolkit converter is no longer part of `com.neoxider.tools`. These parts moved to the standalone package `com.neoxider.htmltouitk` ([HtmlToUitk](https://github.com/NeoXider/HtmlToUitk)):

| Removed from NeoxiderTools | Where it is now |
|---|---|
| Assemblies `Neo.HtmlToUitk.Runtime`, `Neo.HtmlToUitk.Editor` | Same assembly names in `com.neoxider.htmltouitk` |
| Test assembly `Neo.HtmlToUitk.Tests.Editor` | The new package's own tests |
| Velora showcase (`Assets/HtmlToUitkShowcase`, repository only) | The new repository |
| .NET CLI (`Tools/HtmlToUitk`, repository only) and the `HtmlToUitk.Cli` release zip | The new repository and its releases |
| Pages `Docs/UI/HtmlToUitk*.md` | `Documentation~/` in the new package |

To keep using it, add the package from its Git URL (**Package Manager → + → Add package from git URL**):

```text
https://github.com/NeoXider/HtmlToUitk.git
```

Assembly names, namespaces, script GUIDs, menu paths and `EditorPrefs` keys are unchanged, so scenes, prefabs and assembly definitions that reference the converter resolve again once the package is installed. Converter warnings are now switched on with `HtmlToUitkDiagnostics.Configure(warnings: true)`; `NeoDiagnostics.Configure(warnings: true)` no longer covers them. Do not keep an old bundled copy of `Assets/Neoxider/Scripts/UI/HtmlToUitk` next to the new package: the duplicate assembly names will not compile.

**Expect one full texture re-import.** The converter carried a texture `AssetPostprocessor` (`OnPreprocessTexture`). Unity rebuilds every texture artifact when the set of texture postprocessors in a project changes, so the first open after updating from 10.19–11.x re-imports all textures once. The textures and their import settings do not change; only the first import takes longer in large projects. If that first open runs in batch mode, leave out `-nographics`: in a field test on Unity 6000.6, the texture importer crashed under `-nographics` on a third-party texture with legacy import settings and finished cleanly with a graphics device.

## Obsolete APIs and replacements

All of them still compile and work; they produce warnings, never errors.

| Obsolete | Use instead | Notes |
|---|---|---|
| `VariantView.currentStateIndex` | `VariantView.CurrentStateIndex` | React to changes with the new `StateChanged` C# event or the `OnStateChanged` UnityEvent instead of polling. |
| `Map.GetCopmplete()` (Level) | `Map.IsComplete` | Same result; the old name was misspelled. |
| `Neo.Editor.NeoEditorAutoRegister` | Nothing | The class is empty; `NeoCustomEditor` is discovered by Unity directly. Delete calls to it. |
| `ChanceData.OnIdGenerated` (Tools/Random) | `ChanceSystemBehaviour.OnIdGenerated` / `OnIndexSelected` with the asset assigned, or the index returned by `ChanceData.GenerateId()` | A ScriptableObject must not carry scene wiring. Listeners already saved on the asset still load (`[FormerlySerializedAs]`) and fire. |
| `NeoDemoShell.Build(...)`, `NeoDemoShell.ShowInfoCardOnce(...)` (Demo sample) | An editor scene builder with `DemoSceneKit.Frame` and `frame.InfoCard` | Runtime-built demo frames still work as a fallback. See [Samples](./Samples.md#how-demo-scenes-are-made). |
| `ModuleDemoSceneInfo.Show(...)` (Demo sample) | `frame.InfoCard(info, content)` in a builder | The info card is authored in the scene. |
| `QuestDemoView` (Demo sample) | `QuestDemoController` in the authored QuestDemo scene | No shipped scene uses it. `SurvivorHud`, obsolete earlier in this cycle, was deleted; use `SurvivorHudView`, authored by `SurvivorDemoBuilder`. |

Older obsolete APIs that still exist are listed in [Deprecated components and removal candidates](./DEPRECATED_OR_REMOVAL_CANDIDATES.md).

## Source-compatibility notes

These can change how existing code compiles or which exception it sees:

- **StateMachine:** `StateMachineBehaviourBase` and `StateMachineBehaviour<TState>` gained `ChangeState(StateData)`. A literal `wrapper.ChangeState(null)` is now ambiguous (CS0121); write `ChangeState((string)null)` or call the overload you mean.
- **Extensions:** `RandomExtensions.GetRandomEnumValue<T>()` throws `InvalidOperationException` for an enum without values (it was `IndexOutOfRangeException`). `GameObjectArrayExtensions.SetActiveAtIndex` with a `null` source returns `null` and logs a gated warning instead of throwing.
- **Cards:** `PokerHandEvaluator.Evaluate(null)` throws `ArgumentNullException` with parameter name `cards`.
- **Abilities:** `AbilityEventBus.Publish` ignores an event without an id instead of throwing `ArgumentNullException`.
- **Save:** `FileSaveProvider.TryChangeSlot(null)` or an empty/whitespace name returns `false` instead of throwing or using an invalid path.
- **Progression:** callers that pass a list or array to `ProgressionRewardDispatcher.DispatchRewards` now bind to the new allocation-free `IReadOnlyList<ProgressionReward>` overload; results are the same.
- **Interaction:** the diagnostic `InteractionPhysicsQueries.Statistics.ClassifiedHits` now counts the raw hits actually inspected, so the same scene reports smaller numbers. Do not compare it with values recorded on 11.x.
- **Random:** `ChanceData.OnIdGenerated` is an `[Obsolete]` property over a private serialized field (it was a public field). `data.OnIdGenerated.AddListener(...)` and `?.Invoke` still compile with a warning; passing it by `ref`/`out` or finding it through field reflection does not.
- **Assemblies:** `Neo.Tools.Draw` now references `Neo.Tools.Input`, `Neo.Tools.Time` references `Unity.TextMeshPro`, and `Neo.Haptics` references `Neo.Extensions`. No cycle was introduced; this only matters if you copy these assemblies selectively.

## Behaviour changes to review

Grouped by module. Items marked **check** are the ones most likely to need an adjustment in an existing game.

### Audio

- **check** `AMSettings` now saves the mute state (Music, Efx, Master, UI channel) under `saveKeyMute` (default `Neo.Audio.AMSettings.Mute`). **Persist Mute** is on by default, also on existing components, and needs **Persist Volume**. `SetMusic`, `SetEfx` and the toggle methods are therefore a saved player preference. For temporary silence (pause menu, ad, cutscene) use `AudioListener.pause`, a mixer snapshot or [AudioFocusMute](./Audio/AudioFocusMute.md), or turn **Persist Mute** off. See [AMSettings](./Audio/AMSettings.md).
- Binding to `AM` no longer forces Music and Efx back on. A `SetMusic`/`SetEfx` call made before binding (for example in your `Awake`) is kept; otherwise the saved mute, then the sources' own state, is used.
- `SetMasterVolume` while Master is muted only changes the level that unmuting restores.
- The AMSettings mute buttons in the Inspector work in Play Mode only, so they no longer write a mute into scenes or prefabs.
- `RandomMusicController` skips empty track slots instead of ending the playlist.

### UI

- **check** `ButtonScale`, `ButtonShake` and `FakeLoad` (`Neo.UI`) animate in unscaled time by default, including in existing scenes, so press feedback and fake loaders keep working while `Time.timeScale` is 0 or in slow motion. Untick **Use Unscaled Time** (or set `UseUnscaledTime = false`) to keep scaled time.
- `ButtonShake` restores the position captured when the shake started and no longer resets the position in `OnEnable`, so buttons placed by a layout group do not jump back.
- `FakeLoad.Load()` during a running load restarts it, so `OnFinisLoad` fires once.
- **check** In one-shot mode a later `FakeLoad.Load()` in the same session completes one frame after the call in Play Mode instead of inside it, and a load interrupted by deactivating its GameObject continues when the object is active again (the same fix as the Pages `FakeLoad`). Code that read the result right after `Load()` must wait for `OnFinisLoad` or a frame; `IsLoading` tells whether a load is pending.
- `VariantView` applies its saved state in `Awake`, so the Images, colours, texts and GameObjects of an instance spawned or loaded at runtime match its **Current State Index** before the first change.
- `ButtonScale` returns to the scale the button had when the press started, not the scale captured in `Awake`.
- **check** `AnimationFly.Play(request)` works on a copy of the request. Set every field, including `OnAllArrived`, before calling `Play`; later edits do not reach the running flight, and a `Type` request no longer gets its resolved `EndTransform` written back. Exceptions thrown by your callbacks are logged and the flight still completes.
- `CategoryBar.Select`/`Next`/`Prev` called before `Start` become the initial selection instead of being replaced by **Start Index**.

### Abilities

- **check** Custom `IEffectOperation` implementations must not keep the `EffectContext`, its `PrimaryTargets`, its `Random` or a `CreateNested` child after `Execute` returns: contexts created by the system for casts, projectile hits, modifier ticks and reactions are pooled. Copy the values you need. A released context is ignored with a gated warning.
- **check** Modifier tick and event-reaction rolls after the first use their own derived seeds (the first roll is unchanged). Replays or lockstep sessions recorded with 11.x diverge after a modifier's second tick or reaction and must be recorded again.
- `ModifierEngine.VersionOf` values come from one engine-wide counter. Compare them for equality only; they read 0 after `DestroyUnit`/`ForgetOwner`.
- The new spatial grid is opt-in: `AbilitySystemBehaviour` **Spatial Cell Size** 0 keeps the previous linear search.

### RPG

- `AttackSpeedPercent` buffs now have a consumer: `RpgCharacter.GetAttackSpeedMultiplier()`. `RpgAttackController` divides new cooldowns by it only when **Scale Cooldown By Attack Speed** is on (off by default), so existing content keeps its timing.
- **check** `RpgContactDamage.OnTargetFound` now fires when a target first comes into range, and `OnTargetLost` fires once per loss. Listeners that were never called before will run.
- `RpgEvadeController.IsEvadingState` and `RemainingCooldownState` publish once on the first frame after enable and then notify only on change (they re-notified every frame).
- Direct 3D casts apply hits nearest-first, so `MaxTargets` drops the farthest receivers. `AuraWeapon` skips a pulse started from a damage callback of a running pulse. `RpgAutoAttackController.Start` no longer replaces a target set with `SetTarget`.
- `AddXp` ignores NaN and Infinity; `AddUpgradePoints`, `AddLevel` and level-up grants saturate instead of wrapping.

### NPC

- **check** `NpcRpgCombatBrain.OnChaseStarted` fires once per chase, and `onTargetChanged`/`onSpeedChanged` no longer re-fire on every decision tick during a chase.
- In Combined mode an unreachable chase is retried after **Path Update Interval** instead of every frame.
- **Stagger Path Updates** (new, default on, also on existing prefabs) spreads the periodic re-paths of NPCs spawned together.

### Shop, Inventory and pooling

- `ShopListView.OnRefreshed` fires once per purchase or bundle (it fired three times, or N+1 times for bundles). `Shop.SetItems` spawns rows for new catalog entries when Auto Spawn and a prefab are set. `TryBuyBundle` reports `AlreadyOwned` for an owned single-purchase bundle.
- **check** `InventoryHand` no longer re-publishes `Selector.OnSelectionChanged` when an inventory change leaves the selected slot index unchanged. To react when a different item lands in the selected slot, subscribe to the new `InventoryComponent.OnSelectedItemChanged`.
- Inventory count labels skip writing an unchanged value; text changed by other code is rewritten on the next refresh.
- `SpawnUtility` fallback pools survive objects that the incoming scene spawns in its own `Awake`/`OnEnable`; pools whose storage died with the previous scene are retired.

### Tools

- **check** `MagneticField` **Max Overlap Colliders** is now the initial size of a growing buffer, not a hard cap: every collider in range is processed. Bound the cost with **Affected Layers**, the radius or the update interval.
- `DistanceChecker` fires `onDepart` once when its target or source is destroyed or cleared while inside the threshold.
- `Follow`: the 2D Z rotation limit clamps relative to the centre of the authored sector (asymmetric limits snap to the nearer edge), and `TeleportToTarget` applies position limits and resets the smoothing velocity.
- **check** `Follow.TeleportToTarget` snaps only the channels the follower follows: a rotation-only follower (a turret) keeps its position and turns straight to the aim instead of jumping onto the target, and a position-only follower keeps its rotation.
- `AdvancedForceApplier` with **Clamp Max Speed** also clamps during the two physics steps after `ApplyForce`.
- `PlayerController2DPhysics.Teleport` / `PlayerController3DPhysics.Teleport` also move the Rigidbody. The 2D/3D animator drivers skip parameters the Animator Controller does not declare.
- `Drawer` reads the pointer through `MouseInputCompat`, so it works in Input System-only projects.
- **check** `ErrorLogger` history is capped by **Max Entries** (default 50, previously unlimited). Set 0 for the old behaviour.
- `Loot` no longer drops items while its scene unloads or the application quits.
- **check** `CameraAspectRatioScaler` stores its reference size and field of view on the component. Existing scenes capture the camera once on their next initialization; this fixes the camera size creeping up after an Edit Mode preview.
- `Bootstrap` finishes its startup pass when an `IInit.Init` throws; the failing target is set aside. Call `InitializePending()` to retry and read `IsBootstrapCompleted`.
- `TimerObject` **Time Text** (TMP) is now compiled (it sat behind a define that was never set) and must be assigned explicitly; it is never auto-found. An invalid **Time Format** shows the default format with one gated warning instead of throwing.
- `GameTimeController.PauseGame()` remembers the previous non-zero speed for `TogglePause()`; `SetTimeScale(0)` pauses like `PauseGame()`; `ResumeGame()` still resumes at speed 1.
- `ChanceManager` Inspector weights are no longer clamped to a 0–1 slider; values above 1 are kept as raw weights.

### Extensions, Core, Reactive and Haptics

- **check** `StringExtension.ToFloat`/`ToInt` parse the invariant culture first and fall back to the device culture. On locales with a dot as the thousands separator, `"1.500"` now reads as 1.5. `ToBool` compares ordinally.
- `TransformExtensions.LookAt2D` does nothing for a null transform or a target at the same XY position; `GetClosest` returns `null` for a null transform.
- Camera parameters of `ScreenExtensions` and `UIUtils.WorldToCanvasPoint` treat a destroyed `Camera` like `null` and fall back to `Camera.main`.
- `ResourcePoolModel.Tick` no longer raises `OnResourceChanged` for regen or heal cycles that add nothing, so `HealthComponent.OnChanged` stops firing every interval at full health.
- A `ReactiveProperty` weak subscription whose Unity target was destroyed is removed silently (strong subscriptions still warn).
- Exceptions thrown by a `Haptics.Played` observer are logged and no longer cancel the pulse or reach the caller.

### Bonus

- `WheelFortune` with **Single Use** off no longer locks its own CanvasGroup after the first spin.
- `CooldownReward.SetRewardAvailableNow()` raises `OnRewardAvailable`, like `TimeReward` does.
- The obsolete `TimeReward` keeps its running or paused state while its object is inactive.
- `Collection.Set` is read-only (`IReadOnlyCollectionSet`): change a collection through the component so it saves and raises its UnityEvents. An injected random source that returns an out-of-range index throws instead of granting the lowest locked slot.

### Cards

- `DeckComponent.Reset()`/`Initialize()` clear the visual stack board; `ReturnCard` removes the card from the hand or board that holds it.
- **check** Scattered layouts and layout jitter are deterministic per card index and no longer consume `UnityEngine.Random`, so existing scattered layouts look different and your game's random sequence is no longer shifted by layout passes.
- Hand sorting is stable. `BoardComponent.GetAllRanks()`/`GetAllCardData()` skip empty or destroyed entries, and a card moved to another parent during `PlaceCardAsync` is dropped from the board.

### GridSystem

- `TicTacToeBoardService` raises `FieldGenerator.OnCellStateChanged` for moves and resets, after the round state (winner, next player) is committed.
- `SlidingMergeResolver.Slide`, `SlidingMergeBoardService.ClearBoard`, `DiceBoard.ClearBoard` and `GridSlotAllocator.Clear` notify after every cell is written, so listeners no longer see half-updated boards.
- On gapless rectangular grids `GetNearestCellFromWorld` uses a constant-time lookup and can pick the other cell on an exact shared edge. Custom path predicates run at most once per cell per query.

### Quest and Progression

- **check** `QuestManager.Notify`/`NotifyKill`/`NotifyCollect`/`NotifyReachPoint`/`NotifyTalk` with a null or empty target id credit nothing. Credit objectives without a Target Id by index with `AddObjectiveProgress` or `CompleteObjective`.
- If an objective listener throws, the quest still completes when that credit finished its last objective, the completion events still fire, and the exception is rethrown afterwards. Auto Save writes once per top-level operation.
- Progression unlocks and purchases with several rewards publish one settled state before `OnNodeUnlocked`/`OnPerkPurchased`.

### Save, Settings and Level

- `FileSaveProvider.Save()` with nothing new skips the disk write but still raises `OnDataSaved`; setting the value a key already holds raises `OnKeyChanged` without marking the file dirty. Integers are written in the invariant culture; values written with the device culture by older builds are still read.
- **check** `GlobalSave.SaveProgress()` does nothing while `GlobalSave.data` was never loaded or assigned (it used to overwrite the stored profile with an empty one). Assigning `null` stores a fresh `GlobalData`.
- `SaveProviderSettingsComponent` always logs a rejected configuration, even with save debug logging off.
- `SaveManager` flushes on both focus loss and pause, even in the same frame.
- A Deferred settings save whose coroutine was stopped from outside is restarted by the next change; `GameSettingsComponent.HasPendingDeferredSave` reports it.
- `LevelComponent` notifies `XpState` and `XpToNextLevelState` before `LevelState` and `OnLevelUp`, so XP bindings already show the new values inside a level callback. Each `AddXp`/`SetLevel` call notifies every reactive state exactly once, also when an `OnLevelUp` listener adds XP; a listener that counted notifications sees fewer.

### Condition, NoCode and StateMachine

- **check** `NeoCondition` **EveryFrame** mode throttles on unscaled time and keeps checking (and firing its events) while `Time.timeScale` is 0. Under Mirror ServerRevalidate it keeps sending server checks during a pause. **Interval** mode still follows game time unless the new **Use Unscaled Time** is on.
- `NoCodeBindText` no longer re-writes an unchanged value on every poll, so SetText animations are not restarted; call `ForceRewrite()` to push again.
- A machine configured on a `StateMachineBehaviourBase`/`StateMachineBehaviour<TState>` before `Awake` survives activation. Loading `StateMachineData` enters the assigned Initial State asset directly. If a target state's constructor changes or stops the machine, that newer choice wins.
- `StateData` action lists run only the entries present when a pass starts; actions added during a pass run on the next pass.

### Network

- Use the same NeoxiderTools version on every peer.
- `NetworkContextActionRelay` sends AllClients/OthersOnly actions only to the observers of the relay's `NetworkIdentity` (the same audience as a ClientRpc). On a host it sends to remote observers before applying locally, and listener exceptions while applying a received message are logged instead of disconnecting the sender.
- `NetworkActionRelay` OthersOnly sends to the relay's observers instead of every connection.
- `NeoNetworkDiscovery` advertises once per server or host session start with any network manager, stops advertising with the server, and no longer re-broadcasts searches while a session runs. `StartAdvertising` is a no-op while already advertising.
- `NetworkEventDispatcher` and `NetworkPlayerName` reject remote commands while their server-side **Is Networked** is off. `NeoNetworkBootstrap` can start again after a session ended without `StopNetwork()`.

### Editor

- A `[Button]` action runs on every selected object (static methods once) and is recorded with Undo in Edit Mode.
- Auto-assigned `[GetComponent]`/`[FindInScene]`/`[LoadFromResources]` references are recorded with Undo; their warnings are logged once per object and field.
- `NeoxiderSettings.SaveSettings()` no longer refreshes the AssetDatabase, and the scene hierarchy template is saved in `ProjectSettings/NeoxiderSettings.json`.
- The update checker logs request details only with the EditorPrefs switch `Neo.UpdateCheck.Verbose`.
- Scene Saver no longer writes a console line for every auto-save; tick **Log Each Auto-Save** in its window to get them back. A failed save is always logged.
- A click on the mascot chip of a Neo inspector header opens the component's documentation page; clicks elsewhere on the header only animate the mascot.

## New APIs worth adopting

Additive; nothing above requires them. Each module page documents the details.

| Need | API | Page |
|---|---|---|
| Deterministic time in tests and replays | `IFrameClock`, `ManualFrameClock`, `INeoClock`, `NeoTrustedTime.AsClock` | [Clocks](./Tools/Time/Clocks.md) |
| Exact odds such as 1 in 1,000,000 | `IntegerChanceTable` | [IntegerChanceTable](./Tools/Random/IntegerChanceTable.md) |
| Bark lines without repeats, contextual interrupts | `DialogueLinePool`, `DialogueLinePlayer` | [DialogueLinePool](./Tools/Dialogue/DialogueLinePool.md) |
| 64-bit values (money in cents, large counters) | `SaveProvider.GetLong`/`SetLong`, `ILongSaveProvider` | [SaveProvider](./Save/SaveProvider.md) |
| Bonus mechanics without a scene | `CollectionSet`, `BoxProgress`, `CooldownSchedule`, `SlotSpinMath` | [Bonus](./Bonus/README.md) |
| Typed purchase result | `Shop.TryBuy`/`TryBuyBundle` → `ShopPurchaseStatus` | [Shop](./Shop/Shop.md) |
| Typed cast result | `AbilityCasterBehaviour.Cast(CastRequest)` → `CastResult`, `LastCastResult` | [Abilities](./Abilities/README.md) |
| Hundreds of ability units | `AbilitySystemBehaviour.SpatialCellSize`, `AbilitySpatialGrid` | [Abilities](./Abilities/README.md) |
| Shields, armour and damage caps | `IRpgDamageModifier`, `RpgCharacter.ApplyDamage(RpgDamageInfo)`, `DamageApplied` | [RpgCharacter](./Rpg/RpgCharacter.md) |
| Movement ranges and flood fill | `GridPathfinder.TryFindPath`, `CollectReachableCells` | [GridPathfinder](./GridSystem/GridPathfinder.md) |
| Seeded deals, custom card sorting | `DeckModel(seed)`, `HandComponent.SortAsync(IComparer<CardData>)`, `CardComparer` | [Cards](./Cards/README.md) |
| Quest credit from prefabs by id | `QuestManager.Notify*`, `QuestNoCodeAction` Notify actions | [Quest](./Quest/README.md) |
| Testable authority rules without Mirror | `NetworkAuthorityPolicy.IsKnown` / `IsAuthorized` (`Neo.Network.Contracts`) | [Network contracts](./Network/Contracts.md) |
| UnityEvent targets for enum or two-argument setters | `MagneticField.SetModeIndex`, `ImpulseZone.SetDirectionIndex`, `CategoryBar.DisableEntry` / `EnableEntry`, `AnchorMove.SetX` / `SetY` / `Apply`, `Leaderboard.SubmitScore`, `Box.ResetProgress` | [UI](./UI/README.md), [Physics](./Tools/Physics/README.md) |

## Package assets moved out of the samples

Package prefabs and assets used to reference sprites, materials and animation clips that exist only inside `Samples~`, so a UPM install without the imported samples showed missing references. The package now carries its own copies. They have **new GUIDs**; the package prefabs already point to them.

| Was (sample source, under `Assets/Neoxider/Samples~/`) | Now (package, under `Assets/Neoxider/`) | Used by |
|---|---|---|
| `Demo/Sprites/Frame 3.png` … `Frame 6.png`, `Group 2.png`, `Layer 4.png` (kept in the sample too) | `Art/Bonus/Slot/` (same file names) | `Prefabs/Bonus/Slot/Row`, `SlotElement`, `SlotUI`, `Resources/Slot/SpritesDafualt.asset` |
| `Demo/Sprites/Group 2-1.png`, `Demo/Sprites/Layer 11.png` (removed from the sample) | `Art/Bonus/Slot/Group 2-1.png`, `Art/Bonus/Slot/Layer 11.png` | `Prefabs/Bonus/Slot/SlotUI` |
| `Demo/Sprites/Clicker/Group 132.png`, `Group 133.png` (removed from the sample) | `Art/Shop/Group 132.png`, `Art/Shop/Group 133.png` | `Prefabs/Shop/ItemShop` |
| `Demo/Sprites/Clicker/Resource 1@4.png` (kept in the sample too) | `Art/Shop/Resource 1@4.png` | `Prefabs/Shop/ItemShop` |
| `Demo/Materials/yellow.mat`, `red.mat` (kept in the sample) | `Art/Materials/Neo Yellow.mat`, `Neo Red.mat` | `Prefabs/Bullet`, `Prefabs/Simple Weapon`, `Prefabs/Tools/AI/OLD AI NPC` |
| `CharacterMovementFundamentals/Capguy/Animations/Fall, Idle, Jump, Land, WalkBackward, WalkForward, WalkLeft, WalkNeutral, WalkRight.fbx` (removed from the sample) | `ThirdParty/CharacterMovementFundamentals/Art/Capguy/Animations/` | `ThirdParty/.../Art/Capguy/CapguyAnimator.controller` |
| `CharacterMovementFundamentals/Capguy/Model/Textures/Tex_Cap.png`, `Tex_Capguy.png` (removed from the sample) | `ThirdParty/CharacterMovementFundamentals/Art/Capguy/Textures/` | `Mat_Cap.mat`, `Mat_Capguy.mat` |
| `CharacterMovementFundamentals/Environment/Textures/Tex_Grid_Red.png` (removed from the sample) | `ThirdParty/CharacterMovementFundamentals/Art/Environment/Textures/Tex_Grid_Red.png` | `Mat_Grid_Red.mat` |

What to do:

- If you only use the package prefabs, nothing: they resolve with or without the samples.
- An imported 11.x sample copy (`Assets/Samples/NeoxiderTools/11.x/...`) keeps its own files and GUIDs, so your references into it keep working while you keep that folder. If you delete it and import the 12.0.0 sample instead, references to the files marked "removed from the sample" go missing: point them to the package copies in the middle column.
- `Neo Yellow.mat` / `Neo Red.mat` are copies with new names; the sample's `yellow.mat` / `red.mat` are unchanged.

## Samples and demo scenes

- Every module demo in **Demo Scenes** (and the NeoxiderPages UI scene) is saved with its UI, configured components and persistent UnityEvent wiring, so you can inspect the setup without entering Play Mode. The runtime bootstraps were removed or reduced to logic; `GridSystemMatch3DemoSetup`, `GridSystemTicTacToeDemoSetup` and `DiceMergeDemoSetup` were deleted, and `ShopDemoBootstrap` became `ShopDemoController` (same script GUID).
- Imported sample copies are not updated by a package upgrade. Re-import the samples you use, after saving any changes you made to them. See [Samples](./Samples.md#how-demo-scenes-are-made) for how to rebuild a scene from the **Neoxider > Demo** menu.
- Demos now use demo-only save keys (for example `Neo.Demo.Settings.`, `Neo.Demo.Audio.*`, `NeoDemo.Quests`, and `neo_save_demo.json` for the Save demo), so running them no longer overwrites your game's `Neo.Settings.` keys or default PlayerPrefs data. Keys written by older demo runs stay where they are.
- Text-based demos need **TMP Essential Resources**, and the Demo collection needs `com.unity.2d.sprite`; see [sample prerequisites](./Samples.md#host-and-sample-prerequisites).
- New scenes: `Scenes/Animations/AnimatorsDemo.unity`, `Scenes/UI/UIComponentsDemo.unity`, `Scenes/Bonuses/BonusRewardsDemo.unity` and `Scenes/Tools/Physics/Physics3DDemo.unity`. The NoCode reference games (`_ExampleGame/adventure`, `ClickerExample`, `Shooter2D`) and the NeoxiderPages `UI.unity` scene now carry an info card ([Samples](./Samples.md#module-demo-scenes)).
- **check** The example games save under demo keys: `NeoDemo.Adventure.Inventory`, `NeoDemo.Clicker.Money` / `.Shop` / `.BestScore` / `.Level`, `NeoDemo.Shooter2D.Money` / `.BestScore` / `.Level`, and `NeoDemo.Pages.Money` / `.BestScore` / `.Level` for the Pages UI scene. ClickerExample progress saved by older versions under `Money` / `Shop` is not carried over, and Progression_Demo no longer touches `Inventory_Default`.
- Deleted sample scripts (used by no scene or prefab): `RpgDemoUI`, `ConditionDemoSetup`, `HealthTextDisplay`, `Network/TestStart` and `SurvivorHud`. If a scene of yours used one, copy it from the 11.x sample. `AnimatorsDemoController` is now the driver of the authored AnimatorsDemo scene and is hidden from Add Component.
- `SurvivorPlayerController` reads movement through `MoveInputCompat`, so SurvivorDemo runs with Active Input Handling = Input System Package (New). The Character Movement Fundamentals example scenes still need **Both**.
- **check** NeoxiderPages (sample): `PM.SetPage` raises `OnPageChanged` once instead of twice, and page switches log only with **Debug Logs** on. The Pages `FakeLoad` uses unscaled time by default (existing instances included; untick **Use Unscaled Time** to keep scaled time) and completes a one-shot load one frame after `Load()`. A `BtnChangePage` whose Action still holds a legacy UIKit.Page value opens its Target Page again and warns once; pick **Use Open Page** in its inspector to silence it.

## Tests

- Zero-allocation tests count allocations with Unity's GC.Alloc recorder (`Is.Not.AllocatingGCMemory()` or a calibrated counter). In the Unity 6 Editor `System.GC.GetAllocatedBytesForCurrentThread()` reads 0, so assertions built on it cannot fail; rewrite such checks in your own tests.
- Benchmarks are explicit tests in the `Performance` category and print `NEO_PERF` lines; they stay out of normal runs.
- The editor validation host needs an explicit `-neoValidationExpectedPackageVersion`. See [Testing](./Testing.md).

## По-русски

Конвертер HTML/CSS → UI Toolkit вынесен в отдельный пакет `com.neoxider.htmltouitk`: установите его по Git URL, имена сборок, namespaces и GUID сохранены. Других удалений публичного API нет (в сэмпле Demo удалены неиспользуемые скрипты `RpgDemoUI`, `ConditionDemoSetup`, `HealthTextDisplay`, `TestStart` и `SurvivorHud`); устаревшие члены (`VariantView.currentStateIndex`, `Map.GetCopmplete()`, `ChanceData.OnIdGenerated`, `NeoDemoShell`) дают только предупреждения. Префабы пакета больше не ссылаются на арт из `Samples~`: копии лежат в `Assets/Neoxider/Art` и `ThirdParty/CharacterMovementFundamentals/Art` с новыми GUID (таблица [перенесённых ассетов](#package-assets-moved-out-of-the-samples)). Проверьте изменения поведения по своим модулям, особенно отмеченные **check**: AMSettings теперь сохраняет mute, `ButtonScale`/`ButtonShake`/`FakeLoad` по умолчанию в unscaled time, `AnimationFly.Play` копирует запрос, одноразовый `FakeLoad` завершает повторный `Load()` на следующем кадре, `StringExtension.ToFloat` сначала читает invariant culture. Для UnityEvent добавлены одноаргументные методы (`SetModeIndex`, `SetDirectionIndex`, `DisableEntry`/`EnableEntry`, `AnchorMove.SetX`/`SetY`, `Leaderboard.SubmitScore`, `Box.ResetProgress`). Демо-сцены теперь собраны и сохранены редакторными билдерами; переимпортируйте сэмплы.
