# Neo.Level — implementation receipt

> Current status (2026-10-25, P5 E15/E16): **source-final for the bounded zero-duration binding fixture correction and owned documentation; native verification pending**. Earlier source-final sections and native outcomes describe older snapshots, not a pass on the current source.

Target: `demo` / `11.0.0`. Same original session/actor `neo-audit-1023-b04-02`, requested profile `gpt-6.1-sol / max`.

Status: **source-final for all three assigned actions, owned documentation and regression sources**. No Unity/compiler/test/product-checker execution occurred in this source phase. Original audit and plan remain historical evidence and were not rewritten. Earlier coherent source fixes were preserved.

Metadata correction: the new boundary fixture's invalid 33-character GUID has been replaced with the root-approved canonical 32-character GUID. **The corrected snapshot still requires a new freeze and central package/native import verification.** Earlier native imports ignored/rejected this fixture and do not establish its discovery or test coverage.

## Approved actions completed in source

### LEVEL-BUTTON-LABEL

`LevelButton.SetVisual` computes the optional one-based label with `((long)level + 1).ToString()`. `int.MaxValue` now presents `2147483648`. The stored `level`, `activ`, visual ID and UnityEvent payload types/dispatch remain unchanged. This corrects presentation arithmetic without changing selection, unlock rules, serialized fields or optional-label behavior.

### TEXT-INTEGER-PRECISION

Integrated the published `Neo.Tools.Text` API without another formatter or a second offset application. `TextLevel` retains its existing `Set(int)` method-group subscription to `OnChangeLevel` / `OnChangeMaxLevel`; initial snapshots use that same inherited integer overload. The Text owner now implements it through `SetInteger((long)value + IndexOffset)`, with exact completed `BigInteger` formatting and retained exact formatting-only redraws.

Consumed API from `Assets/Neoxider/Scripts/Tools/Text/SetText.cs`:

```csharp
public void SetInteger(long value); // Already offset final display value.
protected virtual void OnEnable();
protected virtual void OnDisable();
```

`SetInteger` itself belongs to `Neo.Tools.Text`; this actor did not edit that assembly. The dependency is available in source and documented in `Neo.Tools.Text.implementation.md`. No pending additive API blocks Neo.Level completion.

### TEXT-LIFECYCLE-HOOKS

`TextLevel.OnEnable/OnDisable` are now `protected override` methods, each calling its base once. Enable resumes the base animation before existing source initialization; if a manager is available, the normal snapshot replaces any older target with the latest level. Disable pauses the base animation before canceling the owned deferred wait and removing the level listener. Existing remove-before-add rebinding remains intact. A queued tween can resume while the label waits for a manager.

## Exact files changed in this implementation phase

| File | Source work |
| --- | --- |
| `Assets/Neoxider/Scripts/Level/LevelButton.cs` | Widen label addition to `long`. |
| `Assets/Neoxider/Scripts/Level/TextLevel.cs` | Override and compose published base enable/disable hooks. Existing typed integer binding retained. |
| `Assets/Neoxider/Tests/Edit/Level/Audit20261023NeoLevelBoundaryTests.cs` | New four-case exact label/index/state/event regression source. |
| `Assets/Neoxider/Tests/Edit/Level/Audit20261023NeoLevelBoundaryTests.cs.meta` | Root-approved canonical GUID `7193ba98667d41d9b7db388c452b613e`; corrected the original invalid 33-character token. All other importer fields retained. |
| `Assets/Neoxider/Tests/Edit/Level/Audit20261023NeoLevelLifecycleTests.cs` | Three new derived precision/lifecycle regression methods, manual owned-tween driving, exact prior loader restoration and owned-tween teardown. Original five cases retained. |
| `Assets/Neoxider/Docs/Level/LevelButton.md` | Widened display example and unchanged index/event semantics. |
| `Assets/Neoxider/Docs/Level/TextLevel.md` | Exact completed integers, offset/formatting/events, approximate interpolation, inherited long API and composed lifecycle. |
| `Assets/Neoxider/Docs/Level/README.md` | Canonical 11.0 summary and backlink to lifecycle/offset guidance. |
| `Docs/Audits/2026-10-23/Neo.Level.implementation.md` | This completion/dependency/evidence receipt. |

No shared trackers, asmdefs, package settings, migration guide, changelog, other actors' sources/tests/reports, scenes, prefabs or sample layout were changed. `LevelManager`, `Map`, `SceneFlowController` and the original progress fixture retain their prior audit corrections. Existing legacy test files belong to the test owner and were not repaired here. No unsupported serial-test annotation was added.

## Authored acceptance — UNEXECUTED

New boundary fixture: four parameterized cases check `int.MaxValue` labels in current/passed/locked states and `int.MinValue` presentation. Each asserts exact TMP text, unchanged signed index/activation, visual payload and the matching state event count.

The actual `TextLevel` lifecycle fixture adds:

1. `TextLevel_BoundIntegerEventsFinishExactlyAndRetainFormatting`: six value/offset scenarios through the bound integer receiver. Four use public manager selection; the two signed input endpoints invoke the public integer event independently of selection clamping. Literal Plain/Grouped expected digits cover `16777217`, `4294967293`, `-2147483648`, `-2`, `-4294967296`, `4294967294`, with `Level ` / ` XP` affixes and last `OnTextUpdated` payloads. Formatting-only switching must preserve exact final digits.
2. `TextLevel_ComponentDisableFreezesActiveTweenAndEnableBindsLatestLevel`: advance a nonzero-duration tween partially; disable only the component; change the manager and advance time. Numeric state, text and update count must remain frozen. Re-enable, complete the latest manager target and verify a subsequent level event still updates correctly.
3. `TextLevel_ComponentEnableResumesQueuedTweenAndDeferredManagerBinding`: queue an integer animation while disabled with no manager; manual advancement must not update it. Re-enable must resume and finish the actual derived tween. A later manager must still bind through the deferred wait and deliver level output.

The fixture identifies tweens through public DOTween target APIs, not private tween reflection. Reflection only configures existing serialized authoring fields. Teardown kills tweens for its own labels before destruction and restores the exact prior `SceneLoader` reference captured in its Play test context, rather than replacing it with a default loader. Diagnostics state is likewise restored. These sources require the isolated/exclusive runner contract: `DOTween.ManualUpdate` advances all manual tweens, and the fixture enters/exits Play Mode from the Edit assembly.

There are **seven newly authored declared cases**: four parameterized boundary cases and three lifecycle/precision methods; one method contains the six explicit integer scenarios. Original 31 declared cases remain, for 38 prepared cases across this owner's three fixtures. These counts describe source declarations, not actual discovery or passing tests.

## Deliberate 11.0 compatibility behavior

- Integer animations now finish with exact text; interpolation and public `currentNum` remain approximate floats. If exact completion differs from the last interpolated string, a final text update can be observed. Later number-format changes retain the exact integer. Float calls retain float semantics.
- `Set(int)` applies `IndexOffset` once; the additive `SetInteger(long)` accepts an already-computed display value and applies no offset. `TextLevel` continues using the integer UnityEvent-compatible path.
- Derived lifecycle composition now honors the base tween pause/resume policy while preserving level subscription/wait ownership. Further subclasses must override with a base call once. Existing serialized values, fields, defaults and public level APIs are preserved; no asset migration is needed.

Root owns release/migration notes and should include the exact final integer/event behavior and lifecycle-hook composition there, coordinating with the Text owner's receipt. Optional progression-model and scene-operation frameworks remain proposals in the original plan; they are not implemented or required for these assigned corrections.

## Actual work and remaining evidence debt

Performed contract/request/handoff and current-source reads using small `cmd.exe` calls, focused source/docs edits, complete authored-fixture source review, scoped read-only diffs, and GUID reference enumeration. The new GUID occurred only in its own `.meta` in that read snapshot. No checker suite, compilation, NUnit discovery/execution, Unity process, installer, service, commit, push or child agent was launched. Historical static passes are not reused as verification of this final source.

### Bounded metadata correction after actual central rejection

Root subsequently reported that the first actual packagecheck and both native imports rejected the boundary fixture metadata: its original GUID had 33 ASCII hex characters. The earlier reference enumeration only located the token; it did not prove canonical length or successful native import. The previous receipt's GUID-validity implication was incorrect.

Root's independent C investigation found no Git history/HEAD file and no serialized references to the invalid token in `Assets`, `Packages` or `ProjectSettings`; its only additional occurrence was this receipt. C also found no existing use of the approved replacement. Under that specific authorization, this resume changed only the GUID line to `7193ba98667d41d9b7db388c452b613e` and updated this receipt. Every other importer field and all existing C# fixture/runtime/public-document bytes were left unchanged. This is repair of newly authored invalid metadata, not regeneration of an existing asset identity. The thirteen legacy baseline metadata repairs remain exclusively with root.

No Unity, compiler, tests or checker was rerun during this correction; all writers/jobs were stopped and root's gate remains closed until the next global source freeze. Corrected package acceptance, native import, nonempty fixture discovery and test results are **pending**, not passed. The original boundary test source and four authored cases are unchanged; prior native ignores cannot be counted as their execution.

After every writer finishes, root's frozen-snapshot operator must compile/discover/execute on the agreed isolated Unity 6.3 and 6.6 hosts, including both actual derived nonzero-duration cases and exact final formatting/event output. Compilation must confirm the consumed protected virtual hooks and DOTween target APIs together with the completed Text assembly. Existing 0.9/manual activation cleanup, request callback reentry and terminal native scene events remain unexecuted engine debt from the original audit. Package/UTF-8/meta/GUID/product validation, MIRROR configurations and release sample import acceptance remain central tasks. No local version-condition branches, package roots or shared validation infrastructure were introduced.

## 2026-10-25 — S3 incomplete-run recovery and lifecycle correction

### Status and scope

The coordinator reported that the preceding S3 source attempt exited **3**. Its older source-final receipt is not completion evidence. This resume completed the released caller correction in `Assets/Neoxider/Tests/Edit/Level/Audit20261023NeoLevelLifecycleTests.cs`; no production runtime fix is inferred from the six failures below. One read-process launch during recovery failed with Windows CreateProcess error 8 (insufficient memory); its retry and subsequent source operations succeeded. An oversized receipt append command was rejected by cmd.exe (exit 1) before launch; the receipt was subsequently written through a direct patch.

Only that caller and this receipt were edited in this resume. Prior implementation, runtime, regression, metadata and documentation edits are retained. The boundary fixture GUID remains `7193ba98667d41d9b7db388c452b613e`; no GUID or importer fields changed here. No shared tracker, asmdef, Fixtures helper, reserved caller, foreign assembly or Core.Level.Data cap source was edited. Primary extraction is complete and this caller is released; no remaining helper extraction is delegated from this correction.

The reservations map was read immediately before each edit: `Local/Audits/assembly-2026-10-23/runtime-fixture-relocation-reservations.json`, status `source-final`, SHA-256 `14b7a974c8a8a861a666b65d43971f42e4f97b21c01734daacd466f1e88fc3a7`. It contains no Neo.Level caller reservation. Fixtures helpers/shared references remain primary-exclusive.

### Actual native input evidence

These results precede the current source and are historical evidence only:

| Artifact | SHA-256 | Owned evidence |
| --- | --- | --- |
| `Local/Audits/assembly-2026-10-23/unity/final/s3/u63/Receipts/22-full-edit-graphics.xml` | `7c6d24e11012ed5903fff8f24d603e8130ec4bcae262af3761f4cf772a6e57d2` | 47 Neo.Level cases: 41 passed, six lifecycle failures. Includes four passing boundary cases in that snapshot. |
| `Local/Audits/assembly-2026-10-23/unity/final/s3/u63/Receipts/21-full-play-graphics.xml` | `2b15216472bdc287418567ed947899346c6a134b33658b07533fa9a1d157dc3a` | No Neo.Level.Tests cases; supplies no owned Play coverage. |
| `Local/Audits/assembly-2026-10-23/full-edit-failure-action-index.json` | `df467fde346b0a1f7eca09a82b972f2e7532b7274758981969400628b1770e97` | E74–E77: confirmed fixture/precondition assessment, L/U actions. |

| Group | Exact lifecycle method(s) | Failure |
| --- | --- | --- |
| E74, one | `RuntimeLevelButton_BindsMissingButtonReferenceAndUnsubscribesOnDestroy` | Expected manager level 1, observed 0 (old line 247). |
| E75, three | `TextLevel_BoundIntegerEventsFinishExactlyAndRetainFormatting`; `TextLevel_ComponentDisableFreezesActiveTweenAndEnableBindsLatestLevel`; `TextLevel_ComponentEnableResumesQueuedTweenAndDeferredManagerBinding` | Expected live target tween; collection null in old `UseManualTween` (line 350). |
| E76, one | `TextLevel_DisabledWhileWaitingDoesNotSubscribeUntilEnabled` | Expected 7, observed 0 (old line 111). |
| E77, one | `TextLevel_ReenabledBeforeManagerCreationResumesWaiting` | Expected 5, observed 0 (old line 84). |

The queued-tween case also reported two DOTween missing-target/field errors. They are not ignored or whitelisted. PageMenu output in the failures does not establish ownership or justify clearing other objects.

### Completed source correction and preserved oracles

- Keep supported **top-level** `UnitySetUp -> EnterPlayMode` and `UnityTearDown -> ExitPlayMode`. Require actual `Application.isPlaying`, an advancing native `Time.frameCount`, and runtime object membership. Ordinary nested wait enumerators contain no Enter/Exit instructions.
- Create one uniquely named additive scene owned by the fixture. Move only owned GameObjects into it before attaching tested behaviors. No active scene switch, foreign scene unload, singleton reset, provider/session deletion or global tween reset occurs. Teardown destroys only tracked objects/label-target tweens and unloads only that scene; exact captured scene loader and diagnostics settings are restored.
- Configure labels/buttons while inactive, then activate them. Assert public `didAwake` and wait for public `didStart` plus a subsequent native frame. Managers must register through actual Awake: `TryGetInstance` must already return that exact owned manager, without forcing the lazy `.I` accessor. Preexisting managers cause an explicit arrangement failure and are left intact.
- Give waiting/button cases an explicit finite 16-level map so levels 1, 4 and 6 are valid fixture inputs. Probe the owned public manager event for exact payload/order/count. Waiting cases retain game-object disable/re-enable, component disable, no-update-while-disabled, and exact final 5/7 output; they now establish initial binding before testing later delivery.
- For text tween cases, require exactly one active tween targeted at the owned label and assert its playing/paused state. Replace manual DOTween advancement with real PlayerLoop observations. Only that tween uses independent normal updates and linear easing; borrowed `Time.timeScale`, production defaults and one-second animation duration are unchanged. Five-second unscaled wait bounds diagnose missing progress; all numeric/text/count assertions remain exact.
- Retain all six signed integer/offset pairs, including values beyond float precision, both negative overflow-sized sums, plain/grouped formatting, prefix/suffix and exact `OnTextUpdated` payload assertions. Signed extrema still exercise the public receiver event independently of selection clamping. Event probes verify all six publications.
- Retain the partially advanced 0 < value < 80 control; disabled value/text/update-count freeze; re-enable at current manager level 90; and later level 91 delivery. Add native destruction/replacement of the **owned** manager while the label is disabled, bind replacement level 12, assert that the old public event cannot change number/text/update count or create a tween, then prove replacement level 13 delivery.
- Retain disabled queued value 42 with exact zero/text/update-count freeze, native OnEnable resumption to 42, deferred manager binding, and later value 7. The deferred manager's initial tween is positively observed before subsequent publication.
- Retain runtime Button lookup with its serialized Button reference explicitly null. One actual Start subscription must produce exactly `[1]`; after native destruction and manager reset the event sequence must remain exactly `[1, 0]` on a further UI click.
- All **eight UnityTest method identities** remain. Both existing SceneFlow cases retain their positive/negative/reentrancy/call-count/completion assertions and intentional null-loader error expectations. The existing plain C# RecordingLoader is unchanged; no helper was extracted or moved.

Callback provenance was checked in source: LevelButton's native Start attaches its UI listener; TextLevel's Start/OnEnable binds the selected manager's public event; deferred initialization runs the label-owned `MonoBehaviour.StartCoroutine` path through `CoroutineExtensions.WaitWhile` and its completion callback; tween progress is supplied by DOTween's native normal update. No private lifecycle/Update/UserCode methods are invoked.

No product contract changed in this correction, so canonical public docs require no additional edit. Earlier integer-precision and lifecycle documentation remains applicable. Existing SetText integer/tween APIs and the separately owned Core.Level.Data capacity work remain cross-assembly dependencies; neither is modified here.

### Current source identities

SHA-256 after correction (receipt excluded to avoid a self-hash):

| File | SHA-256 |
| --- | --- |
| `Assets/Neoxider/Tests/Edit/Level/Audit20261023NeoLevelLifecycleTests.cs` | `8c24fad161f980c52050847c96c6423134ad40a8042a0d7ef9330f7578ca4181` |
| `Assets/Neoxider/Tests/Edit/Level/Audit20261023NeoLevelLifecycleTests.cs.meta` | `ce387360c4c8e59e94f8f4bd045201416e90ab02559c2788a7ef09990dbd6ab5` |
| `Assets/Neoxider/Tests/Edit/Level/Audit20261023NeoLevelBoundaryTests.cs` | `c151cd4dd27cb6cb14f40c239b66fb676f45e78c5e0b7b520bba0354364b6221` |
| `Assets/Neoxider/Tests/Edit/Level/Audit20261023NeoLevelBoundaryTests.cs.meta` | `52242b7d2ca84157bc10c10d25adc01b3abf5684db2c9ab36317fcc2f6f495db` |
| `Assets/Neoxider/Tests/Edit/Level/Audit20261023NeoLevelProgressTests.cs` | `fb2b19da011955c7c6b10badf9424d262b60f3535380b631b64a39cbc0455adc` |
| `Assets/Neoxider/Scripts/Level/LevelButton.cs` | `106830c3459814f61cda10d12591df4856d0a9e7bc8852d23dbe46b8cf0a4bd5` |
| `Assets/Neoxider/Scripts/Level/LevelManager.cs` | `7fc63bc89138c80f1079a458d6c89181a08aafb8267c9295fbf3e9be930c7ed9` |
| `Assets/Neoxider/Scripts/Level/Map.cs` | `1be669416b0649772e8b4fe6721c168c7d386025668ab5beb90ef6614701dbaa` |
| `Assets/Neoxider/Scripts/Level/SceneFlowController.cs` | `1a3a837c215dc2b7f74417c86866206b0efdd0da6870b03781bc0a7503875218` |
| `Assets/Neoxider/Scripts/Level/TextLevel.cs` | `4b0374b7385db2fa9ee2db210d0366d383b89925903acee6424fed44e26aaf8a` |

Caller input hash before this correction: `d7e1d533171f73e2c00672664de8762ce25f0bae25519865ac11103ea3ddcbc2`. The LevelButton, LevelManager, TextLevel and lifecycle meta hashes match the pre-edit snapshot byte-for-byte.

### Validation actually performed and remaining native debt

Performed read-only inspection of the assigned XML cases/action index/triage, relevant current runtime and coroutine callback paths, supported installed test-framework Enter/Exit behavior, and the complete corrected caller source. Computed file identities with Python SHA-256. These are source/artifact observations, **not compilation or passing tests**.

No Unity, compiler, test runner, product checker, installation, service, child agent, commit or push was started. No new native result exists for the source hash above. The historical six failures therefore remain **pending native verification**, not fixed-by-test evidence.

After every writer stops and the coordinator declares a new global freeze, root must compile/import and rerun the eight lifecycle methods, the preserved progress/boundary/legacy cases, and the corresponding full native suites on the agreed isolated Unity 6.3 and 6.6 hosts. Acceptance requires observed native Play/frame/Awake/Start positives; all prior value/text/count/ownership/payload oracles; the added old/new manager controls; and no unexpected DOTween missing-target errors. Play XML with no owned cases is not coverage. Any foreign-manager arrangement failure or absent native frame/tween must be reported at that positive precondition before proposing a runtime change.

Source writes stopped after this caller and receipt. Status: **source-final; new native execution outstanding**.

## 2026-10-25 — P5 E15/E16 binding, target and clock correction

### Actual input and diagnosis

Read the complete implementation contract/request and `Local/Audits/assembly-2026-10-23/p5-source-correction-common.md` (SHA-256 `0db172b4c313e8737ef490c14c5bd386b38edba8a4f28edc8b7b2021d9c30147`). Input snapshot identity: `956e2aa29ea85e7dea90df498a70392083af4a067058cbcbe24891105966efd1`, source under `Local/Audits/assembly-2026-10-23/unity/snapshots/final-20261025T151934Z/source`.

Both actual P5 Edit XMLs contain 47 Neo.Level cases: **45 Passed / 2 Failed**, including six passing and two failing lifecycle methods. These are prior-snapshot results only:

| Artifact | SHA-256 |
| --- | --- |
| `Local/Audits/assembly-2026-10-23/unity/final/p5/u63/Receipts/20-full-edit-graphics.xml` | `2fc329874dff049e845eebd01635deb1f445359fd951fe82536c79cb2bb7debd` |
| `Local/Audits/assembly-2026-10-23/unity/final/p5/u66/Receipts/20-full-edit-graphics.xml` | `adeb027f9f14e1c34c8973767f9a6d3420c7c188f809ea26283ebda32d31a74b` |
| `Local/Audits/assembly-2026-10-23/final-edit-p5-triage.json` | `59c218396e4723537d93c2110b5c0bbc7051f4550fdbbc2b6196e5316f75a9d6` |
| `Local/Audits/assembly-2026-10-23/unity/final/p5/u63/Receipts/20-full-edit-graphics.log` | `c8c40752d7c19dfbb083cf77f1c947d3b8afd89aad58f2bc952fec596e5a1b34` |

E15 is `TextLevel_DisabledWhileWaitingDoesNotSubscribeUntilEnabled`: immediately after native re-enable, old line 120 expected string `5` but observed null. E16 is `TextLevel_ReenabledBeforeManagerCreationResumesWaiting`: the old line 91 wait never observed initial `1`; the native five-second/frame assertion failed at old line 440. Both versions show these same failures. Native manager registration and existing Play/Start/frame preconditions passed before them. The null observation is the TMP **string value**, not proof that the configured TMP component reference was lost.

Source establishes two separate stages. TextLevel's native Start/OnEnable resolves the manager, calls inherited `Set(int)`, and attaches the manager event. `SetInteger` delegates to `Set(float)`, which always constructs a DOTween tween, including with `_timeAnim == 0`; it does not synchronously assign the final text. Thus E15's immediate text assertion did not follow from synchronous binding. Deferred binding uses the label-owned WaitWhile coroutine and then the same numeric tween path. Singleton native Awake sets the same cached instance read by `.I`, and the deferred callback clears its handle before Init, so current source does not demonstrate a separate missing-instance or self-cancellation defect.

The prior three nonzero-duration text cases explicitly configured their owned tween with `SetUpdate(UpdateType.Normal, true)` and passed; these two zero-duration cases did not configure or observe that clock. E16 therefore conflated a missing deferred callback with an unadvanced default-clock tween. Native frame progress alone does not establish DOTween clock progress. **The actual Time.timeScale/default-clock state at the P5 failure was not recorded, so a paused clock remains an inference, not a measured runtime cause.** The correction establishes tween creation/output and controls only that owned tween's clock; it does not lengthen the wait or change production defaults.

E15's output contains the two-error DOTween safe-mode summary, as do the three passing text cases. The detailed captured errors in the same raw 6.3 log at lines 21757–21807 identify destroyed `UnityEngine.UI.Image` targets in `ImageFillAmountAnimator.SetValue`'s setter, source line 164, through `Image.fillAmount`; they do not identify TextLevel/TMP. Repeated aggregate summaries do not establish an owned-label target failure or prove that all later summaries have identical provenance. Root retains this evidence for the ImageFillAmountAnimator owner and final native triage. No warning is suppressed, expected, cleared or attributed to a repaired label without new evidence.

### Completed bounded changes

Only these paths changed in this resume:

- `Assets/Neoxider/Tests/Edit/Level/Audit20261023NeoLevelLifecycleTests.cs`: retain all eight UnityTest identities, real top-level Enter/Exit barriers, Start/frame positives, owned-manager replacement controls and existing assertions. Strengthen the two waiting cases and TMP target arrangement as described below.
- `Assets/Neoxider/Docs/Level/TextLevel.md`: clarify, in English and Russian, that binding schedules numeric output through SetText's configured tween clock even at zero duration; OnTextUpdated observes the actual write. Clarify initialized TMP ownership requirements. This documents existing behavior, not an API/default migration.
- This implementation receipt: preserve prior context and record current evidence, correction and debt.

The fixture now asserts that the exact configured TMP component remains live, belongs to the label GameObject, has received native Awake and is active in the runtime world. Both waiting cases additionally confirm that `.I` returns the already-native-registered owned manager; this lookup follows the existing non-resolving registration assertion and cannot substitute for Awake.

For E16, first require the deferred callback to produce an owned tween **or an already committed output event**. If the tween remains live, require exactly one playing target tween with the original zero duration, then use the same independent normal PlayerLoop clock as the existing nonzero cases. Accept already completed output only with the exact payload and event count; a working normal clock can finish a zero-duration tween before the test observes it. Callback creation and completion share the original absolute five-second deadline. Missing callbacks and missing tween progress now fail at distinct observations. No private callback or manual tween Update runs.

For E15, require native OnEnable to have created the live owned zero-duration tween before yielding for completion. This directly tests binding rather than merely moving the old assertion later. While disabled, require no text event, no queued/active owned tween, unchanged text and currentNum zero. Then require exact snapshot `5`/currentNum 5 and exactly one text event; subsequent manager level 6 must finish at `7`/currentNum 7 with exactly two text events in order `[5, 7]`. Manager publications remain exactly `[4, 6]`.

The companion E16 case now preserves the initial `1`, existing `5` and currentNum assertions and adds later `7`: exact text payload sequence `[1, 5, 7]`, manager publications `[4, 6]`, and matching currentNum at each completion. All signed integer/plain/grouped/affix controls, active/queued freeze/resume controls, stale-manager event and replacement controls, button delivery/unsubscribe counts and SceneFlow oracles remain. All numeric formatting and count comparisons remain exact.

The zero-duration helper records actual frame, Time.timeScale, scaled/unscaled delta and tween elapsed values for the next native run. It modifies only the label-target tween through the public DOTween API. It never resets Time.timeScale, a global DOTween default, a foreign tween/scene/provider/session, or a borrowed setting. Zero duration and the five-second bound remain unchanged. Public/serialized APIs, save behavior, GUIDs, asmdefs and release layout are untouched.

No confirmed shared SetText runtime defect was established, so no overlapping Text b08-04 edit or runtime API request was made. TextLevel and SetText source bytes match P5. Existing coroutine and Text APIs remain consumed dependencies; their native callback/tween behavior is still subject to the frozen rerun.

The reservation map was checked before each edit, status `source-final`, SHA-256 `14b7a974c8a8a861a666b65d43971f42e4f97b21c01734daacd466f1e88fc3a7`; no Neo.Level caller is reserved. No Fixtures/shared helper, other owner source or shared tracker changed.

### Current identities and actual validation

| Path | Current SHA-256 |
| --- | --- |
| `Assets/Neoxider/Tests/Edit/Level/Audit20261023NeoLevelLifecycleTests.cs` | `ed07aea839111d67d983a861a22c4484ba4273f3c81a17da63ce0fa2f8b9e4ed` |
| `Assets/Neoxider/Docs/Level/TextLevel.md` | `1356faadccb930aee6718f46d6ad20b3f06b93bec68a691b6dc60aa8e8baa4fb` |
| `Assets/Neoxider/Scripts/Level/TextLevel.cs` (unchanged) | `4b0374b7385db2fa9ee2db210d0366d383b89925903acee6424fed44e26aaf8a` |
| `Assets/Neoxider/Scripts/Tools/Text/SetText.cs` (read-only dependency, unchanged) | `c5b01a6069a1d1bdedc32edff1729bb41070b0ddbf9351a81fc30d94f7cf07da` |

P5/pre-edit caller SHA-256: `8c24fad161f980c52050847c96c6423134ad40a8042a0d7ef9330f7578ca4181`. P5/pre-edit owned guide SHA-256: `d5a579ab6cc3d7de83144a584f8320a11682628b93c540b282c4c8f7052b6029`. Read the actual prior XML cases and detailed log stacks, inspected the relevant current runtime/coroutine/Singleton paths and bundled DOTween SetUpdate contract, reviewed the complete caller diff against P5, and computed UTF-8/file hashes. These are source/artifact checks only.

**No Unity, compiler, tests, product checker, installation, service, process control, other agent, commit or push was launched.** No new native result exists. Root must freeze only after every writer finishes, then rerun all eight lifecycle methods and the preserved owned/full suites on both Unity 6.3 and 6.6. Acceptance requires owned TMP/source/tween positives, exact disabled no-subscription and 1/5/7 value/payload/count controls, all prior replacement and negative cases, and explicit triage of unexpected DOTween errors. If E16 produces neither a tween nor output under the new first-stage observation, deferred binding remains a separate native failure; the clock explanation must not be claimed proven. Runtime clocks/target state and warning provenance remain unverified until that run.

Writes stopped. **P5 correction source-final; current source remains uncompiled and native-unexecuted.**
