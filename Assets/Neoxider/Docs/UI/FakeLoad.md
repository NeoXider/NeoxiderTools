# FakeLoad

**Purpose:** plays a fake loading progress over a random duration and drives progress-bar events, for splash/loading screens. Useful when there is nothing real to wait for but the UI should feel like it loads.

## Setup

- Add `Neoxider > UI > FakeLoad` (prefab: `Prefabs/UI/Page/Fake Load.prefab`).
- Bind a progress bar to `OnChange` (0..1) or `OnChangePercent` (0..100).

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `_loadOnAwake` | Calls `Load()` automatically in `Awake`. Default on. |
| `timeLoad` | Min/max random load duration in seconds. Default `(1.5, 2)`. |
| `isLoadOne` | Run the load only once per play session; later `Load()` calls finish instantly. |

## Events

- `OnStart()` fires when a real load starts.
- `OnChange(float)` reports progress `0..1` (guaranteed to emit a final `1`).
- `OnChangePercent(int)` reports progress `0..100` (guaranteed to emit a final `100`).
- `OnFinisLoad()` fires when loading completes.

## Public API

| Member | Description |
|--------|-------------|
| `Load()` | Starts the fake load; in one-shot mode a repeat call completes instantly via `EndLoad`. |
| `EndLoad()` | Emits the final `OnChange(1)` / `OnChangePercent(100)` tick and `OnFinisLoad`. |

## Notes

- `isLoadOne` is backed by a static flag reset on subsystem registration, so it survives domain-reload-off play sessions correctly.

## See Also

- [Module Root](../README.md)
