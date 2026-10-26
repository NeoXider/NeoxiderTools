# Neo.Tools.Text — implementation receipt

Target: `demo`, package `11.0.0`. Same original actor/session, `gpt-6.1-sol`, reasoning `max`.

Status: **SOURCE-FINAL FOR P6 FIXTURE NATIVE-CONTEXT PREREQUISITE CORRECTION ONLY; NATIVE ACCEPTANCE PENDING. E25 remains OPEN; no runtime fix or revised-source pass claimed.** All owner writes are finished. No implementation/source/API dependency remains except the new frozen u63/u66 verdict. Root owns native review and the next global freeze/execution. The supplied P6 input artifacts each report 51 cases: 40 Passed and 11 Failed; those historical failures remain recorded below. The revised fixture has not been compiled or executed. Earlier runtime corrections, historical audit and plan remain intact. No Unity/compiler/test/checker execution was started by this actor.

## Published API for dependent owners

Available now in `Assets/Neoxider/Scripts/Tools/Text/SetText.cs`, namespace `Neo.Tools`:

```csharp
public void SetInteger(long value);
protected virtual void OnEnable();
protected virtual void OnDisable();
```

- `SetInteger(long)` animates the supplied final integer **without applying `IndexOffset`**. Intermediate display and public `currentNum` remain approximate floats. Completion retains the exact integer through existing `BigInteger.ToPrettyString` formatting, including later formatting-only redraws. It creates no new formatter or dependency.
- Existing `Set(int)` remains the UnityEvent-compatible path: it widens `value + IndexOffset` once, then delegates to `SetInteger(long)`. Existing Level/Score callers using `Set(int)` automatically receive the exact final integer. Callers with an already-computed display value can call `SetInteger((long)value + offset)`; do not apply the same offset again.
- `Set(float)` and immediate `SetBigInteger` retain their existing roles. Replacing or clearing an integer animation invalidates its pending final commit through owned-tween identity.
- Dependent subclasses should use `protected override void OnEnable()` / `OnDisable()` and call `base.OnEnable()` / `base.OnDisable()` once. Keep existing listener, reactive subscription and deferred-wait cleanup in the derived owner. These hooks pause/resume the existing owned animation; they do not add listeners.

Cross-owner handoff: `Neo.Level` owns `Assets/Neoxider/Scripts/Level/TextLevel.cs`; `Neo.Tools.Components` owns `Assets/Neoxider/Scripts/Tools/Components/TextScore.cs`; `Neo.Shop` owns `Assets/Neoxider/Scripts/Shop/TextMoney.cs`. Their implementations and derived-type lifecycle regressions remain with those actors. This actor did not edit their sources or tests.

## Approved actions

- `TEXT-INTEGER-PRECISION`: source implementation, owned documentation and regression sources complete. `Set(int)` delegates the widened sum to the additive long API; completion retains its `BigInteger` formatter, including formatting-only redraw. Callback ownership prevents a replaced tween from committing an older final integer.
- `TEXT-LIFECYCLE-HOOKS`: protected virtual base messages, public migration documentation and handoff complete. Dependent override integration and actual TextLevel/TextMoney lifecycle regressions belong to Level/Shop.
- `TEXT-SCORE-LIFECYCLE`: protected virtual base messages and handoff complete. Actual TextScore override/reactive-subscription integration belongs to Components; this receipt does not claim that actor's source or runtime acceptance is finished.
- `TEXT-MODE-ATOMICITY`: source implementation, owned documentation and positive/negative transition regression sources complete. `CanDisplayTime(float, TimeDisplayMode)` validates the candidate before assigning `_displayMode`. Rejected switches retain mode, accepted time, text and time-event baseline; valid switches use the existing formatter without a time-change event.

## Exact changed files and compatibility

| File | Completed source work |
|---|---|
| `Assets/Neoxider/Scripts/Tools/Text/SetText.cs` | `SetInteger(long)`, exact completion through existing number options/affixes, ownership-guarded completion, protected virtual enable/disable hooks. |
| `Assets/Neoxider/Scripts/Tools/Text/TimeToText.cs` | Candidate-mode validation before mutation; existing Set range checks reuse the same private predicate. |
| `Assets/Neoxider/Tests/Edit/UI/Audit20261023NeoToolsTextSetTextTests.cs` | Added exact integer/offset/long/formatting/notification/replacement regression sources; preserved earlier coherent regressions. |
| `Assets/Neoxider/Tests/Edit/UI/Audit20261023NeoToolsTextTimeToTextTests.cs` | Added rejected large positive/negative Compact-to-Clock transitions followed by successful 90-second transitions. |
| `Assets/Neoxider/Docs/Tools/Text/README.md` | Canonical module guidance for exact integer completion, derived lifecycle composition and atomic mode changes. |
| `Assets/Neoxider/Docs/Tools/Text/SetText.md` | Exact API/offset semantics, examples, float-state/intermediate limits, completion notifications and derived override migration. |
| `Assets/Neoxider/Docs/Tools/Text/TimeToText.md` | Rejected-mode behavior, getter-based rejection detection and a parsing example that preserves the configured-mode contract. |
| `Docs/Audits/2026-10-23/Neo.Tools.Text.implementation.md` | Early published dependency API and this final completion receipt. |

Deliberate 11.0 changes: animated integer text now finishes with exact digits instead of inheriting float precision loss; the approximate `currentNum` remains unchanged by the exact final commit. If the exact text differs from the last interpolated string, completion emits its own final `OnTextUpdated`; identical text does not produce a redundant completion notification. Existing `Set(float)` remains float-only, and `SetBigInteger` remains immediate. Subclasses with private enable/disable Unity messages must migrate to overrides with base calls; subscription/wait ownership stays with each derived component. Unsupported display-mode switches now leave the previous mode intact instead of committing an unusable mode.

Existing signatures, serialized fields/types/defaults, integer UnityEvent bindings, options, affixes and diagnostics gates are retained. The additive method uses a distinct name, so existing overloaded `Set` method-group resolution is preserved. No asmdefs, package configuration, scenes/prefabs, sample layout, shared trackers, existing legacy fixtures or user data were changed. There are no new package files needing metadata in this implementation step; existing regression metadata remains in place. Root should include the integer completion and derived-hook migration in the 11.0 release/migration notes it owns.

## Initial authored acceptance sources — unexecuted at the initial handoff

The two prefixed fixtures now contain **49 cases** (31 SetText, 18 TimeToText): 18 new cases beyond the original audit's 31. This is source enumeration, not discovery or execution evidence.

- Seven integer completion cases assert literal Plain/Grouped text and the last event string for `16777217`, an offset crossing the float-precision boundary, negative values, both signed input/offset extremes and both Int32 endpoints with offsets.
- Two long API cases assert exact `long.MaxValue`/`long.MinValue` and prove `IndexOffset` is not applied a second time.
- Formatting-only redraw asserts `Level 16777217 XP`, then `Level 16,777,217 XP`, while public `currentNum` remains the approximate `16777216f`. Inspector affixes are configured through `SerializedObject` in the Edit fixture; no runtime reflection helper was added.
- An exactly representable integer has one final notification rather than an additional identical completion write. Four superseding-write cases and a reentrant replacement case protect the newest string/float/integer/clear state from an older pending integer completion.
- Two time-mode cases cover large positive and negative Compact time: rejected Clock transition preserves mode/time/text/event baseline; after accepting 90 seconds, Clock transition produces `01:30` with no time-change event.
- The original two real enter/exit-Play lifecycle cases remain present, along with earlier formatting, invalid-duration and event-reentrancy cases. These plain SetText cases do not stand in for actual TextLevel/TextScore/TextMoney lifecycle coverage.

## Dependency receipt and remaining evidence debt

No other owner's additive API was needed to finish this actor's initial source. The Text base API above was available for downstream integration without a new assembly reference or formatter. During the initial read-only handoff inspection, TextMoney already declared protected overrides with base calls; TextLevel and TextScore still declared private callbacks. This is historical handoff context, not a claim about their current source. Root owns dependent receipt integration; actual derived-type lifecycle evidence belongs to those owners.

`Extensions-Numeric-Time-Random` continues to own the existing `ToPrettyString`, duration parser and Clock/Compact formatting behavior consumed here. The optional shared duration-core API remains a substantial **proposal only** in `Neo.Tools.Text.plan.md`; it was not implemented or introduced as a dependency of these corrections.

Actual work performed in this phase: current source/audit/plan/request reads, scoped source/docs edits, authored regression sources, read-only scoped diffs and cross-owner reference enumeration. No new compile/test/validator result is asserted. Historical static checks predate these final source changes and are not reused as a pass receipt.

The initial receipt requested frozen-snapshot compilation/discovery/execution on isolated Unity 6.3 and 6.6 hosts. Root subsequently executed Snapshot3; its assigned failures are recorded below. That earlier execution is not a pass receipt for the revised source. `DOTween.ManualUpdate` advances global manual tweens, so the isolated/exclusive runner requirement remains. No commits or pushes were performed by this actor.

## Snapshot3 follow-up — TEXT-NATIVE-REPLACEMENT-TWEEN (2026-10-24)

### Evidence read, not rerun

Read the exact assigned XML cases and action-index entries E87–E90 from:

- `Local/Audits/assembly-2026-10-23/unity/final/s3/u63/Receipts/22-full-edit-graphics.xml`, SHA256 recorded by the index: `7c6d24e11012ed5903fff8f24d603e8130ec4bcae262af3761f4cf772a6e57d2`.
- `Local/Audits/assembly-2026-10-23/unity/final/s3/u63/Receipts/21-full-play-graphics.xml` (root reference `2b152164...`). No cases with this owned fixture's fully qualified class were present there; it supplies no independent pass for these six cases.
- `Local/Audits/assembly-2026-10-23/full-edit-failure-action-index.json` and the owned references in both failure-triage documents. Action U explicitly requires assessment rather than assuming every failure is a runtime defect.

| Entry | Existing failed oracle preserved |
|---|---|
| E87 | `Animation_HiddenParentPauses_AndDestroyedComponentStopsWriting`: initial advancement expected greater than zero, actual zero. Pause/resume/destroy behavior was not established by that failed precondition. |
| E88, three cases | `ImmediateWrite_ReplacesAnimationAndRemainsOnScreen`: percentage, currency and BigInteger replacements had expected later notification count 2, actual 3. Existing assertions had already established an advancing unfinished tween and the exact replacement text. |
| E89 | `IntegerCompletion_DoesNotOverwriteASupersedingWrite("float","9.5")`: expected `9.5`, actual `10`. |
| E90 | `SetOnDisabledComponent_WaitsForEnable`: expected `100` after enabling, actual `idle`. The previous fixture did not inspect the created tween's actual update mode or native play/pause state. |

### Completed bounded correction

`Assets/Neoxider/Scripts/Tools/Text/SetText.cs`: every numeric setter callback now checks that its captured non-recyclable tween still owns `_tween` before changing `currentNum` or publishing through the formatter. Previously, the setter unconditionally used the mutable `_formatCurrentNumber`; a late old callback could therefore republish a newer numeric replacement while literal replacements, whose formatter is null, remained quiet. This matches the E88 failure pattern; the revised count/state oracles still need native execution to close it. No DOTween vendor defect is asserted from source inspection.

Float completion now settles the supplied float target before final formatting. Float `start + (target - start)` can lose a small endpoint: with start `16777216f` and target `9.5f`, the rounded float difference can produce `10f`. Completion uses the original target and existing `FormatNumber` options, and only emits a corrective string when it differs from the displayed text. This preserves decimal places, rounding, notation, affixes and all serialized defaults. It does not reinterpret float requests as integers. The integer API intentionally replaces that float completion callback with its existing exact BigInteger commit, retaining the approximate integer interpolation state. Older setter/completion callbacks cannot overwrite a superseding operation.

Production enable/disable hooks and their policies were left intact: the two native setup failures alone did not prove a lifecycle implementation fault.

### Positive preconditions and retained regression sources

`Assets/Neoxider/Tests/Edit/UI/Audit20261023NeoToolsTextSetTextTests.cs` retains every earlier case and substantive text, event-count, precision, pause/resume and destruction oracle. The immediate-replacement test additionally asserts no late mutation of public `currentNum`; its expected notification count was not relaxed.

The helper now obtains the newly created owned tween through public `DOTween.TweensByTarget`, requires exactly one active owned tween with positive duration and clock, and assigns that tween `UpdateType.Manual` with independent timing **after creation**. The old helper only changed a global default before possible first-use initialization, without confirming the actual tween's mode. It no longer changes `DOTween.defaultUpdateType` at all. An advancing global DOTween clock is an explicit precondition, not silently reset; Unity/global time scales, other animations, scenes and providers are not reset or destroyed.

Both lifecycle cases retain supported top-level `UnityTest` EnterPlayMode and top-level `UnityTearDown` ExitPlayMode. They now require actual Play Mode, the real enabled SetText/TMP binding, owned tween readiness and an initial advancing tween before pause evidence. Native disabling must visibly pause that tween, native re-enabling must visibly play it, and destruction must release it. No private lifecycle/Update invocation, nested mode exit, ExecuteAlways, expected-error whitelist, Ignore or tolerance change was introduced. Cleanup still targets only owned objects/tweens.

Two additional public-API float cases start from `16777216f`/`-16777216f` with decimal places explicitly 1 and require the exact requested `9.5f`/`-9.5f`, final event payload and retained formatted text. The fixtures now author 51 cases (33 SetText, 18 TimeToText); this is source enumeration, not new discovery or pass evidence.

`Assets/Neoxider/Docs/Tools/Text/SetText.md` now documents float endpoint settlement and stale-callback suppression. No other runtime module, shared asmdef, helper placement, vendor migration target, settings, GUID or public/serialized signature was changed.

### Reservation/dependency and unresolved native evidence

The live `runtime-fixture-relocation-reservations.json` was read before each edit. None of the four edited paths (runtime SetText, its prefixed fixture, its existing guide and this receipt) was reserved. No helper was moved/extracted and no reserved caller or Fixtures reference was touched. The primary actor's map being source-final was not treated as permission to edit its reserved files. There is no deferred reserved-file edit for this actor.

Source writes for this follow-up are complete. Root must establish the new global freeze after **all** writers finish, then rerun the exact six cases and the preserved positive/negative/reentry/precision matrices. E87 and E90 still require actual native advancement/disable/reenable/destroy evidence; E88 still requires unchanged strict later event counts and retained state; E89 and the added signed endpoint cases require exact replacement output. Foreign `[PM]`/DOTween Safe Mode output in the old XML was not hidden or used as a reason to reset foreign scenes. Whether native host contamination remains is unverified. No compiler, Unity, test, validator or product-checker process was launched by this actor during the closed-execution correction.

## P5 E25 — 2026-10-25, original b08-04 actor

### Actual evidence and preserved source

Read the complete implementation contract, own request and `Local/Audits/assembly-2026-10-23/p5-source-correction-common.md`; read E25 in `final-edit-p5-triage.json` and both engines' actual assigned XML cases. The P5 snapshot is `Local/Audits/assembly-2026-10-23/unity/snapshots/final-20261025T151934Z/source`, archive SHA-256 `956e2aa29ea85e7dea90df498a70392083af4a067058cbcbe24891105966efd1`. Current SetText runtime and its original fixture matched that snapshot and both P5 hosts byte-for-byte before this correction.

XML base: `Local/Audits/assembly-2026-10-23/unity/final/p5/`.

| Exact file beneath XML base | SHA-256 | Owned observations |
|---|---|---|
| `u63/Receipts/20-full-edit-graphics.xml` | `2fc329874dff049e845eebd01635deb1f445359fd951fe82536c79cb2bb7debd` | 51 cases: 49 Passed, 2 Failed. |
| `u66/Receipts/20-full-edit-graphics.xml` | `adeb027f9f14e1c34c8973767f9a6d3420c7c188f809ea26283ebda32d31a74b` | 51 cases: 49 Passed, 2 Failed. |
| `u63/Receipts/21-full-play-graphics.xml` | `7ae7f99a5f648cf31a86a445bdd59761bfc3390d4bcf9d4d3e1371a6e86d681c` | No cases from the two owned fixtures. |
| `u66/Receipts/21-full-play-graphics.xml` | `73d18c2935644dd92c8fecd6cd1415c5d8f9443b9f0fd3a7afadf2ce9974cc69` | No cases from the two owned fixtures. |

Both Edit failures are `Neo.Tests.Edit.Audit20261023NeoToolsTextSetTextTests.IntegerCompletion_DoesNotOverwriteASupersedingWrite`, arguments `("float","9.5")` and `("integer","16777219")`. The snapshot's `Animate:424` reports active count 2 instead of 1, called at lines 267/270. Final endpoint assertions were not reached. The XML does not record tween references, the retired handle's active flag or its kill callback; it cannot distinguish two different live tweens from duplicate query references. The other 49 passes include the prior hidden-parent/destroy, disabled-enable, immediate-replacement count and signed float-endpoint cases. Those are observations about the P5 snapshot, not results for this new fixture.

### Kill/query investigation and bounded regression correction

Read the bundled DOTween XML contracts and DLL metadata/method bytes without loading or executing managed code. In the actual DLL, `TweenExtensions.Kill` (`0x06000085`) checks `initialized` and `active`; during an update it sets active false, otherwise calls `TweenManager.Despawn`. Despawn also sets active false before resetting the tween. `IsActive` (`0x0600009d`) reads the active backing flag. `GetTweensByTarget` (`0x06000317`) reorganizes the active array and selects target matches; the existing fixture separately filters IsActive. `SetUpdateType` (`0x060002f7`) changes update counters/settings, without adding another array entry. The P5 u63 compiled SetText's StopAnimation also contains the expected IsActive and Kill(false) calls. [Upstream Kill](https://github.com/Demigiant/dotween/blob/develop/_DOTween.Assembly/DOTween/TweenExtensions.cs) and [manager source](https://github.com/Demigiant/dotween/blob/develop/_DOTween.Assembly/DOTween/Core/TweenManager.cs) were supplementary context; the installed DLL was inspected directly.

Consequently, ordinary pending removal alone does not explain two **active** entries. Two distinct active handles would be a real ownership failure; duplicate references would implicate query/manager bookkeeping. Neither explanation is established by the supplied XML. No speculative production kill strategy or vendor change was made, and an active tween is not discarded from the count.

Only `Assets/Neoxider/Tests/Edit/UI/Audit20261023NeoToolsTextSetTextTests.cs` and this receipt changed in P5:

- All four original superseding-write cases retain their names, inputs and exact final strings. They now retain the original tween, require active/playing status, positive unfinished elapsed time and intermediate numeric progress before replacement, and observe its OnKill callback. Replacement must immediately release that handle and kill it exactly once.
- Float/integer replacements pass the retired handle to Animate. Failure diagnostics report total query entries, active entries, distinct active references, retired-reference matches and the retired active flag. **The existing exact-one active-entry assertion is retained unchanged in strength; references are not deduplicated for acceptance.** A live retired handle fails separately, and the replacement must have a different reference.
- The original final string assertion remains, with added final event-payload, stable later currentNum/text/event-count and exactly-once kill assertions. The existing seven immediate-write cases also require their retained old handle to be inactive. No case or positive/negative/count/payload oracle was removed. Case inventory remains 51 (33 SetText, 18 TimeToText).

The extra later clock advance probes stale writes only after the strict ownership assertions; it does not flush bookkeeping before counting. No new runtime API, serialized/default/rounding/formatting change, lifecycle workaround, global settings mutation, reflection invocation, shared helper extraction or shared configuration edit was introduced. Canonical public docs remain accurate because this P5 change adds no product contract. Previous float endpoint settlement, exact integer formatter and retired setter/completion guards remain intact.

### Source hashes and precise remaining dependency

| Source/artifact | SHA-256 |
|---|---|
| `Assets/Neoxider/Scripts/Tools/Text/SetText.cs` — unchanged | `c5b01a6069a1d1bdedc32edff1729bb41070b0ddbf9351a81fc30d94f7cf07da` |
| `Assets/Neoxider/Tests/Edit/UI/Audit20261023NeoToolsTextSetTextTests.cs` — P5 input | `d357407af232e088905961080227b9f52e9428ef165af6bf5805ed828b26c277` |
| Same fixture — revised source | `24c9282604c59b9eebb5ae56dff57860459c3cda49a42a436e8bba56a591d2d4` |
| `Assets/Plugins/Demigiant/DOTween/DOTween.dll` — unchanged, same in both P5 hosts | `8e90fc43ab564a5a51424af56531ab074a5a2e9cab271b2d653d287954237b83` |

Actual validation in this phase was artifact XML parsing, byte/hash comparisons, read-only DLL method inspection and review of the complete fixture delta against the frozen input. No revised-source compilation or native/test pass is claimed. The reservation map was checked before both file edits; neither path was reserved. Runtime, public docs, GUIDs, historical audit/plan, other owners, fixtures assembly, shared trackers and migration/changelog files were not edited.

**E25 ownership failure remains OPEN; native diagnosis REQUIRED.** After all writers finish, root owns the next global freeze/execution. Its first execution will run the exact strengthened E25 cases and retained 51-case matrix on u63/u66, collecting retired-identity, kill and raw-active-entry payloads **before choosing any production fix or full rerun**. Required evidence is the original positive tween baseline, retired handle inactive with exactly one kill, exactly one active target entry, exact `9.5`/`16777219` final text/payload and no later writes. If ownership still fails, the new reference diagnostics identify whether it is a surviving retired handle, a second distinct live handle, or a duplicate query reference. No runtime fix or revised-source pass is claimed; the old XML does not authorize an assertion relaxation.

Level b04-02's E15/E16 request was read for shared context: it concerns native initialization/zero-duration binding and missing-target output. No shared SetText cause was demonstrated here, so no Level edit, lifecycle/default change or cross-owner fix is claimed. No new public migration/changelog entry is required. **All authoring and writes by this owner are finished. No source/API dependency remains apart from actual native E25 evidence.** Root owns the next freeze/execution and any later resumption of this SAME session for an actual fix if the diagnostic evidence proves one. This receipt-only handoff changes no runtime, fixture or guide; the diagnostic delivery is source-final while the native verdict remains pending.

## P6 native Play initialization prerequisite — 2026-10-26

### Actual failed input and bounded diagnosis

Read the complete current implementation contract and own request, `Local/Audits/assembly-2026-10-23/p6-text-native-context-assessment.md` (SHA-256 `038e28f0c4d2a7eb2d8f871d1d6ed22c50cdb2a85293a09e3fb15074c933a2fb`), and all 11 raw failures plus lifecycle controls from both actual XML inputs:

| Actual input | SHA-256 | Recorded result |
|---|---|---|
| `Local/Audits/assembly-2026-10-23/unity/final/p6/u63/Receipts/10-text-diagnostic.xml` | `18d4a4957f040d418762749d073c16880e89d9deec90d06890ba2279147322fa` | 51 = 40 Passed / 11 Failed. |
| `Local/Audits/assembly-2026-10-23/unity/final/p6/u66/Receipts/10-text-diagnostic.xml` | `c02b19885a08a7fa29c3f2c935371946dd03f17f5dc6b1f9c150449bbcdb9588` | 51 = 40 Passed / 11 Failed. |

All seven immediate-write variants reached the exact replacement string, then failed because the retired handle remained active. The string/clear superseding variants also failed retired-inactive. The float/integer variants recorded raw entries 2, active entries 2, **distinct active references 2**, retired matches 1 and retired active True. P6 therefore rules out duplicate-reference-only bookkeeping for those observed failures. The exactly-once kill assertion was unreached: **actual kill count is unknown, not observed zero**. The later endpoint/event/stability oracles in those cases were unreached too. Both genuine Play lifecycle cases passed in each input; those controls do not establish initialized SDK context for the other cases.

The assessment records the pinned DLL's public Init/InitCheck refusing first initialization outside Play, Kill returning while uninitialized, and ApplyTo/ManualUpdate being able to continue. Creation, IsActive and numeric progress alone therefore did not establish Kill's prerequisite in the former plain Edit SetUp. The failing calls' actual initialized flag was not recorded. The assessment's compiled PDB binding also identifies the exact `c5b01a...` runtime source; no stale-assembly theory or production Kill defect is asserted.

### Completed fixture correction

Only `Assets/Neoxider/Tests/Edit/UI/Audit20261023NeoToolsTextSetTextTests.cs` and this receipt changed in this P6 source phase.

- SetText's setup is now a supported **top-level UnitySetUp**. It rejects an already-running or pending Play session while ownership is false, yields EnterPlayMode, then marks the successfully acquired session before any SDK/view setup can fail. It requires real Application.isPlaying, a non-null native Behaviour returned by **public DOTween.Init()**, and an enabled driver in an active hierarchy before creating the view. Animate also requires that owned Play context and admitted active driver.
- The fixture is Serializable and its ownership flag is SerializeField. The installed test runner's `TestRunnerStateSerializer` uses JsonUtility to preserve fixture fields across domain reloads; its UnitySetUp documentation explicitly supports a plain Test following EnterPlayMode. No runner or shared framework source was changed.
- Top-level UnityTearDown returns immediately without ownership. After acquisition it cleans only its own component target/hierarchy, then exits only its acquired Play session, including when later SDK/view setup fails. A managed cleanup exception is logged as an observable error while still allowing that owned exit; no expected-error whitelist is added. The ownership flag remains serialized until ExitPlayMode completes.
- The two original UnityTest lifecycle methods use the view/context acquired by setup. Their duplicate view destruction/creation and additional EnterPlayMode instructions were removed. Native parent visibility, component disable/enable, destruction, play/pause, numeric/text and no-late-event assertions remain; both methods retain their original identities and UnityTest attributes.
- Manual advancement now uses the existing **public per-tween ManualUpdate** on retained admitted handles from this fixture, gated by real owned Play and the active native driver. It skips inactive handles and preserves the original deltas. It does not advance unrelated global manual tweens. No manual clock step occurs before the existing raw ownership count; no global flush, clock reset or default-setting mutation was added.

All **33 SetText + 18 TimeToText = 51** source case identities and parameters remain, including all seven immediate and four superseding variants. Strict retired-inactive, exactly-once kill, exactly-one raw active entry, non-deduplicated acceptance, distinct non-recyclable replacement, exact `9.5`/`16777219`, final payloads and later stable values/counts remain unchanged. Runtime, TimeToText fixture and formatter contracts were not edited. No private SDK flag, EditorPreview callback clearing, production Kill strategy, helper migration, asmdef or GUID change was introduced. Public documentation needs no contract change for this fixture prerequisite correction.

### Source pins and pending native acceptance

| File | SHA-256 |
|---|---|
| `Assets/Neoxider/Tests/Edit/UI/Audit20261023NeoToolsTextSetTextTests.cs` — P6 input | `24c9282604c59b9eebb5ae56dff57860459c3cda49a42a436e8bba56a591d2d4` |
| Same fixture — completed P6 source correction | `f39b658696ed4ff647152650d0c8db26b2c9d56c7e315058409b8a1a90e0f3f8` |
| `Assets/Neoxider/Scripts/Tools/Text/SetText.cs` — unchanged | `c5b01a6069a1d1bdedc32edff1729bb41070b0ddbf9351a81fc30d94f7cf07da` |
| `Assets/Neoxider/Scripts/Tools/Text/TimeToText.cs` — unchanged | `57629403908197309e8d3672de81a6553f0e047ea0c10478a03fdd63d91bbbaf` |
| `Assets/Neoxider/Tests/Edit/UI/Audit20261023NeoToolsTextTimeToTextTests.cs` — unchanged | `26e4712866bc4a1859d2b33e45437aa627668fdd2c9142e1f63dfed6a24a6a0d` |
| SetText fixture `.cs.meta` — unchanged | `b455649a448a5decbc79583411bc3adfd8dd615240ef935170f0eb7976f15a3c` |

Actual validation was read-only XML/source/framework inspection, SHA-256 input bookkeeping and review of the complete fixture delta against frozen P6 input `unity/snapshots/final-20261025T194318Z/source`. The reservation map was checked before every edit; neither owned path was reserved. No Unity, compiler, tests, product checker, process control, install, Git operation or child actor was used. This source review is **not a claim that any of the historical 11 failures is fixed or that revised tests pass**.

**SOURCE-FINAL; all owner writes finished. Native acceptance remains PENDING and E25 remains OPEN.** No implementation/API dependency remains except the new frozen verdict on both SDK hosts. After root/native review and the next global freeze, root must establish actual owned Play/public Init/active-driver admission for the unchanged 51 identities, then observe every retained ownership/kill/endpoint/event/lifecycle oracle. A residual failure after those prerequisites is new evidence for the same-session runtime investigation; no speculative runtime correction is included here. Root owns that execution and any later continuation. Source writes are stopped.
