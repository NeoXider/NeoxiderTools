# Neo.Quest: approved implementation request

Original actor: `neo-audit-1023-b05-01`. Target `demo` / `11.0.0`. Root has read your complete audit and plan.

Read `IMPLEMENTATION-CONTRACT.md` first. Do not execute tests/build/validators during this phase.

Also implement additive bool UnregisterQuest(QuestConfig expected) accepting only exact current registration, preserving saved states/other replacement config; publish API for Demo before definitions disposal.

## Assigned review actions

### QUEST-LATE-BINARY-REPAIR (P2; approved-source-correction)

Known-at-load repairs completed binary objective progress0→1; late RegisterQuest only grows lists. Reuse single config reconciliation path preserving counter progress/status/events/save format.

### QUEST-REGISTRATION-OWNERSHIP (P2; approved-source-correction)

Demo must detach owned runtime Quest configs before destroying them. Add minimal same-instance unregister preserving saved progress/other replacement config, no general registry framework.

### QUEST-PREDICATE-RESTART (P1; approved-source-correction)

A-37: supported condition getter restarts quest; post-predicate identity/status check before credit prevents old notification completing replacement.

### QUEST-PREDICATE-ADMISSION (P2; approved-source-correction)

A-38: nested AcceptQuest from start predicate creates duplicate state; recheck admission after predicate, including CanAcceptQuest coherence.

## Relevant full native sections

Source: NativeReview-A.md

### Neo.Quest — source review complete; late-content and predicate reentry gaps remain

Original owner: `neo-audit-1023-b05-01`. Full reads:

- `Docs/Audits/2026-10-23/Neo.Quest.md` — `632a4a8da36bb077da4cf68c1ffc53100fed275c1240f761d0c7479ade5b6145`.
- `Docs/Audits/2026-10-23/Neo.Quest.plan.md` — `1bfe1e9bfd95c5cc40ee61144aef9a7b4d2b9204ccebef9ee241cbc310e8f1ae`.
- `Assets/Neoxider/Tests/Edit/Quest/Audit20261023NeoQuestRegressionTests.cs` — `e05c6297fc4d9b4056262858b916e704354f6d6c9b6b74479492d2889132b25b`.

All four changed source diffs, full 468-line regression source, ten changed guide diffs and current acceptance/notification/state/load paths were read. **Completed fixes appear sound** for binary progress before callbacks, objective commit before progress, active/reference guards around completion callbacks, temporary load normalization, list growth, accurate bridge/flow gates and runtime Inspector buttons. Tests assert callback-visible progress, exact completion counts/status/identity and round-trip save contents; unique key deletion is scoped. The fixture assumes its own Quest singleton and does not preserve an arbitrary existing manager, so execution needs an isolated scene. Native button enablement and JsonUtility recovery remain later evidence. No regression ran.

**A-36, P2, confirmed:** `QuestManager.Load:126–137` repairs completed binary progress for known content, while `RegisterQuest:276–280` only grows restored lists. Load unknown completed Talk with progress0/completed=true, then register its config: progress remains0; known-at-load returns1. Expected identical legacy normalization independent of registration timing. Smallest repair: shared private config reconciliation used by Load/RegisterQuest, preserve counter counts/status/unknown data and emit no gameplay event. Add all three binary kinds plus counter/no-event assertions. This is the original plan's confirmed residual.

**A-37, P1, confirmed old-notification credit through user predicates:** `QuestManager.EvaluateObjectiveConditions:692–701` checks original active/reference identity before calling ConditionEntry.Evaluate, but does not check it afterwards. A supported getter/method restarts its quest once and returns true; CompleteObjective(config,i) then resolves the new state and immediately credits it from the old notification. Expected replacements receive only the next notification, as Q3/docs promise. Minimal repair: recheck original identity/status after each user predicate, before credit, and stop the superseded state iteration. Add a real condition-probe method causing one restart and assert replacement progress0/statusActive. Current tests restart from objective-completed callbacks, so they do not cover predicate-time replacement.

**A-38, P2, confirmed duplicate admission through predicates:** `QuestManager.AcceptQuest:233–260` checks GetState before EvaluateStartConditions, then unconditionally adds a new state. A bounded start-condition method invokes one nested AcceptQuest of the same config and then returns true; both calls add the same id and emit acceptance. Expected one current state per id. Minimal repair: post-predicate existence/admission recheck before committing, and keep CanAcceptQuest coherent after user evaluation. Preserve ordinary callback order/legacy restart policy. Add nested acceptance and predicate-created-state availability assertions; no queue/FIFO engine is required.

Aggregate work: each notification snapshots all states and repeatedly performs linear state/config lookups inside per-objective loops, yielding O(quests² + objectives × quests) in large registries; custom-condition evaluation adds reflection/binding cost. ActiveQuests allocates a list on every read. There is no autonomous Quest Update or physics search; callers choose checkpoints/timers. AutoSave serializes the full state list on accepted operations. Dictionaries/immutable definitions/stable objective IDs are **future proposals**; adding IDs requires explicit legacy migration and should not block A-36–38. Keep synchronous interleaving, positional saves, metadata-only NextQuestIds, optional flow and documented destructive restart defaults; strict invalid-content/FIFO/atomic-restart policy is not silently approved.

## Completion receipt

Write `Docs/Audits/2026-10-23/Neo.Quest.implementation.md` with exact actions/files/additive API/dependencies, migration details and authored acceptance (UNEXECUTED). If blocked on another additive API, complete independent files and identify it precisely for root continuation. Do not edit shared review-action/manifest/other actor reports.

## Final NI4/I5 correction handoff

ROOT CONFIRMED I5-01/P1. Read FULL NativeImplementationReview-5.md. Remove reintroduced [NonParallelizable] from Audit20261023NeoQuestRegressionTests.cs and fix own implementation receipt claim. Both installed Unity NUnit APIs already rejected that attribute; globalcontract explicitly forbids it. Preserve44meaningful cases, assertions and actual isolated/exclusive runner policy. Runtime source is scopedapproved; no unrelated rewrite or NUnitshim/package. Source-only correction, no execution.
