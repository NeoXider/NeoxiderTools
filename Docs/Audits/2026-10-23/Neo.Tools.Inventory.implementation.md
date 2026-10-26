# Neo.Tools.Inventory — implementation receipt

Status: **source-final**. Updated: 2026-10-25. Original actor: `neo-audit-1023-b07-05`.
Branch/target carried forward: `demo` / 11.0.0. Runtime, regression sources, and owned public docs are
complete for the assigned implementation request. Compilation and execution remain pending the
coordinator's global writer freeze; this is not a verification-pass or release receipt.

The completed [audit](Neo.Tools.Inventory.md), [plan](Neo.Tools.Inventory.plan.md), original file-review
coverage, and earlier source edits were retained. Neither historical report was rewritten. No shared
tracker, assembly definition, nested assembly, or release configuration was changed in this phase.

## NI70-01 — slot-wire sentinel correction; source-final, native acceptance UNEXECUTED

Read the complete implementation/common P5 contracts, the current request and all39 lines of
`NativeImplementationReview-70.md` (SHA256
`d82c68a16a46925c0d772243df064d136d3dc51303977d1b370f8f9a81c275bd`). The confirmed
source defect was the shared wire row's missing `ItemId` default0 overriding the slot's existing
-1 sentinel: Count1 plus an empty optional instance could become a simple item0 without a slot id.
This was source reasoning, not an executed native repro.

Only the following four source/docs files and this receipt changed for NI70-01:

| File | NI70-01 source SHA256 |
|---|---|
| `Assets/Neoxider/Scripts/Tools/Inventory/Core/InventorySaveData.cs` | `115f5782731a297f866a4fd339deca3496108e4b9f9cfe092ccf5d8770809ca6` |
| `Assets/Neoxider/Tests/Edit/Tools/Inventory/Audit20261023NeoToolsInventoryRuntimeTests.cs` | `0c74e6a9b6a78ce0077c979e60878bbeb0711eeed2add45e9fe5770bb714360d` |
| `Assets/Neoxider/Docs/Tools/Inventory/InventoryComponent.md` | `8a848c428d2137f1258cbcd487fc7ec80a8dd83f324678fdeb96bfbe4bbf5b1b` |
| `Assets/Neoxider/Docs/Tools/Inventory/InternalTypes.md` | `9756c36efb5b34de648db7eca37e34add295cee8ebd5f4fbba7f6172c8677e79` |

The reviewed receipt baseline was
`7d6d403bb3e8cca66e4f461f24aac99e739fb689a6410f7adebb9e3d3cfd54db`.
The reservation map was checked before each edit. Root's explicit NI70-01 request releases the
original RuntimeTests caller despite its old map entry; extracted helpers and aliases remain intact.

`Slots` now encode/decode through a separate private `WireSlot` with `ItemId=-1`. Records retain
their private wire default0; optional `WireInstance` numeric defaults remain zero. Conversion,
empty-instance matching and admission are otherwise unchanged. All public fields/APIs/domain
defaults, JSON keys/version1, metadata/payloads and invalid-instance precedence are preserved.
Owned guides now distinguish these three defaults. No additional schema/asset migration or shared
release-file edit was needed; Root already integrated the P5 codec handoff separately.

Added four literal JSON decode/load cases, without using the writer to manufacture their input:

- `Load_LiteralSlotWithoutItemIdCannotGrantItemZero`: exact NI70 literal retains decoded id-1,
  raw count1 and null instance; loading grants zero units/records/instances and preserves one slot.
- `Load_LiteralExplicitItemZeroRemainsASimpleSlot`: explicit slot id0/count1 remains a simple
  positive even with the same zero optional instance.
- `Load_LiteralKeylessItemZeroInstanceSurvivesMissingSlotId`: omitted wrapper id stays -1 while
  the real id0/keyless/count1 instance remains authoritative, loads one unit and receives an identity.
- `Load_LegacyRecordWithoutItemIdRetainsZeroDefault`: ordinary literal record Count1 without an
  item id retains the record default0 and loads one simple item0 in aggregated storage.

Actual validation: current UTF-8/source reads, manual baseline source-diff review and SHA256
bookkeeping only. RuntimeTests has additions only; every existing assertion/variant, the nine P5
variants and all20 prior failure-case oracles remain. Persistence remains
`a8985741c3ab1a08e370052d1f25d2f5e3a92f891b43cfd8d1430660a96bcb36`; CommitTests remains
`24369e0fc0905400131d48aed486092cb10b7aacc279c81c1900d2b5716eba30`, retaining all six paired
fault cases and their actual fresh reload assertions. Lifecycle source remains
`b9dfb2fa35aa40636d9d2e2b766c2dddf2881cec18a57973373f4e62fb7641a2`.
No Unity/compiler/test/checker, process control, child, install/service, commit or push ran.
The four sentinel controls, private-wire defaults on both native engines, all P5 corrections and
integrated discovery/provider/cleanup acceptance remain **UNEXECUTED** until all writers/new freeze.
No source dependency blocks reviewer recheck; writes stop with this receipt.

## P5 — nullable inline save semantics; source-final, native correction UNEXECUTED

Action: `P5-INVENTORY-NULLABLE-INLINE-SAVE` (E19–E24). Read the current implementation request,
contract and `Local/Audits/assembly-2026-10-23/p5-source-correction-common.md`, then parsed the
actual Inventory test-case identities, messages, stacks and outputs in both P5 Edit XMLs:

- `unity/final/p5/u63/Receipts/20-full-edit-graphics.xml`: SHA256
  `2fc329874dff049e845eebd01635deb1f445359fd951fe82536c79cb2bb7debd`.
- `unity/final/p5/u66/Receipts/20-full-edit-graphics.xml`: SHA256
  `adeb027f9f14e1c34c8973767f9a6d3420c7c188f809ea26283ebda32d31a74b`.

Both engines have the same 20 assigned failures: six paired-save source-empty failures; six
id-zero/invalid-wrapper failures at the simple first-row count; two invalid-slot cases whose valid
first row became an instance; one callback-free domain probe observing Count1 instead of expected0;
three backend/payload cases losing simple count2; two slot-autosave cases changing simple to instance.
The paired cases reached the source-JSON assertion after their exact fault, both save receipts,
both provider-write counts and committed-state assertions. This does not prove later assertions passed.
The prior exact-zero domain callback alone was insufficient; P5 supersedes that earlier assumption.

Changed exactly these six source/docs files plus this receipt (paths relative to repository root):

| File | P5 source SHA256 (NI70-01 changes listed above) |
|---|---|
| `Assets/Neoxider/Scripts/Tools/Inventory/Core/InventorySaveData.cs` | `8b3767a535f9fb3c70f7938faa83b8d82bc14137c0123f8413a2202d9581032b` |
| `Assets/Neoxider/Scripts/Tools/Inventory/Runtime/InventoryComponent.Persistence.cs` | `a8985741c3ab1a08e370052d1f25d2f5e3a92f891b43cfd8d1430660a96bcb36` |
| `Assets/Neoxider/Tests/Edit/Tools/Inventory/Audit20261023NeoToolsInventoryRuntimeTests.cs` | `d22b7c8dedd0e5cefea6f72cfd3ccc06295bdcbe06f836301ce0d4e77b334ec2` |
| `Assets/Neoxider/Tests/Edit/Tools/Inventory/Audit20261023NeoToolsInventoryCommitTests.cs` | `24369e0fc0905400131d48aed486092cb10b7aacc279c81c1900d2b5716eba30` |
| `Assets/Neoxider/Docs/Tools/Inventory/InventoryComponent.md` | `f1886dada8ad08d4b848241f2529228109e7c5ae92a3a1cedea17f81bafd4258` |
| `Assets/Neoxider/Docs/Tools/Inventory/InternalTypes.md` | `b370da728cc70ab4075ff4b9785e5b60dc2ee1a400f265fe7ee5955d5d7fffd4` |

The receipt baseline was `86803ed9bea0ff3d8bc1e0a55d7ab8c30c258c7936fdd455c87ec1d7d6946e85`.
The reservation map was read before each edit. Its old RuntimeTests entry remains reserved in the
map; the explicit root-released original caller/P5 request authorizes this caller's bounded edits.
Extracted helpers, aliases, shared references, GUIDs and assembly definitions were retained.

Source reasoning and implementation:

- Added public `InventorySaveData.ToJson(bool prettyPrint = false)` and static
  `InventorySaveData.FromJson(string)`; all existing public fields, signatures and domain defaults
  remain. Both encode and decode use private wire types with zero numeric optional-instance defaults;
  NI70-01 separates slot/record row defaults as described above. Existing root/row/instance JSON keys,
  version1 and legacy/merge metadata remain.
- An absent/materialized optional wire instance stays all-zero and becomes null only when identity,
  item id, count, version and payload are all empty/zero. Explicit id0/keyless Count1 remains real.
  Nonempty identity/version/state payload also retains instance precedence. Decode copies raw fields
  without Clone, generated ids or admission normalization. Invalid/overcapacity instances reach the
  existing admission rules and never become their positive wrapper. No count1 normalization was added.
- `InventoryComponent.Save` and `TryReadSaveData` use this same codec. Backend/capacity restoration,
  slot topology, legacy import, observer/finalizer order, pending-return sidecar and state-key policy
  are unchanged. The narrow earlier callback is retained for direct legacy JsonUtility consumers;
  it is not claimed to resolve Count1 domain materialization.
- Both owned fixtures use the production codec. All original semantic assertions and test variants
  remain, including empty saved source, both saves/writes/events/exact exceptions, simple first rows,
  topology/limits, no slot count-delta events, invalid wrappers and exact instance payloads. All six
  paired-fault cases additionally load both saved inventories and assert source empty/zero instances
  and the exact single simple target item in slot2.
- Corrected the callback-free domain probe to the actual P5 Count1 observation. A separate independent
  zero-default wire probe retains the required zero/null control and a real Count1 identity positive.
  Added nine regression variants: missing/null/empty/zero inline representations (4), nonempty
  identity/version/payload retaining raw Count0 and instance precedence (3), and real id0/count1
  identity/version/opaque payload through load/save/load in both storage modes (2).

Root-owned migration-guide/changelog handoff: **Inventory Save/Load now use a private wire DTO to
preserve empty slots/simple records alongside stateful items. Custom snapshot persistence should
use `snapshot.ToJson()` and `InventorySaveData.FromJson(json)` instead of direct domain JsonUtility
calls. Public domain defaults, provider keys, JSON field names/version1 and legacy metadata remain
compatible; no schema/asset migration is introduced. Explicitly encoded id0/keyless/count1 objects
remain instances; an external writer's intention to encode null with that same payload is ambiguous
and cannot be repaired by automatically deleting real data.** Shared release files were not edited.

Actual validation: existing XML parsing, relevant source/UTF-8 reads, manual baseline source-diff and
field-mapping/admission review, and SHA256 bookkeeping only. The source diff removes no original
semantic assertion; the raw domain Count0 assumption is replaced by observed Count1 while the new
wire probe preserves the zero control. The prior lifecycle source remains unchanged at SHA256
`b9dfb2fa35aa40636d9d2e2b766c2dddf2881cec18a57973373f4e62fb7641a2`.
No Unity/compiler/test/product checker, process control, installer/service, child, commit or push ran.
All corrected E19–E24 cases, the nine added variants, native private-wire serialization on both
engines and full integrated discovery/cleanup remain **UNEXECUTED** pending all writers/new global
freeze. No cross-owner source blocker remains; Root owns release-note integration and native proof.

## NI51-01 — source correction completed; native acceptance UNEXECUTED

Action: `NI51-01-INVENTORY-POST-OWNER-NATIVE-WINDOW`. The previous exit127 attempt left the
source and 190-line receipt unchanged; this continuation applied the actual bounded correction.
Read the current implementation contract/request and the Inventory finding in
`NativeImplementationReview-51.md` (SHA256
`b6ae5438baa8d198556e9fd300ac1167f55b37056956147c32ec354491ebcb6f`).

Only `Assets/Neoxider/Tests/Edit/Tools/Inventory/Audit20261023NeoToolsInventoryViewTests.cs`
and this receipt were edited. The relocation reservation map was checked before every edit;
neither file was reserved. Source SHA256 before:
`224c74ad59ad73b28616968d6d946ea53c8ffdcffe57c302807fba1e09f22865`; after:
`b9dfb2fa35aa40636d9d2e2b766c2dddf2881cec18a57973373f4e62fb7641a2`.
The reviewed receipt baseline SHA256 was
`96de4c249da1b99971dccfe7b867681c113f09538134d2bd359066556d453342`.

- All nine original cases remain: five pure API cases and four lifetime cases, including both
  list/grid parameter values. Every original assertion is retained, including synchronous
  grant/remove/regrant, active state, exact child counts, ids, visual identity, owner/visual-null
  positives, manual slot, unrelated child, anchor/prefab survival and inventory units.
- Lifecycle-dependent fixed null polls now use a private completion wait with a five-second
  Editor-clock deadline, actual `Time.frameCount` advancement and the relevant positive predicate.
  Null yields are explicitly only Editor polls. After observing owner-null, hand/list/grid wait
  for visual-null and a further native frame. The manual-slot case waits for a native frame after
  owner-null, explicitly asserts that frame advanced, then checks the same manual object survives.
- Setup rejects a pre-existing Play session and records its acquired session. Teardown attempts
  destruction of every tracked owned object, observes all tracked references become Unity-null,
  then requires a further native frame before declaring the drain complete. Destruction faults,
  drain timeout or premature Play exit remain failures; an active acquired session receives a
  top-level `ExitPlayMode` attempt before the first captured cleanup fault is rethrown.

Actual validation: UTF-8 source reads, manual review of the baseline source delta and SHA256
artifact bookkeeping. The removed-line diff contains only the former cleanup body/fixed polls;
no original assertion was removed. Existing JSON/commit/state-key corrections, relocated helpers,
aliases, metadata, assembly references, public docs and runtime APIs were retained. No shared
tracker changed, and no cross-owner source dependency remains for NI51-01.

Compilation, discovery, all four native lifetime cases, deadline behavior, post-owner destruction
ordering and cleanup/owned-exit behavior remain **UNEXECUTED** on this corrected source until all
writers finish and root authorizes a new global freeze. No Unity/compiler/test/checker, process
control, installer/service, child actor, commit or push was run. This closes the assigned source
work only; native acceptance and the earlier Source3 verification debt remain with central verification.

## Source3 native failures and released-caller closure — 2026-10-25

Actions: `INVENTORY-NATIVE-COMMIT-VIEW` and `INVENTORY-POST-RELOCATION-SAVE-CALLER`.
The earlier reservation deferral is closed at source level: root explicitly released both Inventory
caller files after primary's helper relocation. The reservation map was consulted before edits;
its old caller entries remain unchanged. All moved helpers, GUIDs/type mapping, shared references,
assembly definitions, and aliases supplied by primary are retained. No cross-owner source blocker remains.

Read the actual assigned cases in
`Local/Audits/assembly-2026-10-23/unity/final/s3/u63/Receipts/22-full-edit-graphics.xml`
(root hash `7c6d24e1...`), `21-full-play-graphics.xml` (`2b152164...`), and the Inventory groups
E81–E86 in `full-edit-failure-action-index.json`. E81's six failures are at the deserialized empty-slot
assertion, after the exact exception and both `OnSaved == 1` assertions had succeeded. E82 has two
invalid-instance failures; E83 has three backend/count restoration failures; E84 has two saved simple-id
failures. E85/E86 are the three visual ownership failures. The two original Inventory Shop receipt
cases are reported Passed in that old Play XML; this is historical source3 evidence, not a pass on
the current corrections.

Current source corrections and preserved acceptance:

- `Core/InventorySaveData.cs`: the earlier exact-zero callback is retained, but P5 demonstrated that
  it cannot resolve Count1 domain materialization. The private wire conversion and canonical JSON
  methods described above now establish optional-instance semantics on both write and read.
  All existing JSON keys/version1, legacy entries/instances, merge metadata and admission limits
  remain; no count1 deletion, capacity-default change or sidecar rewrite was introduced.
- `Audit20261023NeoToolsInventoryCommitTests.cs`: retained all six fault cases and every old JSON,
  count, exact-exception, and both-save oracle. Added positive source/empty-target controls, exact
  fault-subscriber invocation, actual writes to both provider keys, and committed physical state
  before inspecting JSON. JSON assertions also reject a simple row becoming an instance. Updated
  only the moved restore-failure helper consumer to primary's existing runtime type alias.
- `Audit20261023NeoToolsInventoryRuntimeTests.cs`: retained E82–E84 cases and their exact content,
  payload/count/identity, rejection and capacity expectations; added successful-admission and decoded
  DTO preconditions. Seven additional cases inspect raw inline fields, empty/simple/instance rows,
  all existing metadata, item-id-zero instances without ids, and invalid/overcapacity instances with
  misleading positive wrappers in both aggregated and slot modes. The callback-free domain probe
  now reflects the observed Count1; the independent wire probe asserts zero/null optional values.
  Neither observation permits broad deletion of positive instances. The P5 variants are listed above.
- `InventorySystemTests.cs`: retained primary's moved helper alias and save isolation scope. Proves
  attachment from `Neo.Tests.Fixtures`, the actual default `FullName` key, a short-name non-match,
  and positive capture/save/load/restore of the exact `coins` payload. Constructs inventory owners
  inactive with explicit serialized configuration and initializes through public queries, removing
  private backend resets and clearing of the ambient inventory singleton. Only owned objects/provider
  scope are cleaned up; no foreign singleton is reset.
- `Audit20261023NeoToolsInventoryViewTests.cs`: the original nine view cases remain: five pure API
  cases in the original fixture and four native ownership cases in the same file's new
  `Audit20261023NeoToolsInventoryViewLifetimeTests`. The hand, list/grid destruction and manually
  assigned-slot cases retain their method names and count/ownership oracles; the latter four full
  test names now use the lifetime fixture. Top-level `UnitySetUp` enters Play Mode, and top-level
  `UnityTearDown` destroys tracked objects and exits. Public grants/removals prove real subscriptions
  before cleanup. Authored waits now require native frame advancement and destruction completion,
  preserving unrelated children, manual slots, prefabs, anchors and inventory units. No executed
  native observation is claimed for this correction.
  This fixture never replaces a provider, scene, or singleton. No private lifecycle/manual Update,
  ExecuteAlways, ignore, expected-error workaround or tolerance relaxation was added.

The four regression files above are under `Assets/Neoxider/Tests/Edit/Tools/Inventory/`; the runtime
file is under `Assets/Neoxider/Scripts/Tools/Inventory/`. Updated owned public pages
`InventoryComponent.md` and `InventoryItemState.md` explain the bounded DTO repair and the existing
exact state-key contract. The runtime state-key policy itself was not changed. No metadata files changed.

Actual validation in this resumed source phase: XML/action-index reads, current relocated helper and
caller/source reads, installed Unity Test Framework attribute/source reads, and manual source/diff
review. Generic FromJson documentation did not establish the actual inline Count behavior: source3
did not log it, while P5's callback-free probe observed Count1 on both engines. Revised wire-probe
assertions remain native acceptance requirements. No compiler, Unity, tests, product checker,
installer, service, child actor, commit or push was launched in the source phases. The revised wire
conversion/raw DTO shape, re-run of the four native lifetime cases on the corrected combined snapshot,
all retained E81–E84 content oracles, isolated cleanup and both-version discovery remain **UNEXECUTED**
until the next complete global freeze. Source completion does not claim native failure closure.

## Assigned actions

| Action | Source result and observable regression source |
|---|---|
| `INVENTORY-PREVIEW-OWNERSHIP` | Removed preview assignment from `InventoryItemData.OnValidate`; authored `_icon` is preserved. The existing view-time fallback remains. The new authoring test invokes validation, checks that an empty icon stays empty, and checks borrowed/authored sprite survival across cache clearing. Generated native previews still require central Unity evidence. |
| `INVENTORY-COMMITTED-SAVE-FINALIZATION` | Mutation notifications are enclosed by eligible Auto Save finalization. Paired transfers capture both committed states before notifications and attempt both finalizers despite event/`OnSaved` faults. The first exception is rethrown with its stack. Tests inspect both saved JSON records and committed contents, including two faults. |
| `INVENTORY-DROP-ORIGIN` | Physical, packed, and id-based extraction retain the originating physical index. Failed drops try that slot first, preserving payload and instance identity; compatible partial instances merge back. Conflicting contents are not overwritten. Tests cover all principal routes, state-restore failure, partial instance extraction, and conflict/partial-capacity recovery. |
| `INVENTORY-WORLD-COMMITTED-RECEIPTS` / A41 | Internal commit receipts capture accepted/extracted quantities before observers. Pickup remainder/latch and drop return ownership settle before those observers run; no quantity is inferred from a later live balance. Tests cover simple/instance pickup faults, reentrant consumption, full/partial pickup, despawn faults, extraction and return faults, exact ids/payloads, and non-replay of committed returns. |
| `SHOP-OBSERVER-REFUND-GRANT` | Consumed the Shop owner's already-present post-commit fault API/policy; no Shop or bridge source was edited. Added two actual Shop + Money + Inventory + bridge Play Mode cases throwing from `OnGranted`: payment/owned profile/grants remain committed, and bundles continue later child delivery. No reservation/rollback framework was introduced. |
| `SHOP-BRIDGE-NAVIGATION` | Added the Inventory entry-point backlink to canonical `Shop/Bridges.md`. The old Inventory topic was already a compatibility pointer on resume and was retained. Shop README's canonical link and the bridge lifecycle guide were already present from their owner; their files were not rewritten. |

## Implementation files and contracts

Under `Assets/Neoxider/Scripts/Tools/Inventory/`:

- `Data/InventoryItemData.cs`: authoring validation only normalizes the stack limit; it no longer
  serializes a transient preview sprite.
- `Runtime/InventoryComponent.Commit.cs` **new**, with `.meta`: internal receipt, callback-free add/take,
  physical-origin resolution, and exact return accounting using existing storage/stack rules and clones.
  Receipt publication is idempotent and retains the first notification/finalization fault.
- `Runtime/InventoryComponent.Operations.cs`: public adds use receipts; common finalization attempts
  eligible save in `finally`, retaining the first exception.
- `Runtime/InventoryComponent.Grid.cs`: paired finalization attempts both sides; physical extraction
  assigns its `out record` before notification.
- `Runtime/InventoryComponent.Queries.cs`: packed extraction likewise assigns its owned record before
  notification.
- `Runtime/PickableItem.cs`: accepted quantity updates remaining amount/collect latch before callbacks;
  positive-grant notification and complete-pickup cleanup are attempted despite earlier observer faults.
  New additive `ConfigureInstance(...)` and cloned `StoredInstance` preserve dropped identity/version and
  unmatched opaque payloads while refreshing matching world-state entries. The template is a hidden
  serialized field; world items are not added to the inventory save format.
- `Runtime/InventoryDropper.cs`: all extraction routes use the receipt's origin; failed return attempts
  account for actual acceptance before notifying. A hidden serialized pending list retains only the
  unaccepted remainder. Additive `PendingReturnCount`, `GetPendingReturnRecords()`, and
  `RetryPendingReturns()` expose bounded recovery. Recursive operations and new extraction while a
  return is pending are blocked. A successful world spawn is never re-added after a success-listener fault.

This reuses the existing storage backends, `InventoryStackRules`, transfer validation, state utility,
clone contracts, `NeoDiagnostics`, `SafeDestroy`, and Extensions preview API. Receipt types remain
internal; this is not a general public transaction/reservation layer. Existing public signatures and
serialized field names are retained, with additive APIs/fields only. Inventory JSON stays version 1.

The 11.0 behavior changes are explicit: complete pickup `Amount` is zero before notifications;
already accepted units cannot be retried after a fault. Explicit repeatable grant sources replenish
only after a fault-free complete request; a faulted source requires explicit reconfiguration. Public
take callers catching an observer exception must retain/account for the already assigned `out record`.
Auto Save attempts do not promise atomic writes across keys or durable disk flushes.

Pending return records belong to the dropper, not its snapshot consumer. Keep the dropper and original
inventory alive and drain the pending quantity before destroying/unloading them. Inventory JSON does
not persist this sidecar. Retry can itself throw after committing a return; inspect the updated
pending quantity rather than replaying an old copied record.

## Regression sources and public docs

New, **unexecuted** regression files, each with its own `.meta`:

- `Assets/Neoxider/Tests/Edit/Tools/Inventory/Audit20261023NeoToolsInventoryCommitTests.cs`:
  39 Edit Mode cases covering the contracts above. Reuses the actor's existing isolated fixture and
  save provider; restores diagnostic flags, destroys created objects, and restores the previous provider.
- `Assets/Neoxider/Tests/Play/Audit20261023NeoToolsInventoryShopReceiptTests.cs`:
  two real bridge `OnGranted` fault cases, item and bundle. Uses the existing Play Mode memory provider,
  explicit dependencies and real wallet/profile/inventory assertions; restores globals and destroys the
  root before its temporary assets. Existing Play Mode assembly references suffice.

Existing audit regression sources remain present. No test attribute requiring unavailable NUnit
support was introduced, and no shared test or assembly definition was edited. These fixtures require
the coordinator's isolated exclusive execution because SaveProvider and diagnostics are global.

Updated owned public pages under `Assets/Neoxider/Docs/Tools/Inventory/`:
`README.md`, `InventoryComponent.md`, `InternalTypes.md`, `InventoryDropper.md`, `PickableItem.md`,
`InventoryItemView.md`, and `Data/InventoryItemData.md`. They document runtime APIs, Inspector usage,
11.0 callback/recovery changes, preview ownership, and relevant Russian guidance. The existing legacy
Shop topic remains a pointer to the canonical cross-assembly guide.

## NI9 — P3 guide clarification

Corrected only `Assets/Neoxider/Docs/Tools/Inventory/PickableItem.md:28` and this receipt:
`ConfigureInstance` permits null `itemData`, requires a non-null `instance`, and rejects non-null
`itemData` with an item id different from `instance.ItemId`. Compared the wording with the current
runtime guard branches and reviewed the scoped documentation diff. Runtime, regression sources,
metadata, and all earlier changes are retained. Source-final status and the 41 authored new cases are
unchanged; execution remains blocked by the global writer barrier.

## Actual inspection and remaining verification

Performed manual reads of the changed source/diffs, new receipt implementation, authored regressions,
owned docs, relevant backend capacity/slot operations, and the current Shop/bridge APIs and real bridge
regressions. Scoped `git diff` and `git status --short` were used for source/artifact inspection. During
that review, corrected the conflict fixture to occupy every physical slot rather than place four units
in a single unlimited stack. New C# files have companion metadata. No product checker or validation
script was run in this implementation phase; historical audit checks are not new implementation evidence.

**Not run:** Unity, compilation, Edit Mode/Play Mode tests, builds, product checkers, installs, or services.
No commits, pushes, child actors, or changes to shared trackers. No execution pass is claimed.

Central verification after all writers freeze must compile the combined graph in the requested Unity
6.3/6.6 configurations and applicable Mirror-on/off lanes, discover/run the existing audit regressions
plus the new cases above, and check global/static cleanup under the prescribed isolated runner.
Actual provider-write failure behavior, generated/native preview recreation and cache lifecycle,
domain reload behavior, and complete combined-suite results still need execution evidence. The
deterministic sprite regression does not prove asynchronous generated-preview readiness.

Cross-owner dependencies consumed read-only: `Neo.Shop` post-commit notification/fault policy,
`Neo.Shop.Bridges` best-effort grant/lifecycle behavior, `Neo.Extensions` preview ownership/cleanup,
`Neo.Save` provider semantics, and the existing test assemblies/provider helper. Their needed APIs were
present when inspected; there is no remaining source-edit blocker for this actor. Integration and
execution remain central responsibilities. Broad reversible-grant/reservation work and other
substantial future designs from the completed plan remain proposals, not implemented guarantees.
