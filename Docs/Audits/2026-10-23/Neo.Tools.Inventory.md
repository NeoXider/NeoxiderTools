# Neo.Tools.Inventory: audit

Status: owned source audit, fixes, regression authoring and public documentation complete. Compilation and test execution remain centrally pending.

- Base revision: `ca198556`; package `10.19.0`.
- Assigned model/reasoning: `gpt-6.1-sol` / `max`.
- Assembly: `Assets/Neoxider/Scripts/Tools/Inventory/Neo.Tools.Inventory.asmdef`.
- Phase: audit. The later manifest plan pointer does not change this session into the plan phase.
- Ownership: all 39 manifest C# files, nine existing module documentation pages, this report, and four new prefixed C# test/support files with unique metadata. No nested assembly code, shared tests, asmdefs, configuration, scenes, prefabs, samples or other workers' docs were edited.
- No Unity/MCP/build/test processes, installs, child agents or git mutations were started.

Кратко: проверены все 39 файлов; исправлены создание лишних предметов, обход лимитов, переполнения, потеря изменений раскладки при сохранении, ошибки загрузки, подбора, выброса и UI. Регрессионные тесты добавлены, но их исполнение и компиляция ожидают координатора.

## Full source review coverage

Every file below was read in full during this session; this is retained coverage from the original audit, not a restarted review after watchdog continuation. Paths are relative to `Assets/Neoxider/Scripts/Tools/Inventory/`.

| File | Reviewed contracts and outcome |
|------|--------------------------------|
| `Core/AggregatedInventory.cs` | All record operations, mixed stacks/instances, limits, cloning, removal and packed indexing; F02-F04. |
| `Core/IInventoryItemState.cs` | Capture/restore and unique-key contract; no change needed. |
| `Core/IInventoryStorage.cs` | All public storage contracts; corrected AddInstance return-count documentation. |
| `Core/ISlottedInventory.cs` | Physical indices, insertion/extraction, swaps, snapshots and unchecked SetSlot contract. |
| `Core/InventoryConstraints.cs` | Non-positive limits, per-id overrides and deep-copy isolation. |
| `Core/InventoryEntry.cs` | Mutable legacy DTO and negative/non-positive import handling; corrected constructor XML documentation. |
| `Core/InventoryItemComponentState.cs` | Null keys/payloads and cloning; string values are immutable. |
| `Core/InventoryItemInstance.cs` | Identity creation, normalized counts, null component lists and deep copies. |
| `Core/InventoryItemRecord.cs` | Instance/simple resolution, null construction, count normalization and clone semantics. |
| `Core/InventoryManager.cs` | Legacy id semantics, ordered snapshots, replace/add/remove and capacity arithmetic; F03. |
| `Core/InventorySaveData.cs` | All format fields, legacy entry/instance paths, slots/records and merge marker; schema unchanged. |
| `Core/InventorySlotState.cs` | Empty/instance resolution, clearing and defensive conversion/cloning. |
| `Core/InventorySlotTransferRules.cs` | All merge/move/swap/extract/insert branches; F01-F02. |
| `Core/InventoryStackRules.cs` | Stack/global boundaries, unlimited limits and representable totals; F03. |
| `Core/InventoryStorageMode.cs` | Serialized enum values and compatibility; unchanged. |
| `Core/InventoryTransferService.cs` | Same-container/cross-container routing, validation, notification and rollback ordering; F06. |
| `Core/SlotGridInventory.cs` | Every storage/slot method, resize, invalid inputs, cloning, capacity and transfer math; F01-F03. |
| `Data/InventoryDatabase.cs` | Null entries, lazy cache, duplicate-id diagnostics and item lookup; editor warnings are gated. |
| `Data/InventoryInitialStateData.cs` | Reusable entry configuration and public read-only list surface. |
| `Data/InventoryItemData.cs` | Item metadata, prefab reference, stack normalization, instance flag and preview lookup. |
| `Runtime/HandView.cs` | Offsets, base scale and minimum clamp; no change needed. |
| `Runtime/InventoryComponent.Grid.cs` | Every physical operation, database/global validation, payload changes and paired commit; F02-F03/F05-F06. |
| `Runtime/InventoryComponent.Operations.cs` | All public operations, event deltas, capacity rejection, auto-save and Inspector buttons; F05/F08-F09/F13. |
| `Runtime/InventoryComponent.Persistence.cs` | All load strategies, legacy/current snapshots, initial content, layout restoration and callbacks; F07-F08/F13. |
| `Runtime/InventoryComponent.Queries.cs` | All queries, packed/physical distinction, instance snapshots and extraction; no direct changes needed. |
| `Runtime/InventoryComponent.cs` | Singleton opt-out/fallback, Awake/Init recursion, initialization, subscriptions, limits, explicit links and events. |
| `Runtime/InventoryDropper.cs` | Every entry point, key input, prefab checks, extraction, physics, restore, error paths and callbacks; F10. |
| `Runtime/InventoryHand.cs` | Enable/disable/destroy, selector synchronization, equip/use/drop, physics and owned visual lifetime; F12; proposal P01. |
| `Runtime/InventoryItemStateBehaviour.cs` | Optional key override and abstract typed capture/restore API. |
| `Runtime/InventoryItemStateUtility.cs` | Inactive-child capture/restore, null payloads and last matching key behavior; unique keys remain caller configuration. |
| `Runtime/InventoryPickupBridge.cs` | Awake fallback and all typed collection forwarding methods; NoCode wiring reviewed. |
| `Runtime/PickableItem.cs` | All collection entry points, collector/tag validation, partial capacity, reentrancy, reuse and child physics; F09. |
| `Runtime/ShopInventoryGrantEvent.cs` | Serializable payload remains independent of optional bridge assembly; replaced cross-assembly XML cref with text. |
| `UI/InventoryItemCountText.cs` | Enable/disable/rebind listeners, missing references, item selection and format contract. |
| `UI/InventoryItemView.cs` | Bind/Clear, optional icon/name/count/root and reuse; F11. |
| `UI/InventorySlotGridView.cs` | Static reset, selection/rebind, lifecycle, subscriptions, manual/prefab slots and transfer; F11-F12. |
| `UI/InventorySlotView.cs` | Empty/occupied binding, highlights and click forwarding; restored ItemView reuse fixes this flow. |
| `UI/InventoryTotalCountText.cs` | All view modes, missing references, enable/disable/rebind and formatting; selection-only changes still need explicit Refresh. |
| `UI/InventoryView.cs` | All display modes, source lists, stale caches, subscriptions and object ownership; F11-F12. |

Dependencies were inspected read-only where needed: `SafeDestroy`, `NeoDiagnostics`, SaveProvider/ISaveProvider, singleton configuration, existing inventory tests, and current test/runtime assembly references. Ready-made `InventoryStackRules` and `Neo.Extensions.SafeDestroy` were reused. No replacement framework or runtime save provider was introduced; the memory provider is a test double.

## Demonstrated findings and completed fixes

The evidence below is a baseline source trace plus authored observable regressions. It is not a claim that C# tests ran. Source locations refer to the final working files.

### F01 — High: slot merge could create items

`Core/InventorySlotTransferRules.cs:14`. A source stack of 2, target stack of 3, cap 10 and an oversized positive request previously moved 7 units: target became 10 and the source disappeared, creating 5 units. Merge requests now clamp to source availability. Moving into an empty slot respects the destination cap and leaves a remainder. Aliased low-level source/target objects return zero. `MoveSlot_ClampsRequestToSource_AndConservesItems`, `MoveToEmptySlot_AfterStackLimitChange_LeavesRemainderInSource`, and `SlotRules_RejectNegativeIds_AndAliasedMoves` assert actual counts and returned movement.

### F02 — High: direct slot insertion bypassed constraints; invalid instances could become unremovable

`Core/SlotGridInventory.cs:443` and `Runtime/InventoryComponent.Grid.cs:98`. Slot insertion previously ignored global total/unique limits, empty targets ignored stack caps, and the facade ignored database restrictions. `SlotGridInventory.AddInstance` accepted negative ids even though removal rejects them. Insertions now check all applicable limits/id rules and reject an instance when the requested count cannot fit atomically. Complete replacements/imported slot rows validate negative instance ids too. Core insertion tests and `SlotInsert_RespectsDatabaseRestriction` assert accepted quantities, empty rejected slots and unchanged totals. Raw `SlotGridInventory.SetSlot` remains explicitly unchecked by contract.

### F03 — High: unchecked count addition wrapped totals and bypassed caps

`Core/InventoryStackRules.cs:33`, `AggregatedInventory.AddInstance`, `SlotGridInventory.AddInstance`, `InventoryManager.CalculateAddableAmount`, and `InventoryComponent.CanApplySlotState`. Adding another item after total `int.MaxValue - 1` could wrap `TotalCount`; `TotalCount + instance.Count` could also wrap before a configured-cap check. Shared capacity arithmetic now limits representable totals even when configuration is unlimited. Whole-instance adds reject overflow, the legacy manager reuses that arithmetic, and replacement validation sums in `long`. Boundary regressions verify exact accepted counts, unchanged rejected state and recovery after removal.

### F04 — Medium: an existing instance id incorrectly consumed another unique-id allowance

`Core/AggregatedInventory.cs:56`. With unique limit 1 and one instance of id 7, adding a simple stack of id 7 was rejected because no simple record existed. The unique check now asks whether the logical id is already present. `Aggregated_AddStackForExistingInstanceType_DoesNotUseAnotherUniqueItem` verifies combined count 3, one unique id, and rejection of a different id.

### F05 — High: layout/payload-only changes neither refreshed UI nor auto-saved

`Runtime/InventoryComponent.Operations.cs:236` and `.Grid` callers. Finalization compared only per-id totals; moves, swaps and equal-count payload replacements were invisible. Successful physical writes now carry a content-change signal while item delta events still describe count changes only. Regressions assert one inventory notification, zero false count deltas, and actual saved slot/payload JSON. Invalid self-moves remain silent.

### F06 — High: cross-container transfer published temporary state and could duplicate items on listener failure

`Core/InventoryTransferService.cs:17` and `Runtime/InventoryComponent.Grid.cs:113`. The target previously mutated/published before source validation/commit. A rejected swap emitted temporary target changes; a throwing target listener could leave both source and target holding the item. Both endpoint candidates now validate before either write, then both contents commit before callbacks. Regressions observe final source/target ids and conserved combined totals from both listeners, silence on rejected swaps, and one total item after a listener throws. Notification/provider durability is not a cross-provider transaction; see remaining risks.

### F07 — High: saved storage mode/capacity did not rebuild the backend

`Runtime/InventoryComponent.Persistence.cs:137`. Import changed `_storageMode`/`_slotCount` while `EnsureStorage` retained the old object. Loading a grid into an aggregated component could discard slots; loading a larger grid into a smaller one truncated contents while reporting saved metadata. Current snapshots now restore storage before import/initial merging. Complete saved slot arrays determine capacity. Legacy entries/instances retain configured layout, including grid mode, and empty modern grids restore correctly. Tests cover both mode directions, capacity changes, physical placement, identity/payload preservation, legacy entries and zero-capacity grids. Invalid/over-limit imported slot rows are skipped through the same replacement validator.

### F08 — Medium: persistence callbacks and initial-only options contradicted their contracts

`InventoryComponent.Save`, `Load`, and `TryReadSaveData`. `OnSaved` was never raised; a blank key prevented initial-content loading; `InitialOnlyIgnoreSave` still read the provider. `OnSaved` now follows the JSON write, blank keys skip persistence while allowing initial content, and initial-only loading performs no provider read. Auto-save regressions observe the callback and persisted JSON; initial-only tests assert contents and zero provider reads with both blank/nonblank keys. XML references were corrected without renaming any field or format member.

### F09 — High/Medium: pickup lost partial remainder, allowed reentrant grants, and failed reuse/child-collider handling

`Runtime/PickableItem.cs:160`. Any positive accepted amount previously marked the entire pickup collected/despawned, losing rejected units. A one-level reentrant `OnCollectStarted` listener could grant twice before `_wasCollected` was set. `Configure` did not reset that latch, and collider disabling searched only the root. Collection now guards the entire request, preserves partial quantity/colliders, finishes/despawns only after full collection, re-arms on Configure, and disables collider descendants including inactive children. Stateful pickup and `AddItemData` stop after the first rejected instance instead of continuing futile requests. Regressions verify simple/stateful remainder quantities, sequential accepted counts, one rejection per attempt, no nested grant, reuse, and disabled 3D/2D child colliders. Pool code remains responsible for activation and collider reset on reuse.

### F10 — High/Medium: drop entry points bypassed the master flag and failure handling changed/duplicated contents

`Runtime/InventoryDropper.cs:231` and `DropSlot`, `DropRecord`, `SpawnDroppedItem`. Packed/physical/first/last paths could drop with `CanDrop == false`. Missing-prefab index drops extracted/re-added contents, moving a physical slot into the first empty slot and emitting temporary mutations. A success listener exception was caught as a spawn failure and re-added an item already spawned; restore exceptions leaked the partially configured clone. All entry points now gate dropping, prefab resolution precedes extraction, success callbacks run after the spawn failure boundary, and partial clones use existing SafeDestroy cleanup. Tests assert untouched disabled inventories, unchanged slot placement/no mutation on missing prefabs, no re-add after a throwing success listener, and clone-count/instance restoration after an injected restore failure. Negative physical-drop amounts are rejected.

### F11 — Medium/High: UI cache/rebind lifecycle produced stale or unintended behavior

`InventoryItemView.Bind`, `InventoryView.RefreshSpawnMode`/`Subscribe`, and `InventorySlotGridView.SetInventory`/`Subscribe`. Cleared item views with no Root never reactivated. Removed snapshot ids remained visible because refresh only visited current ids. Rebinding disabled views subscribed them to future mutations; rebinding a selected grid retained its transfer selection and could move items from the new container unexpectedly. Bind now restores visibility, absent cached ids hide, disabled rebinds do not subscribe, and grid rebind cancels selection. View regressions assert visibility, rebinding between inventories, absence of updates while disabled, and unchanged inventory state after a stale-selection scenario.

### F12 — Medium: externally parented generated objects outlived their owners

`InventoryHand.OnDisable`/`OnDestroy`/`DestroySpawned`, `InventoryView.OnDestroy`, and `InventorySlotGridView.OnDestroy`. Hand visuals and spawned rows/slots under external anchors/roots were not owned by the component hierarchy and survived owner destruction. The hand now releases its visual on disable/destroy/unequip; views release only prefab-created objects. Grid ownership is tracked separately from Manual Slots. Existing `Neo.Extensions.SafeDestroy` handles edit/play destruction. Regressions assert external-root child counts, unrelated-child survival and manual-slot preservation. Actual Unity lifecycle execution remains pending.

### F13 — Medium: runtime Inspector buttons could mutate/save data in Edit Mode

All 13 buttons in `InventoryComponent.Operations` and `.Persistence` now specify `PlayModeOnly = true`. Direct C# APIs remain callable. This is an authoring guard correction; static attribute inspection was used instead of adding a source-text or implementation-mirroring test. Existing shared button infrastructure was not changed.

## Regression files

All new files are under `Assets/Neoxider/Tests/Edit/Tools/Inventory/` and use the exact assigned prefix. Each has a new unique GUID metadata file. Existing `InventorySystemTests.cs` is owned by another worker and was not edited, including changes observed during continuation.

- `Audit20261023NeoToolsInventoryCoreTests.cs`: 18 NUnit cases.
- `Audit20261023NeoToolsInventoryFixture.cs`: shared nonparallel fixture, inactive/configured component setup, disposable test objects, and a scoped memory-provider double with previous-provider restoration.
- `Audit20261023NeoToolsInventoryRuntimeTests.cs`: 33 NUnit cases.
- `Audit20261023NeoToolsInventoryViewTests.cs`: 9 NUnit cases.

Total: **60 authored NUnit cases, not executed**. Tests observe real counts, records, saved JSON, events, visibility, object ownership or rejection state. They do not assert source strings or only absence of exceptions.

## Documentation

Updated existing English pages under `Assets/Neoxider/Docs/Tools/Inventory/`:

- `README.md`: canonical entry point, plain C# example, scene setup, return counts and persistence boundaries.
- `InternalTypes.md`: correct public-core descriptions, capacity/id/clone rules and packed/physical distinction.
- `InventoryComponent.md`: actual ClearInventory API, typed slot methods, events, Inspector usage, legacy/current saves and blank-key behavior.
- `PickableItem.md`: default collector validation, partial remainder, callbacks, reuse and child colliders.
- `InventoryDropper.md`: all-entry-point gate, amount semantics, preflight and spawn/success failure boundaries.
- `InventoryView.md`, `InventoryItemView.md`, `InventorySlotGridView.md`: reuse, stale rows, disabled rebinds, selection and generated-object ownership.
- `InventoryHand.md`: owned visual lifetime and dropper gating.

The optional `ShopInventoryGrantBridge.md` topic and all other worker-owned docs were left untouched. Two owned XML descriptions were corrected: the legacy InventoryEntry constructor accepts its supplied count, and ShopInventoryGrantEvent documents the optional bridge with text rather than an unresolved assembly cref.

## Substantial improvement proposal P01 — typed equipped-instance state editing

**Not implemented in this audit.**

**Problem.** The core deliberately returns clones and offers no typed identity-based update API. InventoryHand restores payload into its visual, but UseEquippedItem can change a weapon's ammo/durability there without committing that state to storage. Refreshing selection, unrelated inventory changes, dropping or saving can therefore reuse the previous stored payload. The current alternative requires manual slot replacement and physical/packed index knowledge; aggregated instances lack an equivalent direct update path.

**Proposed runtime architecture/API.** Add an optional plain C# `IInventoryInstanceEditor` capability implemented by both backends. Expose `TryGetInstance(string instanceId, out InventoryItemInstance snapshot)` and `TryReplaceInstanceState(string instanceId, int expectedRevision, IReadOnlyList<InventoryItemComponentState> states, out InventoryInstanceUpdateResult result)`. Preserve ItemId/InstanceId/Count during state replacement, clone incoming payloads, validate stale revisions, and report typed rejection reasons. Return a small `InventoryChangeSet` identifying payload changes independently of count deltas. InventoryComponent adapts this to `OnInstanceChanged`/existing inventory notification and auto-save. Add explicit `InventoryHand.CaptureEquippedState()` that captures the current visual while retaining the stored identity, runs before visual replacement/drop and before component Save through an explicit adapter dependency, and never stores scene references in ScriptableObjects. Avoid suppressing all external callbacks globally; publication and revision checks should be explicit.

**Compatibility/migration.** Keep all current methods, fields and version-1 payload records readable. The capability is additive; components without it keep existing behavior. Start revisions at zero for legacy records; if persisted revisions are introduced, add an optional schema field with a documented default rather than renaming InstanceId or component keys. Keep auto-capture configurable for applications treating hand visuals as presentation-only. Decide state-key uniqueness and splitting of multi-count instance records explicitly before enabling identity edits.

**Acceptance scenarios.** (1) Two stateful items of the same type keep independent ammo after equip/use/switch. (2) Using two rounds, moving slots, dropping, picking up and save/reload preserves the remaining ammo and original instance identity. (3) A stale revision rejects without partial payload changes or duplicate events. (4) Payload-only replacement emits one inventory change/save and no count delta. (5) Both backends are testable without a scene; destroying a hand releases only its visual. (6) Legacy saves and presentation-only hands retain existing behavior.

**Effort/risk.** Approximately 2–4 engineering days plus focused Edit/Play verification. Medium risk: capture ordering versus listeners, state-key collisions, revision persistence, stacked-instance identity, and equip/drop reentrancy. Prototype the plain C# contract and regression scenarios before authoring integration. This is a capability proposal, not a claim that these scenarios now pass.

## Actual verification and remaining risks

Performed read-only/static validation in this worker session:

1. Exact active assembly/source inventory matched all 39 manifest paths; no nested assembly was included.
2. Strict UTF-8 reads of all 39 sources, four new C# files, four metadata files and nine edited docs (56 files); no replacement characters or checked Latin-1 UTF-8 corruption markers. Final report is additionally checked as UTF-8.
3. Lexical comment/string-aware bracket balance for all 43 C# files; explicit local types; no direct Debug logging or obsolete identity APIs in owned source/new tests. This is not C# compilation.
4. Compared baseline `ca198556` public declaration lines and serialized field names with all final sources: unchanged. Reviewed scoped diffs for the actual implementation changes.
5. Verified all four new GUIDs are distinct and match only their own metadata throughout Assets. New test/meta whitespace was checked separately because git diff does not validate untracked contents.
6. Resolved local links in all nine edited docs and checked existing runtime/test assembly references needed by these changes. No asmdef additions or configuration edits were needed.
7. Confirmed exactly 13 Play Mode-only runtime buttons and no remaining bare Button attributes in this assembly.
8. Scoped `git diff --check -- <owned sources/docs/tests/report>` completed with exit 0. Final continuation reruns the scoped consistency/whitespace check after writing this report.

**Centrally pending:** compile/import, the three new test fixtures (filter `Audit20261023NeoToolsInventory`), existing inventory Edit/Play suites, and Play Mode smoke for input, actual 3D/2D dropping and enable/disable/destroy callbacks. Neither Unity branch (6.0–6.4 / 6.5+) was compiled here; no runner was launched under the worker prohibition.

**Practical limits:** paired slot contents now commit before events, but synchronous user callback exceptions can still interrupt later notifications or provider writes. Provider durability is not atomic across containers. Failed drop configuration attempts re-add the extracted record through normal add rules; original physical placement is not guaranteed on that exceptional fallback. Arbitrary raw SetSlot callers remain responsible for valid limits. Custom state keys must be unique across the captured hierarchy. Selected-count text should be explicitly refreshed after changing selection alone. P01 covers the broader missing equipped-state update path.

**Coordinator actions / boundaries:** run compilation and the requested test/smoke passes; review notification/save exception policy if stronger durability is needed. No cross-boundary code change or new assembly reference is required for the completed fixes. Inventory's pre-existing optional Network/Mirror assembly references were not changed; dependency cleanup, if desired, belongs to the coordinator. Public APIs, serialized names, samples layout and package configuration were preserved.
