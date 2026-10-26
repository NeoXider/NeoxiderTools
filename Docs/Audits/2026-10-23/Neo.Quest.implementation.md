# Neo.Quest: implementation receipt

**Status: SOURCE-FINAL.** Branch `demo`, target `11.0.0`; same actor `neo-audit-1023-b05-01`. Implementation contract, exact request, complete own audit/plan, manifest entry and relevant current source read; `globalReviewApproved: true` observed. Historical audit and plan remain unchanged.

## Additive API ready for dependents

Implemented in `Assets/Neoxider/Scripts/Quest/QuestManager.cs`:

```csharp
public bool UnregisterQuest(QuestConfig expected);
```

Returns true only when `expected` is the exact current registered instance for its nonempty ID. Null, unknown, repeated removal and a stale instance with the same ID return false. Preserves all QuestState objects, statuses, progress and persisted data; emits no events, saves nothing and destroys nothing. Existing RegisterQuest remains first-registration-wins.

**Neo.Demo handoff:** call `manager.UnregisterQuest(ownedRuntimeConfig)` before destroying that config. A replacement config registered under the same ID cannot be removed by the old owner's cleanup. Runtime source is available now; compile/runtime acceptance is pending the global source barrier.

## Source work status

All four assigned actions have implementation, regression source and public documentation:

| Action | Delivered behavior in QuestManager.cs |
|---|---|
| `QUEST-LATE-BINARY-REPAIR` | Private `ReconcileState(QuestState, QuestConfig)` is shared by Load and successful new registration. Both grow saved lists and repair completed legacy binary progress to one. Counter progress, completion flags, status and unknown states are retained; no event replay or implicit save. |
| `QUEST-REGISTRATION-OWNERSHIP` | Exact-instance UnregisterQuest API above; replacing a registration requires explicitly releasing the old one. State survives config disposal. |
| `QUEST-PREDICATE-RESTART` | After every objective ConditionEntry evaluation, recheck the original state's identity and Active status before credit; stop that superseded iteration even when the predicate returns false. |
| `QUEST-PREDICATE-ADMISSION` | CanAcceptQuest rechecks state existence after evaluating start conditions. Accept overloads and TryAcceptQuest share a private admission/commit path; nested acceptance wins, outer acceptance fails with no duplicate state/events and TryAcceptQuest supplies its rejection reason. |

## Exact files written in this phase

- `Assets/Neoxider/Scripts/Quest/QuestManager.cs` — all four corrections; only additive public API is `bool UnregisterQuest(QuestConfig expected)`.
- `Assets/Neoxider/Tests/Edit/Quest/Audit20261023NeoQuestRegressionTests.cs` — 17 added parameterized cases, retaining the previous 27 (44 authored cases total). Requires the coordinator's isolated/exclusive runner policy; fixture initializes its inactive manager only after setting unique persistence key/disabled automatic persistence, then restores the inactive condition context. Cleanup skips already disposed assets and deletes only its own save key.
- `Assets/Neoxider/Tests/Edit/Quest/Audit20261023NeoQuestConditionProbe.cs` and `.cs.meta` — real public boolean getter invokes a bounded, one-shot callback through the supported ConditionEntry reflection path. GUID `a415c9e0597a4e22ba32cf18e62ed0fe`.
- `Assets/Neoxider/Docs/Quest/QuestManager.md`, `Saving.md`, `Code.md`, `README.md` — registration/disposal API, C# cleanup example, Inspector ownership context, legacy reconciliation and predicate reentry behavior; short Russian ownership explanation included.
- `Docs/Audits/2026-10-23/Neo.Quest.implementation.md` — this receipt, published first with the dependent API and finalized here.

Previous coherent Q1–Q8 source/docs edits remain. No other assembly, legacy/shared fixture, asmdef, shared tracker, contract, configuration, scene, prefab or sample was edited by this phase.

## Authored acceptance — NOT EXECUTED

The additional cases in the existing prefixed fixture cover:

- **7 reconciliation cases:** all three binary kinds recover one after unknown-at-load registration; both counter kinds preserve zero and positive saved counts. State identity/status/flags survive, appended objectives grow, repeated registration stays single, every gameplay event remains silent and stored JSON stays byte-for-byte unchanged with Auto Save enabled.
- **3 ownership cases:** Active, Completed and Failed progress survives exact removal. Foreign same-ID, null, repeated, stale and destroyed-instance removal fails; a replacement remains registered; save/event output stays unchanged.
- **2 restart cases:** a genuine condition getter restarts once and returns true or false. Replacement objectives retain zero progress and no completion events; the next explicit evaluation completes them once on both event surfaces.
- **3 acceptance entry points:** config, ID and TryAcceptQuest encounter a getter that accepts the same quest. Assert one state, nested success, outer failure, unchanged UnityEvent/any-event/C# order, and a TryAcceptQuest rejection reason.
- **1 availability case:** getter-created state makes CanAcceptQuest false; a subsequent query does not reevaluate conditions.
- **1 ordinary TryAcceptQuest case:** one start-predicate evaluation and the existing acceptance event order.

Assertions observe public state/events/save output. Reflection only configures private serialized fixture data. The probe is a test-only component, not a new gameplay abstraction.

## Compatibility and reuse

Existing serialized fields, public signatures, enums, GUIDs, positional save format and `Settings_Quests` default remain. Reused QuestState reconciliation methods, existing CanAcceptQuest/ConditionEntry/SaveProvider and the original synchronous callback sequence. TryAcceptQuest now evaluates conditions once per attempt instead of its previous duplicated preflight/Accept evaluation; this deliberate bounded change is documented and covered in source. Destructive restart, opt-in flow and metadata-only NextQuestIds remain unchanged.

No registry/session/FIFO framework, immutable definition layer or stable-objective-ID migration was implemented. The substantial proposal stays in the historical individual plan.

## Actual source review and remaining evidence

Reviewed the runtime diff, added regression source and four documentation diffs. Scoped tracked-file `git diff --check` exited **0**. No-index whitespace diffs for the untracked fixture/probe/meta exited **1** (new-file differences), with only configured LF-to-CRLF notices and no whitespace-error diagnostics; line-ending configuration was not changed. Source GUID reference enumeration found the new GUID only in its own meta. These are textual/source checks, not compilation or product validation.

No Unity, compiler, test runner, product validator/checker, service, install, commit/push or child agent executed. The earlier audit's validation receipt is historical and does not claim these additions passed.

**Incoming source dependencies: none. Outgoing integration:** Neo.Demo must use the published exact-instance unregister before owned QuestConfig disposal; this owner did not edit Demo. The getter regressions use existing public Neo.Condition APIs and need no pending additive API or shared asmdef change.

After EVERY writer finishes, coordinator must compile/import the frozen source and run all 44 prefixed cases plus the separately owned legacy fixture in an isolated, exclusively scheduled empty test scene with no other QuestManager. Verify actual JsonUtility recovery, reflection getter reentry, Singleton lifecycle and Demo unload/replacement disposal. Native Inspector button checks and both Unity-version acceptance remain centralized evidence debt. No authored case is reported as passing.

## I5-01 / P1 correction — 2026-10-24

Read the updated implementation contract, exact correction request and FULL `NativeImplementationReview-5.md`. Removed the reintroduced `[NonParallelizable]` annotation rejected by both installed Unity NUnit configurations, and corrected this receipt's earlier annotation claim. Isolation/exclusive scheduling belongs to the actual runner policy and exact fixture cleanup; the fixture does not establish serialization through an NUnit attribute.

This correction writes only the prefixed regression fixture and this receipt. All 44 meaningful cases, assertions, setup/cleanup and the condition probe are retained. Runtime source, public documentation, packages, NUnit APIs/shims and shared trackers are unchanged by this correction. Compilation, nonempty discovery and all 44 test outcomes remain UNEXECUTED pending the global barrier; no Unity/compiler/test/product-checker or other prohibited process was launched.

Actual textual comparison of the captured fixture before/after confirms its sole change is deletion of that attribute line; all other source is identical. Authored case attributes remain **44 before / 44 after**, with no unsupported annotation remaining. Scoped `git diff --check` for the fixture/receipt exited **0**. This source evidence does not claim compilation, discovery or test execution.
