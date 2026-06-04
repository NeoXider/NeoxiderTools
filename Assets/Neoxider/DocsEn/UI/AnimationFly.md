# AnimationFly

**Purpose:** singleton fly animation for bonuses, currency, items, and UI icons. The component spawns prefabs and moves them along an arc with DOTween.

Supported flows:

- world object to UI;
- UI object to UI;
- UI object to world object;
- world object to world object.
- prefab or `Sprite` visuals through `AnimationFlyRequest` / `PlaySprite...`;
- reward callbacks can be manual, per arrived visual, or once after all visuals arrive;
- completion policy can destroy, keep alive, or disable-and-pool visuals.

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
| `flyDuration` | Flight duration. |
| `delayBetweenBonuses` | Delay between multiple spawned objects. |
| `countMultiplier` | Multiplier for spawned object count. |
| `maxBonusCount` | Maximum objects spawned by one call. |
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
| `maxPoolPerKey` | Maximum pooled objects per prefab/sprite key. |
| `scaleMult` | Spawned object scale multiplier. |
| `ignoreZ` | Zeroes start and end Z values. |
| `useUnscaledTime` | Uses unscaled time for pause/menu effects. |
| `isWorldSpace` | Legacy compatibility field. When `endSpace = Auto`, `true` treats the target as `World`. |

## NoCode

- For simple scenes, use `Bonus Prefab List` and legacy `Execute(type, count, start)` calls.
- If the target is on Canvas, set `End Space = Canvas`.
- If the target is in the world, set `End Space = World`.
- For UnityEvent wiring, prefer explicit methods: `PlayByTypeWorldToCanvas`, `PlayByTypeCanvasToCanvas`, `PlayByTypeCanvasToWorld`, `PlayByTypeWorldToWorld`.
- If `Bonus Prefab List` changes at runtime, call `RefreshPrefabCache()`.

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

- `Play(AnimationFlyRequest request)` - one universal entry point for prefab, sprite, type, parent, spaces, pooling, and reward timing.
- `PlaySprite(...)` / `PlaySpriteWorldToCanvas(...)` - quick helpers for icon-only effects without prefab authoring.
- `AnimationFlyResult` - exposes `TotalCount`, `StartedCount`, `CompletedCount`, `IsCompleted`, and active visuals.

## Fixed Limitations

- World/UI and UI/UI conversion now resolves positions in the actual `spawnParent` local space instead of the root Canvas.
- `OnReward` can run once after all visuals arrive, avoiding accidental per-coin reward grants.
- `DisableAndPool` returns completed visuals to a built-in pool for the next request.

## See Also

- [UI](./UI.md)
- [Money](../Shop/Money.md)
