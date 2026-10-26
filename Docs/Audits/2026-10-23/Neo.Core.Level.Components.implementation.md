# Neo.Core.Level.Components — implementation receipt

Status: **source-final** for the assigned request, 2026-10-24. Target: `demo` / `11.0.0`. This continues the original `neo-audit-1023-b03-02` session. No pending additive API blocks this owner.

## Approved actions completed

The implementation request assigns no numbered review-action IDs. Its authoritative action is the native section **“Neo.Core.Level.Components — source review complete”**: retain the reviewed runtime corrections, finish owned canonical documentation/navigation, preserve the substantial improvement as a proposal, and provide an honest completion receipt. No additional runtime defect or broad framework was assigned.

The original audit's completed F1–F4 remain intact:

| Finding | Retained implementation |
|---|---|
| F1: initialization callback re-entry | Initialization is marked before load/reactive callbacks and reset on escaping failure; load completion follows mirror synchronization. |
| F2: old-cap progress loss | Incoming/configured cap precedes restored state; this component's level forwarding is temporarily detached and reattached in `finally`. |
| F3: early reset/snapshot omit or overwrite saved state | Both public methods initialize before performing their operation. |
| F4: stale mirrors inside XP/level callbacks | All three mirrors are staged before the existing selective notifications. |

The runtime file was read completely and preserved, rather than patched again. SHA-256 remains `616dd0235e0c7f2de3b4a074739dd135fb6227b164910fbb52066aa93fa31cab`.

## Exact files and APIs

| File | Final action |
|---|---|
| `Assets/Neoxider/Scripts/Core/Level/Components/LevelComponent.cs` | Preserved the existing coherent runtime patch; no new change during this continuation. |
| `Assets/Neoxider/Docs/Core/Level/Components/LevelComponent.md` | Completed lifecycle, subscription timing, callback limits, actual NoCode result semantics, assembly-reference guidance, and canonical backlinks. Corrected the example to account for previously loaded XP. Added a concise Russian explanation. |
| `Assets/Neoxider/Tests/Edit/Level/Audit20261023NeoCoreLevelComponentsTests.cs` | Preserved the centrally integrated 14-case fixture, including removal of the unsupported NUnit annotation. Current SHA-256: `ef62319b7de02209d5fb01dc4632bc407c9e4c84c1498683838d663def8f1346`. |
| `Assets/Neoxider/Tests/Play/Audit20261023NeoCoreLevelComponentsLifecycleTests.cs` | Added one focused real-lifecycle regression source for the evidence gap identified by native review. |
| Same PlayMode path with `.cs.meta` suffix | Added metadata, GUID `364d07b47c994bc0a6d28ea54ee417ea`. Global GUID/package validation remains central. |
| `Docs/Audits/2026-10-23/Neo.Core.Level.Components.implementation.md` | This source-final receipt. |

**Additive public runtime API: none.** Existing public signatures, serialized fields, save key/defaults, JSON format, scene/prefab configuration, and sample layout are retained. Earlier deliberate behavior corrections are documented: early snapshots include saved progress; reset is no longer undone by deferred loading; profile restoration avoids intermediate level notifications; callbacks read synchronized mirrors.

Manual pre-model Save/Load still do nothing until initialization; downward direct level changes still raise `OnLevelUp`; reactive refreshes can repeat. Arbitrary mutation-from-callback transaction safety is not promised. The guide points users to existing Core, Level setup, curve, Bridge, Save, and Reactive entry points. The parent Core entry already links this component; no parent/sibling documentation was edited.

## Authored acceptance — UNEXECUTED

New test: `Audit20261023NeoCoreLevelComponentsLifecycleTests.DisableThenEnable_SavesCurrentProgressWithoutReloadingTheOldProfile`.

1. Seed an isolated in-memory provider with 250 XP, create an inactive GameObject, configure the component's existing Inspector inputs, and subscribe before activation. No private lifecycle method is invoked and no manual initialization substitutes for Awake.
2. Activate and yield a frame: expect one load event/read, level 3, 250 XP, and no save.
3. Add 25 XP and deactivate: expect one provider flush and save event; the flushed JSON contains 275 XP, level 3, no cap.
4. Replace the provider's stored JSON with the stale 250-XP profile, then reactivate: expect no further read/load, live and reactive state still at 275 XP / level 3 / 25 XP remaining.
5. Deactivate again: expect exactly two total saves/events and a second flushed 275-XP profile.

Assertions observe public state, real activation-driven callbacks, and flushed serialized output. Reflection configures Inspector-only fields; it does not inspect runtime internals. Teardown disables autosave, destroys the task-owned GameObject, waits for destruction, and restores the exact previous SaveProvider in `finally`.

This complements the existing 14 EditMode cases; it does not duplicate their manually driven initialization/state coverage. Both fixtures depend on the coordinator's isolated/exclusive runner and exact cleanup. Neither uses the unavailable `NonParallelizable` attribute. The old audit's annotation/nonparallel description is retained only as historical evidence and is not a claim about the current fixture.

PlayMode fixture SHA-256: `8d842c55c50a6d81b441a5b35b37019637458c34154517a917fbc23ce4c7a210`.

## Dependencies, proposal, and migration

- `Neo.Core` supplies LevelModel/profile contracts; `Neo.Reactive` supplies mirror notifications; `Neo.Save` supplies the active provider API. Their final integrated behavior must be checked after all writers finish. No additional API change is requested from these owners.
- `Neo.Core.Level.Bridge` already captures an action's resulting level before `OnSuccess`; the component guide now describes that actual boundary. Its source remains owned by its actor.
- Existing `Neo.Tests.Play` references already include Core, Components, Save, Reactive and Unity TestRunner. No shared asmdef edit is needed or performed. Primary test owners retain their existing fixtures/internals.
- The plan's options/injected-profile-store/active-curve API remains a **substantial unimplemented proposal**. It was not required for the approved bounded fixes; no new store, initialization framework, editor wrapper, or migration was introduced.
- No automatic asset rewrite or sample-layout change is needed. Root retains package metadata, migration/release integration, and any later push after complete verification.

## Work performed and evidence debt

Performed only source/document reads, reference/path enumeration, scoped documentation diff review, patching of the files listed above, metadata generation, and artifact-hash bookkeeping. Read the full implementation contract/request and historical audit/plan. Current runtime and centrally integrated EditMode fixture hashes match the reviewed artifacts. Historical audit/plan and all shared trackers remain unchanged.

**Not performed:** Unity import, compiler execution, EditMode/PlayMode test execution, product/package/text validators or checkers, installers, services, child actors, commits, or pushes. No historical static-pass receipt has been rewritten as evidence for this source snapshot.

After every writer finishes, root should discover and execute the 14-case EditMode fixture and new one-case PlayMode fixture, integrate existing Level/Progression/Bridge regressions, inspect actual lifecycle/console results, and compile the frozen snapshot on the designated Unity 6.3 and 6.6 hosts. These cover the supported 6.0–6.4 and 6.5+ compatibility families without introducing dispersed version conditionals. Current UTF-8/link/meta/GUID/package checks remain central as well. Authored acceptance is not a test pass, and source-final does not mean verification-complete.
