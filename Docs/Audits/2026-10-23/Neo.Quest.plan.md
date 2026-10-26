# Neo.Quest — individual implementation plan

Planning only, same auditor/session; requested profile: `gpt-6.1-sol`, reasoning `max`. Inputs: complete PLAN-CONTRACT, [audit](Neo.Quest.md), manifest, current source/tests/docs, and dependency/editor boundaries.

Paths below use `Q = Assets/Neoxider/Scripts/Quest`, `D = Assets/Neoxider/Docs/Quest`, and `T = Assets/Neoxider/Tests/Edit/Quest`. Proposed files do not exist yet.

## 1. Delivered work

Reviewed 8/8 inventory files. Q1–Q8 are implemented in `Q/QuestManager.cs`, `QuestState.cs`, `QuestFlowConfig.cs`, and `Bridge/QuestNoCodeAction.cs`: progress, callback/state identity, saves/registration, NoCode/flow, and Play Mode buttons. Updated ten public pages; `T/Audit20261023NeoQuestRegressionTests.cs` contains 27 authored cases with `.meta`.

Audit static checks passed. Compilation, NUnit, Play Mode and Inspector behavior remain unverified. Q1–Q8 are not implementation backlog.

## 2. Remaining confirmed correction

**P2 / Quest owner: late registration misses legacy binary progress repair.** Source-confirmed during planning; not fixed or executed.

`Q/QuestManager.cs:126–137` repairs binary progress only for known configs; `:276–280` registration only grows lists. Reproduce: load an unregistered legacy Talk quest, Completed, progress `[0]`, completion `[true]`; register its config. `GetObjectiveProgress(config, 0)` remains zero; the known-at-load path returns one.

Extract existing config reconciliation into one private method called from Load/RegisterQuest. Reuse `EnsureObjectiveCount`/`SetObjectiveProgress`; preserve counters, unknown states, statuses, events and save format. Add `T/Audit20261023NeoQuestLateRegistrationTests.cs` plus `.meta`: three binary types recover one; counters retain counts; registration emits no gameplay event. Update `D/Saving.md`, `D/QuestManager.md`. Estimate: half a day plus verification.

## 3. Verification debt and unverified risks

| Priority / owner | Evidence and next action | Observable acceptance |
|---|---|---|
| P1 / coordinator, then Quest owner for failures | Execute the 27 new cases and existing `T/QuestManagerTests.cs` after concurrent source audits settle. Include JsonUtility null/unequal lists, both event surfaces, restart callbacks and late registration. | Exactly one completion/reward event; replacement states receive no old credit; saved progress survives round-trip. |
| P1 / coordinator + Neo.PropertyAttribute owner | `ButtonAttributeDrawer.IsButtonEnabled` honors PlayModeOnly statically; actual Inspector behavior remains unverified. Exercise the nine Quest buttons. | Disabled in Edit Mode without changing the save key; working in Play Mode; adding/resetting the bridge in Edit Mode emits no action. |
| P1 / coordinator | Compile representative Unity 6.0–6.4 and 6.5+ environments, including relevant optional-network configurations. | No new compile/import failures; actual receipts identify version and configuration. |
| P2 / Quest owner | Corrupt saved enum/count values and invalid definitions are not comprehensively validated. These are validation risks, not newly demonstrated valid-content defects. | Add explicit validation contracts before changing rejection/clamping policy. |
| P3 / design decision | Restart deletes old progress before reevaluating conditions; nested synchronous events can interleave. Both are documented current behavior. | Preserve defaults; consider additive atomic restart/FIFO delivery separately. |

Empty quests, duplicate IDs and conflicting chains need an invalid-content policy before becoming defect backlog.

## 4. Substantial proposal: plain C# runtime and stable objective IDs

**Problem:** programmatic quests require ScriptableObjects/private setup; positional saves prevent safe objective reordering. One MonoBehaviour owns transitions, lookup, events and serialization.

**Reuse:** existing transitions/reference guards, list repair, flow rules, SaveProvider, ConditionEntry/context, Singleton and NeoDiagnostics. Source search found no QuestRuntime/QuestDefinition replacement. Extend existing editors.

Add immutable definitions and a single authoritative runtime registry with ordinal quest/objective dictionaries. Suggested public contract:

```csharp
QuestResult Register(QuestDefinition definition);
QuestResult TryAccept(string questId, Func<string, bool> evaluateCondition);
QuestResult TryCredit(string questId, string objectiveId, int amount = 1);
QuestResult TryFail(string questId);
QuestSnapshot ExportSnapshot();
QuestResult ImportSnapshot(QuestSnapshot snapshot);
event Action<QuestChange> Changed;
```

Definitions contain stable objective IDs and condition keys; the scene adapter maps keys to existing ConditionEntry objects. Results carry typed failure reasons and state generation/revision; change payloads contain immutable progress/status data. Keep synchronous legacy callback semantics initially, including failure/restart from progress handlers. FIFO notification delivery from the audit proposal is a later contract change, not a prerequisite for extraction. Do not duplicate mutable runtime state between manager and core.

**Exact implementation surface:** new `Q/Runtime/QuestDefinition.cs`, `QuestRuntime.cs`, `QuestResult.cs`, `QuestSnapshot.cs`, `QuestChange.cs`; new `Q/QuestDefinitionAdapter.cs` and `Q/QuestSnapshotJson.cs`. Adapt existing `Q/QuestManager.cs`, `QuestState.cs`, `QuestConfig.cs`, `QuestObjectiveData.cs`, `QuestFlowConfig.cs`, and `Bridge/QuestNoCodeAction.cs`. Keep these in Neo.Quest initially; a new assembly is unnecessary for testing plain classes without scene objects.

**Compatibility:** preserve component fields/GUIDs, enums, public overloads, UnityEvent bindings, default key `Settings_Quests`, and QuestState reference identity within one quest generation. Preserve its public Status contract until integrations are characterized. Add objective IDs without renaming existing fields. Read legacy index JSON and versioned v2 ID snapshots; record an explicit legacy-index-to-ID mapping before reordering. Never generate new IDs during load, discard unknown IDs, or overwrite legacy saves before successful migration. Keep index overloads as compatibility adapters.

## 5. Dependencies and implementation order

1. Coordinator stabilizes Neo.Save, Neo.Condition, Neo.Tools.Managers, Neo.PropertyAttribute and Neo.Extensions, then runs debt checks. Dependency failures go to their owners first. Existing Neo.Network references gain no replication/transport dependency.
2. Quest owner repairs late registration with its regression; coordinator verifies before redesign.
3. Add typed definitions/results and extract transitions incrementally. Add `T/Audit20261023NeoQuestRuntimeTests.cs`: ordinary C# construction, invalid IDs/counts, exact event counts, failure/restart callbacks, and generation isolation. Existing manager/bridge regressions must remain green before replacing their internals.
4. Add v2 snapshots/legacy importer and `T/Audit20261023NeoQuestMigrationTests.cs`: append/reorder identity, unknown-content retention, round-trip and failed-import preservation. Then adapt scene/NoCode calls to typed results.
5. **Neo.Editor owner**, after runtime contracts settle: extend `Assets/Neoxider/Editor/Quest/QuestConfigEditor.cs` for IDs/migration with Undo, and `QuestFlowConfigEditor.cs` for validation. Coordinate asset migration; no bulk rewrite or scene builder. These files are outside Quest ownership.
6. Update `D/README.md`, `Code.md`, `QuestManager.md`, `QuestConfig.md`, `QuestObjectiveData.md`, `QuestState.md`, `QuestFlowConfig.md`, `Saving.md`, `QuestBridge.md`, and `Scenarios.md` with both construction/API and Inspector migration workflows. Preserve the canonical README.

Estimate: 4–6 days extraction/adapters/tests, 2–3 days migration/editor integration. Principal risks are legacy callback timing, mutable Status integrations and irreversible objective-identity mistakes.

## 6. Acceptance and stop conditions

Observe headless construction/completion, identity after reordering/migration, unchanged counters/flow/bindings. Coordinator checks Play Mode accept/collect/fail/restart/save/reload, Inspector and packaging. Use centralized `NeoDiagnostics.StableId` / `NeoComponentHealth.StableId` if needed; no scattered version branches. Both Unity API generations require actual compilation.

Preserve active `Samples~`; validation must also recognize developer `Samples` and imported `Assets/Samples/NeoxiderTools/<version>/<sample name>`. New package assets require unique `.meta` files; shared asmdef/package changes belong to the coordinator.

Stop the affected rollout if legacy import remaps progress, callbacks duplicate rewards, bindings break, or either Unity compile target fails. Record the failing scenario and owner; continue independent safe work. FIFO events, zero-objective policy, strict validation and atomic restart remain explicit design choices. Phase 2 executes no Unity/build/test process and modifies only this plan.
