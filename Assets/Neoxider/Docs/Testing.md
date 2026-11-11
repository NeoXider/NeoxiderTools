# Testing NeoxiderTools

**What it is:** the canonical guide to running and writing the package tests: the Edit Mode suite `Neo.Editor.Tests` (`Assets/Neoxider/Tests/Edit`), the shared fixture and performance assemblies, demo validation and the isolated validation host. Play Mode specifics are in [PlayModeTesting](PlayModeTesting.md).

**How to use:**
1. Use a dedicated test project or a clean editor session: some fixtures reset global state.
2. Install `com.unity.test-framework`. For a UPM install, add `"com.neoxider.tools"` to the host manifest's `testables`.
3. Run the focused fixture first, then the affected module and integration suites ([running tests](#running-tests)).
4. When you add tests, follow [runtime API contracts](#runtime-api-contracts), [components and NoCode workflows](#components-and-nocode-workflows) and [performance and allocation tests](#performance-and-allocation-tests).
5. Record the Unity version, the filter and the pass, fail and skip counts.

**Navigation:** [← Docs](./README.md) · [Play Mode testing](PlayModeTesting.md) · [Network Play Mode](Testing/NetworkPlayMode.md) · [gameplay coverage](Testing/Gameplay.md) · [utility coverage](Testing/Utilities.md)

---

## Running tests

| Assembly | Folder | Use |
| --- | --- | --- |
| `Neo.Editor.Tests` | `Tests/Edit` | Edit Mode tests. Editor-only, needs `UNITY_INCLUDE_TESTS`, references NUnit, the Test Framework and optional integrations such as Mirror. Some `[UnityTest]` cases enter Play Mode. |
| `Neo.Tests.Play` | `Tests/Play` | Play Mode tests ([PlayModeTesting](PlayModeTesting.md)). |
| `Neo.Tests.Fixtures`, `Neo.Tests.Fixtures.Network` | `Tests/Fixtures` | Runtime test doubles; the Network one holds the Neo.Network and Mirror helpers ([fixture assemblies](#fixture-assemblies)). |
| `Neo.Tests.Performance` | `Tests/Performance` | Allocation counters and benchmarks for every test assembly. |
| `Neo.Tests.Network.Integration` | `Tests/Integration/Network` | The three-process network matrix ([Network Play Mode](Testing/NetworkPlayMode.md#dedicated-server-and-two-independent-clients)). |

Tests in nested assembly definitions have their own scope; a runtime module can need fewer dependencies than these assemblies.

**Host prerequisites.**

- `com.unity.test-framework` is not a package dependency. `testables` exposes the tests of a UPM install but does not make every fixture support every package layout.
- A few legacy source and document checks expect the repository layout under `Assets/Neoxider`. The sample validators, the interaction assembly guard, the Mover asset guard and the inspector source scanner resolve the actual package root.
- Run static-reset and Play Mode transition fixtures in an isolated host with its own save and PlayerPrefs storage. They reset singleton, diagnostic, settings and Mirror state, save providers and editor lifecycle.
- When several people or agents edit sources, launch Unity only after every writer has finished.

**Commands.** In the Editor: Test Runner, EditMode tab, `Neo.Editor.Tests` or a fixture filter; Play Mode tests are under `Neo.Tests.Play` on the PlayMode tab. In batch mode:

```text
"<Unity.exe>" -batchmode -projectPath "<project>" -runTests -testPlatform EditMode -testFilter "<fixture or test>" -testResults "<out>/edit.xml" -logFile "<out>/edit.log"
```

Use `-testPlatform PlayMode` for `Neo.Tests.Play`; benchmarks also need `-testCategory Performance` ([running benchmarks](#running-and-reading-benchmarks)). Without Unity, `python scripts/check_package.py` checks text, manifests, metadata, documentation links and `git diff --check`; it does not compile.

**Scope and results.**

- Run focused cases first, then the module and integration suites. [`Audit20261023NeoEditorTestsIsolationTests`](../Tests/Edit/Editor/Audit20261023NeoEditorTestsIsolationTests.cs) covers save-provider preservation, native audio cleanup, preview-scene disposal, owned scene objects and sample validation.
- For compatibility changes, compile and run on Unity 6.0–6.4 and on 6.5+, through the shared stable-ID APIs rather than version conditionals.
- Record the Unity version, filter, pass, fail and skip counts, and open checks. A skipped audio, graphics or platform case is not a pass; source scans, UTF-8 checks and `git diff --check` are not a compile or test pass.
- There are no GitHub Actions workflows. Recorded 11.0.0 runs are in [Verification 11](../Verification11.md); HtmlToUitk's 11.0.0 evidence is in that package's `Documentation~/HtmlToUitkVerification11.md` ([HtmlToUitk](https://github.com/NeoXider/HtmlToUitk)).

## Runtime API contracts

Call the public C# API and assert state, results, event payloads or resource ownership. "Does not throw" is not enough for a mutation, payout, save or subscription. Cover boundary inputs and repeated calls where the contract depends on them. Use explicit types, expected values that do not come from the implementation (a hand-computed board, known serialized values, a recorded event sequence), and a supplied clock or seeded random source instead of sleeps and probability.

Test file saves through `FileSaveProviderOptions.PersistenceRoot`; it changes neither the active provider nor `Application.persistentDataPath`:

```csharp
string root = Path.Combine(Path.GetTempPath(), "NeoSaveTest-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);
try
{
    FileSaveProviderOptions options = new FileSaveProviderOptions { PersistenceRoot = root };
    FileSaveProvider writer = new FileSaveProvider("slot.json", options);
    writer.SetInt("coins", 42);
    writer.Save();

    FileSaveProvider reader = new FileSaveProvider("slot.json", options);
    Assert.That(reader.GetInt("coins"), Is.EqualTo(42));
}
finally
{
    Directory.Delete(root, true);
}
```

The example uses `System`, `System.IO`, `Neo.Save` and `NUnit.Framework`. The unique directory keeps backup and temporary files inside the cleanup boundary.

## Components and NoCode workflows

Construct the component and its explicit dependencies, call its typed API and assert observable behaviour. Edit Mode does not reproduce the Play Mode lifecycle: prefer a public initialization method, and keep any explicitly invoked callback narrow. Test NoCode adapters through their action API and the resulting state and success or failure UnityEvents; Inspector wiring alone is not a runtime contract.

**Ownership and cleanup.**

- Destroy every object the fixture creates in teardown or `finally`, including generated `AudioClip`, `Texture2D`, `Sprite`, `Mesh`, editor and ScriptableObject instances; destroying a GameObject does not release them.
- Never find and destroy an arbitrary camera, Canvas, settings component or EventSystem of the user's scene. Menu tests track the objects their invocation produced and restore editor selection.
- Preference fixtures use GUID keys and remove only their own.

**Save providers and clocks.**

- Restore the exact previous `ISaveProvider` instance; `new PlayerPrefsSaveProvider()` loses a custom provider and its unsaved data. Never call `SaveProvider.DeleteAll()` on an unknown provider.
- [`Audit20261023NeoEditorTestsSaveScope`](../Tests/Edit/Save/Audit20261023NeoEditorTestsSaveScope.cs) gives temporary file storage and provider restoration. It is internal test support and does not snapshot static events, singleton registries or settings.
- Reward fixtures use the `INeoClock` / `TrustedClock` seam. [`Audit20261023NeoEditorTestsClockScope`](../Tests/Edit/Bonus/Audit20261023NeoEditorTestsClockScope.cs) also keeps the lazy restoration fields of `NeoTrustedTime`; reading its `Clock` getter first would change them.
- Advance UTC and monotonic time together, assert exact timestamps, claim counts and countdowns, and destroy saving components before restoring the clock or provider.

**Editor previews and asynchronous work.**

- Inspectors and mesh previews share editor clients: inspect the addressed repaint lease and stop only the owned motion, never the whole ticker.
- Audio playback skips need an independent native `AudioSource` probe; clip, loop, volume and stop checks stay unconditional.
- Timer and typewriter cancellation tests observe the real asynchronous completion and that callbacks stop. Card tween tests advance only their own tweens.
- Async teardown records the first fault or timeout, cleans up every owned run, yields `ExitPlayMode`, then rethrows the fault. Clearing a list in `finally` does not guarantee the mode exit.

**Module notes.**

- Retime an active state machine with `machine.SetCurrentStateEnterTime(float)` and check its result. `StateDurationPredicate.SetEnterTime` anchors only a standalone predicate.
- Shop: a profile-store failure compensates the debit; a failing post-commit observer keeps paid ownership and reports a notification fault. Throwing from a purchase UnityEvent tests notification, not rollback.
- Drunkard queue-banking cases use the generation/token seam and assert conservation when an operation is rejected; a Play case banks real table views. This covers award and hand admission, not a full `PlayRound`.
- AM's trim/save regression uses public `MusicEntry` playback and an isolated provider: channel 0.8, source 0.4, persisted slider 0.8.
- The public-name rule uses effective visibility: public fields of private or internal wire DTOs and constructor parameters are not public API; members of a visible nested type must be PascalCase.
- UI mesh rig conversion tests capture the source Graphic padding, stop only the converted point's preview and compare at the rest pose; a deformed rig can enlarge its raycast area without changing that baseline.

More coverage notes: [gameplay](Testing/Gameplay.md) and [utility](Testing/Utilities.md).

### Fixture assemblies

`Tests/Fixtures/Neo.Tests.Fixtures.asmdef` holds runtime component test doubles, because Unity can reject `AddComponent` for a component compiled into an Editor-only assembly. It sits outside every `Editor` folder, has no platform restriction, needs `UNITY_INCLUDE_TESTS` and sets `autoReferenced: false`.

- It references no UnityEditor, NUnit or TestRunner assembly; the phase, raycast and quest helpers need only `UnityEngine.UI` beyond the engine.
- `Neo.Editor.Tests` references it one way. No production assembly references a fixture assembly, and there is no friend grant.
- `Neo.Tests.Fixtures.Network` isolates Neo.Network and Mirror; Mirror helpers keep `MIRROR` guards and singleton fallbacks stay for non-Mirror contracts. This split does not prove compilation without optional dependencies.
- Relocated helpers kept their script GUIDs, namespaces, fields and callbacks. [`RuntimeFixtureBoundaryTests`](../Tests/Edit/Editor/Audit20261023NeoEditorTestsRuntimeFixtureBoundaryTests.cs) requires the phase, raycaster and quest components to attach from it.

### Scene fixtures and Play Mode cases

- `Neo.Editor.Tests.Audit20261023NeoEditorTestsSceneAssets.CreateEmptySceneAsset(string ownedAssetPath)` copies the seed scene to a new `.unity` path inside a GUID-owned `Assets/Audit20261023...` folder and refuses existing assets. It never opens, selects or saves a scene: open the copy additively, populate and save it, restore the active context, delete only your assets. Unity rejects `SaveScene` on preview scenes; use them to inspect.
- `CreateMissingScriptSceneAsset(string ownedAssetPath)` copies the seed from `Tests/Edit/Editor/SceneFixtures`: a healthy root and an inactive child with one MonoBehaviour whose script is absent. Load it natively and require root count 0, child count 1 and the exact scene and path; scene text is not evidence.
- Hover ownership cases enter Play Mode through top-level UnitySetUp/UnityTearDown before creating card views. InteractiveObject hover-off cases use native Update and physics, a real collider and a ScreenCenter ray provider, starting in the owned empty scene through `EditorSceneManager.playModeStartScene` and restoring the previous start scene from SessionState. They cover center-policy pointer handling, not physical mouse input.
- UTF 1.6 rejects `ExitPlayMode` yielded from a nested enumerator: each top-level UnityTest or UnityTearDown yields it after the bounded drain recorded its first fault, then rethrows that fault.

## Sample validation

The validators read `Assets/NeoxiderDemos/<name>` (`Demo`, `NeoxiderPages`, `CharacterMovementFundamentals`); the identities are fixed in `Audit20261023NeoEditorTestsPaths.DemoTrees`. The package-root resolver supports the repository `Assets/Neoxider` and registered UPM roots, local packages outside `Assets` included. Discovery counts must be positive, missing identities are reported by name, and a duplicate root cannot stand in for a different missing demo.

- `SampleAssetValidationTests` and `Audit20261023NeoEditorTestsSampleEvidenceTests` check each visible demo, skip hidden or absent ones and log both (`Native sample <name> @ <version>: <root>`). An asset is skipped only when its missing scripts are explained by script slots of a hidden demo, counted per nested prefab instance; a reference that resolves nowhere fails.
- `SampleScenes_HaveNoMissingMonoBehaviours` loads every discovered `SceneAsset` into a preview. A missing identity is inconclusive; empty discovery, a failed load or a missing script fails.
- Named Open/Playable cases are inconclusive without visible roots or compiled demo types and never fall back to YAML. Separate `YamlPackaging` cases require nonempty script references; an empty AssetDatabase loop is never a pass.
- `SamplePrefabs_WithNetworkBehaviours_HaveNetworkIdentityInParents` needs Mirror (inconclusive without it) and more than zero NetworkBehaviours across prefabs and scenes. `INeoOptionalNetworked.IsNetworked == false` counts as solo; other NetworkBehaviours need a parent `NetworkIdentity`. Load errors, unresolved prefab instances included, fail. A GUID in a `.meta` file does not prove the asset imported.
- Previews keep the editor's loaded scenes, active scene and unsaved contents, include inactive descendants and search tags only in the inspected hierarchy ([`Audit20261023NeoEditorTestsPreviewSceneScope`](../Tests/Edit/Editor/Audit20261023NeoEditorTestsPreviewSceneScope.cs)). Do not import or rename demos just for a source or YAML check.

The demo tests are listed in the [inventory](#sample-demo-and-packaging-tests).

## Performance and allocation tests

Every test assembly shares `Neo.Tests.Performance` (`Tests/Performance`, same namespace): it needs `UNITY_INCLUDE_TESTS`, has no platform restriction and references only NUnit and `UnityEngine.TestRunner`. `Neo.Editor.Tests` and `Neo.Tests.Play` reference it; no production or fixture assembly does. Extend its types instead of adding a private counter, recorder loop or `NEO_PERF` formatter.

### Why the managed byte counter is not used

`System.GC.GetAllocatedBytesForCurrentThread()` returns 0 under the Boehm GC of the Unity 6 Editor (6000.3) and IL2CPP, even for a retained `byte[4096]`, so a guard built on it never fails and `Tests/**` does not use it. Allocations are counted with the native `GC.Alloc` profiler marker, the source of `Is.Not.AllocatingGCMemory()`. A count is the number of allocations, not bytes; zero calls means zero bytes.

### Calibrated counter

`GcAllocCounter` records `GC.Alloc` on the calling thread inside an explicit window. Admission requires: a warm-up window; an empty window reading 0; an allocation outside any window not counted; a retained `byte[4096]` reading exactly 1 in two passes; no leak into the next window; calibration within one frame. Without a recorder (for example a player without profiler support) or after a failed calibration the test is Ignored with `NotSupported: the native GC.Alloc counter was not admitted (...)`, never passed. In the Editor `GcAllocCounterTests.Counter_IsAdmittedInTheEditor` fails instead, so a broken recorder shows in one place.

| API | Use |
| --- | --- |
| `GcAlloc.AssertNone(Action body, string message)` | Simple guard: the warm body makes no allocation. |
| `GcAlloc.AssertAtMost(long maxCalls, Action body, string message)` | Documented bounded paths, for example fewer calls than iterations. |
| `GcAlloc.Count(Action body)` | Returns the calls of one window (ignores the test when not admitted). |
| `GcAlloc.TryCount(Action body, out string notAdmitted)` + `GcAlloc.IgnoreIfNotAdmitted(notAdmitted)` | Run the body, finish functional asserts, then ignore only the allocation claim. |
| `GcAllocCounter.CreateOrIgnore()` / `TryCreate(out counter, out failure)` | Several windows in one test; `Begin()`, `End()`, `Count(Action)`, `Dispose()`. |

```csharp
using Neo.Tests.Performance;

service.Tick(0.1f); // warm caches, pools and lazy state outside the window
GcAlloc.AssertNone(() =>
{
    for (int i = 0; i < 1000; i++)
    {
        service.Tick(0.1f);
    }
}, "A warm Tick must not allocate.");

using (GcAllocCounter counter = GcAllocCounter.CreateOrIgnore())
{
    counter.Begin();
    int hits = Poll(256);
    long calls = counter.End();
    Assert.That(hits, Is.EqualTo(256));
    Assert.That(calls, Is.Zero);
}
```

**Rules for allocation guards.**

- Warm the path first. Keep setup, assertions and messages outside the window, and create the delegate before it: a capturing lambda allocates where it is created.
- A window opens and closes in one frame, on the counter's thread; a window across `yield return` throws.
- Warm hot paths assert zero. A bound needs a stated reason, for example `Is.LessThan(2 * boxCalls)` with `boxCalls` calibrated as one box per iteration.
- `Is.Not.AllocatingGCMemory()` is fine for one warm delegate; pair it with a positive control such as `Is.AllocatingGCMemory()` on a `byte[4096]`, as the StateMachine runtime tests do.
- Editor repaint guards assert "no more than Unity itself": in Unity 6.3 every IMGUI text draw and `CalcSize` costs one GC.Alloc, and changing a `GUIContent.text` another.

Zero-allocation guards run in the normal suites ([examples](#zero-allocation-guards)).

### Benchmarks and the Performance category

A benchmark prints numbers and asserts only the correctness of its workload, never a timing threshold. Mark it or its fixture with both attributes so normal runs skip it:

```csharp
[Test]
[Category(NeoPerf.Category)]        // "Performance"
[Explicit(NeoPerf.ExplicitReason)]
public void Performance_TickScaling()
{
    NeoPerf.MeasureSynchronous("module.tick.n64", 64, 100, NeoPerf.DefaultWarmupSamples, 120,
            () => service.Tick(0.1f))
        .Add("role", "current")
        .Emit();
}
```

| API | What it does |
| --- | --- |
| `NeoPerf.MeasureSynchronous(caseId, scale, operationsPerSample, warmupSamples, samples, operation)` | Warm-up batches, timed batches (Stopwatch) and a separate allocation pass of up to eight batches, so the recorder is never inside a timed sample. |
| `NeoPerf.MeasureSamples(...)` | Takes a delegate that runs one whole sample. |
| `NeoPerf.CallsPerMeasure(warmupSamples, samples)` | Batch count for checksums. |
| `NeoPerf.EmitCalibration(fixture, samples)` | In `[OneTimeSetUp]`: prints `<fixture>.calibration.gc_allocs_byte4096`; ignores the fixture unless it reads one call per sample. |
| `NeoPerf.EstimateBytesPerOperation(...)` | Coroutine: bytes per call from `GC Allocated In Frame`, median of loaded frames minus median of idle frames. |
| `NeoPerfUpdateWindowBegin`, `NeoPerfUpdateWindowEnd`, `NeoPerfUpdateWindow.Start(frames)` | PlayerLoop benchmarks: each complete Update phase records wall time and GC.Alloc calls. |

### NEO_PERF line format

Each case prints one line to the Unity log and the NUnit output:

```text
NEO_PERF <case> key=value key=value ... profiler_enabled=0|1
NEO_PERF view.selector.getters.n1000.component scale=1 k=100 samples=120 p50_ms=0.0123 p95_ms=0.0141 p99_ms=0.02 max_ms=0.031 mean_ms=0.0127 op_p50_us=0.123 gc_allocs_per_sample=0 gc_allocs_per_op=0 profiler_enabled=0
```

The case id has no spaces; values use the invariant culture, and spaces in string values become `_`. `MeasureSynchronous` writes `scale`, `k` (operations per sample), `samples`, `p50_ms`, `p95_ms`, `p99_ms`, `max_ms`, `mean_ms`, `op_p50_us`, `gc_allocs_per_sample` and `gc_allocs_per_op` (`gc_allocs_per_sample=unavailable` without an admitted counter). Suites add keys such as `ns_per_op_p50`, `gc_calls_per_op`, `bytes_per_op_est`, `scripting_backend`, `role` or `kind`. `NeoPerf.Line.Emit()` appends `profiler_enabled`.

### Running and reading benchmarks

Select explicit tests with the Performance category and a filter that names them, then collect the lines:

```text
"<Unity.exe>" -batchmode -projectPath "<project>" -runTests -testPlatform EditMode -testCategory Performance -testFilter "Neo.Tools.Tests.SelectorPerformanceTests;Neo.Editor.Tests.GridSystem.Match3SearchPerformanceTests" -testResults "<out>/perf-edit.xml" -logFile "<out>/perf-edit.log"
"<Unity.exe>" -batchmode -projectPath "<project>" -runTests -testPlatform PlayMode -testCategory Performance -testFilter "Neo.Tests.Play.Audit20261102LibraryHotpathMeasurementTests;Neo.Tests.Play.Audit20261023NeoToolsInteractableObjectLifecycleTests.Perf_" -testResults "<out>/perf-play.xml" -logFile "<out>/perf-play.log"
grep "^NEO_PERF" "<out>/perf-play.log"
```

When a normal run selects a fixture that also holds benchmarks, exclude them by regex, for example `^Neo\.Tests\.Play\.Audit20261023NeoToolsInteractableObjectLifecycleTests\.(?!Perf_)`.

- Compare a line only with the same case id from the same machine, Unity version, scripting backend and `profiler_enabled` value; profiler-on runs carry marker overhead.
- p50 is the typical cost; p95, p99 and max show spikes. `gc_allocs_per_op` should be 0 on warm hot paths.
- A calibration row (`*.calibration.gc_allocs_byte4096`, `Input.Play.Calibration.Bytes4096`, `Perf_Calibration_GcBytes4096`) must read one call per sample or about 4 KB per operation, or the numbers next to it are not trusted. `bytes_per_op_est` is an estimate; small values are noise unless the calibration row agrees.

The 12.0.0 numbers (Unity 6000.3.14f1 Editor, Mono, batch, profiler off) are in the repository at `Docs/Audits/2026-11-02/Performance/MeasuredResults-12.0.md`, with the raw lines. Slices without a timing suite (Drawer smoothing, NoCode bindings and Condition evaluation, Quest, Inventory autosave bursts, Shop refresh, network due polls) are listed there as `NOT_MEASURED`.

## Isolated validation host

[`Audit20261023NeoEditorTestsValidationHost`](../Tests/Edit/Editor/Audit20261023NeoEditorTestsValidationHost.cs) has three `-executeMethod` entrypoints in namespace `Neo.Editor.Tests`, for the task-owned final host under `Local/Audits/assembly-2026-10-23/unity/final/` only. They are authored support, not execution receipts; recorded baseline runs are in [Verification 11](../Verification11.md) and do not cover later changes.

| Entrypoint | What it does |
| --- | --- |
| `Audit20261023NeoEditorTestsValidationHost.ProbeEnvironment` | Checks the effective persistent path, records the provider and file root, and proves the Windows Editor PlayerPrefs registry branch with a GUID sentinel removed in `finally`. Unknown providers or redirected roots fail. |
| `Audit20261023NeoEditorTestsValidationHost.ImportPackageSamples` | Imports UPM samples through `Sample.FindByPackage` / `Sample.Import`; records paths, versions, asset, script and reference counts, GUIDs and failures. Needs a package with UPM samples (11.x–12.1); 12.2+ declares none. |
| `Audit20261023NeoEditorTestsValidationHost.DiscoverCompiledTests` | Lists the compiled test trees and the Editor assembly graph without running tests ([test discovery](#test-discovery)). |

| Argument | Needed by | Meaning |
| --- | --- | --- |
| `-neoValidationCompany`, `-neoValidationProduct` | all | Values set before the first launch: `NeoQA_20261023_<run>` and `NeoTools_<editor_profile>`. |
| `-neoValidationOutput` | all | The JSON receipt, inside the isolated project. |
| `-neoValidationExpectedPackageVersion` | import, discovery | The frozen expected version, for example `12.0.0`. No default; a missing, malformed or changed value fails, and the receipt keeps expected and observed versions. |
| `-neoValidationPackageRoot` | import | Must match the registered local or embedded `com.neoxider.tools` root. |

No entrypoint changes PlayerSettings or deletes another preference key, and each exits batch mode itself: omit `-quit`. Import needs absent destinations, resumes after domain reload and waits at most ten minutes. `importReturnObserved: false` means a reload happened before the native call returned; completion still needs UPM's imported state. Manual folder copies are not import evidence.

### Test discovery

`DiscoverCompiledTests` lives in the [`ValidationHost.Discovery` partial](../Tests/Edit/Editor/Audit20261023NeoEditorTestsValidationHost.Discovery.cs). Run it in its own batch call after import and compilation, outside Play Mode:

```text
"<Unity.exe>" -batchmode -projectPath "<isolated-final-host>" -executeMethod Neo.Editor.Tests.Audit20261023NeoEditorTestsValidationHost.DiscoverCompiledTests -neoValidationCompany "<NeoQA_20261023_identity>" -neoValidationProduct "<NeoTools_identity>" -neoValidationExpectedPackageVersion "<frozen-expected-version>" -neoValidationOutput "<host>/Artifacts/discovery.json" -logFile "<host>/Artifacts/discovery.log"
```

- It calls `void TestRunnerApi.RetrieveTestList(TestMode, Action<ITestAdaptor>)` for EditMode and PlayMode. Do not add `-runTests`, `-listTests` (UTF 1.6 has none) or `-quit`.
- The JSON holds the `CompilationPipeline.GetAssemblies(AssembliesType.Editor)` graph (names, flags, defines, references, output DLL path, length, time, SHA-256), loaded assemblies and registered packages. It does not compile a player, prove a fresh compile or infer Mirror absence from a symbol.
- Per tree it records every leaf's ID, names, assembly, run state and skip reason, plus per-assembly counts. Both trees must be nonempty, with `Neo.Editor.Tests` and `Neo.Tests.Play`. Counts include skipped, ignored, explicit and not-runnable leaves; `complete` means discovery finished, not that tests passed.
- Status stays `discovering` until `complete` (exit 0) or `failed` (exit 1). Missing outputs, empty trees, callback faults, a receipt-write failure or the ten-minute Editor-time deadline fail; a domain reload restarts within the same deadline. Discovery never touches the save provider, preferences, scenes or fixtures.
- **Provenance:** a registered `com.neoxider.tools` wins and must match the expected version even when an `Assets` copy exists. Otherwise `Assets/Neoxider/package.json` must name `com.neoxider.tools` with that version. `discovery.neoPackage` records `kind`, `source`, `resolvedPath` and `manifestOrigin` (`ProjectAssets` or `RegisteredPackageInfo`); `discovery.packages` lists only registered records, in SDK order. [`DiscoveryProvenanceTests`](../Tests/Edit/Editor/Audit20261023NeoEditorTestsDiscoveryProvenanceTests.cs) test this with temporary manifests. After changing provenance or host layouts, capture fresh nonempty trees in both the Assets host and a registered UPM consumer.

## Test inventory

### Sample, demo and packaging tests

| Test | Assembly / mode | What it checks |
| --- | --- | --- |
| `Neo.Tests.Play.SampleScenePlaySmokeTests.SampleScene_LoadsTicksWithoutErrorsAndShowsItsGuide` | Play | Per demo scene: load, 12 frames, no error, assert or exception logs, Filled Images with sprites, no controller with `IsSceneBuilt == false`, the `ModuleDemoSceneInfo` card on Demo scenes. |
| `Neo.Tests.Play.AudioDemoScenePlayTests` | Play | Presses the AudioDemo buttons, then removes `AudioDemoController` mid-crossfade: music continues, nothing leaks. Needs Unity audio. |
| `Neo.Tests.Play.Samples.SurvivorDemoGameplayTests` | Play | The Survivor scene uses library components only; an autopilot wins the 180 s run. |
| `Neo.Editor.Tests.SampleSceneSaveKeyTests` | Edit | No persisting component in a demo scene or prefab, overrides included, keeps a library default save key. `KnownOffenders` may only shrink. |
| `Neo.Editor.Tests.PackageSampleIsolationTests` | Edit | No package asset references a GUID shipped only in `Assets/NeoxiderDemos`. |
| `Neo.Editor.Tests.ParamsOverloadAllocationTests` | Edit | No `Mathf.Max` / `Mathf.Min` call with three or more arguments (the `params` overloads allocate). |
| `Neo.Tests.NeoxiderPagesChangedBehaviorTests` | `Assembly-CSharp-Editor`, Edit | PM, `BtnChangePage`, `PageSubscriber`, `ToggleAudio`, UIKit facades, `PageIdGenerator`, `PagesEditorGUI`, two Play Mode `FakeLoad` cases. |

Demo tests report Ignored when their tree under `Assets/NeoxiderDemos` is missing. The Character Movement Fundamentals scenes report Ignored in Input System-only projects (they need Active Input Handling = Both); SurvivorDemo runs on either. The smoke test reads logs through `Application.logMessageReceivedThreaded`, because `LogAssert.NoUnexpectedReceived` in UTF 1.6 also rejects routine logs and warnings. The older `SampleRuntimeSceneSmokeTests` keeps scene-specific gameplay cases; its `CoreModuleDemoScenes_LoadAndTickWithoutRuntimeErrors` overlaps the smoke test.

### Zero-allocation guards

- Edit Mode: `SelectorHotPathTests`, `Match3SearchHotPathTests`, `RpgAllocationTests`, `ShopTypedPurchaseAndRefreshTests`, `NpcHotPathAndChaseTests`, `InventoryHotPathTests`, `PoolingHotPathTests`, the Abilities, Audio and Bonus `*HotPathAllocationTests`.
- Play Mode RPG: `AreaAttack_WarmCast_DoesNotAllocate`, `DirectSphereCast_WarmCast_DoesNotAllocate`, `AuraPulse_Warm_DoesNotAllocate`.
- Editor repaint: `EditorLibraryPassTests`, `InspectorVisualPassTests`; `EditorRepaintCacheTests` checks that badges and row texts rebuild only on change.

### Benchmarks

| Mode | Benchmark | Lines |
| --- | --- | --- |
| Edit | `Neo.Tools.Tests.SelectorPerformanceTests` | `view.selector.*` getters and `Set`, `legacy_emulated` controls |
| Edit | `Neo.Editor.Tests.GridSystem.Match3SearchPerformanceTests` | `grid.match3.*` valid-swap search, rejected swaps |
| Edit | `Neo.Editor.Tests.Edit.Audit20261023NeoStateMachineRuntimeTests.Performance_EvaluateTransitionsScanAndTypeActivation` | `statemachine.*` |
| Edit | `Neo.Editor.Tests.Edit.StateMachineRoutingTests.Performance_NoCodeEvaluationScalesWithOutgoingTransitions` | `statemachine.nocode_evaluate.s*`, `legacy_emulated` controls |
| Edit | `Neo.Tests.Edit.Audit20261102NeoToolsInputPollingPerformanceTests` | `Input.Edit.*` |
| Edit | `Neo.Tests.Edit.UI.Audit20261102NeoUIEditorPersistenceTests.Perf_MotionPreviewDriverTick_PlayingPausedStopped` | `ui.meshrig.preview_tick.*` |
| Edit | `Neo.Tests.UI.UIMeshRigPerformanceTests` | `ui.meshrig.rebuild.animated.*`, `ui.meshrig.frame.clean.*` |
| Edit | `Neo.Editor.Tests.Abilities.AbilityDomainBenchmarks` | `abilities.domain.tick_dot.n*`, `abilities.domain.publish_target.unrelated*` |
| Edit | `Neo.Editor.Tests.Abilities.AbilityQueryBenchmarks` | `abilities.hub_query.{linear,grid,grid_move,grid_rebuild}.n*` |
| Edit | `Neo.Editor.Tests.Cards.CardZonePlacementTests.Performance_StackPlacement_BatchVersusSequential` | `cards.board.place_stack.{batch,sequential}.n*` |
| Edit | `Neo.Editor.Tests.ResourcePoolTickHotPathTests.Benchmark_Tick_HalfRegeneratingPools` | `core.resource_pool.tick_half_regen.pools*` |
| Edit | `Neo.Editor.Tests.ReactivePropertyFoundationTests.Benchmark_SetAndNotify_DistinctListeners` | `reactive.float.set_notify.distinct_listeners*` |
| Play | `Neo.Tests.Play.Audit20261023NeoCardsLifecycleTests.Performance_BoardImmediateLayoutAndAdmission` | `cards.board.*` |
| Play | `Neo.Tests.Play.Audit20261102LibraryHotpathMeasurementTests` | Reactive notify, resource pool tick, interaction query backend, byte calibration |
| Play | `Neo.Tests.Play.Audit20261102NeoToolsInputPollingPerformancePlayTests` | `Input.Play.*` with byte estimates |
| Play | `Neo.Tests.Play.Audit20261023NeoToolsInteractableObjectLifecycleTests.Perf_*` | Interaction routes on the PlayerLoop, incl. `Perf_Interaction_Camera3D_SmallHitsComplete_{2,4,6,8,12,16}` / `SmallHitsIndex_*` (`strategy=index_always`) |
| Play | `Neo.Tests.Play.PoolPerformanceTests` | `tools.spawner.pool.warm_get_return.*`, `tools.spawner.fallback.warm_spawn_despawn.*` |

## По-русски

Тесты запускайте в отдельном проекте или чистой сессии редактора: часть fixtures сбрасывает глобальное состояние и провайдеры сохранений. Нужен `com.unity.test-framework`; для UPM добавьте `"com.neoxider.tools"` в `testables`. Сначала точечный fixture, затем тесты модуля. Записывайте версию Unity, фильтр и счётчики; пропуск — не прохождение. Аллокации считаются через откалиброванный `GcAllocCounter`, потому что `GC.GetAllocatedBytesForCurrentThread()` в Unity 6 всегда даёт 0. Бенчмарки явные (`[Explicit]`, категория `Performance`), печатают строки `NEO_PERF` и запускаются через `-testCategory Performance` с `-testFilter`. Демо-тесты требуют `Assets/NeoxiderDemos/...`, иначе Ignored.
