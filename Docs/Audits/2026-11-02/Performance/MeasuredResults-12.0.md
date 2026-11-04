# Measured results for 12.0.0

This is the measured record of the 12.0.0 performance pass: the numbers behind the changelog's Performance table,
the setup they were taken in, the calibration rows that admit them, and the slices that still have no timing suite.
It does not claim player FPS, IL2CPP, other Unity versions or other machines.

**Navigation:** [Performance audit](README.md) · [Grid / Selector baseline](MeasuredGridSelectorBaseline.md) ·
[Testing guide: benchmarks](../../../../Assets/Neoxider/Docs/Testing.md#benchmarks-and-the-performance-category)

## Setup

| Item | Value |
| --- | --- |
| Unity | 6000.3.14f1 Editor, Windows |
| Scripting backend | Mono (`scripting_backend=mono` on the Input rows) |
| Mode | batch (`-batchmode -runTests`), explicit `-testCategory Performance` |
| Profiler | off: every line carries `profiler_enabled=0` |
| Harness | `Neo.Tests.Performance` (`NeoPerf`, `GcAllocCounter`); EditMode rows use Stopwatch batches plus a separate GC.Alloc pass, PlayMode PlayerLoop rows use `NeoPerfUpdateWindow` |
| "Before" | the old code path measured **in the same run and harness**: `head_reference` (the old Input adapter), `legacy_emulated` (the old algorithm), `strategy=complete` (the old N x H interaction classifier), `sequential` (one card at a time). It is not a measurement of 11.x binaries. |

| Run | Tree | Raw lines | Result |
| --- | --- | --- | --- |
| Run 1 | snapshot after waves 1-2 (2026-11-03) | [EditMode](Raw/neo-perf-run1-editmode.txt) (49 lines), [PlayMode](Raw/neo-perf-run1-playmode.txt) (75 lines) | the Codex reactive listener cases failed (test bug, see below) |
| Run 2 | snapshot after wave 3 (2026-11-04) | [EditMode](Raw/neo-perf-run2-editmode.txt) (121 lines), [PlayMode](Raw/neo-perf-run2-playmode.txt) (93 lines) | 69/69 EditMode and 71/71 PlayMode benchmarks passed |

Run 2 ran while other agents compiled in parallel, so its absolute numbers are noisier than run 1; the ratios
within one run hold. Compare a row only with the same case id from the same run. Rows that changed between runs are
listed as observations, not as before/after pairs.

## Calibration rows

A run's numbers are trusted only when its positive control reads one GC.Alloc call per retained `byte[4096]`.

| Row | Run 1 | Run 2 |
| --- | --- | --- |
| `Input.Play.Calibration.Bytes4096` | 1.001 calls/op, 4128 B est. | 1.000977 calls/op, 4128 B est. |
| `perf.calibration.gc_bytes_4096` (PlayMode hot paths) | 1 call/op, 4128 B est. | 1 call/op, 4128 B est. |
| `grid.match3.*.calibration.gc_allocs_byte4096` | 1 per sample | 1 per sample |
| `view.selector.*.calibration.gc_allocs_byte4096` | 1 per sample | 1 per sample |
| `abilities.domain`, `abilities.hub_query`, `cards.board`, `ui.meshrig`, `tools.spawner` calibration | - (suites new in wave 3) | 1 per sample each |

## Input polling (PlayMode, real virtual devices)

| Path | Run 1 head ns/op | Run 1 current ns/op | Run 2 head | Run 2 current | GC.Alloc calls/op, head -> current |
| --- | ---: | ---: | ---: | ---: | --- |
| KeyDownUp (`InteractiveObject` shape) | 35,059 | 406 | 37,653 | 579 | 24 (3384 B) -> 0 |
| MousePass (`MouseInputManager` shape) | 60,852 | 1,016 | 82,764 | 827 | 39 (6011 B) -> 0 |
| PrimaryTouch (`SwipeController` shape) | 34,389 | 919 | 39,677 | 1,439 | 25 (3074 B) -> 0 |
| ReadMove (8 keys + left stick) | 113,170 | 3,551 | 140,891 | 3,683 | 68 (10,520 B) -> 0 |

EditMode touch read: the reflected fallback costs 2,514 ns and 5 calls in run 1 (4,596 ns, 5 calls in run 2); the
bound path 426 ns (470 ns), 0 calls.

## Interaction (PlayMode, auto_p50 ms per frame, one shared ray)

| Route, N = 1000 targets | Run 1 complete | Run 1 default (indexed) | Run 2 complete | Run 2 default |
| --- | ---: | ---: | ---: | ---: |
| camera3d positive | 1927 | 7.5 | 3366 | 5.45 |
| camera2d positive | 1542 | 10.9 | 1807 | 14.1 |
| keyboard3d positive | 1175 | 6.3 | 3082 | 8.64 |
| keyboard2d positive | 1008 | 6.7 | 1859 | 12.4 |
| camera3d obstacles_on | 1831 | 8.6 | 1352 | 6.08 |

N = 100 camera3d positive: 12.3 -> 0.64 ms (run 1), 7.27 -> 0.56 ms (run 2). Every default row reads
`gc_allocs_per_frame_p50=0`.

Small-hit threshold (run 2, camera3d positive, auto_p50 ms): complete vs `index_always` at 2 / 4 / 6 / 8 / 12 / 16
hits = 0.078 / 0.093 / 0.093 / 0.146 / 0.296 / 0.420 vs 0.062 / 0.065 / 0.099 / 0.143 / 0.185 / 0.201. Below 8 hits
both paths stay within 0.03 ms, so `DefaultIndexedMinimumHits = 8` stays. Large hierarchy (20 targets x 201
colliders): default 0.27 ms, complete 0.27 ms, forced index 1.56 ms, so the ancestor-keyed index is not needed.

## GridSystem and Selector (EditMode)

| Case | Run 1 old p50 / allocs | Run 1 new p50 / allocs | Run 2 old | Run 2 new |
| --- | --- | --- | --- | --- |
| Match3 `TryFindValidSwap` 32x32, no valid move | 37.34 ms / 4978 | 0.518 ms / 0 | 74.5 ms / 4978 | 0.874 ms / 0 |
| Match3 16x16, no valid move | 2.41 ms / 1202 | 0.119 ms / 0 | 4.11 ms / 1202 | 0.186 ms / 0 |
| Match3 8x8, no valid move | 0.178 ms / 274 | 0.027 ms / 0 | 0.287 ms / 274 | 0.041 ms / 0 |
| Match3 32x32, canonical move | 1.17 ms / 163 | 0.058 ms / 0 | 1.98 ms / 163 | 0.095 ms / 0 |
| Selector getters, 1000 indices, 100 groups of 3 | 4.77 ms / 2400 | 0.0075 ms / 0 | 4.46 ms / 2400 | 0.0063 ms / 0 |
| Selector getters, 1 index, 100 groups | 0.374 ms / 2400 | 0.0043 ms / 0 | 0.64 ms / 2400 | 0.0068 ms / 0 |

Run 2 also measured an accepted 32x32 Match3 swap with its cascades at 322 µs and 0 allocations, and `Selector.Set`
at 1000 indices at 254 µs, 0 allocations.

## StateMachine (EditMode)

| Case | Run 1 | Run 2 |
| --- | --- | --- |
| Type transition to a cached C# state | 0.339 µs, 0 | 0.347 µs, 0 |
| Same, uncached / old reflection admission | 0.780 µs, 1 / 0.858 µs, 3 | 0.816 µs, 1 / 1.07 µs, 3 |
| NoCode evaluation, 128 states / 256 transitions: legacy_emulated vs current | - | 17.25 µs vs 0.545 µs, 0 |
| NoCode evaluation, 32 states | - | 3.87 µs vs 0.301 µs, 0 |
| Transition scan, 128 transitions | 19.8 µs, 0 | 24.6 µs, 0 |

## Suites added in wave 3 (run 2 only)

| Case | Result |
| --- | --- |
| `cards.board.place_stack` n128: sequential vs batch | 113.3 vs 14.8 µs per card (0.016 allocs per card, the batch's one list) |
| `cards.board.place_stack` n54 / n13 | 55.1 vs 8.8 / 17.2 vs 8.7 µs per card |
| `core.resource_pool.tick_half_regen` pools 1 / 8 / 64 | 0.09 / 0.37 / 4.21 µs, 0 allocs |
| `reactive.float.set_notify.distinct_listeners` 1 / 16 / 128 | 0.117 / 0.28 / 2.33 µs, 0 allocs |
| `abilities.hub_query` n1024: linear / grid / grid with `TryMoveUnit` every query / grid rebuilt every query | 307 / 0.96 / 1.18 / 300 µs, 0 allocs |
| `abilities.hub_query` n64: linear / grid | 9.55 / 1.15 µs |
| `abilities.domain.tick_dot` n64 / n256 / n1024 (one DoT on every unit) | 378 / 1,407 / 7,436 µs per tick, 0 allocs |
| `abilities.domain.publish_target` with 1 / 100 / 1000 unrelated observers | 0.173 / 0.182 / 0.177 µs, 0 allocs |
| `ui.meshrig.rebuild.animated` grid40 Character: uGUI / UI Toolkit / world / sprite | 2,673 / 2,067 / 2,317 / 2,071 µs, 0 allocs |
| `ui.meshrig.rebuild.animated` grid10 Character: uGUI / UI Toolkit / world / sprite | 192 / 139 / 150 / 138 µs, 0 allocs |
| `ui.meshrig.frame.clean` (nothing changed), every adapter and grid | 0.29-0.58 µs, 0 allocs |
| `ui.meshrig.preview_tick` n128: playing / paused / stopped | 86.7 µs and 1 GC.Alloc per tick / 10.5 µs, 0 / 0.1 µs, 0 |
| `tools.spawner.pool.warm_get_return` with 0 / 1 / 10 poolables | 5.6 / 5.8 / 12.9 µs, 0 allocs |
| `tools.spawner.fallback.warm_spawn_despawn` with 0 / 10 poolables | 12.2 / 26.8 µs, 0 allocs |

## Observations between the runs

- Cards board immediate arrange n128: 266 µs and 5 allocs per op (run 1) -> 205 µs and 0 (run 2); remove + place
  n128: 684 µs and 10 allocs -> 428 µs and 0. Wave 3 moved the board onto reused layout buffers.
- `ResourcePoolModel.Tick`, 64 pools: 150.6 µs (run 1) -> 4.48 µs (run 2), 0 allocs in both. Between the runs `Tick`
  started reusing its registration snapshot; the tree and the machine load also differ, so this is not a same-run
  pair.
- Reactive set and notify: the run-1 `listeners16` / `listeners128` cases failed because the test subscribed the same
  delegate N times and `ReactiveProperty.AddListener` deduplicates it. Wave 3 fixed the test (a distinct delegate per
  listener) and added `distinct_listeners*`; run 2 reads 3.71 µs for `listeners128` and 2.33 µs for
  `distinct_listeners128`, 0 allocs.

## Allocations found by the working GC.Alloc counter

- `AuraWeapon` pulse: one allocation per pulse from a three-argument `Mathf.Max` (a `params float[]`); now nested
  two-argument calls. `SpinController` line counting had the same pattern with `Mathf.Min`. The source guard
  `ParamsOverloadAllocationTests` keeps such calls out of the package.
- RPG direct 3D casts, 2D casts and `RpgProjectile`: `Array.Sort` / `List.Sort` with an `IComparer` wrap the comparer
  in a new delegate per call; `RpgHitSort` sorts in place without allocating.
- Unity floors, not library bugs: in the 6000.3 Editor every IMGUI text draw costs one GC.Alloc in the native binding,
  `CalcSize` one more, and changing `GUIContent.text` one more; TMP `SetText(char[])` costs two in the Editor only (its
  `m_text` mirror); DOTween `Kill` outside Play Mode leaves `IsActive` true. The editor repaint guards compare with
  this floor instead of zero.

## NOT_MEASURED

These slices from the first bounded list have no timing suite yet. Allocation guards may exist; that is not a
timing measurement.

| Slice | State |
| --- | --- |
| Drawer smoothing | `Drawer` stroke rebuilds have an allocation guard (StrokeBuffer); no timing suite. Each appended point still re-smooths the whole stroke (open in `dev-docs/TODO.md`). |
| NoCode bindings and Condition evaluate | Covered by allocation and behaviour tests; `NoCodeBindText` / `NeoCondition` evaluation cost per poll is not timed. |
| Quest | Notifications and reward dispatch are allocation-free (guards); no timing suite. |
| Inventory autosave bursts | `InventoryComponent.DeferAutoSave()` coalesces a burst into one write; the write cost is not timed. |
| Shop refresh | `ShopListView` rebinds and id lookups are allocation-free (guards); `ShopVariantsPanel` per-row `GetComponentsInChildren` is not timed. |
| Network due polls | `NetworkPropertySync` / relay polling is not timed (it needs a Mirror host). |

Contexts not measured at all: player builds, IL2CPP, profiler-on runs, Unity 6.5+, other machines and platforms.

## Re-run after wave 4

Run 3, 2026-11-04, on the final tree (wave 4 plus the closing fixes): Performance category, 69/69 EditMode and 71/71
PlayMode cases passed. The editor ran the whole verification chain back to back, so timings are noisier than run 2
(p95 and p99 spread wider); every current path stayed at 0 GC.Alloc and the ratios to the legacy paths hold. Raw
lines: [Raw/neo-perf-run3-editmode.txt](Raw/neo-perf-run3-editmode.txt) and
[Raw/neo-perf-run3-playmode.txt](Raw/neo-perf-run3-playmode.txt).

| Case | Run 2 | Re-run after wave 4 |
| --- | --- | --- |
| `Input.Play.ReadMove.Current` ns/op | 3,683 | 4,476, 0 GC (legacy 229,263 ns, 68 GC) |
| `interaction.camera3d.positive.n1000` auto_p50 ms | 5.45 | 14.07 (complete path 3,476) |
| `grid.match3.find_valid_swap.n32.control` ms | 0.874 | 1.06, 0 GC |
| `view.selector.getters.n1000.component` ms per 100 groups | 0.0063 | 0.0066, 0 GC |
| `statemachine.nocode_evaluate.s128` µs | 0.545 | 0.623 (legacy 17.43) |
| `cards.board.place_stack.batch.n128` µs per card | 14.8 | 26.9 (0.016 GC per card) |
| `core.resource_pool.tick_half_regen.pools64` µs | 4.21 | 6.23, 0 GC |
| `reactive.float.set_notify.distinct_listeners128` µs | 2.33 | 4.00, 0 GC |
| `abilities.hub_query.grid.n1024` µs | 0.96 | 1.51 (linear 307 in run 2) |
| `ui.meshrig.rebuild.animated.ugui.grid40.Character` µs | 2,673 | 4,122, 0 GC |
| `tools.spawner.pool.warm_get_return.poolables10` µs | 12.9 | 17.2, 0 GC |
| Calibration `Input.Play.Calibration.Bytes4096` calls/op | 1.000977 | 1.000977 |
