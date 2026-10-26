# Native implementation review 21 — Cards / StateMachine / Bonus

Status: **COMPLETE — one remaining Cards P1 accepted by root; all execution UNEXECUTED**. Independent source-only QA in the same native `gpt-6.1-sol` / `max` session. Only this Markdown report was written. No product/tests/public-doc/config/other-report edits, compiler/Unity/checkers, services/process management, children or commits.

## Intake gate

Full current source-final receipts read: Cards `ba36496283d764e5b19364ab3913dd5ce2f8ae7cfb1d6ecb3a5445b10ba57471`, StateMachine `e76e6c755ae95bd718e565288bdb3d8a0c08856c2ff466c6f15a014b7ffd2494`, Bonus `51f0093233acfe2df2f69ac056ba45887613a51d2acdfb1f9edb9cf1bbc15af1`.

Read manifest and current implementation-state entries for exact original ownership. At `2026-10-23T21:01:30Z`, read-only process enumeration found none of recorded launch/recovery PIDs Cards 40852/62856, StateMachine 65200/53336, Bonus 101696/81172. Cards NI12-01–04 and StateMachine E13-01 were admitted. Bonus was rechecked at `2026-10-23T21:04:08Z`: recorded PIDs still absent and its source-final receipt unchanged; Bonus was admitted. Tracker `source-running` alone is stale evidence and is not treated as a live writer. Closing enumeration at `2026-10-23T21:06:48Z` again found none of these PIDs; all three receipts and the central changed source/fixture hashes remained stable. This exact-owner gate does not assert that every repository writer has stopped.

## Scoped dispositions

| Scope | Source disposition |
|---|---|
| Cards NI12-01 presenter | **Source-closed:** post-model and post-await revision checks, model Clear/reuse and delayed replacement controls are coherent. |
| Cards NI12-02 cleanup handoff | Model/component/OnCardDrawn callback controls improve coverage, but **I21-01 / P1 remains at the earlier native transform boundary**. Root accepted the bounded correction below. |
| Cards NI12-03 serialized null slots | **Source-closed:** true null elements skip motion without undoing admission; four null/valid × animated/immediate controls preserve ordinary behavior. |
| Cards NI12-04 fixture waits/time | **Source-closed:** real-time bounded quiescence, captured/restored exact timeScale, active partial-flip and paused-entry controls. |
| StateMachine E13-01 | **Source-closed test-strengthening gap:** both failed-constructor paths exercise public state/events/hooks and exact cleanup; runtime/docs remain unchanged from NI13. |
| Bonus NI16-01 / root precision | **Source-closed in the assigned scope:** monotonic setter ownership and positive effective float-debit readiness, with all nine new controls. No additional confirmed correction found. |

## I21-01 — P1: remaining NI12-02 handoff occurs after native reparent callbacks

**Original owner/task:** SAME `Neo.Cards` / `neo-audit-1023-b02-05`. Current locations: `Assets/Neoxider/Scripts/Cards/Components/HandComponent.cs:201`, `TryAddCardAsync`; hand/model commitment and cleanup release at 209/214; `Model/HandModel.cs:98–101` and 144–147; deck tracked-card destruction in `Components/DeckComponent.cs:147–158`, tracking before OnCardDrawn at 525–527. Root independently accepted this remaining boundary during NI21.

**Concrete trigger:** Draw a tracked deck-created card into a non-full hand. Attach a one-shot genuine `OnTransformParentChanged` observer on that card, or `OnTransformChildrenChanged` observer on the destination hand, which calls the source deck's public `Reset()` when the card is reparented. `TryAddCardAsync` currently calls `card.transform.SetParent(transform, true)` before adding to the hand model and before `ReleaseDeckCleanupOwnership`. Native hierarchy callbacks can run synchronously there while the card remains in `_activeCards`. Reset queues `Destroy(card.gameObject)`. The outer hand call then continues to commit and release cleanup ownership, but cannot revoke that already queued destruction. After native destruction settles, the hand/model retain an accepted member whose scene object is destroyed. The later Deal finally sees a transferred card only after this has happened. This is source control-flow evidence; no native callback was executed in this review.

**Expected:** Cleanup ownership must leave the deck after actual accepted model commitment and before the first externally observable transform or admission callback. Model observers should still see the accepted card's final parent. Rejected admission must retain original cleanup ownership; no preemptive release before a possible rejection.

**Smallest correction, explicitly accepted by root:** Move SetParent into the existing assembly-local accepted callback, **after actual model commitment and deck cleanup release, before subsequent public model notification**. Keep the component/model membership order, ordinary final-parent observation, click ownership, accepted-history result and current callback/version guards. Use the existing `HandModel.Add/Insert(..., Action onAccepted)` seam; no new mandatory API, zone/reservation framework or asset rewrite. Handle both normal and bottom insertion. The same owner must update its receipt after this is complete; the current receipt's claim that all four corrections are complete does not close this boundary.

**Required authored native controls, UNEXECUTED:** Extend the existing owned lifecycle fixture with genuine hierarchy callback components, armed only for the intended transfer and consumed once. Exercise parent/children callback Reset and reinitialize paths, regular and bottom insertion across bounded cases, and keep existing ordinary accepted/rejected-deal controls. Inside the callback verify the actual accepted model/member/parent state; after yielding deferred destruction/quiescence verify that exact card remains alive in the exact hand/model/parent, source replacement composition is unchanged, one admission/draw is retained and no obsolete deal success/rollback or late transform tail occurs. Reinitialize is a generation/ordinary positive control even where Reset supplies the destructive repro. Do not substitute SendMessage, direct invocation of a callback method, an internal ownership flag or a mock destruction counter for actual native survival.

## Cards correction coverage

Read the complete latest request/receipt and full NI12 report, complete current HandPresenter and HandModel, all changed ownership/admission/cleanup/deal/null-slot paths and their layout/parent context in the four components, both complete current Order/Lifecycle fixtures, and all five updated guides: Cards README, DeckComponent, HandComponent, BoardComponent and View/HandView. Source identities were compared with every Cards row in NI12: the expected six runtime files, two fixtures and five guides changed; all other 21 pinned Cards rows are identical. Prior native approval remains historical and is not promoted to execution evidence.

- **Presenter:** Add captures revision before Model.Add, returns after a reentrant model mutation before presenter/view/click work, and checks revision plus current membership after view completion. Remove does the same before using its old index and after the await. Ordinary completed events remain. The four new expanded cases use actual Model.OnCardAdded/OnCardRemoved→Clear, exact empty state across model/presenter/view, zero stale add/remove/click, then successful reuse; delayed custom view completion after Clear/replacement preserves one current member and only current notifications/click ownership. The retained 11 ordering cases remain, including built-in linear work, invalid permutation, self-source, custom fallback and sort callback controls.
- **Handoff:** CardComponent keeps one internal cleanup owner; release clears it before removing the card from the former deck list. Internal HandModel Add/Insert commit data before this callback and before their public events. Deck binds newly spawned and visual-stack cards before exposing draw observers. Deal finally checks the handoff itself, including when source generation changed before DrawCard returned. These corrections cover model/component callback Reset and same-OnCardDrawn admission/reset; I21-01 is specifically the earlier SetParent callback. Existing post-flight rejection rollback/generation behavior is retained.
- **Null slots:** Board's actual serialized null element now continues before dereferencing its position; absent-array/out-of-range GetSlot fallback still returns the board transform. Both animated/immediate paths retain membership, parent, unchanged world transform and one placement for null slots. The valid-slot controls require actual destination movement and the correct movement-complete count, then bounded quiescence with no extra motion/events. Default Table/Slots settings and serialized fields remain.
- **Waits/ownership:** The three old late-write sleeps now call Quiesce, implemented via a real-time deadline and the existing four-second bounded WaitFor. Scene captures the exact borrowed timeScale, establishes 1 for real tween progression, and restores it in finally during both normal disposal and constructor failure. The paused-entry control captures its own outer baseline, enters at 0, observes the Scene's 1, requires a genuinely incomplete partial flip, completes it with exact face/scale/event counts, then proves restoration to 0 and quiescence while paused before restoring its original value.
- **All 13 added cases:** four presenter cases; four handoff cases; four slot cases; one paused-entry case were read in full with helpers and cleanup. The complete current fixtures also retain competing logical/visual multiset conservation, reset/reinitialize during flight, board/hand/view late-transform checks, restart during turn/flip/war and round-start callback, active partial-flip cancellation, replacement and universal one-shot destruction controls. Reflection authors Inspector fields; actual awaits/tweens/hierarchy/destruction remain native execution debt. Source inventory is 59 Edit + 24 Play = 83 authored cases, not discovery or pass evidence.

Five updated guides accurately describe presenter supersession, safe optional null slots, ordinary handoff, public C#/scene use and custom IHandView responsibilities. The ownership text will require the I21-01 boundary to be implemented before its broad survival claim is accepted. Existing signatures, IHandView/ICardView requirements, serialized defaults/equality and custom fallback remain; no new framework/public API was introduced in this correction resume. Other Cards animation/Drunkard/batch files, their metadata, seven guides and retained model/scene/Drunkard fixtures are byte-identical to the NI12 rows listed below. This bounded pass does not reopen Cards.Editor or primary legacy CardSystem fixtures.

## StateMachine E13-01 test-only closure

Read the complete latest StateMachine request/receipt and all 342 lines of the current runtime fixture, including both parameter-expanded new bodies and state doubles. Re-read EvaluateTransitions/target construction/logging context. Current `EvaluateTransitions:414–417` returns after a failed target if a newer explicit choice changed generation; without such a choice it continues to the next eligible rule. All six pinned runtime/wrapper files, the other three owned fixtures, two metadata files and nine guides match NI13 byte-for-byte. The only changed fixture is the existing runtime fixture; no new runtime or authoring option is inferred.

The new control uses exact A/C instances and a precreated exact cached D. Its public parameterless badB constructor consumes the one-shot hook BEFORE invoking it, optionally changes to exact C and then throws. With the choice, public EvaluateTransitions must retain exact C, evaluate only the failed rule, enter C once and never enter D; exact public exited/entered/changed transcript and PreviousState are asserted. Without the choice, the same failed constructor must evaluate the lower-priority rule and enter exact cached D with normal hooks/events. Both cases first prove ordinary initial entry, then a later explicit return to A and Stop with exact transcript/state/hook counts. They therefore exercise the existing postfailure guard and its ordinary-fallback positive control rather than merely inspecting a private generation.

The static constructor hook is cleared in finally. The captured Unity logger's exact logEnabled value is restored in a nested finally around the expected failing evaluation; the existing runtime log gate is unchanged. This is ambient state and still requires the isolated/exclusive runner. No recursive fault fixture, new lifecycle framework, test fixture or private-generation oracle was added. E13-01 is source-closed; the 66-case total is authored inventory only. Prior NI13 default duration/FIFO/native wrapper debt stays unexecuted.

## Bonus NI16-01 and effective-price closure

Read the complete current Bonus receipt/request, full NI16 and RootBoxPrecisionFinding, all 222 lines of Box, relevant unchanged Collection commit/notification paths, all 572 lines of the current Collection fixture and full updated Box guide. Identity comparison against NI16 shows only Box.cs, its existing Collection fixture and Box.md changed, besides request/receipt. The trusted clock/precision consumer, Reward/Play fixtures and other three guides are unchanged; they are preserved prior review, not new executions.

- **Publication ownership:** progress captures a private ulong write revision before storing raw progress and calling external SaveProvider.SetFloat. A superseded outer write returns before reactive publication. After Progress.Value it rechecks before Visual. This distinguishes same-value and ABA writes; equality is not used as ownership. Ordinary AddProgress/ChangeProgress/events and the claim finally guard remain. No live before/after subtraction reconstructs or erases an independent nested credit.
- **Positive price:** CheckProgress first requires current progress to meet the threshold; positive thresholds additionally require finite current/threshold and a strictly smaller clamped candidate from GetProgressAfterPrize. With finite current ≥ positive finite price the candidate is finite. TakePrize tests that readiness before Collection lookup/grant; payment uses the same helper. Thus the reviewed 100,000,000 / 1 case rejects without a grant, while a threshold of 5 stores a positive debit of 8. No exact nominal-price equality or replacement numeric backend is required. Zero thresholds, including zero progress, preserve legacy admission. Independent multi-key provider/callback side effects retain the previously disclosed postcommit limits.
- **All nine new expanded cases:** two actual public progress-key callback controls require 20 − 10 = 10 ordinarily or nested + 5 = 15 across raw/saved/reactive state and inside Collection observers, with one item, exact event order, affordable nested Take rejection and a later independent update. Two same-value/ABA controls record public virtual ReactivePropertyFloat.Value assignments while delegating to the real implementation, detecting stale publication even when equality would hide duplicate events; both handlers are removed in finally.
- The zero-effective-price case explicitly requires the live correct Collection singleton and two usable locked items, then repeated Take rejects with no unlock keys, raw/saved/reactive change, bar change or any save/reactive/progress/threshold/completion/prize event. The SAME box/collection switches to threshold 5 and must grant item 0, store 99,999,992 and emit the exact ordered notifications/key changes. Four ordinary fractional, rounded positive and zero-threshold cases require actual item 0/persistence, no item 1, one grant/prize, and correct raw/saved/reactive/observer progress. These are substantive positive controls, not accidental rejection caused by missing scene dependencies.
- Retained throwing Collection/item-key-save observers still settle an accepted grant and release the claim guard; exhausted collection consumes nothing and later ordinary supply succeeds. Fixture cleanup removes each static key observer in finally, destroys only owned objects/assets and restores its acquired save scope. Runtime/fixture Save keys, public/serialized fields and previously accepted best-effort notification boundary remain. The 49-case total is authored inventory only.

The updated guide accurately discloses nested write supersession, positive effective debit, rounded 8 vs nominal 5, zero-price legacy behavior and individual-key/postcommit limits in English with Russian summary. No new Bonus correction is requested. This pass does not approve unrelated Slot/roulette/wheel behavior or re-execute the trusted-clock tests.

## Artifact identities

First table paths are relative to `Docs/Audits/2026-10-23/`. Full latest receipts/requests and the referenced correction reports were read; NI13 retains the prior complete same-session runtime review.

| Artifact | SHA256 |
|---|---|
| Neo.Cards.implementation.md | `ba36496283d764e5b19364ab3913dd5ce2f8ae7cfb1d6ecb3a5445b10ba57471` |
| Neo.Cards.implementation-request.md | `bf09ab836785224e05540ba3d3053593b1727fcc8b4abc198b55e0021b25f26c` |
| NativeImplementationReview-12.md | `e03a183541e615ad56507e08d2eda2a3fe574b8463dd9c97d63d1f417d09d7ce` |
| Neo.StateMachine.implementation.md | `e76e6c755ae95bd718e565288bdb3d8a0c08856c2ff466c6f15a014b7ffd2494` |
| Neo.StateMachine.implementation-request.md | `83bc002127aeaaad3960fa4b3d3a56a2b01e5a3e3a72e40a0c3dbc071e41d09e` |
| NativeImplementationReview-13.md | `75995f2eac5c59f5dc5956449bb8f96bb0891980439930d0902f96fc5292c2b8` |
| Neo.Bonus.implementation.md | `51f0093233acfe2df2f69ac056ba45887613a51d2acdfb1f9edb9cf1bbc15af1` |
| Neo.Bonus.implementation-request.md | `16207864220833fd5d7c4af667aa00026fb951990b644ee6ba5c495a0b0456af` |
| NativeImplementationReview-16.md | `09de8e90b309666370628f839dcb93d3c621274c2dc0d00bb24a5cc09256fb8e` |
| RootBoxPrecisionFinding.md | `5f5e2b4714ab4cbac1bc89857e3a6088df9c1a05a6ace1298974e299712d7b1f` |

Changed product/fixture/guide paths below are relative to `Assets/Neoxider/`. These hashes identify source reads, not executed acceptance.

| Current file | SHA256 |
|---|---|
| Scripts/Cards/Presenter/HandPresenter.cs | `ba79a2f3790bb03b377d79c55939f3517ee5818635dbfd3978f31c0cf7fbeceb` |
| Scripts/Cards/Model/HandModel.cs | `8377fff257efbb6d0f9d85d93ec4bd2c22653d61bf476f002d269f412814c47f` |
| Scripts/Cards/Components/CardComponent.cs | `cc3acab40ab8c7c79380b69e069ce8a9a8873dbf96e27aed64c76b62cd529ab6` |
| Scripts/Cards/Components/DeckComponent.cs | `c99fa713b13083b3d99c9341ac879fecc51099aa3a079e048f5d51b017f1499a` |
| Scripts/Cards/Components/HandComponent.cs | `b100a54a72178a2063917226b579034473004f2d9bec0c28c1191df2c0ee5d21` |
| Scripts/Cards/Components/BoardComponent.cs | `510bdaffb4adc7aefef7d523c52acc20630fbf220be308af12e67432a339f15b` |
| Tests/Edit/Cards/Audit20261023NeoCardsOrderTests.cs | `331e29cf27275fecd7b574521d302490ed1ee2f7a9e34b087f832352bb7bafcb` |
| Tests/Play/Audit20261023NeoCardsLifecycleTests.cs | `c9836523a2f1fea11baf841c0b8e86c0f727ffc8f084292af6d3068a641ee6f2` |
| Docs/Cards/README.md | `bd8cda4ef285a7c2dc3ec99a93a5d41e662ad4916bcfd33a05653f010ad21bd5` |
| Docs/Cards/DeckComponent.md | `45ee6dbc94d1c7bb178e87a89d62d8a28af3e57f38ce060ce24f76b8a0dea37f` |
| Docs/Cards/HandComponent.md | `25f548b51cd6e40585387e20a041589907d3f50a0a6243ab05a63c8b95480609` |
| Docs/Cards/BoardComponent.md | `c61b426fa6db07412f7bb77e171f80683264974c7365ce8dffbf552a9066bc40` |
| Docs/Cards/View/HandView.md | `3ac38646929d5bb363c0e5f3bfab9eb26e8d37906f5a4f5a3c58351448a00d9a` |
| Tests/Edit/StateMachine/Audit20261023NeoStateMachineRuntimeTests.cs | `323eabf26995eaa4a7c381484665d0cc6e63520ab0e6210e0e927e71122f6c64` |
| Scripts/Bonus/Collection/Box.cs | `30050c7a79a19d133ecf2e126bf3c70d324065e8c207e27084a264bf3ea4fbd7` |
| Tests/Edit/Bonus/Audit20261023NeoBonusCollectionTests.cs | `bf3b759cf61009921d5b3140127af8b231e64762d1147b464b020360994e6fe7` |
| Docs/Bonus/Collection/Box.md | `c364ea741c7318de69e35a3ea5ab83674587a2a71dd9bb3cc2cc223d0d60d81f` |

Exact retained identity comparisons: **21 other Cards rows match NI12**, **20 other StateMachine rows match NI13**, **9 other Bonus rows match NI16**, including their complete SHA256 values recorded in those reports. This covers unchanged Cards concrete views/HandView/tween scope/interface/Drunkard, four metadata rows, seven guides and three fixtures; StateMachine's six runtime/wrappers, three fixtures, two metadata and nine guides; Bonus's other three runtime files, Reward/Play fixtures, three guides and primary clock-scope dependency. These are identity comparisons of source artifacts, not execution/validation. No old report was changed.

## Handoff and execution debt

Return **I21-01 / remaining NI12-02 only** to SAME Cards owner for the already accepted callback ordering repair, genuine native hierarchy controls and accurate final receipt. StateMachine E13-01 and Bonus's two bounded corrections need no further source correction in the reviewed scope. Retain ordinary/public/serialized behavior and all meaningful assertions; no optional framework is a prerequisite.

All compilation, Edit/Play discovery/execution, native hierarchy callbacks/deferred destruction/tweens/time restoration, save/provider behavior, Mirror/optional configurations, package/docs/metadata/UTF-8 validation and supported Unity version acceptance remain **UNEXECUTED**. Source case counts and historical reviews are not pass evidence. The global execution barrier remains closed until every source writer finishes and root explicitly freezes the integrated snapshot; no execution was requested or launched here.
