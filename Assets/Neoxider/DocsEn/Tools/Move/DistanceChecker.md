# DistanceChecker

## Overview
`DistanceChecker` monitors distance between two objects and fires events when crossing a threshold (approach/depart).
It uses squared distance internally to reduce overhead, and optionally exposes a reactive distance value for UI.

- **Namespace**: `Neo.Tools`
- **Path**: `Assets/Neoxider/Scripts/Tools/Move/DistanceChecker.cs`

## Key concepts
- **Source / Target**: `currentObject` (defaults to this `transform`) and `targetObject`
- **Threshold**: `distanceThreshold` with optional hysteresis (`hysteresisOffset`) to avoid boundary flicker
- **Update Mode**:
  - `EveryFrame`
  - `FixedInterval` (controlled by `updateInterval`)

## Events
- `onApproach`
- `onDepart`
- (optional) continuous distance updates via `Distance` reactive property

## Public API (selected)
- `float GetCurrentDistance()`
- `bool IsWithinDistance()`
- `void SetTarget(Transform newTarget)`
- `void SetDistanceThreshold(float threshold)`
- `void SetUpdateMode(UpdateMode mode)`
- `void SetUpdateInterval(float interval)`
- `void SetContinuousTracking(bool enabled)`
- `void ForceCheck()`

---

## See also
- [`Move`](./README.md)
