# Upgrading to NeoxiderTools 12

**What it is:** the upgrade guide from 11.x to 12.x (12.0.0 to 12.4.0), and the one page that records what an existing project notices in 12: removals, obsolete APIs, source-compatibility notes and behaviour changes. Every change is in the [changelog](../CHANGELOG.md).

**How to use:**
1. If you use the HTML/CSS → UI Toolkit converter or its runtime (`UitkScreen`, `UitkRouter`, `BreakpointWatcher`, `UitkAnimator`), install its package first: [HtmlToUitk moved](#htmltouitk-moved-to-its-own-package).
2. Update NeoxiderTools ([installation](./GettingStarted.md#installation)), let it compile and fix the warnings with the [replacement table](#obsolete-apis-and-replacements).
3. Read the [behaviour changes](#behaviour-changes-to-review) for your modules, then [12.1–12.4](#changes-in-121124). Items marked **check** most often need an adjustment.
4. Replace imported samples with the demo downloads: [Samples and demo scenes](#samples-and-demo-scenes).
5. If your scenes use sample art that package prefabs used, read [package assets moved out of the samples](#package-assets-moved-out-of-the-samples).

**Navigation:** [← Docs](./README.md) · [Upgrading from 10.x to 11](./Migration11.md) · [Changelog](../CHANGELOG.md)

---

## At a glance

| Change | Since | What to do |
|---|---|---|
| The converter (`Neo.HtmlToUitk.*`, its tests, the Velora showcase, the CLI) moved to `com.neoxider.htmltouitk`. | 12.0.0 | [Install it](#htmltouitk-moved-to-its-own-package) if you use it. |
| No other public type, member or serialized field of the runtime was removed or renamed; new fields keep the old behaviour, except three deliberate defaults: Audio mute persistence, unscaled UI feedback, NPC path staggering. | 12.0.0 | Read [behaviour changes](#behaviour-changes-to-review). |
| A few APIs are obsolete (warnings only). | 12.0.0 | [Replacements](#obsolete-apis-and-replacements). |
| Package prefabs have their own art copies with new GUIDs. | 12.0.0 | Only if your scenes used that sample art: [moved assets](#package-assets-moved-out-of-the-samples). |
| Git installs use `#upm`; demos are separate downloads in `Assets/NeoxiderDemos`; no UPM samples. | 12.2.0 | [Replace old sample copies](#samples-and-demo-scenes). |
| Demo scenes are plain saved scenes; scene builders and the **Neoxider > Demo** menu are gone. | 12.3.0 | Re-import a demo to restore it. |
| `SurvivorDemo` is a NoCode game with seven weapons and no game scripts. | 12.3.0, 12.4.0 | Only if you used its scripts: [removed demo scripts](#removed-and-renamed-demo-scripts). |

## HtmlToUitk moved to its own package

| Removed from NeoxiderTools | Where it is now |
|---|---|
| Assemblies `Neo.HtmlToUitk.Runtime`, `Neo.HtmlToUitk.Editor` | Same assembly names in `com.neoxider.htmltouitk` |
| Test assembly `Neo.HtmlToUitk.Tests.Editor` | The new package's own tests |
| Velora showcase (`Assets/HtmlToUitkShowcase`, repository only) | The new repository |
| .NET CLI (`Tools/HtmlToUitk`, repository only) and the `HtmlToUitk.Cli` release zip | The new repository and its releases |
| Pages `Docs/UI/HtmlToUitk*.md` | `Documentation~/` in the new package |

1. Delete any old copy of `Assets/Neoxider/Scripts/UI/HtmlToUitk`: duplicate assembly names do not compile.
2. Add the package (**Package Manager → + → Add package from git URL**):
   ```text
   https://github.com/NeoXider/HtmlToUitk.git
   ```
3. Turn converter warnings on with `HtmlToUitkDiagnostics.Configure(warnings: true)`; `NeoDiagnostics.Configure(warnings: true)` no longer covers them.

Assembly names, namespaces, script GUIDs, menu paths and `EditorPrefs` keys are unchanged, so existing references resolve once the package is installed.

**Expect one full texture re-import.** The converter had a texture `AssetPostprocessor` (`OnPreprocessTexture`); when the set of texture postprocessors changes, Unity rebuilds every texture artifact. The first open after updating from 10.19–11.x therefore re-imports all textures once, without changing them. If that open runs in batch mode, leave out `-nographics`: on Unity 6000.6 the texture importer crashed under `-nographics` on a third-party texture with legacy import settings, and finished with a graphics device.

## Obsolete APIs and replacements

All of them still compile and work, with warnings.

| Obsolete | Use instead | Notes |
|---|---|---|
| `VariantView.currentStateIndex` | `VariantView.CurrentStateIndex` | React with the `StateChanged` C# event or `OnStateChanged` UnityEvent instead of polling. |
| `Map.GetCopmplete()` (Level) | `Map.IsComplete` | Same result; the old name was misspelled. |
| `Neo.Editor.NeoEditorAutoRegister` | Nothing | Empty class; Unity discovers `NeoCustomEditor` itself. Delete calls. |
| `ChanceData.OnIdGenerated` (Tools/Random) | `ChanceSystemBehaviour.OnIdGenerated` / `OnIndexSelected` with the asset assigned, or the index from `ChanceData.GenerateId()` | A ScriptableObject must not carry scene wiring. Listeners saved on the asset still load (`[FormerlySerializedAs]`) and fire. |
| `NeoDemoShell.Build(...)`, `NeoDemoShell.ShowInfoCardOnce(...)` (demo) | A frame authored in the scene as uGUI objects | Runtime-built frames still work as a fallback ([Samples](./Samples.md#how-demo-scenes-are-made)). |
| `ModuleDemoSceneInfo.Show(...)` (demo) | An info card authored in the scene | |
| `QuestDemoView` (demo) | `QuestDemoController` in the QuestDemo scene | No shipped scene uses it. |

Older obsolete APIs: [Deprecated components and removal candidates](./DEPRECATED_OR_REMOVAL_CANDIDATES.md).

## Source-compatibility notes

| Module | Change |
|---|---|
| StateMachine | `StateMachineBehaviourBase` and `StateMachineBehaviour<TState>` gained `ChangeState(StateData)`, so `wrapper.ChangeState(null)` is ambiguous (CS0121). Write `ChangeState((string)null)` or pick the overload. |
| Extensions | `RandomExtensions.GetRandomEnumValue<T>()` throws `InvalidOperationException` for an enum without values (was `IndexOutOfRangeException`). `GameObjectArrayExtensions.SetActiveAtIndex` with a `null` source returns `null` with a gated warning. |
| Cards | `PokerHandEvaluator.Evaluate(null)` throws `ArgumentNullException` (parameter `cards`). |
| Abilities | `AbilityEventBus.Publish` ignores an event without an id instead of throwing. |
| Save | `FileSaveProvider.TryChangeSlot` with a null, empty or whitespace name returns `false`. |
| Progression | List or array arguments to `ProgressionRewardDispatcher.DispatchRewards` bind to the allocation-free `IReadOnlyList<ProgressionReward>` overload; same results. |
| Interaction | `InteractionPhysicsQueries.Statistics.ClassifiedHits` counts only the raw hits inspected; do not compare it with 11.x values. |
| Random | `ChanceData.OnIdGenerated` is an `[Obsolete]` property over a private field. `AddListener(...)` and `?.Invoke` compile with a warning; `ref` / `out` use and field reflection do not. |
| Assemblies | `Neo.Tools.Draw` references `Neo.Tools.Input`, `Neo.Tools.Time` references `Unity.TextMeshPro`, `Neo.Haptics` references `Neo.Extensions`. No cycle; matters only when copying assemblies selectively. |

## Behaviour changes to review

The 12.0.0 changes by module. Items marked **check** most likely need an adjustment.

### Audio

- **check** `AMSettings` saves the mute state (Music, Efx, Master, UI channel) under `saveKeyMute` (default `Neo.Audio.AMSettings.Mute`). **Persist Mute** is on by default, existing components included, and needs **Persist Volume**, so `SetMusic`, `SetEfx` and the toggles become saved preferences. For temporary silence use `AudioListener.pause`, a mixer snapshot or [AudioFocusMute](./Audio/AudioFocusMute.md), or turn **Persist Mute** off ([AMSettings](./Audio/AMSettings.md)).
- Binding to `AM` no longer forces Music and Efx on: a `SetMusic` / `SetEfx` call made earlier (for example in `Awake`) is kept; otherwise the saved mute, then the sources' state, applies.
- `SetMasterVolume` while Master is muted changes only the level that unmuting restores.
- The AMSettings mute buttons work in Play Mode only, so they never write a mute into scenes or prefabs.
- `RandomMusicController` skips empty track slots instead of ending the playlist.

### UI

- **check** `ButtonScale`, `ButtonShake` and `FakeLoad` (`Neo.UI`) animate in unscaled time by default, existing scenes included, so they work at `Time.timeScale` 0. Untick **Use Unscaled Time** (`UseUnscaledTime = false`) for scaled time.
- **check** A later one-shot `FakeLoad.Load()` in the same session completes one frame after the call. A load interrupted by deactivation continues on re-activation. Code that read the result right after `Load()` must wait for `OnFinisLoad` or a frame; `IsLoading` reports a pending load.
- **check** `AnimationFly.Play(request)` copies the request: set every field, `OnAllArrived` included, before the call. A `Type` request no longer gets its resolved `EndTransform` written back; callback exceptions are logged and the flight completes.
- `ButtonShake` restores the position captured at shake start and no longer resets it in `OnEnable`, so layout-placed buttons do not jump.
- `FakeLoad.Load()` during a running load restarts it; `OnFinisLoad` fires once.
- `VariantView` applies its saved state in `Awake`, so runtime-spawned instances match **Current State Index** at once.
- `ButtonScale` returns to the scale it had when the press started, not the one from `Awake`.
- `CategoryBar.Select` / `Next` / `Prev` before `Start` set the initial selection instead of being replaced by **Start Index**.

### Abilities

- **check** Custom `IEffectOperation` code must not keep the `EffectContext`, its `PrimaryTargets`, its `Random` or a `CreateNested` child after `Execute`: system contexts are pooled. Copy what you need; a released context is ignored with a gated warning.
- **check** Modifier tick and reaction rolls after the first use derived seeds. Replays or lockstep sessions recorded with 11.x diverge after the second tick or reaction and must be re-recorded.
- `ModifierEngine.VersionOf` comes from one engine-wide counter: compare for equality only; it reads 0 after `DestroyUnit` / `ForgetOwner`.
- The spatial grid is opt-in: **Spatial Cell Size** 0 on `AbilitySystemBehaviour` keeps the linear search.

### RPG

- **check** `RpgContactDamage.OnTargetFound` fires when a target first comes into range and `OnTargetLost` once per loss; listeners that never ran will run.
- `AttackSpeedPercent` buffs feed `RpgCharacter.GetAttackSpeedMultiplier()`. `RpgAttackController` divides cooldowns by it only with **Scale Cooldown By Attack Speed** on (off by default).
- `RpgEvadeController.IsEvadingState` and `RemainingCooldownState` publish once after enable, then only on change.
- Direct 3D casts hit nearest-first, so `MaxTargets` drops the farthest. `AuraWeapon` skips a pulse started from a damage callback of a running pulse. `RpgAutoAttackController.Start` keeps a target set with `SetTarget`.
- `AddXp` ignores NaN and Infinity; `AddUpgradePoints`, `AddLevel` and level-up grants saturate instead of wrapping.

### NPC

- **check** `NpcRpgCombatBrain.OnChaseStarted` fires once per chase; `onTargetChanged` / `onSpeedChanged` no longer re-fire every decision tick.
- In Combined mode an unreachable chase retries after **Path Update Interval**, not every frame.
- **Stagger Path Updates** (new, on by default, existing prefabs included) spreads re-paths of NPCs spawned together.

### Shop, Inventory and pooling

- **check** `InventoryHand` no longer re-publishes `Selector.OnSelectionChanged` when the selected slot index is unchanged. To react to a different item in the selected slot, use `InventoryComponent.OnSelectedItemChanged`.
- `ShopListView.OnRefreshed` fires once per purchase or bundle (was three times, or N+1 for bundles). `Shop.SetItems` spawns rows for new entries when Auto Spawn and a prefab are set. `TryBuyBundle` reports `AlreadyOwned` for an owned single-purchase bundle.
- Inventory count labels skip unchanged values; text changed by other code is rewritten on the next refresh.
- `SpawnUtility` fallback pools survive objects the incoming scene spawns in `Awake` / `OnEnable`; pools whose storage died with the old scene are retired.

### Tools

- **check** `MagneticField` **Max Overlap Colliders** is the initial size of a growing buffer: every collider in range is processed. Bound the cost with **Affected Layers**, radius or update interval.
- **check** `Follow.TeleportToTarget` snaps only the followed channels: a rotation-only turret keeps its position, a position-only follower keeps its rotation.
- **check** `ErrorLogger` history is capped by **Max Entries** (default 50, was unlimited); 0 restores the old behaviour.
- **check** `CameraAspectRatioScaler` stores its reference size and field of view on the component; existing scenes capture the camera once on next initialization. This fixes the size creeping up after an Edit Mode preview.
- `DistanceChecker` fires `onDepart` once when its target or source is destroyed or cleared inside the threshold.
- `Follow`: the 2D Z rotation limit clamps around the centre of the authored sector (asymmetric limits snap to the nearer edge); `TeleportToTarget` applies position limits and resets smoothing.
- `AdvancedForceApplier` with **Clamp Max Speed** also clamps in the two physics steps after `ApplyForce`.
- `PlayerController2DPhysics.Teleport` / `PlayerController3DPhysics.Teleport` move the Rigidbody too; the animator drivers skip undeclared parameters.
- `Drawer` reads the pointer through `MouseInputCompat`, so it works in Input System-only projects.
- `Loot` drops nothing while its scene unloads or the application quits.
- `Bootstrap` finishes its startup pass when an `IInit.Init` throws and sets that target aside; `InitializePending()` retries, `IsBootstrapCompleted` reports.
- `TimerObject` **Time Text** (TMP) now compiles and must be assigned; it is never auto-found. An invalid **Time Format** falls back to the default with one gated warning.
- `GameTimeController.PauseGame()` remembers the last non-zero speed for `TogglePause()`; `SetTimeScale(0)` pauses like `PauseGame()`; `ResumeGame()` resumes at speed 1.
- `ChanceManager` Inspector weights are raw weights, no longer clamped to 0–1.

### Extensions, Core, Reactive and Haptics

- **check** `StringExtension.ToFloat` / `ToInt` try the invariant culture first, then the device culture: on locales with a dot as thousands separator `"1.500"` reads as 1.5. `ToBool` compares ordinally.
- `TransformExtensions.LookAt2D` does nothing for a null transform or a target at the same XY; `GetClosest` returns `null` for a null transform.
- `ScreenExtensions` and `UIUtils.WorldToCanvasPoint` treat a destroyed `Camera` like `null` and use `Camera.main`.
- `ResourcePoolModel.Tick` raises no `OnResourceChanged` for cycles that add nothing, so `HealthComponent.OnChanged` stops firing at full health.
- A weak `ReactiveProperty` subscription whose Unity target was destroyed is removed silently; strong ones still warn.
- Exceptions from a `Haptics.Played` observer are logged and no longer cancel the pulse.

### Bonus

- `WheelFortune` with **Single Use** off no longer locks its CanvasGroup after the first spin.
- `CooldownReward.SetRewardAvailableNow()` raises `OnRewardAvailable`, like `TimeReward`.
- The obsolete `TimeReward` keeps its running or paused state while inactive.
- `Collection.Set` is read-only (`IReadOnlyCollectionSet`): change a collection through the component so it saves and raises events. An injected random source returning an out-of-range index throws.

### Cards

- **check** Scattered layouts and jitter are deterministic per card index and no longer use `UnityEngine.Random`: existing scattered layouts look different, and layout passes no longer shift your random sequence.
- `DeckComponent.Reset()` / `Initialize()` clear the visual stack board; `ReturnCard` removes the card from whichever hand or board holds it.
- Hand sorting is stable; `BoardComponent.GetAllRanks()` / `GetAllCardData()` skip empty or destroyed entries; a card re-parented during `PlaceCardAsync` is dropped from the board.

### GridSystem

- `TicTacToeBoardService` raises `FieldGenerator.OnCellStateChanged` for moves and resets, after the round state is committed.
- `SlidingMergeResolver.Slide`, `SlidingMergeBoardService.ClearBoard`, `DiceBoard.ClearBoard` and `GridSlotAllocator.Clear` notify after all cells are written.
- On gapless rectangular grids `GetNearestCellFromWorld` is constant-time and may pick the other cell on an exact shared edge. Custom path predicates run at most once per cell per query.

### Quest and Progression

- **check** `QuestManager.Notify` / `NotifyKill` / `NotifyCollect` / `NotifyReachPoint` / `NotifyTalk` with a null or empty target id credit nothing; use `AddObjectiveProgress` or `CompleteObjective` by index.
- If an objective listener throws, the quest still completes when that credit finished it, completion events fire, and the exception is rethrown afterwards. Auto Save writes once per top-level operation.
- Progression unlocks and purchases with several rewards publish one settled state before `OnNodeUnlocked` / `OnPerkPurchased`.

### Save, Settings and Level

- **check** `GlobalSave.SaveProgress()` does nothing while `GlobalSave.data` was never loaded or assigned (it used to overwrite the profile with an empty one). Assigning `null` stores a fresh `GlobalData`.
- `FileSaveProvider.Save()` with nothing new skips the write but raises `OnDataSaved`; setting an unchanged value raises `OnKeyChanged` without marking the file dirty. Integers are written in the invariant culture; old device-culture values still read.
- `SaveProviderSettingsComponent` always logs a rejected configuration.
- `SaveManager` flushes on both focus loss and pause, even in one frame.
- A Deferred settings save whose coroutine was stopped externally restarts on the next change; `GameSettingsComponent.HasPendingDeferredSave` reports it.
- `LevelComponent` notifies `XpState` and `XpToNextLevelState` before `LevelState` and `OnLevelUp`, and each `AddXp` / `SetLevel` notifies every state exactly once, even when an `OnLevelUp` listener adds XP; a listener that counted notifications sees fewer.

### Condition, NoCode and StateMachine

- **check** `NeoCondition` **EveryFrame** mode throttles on unscaled time and keeps checking and firing at `Time.timeScale` 0, also sending Mirror ServerRevalidate checks. **Interval** mode follows game time unless the new **Use Unscaled Time** is on.
- `NoCodeBindText` skips unchanged values on poll, so SetText animations do not restart; `ForceRewrite()` pushes again.
- A machine configured on `StateMachineBehaviourBase` / `StateMachineBehaviour<TState>` before `Awake` survives activation. Loading `StateMachineData` enters the assigned Initial State asset directly. If a target state's constructor changes or stops the machine, that choice wins.
- `StateData` action lists run only the entries present when a pass starts; actions added during a pass run on the next one.

### Network

- Use the same NeoxiderTools version on every peer.
- `NetworkContextActionRelay` sends AllClients and OthersOnly actions only to the relay identity's observers (the ClientRpc audience); a host sends to remote observers before applying locally. Listener exceptions on received messages are logged instead of disconnecting the sender.
- `NetworkActionRelay` OthersOnly sends to the relay's observers, not every connection.
- `NeoNetworkDiscovery` advertises once per server or host start with any manager, stops with the server and no longer re-broadcasts searches during a session; `StartAdvertising` is a no-op while advertising.
- `NetworkEventDispatcher` and `NetworkPlayerName` reject remote commands while the server-side **Is Networked** is off. `NeoNetworkBootstrap` can start again after a session ended without `StopNetwork()`.

### Editor

- A `[Button]` action runs on every selected object (static methods once), with Undo in Edit Mode.
- Auto-assigned `[GetComponent]` / `[FindInScene]` / `[LoadFromResources]` references are recorded with Undo; their warnings log once per object and field.
- `NeoxiderSettings.SaveSettings()` no longer refreshes the AssetDatabase; the hierarchy template is saved in `ProjectSettings/NeoxiderSettings.json`.
- The update checker logs request details only with the EditorPrefs switch `Neo.UpdateCheck.Verbose`.
- Scene Saver logs auto-saves only with **Log Each Auto-Save**; failures always log.
- Clicking the mascot chip of a Neo inspector header opens the component's documentation; the rest of the header only animates the mascot.

## Changes in 12.1–12.4

### 12.1.0

Additive (Abilities damage pre-hooks, triggers, payload ops, zones, stack policies, replaceable rules, a runtime trace, the integer core `Neo.Abilities.Fixed`); float content behaves as before. Details:

- Damage, execute and chain ops pass `DamageFlags.Hit` for cast and projectile hits; healing, lifesteal, shield absorption and resource changes go through `AbilityRules.Numeric`, identity by default.
- `DamageService` keeps no static state; NaN damage is rejected as negated.
- Reactions of one unit to one event are ordered by `Priority` when any priority is set.

### 12.2.0

A distribution release; runtime APIs are unchanged.

1. Change the git URL to `https://github.com/NeoXider/NeoxiderTools.git#upm`: the branch holds only the package, a few megabytes.
2. The package declares no UPM samples; the demos moved from `Samples~` to `Assets/NeoxiderDemos/Demo`, `NeoxiderPages` and `CharacterMovementFundamentals`, one archive each per release ([release archives](./Samples.md#release-archives)).
3. Delete a copy imported earlier into `Assets/Samples/NeoxiderTools/<version>/` before importing a demo: both carry the same GUIDs.

Art was recompressed to its import size (oversized PNGs downscaled with pixels-per-unit and sprite rectangles rescaled, WAV to Ogg Vorbis, a few large images to palette PNG or JPEG); GUIDs and references are unchanged. **Repair Missing Scripts In Prefabs** scans `Assets/NeoxiderDemos`.

### 12.3.0

- Demo scenes are plain saved scenes: edit them like any scene and re-import the download to restore. The `Neo.Demo.Editor` assembly (60 scene builders, DemoSceneKit, the **Neoxider > Demo** menu, the RPG demo fixer), its tests and the `DemoGeneratedRoot` marker were removed. The package is not affected.
- SurvivorDemo became a NoCode game; its scripts were removed ([list](#removed-and-renamed-demo-scripts)).
- Ability parameters scale by level, stats or spent resource. The new fields default to off: content resolves as before on both cores and fixed state hashes are unchanged.
- **check** UI Mesh Rig: a rebound direct child point of a uGUI rig rests at `anchoredPosition` (0, 0), so Animator keys are offsets from the bind at any size. Points bound earlier keep their offset until rebound.

### 12.4.0

- **check** `Spawner` **Spawn Limit** counts spawned objects only: a table still locked by **Prefab Unlock Seconds** no longer uses up the limit, so a limit of 1 with an unlock time (a boss) now spawns.
- SurvivorDemo has seven weapons; its `nova` and `ember` abilities and `chill` / `burn` modifiers were removed.
- Additive ability-constructor features: orbit motion, target picks, `LevelUpAbility`, archetype roles, the spawn API, `AbilityEventListener`, `AbilitySummonBehaviour`, `AbilitySceneValidator`, `AbilityCuePlayer`, `PlayAudio` **Play On Enable**.

## New APIs worth adopting

Additive 12.0 APIs; nothing above requires them.

| Need | API | Page |
|---|---|---|
| Deterministic time in tests and replays | `IFrameClock`, `ManualFrameClock`, `INeoClock`, `NeoTrustedTime.AsClock` | [Clocks](./Tools/Time/Clocks.md) |
| Exact odds such as 1 in 1,000,000 | `IntegerChanceTable` | [IntegerChanceTable](./Tools/Random/IntegerChanceTable.md) |
| Bark lines without repeats, contextual interrupts | `DialogueLinePool`, `DialogueLinePlayer` | [DialogueLinePool](./Tools/Dialogue/DialogueLinePool.md) |
| 64-bit values (money in cents, large counters) | `SaveProvider.GetLong` / `SetLong`, `ILongSaveProvider` | [SaveProvider](./Save/SaveProvider.md) |
| Bonus mechanics without a scene | `CollectionSet`, `BoxProgress`, `CooldownSchedule`, `SlotSpinMath` | [Bonus](./Bonus/README.md) |
| Typed purchase result | `Shop.TryBuy` / `TryBuyBundle` → `ShopPurchaseStatus` | [Shop](./Shop/Shop.md) |
| Typed cast result | `AbilityCasterBehaviour.Cast(CastRequest)` → `CastResult`, `LastCastResult` | [Abilities](./Abilities/README.md) |
| Hundreds of ability units | `AbilitySystemBehaviour.SpatialCellSize`, `AbilitySpatialGrid` | [Abilities](./Abilities/README.md) |
| Shields, armour and damage caps | `IRpgDamageModifier`, `RpgCharacter.ApplyDamage(RpgDamageInfo)`, `DamageApplied` | [RpgCharacter](./Rpg/RpgCharacter.md) |
| Movement ranges and flood fill | `GridPathfinder.TryFindPath`, `CollectReachableCells` | [GridPathfinder](./GridSystem/GridPathfinder.md) |
| Seeded deals, custom card sorting | `DeckModel(seed)`, `HandComponent.SortAsync(IComparer<CardData>)`, `CardComparer` | [Cards](./Cards/README.md) |
| Quest credit from prefabs by id | `QuestManager.Notify*`, `QuestNoCodeAction` Notify actions | [Quest](./Quest/README.md) |
| Testable authority rules without Mirror | `NetworkAuthorityPolicy.IsKnown` / `IsAuthorized` (`Neo.Network.Contracts`) | [Network contracts](./Network/Contracts.md) |
| UnityEvent targets for enum or two-argument setters | `MagneticField.SetModeIndex`, `ImpulseZone.SetDirectionIndex`, `CategoryBar.DisableEntry` / `EnableEntry`, `AnchorMove.SetX` / `SetY` / `Apply`, `Leaderboard.SubmitScore`, `Box.ResetProgress` | [UI](./UI/README.md), [Physics](./Tools/Physics/README.md) |

## Package assets moved out of the samples

In 11.x, package prefabs referenced sprites, materials and clips that existed only in `Samples~`, so a UPM install without samples showed missing references. Since 12.0.0 the package has its own copies, with **new GUIDs**; the package prefabs already use them.

| Was (sample source, under `Assets/Neoxider/Samples~/`) | Now (package, under `Assets/Neoxider/`) | Used by |
|---|---|---|
| `Demo/Sprites/Frame 3.png` … `Frame 6.png`, `Group 2.png`, `Layer 4.png` (kept in the sample too) | `Art/Bonus/Slot/` (same file names) | `Prefabs/Bonus/Slot/Row`, `SlotElement`, `SlotUI`, `Resources/Slot/SpritesDafualt.asset` |
| `Demo/Sprites/Group 2-1.png`, `Demo/Sprites/Layer 11.png` (removed from the sample) | `Art/Bonus/Slot/Group 2-1.png`, `Art/Bonus/Slot/Layer 11.png` | `Prefabs/Bonus/Slot/SlotUI` |
| `Demo/Sprites/Clicker/Group 132.png`, `Group 133.png` (removed from the sample) | `Art/Shop/Group 132.png`, `Art/Shop/Group 133.png` | `Prefabs/Shop/ItemShop` |
| `Demo/Sprites/Clicker/Resource 1@4.png` (kept in the sample too) | `Art/Shop/Resource 1@4.png` | `Prefabs/Shop/ItemShop` |
| `Demo/Materials/yellow.mat`, `red.mat` (kept in the sample) | `Art/Materials/Neo Yellow.mat`, `Neo Red.mat` | `Prefabs/Bullet`, `Prefabs/Simple Weapon`, `Prefabs/Tools/AI/OLD AI NPC` |
| `CharacterMovementFundamentals/Capguy/Animations/` (removed from the sample): `Fall`, `Idle`, `Jump`, `Land`, `WalkBackward`, `WalkForward`, `WalkLeft`, `WalkNeutral`, `WalkRight` `.fbx` | `ThirdParty/CharacterMovementFundamentals/Art/Capguy/Animations/` | `ThirdParty/.../Art/Capguy/CapguyAnimator.controller` |
| `CharacterMovementFundamentals/Capguy/Model/Textures/Tex_Cap.png`, `Tex_Capguy.png` (removed from the sample) | `ThirdParty/CharacterMovementFundamentals/Art/Capguy/Textures/` | `Mat_Cap.mat`, `Mat_Capguy.mat` |
| `CharacterMovementFundamentals/Environment/Textures/Tex_Grid_Red.png` (removed from the sample) | `ThirdParty/CharacterMovementFundamentals/Art/Environment/Textures/Tex_Grid_Red.png` | `Mat_Grid_Red.mat` |

- Using only the package prefabs: nothing to do.
- An imported 11.x copy (`Assets/Samples/NeoxiderTools/11.x/...`) keeps its own files and GUIDs while you keep it. If you replace it with the current Demo download, references to files marked "removed from the sample" break: point them to the middle column.
- `Neo Yellow.mat` / `Neo Red.mat` are renamed copies; the sample's `yellow.mat` / `red.mat` are unchanged.

## Samples and demo scenes

Moving a project with imported samples (11.x to 12.1):

1. Save changes you made to the imported copies; package upgrades never update them.
2. Delete `Assets/Samples/NeoxiderTools/<version>/`: it has the same GUIDs as the demo downloads.
3. Import the demos you use into `Assets/NeoxiderDemos/<name>` ([Demo scenes](./Samples.md)).
4. Install TMP Essential Resources and `com.unity.2d.sprite` ([prerequisites](./Samples.md#host-and-sample-prerequisites)).

What changed:

- Since 12.0.0 every module demo, and the NeoxiderPages UI scene, is saved with its UI, components and persistent UnityEvent wiring. Since 12.3.0 there are no scene builders ([how demo scenes are made](./Samples.md#how-demo-scenes-are-made)).
- New in 12.0.0: `Scenes/Animations/AnimatorsDemo.unity`, `Scenes/UI/UIComponentsDemo.unity`, `Scenes/Bonuses/BonusRewardsDemo.unity`, `Scenes/Tools/Physics/Physics3DDemo.unity`, and info cards on `_ExampleGame/adventure`, `ClickerExample`, `Shooter2D` and the Pages `UI.unity` ([Samples](./Samples.md#module-demo-scenes)).
- SurvivorDemo runs with Active Input Handling set to Input System Package (New) or Both; the Character Movement Fundamentals scenes need **Both**.
- Demos use demo-only save keys, for example `Neo.Demo.Settings.`, `Neo.Demo.Audio.*`, `NeoDemo.Quests` and `neo_save_demo.json`, so they never overwrite your `Neo.Settings.` keys or default PlayerPrefs data. Keys from older demo runs stay.
- **check** The example games save under `NeoDemo.Adventure.Inventory`; `NeoDemo.Clicker.Money` / `.Shop` / `.BestScore` / `.Level`; `NeoDemo.Shooter2D.Money` / `.BestScore` / `.Level`; `NeoDemo.Pages.Money` / `.BestScore` / `.Level`. ClickerExample progress under the old `Money` / `Shop` keys is not carried over; Progression_Demo no longer touches `Inventory_Default`.
- **check** NeoxiderPages: `PM.SetPage` raises `OnPageChanged` once instead of twice and logs only with **Debug Logs** on. The Pages `FakeLoad` uses unscaled time by default (untick **Use Unscaled Time** for scaled time) and completes a one-shot load one frame after `Load()`. A `BtnChangePage` with a legacy UIKit.Page action opens its Target Page and warns once; pick **Use Open Page** to silence it.

### Removed and renamed demo scripts

None was used by the package. If your scene used one, copy it from the older demo.

| Script or content | Change | Since |
|---|---|---|
| `GridSystemMatch3DemoSetup`, `GridSystemTicTacToeDemoSetup`, `DiceMergeDemoSetup` | Deleted; scenes are authored | 12.0.0 |
| `ShopDemoBootstrap` | Now `ShopDemoController` (same script GUID) | 12.0.0 |
| `RpgDemoUI`, `ConditionDemoSetup`, `HealthTextDisplay`, `Network/TestStart`, `SurvivorHud` | Deleted; unused | 12.0.0 |
| `RpgCharacterQuickDemo._configureOnAwake` | Field removed | 12.0.0 |
| `AnimatorsDemoController` | Drives the AnimatorsDemo scene; hidden from Add Component | 12.0.0 |
| `Neo.Demo.Editor`, `Neo.Demo.Editor.Tests`, `DemoGeneratedRoot` | Deleted | 12.3.0 |
| `SurvivorGame`, `SurvivorEnemy`, `SurvivorPlayerController`, `SurvivorXpOrb`, `SurvivorHudView`, `SurvivorConfig`, `SurvivorUpgrade`, `SurvivorUpgradeKind`, `SurvivorEnemyType` and their config assets | Deleted | 12.3.0 |
| SurvivorDemo `nova`, `ember`, `chill`, `burn` | Removed | 12.4.0 |

## Tests

- Zero-allocation tests use Unity's GC.Alloc recorder (`Is.Not.AllocatingGCMemory()` or a calibrated counter). `System.GC.GetAllocatedBytesForCurrentThread()` reads 0 in the Unity 6 Editor, so checks built on it never fail; rewrite yours.
- Benchmarks are explicit `Performance` tests that print `NEO_PERF` lines.
- The editor validation host needs an explicit `-neoValidationExpectedPackageVersion` ([Testing](./Testing.md)).

## По-русски

1. Конвертер HTML/CSS → UI Toolkit — отдельный пакет `com.neoxider.htmltouitk`: удалите старую копию `Scripts/UI/HtmlToUitk` и установите его по Git URL; имена сборок, namespaces и GUID сохранены.
2. Обновите пакет по адресу `https://github.com/NeoXider/NeoxiderTools.git#upm`. Других удалений публичного API нет; устаревшие члены (`VariantView.currentStateIndex`, `Map.GetCopmplete()`, `ChanceData.OnIdGenerated`, `NeoDemoShell`) дают только предупреждения.
3. Проверьте пункты **check**: AMSettings сохраняет mute, `ButtonScale` / `ButtonShake` / `FakeLoad` работают в unscaled time, `AnimationFly.Play` копирует запрос, `StringExtension.ToFloat` сначала читает invariant culture, `Spawner` **Spawn Limit** считает только созданные объекты.
4. Удалите сэмплы из `Assets/Samples/NeoxiderTools/<версия>/` и импортируйте демо-архивы в `Assets/NeoxiderDemos/<имя>`; сцены сохранены целиком, исходную возвращает повторный импорт.
5. Префабы пакета используют собственные копии арта с новыми GUID ([таблица](#package-assets-moved-out-of-the-samples)).
