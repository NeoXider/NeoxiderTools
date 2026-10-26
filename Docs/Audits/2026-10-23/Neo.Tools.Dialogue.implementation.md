# Neo.Tools.Dialogue — implementation receipt

Target: `demo` / `11.0.0`. Original actor/session: `neo-audit-1023-b06-07`.

Status: **SOURCE-FINAL** for assigned actions. Authored acceptance remains **UNEXECUTED** behind the global source-writer barrier.

## API publication and dependencies

- Additive public runtime APIs: **none**. Existing serialized fields and public controller/data/UI signatures remain the integration contract.
- No new dependency API, asmdef reference, package, scene setup or asset migration is required by `DIALOGUE-SPARSE-RECURSION`.
- Existing `Neo.Tools.Components.TypewriterEffect` APIs are reused. Its current callback/cancellation guards supersede the historical audit handoff; dependent execution remains behind the global source-writer barrier.

## Assigned actions

- `DIALOGUE-SPARSE-RECURSION` — **implemented**. Empty-container traversal now uses a loop in `ShowCurrentSentence`. Private `TryMoveToNextMonolog()` / `TryMoveToNextDialogue()` return true only for an immediate cursor move; scheduling, final completion and callback-invalidated work return false. `Advance()` presents only after a successful immediate move. Internal traversal no longer calls public `NextMonolog/NextDialogue` recursively. Immediate transitions retain cancellation/version invalidation and the same index resets.
- `DIALOGUE-SHARED-FONT-MUTATION` — **deferred policy respected; documentation complete**. `DialogueUI.ValidateFallbackFonts` and its borrowed-asset cleanup behavior are preserved as instructed. The public UI guide explicitly describes its existing shared `TMP_FontAsset` mutation and the fact that 11.0.0 retains it. No new inspection API, clone, editor repair framework or asset rewrite was introduced.

## Exact files in this implementation phase

- `Assets/Neoxider/Scripts/Tools/Dialogue/DialogueController.cs`: bounded-stack traversal and private movement helpers; retains all coherent phase-1 cancellation/event/completion fixes.
- `Assets/Neoxider/Tests/Edit/Dialogue/Audit20261023NeoToolsDialogueTraversalTests.cs`: new focused fixture, plus its unique `.cs.meta` (`8c4e7e71b53840dfaa27cdfdb9a8f1ab`).
- `Assets/Neoxider/Tests/Play/Dialogue/Audit20261023NeoToolsDialoguePlayModeTests.cs`: six additional acceptance cases, retaining its seven prior cases and cleanup.
- `Assets/Neoxider/Docs/Tools/Dialogue/README.md`: canonical entry explains synchronous sparse traversal, delayed exits and null-entry behavior.
- `Assets/Neoxider/Docs/Tools/Dialogue/DialogueController.md`: C#/scene navigation contracts, bounded traversal, boundary notifications, delayed exits and callback invalidation.
- `Assets/Neoxider/Docs/Tools/Dialogue/DialogueData.md`: missing child arrays versus null elements and repeated container-event timing.
- `Assets/Neoxider/Docs/Tools/Dialogue/DialogueUI.md`: explicit preserved shared-font policy.
- This receipt: `Docs/Audits/2026-10-23/Neo.Tools.Dialogue.implementation.md`.

`DialogueData.cs`, `DialogueUI.cs`, the existing EditMode fixture, shared asmdefs/configuration, all other owners' files, and the historical audit/plan were not changed in this implementation phase. Earlier audit edits remain present in the shared worktree.

## Behavior and compatibility

No public or serialized declarations changed. Existing synchronous public calls remain synchronous for immediate empty-container transitions; no arbitrary item limit truncates valid traversal. Container data events, `OnMonologEnd`, `OnDialogueEnd` and final completion retain their order and pre-transition cursor arguments. `DialogueStarted` is stopped before final listeners. Existing `OnSentenceEnd` presentation timing is retained.

Every immediate internal move advances a dialogue/monolog cursor and re-enters the loop with its current playback version. A listener that navigates/restarts or disables invalidates the interrupted step; the old traversal returns without stale presentation. An enabled auto boundary flag schedules the existing delay and exits immediately, including the existing next-frame behavior for a zero delay. Missing/empty child arrays progress; null dialogue/monolog/sentence entries still warn and stop presentation. No snapshot semantics or graph migration was added.

The correction bounds internal traversal stack depth, not synchronous CPU time across an arbitrarily large graph or user-authored recursively invoking callbacks. No new frame-budget promise is made. The optional session/view/font frameworks remain proposals in the original plan; no sample layout, scene, prefab or package version setting changed here.

## Authored regression source — UNEXECUTED

New EditMode fixture: **10 cases**:

- Two 10,000-container paths (empty dialogues / empty monologs) alternate null and empty child arrays, reach the exact tail sentence synchronously, present once and assert every eligible boundary count.
- Two entirely empty 10,000-container paths assert stopped state, no sentence presentation and exactly one final completion, including a later no-op `Advance()`.
- Two boundary-callback restarts jump to the tail after 25 boundaries and assert the old path cannot continue or duplicate presentation.
- One complete ordered data/boundary/character/presentation event trace crosses both an empty dialogue and an empty monolog, then completes the valid line.
- Three null-entry cases (dialogue, monolog, sentence) assert the cursor stops at that entry instead of silently skipping to a later valid line.

Additional PlayMode source: **6 cases**:

- Empty dialogue and monolog boundary delays each stop synchronous traversal at the old cursor, then reach the exact tail through the queued transitions.
- Disabling inside either empty-container boundary callback leaves the cursor at that boundary without presentation.
- Native natural typewriter completion restarts the controller into a slow replacement run: its text, cursor and live typing state survive old-run cleanup.
- Manual completion's progress callback restarts playback: no old native completion or automatic advance affects the replacement after four frames.

Existing **19** audit-authored cases remain; current owned source contains **35** cases total (12 original EditMode + 10 traversal EditMode + 13 PlayMode). These counts describe authored source, not discovered/executed tests. No legacy test owned by another actor was edited and no unavailable `NonParallelizable` annotation was added.

## Actual inspection and evidence debt

Read the implementation contract/request again after the Windows interruption; preserved the partial controller patch and continued the same actor/session. Branch inspection reported `demo`; `globalReviewApproved` was present and true before implementation. Reused the package's existing movement/cancellation/event/typewriter paths after consulting the dialogue/typewriter reference entries; no replacement framework was needed.

Performed source/diff review of immediate versus scheduled branches, cursor/event ordering, callback version exits, null-entry policy and the new regression source. Scoped `git diff --check -- <owned implementation files>` returned exit 0 with no output. Reference enumeration for the new metadata GUID returned only its own `.cs.meta` path. These are read-only source/artifact checks, not product validation or compilation.

**Pending for root after EVERY source writer finishes:** compile/import and nonempty Edit/Play discovery/execution on the frozen snapshot, including the installed Unity 6.3 / 6.6 hosts; report unsupported-version branches separately. Run the three owned prefixed fixtures, existing Dialogue tests and current Components typewriter regressions. The original TMP label fixture still requires a valid default font/material. Native scheduling, real UI rendering, compiler/version compatibility, packaged sample expectations and aggregate release checks are not claimed verified by this receipt.

## Cross-owner dependency receipt

**No pending source/API dependency blocks this owner.** Current `Neo.Tools.Components/TypewriterEffect.cs` contains callback cancellation checks, completion generation checks and released typing state before natural completion. The two new controller/typewriter acceptance cases consume its existing public APIs; they do not modify the dependency or repeat the superseded repair. Root should execute them against the Components owner's final source and its own regression suite after freeze. A later Components semantic change requires rechecking those two integration cases; no additive API is requested.

No Unity/MCP, compiler, test runner, product checker, service, installer, git commit/push or child agent was launched during this implementation phase. Original audit/plan verification claims remain historical and were not rewritten.
