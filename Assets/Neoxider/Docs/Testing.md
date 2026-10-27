# Testing NeoxiderTools

Windows verification from 26–27 October 2026 is recorded for Unity `6000.3.14f1` and `6000.6.0f1`; see
[Verification 11](../Verification11.md) for exact runs, overlapping profiles and qualification of unchanged
inputs. Focused acceptance passed per SDK: P27 Cards **149/149 Edit**, P29 Cards + Samples **46/46 Play**
and P31 `SampleRuntimeSceneSmokeTests` **10/10**. The current P31 scene test uses the actual EventSystem
top-graphic raycast and `ExecuteHierarchy` click to require exactly one round; three manual OS-click
CardGame rounds are separate evidence. Fresh P31 imports cover all three public UPM samples and
**20/20 imported Play** cases per SDK; **28/28 Edit** retains P30 evidence through unchanged graph identity.
These results accept their recorded inputs, not arbitrary later changes or every platform.

HtmlToUitk Round3 ran the complete assembly **370/370,0 skipped per6000.3.14f1/6000.6.0f1**, including
real three external CLI runs/native ownership import/theme and Play UIDocument. Round2 CLI solution is
**2573 passed/30 NotExecuted/0 failed**, separate pure logic **291/291**. Debt remains7 validate-only,22 open goldens
and1 S08 expected-diagnostic case. Net9 used process-local Major on.NET 10.0.9; net8 used8.0.21; portable
CLI uses LatestMajor without ENV. Four diagnostic csprojs remain machine-specific.
[Exact current evidence and manual OS scope](UI/HtmlToUitkVerification11.md).

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
nested assembly definitions, such as HtmlToUitk, have their own scope. The broad suite currently also
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

Sample import additionally requires `-neoValidationPackageRoot` matching the registered frozen local
or embedded `com.neoxider.tools` 11.0.0 package. All three current-version destinations must be absent.
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
"<Unity.exe>" -batchmode -projectPath "<isolated-final-host>" -executeMethod Neo.Editor.Tests.Audit20261023NeoEditorTestsValidationHost.DiscoverCompiledTests -neoValidationCompany "<NeoQA_20261023_identity>" -neoValidationProduct "<NeoTools_identity>" -neoValidationOutput "<host>/Artifacts/discovery.json" -logFile "<host>/Artifacts/discovery.log"
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
authoritative: its version must be exactly `11.0.0`, and its observed source/root are used. A wrong
registered version fails even if a valid Assets copy exists. Only when no registered Neo exists does
discovery read the exact project `Assets/Neoxider/package.json`, requiring a readable JSON object with
name `com.neoxider.tools` and version `11.0.0`. Missing, malformed or mismatched manifests fail.
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
