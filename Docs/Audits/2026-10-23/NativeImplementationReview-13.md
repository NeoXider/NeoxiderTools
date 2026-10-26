# Native implementation review 13 — StateMachine / Health / Level components

Status: **COMPLETE — scoped source implementations coherent; one StateMachine test-strengthening handoff; execution UNEXECUTED**. Independent source-only QA, 2026-10-24, same native `gpt-6.1-sol` / `max` session. Only this report was written. No product/docs/tests/other-report changes, Unity/MCP/compiler/checkers, services/process management, children or commits.

## Disposition

No new runtime defect was confirmed in the three assigned scopes. StateMachine implements the approved default per-machine duration and bounded lifecycle dispatcher without an authoring option or public framework. Health's initial preparation now covers later pools before the first observer. Level retains its reviewed runtime and adds a substantive native-lifecycle regression source with isolated persistence.

**E13-01 below is a root-accepted meaningful acceptance-source gap, not a newly inferred runtime bug.** Return it to SAME StateMachine owner to prove the post-failed-target generation guard with a constructor-reentry failure and ordinary fallback positive control. No further runtime rewrite is requested from this review. Health and Level have clean scoped source dispositions; execution and integrated dependency evidence remain open.

## Source-final / still-writing gate

Read all three complete latest receipts before source inspection, and their complete dedicated requests. Intake statuses and hashes:

| Module / original owner | Receipt status at intake | SHA256 |
| --- | --- | --- |
| Neo.StateMachine / `neo-audit-1023-b06-02` | SOURCE-FINAL owned runtime/docs/regressions; receipt identifies two legacy-fixture migrations as another owner's dependency | `ca68bc36907be63aa4403d7426d3f7f0fcdb46c813241fb1f0fc5bf9f40e974c` |
| Neo.Core.Resources.Components / `neo-audit-1023-b01-05` | source-final; acceptance UNEXECUTED; no additive API dependency | `8a06bf496d6212d3ee9ca6c69bd30df1c033f777b976671c6c116fdcc2e1ede7` |
| Neo.Core.Level.Components / `neo-audit-1023-b03-02` | source-final assigned request; no additive API dependency | `90dbe6f037a2cd0acdbfe94275797cfa0fc0c5a322e16922791c3263854a30a2` |

The shared state file still labels these owners source-running. Read-only tasklist snapshots at intake and completion found their recorded initial/recovery PIDs absent: StateMachine 65200/72280; Health 100484/63408; Level Components 107460/70244. Both snapshots are recorded-process observations, not exhaustive proof about every unrelated process. The reviewed receipts, four critical StateMachine runtime sources, Health and Level sources and new/changed fixtures retained their intake hashes at completion. No assigned moving source scope was accepted. The root handoff and exact source-final receipts establish this scoped gate; no global source freeze or execution permission is inferred.

The primary Editor test owner has meanwhile integrated the two legacy calls: current `Assets/Neoxider/Tests/Edit/StateMachine/StateDurationPredicateTests.cs:78` and `:92` assert `machine.SetCurrentStateEnterTime(Time.time - 10f/-60f)`. This was observed by a bounded reference read and reported to root. The runtime receipt's pending note therefore describes an earlier integration snapshot. That separately owned fixture is not re-approved as a frozen primary-test artifact here; root handles its receipt/gate. StateMachine.Editor's NI1-approved topic remains excluded, and no serialized timing option is needed.

## E13-01 — strengthen failed-target reentry acceptance through public state/events

**Owner:** SAME Neo.StateMachine / `neo-audit-1023-b06-02`. **Source branch:** `Assets/Neoxider/Scripts/StateMachine/StateMachine.cs`, `EvaluateTransitions` lines 409–417, following false return from `TryApplyTransitionTarget` / `ChangeStateByType` lines 555–606.

The source correctly checks generation after a failed target application, so a constructor/lifecycle callback's newer explicit choice cannot fall through into a later rule. The current fixtures do not directly prove that branch. `UnusableTarget_DoesNotBlockLowerPriorityTransition` uses object/abstract/no-parameterless types rejected before construction; `LifecycleException_ReleasesDispatcherAndDiscardsDeferredRequests` invokes public ChangeState directly. Neither creates a failed automatic target that has already changed the active state.

**Root-accepted bounded strengthening:** extend an existing owned fixture, without another fixture/framework or a private-generation oracle:

1. Start in A; register high-priority A → badB and lower-priority A → D. badB's public parameterless constructor performs a bounded one-shot `machine.ChangeState(C)` and then throws. Invoke public EvaluateTransitions. Assert CurrentState is the exact C, C's legitimate lifecycle/state/event transcript is retained and D never enters. Observe public instances/counts/events, not a fabricated internal generation.
2. Positive control: the same failed constructor without an explicit reentrant choice must allow the ordinary lower-priority D transition. This distinguishes correct supersession from an implementation that aborts every constructor failure.
3. Clean up all owned constructor hooks/static test callbacks in finally, retain successful ordinary lifecycle and later-command recovery controls, and avoid recursive/unbounded failing fixtures. Preserve the existing gated target-resolution error behavior and no-rollback contract.

Root accepted this test-strengthening gap during review and will return it to the SAME actor. There is no requested runtime change: current lines 414–417 already have the intended source guard. The actor should update the source-final receipt with the authored cases and current legacy migration status. All such acceptance remains **UNEXECUTED**.

## StateMachine — source review

Read complete StateMachine (638 lines), evaluation context (113), predicate (603), transition (306), both complete scene wrappers (325/406), StateData and StateMachineData. Read the full baseline-relative deltas for the four phase files; larger base diffs include retained earlier audit corrections and are not mislabeled as all new work. Read all four current runtime/wrapper fixtures and their support types, including every new case/helper, plus all nine owned current guides.

### FIFO, payload ownership and bounded work

`ChangeState` and `Stop` enqueue on one private per-machine queue. While a lifecycle request is executing, a nested request returns to its caller without recursively exiting the still-current source. The outer loop drains FIFO after the current request's full notification sequence. Each accepted request advances the machine generation immediately, including still-deferred choices, rather than waiting for commit.

`ApplyLifecycleRequest` captures `previous` and `newState`. A → B keeps A as CurrentState for A.OnExit, then emits exited(A), sets/anchors B, calls B.OnEnter, entered(B), changed(A,B). Queued Stop therefore cannot turn B's own outer payloads into null/stale instances. Stop intentionally clears CurrentState before its exit hook and publishes captured previous/null. Initial entry still emits exited(null). Same-instance requests outside dispatch and idle Stop remain no-ops; an A request made while departing A is checked when dequeued and can re-enter A after B. Repeated queued Stops become no-ops after the first terminal stop and do not replace PreviousState with null.

The drain increments processed count before every Apply call, so queued no-ops consume the **128 processed-request** budget too. Excess pending requests are warned through the existing debug gate and cleared; the last completed state remains. Finally clears pending references and releases the guard on success, cap, warning fault or lifecycle exception. Completed exit/entry work is not rolled back; later stages after a fault are not guaranteed. A failing enter can leave an assigned partially entered state, as the documented no-rollback contract permits, and a later explicit Stop/change can recover.

This is a processing bound, not a hard bound on arbitrary user callback execution or the size of a callback's finite enqueue burst. Queue capacity can retain a prior peak. It does not make self-recursing predicates/custom code safe or promise fixed frame time. No general scheduler/session system or global manager was introduced.

### Generation reconciliation and evaluation

EvaluateTransitions captures the actual state and generation; after predicate/evaluation-listener callbacks it rejects old results even if A departed and the exact same cached A instance returned. CanTransitionTo and TryChangeState have the corresponding checks, including choices queued during an entry callback. Existing virtual predicate dispatch and priority/source filtering remain in use. A failed target returns false and the post-attempt generation guard prevents later fallback over a newer callback choice; E13-01 is the missing focused source control for that implemented branch.

Usable ordinary CLR targets still require a concrete closed compatible class/public parameterless constructor; assets use ToStateData. The existing unusable-target fallback remains. Direct registered rule/source mutations keep their documented unregister/register and next-view behavior; this review does not add speculative caching that skips side-effecting custom predicates.

### Default duration, clock/addressed API and nested context

Every constructor, including the unchanged optional two-boolean constructor used by wrappers, now owns `currentStateEnterTime`. The additive three-argument constructor accepts `Func<float>`; null retains scaled Unity Time.time. Entry samples that provider and evaluation supplies the active anchor plus a sampled time. Each machine therefore uses its own clock even when both load the same transition/predicate instances from StateMachineData.

The duration scope is a separate thread-static stack from the scene owner/override stack. Entry-hook and evaluation callers push inside their operation and pop in finally. Nested machine evaluation or a caught inner predicate/entry-hook failure restores the enclosing timing; outer failure restores standalone timing. Reset clears both stacks/current values, including the duration addition, while retaining the existing compatibility branch/partial context type. Actual domain-reload-disabled behavior still needs execution.

Built-in StateDurationPredicate consults the current scope in its existing EvaluateInternal, preserving base virtual Evaluate(IState), composite child dispatch and one inversion. Machine entry still invokes custom OnOwnerStateEntered hooks. Its scoped built-in hook no longer overwrites the predicate's standalone timestamp, so one machine does not retime another or a standalone caller. AND/OR/NOT forward hooks and evaluate children through the public virtual method; no graph clone or bypass of custom override dispatch was added.

Outside a machine scope, Evaluate()/Evaluate(IState), SetEnterTime and direct OnOwnerStateEntered retain standalone Time.time semantics, including lazy initial anchoring. SetCurrentStateEnterTime edits only one active machine anchor, returns false while stopped, does not replay entry/custom hooks and does not change the scene wrapper's independent display elapsed time. Its clock domain and the intentional 11.0 post-entry setter migration are documented. No new serialized field/authoring option or isolation of arbitrary custom mutable predicates/actions is claimed.

Wrappers keep owner/override context around public state changes, ticks, Stop during disable/destroy, and reload. Core calls through the exposed StateMachine property still bypass that scene scope, as documented. The queue drains within its outer wrapper call/context. Scene-object references remain on wrappers, with assets referring to context slots; no new scene reference is stored in ScriptableObject configuration.

### StateMachine authored acceptance — UNEXECUTED

Current owned source expands to **64 cases**: retained runtime 19, current wrapper 17 (13 retained + 4 new), new lifecycle 13 and new duration 15. These are source counts, not discovery/results.

| Family / current fixture | Meaningful public oracle / positive control | Boundary |
| --- | --- | --- |
| Exit requests C or same A (2 lifecycle cases) | Exact exit/exited/enter/entered/changed FIFO transcript; source exits once; final chosen instance and PreviousState | Bounded one-shot callback, no stack-overflow fixture |
| Repeated entry/exit Stop; Stop exit requests B (2 cases) | Entered B finishes before one terminal stop; Stop hook sees CurrentState null; later initial exited(null) ordering retained | Captured payload and idle/no-op behavior, not only private queue counts |
| A → B → same A from predicate/listener (4 cases) | Evaluate/Can/Try retain A and exact entry/exit counts; addressed checks return false | Generation behavior proven by state identity/lifecycle rather than a private token |
| Still-deferred predicate choice inside entry (2 cases) | Can/Try reject old result before deferred B commits; final B enters once | Distinguishes request acceptance from eventual commit |
| Cyclic cap (1 case) | Self-bounded 140-request fixture observes 128 enters/127 exits, then later Stop/change gives one new entry with no old pending work | Does not claim a hard callback CPU/memory budget; no-op budget is clear in source but this test uses actual state changes |
| Exit/enter fault cleanup (2 cases) | Pending C never enters, exception visible on direct ChangeState, later Stop/change succeeds | No rollback promise; automatic failed-target choice branch is E13-01 |
| Shared StateMachineData direct/AND/OR/double-NOT × inversion (8 duration cases) | A enters 0, B enters4; at5 only A transitions; at9 B; same configured transition object retained | Default core isolation, no graph clone |
| Re-entry / standalone-addressed split (2 cases) | Only re-entered machine restarts; standalone setters still work; addressed update changes only one active machine, false when stopped | Includes successful transition and standalone positive controls |
| Nested inner result/fault, outer fault, entry-hook fault (4 cases) | Outer timing restored after nested call; standalone future/past anchor has correct result after exceptions; next evaluation/entry works and custom hook counts remain exact | Indirect public timing oracle, not synthetic internal stack state; native reset still unexecuted |
| Custom virtual predicate (1 case) | State overload receives the active instance; raw/virtual counts exactly one; inversion permits the expected transition | Protects existing extension contract |
| Both wrapper variants duration and enter-disable (4 new wrapper cases) | Shared default data with different addressed anchors; real target choice; owner/exit context; exact entered-B/changed then stop transcript, one exit | EditMode Update/OnDisable SendMessage and action simulation; **not real PlayMode lifecycle proof** |

All current new fixture bodies were read, including owned object/asset finally cleanup and public state/event assertions. Helpers use a concrete named generic host component and retained state types; no new test assembly reference is needed. The wrapper's explicit SendMessage after SetActive is honestly described as simulation. Native disable, parent deactivation, destruction/re-enable/reload, pending lifecycle work during actual native callbacks, context access after native destruction, and reset with domain reload disabled remain later acceptance routes; source event order cannot prove those engine observations.

The nine complete guides consistently document code/core and scene workflows, 128/FIFO/fault limits, per-machine default duration, standalone migration, custom shared state and separate display timing. Canonical README links nested Editor.md without rewriting the separately owned editor topic. Global StateMachineLog.Enabled ownership and broader custom-state factory/isolation proposals remain separate limitations, not prerequisites or silently implemented features.

## HealthComponent — source-approved initial cross-pool preparation

Read the complete current 445-line HealthComponent and 441-line/20-case fixture, its complete canonical guide, dedicated request/receipt and source delta. The current runtime/fixture/guide hashes exactly match its source-final receipt. Earlier native-A corrections remain retained; the implementation phase adds the initial silent pass and private notify switches, not a public API or persistence framework.

EnsureInitialized builds the model and applies optional saved state before `SyncAllReactive(notify: false)` at line 208. SyncReactiveFor creates missing Current/Percent/Max reactive objects, assigns all three values without notifying, and returns when notifications are suppressed. Therefore later pools are prepared before the first HP listener. Model listeners and initialized guard are installed before the forced initial publication, preserving public API reentry without recursive rebuild. Initial per-pool current/percent/max order and final OnPoolsChanged remain.

The later forced pass re-reads the **current model** for each pool rather than replaying a staged snapshot. A first HP observer's Mana spend updates its mirrors normally, and the later Mana initial publication exposes that updated balance. Normal model mutations and explicit Load keep their prior notification paths. Explicit Load is still separate max/current application, not atomic; valid unique ids are required and legacy duplicate precedence is not changed. No silent change to configured list lifetime, add/remove behavior or authoring numeric validation is introduced.

### Health authored controls — UNEXECUTED

The five new expanded cases are substantive:

- Four authored/saved × present/missing Mana-state cases observe HP's first public reactive value, all Mana convenience/Inspector mirrors and model getters. Authored values are 12/48/0.25; saved values 21/56/0.375. Missing objects must exist before first HP observation. Nested/repeated initialization is called, then an exact current/percent/max/list-order/ready transcript is asserted outside the reactive listener.
- Callback-spend case observes Mana12 before spending5 from HP's first notification, success/reason and final model/mirror7 with matching percent. Both the normal spend and later initial Mana notification must expose7, and ready is singular. A fix that merely stages all pools then replays stale Mana12 would fail it.

The retained other 15 cases/public event, load/JSON and mutation/no-op positive controls remain. Reflection configures Inspector fields rather than fabricating runtime ownership. Objects start inactive with autosave disabled and are owned by the fixture. Persistence uses unique keys on the ambient provider and deletes only that key; this is **not** a claim of swapping/restoring a private in-memory provider. Its existing isolated/exclusive runner requirement remains relevant. No DeleteAll/foreign object deletion or unsupported NUnit serialization attribute is added.

Remaining evidence includes real Awake/Update regeneration, loaded UI/event serialization, save/disable/re-enable and destruction detach on final Core/Reactive/Save dependencies. Per-pool lookup remains repeated linear work, so initial synchronization and active emissions retain their existing O(P²) wrapper comparison lead; the extra silent pass is initialization work. No per-frame global scan or performance pass is claimed. A catalog/index/store redesign was correctly left out.

## LevelComponent — runtime preserved; focused lifecycle source approved

Read the complete current 337-line runtime, full new 166-line Play fixture/provider/helper/teardown, complete canonical guide, dedicated request and receipt. Runtime SHA256 remains `616dd023...` as declared. The centrally integrated 14-case Edit fixture SHA256 also matches the request/receipt; its prior native-A source disposition is retained by identity rather than presented as a new full fixture audit. No new runtime patch was assigned or manufactured.

The new test drives an inactive, configured component through actual activation/deactivation and yields; it does not invoke private Awake/OnDisable or replace initialization with a manual call. The public SaveProvider routing, Level model/JSON and OnProfileLoaded/Saved outputs are exercised. A private in-memory ISaveProvider records exact reads, flush counts and flushed JSON; it is a persistence seam, not a copied level algorithm.

The authored positive-control sequence is meaningful: load250XP/level3 once with zero saves; add25 and deactivate to flush275XP/level3/no cap; overwrite memory storage with stale250; reactivate and require one total read/load and live/reactive275/level3/25 remaining; deactivate again and require exactly the second save/event with275. A test that only counted OnEnable would not distinguish a hidden reload; this stale-profile control does. All assertions are outside provider/event callbacks.

The fixture captures the exact prior provider, uses owned dictionary storage, disables autosave before destroying its owned host, waits for native destruction, and restores that exact provider in finally. It does not mutate/delete external saved keys or leave destruction autosave targeting the restored provider. ISaveProvider's unused typed operations deliberately throw; Level's reviewed path uses only string storage/Save. Global provider substitution and yields still require the explicit exclusive/isolated runner; no concurrent ambient game state is claimed safe under that substitution.

Documentation accurately describes early subscription, lazy initialization, incoming-cap restoration, nontransactional reentrant notifications, explicit pre-model Save/Load limits, downward OnLevelUp compatibility, enable retention and assembly/backlink requirements. C#/scene/NoCode workflows remain. The LevelNoCodeAction result description is a dependency statement, not fresh approval of its separately owned source. Snapshot/store/curve redesign remains a proposal.

The Play case is **authored UNEXECUTED**, not native lifecycle evidence already obtained. It complements the retained 14 Edit cases; actual frozen-snapshot execution and integrated model/curve/reactive/save behavior remain root's acceptance work. There is no new Update/scene-search loop in this adapter.

## Scope, execution and handoff

StateMachine's bounded dispatcher/default timing source and both component scopes are coherent. Queue only E13-01's focused two-path regression strengthening to the SAME StateMachine actor and refresh that receipt; do not add a serialized timing option, public framework or duplicate fixture. The two primary legacy setter migrations are already observed integrated and remain the primary owner's artifact. Health/Level need no new source correction from this review.

After every writer finishes, root must compile/discover the frozen integrated snapshot on installed Unity 6.3/6.6 and execute the current StateMachine 64+strengthening cases, Health20, Level14 Edit+1 Play plus relevant legacy/dependency cases. Counts above are authored source enumeration. Native scene lifecycle, queued disable/destroy/reload contexts, domain reset, actual saved UI/serialization and real regeneration/scheduling remain unexecuted. Current references must be compiled; metadata/GUID/UTF-8/package checks belong to the later barrier. No source reasoning or prior historical check is converted into a runtime pass.

Actual reviewer operations: source/doc/request/receipt/diff reads, relevant reference enumeration, recorded-PID read-only process snapshots and SHA256 bookkeeping. Only `Docs/Audits/2026-10-23/NativeImplementationReview-13.md` was written. No compiler/Unity/MCP/test/discovery/checker, service/install/process management, child agent, Git mutation, product/docs/tests or other-report edit. All product execution remains **UNEXECUTED**.

## Reviewed snapshot fingerprints (SHA256)

Paths are repository-relative. Full body/delta versus identity-only coverage is distinguished above; hashes are read bookkeeping, not validator results. Receipts and critical source/fixture hashes were unchanged at completion.

| Artifact | SHA256 |
| --- | --- |
| `Docs/Audits/2026-10-23/Neo.StateMachine.implementation.md` | `ca68bc36907be63aa4403d7426d3f7f0fcdb46c813241fb1f0fc5bf9f40e974c` |
| `Docs/Audits/2026-10-23/Neo.Core.Resources.Components.implementation.md` | `8a06bf496d6212d3ee9ca6c69bd30df1c033f777b976671c6c116fdcc2e1ede7` |
| `Docs/Audits/2026-10-23/Neo.Core.Level.Components.implementation.md` | `90dbe6f037a2cd0acdbfe94275797cfa0fc0c5a322e16922791c3263854a30a2` |
| `Docs/Audits/2026-10-23/Neo.StateMachine.implementation-request.md` | `8c6a1ecf6d223ac9a00b9bb04ba5b8801cdb4be61653b6ca4d8d7df5a617675c` |
| `Docs/Audits/2026-10-23/Neo.Core.Resources.Components.implementation-request.md` | `d4d94fd60977a2efe7241010b93f3c2e74b8e1fd760f336045612477bf4a7672` |
| `Docs/Audits/2026-10-23/Neo.Core.Level.Components.implementation-request.md` | `3e0be7dd9dcf3efd63646db1709718db026ba600f9b986cf2217d40b16055d26` |
| `Assets/Neoxider/Scripts/StateMachine/StateMachine.cs` | `26211741759424ad2e06943b0f918198823f15259feae48360e1a3909f84d50c` |
| `Assets/Neoxider/Scripts/StateMachine/StateMachineEvaluationContext.cs` | `122e201c9ede12d4b627fcf8f0b90779a097124b942969fafb55ae9a6321c265` |
| `Assets/Neoxider/Scripts/StateMachine/StatePredicate.cs` | `9c4aaa562c8d59047448d6b0c89f29b91c375ce4d55f720a06489af349a58669` |
| `Assets/Neoxider/Scripts/StateMachine/StateTransition.cs` | `ffadd04439d8106f8f0cff88b0cd3efb818b9c161f873da06861d7aaddf96e30` |
| `Assets/Neoxider/Scripts/StateMachine/StateMachineBehaviour.cs` | `1cb0f135d9f1c7753e492ff874ebd56ea2179972d470b70d9c41be92465ecf1f` |
| `Assets/Neoxider/Scripts/StateMachine/StateMachineBehaviourBase.cs` | `e36f98caa0f70092aa4ccd04638abd30cbdab0492f8d54c55a2bc35d5c2f3225` |
| `Assets/Neoxider/Tests/Edit/StateMachine/Audit20261023NeoStateMachineLifecycleDispatchTests.cs` | `0c4a9e92fcd1537cadc04fdb03bb5ef676cc39d5bba9da04a91415f9dc3ed9a0` |
| `Assets/Neoxider/Tests/Edit/StateMachine/Audit20261023NeoStateMachineDurationContextTests.cs` | `25c4004eb0bb6c2639654a3931d5e6fca76b809cec97174d6137aa61a3bfeb3e` |
| `Assets/Neoxider/Tests/Edit/StateMachine/Audit20261023NeoStateMachineBehaviourTests.cs` | `0165e4b7211df426315dff8f12421ad8d500f4d0cd0cbb839e4bee5b2fcadc78` |
| `Assets/Neoxider/Tests/Edit/StateMachine/Audit20261023NeoStateMachineRuntimeTests.cs` | `655f2fee8f5c904365b24b53c0008ff7d68d6a815dfac53a91c7705cd537633b` |
| `Assets/Neoxider/Tests/Edit/StateMachine/Audit20261023NeoStateMachineLifecycleDispatchTests.cs.meta` | `bf373e5a25345bfd02de69f4153402c939c5f321a8a674d99bcf56e06af4a2aa` |
| `Assets/Neoxider/Tests/Edit/StateMachine/Audit20261023NeoStateMachineDurationContextTests.cs.meta` | `006737b150d4fe1e52400672299c9f66b9894d136a2ed1c46a8df1e25c4c5eb5` |
| `Assets/Neoxider/Scripts/Core/Resources/Components/HealthComponent.cs` | `dae0c8ea13525cc2dff628750e345e42714a9ac47e0e977673346fa21eff20b1` |
| `Assets/Neoxider/Tests/Edit/Core/Audit20261023NeoCoreResourcesComponentsHealthComponentTests.cs` | `e0eb280f51ce356cd29d199944075d0a8ddbe9e2a735e6684914fcb893ce67ce` |
| `Assets/Neoxider/Docs/Core/Resources/Components/HealthComponent.md` | `7958f0bcab0ccbc2fa9bd9453938a848940581f5c6857e2418df35da9c1a0aca` |
| `Assets/Neoxider/Scripts/Core/Level/Components/LevelComponent.cs` | `616dd0235e0c7f2de3b4a074739dd135fb6227b164910fbb52066aa93fa31cab` |
| `Assets/Neoxider/Tests/Play/Audit20261023NeoCoreLevelComponentsLifecycleTests.cs` | `8d842c55c50a6d81b441a5b35b37019637458c34154517a917fbc23ce4c7a210` |
| `Assets/Neoxider/Tests/Play/Audit20261023NeoCoreLevelComponentsLifecycleTests.cs.meta` | `bc473b566b12957f3d908aa0cf72a6e764836116a2b932b6933510785f1eb9ec` |
| `Assets/Neoxider/Tests/Edit/Level/Audit20261023NeoCoreLevelComponentsTests.cs` | `ef62319b7de02209d5fb01dc4632bc407c9e4c84c1498683838d663def8f1346` |
| `Assets/Neoxider/Docs/Core/Level/Components/LevelComponent.md` | `8fcb8fd107d4e2b8a3955de7ffe7adde42810c660d2df04eb2469bd8699debcf` |
| `Assets/Neoxider/Docs/StateMachine/README.md` | `01cddb705935e971661645c23f84b1dc08fd832d03665e1e6230eb0e913e500c` |
| `Assets/Neoxider/Docs/StateMachine/StateMachine.md` | `a3fdd1a0adc2f69c154fe31021b9611ebd554f55ddcdcd50ecbc5b00a20fc750` |
| `Assets/Neoxider/Docs/StateMachine/StatePredicate.md` | `012df82eeed6ad7d005100021aaa06c5e431af73c24d43358a14a098941f259c` |
| `Assets/Neoxider/Docs/StateMachine/StateTransition.md` | `bca2aef6e14f623d1fc7deb05c818dbed529b6c38f2f423e773d9b10fe466662` |
| `Assets/Neoxider/Docs/StateMachine/StateMachineEvaluationContext.md` | `d66d33b767192512ce9142f95fcfb841f5b3958acb9986df24462f4641cd489a` |
| `Assets/Neoxider/Docs/StateMachine/StateMachineBehaviour.md` | `023532ef6f60761ede063d5e4d4b93eaa6181162ed9e904126d02f7372a7ecd2` |
| `Assets/Neoxider/Docs/StateMachine/StateMachineBehaviourBase.md` | `5e2e4128ea9c9cbfd8bde1101d3553230b3bcd8a5bbd43d74b10d629dcaaeb10` |
| `Assets/Neoxider/Docs/StateMachine/NoCode_StateMachine_Usage.md` | `0b0ebdf295c7bc9bdbf4aed7dcb2f7382d2ea8fd79ea132c82ce59f47dfc743e` |
| `Assets/Neoxider/Docs/StateMachine/NoCode/StateMachineData.md` | `f6a069acc4d8bdf887a8c59d3847f9302713af9be4523bb76bae161a812127e2` |
