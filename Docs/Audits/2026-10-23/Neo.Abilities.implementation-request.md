# Neo.Abilities: approved implementation request

Original actor: `/root/audit_abilities`. Target `demo` / `11.0.0`. Root has read your complete audit and plan.

Read `IMPLEMENTATION-CONTRACT.md` first. Do not execute tests/build/validators during this phase.

Keep nearest-assembly ownership. Finish bounded confirmed fixes and owned canonical documentation/backlinks. No optional broad framework is required.

## Assigned review actions

### ABIL-EDITOR-DOC (P2; approved-source-correction)

stale radius rule in AbilityDesignerWindow guide; add Editor.md links

### ABIL-CATALOG-SLOT (P2; source-implemented-unexecuted)

Plan: existing grant slot holds old blueprint while Cast reads replaced catalog; use a consistent granted snapshot with public regression

### ABIL-CLEAR-REACTIONS (P2; source-implemented-unexecuted)

Plan: Events.Clear removes internal modifier-reaction subscription; separate internal delivery preserving existing event order

### ABIL-CAPPED-PAYMENT (P2; source-implemented-unexecuted)

Cost10 against pool with MaxDecreaseAmount1 pays1 yet casts successfully; require cap-aware preflight and captured payment amounts without claiming arbitrary callback transaction atomicity

### ABIL-OPERATION-RECEIPT-DELTA (P1; approved-source-correction)

Damage/lifesteal/HealEffect/AbilityUnitBehaviour recompute deltas after reentrant pool callbacks and can report negative/wrong amount. Use actual existing Decrease/Increase return receipts, test independently of final balance.

### ABIL-UNIT-DISPATCH-COST (P2; approved-source-correction)

All unit behaviours SubscribeAny, every event invokes/snapshots U listeners mostly discarded by Target. Assess Target routing preserving public global observers/event order and measure invocation/allocation counters.

Root decision: Add Target-scoped subscription to existing EventBus; unit adapter rejects same targets as before. Merge matching+global observers by common registration sequence at exact existing snapshot boundary; preserve specific/internal/any phases, Clear/reentry/unsubscribe. Prove1/100/1000units callback/snapshot counts and parity, no new framework.

### ABIL-FINITE-TICK-ADMISSION (P2; approved-source-correction)

A Abilities disposition: public time advancement accepts NaN/infinity. Reject nonfinite step before mutation, retain ordinary finite cadence; no transaction or scheduler redesign.

## Relevant full native sections

Source: NativeReview-A.md

### Neo.Abilities — source review complete; resource receipts and approved dispatch correction remain

Original owner: `/root/audit_abilities`. Full artifact reads:

- `Docs/Audits/2026-10-23/Neo.Abilities.md` — `7da20a26c3966b182d9d93034d1b3b983b4919d70853a0b4164456a7174c2f8c`.
- `Docs/Audits/2026-10-23/Neo.Abilities.plan.md` — `ba37d46f0d5598475e074b4aa015ccfadbea4893000769cad9a87651f8f57f32`.
- `Docs/Audits/2026-10-23/Neo.Abilities.correction.md` — `1069d2e74dfbca14ac03540fd78808bad4d37207ca56e6609883bc5724ccea45`.
- `Assets/Neoxider/Tests/Edit/Abilities/Audit20261023AbilitiesRuntimeTests.cs` — `fc6fe4aba0e757f95a3e5780ec8037ccb35ff33babd7c0dcf6eb2affa4711636`.
- `Assets/Neoxider/Tests/Edit/Abilities/Audit20261023AbilitiesSceneTests.cs` — `ee98fb3aecedd7d3ad57737afd939198ad8b0ef5fd686d999115af92968f547f`.

Every changed runtime source hunk was inspected, with the full event bus/slot, damage/heal, cost capture/payment, modifier removal/timing, shield, maximum-bonus, level subscription, auto-cast and radius query paths. Both complete fixtures and all changed owned public-guide hunks were read. The fresh regressions exercise actual domain outputs, identities, public receipts, replacement lifetime and callback mutations; the scene fixture manually invokes lifecycle methods, so it cannot prove native Unity ordering. No tests ran.

**Completed fixes appear correct:** captured finite cumulative costs with exact per-entry payment; effective cooldown normalization and charge restoration; lifetime-bounded modifier ticks; snapshot-based bulk removal; retained granted blueprint references; internal reactions surviving user Clear with preserved snapshot timing; nested CastId; maximum-bonus staging; live shield checks/replacement survival; actual level-source unsubscription; targetability/effective-range auto-casting and no-health damage rejection. The correction explicitly permits earlier successful payments to remain spent if a later callback invalidates preflight; do not require a rollback framework. Mutable blueprint references are not immutable snapshots. Revoke/regrant is the explicit refresh path.

**Confirmed mandatory residual A-17:** `DamageService.cs:125–126` and `:170–172`, `HealEffectOperation.cs:37–39`, and `AbilityUnitBehaviour.cs:206–208` discard `ResourcePoolModel.Decrease/Increase`'s actual return and recompute an operation receipt from live balance after synchronous resource callbacks. A health listener that heals 20 while taking 10 from 50 makes the outer final balance 60; current `HealthDamage` becomes -10, suppressing damage/lifesteal receipts despite a real 10-point decrease. Healing has the converse defect. Severity **P2**; expected receipts describe that operation's accepted delta, independently of the final callback-mutated balance. Smallest repair: use the actual Decrease/Increase return at all four sites, retain the separate live death/availability decisions, and add callback-mutating damage/heal/lifesteal/wrapper regressions. Root explicitly accepted this repair for the same original author after review. No event transaction or final-balance rollback is implied.

**Approved 11.0 dispatch correction:** `AbilityUnitBehaviour.cs:137` subscribes every enabled unit through SubscribeAny, while `HandleEvent:216–220` immediately rejects every other Target. `AbilityEventBus.Publish:67–94` snapshots/invokes every any-listener for every event, producing O(events × registered units) wrapper calls and O(units) snapshot references per event. Root approves an additive Target subscription in the existing bus and moving the unit wrapper to it, while preserving public global observers. Match-target and global observers must merge by a common registration sequence. Preserve the exact current capture point: run specific snapshot handlers first; then capture matching/global snapshots; then internal reactions; then invoke the merged captured observers. A specific callback's subscription changes affect this publication, whereas internal Clear/unsubscribe does not cancel already captured observers. Required later acceptance: 1/100/1,000-unit invocation/snapshot counters and global/scoped ordering, nested Clear, unsubscribe and reentry parity. This is a concrete approved improvement, not evidence that routing already exists; original `/root/audit_abilities` owns implementation after the review barrier.

Aggregate work beyond dispatch: scene radius queries scan registered behaviours; auto-casters share their own widest-radius query per pass, but many casters/projectiles still scan O(casters × units) / O(projectiles × units) at distinct positions. IAbilityWorldAdapter is already the replacement seam for spatial indexing; a new framework is not required here. Global SlotsVersion invalidates every auto-caster's cache when any grant changes, a profiling lead. Modifier/tick/effect snapshots and nested removal pools allocate or retain capacity per operation/depth. Public Tick methods accept NaN/infinity (only <=0 guard), unlike Core resource Tick; finite-time admission is a bounded robustness lead requiring an explicit accepted input contract and focused evidence, not proof of full Tick/Cast transaction semantics.

**Needs evidence:** both Unity branches, native enable/disable/editor buttons, visual delivery/pause, projectile tunneling and real pooling. The corrected docs accurately distinguish domain time, scene projectile time, mutable catalogs, catch-up limits and adapter-order dependence. No lockstep/network-snapshot guarantee or mandatory catalog extraction is accepted.

## Completion receipt

Write `Docs/Audits/2026-10-23/Neo.Abilities.implementation.md` with exact actions/files/additive API/dependencies, migration details and authored acceptance (UNEXECUTED). If blocked on another additive API, complete independent files and identify it precisely for root continuation. Do not edit shared review-action/manifest/other actor reports.

## Snapshot2 actual AbilitySystem constructor errors

Both actual source2 Unity6.6/6.3 compiles reject owned AbilitiesReceiptsAndTimeTests199/260 newAbilitySystem(world): CS1729 no1argconstructor. Root read actual Domain/AbilitySystem.cs53 parameterlessctor and World71 setter. SAME original native owner: correct exactlythese2fixturearrangements usingexistingparameterlesssystem andactualWorldproperty (retain FakeWorldAdapter identity/PendingProjectileTimeout andalltime/projectile/resource assertions). No productionconstructor/behavior/API change. Read actual both compiler-errors JSON under Local/Audits/assembly-2026-10-23/unity/final/f84c9007d0494cd8a421d84d70d931a0/{u6000_6_0f1,u6000_3_14f1}/mirror-on-dev/Receipts/. Allownedactualjobsstopped; rootclosedgate beforethisresume. No compiler/Unity/tests/checkers beforeALLsourcewritersfinish/newfreeze. Updateownreceipt honestprioractualfailure/sourcefixnotpass, no unrelatedfiles/docs/config/metas/children.
