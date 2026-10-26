# Neo.Core.Resources.Components — implementation plan

Phase 2; same audit session, `gpt-6.1-sol`, reasoning `max`. Planning only. Source changes below are future work, not implementations made in this phase.

## Current outcome

The [completed audit](Neo.Core.Resources.Components.md) reviewed both inventory files. `HealthComponent` now avoids recursive auto-load/initialization, publishes readiness before callback reentry, synchronizes reactive values before pool events, suppresses duplicate/unchanged reactive updates, initializes HP/Mana accessors, reports applied maxima, and tolerates missing authoring entries/state. Public signatures and five serialized field declarations remain intact. `ResourceEntryInspector.cs` was reviewed without edits. The canonical English guide and 15 regression cases were added/updated.

Static UTF-8, declaration compatibility, delimiter/reference/link checks and scoped `git diff --check` passed. No Unity compilation, serialization, EditMode or PlayMode verification has executed. These completed fixes must not be scheduled for implementation again.

## Priorities and remaining evidence

**P2 — confirmed source-level initialization inconsistency; runtime reproduction pending. Owner: this assembly.** `EnsureInitialized` sets `_initialized` before `SyncAllReactive(true)`; that method synchronizes and notifies entries sequentially (`HealthComponent.cs:194,382`). Configure HP `40/80`, Mana `12/48`, both without restore. From HP's first `CurrentState` listener read `ManaCurrentValue`: it still reads the reactive constructor default `100` (`ResourceEntryInspector.cs:30`), while `GetCurrent(Mana)` already returns `12`. The existing callback regression reads only its own model pool, so it does not cover this case.

Smallest correction: stage every valid pool's current/max/percent silently before publishing any initial reactive notification. Then publish once per initial state with readiness already guarded. Retain the existing model and saved-state application path; no public API or serialized change. Add cross-pool getter/reactive assertions, including auto-loaded Mana and recreated null state. Expected observation from HP's first callback: Mana current `12`, max `48`, fraction `0.25`, matching the model. No callback recursion or extra initial notifications.

**P1 — verification debt, not another confirmed source defect. Owner: coordinator.** After source-changing workers finish, compile and run the existing 15-case fixture plus `HealthComponentTests` and `ResourcePoolModelTests`. Integrate final `Neo.Core`, `Neo.Reactive` and `Neo.Save` first. Current core source already rejects capped underpayment/non-finite spending and sanitizes uncapped saved pools; those fixes belong to its owner. Component integration cases should observe failed capped spending with unchanged balance/events, harmless invalid arguments, and max-zero positive-current save/load preservation.

**P2 — confirmed limitation for invalid configuration.** `ResourcePoolModel.AddPool` replaces a duplicate id; component lookup selects the first matching inspector entry. Case-insensitive duplicates can therefore bind different rules/events. They are already documented as unsupported. Add validation through the proposal below; preserve legacy precedence until a migration policy is chosen.

**P3 — documented limitations/unverified risks.** Explicit `Load` publishes max and current separately; atomic loading would be an optional behavioral change coordinated with `Neo.Core`. Provider read exceptions outside the JSON catch and callbacks that mutate other state need targeted reproduction before adding recovery machinery. They are not newly proven failures of supported workflows.

## Substantial proposal: read-only catalog and explicit configuration

Programmers cannot enumerate custom pools, obtain observable state, or configure the private pool list through public C#. Reuse `ResourcePoolModel`, `ResourcePoolEntry`, existing save DTOs, reactive synchronization and UnityEvents. A current Core search found no `IResourcePoolCatalog`, `IReadOnlyResourcePoolState` or `ResourcePoolDefinition` implementation. No replacement resource/save system or custom Inspector is needed.

Add optional plain-C# contracts in `Neo.Core`; leave `IResourcePoolProvider` unchanged:

```csharp
public interface IReadOnlyResourcePoolState
{
    string Id { get; }
    float Current { get; }
    float Max { get; }
    float Percent { get; }
    event System.Action<float, float> Changed;
}

public interface IResourcePoolCatalog
{
    System.Collections.Generic.IReadOnlyList<string> ResourceIds { get; }
    bool TryGetState(string id, out IReadOnlyResourcePoolState state);
}
```

The model implements the catalog; the component delegates to it. Lookup is case-insensitive, missing ids return false, enumeration cannot mutate the model, and views expose no mutable reactive object. Views are valid while their pool exists; callers unsubscribe before replacing/removing that pool. Effective value changes publish `Changed`; max-zero percentage remains `0`.

Proposed component methods:

- `bool TryInitialize(IEnumerable<ResourcePoolDefinition> definitions, out IReadOnlyList<ResourcePoolValidationIssue> issues)`.
- `IReadOnlyList<ResourcePoolValidationIssue> ValidateConfiguration()` for existing Inspector data, without mutation or logging.

Snapshot/validate definitions once before constructing anything live. Definitions carry id and existing resource rules as plain data; issues include index, id and reason. Reject blank/duplicate ids, non-finite values and invalid timing. Failure leaves existing state/subscriptions intact. `TryInitialize` is deliberately pre-initialization only: after initialization it returns an `AlreadyInitialized` issue. Code creates/configures an inactive GameObject before activation; ordinary scenes retain `EnsureInitialized` and their existing authoring list. Successful initialization publishes the existing initial notifications and one `OnPoolsChanged`. Dynamic component add/remove/rebuild and atomic profile application remain separate proposals.

## Exact future files and owners

Paths are repository-relative. New package files require unique `.meta` GUIDs; coordinator handles any folder metadata.

| Owner | Files / purpose |
| --- | --- |
| This assembly | `Assets/Neoxider/Scripts/Core/Resources/Components/HealthComponent.cs`: stage initial snapshot, delegate catalog, validate/map explicit definitions. Preserve `ResourceEntryInspector.cs` fields and event objects. |
| `Neo.Core`, implement first for proposal | `Assets/Neoxider/Scripts/Core/Resources/Interfaces/IReadOnlyResourcePoolState.cs`; `Assets/Neoxider/Scripts/Core/Resources/Interfaces/IResourcePoolCatalog.cs`; `Assets/Neoxider/Scripts/Core/Resources/Domain/ResourcePoolDefinition.cs`; `Assets/Neoxider/Scripts/Core/Resources/Domain/ResourcePoolValidationIssue.cs`; `Assets/Neoxider/Scripts/Core/Resources/Domain/ResourcePoolConfiguration.cs`; update existing `Assets/Neoxider/Scripts/Core/Resources/Domain/ResourcePoolModel.cs`. |
| This worker's regression prefix | New `Assets/Neoxider/Tests/Edit/Core/Audit20261023NeoCoreResourcesComponentsInitializationSnapshotTests.cs`, `Assets/Neoxider/Tests/Edit/Core/Audit20261023NeoCoreResourcesComponentsIntegrationTests.cs`, `Assets/Neoxider/Tests/Edit/Core/Audit20261023NeoCoreResourcesComponentsCatalogTests.cs`. Exercise public outputs; minimum cases described above/below. |
| This assembly's docs | Update `Assets/Neoxider/Docs/Core/Resources/Components/HealthComponent.md`: C# catalog/configuration example and subscription cleanup; scene/NoCode authoring, validation results and unchanged Damage/Heal/UI binding. Core owner updates its own canonical entry. |

`Neo.Reactive`/`Neo.Save` are integration dependencies, not planned edits. Existing Editor-test references cover these assemblies. Any subsequently required shared asmdef/package change belongs to the coordinator. Editor validation presentation, if needed later, belongs to `Neo.Editor`; it is not required for this delivery.

## Order, compatibility and acceptance

1. Add the cross-pool reproduction and apply silent staging; then run focused initialization and existing mutation regressions centrally. Estimate half a day, low risk; preserve callback counts and mutations made from ready listeners.
2. Close integration debt against final dependency revisions. In PlayMode, observe initial saved UI state, scaled-time regeneration, disable/save/re-enable and destruction/subscription cleanup. Verify serialized UnityEvent binding as well as code listeners.
3. Implement Core catalog/validation and pure contract tests, then the component adapter and pre-initialization tests. Proposal estimate 2–4 days, medium risk around view lifetime, validation and event compatibility.
4. Update documentation, then package validation. Preserve `_pools`, all entry fields, save schema, existing UnityEvents and assets. Legacy duplicates receive validation issues without silent precedence changes. Keep release `Samples~` layout and sample paths unchanged; sample consumers must work from active developer/release/imported roots. Add no sample dependency.
5. Coordinator compiles affected dependency branches on Unity 6.0–6.4 and 6.5+; compile both supported compatibility paths. Any object-id usage stays behind existing `NeoDiagnostics.StableId` / `NeoComponentHealth.StableId`; add no scattered version gates.

Minimum proposal acceptance: code configures HP/stamina without reflection; catalog lookup and post-spend notification expose exact values; missing lookup creates nothing; duplicate/NaN/timing errors report indexed reasons without partial initialization; repeated initialization fails explicitly without losing listeners; existing HP/Mana scenes retain saved values and bindings.

Stop release completion on failed compilation, failed observable regressions, broken serialized bindings or unresolved dependency changes. Product decisions concern legacy duplicate migration and atomic-load semantics only; neither blocks the safe initialization fix. All runtime outcomes remain unverified until coordinator execution. Phase 2 changes only this plan; no source, test, public-doc, audit-report, configuration or Unity-state mutation is authorized here.
