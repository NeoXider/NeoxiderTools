# Native implementation review 9 — Neo.Rpg and Neo.Tools.Inventory

Status: **SOURCE REVIEW COMPLETE; three bounded RPG residuals NI9-01/02/03 remain OPEN for the SAME original owner.** Inventory's assigned commit/recovery/preview runtime changes are supported by source review; two small guide wordings are listed separately. Compilation, discovery, tests, native lifecycle, performance and cross-consumer acceptance are UNEXECUTED. There is no blanket runtime or integration approval.

Reviewer: SAME `/root/audit_abilities`, `gpt-6.1-sol` / `max`, target `D:/Git/NeoxiderTools`, `demo` / `11.0.0`. Only this report is written. No product edits, Unity/MCP, compiler/test/checker, service/install, commit/push or child agent is used. The global source execution gate remains closed.

## Coverage ledger and ownership

- Completely read `Neo.Rpg.implementation.md`, `Neo.Rpg.implementation-request.md`, `Neo.Tools.Inventory.implementation.md`, and `Neo.Tools.Inventory.implementation-request.md`.
- Completely read the implementation phase's fourteen RPG and seven Inventory runtime files (including both new internal partial/helper files), their changed hunks, all four new RPG and two new Inventory fixtures, twelve RPG/navigation and seven Inventory public guides. Long/truncated output intervals were separately reread; smaller original-context hunks were retained as context, not claimed as a fresh full audit of every historical module file.
- Context includes full Inventory persistence/item view/probe fixture and RPG profile serializer/status definition; relevant Inventory slot backend/extraction/restore-failure and RPG attack receiver entry points were read. Partial contextual sources are identified in the identity ledger. The hash program only records raw source identity and metadata; no product checker/test was executed.
- RPG owner: SAME original `neo-audit-1023-b05-03`. Inventory owner: SAME original `neo-audit-1023-b07-05`.
- Nested RPG Combat/ConditionBridge/Network and Demo, plus Shop/ShopBridge, retain their separate owners. Their source visible in aggregate diffs is contextual only and not accepted by this provider review. Existing original audits/plans remain phase evidence; this report does not rewrite them.

Per-action dispositions, concrete findings, acceptance limits and read/hash identities follow. All three runtime residuals were source-traced, sent to root and accepted for bounded SAME-RPG-owner correction; no source fix is performed by this reviewer.

## NI9-01 — P2: UpgradeStat continues into replacement dictionaries after a points callback

**Confirmed source-traced residual; OPEN with original RPG owner `neo-audit-1023-b05-03`.** Exact current path: `Assets/Neoxider/Scripts/Rpg/Components/RpgCharacter.cs:1297–1308`, `UpgradeStat`.

The method selects the old rule, debits points, then sets `UpgradePointsState.Value` at :1299. A listener can synchronously ApplyTemplate/RebuildRuntime, which advances `_runtimeGeneration`, rebuilds dictionaries and clears investments. On return the old method has no generation fence: :1301–1305 writes an investment and UpgradeCount into whichever dictionaries now exist, then refreshes the replacement at :1308. The new resource/stat/derived guards cannot stop this continuation because these new writes occur before starting a fresh derived pass against the new generation.

Concrete public-API repro: start a character with stat `S`, one available upgrade point and an upgrade rule adding two per investment. Prepare a replacement template with `S` base 7 (and the same valid rule). Add a one-shot `UpgradePointsState` listener that applies the replacement when the debit publishes zero. Call UpgradeStat("S"). Expected: the replacement's `S` remains 7 with investment 0; an operation begun against the superseded runtime does not grant an upgrade into it. Actual source path: the outer continuation stores investment 1 and resolves the new `S` to 9. A replacement omitting S still receives the obsolete investment row in its captured profile. No exception or dictionary iteration is needed to reproduce the incorrect ownership.

Smallest correction: capture the runtime generation before the reactive debit and stop the old continuation immediately after that callback if it changed, before touching investments or stat runtimes. Preserve public signatures and normal successful upgrade behavior; do not roll old state back into a replacement or introduce a transaction framework. Return semantics should follow the owner's existing superseded-operation policy explicitly.

Required source controls: one-shot callback ApplyTemplate and RebuildRuntime scenarios with replacement stat/investment/profile assertions, plus an ordinary successful upgrade asserting cost, investment, stat change and notification behavior. The complete new implementation character fixture was read: it covers ordinary upgrade under deferred growth and stat/resource callback replacement, but not this points-debit callback. Root accepted this as a bounded same-owner correction after the full context/fixture review. Repro and potential mutant distinction are source traces only; no execution occurred.

## NI9-02 — P2: positive RPG resource cost can succeed with zero stored debit

**Confirmed source-traced residual; OPEN with original RPG owner `neo-audit-1023-b05-03`.** Exact current path: `Assets/Neoxider/Scripts/Rpg/Runtime/RpgCharacterResourceService.cs:114–146`, `Spend`, especially discarded guarded mutation delta at :136–143; facade `RpgCharacter.TrySpendResource` accepts its true result.

Public plain-service repro: Build a resource R with finite Current 100000000 / Max 1000000000, restoreOnAwake=false and ordinary nonnegative-resource settings. Spend("R",1f) subtracts in float precision, so the stored candidate is still 100000000. SetCurrentGuarded reports a zero own change, but Spend discards it, notifies ResourceChanged, applies pause-after-spend and returns true. Repeating that positive cost never pays it. Expected: a positive request whose own stored debit is zero must fail before callbacks or timer/notification/persistence work. This is a precision/admission defect, not callback-sensitive receipt attribution.

Root accepted the narrow policy consistent with Core NI2/Money NI7: compute the clamped stored candidate/admission before callbacks, reject a positive zero-effective debit without mutation, and keep accepted-operation receipts independent of later callback balances. Preserve ordinary/zero-cost/unknown/nonfinite failure precedence. Do not demand exact equality of requested cost and representable delta: at 100000000 a cost 5 can produce an eight-unit stored decrement and remains an accepted ordinary rounded positive operation under this policy. No decimal backend, general reservation or transaction service is required.

Required controls: cost 1 at 100000000 rejects with unchanged Current/reactive values, events, regen pause and relevant facade publication; cost 5 applies the positive stored decrement; ordinary fractional spend succeeds; zero-cost/unknown/nonfinite cases preserve the existing API's precedence. The full new runtime and character fixtures were read and do not yet cover this coarse-ULP payment path. All reproductions and expected controls remain UNEXECUTED.

## NI9-03 — P2: captured bindings accept a destroyed source character

**Confirmed source-traced C31 lifecycle residual; OPEN with original RPG owner.** Exact current locations: `Assets/Neoxider/Scripts/Rpg/Components/RpgResourceBinding.cs:110–118`, session/state predicates and captured callbacks; `RpgStatBinding.cs:84–90`, equivalent session predicate/value callback.

Both predicates require the cached field/local character to compare equal, but neither first requires that source character to remain live under Unity's null semantics. A destroyed field/local referencing the same Unity object still compare equal. Their user-defined managed getters can still return the old managed reactive objects, so ReferenceEquals passes as well. Source destruction does not disable a separate view or advance its subscription version. Public CurrentValue/Value already return zero for a Unity-null character, while the captured observer can still publish the obsolete value.

Concrete callback repro: register the destroying HP/stat listener first, then explicitly bind a separate active view to that character, verifying that both listeners are registered. The first listener calls DestroyImmediate(character.gameObject) when Increase/SetStatBase is triggered. The reactive observer batch already includes the binding tail; it executes after source destruction and its current predicates permit stale output. Expected: no old automatic UI callback after the source has become Unity-null. This uses the same in-flight snapshot mechanism as the existing disable/rebind regressions, with source lifetime as the invalidation boundary.

Smallest correction: include a live/non-null cached character condition before calling its cached-source getters in both predicates. Keep the exact reactive object, id, version and enabled checks and exact-source detach. Do not replace them with a looser source lookup or introduce a session framework.

Required controls: actual earlier-listener source destruction for resource and stat with a verified live/subscribed positive precondition (for example, public listener counts/ordinary output), no captured-tail output, plus ordinary live/rebind controls. The complete current four-method binding fixture covers disable, rebind, initial Current continuation and pre-RuntimeRebuilt stale state, but not destruction of the source character. Root accepted this bounded correction. Native callback execution remains UNEXECUTED; these are explicit expected source transcripts.

## Small guide corrections (P3, separate from runtime defects)

- **Inventory owner:** `Assets/Neoxider/Docs/Tools/Inventory/PickableItem.md:28` says ConfigureInstance throws for “null or mismatched item data”. Actual `PickableItem.ConfigureInstance` allows null itemData, requires a non-null instance, and rejects a non-null itemData with a different id. The new fixtures deliberately use null itemData. Clarify that exact nullability; no runtime or additional test change is needed for the guide.
- **RPG owner:** `Assets/Neoxider/Docs/Rpg/InternalTypes.md:62–63` says nonfinite inputs produce no callbacks. Narrow that to tick-damage/regen delivery: status Tick still processes independent UTC expiry. `Status_InvalidDeltaStillExpiresWithoutCorruptingFinitePhase` explicitly expects that expiry callback, and the StatusEffectDefinition guide already describes it correctly. Preserve the expiry contract and fix only the overbroad wording.

## Neo.Rpg — reviewed behavior supported outside the three residuals

### Bounded ticks, fractional phase and callback work

The new internal RpgTickUtility is shared by the existing resource and status clocks. It validates finite float inputs/intervals, computes in double, reduces prior phase and incoming delta separately with modulo before combining them, and caps logical tick count before integer conversion at1024. A huge finite delta cannot loop on a subtraction that no longer changes a float accumulator. The just-below-boundary float conversion is rounded downward instead of inventing a due tick. Legacy finite negative status phase is retained (with negative representable saturation); resource regen still ignores nonpositive delta.

Both callers store the reduced phase and consume each local pending delivery before callbacks. Nested same-service ticks return immediately and finally releases the guard after observer faults. Service dictionary generation and shelf mutation version stop stale passes after callbacks before the next write/iteration. Finite amount multiplication is checked in double before conversion. Excess whole backlog is intentionally dropped, not queued; fractional recovery remains. Existing enabled/mode/alive/full/pause gates remain, including skipping the complete resource pause-crossing frame. Shelf UTC expiry remains independent of delta; buff expiry precedes statuses, and status damage precedes its due expiry. This bounds each entry/call, not aggregate work for all entries/actors.

The runtime fixture uses actual services and definitions: 16777216, float.MaxValue and2048.25, separate prior0.125, exact next fractional boundary, nested tick suppression, rebuild to C=7, invalid interval/amount recovery, gate cases, negative delayed status phase, clear/remove and throwing-tick recovery. These are meaningful unexecuted source controls. Normal amounts remain float-based; the positive zero-debit admission in NI9-02 is separately required and is not established by tick tests.

### Mutation fences and initialization publication

Build/Clear/InvalidateOperations advance resource generation. Guarded Current/Max mutations capture their own stored change before reactive callbacks, then validate generation/identity before Percent or resource notifications. Max/regen resolvers are followed immediately by checks. RestoreAll and TickRegen return before another dictionary MoveNext after replacement. Facade derived passes carry both runtime and derived revisions; RefreshStat checks after reactive state, stat event and adapter notification. HP damage/heal/death/revive and buff-expiry continuations check replacement before their following work. These paths avoid stale enumerators/updates; NI9-01 is the omitted points-debit continuation.

Initialization/rebuild starts publication suppression before clearing/rebuilding, refreshes coherent derived state before marking ready, fires RuntimeRebuilt before optional auto-load, and publishes the final pending transport notification after it. A synchronous CaptureProfile from that final notification sees initialized state. Reentrant rebuild requests during initialization get at most two immediate attempts, with later retry in Update. Profile application also defers intermediate publication and invalidates old resource work. This is a focused readiness/publication guard, not arbitrary observer rollback.

The transport-neutral probe asserts exactly rebuilt → loaded → snapshot and HP130/160, XP17 and both stats in one final payload, plus no recursive initial capture. It is useful core evidence but does not establish the real MIRROR active-server adapter's complete final snapshot. Rpg.Network remains pending; so do its command23 dispatcher/authority integration and snapshot/no-current-grant-replay fixture.

### Applied growth, v2 maxima and one-shot buff quantities

The applied-growth level is distinct from live Level. Automatic/Hybrid enabled growth follows upward and downward changes; Manual/None/deferred refresh keeps its prior anchor until public ApplyLevelGrowth. Upgrade points remain a separate upward manual/hybrid policy. The append-only command enum value23 and NoCode ordinal34 preserve previous identities and add no mandatory receiver member. V2 cannot persist a separate explicit deferred-growth anchor: automatic restore uses the saved level, other modes start at1 and require an explicit apply under the caller's policy. The limitation is documented rather than disguised as a v3 schema.

Profile restore constructs a call-local last-known-row-wins resource lookup. Saved effective Max is resolved before Current and percent-based regen; unknown ids create no resource, missing rows use the usual resolver, definition caps still apply. BaseMax is not overwritten, and the cached ongoing regen resolver does not capture that restore lookup. Later normal refresh deliberately returns to BaseMax plus modifiers. The fixture asserts130/160, percent0.8125, regen16, no intermediate zero/death and no compounding Max across buff round trips. Serialize clones/sanitizes rows and retains buff stacks with schema2.

Both SO and inline buff entry paths capture finite signed Current grants and ids before callback-bearing derived work. Only a new entry or positive effective stack increment grants; nonstack/cap refresh, removal/expiry and profile/network effect restoration do not replay it. Derived limits resolve first, raw Increase/Decrease settles next, then OnBuffApplied observes final state. Application/profile guards, entry/stack identity and runtime generation stop stale continuation; finally releases the guard after a fault without rolling back or replaying committed grants. The tests cover130/150, additional stack, cap/nonstack, signed/unknown/NaN, reentry, replacement and throwing applied observer. Legacy HP regen ignores targetId, uses canonical HP/clamped stacks and composes flats before percent with all existing gates. AttackSpeedPercent is accurately disclosed as unsupported enum-only data, not mapped to invented attack timing.

### Projectile completeness and bindings

The serialized raw capacity becomes an initial hint. Saturated queries grow/requery to4096 and then use a complete allocating cast; raw ignored/source/duplicate hurtboxes cannot silently cap eligible discovery. Per-dimension distance/StableId sorting is explicit; 3D still precedes2D. Receiver dedup and ProjectileMaxHits remain delivery policy. Initialize/disable generation and expiry flags stop an old hit/movement/destruction continuation after callbacks reinitialize or disable the projectile. No per-site obsolete Unity-id branch was introduced.

Three Play cases use public Initialize plus actual Update/physics flight with raw capacity 1, invalid/source hurtboxes, duplicate receiver colliders, nearer/far targets and mixed-dimensional precedence. They assert real HP and delivered order/count; they do not invoke private hit helpers. Native hit membership/tie order, >4096 fallback, transform/time-scale assumptions and reuse callbacks remain execution debt, not a measured performance/completeness pass.

Bindings retain exact previous reactive states/delegates and add id/source/version/enabled guards plus checks after initial Current/Max callbacks. The current source supports the assigned disable/rebind/rebuild behavior; NI9-03 adds the missing destroyed-source liveness boundary. Existing tests have strong expected rebind/initial-output transcripts; central native/Editor lifecycle execution remains necessary, with explicit live/subscribed preconditions for the new destruction controls.

The four new RPG fixtures contain **35 methods /62 authored cases** by manual source inventory, not runner discovery. Twelve owned/navigation guides were fully read. Runtime APIs, append-only enums, v2/deferred-anchor limits, tick backlog policy, one-shot event order and nested module links are described without requiring a new framework. Apart from the wording note and three residuals, no additional actionable defect was found in this assigned implementation scope.

## Neo.Tools.Inventory — assigned runtime correction supported; cross-consumers pending

### Commit receipts and world/storage conservation

The new internal Commit partial separates callback-free storage mutation from publication. A receipt captures actual accepted/extracted quantity, before/after count snapshots, owned record and physical origin before user observers. Publish is idempotent, retains the first fault, still attempts the applicable rejection notification and delegates eligible save finalization. Public take APIs assign out record before publication, so exception-catching callers retain ownership rather than treating the extraction as absent.

Simple and instance pickup paths subtract each accepted quantity from Amount and settle the latch before publishing. Live inventory balance after reentrant consumption is not used to infer acceptance. Partial/error cases retain only the actual world remainder. OnCollected is attempted after any positive acceptance even with an earlier observer fault; complete collider/despawn cleanup is attempted and destruction/deactivation is finalized despite later event faults. The first exception is rethrown after ownership settles. Explicit reusable sources refill only after a fault-free complete request; faulted grants cannot auto-replay. ConfigureInstance/StoredInstance clone the retained identity/version/opaque payload, refreshing only keys represented by actual world state components. The null itemData contract needs the guide clarification above, not a runtime redesign.

All drop routes select one record and capture its physical origin before extraction observers. Missing prefab preflight precedes extraction. A faulted extraction skips spawn and owns the record for return; spawn/configuration failure releases its partial clone, then tries original slot first. Compatible partial instance state merges into its origin; conflicting contents are never overwritten. Ordinary fallback capacity accepts what it can, and the dropper owns only the remaining sidecar quantity before return notifications. New drops and recursive same-dropper operations are blocked while settlement/pending recovery applies. A committed successful world spawn is not re-added when its success observer throws.

Pending return inspection returns copies, not ownership. Retry updates/removes accepted pending quantity before publishing and cannot replay a return after an observer fault. The sidecar deliberately is not inventory JSON persistence: keep its dropper and original inventory alive until drained. No no-loss promise across destroying that explicit owner or arbitrary provider/disk rollback is inferred. The documented bounded recovery path addresses the assigned physical-origin/conflict defect without a reservation framework.

### Finalization, preview ownership and authored controls

FinalizeMutation determines the committed change before notifications and attempts eligible Auto Save in finally, retaining the first exception/stack. Paired transfers validate and commit both slot states, capture both committed snapshots, then attempt both finalizers even if the first observer/OnSaved/provider path throws. Later listeners inside an interrupted UnityEvent are not replayed. Provider JSON reflects the current settled state; two keys/durable disk flush are not atomic guarantees. Actual failing-provider behavior remains central execution debt; source tests assert both saved JSON records and both notification-fault attempts.

ItemData.OnValidate no longer writes a cache-owned generated preview into serialized Icon. Existing authored Icon is untouched, and ItemView's bind-time fallback borrows the Extensions preview. The deterministic source test asserts empty Icon remains empty and borrowed/authored sprite survival across clear; it does not establish asynchronous generated-preview readiness or all native editor cache lifecycles. Public guides tell callers to rebind after cache clear and disclose no automatic asset migration.

The new Edit fixture has **16 methods /39 authored cases**: paired endpoint faults/two first-fault preservation, simple/instance exact pickup receipts with added/changed/saved faults, full/partial/despawn/reentrant consumption, no repeat grant, physical/packed/id/first/last origin recovery, out-record ownership, restore failure/clone cleanup, full origin collision, partial capacity, partial instance merge, recollection identity and preview ownership. Assertions check real count, combined ownership, ids/payloads, physical JSON, save records and callbacks. The reused fixture isolates provider storage and restores diagnostics/globals/owned objects; tests require the isolated exclusive runner.

Two new Play methods use real Shop/Money/Inventory/bridge objects and assert committed wallet/profile/inventory and later bundle delivery after OnGranted throws. They are useful consumer regression sources, **not approval of pending Shop/ShopBridge/Demo integrations**. Their host/helper/API/symbol setup and combined frozen-source execution still belong to the respective owners/root. No unavailable NUnit attribute or assembly change is introduced in the reviewed new fixtures. All 41 new Inventory cases remain UNEXECUTED.

Seven owned public guides and the existing legacy Shop pointer were fully read. They explain count-versus-layout notifications, committed exceptions, physical origin and pending recovery lifetime, preview ownership, C#/Inspector paths and the canonical Shop bridge backlink. Apart from the precise nullability wording, no current assigned Inventory runtime defect was found by this read-only review.

## Composition, costs, verification gate and handoff

RPG's per-entry1024 bound is termination safety, not a frame budget. Guard delegates/closures are created on resource mutations/derived passes, and per-tick Increase can repeat that work. Normal derived refresh outside publication scopes still emits per-stat/resource notifications; a real network adapter can serialize a complete profile for each. Unique projectile segments remain unique raw physics contexts; no incorrect cross-projectile/LOS sharing was added. These are source-visible aggregate costs without invented FPS/byte claims, and do not require a new architecture for the three fixes.

Inventory still deep-snapshots records/counts around mutations; repeated stateful pickup units and view/hand refreshes can repeat container/payload traversal. Receipts add bounded ownership records and the pending sidecar, not a frame polling registry. Batched/no-copy improvements remain proposals. Existing serialized names/signatures and inventory schema1 are preserved with additive APIs/fields; RPG stays schema2 with explicit documented growth/current-grant/tick/event-order changes.

Root should resume SAME RPG owner for NI9-01/02/03 and their ordinary/reentry/precision/liveness controls, then independently reread changed identities. The two guide clarifications can be handed to their existing owners. Rpg.Network (command23, real active-server final capture/no replay), Demo and ShopBridge remain pending consumer scopes and are not closed here.

Only root can lift the global source freeze gate after all writers finish. Installed Unity6.3/6.6, MIRROR-on/off dependency/discovery, nonempty Edit/Play cases, actual RPC/remote composition, native physics/UI/pooling/preview/time/lifecycle, provider failure and static cleanup, and package/docs/UTF-8/meta/GUID checks all remain UNEXECUTED. Earlier pass logs/diagnostics are not acceptance of these identities. No product source was edited or executed by this reviewer.

## Reviewed identities

Raw-byte hash/read checkpoint and exact paths follow. Full implementation sources/tests/guides are distinguished from partial contextual files; no full cross-owner audit or pass is inferred from a contextual hash. A later same-owner correction needs a fresh delta read/hash before its finding is closed.

Checkpoint: `2026-10-23T20:10:48.538230+00:00` UTC (local date 2026-10-24). Eight new source/fixture metadata files were read; global GUID/import validation was not run.

| Absolute path | Read scope | Bytes | SHA256 |
| --- | --- | ---: | --- |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/IMPLEMENTATION-CONTRACT.md` | full | 15329 | `d5bc7a2d934e20f40647c45d53746c3734a1dbbfe95f182600d66977e9bbcbab` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Rpg.implementation-request.md` | full | 18133 | `d2e9755760a9f7f23e562643036a85a06650ce3672f6d73476ee4eaff804bda3` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Rpg.implementation.md` | full | 19585 | `d4b4ed1366aa0360c5385aa11a05b46d61d0428206c275ae65a09aa26cad5991` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Tools.Inventory.implementation-request.md` | full | 13878 | `dcb40dd2ac730591a199fdd7c5cbec2125424a12bc4779cb26a10ffdffe57788` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Tools.Inventory.implementation.md` | full | 9904 | `7eae03160f11c2f887e88c9de540d8ade55939ed21d4779923d722561fd6d27f` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Components/RpgCharacter.cs` | full | 64855 | `d336373795c7e5f7d34dc54108898abfebf3beb03186ccc229d17602fcf3b2b0` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Runtime/RpgCharacterResourceService.cs` | full | 14895 | `fce278c50aa223850464750308bb8eafaef6896e0c4625be1a5df8fb420f753d` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Runtime/RpgResourceRuntime.cs` | full | 5338 | `67444db3d1f94b66c3f1ba07e2bec73627f3f42a7fccccaf441aa8c2205ed25f` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Runtime/RpgEffectShelf.cs` | full | 19290 | `1c81202e74067d647129ffd79c4323cf04f665b97997a6b4f39256e5280bf41c` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Runtime/RpgStatResolver.cs` | full | 12005 | `284764e3626858060c59c332a00e8f46775a8d1f076993b161e6a1ac8b8ec64b` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Runtime/RpgTickUtility.cs` | full | 2877 | `88a47df4f1ce82afc7acf7e1d3b9ff2373a49695049096ad50b63109a59a1dca` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Components/RpgResourceBinding.cs` | full | 6850 | `af3b361f61921bf4f8bc95890c9f7bca1a735f1767b22c7a38bfa8ca93c14331` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Components/RpgStatBinding.cs` | full | 4001 | `344352c04eb09805de8ad42d6e19c0a1f1ac5d6a17dbad5345e34f6715c2c5c4` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Components/RpgProjectile.cs` | full | 9591 | `c36017f55c1986df0a08d86ca9d7a628217a996b061968f11387bd58c179d00a` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Runtime/RpgCharacterNetworkCommand.cs` | full | 1385 | `a3241ff1a7095b693e1ed810ae5f7af88cc45a48981e9877ca092232036fdd00` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Bridge/RpgNoCodeActionType.cs` | full | 977 | `dd7e076d70a0d6f198861e396fcbcb4c3eff534c30759d82117f1b0d0b4f92d6` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Bridge/RpgNoCodeAction.cs` | full | 14035 | `71ec863b109f9b0939b840e34d529b11910c83fb3accb2f3c64a0ff16133fb0e` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Enums/BuffStatType.cs` | full | 2556 | `865d5a65ce3a9a967301def7d02a2cb20eb6daa22c4edb5c256e17ababadd837` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Runtime/RpgCharacterProfileData.cs` | full | 7857 | `7c23f45445939b5163ddd32fa0efc79c4f0adf5e8a219c701acff29b6eb2aa48` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Inventory/Data/InventoryItemData.cs` | full | 2612 | `7c033a75e3a273484be805abdfe49108ead858e67a17342c4c9abc39ac08019e` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Inventory/Runtime/InventoryComponent.Commit.cs` | full | 9180 | `d45ae7ce276d1ff9d873b07f3a6416f147fdabcd7a07022b5476e9a2da8cc1d9` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Inventory/Runtime/InventoryComponent.Operations.cs` | full | 10428 | `b29499e3b1a4861307745173e47e0d7647bb1d9e8b0479f8714067b5728b0817` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Inventory/Runtime/InventoryComponent.Grid.cs` | full | 7954 | `76dc01ccdafad64e17af31b929c4cecbacddde062a09619aa9b1929af8420662` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Inventory/Runtime/InventoryComponent.Queries.cs` | full | 6837 | `7cc05825b67697b0beef2d94600e1a690b2fb9f36088652319c223318377aeeb` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Inventory/Runtime/PickableItem.cs` | full | 15804 | `cbb656da020c796225ff09dbd28d5a7462d23dcbdc0664d3d2ac9576e070af23` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Inventory/Runtime/InventoryDropper.cs` | full | 22784 | `ceb4738d4806899340d72e5cc2e336757d48a47b1afef67276b1ebe7233d5f5c` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Rpg/README.md` | full | 8712 | `3c5ea33a35b09bacc5dc58ceda841ec5cad530a24d1622acca11338b81673508` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Rpg/RpgCharacter.md` | full | 16748 | `9426f5d960ebe5d032c7f82e42bd3b8960ff83e4c47611a8a984de555d57a9d4` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Rpg/RpgProgressionDefinition.md` | full | 2463 | `41ec3307261180f59644fa2c230673f8dd1b7e0ef8c71ffaa677c9bb4069d256` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Rpg/RpgProjectile.md` | full | 1859 | `d8f3ea16a12c6f478d21aa16397917881b7aa9b004e29bc577bedd52e65d7ece` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Rpg/InternalTypes.md` | full | 5725 | `1b214b423fb8eae96be26be79b1f456ff289f5aad9f9d2cba69689a53c1d975d` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Rpg/Data/BuffDefinition.md` | full | 2784 | `10b4c24430f270e487648795d2edd41f311d42abfc59c487662f98b7906ec9d2` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Rpg/Data/StatusEffectDefinition.md` | full | 1792 | `83ff4fbe72bb30ebda645b920b4c59f38e89619ee1c4f959ceb729015083bc33` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Rpg/RpgNoCodeAction.md` | full | 2581 | `70eed4b81109225e78148c9c40025ab5812101f9b2595599d21a600b2885e2ac` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Rpg/RpgResourceBinding.md` | full | 1529 | `c737201ac19d5a2427c7fe9b6809955fae180e9988f5dfdebd51d02026e644f5` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Rpg/RpgStatBinding.md` | full | 1125 | `347d40e354f2372998f6053d22137e148ce354b51d5d3d01918abaabb955b4b8` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Rpg/RpgConditionAdapter.md` | full | 494 | `a0a209b8a5c2d61d4223b841cff7b62e0d103046b35f1f8243495d1bb833a25d` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Tools/Components/AttackSystem/README.md` | full | 1948 | `f3ec12f26ab786f0e8221b96171b70b116fd4003c26e5b3c3e3035f2a850b451` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Tools/Inventory/README.md` | full | 4507 | `81db4ad57bcc54ee5739570b24fed2ffd75b27983c9cbce57af16278ea60f41a` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Tools/Inventory/InventoryComponent.md` | full | 7502 | `dccf1cf54ff0063ba016f5c2b1700886470eca6cfdcb751a072d3bc2a86f1c65` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Tools/Inventory/InternalTypes.md` | full | 4946 | `0613ec714ec79a73145fc907ce6999d402ea3fd7e11d5116ab48e27bd919ca85` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Tools/Inventory/InventoryDropper.md` | full | 5959 | `f7a2d5405a46e2e4062c0164d4dcabb9c574580ebe92252e71e4b3e4ae28ba8a` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Tools/Inventory/PickableItem.md` | full | 6810 | `b0e4a26ed92d3153af7545d6b2e14f892336e7c74d303338eb240560f11254fa` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Tools/Inventory/InventoryItemView.md` | full | 2499 | `a2c591dbc2f29aa55ed73779a0625d9de38ba66b4f82f81e4d34cbefda28b0f6` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Tools/Inventory/Data/InventoryItemData.md` | full | 2488 | `dc5af36240d38598438d44170b761975a0eaf26b8a5a56eec74d7dcd1fc66d7e` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/RPG/Audit20261023NeoRpgImplementationRuntimeTests.cs` | full | 14053 | `a0ac0aa6d804b21153b223b0cde67f7fb31b349a7352a814adaaa3b4ea645b53` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/RPG/Audit20261023NeoRpgImplementationCharacterTests.cs` | full | 20841 | `a9ed41734db52694a0c3803291987cb375f1860051746e6d65cf82b57f3d1fda` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/RPG/Audit20261023NeoRpgImplementationBindingTests.cs` | full | 5611 | `ca82ccc12eb2ad40a2f361f37cb4e6c5f2a59fdffdd3efaaed460f76fb61a2ca` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Play/RPG/Audit20261023NeoRpgImplementationProjectileTests.cs` | full | 7253 | `5cce47c6b706e21c1851e162399aed2660523df312c2e177945d91e4eeb1703f` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Tools/Inventory/Audit20261023NeoToolsInventoryCommitTests.cs` | full | 26187 | `2bde8b3f50e61e787288d7afd744c394e51e1f226500f3b15798399c4d193496` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Play/Audit20261023NeoToolsInventoryShopReceiptTests.cs` | full | 7452 | `3c1ec0b93e4068060c82ca97b2a6375894049dd8daa8cddd212ac3b318c574a6` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Runtime/RpgTickUtility.cs.meta` | full | 251 | `f7a0497e804c7b8f99913ea425f4e5bad72876bf75a5453eb84a5912e39a7e43` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Inventory/Runtime/InventoryComponent.Commit.cs.meta` | full | 240 | `5b4c91e684bf306e0a9c5294f4483b586f8a3257fc4a2d50a03e79419116e415` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/RPG/Audit20261023NeoRpgImplementationRuntimeTests.cs.meta` | full | 251 | `9925fca234439aa1b6de4844a905626fe4bdfeda817bce580900e7bd2a6d7ece` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/RPG/Audit20261023NeoRpgImplementationCharacterTests.cs.meta` | full | 251 | `08752a6d34a631fae8b3151924ef2443d5197bc2486c2ce5bb9212b540ad64ef` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/RPG/Audit20261023NeoRpgImplementationBindingTests.cs.meta` | full | 251 | `ccca31f66acd96d48d4fd54ed943511019be00e119740d7ff588a391346c64df` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Play/RPG/Audit20261023NeoRpgImplementationProjectileTests.cs.meta` | full | 251 | `c94f256c0130372b4e350595ba01ccf4a56aa15db6ce165ede1aefc771a4cc01` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Tools/Inventory/Audit20261023NeoToolsInventoryCommitTests.cs.meta` | full | 240 | `8f6c15b824ce6f21a3dc131f704775ecc345a423650f4b64b3a7f78931ed1e44` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Play/Audit20261023NeoToolsInventoryShopReceiptTests.cs.meta` | full | 240 | `b75a7cf0d1dd1d9375ccd1f1373f8e19336ebbf9aa6f55700d5d01cfb8569336` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Data/StatusEffectDefinition.cs` | full | 2771 | `211dacdddeb082b3b76bf71af6dc2d14d7705497493fcf4fa3447688ccf0ac76` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Runtime/RpgCharacterProfileService.cs` | full | 1151 | `92bf6baf496877d07de91d318c4a1ec7e7816f178e2d80d29a72953b58c62fe3` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Inventory/Runtime/InventoryComponent.Persistence.cs` | full | 9551 | `376f572ce8ffaca47f1a0d297a813e35499c190d19b68ab96bf0444642e3f3f8` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Inventory/UI/InventoryItemView.cs` | full | 2311 | `124b6b1ea97153b1d6e173db6f0e9594ebabe69ece7da334831ec8a1c601e327` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Inventory/Core/InventoryItemInstance.cs` | full | 2303 | `cc3f2f118dfc6c9d36b868a4fb7c859147b9e53d9642065bcbfcde5b5d866154` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Inventory/Runtime/InventoryItemStateUtility.cs` | full | 4192 | `7ec51629d61aec355055859f6f4416fb7ee34614df715b0ddcfac461edb1e94a` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Tools/Inventory/Audit20261023NeoToolsInventoryFixture.cs` | full | 6311 | `5cfa9b9ce5158e734df9f58810b6b87724d8bac36344e272cc657bdd3561183e` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Tools/Inventory/ShopInventoryGrantBridge.md` | full | 523 | `3ba756e39a33de10a32ef40bb2d22cff90ea1fdfb5b8f12b3b56b615a43fb46c` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Components/RpgAttackController.cs` | partial context | 29575 | `b826bf64fbf932aad55dae867370112a7391d0a2dff17d62a38380f8982a4c7e` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Inventory/Core/SlotGridInventory.cs` | partial context | 15385 | `53ba89ce4b2146ae7c64d1cbfff6b2b500f43fb8cdf9dd9639d2f91dac2c324f` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Tools/Inventory/Audit20261023NeoToolsInventoryRuntimeTests.cs` | partial context | 23316 | `0166e17a1cf6570e8bcef3b82bb3d85c9547377acb2d8c88c21c4c03b7a1f4a7` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Play/ShopPurchasePlayModeTests.cs` | partial context | 28402 | `51101d771e579f146075da77c57a8a65572a27b246f16261539eee47f677585e` |
