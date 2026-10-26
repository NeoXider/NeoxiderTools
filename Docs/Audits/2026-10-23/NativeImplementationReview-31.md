# Native implementation review 31 — NI26 harness correction closure

Status: **COMPLETE; SOURCE ONLY. NI26-01/02 source-closed; NI26-03 comment-closed. No new confirmed residual in this bounded pass.** This is approval of the reviewed correction source, not compilation, build, wire, physics, persistence or native process evidence. All execution acceptance remains **UNEXECUTED**. Global executionAllowed remains false; the same `/root/verify_unity` is the eventual sole Unity operator after explicit root freeze.

Reviewer: SAME native `gpt-6.1-sol` / `max`, `D:/Git/NeoxiderTools`, `demo` / `11.0.0`. Only this report was written. No product/config/test/guide/other-report/recipe edit, Unity/MCP/compiler/syntax checker/validator/build/test/launcher, install/service/process management, child agent or commit occurred. Evidence is source reading and raw file identity bookkeeping.

## Admission and bounded coverage

Original owner remains Tests-Play-Network / `neo-audit-1023-extra-05`. Root explicitly confirmed both original/resumed PIDs 44700/25220 exited and DONE0 before review. The existing actor metadata is done/exit0 (SHA `f9e36faf6870170e83110a5455e17cd1384287159a0283064a3d8cb770153fab`); its recorded historical PID68836 is not misrepresented as fresh PID evidence. The first saved cli-liveness read was empty (`a5338d955b09046ec0b16f3a9625b7955c763aae07dc722e474e6078745f932f`). Final saved gate read `b279dab4845e4d3456f02baa7c51ed65ebaef023ba3641741df41610e4341789` contains only Neo.Editor.Tests PID25388, matching root's separately active PrimaryAssets correction, and no harness writer. No fresh process query was made and no moving PrimaryAssets source was reviewed. This is scope admission, not an all-writers-finished claim.

The **complete latest 155-line Net-extra receipt** was read at SHA `29d19357a718d98042d6d66de3fb98cbb7145343461ab911435b117e1cc651a3`. It explicitly includes NI26-01/02 and correctly retains UNEXECUTED acceptance. The complete current 50-line approved request was read at SHA `d95b0f50e7641e8411ac10eb3daa6e1a4a26a35d3067355a2728301c3e7aa7b7`, including appended NI26 instructions48–50 and retained integration/persistence/NI7/NI19 instructions. Receipt/source identities were reread after tracing and stayed stable.

Baseline is NativeImplementationReview-26.md (142 lines, SHA `4fe66bb4893395f000a39b8d93067dcf08564a7b5f5d7a18243dd68ac5c05dfa`). This pass reads the four changed harness/guide files, not another full read of the 2853 baseline C# lines. Scenarios582, World315, launcher609 and guide192 were read completely; other C# contents were reused only after exact SHA match. Protocol is unchanged. Network's accepted comment-only follow-up was checked separately below.

| Changed artifact | NI26 SHA256 | Current SHA256 |
|---|---|---|
| Assets/Neoxider/Tests/Integration/Network/Audit20261023TestPlayNetworkScenarios.cs |`40776c3eb113f1cda0394b13305aa07a48a1c025fcb8510dbfd90c710730405f`|`6de835e5966f6913b011d225daa17dce4a2078cea4be60ad0c63063e77e3670a`|
| Assets/Neoxider/Tests/Integration/Network/Audit20261023TestPlayNetworkWorld.cs |`c01e8c332c6c31fbd5f506630327ed5b6ebdaf84a3703cf479946a971c6f59d7`|`61608a17c4ba3573c19d6342529978f1f9424a5828a2272ef449a7a866b83314`|
| scripts/network_playmode_matrix.ps1 |`191c430b12611b2e171c5b5e17bae6f351be054de1cf846341ef3bc3a3681ad9`|`0da9ff4420250abc72eeeae2c1c3facfafbf87ed79cf4e84db8c0127f0c1098a`|
| Assets/Neoxider/Docs/Testing/NetworkPlayMode.md |`e7a05076b9153656a7246ee4fc3c36f75e4d0b182e119cbf4db49117fc11c386`|`327a0112ef97a484621e36172c5a5a44027f56e302d59a436ce63486eea1145d`|

Common prefix for the unchanged C# table: `Assets/Neoxider/Tests/Integration/Network/Audit20261023TestPlayNetwork`. All six hashes are identical to NI26; their complete prior reads are reused.

| Unchanged source | SHA256 |
|---|---|
| Artifacts.cs |`eed0a2230ab9c5ffa15fa0d565d92067ce4f0e334d9315ae115af79ac792ca9a`|
| CounterInput.cs |`3e2994cd8cae5731f63c9e407e47491494b299d9c6979bf1be4cdb6eeb1214a7`|
| MoneyPrecision.cs |`6d4438f6b0bbd04f2a074fc76253b2578d231f7fd8203f460c21461ceb5bdc7e`|
| PeerDriver.cs |`f520b2a9f1b99b554f0e353b8442bb38629b052c196e3bf836b23006b163ca22`|
| PlayerBuild.cs |`c6264174fc42c52140192a8a479f20d08d21dcc9f9057b68c8f119d84fc3443c`|
| Protocol.cs |`50939ca42857faafc34ca8bbaaa77b3a50f88b6025b1dd5b9b75f4e97eb2273c`|

`Neo.Tests.Network.Integration.asmdef` also matches NI26 exactly: `b034cf494ab48cfe72575ccec4d39e60bb0aef86dcc56e65791a9acac5fd07d2`. All nine associated asset metas plus Integration/Network folder metas and Testing folder/guide metas match the full 13-entry NI26 metadata ledger. No metadata repair/import/validator was run; there is no claimed new dependency, public API or serialized migration.

## NI26-01 — source-closed, real physics identity barrier and initial role oracles

Current `Scenarios.PhysicsCase:284–299` requires the actual A connection's player, nonzero netId equal to `_playerA` and distinct from B (288–290), verifies server Rigidbody/enabled collider, moves that body into the existing trigger293 and waits through existing realtime-bounded Until on `World.HasPhysicsEvent(player.netId)`294. New test-only World91–92 reads the named event list populated by the actual `PhysicsEvents3D.onTriggerEnter` listener263–267. It neither invokes the callback nor changes child state. Scope remains AllClients and TriggerOnlyForLocalContext=true (World249–250); client colliders remain disabled by the same factory policy.

The predicate is setup admission, not the final count oracle: Scenarios295–298 still supplies exactly one `physics` value equal to `_playerA` for server/A/B. Unchanged Protocol event matching still rejects missing, wrong-identity or duplicate entries. The initial role matrix is explicitly false/true/true for BonusA and false everywhere for BonusB. `Expected:526–535` clones the complete shared snapshot, sets the role BonusA and always false BonusB. `CheckServer:537–545`, both client instruction paths557–563 and independent received-snapshot recheck569–572 all use the corresponding role values. No Context check is removed.

Independent PowerShell `Assert-MatrixReceipts:466–476` requires exactly one authoritative physics record, nonzero A distinct from B, both dedicated-server children false and one actual-A physics event. Client checks503–508 require the same A/B identities, BonusA=true, BonusB=false and one identical event on each client. Run/PID/step/epoch and full snapshot correlation491–493 are retained. Wrong actor, wrong target/sibling or duplicate/missing event still fails; correct dedicated-server inactivity now satisfies the intended policy.

## NI26-02 — source-closed, reconnect retains transient-state boundaries

`RunServer:50–54` keeps the existing real B disconnect/reconnect, then retains StateChecks + Reactive + Context while explicitly passing serverBonusA=false, aBonusA=true, bBonusA=false. World273–286 still creates Bonus inactive, and no replay or extra state synchronizer was added. The same Expected/send/recheck paths above preserve BonusB=false on all roles. A's retained replica is expected active; B's freshly reconstructed A replica is expected inactive.

PowerShell477–484 now independently requires one authoritative reconnect record, retained A identity and both server children false. For each actual client,509–515 sets `$expectedLateBonusA` only for client-a, checks the child's exact boolean and sibling false, links A to the earlier physics identity and B to the server's current B identity, and retains the entire fixed reconnect tuple: Counter12, Selector3 with active0,2,3/excluded1, Money213, RPG73/120/7, property9/reactive15. The retained initial true state on B is still proved by the separate physics case before reconnect; no implicit replay or removal of Context controls masks the fresh inactive B branch.

Guide127–141 and receipt146–155 publish the same initial/reconnect matrices in English plus the Russian explanation. They keep source-authored/UNEXECUTED wording and distinguish transient context effects from replicated gameplay state. No runtime scope/policy/settings change or new persistence framework is required by this closure.

## Original matrix and assertion strength retained

Changed Scenarios was fully traced: original Counter concurrent public operations and server11,12 / client13,12 receipts95–130; owner/unknown/local-only/per-peer and scopes133–179; complete Selector ownership/additive/exclusion paths182–204; Money default/unknown/malformed/None/OwnerOnly tuple207–245; measured unscaled polling255–281; actual handler Unicode/value/count probes301–370; real reconnect/server stop-start/rendezvous373–402; original public execution, actual wire Money and reliable ingress fence405–489. `RunServer39–58` retains both unchanged MoneyPrecisionCases and CounterFiniteIngressCases before the handler/polling/physics/reconnect/restart phases. Shared ObservePeers changes only add role values; ownership, complete state/events, per-client observation and server recheck remain.

Unchanged Protocol/CounterInput/MoneyPrecision identities preserve NI7 pending versus confirmed status, stale0 restoration, zero-effective-payment denial, rounded stored debit8/nominal5, fractional10.25, every NI19 malformed/finite/raw-overflow vector and measured pre-handler/finite-peer controls. The complete current launcher171–439 was read: exact status/received payload/counts, unchanged/changed persistence SHA, per-process real file reloads, malformed operand kinds/bits, pre-handler effect counters, same-frame/window limits, finite duplicate throttle and independent B allowance remain mandatory. `Assert-NI7Receipts` and `Assert-NI19Receipts` are still invoked532–533.

All original plus NI7/NI19 required case IDs remain in launcher442–456; exact client/server received-snapshot correlation remains485–495. Counter12/13,12, owner/scope events, complete reconnect tuple, handler values101/103/105/107, RPG initialization120/120/10 and server relative deltas11,12 are still independently checked496–531. The patch strengthens physics/context/identity evidence without dropping another required case or using an Ignore/host/private-body fallback.

Isolation/build/bootstrap/provider/cleanup C# helpers are hash-identical to NI26. Launcher1–149 retains strict run/PID/namespace/source-token handling, fresh output/persistence prerequisites, hidden returned Process ownership and bounded barrier parsing. Launcher539–609 retains port mutex/TCP availability probe, ordered server/A/B launch, monotonic deadlines, zero-exit/required-success receipts, own-handle-only termination with forced-kill debt, registry names-only probes, retained stores and final failure/cleanup result. No recursive persistence deletion or new foreign-process control was added. These source checks do not prove native process behaviour.

## NI26-03 — independently confirmed comment-only closure

Current Neo.Network receipt is 189 lines, SHA `6b52735e36e6628e7a10feab8114e4ba9bdd856d7f174ae97a521e577ae64f92`, matching root's fully read handoff. Its NI26 append175–189 was read here; this pass does not claim another full read of all189 lines. Current `NetworkContextActionRelay.cs` SHA is `8df142cc3e3046474cff054ea26092519d92fe03d0b342ac3b1e6f62f012c873`. Actual1195–1221 retains host ApplyResolved+remote send and dedicated relay-only branches;1199 now correctly says a dedicated server only relays and has no local client view.

Read-only raw identity check: replacing that one new comment with the exact prior comment **in memory only** reproduces the NI26 whole-file SHA `b5d84db5d20f10f31b317a02d66fd4deaca317f834ebeb6dffc1c647e9f7a315`. The new comment occurs once (first/last decoded UTF-16 string index43553). Thus every other byte, including runtime branches/policies/events/APIs/line endings, matches the reviewed baseline. No file was rewritten by this check and no product checker/compiler was invoked.

## Acceptance boundary

No same-owner source correction is requested by NI31. NI26's bounded source findings are closed at the hashes above; execution debt is unchanged. Actual Unity6.3/6.6 compile/Weaver/discovery, IncludeTestAssemblies builds, baked identity/storage probes, three independent OS peers over Telepathy, physical engine callbacks, actual receiver/status/pre-handler/persistence JSONL, zero exits and clean owned teardown are all UNEXECUTED. A successful source review is not a pass for those capabilities.

PrimaryAssets remains a separate active owner, root documentation/final gates remain the coordinator's responsibility, and this report opens no execution or snapshot preparation. Continue only after root's explicit global freeze and execution authorization; retain the existing verification recipe and all native/manual/performance/query/UPM capability limits.
