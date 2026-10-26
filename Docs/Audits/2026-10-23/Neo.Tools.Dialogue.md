# Neo.Tools.Dialogue: audit

Status: source audit and focused fixes complete; Unity compilation and regression execution pending coordinator validation.

- Base revision: `ca198556` (package 10.19.0).
- Model: `gpt-6.1-sol`, reasoning `max`.
- Assembly: `Assets/Neoxider/Scripts/Tools/Dialogue/Neo.Tools.Dialogue.asmdef`.
- Scope: 3 C# files; nested assemblies excluded.

## Scope

Every owned C# file was read completely and reviewed against the current implementation, not just the historical audit. Inventory matches the manifest exactly; there are no nested assemblies in this directory.

| File | Review coverage | Outcome |
|------|-----------------|---------|
| `Assets/Neoxider/Scripts/Tools/Dialogue/DialogueController.cs` | All methods and fields; Awake/Start/disable/destroy, cursor boundaries, event ordering/reentrancy, cancellation source ownership, typewriter integration, auto delays, restarts, editor reflection, public API, logging | Focused fixes below; final 609 lines |
| `Assets/Neoxider/Scripts/Tools/Dialogue/DialogueData.cs` | Entire serializable Dialogue/Monolog/Sentence graph, arrays, UnityEvents, sprite/text payloads, null values, scene references | No source changes required; 43 lines |
| `Assets/Neoxider/Scripts/Tools/Dialogue/DialogueUI.cs` | Entire optional-view API, TMP font/material validation, shared fallback table mutation, name cache, portrait retention/clearing, Reset behavior | Name-label refresh fixed; final 213 lines |

Read as context only: `TypewriterEffect.cs`, `NeoDiagnostics.cs`, existing Dialogue Edit/Play tests, typewriter regression tests, test/runtime asmdefs, the dialogue editor's controller/event integration points, and the historical `Local/Audits/modules-2026-10-12-4ca6f50f/Tools/Dialogue.md`. No dependency, existing test, editor, asmdef, asset, scene, prefab, or shared configuration was edited.

Reuse receipt: consulted `neoxider-tools/SKILL.md` and dialogue/typewriter/delay entries in `references/tools.md`, `references/extensions.md`, and relevant module references before adding private forwarding methods. Adapted the existing controller and `TypewriterEffect.Complete()`; used the already-installed UniTask `NextFrame(CancellationToken, bool)` overload. No parallel helper framework or new package dependency.

## Findings and fixes

Findings below are established by concrete source/control-flow traces. Their authored behavioral regressions have **not** been executed in Unity during this worker task.

### DLG-01 — P2: cancelled typing resurrects automatic playback

- Trigger: enable `autoNextSentence`, start a slow line, then disable the object, replace the dialogue, or complete typing manually.
- Before: `TypewriterEffect.PlayAsync` catches cancellation internally. `StartTypewriterAsync` therefore resumes normally and unconditionally calls `ScheduleAutoDelay`, even after `CancelAll` has cancelled that run. It can move a disabled controller, advance a replacement line, or overwrite the delay explicitly scheduled by skip completion.
- Source: `DialogueController.StartTypewriterAsync` (now lines 519–548); dependency cancellation handling is in `TypewriterEffect.PlayAsync`.
- Fix: retain the run's own token, check it after awaiting, and dispose only the cancellation source still owned by that run. Auto-delay callbacks also check their token immediately before invoking navigation.
- Regressions: `DisableWhileTyping_DoesNotResurrectAutomaticPlayback`, `RestartWhileTyping_DoesNotAdvanceReplacementFromCancelledRun`, `CompleteTypewriter_CompletesEngineAndKeepsItsScheduledDelay`.

### DLG-02 — P2: zero automatic delays recursively consume playback

- Trigger: immediate text presentation with automatic progression and a zero delay, which is permitted by the Inspector.
- Before: `AutoDelayAsync` takes no await, invokes `Advance` synchronously, and re-enters presentation/scheduling. A short dialogue finishes before `StartDialogue` returns; stack depth grows with a long chain, and presentation notifications unwind after later lines have already been consumed.
- Source: `DialogueController.AutoDelayAsync` (line 558).
- Fix: zero/negative automatic delays await `UniTask.NextFrame`. Positive delays retain scaled-time behavior. Cancellation remains effective before the queued transition.
- Regressions: `ZeroDelay_AutomaticTransitionsYieldAndFinishInOrder` checks immediate state, all sentence presentations, boundary traversal and exactly one final completion; `DisableWithZeroDelayPending_CancelsScheduledTransition` checks cancellation.
- Compatibility: zero-delay automatic navigation is deliberately deferred by one frame. Public signatures and serialized settings are unchanged.

### DLG-03 — P2: old event callbacks overwrite replacement playback

- Trigger: a data presentation callback or a monolog/dialogue boundary listener calls `StartDialogue(1)`; alternatively, `OnAllDialoguesEnd` restarts the controller.
- Before: the interrupted stack continues with old local data and new cursor indices, producing stale/duplicate presentation or ending the replacement dialogue. Both final-completion paths set `DialogueStarted = false` after invoking listeners, overwriting a listener's restart.
- Sources: `ShowCurrentSentence` (line 415), `GoToNextMonolog` (244), `GoToNextDialogue` (271), and final-dialogue overshoot handling.
- Fix: navigation/lifecycle cancellation invalidates a playback version; presentation/boundary code stops after a callback changes that version. Completion sets the stopped state before notifying listeners.
- Regressions: four `PresentationCallback_StartsReplacement_AbandonsOldPresentation` cases, two `EndCallback_StartsReplacement_DoesNotFinishReplacement` cases, and both completion paths in `AllDialoguesEnd_RestartsPlayback_RetainsStartedState`.
- Boundary: callers must use controller methods to replace the selection. Arbitrarily rewriting public arrays inside callbacks is still outside this guard's contract.

### DLG-04 — P2: Advance leaves final-line typing active after completion

- Trigger: call `Advance()` during typing of the final sentence.
- Before: only the auto delay is cancelled. Boundary completion sets `DialogueStarted` false while the typewriter remains active and can continue emitting text/progress.
- Source: `DialogueController.Advance` (line 218).
- Fix: cancel all current playback work before navigation, including when there is no next line.
- Regression: `AdvanceDuringFinalSentence_StopsTypewriterBeforeFinishing` asserts stopped playback/effect and one final notification.

### DLG-05 — P2: skip completion leaves the exposed effect incomplete

- Trigger: call `CompleteTypewriter()` while part of a line is visible.
- Before: the UI receives full text and the controller emits progress 1, but `Typewriter.CurrentText` and `Typewriter.Progress` remain partial; native `Typewriter.OnComplete` is omitted.
- Source: `DialogueController.CompleteTypewriter` (line 301).
- Fix: use the existing `TypewriterEffect.Complete()` API. Its forwarded progress and completion notifications now agree with its public state. A version check prevents completion callbacks from overwriting replacement presentation.
- Regression: `CompleteTypewriter_CompletesEngineAndKeepsItsScheduledDelay` checks full engine text, progress 1, one completion-progress notification, and the preserved scheduled delay.
- Compatibility: native effect completion listeners now also receive manual completion, matching the effect's own API. The controller's historical `OnSentenceEnd` timing is preserved.

### DLG-06 — P2: unsuccessful start retains a stale started flag

- Trigger: start a valid dialogue, replace `dialogues` with null/an empty array, and call `StartDialogue()` again.
- Before: work is cancelled but `DialogueStarted` remains true, although getters return no dialogue.
- Source: `DialogueController.StartDialogue` (line 134).
- Fix: clear the started flag before validating top-level data. Disable/re-enable retains its existing separate cursor-preservation behavior.
- Regressions: null and empty-array cases of `StartDialogue_WithMissingData_ClearsPreviousStartedState`.

### DLG-07 — P2: cached names suppress necessary label refresh

- Trigger: repeat the current name after rebinding/external label changes, or present an empty initial speaker name over a non-empty label.
- Before: the cache early return skips assigning the label, leaving visible stale text.
- Source: `DialogueUI.SetCharacterName` (line 143).
- Fix: refresh a valid assigned label before deduplicating `OnCharacterChange`; event behavior stays unchanged.
- Regressions: empty and named cases of `SetCharacterName_WithCachedName_RefreshesLabelWithoutDuplicateEvent` assert actual TMP text and event count.

### DLG-08 — P2: retained effect continues notifying a destroyed controller

- Trigger: a C# consumer retains the public `Typewriter` reference after destroying the controller and calls `Complete()`.
- Before: anonymous subscriptions remain on that effect, retain the controller/listener graph, and forward progress to its UnityEvent after destruction.
- Source: `DialogueController.Awake` / `OnDestroy` (lines 69 / 100).
- Fix: named forwarding subscriptions are explicitly removed on destruction; disabling still retains them for reactivation.
- Regression: `DestroyController_DetachesNotificationsFromRetainedTypewriter` checks the observable notification count after destruction.

### DLG-09 — P3: runtime Inspector actions and explicit typing

The seven playback Inspector buttons previously permitted state mutation/async work from Edit Mode. They now use the existing `ButtonAttribute.PlayModeOnly` option. The editor-window authoring action stays available. Replaced the one non-anonymous `var` with `Type` in the editor reflection path. No public method was removed or renamed; these small authoring/style fixes do not justify separate behavior tests.

### Reviewed contracts retained

- Data remains plain serializable classes authored on the MonoBehaviour, not a ScriptableObject graph carrying scene references. All public declarations and `[SerializeField]` declarations match baseline.
- Null-safe getters, optional UI, null text handling, portrait retention/clearing, restart permissions, and local component lookup remain intact. Null entries still produce gated warnings and require authoring correction/manual navigation; empty arrays can cross boundaries.
- Logs use `NeoDiagnostics` gates. No direct `Debug.Log` calls or Unity instance/entity identity APIs occur in the owned source. No new Unity-version branches were added.
- Runtime assembly references remain unchanged. Editor-window lookup stays reflection-based under `UNITY_EDITOR`; editor implementation belongs to `Neo.Editor`.

## Documentation

Updated the four existing owned public guides, preserving English and the single canonical entry:

- [README](../../../Assets/Neoxider/Docs/Tools/Dialogue/README.md): composition, C# example, scene/button setup, dependency guidance and playback contracts; removed the incorrect data-asset description.
- [DialogueController](../../../Assets/Neoxider/Docs/Tools/Dialogue/DialogueController.md): actual API behavior, boundaries, cancellation/lifetime, scaled versus typewriter time, zero-delay change, event timing and callback navigation.
- [DialogueData](../../../Assets/Neoxider/Docs/Tools/Dialogue/DialogueData.md): repeated container notifications, public graph mutation boundary, inline scene authoring and optional content.
- [DialogueUI](../../../Assets/Neoxider/Docs/Tools/Dialogue/DialogueUI.md): runtime setters, cached names, Reset/portrait behavior, font validation and its existing shared-asset side effect.

Кратко: исправлены отмена и автопереходы, повторный вход из событий, завершение печати и обновление имени. Существующий момент вызова `OnSentenceEnd` сохранён и описан явно; это уведомление о показе строки, а не об окончании печати.

## Substantial improvement proposal

### Proposed, not implemented: iterative playback core with explicit presentation/completion events

**Problem.** Navigation, UnityEvents, view updates and asynchronous scheduling currently share one MonoBehaviour stack. This makes reentrant callbacks and cancellation difficult to reason about; automatic zero-delay chains now yield, but very long runs of empty containers with immediate boundary traversal still use recursive navigation. There is no complete-graph validation, and `OnSentenceEnd` has an established but misleading presentation meaning.

**Architecture / public API.** Introduce a plain C# `DialogueSession` that consumes an immutable validated `DialogueScript` made of text/speaker/presentation identifiers, without Unity objects or UnityEvents. Expose `DialogueCursor Cursor`, `DialoguePlaybackState State`, `Start(DialogueScript, DialogueCursor)`, `Advance()` and `Stop()`. Return a `DialogueStep` containing line/boundary/completion results; walk empty containers iteratively. The controller maps the existing serialized arrays and sprite references into the script/presentation lookup, applies steps to an optional `IDialogueView`, and dispatches the existing UnityEvents. Keep UniTask/typewriter scheduling in that adapter with one owned session cancellation token. Expose separate typed `SentencePresented` and `SentenceCompleted` notifications; a caller-driven core needs no Unity player loop for navigation tests.

**Compatibility / migration.** Retain every current serialized field and public controller method. Build the script on `StartDialogue` so existing scenes and inline C# graph construction continue working; avoid a mandatory asset migration. Preserve legacy UnityEvent timing, including `OnSentenceEnd`, while documenting and offering the explicit new events. Add graph-validation results with cursor locations for null entries and missing containers; make strict rejection opt-in rather than silently breaking existing sparse graphs. DialogueUI is the default view adapter, with alternate views optional.

**Acceptance scenarios.** Start at arbitrary valid/non-valid cursors; traverse at least 10,000 empty containers without recursive growth; present every valid line once in order; verify manual/automatic/skip completion separately; restart from every boundary notification; cancel during typing/delay/disable/destroy; run with no view and two custom views; preserve serialized scenes and legacy event order; independently configure scaled delay and typing time. Core navigation tests should use plain C# state/output assertions, with a small Unity adapter suite for real lifecycle and TMP behavior.

**Effort / risk.** Approximately 4–6 engineering days plus review and Unity matrix validation, assuming the existing linear graph remains the supported data model. Highest risks are legacy callback ordering, mutable arrays after session creation, and confusing partial UI completion with actual navigation. Stage the work as core + regression suite first, then opt-in adapter integration before switching existing playback.

**Additional scoped follow-up.** `DialogueUI.ValidateFallbackFonts` currently edits the shared TMP font asset's fallback list. Split read-only validation into a structured report and an explicit editor repair action, or use carefully owned runtime clones when sanitation is essential. Preserve the existing validation behavior until the owner chooses the migration; this audit documents the side effect and does not rewrite configured font assets.

## Verification and remaining risks

**Executed checks (file-only):**

1. Read `AGENTS.md`, the complete worker contract, exact manifest entry and this worker report. Checked relevant ancestor paths for additional instructions; none were present. Compared actual owned-directory C# inventory with all three manifest entries.
2. Read-only baseline comparison against `ca198556`: public declaration lines and serialized-field declarations are identical for all three source files. All source/test declarations use explicit types. Verified the installed UniTask `NextFrame` overload and `ButtonAttribute.PlayModeOnly` property directly in dependency source.
3. Python static consistency checks passed: strict UTF-8 decoding without replacement characters, balanced C# delimiters after excluding strings/comments, existing relative links in all four public guides, correct new regression filename prefix, and globally unique GUIDs for both new `.cs.meta` files.
4. `git diff --check -- <owned source/docs/report/new tests and metadata>` passed with no output. This check does not inspect untracked-file content, so the new tests/metadata were also checked directly for whitespace/encoding consistency.
5. Authored **12 Edit Mode cases and 7 Play Mode cases**, all in two new files under the existing Dialogue test directories, plus their unique metadata. No existing tests or test asmdefs were changed. The tests assert cursor/text/progress/event output, rather than source text or no-exception behavior.

New regression files:

- [Audit20261023NeoToolsDialogueEditModeTests.cs](../../../Assets/Neoxider/Tests/Edit/Dialogue/Audit20261023NeoToolsDialogueEditModeTests.cs)
- [Audit20261023NeoToolsDialoguePlayModeTests.cs](../../../Assets/Neoxider/Tests/Play/Dialogue/Audit20261023NeoToolsDialoguePlayModeTests.cs)

Final reviewed source SHA-256 values:

| File | SHA-256 |
|------|---------|
| DialogueController.cs | `1e3bc4bc1affd1178fc8a3f43ef27809ffb064276ca1ecddf6d36c0b94b51d91` |
| DialogueData.cs | `676e25d49420d8d022a0f2fcffd002afd23178b31f5e4ca0f0e5798ebfd385d9` |
| DialogueUI.cs | `f2ad9a1e5878fe8677b23d1407471233495cfa188c191643ba3dd5c54d28608a` |

**Not executed:** Unity launch/import/compilation, Edit/Play test runners, player builds, visual QA, version-matrix runs or performance measurements. These were explicitly prohibited for workers. Static checks are not compilation or a test pass.

**Coordinator actions / residual risks:** run the two new test classes and existing Dialogue/typewriter suites in the central Unity matrix (6.0–6.4 and 6.5+). TMP label regressions require the project's default font/material, following existing TMP test fixtures. No additional assembly references were identified. Confirm the intentional one-frame zero-delay behavior and newly consistent native skip-completion event with representative scenes. Shared-font sanitation, large sparse graph traversal and direct runtime mutation of public graph data remain documented follow-ups.

**Cross-boundary handoff to `Neo.Tools.Components`:** the inspected `TypewriterEffect.PlayAsync` invokes user callbacks while continuing to use mutable shared run state. In particular, an `OnCharacterTyped` listener can restart the effect before the old loop dispatches progress/audio and reaches its next cancellation check; `Complete()` invokes progress listeners before its completion notification, so a listener can replace the run between those notifications. The dependency owner should audit callback reentrancy with real output/event assertions and recheck the latest concurrent source. No dependency edits were made here.
