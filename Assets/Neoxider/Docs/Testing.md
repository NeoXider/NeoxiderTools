# Testing NeoxiderTools

**What it is:** the canonical guide to the package's Edit Mode tests (`Neo.Editor.Tests`, sources in
`Assets/Neoxider/Tests/Edit`): runtime API contracts, component and NoCode workflows, sample validation,
performance and allocation tests, and the isolated validation host.

**How to use:** run `Neo.Editor.Tests` from the Test Runner's EditMode tab in an isolated host project, then follow
the [runtime API](#runtime-api-contracts), [component](#components-and-nocode-workflows) and
[performance](#performance-and-allocation-tests) sections when you add tests. Play Mode coverage is in
[PlayModeTesting](PlayModeTesting.md).

---

Windows verification from 26–27 October 2026 is recorded for Unity `6000.3.14f1` and `6000.6.0f1`; see
[Verification 11](../Verification11.md) for exact runs, overlapping profiles and qualification of unchanged
inputs. Focused acceptance passed per SDK: P27 Cards **149/149 Edit**, P29 Cards + Samples **46/46 Play**
and P31 `SampleRuntimeSceneSmokeTests` **10/10**. The current P31 scene test uses the actual EventSystem
top-graphic raycast and `ExecuteHierarchy` click to require exactly one round; three manual OS-click
CardGame rounds are separate evidence. Fresh P31 imports cover all three public UPM samples and
**20/20 imported Play** cases per SDK; **28/28 Edit** retains P30 evidence through unchanged graph identity.
These results accept their recorded inputs, not arbitrary later changes or every platform.

HtmlToUitk (HTML → UI Toolkit) and its tests moved to the separate `com.neoxider.htmltouitk` package
after 11.0.1; its 11.0.0 Round3 evidence is in that package's `Documentation~/HtmlToUitkVerification11.md`
([HtmlToUitk](https://github.com/NeoXider/HtmlToUitk)).

The hosted package-integrity check passed for 11.0.0; its hosted Unity jobs did not run because GameCI secrets were unavailable. GitHub Actions workflows were subsequently removed.
Full-checkout/UPM testables remain the source-suite route; final archive contents are listed in the release-input manifest.
Final exported-archive import is a distinct gate in [Verification 11](../Verification11.md).

Runtime component test doubles live in `Tests/Fixtures/Neo.Tests.Fixtures.asmdef`, outside every
`Editor` folder, with no platform restriction, `UNITY_INCLUDE_TESTS` and `autoReferenced: false`.
The Editor fixtures retain `Neo.Editor.Tests` and reference this assembly in one direction. Native
component helpers must not be embedded in an Editor-only fixture assembly: Unity can reject their
`AddComponent` call even when the class/file name matches. The original standalone script GUIDs,
namespaces, fields and callbacks are retained during relocation. Private extracted types have unique
test-only names and explicit caller aliases; inventory payload callers use the actual relocated type name.

The independent fixture assembly contains no UnityEditor/NUnit/TestRunner reference. Its references
cover only the runtime bases/contracts actually used by its helpers; the initial phase/raycast/quest
trio needs only `UnityEngine.UI` beyond engine/framework assemblies. The `Neo.Tests.Fixtures.Network`
companion isolates the explicit Neo.Network/Mirror integration dependency. Mirror-specific helper
bodies retain their `MIRROR` guards; the singleton fallback helpers remain available for the existing
non-Mirror contracts. This boundary does not prove optional-dependency compilation. No production
assembly references a fixture assembly, and no generalized production friend grant is added. Play
references are added only for actual consumers; none were needed for this relocation batch.

The native attachment regression is
[`RuntimeFixtureBoundaryTests`](../Tests/Edit/Editor/Audit20261023NeoEditorTestsRuntimeFixtureBoundaryTests.cs).
It requires the actual phase, raycaster and quest components to attach from the runtime fixture
assembly and preserves the quest getter/callback positive controls. Existing phase/raycast/quest,
bootstrap/singleton, inspector, NoCode, save, inventory and network assertions remain in their original
Editor fixtures. Run those native cases only after the shared source freeze. The prior graphical run's
Editor-script rejections are failures; relocation source and an authored smoke case are not a passing run.

`SamplePrefabs_WithNetworkBehaviours_HaveNetworkIdentityInParents` requires Mirror. Its external import
and typed scan are guarded, and an absent Mirror produces an explicit inconclusive capability result.
With Mirror, this legacy-named scan covers both prefabs and native preview scenes: sample network
components can be authored directly in a scene. It records actual typed components and requires more
than zero NetworkBehaviours across the selected manifest identities. The identity requirement follows
configuration: `INeoOptionalNetworked.IsNetworked == false` counts as a typed solo component; enabled
optional components and ordinary NetworkBehaviours require an actual parent NetworkIdentity.
Prefab and scene scans use the same rule, independent of current server/client connection state.
Native load errors remain failures,
including unresolved prefab instances. Generic prefab/scene/YAML/terrain checks remain independent
and retain their nonempty/load/reference assertions. A GUID present in a .meta file does not prove
that Unity imported the referenced prefab or model successfully.

Persistent fixture scenes come from
`Neo.Editor.Tests.Audit20261023NeoEditorTestsSceneAssets.CreateEmptySceneAsset(string ownedAssetPath)`.
Create a GUID-owned `Assets/Audit20261023...` folder first and pass a new `.unity` path. The helper
copies/imports the test seed and refuses existing assets. It preserves borrowed scene setup because
it never opens, closes, selects or saves a scene. Callers can open that persistent copy additively,
populate it and save the regular owned scene, then restore active context and close/delete only
their own scene/assets. Unity rejects SaveScene on preview scenes; use previews for inspection.

`CreateMissingScriptSceneAsset(string ownedAssetPath)` copies the intentional missing-script test
seed under `Tests/Edit/Editor/SceneFixtures`. It has a healthy root and an inactive child with one
serialized MonoBehaviour whose script is absent. Tests must load that scene natively and require
root count 0, child count 1 and the exact reported scene/path. Scene text or an edited native
m_Component array is not acceptance evidence. These fixtures have no optional sample dependency.

По-русски: постоянную тестовую сцену создаём копированием собственного seed, затем открываем
additively и сохраняем только обычную owned-сцену. Preview используется для чтения. Отсутствующий
скрипт подтверждаем нативными счётчиками, а solo-компонент учитываем без обязательного networking.

Legacy hover ownership cases enter Play Mode through top-level UnitySetUp/UnityTearDown before
creating card views. They retain tween retirement, exact return pose and active/disabled destruction
checks. The InteractiveObject hover-off cases use native Update/physics, a real positive collider and
an explicit ScreenCenter ray provider; they call the public pointer API and also reject a disabled
collider. The fixture uses the owned empty scene through `EditorSceneManager.playModeStartScene`
before entering Play, so an existing Editor MainCamera does not become the runtime query context.
The previous play-start SceneAsset is retained across domain reload in a fixture-specific SessionState
entry and restored after top-level ExitPlayMode. No borrowed camera tags, enabled state or selection
policy are changed. The bounded frame barrier observes Time.frameCount. These cases cover center-policy
pointer handling, not physical mouse-button delivery or cursor-only input.

Drunkard queue-banking cases use the current generation/token operation seam and assert conservation
when a cancelled or replaced operation is rejected. A separate Play case banks real table views into
either winning hand through that seam. An owned DeckComponent, DeckConfig and inactive card template
are assigned before activation; real Start runs with valid prerequisites before the award operation.
This is focused award/hand-admission coverage, not a claim
that an entire public PlayRound completed. AM's trim/save regression uses public MusicEntry playback
and an isolated save provider, requiring channel 0.8, source 0.4 and persisted slider 0.8.

The public-name source rule considers the effective visibility of containing types. Public fields in
private/internal wire DTOs and constructor parameter names do not become public API; public members
of a visible nested type remain subject to PascalCase. Conversion tests capture the actual source
Graphic padding, stop only the converted point's preview and compare that exact baseline at the rig's
public rest pose. A live deformed rig can expand its effective raycast area without changing that
authored baseline.

По-русски: lifecycle-проверки используют настоящий Play Mode и положительный physics-target.
Чистый C# seam не считается подтверждением нативных callbacks. Приватные JSON DTO сохраняют
исходные wire keys; нулевой typed network scan и ошибки загрузки сцен остаются ошибками.

UTF 1.6 rejects `ExitPlayMode` yielded from a nested cleanup enumerator. Each top-level UnityTest or
UnityTearDown must yield the native exit itself after the bounded drain records its first genuine
GetResult/timeout fault, then inspect or rethrow that preserved fault. A nested drain may yield frames;
it must not hide the mode switch or turn a fault into success. The typewriter fault/nonsettlement
controls retain actual Editor-mode assertions after this top-level exit.

По-русски: добавляемые через AddComponent тестовые компоненты находятся в отдельной runtime-сборке
Fixtures с тестовым define. NUnit/UnityEditor-код остаётся в Editor-сборке; существующие проверки не
ослабляются. ExitPlayMode выдаётся из верхнего уровня UnityTest/UnityTearDown. Для будущих изменений
нужны компиляция и затронутые проверки после согласованного freeze; уже записанные результаты и
область их применимости и ограничения указаны в [Verification 11](../Verification11.md).

`Neo.Editor.Tests` contains the package's cross-module Edit Mode tests. Its assembly is Editor-only,
requires `UNITY_INCLUDE_TESTS`, and references NUnit and the Unity Test Framework. Tests belonging to
nested assembly definitions have their own scope. The broad suite currently also
references optional integrations including Mirror; a module's runtime API can have fewer dependencies
than this test assembly.

## Runtime API contracts

Prefer tests that call the public C# API directly and assert state, results, event payloads, or resource
ownership. Exercise boundary inputs and repeated calls when they matter to the contract. Checking only
that a call does not throw is insufficient for a mutation, payout, save operation, or subscription.

Use explicit C# types. Keep expected results independent of the implementation: use a hand-computed
board outcome, known serialized values, or a recorded event sequence. Prefer a supplied clock or seeded
random source over wall-clock sleeps and probability-dependent assertions.

File saves can be tested through the public `FileSaveProviderOptions.PersistenceRoot` API without
changing the application's active provider or writing into `Application.persistentDataPath`:

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

The example uses `System`, `System.IO`, `Neo.Save`, and `NUnit.Framework`. A unique directory also keeps
backup and temporary save files inside the fixture's cleanup boundary.

## Components and NoCode workflows

Construct the component and its explicit dependencies, configure it, exercise its typed API, and assert
observable behavior. Edit Mode does not reproduce the full Play Mode lifecycle. Prefer an existing
public initialization method; when a lifecycle callback must be invoked explicitly, keep that seam
narrow and also check its effects. Test NoCode adapters by invoking their action API and observing the
runtime state and success/failure UnityEvents. Inspector wiring alone does not establish a runtime
contract.

Keep references to objects created by the fixture. Destroy those objects in teardown or `finally`,
including generated `AudioClip`, `Texture2D`, `Sprite`, `Mesh`, editor, and ScriptableObject instances.
Destroying a GameObject does not release every separately created native asset it references. Do not
find an arbitrary camera, Canvas, settings component, or EventSystem in the user's scene and destroy it.
Menu tests must identify the objects produced by the tested invocation and restore editor selection.

When replacing `SaveProvider`, retain and restore the exact previous `ISaveProvider` instance.
`new PlayerPrefsSaveProvider()` does not restore a custom provider or its unsaved data. Never call
`SaveProvider.DeleteAll()` against an unknown provider. The test-only
[`Audit20261023NeoEditorTestsSaveScope`](../Tests/Edit/Save/Audit20261023NeoEditorTestsSaveScope.cs)
supports temporary file storage and provider restoration; it is internal test support, not a runtime API.
It does not snapshot all static events, singleton registries, or application settings.

Reward fixtures use the existing `INeoClock` / `TrustedClock` seam. The narrowly scoped
[`Audit20261023NeoEditorTestsClockScope`](../Tests/Edit/Bonus/Audit20261023NeoEditorTestsClockScope.cs)
also preserves the exact lazy restoration fields of `NeoTrustedTime`; reading its public `Clock`
getter before capturing state would itself change that state. Advance UTC and monotonic time together,
and assert exact timestamps, claim counts and countdown progress. Destroy saving components before
restoring the clock/provider. Preference fixtures use GUID keys and remove only their own keys.

Inspectors and mesh previews share editor clients. Inspect an addressed repaint lease and stop only
the owned motion; never advance the whole ticker into the future or stop every preview as cleanup.
Audio playback skips require an independent native `AudioSource` probe. Clip/loop/volume/stop-state
checks remain unconditional. Timer/typewriter cancellation tests must observe the actual asynchronous
completion and verify that callbacks stop, including failure cleanup. Card tween tests advance only
the captured owned tweens, preserving unrelated animations.

Async teardown records the first GetResult fault or settlement timeout, attempts cleanup of every
owned run, yields the actual `ExitPlayMode`, then rethrows the preserved failure. Clearing a list in
`finally` does not guarantee that mode exit runs. The typewriter fixture has separate real fault and
nonsettlement controls; their expected failure assertions are distinct from a successful SUT operation.

To retime an active state machine in tests, use `machine.SetCurrentStateEnterTime(float)` and check its
boolean result. `StateDurationPredicate.SetEnterTime` remains a standalone predicate anchor; it does
not retime machines sharing that predicate. Threshold and reentry assertions still observe transitions.

Shop 11.0 regression expectations follow its commit boundary: a profile-store failure compensates the
debit, while a failing postcommit observer preserves paid ownership and reports a notification fault.
Throwing from a purchase UnityEvent is therefore a notification test, rather than a persistence rollback
injection. See the [gameplay](Testing/Gameplay.md) and [utility](Testing/Utilities.md) coverage guides.

## Sample validation

The validators accept these layouts:

- Development: `<package root>/Samples/Demo/...`.
- Release packaging: `<package root>/Samples~/Demo/...`.
- Imported UPM samples: `Assets/Samples/NeoxiderTools/<actual package version>/Demo Scenes/...`.

The resolver supports repository `Assets/Neoxider` and registered UPM package roots, including local
packages outside `Assets`. It reads the current package manifest and selects its exact version/sample
identity; an older imported version cannot satisfy the smoke-scene lookup. Positive discovery counts
are required. Full native coverage selects one current-version developer/imported root for each exact
declared sample identity, preferring its imported root. Duplicate roots for one sample cannot replace
a missing different sample. Missing identities are reported explicitly.

The generic `SampleScenes_HaveNoMissingMonoBehaviours` case uses these same identity-selected
AssetDatabase roots, loads every discovered `SceneAsset`, and checks the actual preview hierarchy.
A missing native identity is inconclusive; empty visible scene discovery, failed loading and missing
scripts fail. The direct native-path control checks unique scene GUIDs and rejects unselected roots.

The named Open/Playable scene cases are inconclusive when their visible sample roots or compiled
sample types are unavailable. They never substitute YAML inspection for opening a scene. Once the
native prerequisites exist, wrong component types, failed scene loads and broken scene references
remain failures. Separate `YamlPackaging` cases inspect every required scene file and require nonempty
serialized script references/markers. Hidden-only packaging can satisfy those file checks while native
integration remains inconclusive; an empty AssetDatabase loop is never successful evidence.

Unity does not import `Samples~` as ordinary scene assets. Release-layout checks inspect the hidden
sample YAML and metadata on disk. Developer and imported scenes are inspected in disposable preview
scenes, preserving the editor's loaded scenes, active scene, and unsaved contents. Missing-script
checks include inactive descendants. Tagged-object checks search only the inspected scene hierarchy.
The preview ownership helper is
[`Audit20261023NeoEditorTestsPreviewSceneScope`](../Tests/Edit/Editor/Audit20261023NeoEditorTestsPreviewSceneScope.cs).
Do not import or rename samples just to run a source/YAML check.

Native sample validation (`SampleAssetValidationTests`, `Audit20261023NeoEditorTestsSampleEvidenceTests`) checks each
visible sample on its own and skips only the hidden or not-imported ones; the log names every validated root
(`Native sample <name> @ <version>: <root>`) and every skipped one. An asset is skipped only when its missing scripts
are fully accounted for by script slots of a hidden sample (counted per nested prefab instance); a reference that
resolves nowhere still fails. A sample visible at a non-canonical path is reported as such, not as hidden.

### Sample, demo and packaging tests

| Test | Assembly / platform | What it checks |
| --- | --- | --- |
| `Neo.Tests.Play.SampleScenePlaySmokeTests.SampleScene_LoadsTicksWithoutErrorsAndShowsItsGuide` | `Neo.Tests.Play` / PlayMode | One case per scene of the visible samples (named by the scene path): load, 12 frames, no error/assert/exception logs, Filled Images with sprites, no demo controller reporting `IsSceneBuilt == false` and no "scene not authored / Rebuild Active Demo Scene" warning, and the authored `ModuleDemoSceneInfo` card on every scene a Demo builder owns. |
| `Neo.Tests.Play.SampleScenePlaySmokeTests.EveryRegisteredSceneBuilder_HasItsSceneSavedInTheVisibleSample` | `Neo.Tests.Play` / PlayMode | Every registered demo scene builder has its scene saved in the visible sample. |
| `Neo.Tests.Play.AudioDemoScenePlayTests` | `Neo.Tests.Play` / PlayMode | Presses the authored buttons of `Scenes/Audio/AudioDemo.unity` (AM voice, music pool and track changes), then removes `AudioDemoController` mid-crossfade: the borrowed music keeps playing and nothing leaks. Needs enabled Unity audio. |
| `Neo.Editor.Tests.SampleSceneSaveKeyTests` | `Neo.Editor.Tests` / EditMode | Scans the YAML of every sample scene and prefab, prefab-instance overrides included, and fails when a persisting component (inventory, level profile, progression, quests, counter, selector, timer, wallet, shop, score, level manager, equipment, leaderboard, box, collection, rewards) keeps its library default save key. Remaining offenders are listed in `KnownOffenders`, which may only shrink; a listed offender that became clean fails too. |
| `Neo.Editor.Tests.PackageSampleIsolationTests` | `Neo.Editor.Tests` / EditMode | No package asset outside `Samples~` / `Samples` references a GUID that ships only inside the samples (parsed from disk, so it also runs while `Samples~` is hidden). |
| `Neo.Editor.Tests.ParamsOverloadAllocationTests` | `Neo.Editor.Tests` / EditMode | Source guard: no `Mathf.Max` / `Mathf.Min` call with three or more arguments (the `params` overloads allocate an array per call). |
| `Neo.Tests.NeoxiderPagesChangedBehaviorTests` | `Assembly-CSharp-Editor` / EditMode | PM, `BtnChangePage` (legacy actions, primary-button clicks, `Reset` without asset creation), `PageSubscriber`, `ToggleAudio`, the UIKit facades without managers, `PageIdGenerator`, `PagesEditorGUI` and two Play Mode `FakeLoad`-on-startup-page cases. Ignored while the Pages sample is hidden. |
| `Neo.Demo.Editor.Tests.*` | `Neo.Demo.Editor.Tests` / EditMode | The kit and every builder end to end in a temporary scene, built twice (wiring present, the second pass creates and removes nothing). Wave additions: `AnimatorsAndUIComponentsDemoBuildersTests`, `BonusRewardsDemoBuilderTests`, `Physics3DDemoBuilderTests` (wiring, explicit references, `NeoDemo.` save keys) and `DemoSceneRunnerSceneCreationTests` (a missing builder scene is created). |

The Play Mode sample cases and the Demo/Pages tests report Ignored in the release layout, because Unity does not
compile scripts inside `Samples~`. Make **Demo Scenes** and **NeoxiderPages** visible first: import them, or rename
`Samples~/Demo` and `Samples~/NeoxiderPages` to the development `Samples` folder (keep Character Movement
Fundamentals hidden while the repository's older `Assets/Samples/CharacterMovementFundamentals` copy exists, because
both share their GUIDs). Hidden layouts and scenes that need a hidden sample report an explicit Ignore. In Input
System-only projects the Character Movement Fundamentals scenes report Ignored (they need Active Input Handling =
Both); SurvivorDemo runs on either backend. The smoke checks error, assert and exception logs through
`Application.logMessageReceivedThreaded` rather than `LogAssert.NoUnexpectedReceived`, which in Unity Test Framework
1.6 also rejects routine `Debug.Log` and warnings. The older `SampleRuntimeSceneSmokeTests` keeps its scene-specific
gameplay cases; its generic `CoreModuleDemoScenes_LoadAndTickWithoutRuntimeErrors` overlaps the new smoke test.

## Performance and allocation tests

Allocation guards and benchmarks of every test assembly share one test-only assembly,
`Neo.Tests.Performance` (`Tests/Performance`, namespace `Neo.Tests.Performance`). It is gated by
`UNITY_INCLUDE_TESTS`, has no platform restriction, and references only NUnit and `UnityEngine.TestRunner`.
`Neo.Editor.Tests` and `Neo.Tests.Play` reference it; no production or fixture assembly does. Do not add a
private counter, recorder loop or `NEO_PERF` formatter to a test file: extend the shared types instead.

### Why the managed byte counter is not used

`System.GC.GetAllocatedBytesForCurrentThread()` returns 0 under the Boehm GC of the Unity 6 Editor
(6000.3) and IL2CPP, even for a retained `byte[4096]`. A guard such as `after - before == 0` built on it can
never fail, so it is not used anywhere in `Tests/**`. Allocations are counted with the native `GC.Alloc`
profiler marker, the same source as the Unity Test Framework constraint `Is.Not.AllocatingGCMemory()`.
A count is the number of managed allocations (GC.Alloc calls), not bytes; zero calls means zero bytes.

### Calibrated counter

`GcAllocCounter` records `GC.Alloc` on the calling thread inside an explicit window. Before a counter is
used it must pass admission: a warm-up window, an empty window that reads 0, an allocation made outside any
window that is not counted, a retained `byte[4096]` that reads exactly 1 (two passes), no sample leaking into
the next window, and the whole calibration within one frame. When the recorder is unavailable (for example a
player built without profiler support) or calibration fails, the calling test is reported as Ignored with a
`NotSupported: the native GC.Alloc counter was not admitted (...)` message; it never passes silently. In
the Editor the self-test `GcAllocCounterTests.Counter_IsAdmittedInTheEditor` fails instead, so a broken
recorder is visible in one place rather than as many ignored guards.

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

Rules for allocation guards:

- Warm the path first (first call, pool growth, lazy caches) and keep setup, assertions and message strings
  outside the window. Create the delegate before the window: a capturing lambda allocates where it is created.
- A window must open and close in the same frame on the thread that created the counter; a window across
  `yield return` throws. In PlayMode measure synchronous work inside one frame.
- Warm hot paths assert zero. A bound is acceptable only with a stated reason, for example
  `Is.LessThan(2 * boxCalls)` where `boxCalls` is the calibrated count of one box per iteration, or a bound
  below the iteration count that proves nothing allocates per iteration.
- `Is.Not.AllocatingGCMemory()` (`UnityEngine.TestTools.Constraints`) remains acceptable for one warm delegate.
  It has no admission step, so pair it with a positive control such as `Is.AllocatingGCMemory()` on a
  `byte[4096]`, as the StateMachine runtime tests do.

Zero-allocation guards run in the normal EditMode/PlayMode suites. Examples: `SelectorHotPathTests`,
`Match3SearchHotPathTests`, `RpgAllocationTests` and the RPG PlayMode cases `AreaAttack_WarmCast_DoesNotAllocate`,
`DirectSphereCast_WarmCast_DoesNotAllocate` and `AuraPulse_Warm_DoesNotAllocate`,
`ShopTypedPurchaseAndRefreshTests`, `NpcHotPathAndChaseTests`, `InventoryHotPathTests`, `PoolingHotPathTests`
and the Abilities, Audio and Bonus `*HotPathAllocationTests`. Editor repaint guards (`EditorLibraryPassTests`,
`InspectorVisualPassTests`) compare a redraw with Unity's own floor of one text draw: in Unity 6.3 every IMGUI text
draw and `CalcSize` costs one GC.Alloc in the native binding, and changing a `GUIContent.text` costs another, so
those guards assert "no more than Unity itself", not zero. `EditorRepaintCacheTests` checks that inspector badges
and row texts are rebuilt only when a value changes.

### Benchmarks and the Performance category

A benchmark measures and prints numbers; it asserts correctness of its workload, never a timing threshold.
Mark every benchmark (or its fixture) with both attributes so normal runs skip it:

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

- `NeoPerf.MeasureSynchronous(caseId, scale, operationsPerSample, warmupSamples, samples, operation)` runs
  warm-up batches, timed batches (Stopwatch) and a separate allocation pass of up to eight batches, so the
  recorder never sits inside a timed sample. `NeoPerf.MeasureSamples(...)` takes a delegate that already runs
  one whole sample. `NeoPerf.CallsPerMeasure(warmupSamples, samples)` gives the batch count for checksums.
- `NeoPerf.EmitCalibration(fixture, samples)` in `[OneTimeSetUp]` prints the positive-control row
  `<fixture>.calibration.gc_allocs_byte4096` and ignores the fixture unless it reads one call per sample.
- `NeoPerf.EstimateBytesPerOperation(...)` (coroutine) estimates bytes per call from the frame-scoped
  `GC Allocated In Frame` counter: median of loaded frames minus median of interleaved idle frames.
- PlayerLoop benchmarks add `NeoPerfUpdateWindowBegin` and `NeoPerfUpdateWindowEnd` to a scene object and call
  `NeoPerfUpdateWindow.Start(frames)`: each complete Update phase records wall time and GC.Alloc calls.

### NEO_PERF line format

Each measured case prints exactly one line to the Unity log and to the NUnit output:

```text
NEO_PERF <case> key=value key=value ... profiler_enabled=0|1
NEO_PERF view.selector.getters.n1000.component scale=1 k=100 samples=120 p50_ms=0.0123 p95_ms=0.0141 p99_ms=0.02 max_ms=0.031 mean_ms=0.0127 op_p50_us=0.123 gc_allocs_per_sample=0 gc_allocs_per_op=0 profiler_enabled=0
```

The case id has no spaces; values use the invariant culture, and spaces inside string values become `_`.
`MeasureSynchronous` writes `scale`, `k` (operations per sample), `samples`, `p50_ms`, `p95_ms`, `p99_ms`,
`max_ms`, `mean_ms`, `op_p50_us`, `gc_allocs_per_sample` and `gc_allocs_per_op`
(`gc_allocs_per_sample=unavailable` when no counter is admitted). Suites add their own keys, for example
`ns_per_op_p50`, `gc_calls_per_op`, `bytes_per_op_est`, `scripting_backend`, `role` or `kind`. Every line
ends with `profiler_enabled`, added by `NeoPerf.Line.Emit()`.

### Running and reading benchmarks

Explicit tests are not part of normal runs. Select them with the Performance category plus an explicit
filter naming the fixtures or tests, then collect the lines from the log:

```text
"<Unity.exe>" -batchmode -projectPath "<project>" -runTests -testPlatform EditMode -testCategory Performance -testFilter "Neo.Tools.Tests.SelectorPerformanceTests;Neo.Editor.Tests.GridSystem.Match3SearchPerformanceTests" -testResults "<out>/perf-edit.xml" -logFile "<out>/perf-edit.log"
"<Unity.exe>" -batchmode -projectPath "<project>" -runTests -testPlatform PlayMode -testCategory Performance -testFilter "Neo.Tests.Play.Audit20261102LibraryHotpathMeasurementTests;Neo.Tests.Play.Audit20261023NeoToolsInteractableObjectLifecycleTests.Perf_" -testResults "<out>/perf-play.xml" -logFile "<out>/perf-play.log"
grep "^NEO_PERF" "<out>/perf-play.log"
```

When a normal run selects a fixture that also contains benchmarks by regex, exclude their names, for
example `^Neo\.Tests\.Play\.Audit20261023NeoToolsInteractableObjectLifecycleTests\.(?!Perf_)`.

| Platform | Benchmark | Lines |
| --- | --- | --- |
| EditMode | `Neo.Tools.Tests.SelectorPerformanceTests` | `view.selector.*` getters and `Set`, with `legacy_emulated` controls |
| EditMode | `Neo.Editor.Tests.GridSystem.Match3SearchPerformanceTests` | `grid.match3.*` valid-swap search and rejected swaps |
| EditMode | `Neo.Editor.Tests.Edit.Audit20261023NeoStateMachineRuntimeTests.Performance_EvaluateTransitionsScanAndTypeActivation` | `statemachine.*` transition scan and type activation |
| EditMode | `Neo.Tests.Edit.Audit20261102NeoToolsInputPollingPerformanceTests` | `Input.Edit.*` adapter polling |
| EditMode | `Neo.Tests.Edit.UI.Audit20261102NeoUIEditorPersistenceTests.Perf_MotionPreviewDriverTick_PlayingPausedStopped` | `ui.meshrig.preview_tick.*` |
| PlayMode | `Neo.Tests.Play.Audit20261023NeoCardsLifecycleTests.Performance_BoardImmediateLayoutAndAdmission` | `cards.board.*` immediate layout and admission |
| PlayMode | `Neo.Tests.Play.Audit20261102LibraryHotpathMeasurementTests` | Reactive notify, resource pool tick, interaction query backend, byte calibration |
| PlayMode | `Neo.Tests.Play.Audit20261102NeoToolsInputPollingPerformancePlayTests` | `Input.Play.*` device polling with byte estimates |
| PlayMode | `Neo.Tests.Play.Audit20261023NeoToolsInteractableObjectLifecycleTests.Perf_*` | Automatic interaction routes on the real PlayerLoop, including the small-hit cases `Perf_Interaction_Camera3D_SmallHitsComplete_{2,4,6,8,12,16}` / `SmallHitsIndex_*` (`strategy=index_always`) |
| EditMode | `Neo.Editor.Tests.Abilities.AbilityDomainBenchmarks` | `abilities.domain.tick_dot.n*` (system tick with a DoT on every unit) and `abilities.domain.publish_target.unrelated*` (event publish among unrelated observers) |
| EditMode | `Neo.Editor.Tests.Abilities.AbilityQueryBenchmarks` | `abilities.hub_query.{linear,grid,grid_move,grid_rebuild}.n*` hub radius queries |
| EditMode | `Neo.Editor.Tests.Cards.CardZonePlacementTests.Performance_StackPlacement_BatchVersusSequential` | `cards.board.place_stack.{batch,sequential}.n*` |
| EditMode | `Neo.Editor.Tests.ResourcePoolTickHotPathTests.Benchmark_Tick_HalfRegeneratingPools` | `core.resource_pool.tick_half_regen.pools*` |
| EditMode | `Neo.Editor.Tests.ReactivePropertyFoundationTests.Benchmark_SetAndNotify_DistinctListeners` | `reactive.float.set_notify.distinct_listeners*` (a distinct delegate per listener) |
| EditMode | `Neo.Editor.Tests.Edit.StateMachineRoutingTests.Performance_NoCodeEvaluationScalesWithOutgoingTransitions` | `statemachine.nocode_evaluate.s*`, with `legacy_emulated` controls |
| EditMode | `Neo.Tests.UI.UIMeshRigPerformanceTests` | `ui.meshrig.rebuild.animated.*` per adapter and grid, `ui.meshrig.frame.clean.*` controls |
| PlayMode | `Neo.Tests.Play.PoolPerformanceTests` | `tools.spawner.pool.warm_get_return.*`, `tools.spawner.fallback.warm_spawn_despawn.*` |

The numbers measured for 12.0.0 (Unity 6000.3.14f1 Editor, Mono, batch, profiler off) are recorded in the repository
at `Docs/Audits/2026-11-02/Performance/MeasuredResults-12.0.md`, with the raw `NEO_PERF` lines next to it. Slices
without a timing suite (Drawer smoothing, NoCode bindings and Condition evaluation, Quest, Inventory autosave bursts,
Shop refresh, network due polls) are listed there as `NOT_MEASURED`.

Compare lines only with lines of the same case id from the same machine, Unity version, scripting backend
and `profiler_enabled` value; profiler-on runs carry marker overhead. Read p50 for the typical cost and
p95/p99/max for spikes. `gc_allocs_per_op` should be 0 on warm hot paths; a calibration row
(`*.calibration.gc_allocs_byte4096`, `Input.Play.Calibration.Bytes4096`, `Perf_Calibration_GcBytes4096`)
must read one call per sample or about 4 KB per operation, otherwise the numbers next to it are not trusted.
`bytes_per_op_est` is a frame-scoped estimate; treat small values as noise unless the calibration row agrees.

По-русски: тесты сэмплов (`SampleScenePlaySmokeTests`, `AudioDemoScenePlayTests`, `SampleSceneSaveKeyTests`,
`PackageSampleIsolationTests`) и тесты билдеров `Neo.Demo.Editor.Tests` описаны в таблице выше; Play Mode-случаи
требуют видимых сэмплов Demo и NeoxiderPages. Проверки аллокаций и бенчмарки используют общую тестовую сборку `Neo.Tests.Performance`.
`GC.GetAllocatedBytesForCurrentThread()` в редакторе Unity 6 всегда возвращает 0, поэтому вместо него
считаются вызовы `GC.Alloc` через откалиброванный `GcAllocCounter` (пустое окно = 0, удерживаемый
`byte[4096]` = 1). Если счётчик недоступен, тест помечается Ignored с сообщением `NotSupported`, а не
проходит молча. Бенчмарки помечаются `[Category(NeoPerf.Category)]` и `[Explicit(NeoPerf.ExplicitReason)]`,
печатают по одной строке `NEO_PERF <case> key=value ...` и запускаются через `-testCategory Performance`
вместе с явным `-testFilter`. Сравнивать строки можно только с тем же case id, машиной, версией Unity и
значением `profiler_enabled`.

## Running and reviewing results

Use a dedicated test project or clean editor session. Some fixtures deliberately reset singleton,
diagnostic, settings, and Mirror static state; several `[UnityTest]` cases in this Editor assembly
explicitly enter Play Mode. The entire suite is not a read-only inspection of an in-progress scene.
Source/document architecture checks also currently expect the repository layout under
`Assets/Neoxider` in some legacy fixtures. The sample validators, interaction assembly guard, Mover
asset guard and inspector source scanner resolve the actual package root.

Run focused cases in Unity Test Runner first, followed by the affected module and integration suites.
The fixture
[`Audit20261023NeoEditorTestsIsolationTests`](../Tests/Edit/Editor/Audit20261023NeoEditorTestsIsolationTests.cs)
covers save-provider preservation, native audio cleanup, preview-scene disposal, owned scene objects,
and sample validation. A skipped audio, graphics, or platform case is not a pass for that behavior.

For compatibility changes, compile and execute applicable tests in both Unity 6.0–6.4 and Unity 6.5+.
Use the shared stable-ID compatibility APIs instead of repeating version conditionals in tests.
Record the Unity version, test filter, actual pass/fail/skip counts, and any remaining compile or runtime
checks. Source scans, UTF-8 checks, and `git diff --check` are useful evidence, but do not establish a
Unity compilation or test pass.

## Isolated final-host entrypoints

The test-only
[`Audit20261023NeoEditorTestsValidationHost`](../Tests/Edit/Editor/Audit20261023NeoEditorTestsValidationHost.cs)
provides three explicit Unity `-executeMethod` entrypoints:

- `Neo.Editor.Tests.Audit20261023NeoEditorTestsValidationHost.ProbeEnvironment`
- `Neo.Editor.Tests.Audit20261023NeoEditorTestsValidationHost.ImportPackageSamples`
- `Neo.Editor.Tests.Audit20261023NeoEditorTestsValidationHost.DiscoverCompiledTests`

Use them only in the task-owned final host under
`Local/Audits/assembly-2026-10-23/unity/final/`. Before the first Unity launch, the verification operator
sets a unique company `NeoQA_20261023_<run>` and product `NeoTools_<editor_profile>`, then supplies their
exact values as `-neoValidationCompany` and `-neoValidationProduct`. Supply `-neoValidationOutput` as a
JSON file inside that isolated project. The environment method verifies the effective persistent path,
records the active provider/file root, and proves the actual Windows Editor PlayerPrefs registry branch
with a GUID sentinel that it removes in `finally`. Unknown providers or redirected file roots fail closed.
No entrypoint changes PlayerSettings or deletes another preference key.

Import and discovery additionally require `-neoValidationExpectedPackageVersion` from the operator's
independently frozen validation request (for example `12.0.0`). There is no default or implementation
version pin. Missing/malformed expectations, wrong observed versions and changed expectations after
domain reload fail explicitly; the receipt records expected and observed versions separately.
Sample import also requires `-neoValidationPackageRoot` matching the registered frozen local
or embedded `com.neoxider.tools` package. All three current-version destinations must be absent.
The driver calls the public UPM `Sample.FindByPackage` / `Sample.Import` API, resumes after domain reload,
and waits at most ten minutes. It records actual import paths, version, native import return observation,
nonzero asset/script/reference counts, per-asset GUIDs/dependency counts and reference failures.
`importReturnObserved: false` discloses a domain reload before the native call returned; completion still
requires UPM's imported state and actual resolved assets. Manual folder copies provide no import evidence.
All methods exit batch mode themselves: omit Unity's early `-quit` for asynchronous import or discovery.

`DiscoverCompiledTests` is implemented in the owned
[`ValidationHost.Discovery` partial](../Tests/Edit/Editor/Audit20261023NeoEditorTestsValidationHost.Discovery.cs).
Run it in a separate batch invocation after import and compilation have finished, outside Play Mode:

```text
"<Unity.exe>" -batchmode -projectPath "<isolated-final-host>" -executeMethod Neo.Editor.Tests.Audit20261023NeoEditorTestsValidationHost.DiscoverCompiledTests -neoValidationCompany "<NeoQA_20261023_identity>" -neoValidationProduct "<NeoTools_identity>" -neoValidationExpectedPackageVersion "<frozen-expected-version>" -neoValidationOutput "<host>/Artifacts/discovery.json" -logFile "<host>/Artifacts/discovery.log"
```

This uses the installed Test Framework's public instance API
`void TestRunnerApi.RetrieveTestList(TestMode, Action<ITestAdaptor>)`, separately for EditMode and PlayMode.
It never calls test execution. Do not add `-runTests`, `-listTests` or `-quit` to this command; the installed
UTF 1.6 command-line settings do not provide a `-listTests` option.

The JSON records the actual `CompilationPipeline.GetAssemblies(AssembliesType.Editor)` graph: assembly
names/flags/defines, assembly and compiled-DLL references, output DLL identity/path/length/time/SHA-256;
loaded assembly identities/locations/module IDs; registered package identities, versions, sources,
resolved paths and requested dependencies. The scope is the Editor graph and currently loaded test
trees. It does not compile a player, prove a successful fresh compile or infer Mirror absence from a symbol.
Compilation logs and any player-profile evidence remain separate operator responsibilities.

Neo provenance supports both final-host profiles. An actual registered `com.neoxider.tools` is
authoritative: its version must exactly match the independent frozen expectation, and its observed source/root are used. A wrong
registered version fails even if a valid Assets copy exists. Only when no registered Neo exists does
discovery read the exact project `Assets/Neoxider/package.json`, requiring a readable JSON object with
name `com.neoxider.tools` and the expected version. Missing, malformed or mismatched manifests fail.
The selected `discovery.neoPackage` summary records `kind`, `source`, canonical `resolvedPath` and
`manifestOrigin`. Assets selection uses `ProjectAssets` for both kind/source and the actual manifest
path as its origin. Registered selection uses `RegisteredPackageInfo`, the native source and
`PackageInfo.GetAllRegisteredPackages` as its identity origin. `discovery.packages` retains only the
observed registered records in SDK order; Assets provenance never adds a synthetic UPM entry.

This fallback admits discovery in the Assets development profile. `ImportPackageSamples` still
requires actual Local/Embedded registration, the explicit frozen root and real UPM import; its
registration guards are unchanged. The owned
[`DiscoveryProvenanceTests`](../Tests/Edit/Editor/Audit20261023NeoEditorTestsDiscoveryProvenanceTests.cs)
use temporary manifests and projected records to test admission, precedence and list preservation.
Those controls alone do not prove native PackageInfo state or discovery. For future changes to provenance
or host layouts, freeze the source and capture fresh compilation and nonempty SDK Edit/Play trees in both
the real Assets host and registered UPM consumer. Recorded baseline executions are listed in
[Verification 11](../Verification11.md); they do not automatically accept later source changes.

For both real SDK trees it records every discovered leaf ID/full name/unique name, actual assembly
identity, run state/skip reason and per-assembly counts. Both modes must be nonempty, including
`Neo.Editor.Tests` in EditMode and `Neo.Tests.Play` in PlayMode. Discovery counts include skipped,
ignored, explicit and not-runnable leaves; `complete` means discovery completed, not that tests passed.
Missing outputs/identities, empty required discovery, callback faults or the ten-minute monotonic
Editor-time deadline produce a failed receipt/exit 1. Domain reload restarts both trees and the graph
within the original deadline, using only a small SessionState resume record. Status is `discovering`
until terminal `complete`/exit 0 or `failed`/exit 1, including receipt-write failure. The discovery helper
reads host identity without probing or changing the save provider, preferences, scenes or fixture state.

These entrypoints and regression sources are authored support, not execution receipts. During a shared
source-writing phase, wait until every writer finishes before launching Unity or validation processes.
По-русски: исходники тестов не являются доказательством прохождения. Запускать их следует только после
общего завершения изменений, в отдельном host с подтверждённым собственным пространством сохранений.
