# FakeLoad

**What it is:** `Neo.UI.FakeLoad` (`Scripts/UI/Simple/FakeLoad.cs`) is a `MonoBehaviour` that plays a fake loading progress over a random duration and drives progress-bar events, for splash/loading screens where there is nothing real to wait for. Existing serialized components remain compatible with the former `Neo` namespace; C# callers import `Neo.UI`.

**How to use:**
1. Add `Neoxider > UI > FakeLoad` (prefab: `Prefabs/UI/Page/Fake Load.prefab`).
2. Bind a progress bar to `OnChange` (0..1) or `OnChangePercent` (0..100) and the next step to `OnFinisLoad`.
3. Keep `_loadOnAwake` on, or call `Load()` yourself.

---

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `_loadOnAwake` | Calls `Load()` automatically in `Awake`. Default on. |
| `timeLoad` | Min/max random load duration in seconds. Default `(1.5, 2)`. |
| `isLoadOne` | Run the load only once per play session; later `Load()` calls finish instantly. |
| `_useUnscaledTime` | Count unscaled time, so the loader finishes even when a previous scene left `Time.timeScale` at `0`. Default on. |

## Events

- `OnStart()` fires when a real load starts.
- `OnChange(float)` reports progress `0..1` (guaranteed to emit a final `1`).
- `OnChangePercent(int)` reports progress `0..100` (guaranteed to emit a final `100`).
- `OnFinisLoad()` fires when loading completes.

## Public API

| Member | Description |
|--------|-------------|
| `Load()` | Starts the fake load. In one-shot mode a call after the session's load has finished completes on the next frame in Play Mode (at once outside Play Mode), so an `OnFinisLoad` that switches pages is not undone by the page switch that showed the loader. A call while a load runs restarts it in both modes (`OnStart` fires again, progress starts over), so `OnFinisLoad` still fires once. |
| `IsLoading` | True while a load runs, waits to complete, or is paused on an inactive object. |
| `UseUnscaledTime` | Reads or switches the time mode of the progress. |
| `EndLoad()` | Stops a running load, then emits the final `OnChange(1)` / `OnChangePercent(100)` tick and `OnFinisLoad`. Use it to skip the loader. |

## Notes

- `isLoadOne` is backed by a static flag reset on subsystem registration, so it survives domain-reload-off play sessions correctly.
- A load interrupted by deactivating the GameObject pauses: it continues from the same progress when the object
  is active again, and `Load()` on an inactive object starts when it becomes active.
- A zero-length load (`timeLoad = (0, 0)`) starts and finishes inside `Load()`; in one-shot mode later calls
  complete on the next frame (several calls in one frame complete once). A progress listener may call `EndLoad()` or `Load()`: the superseded progress stream stops
  reporting at once and never finishes a second time.
- Upgrade note: `_useUnscaledTime` is new and defaults to on, so existing loaders now ignore `Time.timeScale`
  (slow motion included). Untick `Use Unscaled Time` or set `UseUnscaledTime = false` to keep scaled time.

## See Also

- [Module Root](../README.md)
