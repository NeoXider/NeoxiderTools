# AnimationFly

**What it is:** `Neo.UI.AnimationFly` is a singleton `MonoBehaviour` (`Scripts/UI/AnimationFly.cs`) that spawns
prefab or `Sprite` visuals for bonuses, currency, items and UI icons and flies them to a target with DOTween.
Existing serialized components remain compatible with the former `Neo` namespace; C# callers import `Neo.UI`.

**How to use:**
1. Add `Neoxider > UI > AnimationFly` to a scene object (one per scene; access it through `AnimationFly.I`).
2. For Canvas effects assign `Parent Canvas` and `Spawn Parent`, and set `Spawn Space = Canvas`.
3. From code call `AnimationFly.I.Play(new AnimationFly.AnimationFlyRequest { ... })`, or `PlayByType...` with
   the prefabs of `Bonus Prefab List`. The `Play`/`PlayByType` overloads take several arguments, so a UnityEvent
   cannot bind them directly: point the event at a one-method component that builds the request (the demo's
   `AnimationFlyDemoController` does exactly that). `Test Fly By Type` flies a list entry from the Inspector.
4. Grant the reward in `OnReward` (or `OnAllArrived`) instead of right after the call.

---

Supported flows:

- world object to UI;
- UI object to UI;
- UI object to world object;
- world object to world object.
- prefab or `Sprite` visuals through `AnimationFlyRequest` / `PlaySprite...`;
- reward callbacks can be manual, per arrived visual, or once after all visuals arrive;
- completion policy can destroy, keep alive, or disable-and-pool visuals.

## Demo Scene

The demo scene is `Assets/NeoxiderDemos/Demo/Scenes/UI/AnimationFlyDemo.unity` (Demo download).

Everything is saved in the scene, so the wiring is visible in the Hierarchy and Inspector without entering Play Mode (the scene is authored by the editor builder `AnimationFlyDemoBuilder`, nothing is built at runtime):

| Object | What to look at |
|---|---|
| `[Demo] Services/Animation Fly` | The configured `AnimationFly`: `Parent Canvas`, `Spawn Parent` = `[Demo] Frame/Fly Layer`, `Spawn Space = Canvas`, `Use Unscaled Time`, and `Bonus Prefab List` entry 0 (glow-gem prefab flying to the gem counter; try `Test Fly By Type` in Play Mode). |
| `[Demo] World/World Pickup`, `World Chest` | World-space start points (sprites behind the overlay canvas). |
| `[Demo] Frame/Content` | Wallet and gem targets (`EndTransform` of coin and gem flights), live request callbacks (`OnReward`, `OnItemStarted`, `OwnedVisualCount`) and the tuning sliders (`Count`, `Duration`, `Delay`, `Arc` in canvas pixels, `Scale`, `Rotation`). |
| `[Demo] Frame/Play Panel` | Play buttons. Each `Button.onClick` calls one public method of `AnimationFlyDemoController`, which builds an `AnimationFlyRequest` and calls `AnimationFly.Play`: World → Wallet, Screen point → Wallet, UI burst / Scatter → Gems, `Fountain`, `Magnet`, `Fountain + Magnet`, a sample `Sprite` asset, a pooled prefab (`DisableAndPool`) and the Inspector-list path (`PlayByType`). The buttons use `ButtonScale` for the press effect. |
| `Time · pause menus` toggles | `Time.timeScale = 0` and `AnimationFly.useUnscaledTime`: with unscaled time the flights (and the `ButtonScale` press) keep running while the game is paused. |

The log feed at the bottom prints the C# call behind every button.

## Setup

1. Add `Neoxider > UI > AnimationFly` to a scene object.
2. Fill `Bonus Prefab List`: `Bonus Type`, `Prefab`, `End Pos`.
3. For Canvas effects, assign `Parent Canvas`, `Spawn Parent`, and set `Spawn Space = Canvas`.
4. In complex scenes, set `Default Start Space` / `Default End Space` explicitly instead of relying on `Auto`.

## Common Flows

### World coin flies to a UI counter

1. `Spawn Space = Canvas`.
2. `Parent Canvas` points to the gameplay Canvas.
3. `Spawn Parent` is a container under that Canvas.

```csharp
AnimationFly.I.PlayByTypeWorldToCanvas(0, amount, worldPickupTransform, moneyTextRectTransform);
```

### UI icon flies to a UI counter

```csharp
AnimationFly.I.PlayByTypeCanvasToCanvas(0, amount, sourceRectTransform, targetRectTransform);
```

### UI object flies to a world object

```csharp
AnimationFly.I.PlayByTypeCanvasToWorld(0, amount, sourceRectTransform, worldTargetTransform);
```

### World-space flight

```csharp
AnimationFly.I.PlayByTypeWorldToWorld(0, amount, startTransform, endTransform);
```

### Sprite without prefab and grant once after arrival

```csharp
AnimationFly.I.Play(new AnimationFly.AnimationFlyRequest
{
    Sprite = coinSprite,
    Count = 10,
    StartTransform = worldPickup,
    EndTransform = moneyCounterRect,
    StartSpace = AnimationFlyCoordinateSpace.World,
    EndSpace = AnimationFlyCoordinateSpace.Canvas,
    SpawnSpace = AnimationFlySpawnSpace.Canvas,
    Parent = flyContainer,
    RewardTiming = AnimationFlyRewardTiming.OnAllArrived,
    OnReward = () => money.Add(10)
});
```

For world -> UI rewards, the visual is spawned under the Canvas, but its start position is calculated from the world object's screen position into the actual `Parent` local space. This matters when `Parent` is an offset/scaled container rather than the root Canvas.

### Match a source UI icon, shrink on arrival, and speed up one request

```csharp
AnimationFly.I.Play(new AnimationFly.AnimationFlyRequest
{
    Sprite = rewardSprite,
    Count = rewardAmount,
    StartTransform = winningSymbol,
    EndTransform = currencyCounter,
    StartSpace = AnimationFlyCoordinateSpace.Canvas,
    EndSpace = AnimationFlyCoordinateSpace.Canvas,
    SpawnSpace = AnimationFlySpawnSpace.Canvas,
    Parent = flyContainer,

    // The spawned image starts at the symbol's rendered size, even when the
    // source and fly container have different RectTransform scales.
    UiSizeSource = winningSymbol,
    SpeedMultiplier = 1.5f,
    EndScaleMultiplier = 0.35f,
    ScaleEase = DG.Tweening.Ease.InQuad
});
```

Request timing is resolved as `Duration / SpeedMultiplier`. The same speed multiplier is applied to fountain holds and the delay between items. `UiSize` takes priority over `UiSizeSource` when both are supplied. Scale and size are reset before a pooled visual is reused.

### Request lifetime and callbacks

- `Play(request)` works on a copy of the request (`request.Clone()`). Changing or reusing the same request
  object for the next call never retargets or re-wires flights that are already running, and resolving a
  `Type` no longer writes the type's `endPos` back into your request.
- Keep a configured request as a template with `Clone()` (shallow copy: callbacks and scene references are
  shared, values are copied).
- Upgrade note: fields assigned on the request after `Play` (for example `request.OnAllArrived = ...`) no
  longer affect the running flight, and a `Type` request no longer gets its resolved `EndTransform` written
  back. Set everything before `Play`; use the returned `AnimationFlyResult` to follow progress.
- Every callback (`OnItemStarted`, `OnItemArrived`, `OnReward`, `OnAllArrived`, legacy `onStart` / `onEnd`)
  runs isolated. An exception is logged through `NeoDiagnostics.LogException` and the flight still completes:
  the visual is destroyed or pooled, `AnimationFlyResult` counts the arrival and the remaining callbacks run.
- `OwnedVisualCount` reports how many spawned visuals the component still owns (in flight or pooled).
  Visuals destroyed from outside are forgotten on the next `Play`.

```csharp
AnimationFly.AnimationFlyRequest coinTemplate = new AnimationFly.AnimationFlyRequest
{
    Sprite = coinSprite,
    SpawnSpace = AnimationFlySpawnSpace.Canvas,
    EndTransform = walletRect,
    RewardTiming = AnimationFlyRewardTiming.OnEachArrived
};

AnimationFly.AnimationFlyRequest request = coinTemplate.Clone();
request.StartTransform = pickup;
request.Count = coins;
request.OnReward = () => wallet.Add(1);
AnimationFly.AnimationFlyResult flight = AnimationFly.I.Play(request);
```

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `bonusPrefabList` | Bonus type, prefab, and default target list. |
| `bonusType` | Numeric bonus type used by `PlayByType...` / `Execute(...)`. |
| `prefab` | Flying object prefab. UI effects usually need a `RectTransform`. |
| `endPos` | Default target for legacy `Execute(type, count, start)` calls. |
| `endSpace` | Target coordinate space: `Auto`, `World`, `Canvas`, `Screen`. |
| `defaultStartSpace` | How generic `Play(...)` and legacy `Execute(...)` calls read start positions. |
| `defaultEndSpace` | How generic `Play(...)` and legacy `Execute(...)` calls read end positions. |
| `spawnSpace` | Where spawned objects are created and animated: `Auto`, `World`, `Canvas`. |
| `parentCanvas` | Canvas used for coordinate conversion. |
| `spawnParent` | Parent for spawned objects. For UI, usually a container under Canvas. |
| `animationCamera` | Camera used for world/screen/canvas conversion. Falls back to Canvas camera or `Camera.main`. |
| `useAnchoredPositionForUI` | Moves UI objects through `RectTransform.anchoredPosition` instead of world position. |
| `defaultScreenToWorldDepth` | Camera depth used when a Screen/Canvas point is converted to a world spawn space and no target depth is known. Each component uses its own value. |
| `flyDuration` | Flight duration. |
| `delayBetweenBonuses` | Delay between multiple spawned objects. |
| `countMultiplier` | Multiplier for spawned object count. |
| `maxBonusCount` | Maximum objects spawned by one call (the global cap; a request `MaxCount` can only lower it). |
| `arcStrength` | Arc strength. |
| `middlePoint` | Middle arc point position from 0 to 1. |
| `multY` | Multiplier for the vertical arc component. |
| `easyStart` / `easyEnd` | Ease for the first and second half of the flight. |
| `startRandomOffset` | Random start offset. |
| `endRandomOffset` | Random target offset. |
| `middleRandomOffset` | Random middle arc offset. |
| `rotateDuringFlight` | Rotates the object during flight. |
| `rotationDegrees` | Rotation amount during the flight. |
| `setAsLastSibling` | Moves spawned UI objects above their siblings. |
| `destroyOnComplete` | Destroys the object after arrival. Disable it for manual pooling through `onEnd`. |
| `defaultCompletionMode` | Completion behavior for typed requests: destroy, keep alive, or disable-and-pool. |
| `maxPoolPerKey` | Maximum pooled objects per prefab/sprite and resolved spawn-space pair. |
| `scaleMult` | Spawned object scale multiplier. |
| `ignoreZ` | Zeroes start and end Z values. |
| `useUnscaledTime` | Uses unscaled time for pause/menu effects. |
| `isWorldSpace` | Legacy compatibility field. When `endSpace = Auto`, `true` treats the target as `World`. |
| `motionPreset` | Default trajectory preset for typed requests: `Arc`, `Fountain`, `Magnet`, `FountainMagnet`, `Scatter`. |
| `burstOffset` | Initial pop offset for `Fountain` / `FountainMagnet`, in resolved spawn-space units. |
| `burstRandomOffset` | Random spread around the pop point for `Fountain`, `FountainMagnet`, and `Scatter`. |
| `burstDurationRatio` | Fraction of `flyDuration` reserved for the initial pop stage (0.05-0.85). |
| `burstHoldDuration` | Optional pause after the pop before items fly to the target. |
| `magnetDistance` | Distance from the target where `Magnet` presets switch to the final pull. |
| `magnetDurationRatio` | Fraction of `flyDuration` reserved for the final magnet pull (0.05-0.85). |

## Motion presets

Typed `AnimationFlyRequest` calls choose a trajectory through `MotionPreset` (falling back to the
inspector `motionPreset`):

- `Arc` - the classic two-stage arc controlled by `arcStrength`, `middlePoint`, `multY`, `CruiseEase` / `MagnetEase`.
- `Fountain` - items pop up to `BurstOffset` (+ `BurstRandomOffset`), optionally hold, then cruise to the target.
- `Magnet` - items drift to an approach point `MagnetDistance` from the target, then snap in with `MagnetEase`.
- `FountainMagnet` - a fountain pop followed by the magnet finish.
- `Scatter` - items spray outward within `BurstRandomOffset`, then converge on the target.

## NoCode

- For simple scenes, use `Bonus Prefab List` and legacy `Execute(type, count, start)` calls.
- If the target is on Canvas, set `End Space = Canvas`.
- If the target is in the world, set `End Space = World`.
- For UnityEvent wiring, prefer explicit methods: `PlayByTypeWorldToCanvas`, `PlayByTypeCanvasToCanvas`, `PlayByTypeCanvasToWorld`, `PlayByTypeWorldToWorld`.
- If `Bonus Prefab List` changes at runtime, call `RefreshPrefabCache()`.
- Inspector buttons (`[Button]`): `RefreshPrefabCache` rebuilds the type cache, and
  `TestFlyByType(type, bonusCount = 5)` runs a smoke-test flight from the spawn parent (or the component
  itself) to the end point configured for that type — handy for tuning a scene without wiring UI (Play Mode).

## Public API

```csharp
AnimationFly.I.PlayByType(
    type: 0,
    bonusCount: 5,
    start: startTransform,
    end: endTransform,
    startSpace: AnimationFlyCoordinateSpace.World,
    endSpace: AnimationFlyCoordinateSpace.Canvas);

AnimationFly.I.Play(
    prefab,
    bonusCount: 5,
    start: startTransform,
    end: endTransform,
    startSpace: AnimationFlyCoordinateSpace.World,
    endSpace: AnimationFlyCoordinateSpace.Canvas,
    parent: canvasContainer,
    onStart: spawned => spawned.SetActive(true),
    onEnd: spawned => Debug.Log("Arrived"));
```

`Auto` detects Canvas by `RectTransform` or a parent `Canvas`. For predictable behavior, set `World` / `Canvas` explicitly.

New typed API:

- `Play(AnimationFlyRequest request)` - one universal entry point for prefab, sprite, type, parent, spaces, pooling, and reward timing. The request is copied on entry.
- `PlaySprite(...)` / `PlaySpriteWorldToCanvas(...)` - quick helpers for icon-only effects without prefab authoring.
- `AnimationFlyResult` - exposes `TotalCount`, `StartedCount`, `CompletedCount`, `IsCompleted`, and active visuals.
- `AnimationFlyRequest.FromPrefab(prefab, count = 1, start = null, end = null)` / `FromSprite(...)` - short factories; set the remaining fields afterwards.
- `AnimationFlyRequest.Clone()` - shallow copy for request templates.
- `OwnedVisualCount` - number of spawned visuals the component still owns.

Important `AnimationFlyRequest` overrides:

| Field | Description |
|-------|-------------|
| `Count` / `CountMultiplier` / `MaxCount` | Spawned items: `ceil(Count * CountMultiplier * countMultiplier)`, clamped to `[0, cap]`. The cap is `maxBonusCount`, or the smaller `MaxCount` when it is positive. A product beyond the cap (even beyond `int` range) spawns the cap; a NaN or non-positive product spawns nothing and returns `AnimationFlyResult.Empty`. |
| `Duration` | Optional base duration for this request; otherwise uses `flyDuration`. |
| `SpeedMultiplier` | Positive request speed. `1.5` makes movement, holds, and item stagger 1.5x faster. Invalid values safely fall back to `1`. |
| `DelayBetweenItems` | Optional per-request spawn delay before the speed multiplier is applied. |
| `UiSize` | Explicit size for a spawned Canvas `RectTransform`. |
| `UiSizeSource` | Copies the source's rendered world-corner size into the actual spawn parent. Ignored when `UiSize` is set. |
| `ScaleMultiplier` | Start scale relative to the prefab/sprite visual's original scale. |
| `EndScaleMultiplier` | Optional scale at arrival, relative to the same original scale. |
| `ScaleEase` | Ease for the scale tween; defaults to `Ease.InQuad`. |
| `MotionPreset` | Overrides the trajectory preset (`Arc`, `Fountain`, `Magnet`, `FountainMagnet`, `Scatter`). |
| `BurstOffset` / `BurstRandomOffset` | Pop offset and random spread for fountain/scatter presets. |
| `BurstDurationRatio` / `BurstHoldDuration` | Length and optional hold of the initial pop stage. |
| `MagnetDistance` / `MagnetDurationRatio` | Approach distance and final-pull length for magnet presets. |
| `BurstEase` / `CruiseEase` / `MagnetEase` | Ease overrides for the pop, cruise, and magnet stages. |

## Fixed Limitations

- World/UI and UI/UI conversion now resolves positions in the actual `spawnParent` local space instead of the root Canvas.
- `OnReward` can run once after all visuals arrive, avoiding accidental per-coin reward grants.
- `DisableAndPool` returns completed visuals to a built-in pool for the next request.

Pools separate Canvas and World visuals: the same Sprite can safely produce an `Image` on Canvas and a
`SpriteRenderer` in the world. A reused visual is always assigned the resolved request parent; when both
`Parent` and `spawnParent` are null, it becomes a scene root rather than retaining an earlier container.

The component owns active and pooled visuals, including objects under external parents. Destroying it
stops its spawn coroutines, kills owned visual tweens and removes those visuals. A completed `KeepAlive`
visual (or a legacy visual completed with `destroyOnComplete = false`) is transferred to the caller and
survives the component. Interrupted requests remain incomplete in `AnimationFlyResult`; destruction does
not grant arrival rewards. Disabling the component is not a cancellation API.

### Several AnimationFly components

`AnimationFly.I` is the component whose `Set Instance On Awake` is on. Further components (for example one per
Canvas, with `Set Instance On Awake` off) are called through their own references. Every component resolves
its `Bonus Prefab List` types on its own: a type missing from the cache is looked up in the authored list
before the call fails, so `PlayByType`, `Execute(type, ...)`, `GetPrefab`, `GetPos` and typed requests work on
extra components in a player build too. Call `RefreshPrefabCache()` after you change or remove list entries
at runtime.

`WorldToCanvasPosition` and `CanvasToWorldPosition` accept an explicit camera. When it is omitted for a
non-overlay Canvas, they use `Canvas.worldCamera` before the fallback camera. Overlay conversion uses no
event camera. These helpers can be called without an `AnimationFly` instance when the Canvas is supplied.

## Performance

- One yield instruction is created per flight, not per spawned item; it is rebuilt only when the delay or
  the time mode changes.
- `DisableAndPool` reuses visuals per prefab/sprite and spawn space up to `maxPoolPerKey`.
- `UiSizeSource` measurement uses a shared corner buffer instead of allocating per request.

## Code structure

The component keeps its serialized fields and public API; the rules it applies live in plain C# types next
to it so they are testable without a scene:

- `AnimationFlyMath` (`Scripts/UI/AnimationFlyMath.cs`) - item count, duration, delay, speed multiplier,
  stage ratios and the magnet approach point.
- `AnimationFlyVisualPool` (`Scripts/UI/AnimationFlyVisualPool.cs`) - ownership of spawned visuals, per-key
  pooling, KeepAlive hand-over, pruning of externally destroyed visuals and teardown.

Both are internal to `Neo.UI`; EditMode coverage lives in `Tests/Edit/UI/AnimationFlyCoreTests.cs`.

## See Also

- [UI](./UI.md)
- [Money](../Shop/Money.md)
