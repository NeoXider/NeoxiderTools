# DistanceChecker

**What it is:** a `MonoBehaviour` (`Scripts/Tools/Move/DistanceChecker.cs`, namespace `Neo.Tools`) that monitors the distance between two transforms and fires events when it crosses a threshold (approach/depart). It compares squared distances and can expose the live distance as a reactive value for UI.

**How to use:**
1. Add the component; `currentObject` defaults to this transform. Assign `targetObject` (or call `SetTarget`).
2. Set `distanceThreshold` and an optional `hysteresisOffset` to avoid boundary flicker.
3. Connect `onApproach` / `onDepart`, or subscribe to `Distance.OnChanged` with **Continuous Tracking** enabled.

---

## Key concepts
- **Source / Target**: `currentObject` (defaults to this `transform`) and `targetObject`.
- **Threshold**: approach when closer than `distanceThreshold`; depart when farther than `distanceThreshold + hysteresisOffset`.
- **Update Mode**: `EveryFrame`, or `FixedInterval` (controlled by `updateInterval`, scaled time).
- **Lost target**: when the target or source is destroyed or cleared while inside the threshold, the checker leaves the inside state once: `onDepart` fires and `IsWithinDistance()` returns `false`.

## Events
- `onApproach`
- `onDepart`
- `Distance` (`ReactivePropertyFloat`): continuous distance when **Continuous Tracking** is on; changes smaller than `continuousEventThreshold` are not published.

## Public API (selected)
- `float GetCurrentDistance()` — `float.MaxValue` without both transforms.
- `bool IsWithinDistance()`
- `float DistanceValue` — last published reactive distance.
- `void SetTarget(Transform newTarget)` / `void SetCurrentObject(Transform newCurrent)`
- `void SetDistanceThreshold(float threshold)`
- `void SetUpdateMode(UpdateMode mode)` / `void SetUpdateInterval(float interval)`
- `void SetContinuousTracking(bool enabled)`
- `void ForceCheck()` — evaluate immediately (also releases a lost target).

```csharp
[SerializeField] private DistanceChecker _aggroRange;

private void Start()
{
    _aggroRange.onApproach.AddListener(StartChase);
    _aggroRange.onDepart.AddListener(StopChase);
}
```

---

## See also
- [`Move`](./README.md)
