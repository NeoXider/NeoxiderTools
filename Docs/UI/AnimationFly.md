# AnimationFly

**What it is:** `Neo.UI.AnimationFly` is a singleton `MonoBehaviour` (`Scripts/UI/AnimationFly.cs`) that spawns
prefab or `Sprite` visuals for bonuses, currency, items and UI icons and flies them to a target with DOTween. It
handles world → UI, UI → UI, UI → world and world → world flights, and can destroy, keep or pool the visuals.
C# callers import `Neo.UI`; components serialized under the former `Neo` namespace keep loading.

**How to use:**
1. Add `Neoxider > UI > AnimationFly` to a scene object. Use one per scene and reach it through `AnimationFly.I`.
2. Fill `Bonus Prefab List` (`Bonus Type`, `Prefab`, `End Pos`). For Canvas effects assign `Parent Canvas` and
   `Spawn Parent` and set `Spawn Space = Canvas`.
3. From code call `AnimationFly.I.Play(new AnimationFly.AnimationFlyRequest { ... })` or a `PlayByType...` method.
4. Grant the reward in `OnReward` or `OnAllArrived`, not right after the call.
5. In complex scenes set `Default Start Space` and `Default End Space` explicitly instead of `Auto`.

---

## Common flows

For a world coin flying to a UI counter: `Spawn Space = Canvas`, `Parent Canvas` is the gameplay Canvas and
`Spawn Parent` is a container under it.

```csharp
AnimationFly.I.PlayByTypeWorldToCanvas(0, amount, worldPickupTransform, moneyTextRectTransform);
AnimationFly.I.PlayByTypeCanvasToCanvas(0, amount, sourceRectTransform, targetRectTransform);
AnimationFly.I.PlayByTypeCanvasToWorld(0, amount, sourceRectTransform, worldTargetTransform);
AnimationFly.I.PlayByTypeWorldToWorld(0, amount, startTransform, endTransform);
```

### Sprite without a prefab, reward once after arrival

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

For world → UI flights the visual spawns under the Canvas, and its start is converted from the world object's
screen position into the local space of the actual `Parent`. This matters when `Parent` is an offset or scaled
container rather than the root Canvas.

### Match a source UI icon, shrink on arrival, speed up one request

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

Timing is `Duration / SpeedMultiplier`; the multiplier also speeds up fountain holds and the delay between items.
`UiSize` wins over `UiSizeSource`. Scale and size are reset before a pooled visual is reused.

## Request lifetime and callbacks

- `Play(request)` works on a copy (`request.Clone()`). Changing or reusing your request object never affects
  running flights, and `Play` never writes back into it (for example a `Type`'s resolved `EndTransform`).
- Set every field before `Play`; follow progress through the returned `AnimationFlyResult`.
- `Clone()` is a shallow copy: callbacks and scene references are shared, values are copied. Use it for templates.
- Every callback (`OnItemStarted`, `OnItemArrived`, `OnReward`, `OnAllArrived`, legacy `onStart` / `onEnd`) runs
  isolated. An exception is logged through `NeoDiagnostics.LogException` and the flight still completes: the
  visual is destroyed or pooled, the arrival is counted and the remaining callbacks run.
- `OwnedVisualCount` is the number of spawned visuals the component still owns (in flight or pooled). Visuals
  destroyed from outside are forgotten on the next `Play`.

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

## Inspector fields

| Field | Description |
|-------|-------------|
| `bonusPrefabList` | List of bonus type, prefab and default target. |
| `bonusType` | Numeric type used by `PlayByType...` and `Execute(...)`. |
| `prefab` | Flying prefab. UI effects usually need a `RectTransform`. |
| `endPos` | Default target for legacy `Execute(type, count, start)` calls. |
| `endSpace` | Target space: `Auto`, `World`, `Canvas`, `Screen`. |
| `defaultStartSpace` / `defaultEndSpace` | How generic `Play(...)` and legacy `Execute(...)` read start and end positions. |
| `spawnSpace` | Where visuals are created and animated: `Auto`, `World`, `Canvas`. |
| `parentCanvas` | Canvas used for coordinate conversion. |
| `spawnParent` | Parent of spawned visuals. For UI, usually a container under the Canvas. |
| `animationCamera` | Camera for world, screen and canvas conversion. Falls back to the Canvas camera or `Camera.main`. |
| `useAnchoredPositionForUI` | Moves UI visuals through `RectTransform.anchoredPosition` instead of world position. |
| `defaultScreenToWorldDepth` | Camera depth used when a Screen or Canvas point becomes a world spawn and no target depth is known. Per component. |
| `flyDuration` | Flight duration. |
| `delayBetweenBonuses` | Delay between spawned visuals. |
| `countMultiplier` | Multiplier for the spawned count. |
| `maxBonusCount` | Global cap per call; a request `MaxCount` can only lower it. |
| `arcStrength` | Arc strength. |
| `middlePoint` | Position of the middle arc point, 0 to 1. |
| `multY` | Multiplier for the vertical arc component. |
| `easyStart` / `easyEnd` | Ease for the first and second half of the flight. |
| `startRandomOffset` / `endRandomOffset` / `middleRandomOffset` | Random offsets of the start, target and middle arc point. |
| `rotateDuringFlight` / `rotationDegrees` | Rotate during the flight, and by how much. |
| `setAsLastSibling` | Moves spawned UI visuals above their siblings. |
| `destroyOnComplete` | Destroys the visual on arrival. Turn it off for manual pooling through `onEnd`. |
| `defaultCompletionMode` | Completion for typed requests: destroy, keep alive, or disable and pool. |
| `maxPoolPerKey` | Pooled visuals per prefab or sprite and resolved spawn space. |
| `scaleMult` | Scale multiplier of spawned visuals. |
| `ignoreZ` | Zeroes start and end Z. |
| `useUnscaledTime` | Uses unscaled time, for pause and menu effects. |
| `isWorldSpace` | Legacy field. With `endSpace = Auto`, `true` treats the target as `World`. |
| `motionPreset` | Default trajectory for typed requests: `Arc`, `Fountain`, `Magnet`, `FountainMagnet`, `Scatter`. |
| `burstOffset` | Initial pop offset for `Fountain` and `FountainMagnet`, in spawn-space units. |
| `burstRandomOffset` | Random spread around the pop point for `Fountain`, `FountainMagnet` and `Scatter`. |
| `burstDurationRatio` | Share of `flyDuration` for the pop stage (0.05–0.85). |
| `burstHoldDuration` | Optional pause after the pop. |
| `magnetDistance` | Distance from the target where `Magnet` presets switch to the final pull. |
| `magnetDurationRatio` | Share of `flyDuration` for the final pull (0.05–0.85). |

`Auto` detects a Canvas by `RectTransform` or a parent `Canvas`. Set `World` or `Canvas` explicitly for
predictable results.

## Motion presets

Typed requests pick a trajectory through `MotionPreset`, falling back to the Inspector `motionPreset`:

- `Arc` — the two-stage arc controlled by `arcStrength`, `middlePoint`, `multY`, `CruiseEase` / `MagnetEase`.
- `Fountain` — items pop up to `BurstOffset` (+ `BurstRandomOffset`), optionally hold, then cruise to the target.
- `Magnet` — items drift to a point `MagnetDistance` from the target, then snap in with `MagnetEase`.
- `FountainMagnet` — a fountain pop followed by the magnet finish.
- `Scatter` — items spray outward within `BurstRandomOffset`, then converge on the target.

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

- `Play(AnimationFlyRequest request)` — one entry point for prefab, sprite, type, parent, spaces, pooling and
  reward timing. The request is copied on entry.
- `PlaySprite(...)` / `PlaySpriteWorldToCanvas(...)` — icon-only effects without a prefab.
- `AnimationFlyResult` — `TotalCount`, `StartedCount`, `CompletedCount`, `IsCompleted` and the active visuals.
- `AnimationFlyRequest.FromPrefab(prefab, count = 1, start = null, end = null)` / `FromSprite(...)` — short
  factories; set the other fields afterwards.
- `AnimationFlyRequest.Clone()` — shallow copy for templates.
- `OwnedVisualCount` — spawned visuals the component still owns.
- `WorldToCanvasPosition` / `CanvasToWorldPosition` — static conversion helpers; see
  [Several AnimationFly components](#several-animationfly-components).

### Request overrides

| Field | Description |
|-------|-------------|
| `Count` / `CountMultiplier` / `MaxCount` | Spawned items; see the formula below. |
| `Duration` | Base duration for this request; otherwise `flyDuration`. |
| `SpeedMultiplier` | Positive speed. `1.5` makes movement, holds and item stagger 1.5x faster. Invalid values fall back to `1`. |
| `DelayBetweenItems` | Per-request spawn delay, before the speed multiplier. |
| `UiSize` | Explicit size of a spawned Canvas `RectTransform`. |
| `UiSizeSource` | Copies the source's rendered size into the actual spawn parent. Ignored when `UiSize` is set. |
| `ScaleMultiplier` | Start scale relative to the visual's original scale. |
| `EndScaleMultiplier` | Optional scale on arrival, relative to the same original scale. |
| `ScaleEase` | Ease of the scale tween; default `Ease.InQuad`. |
| `MotionPreset` | Overrides the trajectory preset. |
| `BurstOffset` / `BurstRandomOffset` | Pop offset and random spread for fountain and scatter presets. |
| `BurstDurationRatio` / `BurstHoldDuration` | Length and optional hold of the pop stage. |
| `MagnetDistance` / `MagnetDurationRatio` | Approach distance and final-pull length for magnet presets. |
| `BurstEase` / `CruiseEase` / `MagnetEase` | Ease overrides for the pop, cruise and magnet stages. |

**Count formula.** Spawned items = `ceil(Count * CountMultiplier * countMultiplier)`, clamped to `[0, cap]`. The
cap is `maxBonusCount`, or `MaxCount` when it is positive and smaller. A product beyond the cap (even beyond the
`int` range) spawns the cap. A NaN or non-positive product spawns nothing and returns `AnimationFlyResult.Empty`.

## NoCode and Inspector

- For simple scenes use `Bonus Prefab List` and legacy `Execute(type, count, start)` calls. Set `End Space` to
  `Canvas` or `World` to match the target.
- `Play` and `PlayByType` take several arguments, so a UnityEvent cannot bind them directly. Point the event at a
  one-method component that builds the request, as the demo's `AnimationFlyDemoController` does. The explicit
  helpers `PlayByTypeWorldToCanvas`, `PlayByTypeCanvasToCanvas`, `PlayByTypeCanvasToWorld` and
  `PlayByTypeWorldToWorld` are the easiest to call from such a component.
- If `Bonus Prefab List` changes at runtime, call `RefreshPrefabCache()`.
- Inspector buttons: `RefreshPrefabCache` rebuilds the type cache. `TestFlyByType(type, bonusCount = 5)` (Play Mode)
  flies a list entry from the spawn parent, or the component itself, to that type's end point.

## Pooling and ownership

- `DisableAndPool` returns finished visuals to a built-in pool, up to `maxPoolPerKey` per prefab or sprite and
  spawn space.
- Canvas and World visuals are pooled separately, so one Sprite can be an `Image` on a Canvas and a
  `SpriteRenderer` in the world.
- A reused visual always gets the resolved request parent. When both `Parent` and `spawnParent` are null, it
  becomes a scene root.
- The component owns active and pooled visuals, including those under external parents. Destroying it stops its
  spawn coroutines, kills their tweens and removes the visuals.
- A finished `KeepAlive` visual, or a legacy one with `destroyOnComplete = false`, passes to the caller and
  survives the component.
- Interrupted requests stay incomplete in `AnimationFlyResult`, and destruction grants no arrival rewards.
  Disabling the component is not a cancellation API.

## Several AnimationFly components

`AnimationFly.I` is the component with `Set Instance On Awake` on. Other components (for example one per Canvas,
with `Set Instance On Awake` off) are called through their own references. Each resolves its own
`Bonus Prefab List`: a type missing from the cache is looked up in the authored list before the call fails, so
`PlayByType`, `Execute(type, ...)`, `GetPrefab`, `GetPos` and typed requests work on extra components in a player
build too. Call `RefreshPrefabCache()` after changing or removing entries at runtime.

`WorldToCanvasPosition` and `CanvasToWorldPosition` accept an explicit camera. Without one, a non-overlay Canvas
uses `Canvas.worldCamera` before the fallback camera; overlay conversion uses no camera. The helpers work without
an `AnimationFly` instance when the Canvas is supplied.

## Demo scene

`Assets/NeoxiderDemos/Demo/Scenes/UI/AnimationFlyDemo.unity` (Demo download). Everything is saved in the scene.

- `[Demo] Services/Animation Fly` — the configured component; try `Test Fly By Type` in Play Mode.
- `[Demo] Frame/Play Panel` — each button calls one method of `AnimationFlyDemoController`, which builds an
  `AnimationFlyRequest`. The log feed at the bottom prints the C# call.
- `[Demo] Frame/Content` — the targets, live callback readouts and tuning sliders.
- `Time · pause menus` toggles — with `useUnscaledTime`, flights keep running at `Time.timeScale = 0`.

## See also

- [UI](./UI.md)
- [Money](../Shop/Money.md)
