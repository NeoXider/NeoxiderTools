# Neo.Core.Resources.Components: approved implementation request

Original actor: `neo-audit-1023-b01-05`. Target `demo` / `11.0.0`. Root has read your complete audit and plan.

Read `IMPLEMENTATION-CONTRACT.md` first. Do not execute tests/build/validators during this phase.

Keep nearest-assembly ownership. Finish bounded confirmed fixes and owned canonical documentation/backlinks. No optional broad framework is required.

## Assigned review actions

### HEALTH-INITIAL-CROSSPOOL (P2; approved-source-correction)

Stage all initial pool mirrors before notifying first pool; HP callback currently reads Mana reactive100 while model12.

## Relevant full native sections

Source: NativeReview-A.md

### A-06 — P2: first health-pool callback sees uninitialized state in later pools

Owner: `Neo.Core.Resources.Components`, original task `neo-audit-1023-b01-05`. Current source: `Assets/Neoxider/Scripts/Core/Resources/Components/HealthComponent.cs:210–211` and `:382–396`, `EnsureInitialized` / `SyncAllReactive`; later-pool constructor defaults are in `ResourceEntryInspector.cs:30–36`. Health source SHA256 `cc94c14738a2608a2ed0d4a4a8a5768fc9d4b4f89db7e9e867db45c345ab68fd`.

Trigger: initialize HP 40/80 followed by Mana 12/48, both without restore, and read `ManaCurrentValue`/max/percent from HP's first CurrentState listener. Expected: 12 / 48 / 0.25, consistent with the already-built model. Actual: initialization is marked complete but SyncAllReactive stages/notifies one entry at a time, so the later pool still exposes 100 / 100 / 1 through reactive convenience getters. A null later reactive object can also remain unrecreated at that point. The same owner's plan already identifies this remaining defect; independent source inspection confirms it.

Smallest repair: stage all valid pools, including missing reactive objects, before publishing any initial pool notifications, then preserve one initial notification per state and the readiness/reentry guard. Add cross-pool callback assertions for authored and loaded values. Do not expand this into atomic explicit Load, which remains a documented separate behavior.
Source: NativeReview-A.md

### Neo.Core.Resources.Components — source review complete; planned correction confirmed

Original owner: `neo-audit-1023-b01-05`. Full reads:

- `Docs/Audits/2026-10-23/Neo.Core.Resources.Components.md` — `98281c1c70dbb7b29f43e6868454ca5a9ae67f901c855696bdf9f7e20ef475a3`.
- `Docs/Audits/2026-10-23/Neo.Core.Resources.Components.plan.md` — `f3fa0c8d967660d1649391cf7b0ced83bcd33995701c8cd159f08979f69c9d53`.
- `Assets/Neoxider/Tests/Edit/Core/Audit20261023NeoCoreResourcesComponentsHealthComponentTests.cs` — `d9e55e06da66324286eba20e74c9cfb08e7450e43bd9f27929b1f3d455f0aaa8`.

All HealthComponent changed hunks, current lifecycle/accessor/synchronization paths, complete ResourceEntryInspector and complete new fixture were read, together with the canonical guide diff and Core/resource dependencies. Recursive initial load is removed, model construction is silent, readiness precedes callbacks, same-pool mirrors are staged before their listeners, repeated no-op reactive publications are suppressed and max-change reports the accepted value. These **completed fixes appear correct**, but the cross-pool initial snapshot defect in its plan remains: A-06.

Fixture state/event/JSON assertions are substantive; inactive objects disable autosave, owned objects are destroyed and unique persistence keys deleted. Tests only read the same pool during the first reactive callback, so they do not close A-06. Real Update regeneration, serialization, save/disable/re-enable and callback mutation remain **needs evidence** after source work.

Aggregate cost: every HealthComponent ticks every frame, scanning all its pools even when their timers do not fire. Each changed pool calls `TryGetPoolEntry` at least twice through synchronization/event forwarding; that lookup scans the Inspector list. Initial SyncAllReactive also repeats the lookup for each entry. For P pools with active regen, this produces O(P²) wrapper comparisons on emission frames even though Core Tick is O(P). A small stable case-insensitive first-entry index is a **future performance proposal / profiling lead**, preserving the documented unsupported-duplicate policy and authoring-only list lifetime. It should be considered before adding a broad catalog API purely for performance. No measured timing claim is made.

The readonly catalog/explicit initialization/validation proposal is additive composition work for root to prioritize; it is not necessary to stage initial values safely. Reject silent changes to duplicate precedence or explicit-load atomicity while implementing A-06.

## Completion receipt

Write `Docs/Audits/2026-10-23/Neo.Core.Resources.Components.implementation.md` with exact actions/files/additive API/dependencies, migration details and authored acceptance (UNEXECUTED). If blocked on another additive API, complete independent files and identify it precisely for root continuation. Do not edit shared review-action/manifest/other actor reports.
