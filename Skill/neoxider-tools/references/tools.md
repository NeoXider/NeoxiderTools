# Neo.Tools — component catalog (the big one)

`Neo.Tools` is the package's catch-all and has MANY components. Catalog below grouped by category; for
each: purpose + key public API (verify exact signatures against `Assets/Neoxider/Scripts/Tools/` before
use). Singletons expose `static T I` / `Instance`. Components marked **[no-code]** are inspector-wiring
primary — avoid building on them in code (see avoid-nocode.md).

## Managers
- **`Singleton<T>`** — base: `I`/`Instance`, `HasInstance`, `TryGetInstance(out T)`, `DestroyInstance()`. **`SingletonById<T>`** — keyed instances; `TryGet(id, out T)` never returns a destroyed instance.
- **`GM`** (`GM.I`) — game state. `enum GameState {NotStarted,Menu,Preparing,Game,Win,Lose,End,Pause,Other}`; `State{get;set}`; `StartGame(bool restart=false)`, `StopGame()`, `Win()`, `Lose()`, `End()`, `Pause()`, `Resume()`, `Menu()`; `IsPlaying`.
- **`EM`** (`EM.I`) **[no-code]** — event hub of `UnityEvent`s (`OnGameStart/OnWin/OnLose/OnStateChange<GameState>/OnPause/...`). Static fire shortcuts: `EM.GameStart()`, `EM.Win()`, `EM.Lose()`, `EM.Pause()`, `EM.PlayerDied()`, … Subscribe from code with `EM.I.OnWin.AddListener(...)`.
- **`Bootstrap`** (`Bootstrap.I`, DontDestroyOnLoad) — runs `IInit.Init()` by `InitPriority`. `interface IInit{int InitPriority;void Init();}`. A throwing `Init` no longer stalls startup: the target is set aside (`HasFailed(target)`), `IsBootstrapCompleted` turns true, `InitializePending()` retries explicitly.

## Spawner / Pool (prefer these over Instantiate/Destroy)
- **`SpawnUtility`** (static) — main entry: `Spawn(prefab[,pos][,rot][,parent])`, `Despawn(instance)`, `ClearFallbackPools()`. Uses `PoolManager` if present, else internal fallback pools (a scene load keeps pools the new scene already uses).
- **`PoolManager`** (`PoolManager.I`, DontDestroyOnLoad) — `static Get(prefab,pos,rot,parent=null)`, `static Release(instance)`; pre-warm via `_preconfiguredPools`. **`NeoObjectPool`** (`Prefab`, `ReturnObject(instance)`) — warm get/return is O(1) and allocation-free; double, foreign and destroyed returns are ignored.
- **`Spawner`** — interval/wave area spawner. `StartSpawn()`/`StopSpawn()`/`Clear()`, `SpawnRandomObject()`, `SpawnById(int,pos)`, `ResolveSpawnPoint()`; events `OnObjectSpawned`, `OnWaveStarted`. `SpawnMode.Loop|Waves`. Spawn location: `Spawn Points` (`Transform[]` — empty = own transform, else random point per spawn) or a `Spawn Area` collider (2D/3D).
- **`SimpleSpawner`** — `Spawn()` single-shot for UnityEvents; `GameObject SpawnInstance()` is the typed C# path. **`Despawner`** — `Despawn()`, `DespawnOther(go)`, static `DespawnObject(go)`, `OnDespawn`.
- **`IPoolable`** {`OnPoolCreate/OnPoolGet/OnPoolRelease`}; **`PoolableBehaviour`** base (override these; the pool activates the object itself). **`PoolExtensions`**: `go.ReturnToPool()`, `go.SpawnFromPool(pos,rot,parent)`, `PooledObjectInfo.Return()`.

## Score / Counters
- **`ScoreManager`** (`ScoreManager.I`, saves best) — `Add(int,bool updateBest=true)`, `Set(int,...)`, `SetBestScore(int?)`, `ResetScore()`, `GetCountStars()`, `SaveKey`; props `ScoreValue`, `BestScoreValue`, reactive `Score/BestScore/Progress/CountStarsReactive`.
- **`Counter`** — numeric with optional save key + Mirror sync. `Add/Subtract/Multiply/Divide/Set(int|float)`, `Send()`, `LoadFromSave()`; `ValueInt`, `ValueFloat`, reactive `Value`; `OnValueChangedInt/Float`; `static Dictionary<string,List<Counter>> Registry` (cleared per play session).
- **`ModifyCounterByKey`** — `Execute()` modifies a Counter/Money by save key. **`RevertAmount`** — `Amount(a)` → fires `OnChange(1-a)`.

## Time (assembly `Neo.Tools.Time`; guide `Docs/Tools/Time/Clocks.md`)
- **`Timer`** (`[Serializable]`, `IDisposable`, UniTask — NOT a MonoBehaviour) — `new Timer(duration,updateInterval=0.05f,looping=false,useUnscaledTime=false)`; `Start()`/`Stop()`/`Pause()`/`Resume()`, `AddTime(s)`, `SetProgress(f)`, `Dispose()`; events `OnTimerStart/End`, `OnTimerUpdate(remaining,progress)`; props `IsRunning`, `RemainingTime`, `Progress`; `IFrameClock FrameClock` (inject `ManualFrameClock` for tests/replays); opt-in `CarryLoopOvershoot` keeps looping periods exact.
- **`TimerObject`** — scene timer wrapping `Timer` (count up/down, loop, day-cycle, milestones, save, auto UI). `Play()/Stop()/Pause(bool)/Resume()/TogglePause()`, `SetTime/SetProgress/AddTime`, `Tick(delta)` for an external driver, reactive `Time`, many events. `FrameClock`, `UtcClock` (`INeoClock`, e.g. `NeoTrustedTime.AsClock` for tamper-resistant real-time saves), `CarryLoopOvershoot`. Its **Time Text** (`TMP_Text timeText` + `timeFormat`) label works and is allocation-free (`TimerTextFormatter`, reusable for your own per-frame labels).
- **Clock seams** — `IFrameClock` (`DeltaTime`, `UnscaledDeltaTime`, `TimeScale`; default `UnityFrameClock.Instance`; `ManualFrameClock` settable), `INeoClock` (`UtcNow`, `MonotonicSeconds`; `NeoTrustedTime.AsClock`, `trustedClock.AsNeoClock()`, `SystemNeoClock`).
- **`GameTimeController`** — `PauseGame()` (timeScale 0), `ResumeGame()`, `SetTimeScale(f)`, `TogglePause()` (resumes to the previous non-zero speed), `IsPaused`, `ResumeScale`.

## Move / Camera
- **`Follow`** — `SetTarget(t)`, `SetOffset`, `SetPositionSpeed`, `SetStoppingDistance`, `TeleportToTarget()` (applies position limits), `SmoothMode{None,MoveTowards,Lerp,SmoothDamp,Exponential}`; events `onStartFollowing/onStopFollowing/onTargetLost`.
- **`ConstantMover`** — `SetSpeed(f)`; `MovementMode{Transform,Rigidbody,Rigidbody2D}`. **`ConstantRotator`** — `SetDegreesPerSecond(f)`.
- **`UniversalRotator`** — aim at Transform/point/mouse: `SetTarget`, `RotateTo(point,instant)`, `RotateBy(deg)`; 2D/3D, axis limits.
- **`KeyboardMover`** (`IMover`) — `MoveDelta(v2)`, `MoveToPoint(v2)`; `MovementPlane{XY,XZ,YZ}`. **`MouseMover2D/3D`** — follow mouse.
- **`DistanceChecker`** — `GetCurrentDistance()`, `IsWithinDistance()`, `SetTarget`, reactive `Distance`; events `onApproach/onDepart` (`onDepart` also fires once when the target is cleared or destroyed while inside).
- **3D character controller (v10.3+, preferred)** — CMF motor (`ThirdParty/CharacterMovementFundamentals`: `Mover` + `AdvancedWalkerController`, slope limit with slide-off, stairs, moving platforms via `CMF.MovingPlatform`+`TriggerArea`, momentum `AddMomentum/SetMomentum`, `OnJump/OnLand` C# events) wrapped by Neo components in `Scripts/Tools/Move/CharacterController/`: **`NeoCharacterInput`** (New/legacy input auto-detect; injection `SetMoveInput(Vector2?)/SetJumpInput(bool held)/SetRunInput(bool)`; gates `SetMovementEnabled/SetJumpEnabled`), **`NeoCameraInput`** (look; `GameSettings.MouseSensitivity`, cursor gate, EM pause; `SetLookInput(v?)`), **`NeoCharacterSprint`** (`IsSprinting`, multiplier over `movementSpeed`), **`NeoCharacterCameraBridge`** (Cinemachine-driven camera), **`NeoCharacterNetworkBinding`** (Mirror local/remote split). Ready prefabs in Create window *Presets → Player*: Character First/Third Person (capsule, self-contained, drop-in: footstep audio + `CursorLockController` on root) + Animated/Top Down/Side Scroller/Click To Move variants (need the CMF sample imported for Capguy assets); *Presets → Environment → Moving Platform*. Docs: `Docs/Tools/Move/CharacterController/`.
- **`PlayerController2DPhysics` / `PlayerController3DPhysics`** (3D one is legacy since v10.3, Mirror-aware) — `SetMovementEnabled/SetJumpEnabled/SetLookEnabled`, `Teleport(pos)` (moves the Rigidbody too), external input `SetMoveInput(v?)/SetJumpInput()/SetRunInput(bool)`; props `IsGrounded`, `IsRunning`. `SetJumpInput()` is also a Play Mode-only **Jump** Inspector button. Animator drivers `PlayerController2D/3DAnimatorDriver` **[no-code]** observe them (cached hashes; parameters the controller lacks are skipped).
- **`CameraShake`** (DOTween) — `StartShake([dur,strength])`, `StopShake()`, `ResetTransform()`, `StopAndReset()`, `ShakeType{Position,Rotation,Both}`. Start, stop, and stop-and-reset have Play Mode-only Inspector buttons. **`CameraRotationController`**, **`FreeFlyCameraController`** (`SetControllerEnabled`, `Warp`, `Tick(dt)`), **`CameraConstraint`**, **`CameraAspectRatioScaler`** (`ScaleMode{FitWidth,FitHeight,FitBoth,Manual}`; `Mode`, `TargetResolution`, `ReferenceOrthographicSize`/`ReferenceFieldOfView` stored on the scaler, `Refresh()`, `CaptureReferenceFromCamera()`), **`CursorLockController`**, **`ScreenPositioner`** (`Configure(edge,offset,depth)`).

## Interaction / Physics
- **`InteractiveObject`** (Mirror-aware) — hover/click/range; `interactable`, `InteractionDistance`, `IsHovered`, `IsInInteractionRange`, `UseScreenCenterRay`; typed manual dispatch through `InteractDown()`, `InteractUp()`, `Click(MouseButton,bool)`; events `onInteractDown/Up`, `onHoverEnter/Exit`, `onClick/onDoubleClick/onRightClick`, `onEnterRange/onExitRange`. Many targets on one shared ray cost about H + N (hits + targets), not N × H. Depend on **`IInteractiveTarget`** for custom AI/XR/proximity input. Reuse allocation-free **`InteractionQueryMath`** / **`InteractionRayHit`** for range and ordered-hit rules, and **`InteractionCameraResolver`** for the shared cached-camera policy. These reusable APIs stay in namespace `Neo.Tools` but live in the dependency-free `Neo.Tools.InteractableObject.Core` assembly; custom asmdefs should reference that leaf directly when they do not need the scene component or Mirror integration. The manual dispatch methods and collider-cache invalidation have Play Mode-only Inspector buttons for scene testing.
- **`PhysicsEvents2D` / `PhysicsEvents3D`** **[no-code]** (Mirror-aware) — forward trigger/collision to UnityEvents AND C# events (`TriggerEnterOccurred`, `CollisionEnterOccurred`); tag/layer filters.
- **`ToggleObject`** — `Toggle()`, `Set(bool)`, reactive `Value`, `OnChangeFlip(bool)`; both methods have Play Mode-only Inspector buttons.
- **Physics zones** (reused, self-growing overlap buffers): **`ImpulseZone`** (`ApplyImpulseToObject(go)`, `ImpulseDirection{AwayFromCenter,TowardsCenter,...}`, `SetDirection`, `SetDirectionIndex(int)` for UnityEvents, `CurrentDirection`, `SetCustomDirection(v)`, `SetCooldown(s)`, `ImpulseForce`), **`ExplosiveForce`** (`Explode([force])`, `ActivationMode{OnStart,OnAwake,Delayed,Manual}`), **`MagneticField`** (`SetMode(FieldMode{Attract,Repel,ToTarget,ToPoint,Direction})`, `SetModeIndex(int)` for UnityEvents, `CurrentMode`, `SetStrength/SetRadius`, `OnObjectEntered/Exited`; **Max Overlap Colliders** is an initial capacity, not a cap). All three: `LayerMask AffectedLayers {get;set}`.
- **`AdvancedForceApplier`** — knockback used by attack system; **Clamp Max Speed** also caps the two physics steps after `ApplyForce`.

## View / UI helpers
- **`Selector`** (Mirror-aware) — which child is active: `Set(int)`, `Next()/Previous()`, `SetRandom([deactivateOthers])`, unique-random mode, exclude lists; props `Value`, `Count`, `Item`, `Items`, `FillMode`, `UniqueSelectionMode`; events `OnSelectionChanged(int)`, `OnFinished`, `OnUniqueCycleComplete`. `IsAtStart`/`IsAtEnd`/`CountActive` and warm commands do not allocate. The rules live in plain C# **`SelectorModel`** (`IsItemActive(itemIndex)` for custom views). (`SelectorItem` **[no-code]** child.)
- **`ImageFillAmountAnimator`** (DOTween) — `SetValue(0..1)`, `SetBool(bool)`. **`BillboardUniversal`** — `SetBillboardMode(TowardsCamera|AwayFromCamera|TowardsDirection)`; getters `Mode`, `IgnoreY`, `CustomDirection`, `TargetCamera`. **`ZPositionAdjuster`** — 2D depth from Y: `Adjust()`, `Ratio`, static `ComputeDepth(y, useNormalizeToUnit, ratio)`.
- **`LightAnimator`** — `Play/Stop/Pause/Resume` (also Play Mode-only Inspector buttons), `ManualUpdate` + `Tick(dt)`, `UseUnscaledTime`, `RandomizeStartTime`, `LightComponent` (Light or Light2D). **`StarView`** — `Bind(ScoreManager)`, `ShowStars(int)`.
- **[no-code] view bits**: `TextScore` (auto-binds ScoreManager), `MeshEmission` (writes only on change), `DOTweenUIImageFallback`, `UpdateChilds`.

## Text
- **`SetText`** (DOTween for number anim) — `Set(int|float|string)`, `SetPercentage(v,addSign=true)`, `SetCurrency(v,sym="$")`, `SetBigInteger(v)`, `SetFormatted(v,NumberFormatOptions)`, `Clear()`, `Prefix`/`Suffix`; `OnTextUpdated(string)`.
- **`TimeToText`** — THE component for showing seconds on a TMP label (countdown/elapsed). `Set(float)`
  (or wire a `UnityEvent<float>` → `Set`); `TimeFormat` (e.g. `MinutesSeconds` → `mm:ss`),
  `TimeDisplayMode{Clock,Compact}`, `Prefix`/`Suffix`, auto-grabs `TMP_Text`; reuses its string while the
  shown unit is unchanged (no per-frame allocation); static `FormatTime(t,TimeFormat,sep)`.
  **Don't write your own timer-text MonoBehaviour.**

## Input
- **`MouseInputManager`** (`MouseInputManager.I`, DontDestroyOnLoad, zero-GC) — C# events `OnPress/OnHold/OnRelease/OnClick` with `MouseEventData{ScreenPosition,WorldPosition,HitObject,Hit3D,Hit2D}`; `SetTargetCamera(cam)`; static `LastEventData`. `OnHold` fires only with **Enable Hold** on.
- **`MouseEffect`**, **`MultiKeyEventTrigger`** (key combo → UnityEvent), **`SwipeController`** (swipe direction events). Compat helpers `MouseInputCompat`/`KeyInputCompat` (legacy Input + Input System fallback, allocation-free polling on both backends), `UIInputCompat.IsPointerOverUI()`, `OptionalInputSystemAdapter`. Use them instead of `UnityEngine.Input` so code works in Input System-only projects.

## Debug
- **`FPS`** (TMP) — `CurrentFps`, `SetTargetFramerate(int)`, `SetVSync(bool)`; color thresholds.
- **`NeoDebugOverlay`** **[no-code]** — IMGUI overlay (FPS/scene/timescale/AM+SaveManager), toggle `_toggleKey` (F3).
- **`ErrorLogger`** — captures `logMessageReceived`, optional file output; bounded history `MaxEntries` (default 50, 0 = unlimited), `Entries`, `Clear()`.

## Draw
- **`Drawer`** — mouse/touch line drawing (Chaikin smoothing, EdgeCollider2D, pooling; pointer through `MouseInputCompat`): `BeginLine(pos)`, `AppendPoint(pos)`, `EndLine()`, `DeleteFirst/Last/All()`, `CreateCollider()`; reactive `Distance`/`PointCount`; static `Smooth(points,passes,fixedZ)`, `LimitPoints(pts,max)` plus non-allocating `Smooth(points,passes,fixedZ,results)` / `LimitPoints(points,max,results)` (results may be the source list).

## Components (Neo.Tools.Components)
- **`TypewriterEffect`** (`[Serializable]`, UniTask) — `PlayAsync(text,onTextChanged,ct)`, `PlayRevealAsync(text, onVisibleCharactersChanged, ct)` (reports a visible-character count, no per-character strings — pair with TMP `maxVisibleCharacters`), `Stop()`, `Complete()`; `IsTyping`, `Progress`; events `OnComplete`, `OnCharacterTyped(char)`. Scene wrapper **`TypewriterEffectComponent`** — `RevealMode` (`TypewriterRevealMode.MaxVisibleCharacters` is allocation-free and avoids word-wrap jumps; `RebuildText` default), `PlayAutoText()`, `Stop()`, `Clear()` as Play Mode-only Inspector buttons.
- **`Loot`** — drop on destroy; `ScatterPlane` (`LootScatterPlane.XZ` default, `XY` for 2D); no drops while the scene unloads or the app quits.
- **`AnimatorParameterDriver`** **[no-code]** — `Animator` property; cached parameter hashes.

## Dialogue / Leaderboard / Random
- **`DialogueController`** (UniTask) — `StartDialogue([d,m,s])`, `NextSentence/NextMonolog/NextDialogue()`, `SkipOrNext()`, `Advance()`, `CompleteTypewriter()`; events `OnSentenceEnd/OnMonologEnd/OnDialogueEnd/OnAllDialoguesEnd`, `OnCharacterTyped(char)`. Data `Dialogue[]` (→`Monolog[]`→`Sentence[]`). `DialogueUI`, `DialogueData` (SO).
- **`DialogueLinePool`** (plain C#) — pooled bark lines: `new DialogueLinePool(lines)`, `TryPlay(tag, contextTime, priority, out line)` (`IdlePriority`/`ContextPriority` — a contextual line pre-empts idle chatter), `TryPick(tag, [contextTime,] out line)`, `Finish()`, `ResetHistory()`; no back-to-back repeats, time windows (`MinTime`/`MaxTime`), first-pick slots (`PooledLineSlot`); events `LineStarted/LineInterrupted/LineFinished`. Scene wrapper **`DialogueLinePlayer`** — `PlayIdle()`, `PlayContext(tag)`, `Play(tag, priority)`, `RequestIdle()`, `RequestContext(tag)`, `FinishLine()`, `Tick(deltaTime)`, TMP `LineText`/`SpeakerText`. Usable without `DialogueController`.
- **`Leaderboard`** (`Leaderboard.I`, fake players) — `UpdatePlayerScore(int,overrideBest=false)`, one-argument `SubmitScore(int|float)` for UnityEvents, `UpdatePlayerName(s)`, `Sort()`, `GetIdPlayer()`. `LeaderboardItem`/`LeaderboardMove` rows.
- **`ChanceManager`** (`[Serializable]`) — float-weighted table: `new ChanceManager(params float[] weights)`, `AddEntry(weight,...)`, `GetChanceId()`, `Evaluate()`, `TryEvaluate(out idx,out entry)`, `Normalize()`; weight changes do not allocate; rolls are O(n). **`ChanceSystemBehaviour`** scene wrapper — `GenerateId()`, `EvaluateAndNotify()`, per-index `EventsByIndex`. `ChanceData` (SO).
- **`IntegerChanceTable`** (`[Serializable]`, plain C#) — exact integer weights (per-million jackpot odds), O(log n) rolls: `new IntegerChanceTable(params int[] weights)`, `Add(weight,label,customId)`, `SetWeight`, `Roll()`, `TryRoll(out index, out entry)`, `GetIndex(ticket)`, `TryGetOdds(index, out weight, out total)`, `GetProbability`, injectable `RandomBelow` (`Func<long,long>`).

## NPC nav (note)
`AiNavigation (Legacy)` under Tools is `[Obsolete]` → use **`Neo.NPC.NpcNavigation`** for new code. The legacy `AttackSystem` (`Health`/`AttackExecution`/`AdvancedAttackCollider`) is `[Obsolete]`; its sources now live in `Scripts/Rpg/Combat/` and compile in the optional `Neo.Rpg.Combat` compatibility assembly. `Neo.Rpg` itself is legacy too (superseded in v10) → for new combat use **`Neo.Abilities`** (see abilities.md).

## Inventory
`InventoryComponent` (`InventoryComponent.I`) — `OnSelectedItemChanged` (`UnityEvent<int>`, raised when `SelectedItemId` changes; does not save). `InventoryHand` writes to a linked `Selector` only when the selectable count or slot index changes — subscribe to `OnSelectedItemChanged` to react when a different item lands in the selected slot. `InventoryTotalCountText` (Selected mode) follows the selection. Mutations, pickups and Auto Save no longer deep-clone payloads; for bursts of pickups turn Auto Save off and call `Save()` once.

## Shop / Inventory bridge
`ShopInventoryGrantBridge` is an opt-in scene adapter in the **`Neo.Shop.Bridges`** assembly. It references
both `Neo.Shop` and `Neo.Tools.Inventory`, while neither base module references the other. Its public type
remains `Neo.Tools.ShopInventoryGrantBridge` for compatibility.

## Interfaces in Tools
`IDamageable{TakeDamage(int)}`, `IHealable{Heal(int)}`, `IRestorable{Restore()}`, `IPoolable{...}`, `IMover{IsMoving;MoveDelta;MoveToPoint}`, `IInit{InitPriority;Init()}`, `INeoOptionalNetworked{IsNetworked}` (see network.md).

## Code-first snippets (highest-value Tools)
```csharp
using Neo.Tools; using Neo.Extensions;

// spawn/despawn via pool
GameObject bullet = SpawnUtility.Spawn(bulletPrefab, muzzle.position, muzzle.rotation);
SpawnUtility.Despawn(bullet);                       // or bullet.ReturnToPool();

// code-only countdown
Timer t = new Timer(5f, looping: false);
t.OnTimerEnd.AddListener(OnExpired);
t.OnTimerUpdate.AddListener((rem, prog) => bar.value = prog);
t.Start();                                          // t.Dispose() when done

// game flow
GM.I.StartGame();  EM.I.OnWin.AddListener(ShowWin);  GM.I.Win();

// score / counter
ScoreManager.I.Add(100);  int stars = ScoreManager.I.GetCountStars();

// camera shake + follow
cameraShake.StartShake(0.5f, 0.3f);
follow.SetTarget(player); follow.SetPositionSmoothMode(Follow.SmoothMode.SmoothDamp);

// selector (e.g. cycling panels / spawn variants)
selector.SetRandom(); selector.OnSelectionChanged.AddListener(i => Log(i));

// physics events from code
go.GetComponent<PhysicsEvents2D>().TriggerEnterOccurred += col => Hit(col);

// weighted loot (float weights) and exact jackpot odds (integer weights)
ChanceManager loot = new ChanceManager(0.6f, 0.3f, 0.1f);
if (loot.TryEvaluate(out int idx, out _)) Spawn(lootPrefabs[idx]);
IntegerChanceTable jackpot = new IntegerChanceTable(999_999, 1);   // 1 in 1,000,000
bool hit = jackpot.Roll() == 1;

// barks: a contextual line interrupts idle chatter
DialogueLinePool barks = new DialogueLinePool(lines);
barks.LineStarted += line => bubble.text = line.Text;
barks.TryPlay("hit", elapsedSeconds, DialogueLinePool.ContextPriority, out _);

// mouse picking (zero-GC)
MouseInputManager.I.OnClick += d => { if (d.HitObject) Select(d.HitObject); };
```
