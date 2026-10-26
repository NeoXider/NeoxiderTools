# Neo.HtmlToUitk.Runtime — implementation receipt

Status: **source-final including NI39; native acceptance awaits the next freeze; earlier dependency handoffs remain recorded below**.

Original actor/session: `neo-audit-1023-b03-08`; model `gpt-6.1-sol`, reasoning `max`. Target: branch `demo`, release `11.0.0`. Resumed 2026-10-24 after the Windows resource interruption, retaining the original audit/plan and their source changes. No replacement actor or child worker was launched.

## Approved actions completed

| Action | Source outcome |
|---|---|
| `HTML-EDITOR-NAVIGATION` | Canonical `Assets/Neoxider/Docs/UI/HtmlToUitk.md` links to the existing `HtmlToUitkEditor.md` authoring/importer topic. |
| `HTML-TEST-NAVIGATION` | The same canonical entry links to the existing `HtmlToUitkTesting.md` testing/evidence topic. Historical counts and receipts were not refreshed or presented as current results. |
| `HTML-ANIMATOR-REPLACEMENT-REENTRY` / native C-36 | A pending animation replacement yields to a newer reentrant request for the same element instance and ordinal animation ID. It cannot append a second run after cancellation dispatch. Added real-panel regression sources for latest-request ownership, unrelated keys and snapshot StopAll. |
| `HTMLRUNTIME-NATIVE-BLUR-AND-DIAGNOSTICS` / source3 | Corrected the blur fixture's typed applied-background getter, strengthened output reuse/retarget/release oracles and routed the three runtime diagnostic sites through the existing shared gate. The `root` parameter is preserved; its contextual scanner correction belongs to primary. Details and native limitations follow below. |
| `NI39` / first Release cleanup oracle | Restored all cleanup assertions immediately after the first Release; the second Release and repeated assertions now prove idempotence separately. Only the owned blur fixture and this receipt changed in this follow-up. |

## Exact implementation files and behavior

1. `Assets/Neoxider/Scripts/UI/HtmlToUitk/Runtime/Animation/UitkAnimator.cs`
   - Added private `_startRequests` bookkeeping: a `ConditionalWeakTable` identifies the exact element instance; its per-element dictionary uses `StringComparer.Ordinal` for animation IDs. Each pending Start registers a unique request token before cancellation callbacks. Token identity is the request generation; no Unity object instance-ID API or new version switch is needed.
   - `Start` checks that token after cancellation dispatch, after the reduced-motion callback and immediately before committing a run. A newer same-key call overwrites the pending token. Its subsequent cleanup cannot revive the older request: an absent or different token rejects that older Start.
   - `finally` removes only the token still belonging to that request and releases empty per-element bookkeeping. Added private `IsCurrentRequest`; that C-36 correction added no public API or assembly reference. The later diagnostic dependency is recorded below.
   - Retained snapshot-based `Stop`/`StopAll`, existing style restoration before `Cancelled`, prepared-animation reuse and per-run scheduling. Another element or animation ID does not invalidate the pending request. No rescan/cancel retry loop was introduced.

2. `Assets/Neoxider/Tests/Edit/HtmlToUitk/Editor/Audit20261023NeoHtmlToUitkRuntimeAnimationTests.cs`
   - Extended only this actor's existing fixture with the three acceptance sources listed below. Existing audit cases and its `.meta` GUID remain intact.
   - Each new case owns an EditorWindow, attaches real VisualElements, yields for panel attachment and disposes its animator/window in `finally`. No `[NonParallelizable]` attribute or shared-state annotation was added.

3. `Assets/Neoxider/Docs/UI/HtmlToUitk.md`
   - Added both owned canonical backlinks.
   - Documented same-key latest-request replacement, the interrupted outer `PlayManual` result, independent keys and live-animator snapshot StopAll semantics for C# consumers of the existing screen/animation workflow.

4. This receipt: `Docs/Audits/2026-10-23/Neo.HtmlToUitk.Runtime.implementation.md`.

The original F1–F10 changes in the seven audited runtime files remain preserved. The original audit and individual plan remain historical evidence; their optional strict metadata compiler/service proposal was not implemented and is not a dependency of C-36.

## Authored acceptance — UNEXECUTED

- `CancellationRestartSupersedesThePendingSameKeyReplacement`: start A; its one-shot cancellation callback starts C for the same key while external replacement B is pending. C is accepted, B returns false, ActiveCount is exactly one and the event sequence is Started(A), Cancelled(A), Started(C). Stopping once cancels C once and restores authored opacity 0.9; stopping again and a later panel update produce no extra event/style write.
- `CancellationStartsForOtherKeysDoNotSupersedeThePendingReplacement`: A's callback starts a scale animation on the same element and fade on another element. The outer fade replacement remains accepted; all three distinct keys stay active. Stopping the first element restores its exact opacity/scale while leaving the second element's fade alive. Stopping the second restores opacity 0.7; exact event ordering is asserted.
- `StopAllDoesNotRevisitAReplacementStartedByItsCancellationCallback`: a one-shot callback starts a new same-key run during StopAll. The new run survives that snapshot, is stopped by the next explicit StopAll, and restores the original opacity with exactly one cancellation per run.

These are three additional declared UnityTest sources, not three passing tests. Prior audit sources likewise remain unexecuted by this worker during implementation. Public test totals must be refreshed only after the real final run.

## Compatibility, reuse and dependencies

The initial 11.0.0 correction changes reentrant replacement that formerly admitted duplicate owners: the superseded pending `PlayManual` now returns false. Ordinary accepted requests, existing public/serialized names and unrelated runs retain their paths. Native compatibility gates in `PanelCompatibility`/`BackdropBlur`, assets, scenes, prefabs and sample layout were not changed. The source3 follow-up changes diagnostic gating and adds one owned runtime assembly reference, as recorded below; no test/shared asmdef was edited.

Reused existing `Running` snapshots, `Finish`/style restoration, `ReducedMotionPolicy` and `PreparedAnim`. The assembly already uses the BCL `ConditionalWeakTable` in `BreakpointController`; the package extension reference exposes no general replacement-request ownership primitive. Bookkeeping is local to pending synchronous starts and adds no per-frame request polling.

Both linked documentation targets existed during source enumeration; their editor/test owners retain their content. No other actor's source, fixtures, public topic or shared tracker was edited. Existing regression metadata was reused, so no new package file/folder metadata is required. The new diagnostic dependency and primary scanner handoff are explicit below.

## Actual work/evidence and remaining debt

Read `IMPLEMENTATION-CONTRACT.md`, this actor's approved implementation request, current animator, owned animation fixture and canonical documentation; continued the complete audit/plan context. Inspected the animator source diff, enumerated the two backlink targets and confirmed `git branch --show-current` reports `demo`. These are source/reference inspections, not product validation.

No Unity/MCP, compiler, test runner, product checker/validator, install, service, commit or push was launched by this actor. No current compile/test/visual/performance pass is claimed. The initial three actions remain source-complete; the source3 follow-up has the precise primary dependency below.

After EVERY writer finishes, the coordinator must compile on the installed Unity 6.3/6.6 hosts, discover and execute the new/previous HtmlToUitk cases, inspect native focus/lifecycle and rendering evidence, and update central receipts/counts only from those results. Other Unity-version branches and native filter/camera-error restoration retain the explicit evidence debt already recorded in the audit. The later root push remains conditional on complete thorough integrated verification.

## Source3 correction receipt — 2026-10-24

Read the updated implementation contract/request, reservation map relevance, current owned runtime/fixture sources, actual diagnostic API/assembly references, native API documentation and the exact graphical XML failures before editing. The reservation map explicitly excludes the HtmlToUitk nested assembly; the edited blur fixture contains only an ordinary IDisposable host, not an embedded MonoBehaviour/ScriptableObject/EditorWindow helper. No helper was moved/extracted, no reserved caller or shared test asmdef was changed.

### Frozen evidence inspected

- `Local/Audits/assembly-2026-10-23/unity/final/s3/u63/Receipts/22-full-edit-graphics.xml`, SHA256 `7c6d24e11012ed5903fff8f24d603e8130ec4bcae262af3761f4cf772a6e57d2`; job identifies Unity **6000.3.14f1**, graphics requested, source snapshot `2d12ae058b989ce064e86a8d7db8223ecb65241d5dcd19babfb2fd7082adfc70`. This is external source3 evidence, not execution of the changes below.
- `FallbackRetargetAndReleaseRestoreBackgroundsAndReleaseTheOutput`: expected the same RenderTexture, actual null; stack ends at `Audit20261023NeoHtmlToUitkRuntimeBlurTests.cs:101`. Its blur suite had two other passing cases and this one failure. The failing read preceded its remaining restoration/release assertions, so those later assertions were not proven by that run.
- `ModulePrinciplesTests.RuntimeModules_DoNotUseRawDebugLogsOutsideDiagnosticGateways`: expected empty; reported owned `UitkAnimator.cs`, `UitkScreen.cs`, `BreakpointWatcher.cs`; stack `ModulePrinciplesTests.cs:128`.
- `ModulePrinciplesTests.RuntimePublicMembers_UsePascalCase`: reported `UitkActionBindings.cs:25`, public member `root`; stack `ModulePrinciplesTests.cs:382`.
- Targeted search of the supplied `21-full-play-graphics.xml` found no HtmlToUitk cases. It supplies no native Play acceptance for these corrections.

### Exact source delta after source3

1. `Assets/Neoxider/Tests/Edit/HtmlToUitk/Editor/Audit20261023NeoHtmlToUitkRuntimeBlurTests.cs`: reads the applied RT through `resolvedStyle.backgroundImage.renderTexture`, preserving exact reference identity. The installed 6.3 scripting reference declares `Background.texture` as Texture2D and `renderTexture` as RenderTexture. Unity's [6.3 inline implementation](https://github.com/Unity-Technologies/UnityCsReference/blob/6000.3/Modules/UIElements/Core/Style/InlineStyleAccess.cs) accepts RT in its setter but omits RT in `GetStyleBackground`; its [resolved getter](https://github.com/Unity-Technologies/UnityCsReference/blob/6000.3/Modules/UIElements/Core/Style/Generated/ResolvedStyleAccess.cs) reads computed background directly. These sources explain the null; the legacy production baseline already assigned `Background.FromRenderTexture(_output)`, just as current production does. No production blur assignment was changed to accommodate the fixture.
   - Added a typed `Background.FromRenderTexture` baseline, retained exact output identity and active-render-target sentinel assertions, and asserted a second Refresh reuses the same Output.
   - Retained both targets' saved image/size comparisons and first output release check; added untouched-second-target checks, first-target restoration after the second render, distinct non-null second output, its release check, and idempotent Release. Existing graphics-device prerequisite is unchanged. NI39 subsequently identified that placing two Release calls before cleanup assertions weakened the first-call cleanup oracle; the correction below restores that ordering. No new declared test case or helper was added.
2. `Assets/Neoxider/Scripts/UI/HtmlToUitk/Runtime/Animation/UitkAnimator.cs`: warning calls use `NeoDiagnostics.LogWarning`; the existing per-animator once-per-message set is updated only when `RuntimeWarningsEnabled`. Enabling warnings after earlier suppression can therefore still emit the warning once. C-36 request ownership remains intact.
3. `Assets/Neoxider/Scripts/UI/HtmlToUitk/Runtime/Navigation/UitkScreen.cs` and `Runtime/Responsive/BreakpointWatcher.cs`: errors use `NeoDiagnostics.LogError` with the existing message/context. Existing catch cleanup and public `LastError` assignment remain independent of the console gate.
4. `Assets/Neoxider/Scripts/UI/HtmlToUitk/Runtime/Neo.HtmlToUitk.Runtime.asmdef`: added the single named reference `Neo.Extensions`, authorized by this root request's minimal-diagnostic-dependency provision. That existing assembly owns the actual gateway and depends on `Neo.PropertyAttribute` (which has no assembly references). No duplicate gateway, force flag, raw-log alias or policy allowlist was introduced. All platform/version/serialization settings and public API signatures are preserved.
5. `Assets/Neoxider/Docs/UI/HtmlToUitk.md`: documented warning/error configuration and defaults, LastError availability, the actual dependency and native RT getter/ownership limitation. Historical audit/plan and public test counts were not rewritten.

### Precise dependency handoff to primary / root

**`Neo.Editor.Tests` b03-04 — constructor-parameter false positive.** `UitkActionBindings.cs:25` is `public UitkActionBindings(VisualElement root = null) { Rebind(root); }`. `root` is a constructor parameter, not a public field/property. In primary's current `CamelCasePublicMemberPattern`, `\S+` consumes `UitkActionBindings(VisualElement`, then matches `root =` as a member. Preserve this signature, including named argument `root`, and correct constructor recognition in the scanner with a focused positive/negative regression (e.g. exclude an opening parenthesis from the type-token match while retaining real lowercase-member detection). No blanket file exception or runtime rename is justified. This actor did not edit the primary-owned scanner or disguise the constructor with formatting changes. Root must route this precise dependency before expecting the global principle case to pass.

**Verification adapters:** the legacy `Verification~/6000.3.14f1[.Tests].csproj` and `6000.6.0f1[.Tests].csproj` glob native runtime sources directly rather than consuming the runtime asmdef. If those adapters are used later, their operator must supply the real Neo.Extensions/Neo.PropertyAttribute assembly dependency (or the explicit gateway source closure) before treating them as current native type-check evidence. Their project configuration belongs to the test/editor coordinator and was not edited here.

### Remaining native capability and acceptance debt

The expanded blur regression and diagnostic changes are **unexecuted**. In the next authorized freeze: compile the real assembly dependency on both installed Unity versions; rerun the exact three blur cases and both principle cases after primary's scanner correction; observe repeated Refresh identity, both background/size restorations, active RT restoration and both resource releases. Check warnings disabled/enabled and once-per-message behavior, and verify disabling error output still leaves Screen/Watcher LastError and cleanup intact. No new native pass is inferred from source inspection or the old XML.

A **pre-existing authored inline RenderTexture** is an additional unverified ownership risk: production captures `style.backgroundImage`, and the 6.3 getter cannot faithfully report that RT. The fixture authors Texture2D backgrounds and therefore does not establish authored-RT restoration. No speculative replacement with resolved capture was made: resolved capture can turn a stylesheet background into a persistent inline override. A separate native authored-RT/stylesheet acceptance and a public-API-safe ownership strategy are required before claiming that capability. Canonical docs advise a separate blur element for an externally owned RT. Native filter rendering, camera-error cleanup, player/performance and other Unity-version branches remain unverified as previously recorded.

### .NET reuse boundary and stopped-writer declaration

Relative to source3, **zero CLI/PureLogic subgraph inputs changed in this actor's correction**: no `Tools/HtmlToUitk` file, `Runtime/Logic`, `Editor/Logic`, PureLogic fixture, consumed Verification project/probe/props file, ancestor build config or glob membership was edited. The existing root reuse receipt selects those exact inputs; native-only logging source, the runtime asmdef, this Editor blur fixture and canonical documentation are outside that declared CLI/PureLogic projection. Root can reuse the earlier results only after independently confirming the full shared projection is still identical. Native source-check/import/test evidence must be refreshed for the changed files and dependency. No CLI/PureLogic/compiler/suite execution occurred here.

Actual follow-up validation consists only of source/API/reference/XML inspection and review of the bounded diff; it is not a product checker or execution result. All substantive earlier patches are preserved. **Owned source writes are finished**; no shared tracker, protected asset, vendor file, other owner's source, install/service, child, commit or push was touched/launched.

## NI39 correction receipt — 2026-10-25

Original actor `neo-audit-1023-b03-08`, `gpt-6.1-sol` / `max`. Read the complete current implementation contract/request and current owned blur fixture/receipt. Accepted the bounded review finding: my previous placement of two Release calls before cleanup assertions could let the second call repair incomplete first-call cleanup. The prior blanket claim of no weakened ownership oracle is corrected above.

In `Assets/Neoxider/Tests/Edit/HtmlToUitk/Editor/Audit20261023NeoHtmlToUitkRuntimeBlurTests.cs`, `FallbackRetargetAndReleaseRestoreBackgroundsAndReleaseTheOutput` now asserts immediately after the **first** Release that the second target's saved image and size are restored, `secondOutput` is released/destroyed, `Output` is null and `IsUsingNative` is false. Only after these five assertions does the second Release run; all five assertions repeat to prove stable idempotent cleanup separately.

All existing output identity/reuse, native typed renderTexture baseline, active-render-target sentinel, first-target restoration/release and other cases remain intact. This follow-up changes only that owned fixture and this receipt; no runtime, public docs, helper, metadata, shared reference/configuration or other owner's file changed. No runtime bug or new framework is implied. CLI/PureLogic subgraph inputs remain unchanged by NI39.

Actual validation: inspected the current source and reviewed the bounded source diff. No Unity, compiler, test, checker, install, service, child, commit or push was launched. The authored first-call and idempotence assertions are **UNEXECUTED**; native acceptance awaits the next root-authorized freeze after every source writer finishes. **NI39 source-final; owned source writes stopped.**
