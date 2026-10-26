# Native implementation review 3 — Neo.Abilities

Status: reviewed runtime implementation is coherent in the assigned scope; required bounded regression-control follow-up NI3-01 is accepted by root. Core precision NI2-01 remains a separate OPEN dependency until its new source/receipt is ready and reviewed. All compilation, discovery, tests, native lifecycle and workload/profile acceptance are **UNEXECUTED**.

Original owner: SAME `/root/audit_abilities`, `gpt-6.1-sol` / `max`, branch/target `demo` / `11.0.0`. Only this review document is written. No product source, tests, public docs, metadata, shared helpers/configuration, original receipt/plan or isolate is changed. No Unity/MCP, compiler, test/validator/checker, installer/service, process management or child agent is used. Global execution gate remains closed until EVERY writer finishes and root declares freeze.

## Complete read coverage and scope

Read the full current implementation receipt and request, every current changed hunk in the six phase-owned runtime files, both complete new fixtures and both metadata files, all changed owned README/Modifier guide hunks and complete Unit/Designer guides. The event bus was read in full, along with current damage/live-death/lifesteal, unit attach/detach/identity, system Tick/projectile retention/pruning and modifier Tick paths. Existing AbilitySceneRig was read only as test integration context, not edited or approved as native lifecycle evidence.

The receipt's15 phase-written files reconcile to six runtime sources, four owned guides, two new fixture sources/two metas and the receipt. The request is a separate reviewed input. Nested Editor source/Editor.md, Core, RPG, shared test assembly/support files and package/settings/sample files remain outside this owner's phase writes. Earlier catalog/cost/shield corrections remain visible in the base diff; they were independently reviewed previously and are not mislabelled as new phase work.

## NI3-01 — P2: overflow regression needs an observable normal-step positive control

Owner: SAME `/root/audit_abilities`. Current `Assets/Neoxider/Tests/Edit/Abilities/Audit20261023AbilitiesReceiptsAndTimeTests.cs:258–275`, `SystemTick_FiniteClockOverflowRejectsTheWholeStep`.

The current implementation is correct at `AbilitySystem.cs:621–632`: reject invalid delta, compute a prospective clock, reject nonfinite sum, THEN assign `_time` and advance/prune subsystems. Current test proves the pending projectile was not pruned. It does not prove the clock stayed usable: a faulty implementation assigning `_time = nextTime` before checking the nonfinite sum would preserve that pending entry and still pass NotifyProjectileHit, which does not validate clock/expiry itself. Later ordinary ticks would all reject the poisoned Infinity clock. This is a regression-control gap, not a claimed current runtime defect or an executed mutation test. Root explicitly accepted the focused test strengthening.

Exact existing-public-API scenario, with valid source preconditions:

1. Create an EMPTY AbilitySystem with FakeWorldAdapter and PendingProjectileTimeout1; call Tick(float.MaxValue) before any unit/modifier/timer exists. `_time` is a FLOAT (`AbilitySystem.cs:51`) and is now the largest finite float.
2. Only afterwards create a unit with HP50/Max100 and set HealthRegen4. Apply a fresh modifier with Duration2, TickInterval0 and no TickEffects. Assert the unit is alive at50 and the modifier is active with remaining2. These are relative delta-driven values unaffected by the earlier huge empty step.
3. Retain the existing successful empty-impact projectile cast. Call Tick(float.MaxValue), which must reject overflow without changing HP50, modifier remaining2/activity or pruning that pending cast.
4. Keep the existing successful NotifyProjectileHit assertion, then call ordinary Tick(1f). Assert HP54 and active modifier remaining1. These use deltaTime in `AbilitySystem:661–666` / `ModifierEngine:192–198` and prove a normal step is still admitted. A poisoned-clock implementation fails this positive control.

Do not invent an absolute readyAt=time+1 precondition: at float.MaxValue such a sum rounds back to the same time. The pending projectile's ExpireAt already has this representation limit; NotifyProjectileHit refreshes its record without checking expiration, and the next admitted pruning may remove it because clock>=rounded ExpireAt. Its continued long-term deadline is not this positive control. A cooldown could only be used after tracing its actual relative representation; the resource/modifier scenario avoids that dependency entirely. No private clock probe/new public getter, new harness or framework is needed.

## ABIL-UNIT-DISPATCH-COST — source closure

### Actual existing-bus index and order

AbilityEventBus retains the original constructors and specific/global API. Target subscriptions are additive in the SAME bus: a Dictionary<UnitId,List<ObserverRegistration>> indexes matching Target, while globals have their own ordered list. Registration records carry immutable delegate/order, with one shared monotonically increasing order sequence for both scopes. SubscribeTarget matches args.Target exactly, including None; Source does not select it. Null handlers are ignored; duplicates remain distinct; unsubscribe removes the first matching delegate only in its scope, removing empty Target buckets. Clear removes all user scopes but leaves the readonly internal route and lifetime counters.

CaptureObservers performs one dictionary lookup and merges only global plus matching lists, already ordered by registration. There is no scan/snapshot over every unit Target. Records are copied to a local array without user callback during merge. Removed/readded registrations receive new sequence positions; Clear does not reset the sequence or mutate captured records. Ordinary global observer relative order remains intact, with matching unit wrappers interleaved where their former global registrations occurred.

### Exact capture boundary and reentry

Current Publish phases are exact: event-id-specific list snapshot/invocation FIRST; CaptureObservers SECOND; internal modifier route THIRD; captured matching/global invocation FOURTH. Specific callback subscription changes affect the current observer batch. Internal or observer Clear/unsubscribe/subscribe affects future and nested batches, while captured outer records survive. No synchronous callback is deferred to a later frame. Each nested Publish captures a separate array and has its own counters. Exceptions propagate; a specific exception prevents capture, an internal exception stops later captured observers, and an observer exception stops remaining calls. No observer fault aggregation or rollback was added.

AbilityUnitBehaviour registers/unregisters its Target rather than every event globally and retains the current Unit/Target check. The two re-enable tests distinguish a specific callback replacing registration BEFORE capture from an earlier observer replacing it AFTER capture. The latter still attempts the captured old wrapper, which rejects its obsolete Target; next events reach the new id. Exact bus/system lifetime during arbitrary hub replacement remains a broader native integration concern, not a new claim of this Target patch.

### Counters and meaningful1/100/1000 controls

ObserverSnapshotCount counts nonempty combined arrays, EntryCount counts copied entries, InvocationCount increments immediately before attempted observer calls including a throw. Specific snapshots/calls and internal reaction work are excluded, and nested publications contribute. Clear retains counters. This instrumentation is at the actual array/call path, not a mock selector; it measures dispatch work, not CPU time or allocation bytes.

The wrapper workload creates1/100/1000 actual AbilityUnitBehaviours through the existing EditMode rig, attaches distinct per-unit UnityEvent counters, adds one global observer, records baseline bus counters, then publishes16 events to one chosen Target. It asserts every wrapper's result (selected16, others0), global16 and constant deltas16 arrays/32 copied entries/32 attempted calls. Closure captures a per-loop index correctly. This protects against a per-unit snapshot/call regression instead of merely checking one callback.

Other complete Target tests cover interleaved registration order with specific-first and Source-only exclusion; specific replacement affecting the current batch; observer unsubscribe/replacement with nested/later events; a REAL internal thorns reaction whose nested specific handler clears users while captured outer Target/globals survive; duplicate/wrong-target/None behavior; throwing global/scoped observers and attempted-call counts. These are nonvacuous expected transcripts and exact counters, all UNEXECUTED. The source supports O(global+matching) observer work; world-adapter radius scans, specific lists, modifier reaction cost and global SlotsVersion cache invalidation remain separate costs.

## ABIL-OPERATION-RECEIPT-DELTA — source closure with OPEN upstream NI2

All four sites consume Core's return value: DamageService health Decrease, DamageService lifesteal Increase, HealEffectOperation Increase and AbilityUnitBehaviour.ApplyHeal Increase. They do not reconstruct the receipt from callback-mutated balance. Death is still decided from live target IsAlive/Health after the resource callback, and later lifesteal eligibility rechecks the live source. Source/Target/CastId, shield/death/kill/damage/lifesteal phase ordering and exception propagation are retained.

The new cases differentiate accepted operation from final balance using real synchronous pool callbacks: damage10/3 then healing20 leaves60/67; damage clamped to available5 followed by SetCurrent20 emits no death; lifesteal5/3 is reported even when source callback drains20; both heal paths honor multiplier2 and cap20/4 while a damaging callback leaves40/24. They check receipts/counts/payloads plus final live state; wrapper healing checks its UnityEvent delta once. These are not mocks of return arithmetic. They intentionally use small exact representable amounts, so they do not establish large-float debit precision.

**NI2-01 is NOT marked fixed.** Last reviewed Core source was ResourcePoolModel SHA256 `9d26c0d655e40ecf47483512be88ed47b6f8934d371a4069cae071a5246641ec`; root queued the bounded Core correction separately. The existing API is available, but coarse-ULP Decrease/spend correctness remains an upstream integration dependency until the new receipt/source is ready. Abilities must KEEP trusting the accepted Core return contract; reverting to before/after reads would reintroduce callback attribution defects. No downstream workaround/new numeric backend is required. Final Core precision controls and Abilities receipt cases must run against the same frozen integrated source after the global barrier.

## ABIL-FINITE-TICK-ADMISSION — correct source; NI3-01 test follow-up

Both public Tick methods reject NaN, +/-Infinity and nonpositive steps before scratch/timer mutation. System Tick also rejects float domain-clock overflow before clock assignment, pending pruning, modifier/slot/pool/property regeneration. Direct ModifierEngine guards before clearing its tick scratch. Finite authored timer/accumulator limits, float clock precision, arbitrary nested Tick and callback transaction behavior are not newly guaranteed; the docs/receipt correctly state this boundary.

The nonfinite system test first advances0.25 with a real cooldown, finite DoT, regeneration and pending projectile. It proves invalid steps leave health/cooldown/duration/activity/events unchanged, then uses a valid1-second step to assert exact damage/regen/cooldown/expiry behavior. The direct modifier test proves interval phase0.25 survives invalid input and0.75 yields exactly one tick. Those are positive recovery controls. The separate finite-overflow case needs NI3-01's recovery/state controls as above; current guard code is source-correct.

## Retained actions, documentation and compatibility

Current Cast continues to use the granted slot's mutable Blueprint reference after preserving UnknownAbility/NotGranted precedence; catalog replacement only affects new grants. Captured resource ids/effective amounts, per-entry Core CanSpend, cumulative repeated-id balance checks and TrySpend payment remain. Earlier successful payments are not rolled back when a later callback invalidates payment. Clear's internal reaction route remains separate and preserved. No immutable catalog or arbitrary resource transaction was silently added.

ABIL-EDITOR-DOC is closed in owned guides: parent README links Editor.md, the legacy designer guide states every overriding radius entry must be positive and gives both counterexamples, and its outgoing canonical links remain. README/unit/modifier docs accurately describe accepted amounts versus live balance, Target/global merge/capture/counter scope, handler exceptions, native lifecycle limits and finite time admission. Serialized component fields/public signatures/sample layout/shared assembly graph are preserved; the Target APIs/counters are additive and require no new scene wiring.

The two new fixture sources contain17 methods/28 authored cases (16 receipt/time and12 Target), not discovered/passing tests. Their meta files were read; GUIDs are the declared `3b7f2e94ab804d7bac823c9e8fe56d10` and `e2c8a30fa94d4a79a73a68ce19f6b2d5`. No global metadata uniqueness check or package validator is run here. AbilitySceneRig owns/destroys its objects/restores the previous hub, but manually invokes EditMode lifecycle; real Unity enable/disable/domain reload remains separate acceptance. No unsupported NUnit attribute, helper rewrite, nested Editor edit or new dependency is introduced in this phase.

## Reviewed identities

Source-read/hash checkpoint: 2026-10-23 18:47:17 UTC. Full paths are relative to `D:/Git/NeoxiderTools/`; hashes are bookkeeping, not executable verification.

| Artifact | SHA256 |
|---|---|
| `Docs/Audits/2026-10-23/Neo.Abilities.implementation-request.md` | `b8e98ad1c8c5720ef853416e631420d6a9807b6c6d85e18c5a89253cc73dbef8` |
| `Docs/Audits/2026-10-23/Neo.Abilities.implementation.md` | `eacf585c1121054685b205253f1b1ab1c187fc205922c9a29f71290303aa9fab` |
| `Assets/Neoxider/Scripts/Abilities/Domain/Combat/DamageService.cs` | `2ad7863207fee93edff5bec9533135679ef6550f87ec4893b98f502c351064ea` |
| `Assets/Neoxider/Scripts/Abilities/Domain/Effects/Ops/HealEffectOperation.cs` | `971668ac89d6169536df7779dfb9897f385884af7111cce5a05a6e89391eec81` |
| `Assets/Neoxider/Scripts/Abilities/Components/AbilityUnitBehaviour.cs` | `9d67f03e54874c3aa8e66298a94b47429d760991d54b60008d1f5b2015b9f0d1` |
| `Assets/Neoxider/Scripts/Abilities/Domain/Events/AbilityEventBus.cs` | `c484895541866c44a7079665caa81ec4ef3bcd418f6d91a6e98d67b424fd3d3a` |
| `Assets/Neoxider/Scripts/Abilities/Domain/AbilitySystem.cs` | `04646aa2f2f2f3d3c51b54d130e2a2d6d2b62331cafc54c21167c8fa37f131dc` |
| `Assets/Neoxider/Scripts/Abilities/Domain/Modifiers/ModifierEngine.cs` | `18f1852610822812b5ec7a8fccf392a60734fb46059f85dc487a4ff5374ffa9d` |
| `Assets/Neoxider/Docs/Abilities/README.md` | `d9867a7c6e0968a7198f2dfe23b35483bd618c3734dcd1ed0a86942263d487ca` |
| `Assets/Neoxider/Docs/Abilities/AbilityUnitBehaviour.md` | `19bdcd82020d7846cde2ce15e04afd0091c4fd01f6e8fd94b0810d348a504a79` |
| `Assets/Neoxider/Docs/Abilities/ModifierDefinition.md` | `bb3bcd7ec5a7df55480e3eacf4c5a34b558bfff4ec5046af72aabea85d7a631c` |
| `Assets/Neoxider/Docs/Abilities/AbilityDesignerWindow.md` | `d4e66c707b03dbe30cad0b5c0b3e34df54ce8444a8c127cadf291d7ec5cd4944` |
| `Assets/Neoxider/Tests/Edit/Abilities/Audit20261023AbilitiesReceiptsAndTimeTests.cs` | `de3cd60fae3beb4dae545b7933f78394fd217ff2a6ce584c208719c57e30d771` |
| `Assets/Neoxider/Tests/Edit/Abilities/Audit20261023AbilitiesReceiptsAndTimeTests.cs.meta` | `ce5ba09dc6c59eb696435e96760f84cc4993d6d8e9c768b502eb169e0518c3e6` |
| `Assets/Neoxider/Tests/Edit/Abilities/Audit20261023AbilitiesTargetEventTests.cs` | `a58743744229db713bf5baab22e6e93b0d5355baabe5a40f6323460067be535d` |
| `Assets/Neoxider/Tests/Edit/Abilities/Audit20261023AbilitiesTargetEventTests.cs.meta` | `040f3cf9fa6bb3c18f04f1baf139fb9d2e9db196e2972cebf168a0dabb5a7bdd` |

## Handoff

Root should return NI3-01 to SAME `/root/audit_abilities` for the bounded existing-test strengthening/receipt update. No current Abilities runtime defect was found in the reviewed phase patch. Core NI2 remains OPEN and separately assigned; do not accept its precision semantics through this review. All28 cases, actual1/100/1000 execution/profile evidence, both Unity compilers/discovery, real scene lifecycle, optional/MIRROR dependency matrix and package/native acceptance remain UNEXECUTED. Only root can lift the global source gate after every correction/writer finishes.
