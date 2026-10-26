# Neo.Core.Resources.Components: audit

Status: source audit and scoped fixes complete; Unity compilation and test execution pending coordinator.

- Base revision: `ca198556` (package 10.19.0).
- Model: `gpt-6.1-sol`, reasoning `max`.
- Assembly: `Assets/Neoxider/Scripts/Core/Resources/Components/Neo.Core.Resources.Components.asmdef`.
- Scope: 2 C# files; nested assemblies excluded.

## Scope

Reviewed **2/2 inventory C# files in full**. No nested assembly source was edited.

| File | Review coverage |
| --- | --- |
| [HealthComponent.cs](../../../Assets/Neoxider/Scripts/Core/Resources/Components/HealthComponent.cs) | All public methods/properties; lazy initialization and callback reentry; Awake/Update/disable/destroy; model subscriptions; initial and live reactive synchronization; regeneration delegation; persistence and saved-data application; authoring nulls; case-insensitive lookup; per-pool UnityEvents and convenience NoCode methods. |
| [ResourceEntryInspector.cs](../../../Assets/Neoxider/Scripts/Core/Resources/Components/ResourceEntryInspector.cs) | Every serialized field/default, numeric authoring constraint, mutable reactive reference, event and read-only accessor. This serializable scene configuration has no independent lifecycle or scene-object asset references. No source change needed; the owning component now recreates missing reactive state. |

Read dependencies as context: `ResourcePoolModel`, `ResourcePoolEntry`, resource save DTOs, `IResourcePoolProvider`, reactive notification APIs, `SaveProvider`, `NeoDiagnostics`, existing `HealthComponentTests`, relevant core tests, and existing runtime/test asmdefs. Core dependencies remain owned by `Neo.Core`; existing tests remain untouched.

Reuse receipt: read the `neoxider-tools` and `unity-game-agent` skills and searched the tools/modules/extensions references for health, resources and reactive APIs. Reused the existing model, reactive properties, JSON DTOs, save provider and diagnostics. `LoadModel` extracts the existing loading body to avoid recursive public initialization; no new resource system, persistence helper or dependency was introduced.

## Findings and fixes

### F1 — P1: recursive initialization can overflow the stack

- **Trigger:** enable `_loadOnAwake` with any nonblank `_saveKey`, even when no saved value exists. `EnsureInitialized -> Load -> EnsureInitialized` repeats before `_initialized` is set. Independently, a `BuildModel` reactive or `OnPoolsChanged` listener that reads/spends through the component can reenter initialization.
- **Consequence:** repeated model creation, repeated callbacks, lost mutations, or stack overflow during scene startup.
- **Source:** original `EnsureInitialized`, `Load`, and `BuildModel` notification ordering.
- **Fix:** build the complete model silently, apply initial saved data through private `LoadModel`, attach model handlers, set the initialized flag, publish initial reactive values, then invoke `OnPoolsChanged`. Initial restoration does not emit damage/death events. Repeated initialization calls retain the model.
- **Regressions:** `Initialize_LoadOnAwake_PublishesSavedStateExactlyOnce`, `Initialize_PoolsChangedListener_CanSpendWithoutRebuilding`, `Initialize_ReactiveListener_CanReadWithoutRebuilding`. They assert saved values, callback counts and retained spending, rather than merely successful return. Callback guards bound the old rebuild recursion in the principal reproductions.

### F2 — P2: duplicate/stale reactive notifications

- **Trigger:** damage, heal, restore or max adjustment. The model notifies `HandleResourceChanged`, then the wrapper synchronizes again. `HandleResourceChanged` originally invoked `OnChanged` before synchronization. Zero-amount calls and healing a full pool also forced unchanged reactive values.
- **Consequence:** duplicate UI work/effects; listeners observe previous current/max/percent values despite receiving a new event payload; unchanged maximum values are repeatedly broadcast.
- **Source:** explicit post-mutation `SyncReactiveFor` calls, unconditional `ForceNotify`, and handler ordering.
- **Fix:** model notifications perform synchronization once; assign all three pool reactive values before invoking reactive listeners and `OnChanged`. Notify only changed reactive values, with an explicit initial publication. Final explicit-load reconciliation does not duplicate already synchronized changes. The model's own `OnChanged` emission policy is preserved.
- **Regressions:** five `Mutation_SyncsReactiveOnce_BeforeChangedEvent` cases cover decrease/increase/restore/max/spend, including event payloads, callback-visible state and zero-effect follow-up calls; `Increase_AtMax_DoesNotRepublishUnchangedReactiveStates`; `Load_RestoresOnce_WithoutDamageOrDeathEvents`.

### F3 — P2: early condition properties return constructor defaults

- **Trigger:** read HP/Mana convenience properties before `Awake` or another API initializes the component. Inspector `current/max` can differ from `ReactivePropertyFloat` constructor defaults.
- **Consequence:** conditions and code see HP `100` instead of configured `40`, or mana `100` instead of configured `12`.
- **Source:** `GetPoolCurrentValue`, `GetPoolPercentValue`, `GetPoolMaxValue` bypassed lazy initialization.
- **Fix:** all three accessor helpers initialize before reading live state.
- **Regression:** `ConditionProperties_BeforeAwake_ReadConfiguredPools` checks all six properties with distinct HP/Mana values and fractions.

### F4 — P2: max-change event reports an unapplied value

- **Trigger:** `SetMaxHp(-5)`; the model accepts a normalized maximum of `0`.
- **Consequence:** `OnChangeMax` listeners receive `-5` while the model and reactive maximum are `0`.
- **Source:** `SetMax` forwarded its raw input to the event.
- **Fix:** publish the model's applied maximum after synchronization.
- **Regression:** `SetMax_Negative_ReportsAppliedMaximum` observes event payload, callback-visible maximum and reactive max/percent. It does not impose a new policy for the current value of an uncapped pool.

### F5 — P2: malformed authoring slots prevent valid pools from working

- **Trigger:** null `_pools`, a null element preceding a valid entry, or null public reactive objects on an inspector entry.
- **Consequence:** initialization, lookup or save throws before valid pool state can be used/persisted.
- **Source:** direct list/entry/reactive dereferences in `BuildModel`, `SyncAllReactive`, `TryGetPoolEntry`, `Save`, and synchronization.
- **Fix:** normalize a missing list to empty; ignore null/blank slots consistently; recreate absent reactive properties before synchronizing. Valid authoring fields, event objects and serialized names are preserved.
- **Regressions:** `Initialize_NullAndBlankEntries_SaveOnlyValidPools`, `Initialize_NullPoolList_SavesAnEmptyProfile`, `Initialize_MissingReactiveObjects_RestoresLiveState`. Assertions inspect real saved JSON, pool values and subsequent damage updates.

### Boundary and lifecycle review

- `ResourceEntryInspector` remains scene configuration plus reactive/event outputs. Numeric authoring fields are copied once; editing them at runtime is not a supported rebuild API. Public methods provide the live mutation path. Directly overwriting reactive values is not a model mutation.
- `Update` delegates scaled-time regeneration/healing to the model; no additional scene search, coroutine, runtime allocation loop or global singleton was added. Disabled components retain their initialized model. `OnDestroy` unsubscribes both model handlers. Caller-owned reactive subscriptions still need disposal/removal.
- Persistence preserves configured rules, restores only known ids, and uses `SetCurrent` to bypass damage caps/death effects. Explicit loads apply max/current separately and can expose intermediate values; initial load is published only after completion. Provider I/O exceptions outside JSON application and arbitrary user-listener failures are not converted into a new recovery protocol.
- Existing load warnings use `NeoDiagnostics.LogWarning`; the dependency gates warnings on `RuntimeWarningsEnabled`. No new runtime logging or version-sensitive Unity object-id API was added.
- Case-insensitive **unique ids are required**. Existing duplicate-id ambiguity remains a configuration limitation: the model replaces the last matching pool while inspector lookup selects the first matching entry. Defining a rejection/migration policy is included in the proposal rather than silently changing existing serialized pool precedence.

### Cross-boundary observations for coordinator

The initially inspected `Neo.Core` model accepted non-finite amounts, threw for null ids, and could return successful `TrySpend(20)` after removing only `5` under a decrease cap. Those files changed concurrently during this audit. A targeted subsequent inspection confirmed finite/null guards, rejection of underpayment, current/max normalization and uncapped-profile sanitization in the other worker's source. **These are not fixes made or runtime-verified by this worker.** Integrate and centrally verify them with this component; do not attribute the dependency changes here. No dependency/config edits are requested by this patch.

## Documentation

Updated the existing English canonical [HealthComponent.md](../../../Assets/Neoxider/Docs/Core/Resources/Components/HealthComponent.md), without adding a competing entry point:

- typed C# example using `IResourcePoolProvider` and existing component methods;
- scene/NoCode `Damage`/`Heal`, normalized UI binding and per-pool events;
- lazy initialization, ready-event timing and reactive notification semantics;
- save-key ownership, initial/explicit loading, disable/destroy behavior and intermediate explicit-load states;
- null authoring guards, unique-id/configuration requirements, subscription ownership and absence of a public runtime pool catalog.

Кратко: устранены рекурсивная инициализация, повторные уведомления и рассогласование событий с текущим состоянием. Сцены, публичные сигнатуры и имена сериализуемых полей сохранены; запуск Unity оставлен координатору.

## Substantial improvement proposal

**Proposed only: typed pool catalog and explicit configuration boundary.**

**Problem.** The component implements a usable mutation interface, but programmers cannot enumerate custom pools, retrieve their observable state, or configure a pool set through public C#. `_pools` is private while `ResourceEntryInspector` exposes mutable authoring and live-state objects together. Thus code-first UI needs a separately held inspector entry, reflection, polling, or a parallel model. Duplicate-id collisions have different effective authoring/event identities. There is no place to report all configuration errors before a scene starts.

**Architecture / API.** Reuse `ResourcePoolModel` as the runtime core. In cooperation with its owner, introduce optional interfaces without changing `IResourcePoolProvider` implementers:

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

Add an explicit `Initialize(IEnumerable<ResourcePoolDefinition> definitions)` path using plain data definitions and a validation result with id/index/reason. Validate the complete set before replacing any live model: duplicate ids under ordinal case-insensitive comparison, blank ids, non-finite values and invalid timing configuration. Keep observable state read-only to consumers. `HealthComponent` adapts its existing serialized entries to the same path; existing UnityEvents/reactive states remain the scene outputs. Runtime C# can subscribe through the catalog without gaining mutable access to serialized configuration. Asset definitions, if later offered, contain data only and no scene object references.

**Compatibility / migration.** Preserve `_pools`, all existing entry fields, `IResourcePoolProvider`, `Damage`/`Heal` and current event signatures. Keep `EnsureInitialized` as the existing-scene adapter. First ship read-only validation/editor warnings for legacy duplicates; make rejection the contract of the new explicit initializer. Do not rewrite existing scenes or choose a new duplicate precedence silently. Saved ids/current/max retain their schema. Dynamic add/remove and atomic save-profile application should be separate optional work, with their event contracts specified before implementation.

**Acceptance scenarios.**

1. Plain C# configures HP and stamina, retrieves stamina by case-insensitive id and observes the exact post-spend values without a scene search or reflection.
2. Existing serialized HP/Mana scenes retain values, event bindings, save keys and initial notification timing.
3. `HP` / `hp`, blank ids, NaN values and invalid regen timing produce indexed validation errors; a rejected configuration leaves the existing model/subscriptions intact.
4. UI code enumerates ids and unsubscribes cleanly; read-only state cannot bypass damage limits by writing a reactive property.
5. The same contract tests cover explicit initialization and the scene adapter; missing state lookups return false without creating pools.

**Effort / risk.** About 2–4 engineering days including pure-core contracts/tests, component adapter, validation UI and bilingual documentation. Medium risk: lifecycle/event compatibility and coordination with `Neo.Core` matter more than the interface code. Keep the first release additive; migrate duplicate configurations only through a separately reviewed policy.

## Verification and remaining risks

### Added regression inventory

- [Audit20261023NeoCoreResourcesComponentsHealthComponentTests.cs](../../../Assets/Neoxider/Tests/Edit/Core/Audit20261023NeoCoreResourcesComponentsHealthComponentTests.cs): **15 cases** (10 standalone tests plus five parameterized mutation cases), in the existing Editor test assembly.
- New `.cs.meta` GUID: `a6f4e276c34f4eb78c3cc93746c5a951`; a package-wide exact GUID search found one occurrence.
- Reflection is confined to fixture setup of private Inspector configuration. Tests invoke public methods and observe real events, live values and persisted JSON. Each persistence test uses a unique key, disables component auto-save and deletes its key during teardown. No provider replacement or existing test edits.

### Checks actually run

1. `git diff --check --` scoped to the changed source, canonical doc, new test/meta and this report: passed.
2. Inline Python static consistency checks: strict UTF-8, no replacement characters or trailing whitespace; inventory matches both owned C# files; the unchanged `ResourceEntryInspector` SHA-256 matches its initial snapshot; public declarations and all five serialized field declarations match `HEAD`; explicit local declarations; balanced C# delimiters; no obsolete object-id calls. These are static checks, not compiler validation.
3. Verified 15 declared regression cases and the required unique filename prefix. Existing `Neo.Editor.Tests` already references `Neo.Core`, `Neo.Core.Resources.Components`, `Neo.Reactive` and `Neo.Save` and targets Editor; no asmdef edits needed.
4. Checked the owned canonical documentation's relative links and scoped worktree changes. Only the owned component/doc/report and new prefixed test/meta are changed by this worker.

**Not run:** Unity, MCP, import, compilation, Play Mode, EditMode tests, builds, test runners or installs, as required by the worker contract. No commits or shared configuration changes.

**Coordinator actions / remaining risks:** compile and run the new fixture plus existing `HealthComponentTests` against the final concurrent `Neo.Core`, `Neo.Reactive` and save changes on supported Unity versions. Confirm serialized reactive listeners/UI binding and auto-save in a scene. Model zero-max semantics now represent uncapped pools; the max-event regression deliberately checks applied max/percent without imposing a contradictory current-value policy. Explicit-load atomicity, arbitrary user callback failures and legacy duplicate configurations remain documented limitations. Static validation cannot establish Unity serialization/runtime correctness.
