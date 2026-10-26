# Neo.Core.Level.Components: approved implementation request

Original actor: `neo-audit-1023-b03-02`. Target `demo` / `11.0.0`. Root has read your complete audit and plan.

Read `IMPLEMENTATION-CONTRACT.md` first. Do not execute tests/build/validators during this phase.

Keep nearest-assembly ownership. Finish bounded confirmed fixes and owned canonical documentation/backlinks. No optional broad framework is required.

## Assigned review actions

No additional confirmed runtime defect was assigned after independent review. Preserve your coherent earlier patch. Finish canonical docs/navigation and factual API/scene/NoCode contracts in owned scope, keep at least one substantial improvement described as a proposal, and write an honest source-final receipt. Do not manufacture a runtime rewrite or duplicate tests.

## Relevant full native sections

Source: NativeReview-A.md

### Neo.Core.Level.Components — source review complete

Original owner: `neo-audit-1023-b03-02`. Full reads:

- `Docs/Audits/2026-10-23/Neo.Core.Level.Components.md` — `cec60cc0f21b208e52fbc1afc81a3bb36c9c897f4332b26cd98b3bdf2deed2dc`.
- `Docs/Audits/2026-10-23/Neo.Core.Level.Components.plan.md` — `6ca3630d46bc324e0b93cff1fcd847b7704d986764163fc3da8b408017fb063a`.
- `Assets/Neoxider/Tests/Edit/Level/Audit20261023NeoCoreLevelComponentsTests.cs` — `ef62319b7de02209d5fb01dc4632bc407c9e4c84c1498683838d663def8f1346` (current centrally integrated source, rather than the old fixture hash in the audit).

All changed hunks and the complete current `LevelComponent.cs` were inspected; the fixture and canonical-guide diff were read. **Completed fixes appear correct:** initialization state is set before consumer callbacks and reset on escaping initialization failure; restore applies incoming cap before state with this component's forwarding detached in `finally`; early reset/snapshot initialize intentionally; all three mirrors are staged before notifications. No per-frame Update or scene search exists. The model events are attached once and removed on destruction. Snapshot/JSON allocations occur on explicit save/snapshot operations, not per frame. The singleton `SaveProvider` remains an existing shared dependency; the fixture restores its prior provider in `finally` and disables object autosave before teardown.

Tests inspect saved profiles, counts, mirror state inside callbacks and bounded retry/reentry, with assertions outside swallowed reactive callbacks. They do not claim arbitrary mutation-from-callback correctness; the plan explicitly retains that evidence gap. **Future proposal:** options plus an injected profile-store adapter can improve runtime factories but are not a prerequisite for current fixes; preserve caller ownership and default SaveProvider routing. Existing manual pre-model Save/Load no-op and historical downward `OnLevelUp` semantics are documented, not new defects. **Needs evidence:** real disable/re-enable autosave and final integrated Core/Reactive behavior.

## Completion receipt

Write `Docs/Audits/2026-10-23/Neo.Core.Level.Components.implementation.md` with exact actions/files/additive API/dependencies, migration details and authored acceptance (UNEXECUTED). If blocked on another additive API, complete independent files and identify it precisely for root continuation. Do not edit shared review-action/manifest/other actor reports.
