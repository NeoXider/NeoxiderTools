# Native implementation review 12 — Cards and bounded Network/Time rechecks

Status: **SOURCE REVIEW COMPLETE. Network NI7-01/03 and Time NI8-01 are source-closed; four remaining Cards findings NI12-01–04 are accepted by root for the SAME Cards owner.** All compilation, discovery, tests, native tween/lifecycle, profile and performance acceptance remains **UNEXECUTED**. Only this review MD is written. No product source/tests/public docs, original receipts/plans, metadata/configuration or isolates are edited. No Unity/MCP, compiler, runner/checker, service/install/process management, children or commits are used. The global source gate stays closed until every writer finishes and root explicitly freezes/resumes execution.

Target `demo` / `11.0.0`, contract base `ca198556`; local review date 2026-10-24. Working-tree source identities below are read bookkeeping, not a final accepted runtime snapshot.

## Complete read coverage

Read the complete latest Network, Time and Cards implementation receipts and the Cards approved request, including its prior native findings/compatibility scope. Network recheck covers the changed presenter publication, all eight new lobby cases/helpers, current Lobby documentation and an exact byte-identity comparison of the handler fixture with its old attribute restored. Time recheck is deliberately limited to the two attribute removals/receipt: exact byte comparison against NI8 confirms no other fixture-body change. Neither recheck reopens active Shop, Bonus, Spawner, Editor or primary-test writers.

Main Cards coverage: every current implementation-phase hunk in all ten changed runtime sources, both complete new runtime files, full HandView/HandPresenter, the relevant HandModel event paths, complete Drunkard round/war/deal/restart/ownership call graph, concrete card/view tween scopes, full new 11-case ordering fixture and 14-case lifecycle fixture, all four added scene cases and their relevant setup/cleanup, all four new metadata files, and all 12 owned public-documentation phase diffs. Earlier model/presenter audit repairs visible in the base diff are distinguished from new phase work. Retained model/Drunkard fixture counts are source inventory, not rerun evidence. Nested Cards.Editor and primary CardSystem tests are outside this owner's changes; this reviewer authored neither worker's source.

Root already added explicit UnityEngine.UI in the shared Editor test asmdef. That dependency is present; the Cards receipt's remaining-reference request is stale integration context and is not repeated. No source workaround/reference edit is made here.

## NI7-01/03 recheck — source-closed

`Audit20261023NeoNetworkHandlerOwnershipTests.cs` contains no NonParallelizable annotation. Re-inserting that ONE removed declaration line into the current bytes reproduces the NI7 original SHA256 `fc6caa30ee284b43bf740d55d01df95ae553801db23b4ff2a4f6ebdc527154f0`. Thus all six client/server receiver/payload/disposal cases and cleanup are byte-for-byte retained otherwise. Current hash is `7fef587773092ed5d04c5d3148f0a7f370e639bb0164ffb4850bc9c639184291`. This proves the bounded source delta, not a new compiler result.

`NeoLobbyPresenter.RefreshView:121–160` now returns on an unchanged snapshot BEFORE changing the publication revision. A changed snapshot is committed, obtains a captured revision, and checks it after count, countText and ready events before later stages. The final status has no stale post-event continuation. A nested changed Join snapshot invalidates old outputs; unchanged nested refresh preserves ordinary pending stages. Public fields/events and normal count→text→ready→status order remain. The fence operates between UnityEvent stages, preserves listeners already inside a single event and adds no deferred replay.

Eight new authored cases cover public Join reentry at count/countText/ready, ordinary four-stage order and unchanged nested refresh at all four stages. They assert actual connecting state/address and exact transcripts, then an unchanged refresh emits no additional outputs. They use actual native enable and public methods, without reflected lifecycle invocation; nevertheless they are UNEXECUTED and do not prove saved UI or remote admission. The current lobby inventory is 13 cases instead of the earlier 5; old NI7 hashes/counts remain history. NI7-01/03 close at source level only.

## NI8-01 recheck — source-closed, no runtime reapproval

Both Time fixtures omit the unsupported attribute. Re-inserting that single removed line reconstructs each exact NI8 recorded hash:

- UtcHookTests: old `e4687bae1b2947f962dcfd6abcf88fcd4a306fc47e4389f1ae6642e2b178db13`; current `4cca04f3b9bcad9d841f816d05342e6eee42b437406e155f7f8be91e13028d98`.
- ClockRestoreTests: old `441b74971e16175d78757aadf42d870739df9a552733f8071b562fa47c7e78ff`; current `ef6b28737aac98a05f7370264ba45ee34f45af038072c923efa0e7f8a461649a`.

All 15 cases, storage/provider/lazy-clock cleanup and metadata are preserved. The updated Time receipt correctly relies on exclusive isolated execution rather than a fixture annotation. No Time runtime or unfinished Bonus consumer source is approved by this attribute-only recheck; current compile/discovery/results remain unexecuted.

## NI12-01 — P1: Presenter Add/Remove continue after model callbacks invalidate their state

Owner: SAME `Neo.Cards` / `neo-audit-1023-b02-05`. Current `Assets/Neoxider/Scripts/Cards/Presenter/HandPresenter.cs`, AddCardAsync at72–87 and RemoveCardAsync at95–116; synchronous model event paths are in `Model/HandModel.cs`, Add and RemoveAt. Root accepted the bounded fix; source tracing only.

Add trigger: subscribe a one-shot `hand.Model.OnCardAdded` handler that calls public `hand.Clear()`, then call hand.AddCardAsync. The outer path increments `_revision`, calls Model.Add, and that handler clears model/presenter/view. After return, the outer call still appends the presenter, subscribes its click and adds the view. Actual result is model 0 with a presenter/view member, plus a stale add notification. Remove trigger: seed a hand, subscribe Model.OnCardRemoved to hand.Clear, then RemoveCardAsync. Clear empties `_cardPresenters` inside Model.RemoveAt; the outer `_cardPresenters.RemoveAt(index)` then uses a stale index and throws. Sort already checks its captured revision after Model.Sort; Add/Remove do not apply that protection.

Expected: nested clear/change wins consistently across model/presenter/view and old notifications cannot resurrect removed members. Smallest repair: capture/validate operation revision AFTER model callbacks and before remaining list/index/view/subscription work; fence later post-await notifications to current admission/removal as well. Preserve synchronous ordinary API/event order, exact indexed removal and custom IHandView compatibility. No zone/transaction framework is required.

Required same-owner controls: one-shot public Model.OnCardAdded/OnCardRemoved→presenter.Clear, exact all-three-layer empty state, no stale index exception/notification/click subscription, and a subsequent ordinary add/remove positive control. Include delayed custom-view completion followed by clear/replacement so post-await add/remove notifications are not accepted merely because a previous await succeeded. Existing model-sort/view-sort Clear controls are meaningful but cover different boundaries.

## NI12-02 — P1: draw-observer admission still belongs to deck reset cleanup

Owner: SAME Cards actor. Current `Assets/Neoxider/Scripts/Cards/Components/DeckComponent.cs:147–160` Reset, `:504–531` DrawCard and `:402–438` DealToHandAsync; admission occurs synchronously in HandComponent.TryAddCardAsync. Root accepted this bounded ownership correction.

Public trigger: a deck OnCardDrawn listener calls `hand.AddCard(card)` (or immediate accepted TryAddCardAsync), then `deck.Reset()` before returning. DrawCard registered the spawned view in `_activeCards` BEFORE invoking that listener. Reset invalidates the generation but blindly destroys every tracked active card, including the now-admitted hand member. DealToHand cannot recognize the admission/remove cleanup ownership until AFTER DrawCard returns, which is too late. The hand retains membership/model data pointing to a destroyed accepted object; accepted reset survival documented by this phase is violated. A similar late release is possible when Reset is reentered from admission callbacks before TryAdd returns.

Expected: cleanup ownership leaves the deck AT the admission boundary, before any observer can reset it; source generation still prevents rollback into a replacement deck. Smallest repair is a bounded explicit ownership handoff integrated with existing card/admission paths. Do not infer ownership only after a whole callback returns, destroy a transferred card as a rejected visual, or introduce reservations/zones as a prerequisite. Preserve public signatures, rejected-data rollback, ordinary deal event count and deck model generation behavior.

Required controls: OnCardDrawn→accepted hand admission→Reset/Reinitialize in the same observer, capture the accepted object, let native destruction settle, and assert it remains alive in exact hand/model/parent state with no duplicate rollback or old-generation success. Cover the admission-callback reset boundary and ordinary full-hand rejection/accepted-deal positive controls. Existing scene draw-observer test omits reset; existing lifecycle accepted-reset test resets only AFTER the deal returns. Neither covers this narrow ownership gap.

## NI12-03 — P2: unified slot layout dereferences serialized empty slots

Owner: SAME Cards actor. Current `Assets/Neoxider/Scripts/Cards/Components/BoardComponent.cs:638`, ArrangeCardsInternalAsync; GetSlot555–567 returns a configured array element verbatim, including null; ShouldUseSlotPlacement570–573 does not validate it. Root accepted the bounded compatibility repair.

Trigger: existing Table/Slots authoring with `_autoGenerateSlots=false` and a serialized `_cardSlots` element left empty. New placement commits `_cards`/membership, then dereferences GetSlot(i).position and faults after admission. Earlier PlaceCardAsync explicitly tested `slot != null` before both animated and immediate placement, so the optional empty slot was safe. This is a new source regression, not a native-run finding.

Smallest repair: retain the prior safe optional empty-slot behavior while preserving valid-slot placement/animation and current membership/layout guards; no automatic asset rewrite or layout redesign. Add serialized-null versus valid-slot controls for animated/immediate paths, assert the exact after-admission member/parent/transform/event outcome and no late motion. A generic out-of-range fallback alone does not cover an actual null array entry.

## NI12-04 — P2: new lifecycle tests contain unbounded scaled quiescence waits

Owner: SAME Cards actor. Current `Assets/Neoxider/Tests/Play/Audit20261023NeoCardsLifecycleTests.cs:167`, `:197`, `:242` use default scaled UniTask.Delay180/240 outside Within/WaitFor. Scene493–563 does not establish or restore Time.timeScale. Root accepted the bounded test correction.

At timeScale0, replacement/cancellation can complete: for example, the Board test starts layout, replaces it through immediate placement/removal/clear, and all awaited old operations settle. It then waits forever in the scaled Delay180 before checking late transforms. The four-second real-time wrapper does not surround that sleep. Other cases requiring actual moving tweens can instead time out before exercising their intended oracle. No paused test was executed here.

Smallest repair: unscaled bounded quiescence waits; capture/restore exact borrowed timeScale and explicitly configure the native tween-progression scenarios. Preserve actual transforms, active partial flips, event counts and late-write checks. No fake scheduler, manual global tween advancement or skipped tween promoted to a pass. Add a paused-entry/restoration positive control proving the fixture cannot stall and retains the original time setting afterwards.

## Other Cards source closure and deliberate boundaries

R1 transfer logic now captures exact source model/generation/token, rechecks hand admission after flight, records admission before awaiting its layout, returns rejected data to the same current source top and restores stack views or destroys new rejected visuals. Old source cancellation stops flight and cannot return old data into a replacement model. Explicit zero flight duration remains distinct from configured fallback. Competing one-slot logical/stack cases check exact combined card multiset, restored stack order, one success and rejected visual lifetime. NI12-02 remains in callback-time ownership; these otherwise coherent paths do not discharge it.

R2 layouts use fixed member arrays and revision cancellation; remove/clear/capacity/new arrangement cancel owned move/rotation tweens. Post-await transform/notification writes check current parent/membership. Hand admission versions suppress stale add notifications after removal/clear, while accepted membership survives canceled layout. Board slots compact to current order; NI12-03 remains for null slots. Generic HandView independently owns raw layout tweens and cancellation registrations, rather than relying on an unrelated card operation handle.

R3 Drunkard creates a game generation/token before restart cleanup, checks it after the read game events/awaited helpers, and keeps old finally from clearing a newer game's flags. Distinct table/war views are tracked before animation and released from table ownership before bank/admission. Existing finite-hand data reserves count overflow and restore shortage-tie data without losing playable cards. Restart during redeal ignores round starts; public old round completes quietly on replacement. The complete round/war/restart call graph was read; no real callback/tween/native destruction result is inferred from source.

Concrete CardComponent/CardView/CardViewUniversal move/flip/rotation additions retain mandatory ICardView and serialized fields. CardTweenScope links caller/destruction tokens, retains the EXACT awaited tween, disables recycling and kills only that operation. Replacement cancels before starting another operation; flip scale restoration is guarded by operation identity. Stale cleanup cannot kill a recycled new handle. Universal one-shots are tracked and keep their quiet cancellation behavior; template roots/sequences link target lifetime. Arbitrary native Image/activation/destruction callback ordering, active/inactive target behavior and engine frame timing still need execution; source guards are not that evidence.

R4 visibility derives from show flag and actual bottom card at initialization/rollback; the added two-case test observes a real display's active flag after prior nonempty state and empty/disabled reinitialization. Configured display reference is retained. The model's explicit reset snapshot and direct-Model/visual-stack synchronization limits remain documented; no asset or equality migration is introduced.

Batch ordering is additive IOrderedHandView.SetOrderAsync over unchanged IHandView. HandView snapshots self-source, validates a complete current-member permutation before mutation, commits list order without reparent/membership callbacks, then performs one layout. HandPresenter selects that path once; legacy custom views retain remove/add/final-arrange behavior with revision checks after each await. Model sort callback reentry and custom-view removal callback Clear stop stale synchronization. NI12-01 remains in ordinary presenter Add/Remove, rather than in the new sort fence.

Aggregate work is explicit: built-in sort eliminates 2N+1 layouts and performs O(N) snapshot/validation/layout work plus sorting, with bounded allocations per call. The new 16-card transform-access control checks at most 8N accesses against the real built-in path and also order, retained clicks and indexed removal. It does not measure FPS/bytes or prove 1/100/1000 profiling. Layout snapshots/tasks/token registrations and per-round lists/dictionaries still allocate; no per-frame physics/scene search or new broad cache/framework was introduced. Custom fallback intentionally retains its old membership-based cost and semantics.

## Regression, docs and execution evidence

The full 11-case new ordering fixture covers built-in and legacy sort, invalid permutations with unchanged transforms, self-source, pre-cancellation, model/view callback Clear and click/index-removal parity. Its transforms/counters are observable API work, not source-string assertions. The full 14-case lifecycle fixture drives competing public deals, reset/reinitialize in flight, add/remove/clear during motion, restart in turn/flip/war/from round-start callback, old finally, active partial-flip cancellation/replacement and destroyed universal one-shot. Reflection configures Inspector fields rather than directly driving private completion/state. Bounded WaitFor/Within observe tasks on the real player loop; NI12-04 remains outside those wrappers.

Current authored inventory is 27 scene + 11 order + 17 retained model = 55 Edit and 14 lifecycle + 1 retained Drunkard = 15 Play, total 70. These are source-attribute counts, not discovered or passing tests. The primary CardSystem tween cases and nested Cards.Editor tests are excluded. The worker does not own the primary fixture and this reviewer does not approve its pending NI10 corrections through this pass.

All12 owned guide diffs were read: runtime and Inspector paths, cancellation/admission timing, exact source rollback, reset survival, custom fallback, restart notification/ownership and Cards.Editor backlinks are described. New readonly FlipDuration/cancellation overloads and optional ordering capability preserve old signatures. No required scene setup, package choice, serialized rename, card equality or mandatory zone/transaction framework was added. The ownership claims must be reconciled with NI12-01/02 before final source approval; unknown native capability must remain unverified.

After all source writers finish and root freezes the integrated snapshot, both Unity 6.3/6.6 compilers, actual Edit/Play discovery, these cases/existing Cards/Editor slices, real motion/destruction/reentry/paused cleanup, imported sample paths and metadata need execution. Test presence, direct reflection, historical diagnostics or a zero-selected/ignored run cannot supply that acceptance. No verification launch is requested now.

## Artifact identity

SHA256 rows identify this reviewed source snapshot. Full receipts/requests and new sources/cases were read; runtime/docs coverage is every phase hunk plus the call graph stated above. Unchanged prior audit cases are counted/pinned without misrepresenting them as new runs. Root configuration rows are integration context only.

| Reviewed artifact | SHA256 |
| --- | --- |
| `Docs/Audits/2026-10-23/IMPLEMENTATION-CONTRACT.md` | `d5bc7a2d934e20f40647c45d53746c3734a1dbbfe95f182600d66977e9bbcbab` |
| `Docs/Audits/2026-10-23/Neo.Cards.implementation-request.md` | `18782871fc23ba64ee09d1e5a670b7f604c68a04690e5ea3c5bc56f85b3024d9` |
| `Docs/Audits/2026-10-23/Neo.Cards.implementation.md` | `77cc7fbf75026fbf763a672f7925f4b3638c99cd42f37f36e16d78de8ec032b4` |
| `Docs/Audits/2026-10-23/Neo.Network.implementation.md` | `f43964d13b6e88a4bc92cc157b956520a9f4e29498009289517947bf005c36a6` |
| `Docs/Audits/2026-10-23/Neo.Tools.Time.implementation.md` | `865ef4d2fd3cf34dd01e468a771f63805500ee4246e7eebcda4bd0d450fe190a` |
| `Docs/Audits/2026-10-23/NativeImplementationReview-7.md` | `1b85c059b89c6bc3a6526590c3a502b9fb6ef6fec188b26bc079c42becf48d1d` |
| `Docs/Audits/2026-10-23/NativeImplementationReview-8.md` | `428bf4ad3b4698d410531c976190f60ba71ff5c022f4ead46dd549cec4dbff01` |
| `Assets/Neoxider/Scripts/Cards/Components/DeckComponent.cs` | `dbf5f482db0b776d01e91d6325a472de7f265b318f28d4cb3b23aac6443af968` |
| `Assets/Neoxider/Scripts/Cards/Components/HandComponent.cs` | `7315ae6f9c58fdca9f23b094cd557d4143520978c479a5f83ffa6e0f6e28a910` |
| `Assets/Neoxider/Scripts/Cards/Components/BoardComponent.cs` | `3a6bfc53696f6ac4743f9234ed4138583597c03aed8df487a4784e0798b88574` |
| `Assets/Neoxider/Scripts/Cards/Components/CardComponent.cs` | `61e7f3e74e363c555593d6c50b443fba129d127e4ef43d1e9fcaddbf1453de88` |
| `Assets/Neoxider/Scripts/Cards/View/CardView.cs` | `8ebe9296c9a15ef1135b2073454ea13b9dc0906dbd0bdf63a614af886a9addcd` |
| `Assets/Neoxider/Scripts/Cards/View/CardViewUniversal.cs` | `fc82754718263bf23f093aa1c23840b7f90fa71050d0aee9fe69a395178203c6` |
| `Assets/Neoxider/Scripts/Cards/View/HandView.cs` | `13c2b5a95a94a09f2ae58ee6c8d361718d4e2e16bf66e2c707342ce47cac6ced` |
| `Assets/Neoxider/Scripts/Cards/View/CardViewAnimationTemplates.cs` | `b2c3092c4c6efd0dfd66fe56e4df07f5feec39f552f821f7c612c3b347d6c194` |
| `Assets/Neoxider/Scripts/Cards/Utils/CardTweenScope.cs` | `2a0b78a2feafe26a82a990380cffa7933ea6b1cd15c14789ec59e87ffe2cb7f4` |
| `Assets/Neoxider/Scripts/Cards/Utils/CardTweenScope.cs.meta` | `2b071538bf0aa72b06f8811a89333971487c6a9fdd1c9feadc0e51932614d4ee` |
| `Assets/Neoxider/Scripts/Cards/Core/Interfaces/IOrderedHandView.cs` | `1ecdc34b2db6d13df03f65b3de04f5b11cd2955f56e4cfdeb35a24b8d0b96056` |
| `Assets/Neoxider/Scripts/Cards/Core/Interfaces/IOrderedHandView.cs.meta` | `9182cd5dc024151e979b85e21ef4110fc4a530b1ca29f96dfd9d539f26a65938` |
| `Assets/Neoxider/Scripts/Cards/Presenter/HandPresenter.cs` | `30bc4453645a3e94a682cc3c16e3c4c6373be3fe5e49efd2a6f9686146c99953` |
| `Assets/Neoxider/Scripts/Cards/Drunkard/DrunkardGame.cs` | `bd5f1cd9248ab5adbbda611f5caec13aae80d7d3505822b8a611a852fc268044` |
| `Assets/Neoxider/Scripts/Cards/Model/HandModel.cs` | `d565e3b97616fc6a1609e9459d34d736fb77b500d5b7f7b063219c693918111a` |
| `Assets/Neoxider/Tests/Edit/Cards/Audit20261023NeoCardsSceneTests.cs` | `c19d0780ddc2c5bc4402d1e5b75f87bd583900390f3f510e1594b382fff28e9d` |
| `Assets/Neoxider/Tests/Edit/Cards/Audit20261023NeoCardsOrderTests.cs` | `a398f7a1edbd8ba627381d7fb0646829f3e0150cd10548261221e0b6ac629632` |
| `Assets/Neoxider/Tests/Edit/Cards/Audit20261023NeoCardsOrderTests.cs.meta` | `f02556108ab01be6cf88cd80eb4741d62de85d0344631712a786285a08bb9441` |
| `Assets/Neoxider/Tests/Edit/Cards/Audit20261023NeoCardsModelTests.cs` | `9b6902897ce15f3819e4502d9eff09e569e8466fb7867abc1caed0f6e209aca2` |
| `Assets/Neoxider/Tests/Play/Audit20261023NeoCardsLifecycleTests.cs` | `e117cb88f8536c7a0190310e3c88dd10ea6559b3cbe9ff1453429df6475f8a11` |
| `Assets/Neoxider/Tests/Play/Audit20261023NeoCardsLifecycleTests.cs.meta` | `7c5f6a5009dc859eb12c5db11e5de784560dfa91344672a6bac34ee0cdab6c72` |
| `Assets/Neoxider/Tests/Play/Audit20261023NeoCardsDrunkardTests.cs` | `6d96c61b76a5be138c76b8bfa51a185df98342b1899d544b621367148a859136` |
| `Assets/Neoxider/Tests/Edit/Neo.Editor.Tests.asmdef` | `204a5ba6382792131c48cae74f38e27f5431909c0c0866c3b2905be0f2c4b4a5` |
| `Assets/Neoxider/Tests/Play/Neo.Tests.Play.asmdef` | `1856ba6d704ec0852b65cf22f63fdc0427818c49259f6f2032488b02745d62ad` |
| `Assets/Neoxider/Docs/Cards/README.md` | `feaad699f2bbf69811946a6fa5b42ea1da8e6486d688056433259e90c53f9b76` |
| `Assets/Neoxider/Docs/Cards/DeckConfig.md` | `5f735fb8d4f246d8087dd9266da84000c423c9eaf0509094801fe0904adbd3fc` |
| `Assets/Neoxider/Docs/Cards/DeckComponent.md` | `57fe108bbed90f00576698cece5643eb7bacc8ae4fb79e20eba46805174f83f8` |
| `Assets/Neoxider/Docs/Cards/HandComponent.md` | `5b551884b0587a8a608f847b763c2fd030ef0b8a625d276d310100fef0c8dee8` |
| `Assets/Neoxider/Docs/Cards/BoardComponent.md` | `20ea743e35a3e695e2dec446051a7cfaca419eeccad02f18d907b84010574cfe` |
| `Assets/Neoxider/Docs/Cards/CardComponent.md` | `180b880dc7f5b58b8e51dd1b5b3e8168f5c750c79c64c51a98174b65f528a39a` |
| `Assets/Neoxider/Docs/Cards/InternalTypes.md` | `68667268020adc5327fe5895b1151aea46e6db8e6f9b460e03dafead8ca67b44` |
| `Assets/Neoxider/Docs/Cards/View/HandView.md` | `53b5a4a5eb4453b7151363aa4c1c536cd1075e238b695ff19b1fb55b42f90871` |
| `Assets/Neoxider/Docs/Cards/View/CardView.md` | `b73383bc76709d7f5022715676c5548562a667127928a3033083dd33fe8ee5c8` |
| `Assets/Neoxider/Docs/Cards/View/CardViewUniversal.md` | `202b8ecbc72226d91baa1b8ea9fac7a26cf3e093244fb78e800cfa4137551799` |
| `Assets/Neoxider/Docs/Cards/View/CustomCardViewGuide.md` | `3874d966595ff33f424a50b90cae75978cec4b84672f8b42ae4fa8ef1db7694c` |
| `Assets/Neoxider/Docs/Cards/Examples/Drunkard.md` | `604b331fb628ea6ce70528109cc712fb485a4965ea7f118fb92c1454581b42ad` |
| `Assets/Neoxider/Scripts/Network/Lobby/NeoLobbyPresenter.cs` | `d1d01fa0a49a7ccf414190ce9024720971477aaad95d4bd68b8b10bc13b06d7e` |
| `Assets/Neoxider/Tests/Play/Network/Audit20261023NeoNetworkLobbyTests.cs` | `1d3c7fdcdce9be627071637b61a3335bcc4eef276daf95a53686e73f320403e0` |
| `Assets/Neoxider/Tests/Edit/Network/Audit20261023NeoNetworkHandlerOwnershipTests.cs` | `7fef587773092ed5d04c5d3148f0a7f370e639bb0164ffb4850bc9c639184291` |
| `Assets/Neoxider/Docs/Network/Lobby.md` | `baa336b0f69c546023a34722abb216e321242da5bc7d7fbb564c454bf88317b8` |
| `Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsTimeUtcHookTests.cs` | `4cca04f3b9bcad9d841f816d05342e6eee42b437406e155f7f8be91e13028d98` |
| `Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsTimeUtcHookTests.cs.meta` | `8de23d208a8638090a60a64a099079ee32cbbb161ccb2b0375eadbf2b6b9cc36` |
| `Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsTimeClockRestoreTests.cs` | `ef6b28737aac98a05f7370264ba45ee34f45af038072c923efa0e7f8a461649a` |
| `Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsTimeClockRestoreTests.cs.meta` | `67cbc7ec71429d97f1b3bda5f10245022cef3da3d8e8686aa893c22c972df972` |

## Handoff

Return NI12-01–04 to the SAME original Cards actor/session after root's full read. Recheck its later bounded source/receipt deltas without replacing or relabelling this snapshot. Network/Time removals and changed-only Lobby generation are source-closed, with all native/compile/test debt preserved. The original verification operator remains idle for execution until the explicit global freeze.
