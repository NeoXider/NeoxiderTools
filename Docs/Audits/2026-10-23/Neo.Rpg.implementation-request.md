# Neo.Rpg: approved implementation request

Original actor: `neo-audit-1023-b05-03`. Target `demo` / `11.0.0`. Root has read your complete audit and plan.

Read `IMPLEMENTATION-CONTRACT.md` first. Do not execute tests/build/validators during this phase.

Keep nearest-assembly ownership. Finish bounded confirmed fixes and owned canonical documentation/backlinks. No optional broad framework is required.

## Assigned review actions

### RPG-BRIDGE-NAVIGATION (P3; approved-source-correction)

Link ConditionBridge.md from canonical RPG entry and delegate old RpgConditionAdapter.md semantics to it.

### RPG-COMBAT-NAVIGATION (P2; approved-source-correction)

Link Rpg/Combat/README from canonical RPG and old Tools/Components/AttackSystem entry; create missing Combat.meta once with unique stable GUID.

### RPG-RESOURCE-REENTRY (P1; approved-source-correction)

Build/Clear from reactive/resource callback invalidates live Dictionary enumeration; generation guard terminates stale pass after external callbacks/resolvers.

### RPG-INFINITE-TICK (P1; approved-source-correction)

Infinity delta/accumulator in tick regen cannot decrease and loops forever; reject nonfinite before loops, preserve finite pause contract.

Root decision: Both services cap1024 delivered ticks per entry/call; drop excess WHOLE backlog, retain fractional remainder. Double arithmetic/cap before cast, validate finite delta/interval/resolved amount, reduce backlog before callbacks and consume before delivery. Generation/removal/reentry guards. Normal cadence/pause preserved; document pathological catchup truncation explicitly, next normal interval recovers1tick.

### RPG-APPLIED-GROWTH (P2; approved-source-correction)

Manual/None/deferred growth unexpectedly applied by unrelated refresh; explicit applied-growth state and ApplyLevelGrowth preserve selected policy.

### RPG-INITIAL-SNAPSHOT-REENTRY (P1; approved-source-correction)

Server stat refresh pushes snapshot while _initialized false; CaptureProfile re-enters initialization and may recurse/rebuild during enumeration. Fix core readiness/commit before publication.

### RPG-BASE-MAX-CONTRACT (P2; approved-source-correction)

Live final Max cannot persist through derived refresh; assess additive BaseMax setter and v3 presence flag migration without treating v2 final Max as base.

Root decision: Approved final-effective-Max snapshot restore within existingv2: restore-call-only max lookup, saved Max before Current and percent regen; unsaved rows ordinary resolver, last-row-wins, no temporary low clamp/death. Keep BaseMax and later refresh semantics. Durable base fields/v3 deferred as distinct feature. Read NativeReview-C-contracts.md.

### RPG-MODIFIER-CONSUMERS (P2; approved-source-correction)

AddResourceCurrentFlat, AttackSpeedPercent and legacy HpRegenPerSecond incompletely consumed; settle one-shot/new-stack, next-cooldown and enabled-regen semantics explicitly.

Root decision: Approve HP legacy regen alias (canonicalHP, ignore targetId, clamped stacks, flat before percent/all existing gates) and declared one-shot Current per new/positive effective stack delta. Resolve derived limits then grant then settled OnBuffApplied; no grant for nonstack/cap refresh/remove/expiry/profile restore, signed Increase/Decrease, capture before callbacks, no replay after notification fault. AttackSpeedPercent enum-only unsupported feature documented/deferred, no invented timing policy. Read NativeReview-C-contracts.md.

### RPG-NETWORK-NAVIGATION (P3; approved-source-correction)

Link new Rpg/Network.md from canonical RPG entry.

### RPG-EFFECT-INFINITE-TICK (P1; approved-source-correction)

RpgEffectShelf status accumulator also hangs on Infinity and huge FINITE delta where accumulator-interval equals accumulator; finite-only guard insufficient. Require progress/bounded drain preserving normal cadence and explicit extreme-input policy.

Root decision: Both services cap1024 delivered ticks per entry/call; drop excess WHOLE backlog, retain fractional remainder. Double arithmetic/cap before cast, validate finite delta/interval/resolved amount, reduce backlog before callbacks and consume before delivery. Generation/removal/reentry guards. Normal cadence/pause preserved; document pathological catchup truncation explicitly, next normal interval recovers1tick.

### RPG-STAT-REFRESH-REENTRY (P1; approved-source-correction)

RpgCharacter.RefreshAllDerived stat foreach invokes reactive/stat callbacks that can RebuildRuntime. Generation guard immediately after callbacks/resolvers, including before stale NotifyChanged, not only next enumeration step.

### RPG-BINDING-INFLIGHT-DISABLE (P2; approved-source-correction)

Resource/stat binding automatic callbacks still forward after earlier listener disables/removes subscription from current Reactive snapshot; initial Current callback disable/rebind still emits old Max/Percent. Guard enabled/exact subscription generation at callbacks and each initial continuation.

### RPG-PROJECTILE-COMPLETE-HITS (P2; approved-source-correction)

C-30: fixed/configured raw hit capacity can omit eligible receivers and 3D order unspecified. Complete grow/requery/full fallback, sort distance/tie deterministically while preserving ProjectileMaxHits actual gameplay delivery cap and existing3D/2D precedence. Dense public impact sources and native acceptance required.

## Relevant full native sections

Source: NativeReview-C.md

### C-03 / P1 — ConditionBridge's active-controller regressions create inactive controllers

- Current location: `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/RPG/Audit20261023NeoRpgConditionBridgeTests.cs:26`, `Evaluate_ControllerMode_UsesAssignedControllerWithoutCharacter`, lines 128–150. Cross-owner contracts: `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Components/RpgEvadeController.cs:56` and `RpgAttackController.cs:502`.
- Trigger: all four parameterized active-controller cases run after SetUp deactivates `_root`; child controller objects stay inactive in hierarchy. Expected precondition is a ready active assigned controller without a character. Actual source result: `CanEvade` and `CanUseAttack` reject `isActiveAndEnabled == false`; the explicit readiness assertions at 137/150 fail before evaluating the adapter. The owner's plan already records this; it remains unfixed in the current fixture.
- Smallest repair: disable built-in input, finish configuration, activate the fixture root in this test before asserting readiness; add disabled-controller false/inverted-true cases. Preserve parent RPG activity gates. Owner: **Neo.Rpg.ConditionBridge / `neo-audit-1023-b01-07`**, after final `Neo.Rpg` controller contracts from `neo-audit-1023-b05-03`.
- The historical missing EditMode bridge reference is already present in current `Neo.Editor.Tests.asmdef`; do not schedule that central repair again or infer a compiler pass from JSON alone.
Source: NativeReview-C.md

### C-27 / P1 — resource/stat callbacks can rebuild live dictionaries during their traversal

`D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Runtime/RpgCharacterResourceService.cs:171–177` RestoreAll, `209–261` TickRegen, `278–304` RefreshDerived; facade `Components/RpgCharacter.cs:1221–1252` RefreshAllDerived/RefreshStat. Build A/B, subscribe first resource reactive/ResourceChanged callback (or derived resolver) to Build C=7. Setters/resolvers call external code, then the old pass continues/stale-notifies or next MoveNext throws after new entries. Stat reactive/OnStatChanged can similarly RebuildRuntime inside facade's stat foreach. Expected replacement C remains7, no stale old resource mutation/notification or invalid enumeration.

Minimum generation on Build/Clear/rebuild and checks immediately after **each** external resolver/reactive setter/notification before subsequent writes/events/MoveNext; capture current entry identity when needed. Existing EffectShelf mutation-version approach is reusable, no full transaction service prerequisite. Cover RestoreAll, continuous/tick regen, max/regen resolvers and facade stat refresh with observed C/state/event identities. Owner **Neo.Rpg / `neo-audit-1023-b05-03`**. Calling a dictionary snapshot alone would still mutate obsolete runtime objects, so it is insufficient.
Source: NativeReview-C.md

### C-28 / P1 — regeneration/status loops can fail to terminate for invalid or huge steps

`D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Runtime/RpgCharacterResourceService.cs:243–255` TickRegen and `Runtime/RpgEffectShelf.cs:274–290` Tick. A not-full tick resource or live damaging status, finite positive interval, delta +Infinity: accumulator stays Infinity after subtraction and loop never ends. Large **finite** float delta (e.g. float.MaxValue with interval0.5) also does not decrease when subtracting that interval due to float precision, so IsFinite alone is insufficient. Expected public tick returns without poisoning state or unbounded notifications; ordinary cadence and pause-crossing behavior preserved.

Minimum finite guards for delta/interval/rate/accumulator arithmetic plus subtraction-progress or explicit bounded-drain policy; sanitize/reject unsupported catch-up with documented residual semantics. Do not run known infinite repro on unfixed code. Add public tests for NaN, infinities, overflow/nonprogress finite step, then ordinary fractional/multiple ticks, callback removal and pause timing. Owner **Neo.Rpg / `neo-audit-1023-b05-03`**. This is a bounded correctness fix, not prerequisite for a simulation-clock/state framework.
Source: NativeReview-C.md

### C-29 / P2 — unrelated derived refresh applies disabled/manual/deferred level growth

`D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Components/RpgCharacter.cs:1009–1040` SetLevel gates auto-growth, but `RefreshStat:1249` always passes live `_level`; `Runtime/RpgStatResolver.cs:25–29` always evaluates affectedByLevel. Wisdom base10/+2 per level, ManualUpgradePoints or None (or autoApplyGrowthOnLevelUp=false), SetLevel(3) leaves10; apply empty buff calls RefreshAllDerived and changes it to14. Expected no growth until permitted/explicitly applied, and unrelated buffs cannot change policy. Minimum applied-growth state/level passed to existing resolver, explicit apply command if needed, correct upward/downward automatic paths; no model extraction required. Owner **Neo.Rpg / `neo-audit-1023-b05-03`**. Test all modes, deferred explicit apply, repeated refresh and profile policy; keep upgrade points separate.
Source: NativeReview-C.md

### C-30 / P2 completeness/ordering risk — projectile raw hit buffers silently saturate

Current source `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Components/RpgProjectile.cs:91–128`, HandleHitsBetween: 16 (or configured `_maxHitsPerCast`) 3D/2D hits are consumed directly with no saturation handling, and 3D hits are not distance-ordered. A segment crossing more colliders than capacity can omit an eligible receiver, especially when returned entries are self/duplicate/otherwise rejected hurtboxes; ProjectileMaxHits then does not describe all reachable impacts. Exact native membership/order requires central deterministic scene evidence; the silent incomplete-query structure is confirmed, not a measured failure/FPS claim.

Smallest next action: original owner **Neo.Rpg / `neo-audit-1023-b05-03`** adds dense-3D/2D public impact regression with ignored/source hurtboxes and near/far receivers; establish `_maxHitsPerCast`'s documented work-budget versus raw-capacity meaning before repair. Complete raw context via grow/requery or full fallback when saturated, then deterministic distance/tie and existing delivery/ProjectileMaxHits policy. Preserve actual configured gameplay limits rather than silently disabling them; expose explicit incomplete/budget outcome if intentionally bounded. Do not cache selected receiver globally. Direct/area allocating casts have complete arrays but their hit-order policy is a separate contract; changing target ordering/3D→2D precedence needs explicit acceptance.
Source: NativeReview-C.md

### C-31 / P2 — RPG bindings still deliver automatic UI output after disable/rebind

`D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Components/RpgResourceBinding.cs:117–130,161–174` and `RpgStatBinding.cs:107–110`. Neo.Reactive snapshots all live subscribers; removal during delivery does not cancel that snapshot. Earlier listener disables binding, OnDisable unsubscribes, then snapshot calls unguarded handler and emits UI output while disabled, contradicting both guides. Initial resource OnCurrent listener may disable/rebind; outer SubscribeIfReady still emits old Max/Percent afterward. Expected automatic output only for current enabled binding/session.

Minimum enabled/subscription-generation checks in automatic typed handlers and after initial output callbacks; prevent obsolete initial continuation after source/ID/rebuild change. Retain exact old-object unsubscription introduced by audit. Regression uses an earlier reactive listener plus event/text counts, and initial OnCurrent rebind/disable observing no stale subsequent fields. Owner **Neo.Rpg / `neo-audit-1023-b05-03`**. No general binding-session API is needed; NoCode's existing snapshot guards are a local reuse example.
Source: NativeReview-C.md

### C-32 / P1 — active-server snapshot capture recursively reinitializes character state

`D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Components/RpgCharacter.cs:243–269` EnsureInitialized refreshes before ready; RefreshStat:1252/NotifyResource call PushSnapshotIfServer; `RpgNetwork/RpgCharacterNetworkAdapter.cs:65–71` synchronously CaptureProfile; character `CaptureProfile:1399` calls EnsureInitialized again. Active server, enabled/resolved adapter (Damage(0) resolves it), ApplyTemplate/RebuildRuntime with stat/resource reproduces recursive Build/refresh/capture before initialized, risking stack exhaustion/dictionary replacement. Expected one completed rebuild and valid final snapshot.

Minimum core initialization/rebuild publication guard acquired **before** collection clearing, finally released, readiness only after consistent state, one final network notification after required loading/derived resolution. Adapter-only recursion guard still permits the first nested rebuild and is insufficient. RuntimeRebuilt currently precedes LoadProfile, so do not relabel it as complete-profile commit without documenting order. Owner **Neo.Rpg / `neo-audit-1023-b05-03`** first; **Neo.Rpg.Network / `neo-audit-1023-b05-06`** adds active-server final-payload regression after core repair. Do not run stack-overflow repro on unfixed tree. Snapshot coalescing/protocol envelope are separate optional work.
Source: NativeReview-C.md

### Neo.Rpg — reviewed

Full audit/plan, all nineteen actual changed source diffs, relevant complete resource/targeting and facade initialization/growth/profile/binding/projectile bodies, all three complete new fixtures and eleven guide diffs read. Profile stack/null cloning, upgrade rollback, tick-unit/derived regen, cast cancellation, exact evade lock owner, projectile preflight, legacy stack/slow factors, generic revival and invalid upgrade rejection are locally coherent. C-27–C-32 remain; C-30 is confirmed structural incompleteness requiring native/contract resolution, not claimed deterministic result. Forty-two prepared cases assert actual rates/stacks/HP/locks/events/targets and stale-object detach; objects/assets owned, no new shared config. Play fixture uses tagged Player and world position20000; isolate external actors/camera/NavMesh state, native timing and provider boundaries remain unexecuted.

Contract decisions separated from mandatory fixes: live SetMaxResource writes derived Max while resolver starts from BaseMax, and v2 CaptureProfile records final Max. Explicit160 can refresh/load back100; treating saved final Max as base would double-count modifiers. Additive base-max setters/versioned presence field are sensible optional v3 design, but legacy setter semantics/v2 migration need root decision. Likewise AddResourceCurrentFlat, AttackSpeedPercent and legacy HpRegenPerSecond lack complete consumers; one-shot grant versus refresh/load and next-versus-current cooldown need product contracts before implementing. Do not advertise these as implemented, or make full transactional service prerequisite for guards/growth/init fixes. Effect UTC versus simulation time is a documented legacy contract; opt-in simulation clock is separate.

Aggregate: character derived refresh iterates every stat/resource and publishes each change; adapter currently clones/sanitizes/serializes the full profile for every NotifyStateChanged, plus wrapper calls can add another capture. Cost scales with notification count × full profile size; actual allocations/bytes should be measured, no FPS invented. Target selection allocates complete 3D/2D overlap arrays/list/receiver set per request and deduplicates receivers after querying. Identical raw context/epoch can share complete overlaps, while selection mode/dead/action status remain per caller; distinct origins/ranges/masks/scenes/trigger policies stay separate. Unique projectile segments and target LOS cannot be merged. Preserve mask versus receiver/tag/blocker decisions; target-layer filtering already excludes unrelated geometry by contract, do not rebrand it as a wall/LOS blocker query. Aura queries use authored transformed center/absolute scale and per-call receiver dedup; coincident identical contexts may share discovery while each aura's damage remains additive.

## Completion receipt

Write `Docs/Audits/2026-10-23/Neo.Rpg.implementation.md` with exact actions/files/additive API/dependencies, migration details and authored acceptance (UNEXECUTED). If blocked on another additive API, complete independent files and identify it precisely for root continuation. Do not edit shared review-action/manifest/other actor reports.

## Root accepted NI9 three bounded residuals and guide wording

ROOT personally FULL-read all205lines NativeImplementationReview-9.md and accepts NI9-01/02/03/P2. SAME original RPG owner correct all3 in one resume, preserve current62new authoredcases and approved contracts. (1) UpgradeStat capture runtime generation before UpgradePointsState reactive debit; after callback stop obsolete continuation BEFORE replacement investments/count/runtime refresh. ApplyTemplate/RebuildRuntime public controls keep replacement S7/investment0/profile and ordinary upgrade cost/stat/events control; no rollback into replacement, explicit superseded return policy. (2) ResourceService.Spend positive zero-effective stored debit at1e8 cost1 rejects BEFORE reactive/event/pause/facade-publication mutation. Share clamped candidate admission, preserve zero/unknown/nonfinite precedence; cost5 stored8 and ordinary fractional remain valid (no exact nominal equality/new numericbackend). Test service+facade state/reactive/events/regenphase/rejection and ordinary controls. (3) RpgResourceBinding/RpgStatBinding exactsession predicates first require cached character Unity-live/nonnull BEFORE source getters, retain enabled/id/version/ReferenceEquals/detachguards. Genuine earlierlistener DestroyImmediate(source.gameObject) capturedtail tests for resource/stat with verified two listeners/live ordinary precondition, no stale output and normal/rebind controls. P3 owned InternalTypes.md62 currently says no callbacks for invaliddelta: narrow to tick-damage/regen DELIVERY; independent UTC expiry still runs (existing fixture expects). No general transaction/sessionframework, no unrelatedrewrite/nestedassemblyedits. Source/regression/docs/receipt only, no execution until globalfreeze.

## Snapshot3 actual RPG-ACTIVE-BINDING-POSITIVES

SAME original source actor, gpt-6.1-sol/max. Root closed execution after all owned Unity jobs stopped. NO compiler/Unity/checkers/tests until ALL writers finish and a new global freeze. Root personally read all98 graphical Edit failure messages/counts and all43 Play groups, independent full-edit-failure-triage.md and full-play-failure-triage.md. Read exact assigned cases in Local/Audits/assembly-2026-10-23/unity/final/s3/u63/Receipts/22-full-edit-graphics.xml (SHA7c6d24e1...) and21-full-play-graphics.xml (SHA2b152164...) plus Local/Audits/assembly-2026-10-23/full-edit-failure-action-index.json. PRIMARY b03-04 exclusively owns ALL helper moves/extractions, Fixtures assembly/shared refs and files in Local/Audits/assembly-2026-10-23/runtime-fixture-relocation-reservations.json. Check map before every edit. Do not race/extract helpers or change reserved fixture callers; work only disjoint source now, explicitly report deferred reserved work for root to resume after release. Preserve public/serialized API/settings/GUIDs and EVERY substantive positive/negative/count/ownership/payload oracle. Establish a real positive precondition before inferring runtime defect. Native mode failures cannot be hidden with ExecuteAlways, manual Update/private lifecycle as native evidence, blanket Ignore, expected-error whitelist, arbitrary tolerance/default change, reflection UserCode bypass or lost cases. Use supported TOP-LEVEL UnitySetUp/UnityTearDown/UnityTest Enter/ExitPlayMode for true lifecycle, or existing public API for explicit pure contract tests; nested ExitPlayMode is unsupported. No user/foreign scenes/providers/sessions may be reset/deleted; only owned test resources. No other owners/shared asmdefs/vendor314 migration targets, no children/commits/push. Update own canonical docs if actual contract changes and own honest implementation receipt; no test pass inferred from source. Stop writes when done; name unresolved native evidence. Immediate disjoint scope: ImplementationBindingTests, NOT reserved ImplementationCharacterTests. Inactive view.Character assignment never attaches binding in Edit; activate then use actual PUBLIC Character setter after output observer registration as appropriate. Retain exact2listeners, initial50, ordinary51, destroyed managed52/no old tail, rebind7/8 and old-state99 suppression. Add the same real old-tail positive precondition to Resource/Stat InFlight cases to remove vacuous empty-output pass; InitialCurrentObserver must trigger public active binding AFTER observers, keeping max/percent counts. No production changes needed without contrary proof. Character init auto-load event sequence case is reserved for helper extraction; defer that caller until primary release, then public initialization or genuine native Awake with exact rebuilt/loaded/snapshot ordering, no synthetic lifecycle acceptance.

## Source3 released caller RPG-POST-RELOCATION-INITIAL-PUBLICATION

Continue SAME original actor/session gpt-6.1-sol/max. General execution/freeze remain FALSE. NO Unity/compiler/test/checker/install/service/children/commit/push before ALL writers finish/newfreeze. Primary b03-04 has SOURCE-FINAL completed its54 helper relocations; root FULL-read dedicated21-line audit/17-line plan/112-line mapping and source-final declaration. ROOT NOW RELEASES ONLY your original module fixture CALLER files for the bounded behavioral correction below. The existing moved helper files under Tests/Fixtures, shared asmdefs/refs, original GUID/type mapping and extraction stay PRIMARY-EXCLUSIVE; do not relocate/recreate helpers or undo aliases. Read current source after relocation and actual source3 XML before patching. No production API/friend/serialized settings changes just to hide a fixture precondition. Retain every exact substantive positive/negative/count oracle, no blanket Ignore/expected-error/no-op/private fake lifecycle/ExecuteAlways/manual Update as native acceptance. Actual native callback claims require supported top-level Play transitions or real PlayerLoop/physics; pure public C# contract tests must say what they cover. Preserve UTF8/no-var, Samples~ and protected DocsEn.meta; no314 migration targets. Update honest own source receipt including deferred closure and stop. Proposed correction is unexecuted until later native run. Previously completed BindingTests source corrections retained. Your ImplementationCharacterTests caller is now released; its snapshot probe moved runtime-compatible. Actual initial auto-load case160 expected rebuilt/loaded/snapshot but0 events; arrange valid init trigger via genuine native Awake/public initialization with configured inactive graph, observer registration before trigger, same old saved data/generation and EXACT rebuilt->loaded->snapshot notification count. No direct private lifecycle simulation or weakening event order; distinguish old helper-null from production publication defect. Do not move probe or broaden APIs. Read other current owned XML cases; keep all9 NI9 guards and completed live-binding controls.
