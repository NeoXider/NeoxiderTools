# Neo.Core.Resources.Components — implementation receipt

Status: **source-final; acceptance UNEXECUTED**. Original actor/session `neo-audit-1023-b01-05`, `gpt-6.1-sol`, reasoning `max`. Approved target: `demo` / `11.0.0`.

## Assigned action completed

**HEALTH-INITIAL-CROSSPOOL (P2), NativeReview-A A-06.** Previously, HP's first reactive callback could read Mana's constructor mirrors `100 / 100 / 1` while the model already held `12 / 48`. A missing later-pool reactive object was not yet recreated either.

`EnsureInitialized` now completes a silent synchronization pass over the valid pool entries after model creation/optional saved-state application and before subscribing/publishing. This prepares all mirrors and recreates missing reactive objects before the first external listener runs. Model handlers and the initialized guard are then installed as before; the existing forced initial publication follows in pool-list order. Its synchronization reads current model values, preserving spending performed by an earlier callback rather than replaying a stale staged value.

The coherent audit fixes already present on resumption were retained. This implementation does not reapply the old patches or rewrite historical verification claims.

## Exact changed files and APIs

- `Assets/Neoxider/Scripts/Core/Resources/Components/HealthComponent.cs`: added the silent initial pass; private `SyncReactiveFor` and `SyncAllReactive` accept an optional `bool notify = true`. They still assign state when notification is suppressed. Normal mutations and explicit `Load` retain their existing notification path.
- `Assets/Neoxider/Tests/Edit/Core/Audit20261023NeoCoreResourcesComponentsHealthComponentTests.cs`: appended four parameterized cross-pool cases and one callback-spend case to this worker's existing regression file. The fixture now declares **20 cases**, including the previous 15.
- `Assets/Neoxider/Docs/Core/Resources/Components/HealthComponent.md`: documented preparation of every initial mirror, initial publication order, and callback mutations retaining their normal notifications. Existing C# / scene / NoCode instructions and module backlink remain the entry point.
- This implementation receipt.

No additive public API was required. Public signatures, all serialized fields/event objects, save schema and existing asset bindings are preserved. `ResourceEntryInspector.cs` requires no edit. Existing package metadata is reused; no new package files, GUIDs or assembly references are needed.

## Authored acceptance — UNEXECUTED

`Initialize_FirstHpNotification_SeesAllManaMirrors` covers the four combinations of authored/auto-loaded values and present/missing Mana reactive objects. It observes:

1. HP's first current callback reads authored Mana `12 / 48 / 0.25`, or saved Mana `21 / 56 / 0.375`, through both convenience accessors and inspector mirrors, consistent with model getters.
2. Missing reactive objects already exist at that first callback. Saved HP is also the value initially published.
3. Nested/repeated `EnsureInitialized` does not rebuild or republish. Each pool publishes current, percent and max once in list order, followed by one ready event.

`Initialize_FirstHpListener_CanSpendLaterPoolWithoutOverwritingResult` observes Mana `12` before a callback spends `5`, then verifies model/mirror value `7`, matching percentage and successful spending. Both the normal spend notification and the later initial Mana notification expose `7`; readiness remains singular.

Assertions observe public methods, real reactive/event outputs and saved JSON data. Reflection remains limited to private Inspector setup. The existing inactive-object/autosave settings, unique save keys and exact teardown cleanup are retained. No unavailable NUnit serialization annotation was introduced; eventual execution uses the coordinator's isolated/exclusive runner contract.

## Compatibility, dependencies and scope

The deliberate behavior correction is that early listeners can read all initialized pools instead of stale constructor defaults or missing reactive references. Existing per-pool initial order, callback reentry guard and final `OnPoolsChanged` remain intact.

Explicit `Load` remains non-atomic and can expose separate maximum/current applications. Legacy duplicate-id precedence remains unchanged and unique ids remain required. The read-only catalog/configuration framework and first-entry index/performance lead remain proposals; neither was needed for this action.

There is **no pending additive API dependency**. Final integrated evidence still depends on the `Neo.Core` model/DTOs, `Neo.Reactive` notification implementation and `Neo.Save` provider owned by their respective actors. No cross-owner source, shared configuration, trackers, sample layout or existing legacy tests were changed here.

## Actual inspection and remaining evidence

Performed sequential small `cmd.exe` source/request reads, owned source/doc diffs, regression-source inspection and SHA-256 artifact bookkeeping. No Unity/MCP, compiler, build, test runner, product validator/checker, installer, service, commit/push or child actor was launched. Historical static-check results in the audit are not current integrated passes.

Source-final artifact SHA-256:

| File | SHA-256 |
| --- | --- |
| `HealthComponent.cs` | `dae0c8ea13525cc2dff628750e345e42714a9ac47e0e977673346fa21eff20b1` |
| Prefixed regression source above | `e0eb280f51ce356cd29d199944075d0a8ddbe9e2a735e6684914fcb893ce67ce` |
| Canonical `HealthComponent.md` | `7958f0bcab0ccbc2fa9bd9453938a848940581f5c6857e2418df35da9c1a0aca` |

Historical audit and plan remain byte-identical to the root-reviewed artifacts: audit `98281c1c70dbb7b29f43e6868454ca5a9ae67f901c855696bdf9f7e20ef475a3`; plan `f3fa0c8d967660d1649391cf7b0ced83bcd33995701c8cd159f08979f69c9d53`.

After every writer finishes, coordinator must compile/discover/run the current fixture with existing HealthComponent/Core resource tests on the frozen integrated source, then verify real Update regeneration, initial saved UI/serialized event binding, disable/save/re-enable and destruction cleanup. Unity 6.3/6.6 and dependency configurations follow the global acceptance contract; other unsupported/unavailable branches require explicit evidence debt. This receipt closes source work only.
