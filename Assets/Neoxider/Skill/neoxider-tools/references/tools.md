# Neo.Tools — component catalog (the big one)

`Neo.Tools` is the package's catch-all and has MANY components. Catalog below grouped by category; for
each: purpose + key public API (verify exact signatures against `Assets/Neoxider/Scripts/Tools/` before
use). Singletons expose `static T I` / `Instance`. Components marked **[no-code]** are inspector-wiring
primary — avoid building on them in code (see avoid-nocode.md).

## Managers
- **`Singleton<T>`** — base: `I`/`Instance`, `HasInstance`, `TryGetInstance(out T)`, `DestroyInstance()`.
- **`GM`** (`GM.I`) — game state. `enum GameState {NotStarted,Menu,Preparing,Game,Win,Lose,End,Pause,Other}`; `State{get;set}`; `StartGame(bool restart=false)`, `StopGame()`, `Win()`, `Lose()`, `End()`, `Pause()`, `Resume()`, `Menu()`; `IsPlaying`.
- **`EM`** (`EM.I`) **[no-code]** — event hub of `UnityEvent`s (`OnGameStart/OnWin/OnLose/OnStateChange<GameState>/OnPause/...`). Static fire shortcuts: `EM.GameStart()`, `EM.Win()`, `EM.Lose()`, `EM.Pause()`, `EM.PlayerDied()`, … Subscribe from code with `EM.I.OnWin.AddListener(...)`.
- **`Bootstrap`** (`Bootstrap.I`, DontDestroyOnLoad) — runs `IInit.Init()` by `InitPriority`. `interface IInit{int InitPriority;void Init();}`.

## Spawner / Pool (prefer these over Instantiate/Destroy)
- **`SpawnUtility`** (static) — main entry: `Spawn(prefab[,pos][,rot][,parent])`, `Despawn(instance)`, `ClearFallbackPools()`. Uses `PoolManager` if present, else internal fallback pools.
- **`PoolManager`** (`PoolManager.I`, DontDestroyOnLoad) — `static Get(prefab,pos,rot,parent=null)`, `static Release(instance)`; pre-warm via `_preconfiguredPools`.
- **`Spawner`** — interval/wave area spawner. `StartSpawn()`/`StopSpawn()`/`Clear()`, `SpawnRandomObject()`, `SpawnById(int,pos)`, `ResolveSpawnPoint()`; events `OnObjectSpawned`, `OnWaveStarted`. `SpawnMode.Loop|Waves`. Spawn location: `Spawn Points` (`Transform[]` — empty = own transform, else random point per spawn) or a `Spawn Area` collider (2D/3D).
- **`SimpleSpawner`** — `Spawn()` single-shot. **`Despawner`** — `Despawn()`, `DespawnOther(go)`, static `DespawnObject(go)`, `OnDespawn`.
- **`IPoolable`** {`OnPoolCreate/OnPoolGet/OnPoolRelease`}; **`PoolableBehaviour`** base (override these). **`PoolExtensions`**: `go.ReturnToPool()`, `go.SpawnFromPool(pos,rot,parent)`.

## Score / Counters
- **`ScoreManager`** (`ScoreManager.I`, saves best) — `Add(int,bool updateBest=true)`, `Set(int,...)`, `SetBestScore(int?)`, `ResetScore()`, `GetCountStars()`; props `ScoreValue`, `BestScoreValue`, reactive `Score/BestScore/Progress/CountStarsReactive`.
- **`Counter`** — numeric with optional save key + Mirror sync. `Add/Subtract/Multiply/Divide/Set(int|float)`, `Send()`, `LoadFromSave()`; `ValueInt`, `ValueFloat`, reactive `Value`; `OnValueChangedInt/Float`; `static Dictionary<string,List<Counter>> Registry`.
- **`ModifyCounterByKey`** — `Execute()` modifies a Counter/Money by save key. **`RevertAmount`** — `Amount(a)` → fires `OnChange(1-a)`.

## Time
- **`Timer`** (`[Serializable]`, `IDisposable`, UniTask — NOT a MonoBehaviour) — `new Timer(duration,updateInterval=0.05f,looping=false,useUnscaledTime=false)`; `Start()`/`Stop()`/`Pause()`/`Resume()`, `AddTime(s)`, `SetProgress(f)`, `Dispose()`; events `OnTimerStart/End`, `OnTimerUpdate(remaining,progress)`; props `IsRunning`, `RemainingTime`, `Progress`.
- **`TimerObject`** — scene timer wrapping `Timer` (count up/down, loop, day-cycle, milestones, save, auto UI). `Play()/Stop()/Pause()/Resume()`, `SetTime/SetProgress/AddTime`, reactive `Time`, many events.
- **`GameTimeController`** — `PauseGame()` (timeScale 0), `ResumeGame()`, `SetTimeScale(f)`.

## Move / Camera
- **`Follow`** — `SetTarget(t)`, `SetOffset`, `SetPositionSpeed`, `SetStoppingDistance`, `TeleportToTarget()`, `SmoothMode{None,MoveTowards,Lerp,SmoothDamp,Exponential}`; events `onStartFollowing/onStopFollowing/onTargetLost`.
- **`ConstantMover`** — `SetSpeed(f)`; `MovementMode{Transform,Rigidbody,Rigidbody2D}`. **`ConstantRotator`** — `SetDegreesPerSecond(f)`.
- **`UniversalRotator`** — aim at Transform/point/mouse: `SetTarget`, `RotateTo(point,instant)`, `RotateBy(deg)`; 2D/3D, axis limits.
- **`KeyboardMover`** (`IMover`) — `MoveDelta(v2)`, `MoveToPoint(v2)`; `MovementPlane{XY,XZ,YZ}`. **`MouseMover2D/3D`** — follow mouse.
- **`DistanceChecker`** — `GetCurrentDistance()`, `IsWithinDistance()`, `SetTarget`, reactive `Distance`; events `onApproach/onDepart`.
- **`PlayerController2DPhysics` / `PlayerController3DPhysics`** (Mirror-aware) — `SetMovementEnabled/SetJumpEnabled/SetLookEnabled`, `Teleport(pos)`, external input `SetMoveInput(v?)/SetJumpInput()/SetRunInput(bool)`; props `IsGrounded`, `IsRunning`. Animator drivers `PlayerController2D/3DAnimatorDriver` **[no-code]** observe them.
- **`CameraShake`** (DOTween) — `StartShake([dur,strength])`, `StopShake()`, `ShakeType{Position,Rotation,Both}`. **`CameraRotationController`**, **`FreeFlyCameraController`** (`SetControllerEnabled`, `Warp`, `Tick(dt)`), **`CameraConstraint`**, **`CameraAspectRatioScaler`** **[no-code]** (`ScaleMode{FitWidth,FitHeight,FitBoth,Manual}`), **`CursorLockController`**, **`ScreenPositioner`** (`Configure(edge,offset,depth)`).

## Interaction / Physics
- **`InteractiveObject`** (Mirror-aware) — hover/click/range; `interactable`, `InteractionDistance`, `IsHovered`, `IsInInteractionRange`, `UseScreenCenterRay`; events `onInteractDown/Up`, `onHoverEnter/Exit`, `onClick/onDoubleClick/onRightClick`, `onEnterRange/onExitRange`.
- **`PhysicsEvents2D` / `PhysicsEvents3D`** **[no-code]** (Mirror-aware) — forward trigger/collision to UnityEvents AND C# events (`TriggerEnterOccurred`, `CollisionEnterOccurred`); tag/layer filters.
- **`ToggleObject`** — `Toggle()`, `Set(bool)`, reactive `Value`, `OnChangeFlip(bool)`.
- **Physics zones**: **`ImpulseZone`** (`ApplyImpulseToObject(go)`, `ImpulseDirection{AwayFromCenter,TowardsCenter,...}`), **`ExplosiveForce`** (`Explode([force])`, `ActivationMode{OnStart,OnAwake,Delayed,Manual}`), **`MagneticField`** (`SetMode(FieldMode{Attract,Repel,ToTarget,ToPoint,Direction})`, `SetStrength/SetRadius`, `OnObjectEntered/Exited`).
- **`AdvancedForceApplier`** — knockback used by attack system.

## View / UI helpers
- **`Selector`** (Mirror-aware) — which child is active: `Set(int)`, `Next()/Previous()`, `SetRandom([deactivateOthers])`, unique-random mode, exclude lists; props `Value`, `Count`, `Item`, `Items`, `FillMode`, `UniqueSelectionMode`; events `OnSelectionChanged(int)`, `OnFinished`, `OnUniqueCycleComplete`. (`SelectorItem` **[no-code]** child.)
- **`ImageFillAmountAnimator`** (DOTween) — `SetValue(0..1)`, `SetBool(bool)`. **`BillboardUniversal`** — `SetBillboardMode(TowardsCamera|AwayFromCamera|TowardsDirection)`. **`ZPositionAdjuster`** — 2D depth from Y.
- **[no-code] view bits**: `TextScore` (auto-binds ScoreManager), `StarView` (binds CountStars), `LightAnimator`, `MeshEmission`, `DOTweenUIImageFallback`, `UpdateChilds`.

## Text
- **`SetText`** (DOTween for number anim) — `Set(int|float|string)`, `SetPercentage(v,addSign=true)`, `SetCurrency(v,sym="$")`, `SetBigInteger(v)`, `SetFormatted(v,NumberFormatOptions)`, `Clear()`; `OnTextUpdated(string)`.
- **`TimeToText`** — THE component for showing seconds on a TMP label (countdown/elapsed). `Set(float)`
  (or wire a `UnityEvent<float>` → `Set`); `TimeFormat` (e.g. `MinutesSeconds` → `mm:ss`),
  `TimeDisplayMode{Clock,Compact}`, `startAddText`/`endAddText` prefix/suffix, auto-grabs `TMP_Text`;
  static `FormatTime(t,TimeFormat,sep)`. **Don't write your own timer-text MonoBehaviour.**

## Input
- **`MouseInputManager`** (`MouseInputManager.I`, DontDestroyOnLoad, zero-GC) — C# events `OnPress/OnHold/OnRelease/OnClick` with `MouseEventData{ScreenPosition,WorldPosition,HitObject,Hit3D,Hit2D}`; `SetTargetCamera(cam)`; static `LastEventData`.
- **`MouseEffect`**, **`MultiKeyEventTrigger`** (key combo → UnityEvent), **`SwipeController`** (swipe direction events). Compat helpers `MouseInputCompat`/`KeyInputCompat` (Input + new-InputSystem fallback), `OptionalInputSystemAdapter`.

## Debug
- **`FPS`** (TMP) — `CurrentFps`, `SetTargetFramerate(int)`, `SetVSync(bool)`; color thresholds.
- **`NeoDebugOverlay`** **[no-code]** — IMGUI overlay (FPS/scene/timescale/AM+SaveManager), toggle `_toggleKey` (F3).
- **`ErrorLogger`** — captures `logMessageReceived`, optional file output.

## Draw
- **`Drawer`** — mouse/touch line drawing (Chaikin smoothing, EdgeCollider2D, pooling): `BeginLine(pos)`, `AppendPoint(pos)`, `EndLine()`, `DeleteFirst/Last/All()`, `CreateCollider()`; reactive `Distance`/`PointCount`; static `Smooth(points,passes,fixedZ)`, `LimitPoints(pts,max)`.

## Dialogue / Leaderboard / Random
- **`DialogueController`** (UniTask) — `StartDialogue([d,m,s])`, `NextSentence/NextMonolog/NextDialogue()`, `SkipOrNext()`, `Advance()`, `CompleteTypewriter()`; events `OnSentenceEnd/OnMonologEnd/OnDialogueEnd/OnAllDialoguesEnd`, `OnCharacterTyped(char)`. Data `Dialogue[]` (→`Monolog[]`→`Sentence[]`). `DialogueUI`, `DialogueData` (SO).
- **`TypewriterEffect`** (`[Serializable]`, UniTask) — `PlayAsync(text,onTextChanged,ct)`, `Stop()`, `Complete()`; `IsTyping`, `Progress`; events `OnComplete`, `OnCharacterTyped(char)`.
- **`Leaderboard`** (`Leaderboard.I`, fake players) — `UpdatePlayerScore(int,overrideBest=false)`, `UpdatePlayerName(s)`, `Sort()`, `GetIdPlayer()`. `LeaderboardItem`/`LeaderboardMove` rows.
- **`ChanceManager`** (`[Serializable]`) — weighted table: `new ChanceManager(params float[] weights)`, `AddEntry(weight,...)`, `GetChanceId()`, `Evaluate()`, `TryEvaluate(out idx,out entry)`, `Normalize()`. **`ChanceSystemBehaviour`** scene wrapper — `GenerateId()`, `EvaluateAndNotify()`, per-index `EventsByIndex`. `ChanceData` (SO).

## NPC nav (note)
`AiNavigation (Legacy)` under Tools is `[Obsolete]` → use **`Neo.NPC.NpcNavigation`** for new code. The legacy `AttackSystem` (`Health`/`AttackExecution`/`AdvancedAttackCollider`) is `[Obsolete]` → use **`Neo.Rpg`**.

## Interfaces in Tools
`IDamageable{TakeDamage(int)}`, `IHealable{Heal(int)}`, `IRestorable{Restore()}`, `IPoolable{...}`, `IMover{IsMoving;MoveDelta;MoveToPoint}`, `IInit{InitPriority;Init()}`, `INeoOptionalNetworked{IsNetworked}` (see network.md).

## Code-first snippets (highest-value Tools)
```csharp
using Neo.Tools; using Neo.Extensions;

// spawn/despawn via pool
var bullet = SpawnUtility.Spawn(bulletPrefab, muzzle.position, muzzle.rotation);
SpawnUtility.Despawn(bullet);                       // or bullet.ReturnToPool();

// code-only countdown
var t = new Timer(5f, looping:false);
t.OnTimerEnd.AddListener(OnExpired);
t.OnTimerUpdate.AddListener((rem,prog) => bar.value = prog);
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

// weighted loot
var loot = new ChanceManager(0.6f, 0.3f, 0.1f);
if (loot.TryEvaluate(out int idx, out _)) Spawn(lootPrefabs[idx]);

// mouse picking (zero-GC)
MouseInputManager.I.OnClick += d => { if (d.HitObject) Select(d.HitObject); };
```
