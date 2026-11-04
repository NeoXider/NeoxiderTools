
## [Unreleased]

## [12.0.0] - 2026-11-04

A library-wide quality pass: bug fixes and behaviour corrections in every module, allocation-free hot paths, typed
C# APIs next to the Inspector and NoCode workflows, plain C# cores with injectable time and random sources, demo
scenes authored in the scene by editor builders, and a reworked Neo inspector. The HTML/CSS → UI Toolkit converter
(HtmlToUitk) moved to its own package, [HtmlToUitk](https://github.com/NeoXider/HtmlToUitk)
(`com.neoxider.htmltouitk`). Read [Upgrading to 12](Docs/Migration12.md) before updating: it lists the removed
assemblies, the obsolete APIs with their replacements, the source-compatibility notes and every behaviour change,
with the ones most likely to need an adjustment marked **check** (also marked below).

### Breaking and removed

- **HtmlToUitk moved to its own package.** `Neo.HtmlToUitk.Runtime`, `Neo.HtmlToUitk.Editor`,
  `Neo.HtmlToUitk.Tests.Editor`, the Velora showcase (`Assets/HtmlToUitkShowcase`), the .NET CLI
  (`Tools/HtmlToUitk`) and the `Docs/UI/HtmlToUitk*.md` pages left `com.neoxider.tools`. Install it from
  `https://github.com/NeoXider/HtmlToUitk.git`: assembly names, namespaces, script GUIDs, menu paths and EditorPrefs
  keys are unchanged, so scenes and prefabs resolve again once it is installed. Converter warnings are switched on
  with `HtmlToUitkDiagnostics.Configure(warnings: true)` instead of `NeoDiagnostics`. Delete any bundled copy of
  `Scripts/UI/HtmlToUitk` first; duplicate assembly names do not compile. See [HtmlToUitk moved to its own
  package](Docs/Migration12.md#htmltouitk-moved-to-its-own-package).
- **Source compatibility.** A literal `ChangeState(null)` on `StateMachineBehaviourBase` /
  `StateMachineBehaviour<TState>` is now ambiguous (CS0121) because of the new `ChangeState(StateData)`; cast it to
  `(string)null`. `RandomExtensions.GetRandomEnumValue<T>()` throws `InvalidOperationException` for an enum without
  values; `PokerHandEvaluator.Evaluate(null)` throws `ArgumentNullException`; `AbilityEventBus.Publish` ignores an
  event without an id; `FileSaveProvider.TryChangeSlot` with an empty name returns `false`; `ChanceData.OnIdGenerated`
  is a property now (it was a public field), so code that passes it by `ref` or finds it through field reflection
  must change; list and array callers of `ProgressionRewardDispatcher.DispatchRewards` bind to the new
  `IReadOnlyList` overload (same results). The
  diagnostic `InteractionPhysicsQueries.Statistics.ClassifiedHits` now counts the raw hits actually inspected, so it
  reports smaller numbers than 11.x.
- **Changed defaults that existing scenes pick up:** `AMSettings` **Persist Mute** is on; `ButtonScale`,
  `ButtonShake`, `FakeLoad` and the `UI` page manager animate in unscaled time; `NpcNavigation` **Stagger Path
  Updates** is on; `ErrorLogger` **Max Entries** is 50 (was unlimited); in Input System-only projects
  `MouseInputCompat` reads the primary touch when no mouse is connected. See [behaviour
  changes](Docs/Migration12.md#behaviour-changes-to-review).
- **Assembly references:** `Neo.Tools.Draw` now references `Neo.Tools.Input`, `Neo.Tools.Time` references
  `Unity.TextMeshPro`, and `Neo.Haptics` references `Neo.Extensions` (no cycles; this matters only when copying
  assemblies selectively).
- **Editor drawing constants:** `NeoxiderEditorGUI.SectionHeaderHeight` 32 → 24, `PillToggleWidth` 54 → 32 and
  `CustomEditorSettings.SignatureSpacing` 6 → 4; `SectionAccent(bool)` returns one accent for both states and
  `SectionTitleColor(bool)` the theme title colour. Custom inspectors drawn with these helpers get the new compact
  look.
- **Samples:** the runtime bootstraps `GridSystemMatch3DemoSetup`, `GridSystemTicTacToeDemoSetup` and
  `DiceMergeDemoSetup` were removed, `ShopDemoBootstrap` became `ShopDemoController` (same script GUID), and
  `RpgCharacterQuickDemo` dropped its `_configureOnAwake` field. The unused sample scripts `RpgDemoUI`,
  `ConditionDemoSetup`, `HealthTextDisplay`, `Network/TestStart` and the obsolete `SurvivorHud` were deleted (no
  scene or prefab used them), and `AnimatorsDemoController` became the driver of the authored AnimatorsDemo scene
  (hidden from Add Component). Sample assets used only by package prefabs moved into the package (see
  **Packaging** under Changed). A package upgrade does not update imported sample copies; re-import the samples
  you use.
- **Docs:** removed `Docs/Tools/View/MeshEmissionAnimator.md`, which described a component that does not exist.

### Deprecated

Warnings only; everything below still compiles and works. Replacements are listed in [Obsolete
APIs](Docs/Migration12.md#obsolete-apis-and-replacements).

- `VariantView.currentStateIndex` → `CurrentStateIndex`; `Map.GetCopmplete()` → `Map.IsComplete`;
  `Neo.Editor.NeoEditorAutoRegister` (empty; delete calls to it); `CustomEditorSettings.EnableRainbowOutline` /
  `SetEnableRainbowOutline` (no visible effect since v10); `ChanceData.OnIdGenerated`, because a ScriptableObject
  must not carry scene wiring: subscribe to `ChanceSystemBehaviour.OnIdGenerated` or use the index `GenerateId()`
  returns (listeners already saved on the asset still load and fire).
- Demo sample: `NeoDemoShell.Build` / `ShowInfoCardOnce`, `ModuleDemoSceneInfo.Show` and `QuestDemoView` (used by
  no scene). Author the frame, info card and views in the scene with an editor builder instead.

### Added

- **Core**: `ResourcePoolModel.TrySpendAll` / `CanSpendAll` with the new `ResourceCost` value type pay a
  multi-resource cost completely or not at all: costs on one pool must fit the balance the earlier ones leave, each
  cost obeys the decrease limit, listeners run after the last payment, and `out int failedIndex` overloads name the
  missing resource. `HealthComponent` exposes `CanSpend`, `CanSpendAll` and `TrySpendAll` (mana plus HP, or two
  currencies, without partial payments). `BeginNotificationBatch` / `EndNotificationBatch` /
  `IsBatchingNotifications` apply mutations at once and deliver Changed/Depleted notifications in order at the
  outermost end; a throwing listener does not drop the rest, and the first exception is rethrown afterwards.
  Resource entries have Inspector tooltips for every field.
- **Extensions, Network.Contracts**: seeded `RandomExtensions` overloads (`Shuffle` with a `System.Random` or a
  `Func<int,int>`, `GetRandomElement` / `GetRandomIndex` / `GetRandomWeightedIndex(System.Random)`) for
  deterministic tests, replays and daily seeds; `UIUtils.IsPointerOverUI(Vector2)`;
  `UnityEventDelegateCache.UnsubscribeAll()`; `NeoDiagnostics.MaxThrottleKeys` (1024);
  `NetworkAuthorityPolicy.IsKnown` / `IsAuthorized`, a transport-free authority decision table that can be
  unit-tested without Mirror.
- **Tools/Input**: `MoveInputCompat.ReadMove()` / `ReadMove(horizontalAxis, verticalAxis)` reads two-axis movement
  on both backends (legacy raw axes, or WASD, arrows and the gamepad left stick; not normalized);
  `MouseInputCompat.TryGetScrollDelta` / `TryGetPointerDelta`;
  `OptionalInputSystemAdapter.SimulateMouseWithTouches`, `TryGetMouseDelta`, `TryGetMouseScroll`;
  `InputBackendCompat.IsLegacyInputConfigured`; `MouseEffect.RequiresHoldEvents`, with one gated warning when the
  effect subscribes to a manager whose Hold events are off.
- **Tools/Time, Random, Dialogue**: clock seams `IFrameClock` / `UnityFrameClock` / `ManualFrameClock`
  (`Timer.FrameClock`, `TimerObject.FrameClock`) and `INeoClock` views (`TimerObject.UtcClock`,
  `TrustedClock.AsNeoClock()`, `NeoTrustedTime.AsClock`); `TimerTextFormatter`; opt-in `CarryLoopOvershoot` on
  `Timer` and `TimerObject` for exact looping periods; `GameTimeController.TogglePause()`, `IsPaused` and
  `ResumeScale`. `IntegerChanceTable`: exact integer weights (odds such as 1 in 1,000,000), O(log n) rolls and an
  injectable unbiased random source. `DialogueLinePool` (plain C#) and `DialogueLinePlayer`: pooled bark lines by
  tag without back-to-back repeats, with time windows, first-pick slots, contextual interrupts and
  `Tick(deltaTime)`. `DialogueController.RevealMode` (`MaxVisibleCharacters` reveals through TMP with no string per
  character and no word-wrap jumps) and `DialogueUI.SetDialogueTextRevealed` / `SetVisibleCharacters` /
  `ShowAllCharacters`.
- **Tools/Components, Text, Debug, Managers**: `TypewriterEffect.PlayRevealAsync` and
  `TypewriterEffectComponent.RevealMode`; `Bootstrap.InitializePending()`, `HasFailed(IInit)` and
  `IsBootstrapCompleted`; `SingletonById<T>.TryGet`; `CameraAspectRatioScaler.ReferenceOrthographicSize`,
  `ReferenceFieldOfView`, `Mode`, `TargetResolution`, `Refresh()` and `CaptureReferenceFromCamera()`;
  `Loot.ScatterPlane` (XY for 2D); `Prefix` / `Suffix` on `SetText` and `TimeToText`; `ScoreManager.SaveKey`;
  `AnimatorParameterDriver.Animator`; `ErrorLogger.MaxEntries`, `Entries`, `Clear()` and an optional character
  budget `MaxCharacters` (default 0 = off) with `HistoryCharacterCount`.
- **Tools/Move, Physics, Draw**: non-allocating `Drawer.Smooth(points, passes, fixedZ, results)` and
  `Drawer.LimitPoints(points, max, results)` (in place allowed); `AffectedLayers` on `ExplosiveForce`,
  `MagneticField` and `ImpulseZone`, plus `ImpulseZone.SetDirection`, `SetCustomDirection`, `SetCooldown` and
  `ImpulseForce`; `CameraConstraint.HasValidBounds`.
- **Tools/Spawner, Inventory**: `SimpleSpawner.SpawnInstance()` (typed return next to the UnityEvent `Spawn()`);
  `NeoObjectPool.Prefab`; `InventoryComponent.OnSelectedItemChanged`, `AutoSave`, and `DeferAutoSave()` /
  `IsAutoSaveDeferred`: a burst of mutations inside `using (inventory.DeferAutoSave())` writes the JSON once while
  events still fire per mutation.
- **Tools/View, Animations, Audio**: `Tick(deltaTime)` and `ManualUpdate` on `FloatAnimator`, `ColorAnimator`,
  `Vector3Animator`, `TransformAnimator` and `LightAnimator`, plus `RandomizeStartTime` and `UseUnscaledTime`;
  `AnimationUtils.ApplyToRenderer` (emission through a `MaterialPropertyBlock`, no material copies); `StarView.Bind`
  / `ShowStars`; `BillboardUniversal` getters; `ZPositionAdjuster.Adjust`, `ComputeDepth` and `Ratio`;
  `SelectorModel.IsItemActive(int)`; `AMSettings.PersistVolume` / `PersistMute`.
- **UI**: `AnimationFlyRequest.Clone()`, optional count/start/end on `FromPrefab` / `FromSprite`,
  `AnimationFly.OwnedVisualCount`; `CategoryBar.SelectionChanged` and `IsInitialized`; `VariantView.StateChanged`,
  `OnStateChanged` and `CurrentStateIndex`; **Use Unscaled Time** (`UseUnscaledTime`) on `ButtonScale`,
  `ButtonShake` (plus `IsShaking`), `FakeLoad` and the `UI` page manager;
  `UIMeshRigGeometryBuilder.Build(..., reuse)`.
- **Abilities**: an opt-in spatial grid for unit radius queries (`AbilitySystemBehaviour` **Spatial Cell Size**,
  `InvalidateSpatialCache`, reusable `AbilitySpatialGrid` with `Move`, `Remove` and the shared inclusive
  `IsWithinRadius` / `SqrDistance` tests) with results identical to the linear scan;
  `AbilityCasterBehaviour.Cast(CastRequest)` returns a typed `CastResult`, and `LastCastResult` keeps the latest;
  `EffectContext.IsReleased`; `ModifierEngine.ForgetOwner` / `TrackedOwnerCount`; `XorShiftRandom.Reseed`;
  `AbilityProjectileBehaviour.IsFlying` / `CastId`.
- **RPG**: an extensible damage pipeline (`IRpgDamageModifier`, `RpgCharacter.AddDamageModifier` /
  `RemoveDamageModifier`, `ApplyDamage(RpgDamageInfo)`, the `DamageApplied` event) for shields, armour, caps and
  thorns; plain C# `RpgCharacterStatService`, `RpgCharacterProgressionService` and `RpgModifierMath` behind
  `RpgCharacter`; `GetAttackSpeedMultiplier()` with the opt-in `RpgAttackController.ScaleCooldownByAttackSpeed`;
  code setters for `RpgTargetQuery`, `RpgTargetSelector.Query`, `RpgAutoAttackController` and `RpgContactDamage`;
  `RpgStatsDamageableBridge.Initialize(IRpgCombatReceiver)`, `ClearReceiver`, `TryTakeDamage` and `TryHeal` for
  pooled hitboxes; `MeleeWeapon.DamageType` (also on `AuraWeapon`) so resist buffs apply to melee and aura hits;
  `RpgCharacterNetworkCommand.IsKnownType`.
- **NPC**: `NpcNavigation.IsRunning`, `AutoUpdatePath`, `RefreshPath()` and **Stagger Path Updates**
  (`NpcFollowTargetCore.PathUpdatePhase`); `NpcAnimationCore.HasSpeedParameter` / `HasIsMovingParameter`;
  `NpcRpgCombatBrain.IsActive`, `Preset`, `CurrentDecision` and C# accessors for its events.
- **Shop**: `Shop.TryBuy` / `TryBuyBundle` return a `ShopPurchaseStatus` (`Purchased`, `AlreadyOwned`, `Selected`,
  `InsufficientFunds`, `CommitFailed`, `PendingServerAuthority`, `Unavailable`, `Busy`);
  `GetItemsInCategory(category, results)`; `TextMoney.BoundMoney`.
- **Bonus**: plain C# cores `CollectionSet` (`Collection.Set` as `IReadOnlyCollectionSet`,
  `Collection.RandomIndexSource`), `BoxProgress` / `BoxProgressRules` with idempotent claim receipts,
  `CooldownSchedule` on an injected `INeoClock` (shared by `CooldownReward` and `TimeReward`) and `SlotSpinMath`;
  `Box.TargetCollection`, `WheelMoneyWin.Wallet`, `LineRoulett.UseUnscaledTime`, `Row.GetVisibleElement`; a
  `CollectionVisualManager` **Collection** field (`TargetCollection`) and `SetItems(ItemCollection[])`, so each
  panel can show its own collection.
- **Cards**: seeded shuffling (`DeckModel(seed)`, `DeckModel(System.Random)`, `ShuffleRandom`, `Shuffle(random)`,
  `DeckComponent` **Use Fixed Shuffle Seed** and `CreateShuffleRandom()`); stable custom sorting (`HandModel.Sort`,
  `HandComponent` / `HandPresenter.SortAsync` with an `IComparer<CardData>` or `Comparison<CardData>`,
  `CardComparer.ByValueAscending` / `ByValueDescending` / `ByGroupThenValueAscending`); `HandComponent.Contains` and
  `DrawRandom(System.Random)`; list-filling `CardLayoutCalculator` overloads; `PokerHandEvaluator.EvaluateStrength`
  / `GetCombination`; `BoardComponent.RemoveCards(cards)` next to batch `PlaceCardsImmediate(cards)`, both with one
  layout pass.
- **GridSystem**: `GridPathfinder.TryFindPath` and `CollectReachableCells` (movement ranges, flood fill);
  `FieldGenerator.CollectNeighbors`; `FieldGeneratorConfig.MovementPreset` (8-way, diagonal, hex and 3D rules from
  the Inspector); `GridShapeMask.Create`, `SetCells`, `Contains` and a `Mode` setter; TicTacToe `WinLineLength`
  (Gomoku-style boards), `WinningLine` and `TicTacToeWinChecker.TryGetWinningLine`; SlidingMerge `HasAnyMove`,
  seedable `TrySpawnRandomContent`, `SlidingMergeStep.MergedFrom` / `MergedValue`, `OnNoMovesLeft`,
  `OnContentSpawned`, `Slid`, `ContentSpawned` and custom merge rules (a complete 2048 loop); Dice
  `CanPlaceAnywhere` / `TryFindPlacement` over all rotations; `FieldObjectSpawner.Unregister` /
  `PruneDestroyedObjects`; `Match3BoardService.RandomIndexProvider` (reproducible boards) and `IsResolving`;
  `Match3MatchFinder.HasMatch` and `CollectMatchedCells(field, minMatchLength, set)`.
- **Save, Settings, Level**: 64-bit values (`SaveProvider.GetLong` / `SetLong`, `ISaveProvider` extensions,
  `ILongSaveProvider` implemented by `FileSaveProvider`); `FileSaveProvider.FilePath` / `HasUnsavedChanges`;
  `GameSettingsComponent.HasPendingDeferredSave`; `SaveProviderSettingsComponent.ExecutionOrder`; `Map.IsComplete`;
  `LevelManager.TryResetProgress(mapId)`, `ResetProgress(mapId)`, `ResetAllProgress()` and `Map.ResetProgress()` for
  a "New game" button (they also work before the manager initialized; the Inspector buttons are Play Mode-only).
- **Quest, Progression**: `QuestManager.Notify(type, targetId, amount)`, `NotifyReachPoint`, `NotifyTalk`,
  `NotifyKill` / `NotifyCollect(id, amount)`, `AddObjectiveProgress`, `GetActiveQuests(List<QuestState>)`,
  `AutoSave`, `TryAcceptQuest(quest, out reason)` and the C# events `ObjectiveCompleted`, `QuestFailed`,
  `QuestReset`, `QuestsLoaded`; `QuestNoCodeAction` NotifyKill / NotifyCollect / NotifyReachPoint / NotifyTalk /
  EvaluateObjectiveConditions actions with **Target Id** and **Amount**; `QuestConfig.ValidateDefinition()`,
  `QuestObjectiveData.IsCounter` / `TargetProgress`, `QuestFlowConfig.IsChainSatisfied`. Progression gains the C#
  events `NodeUnlocked`, `PerkPurchased` and `PremiumActivated`, **On Premium Activated**, the NoCode
  `ActivatePremium` action and `HasPremium` condition mode, `LevelCurveDefinition.TryGetNextDefinition` /
  `IsAscending`, and an allocation-free
  `ProgressionRewardDispatcher.DispatchRewards(IReadOnlyList<ProgressionReward>, ...)` overload.
- **Condition, NoCode, StateMachine**: `ConditionEntry.TryEvaluate(context, out result)` (an unavailable source is
  told apart from a failed comparison) and public properties for every Inspector setting;
  `NeoCondition.UseUnscaledTime`; `BindingSourceGameObjectResolver.IsDestroyedReference` and
  `FindComponentOccurrence`; `ReflectionCache.TryGetProperty`; NoCode `RefreshBinding()`, `UpdateMode`,
  `PollIntervalSeconds`, `NoCodeFormattedText.SetValues` / `Format` / `CurrentText`, `SetProgress.SetRange`,
  `NoCodeBindText.ForceRewrite()`; `ChangeState(StateData)`, `EvaluateTransitionsNow()`, `AutoEvaluateTransitions`,
  `StateMachineData` and event accessors on the wrappers; a per-machine `StateMachine<TState>.DebugLogEnabled` and
  `EnableDebugLog` on both wrappers.
- **Network**: `NetworkActionRelay.TriggerFloatByName`, `TriggerStringByName`, `TryGetChannelIndex` and
  `GetChannel`; `NeoNetworkDiscovery.IsAdvertising`, `AutoAdvertiseOnHost` and `AutoDiscoverOnClient`;
  `StartAsClient(address, port)` and `ApplyPort` on the offline `NeoNetworkManager` stub, so code compiles without
  Mirror.
- **Editor**: `NeoxiderSettings.FindMissingFolders()`; `CustomEditorSettings.EnableTransitions`,
  `CompactRepeatedBanners`, `SetRainbowSaturation` and `SetRainbowBrightness`; `NeoInspectorTheme.AccentText`,
  `AccentFill`, `Snap` and `DrawChevron`; `NeoDocHelper.GetBrokenNeoDocPath`; `RequireInterfaceDrawer.GetTooltip`;
  `ComponentBindingInspectorShared.FindSceneObject`; `SceneSaverAutoSaveScheduler.ObserveDirty` /
  `IsIntervalElapsed`.
- **NeoxiderPages (sample)**: `PM.TryGetPageIdByName`, `PM.DebugLogs` (**Debug Logs**),
  `PM.ClosePopupsOnExclusivePageChange` (now also in the PM inspector) and the editor-only
  `PM.PreviewPageInEditor(PageId)`; `BtnChangePage.Interactable`, `ButtonAction`, `TargetPageId`, `HasLegacyAction`
  and `IsLegacyAction(Action)`; `FakeLoad.UseUnscaledTime` and `IsLoading`; `PageSubscriber.IsSubscribed`;
  `ToggleAudio.SyncFromSettings()`; `IsAvailable` on the UIKit `Audio`, `Wallet`, `Score` and `Level` facades; the
  menu **Neoxider > Samples > Generate Default PageIds**.
- **One-argument targets for UnityEvents**: `AnchorMove.Apply()`, `SetX`, `SetY` and `SetAnchor` (it used to apply
  only in `OnValidate`); `CategoryBar.DisableEntry(int)` / `EnableEntry(int)`; `MagneticField.SetModeIndex(int)` and
  `CurrentMode`; `ImpulseZone.SetDirectionIndex(int)` and `CurrentDirection` (an undefined index is ignored);
  `Leaderboard.SubmitScore(int)` / `SubmitScore(float)` (keeps the best result); `Box.ResetProgress()` (writes 0
  and raises the progress events); `Neo.UI.FakeLoad.IsLoading`.

### Changed

- **Neo inspector look**: a 44px banner whose version pill carries the update status (click to check, with a
  separate Update pill; the update strip is gone); a compact one-row header when **Show banner** is off and, by
  default, for every Neo component after the first on a GameObject (**One banner per GameObject**); 24px section
  bars with vector chevrons, skin-aware icons and accent count pills; compact boolean switches; eased transitions
  (**Smooth transitions**); parameterless `[Button]` actions are single buttons with a signature tooltip, actions
  with parameters show an "N params" chip; a redesigned Visual Settings window with editable saturation and
  brightness; mipmapped mascot icons with a per-inspector click spring.
- **Neo inspector behaviour**: `[Button]` actions run on every selected object (static methods once) and are
  recorded with Undo in Edit Mode; `[GetComponent]` / `[FindInScene]` / `[LoadFromResources]` assignments apply to
  every selected object, are recorded with Undo and warn once per object and field; mixed booleans show a dash;
  nested class headers carry the prefab override bar and Revert menu, and a closed section shows a hidden override;
  `[Header]` on private base-class fields is recognised; `[NeoDoc]` is inherited and a broken path is reported; the
  documentation preview renders code, links and bullets; a click on the mascot chip opens the component's
  documentation page (the rest of the header only bounces the mascot). `NeoxiderSettings.SaveSettings()` no longer
  refreshes the AssetDatabase, the scene hierarchy template is saved in `ProjectSettings/NeoxiderSettings.json`, and
  update-check request logs need the EditorPrefs switch `Neo.UpdateCheck.Verbose`.
- **Editor tools**: the NeoCondition inspector draws **Use Unscaled Time** and edits only the shared settings when
  several objects are selected; the Quest inspector lists `ValidateDefinition()` issues, and Generate ID derives
  each selected quest's id from its own title; Save Project Zip shows a cancelable progress bar and leaves no
  partial archive; the `StateMachineBehaviourBase` inspector uses the package sections and refreshes its Runtime
  values in Play Mode. Scene Saver writes a console line per auto-save only with **Log Each Auto-Save** (off by
  default; a failed save is always logged). The UI Mesh Rig menu messages are in English, and the Mirror lobby demo
  scene creator adds `InputSystemUIInputModule` when the Input System package is installed.
- **Audio** (**check**): `AMSettings` saves the Music/Efx/Master/UI mute under `saveKeyMute` (**Persist Mute**, on
  by default, needs **Persist Volume**) and no longer forces channels back on when it binds to `AM`; a `SetMusic` /
  `SetEfx` call made before binding is kept. For temporary silence use `AudioListener.pause`, a mixer snapshot or
  `AudioFocusMute`. `SetMasterVolume` while Master is muted only sets the level that unmuting restores, the mute
  Inspector buttons work in Play Mode only, and `RandomMusicController` skips empty track slots.
- **UI** (**check**): `AnimationFly.Play(request)` works on a copy of the request (set everything, including
  `OnAllArrived`, before `Play`) and isolates callbacks, so a throwing listener is logged and the flight still
  completes. `ButtonScale`, `ButtonShake` and `FakeLoad` default to unscaled time; `ButtonShake` restores the
  position from shake start and no longer resets it in `OnEnable`; `FakeLoad.Load()` during a load restarts it. The
  `UI` page manager's `SetPageAnim`, `SetOnePageAnim` and `SetPageDelay` finish while `Time.timeScale` is 0 (**Use
  Unscaled Time**, on by default; untick it to keep scaled delays, and give the curtain Animator Update Mode =
  Unscaled Time). `CategoryBar.Select` / `Next` / `Prev` called before `Start` become the initial selection.
  `Neo.UI.FakeLoad` matches the Pages one: in one-shot mode a later `Load()` in the same session completes one frame
  later in Play Mode (it finished inside `Load()`, often inside the page switch that showed the loading page, so an
  `OnFinisLoad` page change was undone), and a load interrupted by deactivating the GameObject continues when it is
  active again. `VariantView` applies its saved state in `Awake`, so a spawned or loaded view shows it at once.
  `ButtonScale` takes the release scale from the button's scale when a press starts instead of the `Awake`
  snapshot, so a button rescaled by layout or code comes back to its new size.
- **Abilities** (**check**): contexts the system creates for casts, projectile hits, modifier ticks and reactions
  are pooled, so a custom `IEffectOperation` must not keep the `EffectContext`, its `PrimaryTargets`, `Random` or a
  nested child after `Execute`. Modifier tick and event-reaction rolls after the first use their own derived seeds
  (a 50% DoT or thorns node no longer fires on every tick or on none; replays recorded with 11.x diverge after a
  modifier's second tick). `ModifierEngine.VersionOf` comes from one engine-wide counter (compare for equality
  only). `Direction` skillshots fly to their intended end point (`Range + cast_range_bonus`; Range 0 flies the aim
  length, at least `AbilitySystem.DefaultDirectionReach`) instead of detonating one unit after launch. Range checks,
  hub radius queries, the spatial grid and `AbilityAutoCaster` share one exact inclusive radius test in double
  precision.
- **RPG** (**check**): `RpgContactDamage.OnTargetFound` now fires when a target comes into range and `OnTargetLost`
  fires once per loss; `RpgEvadeController` reactive states notify only on change; direct 3D casts apply
  `MaxTargets` nearest-first; `AuraWeapon` skips a pulse started from a damage callback of a running pulse;
  `RpgAutoAttackController.Start` keeps a target set with `SetTarget`.
- **NPC** (**check**): `NpcRpgCombatBrain.OnChaseStarted` fires once per chase, and an unchanged chase target no
  longer restarts the path and re-fires `onTargetChanged` / `onSpeedChanged` every decision tick; a continuing chase
  re-paths at **Path Update Interval**, and with **Auto Update Path** off the brain still follows a moving target;
  Combined mode retries an unreachable chase after that interval; `onPathBlocked` / `PathBlocked` fire once per path
  request; a combat receiver added after `Awake` gates actions.
- **Shop**: `ShopListView.OnRefreshed` fires once per purchase or bundle (it fired 3 or N+1 times); `SetItems`
  spawns rows for new catalog entries; `TryBuyBundle` reports `AlreadyOwned` for an owned single-purchase bundle;
  `ShopVariantsPanel` re-pushes only the slots that changed and raises `ApplyVariantState` and the
  `ShopVariantStateView` events once per change (`RefreshStates()` still pushes every slot). After editing
  `ShopItemDatas` in place, call `SetItems(shop.ShopItemDatas, false)` to re-index.
- **Bonus**: a `Collection` with **Set Instance On Awake** off loads its saved unlocks in `Awake`; such a
  `CollectionVisualManager` wires its own item buttons, and `ItemCollection.Unlock()` writes into the collection its
  manager displays; `Collection.Set` is read-only, and an out-of-range injected random index throws instead of
  granting the lowest locked slot. Every UnityEvent of `Collection`, `CollectionVisualManager`, `Box`,
  `ItemCollection`, `LineRoulett`, `SpinController`, `ButtonPrice`, `ShopItem` and `NpcNavigation` exists right
  after `AddComponent`, so code-built scenes can subscribe without null checks.
- **Cards** (**check**): scattered layouts and layout jitter are deterministic per card index and no longer consume
  `UnityEngine.Random`; hand sorting is stable; `DeckComponent.Reset()` / `Initialize()` clear the visual stack
  board, `ReturnCard` detaches the card from its hand or board, and the trump preview follows the bottom card. A
  batch placement raises `OnCardPlaced` per card after the whole batch and then at most one `OnBoardFull`; batch
  removal raises nothing; `DrunkardGame` fills and empties its board in batches (no extra `OnBoardCleared` per
  restart). A UI card released by a board or hand stays under the root Canvas of the zone it left.
  `DeckComponent.BuildVisualStackAsync()` and `BoardComponent.RestoreAllSourcesToBoardAsync()` complete
  synchronously; `GetAllRanks()` / `GetAllCardData()` skip empty entries.
- **GridSystem**: `TicTacToeBoardService` raises `FieldGenerator.OnCellStateChanged` for moves and resets after the
  round state is committed; `SlidingMergeResolver.Slide`, the SlidingMerge and Dice `ClearBoard` and
  `GridSlotAllocator.Clear` notify after every cell is written; on gapless rectangular grids
  `GetNearestCellFromWorld` is constant-time and may pick the other cell on an exact shared edge; custom path
  predicates run at most once per cell. `Match3BoardService` reports busy inside a delayed resolve's first step and
  idle inside its completion callbacks, so a move or level chained from `OnMatchesResolved` is accepted; a cascade
  limit of 0 no longer sticks busy, and `InitializeBoard()` from a resolve callback stops the old cascade.
- **Save, Settings, Level**: `SaveProviderSettingsComponent` runs at `[DefaultExecutionOrder(-1000)]`, so readers in
  the same scene load from the configured provider; an `OnDataLoaded()` exception is logged and the rest of the load
  continues; `FileSaveProvider.Save()` with nothing new skips the disk write (it still raises `OnDataSaved`),
  integers are written in the invariant culture (device-culture values from older builds still read), and a rejected
  provider configuration is always logged. **check**: `GlobalSave.SaveProgress()` does nothing before
  `GlobalSave.data` was read or assigned (it used to erase the stored profile). `SaveManager` flushes on both focus
  loss and pause.
- **Quest, Progression** (**check**): `Notify*` with a null or empty target id credit nothing (credit such
  objectives by index); when an objective listener throws, the quest still completes and the exception is rethrown
  afterwards; Auto Save writes once per top-level operation; unlocks and purchases with several rewards publish one
  settled state before `OnNodeUnlocked` / `OnPerkPurchased`, and `TryUnlockNode` / `TryBuyPerk` re-check ownership
  and points after user conditions, so a re-entrant claim cannot charge twice.
- **Condition, NoCode, StateMachine** (**check**): `NeoCondition` **EveryFrame** throttles on unscaled time and
  keeps checking while `Time.timeScale` is 0 (**Interval** follows game time unless **Use Unscaled Time** is on).
  `NoCodeBindText` writes only changed values. **Enable Debug Log** is per component instead of one shared switch. A
  machine configured before `Awake` survives activation; loading `StateMachineData` enters the assigned Initial
  State asset; a target state's constructor that changes or stops the machine wins; a `FromStateType` changed after
  `RegisterTransition` routes from its new source; `StateData` action lists run the entries present when a pass
  starts; `StateMachineData.Validate(false)` always writes its warnings.
- **Network**: `NetworkContextActionRelay` sends AllClients/OthersOnly actions to the observers of its
  `NetworkIdentity`, so a client that hides the relay is no longer disconnected; on a host it sends to remote
  observers before applying locally, and listener exceptions are logged instead of disconnecting the sender.
  `NetworkActionRelay` OthersOnly also targets observers. `NeoNetworkDiscovery` advertises once per server or host
  start with any network manager, stops with the server and no longer re-broadcasts searches during a session; an
  advertising failure is one warning. `NetworkEventDispatcher` and `NetworkPlayerName` reject remote commands while
  **Is Networked** is off, and `NeoNetworkBootstrap` starts again after a session ended without `StopNetwork()`.
- **Tools/Input** (**check** in Input System-only projects): without a connected mouse, `MouseInputCompat` position,
  delta and left button read the primary touch, as legacy `Input.simulateMouseWithTouches` does (MouseInputManager,
  MouseEffect, InteractiveObject, Drawer, the Move mouse components, RpgButtonBinding). Opt out with
  `OptionalInputSystemAdapter.SimulateMouseWithTouches = false` from a `BeforeSceneLoad` hook. Legacy and Both
  projects are unchanged.
- **Tools/Move, Physics** (**check**): `MagneticField` **Max Overlap Colliders** is the initial size of a growing
  buffer, not a cap. `Follow` rotation limits and `UniversalRotator.limitRange` share one sector rule: the nearer
  edge wins, sectors past 180° stay reachable, a range of a full turn or more disables limits (`[-360, 360]` no
  longer pins every aim to 0), and an inverted range wraps through 180°. `Follow.TeleportToTarget` applies position
  limits and snaps only the channels the follower follows (a rotation-only follower keeps its position and turns
  straight to the aim, a position-only follower keeps its rotation); `DistanceChecker` fires `onDepart` once when
  its target or source is lost; `AdvancedForceApplier` **Clamp Max Speed** also clamps the two physics steps after
  `ApplyForce`; player `Teleport` moves the Rigidbody; the player animator drivers skip parameters the controller
  does not declare. `CameraConstraint` pauses instead of disabling itself when its bounds source disappears and
  resumes on the next one; `IsAtEdge` reports only constrained axes. A deactivated or destroyed
  `CursorLockController` hands look back like `DisableController()`, and `PlayerController3DPhysics` treats an
  inactive owner as absent.
- **Tools** (**check**): `ErrorLogger` history is capped by **Max Entries** (50; 0 = unlimited); `Loot` does not
  drop while its scene unloads or the app quits; `CameraAspectRatioScaler` stores its reference size and field of
  view on the component (existing scenes capture the camera once); `Bootstrap` finishes its startup pass when an
  `IInit.Init` throws and sets that target aside; `TypewriterEffect` keeps `CharactersPerSecond` over time, typing
  several characters per frame when needed, and carries at most 0.1 s of a long frame into the next characters;
  `ChanceManager` Inspector weights are no longer clamped to 0–1; `GameTimeController.SetTimeScale(0)` pauses like
  `PauseGame()`.
- **Pools and inventory**: a destroyed `PoolManager` retires its pools (stored instances are destroyed, a rental
  still out is destroyed on its return, and `Get` on a dying manager returns null);
  `SpawnUtility.ClearFallbackPools()` retires the pools it drops; fallback pools keep objects the incoming scene
  spawned in its own `Awake` / `OnEnable`. **check**: `InventoryHand` keeps its spawned visual and writes to a
  linked `Selector` only when the slot index or count changes; subscribe to
  `InventoryComponent.OnSelectedItemChanged` for item changes in the selected slot.
- **Packaging**: package assets no longer reference assets that ship only in `Samples~`, so a UPM install without
  the samples has no missing references. The package now has its own copies (new GUIDs): `Prefabs/Bonus/Slot/Row`,
  `SlotElement`, `SlotUI` and `Resources/Slot/SpritesDafualt.asset` use `Art/Bonus/Slot/` (`Frame 3`-`Frame 6`,
  `Group 2`, `Group 2-1`, `Layer 4`, `Layer 11`); `Prefabs/Shop/ItemShop` uses `Art/Shop/` (`Group 132`,
  `Group 133`, `Resource 1@4`); `Prefabs/Bullet`, `Prefabs/Simple Weapon` and `Prefabs/Tools/AI/OLD AI NPC` use
  `Art/Materials/Neo Yellow.mat` and `Neo Red.mat`; the bundled `ThirdParty/CharacterMovementFundamentals/Art`
  controller and materials use `Art/Capguy/Animations/` (nine clips), `Art/Capguy/Textures/` and
  `Art/Environment/Textures/Tex_Grid_Red.png`. Files that only those package assets used left the samples: Demo
  `Sprites/Group 2-1.png`, `Sprites/Layer 11.png`, `Sprites/Clicker/Group 132.png` and `Group 133.png`; Character
  Movement Fundamentals the nine `Capguy/Animations/*.fbx` clips (Fall, Idle, Jump, Land, WalkBackward, WalkForward,
  WalkLeft, WalkNeutral, WalkRight), `Capguy/Model/Textures/Tex_Cap.png`, `Tex_Capguy.png` and
  `Environment/Textures/Tex_Grid_Red.png`. See
  [moved assets](Docs/Migration12.md#package-assets-moved-out-of-the-samples). The folder metas without a folder
  (`Scripts/GridSystem/Editor.meta`, `Scripts/NPC/Core.meta`, `Scripts/Rpg/DemoUtils.meta` and the Character
  Movement Fundamentals `Misc/Physics Materials.meta`) are gone, so a fresh install no longer warns about them.
- **NeoxiderPages (sample)** (**check**): `PM` page switches and setup warnings log through `NeoDiagnostics` behind
  the per-manager **Debug Logs** flag (errors for missing pages still log), and name lookups (`ChangePageByName`,
  `TryGetPageIdByName`, GM Win/Lose/End) use a name index instead of LINQ and per-switch `Object.name` reads. The
  Pages `FakeLoad` uses unscaled time by default (existing instances included); a one-shot load completes one frame
  after `Load()` in Play Mode, so a loading page used as PM's startup page closes on scene revisits, and a load
  interrupted by deactivating its GameObject continues on reactivation. `BtnChangePage` buttons whose Action still
  holds a legacy UIKit.Page value (for example 12 = Pause) open their Target Page again with a one-time warning
  (the inspector offers **Use Open Page**), and adding the component no longer creates a `PageOpen` asset. The PM
  Editor Preview buttons no longer raise `OnPageChanged`. `UIPage.HasShowAnimation`, `ShowAnimationDuration` and
  `HideAnimationDuration` are virtual, so animated pages can make PM wait.
- **Extensions, Core, Reactive, Haptics** (**check**): `StringExtension.ToFloat` / `ToInt` parse the invariant
  culture first (`"1.500"` now reads as 1.5 on dot-grouping locales) and `ToBool` is ordinal; a destroyed `Camera`
  counts as null in `ScreenExtensions` and `UIUtils.WorldToCanvasPoint`; `LookAt2D` and `GetClosest` are null-safe;
  `NeoDiagnostics.LogWarningThrottled` never forgets a running window and lets new keys at the bound share one
  overflow window; `ResourcePoolModel.Tick` raises nothing for regen or heal cycles that add nothing; weak
  `ReactiveProperty` subscriptions with a destroyed target are removed silently; a throwing `Haptics.Played`
  observer is logged and the pulse still plays.

### Performance

Measured in the Unity 6000.3.14f1 Editor (Mono, batch, profiler off), with the old code path and the new one in the
same harness. Run the explicit `Performance` test category to measure on your machine
([Testing](Docs/Testing.md#performance-and-allocation-tests)) and compare `NEO_PERF` lines only from the same
machine and Unity version.

| Hot path | Before | 12.0.0 |
|---|---:|---:|
| Input System key read (`InteractiveObject`) | 35.1 µs, 24 GC.Alloc | 0.41 µs, 0 |
| Mouse poll (`MouseInputManager`) | 60.9 µs, 39 GC.Alloc | 1.02 µs, 0 |
| Primary touch (`SwipeController`) | 34.4 µs, 25 GC.Alloc | 0.92 µs, 0 |
| `MoveInputCompat.ReadMove` (8 keys + left stick) | 113.2 µs, 68 GC.Alloc | 3.55 µs, 0 |
| 1000 `InteractiveObject`s on one shared ray, per frame (5 routes) | 1008–1927 ms | 6.3–10.9 ms |
| 100 `InteractiveObject`s on one camera ray, per frame | 12.3 ms | 0.64 ms |
| `Match3BoardService.TryFindValidSwap`, 32×32 board | 37.3 ms, 4978 allocations | 0.52 ms, 0 |
| `Match3BoardService.TryFindValidSwap`, 16×16 board | 2.41 ms, 1202 allocations | 0.12 ms, 0 |
| `Selector` getters at 1000 indices, 100 calls | 4.77 ms, 2400 allocations | 0.0075 ms, 0 |
| StateMachine transition to a cached C# state type | 0.86 µs, 3 allocations | 0.34 µs, 0 |
| NoCode StateMachine evaluation, 128 states / 256 transitions | 17.25 µs | 0.55 µs, 0 |
| 128-card stack on a `BoardComponent`: one by one vs `PlaceCardsImmediate(cards)` | 113 µs per card | 14.8 µs per card |
| Ability radius query among 1024 units: linear scan vs opt-in spatial grid | 307 µs | 0.96 µs, 0 |

- **No allocations once warm**, guarded by GC.Alloc tests: Input System polling and
  `UIInputCompat.IsPointerOverUI()`; ability event publication, instant and projectile casts, hits, modifier ticks,
  reactions, `chain`, `dispel` and `remove_modifier`; the RPG character tick, regen, damage, Direct/Area casts, aura
  pulses and target selection; accepted Match3 swaps with their cascades and the no-moves shuffle; SlidingMerge
  `CanSlide` / `HasAnyMove` and pathfinding queries; immediate board and hand layouts,
  `PokerHandEvaluator.EvaluateStrength`, hand queries, comparers and shuffles; `NeoObjectPool` Get/Return (also
  O(1)); StateMachine updates and transition evaluation; `Selector` commands; quest notifications, reward dispatch
  and `NpcNavigation` ticks; `Shop` id lookups and `ShopListView` rebinds; `TimerObject` labels and `TimeToText`;
  warm `AM.Play` / `TryPlay` / `PlayAt`; `Drawer` stroke rebuilds; `ExplosiveForce` / `MagneticField` overlaps; UI
  Mesh Rig rebuilds; settings preset and quality changes and `GameSettings.SaveState()`; throttled `NeoDiagnostics`
  warnings; `UIUtils.IsPointerOverUI` / `GetUIElementsAtPosition`, `GetCombinedBounds` and the list/array
  extensions. `ResourcePoolModel.Tick` and `ReactiveProperty` set-and-notify measured 0 allocations. The working
  GC.Alloc counter also found two hidden allocations, now fixed: RPG direct 3D casts, 2D casts and
  `RpgProjectile` sorted hits through `Array.Sort` / `List.Sort` with a comparer, which wraps a new delegate on
  every call, and three-argument `Mathf.Max` / `Mathf.Min` (a `params` array per call) ran in every `AuraWeapon`
  pulse and every `SpinController` line count; a source guard keeps `params` overloads of `Mathf.Max` / `Min` out of
  the package.
- **Algorithms**: N interaction targets on a shared ray of H hits cost about H + N instead of N × H; batch board
  placement and removal use one layout pass (O(n) transform writes instead of O(n²)); `SlotGridInventory.Add` is
  O(slots) instead of O(slots²); ascending progression reward tracks are walked in one pass; the ability spatial
  grid is patched in place on `TryMoveUnit` and unregistration; ability id lookups keep their case-insensitive rules
  with an ASCII fast path; `ResourcePoolModel.Tick` reuses its registration snapshot; `MergeResolver` cleans visited
  state in O(group); NPC re-paths are staggered. Bonus `CheckSpin` payline counting is allocation-free
  (`GetWinningLines` / `GetMultiplayers` still allocate once per spin).
- **Fewer native calls**: `ParallaxLayer` skips its per-tile pass while nothing moved; `Follow`, `ScreenPositioner`,
  `CameraConstraint`, `BillboardUniversal`, `ZPositionAdjuster`, `MeshEmission`, `LightAnimator`,
  `TransformAnimator` and `AM` write only on change; `ImageFillAmountAnimator.SetValue` keeps the running tween for
  a repeated target; `UIMeshRigElement` repaints only while its pose changes over time, and the world and sprite rig
  renderers upload only positions on a pose rebuild; `FileSaveProvider` skips unchanged flushes; `[SaveField]`
  lookups and collection wrappers are resolved once per type; `NetworkContextActionRelay` caches resolved types and
  methods; animator drivers and `AnimatorParameterDriver` use cached hashes.
- **Startup and Editor**: the singleton and `NetworkSingleton` static-reset sweeps run only in the Editor over the
  assemblies that can declare them, and the `NeoEditorAutoRegister` type scan is gone. Each Neo inspector resolves
  its property plan once, reuses cached `SerializedProperty` objects, styles and labels, retries unresolved
  `[FindInScene]` / `[GetComponents]` / `[LoadFromResources]` lookups only after a change or every 0.5 s, and idles
  its repaint ticker while Unity is in the background. The NoCode pickers and the NeoCondition, AM, DeckConfig,
  Progression, Level Curve and Stat Growth inspectors, UI Mesh Rig handles, Ability Designer, the State Machine
  editor and the Create Neoxider Object window cache what they used to rebuild per repaint. Section titles, count
  pills, badges and the banner reuse their `GUIContent` and remember text widths; the State Machine Data, State
  Data, Quest Flow Config, Scene Saver and UI Mesh Rig adapter inspectors rebuild their badge arrays and row texts
  only when a value changes, and the State Data action-type scan runs once per domain reload.
- **`ImageFillAmountAnimator.SetValue` with the running target** no longer allocates: its tween lambdas captured
  locals of the whole method, so the compiler created their closure object on every call, the early return for a
  repeated value included (one GC.Alloc per call from a binding that pushes the same value each frame).

### Fixed

- **Tools/InteractableObject, Input**: a target whose collider sits only on a child is found again in Editor Play
  Mode (player builds were not affected); a filtered 2D raycast no longer returns a stale cached result after
  `Physics2D.queriesHitTriggers` changed in the same frame; the automatic EventSystem check costs one scene search
  for many targets and no longer throws and catches in Input System-only projects; `FreeFlyCameraController` wheel
  speed, `Drawer` and the player controllers work in Input System-only projects; the `MouseInputManager` gizmo uses
  the event it built.
- **Tools/Move, Physics**: `Follow` 2D keeps its rotation while sitting on its target, and a follow-state listener
  can move the follower or clear the target safely; `PlayerController3DPhysics` applies camera pitch every rendered
  frame and unsubscribes exactly what it subscribed; `NeoCharacterSprint` without an `AdvancedWalkerController`
  warns once and disables itself instead of throwing every frame; `MagneticField` with an Update Interval runs its
  first update immediately; `ExplosiveForce` survives a listener that explodes again; `MouseMover2D` /
  `MouseMover3D` find `Camera.main` again after a camera swap.
- **Tools/Time, Random**: `TimerObject` **Time Text** (TMP) works; it was compiled out behind an undefined define
  (assign the label explicitly; it is never auto-found). An invalid Time Format shows the default with one gated
  warning instead of throwing; day-cycle and infinite timers no longer drift from float rounding; the start pulse
  restores the original scale on restart or disable; `TogglePause` no longer resumes to a stale slow-motion speed.
  `IntegerChanceTable` applies Inspector edits without `Invalidate()`, and `UnityRandomBelow` no longer loops
  forever above 2^60.
- **Tools/Components**: `CameraAspectRatioScaler` no longer creeps the camera size up after an Edit Mode preview and
  a reload; a relative `CameraShake` returns a moving camera to its pre-shake pose; `Counter.Registry` is cleared
  between play sessions with domain reload disabled; `ScoreManager` derives its starting stars like `ResetScore()`.
- **Tools/Spawner, Inventory**: a pool rental with no parent moves into the active scene in Play Mode instead of
  leaking into the next scene from `DontDestroyOnLoad`; destroying only the `PoolManager` component destroys its
  stored instances; a prefab spawned from the incoming scene's `Awake` gets a fresh fallback pool instead of one
  whose storage died; `InventoryPickupBridge` and `InventorySlotView` ignore destroyed pickups and grids; a stale
  `AutoSaveScope` dispose is ignored.
- **Tools/View, Animations**: `Selector` keeps Inspector-authored Excluded Indices in player builds, and an emptied
  set is saved as an empty value; `LightAnimator` stops cleanly when its light is destroyed;
  `MeshEmission.ResetToOriginal` restores the exact baseline with `emissionCutoff >= 1`.
- **UI**: `AnimationFly` components other than the singleton find their Bonus Prefab List types in player builds and
  use their own screen-to-world depth, and a huge count product spawns the cap; `ButtonChangePage` press feedback
  scales every axis, and navigation ignores a destroyed page manager; `FakeLoad` finishes zero-length loads once and
  stops the old stream when `EndLoad()` / `Load()` is called from a progress listener; `CategoryBar` logs no error
  when it is destroyed with its scene. UI Mesh Rig: adding or removing an owner component between a rig and its
  points moves those points to the right rig, points with a destroyed owner no longer throw, the creation menu
  commands are one Undo step, and Add Point / Duplicate / preset points keep their keys and settings through Redo.
- **Abilities**: grid queries with huge radii or far coordinates return the same units as the linear scan on every
  platform (x64 builds returned nothing); a unit on the auto-caster's range boundary is no longer rejected as
  `OutOfRange`; a projectile releases its pending cast when disabled or destroyed and returns to the pool when the
  hub is gone; `DestroyUnit` forgets the unit's modifier bookkeeping; an `AbilityUnitBehaviour` that outlives its
  hub no longer unregisters the replacement hub's unit with the same id; shielded hits no longer keep the last
  victim's modifiers alive.
- **RPG**: NaN, infinite and overflowing stat bases are ignored and such save rows are skipped (one NaN base made a
  character that could never die); resource and stat `SetCurrent` / `SetMax` keep their value on NaN; `AddXp(NaN)`
  is ignored, and upgrade points and levels saturate instead of wrapping; destroyed network adapters no longer
  swallow commands; `IRpgCombatReceiver.TakeDamage` keeps the hit source; the server drops a network command of an
  unknown type instead of replicating a snapshot for it.
- **NPC, Shop**: NPC animator parameters are validated once per controller (no per-frame "parameter does not exist"
  warnings); `TextMoney` binds when a wallet registers instead of scanning the scene every frame, and recovers after
  disable/enable; Shop-owned buy buttons unsubscribe from the button they subscribed, and `SetItems` no longer
  stacks duplicate listeners.
- **Bonus**: `WheelFortune` with Single Use off no longer locks its CanvasGroup after the first spin, and a
  destroyed wheel graphic ends the spin; the obsolete `TimeReward` keeps its running or paused state while inactive;
  `CollectionVisualManager` subscribes when hidden before its Collection exists and no longer throws when a
  code-created Collection appears after it; automatic `BoxProgress` claims never collide with caller claim ids; a
  slot `Row` spun before its `Awake` keeps its spin.
- **Cards**: deals after `Reset` / `Initialize` always carry the card that left the model; `ShuffleVisualAsync` no
  longer shakes cards dealt mid-shake and accepts negative durations; `DeckModel.Draw(-1)` and negative layout
  counts no longer throw; `CardSettingsRuntime.GlobalAnimationConfig` resets each play session; a card that leaves
  the board through a hierarchy callback during placement no longer stays a phantom member; a `DrunkardGame` restart
  from such a callback cannot place the old card; an aborted stack build destroys the cards it created, and the
  debug list no longer grows by a deck per restart; `PlaceCardsImmediate` stops after a listener clears the board.
- **GridSystem**: a TicTacToe move or reset made from a cell listener is handled consistently; Dice Inspector edits
  in Play Mode reach the live board; on an inactive GameObject a delayed Match3 swap or resolve runs immediately
  instead of failing to start its coroutine.
- **Save, Settings, Level**: a Deferred settings save whose coroutine was stopped from outside is restarted by the
  next change; if the save provider throws partway through `ResetAllProgress()`, the maps already reset stay reset
  and their notifications still run. `LevelComponent` publishes the new XP and XP-to-next values before the level
  listeners and `OnLevelUp` run, so an XP label or bar next to a level label no longer shows the previous profile
  inside a level callback, and each `AddXp` / `SetLevel` call (also one made from an `OnLevelUp` reward) notifies
  every reactive state exactly once.
- **Quest, Progression**: crediting an objective through a config with appended objectives stores the completion it
  reports; ReachPoint and Talk objectives can be completed by Target Id; a jump to a very high level no longer
  iterates every level number; `ValidateDefinition` reports a whitespace-only id.
- **Condition, NoCode, StateMachine**: an empty Source Object, Other Source Object or NoCode Source Root saved in a
  scene, prefab or asset falls back to the context in Editor Play Mode (for example, a `ConditionEntryPredicate` in
  a saved `StateMachineData` never passed); an inverted entry can no longer pass on a missing object; Initial State
  Type Name resolves full type names across assemblies and warns when **Enable Debug Log** is on; labels of
  unregistered transitions are no longer kept alive.
- **Network**: the relay type-name cache scans again after a new assembly loads (mods, hot-update DLLs);
  `NeoMirrorSceneReactivator.Enabled` resets at play start with domain reload disabled, and
  `NeoNetworkBootstrap.HasStartedNetwork` clears on the frame after a session ends without `StopNetwork()`;
  `NeoNetworkComponent.RateLimitCheck` drops expired connection entries on long-running servers.
- **Editor**: the scene hierarchy template is applied on the first access after an editor restart; a manual update
  check no longer drops back to idle while the tags fallback runs; an empty `[GetComponents]` / `[FindAllInScene]`
  collection no longer keeps the scene marked modified; removing a Neoxider Settings folder no longer desyncs the
  layout, the Missing Scripts window no longer removes rows mid-draw, `RequireInterface` and UnityEvent labels keep
  their tooltips, the neutral mascot face keeps its quality, and DeckConfig / NeoCondition text is readable on both
  editor skins; the Dialogue Editor no longer dirties the scene when closed and follows undo, outside edits and skin
  switches; the Edit Transition window keeps its transition across script reloads only when that row is unchanged;
  `CreateMenuObject.startPath` always uses '/', Prefab To Sprite shows the real preview, and save panels and dialogs
  no longer leave layout errors; the Ability Designer rebuilds its effect board after outside edits; the
  popup-option cache no longer keeps closed inspectors alive.
- **NeoxiderPages (sample)**: `PM.SetPage` raises `OnPageChanged` once (it raised it twice); a switch to an unknown
  PageId no longer blanks the current and previous page; reopening the current page keeps the Back history, so Back
  no longer closes the only visible page; `CloseCurrentPage` on a popup makes the page under it current again;
  `FindPage` no longer returns a destroyed or re-assigned cached page. `BtnChangePage` no longer throws without a
  `PM` or a parent, ignores right and middle clicks, warns on an undefined Action value and keeps non-uniform scales
  in its press animation. `PageSubscriber` resolves PageIds through the PM instead of
  `Resources.FindObjectsOfTypeAll`, waits for EM (it threw with GM but no EM) and unsubscribes when destroyed.
  `ToggleAudio` works without GM and no longer lets the toggle's initial state overwrite the saved mute. The UIKit
  `Audio`, `Wallet`, `Score` and `Level` facades return defaults instead of throwing when their manager is missing,
  and `G` checks GM/EM with Unity null semantics. A repeated one-shot `FakeLoad` completes at once, `Load()` during a
  load restarts it, and its one-shot flag and the `UIKit` listeners reset each play session. The Privacy Page
  prefab's Accept button opens PageMenu and the Menu Page settings button opens PagePause again (their stale action
  values 3 and 12 did nothing).
  The Pages inspectors no longer allocate or search the scene on every repaint, the PageId dropdown writes only
  when you pick an entry (safe multi-object editing), and Auto Sprite Assigner resizes can be undone.
- **Rewards and events of components created from code**: `CooldownReward.SetRewardAvailableNow()` raises
  `OnRewardAvailable` (it marked the reward available before the refresh that announces it, unlike `TimeReward`).
  `ColorAnimator`, `Vector3Animator`, `FloatAnimator`, `TimerObject` and `Neo.UI.FakeLoad` initialize their
  UnityEvents, so a component added with `AddComponent` has events to subscribe to.

### Demos

- **Every module demo scene is authored in the scene.** The Demo Scenes sample and the NeoxiderPages UI scene are
  built by editor scene builders (`Neo.Demo.Editor`) and saved with their UI, configured components and persistent
  UnityEvent wiring, so you can inspect the setup without Play Mode; nothing is built at Play start. Rebuild a scene
  from **Neoxider > Demo > Rebuild Active Demo Scene**; see [how demo scenes are
  made](Docs/Samples.md#how-demo-scenes-are-made).
- **New and reworked scenes**: CardGame guide overlay (fixed shuffle seed, restart); AnimationFlyDemo (coin, gem and
  chest flights, `PlayByType`, live readouts) and UIMeshRigDemo with the SpriteRenderer adapter; SlotExample and
  WheelFortuneExample with demo bets, paylines and paytable data; ChanceSystemExample (`IntegerChanceTable` jackpot
  odds) and a Dialogue scene with `DialogueLinePlayer` barks; Audio, Parallax, Light and Selector demos;
  InventoryUseDemo with slot grid views and a generated item database; AbilitiesNoCodeDemo, SurvivorDemo
  (`SurvivorHudView`) and the VampireSurvivorMCP HUD; RpgCharacterQuickDemo and RpgCombatNpcDemo (`ApplyDamage` with
  a source, `DamageApplied`, an `IRpgDamageModifier` guard); CameraAspectRatio, Follow, DrawExample,
  MouseEffectorExample and CameraConstraint; Save (64-bit wallet, write counters), Settings (deferred save) and
  LevelFlowDemo; AttributeExample; GridSystem Match3, TicTacToe (5×5 four-in-a-row, O bot) and Dice Merge with an
  Edit Mode board preview; Progression and Quest; Shop and the NPC scene migrated from `AiNavigation` to
  `NpcNavigation`; StateMachine, NeoCondition, NoCode binding and NoCode actions; NetworkDemo (relay scopes, a
  server-authoritative boss, LAN discovery, telemetry rows).
- **New module scenes**: `Scenes/Animations/AnimatorsDemo.unity` (`FloatAnimator`, `ColorAnimator`,
  `Vector3Animator` and `TransformAnimator` on real targets with Inspector-wired value bindings,
  Play/Pause/Resume/Stop, a shared AnimationType and speed, TransformAnimator channels and a Shake impulse; menu
  **Neoxider > Demo > Open Animators Demo**); `Scenes/UI/UIComponentsDemo.unity` (`CategoryBar` tabs switching a
  `VariantView` page, a `VariantView` badge, `AnchorMove`, `FakeLoad`, `ButtonShake` and a `PausePage` overlay with
  scaled and unscaled clocks); `Scenes/Bonuses/BonusRewardsDemo.unity` (a reward loop of Bonus components only: a
  `Collection` of six pizzas shown through `CollectionVisualManager` slots and an `ItemCollectionInfo` details row, a
  `Box` chest filled by a `CooldownReward` gift that grants a random locked pizza, the legacy `TimeReward` paying
  into a session-only `Money` wallet, and a reset); `Scenes/Tools/Physics/Physics3DDemo.unity` (`MagneticField`
  pulsing between pull and push, an `ImpulseZone` trigger above an elastic plate that keeps balls bouncing, with a
  Kick button, and a manual `ExplosiveForce` mine fired with `Explode()` and `Explode(18)`). The UI Components,
  Bonus Rewards and Physics 3D scenes are wired without a demo script.
- **Example games**: the hand-authored NoCode reference games `_ExampleGame/adventure`, `ClickerExample` and
  `Shooter2D` get minimal builders (`Editor/Builders/ExampleGames`) that add an info card naming the key objects and
  their UnityEvent wiring and replace the legacy `StandaloneInputModule`; the games themselves are never rebuilt.
  The NeoxiderPages `UI.unity` gets a plain uGUI **[Demo] Pages Info** card (no Demo-sample components, portrait
  scaling) with Pause popup / GM Win / Back / Close buttons, authored by `PagesUIBuilder`.
- **Behaviour**: demos use demo-only save keys and files (`Neo.Demo.*`, `NeoDemo.Quests`, `neo_save_demo.json`), so
  they no longer overwrite a game's keys; EventSystems use `InputSystemUIInputModule` (a hand-authored
  `StandaloneInputModule` is replaced on rebuild); `DemoPointerGate` keeps clicks on demo UI from drawing, dragging
  or spawning in the world; NeoxiderPages scenes contain no Demo-assembly components, so a Pages-only import has no
  missing scripts; the NetworkDemo manager no longer auto-creates a player without a prefab and is no longer
  `DontDestroyOnLoad`.
- **Save keys of the example games** (**check**): adventure saves its inventory under `NeoDemo.Adventure.Inventory`,
  ClickerExample under `NeoDemo.Clicker.Money` / `.Shop` / `.BestScore` / `.Level`, Shooter2D under
  `NeoDemo.Shooter2D.Money` / `.BestScore` / `.Level`, and the Pages UI scene under `NeoDemo.Pages.Money` /
  `.BestScore` / `.Level`; Progression_Demo drops the unrelated `PlayerInventory` / `Progression_Managers` objects,
  whose inventory autosaved `Inventory_Default`. ClickerExample progress saved by earlier versions under the
  default `Money` / `Shop` keys is not carried over: those keys belong to your game.
- **Sample scripts**: `SurvivorPlayerController` reads movement through `MoveInputCompat`, so SurvivorDemo works with
  Active Input Handling = Input System Package (New). `RpgWorldHealthBars` gains `Track` / `Untrack`, a
  scene-assigned target list and a **Scan Scene For Targets** toggle (the scene search is only a fallback for
  hand-made scenes) and no longer allocates per frame; RpgCombatNpcDemo authors its targets. The `CubeEnemy` prefab
  carries an authored `RpgDeathHandler`, so `VampireSurvivorDemoController` no longer adds one at runtime (an enemy
  without it is deactivated on death with a one-time editor warning), and `SurvivorGame` reports a missing
  `SurvivorConfig` as a one-time editor warning. Builder texts use `->` instead of U+2192, which the static TMP atlas
  lacks.
- **Demo kit** (`Neo.Demo.Editor`, for builder authors): `DemoSceneBuildContext` gains `Retire`, listener ownership
  (`OwnListeners`, `TrackListeners`, `ValidateListeners`), `AllowRuntimeAsset`, `FindHandAuthoredRoot`, `HostOf` /
  `IsGeneratedTarget` / `IsGeneratedRoot` / `TryGetRootKey` and nested `.Array.size` paths in `Set`;
  `DemoWiring.AddDynamic` for two- and three-argument events, `AddSetter` / `Setter`, `PruneOwned` / `PruneWhere`;
  `DemoSceneKit.CornerPanel`, `WorldLabel`, `Readout`, `Step` / `StepFailed`, `UpgradeLegacyInputModule(s)` and
  public backdrop helpers; `DemoFrameOptions` header/log widths, content card and input-module upgrade; `DemoPanel`
  value, text, log and bar rows; `DemoSceneAssets.WhiteSquare`, `EnsureAsset`, `EnsureRenderTexture`,
  `EnsurePrefab`, `LoadDemoAsset`; `DemoSceneRunner.TryCreateSceneAsset`, so **Neoxider > Demo > Rebuild ...** and
  `DemoSceneBatch.RebuildAll` create a missing builder scene (and its folder `.meta` files) instead of failing with
  "Scene asset not found". Validation warns about characters missing from a static TMP atlas and Mirror scene ids
  of 0; reused primitives and prefabs no longer get duplicate children. Game-view PNGs can be captured
  without Play Mode (`Neo.Demo.Editor.DemoSceneBatch.CaptureAll` with `-neoDemoShots <dir>`, `-neoDemoScenes`,
  `-neoDemoShotSize WxH`, or **Neoxider > Demo > Capture**); scenes are never saved.
- **Demo save keys and text**: Shop, AttributeExample, NeoConditionDemo and CardGame now persist under
  `NeoDemo.<Demo>.<Data>` keys too, so no sample scene reads or overwrites the save of the user's own game (the
  NeoCondition '+25' button used to write the user's `BestScore`). Arrows and circled digits in demo labels and log
  lines are plain ASCII (`->`, `<-`, `1.`), so TMP no longer adds them to the dynamic fallback font asset during
  Play. VampireSurvivorMCP assigns its health bars to the controller and turns the bars' scene search off in the
  saved scene.
- **Mirror scene ids in demo scenes**: with Mirror installed, entering Play Mode in ClickerExample or Shooter2D
  stopped at once ("needs to be opened and resaved"): the `--System--` prefab's `Money` carries a `NetworkIdentity`
  whose sceneId Mirror assigns only in memory. A rebuild now gives every scene `NetworkIdentity` without an id a
  stable one (scene path + hierarchy path) and records it on prefab instances.

### Docs and tests

- **Multiplayer verified on real peers**: the dedicated-server matrix (`Tests/Integration/Network`,
  `scripts/network_playmode_matrix.ps1`) ran for the first time: one server and two client players over Mirror
  Telepathy passed all 54 cases, and every package assembly also compiles without the `MIRROR` symbols. The
  launcher now compares Unity's forward-slash `persistentDataPath` as a full path (its isolation probe rejected
  every run), and the matrix players carry version 12.0.0.
- **Pictures in the docs**: the repository README, the package README and the Docs index open with a gallery of
  demo-scene captures (Editor and Play Mode) linked to the module guides, plus a "What's new in 12" summary with
  measured numbers; every module guide shows its demo scene and a concept illustration in the README style. The
  images live in the repository's `Images/demos` and `Images/modules` folders, outside the UPM package.
- **Docs**: new [Upgrading to 12](Docs/Migration12.md). The READMEs, the Docs index and `PROJECT_SUMMARY.md`
  describe the current module set, the plain C# cores and time/random seams, and the builder-authored demos;
  `Samples.md` describes how every demo scene is made. New pages: Tools/Time/Clocks,
  Tools/Random/IntegerChanceTable, Tools/Dialogue/DialogueLinePool, GridSystem/GridPathfinder, Bonus CollectionSet,
  BoxProgress and CooldownSchedule. Module pages open with What it is / How to use and match the current API;
  `MODULE_STRUCTURE.md` is in English with the current assembly map; `DEPRECATED_OR_REMOVAL_CANDIDATES.md` lists
  every current `[Obsolete]` member; `UsefulComponents.md` is rebuilt from the real component menus; `Testing.md`
  gains "Performance and allocation tests"; Tools/Input and Tools/InteractableObject document the measured numbers.
  The Cookbook slot recipe no longer wires `SpinController.OnWin` to `Money.Add` while Auto Payout is on (that paid
  twice). The `neoxider-tools` skill is synced with 12.0.0 and its stale examples are fixed. The NeoxiderPages pages
  describe the PM behaviour; the Animations, Bonus, Tools/Physics and UI pages point to their new demo scenes;
  `Samples.md` lists every scene and the modules that still have no demo scene; `Testing.md` lists every benchmark
  suite and the sample-scene tests.
- **Tests**: the new test-only assembly `Neo.Tests.Performance` (`Tests/Performance`) provides a self-calibrating
  native GC.Alloc counter (`GcAllocCounter`, `GcAlloc`) and the single `NEO_PERF` line writer (`NeoPerf`,
  `NeoPerfUpdateWindow`); `Neo.Editor.Tests` and `Neo.Tests.Play` reference it. Zero-allocation guards count
  GC.Alloc calls instead of `System.GC.GetAllocatedBytesForCurrentThread()`, which reads 0 in the Unity 6 Editor, so
  the old guards could never fail; when the recorder is unavailable a guard reports Ignored, never passed.
  Benchmarks are explicit tests in the `Performance` category. New regression and allocation tests cover every
  module, `Neo.Demo.Editor.Tests` builds each demo scene twice (wiring present, second pass changes nothing), the
  cursor-ownership tests run against an in-memory cursor on batch hosts, and the editor validation host requires an
  explicit `-neoValidationExpectedPackageVersion`.
- **Sample and packaging tests**: `SampleScenePlaySmokeTests` (PlayMode) loads every scene of the visible samples,
  ticks 12 frames and fails on error, assert or exception logs, Filled Images without a sprite, a demo controller
  that reports `IsSceneBuilt == false` or logs an unbuilt-scene warning, and a builder-owned scene without its
  authored info card; `EveryRegisteredSceneBuilder_HasItsSceneSavedInTheVisibleSample` fails for a builder whose
  scene was never saved. `AudioDemoScenePlayTests` presses the authored AudioDemo buttons and removes
  `AudioDemoController` mid-crossfade (borrowed music keeps playing); it replaces the session case that expected
  runtime-built clips. `SampleSceneSaveKeyTests` (EditMode) scans every sample scene and prefab, prefab-instance
  overrides included, and fails when a persisting component (inventory, level profile, progression, quests,
  counter, selector, timer, wallet, shop, score, level manager, equipment, leaderboard, box, collection, rewards)
  uses its library default save key. `PackageSampleIsolationTests` fails when a package asset references a GUID
  that ships only in the samples, and `ParamsOverloadAllocationTests` keeps `params` `Mathf.Max` / `Min` calls out
  of the sources. Sample asset validation and the sample evidence tests check each visible sample and skip only
  hidden or not-imported ones (missing scripts from a hidden sample are skipped, references that resolve nowhere
  still fail); Character Movement Fundamentals scenes are skipped in Input System-only projects with that reason;
  the sample PlayMode isolation removes the demo-only `neo_save_demo.json`. `Neo.Demo.Editor.Tests` adds the
  AnimatorsDemo / UIComponentsDemo, Bonus Rewards, Physics 3D and missing-scene-creation builder tests, and
  `NeoxiderPagesChangedBehaviorTests` the PM, `BtnChangePage`, `PageSubscriber`, `ToggleAudio`, facade and
  `FakeLoad` regressions (they run while the Pages sample is visible).
  `NoCodeWrapperAdditionsTests` covers the new one-argument targets and the event initializers; new regressions pin
  `CooldownReward` / `TimeReward` availability events, `Box.ResetProgress`, the `ButtonScale` release scale and a
  `FakeLoad` load that survives deactivation. `RpgDemoHealthBarFeedTests` (PlayMode), `VampireSurvivorEnemyPrefabTests`
  and `ExampleGameBuildersTests` (`Neo.Demo.Editor.Tests`) cover the health-bar feed, the authored enemy death
  handlers and the example-game guides. The Neo inspector chrome repaint test now allows Unity's own text-draw cost
  (one GC.Alloc per IMGUI text in Unity 6.3) and fails on anything the chrome allocates itself.

## [11.0.1] - 2026-10-28

### Changed

- **License:** releases starting with 11.0.1 are distributed under the
  [PolyForm Small Business License 1.0.0](LICENSE.md): free for personal and non-commercial use and for small
  businesses (fewer than 100 people and less than 1,000,000 USD (2019) revenue in the prior tax year); larger
  companies need a commercial license (neoxider@gmail.com). Earlier releases keep the license they shipped with.
- The package README drops the GitHub Actions test badge: the workflows were removed, so the badge could only
  report a missing status. Run the suites locally as described in the Tests section.

## [11.0.0] - 2026-10-27

This major update introduces the reusable contracts and workflows below. Windows verification recorded on 26–27 October 2026
for Unity 6000.3.14f1 and 6000.6.0f1 is listed in [Verification 11](Verification11.md), including focused
CardGame sizing/gameplay acceptance and three manual OS-click rounds. Current P31 UPM checks imported
all three public samples and passed **20/20 imported Play** cases per SDK; P30 **28/28 Edit** is qualified by
unchanged graph identity. Scopes overlap; manual observations remain limited to their recorded
scenarios. Authored regressions and actual executed results are distinguished in the verification record.
See [migration from 10.x](Docs/Migration11.md) for deliberate behavior changes.

HtmlToUitk current acceptance: fresh CLI **2573 passed/30 NotExecuted/0 failed**, separate pure logic **291/291**,
and complete Unity assembly **370/370,0 skipped per SDK**, with real three external CLI runs/native import/Play UI and manual
Unity 6.3 OS interaction at1280×720. The30 debt cases remain nonexecuted. Portable net9 CLI uses
LatestMajor on tested.NET 10; no runtime9 is bundled. [Exact scope](https://github.com/NeoXider/HtmlToUitk/blob/main/Documentation~/HtmlToUitkVerification11.md).
Release delivery is choose-one Core/Complete plus `HtmlToUitk.Cli.v11.0.0.zip`, SHA256SUMS and
a release-input manifest; actual final archive export/import and publication are separate gates.

### Major contracts

- HtmlToUitk reserves filename/CSS/canonical-asset destinations, rejects conflicting aliases before commit,
  preserves newest projections across same-root reentry/failure cleanup, and supports default mixed-case
  plus explicit custom localization keys without losing literal text or original keys.
- Networked Money gains one server-enforced mutation authority policy, defaulting to
  ServerOnly. Cooperative/owned-wallet client requests require explicit policy selection;
  direct server/host and offline operations retain their paths.
- Shop purchase notifications run after profile persistence commits. An observer fault
  no longer refunds committed ownership; legacy inventory delivery remains best effort.
- Interaction shares complete raw physics hits for exact matching contexts, retaining
  per-object policies, masks, triggers and unique LOS. Callback/fresh-query boundaries are explicit.
- StateMachine isolates built-in duration anchors per machine and bounds nested lifecycle
  requests. Pause/cursor owners restore the captured baseline after the final release.
- Cards separates committed admission from animation cancellation, retires obsolete layout/game
  work and supports built-in batch hand ordering without add/remove churn.
- Canvas card prefabs are created under their UI spawn point, preserving authored scale through
  hand/board transfers; world-space spawning retains its contract. Visual stack work stops after
  reentrant deck changes. CardGame fixes pile bounds, tutorial click interception and portrait dealer
  placement; one real UI click starts exactly one round.
- Resource receipts capture their stored float delta before observers. Positive spending that
  rounds to no debit is rejected; ordinary float rounding remains part of the contract.
- RPG bounds pathological tick catch-up at 1024 logical ticks per entry/call, retaining
  fractional phase and dropping excess whole backlog. V2 snapshots honor saved effective Max;
  declared one-shot resource grants occur before settled OnBuffApplied notification and never replay on restore.
- Persistent leaderboard keys and explicit ambiguous-entry binding preserve legacy name/score
  saves. Invalid enabled encryption rejects construction without falling back to plaintext.
- Save and legacy leaderboard readers require their explicit root arrays while accepting empty arrays.
  Inventory snapshot `ToJson`/`FromJson` uses private zero-default wire fields to preserve nullable
  item instances and simple stacks without discarding legitimate id-zero/count-one items; existing
  JSON keys/version 1 and public domain defaults remain compatible.
- Numeric text animations settle the requested float endpoint and ignore callbacks from retired tweens,
  preserving newer numeric and literal replacements alongside exact completed integer formatting.
- Image fill animations retire their owned tween when its target is destroyed or replaced. Sliced-width
  updates preserve newer values written by UI callbacks and capture each replacement's authored anchors.
- The Lobby scene generator creates persistent lobby/gameplay assets before opening them, including
  additive authoring beside unsaved work. Choose a fresh destination; existing assets are refused.
- Legacy leaderboard JSON recognizes Mono's mapped escaped member names once, retaining exact
  duplicate-key rejection and preserving literal text and custom member names.
- Inventory gains owner-bound `InventorySelection`, exact-instance consumption/transfer and an optional
  `InventoryUseAction` Inspector adapter. Same-type unique items retain their UID and payload; stale
  selections cannot consume a replacement. Hand use no longer activates a visual selected by a callback.
- A playable Inventory sample covers temporary catch, nine-slot hotbar, podium placement/recall and
  consumable healing. The Abilities NoCode sample explains HP changes, cooldown rejection and level
  scaling, offers Reset, and lets users enable automatic casting explicitly.
- Abilities and NoCode progress bars include their required sprites, and sample guides reflow within
  the viewport. Audio music controls keep their toggle synchronized after playback and Stop.

### Audit integration

- Development dependencies move to Mirror 96.11.3, UniTask 2.5.11 and compatible stable Unity package
  updates; MCP for Unity is pinned to 10.3.0 with a matching server. Editor-bundled package versions
  remain tied to their Unity SDK. See [dependency and upgrade guidance](Docs/Dependencies.md).
- The loading-screen prefab uses core ConstantRotator instead of two Pro-only components. Legacy
  sample uGUI Text references and missing UI artwork/font references are migrated to available assets;
  sample GUIDs and component identities are retained.
- Modular runtime/editor/sample ownership, reentrant operation receipts, complete query buffers,
  asynchronous cancellation and world/inventory quantity conservation receive focused corrections.
- Regression sources cover public outcomes, resource ownership and real network/physics boundaries.
  Recorded compiler/runtime/remote evidence and manual limits are linked above; future changes require
  an agreed source freeze and affected verification before their acceptance.
- Canonical module documentation, runtime C# and scene/NoCode routes, migration guidance and
  release/developer/imported sample instructions are updated. Package minimum Unity remains 6000.0.
- Duplicate or malformed package metadata is repaired with retained existing identities. The repository's
  six Demigiant DLL importer records were natively migrated to schema 3 in Unity 6.3, preserving DLL bytes,
  GUIDs and observed native settings. Recorded 6.3/6.6 import and Pro-absent UPM scopes are linked above.

### Existing development notes


### Added

- **`AM` sound channels — separate SFX and UI volumes.** Every sound entry has a `Channel`
  (`SoundChannel.Sfx`, the default, or `SoundChannel.Ui`), each with its own volume (0..1), mute and optional
  `AudioMixerGroup`: `GetChannelVolume` / `SetChannelVolume`, `IsChannelMuted` / `SetChannelMute`,
  `GetChannelMixerGroup` / `SetChannelMixerGroup`, `GetChannelSource`, plus `UiVolume` / `SetUiVolume(float)`.
  `Sfx` **is** the existing Efx `AudioSource` (`EfxVolume`, `SetEfxVolume`, `AMSettings` keep driving it); `Ui`
  is a 2D source created on first use (never saved) with `_uiVolume` (default 1) and `_uiMixerGroup` (empty = the
  Efx group). Each channel has its own pitch-voice rotation (a ringing Sfx tail is never re-routed to the UI
  level) and voices are re-levelled live on a channel change; `SoundOptions.Channel(...)` routes a single
  play. In Play Mode and builds AM destroys the helper sources it created in `OnDestroy` - UI source, pitch,
  positional and crossfade voices, and the Ambience source when AM created it (an authored one is kept); in
  edit mode `OnDestroy` is not sent, so helpers made by an inspector button stay as never-saved `DontSave`
  children until the scene reloads. Also released as 10.13.11 on `release/10.13.x`.
- **`AM.TryPlay(id[, volume | options])`** returns whether a clip started; an empty or unknown id or an entry
  without a usable clip return `false` without a warning.
- **Placeholder entries** (`AudioEntry.IsPlaceholder`): while empty, every play path (`Play` by id / index,
  `PlayAt`, `TryPlay`, `PlayMusicPool`) is a silent no-op (a placeholder music pool keeps the current music) and
  the AM inspector stops flagging the entry. With clips it plays normally.
- **`AM.PlayAt(id, position[, volume | options])` / `PlayAt(clip, position, volume, spatialBlend, channel)`** —
  positional one-shots on a pool of `_positionalVoices` (default 8) `AudioSource`s (free voice first, else the
  next in rotation is cut), with the same clip pick, pitch randomisation and channel x entry x trim volume as
  `Play` (capped at 1). Spatial blend from `SoundEntry.SpatialBlend` (default 1) or `SoundOptions.Spatial(...)`;
  rolloff, distances and spread from the Efx source on every channel, doppler 0. No allocation per call once
  warm. Migration: none — old entries load as `Sfx`, spatial blend 1, not a placeholder; the UI source and the
  voices are created on first use (also in edit mode, from an inspector button) and never saved.
  Tests: `AmChannelEditModeTests`, `AmEditModeTests` (owned vs authored Ambience on `OnDestroy`),
  `AmTeardownPlayModeTests` (PlayMode; `Neo.Tests.Play` now references `Neo.Audio`). Docs: `Docs/Audio/AM.md`.
- **`AudioFocusMute` — silence while the game has no focus.** A minimized window, another browser tab or another
  application in front sets `AudioListener.volume` to zero; focus return restores the volume it found. Nothing is
  paused (no burst of queued sounds on return, a game's own `AudioListener.pause` is left alone). One call at
  startup — `AudioFocusMute.Install()` (or `Install(muteInEditor)`) — creates a persistent instance; the state is
  applied on enable too, so a game opened in a background tab starts silent. Docs: `Docs/Audio/AudioFocusMute.md`.
  Tests: `AudioFocusMuteEditModeTests`. Also released as 10.13.10 on `release/10.13.x`.

### Changed

- **`AM`: ringing voices follow the Sfx channel.** `SetEfxVolume`, `SetChannelVolume`, `SetChannelMute` and
  `ApplyStartVolumes` re-level pitched voices that are still ringing, instead of only the next shot.
- **`AMSettings` drives the Sfx channel through `AM`.** `SetEfxVolume`, `SetEfx` / `ToggleEfx` and
  `ToggleAllAudio` call `AM.SetEfxVolume` / `AM.SetChannelMute` instead of writing the Efx source, so ringing
  voices follow them; `ToggleAllAudio` also mutes and restores `AM`'s UI channel. Also in 10.13.11.

### Fixed

- **CMF: a frozen walker no longer plays footsteps.** Disabling `AdvancedWalkerController` (a camera focus or a
  UI lock freezing the player) left `GetVelocity()` returning the velocity of the last physics step, and
  `AudioControl` kept advancing its footstep timer on it, so a player standing still heard steps. The walker
  now clears its saved velocity in `OnDisable` (`protected virtual`; momentum is kept, so a walker disabled
  mid-jump resumes its arc), and `AudioControl` skips footsteps while its controller is disabled. Regression
  tests: `CharacterControllerInputTests.FrozenWalker_ReportsNoVelocity`,
  `Footsteps_DoNotAccumulate_WhileControllerIsDisabled`. Also released as 10.13.9 on `release/10.13.x`.
- **Imported Demo scenes compile after the module split.** `Neo.Rpg.Demo` now references
  `Neo.Rpg.Combat` for the legacy Condition demo's `Health` and `Neo.Core.Level.Bridge` for the Level demo.
  The Progression demo's reset button calls the current `LevelComponent.ResetProgress()` API.
- **The repository's bundled UniTask tracker compiles on Unity 6.5+.** Its int-based IMGUI tree controls
  use generic aliases on newer editors; older editors keep the existing API.
- **`Neo.Haptics` keeps its `tsyk5.MobileHapticFeedback` reference (no change against 10.16.2).** An unreleased
  change had removed it, which would have broken every build with `com.tsyk5.mobilehapticfeedback` installed:
  `NEO_MOBILE_HAPTICS` was defined but the assembly could not see the tsyk5 types (`CS0246`). Verified on Unity
  6000.3.14f1 and 6000.6.0f1 with a fake `tsyk5.MobileHapticFeedback` package that a name reference to an absent
  assembly is dropped without any error (the assembly still compiles without the package) and resolves once the
  package is installed, exactly as `Neo.Network` does with Mirror. The `CS0234` that removal targeted does not
  reproduce with the reference in place.
- **`Haptics` resets its static state on `SubsystemRegistration`** (`Enabled`, `EnabledProvider`,
  `MinRepeatInterval`, `Played`, the repeat throttle), so Enter Play Mode with domain reload disabled no longer
  drops pulses through a stale throttle timestamp.
- **Ambience follows the music slider.** `AMSettings.SetMusicVolume` (the `AudioControl` Music and Master slider
  path) now sets `AM`'s music channel instead of writing the music source directly, so the ambience moves with
  it and the next track keeps the slider level; `GetMusicVolumeNormalized` reports the channel.
  `AM.ApplyStartVolumes` re-levels an ambience that was already playing. Without a mixer, `music.volume` after
  the slider is now slider x the current music entry trim, as `AM` defines it.
- **`AM`'s runtime `Ambience` source is `HideFlags.DontSave`**, like `PitchVoice_*` and `MusicCrossfade_*`, so
  an edit-time call no longer leaves a saved child.
- **Haptics edit-mode tests removed** (`Tests/Edit/Haptics/`, owner decision); the dangling
  `Neo.Haptics` / `UniTask.dll` references the editor had already pruned from `Neo.Editor.Tests`
  are kept as-is.
- **`ImageFillAmountAnimator` shows the requested value after a mid-tween disable.** Closing a page while
  the bar animated only killed the tween, so the page reopened with a half-way bar. `OnDisable` now applies
  the last requested value directly (never through DOTween `Complete()`, whose setter may hold a destroyed
  `Image`); `OnDestroy` still only stops the tween.
- **`ImageFillAmountAnimator` `SlicedWidth` warns about point anchors.** With `anchorMin.x == anchorMax.x`
  (the default of *GameObject > UI > Image*) the width cannot change and only value 0 did anything, silently.
  When `NeoDiagnostics` warnings are enabled (they are off by default) the component logs one warning per
  instance naming the GameObject, and turning them on later still reports it; the docs say the fill must be
  horizontally stretch-anchored.
- **`ImageFillAmountAnimator` `SlicedWidth` starts from the authored rect.** The first animated `SetValue`
  grew from 0 (the bar snapped empty first) and `DisplayedValue` reported 0 while the bar was full; the
  authored rect now counts as value 1.
- **`ImageFillAmountAnimator.Mode` restores the rect when leaving `SlicedWidth`.** Switching to
  `FillAmount` at runtime left the right anchor collapsed and the image hidden; it now stops the tween and
  restores the authored anchor and visibility. The `Image` auto-lookup no longer uses `??=` on a Unity
  object (which skips the destroyed-object check).

### Changed

- **UI Mesh Rig: one place for the `PanelRenderer` version switch.** The `UNITY_6000_5_OR_NEWER`
  checks for `PanelRenderer` that were spread over the UI Toolkit host, the creation menu and the editor test now
  live only in the internal `UIMeshRigPanelCompat` (next to `UIMeshRigUIToolkitHost`); the menu keeps one
  separate `#if` for the unrelated `CreateAssetWithTextContent` API. The 6000.5 threshold is confirmed by the Unity
  docs: the 6000.5 Scripting API has `PanelRenderer`, 6000.4 does not, and "What's new in Unity 6.5" lists
  the new Panel Renderer component. No behaviour change.

### Tests

- `AmEditModeTests`: ambience stop, null and same-clip no-op, `DontSave`, the `LateUpdate` mute copy,
  `ApplyStartVolumes` re-level, and the `AMSettings` music and master sliders driving the ambience.
- `ImageFillAmountAnimatorTests`: target applied on disable (FillAmount and SlicedWidth), point-anchor
  warning logged once (including diagnostics enabled after anchor capture), `Mode` switch restores the rect,
  authored rect counts as full (also inverted).
- `NeoxiderPagesChangedBehaviorTests`: `PM.OnValidate` defers the editor preview to
  `EditorApplication.delayCall` without toggling pages synchronously, does not stack refreshes, the
  deferred refresh shows the preview page, a destroyed `PM` makes it a no-op, and in Play Mode nothing is
  scheduled or toggled.

## [10.19.0] - 2026-10-14

### Added

- **HTML/CSS → UI Toolkit**: встроенный runtime/editor для native controls, typed C# actions, навигации/modal/tabs, media width/height/aspect и keyframes/reduced motion. CLI создаёт UXML/USS/localization specs и проверяемые gradient PNG с manifest.
- Static gradient backend поддерживает linear/radial/conic/repeating/alpha в явно ограниченном контракте; неподдерживаемая dynamic geometry и CSS/JS получают nonzero diagnostic. Full browser parity не обещается.
- [Runtime/editor и C# API](https://github.com/NeoXider/HtmlToUitk/blob/main/Documentation~/HtmlToUitk.md), [канонический внешний CLI](https://github.com/NeoXider/HtmlToUitk/blob/main/Tools~/README.md). CLI/.NET DLL не входят в package/player; для authoring нужен полный checkout либо отдельно собранный CLI с настроенным внешним путём. C# preview/compare пока unsupported nonzero; проверенные Python oracle альтернативы описаны в CLI документации. Проверенный native срез: Unity 6000.3.14f1 EditMode — 239/239, Unity 6000.6.0f1 EditMode — 239/239 и PlayMode — 7/7; failures/skips — 0, exit0. [Verification](https://github.com/NeoXider/HtmlToUitk/blob/main/dev-docs~/Plans/HtmlToUitk-Verification-20261014.md), [TODO и открытые гейты](https://github.com/NeoXider/HtmlToUitk/blob/main/dev-docs~/Plans/HtmlToUitk-TODO.md). Player/mobile и полный browser parity не заявляются.

## [10.18.1] - 2026-10-08

### Fixed

- **`NetworkContextActionRelay` handler lifecycle.** Mirror handlers register only while at least
  one relay is enabled (`Awake` / `OnEnable` track, `OnDisable` / `OnDestroy` untrack with stale-entry
  pruning); the last disable unregisters both handlers and clears per-connection ingress budgets.
  Restart-safe (`OnStopServer` / session reset clear budgets; disconnect forgets the peer).
- **Server ingress hardening.** `OnServerMessage` now requires a live, authenticated, Ready connection
  that owns its player object (checked against the live `NetworkServer.connections` reference) before
  any relay/context lookup, then applies a small per-connection token budget (5 burst / 5 per second).
  Resolution is spawned-dict only (server side vs client side split, no scene scan); the relay resolves
  by exact `NetworkBehaviours` component index with no fallback to a different action. Relay
  `AuthorityMode` is preserved, and a context owned by another connection is rejected while unowned
  (shared scene) contexts stay usable. Disabled-state logs are gated, so they build no strings.
- **`NeoNetworkManager` event robustness.** Throwing `UnityEvent` listeners are caught and logged
  (`NetworkDiagnostics.LogException`) so handshake continuation and disconnect cleanup are not broken;
  null host sender is handled in the connection helpers.

### Added

- **Optional `TelepathyQueueLimit` + `ApplyTransportQueueLimits()` on `NeoNetworkManager` (Mirror only).**
  Caps the four root Telepathy transport queues (`serverSendQueueLimitPerConnection`,
  `serverReceiveQueueLimitPerConnection`, `clientSendQueueLimit`, `clientReceiveQueueLimit`) via
  public-field reflection on the transport type or its Telepathy subclass, applied from `Awake` /
  `Start` / `StartAs...` before the session starts. `0` keeps authored defaults (recommended `256`);
  positive authored limits are never raised, nonpositive authored limits become the cap. Unsupported
  transports and root `MultiplexTransport` are a no-op. Starting via `StartHost()` / `StartServer()` /
  `StartClient()` on a Mirror base-typed reference bypasses the hidden wrappers, so after swapping the
  transport call `ApplyTransportQueueLimits()` explicitly before startup. No packet-size or timeout changes.

### Limitations

- **Unchanged by this release:** Mirror malformed-batch parsing, retained-batch queue growth, and
  raw-socket admission behavior. The new queue cap is a partial memory-capacity mitigation only;
  deadline / pending-cap behavior remains a proposal, not shipped code.

### Verification

- FINAL compile: 37 runtime + 14 NUnit files, 0 errors / 11 pre-existing warnings. Verbatim bucket
  checks 6/6, clock/table stubs, and existing pure/docs suites: 163/163 passed. Execution of 31 new
  Unity cases plus the Weaver step is PENDING (editor unavailable). No full-suite green claim.

## [10.18.0] - 2026-10-07

### Added

- **Realtime IO toolkit (`Neo.Network.Realtime`, `Scripts/Network/Realtime/`).** The game-agnostic parts every
  server-authoritative realtime game ends up hand-writing, extracted from a shipped 10-player .io game and tested:
  - `SnapshotTimeline` / `SnapshotTimelineSettings`: an adaptive jitter-buffer render clock in the authority's time
    (holds 1.5 snapshot intervals plus measured jitter behind the newest frame, steered +-10 % instead of stepped,
    snaps after a stall, measures the authority's clock rate so a hit-stop does not push it ahead of the frames).
  - `SnapshotBuffer<T>` + `NetInterpolation`: a ring of timestamped frames with allocation-free bracket search
    (`SnapshotSampleKind.Between / BeforeOldest / AfterNewest`), pooled-payload hook, angle blends.
  - `LocalPredictionModel`: client-side prediction with input acknowledgement (ack-tick replay, decaying render
    offset, snap threshold, optional obstacle constraint).
  - `NetQuantization`: saturating fixed-point packers (positions 1/32 unit, scale, speed, timers, fractions, signed
    axes, angles, hit points rounded up) plus explicit-density `PackShort` / `PackUShort`.
  - `NetFrameFraming`: version byte + body length + bounded counts, so a corrupt or old frame is a `FormatException`
    (Mirror disconnect), not a silent desync.
  - Datagram fragmentation for KCP/UDP: `NetFragmentation`, `NetFragmentAssembler` (pure, bounded, latest-wins),
    `NetFrameSender` / `NetFrameReceiver` / `NetFragmentMessage` (streams share one message type).
  - `NetClientHandlers` / `NetServerHandlers`: message handlers that survive every shutdown and re-register in
    `OnStartClient` / `OnStartServer`, before any message can arrive. `NetReadyBroadcast`: send only to
    connections that are ready **and** own a player. `NetEventChannel<T>`: a typed reliable event, authority to
    remote clients, never back to the host.
- **`NeoNetworkBootstrap` + the pure `NeoStartupIntent` parser** (`Scripts/Network/Bootstrap/`). One component for
  the command line of every networked game: `-host`, `-server`, `-client`, `-address`, `-port`, `-maxplayers`,
  `-name` (plus your own switches), a dedicated server that can only be started from the command line, rendering /
  audio / UI suppression on a dedicated server, headless runtime settings (frame cap, vSync off, run in
  background for any networked peer), a periodic `[Server] status:` line, and on WebGL a `SimpleWebTransport`
  override plus `?client&address=...&port=...` page-URL start-up (client only).
- **`NeoNetworkManager`**: `NeoHandshakeMode` (Auto / Always / Manual) guarantees the client handshake (`Ready`, then
  `AddPlayer`, once per connection, also after scene changes); server events `OnServerClientConnectedEvent`,
  `OnServerClientReadyEvent`, `OnServerPlayerReadyEvent`, `OnServerClientDisconnectedEvent` (UnityEvent + C# events,
  carrying the connection) and `OnLocalPlayerSpawnedEvent`; `IsConnectionSpawned(conn)` (also
  `NeoNetworkState.IsConnectionSpawned` / `IsLocalHostConnection`); `StartAsClient(address)` and
  `StartAsClient(address, port)`; `ApplyPort`; static `ClientSessionStarted` / `ServerSessionStarted`.
- **Telemetry** (`Scripts/Network/Telemetry/`): `NeoNetworkTelemetry` (RTT and jitter, worst client RTT on a server,
  bytes and messages per second in and out, busiest message kinds, optional on-screen overlay), the pure
  `NetTrafficMeter`, and static `NetTelemetry`.
- `NeoMirrorSceneReactivator.ActivateNetworkedSceneObjects(...)` and `NetworkDiagnostics.LogException`.
- Tests: `SnapshotTimelineTests`, `SnapshotBufferTests`, `NetQuantizationTests`, `LocalPredictionModelTests`,
  `NetFragmentationTests`, `NetTrafficMeterTests`, `NeoStartupCommandLineTests`, `NetRealtimeMirrorTests`,
  `NetworkSessionHelpersTests` (EditMode), `NeoNetworkManagerSessionTests` and `NeoNetworkBootstrapTests`
  (PlayMode host loopback: handshake, events, restart, handlers across a restart, fragment delivery).

### Changed

- **`NeoNetworkManager` wakes scene `NetworkIdentity` objects on every peer when a session starts.** Mirror's scene
  post-process disables them and only the server re-enables them, so on a client a scene object that registers a
  message handler in `Awake` had no handler when the first message arrived and Mirror dropped the client. The
  disabled scene player template stays off. Switch: **Activate Scene Objects On Start** (default on).
- `NeoNetworkManager.OnServerAddPlayer` no longer throws from inside Mirror's message loop when there is neither a
  Player Prefab nor a scene template; it logs a gated warning (a game overriding the method is unaffected).
- `NeoNetworkManager.ConfigureHeadlessFrameRate` respects the frame cap applied by `NeoNetworkBootstrap`.

### Fixed

- **`NetworkSingleton<T>.I` no longer caches a failed lookup for the whole play session.** A miss is trusted for the
  rest of the current frame and until the next scene load or unload, so a singleton that appears later (additive
  scene, object enabled after the first caller) is found. New `NetworkSingleton<T>.ForgetFailedSearch()`.

### Documentation

- New `Docs/Network/Realtime_IO_Guide.md` (handshake -> snapshots -> interpolation -> prediction -> events ->
  bootstrap), `NeoNetworkBootstrap.md`, `NeoNetworkTelemetry.md` and `Docs/Network/Realtime/` (one page per piece).
- **Stale docs corrected.** `Multiplayer_Guide.md` and `NeoNetworkManager.md` documented `StartHost()` /
  `StartClient()` / `StopHost()` and a non-existent `NeoNetworkManager.Singleton`; they now describe the real
  `StartAsHost()` / `StartAsClient()` / `StartAsServer()` / `StopNetwork()` with a start/stop cheat sheet.
  `NetworkSingleton.md` listed `Singleton`, `HasServerAuthority()` and `IsServer()`; it now lists `I`, `Instance`,
  `HasInstance`, `IsInitialized`, `TryGetInstance`, `HasServerAuthority`.
- Skill `neoxider-tools` -> 10.18.0: `references/network.md` covers the new types.

## [10.17.1] - 2026-09-30

### Fixed

- **A press in a Neo inspector no longer animates the button above, and text selection is visible again.**
  The header mascot opened `GUI.BeginGroup` only while repainting. `BeginGroup` takes an IMGUI control id, so every
  control below the header got a different id on Repaint than on Layout and MouseDown: the pressed state was
  drawn one control up, and text fields lost their selection highlight because the id being edited no longer
  matched the id being drawn. The group now opens on every event and only the drawing stays Repaint-only.
  `AudioEntryDrawer` had the same bug in a smaller form (`EditorGUI.LabelField` for the "id (optional)"
  placeholder inside a Repaint-only branch) and now uses `GUI.Label`, which takes no id.
- `InspectorControlIdStabilityTests`: renders the header in a real IMGUI window and compares the control ids
  of Layout and Repaint (red before the fix: 52 vs 103), plus a source scan that rejects id-allocating calls
  in `Event.current.type == EventType.Repaint` branches across the editor code.

## [10.17.0] - 2026-09-30

### Added

- **Inspector animation can be switched off.** *Neoxider > Visual Settings > Animation* has a master switch
  (*Animate inspector*), *Animate in Play Mode* and *Max FPS* (5-60, default 30). With the switch off the
  header mascot, the spectrum half-frame and the update-pill pulse are drawn as one still frame and nothing
  schedules repaints, so a slow machine gets its editor time back. New `CustomEditorSettings.EnableAnimations`,
  `AnimateInPlayMode`, `AnimationFps` and their `Set*` methods (stored in `NeoInspectorSettings`).
- `CustomEditorBase.EnsureLiveRepaint()` for content that must stay current whatever the animation setting
  says (update-check progress, the live result in `NeoConditionEditor`). `EnsureRepaint()` is now strictly
  decorative and does nothing while animation is off.
- `NeoInspectorAnimationTests`: animation policy, fps clamp, repaint leases and ticker lifetime, and the
  reflection caches.

### Changed

- **Neo inspectors do not animate in Play Mode by default.** The game already competes with the editor for
  time; *Animate in Play Mode* (off by default) turns the animation back on. The header still polls its status
  at 2 Hz, so a fresh console error or a missing reference reaches the badge without a repaint loop.
- **One shared, throttled repaint ticker.** Every open Neo editor used to add its own `EditorApplication.update`
  callback that called `Repaint()` on every editor tick until the editor closed: N components meant N
  unthrottled loops, and switching an animation off left the loop running. Editors now ask for short repaint
  leases that every draw renews; a hidden or collapsed inspector stops drawing, its lease lapses and it stops
  repainting. Animation is capped at *Max FPS*, live content at 10 Hz.
- **Inspector reflection and asset lookups are cached.** Every IMGUI event (Layout, Repaint, mouse moves) used to
  reflect over all methods of the component and read their attributes twice, and to resolve the doc page through
  the `[NeoDoc]` attribute, a `TextAsset` load and an `AssetDatabase.FindAssets` query. Both are now resolved
  once per type; `[Header]` titles, field-by-property-path lookups, the namespace check and the rich-text doc
  preview are cached too (doc caches drop on `EditorApplication.projectChanged`). Measured on `SpinController`
  (204 methods, 6 `[Button]`): Actions discovery 0.50 ms -> 0.00005 ms and doc-path resolution 0.13 ms ->
  0.0001 ms per event.
- The always-drawn chrome (banner, badge, update strip, action buttons) reuses cached `GUIStyle`s and the
  refresh icon (`NeoInspectorStyles`) instead of allocating about a dozen per repaint, and the health report is
  read once per frame instead of twice.

### Fixed

- **Package version files are in sync again.** The 10.16.2 release left the READMEs, `PROJECT_SUMMARY.md`, the Docs
  entry point, `PackageCompatibility.md`, the sample paths in `AGENTS.md` / `Docs/Samples.md` and the skill on
  older versions, which kept `PackageVersionParityTests` red.

## [10.16.2] - 2026-09-26

### Documentation

- **Skill `neoxider-tools` → 10.16.1**, with a new `references/mobile-casual.md`: how the package
  assembles into a shipped mobile casual game — Loading scene with real preload and the age gate, `PM`
  pages and Android Back, the NeoxiderPages sample-version gotcha, `AM` music + ambience + background
  pause, `Neo.Haptics`, `SaveProvider` flags, reward flights through `AnimationFly` (queued on popups,
  counter ticks per arrival), which motion pieces stay game-level, economy config. `Haptics`,
  `AnimationFly` and the ambience layer added to the module table and the "reach for the package
  first" list; a mobile reward/audio/haptics eval.
- **Cookbook:** reward earned on a popup flies on the next screen; mobile audio (music, ambience,
  background pause, haptics); Android Back over `PM` pages.
- **`AM`:** pausing in the background and during ads with one owner of `AudioListener.pause`.
- **NeoxiderPages:** where an imported copy lives, why bumping the package does not update it, and how
  to tell which fixes a project has; Android Back over pages.

### Fixed

- **UI Mesh Rig compiles on Unity 6000.4.** `PanelRenderer` exists only since Unity 6.5 (6000.5), but the
  host, creation menu and tests guarded it with `#if UNITY_6000_4_OR_NEWER`, so 6000.4 defined the branch
  without providing the type (`CS0246`). The guard is now `#if UNITY_6000_5_OR_NEWER`; on 6000.4 and older
  the host falls back to `UIDocument` as intended.
- **`BillboardUniversal` no longer rotates in `OnValidate`.** Unity calls `OnValidate` on prefab assets
  as they load, so every prefab holding the component got a `Camera.main`-dependent rotation written
  into its root (and into nested-instance overrides) the next time the project saved, and a scene
  instance got `Camera.main` baked into `targetCamera`. The component now rotates only in `LateUpdate`
  (Play Mode); an explicit edit-mode preview is the `Face Camera Now` context-menu item, recorded in Undo.
  Covered by `BillboardUniversalEditModeTests`. (also shipped as 10.13.8 on release/10.13.x)

## [10.16.1] - 2026-09-23

### Fixed

- **`PM` editor page preview no longer runs in Play Mode.** `OnValidate` also fires when a scene loads in
  the Editor, and it switched pages there, behind the game's back, logging a "SendMessage cannot be
  called during Awake, CheckConsistency, or OnValidate" warning for every UI element on those pages. The
  preview now runs only in Edit Mode and is applied on the next editor tick, so it stays silent there too.
- `PMEditor` uses `FindAnyObjectByType` (the ordered `FindFirstObjectByType` is obsolete in Unity 6.x).
- The NeoxiderPages behaviour tests resolve types through `CompilationPipeline` instead of
  `AppDomain.GetAssemblies()` (UAC0005).

## [10.16.0] - 2026-09-23

### Added

- **`AM` ambience layer.** `PlayAmbience(clip[, fade])`, `StopAmbience([fade])` and `AmbienceVolume` loop
  a quiet bed (night lake, rain) on its own `AudioSource` under the music, so it keeps playing while music
  pools crossfade. Its level is the music channel volume times `AmbienceVolume`; it uses the music mixer
  group and follows the music mute (including `AMSettings`). Two new edit-mode tests.

## [10.15.0] - 2026-09-23

### Added

- **New `Neo.Haptics` module.** `Haptics.Play(HapticType)` with nine named feels (`Selection`, `Light`,
  `Medium`, `Heavy`, `Soft`, `Rigid`, `Success`, `Warning`, `Error`), custom `Play(intensity,
  sharpness, seconds)` and `PlayPattern`. `Enabled` / `EnabledProvider` bind it to a Vibration setting,
  `MinRepeatInterval` drops same-type pulses fired in one burst so a fast drag does not become one long
  buzz, and `Played` reports every pulse on every platform so tests can assert on haptics in the editor.
  Uses `com.tsyk5.mobilehapticfeedback` (Core Haptics / VibrationEffect) when installed, detected by a
  version define; without it Android falls back to `Handheld.Vibrate` for the strong feels only.
  Four edit-mode tests.

## [10.14.3] - 2026-09-23

### Added

- **`ImageFillAmountAnimator` can drive rounded progress bars.** New `Mode` = `SlicedWidth` stretches
  a 9-sliced fill between its authored left edge and its full-progress right edge instead of cutting
  a Filled image, so the bar keeps its rounded caps at every value. `_minVisibleWidth` keeps the two
  caps from overlapping at tiny values; value 0 hides the fill. `FillAmount` stays the default, so
  existing scenes are unchanged. Covered by three new edit-mode tests.

## [10.14.2] - 2026-09-19

### Fixed

- `ImageFillAmountAnimator` no longer leaves an orphan tween behind: setting a value on an inactive
  object applies it immediately, and the tween is killed on disable/destroy. Optional unscaled-time
  tween for progress shown over a pause.

## [10.14.1] - 2026-09-04

### Fixed

- **The package did not compile in Unity 6.5+ projects: `SpinController` still called `Object.GetInstanceID()`.**
  Unity 6.5 turned `GetInstanceID` into an obsolete-as-error in favour of `GetEntityId`, and the two throttle
  keys added in 10.14.0 (`SpinController.cs:799` and `:917`) broke the build of any consuming project on that
  version with `CS0619`. The earlier compatibility pass had guarded each call site with its own
  `#if UNITY_6000_5_OR_NEWER`, so new code simply did not know the rule existed - which is exactly how this
  regression got in. The switch now lives in one place, `NeoDiagnostics.StableId(Object)`, and every runtime
  call site goes through it; the per-site `#if` blocks in `ParallaxLayer`, `MouseEffect`, `MouseInputManager`
  and `NetworkContextActionRelay` are gone.
- **The Unity 6.5 branch of the Survivor sample would not have compiled either.** `SurvivorGame` assigned
  `int id = upgrade.GetEntityId();` and looked that value up in a `Dictionary<int, int>`, but `GetEntityId`
  returns `EntityId` and its cast to `int` is itself obsolete on 6.5. The upgrade asset is now the dictionary
  key directly, so the sample needs no version switch at all.

### Added

- `NeoDiagnostics.StableId(Object)` - stable per-object identity for throttle keys and diagnostic text,
  valid on both Unity 6.0-6.4 and 6.5+. Returns `"null"` for a null target instead of throwing, because it
  is called precisely where something is missing.

## [10.14.0] - 2026-09-02

Durability release. Every change below closes a defect that silently cost the player data, money, or
progress, and each one ships with edit-mode coverage.

### Fixed

- **`FileSaveProvider` wrote the save file in place, so an interrupted write destroyed the profile.**
  A single `File.WriteAllText` straight onto the target meant that a process kill, a flat battery, or a
  full disk left a truncated JSON document; the next launch failed to parse it, silently reset the
  dictionary in memory, and the first flush afterwards overwrote whatever was left. Saving now stages the
  payload in `<save>.tmp` and commits it with `File.Replace`, which swaps the file and rotates the
  previous one into `<save>.bak` in a single operation. Loading falls back to that backup when the main
  file is missing, empty, or unparsable, and reports the recovery through `Debug.LogError` regardless of
  `SaveProvider.DebugLoggingEnabled` - losing a save is never an acceptable silent default. Filesystems
  without `File.Replace` (some Android and WebGL backends) fall back to copy-then-move, which still keeps
  a backup.
- **`SaveManager` only persisted on `OnApplicationQuit`, which Android and iOS routinely never call.**
  Backgrounding the app killed the session's progress even though the provider had already flushed its own
  staged values. `OnApplicationPause` and `OnApplicationFocus` now flush as well, and the new public
  `SaveManager.FlushNow()` exposes the same path for manual use before a risky operation.
- **`ReactiveProperty` silently dropped listeners when a callback wrote back into the same property.**
  The notification snapshot lived in one buffer per instance and was cleared entry by entry as it was
  consumed, so a re-entrant `Value` assignment reused that buffer and blanked the remaining slots; the
  outer notification then read `null` for every listener it had not reached yet and skipped them without a
  word. Nested notifications now get their own buffer, and the value delivered to each listener is
  captured when its own notification starts, so re-entrant writes no longer change what earlier listeners
  observe.
- **`Money.Add` accepted negative amounts and drove the balance below zero.** The deposit path clamped
  only the upper bound, so a negative "reward" bypassed every check in `TrySpend`/`CanSpend`. `Add` and
  `AddOverflow` now reject negative and non-finite amounts with an error, and `SetMoney` clamps to zero -
  a negative balance would otherwise let later spends succeed against a debt.
- **A shop purchase was not transactional: the money was taken before the item was granted.** Charging,
  recording ownership, saving the profile, and notifying listeners were four independent steps, so a grant
  handler that threw left the player paying for nothing. Both single purchases and bundles now snapshot the
  profile, and roll it back plus refund the price through `IMoneyAdd` when the grant fails; the attempt is
  reported through `OnPurchaseFailedId` instead of appearing to succeed.
- **`SpinController` charged for a spin but never paid out or refunded.** The bet was taken by
  `TryPayForSpin`, while the win only reached the `OnWin` UnityEvent - a machine wired without a listener
  ate every win - and `CancelActiveSpin` documented the refund as somebody else's job. Winnings are now
  credited through the new `IMoneyAdd` wallet when `Auto Payout` is on (default), and a cancelled spin
  hands the bet back.
- **`StateDurationPredicate` never worked.** Its `SetEnterTime` was not called anywhere in the package, so
  "wait N seconds in this state" measured from scene load and was already satisfied the moment the state
  was entered. `StateMachine.ChangeState` now anchors every duration predicate on the transitions leaving
  the new state, composite predicates (`And`/`Or`/`Not`) forward the signal to their children through the
  new `StatePredicate.OnOwnerStateEntered` hook, and an un-anchored predicate starts its clock on first
  evaluation instead of reporting a scene-load-sized elapsed time.
- **`LevelComponent.Reset()` collided with the Unity `Reset` message**, so choosing "Reset" on the
  component in the Inspector wiped the player's level and XP. Renamed to `ResetProgress()`.
- **Offline rewards trusted the device clock completely.** `TimeReward` and `CooldownReward` read
  `DateTime.UtcNow` directly, so moving the clock forward collected every pending reward and moving it
  back stalled cooldowns. They now read through the new tamper-resistant clock.
- **`Neo.NPC` referenced Mirror without using it.** Removed; the module no longer pulls the networking
  assembly into its compilation closure. The remaining Mirror references across the package are real -
  they were verified by compiling each assembly without them.

### Added

- **Weak subscriptions on reactive properties.** `AddWeakListener`, `SubscribeWeak(...)`, and the
  allocation-free `SubscribeWeak(target, static (t, v) => ...)` hold the subscriber weakly, so a forgotten
  `RemoveListener` can no longer keep it alive; the subscription drops itself once the target is collected.
  Weak dispatch goes through a cached open-instance delegate rather than reflection, and degrades to a
  strong subscription with a warning on AOT platforms that refuse the generic instantiation.
- **Automatic cleanup of dead subscriptions.** A listener whose target is a destroyed `UnityEngine.Object`
  is now unsubscribed and reported once through a throttled warning naming the method, instead of throwing
  `MissingReferenceException` on every notification. Listener exceptions are caught, logged, and skipped so
  one broken subscriber cannot stop the others or the gameplay code that set the value.
- `ReactiveProperty.Subscribe(...)` returning an `IDisposable` handle, an optional immediate first
  delivery, a `ListenerCount` property, and the `IReadOnlyReactiveProperty<T>` / `IReactiveProperty<T>`
  interfaces so a type can declare that it only observes a value.
- **`TrustedClock`** (`Neo.Tools`), a wall clock hardened against device clock tampering. A high-water mark
  that never decreases makes rewinding worthless and forces a forward jump to be waited out afterwards; a
  per-session comparison against a monotonic source caps a forward jump to the time that actually passed.
  `NeoTrustedTime` is the process-wide instance whose mark survives restarts through `SaveProvider`. Plain
  C# with an injectable `INeoClock`, so the behaviour is unit-testable without waiting in real time.
- `SpinController.TryPayout(int)` and the `Auto Payout` toggle.
- Edit-mode coverage for all of the above: `ReactivePropertyLifetimeTests`,
  `FileSaveProviderDurabilityTests`, `TrustedClockTests`, `MoneyDepositGuardTests`,
  `ShopPurchaseRollbackTests`, `StateDurationPredicateTests`.

### Migration

- `LevelComponent.Reset()` is now `LevelComponent.ResetProgress()`. This is the only source-breaking
  change; no compatibility alias is provided because keeping a method named `Reset` would reintroduce the
  Unity message collision that caused the data loss.
- `SpinController` credits winnings itself by default. If your scene already pays the player from an
  `OnWin` listener, turn off `Auto Payout` on the component to avoid paying twice.
- `Money.Add(-x)` used to reduce the balance. Code that relied on that must call `TrySpend(x)` instead.

## [10.13.7] - 2026-08-28

### Fixed

- **`NeoCameraInput` could rotate the camera by roughly 180 degrees after a WebGL pointer-lock gap.**
  Without pointer lock the browser derives mouse delta from cursor position; when the cursor reaches the
  window edge and is later recaptured, one frame can report a jump almost as wide as the window. The New
  Input System path now discards raw pointer deltas above the serialized `Max Pointer Delta Per Frame`
  magnitude before applying the existing scale and sensitivity. The default is `400` pixels per frame;
  `0` disables the filter. Accepted pointer motion remains unchanged, and gamepad-stick input bypasses the
  filter entirely.

### Added

- `CharacterControllerInputTests` regression coverage for rejecting a screen-sized pointer jump, passing
  a fast legitimate movement unchanged, and preserving the gamepad-stick rate when a pointer outlier is
  discarded.

## [10.13.6] - 2026-08-23

### Fixed

- **`InteractiveObject`: `includeTriggerCollidersInMouseRaycast` did nothing for 2D objects.** The 3D
  branch of `TryGetCurrentMouseTargetHit` turned the flag into a `QueryTriggerInteraction` for the
  query, but the 2D branch passed a hardcoded `true` into `BuildInteractionHits2D`, so trigger
  colliders were always kept no matter how the checkbox was set. A checkbox that silently does
  nothing is worse than no checkbox: it reads as a working switch in the Inspector. The 2D branch now
  passes the serialized value. Because `Physics2D.GetRayIntersectionNonAlloc` accepts no
  `QueryTriggerInteraction`, 2D reaches the same result by dropping triggers from the hits instead of
  from the query — the observable behaviour matches 3D.
- The `includeTriggerCollidersInMouseRaycast` tooltip now states that the flag covers 2D as well, and
  that in 2D the global `Physics2D.queriesHitTriggers` applies first, so the flag can only narrow that
  result and never widen it.

### Changed

- **Migration (2D only).** A 2D object whose collider is a trigger and whose
  `includeTriggerCollidersInMouseRaycast` is **off** stops being hoverable and clickable by the mouse —
  previously the flag was ignored and such objects worked by accident. The default is `true`, so a
  component left at defaults is unaffected. If a 2D object stops reacting to the cursor after this
  update, tick `includeTriggerCollidersInMouseRaycast` on it. 3D behaviour is unchanged.

### Added

- `InteractiveObjectMouseTrigger2DTests` — four EditMode cases pinning that the 2D mouse ray follows the
  flag in both positions, and that the 3D branch keeps its existing behaviour.

## [10.13.5] - 2026-08-23

### Fixed

- **`InteractiveObject`: the key and the mouse aimed by different rules.** The mouse ray asked the
  serialized settings ("do foreign triggers block? does this need line of sight?"); the keyboard look ray
  answered both questions with a hardcoded `true`. Two consequences, both invisible in the Inspector:
  a foreign **trigger** volume counted as a wall for the key but not for the cursor, and `checkObstacles`
  did not reach the look ray at all, so turning it off changed what the mouse could reach and left the key
  exactly as strict as before. The case that surfaced it: every door in the host game carries a
  2.4x3x2.4 trigger collider, and a pickup standing inside one was clickable while `E` did nothing at all —
  with no message, no ray colour and no setting that made a difference. Both branches (3D and 2D) now read
  `checkObstacles` and treat foreign triggers exactly as the mouse ray does.
- The `requireDirectLookRay` and `checkObstacles` tooltips described the old split. `Docs/Tools/InteractableObject/InteractiveObject.md`
  had already documented the *fixed* behaviour since the look-ray rewrite — the code was the side that
  drifted.

### Changed

- **Migration — read this if a scene relies on the old strictness.** With `checkObstacles` disabled, the
  key now reaches a target through solid geometry, just as the mouse always did; with `checkObstacles`
  enabled, foreign trigger volumes no longer block it. Nothing about `requireDirectLookRay` changed: it
  still decides whether an aim ray is required at all, and an object still has to be under the crosshair.
  A scene that wanted the old behaviour asks for it explicitly now — enable `checkObstacles`, and put
  genuine blockers on non-trigger colliders (or list them in `obstacleLayers` and enable
  `includeTriggerCollidersInObstacleCheck`, which is what the distance ray reads).

### Added

- `InteractiveObjectAimSymmetryTests` — an EditMode guard that runs the keyboard predicate and the mouse
  predicate over one configured component and asserts they agree: through a foreign trigger with obstacle
  checks on and off, through a solid wall with them off, and blocked by that same wall with them on. Each
  of the two former constants has a case that fails if it comes back.

## [10.13.4] - 2026-08-21

### Fixed

- **The 0..2 volume headroom introduced in 10.13.1 was unreachable.** The entry slider and the per-clip
  trims were authored up to `AudioEntry.MaxVolume`, but every playback path ran the multiplier through
  `Mathf.Clamp01` before using it - so an entry set to `2` played at exactly `1` and the slider moved
  with nothing to hear. Entry volume, clip trim and the per-call `SoundOptions` / `MusicOptions`
  overrides now keep their headroom. The *audible* music level is still capped at `1`, because
  `AudioSource.volume` genuinely is `0..1`; the headroom pays off against a channel the player has
  turned down.
- **`MusicControl`'s volume override was capped at 1** for the same reason - a no-code music trigger
  could only ever make a pool quieter. Its slider now spans `-1..2` like the entry it overrides.
- **`PlayAudio` / `PlayAudioBtn` threw away the entry's volume.** Their `_volume` field *replaces* the
  entry's own multiplier, and it defaulted to `1` - so pointing a fresh component at a sound entry
  authored at `0.5` played it at full, with the entry's slider apparently doing nothing. The default is
  now `-1`, meaning "use the entry's volume"; set it to `0..2` to override deliberately. Components
  serialized before this carry an explicit `1` and go on overriding exactly as they did.
- **A new `AM` entry inherited the previous entry's per-clip trims.** Unity copies the last element when
  an array grows, and `ResetEntry` cleared everything except `_clipVolumes`. Because trims are keyed by
  clip *index*, the leftovers did not read as stale data - they silently re-levelled whichever clip
  landed in the copied slot, so a freshly dropped clip could play at 40% with nothing in the inspector
  to explain it.

### Added

- **Per-clip volume trims are editable.** `_clipVolumes` has been serialized since 10.13.1 and is
  described in its own tooltip, but the entry drawer never drew it, so the only way to set a trim was
  from code. The expanded entry now lists each clip as a row - the clip on the left, its trim slider on
  the right - and add/remove keeps the two index-aligned arrays in step. Untouched clips still store no
  trim at all, so entries authored earlier load unchanged.

### Changed

- Runtime audio code no longer uses `var`, per the explicit-types rule in `AGENTS.md`.

## [10.13.3] - 2026-08-20

### Fixed

- **Selecting an `AM` spammed the console with `GetLastRect` errors.** The new drop-on-the-list handler
  measured the list with `GUILayoutUtility.GetLastRect()`, which logs an error on the Layout pass because
  nothing has been measured yet. The list is now wrapped in a vertical scope, which reports the same area
  and stays silent during Layout.

## [10.13.2] - 2026-08-20

### Changed

- **The AM `Authoring` block is gone; its drop behaviour moved onto the lists.** A separate block with
  `+ Sound` / `+ Music Pool` buttons and two drop boxes sat below the fields it operated on, and its two
  buttons duplicated the `+` the entry lists already carry. Dropping clips is now done on the
  **Sound Entries** / **Music Entries** list itself — anywhere on the list adds one entry per clip, on an
  existing entry's row adds them as variations of that entry. One gesture, aimed where the result lands.

### Fixed

- **A section holding a single list rendered as a bare label instead of a folder.** The one-field
  shortcut in `CustomEditorBase` ran before the force-foldout rule, so `Sounds` (one array) and `Music`
  (an array plus two settings) were styled differently in the same inspector. The shortcut now yields to
  the force-foldout rule.

### Added

- **`CustomEditorBase.DrawCustomProperty`** — a derived inspector can take over one field's rendering and
  call `DrawStandardProperty` to keep the standard look while adding to it.

## [10.13.1] - 2026-08-20

### Fixed

- **`CustomEditorBase` did not compile.** Extracting the section bar into `NeoxiderEditorGUI` dropped the
  trailing `countColor` argument, but `Docs`, `Events` and `Actions` still passed it — three CS1501s, so
  the whole editor assembly was dead. The colour is now derived from the accent inside the shared method,
  and the three call sites pass six arguments like everyone else.

### Changed

- **`AudioEntry` volume range is now 0..2**, for the entry and for per-clip trims. These are multipliers of
  the channel, so a ceiling of 1 could only ever make a clip quieter than the channel — a sample mastered
  too quietly could never be lifted without re-exporting the file.
- **The collapsed entry row leads with the clip, not the volume.** An empty or single-clip entry shows its
  object field right on the row, so a fresh entry is filled by one drag with nothing to expand; a multi-clip
  entry offers a "Show clips" button instead. Volume moved into the body: it is tuned once, clips are
  assigned constantly.
- **`Randomize Pitch` is drawn as the standard ON/OFF pill** through the shared `DrawPillToggleField`,
  instead of a raw checkbox that read as a foreign island in an otherwise styled inspector.

## [10.13.0] - 2026-08-20

### Added

- **`AM`: one record contract for sounds and music.** A cue is now an `AudioEntry`: an optional **id**, a
  **set of clips** (a random one plays each time, never the same twice in a row), a **volume multiplier**
  and an optional **pitch range**. Entries are addressable by index *and* by id, so `AM.I.Play("hit")`
  survives reordering the list. Two ready ways to break up a repeated cue - several clips, or a pitch
  spread - and both work together.

  Volume **multiplies**: what you hear is `channel volume x entry volume`. A music channel at `0.3`
  playing an entry at `1` comes out at `0.3`. Pitch randomisation defaults to **on** for sounds and
  **off** for music.

- **`AM`: music entries are pools, with two modes.** A music entry holding several clips starts on a
  random one; `MusicPoolMode.Loop` (the default) then holds that track until the game says otherwise,
  and `MusicPoolMode.Shuffle` crossfades on to another track when the clip ends - what the old
  `EnableRandomMusic()` did, now one option among two rather than the only behaviour.

  A menu / gameplay / boss soundtrack is therefore three entries with three ids configured in the
  inspector, and the game only ever says `AM.I.PlayMusicPool("boss")` or `AM.I.NextMusicTrack()`. The
  component each project used to write on top of `SetRandomMusicTracks` is no longer needed.

- **`AM`: music crossfades by default.** Every music change - pool switch, `PlayMusic`,
  `PlayMusicByClip`, a shuffle advance - fades across a configurable `Music Fade Duration` (default
  `0.8s`), overridable per pool and per call. `MusicTransition.Instant` cuts hard,
  `MusicTransition.Fade(2f)` sets a one-off length.

  A crossfade needs two sources, so the outgoing track is handed to a hidden second `AudioSource` at its
  exact playback position while the primary `_music` source takes the incoming one. Keeping the primary
  source on the *incoming* track is what lets `Music`, `GetCurrentMusicClip()` and every existing volume
  tweak go on pointing at the track you can actually hear. Fades run on `Time.unscaledDeltaTime`, so
  `Time.timeScale = 0` does not freeze them, and re-asserting the pool that is already playing is a
  no-op instead of a fade into itself.

- **`AM`: per-call overrides.** `SoundOptions` and `MusicOptions` override an entry's volume, pitch and
  clip choice for **one play only**, without touching the configured entry -
  `AM.I.Play("ui", SoundOptions.Volume(0.6f).WithoutPitch())`,
  `AM.I.PlayMusicPool("boss", MusicOptions.Volume(0.5f).WithFade(2f))`. A volume override replaces the
  *entry* volume and is still multiplied by the channel, so the player's own volume slider keeps
  working - which is the point of the `SetVolume` / play / `SetVolume` dance it replaces.

- **`AM`: no-code parity.** The inspector gains a bulk drop zone (drop N clips, get N entries named
  after them), drag-several-clips-onto-a-row to fill one entry, a collapsed row that already shows id,
  clip count and volume slider, duplicate-id and empty-entry warnings, and a live "now playing" readout.
  `PlayAudio` and `PlayAudioBtn` gained a **Sound Id** field with a dropdown of the ids actually
  configured on `AM`, and a new `MusicControl` component starts a pool, moves to the next track or stops
  the music from a UnityEvent. `Play(string)`, `PlayMusicPool(string)`, `NextMusicTrack()` and
  `StopMusic()` all take zero or one argument on purpose, so a UnityEvent can call them directly.

  The `AM` inspector derives from `CustomEditorBase` and only adds blocks after the standard property
  pass, so the banner, docs foldout, health panel and section rails are untouched.

### Changed

- **`AM`: `PlayMusic(int)` and `PlayMusicByClip` now respect the music channel volume.** Previously they
  wrote the requested volume straight onto the `AudioSource`, so `SetMusicVolume(0.3f)` followed by
  `PlayMusic(0)` played at full volume and quietly discarded the player's setting. They now follow the
  `channel x entry` contract like everything else. With the channel at its default `1` the result is
  identical to before; a project that lowered the music channel will now actually hear it.

- **`AM`: the music `AudioSource` volume authored in the inspector is now the music channel.** It is
  adopted once, at runtime init, and everything else multiplies against it. Before, the channel started
  at `1` regardless of what the source said - so a project that had turned the source down and relied on
  `EnableRandomMusic()`, which never wrote the volume, would suddenly have played its soundtrack at full.
  This also collapses two competing numbers into one: `SetMusicVolume` and the authored value.

- **`AM`: `StopMusic()` fades out** instead of cutting, on the same default duration.
  `StopMusic(MusicTransition.Instant)` restores the old behaviour. `OnMusicStopped` still fires
  immediately, so event counts are unchanged.

### Deprecated

- **`AM.EnableRandomMusic()` and `AM.SetRandomMusicTracks(...)`.** Both still work exactly as before -
  the track list is played as a shuffle pool - but the modern shape is a music entry with several clips
  and `MusicPoolMode.Shuffle`, addressed by id. Nothing is removed in this release.

### Compatibility

- **Old scenes keep playing.** `_sounds`, `_musicClips`, `_randomMusicTracks` and `_useRandomMusic` are
  still serialized and are migrated into the new entry lists on first load, once, guarded by a stamped
  data version so an intentionally emptied list is not repopulated. Legacy `Sound` records inherit the
  manager's old global pitch switch, and the old `volume == 0` meaning "full" is folded into the
  migrated value rather than carried into the new contract, where zero means zero. Music indices are
  preserved (one entry per clip, in order) and the random list is appended after them as a pool, so
  `PlayMusic(int)` still resolves to the same track.

- Every pre-10.13 member - `Play(int)`, `Play(int, float)`, `Play(AudioClip)`, `Play(AudioClip, float)`,
  `PlayMusic(int)`, `PlayMusic(int, float)`, `PlayMusicByClip(...)`, `StopMusic()`,
  `SetRandomMusicTracks`, `EnableRandomMusic`, `DisableRandomMusic`, `IsRandomMusicEnabled`,
  `GetCurrentMusicClip`, `SetVolume`, `SetMusicVolume`, `SetEfxVolume`, `ApplyStartVolumes`, `Efx`,
  `Music`, `RandomizePitch`, `SetPitchRange`, `OnMusicStarted`, `OnMusicStopped`,
  `OnRandomMusicTrackChanged` - is still present and behaves as documented above.
## [10.12.0] - 2026-08-20

### Fixed

- **Scene Saver re-saved the same scene forever and did it inside the editor tick.** The backup is
  written with `EditorSceneManager.SaveScene(..., saveAsCopy: true)`, which deliberately leaves the
  edited scene **dirty**, while `SceneSaverGUI.SaveSceneClone` used that same dirty flag as its "not
  backed up yet" trigger. A scene that stays dirty — the normal state while you work, and the permanent
  state when some tool keeps dirtying it — was therefore re-serialized in full every 3 minutes for as
  long as the editor stayed open. It was the amplifier behind the UI Mesh Rig freeze fixed below
  (`SceneSaver.BackgroundSaveCheck` in the editor's "Hold on" dialog), and it fires from any dirty
  scene, not only from a rig preview. Scheduling now runs on a scene **revision** (scene path +
  `Undo.GetCurrentGroup()` + observed clean-to-dirty transitions) held by the new
  `SceneSaverAutoSaveScheduler`: the same revision is never written twice, in any mode, so another copy
  is only possible after the scene actually changes.
- **The auto-save no longer blocks the editor tick.** `SceneSaver.BackgroundSaveCheck` used to call
  `SaveScene` straight from `EditorApplication.update`. The tick callback now only compares numbers and
  strings, and hands the save to `EditorApplication.delayCall` (one queued save at a time). Checks are
  additionally skipped in Play mode, while the editor compiles or imports assets, and while a prefab
  stage is open — in prefab isolation the "active scene" is the stage, so the old code could write a
  bogus `<prefab>_AutoSave.unity`. Batch mode is skipped entirely: there is no user to protect there,
  and a backup written into the repository during a CI run is pure side effect.
- **Auto-save can finally be switched off for good.** The settings existed only inside the window's GUI
  instance, and the background checker owned a *second* instance of its own — so the toggle in the
  window did not stop the background saver at all, and nothing survived a domain reload or a restart.
  Settings now live in `SceneSaverSettings`, persisted in `EditorPrefs`
  (`Neoxider.SceneSaver.Enabled`, `.IntervalMinutes`, `.SaveEvenIfNotDirty`) and shared by the window
  and the background check. Defaults are unchanged: enabled, 3 minutes.
- **A failing auto-save no longer hurts the editor.** `BackgroundSaveCheck` is wrapped: an exception
  detaches the callback (instead of repeating every tick) and logs how to re-arm it; the deferred save
  catches its own failures and still marks the revision handled, so a scene that cannot be written is
  not retried on every tick. `EditorApplication.update`, `EditorSceneManager.sceneOpened`,
  `AssemblyReloadEvents.beforeAssemblyReload` and `EditorApplication.quitting` are subscribed with a
  paired `-=` and released on assembly reload and on quit. Reading scene state moved out of the
  `[InitializeOnLoad]` static constructor into `delayCall`.
- **A zero interval is no longer accepted.** `Interval (minutes)` is clamped to 0.25; the field used to
  take `0`, which means "save on every editor tick".

- **UI Mesh Rig `Start Preview` could freeze the Unity Editor.** `UIMeshRigMotionPreviewDriver` held a
  permanent `EditorApplication.update` subscription created at `[InitializeOnLoad]`, and every tick — in
  every project, with or without a rig in the scene — ran `Resources.FindObjectsOfTypeAll<UIMeshRigPointMotion>()`
  and then `SceneView.RepaintAll()` + `EditorApplication.QueuePlayerLoopUpdate()`. The preview also wrote
  serialized state each tick, so the scene stayed permanently dirty and the package's own auto-save
  (`SceneSaver.BackgroundSaveCheck`, itself an `EditorApplication.update` callback) kept writing a full
  scene copy — the callback named in the editor's "Hold on / busy" dialog. Now the driver keeps a registry
  of active previews, subscribes to `EditorApplication.update` **only** while one of them actually needs
  ticks, and unsubscribes as soon as the last one stops.
- **Edit Mode preview stopping is now guaranteed.** Previews of objects that are no longer selected are
  dropped on selection change *and* whenever any rig inspector is disabled, so closing or re-targeting an
  Inspector cannot orphan a running preview. Previews also stop on
  `AssemblyReloadEvents.beforeAssemblyReload`, on `EditorApplication.playModeStateChanged` and on
  `EditorApplication.quitting`; every one of those handlers is unsubscribed again on shutdown. A motion
  that throws is dropped from the registry instead of taking the shared editor callback down with it.
  The teardown is deliberately selection-based and never reads `Editor.target`: an Editor created through
  `ScriptableObject.CreateInstance` has no target array, and Unity's own `target` getter throws on it
  while `OnDisable` runs inside `DestroyImmediate`.
- **Preview no longer writes the point Transform.** Dragging the bind anchor used to run
  `point.transform.position = NormalizedToWorld(RestCenter)` unconditionally, which snapped an authored
  pose back onto its anchor — the reported "moving the anchor resets the position". The write is now
  skipped while the point is previewed or the rig is in `Pose / Animate`. `UIMeshRigLayoutBuilder` (a
  runtime API) no longer flips the rig's serialized authoring mode as a preview side effect; the explicit
  editor action `Apply Layout & Preview` still sets `Pose / Animate`, inside its own Undo group.
- **The rig no longer re-binds a previewed point behind the user.** `SynchronizePointTransforms` writes
  bind centre, anchors and rest TRS from `LateUpdate` with no Undo entry; the preview forces a player-loop
  update every editor tick, so any stray `Transform.hasChanged` was silently baked into the asset and left
  the scene dirty. Points carrying a procedural pose are skipped in `UIMeshRigGraphic`,
  `UIMeshRigWorldRenderer` and `UIMeshRigSpriteRenderer`.
- **`UIMeshRigPointMotion.PreviewInEditMode` is no longer serialized.** As a `[SerializeField]` the
  Start Preview button dirtied the scene and the preview survived save, domain reload and Play Mode.
- **Preview registry could skip entries or index past its end.** Stopping a preview raises
  `EditModePreviewStateChanged`, whose handler edits the same list the tick and selection loops were
  walking by index. Both loops now run over a copy.
- **Inspector repaint loop.** `UIMeshRigPointEditor` called `SceneView.RepaintAll()` from
  `OnInspectorGUI` while `RequiresConstantRepaint()` was true, so the Inspector and Scene view repainted
  each other for as long as a motion component existed — including while the preview was paused or
  stopped. Constant repaint is now bound to a preview that is actually advancing.
- **`AM`: the effects volume slider stopped working for pitched one-shots.** A voice from the pitch pool
  introduced in `10.11.0` copied `_efx.volume` once, when it was created. `SetEfxVolume` writes
  `_efx.volume`, so turning effects down silenced the plain one-shots and left every pitched one at the
  volume that happened to be current when its voice was first needed. Routing, volume, mute, spatial blend
  and the bypass flags are now mirrored from `_efx` on every shot. Resizing the pool from the Inspector in
  Play Mode also no longer orphans the voices it already built.

### Added

- `Assets/Neoxider/Tests/Edit/Editor/SceneSaverAutoSaveTests.cs` — nine EditMode tests covering the
  scheduler (an unchanged scene is never written twice, a changed one still is, a scene dirtied again
  earns exactly one more copy, an unsaved scene is skipped) and the persisted settings (the disabled
  state, the interval and the not-dirty option survive a restart, the interval is clamped, reset clears
  the keys).
- `Reset Settings` button in the Scene Saver window.

### Changed

- The Mesh Rig Point Motion transport shows preview state (`Stopped` / `Playing` / `Paused` with the
  current time), disables `Start Preview` while a preview is already running, and states the preview
  contract next to the buttons instead of hiding it inside one preset's hint.
- `Assets/Neoxider/Docs/UI/UIMeshRig.md` is now English like every other page under `Docs/`, and
  documents the Edit Mode preview contract.

## [10.11.0] - 2026-08-20

### Fixed

- **UI Mesh Rig preview could peg the editor.** The edit-mode preview driver called
  `EditorApplication.QueuePlayerLoopUpdate()` on every tick; that schedules another editor tick,
  which re-enters the driver, which queues another - a self-sustaining loop running as fast as the
  machine allows. Nothing ended it, because the motion presets are continuous and `IsPlaying` never
  returns to false on its own, and `PreviewInEditMode` is serialized, so a scene saved with a live
  preview restarted the loop on load with nothing on screen to explain the load. The queue call is
  gone - `SceneView.RepaintAll()` already animates the view - and the driver now steps at most sixty
  times a second.

- **Package version parity.** `README.md`, `Assets/Neoxider/README.md`, `PROJECT_SUMMARY.md`,
  `Docs/README.md`, `Docs/PackageCompatibility.md`, `Docs/Samples.md`, `AGENTS.md` and
  `Skill/neoxider-tools/SKILL.md` still advertised `10.10.0` (the repo-root README `10.8.4`) against
  a `10.10.2` package, so `PackageVersionParityTests` was already failing before this release.

### Added

- **`AM`: random pitch for sound effects.** New `_randomizePitch` toggle with a `_pitchMin` /
  `_pitchMax` range (default `0.94`-`1.06`) and `_pitchVoices` pool size, plus the `RandomizePitch`
  property and `SetPitchRange(min, max)` for code. Repeated cues - a blade hit, a button, a coin -
  stop sounding like the same sample on a loop.

  The pitch is NOT set on the shared `_efx` source: `AudioSource.pitch` applies to the whole source,
  so it would also retune the one-shots still ringing on it - and overlapping shots are precisely
  the case the feature exists for. Each pitched shot takes a voice from a small round-robin pool
  parented to `_efx`, copying its mixer group, spatial blend and volume. With the toggle off the
  path is byte-for-byte the old one and nothing is allocated. Music is never pitched.

## [10.10.2] - 2026-08-13

### Fixed

- **`UIMeshRigMenu` broke compilation on Unity 6.5+.** `ProjectWindowUtil.CreateAssetWithContent` is
  an ERROR-level obsolete there, so the call fails the build instead of warning. Now guarded by
  `UNITY_6000_5_OR_NEWER` and routed to `CreateAssetWithTextContent`; older editors keep the old call.

## [10.10.1] - 2026-08-13

### Fixed

- **`UIMeshRigGraphic` broke every player build.** `Graphic.OnValidate` is declared under
  `UNITY_EDITOR`, so overriding it unconditionally compiles fine in the editor and fails the player
  build with `CS0115: no suitable method found to override`. The override is now wrapped in
  `#if UNITY_EDITOR`. This is invisible to edit-mode tests and to the console — only a real build
  catches it, which is exactly how it was found (a WebGL build of a consuming project).

## [10.10.0] - 2026-08-12

### Added

- **UI Mesh Rig now supports a plain `SpriteRenderer`** (`UIMeshRigSpriteRenderer`), so sorting layers,
  2D lights, sprite masks and SRP batching keep working while the artwork deforms. The imported Sprite
  asset is never touched: the component renders a runtime clone and hands the original back on disable.
  10.9.0 had documented this adapter as impossible — it is not.
  - The clone is written through the public `UnityEngine.U2D.SpriteDataAccessExtensions`
    (`SetVertexCount` / `SetVertexAttribute` / `SetIndices`), which ships in `UnityEngine.CoreModule` and
    needs no 2D Animation package. `Sprite.OverrideGeometry` is deliberately *not* used: measured in a live
    6000.3.14f1 editor it is a silent no-op on a runtime sprite (vertex count and positions unchanged) and
    only takes effect on the imported asset — exactly the shared-state mutation to avoid.
  - `Bounds Headroom` inflates the clone's culling bounds, which Unity derives from the sprite rect and
    never grows with written geometry, so a strongly warped sprite is not culled early at the screen edge.
- `GameObject > 2D Object > Neoxider UI Mesh Rig (Sprite Renderer)` creation menu.
- Scene-view overlay for every rig: Setup / Pose mode switch, Move / Rotate / Scale tool, and `Labels` /
  `All rings` readability toggles.

### Changed

- **The UI Toolkit host binds to `PanelRenderer`, not `UIDocument`.** From Unity 6.4 world-space UI Toolkit
  renders through `PanelRenderer`, so the host subscribes to its UI-reload callback and adds the element to
  the root it hands out; `UIDocument` is now only the fallback for editors that have no `PanelRenderer`
  (verified by reflection: the type does not exist in 6000.3). `[RequireComponent(typeof(UIDocument))]` is
  gone — it forced the legacy component onto projects that had already migrated.
- **Rig inspectors are attribute-driven again.** Fields carry ordinary `[Header]` / `[Tooltip]` and are
  drawn by `CustomEditorBase`, which is what produces the collapsible sections with counts, the ON/OFF
  switches and the coloured rails everywhere else in the package; the custom editors keep only what no
  attribute can express (layout button, diagnostics, point list, Scene handles). `Raycast Target`,
  `Raycast Padding` and `Maskable` are visible again instead of being buried inside a collapsed
  `Advanced Rig Controls` foldout, and the authored raycast padding moved to a hidden field so the same
  value is no longer shown twice. The `Script` field is left exactly where Unity puts it.
- **Scene handles are readable and usable in both modes.** Bind-pose handles (anchor and radii) used to
  exist only in Setup, so Pose looked like the anchor and the rings could not be moved at all. Unselected
  points now draw one faint outer ring without a label instead of two solid ellipses plus overlapping
  names, radius handles exist on all four sides (±X, ±Y), and the anchor is larger with a contrast ring.
  Labels are dimmed through their own `GUIStyle` because `Handles.Label` ignores `Handles.color`.
- One owner-generic implementation (`IUIMeshRigOwner`, `UIMeshRigOwnerResolver`) replaces the per-renderer
  copies of point resolution, layout application, undo bookkeeping and inspector blocks. A point finds its
  rig by nearest ancestor implementing the interface instead of a hard-coded list of component types.
- `UIMeshRigMotionProfile` fields are PascalCase per the package rule, with `[FormerlySerializedAs]` so
  profiles authored under the old camelCase names keep their values.
- The remaining `EditorUtility.DisplayDialog` guards in the Image conversion menu became console warnings,
  matching the conversion notice — a modal dialog hangs MCP-driven automation.

## [10.9.0] - 2026-08-12

### Added

- UI Mesh Rig now has one renderer-neutral geometry/deformation core and three output adapters: existing
  uGUI `UIMeshRigGraphic`, Unity 6 UI Toolkit `UIMeshRigElement`, and world-space
  `UIMeshRigWorldRenderer` for `MeshFilter`/`MeshRenderer` scenes without Canvas.
- Added UI Toolkit UXML/UI Builder support, atlas-safe UV handling, world/UI Toolkit creation menus,
  matching Module inspectors, reusable layout presets, adapter-equivalence tests, and a six-example demo.

### Changed

- Documented why plain `SpriteRenderer` is intentionally unsupported: Unity's supported deformable Sprite
  workflow requires the optional 2D Animation package and sprites authored with bones and weights.

## [10.8.4] - 2026-08-12

### Fixed

- Added the getter-only `ILayoutElement.maxWidth` and `maxHeight` members required by the uGUI version
  shipped with Unity 6.6. UI Mesh Rig remains unconstrained by maximum layout size and continues to
  compile on earlier Unity versions where these ordinary public properties are not interface members.

## [10.8.3] - 2026-08-12

### Fixed

- **Static state left over between Play Mode sessions when Domain Reload is disabled.** Consumer projects
  that enable Enter Play Mode Options never reload the domain, so package statics survive into the next
  session. Four remaining holders now reset themselves, joining the modules that already did:
  - `AbilitySystemBehaviour` releases its scene hub, which had been keeping the previous session's
    `AbilitySystem` and every registered unit alive and handing a destroyed component to `InstanceOrNull`;
  - `DamageService` clears its shield scratch pool and zeroes the re-entrancy depth, so a session cannot
    start on top of the previous session's `ModifierInstance` graph or a misaligned nesting level;
  - `StateMachineEvaluationContext` drops the current context and its push/pop stacks, so an evaluation
    aborted by an exception cannot leave a destroyed `GameObject` as the transition context;
  - `PrefabPreviewExtensions` clears its preview-sprite cache — the old `[InitializeOnLoadMethod]` hook
    only fires on a domain reload and therefore never ran for these projects.

  Each reset runs from `SubsystemRegistration` on every editor, and additionally from `[OnExitingPlayMode]`
  on Unity 6.5+ so the editor stops holding destroyed objects the moment play stops.
- Types using the `[OnExitingPlayMode]` lifecycle attribute are now declared partial, as its source
  generator requires.

### Changed

- Documented Unity 6.5/6.6/6.7 forward compatibility in
  [Docs/PackageCompatibility.md](Docs/PackageCompatibility.md): the package carries no legacy
  `UxmlTraits`/`UxmlFactory` UI Toolkit elements, no `com.unity.inputsystem` `versionDefines`, no
  serializable reference cycles and no APIs from the Unity 6.7 obsolete-to-error sets.
- Version parity restored across every file `PackageHealthCheck` guards: the repo-root and package README
  badges, `PROJECT_SUMMARY.md`, `Docs/README.md`, `Docs/PackageCompatibility.md`, `Docs/Samples.md`,
  `AGENTS.md` and the skill metadata, which had been left at `10.6.0`/`10.6.2` while `10.7.0`–`10.8.2` shipped.

## [10.8.2] - 2026-08-12

### Changed

- UI Mesh Rig Graphic, Point and Point Motion inspectors now inherit the shared `CustomEditorBase`, so they
  use the complete Neoxider Tools chrome, mascot/version/update status, documentation and module styling.

## [10.8.1] - 2026-08-12

### Fixed

- UI Mesh Rig editor motion preview now switches the owner into Pose mode, so Start and Restart always
  produce visible deformation.
- Removing a rig Sprite clears its cached hit mesh and restores authored raycast padding; an invisible rig
  no longer intercepts pointer input in any hit-test mode.
- Image conversion now preserves disabled rendering and retargets same-object Selectables in both in-place
  and non-destructive workflows, including a single-step Undo.
- Scene handles clamp the point Transform to the same normalized center shown by the dedicated center gizmo.
- The Animator workflow in the UI Mesh Rig demo now loops as documented, and click feedback restores the
  authored tint when interrupted.

## [10.8.0] - 2026-08-12

### Added

- **Professional UI Mesh Rig point authoring.** Every point has independent inner FULL and outer ZERO
  ellipse gizmos, a dedicated center handle, preset/custom falloff curves and a full-smooth mode.
- **Interactive deformed graphics.** Rectangular, deformed-mesh and Sprite-alpha hit tests integrate with
  standard uGUI Buttons, masks and raycast padding.
- **Purpose-built inspectors.** Rendering, interaction, mesh, influence, deformation and animation controls
  are grouped into clear sections with contextual guidance and quick actions.

## [10.7.0] - 2026-08-11

### Added

- **UI Mesh Rig.** `UIMeshRigGraphic` renders a subdivided Sprite inside uGUI and blends any number of
  child `UIMeshRigPoint` transforms. The points use native RectTransform position/rotation/scale, so Unity
  Animator can record them without a proprietary clip format.
- **Two-mode Scene authoring.** Setup mode edits bind positions, elliptical influence radii and falloff;
  Pose / Animate mode previews move, rotate and scale deformation directly in Scene View. Reset and Capture
  Rest Pose commands support iteration, while a saved Pose can be used as a permanent static deformation.
- **Image conversion and creation menus.** Create a rig from `GameObject/UI`, or convert an existing Image
  to a stretch-aligned rig child while preserving sprite, color, material and raycast behavior.
- **Non-destructive point motion.** `UIMeshRigPointMotion` adds editable Position X/Y, Rotation and Scale X/Y
  curves on top of a point's Transform pose, so procedural motion and Unity Animator can run together without
  writing the same Transform. Built-in editable presets cover Float, Breathe, Body/Head Sway, Soft Jiggle,
  Pulse and Squash/Stretch, with scaled/unscaled time and safe Edit Mode preview.
- **Production authoring contracts.** Bind points use responsive normalized anchors, disabled points stop
  influencing the mesh, nested rigs are isolated, nested point reset is deterministic, and bind weights are
  cached separately from runtime pose updates.

## [10.6.2] - 2026-08-10

### Fixed

- **Disabling a slot no longer dispatches result callbacks into an inactive object graph.** An accepted spin
  keeps its planned result while disabled and completes exactly once after reactivation. Durable owners can
  explicitly call `CancelActiveSpin()` when they will void/refund the transaction themselves.

## [10.6.1] - 2026-08-10

### Fixed

- **Slot spins can no longer strand the lifecycle while gameplay time is paused.** `Row` movement and
  `SpinController` column staggering now use unscaled time. A configurable unscaled safety deadline settles
  non-updating reels to the already planned outcome, and disabling a row/controller completes an accepted spin
  through the normal result callbacks exactly once. The controller becomes idle before callbacks, so the next
  spin can be accepted immediately without application-side state repair.

## [10.6.0] - 2026-08-09

- **Module namespace cleanup.** Flattened legacy nested namespace declarations without changing their CLR
  type names. `AnimationFly` / `FakeLoad` now live in `Neo.UI`, and `ParallaxLayer` in `Neo.Parallax`, with
  Unity `MovedFrom` metadata preserving serialized component references. Root `Neo` remains intentionally
  limited to shared Inspector/authoring attributes for source compatibility.
- **RPG networking is now optional.** `Neo.Rpg` no longer references the `Neo.Network` runtime
  implementation or Mirror; it depends only on the transport-neutral `Neo.Network.Contracts` leaf
  assembly, and
  `RpgCharacter` is a local `MonoBehaviour` instead of a `NeoNetworkComponent`. Mirror command routing,
  authority checks, rate limiting, late-join snapshots, and network projectile policy now live in the
  optional `Neo.Rpg.Network` adapter assembly. Existing networked prefabs must add
  `RpgCharacterNetworkAdapter`; local/offline prefabs keep their API and serialized data.
- **RPG dependency closure is now network-free.** Removed unused runtime references from `Neo.Rpg` and
  removed the unused `Neo.Network`/Mirror define from `Neo.Save`. The condition-dependent
  `RpgConditionAdapter` now compiles in `Neo.Rpg.ConditionBridge`, preserving its script GUID, namespace,
  and Unity assembly-migration metadata. `RpgNoCodeAction` remains in `Neo.Rpg` to preserve external
  UnityEvent type names. An EditMode architecture test walks the complete named-asmdef closure and prevents
  the base RPG assembly from reaching Mirror or a `Neo.Network` implementation again.
- **`RpgCharacterProfileService` plain-C# core.** Profile validation/serialization is reusable by save
  persistence and network adapters; `RpgCharacter` now exposes `CaptureProfile()` / `ApplyProfile(...)`.
- **`RpgCharacterResourceService` plain-C# core.** Resource dictionaries, clamped mutations, reactive
  queries, derived max/regen updates, spend/damage pause windows, and regen tick timing moved out of the
  scene component. Existing `RpgCharacter` serialized fields, public APIs, UnityEvents, death handling,
  and network snapshot routing remain compatible delegates.
- **Assembly boundaries hardened.** All package asmdef references now use assembly names instead of GUIDs;
  the broad `Neo` assembly was removed; `Neo.Core` is a leaf with separate Level data/components/bridge and
  Resources component assemblies. Legacy AttackSystem sources moved to `Rpg/Combat` in the optional
  `Neo.Rpg.Combat` compatibility assembly, and optional
  Shop/Inventory integration moved to `Neo.Shop.Bridges`, preserving script GUIDs and public type names.
- **CI and behavioral coverage expanded.** CI now runs PlayMode tests as a separate job. EditMode tests are
  organized by module, Cards deck/hand edge cases are stronger, and behavioral smoke coverage now includes
  FakeLeaderboard, Tools.Other, and Tools.Debug.

- **`InteractiveObject` Inspector testing API.** Added Play Mode-only `Test Interact Down`,
  `Test Interact Up`, `Test Click` and `Invalidate Colliders` buttons. The corresponding public
  `InteractDown()`, `InteractUp()` and `Click(...)` methods use the same local/Mirror dispatch path as
  real input and respect `interactable`.
- **Reusable `InteractiveObject` runtime core.** `IInteractiveTarget` lets custom input, AI, XR, and
  proximity controllers drive interaction without depending on the scene component. Pure, allocation-free
  `InteractionQueryMath` / `InteractionRayHit` APIs now own range and ray-hit ordering rules, while
  `InteractionCameraResolver` centralizes the existing main-camera/fallback lookup. These APIs now live in
  the leaf `Neo.Tools.InteractableObject.Core` assembly, which has no Mirror or `Neo.Network` dependency;
  their `Neo.Tools` namespace and asset GUIDs remain compatible. Serialized fields, public methods,
  Inspector behavior, and Mirror dispatch remain compatible.
- **More complete runtime test controls.** Added missing Play Mode-only Pause/Resume actions to the
  animator and random-music components, plus practical Jump, AI Resume, CameraShake Stop/Reset, Typewriter
  Clear, NPC navigation and toggle controls. Existing runtime buttons in these components are now
  disabled outside Play Mode.

## [10.5.0] - 2026-08-09

Inspector test buttons for the components you reach for when tuning a scene by hand, plus a new
`TransformAnimator` runtime component. The runtime buttons are Play-Mode-only: starting coroutines,
spawning objects or shaking a transform takes effect in Play Mode.

### Added — authoring

- **`[Button]` inspector buttons on trigger-style methods across the package.** Spin a roulette, fire a fly
  animation, shake a button or start a patrol straight from the Inspector, without wiring a temporary UI button:
  - `LineRoulett` — `StartRolling`, plus `Update Visual` (rebuilds the reel layout; the `updateSetting`
    checkbox toggle still works);
  - `SpinController` — `StartSpin`, `AddLine` / `RemoveLine`, `AddBet` / `RemoveBet`, `SetMaxBet`;
  - `AnimationFly` — `RefreshPrefabCache`, and the new `TestFlyByType` below;
  - `SimpleSpawner.Spawn`, `FieldSpawner.SpawnOnAllWalkable`, `ButtonShake.Shake` / `StopShake`,
    `RandomMusicController.Start` (labelled "Play Random Music") / `Stop`, `Play` / `Stop` on `ColorAnimator`,
    `FloatAnimator`, `Vector3Animator` and `LightAnimator`, `TypewriterEffectComponent.PlayAutoText` / `Stop`,
    `AiNavigation.StartPatrol` / `StopPatrol` / `Stop`, `SpineController.PlayDefault` / `PlayDefaultForced` /
    `NextSkin` / `Stop`.
- **`AnimationFly.TestFlyByType(int type, int bonusCount = 5)`** — one-click smoke test for a fly setup:
  spawns items of the given type from the spawn parent (or the component's own transform) towards the end
  point configured for that type in `Bonus Prefab List`.

### Added — Animations

- **`TransformAnimator` — universal transform animator** (`Neo.Animations`). Generalizes ad-hoc
  "rotating pickup" scripts (asset-store gem animators and the like) into one component with combinable
  channels on a shared clock: constant **rotation** (deg/s per axis), curve-eased **float/bob** along any
  direction, **scale pulse**, continuous **Perlin shake**, and one-shot **impulse shake** via
  `Shake(strength)`. Every eased channel takes an `AnimationCurve`; `RandomizeStartTime` desyncs rows of
  items. The math lives in the pure, scene-free `TransformAnimationEvaluator` with edit-mode tests;
  the MonoBehaviour only captures the base pose and applies the evaluated state. Trigger methods carry
  `[Button]`. Docs: `Docs/Animations/TransformAnimator.md`.

## [10.4.1] - 2026-07-31

The character controller's collider is a generated value, and nothing said so. Fixed at the source.

### Fixed

- **`Character First Person` and `Character Third Person` presets stood 1 m above the floor.** Both shipped with
  `Mover.colliderOffset = (0, 0, 0)` while their children are authored feet-at-origin — `Model` at `y = 1` (the
  built-in capsule mesh is 2 m tall around its own centre, so its feet land on `y = 0`) and `CameraPivot` at
  `y = 1.6` first person / `y = 1.5` third person. `colliderOffset` is normalised — it is multiplied by
  `colliderHeight` — and ground detection parks the transform origin at `colliderHeight * (0.5 - colliderOffset.y)`
  above the floor, so with `0` the whole rig, camera included, hung half a body height in the air. Both presets now
  use `(0, 0.5, 0)` like every CMF prefab and every animated preset in this package already did. Standing on flat
  ground the collider still occupies `stepHeightRatio * colliderHeight … colliderHeight` above the floor whatever
  the offset is, so walking, slopes and steps are unchanged.

  ⚠ **Breaking for existing scenes that use these two prefabs.** `colliderOffset` lives on the `Mover` of the
  prefab root and is unlikely to be overridden per instance, so the new value propagates on upgrade and every such
  character settles 1 m lower relative to the floor than before.

  **You are affected if, after upgrading, the camera sits about a metre too low or the character model is buried
  up to the waist in the floor.** That means you had compensated for the old defect somewhere in your own scene,
  and the compensation is now counted twice. Undo it — the presets are authored feet-at-origin, so the shipped
  values are the correct ones: `Model` at `y = 1` (built-in capsule mesh, pivot at its centre), `CameraPivot` at
  `y = 1.6` first person / `y = 1.5` third person, and spawn points at floor level rather than a metre above it.
  If instead your character simply dropped a metre and now stands correctly, you had not compensated and there is
  nothing to do.

  One more consequence, easy to miss because it only shows at spawn time: a character created in mid-air now
  starts 1 m higher relative to its own origin, so it falls that extra metre before ground detection catches it.

### Added — authoring

- **The Mover inspector now shows the collider it generates, read-only.** `Mover.RecalculateColliderDimensions()`
  overwrites the attached collider's height, radius and centre from `Collider Height` / `Collider Thickness` /
  `Collider Offset` / `Step Height Ratio` in `Awake` *and* in `OnValidate` — so any value typed into the
  `CapsuleCollider` component is silently discarded on the next inspector change. `MoverInspector` now prints the
  resulting dimensions in a disabled block, states where the numbers come from, reports where the transform origin
  rests on flat ground, and warns with a one-click fix when `Collider Offset` Y is not `0.5` while children are
  authored feet-at-origin.
- **Tooltips on the four collider fields of `Mover`**, including the fact that `Collider Offset` is normalised.
  Attributes only — no behaviour or API change.
- **`Docs/Tools/Move/CharacterController/README.md`** gained a "Character size" section with the formulas.

## [10.4.0] - 2026-07-31

A full audit pass over the package: runtime defects found by reading every module, plus asset, prefab and
test hygiene. Nothing was removed and no public API changed shape.

### Fixed — runtime defects

- **`SingletonRuntimeReset` threw on any two-level singleton subclass.** `class MyGm : Gm` (where `Gm : Singleton<Gm>`)
  made the reset sweep call `MakeGenericType` on an already-closed type; the `ArgumentException` aborted the whole
  sweep, so *every* singleton kept its statics across play sessions with domain reload disabled. The sweep now walks
  the inheritance chain to the closed base instead of reconstructing it.
- **Statics surviving between play sessions.** Added the package's `ResetStaticState` hook to `FieldGenerator.I`,
  `InventorySlotGridView`'s pending selection, `Money.Registry`, `GlobalSave` (`_data`/`IsReady`) and `UI.I`.
  `FieldGenerator` also clears `I` in `OnDestroy` when it owns the reference.
- **`Money.AddOverflow` bypassed Mirror authority** — it wrote the balance locally on clients instead of going through
  the command/RPC path `Add` already used. Added `MoneyOp.AddOverflow` and wired it into the dispatch.
- **`Money.SetMoneyForLevel` skipped the shared deposit path**, so the level payout ignored `Max Money`, never grew
  `AllMoney` and never updated `LastChangeMoney`. It now goes through the same local add as everything else.
- **`Money.ReloadBalanceFromSave` did not notify.** The public reload wrote with `SetValueWithoutNotify`, so UI bound
  to `CurrentMoney` silently kept the stale balance.
- **`AbilitySystem.Revive()` published no event.** Death publishes a receipt, revive did not, so nothing downstream
  could react. Added `AbilityEvents.Revive`, published with the restored health as `Amount`.
- **`TypewriterEffect` restarted mid-run killed its own successor.** The finishing run disposed the shared
  `CancellationTokenSource` in its `finally`, cancelling the run that had just replaced it. Guarded with a run
  generation counter, the same pattern `Timer` uses.
- **`Evade` left `IsEvading` stuck true when disabled mid-evade**, permanently blocking further evades.
  `OnDisable` now clears the state; `OnEvadeCompleted` is deliberately *not* raised — the evade was interrupted,
  not completed.
- **`AdvancedAttackCollider` silently dropped knockback.** With `Use Advanced Force Applier` on, a target without an
  `AdvancedForceApplier` returned early instead of falling back to the Rigidbody path.
- **`TimerObject.Reset()` sent countdown timers to 0** instead of back to full duration, so a reset countdown
  reported 100% progress and could fire immediately.
- **`InteractiveObject` clicks were dead when `Use Hover Detection` was off** — the target raycast only ran for hover,
  and the mouse path then had no target to act on.
- **`Spawner` never recovered from being disabled.** The spawn coroutine died with the component but `isSpawning`
  stayed true, so re-enabling produced nothing forever. Added `OnDisable`; pending destroy coroutines are left
  alone on purpose so disabling the component does not leak spawned objects.
- **`Drawer` destroyed pooled `LineRenderer`s** on discard and on `DeleteAll`, draining the pool it was supposed to
  reuse.
- **`Match3BoardService` stayed locked** if it was disabled mid-cascade — the resolve routine handle was never cleared.
- **`Selector` ignored `result.UniqueReset`**, so `OnUniqueReset` never fired on an automatic cycle reset.
- **`AnimationFly.WorldToCanvasPosition`/`CanvasToWorldPosition` threw** when called with no canvas and no instance
  in the scene; they now log and return zero like the sibling helpers.
- **`DrunkardGame` destroyed captured cards** when only one side used a hand — the war-pile branch tested both hand
  flags together instead of the winner's.
- **`Bonus/Slot/Row` returned two strays swapped** in the top-down fallback ordering.

### Fixed — packaging and assets

- **12 dead asset references across 7 shipped prefabs.** `UI/ButtonPageSwitch` and `Shop/ButtonPrice` pointed
  at TMP font assets and materials that exist nowhere in the project (those labels rendered no text at all);
  `Bonus/LineRoulett`, `Bonus/Slot/SlotElement`, `FakeLeaderboard/Page LeaderBoard`, `UI/Page/Fake Load` and
  `Tools/First Person Controller` had missing sprites and an audio clip. Fonts now use the TMP default the
  rest of the package already uses, sprites fall back to Unity's built-in `UISprite`, and the dead clip
  reference is cleared.
- **The package no longer needs Mirror to be installed.** `-System--`, `First Person Controller`,
  `Interactive Sphere` and `Toggle Interactive` shipped with a `Mirror.NetworkIdentity` component, which is a
  missing script for everyone who does not use Mirror — `-System--` being the system root made it the widest
  case. Multiplayer stays an opt-in layer: add `NetworkIdentity` yourself when you go online.
- Sample sprites with Cyrillic and Chinese filenames were renamed to ASCII — non-ASCII asset names break CI
  runners and cross-OS checkouts.

### Fixed — tests and infrastructure

- The two play-mode test assemblies were merged: `Neo.Tests.PlayMode` (a single file on the deprecated
  `optionalUnityReferences` config) folded into `Neo.Tests.Play`.
- `DialoguePlayModeTests` contained one placeholder asserting nothing; replaced with real coverage of
  `DialogueController` start/advance/end-event behaviour.
- Added a GitHub Actions workflow that runs the EditMode suite on push and pull request — the repository had
  no CI at all.

## [10.3.0] - 2026-07-31

### Added
- **New 3D character controller** (`Tools/Move/CharacterController`). [Character Movement Fundamentals](https://github.com/Jan-Ott/CharacterMovementFundamentals) (MIT, ex-commercial asset) is bundled in `ThirdParty/CharacterMovementFundamentals/` as the motor, camera and animation layer, wrapped by Neoxider components. What it brings over the legacy controller: `Slope Limit` with slide-off (no more climbing walls), stair and ledge traversal without losing ground contact, moving platforms, momentum-based movement so external forces (`AddMomentum`) actually push the character, arbitrary gravity direction (wall/ceiling/planet walking), first- **and** third-person cameras with camera collision, and `AnimationControl` for animator-driven characters.
- `NeoCharacterInput` — movement and jump input for CMF controllers through the Neoxider input stack: New Input System or legacy Input Manager (auto-detected, per-read fallback), plus `SetMoveInput`/`SetJumpInput`/`SetRunInput` injection for on-screen joysticks, AI and network drivers. Jump is exposed as a *held* state, which is what CMF needs for variable jump height.
- `NeoCameraInput` — look input for CMF cameras with `GameSettings.MouseSensitivity`, per-axis inversion, `EM.OnPause`/`OnResume` handling and a cursor gate. It never writes `Cursor` state: `CursorLockController` stays the single cursor owner and this component only reads the result. Pointer delta and gamepad stick are converted separately (`NeoLookRate`) so sensitivity does not drift with frame rate.
- `NeoCharacterSprint` — sprint on top of CMF's single-speed walker, scaling from the controller's authored `movementSpeed`, with optional ramp and `On Sprint Start`/`On Sprint Stop` events.
- `NeoCharacterCameraBridge` — keeps movement camera-relative when Cinemachine (or any external rig) drives the camera, by pointing `AdvancedWalkerController.cameraTransform` at the camera actually being rendered through. References no Cinemachine API, so it compiles with or without the package and works with Cinemachine 2 and 3.
- `NeoCharacterNetworkBinding` — Mirror support. CMF simulates on every instance, so remote copies are reduced to `NetworkTransform`-driven proxies: controller, mover, input and camera rig disabled, Rigidbody kinematic. `NetworkTransform.target` and `syncDirection = ClientToServer` are wired automatically. Compiles to a no-op without Mirror. Known gap, documented on the component page: remote proxies do not animate, because CMF's `AnimationControl` reads velocity from the disabled controller.
- `OptionalInputSystemAdapter`/`Bridge`: `ReadJumpHeld()`, `ReadPointerDelta()` and `ReadLookStick()` — the last two let callers treat a frame-accumulated pointer delta and a continuous stick rate differently instead of summing them.
- **Ready-made character prefabs**: `Prefabs/Tools/Character Controller/Character First Person.prefab` and `Character Third Person.prefab`, wired to the Neoxider input layer (Mover + AdvancedWalkerController + NeoCharacterInput + NeoCharacterSprint, camera pivot with NeoCameraInput; third person adds ThirdPersonCameraController, CameraDistanceRaycaster and TurnTowardControllerVelocity). Listed in the **Create Neoxider Object** window under *Presets → Player*, in **GameObject → Neoxider → Presets**, and as a component entry via `NeoCharacterInput`. The superseded `First Person Controller` prefab moved to a new **Legacy** preset category, collapsed by default.
- Sample: **Character Movement Fundamentals** (`Samples~/CharacterMovementFundamentals/`, importable from Package Manager) — **demo scenes only**: the upstream showcase, top-down, gravity-tunnel, planet-walker and click-to-move levels with their environment art, CMF's own controller prefabs and showcase scripts. Everything the shipped presets need (Capguy model + animator + materials, the platform mesh, the physic material, the footstep sounds) lives in the package itself under `ThirdParty/CharacterMovementFundamentals/Art/` and `Audio/Character Controller/`, so no preset depends on the sample being imported.
- **CMF showcase controllers as presets**, rewired from CMF's legacy-Input scripts to `NeoCharacterInput`/`NeoCameraInput`/`NeoCharacterSprint`: `Character Third Person (Animated)`, `Character Top Down (Animated)`, `Character Side Scroller (Animated)`, `Character Click To Move (Animated)` (keeps its mouse-raycast input) in *Presets → Player*, and `Moving Platform` in a new *Presets → Environment* category. The animated ones reference Capguy/audio assets from the imported CMF sample — see the CharacterController README.
- **Character prefabs are drop-in now**: the two capsule prefabs got footstep/jump/land audio (`AudioControl`, CMF clips vendored into `Audio/Character Controller/` — no sample dependency), every character prefab carries exactly one `AudioListener` on its camera (footsteps were silent without a listener in the scene), and mouse-look prefabs (First/Third Person, Third Person/Top Down Animated) carry a `CursorLockController` (lock on start, Escape toggles), so look works in an empty scene without extra wiring.
- 20 new EditMode tests (`CharacterControllerInputTests`) covering the backend decision rule, the look-rate conversion and the gating/injection contracts.

### Changed
- `PlayerController3DPhysics` and `PlayerController3DAnimatorDriver` are now **legacy**, tagged `[LegacyComponent]` with the replacement recorded. They stay listed in the Create window (`hideFromCreateMenu: false`) and the old prefab moved to **Presets → Legacy → First Person Controller**. Nothing is removed: serialized fields, public API and behavior are unchanged, and existing scenes keep working. Their menu entries moved to `Neoxider/Tools/Legacy/*` and their XML docs point at the new module. The 2D controllers are unaffected.
- Docs: new `Docs/Tools/Move/CharacterController/` section with a migration table from `PlayerController3DPhysics`; `Docs/Tools/Move/README.md` updated.
- `THIRD-PARTY-NOTICES.md` now distinguishes bundled code from referenced dependencies, and records the CMF MIT notice plus the three deviations from upstream.

### Fixed
- **Bundled CMF did not compile on Unity 6.** `Sensor.cs` carried a stray `[SerializeField]` on an *enum declaration*. `SerializeField` is declared as `AttributeTargets.Field`, and since Unity 6000.0.3 applying it to anything else is a hard compiler error (CS0592) rather than being ignored — so the whole assembly would have failed to build. The attribute never did anything there and was removed.
- Bundled CMF `Mover.SetVelocity` writes `Rigidbody.linearVelocity` (the Unity 6 name for `velocity`).
- **Look sensitivity was ~10x too fast on the New Input System backend.** `Mouse.delta` is raw pixels, while the legacy `"Mouse X"`/`"Mouse Y"` axes are pixels scaled by the Input Manager's default 0.1 sensitivity. `NeoCameraInput` now normalizes the pointer delta by that factor, so both backends produce the same look speed for the same physical mouse move. The default `Mouse Input Multiplier` is 0.0025 (a quarter of CMF's 0.01), tuned by feel at the default `GameSettings.MouseSensitivity` of 2.
- **Third-person camera collapsed onto the character.** In `Character Third Person.prefab`, `CameraDistanceRaycaster.cameraTargetTransform` pointed at the camera pivot itself, so the obstruction cast had zero direction: the camera distance lerped to 0 on spawn and never recovered. The raycaster now targets a dedicated `CameraPivot/CameraTarget` marker at the design distance (0, 0, -5), and the character's own capsule is in its `ignoreList`. Caught during live Play Mode verification.

- `MultSceneNetworkTests` hard-failed for anyone without the development project's `Assets/Scenes/Mult.unity`; it now skips with a clear message, like the sample smoke tests already did.
- Build settings listed a single disabled scene under the tilde-hidden `Samples~/` path, which Unity cannot import — replaced with the two real project scenes.

The two CMF source patches above are marked with a `NEOXIDER PATCH` comment in place.

## [10.2.0] - 2026-07-22

### Added
- **`CursorLockController` now drives player controllers** (new *Player Control* section): reference `PlayerController3DPhysics` instances (plus an optional off-by-default auto-find fallback) and the controller suspends their look — and optionally movement — while the cursor is visible, restoring them when it locks again. Only what the controller itself suspended is restored, so a player disabled externally (pause, cutscene) is never force-enabled. Runtime API: `RegisterPlayer`/`UnregisterPlayer` (network-spawned players), `DisableLookWhileCursorVisible`/`DisableMovementWhileCursorVisible`.
- **Full cursor opt-out for games with their own cursor system**: per-instance `Manage Cursor` master switch on `CursorLockController` (automatic key/lifecycle/start behavior off; explicit method calls still work) and a static `CursorLockController.GlobalCursorManagement` kill-switch that silently stops every instance from writing cursor state or driving players. `PlayerController3DPhysics.CursorControlEnabled = false` remains the player-side opt-out and covers every cursor path.
- `PlayerController3DPhysics`: public `HasExternalCursorControl()`, `ExternalCursorLockController` and `SetExternalCursorLockController()` for explicit ownership wiring; internal `ShouldHandleEscape()`/`ShouldLockCursorOnStart()` decision seams covered by tests.

### Changed
- **Single Escape owner.** With both components present, only `CursorLockController` handles Escape; `PlayerController3DPhysics` defers automatically (referenced players are auto-bound to the controller in `Awake`, so no manual wiring is needed). The player's `SetCursorLocked` forwards to the owning controller instead of fighting it with direct `Cursor` writes. Scenes without a `CursorLockController` keep the standalone player behavior unchanged.
- `CursorLockController` inspector regrouped (*Cursor Ownership* / *Keys* / *Player Control*); presets set sensible values for the new toggles. Docs for both components describe the ownership rule and a "Choosing a cursor setup" matrix.

### Fixed
- Escape-handling collision between `PlayerController3DPhysics` and `CursorLockController`: both reacted to Esc in the same frame (double toggle — cursor unlocked while look re-enabled, or vice versa). All cursor paths now flow through the single owner, and player look/movement follow the cursor state via the controller's one apply choke point (toggle key, access key, `ShowCursor`, stack release re-apply, lifecycle and scene-load paths included).
- 16 new EditMode regression tests (`CursorLockPlayerControlTests`).

## [10.1.0] - 2026-07-18

### Added
- **Inspector mascot as a live "slime linter".** The banner slime now reflects the inspected component's health: a remembered console-error count (attributed to the component type by parsing stack traces, kept for the session) plus a cheap cached scan for missing object references and NaN/Infinity float fields. Faces: neutral/blink when healthy, worried on missing references, angry on errors/invalid numbers, a brief "surprised" reaction the moment a new error appears (auto-opening a compact issue list with a **Clear** action), and a "watching" face in Play Mode. The spectrum half-frame matches the mood (amber shimmer / red pulse) and flows faster in a healthy Play Mode.
- **Poke the slime.** Clicking the mascot plays a springy bounce and a startled face — a bit of life in the inspector. The issue-count badge on the chip still opens/closes the problem list.
- Performance: the console hook is O(1) per error (dedup + type cap), the validation scan runs only for inspected objects, is throttled (~2 s) and property-capped, and remembered errors persist via `SessionState` (survive domain reloads, reset with the editor).
- **Abilities go NoCode.** `AbilityNoCodeAction` bridge (cast/grant/revoke/level/modifier/damage/heal from UnityEvents, uniform with the Level/Rpg/Progression bridges), `AbilityAutoCaster` (Survivor-style auto-cast with nearest-target lock-on, interval mode and failure backoff) and `AbilityCooldownSource` (poll-friendly `CooldownNormalized`/`SecondsRemaining` for `SetProgress`/`NoCodeBindText` bindings). `AbilityUnitBehaviour.ApplyHeal(float)` added, mirroring the heal effect op. The Abilities quick start is now genuinely no-code.
- Inspector mascot Play Mode gaze: the "watching" face turns toward the Game view (mirrors horizontally when the Game view sits left of the inspector).
- New **Trigger Cube** preset in the GameObject creation menu; UI presets created without a Canvas now auto-create Canvas + EventSystem; 3D presets spawn at the Scene view pivot (honoring "Create Objects at Origin").
- **Abilities NoCode demo** (`Samples~/Demo/Scenes/Abilities/AbilitiesNoCodeDemo.unity`) — live-verified showcase of the NoCode trio: auto-cast at the training dummy, bridge-driven grant/cast/heal buttons, cooldown bar via `AbilityCooldownSource` + `SetProgress`, health bindings, and +XP → level → bigger zap damage through `LevelNoCodeAction` + `SetAbilityLevel`.
- **Scene-authored NoCode demos** — `NoCodeBindingDemo` (one reactive `Counter.Value` drives three inspector-wired bindings: raw text, formatted text and a progress fill) and `NoCodeActionsDemo` (Level/Progression/Quest/Rpg wired in the inspector — buttons trigger the module action, module events drive the readout). No runtime construction: everything is authored in the scene, matching the reference example scenes.

### Changed
- **All editor menus consolidated under a single top-level `Neoxider` menu** — Windows (Ability Designer, Dialogue Editor, Prefab To Sprite, Create Neoxider Object), Tools (Scene Saver, Texture Max Size, Save Project Zip, missing-script utilities, Fix Editor Assembly References), Network, Samples, Settings, Visual Settings and Health Check; old `Tools/Neoxider`, `Tools/UIKit` and `Window/Neoxider` paths removed. Docs updated to the new paths.
- GameObject/Neoxider creation menu tidied: flat Presets group with separators, validated Create/Sort Scene Hierarchy entries (disabled in prefab mode / with nothing to sort), consistent naming, no creation log spam.
- Clicking the mascot no longer collapses the other components (the relayout made the inspector jump); it is now a pure animation. A future click action is left as a TODO.
- **"Create Neoxider Object" window** redesigned — gradient banner, category pills, folder icons and count badges, and distinct raised component rows (fixed the low-contrast rows that blended into the dark window background). The catalogue now includes the previously-absent Abilities module and every `[CreateFromMenu("Neoxider/…")]` component.

### Fixed
- Mascot error memory now mirrors the Unity Console: clearing it (Clear button, Clear on Play, or any console wipe) calms the mascot within half a second — event-driven when the Console window is visible, throttled O(1) count-poll backstop when it is hidden. NUnit frames are excluded from error attribution.
- Spectrum half-frame corners no longer look crooked: the corner arcs swept the wrong quadrants; they now follow the card radius exactly and are drawn as mitred anti-aliased arcs.
- GameObject creation entries now register undo, align under the right-click context target, keep prefab links (`PrefabUtility.InstantiatePrefab`), give unique sibling names, work inside prefab stages, and respect the container list configured in Neoxider settings without duplicating inactive containers.
- `AutoBuildName` renames only Android `.apk`/`.aab` outputs — Windows/WebGL and other build targets are untouched (renaming a Windows `.exe` broke its `_Data` folder link); colliding artifacts get a unique suffix instead of being deleted, and mixed path separators no longer confuse the same-path guard.
- Review pass over the new code (8 finder angles, 21 confirmed findings, all fixed): `AbilityAutoCaster` searched targets around its own transform instead of the caster unit (silent mis-targeting on manager objects) and re-sorted/re-queried every frame with allocations; `AbilityNoCodeAction` could leak its cast-failure capture listener when a user handler threw; GameObject creation multi-selection created one instance per selected object; every created object got a spurious " (1)" suffix; the "Create Objects at Origin" preference read a dead Unity-5-era EditorPrefs key; `Sort Scene Hierarchy` reindexed nested/additive-scene containers and its menu validator deep-scanned scenes on every context-menu open; Canvas-rooted presets no longer nest under another Canvas and UI reroute prefers screen-space canvases; EditMode ability tests pin the hub singleton so an open user scene cannot hijack registration; inspector frame drawing and the anchor-restore loop no longer allocate or force-repaint per tick.
- Package-wide module audit (every module reviewed): `ResourcePoolModel.Increase` now returns the delta actually applied (headroom-aware), so `HealthComponent.OnHeal` reports the real heal instead of an overheal; sample `FakeLoad` initializes its UnityEvents inline so a runtime-spawned instance is subscribable; the Progression demo no longer self-destructs in Play Mode (its NoCode bridge resolved the `ProgressionManager` singleton via `AddComponent`, whose duplicate guard killed the demo object); plus assorted per-module correctness fixes, ~48 documentation pages corrected against code, and NoCode-bridge coverage added to the Level, Progression, Quest and StateMachine demo controllers. Regression tests added across Core, Audio, Grid, Bonus, UI and NoCode (EditMode total 978, PlayMode 109).
- **Unity 6.5 compatibility**: the inspector health backend no longer casts `EntityId` to `int` (obsolete-as-error / CS0619 on 6.5) — it keys its validation cache by `EntityId` on 6.5 (`int` on 6.0–6.4 via a conditional alias) and detects dangling references with `EntityId.IsValid()`. Fixes the package failing to compile in Unity 6.5 projects.
- Card views keep their artwork aspect ratio in adaptive UI (`Image.preserveAspect`) instead of stretching, and UI cards hover/return by `anchoredPosition` (RectTransform-aware) so a scaled or camera-space canvas no longer warps their size or position.
- Under `MIRROR`, exact-type Neo inspectors for `NetworkBehaviour`-conditional components (physics player controllers, `NeoNetworkComponent` subclasses) now win over Mirror's `[CustomEditor(typeof(NetworkBehaviour), true)]`, restoring the Neo styling on those components.
- `RpgNoCodeAction` exposes public `OnSuccess` / `OnFailed` / `OnResultMessage` accessors, matching the other NoCode bridges so code can subscribe, not only inspector wiring.
- `RpgCharacter` inspector no longer shows doubled section titles: its editor drew its own section foldouts (Template, Resources, Progression…) and then re-drew each field's built-in `[Header]`. Fields are now drawn through a `DrawPropertyFieldNoHeader` helper that strips the `[Header]` decorator, so each title appears once. The header-suppression in the shared `CustomEditorBase` was also extended to cover array/generic fields (previously they returned early and doubled their header).

## [10.0.1] - 2026-07-18

Patch release: three audit-fix cycles over the whole package — 52 independently verified correctness bugs fixed, plus a consistency pass over asmdefs, docs and package metadata.

### Fixed
- **Abilities**: nested event-driven effects no longer corrupt shared scratch lists (area damage + thorns-style reactions now hit every target; stacked reactive modifiers all fire; shields survive re-entrant damage). Team override survives `UnitTemplate.ApplyTo`; piercing projectiles hit distinct units instead of re-hitting the first; per-hit RNG is decorrelated; spawn ops fall back to the cast point instead of world origin; play-mode teardown no longer resurrects the system hub; unrealized projectile casts expire instead of leaking; caster grants are order-independent; `max_health_bonus` / `max_mana_bonus` now actually resize resource pools.
- **Core/Level**: `GetXpToNextLevel` off-by-one on all curve types; `HealthComponent.Load` no longer clamped by `MaxDecreaseAmount` (and no longer fires death events while loading); `Increase()` on unlimited pools no longer computes negative headroom; `TextLevel` resubscribes after disable/enable; removed per-frame forced reactive sync; `SetLevel` overflow guard; weighted-random zero-weight fallback; `StringExtension.Truncate` small-length guard.
- **Tools**: `Timer` stop/start async race; `Spawner.Clear` double-release and stale delayed-destroy handles; `KeyboardMover` fixed-update compensation; `SwipeController` swapped start/end positions; `TimerObject` RealTime mode persists `isActive`; `MouseInputManager` picks the nearest hit; mouse movers guard `deltaTime == 0`; pooled-component cache and additive-scene pool lifetime leaks.
- **Save/Quest/Audio/Shop/Network**: global `Save()` no longer deletes data of fields with `autoSaveOnQuit` off; file saves flush on pause/focus loss (mobile); `QuestManager` tolerates registry mutation from completion handlers and migrates saved states when objectives are added; music track timer is pause-aware and unscaled; networked wallet rate limit is per-connection; `GlobalSave` initializes on first launch; `NetworkReactiveSync` validates the reactive property type; saveable behaviours persist on disable.
- **Bonus/Cards/NoCode/UI**: slot line evaluation only pays contiguous runs; `SetRewardAvailableNow` makes rewards claimable immediately; `SpinController` window-size bounds; `DeckModel.Draw` preserves duplicate-card order; `NoCodeFormattedText` retries late-spawned sources; `LineRoulett` honors its serialized slow-down time; `FakeLoad` static state resets between plays.
- **Survivor demo**: enemies freeze during the level-up pause; upgrade-granted projectile abilities get archetypes; pooled templates are cleaned across reloads; Shop demo guards a missing wallet.
- TMP smeared-text fix follow-up: obsolete `enableWordWrapping` replaced in sample UI.

### Changed
- 30 new regression tests (EditMode total 889).
- Dead asmdef references removed (`Neo.Tools`, `Neo.Network.Core`, an old Odin GUID); PlayMode test assembly renamed to `Neo.Tests.PlayMode` to avoid consumer name collisions; deprecated `com.unity.textmeshpro` dependency dropped (TMP ships in `com.unity.ugui` on Unity 6).
- Docs: pages matched to real APIs (SaveProvider static facade, StatePredicate family, StateMachineEvaluationContext, QuestStatus values), hidden-sample paths corrected, ~120 generator-artifact rows purged from 61 pages, `[NeoDoc]` added to the last 5 undocumented components (+ new AuraWeapon page).
- Inspector polish: the property block is a real rounded card (accent tint + 1px edge); the segmented left rainbow line is replaced by a continuous HSV spectrum half-frame hugging the card (rounded corners, fading arms, seamless animated hue); the banner mascot is drawn as a close-up that nearly fills its chip.

## [10.0.0] - 2026-07-18

Major release. Headline: a new data-driven combat core (`Neo.Abilities`) that supersedes `Neo.Rpg`, a redesigned inspector, and a modular survivor demo built on the new system.

### Added
- **`Neo.Abilities` module — a Dota-derived, data-driven ability/modifier system.** Author new abilities entirely in data (ScriptableObjects), no code required. Pure-C# deterministic domain plus Unity wrappers:
  - Units with teams and resource pools (reuses `Neo.Core.Resources.ResourcePoolModel`), and an **open property registry** aggregated `base -> +Add -> xMul -> Max`.
  - **Modifiers** unify buffs/debuffs/DoTs/auras/shields/stuns: typed property contributions, boolean states (any-true-wins), interval ticks with guaranteed expiry, stack policies (Independent/Refresh/Stack + per-stack scaling), and declarative event reactions (e.g. absorb-on-take-damage).
  - **Cast pipeline** with cost/cooldown/charges/targeting/team-filter/range validation, instant or homing-projectile delivery, area effects, and an open effect-op registry (damage/heal/apply-modifier/remove-modifier/dispel/resource/spawn) plus motion/utility atoms: **knockback, pull, teleport** (routed through the `IAbilityWorldAdapter.TryMoveUnit` seam so navigation/physics stay pluggable), **execute** (health-fraction damage over missing/max/current HP), and **chain** (deterministic nearest-first bounces with per-hop falloff).
  - **Leveled values and specials**: every effect amount is a `LeveledValue` (per-level array plus property scaling from caster or target, driven by ability or unit level); named `Specials` on an `AbilityDefinition` are reusable Dota-style `%value%` entries referenced by key from any effect node.
  - **Live combat properties** read from the property registry at damage time by `DamageService`: `crit_chance` / `crit_multiplier`, `lifesteal_percent`, and physical-only `evasion_chance` — modifiers change them mid-fight with no extra wiring.
  - **Receipt-driven**: everything observable flows through one event bus; casts carry deterministic seeds and serializable ids (authority-ready seams for a future Mirror bridge).
  - Authoring assets: `AbilityDefinition`, `ModifierDefinition`, `UnitTemplate`, `AbilityLibrary`. Scene components: `AbilitySystemBehaviour`, `AbilityUnitBehaviour`, `AbilityCasterBehaviour`, `AbilityProjectileBehaviour`.
  - **UI Toolkit "Ability Designer"** window (`Tools -> Neoxider -> Ability Designer`) plus SO inspectors.
  - Docs under `Docs/Abilities/` and 93 EditMode tests.
  - `Neo.Rpg` is **superseded by `Neo.Abilities`** and slated for removal in a later release.
- **Modular Survivor demo** (`Samples/Demo/Scenes/SurvivorDemo.unity`) — a complete Vampire-Survivors-style game assembled entirely from a single `SurvivorConfig` data asset on top of `Neo.Abilities` + Core level/resource systems (waves, auto-cast weapons, XP, level-up upgrade cards, escalating difficulty). Swap the data to clip a different game. Bright uGUI, no IMGUI.
- **Demo-shell scenes** — eight module demos rebuilt as bright, self-explaining uGUI scenes on a shared `NeoDemoShell` frame (procedural sprites, header, content card, action log; zero imported assets): Audio, Save, Settings, LevelFlow, StateMachine, NoCode binding, Parallax, and Quest.
- `AbilitySystemBehaviour.Paused` to freeze the ability tick for menus/level-up screens.
- `AbilityUnitBehaviour.SetTemplate` / `SetTeamOverride` for runtime/pooled unit spawning.
- `ResourcePoolModel.SetCurrent(id, value)` — a direct clamped setter (loads/revives/scripted adjustments) that bypasses the heal gate.

### Changed
- **Redesigned the Neoxider custom inspector** with a modern theme (`NeoInspectorTheme`): gradient hero banner, themed rounded section cards with accent rails, gradient action buttons, a readable version pill, a themed property-panel card, and a new avatar logo with an occasional eye-blink. Dark and light editor skins supported. All functionality preserved; still IMGUI.
- **Package-wide comment cleanup**: comments reduced to XML `<summary>` docs plus `WHY:` / `TODO:` / `HACK:` markers, all in English; banner/section dividers and comments restating the code removed across runtime and editor sources.

### Fixed
- **Re-imported the TMP essential resources** shipped with the project (`Assets/TextMesh Pro`): the stale `LiberationSans SDF` font asset and materials rendered smeared/blurry SDF text in Unity 6; demo scenes now show crisp text.
- `PoolManager` threw a `NullReferenceException` when created at runtime (its preconfigured-pools list was null) — now null-guarded, so a runtime-instantiated `PoolManager` works.
- `AbilitySystem.Revive` could not restore HP after a lethal-damage death (the resource pool's heal-from-zero gate); it now sets health directly and never revives at 0 HP.
- `UnitTemplate` pool regeneration never fired (missing regen interval); templates with `RegenPerSecond > 0` now tick.

## [9.13.1] - 2026-07-17

### Fixed
- **Shop / purchase failure event never fired when out of money (solo mode):** `Shop.Buy` resolved the wallet through `IMoneySpendAuthority.CanConfirmSpendNow`, which returns false for *both* insufficient funds and pending-server-confirmation. Insufficient funds were misread as "awaiting server authority", so `Buy` returned silently and `OnPurchaseFailed` / `OnPurchaseFailedId` never fired for a non-networked wallet. Now affordability is checked first (via `IMoneyCanSpend`): a shortfall reports as a normal failed purchase (and, on a networked client, no longer sends a doomed spend command to the server), while a genuine pending-server case still short-circuits without a failure event. Found by the real Unity Test Runner (735-test suite), not the standalone compile check.
- **Shop / EquipmentManager required a visual slot to equip:** `Equip` / `Unequip` bailed out when a category had no `CategorySlot`, so state-only equipment (driven by `ShopVariantsPanel` or headless logic) silently did nothing. Equipment state is now always tracked, persisted, and broadcast; the `CategorySlot` is optional and only drives the sprite target. Persisted slotless categories are restored on load from the item catalog. `Unequip` on an already-empty slotless category is a no-op (no spurious `OnEquipChanged`).

### Tests
- `EquipmentManagerTests`: slotless equip/unequip state tracking + event, and the empty-category no-op. The existing `ShopAffordabilityTests.Buy_InsufficientFunds_FiresFailedAndDoesNotOwn` now guards the Money regression. Fixed two of the new 9.11.0 tests that only surfaced under the real runner (a grid default-content assumption and an editor-folder scan in `ModulePrinciplesTests`).

## [9.13.0] - 2026-07-17

### Added
- **Bonus / one-call economy spin:** `SpinController.StartEconomySpin()` builds the whole outcome from the assigned `SlotEconomyDefinition` (weighted pick per cell honoring the per-machine overrides, then the special/wild conversion along each active payline), queues it via `ForceNextOutcome`, and starts the spin. The building blocks are public: `BuildEconomyOutcomeMatrix()` (+ a deterministic `Func<int>`-picker overload for tests/replays/server outcomes) and `EvaluateActivePaylinesWithEconomy()` returning one `LineResult` per active payline of the settled grid. Before this the economy asset could only be wired by hand-rolling the pick → special-rule → force-outcome → evaluate chain in game code.

### Tests
- EditMode: `SpinControllerEconomyTests` — weighted fill, empty-economy guard, deterministic special-line conversion on the active payline (off-line cells untouched), and loss reporting on an unsettled grid.

## [9.12.1] - 2026-07-17

### Fixed
- **Tools / SpineController:** the `[NeoDoc]` attribute was declared twice on the class. `NeoDocAttribute` does not allow multiples, so any project with Spine installed (`SPINE_UNITY` defined) failed to compile with CS0579; without Spine the file is compiled out, which is why the error stayed invisible.

### Editor tooling
- **PackageHealthCheck** (`Tools → Neoxider → Package Health Check`) now also catches the two doc-drift classes that path checking alone could never see: (1) public, non-abstract `MonoBehaviour`/`ScriptableObject` types in `Neo.*` runtime assemblies that carry no `[NeoDoc]` attribute at all (the 9.8.2 audit found 39 such gaps by hand), and (2) dead relative `.md` links inside `Docs/` (URL-encoded paths supported; six dead links shipped in 9.8.1 alone). Obsolete types and editor/test/demo assemblies are excluded.

## [9.12.0] - 2026-07-17

### Added
- **GridSystem/Dice / plain C# `DiceBoard` core:** all dice placement/merge logic moved out of the scene component into `Neo.GridSystem.Dice.DiceBoard` — constructible over any generated `FieldGenerator` (tests, server/replay logic, custom loops), with C# events `BoardChanged`/`MergesResolved`. `DiceBoardService` keeps the identical scene API, forwards Inspector settings into the core (exposed via the new `Board` property), and re-raises the core events as its UnityEvents. Closes the remaining GridSystem TODO item.
- **Shop / `ShopListViewCategoryBar` auto categories:** `Build Categories From Shop` fills the bar from the Shop catalog on enable — one entry per distinct `ShopItemData.Category` (first-seen order) with an optional show-all entry (`Include All Entry` / `All Entry Name`); `BuildCategoriesFromShop()` re-runs it after catalog swaps.

### Tests
- EditMode: `DiceBoardCoreTests` (placement, occupied-cell rejection, merge with content cap, single-notification contract, clear, service→core settings/event forwarding), `ShopListViewCategoryBarTests` (selection drives the list view, auto-built categories with and without the All entry).

## [9.11.0] - 2026-07-17

### Added
- **Bonus / per-machine slot weights (`SlotSymbolWeightOverrides`):** `SpinController` now takes an optional `SlotEconomyDefinition` (`Economy`) plus a local weight table layered over it — enable the override to change drop weights for one machine without touching the shared asset. Entries match symbols by id (reordering/extending the definition's symbol list is safe; unmatched symbols fall back to their definition weight), weight `0` disables a symbol, negatives clamp to `0`. New `PickEconomySymbolId()` picks through the override; Inspector `⋮` menu **Normalize Weights** rescales all positive local weights to a total of `1`. `SlotEconomyDefinition.PickWeightedId` gained weight-selector and deterministic-roll overloads for tests/replays/server outcomes.
- **UI / `CategoryBar` + `CategoryBarItem`:** reusable horizontal/tab category bar that owns selection state — initial selection, select by index/id, `Next`/`Prev` with configurable wrap, disabled entries, Inspector-authored or runtime `SetCategories(...)` lists. Item views are authored children or spawned from a prefab; the shared selection marker is re-parented onto the selected item with an offset, never resizing or repositioning authored graphics. Reports through `OnCategorySelected(int)` / `OnCategoryIdSelected(string)` and has no Shop dependency; the optional `ShopListViewCategoryBar` adapter (in `Neo.Shop`) drives a `ShopListView` from the bar.
- **Shop / reactive affordability:** public `Shop.CanAfford(item/id)` — owned and free items are always affordable; priced items query the same wallet the purchase would use (per-item `Currency Override Save Key` included). New `Shop.ResolveCurrencyMoney(itemId)` exposes that wallet for balance subscriptions; new optional `IMoneyCanSpend` interface lets custom wallets answer affordability (`Money` implements it). `ButtonPrice` gained an explicit `Unaffordable` state (optional visual group, label, `OnUnaffordable` event, `CurrentType` accessor) — old prefabs keep showing the Buy visuals. New `ShopPurchaseButtonView` subscribes to shop refreshes and wallet balance while enabled, drives `ButtonPrice` (Buy/Select/Selected/Unaffordable) and `Button.interactable` immediately, and re-subscribes on slot rebinding; it unsubscribes safely on disable.
- **Shop / `ShopVariantsPanel` + `ShopVariantStateView`:** furniture/equipment variants panel over `ShopListView`/`ShopItem` with optional `EquipmentManager`: renders unowned/owned/equipped per slot through the small `IShopVariantView` interface (visuals stay prefab-driven), equips after successful purchase, forwards Shop selection into the equipment manager, refreshes on ownership/equipment/list changes, and supports an empty/unequip control (`Unequip()`). `ShopListView` exposes `Views` and `ButtonAction`.
- **GridSystem / `GridPlacementService` + `GridPlacementRequest`:** plain-C# rule-driven placement over the `FieldGenerator` placement API — `RequireEnabled`/`RequireWalkable`/`RequireUnoccupied`, custom `CellPredicate`, `GridOverwritePolicy` (Reject/Overwrite), `Notify` toggle, single-cell factory `GridPlacementRequest.Single(...)`, atomic multi-cell writes with readable failure reasons.

### Fixed
- **Shop / currency resolution before `Start`:** `Shop` now lazily resolves its default wallet on first use, so `CanAfford`/`Buy` called before `Start` (e.g. from a view's `OnEnable`) use the configured `moneySpendSource` instead of falling back to `Money.I`.

### Repo
- Removed accidentally committed dev debris from version control (`TestRunner.cs`, `test_extensions*.cs`, `debug.log`, `memory.db`, `msp_server.log`, `replay_pid*.log`) and extended `.gitignore` so it cannot return.

### Docs
- New pages: `UI/CategoryBar.md`, `Shop/ShopListViewCategoryBar.md`, `Shop/ShopPurchaseButtonView.md`, `Shop/ShopVariantsPanel.md`, `GridSystem/GridPlacementService.md`; updated `SlotEconomyDefinition.md`, `SpinController.md`, `ButtonPrice.md`, `Shop.md`, and the Shop/UI/GridSystem READMEs.

### Tests
- EditMode coverage: `SlotSymbolWeightOverridesTests` (disabled override, reordered/changed symbol lists, zero/negative weights, normalization, deterministic weighted selection), `CategoryBarTests` (initial/runtime selection, wrap and non-wrap navigation, disabled entries, runtime category lists, events), `ShopAffordabilityTests` (balance changes, multi-currency wallets, owned/free items, failed purchases, `ButtonPrice` state rules, `ShopPurchaseButtonView` subscription/rebinding/lifecycle), `ShopVariantsPanelTests` (state rendering, buy-then-equip, failed purchase, unequip, `EquipmentManager` bridge), `GridPlacementServiceTests` (rule toggles, predicate, overwrite policy, atomic footprints, notifications).

## [9.10.0] - 2026-07-16

### Added
- **Cards / `CardSpriteNameParser`** (runtime, `Neo.Cards`): parses card sprite/file names into suit, rank, card back, or joker. Understands English and Russian tokens (`ace_of_spades`, `дама_червы`), numeric ranks 2–14 (`hearts_02`, `spades_14`), compact forms (`AS`, `KH`, `10c`), and common separators. `GetCanonicalName(suit, rank)` returns the recommended file name (`hearts_02` … `spades_14`), so the same convention works for editor auto-fill and runtime sprite loading.
- **Cards / DeckConfig inspector auto-fill:** new "Auto-Fill From Folder..." button assigns all four suit lists, the back sprite, and both jokers from sprite names in a selected `Assets/` folder (multi-sprite sheets supported). Suit slots are cleared first, so the folder is the source of truth; unrecognized and conflicting names are reported in a summary dialog and the console.

### Changed
- **Cards / DeckConfig validation:** a missing back sprite is now a warning instead of an error — the deck generates and face sprites resolve normally; only face-down display is unavailable.

### Fixed
- **Cards / DeckConfigEditor deck-type casts:** `DeckType` was cast from `enumValueIndex` (0/1/2) instead of the stored enum value (36/52/54), so a `Standard36` sprite deck was previewed and validated as 13 cards per suit instead of 9, and the 54-card joker requirement never triggered from the correct enum member.

### Tests
- EditMode coverage for `CardSpriteNameParser`: standard/compact/Russian names, back and joker detection, invalid names, and canonical-name formatting (also verified standalone against .NET with 37 passing cases).

## [9.9.0] - 2026-07-15

### Added
- **Tools / TimerObject:** public `Tick(float deltaTime)` advances a timer deterministically through the same active-state, pause, time-scale, update-interval, event, milestone, completion, and looping pipeline used by Unity's frame update. This supports tests, replay/server clocks, and custom update loops without reflection or duplicate timer logic.
- **UI / AnimationFly request overrides:** individual flights can now override `Duration`, `SpeedMultiplier`, and `DelayBetweenItems`, copy their rendered UI size from a `RectTransform` with `UiSizeSource` (or use an explicit `UiSize`), and tween from `ScaleMultiplier` to `EndScaleMultiplier` with an optional `ScaleEase`.

### Fixed
- **Tools / TimerObject inheritance:** Unity lifecycle hooks (`Awake`, `OnEnable`, `Update`, `OnDisable`, and `OnValidate`) are now `protected virtual`. Derived timers such as `CooldownReward` reliably inherit the frame update in player builds and may extend lifecycle behavior by overriding and calling `base`. Previously the private base `Update` could leave a derived countdown frozen at `00:00`.
- **Bonus / CooldownReward:** validation now overrides and chains to `TimerObject.OnValidate`, preserving both cooldown-specific synchronization and base timer validation.
- **UI / AnimationFly pooling:** pooled visuals now restore their original scale, rotation, and `RectTransform.sizeDelta` before reuse, preventing size or scale compounding across repeated reward flights.
- **Bonus / CooldownReward runtime creation:** dynamically added components now initialize their completion event before subscribing, avoiding a null event when configured entirely from code.

### Tests
- Added deterministic EditMode coverage for `TimerObject.Tick`, API-contract coverage for all inheritable lifecycle hooks, and a PlayMode regression proving that `CooldownReward` advances through the inherited Unity update without a project-side driver.
- Added EditMode and PlayMode coverage for request-level UI sizing, duration/speed overrides, end-scale tweening, arrival ordering, and pooling-safe visual reuse.

## [9.8.2] - 2026-07-03

### Fixed
- **Missing `[NeoDoc]` attributes:** 32 public `MonoBehaviour`/`ScriptableObject` types (e.g. `LevelComponent`, `ShopItemData`, `RpgAttackDefinition`, `StatusEffectDefinition`, `SaveProviderSettings`, `InteractionRayProvider`, and 26 others) had a matching `.md` page in `Docs/` but no `[NeoDoc(...)]` attribute pointing at it — the Inspector showed "No documentation linked" even though the page existed. All 32 fixed. `PackageHealthCheck` only verified that *existing* `[NeoDoc]` paths resolve; it didn't catch a component missing the attribute entirely — this class of bug was invisible to automation until inspected manually.
- **Duplicate auto-generated stub docs:** 4 `.md` pages (`Cards/Config/DeckConfig.md`, `Core/Level/Data/LevelCurveDefinition.md`, `Rpg/Data/RpgAttackDefinition.md`, `Rpg/Data/RpgAttackPreset.md`) were low-quality auto-generated field dumps (including bogus "fields" like literal `true`/`100f` picked up by whatever tool scaffolded them) duplicating a better hand-written page elsewhere for the same class. Deleted; `[NeoDoc]` now points at the real page. Fixed a doc link in `Core/README.md` left dangling by the deletion.
- **7 more classes had zero doc coverage** (no `[NeoDoc]`, no matching page): `GridCellMarker`, `NoCodeFloatBindingBehaviour` (+ its embedded `ComponentFloatBinding`), `InventoryDatabase`, `InventoryInitialStateData`, `InventoryItemStateBehaviour`, `PageId`, `NeoDebugOverlay`. Wrote real pages for all 7 and linked them.

### Notes
- 21 of the 32 fixed classes above still only have an auto-generated field-dump page as their *only* doc (no hand-written alternative existed to fall back on) — the link now resolves and something real is shown, but the prose quality is low. Flagged as follow-up debt, not rewritten in this pass to keep scope bounded.

## [9.8.1] - 2026-07-03

### Added
- **`link.xml`** — preserves `Neo.*` assemblies and `Assembly-CSharp` from IL2CPP managed code stripping. `NeoCondition`, `ComponentFloatBinding`, `[SaveField]`, `NetworkPropertySync`, and `NetworkReactiveSync` all resolve members by name via reflection (including private, non-serialized fields) — under IL2CPP that member can be legally stripped if nothing else references it, silently breaking the NoCode binding in a release build while working fine in the Editor. See `Docs/IL2CPP.md` for the full explanation and the escape hatch for custom asmdef setups.
- **`THIRD-PARTY-NOTICES.md`** — lists every optional/required third-party dependency (UniTask, DOTween, Mirror, Spine, Odin, MarkdownRenderer), why it's referenced, and where to find its license. None of them are bundled inside the package.

### Changed
- **Minimum Unity version raised to `6000.0`** (was `2022.1`). The package is now developed and validated against Unity 6 only; projects on Unity 2022 LTS should stay on the last `9.7.x` release.

### Fixed
- **`Docs/Tools/Spawner/Spawner.md`** described deny zones (`_denyAreas`/`_denyAreas2D`, `IsPositionAllowed`) as a "Planned (TODO)" feature — they shipped in `9.7.0`. Doc rewritten to match the current API (also documents `Spawn Area`, `Max Waves`, `Spawn On Awake`, `Parent Transform`, which were undocumented).
- **`Docs/StateMachine/README.md`** had a malformed module table (missing separator row, a stray bullet line instead of the README's own entry) that broke rendering.
- Six dead internal doc links fixed (`Bonus/Slot/*` cross-links and `GridSystem/Dice/DiceBoardService.md` → `Merge/README.md`), left over from the RU→EN docs folder reorganization.
- **`Samples~/NeoxiderPages/Runtime/API/UIKitAPI.cs`** — removed unconditional `Debug.Log` calls on every `G.Pause/GoMenu/Start/Restart/Win/Lose/End` call. This is a reference-implementation sample meant to be copied into real projects; the logging was leftover debug instrumentation with no gate, so every game using the pattern verbatim would spam the console on every state transition.

## [9.8.0] - 2026-07-03

### Changed
- **Docs are English-only.** The `Docs/` (RU) tree has been removed and `DocsEn/` renamed to `Docs/`; `[NeoDoc(...)]` attributes and all package/README links now point at the single English tree. `PackageHealthCheck` was rewritten to verify every `[NeoDoc]` path resolves under `Docs/` instead of checking RU/EN parity.
- **README rewritten** around four audiences: NoCode beginners, professional C# API users, multiplayer, and AI-agent development. Added `Docs/NoCode/GettingStarted.md`, a beginner-facing tour of every no-code building block.
- `DOCUMENTATION.md` / `DOCUMENTATION_GUIDELINES.md` rewritten in English (they previously mandated Russian for `.md` pages).
- Removed the root `README_RU.md`; the repo now ships a single English README.
- Filled in genuinely missing `Network/*` documentation pages (`NeoNetworkComponent`, `NetworkSingleton`, `NeoNetworkManager`, `NetworkOwnerFilter`, `NetworkActionRelay`, `NetworkPropertySync`, `NoCode_Network_Spec`, `Lobby`, `Multiplayer_Guide`) and `NeoxiderPages/PM.md`, which were English placeholder stubs.

### Fixed
- **Save / FileSaveProvider:** removed the GC finalizer that called `Save()` (and therefore `JsonUtility`, a main-thread-only Unity API) from the finalizer thread.
- **Save / SaveManager:** `OnApplicationQuit` now calls `SaveProvider.Save()` after writing, so file-backed providers actually flush to disk on quit instead of relying on the removed finalizer.
- **Shop.cs:** translated two legacy-field tooltips from Russian to English.
- **Cards/Editor/DeckConfigEditor.cs:** fixed a mojibake character in a HelpBox warning string.
- **StateMachine.ChangeState:** `OnExit` now runs on the previous state before `CurrentState` is reassigned, so exit-handlers and re-entrant `ChangeState` calls see consistent state.
- **Network/NeoNetworkComponent.cs:** removed a stray Russian word left in an XML doc comment (last remaining Cyrillic string in the runtime/editor code).

### Tests
- No behavior changes to test coverage this release; full EditMode (631 tests) and PlayMode (106 tests) suites verified green after the docs/audit pass.

## [9.7.1] - 2026-07-02

### Fixed
- **Naming:** new 9.7.0 public serialized fields renamed to PascalCase per package convention (`ShopCategorySelector.Category`: `Id/DisplayName/Icon`; `SlotEconomyDefinition.Symbol`: `Name/Id/MoneyReward/BonusReward/IsSpecial/Weight`; `EquipmentManager.CategorySlot`: `CategoryId/SpriteTarget/ImageTarget/ApplyNativeSize/DefaultItemId`). `[FormerlySerializedAs]` keeps existing scene/asset data intact.
- **Tests:** `NetworkRateLimitTests` no longer triggers Mirror's "requires a NetworkIdentity" `OnValidate` error — the probe object now carries a `NetworkIdentity`.

### Tests
- Edit-mode coverage for the 9.7.0 additions: `SlotEconomyDefinition` (payline evaluation, special-line conversion, weighted picker), `EquipmentManager` (equip/unequip/toggle, category replacement, slot visuals), `ShopCategorySelector` (wrap-around cycling, select-by-id, empty-list safety), `Spawner` deny zones (3D/2D rejection, null entries).

## [9.7.0] - 2026-07-02

### Added
- **Shop / ShopCategorySelector:** NoCode category pill with prev/next arrows cycling a serialized category list into `ShopListView.SetCategory` — complements `ShopCategoryButton` for shops browsed sequentially (pattern extracted from a shipped dress-up game).
- **Shop / Equipment (new):** `EquipmentManager` + `EquipItemDefinition` — multi-category dress-up/skins: one item per category, sprite applied to a `SpriteRenderer`/`Image` slot (optional `SetNativeSize`), worn set persisted via `SaveProvider`, `OnEquipChanged` event, `EquipById/Unequip/ToggleById` NoCode API. Pairs with `Shop` ownership for buy-then-wear flows.
- **Bonus / SlotEconomyDefinition:** slot-machine economy SO — weighted symbol table (money/bonus payouts, special flag), `PickWeightedId()`, `ApplySpecialRule()` (one special converts the payline) and `EvaluateLine()` returning a typed `LineResult`. Removes the per-game hand-rolled economy layer over `SpinController`.
- **Bonus / ResourceRegen:** one-component regenerating resource — couples `CooldownReward` (auto-claim forced on) with a capped `Money` wallet and an optional `TimeToText` countdown (shows 0 while full).
- **Network / NetworkReactiveSync:** NoCode replication for `ReactivePropertyFloat/Int/Bool` — inspector counterpart of `NetworkReactivePropertyBridge`; multiplayer wallets/score/HP without hand-written SyncVar code. Inert without Mirror.
- **Network / NetworkPlayerName:** replicated player nickname (trimmed + length-capped server-side, rate-limited command, `OnNameChanged` for TMP labels). Works locally without Mirror.
- **Network / NeoNetworkDiscovery Quick Play:** `QuickPlay()` — one-button LAN flow: auto-join the first server found, or host after `Host If None Found After` seconds; `OnQuickPlayResolved(bool becameHost)`.
- **Network / NetworkEventDispatcher payloads:** `DispatchGlobalInt/Float/String` + matching UnityEvents (rate-limited, authority-checked like the parameterless event).
- **Network / NeoNetworkComponent:** per-connection `RateLimitCheck(sender)` overload — one spamming client no longer starves other clients' commands on shared scene objects (used by `NetworkEventDispatcher`).
- **Network / NetworkPropertySync:** `Skip Hook On Owner` option — the owner ignores the server echo of its own values in `OwnerToServer` mode (prevents rubber-banding).
- **Network / NeoNetworkManager:** inspector toggles for gated `NetworkDiagnostics` runtime logs/warnings (NoCode network debugging).
- **Tools / Spawner deny zones:** `_denyAreas`/`_denyAreas2D` + `Max Rejection Tries` + `IsPositionAllowed(Vector3)` — random points inside deny zones are re-rolled (closes the long-standing in-code TODO documented in 9.5.1).
- **Pages (sample):** `PM.ChangePageByName` now falls back to the page GameObject name and lists known PageId names in its error; `UIPage` gained an inspector `Open` button.
- **Editor / Package Health Check** (`Tools → Neoxider → Package Health Check`): verifies package version parity (package.json ↔ README ↔ PROJECT_SUMMARY ↔ CHANGELOG) and Docs/DocsEn parity — both drifts have shipped before.

### Tests
- `CooldownReward` auto-claim re-arm (covers the 9.6.1 fix), `Money` soft cap (`Add` clamps / `AddOverflow` ignores / 0 = unlimited), network command rate limit.

### Performance
- **Network / NeoMirrorSceneReactivator:** walks scene roots instead of `Resources.FindObjectsOfTypeAll` (no longer touches prefab assets on every scene load).

### Docs
- RU+EN pages for every new component; "Lobby on Neo.Pages" recipe in `Multiplayer_Guide`; deprecated types now have an explicit removal target (10.0); `ReactiveProperty` performance/naming notes; missing `Cookbook.md` metas restored.

## [9.6.2] - 2026-07-02

### Fixed
- **Network / NetworkEventDispatcher:** `CmdDispatchEvent` is now rate-limited (`RateLimitCheck`), closing a spam-amplification hole — any client could flood the global RPC broadcast (the command is `requiresAuthority = false` with default authority `None` by design).
- **Network / NetworkPropertySync:** `Sync Interval` gained a `[Min(0.1)]` floor. An interval below the server rate limit (0.05 s) caused silent Cmd drops in `OwnerToServer` mode: the owner marked the value as sent while every client stayed stuck on the stale value until the next change. Also: a missing target/field no longer poisons the reflection cache — a target assigned later at runtime is picked up (warning logged once).
- **Reactive / ReactiveProperty:** `NotifySubscribers` now takes a real snapshot of code listeners (reusable buffer, no per-notify allocation). Previously removing an earlier listener inside a callback shifted indices and the next listener silently skipped that notification. New edit-mode test covers the case.
- **Network / NetworkSingleton:** `IsInitialized` now actually reflects initialization instead of duplicating `HasInstance`.
- **Network / NeoNetworkManager:** one-time warning when Mirror's private `NetworkIdentity.hasSpawned` field is missing (Mirror upgrade guard) instead of silent scene-player-template degradation.

### Docs
- RU/EN pages updated for the fixes: `NetworkPropertySync` (interval floor + owner rubber-band caveat), `NeoNetworkComponent` (rate limit is per object, not per client), `ReactiveProperty` (snapshot semantics, main-thread only), `NetworkEventDispatcher` (RU; command rate limit).

## [9.6.1] - 2026-07-02

### Fixed
- **Bonus / CooldownReward + Tools / TimerObject:** continuous auto-claim (`_autoClaim`) now re-arms after each grant. Previously the underlying non-looping timer deactivated itself right after the completion event, so auto-claim fired once and `RemainingTime` stopped ticking. `TimerObject` now deactivates a non-looping timer **before** invoking `OnTimerCompleted`, so completion handlers may restart it with `Play()`; `CooldownReward.TakeReward()` restarts the cooldown timer (mirroring `RestartTime()`) and resets the availability flag so `OnRewardAvailable` fires on every cycle.

### Docs
- Synchronized version references in `README.md` and `PROJECT_SUMMARY.md` (were still `9.5.2`).

## [9.6.0] - 2026-06-27

### Added
- **Shop / Money:** optional soft cap `_maxMoney` (0 = unlimited). `Add()` and `SetMoney()` now clamp to it; new `AddOverflow(float)` deposits **ignoring** the cap for bonus/overflow rewards allowed to exceed it. Enables capped resources (energy / stamina / lives) without custom code. Runtime get/set via `MaxMoney`.
- **Bonus / CooldownReward:** `_autoClaim` option — automatically claims the reward the moment it becomes available (continuous regen) without manual `TakeReward()` / event wiring. Stays decoupled from wallets (no `Money` dependency); deposit via `OnRewardClaimed → Money.Add(...)`. Capped-regen recipe: `_autoClaim` + `OnRewardClaimed → Money.Add(1)` + `Money._maxMoney` cap (+ `AddOverflow` for sources allowed past the cap). Runtime get/set via `AutoClaim`, `CooldownSeconds`, `MaxRewardsPerTake`.

### Docs
- **Cookbook (new):** added a cross-module recipes page (RU + EN) consolidating end-to-end examples — capped energy + auto-regen, capped currency, daily reward, slot → wallet, shop buy/equip, reward fly — linked from the docs index.
- **Shop / Money:** documented `_maxMoney` and `AddOverflow`. **Bonus / CooldownReward:** documented `_autoClaim` and the capped-regen recipe.

## [9.5.2] - 2026-06-27

### Fixed
- **Samples / Demo Scenes / Network:** imported Demo Scenes now compile in projects without the optional Mirror package. `TestStart` follows the `Neo.Network` optional-Mirror pattern: Mirror `NetworkBehaviour`/`Command`/`ClientRpc` code is compiled only under `MIRROR`, while non-Mirror projects get a local solo-mode fallback.

### Docs
- **Package docs:** synchronized package version references and sample import paths for `9.5.2`.

## [9.5.1] - 2026-06-27

### Docs
- **Tools / Spawner:** documented planned **deny zones** (areas where spawning is forbidden) as a TODO — `_denyAreas`/`_denyAreas2D` + `_maxRejectionTries` reject candidates inside a deny zone and re-roll, plus `IsPositionAllowed(Vector3)`. "Where allowed" = spawn points / spawn area; "where forbidden" is planned. (#3)

## [9.5.0] - 2026-06-27

### Changed
- **Tools / Spawner:** the single `_spawnTransform` spawn point is now a `Transform[] _spawnPoints` array. Empty → spawn from the spawner's own transform (previous default); with one or more points, a random non-null point is picked per spawn (position and rotation stay consistent for that spawn). New public `ResolveSpawnPoint()`. ⚠ **Breaking:** scenes that referenced the old single `Spawn Transform` field lose that reference on upgrade and fall back to the spawner's own transform — re-assign points in `Spawn Points`. (#3)

## [9.4.0] - 2026-06-25

### Added
- **Extensions / Random:** `GetRandomWeighted<T>(items, weights)` and `GetRandomWeighted<T>(items, weightSelector)` — return the weighted random **element** directly instead of just the index (`GetRandomWeightedIndex`).
- **Extensions / Dictionary:** new `DictionaryExtensions` with `GetOrCreate(key)`, `GetOrCreate(key, factory)`, and `Increment(key, amount = 1)` for `int`/`float` counters — replaces the repeated get-or-create and `dict[k] = dict.GetValueOrDefault(k) + 1` boilerplate. (`GetValueOrDefault` is intentionally left to the BCL to avoid overload ambiguity.)
- **Extensions / Primitive (Math):** `Snap(step)` (float/int), `Wrap(min, max)` (negative-safe cyclic index), `PingPong(length)` (int), and `Approximately(b)` / `Approximately(b, tolerance)` float-equality helpers.

## [9.3.0] - 2026-06-23

### Added
- **Audio / AM:** `Play(AudioClip)` and `PlayMusicByClip(AudioClip)` convenience overloads; `SetMusicVolume`/`SetEfxVolume` (replacing the boolean-trap `SetVolume(volume, bool)`); `startVolumeEfx`/`startVolumeMusic` renamed to PascalCase with `[Obsolete]` forwarders.
- **Bonus / Slot:** `SpinController.SpinResult` value type with `GetLastResult()` and `LastPayout` — one coherent spin outcome (symbol grid, winning lines, payout).
- **Tools / Debug:** `NeoDebugOverlay` — drop-in IMGUI runtime overlay (FPS, active scene, time scale, AM/SaveManager status), toggled with F3.
- **Tests:** `AmEditModeTests`, `SpinControllerPaylineTests`, and a public-PascalCase convention check in `ModulePrinciplesTests`.
- **Docs:** EN parity for Quest and Tools, bilingual Getting Started, and a NeoDoc link checker that now verifies the DocsEn mirror.
- **GridSystem / Dice:** added serializable `DiceValueWeight` and `DicePieceGenerator.GenerateWeighted(...)` for designer-controlled dice value pools, explicit invalid-weight validation, and non-duplicating weighted pairs.
- **GridSystem:** expanded `GridSlotAllocator` with `Capacity`, `HasAvailableSlot`, slot-index preferred allocation, slot-index release, and `Clear(...)` for reusable autobattler benches, tactical rows, backpack rails, and compact board lifecycle management.
- **GridSystem:** extended `GridSlotAllocator` with linear slot-index helpers (`TryGetSlotPosition`, `TryGetSlotIndex`, `IsAvailable(int)`, `Allocate(int, int)`) for rectangular 2D boards such as autobattler benches, tactical rows, hotbars, and card-game lanes.
- **Cards:** added optional finite `HandModel.Capacity`, `RemainingCapacity`, `IsFull`, `TryAdd(...)`, and `AddRangeUntilFull(...)` so CCG hands, autobattler benches, backpack rails, and market rows can reject overflow explicitly while unlimited hands remain the default.
- **GridSystem / Dice:** added `DicePieceGenerator.CreateD6Pool()` and `CreateSequentialPool(minValue, maxValue)` for classic dice rolls and custom numbered pools without duplicating pool construction in games.
- **GridSystem:** added `GridSlotAllocator` for ordered one-cell slot allocation on top of `FieldGenerator`, covering benches, tactical rows, autobattler boards, hotbars, and market rows without duplicating occupancy checks.
- **UI / AnimationFly:** added reusable motion presets for typed requests (`Arc`, `Fountain`, `Magnet`, `FountainMagnet`, `Scatter`) with burst and magnet tuning fields, plus PlayMode coverage for fountain trajectory and deterministic fountain+magnet rewards.
- **Samples / UI:** added an `AnimationFlyDemo` scene with runtime buttons, a real sample sprite asset example, and labeled sliders for count, duration, delay, arc, scale, and rotation so fly-effect flows can be inspected without manual scene editing.

### Fixed
- **Editor scene-dirtying:** `ParallaxLayer`, `CameraAspectRatioScaler`, and `AM` no longer mark the open scene dirty on load (perpetual `*`): removed unconditional `SetDirty` in editor delay-calls, made preview generation transient (`HideAndDontSave`), and drive the aspect-ratio camera only at runtime.
- **Runtime performance:** `RpgProjectile` and `MagneticField` now use NonAlloc physics queries + reused buffers instead of per-frame heap allocations; `Drawer` releases its owned/cloned Materials in `OnDestroy`; `InteractiveObject` caches colliders and reuses the cached camera; `Singleton.I` no longer re-runs `FindObjectsByType` on every access when no instance exists.
- **Cards (async lifetime):** `DrunkardGame`, `BoardComponent`, `DeckComponent`, `HandComponent`, `HandView`, and `CardView` bind UniTask awaits to `GetCancellationTokenOnDestroy` and link tweens to the GameObject, preventing `MissingReferenceException` on scene change mid-animation.
- **Audio / AM:** `OnDestroy` now stops `RandomMusicController` so its looping UniTask cannot run after teardown.
- **Bonus / Slot:** aligned slot element scene gizmo coordinate labels with the `SpinController` console grid index base and guarded `VisualSlotLines` against missing line references.
- **Tools / Compatibility:** switched `MouseInputManager`, `MouseEffect`, `ParallaxLayer`, and `NetworkContextActionRelay` debug IDs to `Object.GetEntityId()` under `UNITY_6000_5_OR_NEWER` (Unity 6.5+), while preserving `GetInstanceID()` for older versions.
- **Samples / NeoxiderPages:** removed hard DOTween/DOTween Pro runtime dependencies from `UIPage` and `BtnChangePage`, stripped legacy `DOTweenAnimation` components from sample prefabs, and declared `com.unity.ugui` as a package dependency so imported page prefabs resolve standard uGUI scripts.
- **Samples / NeoxiderPages:** fixed `UIPageEditor` null-reference spam after removing the legacy `_animation` field and cleaned stale `_animation` serialized references from sample page prefabs.
- **Samples / NeoxiderPages:** hardened `UIPageEditor` against missing serialized fields so partial imports or stale sample objects render warnings instead of throwing inspector `NullReferenceException`s.
- **Samples / NeoxiderPages:** removed dangling prefab component references from `_Page base` and `Shop Page` so Unity no longer reports corrupt prefab imports after the legacy tween cleanup.

## [9.2.0] - 2026-06-04

### Added
- **GridSystem:** added `FieldGenerator.TryGetCellPositionFromWorld`, `TrySnapWorldToCellCenter`, and `SnapWorldToCellCenter` so grid drag/drop and preview snapping can use the generator's own origin-aware nearest-cell conversion API.
- **GridSystem:** added reusable `GridPlacementEntry`, `GridPlacementResult`, `FieldGenerator.CanPlaceContentFootprint`, and `FieldGenerator.PlaceContentFootprint` for writing multi-cell pieces/items/shapes into grid cells.
- **GridSystem / Merge:** added `GridMergeRequest.Increment(...)` factory preset so the common "merge equal content into content+step at the seed" rule no longer needs ~10 delegates wired by hand, plus a `NotifyOnContentChanged` toggle so callers can apply extra state before notifying.
- **Merge:** added `MergeRequest.MaxCascadeIterations` and `MergeResult.CascadeLimitReached` (mirrored on `GridMergeResult`) so the cascade safety limit is configurable and surfaced instead of stopping silently.
- **GridSystem / Dice:** exposed `DiceBoardService.MinMergeGroupSize`, `MergeStep`, `MaxContentId`, and `RequireWalkable` so dice merge rules are tunable without editing the service; `DicePiece` now exposes `CellCount`.
- **Samples / Dice:** added a `Dice.prefab` visual used by the Dice Merge demo instead of constructing dice visuals in code.
- **Docs:** added RU/EN API pages for `GridPlacementEntry` and `GridPlacementResult`, placement examples in `FieldGenerator` docs, and current TODO/Ideas notes for GridSystem placement follow-ups.
- **Tests:** added EditMode coverage for cascade-limit flagging, multi-cell `DicePiece` rotation, single consistent merge notifications, single `OnBoardChanged` per merging placement, and configurable merge step/cap.

### Changed
- **GridSystem / Dice:** `DicePiece.RotateClockwise`/`RotateCounterClockwise` now rotate footprints of any size around the anchor (not just pairs), and the demo controller enumerates orientations / allows rotation for any multi-cell piece.

### Fixed
- **Build / Runtime:** guarded runtime assembly references to `UnityEditor` APIs in prefab previews and button drawers so player builds do not pull editor-only namespaces; added an architecture test for unguarded runtime `UnityEditor` usage.
- **Save:** changed `SaveManager.Save()` to read-modify-write the shared `SaveData_All` container so data for unloaded scene objects is preserved; added EditMode coverage for absent component entries, multiple preserved unloaded entries, and current-component load round-trip.
- **Bonus / Slot:** split paid and free spin payment logic so positive-price spins require an `IMoneySpend` wallet while zero-price spins remain explicitly free; added EditMode coverage for paid/free payment paths.
- **Shop / Money:** added `MoneySpendResult` / `IMoneySpendWithResult` / `IMoneySpendAuthority` so callers can distinguish confirmed, rejected, and pending server-authority spends; `Shop` no longer performs wallet-only server spend requests for pending item/bundle purchases without a matching server-side grant path; added EditMode coverage for detailed spend statuses and pending authority item/bundle purchases.
- **Core / Level & Resources:** fixed XP-backed `SetLevel` threshold sync, `LevelNoCodeAction` false level-up events, duplicate `OnDeath` dispatch, custom resource depleted events, and regen-from-zero contracts; added targeted EditMode coverage.
- **RPG:** fixed buff stack application/projection/snapshot persistence, required-target presets spending resources before target resolution, hit dedup/target resolution for arbitrary `IRpgCombatReceiver` implementations, and reusable projectile initialization state; added targeted EditMode coverage.
- **Samples / Package Validation:** removed a sample-only page-switch component reference from the package `ButtonPageSwitch` prefab, restored the active `DemoLevelCurve` validation asset, made sample scene coverage resolve `Samples~` scene files by filesystem path/YAML, and made PlayMode sample smoke skip hidden `Samples~` runtime scenes that Unity cannot compile directly.
- **Tools / Move:** made `FreeFlyCameraController` movement tests deterministic by pinning external look input when verifying local-forward movement.
- **UI / AnimationFly:** added a typed request/result API, sprite-only fly visuals, built-in disable-and-pool completion, reward timing callbacks, and parent-local Canvas coordinate conversion so world-to-UI rewards spawn in UI while visually starting at the world pickup position; pooled fly visuals now kill/link tweens and reset base transform state before reuse; added EditMode/PlayMode coverage for coordinate, reward timing, and pooled scale contracts.
- **Cards:** fixed duplicate-card removal by index so `HandModel`, `HandPresenter`, and `HandComponent` remove the same indexed card/presenter/component instead of the first equal card data; `HandComponent` now removes only its own card click listeners, `DeckComponent` detaches old model events on reinitialize, and card views own/kill hover tweens; added EditMode coverage for duplicate card ordering and lifecycle contracts.
- **NoCode / Lifecycle:** exposed explicit editor/runtime refresh and subsystem reset hooks used by EditMode validation across NoCode bindings, Save, MouseInputManager, and SwipeController.
- **GridSystem / Dice:** fixed double `OnCellStateChanged` notifications with stale `IsOccupied` during merges — the resolver no longer notifies mid-mutation; `DiceBoardService` applies occupancy and raises one fully-consistent notification per cell.
- **GridSystem / Dice:** fixed `DiceBoardService.Place` raising `OnBoardChanged` twice on a merging placement; placement and follow-up merges now raise it exactly once.
- **GridSystem / Merge:** stopped allocating a full board cell list on every `GridMergeResolver.Resolve` when explicit seeds are supplied.
- **Samples / Dice:** fixed drag preview/drop behavior so the tray dice is hidden while dragging, the preview can be offset above the pointer, optional snap preview uses `FieldGenerator`, final release uses the current snapped/nearest grid cell, and placed dice keep the prefab world scale instead of shrinking under scaled cell prefabs.
- **Samples / Dice:** fixed placed dice visuals disappearing after drag/drop by syncing from `DiceBoardService.OnBoardChanged`, resolving missing demo view references within the view's own hierarchy (no global `FindObjectOfType` that could bind to another board), rebuilding missing cell views before board refresh, keeping board dice under a dedicated `DicePlacedPiecesView` root, refreshing placed visuals after drag preview cleanup, and reusing placed die views across refreshes instead of destroying/recreating them every frame.
- **Samples / Dice:** made the demo empty-content fallback consistent with `DiceBoardService.EmptyContentId` (-1) and cached the fallback solid sprite instead of allocating a new `Sprite` per cell/die.
- **Samples / Dice:** fixed placed dice being destroyed on mouse release. Two reinforcing fixes: (1) the view's visual roots are now deterministic — cached and never recovered via `transform.Find` (which, with play-mode deferred `Destroy` and runtime roots persisted in the saved scene, could return a stale/duplicate root and orphan placed dice), and the view rebuilds cleanly instead of adopting persisted scene cells; (2) on a successful drop the dragged preview dice are now *promoted* into their destination cells and reused as the persistent placed visuals (registered before the model mutates, so `OnCellStateChanged`/merges reuse the same objects), and the preview is only destroyed on a failed drop.
- **GridSystem / Dice:** guarded Dice placement and demo previews against missing `DicePiece.Cells` data so startup/drag/drop cannot throw when a piece is empty or partially initialized.
- **GridSystem / Merge:** hardened `GridMergeResult`, `GridMergeGroupResult`, and `DicePlacementResult` collections as read-only-reference lists so callers cannot reassign result buffers.
- **Docs:** updated `PROJECT_SUMMARY.md` as a compact module/reuse map and linked it more prominently from the main README entry points.

## [9.1.0] - 2026-06-02

### Added
- **Merge:** added `Neo.Merge`, a standalone pure C# connected-group merge engine for grids, inventories, lists, graphs, and custom board-like mechanics.
- **GridSystem:** added `GridMergeResolver` adapter for applying generic merge rules to `FieldGenerator` / `FieldCell.ContentId`.
- **GridSystem / Dice:** added `Neo.GridSystem.Dice` with dice pieces, pool-based piece generation, dice placement, and dice merge resolution.
- **Samples:** added a playable 5x5 Dice Merge demo scene using the Dice sprites under `Assets/Neoxider/Sprites/Dice`.
- **Tests:** added EditMode coverage for generic merge, GridMerge, Dice mechanics, combined Dice/GridMerge behavior, and PlayMode smoke coverage for the Dice demo.

## [9.0.0] - 2026-05-26

### Added
- **Diagnostics:** added `NeoDiagnostics` in the shared `Neo.Extensions` assembly as the package logging gate. Info and warning output is disabled by default, errors remain visible, throttled warnings are supported, and static state resets under domain-reload-disabled play mode.
- **Tests:** added EditMode coverage for `NeoDiagnostics` and architecture coverage that keeps aligned runtime roots from reintroducing raw `Debug.Log*` calls.
- **Samples:** added required smoke scenes for Audio, Level, Network, NoCode, Parallax, Save, Settings, and StateMachine under the active development samples root. Each scene has a `ModuleDemoSceneInfo` marker and minimal module wrapper setup.
- **Tests:** added sample scene coverage that opens required smoke scenes, checks `ModuleDemoSceneInfo`, and verifies missing scripts. Sample validation now supports both active development `Samples` and release/UPM `Samples~` roots.

### Changed
- **Package samples:** aligned `displayName` to `NeoxiderTools` so Unity imports samples under `Assets/Samples/NeoxiderTools/<version>/<sample>`. Validation still accepts the legacy `Assets/Samples/Neoxider Tools` root for already-imported samples.
- **Cards / Bonus / UI / Tools Move:** moved remaining direct runtime logs in the aligned roots behind `NeoDiagnostics` or explicit component debug flags. `AnimationFly` and `UniversalRotator` now cache their `Camera.main` fallback instead of resolving it on every conversion/aim call.
- **Parallax / Tools Input:** added explicit camera injection APIs and throttled/optional `Camera.main` fallback paths for `ParallaxLayer` and `MouseInputManager`, with RU/EN docs and EditMode coverage.
- **Samples:** routed demo setup feedback through one sample diagnostics helper instead of direct setup-script `Debug.Log` calls.
- **Docs:** documented the current `Samples` development path, the `Samples~` UPM source path, and Unity's imported `Assets/Samples/NeoxiderTools/<version>/<sample>` path in `AGENTS.md`, Samples docs, package compatibility docs, and README navigation.
- **Docs / GridSystem:** restored the top-level RU/EN GridSystem docs to readable UTF-8 and documented its constructor role for Match3, TicTacToe, 2048-like SlidingMerge, pathfinding, views, and spawners.

## Legacy History

Entries before `9.0.0` were trimmed from the package changelog to keep the release notes focused (earlier versions were also cleaned during a prior UTF-8 pass). Use git history or release tags for the exact older notes.
