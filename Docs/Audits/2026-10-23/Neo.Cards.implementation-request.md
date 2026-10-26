# Neo.Cards: approved implementation request

Original actor: `neo-audit-1023-b02-05`. Target `demo` / `11.0.0`. Root has read your complete audit and plan.

Read `IMPLEMENTATION-CONTRACT.md` first. Do not execute tests/build/validators during this phase.

Keep nearest-assembly ownership. Finish bounded confirmed fixes and owned canonical documentation/backlinks. No optional broad framework is required.

## Assigned review actions

### CARDS-EDITOR-DOC (P3; approved-source-correction)

Link Cards/Editor.md from canonical README/DeckConfig guide

### CARDS-R1-TRANSFER (P1; approved-source-correction)

DealToHand loses a card when destination fills during await; add acceptance and generation-aware rollback.

### CARDS-R2-LAYOUT (P1; approved-source-correction)

Mutable membership after layout await can index beyond calculated arrays or move a transferred card.

### CARDS-R3-RESTART (P1; approved-source-correction)

Destroy-only token does not cancel a restarted Drunkard round; prior continuation can mutate new game; concrete tween ownership required.

### CARDS-R4-PREVIEW (P2; approved-source-correction)

Empty custom deck initialization leaves prior trump preview visible.

### CARDS-BATCH-ORDER (P2; approved-source-correction)

Sort synchronization executes 2N+1 layouts; optional ordered-view path can batch once preserving legacy interface.

## Relevant full native sections

Source: NativeReview-B.md

### B-01 / P1 — a deal can publish success after the destination rejects it

Original owner: Neo.Cards / `neo-audit-1023-b02-05` (plan R1 independently confirmed). Current `Assets/Neoxider/Scripts/Cards/Components/DeckComponent.cs:346`, `DealToHandAsync`, consumes model/stack state, awaits movement, then calls void-result `HandComponent.AddCardAsync` and publishes `OnCardDealt`. Trigger: two deals to the last hand slot, or another add during movement. Expected: one accepted transfer, conserved source/destination multiset and no false success. Actual: the second hand add returns on IsFull, but source consumption and success event remain. Smallest repair: acceptance-result path plus exact-source rollback/reservation guarded by source generation; preserve old wrapper/signature. Verify through competing public deals, not a copied transfer algorithm. Large CardZone redesign is not required for this correction.
Source: NativeReview-B.md

### B-02 / P1 — board layout indexes stale geometry after an await

Original owner: Neo.Cards / `neo-audit-1023-b02-05` (plan R2 confirmed). `Assets/Neoxider/Scripts/Cards/Components/BoardComponent.cs`, animated layout loop around lines 582–607, calculates positions/rotations for the old count but loops over the current mutable `_cards.Count` across `await card.MoveToAsync`. Trigger: add a card while the first card moves. Expected: cancel/recompute current layout without moving cards that left the board. Actual: later index can exceed the old arrays, or an old card's completion applies stale rotation. Smallest repair: membership snapshot plus revision/generation and invalidation/restart; apply equivalent guards to other public layout paths. Observable assertion: add/remove/clear during animation leaves current members ordered and no late transform mutation.
Source: NativeReview-B.md

### B-03 / P1 — restarting Drunkard leaves the old round able to mutate the new game

Original owner: Neo.Cards / `neo-audit-1023-b02-05` (plan R3 confirmed). `Assets/Neoxider/Scripts/Cards/Drunkard/DrunkardGame.cs:83,212,898`, destruction-only `_ct`, `PlayRound`, `RestartGameAsync`. Restart clears views/queues and resets state without cancelling the still-awaited round. Trigger: restart during turn/war delay. Expected: only new game state/events survive. Actual: the old continuation uses new fields/queues and can deal/award or finish the replacement game. Smallest repair: game-generation cancellation before reset, generation checks after awaits and exactly-once war-pile ownership cleanup; linked per-operation tween cancellation where required. Existing new test restarts only after a completed round and does not cover this trigger.
Source: NativeReview-B.md

### B-04 / P2 — stale trump preview survives an empty deck initialization

Original owner: Neo.Cards / `neo-audit-1023-b02-05` (plan R4 confirmed). `Assets/Neoxider/Scripts/Cards/Components/DeckComponent.cs:204`, `Initialize`: preview is activated only inside `trump.HasValue`, with no inverse branch. Trigger: initialize a nonempty deck then an empty custom composition using the same display. Expected: count zero and inactive/cleared preview. Actual: old card remains visible. Smallest repair: derive display active state from `_showTrumpCard && TrumpCard.HasValue`, preserving its configured reference; regression must observe the real display GameObject after both initializations.
Source: NativeReview-B.md

### Neo.Cards — full artifacts, 17 changed sources and all three new fixtures reviewed

Owner/task `neo-audit-1023-b02-05`. Snapshot/reset/capacity/duplicate identity/art clearing/face-state and normal conservation fixes are supported by current code. Forty EditMode cases inspect real public model/view/order/sprite state and one PlayMode scenario covers completed short wars and finite-hand overflow. They do not prove mid-await concurrency or mid-round restart; B-01–B-04 remain actual source defects already recognized by the original plan. Serialized/public declarations are retained; changing rejected BoardModel.Add to throw is a documented contract enforcement.

Aggregate work: `Presenter/HandPresenter.cs:220–233` removes then re-adds every view; built-in `View/HandView.cs:82,94,98` performs a full layout on each operation and once again at the end. A sort of N cards calls layout 2N+1 times (one empty pass returns), making allocations/transform writes quadratic. This is confirmed work amplification, without an FPS measurement; accept a small optional batch-order path for the built-in view before endorsing scaling claims. Preserve custom IHandView compatibility and independently test callback/reentry semantics. No per-frame physics/search loop exists in Cards; poker combinatorial allocations are explicit request work, not equivalent contexts to cache indiscriminately. Atomic zones/instance IDs remain a large proposal, separate from required minimal defects.

## Completion receipt

Write `Docs/Audits/2026-10-23/Neo.Cards.implementation.md` with exact actions/files/additive API/dependencies, migration details and authored acceptance (UNEXECUTED). If blocked on another additive API, complete independent files and identify it precisely for root continuation. Do not edit shared review-action/manifest/other actor reports.

## Root accepted NI12 four bounded Cards corrections

ROOT personally FULL-read all154lines NativeImplementationReview-12.md, accepts NI12-01/02/P1 and NI12-03/04/P2, SAME actor fix all4 one resume preserving approved70cases/contracts. (1) HandPresenter Add/Remove capture revision and check AFTER Model.Add/RemoveAt callback before stale list/index/subscription/viewwork; also fence postawait notifications to current admission/removal. Model.OnCardAdded/Removed→Clear controls all3layers empty/no stale index/click/event, subsequentordinaryadd/remove, delayedcustomViewcompletion thenClear/replacement. (2) Deck cleanup ownership leaves AT successful hand admission boundary BEFORE any OnCardDrawn/admission observer can Reset/Reinitialize; do NOT wait until DrawCard/handTryAdd returns. Bounded explicit internal handoff integrated existingpaths, no zones/reservationframework/newmandatoryinterface/assetrewrite. Test sameobserver OnCardDrawn→admit→Reset/Reinitialize, admissioncallbackReset, waitnative destruction, actual accepted card alive/in exacthandmodel/parent, no duplicatedrollback/oldsuccess; retained rejection/ordinarydeals. (3) Board unified Slots must retain old safe serialized NULL slot behavior for animated+immediate while valid slots stillwork; assert committedmember,parent,transform,event/no latewrite, not just outofrangefallback. (4) CardsLifecycle unbounded scaled UniTask.Delay180/240 under timescale0 must become unscaled boundedquiescence, explicitcaptured/restoredtimeScale for realtweenprogression, pausedentry/restoration control, retain activepartialflip/transform/eventoracles, no fake scheduler/globalmanualtween skip. Root UnityEngine.UI Edit reference ALREADYpresent (oldreceiptpendingstale). Source/docs/regressions/receipt only; no execution untilglobalfreeze.

## Root accepted I21 native hierarchy handoff

ROOT personally FULL-read all111lines NativeImplementationReview-21.md and accepts I21-01/P1 remainingNI12-02. SAME original Cards actor: move SetParent from before HandModel commitment into existing assembly-local onAccepted seam AFTER model/member commit and ReleaseDeckCleanupOwnership BEFORE public model notifications. Both normal/bottom insertion; preserve observer-visible final parent, accepted-history result, callback/version guards, click and ordinary/rejected behavior. Genuine one-shot OnTransformParentChanged / OnTransformChildrenChanged observers Reset or reinitialize source deck; no SendMessage/direct fake callback. Assert callback sees accepted state and released cleanup via real card survival after deferred Destroy/quiescence, exact hand/model/parent, replacementdeck unchanged, one draw/admission/no obsolete rollback/tail, with ordinary/rejected positive controls. Read full NI21. No zones/framework/public breaking API. Update narrow guides/receipt. All execution remains UNEXECUTED until globalfreeze.
