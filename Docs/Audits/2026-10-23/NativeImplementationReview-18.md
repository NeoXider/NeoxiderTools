# Native implementation review 18 — RPG corrections and consumers; FakeLeaderboard identity

Status: **SOURCE REVIEW COMPLETE. NI9-01/02/03 and the RPG guide correction are source-closed. Rpg.Network command23/snapshot integration, ConditionBridge readiness correction and FakeLeaderboard persistent-identity implementation receive scoped source approval. One bounded P3 evidence-wording correction remains for the SAME Rpg.Network owner (NI18-01).** Compilation, weaving, discovery, test execution and native/remote acceptance remain UNEXECUTED. This report does not open the global execution gate.

Reviewer: SAME `/root/verify_unity`, native `gpt-6.1-sol` / `max`. Workspace `D:/Git/NeoxiderTools`, branch `demo`, target `11.0.0`, HEAD `ca1985564dfe7726834e2832d51c33e0e63e060c` plus the reviewed uncommitted files below. This is a per-file source checkpoint, not a frozen identity for the whole concurrent working tree. Only this report is written. No product/test/public-doc/receipt/plan/config/asset edits, Unity/MCP, compiler, tests/checkers, service/install/process management, children or commits were used.

## Admission, ownership and read coverage

All four complete current implementation receipts were read, together with the complete NI9 report, the complete Rpg.Network, ConditionBridge and FakeLeaderboard requests, and the FakeLeaderboard contract section of NativeReview-B-contracts.md. NI9's original full RPG runtime/projectile review remains prior phase evidence; unchanged RPG/Combat internals were not broadly re-audited here.

| Scope | Original owner/task | Admission and disposition |
|---|---|---|
| Neo.Rpg NI9 correction | `neo-audit-1023-b05-03` | Receipt explicitly SOURCE-FINAL and names all three residuals. All 12 declared raw source identities match the current bytes. Root's liveness statement and the saved liveness artifact omit this actor. Source-closed for the bounded correction. |
| Neo.Rpg.Network | `neo-audit-1023-b05-06` | SOURCE-FINAL receipt; current consumer dependency is supplied. Actor absent in the saved liveness artifact. Assigned runtime and regression source supported; NI18-01 wording remains. |
| Neo.Rpg.ConditionBridge | `neo-audit-1023-b01-07` | SOURCE-FINAL receipt, actor absent in the saved liveness artifact. Assigned fixture/activity correction source-closed without weakening controller gates. |
| Neo.Tools.FakeLeaderboard | `neo-audit-1023-b07-01` | Reached only after the RPG reads. Receipt SOURCE-FINAL; updated saved liveness artifact no longer includes its writer. Thus admitted, rather than reviewed while writing. Persistent-identity source supported; native migration remains unexecuted. |

The source-liveness evidence was read from an existing root-produced artifact; no process enumeration was performed by this reviewer. At first admission its SHA was `61cd3287e76922c5dc5641770883ed3ad94f0e9aaf761de00798d7eeb92ae730` and FakeLeaderboard was still listed. At FakeLeaderboard admission the updated artifact SHA was `50fdd8fd9d0cd7f5789b4e44e2f9c1f97b2d728ce4f48ed79a78c8cd047fc3f9`, listing only View, Tools.Other, HtmlToUitk.Editor and Rpg.Demo. It is historical admission evidence, not a claim that all other writers have stopped.

Fresh source coverage:

- Read every NI9 runtime correction: complete RpgResourceRuntime and both binding files, Spend and its service/query/clock context, UpgradeStat and its facade/growth/init/profile/publication dependencies. Read all three complete current Edit implementation fixtures, including every added body, setup, helper and cleanup; all four NI9 public guides were read completely. The unchanged projectile fixture remains NI9 evidence rather than a fresh execution/read claim.
- Read both complete Rpg.Network adapters and their current changed hunks, the complete 552-line regression fixture, command enum/wire data and canonical Network.md. Read actual NeoNetworkComponent/NeoNetworkState admission, client/host and rate-limit paths as dependency context.
- Read the complete ConditionBridge runtime adapter, complete 295-line fixture and canonical guide. Read the actual CanEvade/CanUseAttack activity/readiness paths and input-disable API used by the corrected fixture.
- Read complete Leaderboard.cs and its current diff, complete new legacy JSON reader, both complete serialization/lifecycle fixtures, new metadata and both changed guides. Read the shipped legacy prefab's player/source/derived old-token locations and its meta as asset context; it was not loaded or rewritten. Singleton and ReactiveProperty excerpts were dependency context only. The concurrently owned legacy FakeLeaderboardBehaviorTests fixture and unchanged motion/item implementation are not re-approved by this pass.
- Current shared Edit and Play asmdefs, the two RPG consumer asmdefs and available named references were read. The root-added UnityEngine.UI Edit reference is present; no repeat integration request is made. An asmdef read is not compiler evidence, and test `MIRROR` discovery is still a real later requirement.

## Neo.Rpg — NI9 closure

### NI9-01: point-debit observer replacement

**Source-closed.** RpgCharacter.UpgradeStat at lines1299–1305 captures `_runtimeGeneration` before the point debit and checks it immediately after UpgradePointsState publication, before investments, UpgradeCount or derived refresh. A replaced runtime receives neither the old investment nor an attempted refund. The original accepted debit still returns true; XML and RpgCharacter.md:105–107 describe that acceptance receipt explicitly.

New UpgradeStat_DebitObserverReplacementOwnsItsInvestmentsAndProfile (fixture350–396) covers both ApplyTemplate and RebuildRuntime with either S=7 or S omitted. It checks replacement value, zero investment/count, no obsolete profile row and no extra old refresh. The ordinary control at399–433 checks point cost1, investment/count1, stat12, point/reactive/stat/resource events, no rebuild and the captured profile. These distinguish stopping after the debit callback from merely correcting a displayed stat later. Five authored cases; execution remains pending.

### NI9-02: positive zero-effective resource payment

**Source-closed.** RpgResourceRuntime.ClampCurrent:57–67 is the same callback-free clamp used by SetCurrentGuarded. RpgCharacterResourceService.Spend:138–142 computes that stored candidate and rejects unless it is strictly below Current, before reactive publication, ResourceChanged or pause mutation. The accepted candidate then uses the existing guarded setter/generation behavior. Finite/nonpositive/unknown checks retain their existing order. The facade's legacy unknown-ID/nonpositive distinctions were not silently unified.

All new controls were read: service two zero-effective cases, two rounded/fractional success cases and ten precedence cases; facade rejection/no-publication followed by actual rounded acceptance, plus fractional/zero/unknown/nonfinite controls. They assert raw and reactive balances, signal/event counts, pause0.25 and tick phase0.125 preservation, and transport-probe silence on rejection. The subsequent positive cost5 at1e8 stores a debit8 and publishes once; ordinary0.25 remains supported. No exact-nominal-delta requirement, double backend or callback-sensitive receipt reconstruction was introduced. Public wording at RpgCharacter.md:84–87 is accurate. Sixteen authored cases across service/facade; unexecuted.

### NI9-03: destroyed source binding tail

**Source-closed.** Both cached session predicates now begin with `character != null` under Unity semantics (RpgResourceBinding:110; RpgStatBinding:84), before accessing managed reactive getters. Exact source identity, id, version, enabled state and old-source delegate detach remain intact.

The two new resource/stat cases at binding fixture127–196 install an earlier real listener, verify live source/ListenerCount2 and ordinary50→51 output, then destroy the source during publication of52. The separate view remains enabled, old managed state retains52, and no52 output is accepted. Rebinding publishes7→8, removes the old binding listener and rejects later old-state99. These are meaningful liveness and positive controls; actual Unity-null/captured-tail behavior remains native execution debt.

### Guide and source integrity

InternalTypes.md:62–63 now limits nonfinite suppression to tick damage/regen and explicitly preserves independent UTC expiry callbacks. The existing invalid-delta/expiry test still expresses that behavior. All new NI9 controls total **8 methods /23 authored cases** (service14, facade2, upgrade5, binding2); the receipt's four implementation fixtures now total85 cases. Counts are source inventory, not discovery. No new public/serialized field, schema or enum identity was needed for these corrections. No additional bounded runtime residual was confirmed in this assigned delta.

## Neo.Rpg.Network — consumer closure and evidence boundary

The additive Execute case at RpgCharacterNetworkAdapter:282–284 dispatches ApplyLevelGrowth=23 to the core API. Existing ordinals0–22 and command signature/field encoding remain unchanged. Remote ingress still uses the current character networking opt-in, existing per-sender rate limit and manual authority policy. Core calls route on pure clients; server/host/offline paths execute locally. This pass does not broaden or redesign those accepted legacy policies.

Initial/rebuild publication context was traced through EnsureInitialized, RebuildRuntime, ApplyProfileCore and EndPublication. Readiness follows coherent derived state; RuntimeRebuilt precedes auto-load; nested publication suppression encloses both; complete pending publication follows it. Restore uses a call-local effective-Max lookup before Current/percent regen, leaves BaseMax unchanged and does not retain the lookup as future regen state. RestoreActiveEffects copies active entries without invoking buff application/current-grant delivery. These consumer boundaries agree with NI9's prior provider evidence and the current source identities.

The nine new MIRROR cases have substantive oracles:

- Active-server first initialization and already-spawned rebuild read the actual adapter's woven OnSerialize bytes. RuntimeRebuilt/OnProfileLoaded must retain the previous payload; completed state includes HP130/160, percent0.8125, regen16, BaseMax100, Mana7/25, XP17, level3, points2, S42/Other11 and no false death. Rebuild occurs after enabling/resolving and spawning the adapter. The first-init actor is later activated/spawned, with real isServer/netId assertions and unchanged completed payload.
- The original woven payload is re-fed before each public OnStartClient/ApplyNetworkState replay. Receiver state remains130/160, the active effect really exists, and no applied event/current grant is replayed. An ordinary refresh between deliveries resolves Max150, while re-delivering the original payload restores160. The global server flag suppresses the client SyncVar hook in this fixture; the public startup lifecycle supplies the body replay. It is not a remote peer.
- Registered command-delegate dispatch exercises encoded command23 with opt-out denial or opt-in/None acceptance (stat10→14, one event). A same-frame repeat after changing server level4 cannot grant16 or another event. Five v2 roundtrip cases retain the documented automatic/deferred/manual anchor policy rather than inventing a persisted anchor.

Current fixture inventory is **18 authored cases with MIRROR, one without**. The existing test helper's dummy transport and delegate/body invocations do not prove OnCommandMessage remote admission, real packets, ownership transfer, late join or reconnect. Host startup keeps the base `!isServer` application gate; networking opt-out and current toggle refresh remain present. Production transport peers and both symbol/dependency profiles remain unexecuted.

### NI18-01 — P3: distinguish first-init facade completion from isolated core-publication proof

**Confirmed evidence-wording residual, accepted by root for SAME Rpg.Network owner `neo-audit-1023-b05-06`. No runtime defect is asserted.** Exact current source: `Assets/Neoxider/Tests/Play/Network/Audit20261023NeoRpgNetworkRegressionTests.cs:264–266,279`, ActiveServer_AutoLoadPublishesCompleteSnapshotWithoutReplayingCurrentGrant; related receipt lines55–59.

Trigger/evidence limit: the first-init branch uses `source.NetDamage(0)`. RpgCharacterNetworkAdapter.RouteOrExecute:130–134 calls Execute and then NotifyStateChanged unconditionally on this local server path. Thus that branch's completed payload remains obtainable from the wrapper's post-Execute publication even if a final core notification were omitted. The branch still meaningfully proves actual facade completion, data integrity, absence of exposed intermediate payload and no replay; it does not independently isolate/count final core notifications. The already-spawned `rebuild=true` branch calls core RebuildRuntime directly and remains sensitive to the core→adapter final-publication guard. The core transport-neutral fixture separately has an exact one-notification oracle.

Expected: comments/receipt attribute these different claims precisely. Smallest repair: bounded comment/receipt wording only, preserving both substantive branches and assertions. If an already available small public lifecycle oracle can strengthen first-init proof, the same owner may use it; no runtime rewrite, synthetic seam, new test framework or invented native claim is required. Root explicitly accepted this disposition before this report was finalized.

## Neo.Rpg.ConditionBridge — readiness source correction

**RPG-BRIDGE-FIXTURE-ACTIVITY / C-03 source-closed.** The corrected matrix at fixture124–180 disables built-in input, configures assigned controllers and adapter while inactive, sets component enabled state, then activates the hierarchy at163 before checking either controller. CanEvade really requires isActiveAndEnabled; CanUseAttack rejects an inactive controller before readiness. The adapter still queries those controller policies without requiring a character.

Eight matrix cases cover both controller modes × enabled/disabled × inversion. They assert the actual controller predicate first, then public Evaluate(null), LastResult and exactly one OnEvaluated value. The former four inactive-but-expected-ready cases were replaced, not rescued by weakening the runtime gate. The complete current fixture totals35 cases in8 methods; missing character, local-result reentry/branch ordering, destroyed reference fallback, parent lookup, numeric boundaries and legacy resource-id coverage remain represented. Canonical guide31/74 now states active/enabled setup. Actual lifecycle/input-backend execution remains unexecuted; the controller's gameplay time/authority semantics are unchanged by this correction.

## Neo.Tools.FakeLeaderboard — persistent identity source approval

**LEADERBOARD-PERSISTENT-IDENTITY is supported by source review.** LeaderboardUser:24–78 preserves public readonly id and adds a serialized private key with FormerlySerializedAs("id") plus read-only PersistentId. The constructor initializes both; serialization callbacks only copy a known nonempty managed id into an absent key. Getters/sort do not generate ids, and readonly assignment through reflection or Unity StableId is absent.

Binding at Leaderboard:258–333 prefers a known canonical reference, otherwise accepts one distinct nonempty-key source object. Known repeated aliases are distinct from collisions; recognized aliases are canonicalized without erasing source slots and rank once. Multiple distinct keyed matches and keyless rows are not inferred from name, score, rank or derived sortUsers. Sync can append the canonical player while preserving unresolved rows, and duplicate status remains observable. OnValidate conservatively sorts nonempty unresolved/colliding lists without regeneration/truncation.

TryBindPlayerEntry:341–376 preflights invalid/missing/null source data before writes/events. A selected valid slot gets the current canonical player's values; collision repair renews only its owned key. When another slot aliases the current player, an explicit new canonical copy preserves that unselected row and readonly legacy id rather than changing its key through shared ownership. The operation sorts through the existing public behavior and performs no SaveProvider I/O. Its explicit canonical-reference change is documented. Start still overlays only existing `_Name`/`_Score` before synchronization; empty-key and flush semantics are preserved. No `_Id`/`_Snapshot` storage format, automatic asset migration or scene-reference requirement was introduced.

The new bounded legacy JSON reader uses writable legacy id DTOs, validates required aggregate data and duplicate player-key matches before publishing outputs, ignores derived sortUsers and retains keyless rows without name inference. It neither loads a live board nor supplies a general snapshot/persistence framework.

All **16 new serialization cases plus one new lifecycle case** were read in full. Whole-aggregate tests explicitly assert detached inline copies before binding, retain a same-name bot, then rename/update. Other cases cover reference precedence/highlight, ambiguous rows surviving sync, explicit repair with unselected row/key preservation and a second aggregate roundtrip, alias ranking, keyless data, invalid-binding no-write/event/snapshot mutation, real SerializedObject/OnValidate dispatch, raw old JSON conversion/rejection, actual task-owned PrefabUtility save/reload, and the shipped old-token prefab. The new natural-Start case checks save-overlay precedence and absence of new storage keys using the existing isolated in-memory provider. Tests invoke public behavior; native asset calls are authored rather than executed.

The shipped prefab exists at `Assets/Neoxider/Prefabs/FakeLeaderboard/Leaderboard.prefab` outside the hidden sample tree. Its old player/user/derived tokens are still present and its GUID matches the fixture. This is static asset evidence only. Actual nested FormerlySerializedAs recovery, callback timing, JsonUtility inline behavior, prefab import/save/reopen, Inspector dispatch and Play transitions require Unity execution on both installed versions. Raw DTO coverage does not prove authored-token recovery. Existing lifecycle motion/time-scale/RNG and foreign-world isolation remain prior/central execution debt, not a newly claimed pass. No additional bounded defect was confirmed in this assigned identity implementation.

## Costs, compatibility and remaining acceptance

NI9 admission checks are constant work and retain the prior resource/derived mutation model. The live-source predicate adds one Unity liveness check per captured binding callback. Snapshot suppression coalesces initial/rebuild/profile publication, but ordinary notification paths still serialize a complete profile; no aggregate byte/FPS result is inferred. Rpg condition evaluation remains explicit-call local/parent lookup rather than Update/scene-wide polling. Leaderboard canonicalization/queries scan source rows; sort allocates the existing ordered snapshot and refreshes rows, with an extra reference/key scan. Repair/DTO migration allocate only on explicit operations. No new Physics query loop, per-frame identity generation or shared global identity registry was added. Virtualization/publisher frameworks remain optional proposals.

After the explicit global freeze, the SAME verification operator still owes actual installed Unity6.3/6.6 compile/weaving and nonempty discovery/execution, both MIRROR dependency/symbol profiles with actual skips, isolated persistence/company/product receipts, native destroyed-source callbacks, controller lifecycle/backend admission, true prefab/old-token migration and save precedence, and separate production-transport server+two-client acceptance. Core projectile/native performance and other still-owned consumers retain their prior reports/debt. The present dummy server and lifecycle replays are not those peers. No tests or validators should run merely because this report is complete.

## Raw identities and coverage checkpoint

Main raw-byte bookkeeping at `2026-10-23T20:48:32.899020+00:00`, with dependency identities reread while finalizing; no product validation was run. `full` below means the current file was fully read in this pass; `scoped` means only the named methods/call graph above. Paths in the table are relative to the absolute workspace `D:/Git/NeoxiderTools/`. Earlier truncated display intervals were reread before recording coverage. RPG's twelve declared identities all match its receipt; all other hashes identify the actual reviewed current files.

| Workspace-relative path | Read scope | SHA256 |
|---|---|---|
| `Docs/Audits/2026-10-23/Neo.Rpg.implementation.md` | full | `cb0dfdb0a9bc1e0844ea383cb1356e567d4c3e2c01a5be1ac79762741d33530c` |
| `Docs/Audits/2026-10-23/Neo.Rpg.Network.implementation.md` | full | `ee5e2df4cf5b5e65ddbdb16cdb4b7c6caa1673f778f4c68146c66b9bf42c1172` |
| `Docs/Audits/2026-10-23/Neo.Rpg.ConditionBridge.implementation.md` | full | `0f95d2a1401832c5e6982f74a494e3915d772a0dce5684350174e35baecfc8a4` |
| `Docs/Audits/2026-10-23/Neo.Tools.FakeLeaderboard.implementation.md` | full | `101c6e63d328002377f4eef49f5933cac0992bf23edc5e0ab279c871f86902a3` |
| `Docs/Audits/2026-10-23/NativeImplementationReview-9.md` | full; prior phase evidence retained | `946e6c559b45bb4bac1e60f7e9e8e4fa4c6eed9cd99260b0cf240dd011fdce50` |
| `Assets/Neoxider/Scripts/Rpg/Components/RpgCharacter.cs` | scoped NI9 corrections and network/growth/profile publication context | `9dfadfbaac83b7797da38ff0064979648ef3214b1fad98a7018a1e733bfd637c` |
| `Assets/Neoxider/Scripts/Rpg/Runtime/RpgResourceRuntime.cs` | full | `5452d4b995547b89b0a035f9e4ae407781bd613a5fa400c927a2b5ff9af5f439` |
| `Assets/Neoxider/Scripts/Rpg/Runtime/RpgCharacterResourceService.cs` | scoped Spend/query/restore/regen/RefreshDerived context | `3a71ffc8f6528288e5aa9d90fe5a84a375096921e4cef59e5e09b9233936c5f5` |
| `Assets/Neoxider/Scripts/Rpg/Components/RpgResourceBinding.cs` | full | `542aad718b19a7adb065d87e6a5194ef826b1afc137925f15161831e7ad7aa23` |
| `Assets/Neoxider/Scripts/Rpg/Components/RpgStatBinding.cs` | full | `91e070f366d19ebbe37dc26347f68d85382f857b696aa04676ce87493af9829a` |
| `Assets/Neoxider/Tests/Edit/RPG/Audit20261023NeoRpgImplementationRuntimeTests.cs` | full;14 new NI9 cases | `082158e032c46f810eec8eaf40d5708320f0d01c08100095c7ffdac3f630ab75` |
| `Assets/Neoxider/Tests/Edit/RPG/Audit20261023NeoRpgImplementationCharacterTests.cs` | full;7 new NI9 cases | `75eeb9be124c256a5e84457a859416126025ace9355744cf14e7cf96d5d6cc23` |
| `Assets/Neoxider/Tests/Edit/RPG/Audit20261023NeoRpgImplementationBindingTests.cs` | full;2 new NI9 cases | `a635e3f46afd905e3c7f9d83e4c32a587f142988a140fd7b87aa6ae44696b68d` |
| `Assets/Neoxider/Docs/Rpg/InternalTypes.md` | full | `2ead60e62f9809e368bba2e618a8f129310b1b2416020b4b5f90e1a753e72086` |
| `Assets/Neoxider/Docs/Rpg/RpgCharacter.md` | full | `547b8897909bdfd411d9190066cc1d3dbe35c0e0cd36cdd4f608c1b0f5e8bb2d` |
| `Assets/Neoxider/Docs/Rpg/RpgResourceBinding.md` | full | `025a97a762d4d4d67571e0627ad9ed5377c442d6a1bdfaa045276b8f5f27f3b4` |
| `Assets/Neoxider/Docs/Rpg/RpgStatBinding.md` | full | `9f024a66d118152ab274a1a138813f1e7a3553769d0a81ad0ddeec9bf06d6243` |
| `Assets/Neoxider/Scripts/RpgNetwork/RpgCharacterNetworkAdapter.cs` | full + current diff | `41d917bed933ae330e35bec822df5e103b60786667284c75ea12753479c660cb` |
| `Assets/Neoxider/Scripts/RpgNetwork/RpgAttackNetworkAdapter.cs` | full + current diff | `a9e143727c687e901abefaea8f175d76957049baa30da1884652d65057de9163` |
| `Assets/Neoxider/Tests/Play/Network/Audit20261023NeoRpgNetworkRegressionTests.cs` | full;9 new cases | `308a59ed536de632dafb2ce00322968904723d49f7a43fb3b617889f230841ae` |
| `Assets/Neoxider/Docs/Rpg/Network.md` | full | `8f6817a269856155a53dde0192c54e0ad1f40482e298572210e781f39eacbe34` |
| `Assets/Neoxider/Scripts/Rpg/Bridge/Condition/RpgConditionAdapter.cs` | full | `1043b3d483fbbfc12d46553b6a9b5e585920efc19aa8ff8148468a07240e2bcf` |
| `Assets/Neoxider/Tests/Edit/RPG/Audit20261023NeoRpgConditionBridgeTests.cs` | full;8 replacement matrix cases | `1fc90c2ba4910b1d69c518d16dc4aabdf296c9670df895f2c868cfabc2111033` |
| `Assets/Neoxider/Docs/Rpg/ConditionBridge.md` | full | `28acf9ee0b42587f76c93e2beec00cb10d8a8b074df4d44088c5f232df9c84ca` |
| `Assets/Neoxider/Scripts/Tools/FakeLeaderboard/Leaderboard.cs` | full + current diff | `8c3a896137e7bacf5f153fc3619c744265e90b35bf7eb0cedeed9878d4d44269` |
| `Assets/Neoxider/Scripts/Tools/FakeLeaderboard/LeaderboardLegacyJson.cs` | full | `46e91eedbf43612dca286ad6ab7d0f390819d297a24cfa5c54fc9d7b1eb2be86` |
| `Assets/Neoxider/Tests/Edit/Tools/FakeLeaderboard/Audit20261023NeoToolsFakeLeaderboardSerializationTests.cs` | full;16 cases | `94e8ff1dc7d94da6a15e650796ddfd4a53cba176df3036b484ac1ebf3d482f59` |
| `Assets/Neoxider/Tests/Edit/Tools/FakeLeaderboard/Audit20261023NeoToolsFakeLeaderboardLifecycleTests.cs` | full;1 new case | `cc939fe4df11cee5ac164c104b2dc0c06ee95ea49827e06112376db7ae26e2ec` |
| `Assets/Neoxider/Docs/Tools/FakeLeaderboard/README.md` | full | `02bd4460336e40e7acc428b878d47e24412693409f6ab4b4ad41527f2984aa69` |
| `Assets/Neoxider/Docs/Tools/FakeLeaderboard/Leaderboard.md` | full | `5811ab3ddbc4f2288e01eacf14e76558fc82dfd2cdac902f8f5c9704d13f4e2b` |
| `Assets/Neoxider/Scripts/Tools/FakeLeaderboard/LeaderboardLegacyJson.cs.meta` | full | `3678ff7ef9dd94c2843f9b51a779dd4f2072205f9b78c68a9fb7bf670f017e8f` |
| `Assets/Neoxider/Tests/Edit/Tools/FakeLeaderboard/Audit20261023NeoToolsFakeLeaderboardSerializationTests.cs.meta` | full | `ba60d668a95ad1e88e0910e876edf6bdfb474b961397b2eee63f51108a7d8743` |
| `Assets/Neoxider/Tests/Edit/Neo.Editor.Tests.asmdef` | full; dependency context | `204a5ba6382792131c48cae74f38e27f5431909c0c0866c3b2905be0f2c4b4a5` |
| `Assets/Neoxider/Tests/Play/Neo.Tests.Play.asmdef` | full; dependency context reread after concurrent shared-reference additions | `a7c2ed5da43c940c4757a507ab1c07bb10ff992f66c001177ce0c8a03a429a9d` |
| `Assets/Neoxider/Scripts/RpgNetwork/Neo.Rpg.Network.asmdef` | full; dependency context | `b1fe93622577ea9d9f4174f8a98628a24c574d6f951168f15f25f05b121dac4c` |
| `Assets/Neoxider/Scripts/Rpg/Bridge/Condition/Neo.Rpg.ConditionBridge.asmdef` | full; dependency context | `252223fe61d7e65957dbc813a46d305f8a8437ffe691174de3ea20de9da18e9d` |

Additional artifact/dependency identities (context is not whole-module approval):

| Workspace-relative path | Read scope | SHA256 |
|---|---|---|
| `Docs/Audits/2026-10-23/IMPLEMENTATION-CONTRACT.md` | previously read full; identity unchanged | `d5bc7a2d934e20f40647c45d53746c3734a1dbbfe95f182600d66977e9bbcbab` |
| `Docs/Audits/2026-10-23/Neo.Rpg.Network.implementation-request.md` | full | `bee23ac79973caec5d3e8a5f2d04d1e2b6f60e459f240c4e3428b04c6ee9a805` |
| `Docs/Audits/2026-10-23/Neo.Rpg.ConditionBridge.implementation-request.md` | full | `218aa10630a6a7ccd5b3dd0289ca1c90cbfb0ca58a707c0f71d6db35fd7c0113` |
| `Docs/Audits/2026-10-23/Neo.Tools.FakeLeaderboard.implementation-request.md` | full | `40cd7434a467d865dc7654a6c06b15c97910f23228919d21a4215a97f0d6006c` |
| `Docs/Audits/2026-10-23/NativeReview-B-contracts.md` | lines1–44 FakeLeaderboard contract; later section context excluded | `e75ac9f477e99f39ab633da4acc3c679b77655c33b0f0c9833288447c0332c36` |
| `Assets/Neoxider/Scripts/Network/Core/NeoNetworkComponent.cs` | full; unchanged consumer dependency | `45012ee0df1c54ffe20ae7b2786c33daf1c0ad57f81a0bd36f62ec6bf6913e60` |
| `Assets/Neoxider/Scripts/Network/Core/NeoNetworkState.cs` | full; unchanged consumer dependency | `1e86b88bb98d9b6a1cffe0fb026e2905f7fe412ed0d3638fb8cf5a17e20156c6` |
| `Assets/Neoxider/Scripts/Rpg/Runtime/RpgCharacterNetworkCommand.cs` | full | `a3241ff1a7095b693e1ed810ae5f7af88cc45a48981e9877ca092232036fdd00` |
| `Assets/Neoxider/Scripts/Rpg/Runtime/RpgEffectShelf.cs` | restore/copy/modifier context; prior NI9 hash unchanged | `1c81202e74067d647129ffd79c4323cf04f665b97997a6b4f39256e5280bf41c` |
| `Assets/Neoxider/Scripts/Rpg/Components/RpgEvadeController.cs` | CanEvade/input/lifetime context | `40cb0db451fbcfb99a7fa7e7d243e743a2ced3797a9f7265971f0000ae36d2a8` |
| `Assets/Neoxider/Scripts/Rpg/Components/RpgAttackController.cs` | public string/definition CanUseAttack and readiness/lookup/input context | `b826bf64fbf932aad55dae867370112a7391d0a2dff17d62a38380f8982a4c7e` |
| `Assets/Neoxider/Prefabs/FakeLeaderboard/Leaderboard.prefab` | old player/users/derived tokens and authoring settings only | `7a5acbe3afad2a7536aa8a8af9de0dff743a7cc8bf1d9d55a2fa7693a8a9e434` |
| `Assets/Neoxider/Prefabs/FakeLeaderboard/Leaderboard.prefab.meta` | full | `db44d01d8aab5ff8b88e1442c4f7d38aa37b37a164e2833b0f4f9d1d3953911f` |
| `Assets/Neoxider/Scripts/Tools/Managers/Singleton.cs` | lines1–160 lifetime/registration context | `fcc91f59e26dec884c2604943d0de58aee7c8023c2a1fae20ac89b113e71c874` |
| `Assets/Neoxider/Scripts/Reactive/ReactiveProperty.cs` | equality-policy excerpt only | `14df81d1e13d76b658e20e8560dfa14376ce882c716a827da80f34e03256b2a4` |
| `Assets/Neoxider/Tests/Play/Network/NetworkTestHelper.cs` | CreateTestNetworkManager/SetAssetId excerpts only; central owner retained | `09c518b1bf11feac9f9592c96d178d0f91c01aaf1497ef4a5bdc5f241cbe2f81` |

The current NetworkTestHelper CreateTestNetworkManager/SetAssetId excerpt was also read: the fixture really creates DummyTransport, sets it active and activates its test manager. This dependency is contextual, remains centrally owned, and is not granted whole-helper approval by this report. Existing isolated-host/exclusive-session preconditions still apply.

During final identity reread, the shared Play asmdef changed from `1856ba6d704ec0852b65cf22f63fdc0427818c49259f6f2032488b02745d62ad` to the recorded current hash, adding Neo.Tools.Text and Neo.GridSystem references. Its complete current text was reread; this reviewer made no config edit. All scoped product/fixture/guide/receipt identities remained unchanged at that reread. The extra references do not change this source review's RPG/identity contract and provide no execution evidence.
