# Neo.NoCode: implementation receipt

Status: **source-final**, including the 2026-10-24 I5-02 correction and 2026-10-25 source3 caller correction. Native closure remains deferred. Target: `demo` / `11.0.0`. Original actor: `neo-audit-1023-b04-06`; model `gpt-6.1-sol`, reasoning `max`.

Read `IMPLEMENTATION-CONTRACT.md`, the dedicated implementation request, manifest entry, original audit/plan and relevant current source. Earlier coherent audit patches are preserved. The [audit](./Neo.NoCode.md) and [plan](./Neo.NoCode.plan.md) remain historical evidence and were not rewritten. The optional typed-source/session framework remains a proposal; it was not implemented or required for these corrections.

## Completed approved actions

### EDITOR-COMPONENT-INSTANCE

Changed `Assets/Neoxider/Scripts/NoCode/ComponentFloatBinding.cs`.

Additive API published for the Editor owner:

```csharp
public int ComponentOccurrence { get; set; }
```

Serialized backing field: **`_componentOccurrence`**, type `int`, default **0**. Its property setter invalidates cached resolution without clamping the value. `EnsureCache` rejects negative occurrences with an explicit error. Resolution counts only components matching the existing full-name/name type predicate, in `GameObject.GetComponents<Component>()` order. Unrelated types and missing-script entries do not count. Missing requested occurrences fail; no fallback selects occurrence zero. Missing-component diagnostics now include the requested occurrence.

`TryReadRaw`, `TryReadFloat` and reactive accessors use the selected component through the existing cache. A selected component being destroyed invalidates its cache and rechecks the requested occurrence. Existing serialization without the new field retains occurrence zero. The new selector does not read or reinterpret any legacy Editor `ComponentIndex`/`OtherComponentIndex` value.

The cache retains its existing explicit invalidation model: after reordering/removing other components while the cached selected component remains alive, invalidate and refresh to resolve against the new composition. No per-read component-list scan or global scene watchdog was added.

### NOCODE-VALIDATE-FALLBACK

Changed:

- `Assets/Neoxider/Scripts/NoCode/NoCodeFloatBindingBehaviour.cs`
- `Assets/Neoxider/Scripts/NoCode/NoCodeFormattedText.cs`

Both wrappers now mark a nonserialized `volatile` dirty flag in `OnValidate`, alongside the existing interval clamp. Validation no longer invalidates source caches or reads bound getters. Dirty configuration is invalidated/reconciled on the main-thread refresh path and before `LateUpdate` evaluates `shouldPoll`. Enable-time initialization also clears dirty state and invalidates the appropriate descriptors.

Reactive plain/unresolved sources therefore keep or regain polling after Inspector changes. Reconciliation removes obsolete listeners even when the new mode is Once. Once does not gain a periodic read or repaint. `Poll In Late Update = false` still prevents periodic UI refreshes; explicit manual refresh remains available. Dirty reconciliation can resolve/reconnect a reactive source once even when periodic polling is disabled.

### NOCODE-REENTRANT-REWIRE

Changed `Assets/Neoxider/Scripts/NoCode/NoCodeFloatBindingBehaviour.cs`.

After `ApplyFloat`, the wrapper uses the **latest** `TrySubscribeReactive()` result rather than OR-ing it with the pre-callback result. A Slider callback changing the binding to an ordinary member releases the old listener and enables fallback for the current binding. The fallback helper also checks `isActiveAndEnabled`, preserving disabled/manual-refresh behavior.

### I5-02 / P2 — preserve formatted notification baseline during validation

Read the complete `NativeImplementationReview-5.md` and root's appended correction handoff. Changed only the owned `NoCodeFormattedText.cs` runtime for this correction; previous occurrence/getter/polling/reentrant corrections remain.

`ReconcileDirtyConfiguration` preserves `_lastText`, which remains the semantic comparison baseline. A separate private, nonserialized `_forceRender` marks the pending redraw. `RefreshFromSource` writes when text changes, a target is newly resolved, or a redraw is pending; only a changed string enables `OnTextChanged`. The pending flag is consumed before external display/event callbacks. Interval-only validation and reassignment to a new target can therefore write the same text with zero extra semantic events, while later changed source/format output notifies once.

`OnEnable` retains its explicit `_lastText = null` reset and clears pending-render state, preserving one initial notification per enable. Once remains nonpolling and keeps the redraw pending for an explicit refresh/re-enable. Validation still does not call bound getters; dirty processing still precedes the existing polling gate. No public/serialized API or shared/other-owner file changed for I5-02.

## Documentation completed

Updated the existing owned guides:

- `Assets/Neoxider/Docs/NoCode/README.md`: occurrence selection, runtime setter contract, validation/reconciliation, callback rewiring and render-versus-notification behavior, including enable-time publication.
- `Assets/Neoxider/Docs/NoCode/ComponentFloatBinding.md`: serialized/default/error semantics, C# selection of a second AudioSource, legacy-index separation and cache invalidation after composition changes.
- `Assets/Neoxider/Docs/NoCode/NoCodeFloatBindingBehaviour.md`: source configuration, deferred validation, Once/polling gates and final-binding fallback.
- `Assets/Neoxider/Docs/NoCode/GettingStarted.md`: matching-type occurrence, Inspector settings and unchanged-text target reassignment/event workflow for scene users.

Existing canonical guide backlinks and release/developer/imported sample-layout explanations remain. No sample asset, layout, package version or shared release documentation was changed.

## Authored acceptance sources — UNEXECUTED

### New: `Assets/Neoxider/Tests/Edit/NoCode/Audit20261023NeoNoCodeFollowupTests.cs`

**21 declared cases** (15 original plus 6 for I5-02):

- Both text wrappers continue updating ordinary values after validation in Reactive and Poll modes. Formatted cases now count zero events for an unchanged interval-only refresh, then exactly one after a later changed value.
- Validation does not invoke a counted source getter; the main-thread poll does and displays the new value.
- Once preserves its previous text/getter count after validation and due `LateUpdate` calls.
- Disabled interval polling leaves text unchanged until an explicit refresh.
- Inspector reactive-to-plain reconfiguration releases the old listener before the polling gate and displays the plain value.
- Switching to Once releases its subscription without repainting or receiving later pushes.
- A real Slider value callback rewires `SetProgress` to a plain member: old listener count becomes zero, and a later due poll reaches normalized value 0.75.
- New target-reassignment cases in Reactive and Poll modes write unchanged text to the new target with zero events, leave the old target unchanged, and deliver exactly one event for a later new value.
- New format-validation cases in both modes keep equivalent output silent; a different formatted result emits once, repeated refresh stays silent, and a later changed value is a positive notification control.
- New Once target-reassignment case retains the getter/polling assertions, renders unchanged text only on explicit refresh without an event, then notifies once for a changed value.
- New explicit enable-hook case preserves one initial publication and resumes ordinary change detection. This uses the existing reflection lifecycle seam and is not native enable/disable execution evidence.

Existing getter/polling/Once tests now include notification counts and later/manual changed-value positive controls; no earlier public-output assertions were removed.

The EditMode deadline/lifecycle seam uses reflection solely to invoke validation/actual `LateUpdate` and make its deadline due; assertions observe TMP text, getter counts, subscription counts and Slider output. These are not claims of real-time PlayMode scheduling verification.

### New: `Assets/Neoxider/Tests/Edit/NoCode/Audit20261023NeoNoCodeOccurrenceTests.cs`

**11 declared cases**:

- Occurrences 0/1/2 read 10/20/30 despite an intervening unrelated component.
- Negative, out-of-range and `Int32.MaxValue` selectors fail after a previously successful read; invalid values are not clamped to zero.
- A missing second instance becomes readable after it is added.
- Destroying the selected instance does not fall back to the first.
- A legacy descriptor missing the field defaults to the first match; a new JsonUtility roundtrip preserves occurrence one.
- Changing the occurrence on a live text binding transfers the reactive subscription to the selected instance and updates actual text.

Both new files retain their generated unique-GUID `.meta` sources. The existing owned `Audit20261023NeoNoCodeBindingTests.cs` retains 20 cases; its late/destroyed-target cases now also check a later reactive value publishes exactly one event. Total declared NoCode audit cases: **52**, with no discovery or execution claim. I5-02 added no files or metadata.

## Compatibility and ownership

Only one serialized field was added across the assigned implementation; existing serialized fields, public APIs, enum values, target priority, default/minimum polling interval, invariant formatting and missing-source display behavior are preserved. There is no automatic asset migration and no reassignment of old flat UI indices. The deliberate behavior changes are explicit occurrence selection/rejection, the two approved polling corrections, and suppression of duplicate unchanged-string validation events while preserving enable-time publication.

Runtime reuse remains `BindingSourceGameObjectResolver`/`ReflectionCache` from `Neo.Condition`, typed properties from `Neo.Reactive`, existing text APIs and gated `NeoDiagnostics`. No new service, dependency, asmdef, version switch or scene setup was introduced.

**Cross-owner handoff:** `Neo.Editor` owns `Assets/Neoxider/Editor/NoCode/ComponentFloatBindingDrawer.cs`. It can now bind its additive occurrence control to `_componentOccurrence`/`ComponentOccurrence`; it must preserve the legacy list-index fields and reject invalid requested occurrences rather than clamp/reinterpret them. This runtime API is available now. Condition's separate occurrence APIs remain that owner's work; NoCode does not depend on those new APIs. No runtime source dependency blocks this receipt.

No other owner's runtime, tests, docs or shared tracker/configuration was edited. `NoCodeBindText.cs` retains its earlier audit correction; no additional change to it was needed in this source phase.

## Actual inspection and remaining evidence debt

- Read current source and manually inspected the complete scoped runtime diff and authored regression sources.
- Confirmed current branch `demo` through a read-only Git query.
- Scoped `git diff --check` completed with exit code 0. This is a whitespace/diff inspection, not compilation or a product-validation result; untracked new files are outside Git's ordinary tracked diff coverage.
- Generated the two new test GUIDs as artifact bookkeeping. Package-wide GUID/encoding/product checkers were not run.
- For I5-02, read the complete native review and current affected runtime/tests/receipt/docs, then manually inspected the correction. The review's earlier hashes/verdict identify the pre-correction snapshot; root needs a fresh review of this delta before freezing source.

**UNEXECUTED:** compilation, imports, test discovery, all new/existing EditMode cases, actual PlayMode cadence/lifecycle/UI behavior, JsonUtility/serialized asset acceptance and player/IL2CPP stripping. Historical static checks in the audit are not new-source verification receipts.

After every writer finishes, root should compile the frozen dependency closure in installed Unity 6.3 and 6.6, execute these three audit fixtures plus the existing NoCode legacy fixtures, and verify Inspector occurrence authoring after the Editor handoff. Real-time smoke should include unscaled polling with `timeScale = 0`, disable/re-enable/parent deactivation and no new runtime errors. No distributed Unity-version `#if` was added; any necessary version compatibility remains centralized.

The phase-2 proposed PlayMode fixture would require root-coordinated `Neo.NoCode` reference addition to `Neo.Tests.Play.asmdef`; no uncompilable PlayMode source or shared asmdef change was added here. Root can instead use its approved native verification host/driver for the scheduling smoke.

No Unity, compiler, test runner, product checker, install, service, commit/push or child agent was launched during this source phase. All assigned owned source/docs/regression work is complete; central execution and Editor integration remain outside this worker's source ownership.

## NOCODE-POST-RELOCATION-LIFECYCLE — source3 E59/E60, 2026-10-25

Read the complete updated implementation contract/request, current caller and relevant runtime, relocated `Tests/Fixtures/NoCode/Audit20261023NeoNoCodeSource.cs`, the primary's NoCode mapping, and actual source3 XML. The helper retains its original namespace/type and is now runtime-compatible. Primary-owned fixture sources, GUID/type mapping, asmdefs and references were not edited. This delta changes only `Assets/Neoxider/Tests/Edit/NoCode/Audit20261023NeoNoCodeBindingTests.cs` and this receipt; prior source/docs/regressions and the helper extraction are preserved.

Historical evidence: `Local/Audits/assembly-2026-10-23/unity/final/s3/u63/Receipts/22-full-edit-graphics.xml` records **10 passed / 10 failed** cases in this 20-case fixture. Nine formatted cases fail on `SendMessage OnValidate has no receiver!`; their stacks enter `WireFormattedText` at snapshot line 416. The Inspector target case fails on `ShouldRunBehaviour()` with its stack explicitly entering `Component.SendMessage` at snapshot line 197. These are pre-correction results, not execution of the current source.

The caller now uses genuine serialized authoring through `SerializedObject.ApplyModifiedPropertiesWithoutUndo`, asserting that both the target edit and each source/format edit were committed. Unity's serialized validation path supplies validation; the fixture no longer dispatches lifecycle messages manually. Subsequent assertions exercise the existing public refresh/reactive contracts. No `DontRequireReceiver`, expected-error allowance, reflection lifecycle substitute, manual Update, ExecuteAlways, production API, friend access or serialized-setting workaround was added.

The target-reassignment case additionally seeds the replacement with `waiting`, explicitly refreshes the unchanged value, and requires **new target `0.25` / old target `0.25`** before retaining the original later-value **new target `0.75` / old target `0.25`** checks. A missing validation/cache invalidation therefore remains observable. All original target recovery, source/type rewiring, format, exact listener counts, disabled/in-flight guards, duplicate notification counts, late-target same-text writes and later changed-value assertions remain. No test or parameterized case was removed: this caller still declares 20 cases. Getter/polling/Once/format/reentry regressions in the other owned callers were left intact.

Coverage is stated in the caller: serialized Inspector authoring plus explicit public C# refresh/reactive contracts in Edit Mode. Toggling `enabled` exercises the public refresh/notification guard; it does not establish native `OnDisable` scheduling. This correction makes no new native enable/disable, PlayerLoop or physics acceptance claim. The earlier followup fixture's documented reflection seams remain separate evidence debt; this bounded release does not rewrite them or claim they prove native lifecycle behavior.

**Actual inspection:** parsed the existing result XML to read this fixture's case results/messages/output and the Inspector failure stack. Read the caller diff against immutable `final-20261024T002340Z/source`: apart from the primary's helper extraction, it contains only the coverage comments, the two serialization changes and added target assertions; every earlier substantive oracle remains. Both edited files are untracked, so ordinary `git diff` does not inspect them; the explicit `git diff --no-index` comparison returned 1 for the displayed differences, not a test failure. No test, compiler, Unity process or product checker was executed. Source3 results do not close this correction.

**Deferred closure:** after all writers finish and root issues a new freeze, compile the frozen runtime-helper/Editor-caller dependency closure and rerun all 20 cases in this fixture on the admitted native hosts. Require every existing exact oracle and the new unchanged-value target checks, actual serialized edit application, and no unexpected `OnValidate` receiver or `ShouldRunBehaviour` errors. Root must separately retain the previously required getter/polling/Once/format/listener/reentry and real lifecycle acceptance from the other fixtures/native smoke; no exclusions or helper/API changes are needed for this source handoff. Proposed correction remains **UNEXECUTED** pending that native run.
