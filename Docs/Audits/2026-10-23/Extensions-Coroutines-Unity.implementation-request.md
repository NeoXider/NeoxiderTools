# Extensions-Coroutines-Unity: approved implementation request

Original actor: `neo-audit-1023-extra-02`. Target `demo` / `11.0.0`. Root has read your complete audit and plan.

Read `IMPLEMENTATION-CONTRACT.md` first. Do not execute tests/build/validators during this phase.

Own23 nonnumeric Extensions runtime files. Add UIUtils screen-position/caller-buffer overload preserving legacy API, and publish exact signature early for Input. Complete raw-stop/deferred disposal/native cancellation acceptance sources, no scheduler extraction.

## Assigned review actions

### INPUT-POINTER-API (P2; approved-source-correction)

UIUtils directly reads legacy Input; add coordinate overload/caller buffer and upper Input adapter if New-only failure confirmed, preserving legacy APIs and dependency direction.

## Relevant full native sections

Source: NativeReview-A.md

### A-08 — P2: Inventory serializes a Sprite owned by the transient preview cache

Owner: `Neo.Tools.Inventory`, original task `neo-audit-1023-b07-05`; coordinate with supplemental Extensions task `neo-audit-1023-extra-02`. Current source: `Assets/Neoxider/Scripts/Tools/Inventory/Data/InventoryItemData.cs:62–72`, `OnValidate`; read SHA256 `18765663fff95ff1ec802b9952a785b19fc9848d4d8afdd9063f2d2d0caf3024`. Preview ownership/release is in `Assets/Neoxider/Scripts/Extensions/PrefabPreviewExtensions.cs`, read SHA256 `ae984eed918606ad7bbd33d36392269abf842756e2aef28731ee23dc2cdc25e0`.

Trigger: an item has no authored icon and a prefab without an authored Sprite; OnValidate stores `GetPreviewSprite()` in serialized `_icon`. The fallback Sprite is now HideAndDontSave and is explicitly destroyed by cache clear/lifecycle cleanup. Expected: persistent item data retains authored assets only, while UI obtains a fresh temporary fallback. Actual: persistent configuration holds a cache-owned invalidatable object. Save/reload/visual consequences have not been executed, so no persisted-corruption or visual-pass claim is made.

Smallest repair: stop assigning generated thumbnails to `_icon`; preserve authored/borrowed assets and let views request temporary fallbacks while binding. InventoryItemView already contains a fallback path, to be reviewed with the rest of Inventory. Confirm cache clear/rebind and authored-asset lifetime later; do not undo cache disposal to retain an invalid ownership relationship.
Source: NativeReview-A.md

### Extensions-Coroutines-Unity — source review complete; native semantics remain unverified

Original owner: `neo-audit-1023-extra-02`. Full artifact reads:

- `Docs/Audits/2026-10-23/Extensions-Coroutines-Unity.md` — `df2bc86a7999503d349fe9b073e670a912f617296dcef4440f64af4196e0aaf3`.
- `Docs/Audits/2026-10-23/Extensions-Coroutines-Unity.plan.md` — `6f35108c5e4cd8c4e6fc8634cf041628f93a940b2727c0dd580bd354087bff2c`.

Every changed supplemental source hunk was read, with the full startup/iterator tree/cleanup/reset/lifecycle call graph and relevant existing helper bodies. Current `CoroutineExtensions.cs` SHA256: `8397679b6a2aada37dd3309b07791c47f641623b283448f2804dcb89146e0fbe`. The complete `LifecycleAndOwnership.md` guide and both complete new fixtures were read:

- `Assets/Neoxider/Tests/Edit/Extensions/Audit20261023CoroutineUnityTests.cs` — `16cec2059e6322a134f5b15dc395997b21b55b70700e55cee5c11796e79cc57d`.
- `Assets/Neoxider/Tests/Edit/Extensions/Audit20261023CoroutineUnityLifecycleTests.cs` — `7ae920631e57100b9b3050834b793225854413c03ecaf848bc111f6189f955ab`.

Disposition: **completed fixes appear structurally sound**, with terminal state before cleanup, synchronous-completion protection, tracked nested disposal, owner destruction registration, active-hierarchy rejection, idempotent lifecycle cancellation, generated-only Sprite release and target lifetime checks. Global audio fades retain independent ownership/scaled time. Raw native StopCoroutine observability is a documented API limitation, not a defect that can be repaired by guessing whether a native Coroutine is alive.

Tests drive meaningful callback/disposal/order/lifetime state, track owned handles/objects and remove only new global helpers; Editor tests entering Play Mode remain an actual runtime driver when later executed. **Needs evidence:** native custom-wait/fixed/end-of-frame phases, cancellation during MoveNext/Current, reset/cleanup exceptions, raw stop→tracked Stop, and domain reload on/off. In particular ReleaseRoutine can be reached from native wrapper finalization while Advance is active; the claimed deferred-disposal contract must be checked through that native cancellation path rather than inferred from the `DisposeRoutines` guard alone. No executed defect is alleged without that evidence.

Per-operation list/proxy/cancellation-registration allocations and lifecycle snapshot allocation remain unmeasured; do not introduce a new scheduler or pooling solely from this review. CoroutineScope is a **future proposal**. Preview consumer ownership requires A-08, and ScreenPositioner's orthographic near-plane depth must be reconciled by its original Move owner using explicit migration notes, without rewriting serialized scene values automatically.

## Completion receipt

Write `Docs/Audits/2026-10-23/Extensions-Coroutines-Unity.implementation.md` with exact actions/files/additive API/dependencies, migration details and authored acceptance (UNEXECUTED). If blocked on another additive API, complete independent files and identify it precisely for root continuation. Do not edit shared review-action/manifest/other actor reports.

## Source3 released caller COROUTINE-POST-RELOCATION-NATIVE-CONTRACTS

Continue SAME original actor/session gpt-6.1-sol/max. General execution/freeze remain FALSE. NO Unity/compiler/test/checker/install/service/children/commit/push before ALL writers finish/newfreeze. Primary b03-04 has SOURCE-FINAL completed its54 helper relocations; root FULL-read dedicated21-line audit/17-line plan/112-line mapping and source-final declaration. ROOT NOW RELEASES ONLY your original module fixture CALLER files for the bounded behavioral correction below. The existing moved helper files under Tests/Fixtures, shared asmdefs/refs, original GUID/type mapping and extraction stay PRIMARY-EXCLUSIVE; do not relocate/recreate helpers or undo aliases. Read current source after relocation and actual source3 XML before patching. No production API/friend/serialized settings changes just to hide a fixture precondition. Retain every exact substantive positive/negative/count oracle, no blanket Ignore/expected-error/no-op/private fake lifecycle/ExecuteAlways/manual Update as native acceptance. Actual native callback claims require supported top-level Play transitions or real PlayerLoop/physics; pure public C# contract tests must say what they cover. Preserve UTF8/no-var, Samples~ and protected DocsEn.meta; no314 migration targets. Update honest own source receipt including deferred closure and stop. Proposed correction is unexecuted until later native run. Your Acceptance/UIUtils caller files released; shared phase probe/raycaster moved to Fixtures with original namespaces/GUIDs, do not undo. Source3 E05–E10 includes both old helper nulls and INDEPENDENT Stop/owner/target-destroy handles stillRunning or predicate fault missing. Review actual complete XML and callback/yield finalization. Admit native probes, real frame barriers and proper supported top-level Play setup first; then fix only demonstrated iterator ownership/timing fault preserving cancellation idempotence, exact cleanup/dispose/order, original exception/GetResult and WaitUntil/WaitWhile FixedUpdate/Update/LateUpdate/end-of-frame contracts. Do not claim helper move alone fixes nonhelper failures. Preserve native raycaster positive coordinate-hit controls. End-of-frame interactive capability must remain explicit, no new blanket skip or manual loop.
