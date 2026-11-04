# Selector getter implementation — independent SOURCE gate

2026-11-02. **SOURCE ADMISSIBLE for guarded root TEST-ONLY import/application and subsequent compilation/functional execution. No concrete blocker found.** This review read the complete product diff, full new502-line test source, base/review metadata and relevant reused support APIs; exact scope inversion also checks all otherwise-unchanged product bytes. Root full final-source review is still required. Nothing here is a compile/test/after-CPU/after-GC or final adoption PASS.

No Main/candidate/product/test/config/capture file was changed by this reviewer. No compiler, Unity, process, native raw load or new worker was started. The only write is this report.

## Exact identities / base guard

| Artifact | SHA256 |
|---|---|
| Candidate Selector.cs | `0c3280f3c4f881c31265c6e14f3c8656a6ded8c83f7335d9864f0511aea9a737` |
| Candidate SelectorModel.cs | `f8999334d87a714daf470141dfca95c6024ed6ac49c5d9f75199cffe7505aabb` |
| New Edit test | `42c18af8db189d3a7866581eb704d19b98cffa365d62aebc9d9ed7f570a1eb4a` |
| Complete product.diff | `f9df72310707247bbeff2c5a2fd27b70cd0c270d6152d140bf3e4c14135c1245` |
| REVIEW.md | `4dda6871d4fe450fea8f0082bc57890763d622da841ef39dd1d26bcac0158291` |
| base-inputs.json | `1b02c110a844a6ed10e28684fdd0611144f7d7cbefe2f24631cd28e72ac446a7` |

Root files still match original Selector `8117a7719e9f3808a7c48e0afc1f2bfeeed4336c106957d0d133b618a6fc3d15` and Model `45928c9c1ab01181de097ce5ca72d3614eabf6015d80c7a051a6fd87619d116e`. All listed output byte/hash records, implementation authority and seven supporting source hashes matched actual files. The intended new test path remained absent. The previously accepted [plan review](NativeSelectorGetterPlanReview.md) is retained; this is its separate implementation source gate.

## Product scope and semantics

Reversing the two boundary-getter calls and public CountActive expression reconstructed the **entire original Selector file byte-for-byte**. Thus all other public/private methods, serialized fields, metadata/BOM/CRLF, conditional network branches, mutation/lifecycle/save/event/reentry/Inspector bodies are unchanged—not merely selected examples. Model changes are confined to the two canonical scalar extractions and public-instance delegation; replacing that region with the old region reconstructs the entire original Model file. New statics are **internal**, with no public/friend exposure, field/cache, setting, asmdef/dependency or class/public-signature change.

Actual changed behavior path is exactly the approved one:

- IsAtStart/End call existing internal static GetBounds using **live Count, offset, allow-empty**, then compare **raw** `_currentIndex`. No wrap/clamp/state mutation while reading.
- CountActive evaluates `_count<=0 && HasItems` first and reuses unchanged CountActiveItemsInArray. This preserves activeSelf under inactive parents, null/destroyed skipping, duplicate array-entry counting, caller array changes, SelectorItem.ActiveValue and missing-component fallback. Positive raw virtual count overrides Items. Real-item counting acquires **no new offset validation**.
- Otherwise CountActive calls canonical scalar logical count with live fields. Model public instance APIs delegate to the new overloads; no second formula or frame/value cache is introduced.

Scalar order matches the existing methods: count<=0 returns0 **before** bounds; positive effective-index validates GetBounds **before** widened long addition/clamps; logical count obtains that validated effective index before empty/non-fill handling. Fill prefix/Math.Min and checked IndexWithOffset remain unchanged; invalid representable bounds still throw ArgumentOutOfRangeException from existing GetBounds with ParamName=indexOffset. The old count-normalization and empty-bound behavior remain where they were; no eager validation is added to real Items.

All old private `_model` consumer/caller reasoning from the plan still applies because the whole surrounding component is unchanged. In particular, private GetCurrentBounds/ComputeCountActive/ComputeLogicalCountActive, notification equality/ordering, ApplyUpdateSelection, SyncIncludedIndicesInspector and RefreshItemsFromChildren keep synchronization. Commands/queries refresh their private model independently; snapshots return new defensive state. Getter reads inside callbacks therefore have no newly introduced model-value cache; actual reentry/network/lifecycle execution remains a test obligation.

## Full new test assessment

The complete new fixture (machine-local `D:/Temp/Neoxider-LibraryClosure-20261102/candidates/lib-1102-04-selector/Assets/Neoxider/Tests/Edit/Tools/Audit20261102SelectorReadOnlyGetterTests.cs`, not committed) has **13 authored test methods /36 authored cases**, from6 Test +30 TestCase attributes plus supported SetUp/TearDown. This is source counting, not discovered/executed count. No unsupported NonParallelizable/TestContext dependency or `var` was introduced. Setup reflection writes existing fields only; it does not invoke private lifecycle/runtime methods or expose product internals.

The oracles are meaningful independently of the new shared helper:

- Fourteen explicit component/public-model cases and five effective-index cases use hand-derived constants, not only component-to-model comparison. Wide sums, Int32 extrema/max fill prefix, empty/count-negative early returns, checked raw overflow and invalid positive bounds in both fill modes are covered.
- The previously noted offset2/current−2 ambiguity is resolved with explicit allow-empty false/true cases: start=true versus false, active1 in both. Live-field reads change those flags and raw index repeatedly in one Time.frameCount while an earlier public model snapshot stays isolated.
- Real Items tests use an inactive parent, activeSelf positives, null/destroyed slots, in-place caller array mutation, duplicates, and virtual-count precedence. Invalid offset throws for boundary/positive virtual reads but **does not** stop real-item CountActive. SelectorItem.SetActive only changes logical ActiveValue and events in the actual support source; it does not toggle GameObject activeSelf, matching the explicit test controls.
- Repeated reads preserve serialized data, E/U/included snapshots and item-array identity with zero subscribed selection/object/count events and zero save writes. The same subscribed provider then observes a real Set(3): all event observers fire and two writes occur. This positive control prevents an uninstalled/inert observer from masquerading as purity.
- Paired with/without prior getter reads, commands refresh live edited fields and return explicit indices/counts; callback replacement finishes at the latest selection. Same-index Set retains explicit selection→object→index-save→excluded-save order and no redundant count notification. There is no no-op setter optimization hidden in this diff.
- Caller input mutation, deferred/direct self-input, throwing replacement enumerables and snapshot isolation preserve explicit sets/history; a single eligible unique-random value3 provides a deterministic positive result. RNG state is restored.

Reused Audit20261023NeoEditorTestsSaveScope is internal in the **same existing Editor test assembly**, so no new visibility/reference is needed. It installs a unique temporary FileSaveProvider and restores the exact previous provider. Actual FileSaveProvider SetInt/SetString emit OnKeyChanged even for same values, validating the expected two-write observer. Event handlers are removed in finally. Owned objects are inactive, destroyed only by the fixture, and no Selector destruction/save hook was added. No custom provider framework, owner prefs or allocation-counter surrogate is introduced.

Limitations: these are Edit contract tests. They do not execute native Start/OnEnable or prove network behavior/Player/SDK compatibility. Network is disabled on owned objects; absence of getter network code is source evidence, while retained admitted-graph ViewNetwork tests remain the native contract gate. Existing child refresh/remove/empty, save/load, unique reset, enumerable and event-order fixtures are not replaced.

## Root next and performance boundary

After completing root's own final-source read, root may guard both old hashes and import only these reviewed product copies/new test with owned metas. Require actual compile and exact discovered36 functional cases, plus retained SelectorTests/AuditFixes/ViewSelector/ViewSelectorInput and admitted ViewNetwork scope, preserving skips/failed premises honestly. A source-only authored count is not an execution receipt.

Before any after capture, independently review the later **source-identity-only** producer-pin revision for accepted final product hashes. Current f125 producer and626 reader are unchanged by this candidate. Keep N1/100/1000, K100 groups/300 getters, E/U/included/scalar setup, API bodies,120+120 outcomes, calibration4096/native bytes, empty control, marker/frame/thread topology and whole provenance checks unchanged. Old baseline artifacts remain immutable. No suppressed pin/workload/expected-value change may create apparent improvement.

The candidate removes repeated Configure/E/U copy work from the three logical public reads at source level. It does **not** establish an actual after allocation/CPU number, zero-GC promise, real-Items/mutation speedup, arbitrary threshold, Player/FPS/cross-SDK result or global closure. Those remain later root/native gates. No implementation file was fixed by this reviewer; only this source admission report was created.

## Actual functional admission: 95 passed on Unity 6000.3.14f1

Independent readonly review completed on 2026-11-02 after all three root jobs exited naturally. **ACCEPTED for the selected Selector functional scope: 36 new + 46 retained Edit cases + 13 retained Play network cases = 95 Passed, 0 Failed, 0 Skipped.** This supersedes the earlier pending compile/functional qualification for this exact source cohort. It does not establish after CPU/GC performance.

I read the complete three XML files and editor logs, launch records, testcase indexes, source manifests, public prefs comparisons and root summaries. I independently parsed every leaf, checked unique fullnames, and matched the index IDs/results to XML. Each launch identifies the main project, Unity 6000.3.14f1, D3D11, its intended assembly/filter and a fresh owned results directory. Every native exit code is 0; all logs contain normal test-run completion. Root counters agree with the actual leaves:

| Actual job | Native PID | Mode / assembly | XML leaves | Result |
| --- | --- | --- | --- | --- |
| selector-getters-u63-new36-r1 | 34072 | EditMode / Neo.Editor.Tests | 36 | 36 Passed |
| selector-getters-u63-retained46-r1 | 42956 | EditMode / Neo.Editor.Tests | 46 | 46 Passed |
| selector-getters-u63-network13-r1 | 39600 | PlayMode / Neo.Tests.Play | 13 | 13 Passed |

Evidence is under the task runs directory (machine-local `D:/Temp/Neoxider-LibraryClosure-20261102/runs`, not committed). The three complete XML SHA-256 values, in table order, are `0fdb6846067a14087d56b90907e7bc700e127f5bacc4357aea813f2894f2946e`, `89f73bf80a01db753f5a55280df970730a0fb6150829531b3c1553a176243a9c`, and `02b0f81f7b03f4ae6430050f2933827ae5ec844d2a900110469e3928cce37974`. The complete editor-log hashes are respectively `c222b1d247d9a77c06b3387a5f6e9fa7779556661de5966ab80d8a3e4ae334dd`, `70c1bdbb318a0cef861cd42abff72c7f3a05efecb151abe287a68f03fb296c37`, and `8248b2314db2c6c7de336c1a80106da38eb37d3ca261ff6ecf2e172437f75fd4` (778/590/1183 lines read). Summary artifact hashes matched the actual files. Logs contain no C# compiler error, assertion failure, unhandled exception or asset-import failure. Existing startup licensing messages and, in new36, obsolete audio API and Mirror Weaver dependency-resolution warnings remain in the raw log; this is not a warning-free-log claim.

### Exact retained identity and unchanged test oracles

The retained Edit set is exactly 17 SelectorTests + 3 AuditFixesSelectorTests + 10 Audit20261023NeoToolsViewSelectorTests + 16 Audit20261023NeoToolsViewSelectorInputTests. Its 46 fullnames equal the corresponding subset of `edit-main-u63-baseline-r1/results.xml`, with no additions, omissions or skipped cases converted into passes. The Play set is exactly the 13 fullnames of Audit20261023NeoToolsViewNetworkTests in `play-main-u63-baseline-r1/results.xml`. This comparison concerns those selected subsets; it does not promote unrelated baseline results.

All five retained fixture source files are byte-identical to their respective old baseline freezes. SHA-256 values are: SelectorTests `f41b801873281e10f3e3fd77c8d1cd078876a5bb802a181f180637c7407bfffa`; AuditFixesSelectorTests `fcf6136284924b294f0aac56c951620747ee19619114e7679b5713614eff5e35`; ViewSelector `fe8eadd9c11654ac7e1a3ed6401df18b29fdaee7850a07285667a5d64c4dfecd`; ViewSelectorInput `d634f105b755ebc2ae76c1474d36ff6106f4334425fdbfc7ecc0af8694e24793`; ViewNetwork `9c6cfbd416ab4451e385ddb76ae5b5baba50be8f6d933e85a652f268b9dc2422`.

For reproducible set accounting, SHA-256 of the ordinal-sorted fullnames joined by LF with a final LF is `b95ae7520e32dc71611a2cc5ed97733b5222f31e6a50d7106cda615835953bc1` for new36, `e8492b1f7426cb7801aacab33c75cbefb77e2c598c6206caf51e289b092524be` for retained46 and `99a734bbdc89ffc4e9e425b06d314d0043b88f1b1f441293f443e6dfa33e9ab6` for network13.

The new36 XML resolves the 13 authored methods and all parameter cases discussed in the full source review above. The passing scope includes hand-derived raw-bound/logical-count/extreme/allow-empty cases, validation ordering, live fields within one frame, real Items/activeSelf/SelectorItem/null/destroyed precedence, defensive enumerables and snapshots, paired commands with/without prior reads, reentry, same-index publication/save order and repeated-read purity. The save-purity test uses a real temporary FileSaveProvider and subsequently performs a command that produces the expected events and two writes; silent reads therefore have an exercised positive observer. These are reached functional assertions in the reviewed source, not a claim inferred from XML's aggregate `asserts` attribute.

The 13 Play cases exercise retained server admission, per-peer budget preservation, invalid/disabled/unknown-policy rejection, same-peer and independent-peer behavior, trusted null-server policies, final initial-state callbacks and local event order. Their source explicitly owns an isolated Mirror server with DummyTransport, constructs two NetworkConnectionToClient instances, calls protected typed server ingress through a small subclass and replays incoming SyncVar state. This is local server-core/initial-state replay coverage. It does not execute remote Command transport or provide the separate three-OS-process Telepathy/receiver acceptance, Player networking or cross-platform proof.

### Exact source cohort and preservation

The guarded Selector application receipt (machine-local `D:/Temp/Neoxider-LibraryClosure-20261102/candidates/lib-1102-04-selector/project-application-receipt.json`, not committed), SHA-256 `6b255232306e1647bd6b3c756730f24cba1108d70e069bf755d3c9ea82c09e66`, binds the source gate `050d4890da1a62c4ef21c5c68f031186134249f2e995324847d2ca0e0c2e7008` to the exact reviewed copies: Selector `0c3280f3c4f881c31265c6e14f3c8656a6ded8c83f7335d9864f0511aea9a737`, Model `f8999334d87a714daf470141dfca95c6024ed6ac49c5d9f75199cffe7505aabb`, and new tests `42c18af8db189d3a7866581eb704d19b98cffa365d62aebc9d9ed7f570a1eb4a`. Both old product hashes and unchanged product metas are recorded. I checked the actual applied hashes and owned new-test meta against this receipt; they match.

Each campaign source manifest contains the same 5957 package files. I independently checked all those identities against actual bytes before the subsequent Grid application, and all three recorded post-run frozen-path/protected differences are empty. The 12 protected resources match the preserved pre-main-merge identities. Every public prefs comparison retains 51 names with no added or changed original names; private prefs values were not read or published. The producer `f1257e0060a7c909fb771564e4b73bd935687422f21b805eed86f20dc3f90ca3` and reader `626a653c780dfc339f11c5c12abc9252439060e3aa6af092012487de3cf7f1a1` are unchanged. The previously reviewed public/serialized API, private mutation/lifecycle/sync bodies, asmdefs and dependency boundaries remain the source basis of this functional result.

Timing matters for later comparisons: network13 finished at 15:45:09Z, after the two Edit jobs. The separate guarded Grid application is timestamped 15:49:14Z and therefore occurred **after all 95 Selector cases**. Its receipt (machine-local `D:/Temp/Neoxider-LibraryClosure-20261102/candidates/lib-1102-03-grid-search/project-application-receipt.json`, not committed), SHA-256 `1469e02fdee7f2fa9370537e0e1771922110f526974c7cfeaf2971e9313a6eeb`, accounts for Grid `c107c61b82c0869a1f2e1848afd035e7a2d6a72f9ed983451108738aabf1b370` to `36810f5c4b13a9f961767382cee658e55e52a598318a58f021eb8b440cbd60fc` plus its new test/meta, taking the current package cohort to 5959. I checked those three later identities and the unchanged Selector pins. This expected later delta is not a failure of the captured 5957 Selector freezes, and these Selector runs are not functional evidence for the later Grid change.

### Remaining gate

No concrete blocker remains for the exact selected Selector functional scope. Root may proceed to the separately reviewed combined source-pin-only measurement update and unchanged-workload native after captures. The 95 passes do not establish after scoped bytes/calls/CPU, a zero-GC or throughput promise, Player/FPS behavior, another Unity SDK, remote-network campaign or package-wide closure. The accepted old baseline remains immutable, and its K100 unit remains 100 groups of three getters. After performance acceptance still requires actual fresh capture/readback with matching final pins, native calibration/empty controls, all 120+120 rows, thread/frame/stamp/GC-parent validation and unchanged operation/control semantics. No Unity process, compiler, test, native raw load, source/configuration change or candidate edit was performed by this reviewer.
