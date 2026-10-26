# Neo.Tools.Dialogue: approved implementation request

Original actor: `neo-audit-1023-b06-07`. Target `demo` / `11.0.0`. Root has read your complete audit and plan.

Read `IMPLEMENTATION-CONTRACT.md` first. Do not execute tests/build/validators during this phase.

Keep nearest-assembly ownership. Finish bounded confirmed fixes and owned canonical documentation/backlinks. No optional broad framework is required.

## Assigned review actions

### DIALOGUE-SPARSE-RECURSION (P1; approved-source-correction)

Immediate empty dialogue/monolog boundary traversal remains recursive despite zero-delay fix; assess bounded iterative traversal preserving events without full session extraction.

### DIALOGUE-SHARED-FONT-MUTATION (P2; documented-policy-proposal-deferred)

ValidateFallbackFonts mutates shared TMP font asset; assess read-only runtime validation and explicit editor repair/owned clone policy, no silent shared asset edits.

Root decision: Borrowed TMP fallback repair is a policy proposal without demonstrated rendering failure. Preserve/document current behavior; no implicit asset rewrite or new font framework in this patch.

## Relevant full native sections

Source: NativeReview-B.md

### B-13 / P2 — sparse dialogue traversal still grows the synchronous call stack

Owner: Neo.Tools.Dialogue / `neo-audit-1023-b06-07` (plan empty-container path confirmed). Current `Assets/Neoxider/Scripts/Tools/Dialogue/DialogueController.cs:415`, ShowCurrentSentence calls GoToNextDialogue for empty monologues and GoToNextMonolog for empty sentences; with automatic boundary delay disabled, those synchronously call NextDialogue/NextMonolog and ShowCurrentSentence again. Trigger: thousands of non-null empty containers followed by one valid line. Expected: bounded-stack traversal and one final presentation; actual call-stack depth grows with container count, risking process-fatal StackOverflowException. Zero-delay NextFrame correction does not cover this branch. Smallest repair: iterative internal traversal preserving boundary event order, version invalidation and scheduled-delay exits; do not require a new DialogueSession API. Verify 10,000 empty containers, all-empty graph once-only termination and callback restart without stale presentation; retain current null-entry policy.
Source: NativeReview-B.md

### Neo.Tools.Dialogue — full artifacts, both diffs, current traversal/UI and all 19 new cases reviewed

Owner/task `neo-audit-1023-b06-07`. Captured cancellation token checks after swallowed typewriter cancellation, version fences after presentation/boundary callbacks, stopped-before-final-notification state, named unsubscribe and native Complete reuse are coherent. Tests observe controller cursors/text/effect state/progress/event counts and cancel/destroy their owned graph. Their slow typing, bounded frame loops and 60-second scheduled-delay test distinguish stale work from natural advancement. TMP default font/material is a real fixture dependency; missing assets must not be masked as a navigation pass. Native scheduling and actual UI are unexecuted.

B-13 remains minimal required work. The TypewriterEffect handoff in the original audit is superseded by current dependency callback/cancellation guards described in the plan; inspect its final integration with the Components owner rather than repeating that old repair. Shared fallback-font removal is a confirmed mutation of borrowed TMP_FontAsset data, but no resulting rendering failure was demonstrated; read-only inspection plus explicit repair is a policy proposal, not a silent change to configured assets.

Aggregate review: DialogueController has no continuous Update/search/physics query; progression is owned UniTask/typewriter work. Repeated font validation traverses shared fallback tables at explicit validation/Awake, not per frame; a read-only font report can share equivalent validation context only with proper asset invalidation, without sharing target UI state. New core/view/snapshot proposal must preserve legacy OnSentenceEnd presentation timing and sparse/null graph mechanics; it is not prerequisite to bounded traversal.

## Completion receipt

Write `Docs/Audits/2026-10-23/Neo.Tools.Dialogue.implementation.md` with exact actions/files/additive API/dependencies, migration details and authored acceptance (UNEXECUTED). If blocked on another additive API, complete independent files and identify it precisely for root continuation. Do not edit shared review-action/manifest/other actor reports.
