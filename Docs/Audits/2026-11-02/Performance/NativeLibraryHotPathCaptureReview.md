# Grid / Selector native capture pair — source admission review

2026-11-02. **Current pair NOT ADMITTED: one concrete reader resolver blocker.** Producer source is otherwise suitable for the agreed bounded workload. Root module import/native gate stays CLOSED until the same reader author corrects the resolver and its README, returns new source hashes, and root/independent review accepts that delta. No other concrete blocker was established by this full review.

This is **SOURCE review only**: no import, compiler, Unity, native load, test, capture, CPU/GC result or optimization execution. The reviewer read both complete C# files, both READMEs, contract/base metadata and relevant existing product/regression APIs. No other author's file was changed. The only write is this report; no subprocesses were started.

## Reviewed identities

| Artifact | SHA256 |
|---|---|
| Producer `Audit20261102LibraryHotpathMeasurementTests.cs`, 44660 bytes | `6bbdd57862f3a5231edb23b011c7b1006680621b9edba052ea749677072e886c` |
| Producer README | `039626ebf17dd6bddf74778c88e2a04727690b333d1c7fb24ae42fcdfdfa03be` |
| Producer base-source-hashes.json | `4c4abddd1e3326601da102d5c080d8cebe758febd56965ec6ce2cc1aafafd8f5` |
| Reader `Audit20261102LibraryHotPathProfileReadback.cs`, 39492 bytes | `263a341e84512e395006b16dbc46aea77394a1692d724943c3b96b9a07710ab4` |
| Reader README | `6327a979fb9233e0b12a7d2e86e2dd26c504cbae7d619d20cd65308a6b98fa62` |
| library-hotpaths-measurement-03.md | `66d12d82e5517256a08a1a169a8990604804425866dc811ae4ef02b1cac0796a` |

All 13 declared existing source pins and both external reference hashes matched actual current bytes. Both intended project import paths remained absent at this check. Both C# files use explicit types without `var`. Root reported the current main count5951 after the separately reviewed two Input capture wrappers/metas; those four owned additions are not an unexplained mutation of the 13 pinned product/reference sources.

## Blocking finding: package-relative pins resolved from project root

The agreed contract and producer emit **package-relative** paths. Producer ReadSourcePins uses entries such as `Tests/Play/Tools/Audit20261102LibraryHotpathMeasurementTests.cs`, `Scripts/GridSystem/FieldGenerator.cs`, `Scripts/Tools/View/Selector.cs`, and hashes them under `Application.dataPath/Neoxider`.

Reader ValidateMetadata line285 (machine-local `D:/Temp/Neoxider-LibraryClosure-20261102/fixtures/Audit20261102LibraryHotPathProfileReadback.cs:285`, not committed) combines those paths with **Project**. Its following OwnedPath check requires the result below `Project/Assets/Neoxider`. A real producer fixture pin therefore resolves to:

```text
D:\Git\NeoxiderTools\Tests\Play\Tools\Audit20261102LibraryHotpathMeasurementTests.cs
```

It lies outside the required package root and is rejected **before Native LoadProfile**. Reader post-readback line312 (machine-local `D:/Temp/Neoxider-LibraryClosure-20261102/fixtures/Audit20261102LibraryHotPathProfileReadback.cs:312`, not committed) repeats the same base-path mistake. The reader README's sample `Assets/Neoxider/...` pin is project-relative and contradicts the producer/agreed package-relative schema.

Required bounded correction: resolve and recheck metadata source pins consistently beneath the package root, retaining canonicalization, escape/reparse/duplicate/hash protections; correct the README example and returned source identities. Return this to the same reader author. **Do not alter producer workload/DTO values or add guessed aliases to work around the wrong base.** No new product or package change is needed.

## Producer workload/source assessment

Public visibility was checked against actual pinned code and unchanged Neo.Tests.Play references (`Neo.GridSystem`, `Neo.Tools.View`). FieldGenerator/Config/Cells/GeneratedSize, FieldCell fields/flags/UserData, MovementRule constructor/Directions, Match3BoardService.TryFindValidSwap, Match3MatchFinder.FindMatches, Selector properties/events/Set/CreateModelSnapshot and SelectorModel getters are public and directly callable. `isNetworked` is a public inherited field in the existing optional-network wrapper. No internal-only API or singleton setter access was found. This source-level check does not replace compilation.

### Grid

8×8/16×16/32×32, depth1, K=1. Positive embeds the exact existing directed-down 3×2 regression patch `1,3,1 / 2,1,2` into otherwise unique positive content. Canonical swap is `(1,1,0)`→`(1,0,0)`. Independent setup oracle uses the existing public live Match3MatchFinder and temporary swaps restored in finally, requiring no initial match, exactly one positive legal directed edge and no control legal edge. It does not clone the product snapshot/search algorithm.

Disabled, blocked, occupied cells and type/flags/distinct payload references are present. Full array/config/cell/payload identity and state are sampled outside operation scopes each selected frame, retained as flags, then asserted after stop. Final serialized config/service state is compared as well. Match3 auto-generation is disabled before activation; native Awake/Start precedes capture. Borrowed FieldGenerator.I blocks admission; both rigs are owned, the final owned singleton is checked, teardown destroys only owned roots and verifies clearing.

**Performance qualification:** this positive is a directed canonical regression search, not random/four-direction or worst-case positive search. The control searches a no-swap board. Measured costs may differ substantially between those paths and dimensions; no representative-game or whole-module performance guarantee follows.

### Selector

N=1/100/1000, K=100 **groups of three getters** per frame: IsAtStart, IsAtEnd, logical CountActive. The pure control reads the corresponding existing SelectorModel snapshot getters. The snapshot and a separate mutated isolation probe are created before capture; E/U/included arrays and flags are validated and recorded. E and U are nonempty, including the N1 case; current public Set semantics mark the current index used in unique mode, so setup does not require a new same-index contract. Included pool excludes E independently of U; N1's empty included pool is explicitly recorded rather than presented as a default empty configuration.

Save/network/child-auto-update/object activation/debug/start behavior is disabled on the owned component only. Setup setters and list construction occur before capture. Measured scopes contain only the direct getters and struct assignments; all 24000 group outcomes are retained and checked, along with unchanged component/model state and silent subscribed selection/object/active events. Public enumerable isolation is preserved. State/list/JSON/hash checks do not become measured getter work. No product cache, public API, optimization or runtime marker was introduced.

## Producer/reader protocol assessment apart from blocker

Required DTO field names/types match: schemaVersion/module/action/workloads/unityVersion/K, 240 ordered frame records, phase/ordinal/timeFrameCount/operations and sourcePins path/hash. Producer adds outcome/state/provenance fields; reader deliberately ignores those extras and leaves product-result authenticity to root/fixture checks. The source-path base above is the concrete incompatibility.

Producer preallocates all outcome arrays. Eight frames warm both actual paths and identical writer/marker boundaries with Profiler disabled. Recording has exactly one retained4096 calibration outside measurement, 120 contiguous positive frames, eight unstamped transition frames, then120 control frames; expected phase-start gap is9 Time.frameCount ticks. Each selected frame contains one separate phase stamp, one empty negative-control scope and exactly K disjoint ProductOperation scopes. Final yield flushes the last native frame; owned flags/file are restored in finally. JSON/hash/I/O and final assertions occur after recording stops, and success companions are written CreateNew only after source-side outcomes/state checks pass.

Reader reuses proven ca85/746d native API/lifecycle patterns for exact Unity6000.3.14f1/project: explicit owned batch, no active profiler/binary log or existing history, guarded completed raw/metadata/manifest streams, expected hashes before/after, one load event, bounded actual native range, fresh output and failure receipts. Installed public RawFrameDataView metadata/thread/recursive-subtree APIs match prior native-accepted helpers; no guessed parent API is used. No global history reset, preference mutation or binary parser is added. Native compilation remains unverified.

Every valid thread view is scanned with the cap4096 failure guard. Named scopes require one persistent nonzero Main Thread identity; selected frames require one matching view. Invalid/crossing bounds, unknown markers, nested operations, invalid topology, duplicate stamps or bad metadata fail. Operation CPU sums K disjoint inclusive subtrees. Each GC sample contributes once to each enclosing scope's inclusive fields and once to the named union; parent stamp and operation totals are not added together to invent aggregate product cost. Main-frame/all-thread context stays separate.

Reader requires 120+120 contiguous native frames, correct phase order/K/one empty scope, empty GC calls/bytes0 and exactly one outside-phase calibration event with byte metadata≥4096. One constant Time.frameCount/native-frame offset joins both phases to actual metadata. It imposes no product zero-GC budget or CPU threshold; success only exports metrics, not PERFORMANCE_PASS. Missing/drop/skew/source drift is not accepted as zero.

## Root execution/provenance conditions after corrected reader admission

1. Root must review the corrected full source/diff and new hashes, then import only the temporary reviewed producer/reader between owned jobs and compile. First bounded capture should be Grid8/K1, including its real calibration and positive/control assertions, followed by native readback. Compilation, calibration/native raw readability and outcomes are still NotRun here.
2. Use one actual exact leaf and absent owned directory per capture. Guard against the already observed filter-prefix pitfall: plain `Capture_Selector_1` can select `_100/_1000`. Root should use its verified exact-selection mechanism and require exactly one discovered/executed intended leaf from XML; do not assume a fullname string guarantees exact CLI matching. The producer's fresh-directory guard makes accidental multi-selection fail, not silently become a correct single capture.
3. Root must correlate actual XML/log/raw identity, finalized metadata/counter hashes, fixture hash/MVID and every package-relative source pin with its independently frozen manifest. Reader hashes the manifest but does not authenticate its dynamic file map or compiled-binary origin. Source pin role/labels and all source-side result/state assertions remain explicit root obligations; metric export alone does not prove Grid legal-swap correctness or Selector parity.
4. Producer records no operations/empty scopes in the binary transition/warm-up path. Reader separately counts any unselected scopes; root must inspect that context rather than relabel unexpected work as measured. All-thread GC includes harness/Editor/other threads and must remain separate. No empty-overhead subtraction, threshold invention, synthetic metadata/XML, final source cleanup or library-wide performance claim follows from source prep.

This bounded review found the resolver/README issue only. It does not reopen all prior module audits or authorize edits to other authors' files. The pair stays blocked pending that source-final correction; no real CPU/GC result exists yet.

## Corrected reader — source re-admission

**The package-root blocker is resolved. Pair SOURCE ADMITTED for root TEST-ONLY import, compilation and a first small Grid8/K1 calibration/capture/readback.** Producer remains exact `6bbdd578...`; corrected reader is `626a653c780dfc339f11c5c12abc9252439060e3aa6af092012487de3cf7f1a1` (39775 bytes), corrected README `d65c0c762c1ff01328b72488227fd3ca334942abf2b6f9cb9e74033f43cb0667`. No remaining concrete Module source blocker was found in this bounded re-review. This supersedes the source NOT ADMITTED disposition for the old263a reader above, without turning source admission into native success.

The complete C# change was checked: preload line285 and post-export line312 now both call one private `ResolvePackageSource`. The outer entry still rejects a null pin; nonempty/relative/no-alternate-stream validation moved into that shared resolver. It combines `Project/Assets/Neoxider` with the original package-relative path, then applies the existing canonical owned-root/reparse protection. Normalized duplicate detection, actual hash checks, before/after source checks and original relative receipt spelling remain intact.

Removing that focused helper and reversing those two calls plus the consolidated null/path guard reproduced the old source **byte-for-byte**, SHA256 `263a341e...`. Thus native scan, calibration, GC uniqueness/union, K/phase/frame/thread mapping, export, budgets, ownership lifecycle and API surface are unchanged. Archived old reader/README under `review-history/lib03-reader-before-package-root-r1` independently retain full263a /6327 hashes. No guessed project-relative alias was added.

Every concrete producer path was resolved with the corrected base: fixture `Tests/Play/Tools/...`→`D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Play/Tools/...`; Grid/Selector `Scripts/...`→that package's Scripts tree; `package.json`→that package root. All **13 existing** base source hashes matched current files again. The fourteenth union path is the exact future producer import itself, whose reviewed external hash remains6bb; no claim is made that an unimported file already exists or compiled. Producer README039626/base4c4ab, reused ca85 reader and746d driver remain unchanged. Corrected README now uses Tests/... and Scripts/... examples and explicitly distinguishes package-relative pins from project-relative manifest aliases.

Root's separate TMP prepare helper `PackageInfo` compile ambiguity occurred before its native import and is owned by its existing author. It is not a Module source defect and was not modified/read as part of this correction. Root must resolve the current project compile context before any actual Module run; the Module pair itself has only this source re-admission, not a compilation/native receipt.

The first execution remains the previously specified bounded Grid8 case: exact one leaf/fresh owned directory, real positive/legal-swap and negative/control outcomes, unique retained4096 native calibration, 120+120 frames, K1, empty zero control, raw/metadata/source-freeze identity and native readback. Then expand to the other admitted fixed workloads only after that path succeeds. Selector prefix-selection protection, directed-positive performance qualification, K100 getter-group meaning, provenance obligations and all CPU/GC/Player/cross-SDK limitations above remain unchanged. No compiler, Unity, process, import or measurement was run by this reviewer; only this report was appended.

## Producer NUnit compatibility correction — narrow source re-admission

**Corrected producer f125 + unchanged reader626 SOURCE ADMITTED for guarded root replacement/compilation and one fresh Grid8 R2 capture/readback first.** No remaining concrete blocker was found in this source delta. Actual R1 below is retained as a temporary-helper compile abort, not a product failure, test red/pass or performance result. The corrected revision has not been compiled/run by this reviewer and no Module native metric is admitted yet.

New producer SHA256 `f1257e0060a7c909fb771564e4b73bd935687422f21b805eed86f20dc3f90ca3` (44636 bytes); README `981e4216cfce1f0db1029ccf24da77ad3ad452f32b47c61e3d2f10ddc545db66`; base metadata `b53fe7b4030528cbbab69c14f43198eba85bb72358760bd61e0ade37f72e9b8a`. The entire C# change is deletion of the single24-byte line:

```diff
-    [NonParallelizable]
```

Applying only that deletion in memory to the archived original recreated the exact new bytes. Archived producer6bbdd578..., README039626... and base4c4abddd... independently retain their original hashes under `review-history/lib03-producer-before-nunit-attribute-r1`. Reader remains exact626a653c.... All13 existing current source hashes, markers, six case metadata/workload labels, K, frame counts, source-pin path convention, directory/ownership policy and calibration payload compare unchanged. Updated output hashes/lengths and the new correction-authority reference match actual files. Runtime self-fixture hash remains dynamically recorded, so it will report f125 after guarded replacement rather than mislabel the compiled cohort as6bb.

### Actual R1 failure stage

Independently read library-hotpath-u63-grid8-r1 (machine-local `D:/Temp/Neoxider-LibraryClosure-20261102/runs/library-hotpath-u63-grid8-r1/launch.json`, not committed), complete editor log, compile-abort receipt and public prefs. PID37072 exited **1** on CS0246 at producer line21: NonParallelizableAttribute/NonParallelizable could not be found by the installed Unity NUnit context. Log SHA256 `6d2f41cfceb3dd8c42ccdde93bf2ef7f3a4de9d999ffc3c0a35c85ebc2ce84ae`; abort receipt (machine-local `D:/Temp/Neoxider-LibraryClosure-20261102/runs/library-hotpath-u63-grid8-r1/compile-abort-receipt.json`, not committed) SHA256 `83ce5db05d7d95e641ff397f2ccbb5f90a497454f4446b2bb3a93f0fa2162452` records COMPILE_ABORT_BEFORE_TESTS /0executed /NOT_MEASURED. Actual results.xml is absent; no Grid oracle or capture assertion reached execution. Prefs stayed51→51 without added/changed names or private-value publication. This is separate from the consumer TMP scratch helper's CS0104 and subsequent successful resource preparation.

Removing an unavailable, never-executed attribute introduces no replacement attribute, NUnit upgrade, dependency or weaker assertion. Serial ownership remains an explicit **execution-context condition**: root runs one owned job and one verified exact leaf per fresh directory, retaining borrowed Profiler/singleton refusal and all current fixtures' checks. This source admission is not permission to run the whole class concurrently or reuse a shared directory; Selector prefix-selection protection still applies. No raw CLI filter-equals-fullname requirement was added that would reject root's verified anchored selection.

At this check Main still contained exact6bb and the original owned meta; the external author had not replaced it. Root may perform the guarded6bb→f125 test-only replacement while preserving that meta, freeze actual inputs and run a new exact Grid8 attempt. K1/120+120/calibration/empty control, canonical-positive/no-swap oracle, complete no-mutation checks and CSV/native reader validation remain mandatory. Product optimization and runtime/SDK/performance claims remain outside this correction. Only this report was appended; no candidate/Main/config/host/process/compiler/Unity action was performed by this reviewer.

## Actual first Grid8 end-to-end path — scoped evidence accepted

**Grid8 capture/outcomes/native readback ACCEPTED for this directed warmed Editor workload.** This is the first actual Module measurement path, not source-only admission. Product costs below are observations, not a newly imposed allocation budget, confirmed product defect, optimization pass or Player/FPS claim. Other Grid/Selector cases are not reviewed by this appendix.

Capture run (machine-local `D:/Temp/Neoxider-LibraryClosure-20261102/runs/library-hotpath-u63-grid8-r2/results.xml`, not committed): PID9908, exact single `Neo.Tests.Play.Audit20261102LibraryHotpathMeasurementTests.Capture_Grid_8`, **1 Passed/0skip/exit0**. XML SHA256 `de6a0fc24d855cd999a99f5496858c1b44b40970722e80485ce4fd7aefab0faf`. Readback run (machine-local `D:/Temp/Neoxider-LibraryClosure-20261102/runs/library-hotpath-u63-grid8-readback-r2/launch.json`, not committed): PID35060, native exit0 and EXPORTED_SCOPED_METRICS. Both full logs/launches and capture summary/index were independently checked. The capture log retains two Mirror.Weaver DOTween-resolution warnings and a startup licensing error; no clean-zero-warning claim is made. The successful reader log's Finish(System.Exception) stack signature is not a thrown exception result.

### Raw/companion/source chain

Under `profiles/lib03-grid8-u63-capture-r2`, completed raw is **8339297 bytes**, SHA256 `f1cc2f00aeca38d7e0369692d5b330f7ddb9092b92e084cbaee8edf83df47a39`. Actual raw/metadata/counters/CSV hashes were read independently and matched all declared paths, runIds and native before/after identities:

| Artifact | SHA256 |
|---|---|
| metadata JSON | `c7c6257c13e986304b3f79a21c46e3958dbf03c423a969c33dc8b807194e1b6b` |
| retained counters JSON | `c94d08a58beb27ce7c3f4427fee6769861c0b49f34b7e67deca3dd6ad0b443fe` |
| native-readback-r2.csv (machine-local `D:/Temp/Neoxider-LibraryClosure-20261102/profiles/lib03-grid8-u63-capture-r2/native-readback-r2.csv`, not committed) | `a876eb2b3af4c0668470f52c84109aa1b517dd9c80034219b52ca8d38bb65ade` |
| native readback receipt (machine-local `D:/Temp/Neoxider-LibraryClosure-20261102/profiles/lib03-grid8-u63-capture-r2/native-readback-r2.csv.readback.json`, not committed) | `23ce271c39df5340d1f0df6fac3a208017b3fdab6a4ad588318545dac28c3870` |

Actual producer is f125, reader626, reused ca85/746d and native API assembly30019c1f...; package11.0.1, Unity6000.3.14f1, kind match3/K1 and exact positive/control labels agree. All **11** package-relative metadata pins match `Assets/Neoxider/` entries in the captured freeze, readback freeze and current actual files. Their relative spelling is retained in the native receipt. Frozen capture manifest SHA `41dfec8bd96f492bea9ab5ede33a9735190fa4fee0fea38099dcba7b7759198c` matches caller/native before/after values. Source metadata remains honestly nativeReadback=PENDING/nativeCalibrationBytes−1 from its capture-time creation; later measured values are in the separate immutable readback artifact, not retroactively inserted into the producer companion.

### Outcomes, calibration and attribution

All 240 metadata/counter/native CSV/receipt rows were joined and checked: positive120 native frames3..122, control120 frames131..250, ordinals0..119, one operation/frame and constant Time.frameCount−profilerFrame offset **22** across both phases. Both phases are contiguous; transition gap is9. Raw loaded bounds0..251, one completion event. Calibration frame1 maps to metadata Time.frameCount23 and has **one native GC.Alloc /4128 bytes** for retained4096 payload; it is outside selected frames.

There are exactly **721 observed named scopes**: calibration1, positive stamps120, control stamps120, empty240 and operations240. All have the same nonzero native Main Thread ID4712/name/group. Every selected frame has three disjoint peer named subtrees, with native parent indices/names retained by the reviewed recursive traversal. All EmptyOperation and stamp GC calls/bytes are0; unselected operation/empty counts are0. Each K1 operation's inclusive time/count/bytes equals its CSV row. Selected named union is **46560 calls /4751040 bytes**, exactly the sum of disjoint operation totals; inclusive parents are not added a second time. No new native raw load/parser was executed by this reviewer: the independent check used the exact source-reviewed native receipt/ancestry and raw identity plus complete exported data.

All retained outcomes agree: positive found=true with `(1,1,0)`→`(1,0,0)`, control found=false with default endpoints. Every selected-frame stateUnchanged flag is true. Initial 64-cell records for both boards match unique content plus the canonical3×2 patch, positions/type/flags, disabled(7,7), blocked(6,7), occupied(7,6), directed-down rule and minMatch3. Source-side independent legal-edge oracle recorded1 positive/0control, no existing match, and its finally restoration. The actual Passed fixture reaches the unchanged full board/array/config/cell/payload identity checks and final serialized service/config comparisons; payload references are an in-process assertion, not invented JSON object identities.

### Measured cost — per one direct call

| Scope/path | CPU p50 / p95 / p99 / max, ms | GC calls each frame | GC bytes each frame |
|---|---:|---:|---:|
| Directed canonical legal-swap positive | 0.065850 /0.101000 /0.120100 /0.174600 | 53 | 5312 |
| Directed no-legal-swap control | 0.356550 /0.521200 /0.544500 /0.631800 | 335 | 34280 |

Quantiles were recomputed from every row, with median/nearest-rank definitions; call/byte values are constant across all120 frames of each phase. Main-thread frame p50 is separately1.182750/1.474450 ms; all-thread GC is separately6696/35664 bytes. Neither whole-frame figures nor an overhead subtraction replaces measured operation cost. The positive's early canonical directed search is not a worst-case/four-direction board workload, and the no-swap control has different search work; their difference is not a benchmark of an optimization or proof of a defect.

Both capture and readback freezes contain **5955** files, each matched independently to current bytes/SHA. Protected12 and both public prefs51→51 comparisons also match, without private-value reads or publication. This successful main-project run removes the previous temporary-producer compile block in its actual tested scope. The prior CS0246 abort remains retained with0executed; the separate consumer TMP step is not conflated with it.

This review admits one real bounded baseline datapoint and its measurement path. Ranking/scaling decisions need the remaining dimensions/Selector workload results and their correct units/control semantics; no product code was optimized or changed here. No all69-module performance, universal zero-GC, arbitrary threshold, cross-SDK, visual, Player or FPS acceptance is asserted. Only this report was appended; no Unity/compiler/process/native-load/source/config action was performed by this reviewer.

## Actual remaining five baselines — full six-case evidence accepted

**Remaining Grid16/32 and Selector1/100/1000 evidence ACCEPTED in the fixed warmed workload scope.** The previously accepted Grid8 chain above is reused without rerunning or reloading it. Together the six baseline cases have **6 actual single-leaf capture PASS +6 native EXPORTED_SCOPED_METRICS/exit0**, 1440 selected frames and the specified outcomes/calibration/empty controls. This accepts baseline measurements; no optimization, performance threshold or library-wide closure follows.

Independent new verification covered all **1200 remaining rows**, complete XML/log/launch/index/summary/native receipts, raw/companion/CSV hashes, retained outcomes/settings, named ancestry/disjoint sums and provenance. Queue5 (machine-local `D:/Temp/Neoxider-LibraryClosure-20261102/profiles/library-hotpaths-u63-remaining-queue-r1.json`, not committed) is COMPLETE with five unique expected case pairs, no current/error; SHA256 `900ddc201329b64dbbb3bdad62123fec88f063dd4f705e360479d0e859ab7ae7`. Six-case root summary (machine-local `D:/Temp/Neoxider-LibraryClosure-20261102/profiles/library-hotpaths-baseline-u63-r1.json`, not committed) SHA256 `9a526b3ce8b23794e739c8bdb5eff28386259e428966ad5bd11641907b78b7c5`; all its remaining p50/p95/p99/max values were independently reproduced. Its Grid8 identities/statistics match the already accepted chain. The [canonical measured table](MeasuredGridSelectorBaseline.md), reviewed SHA `b8f52e23455a11e4d6118604525cebe04d1d685f4afe2110d85711de0a5b1793`, has the same units/qualifications.

| New case | Exact one-PASS XML SHA256 | Native readback receipt SHA256 |
|---|---|---|
| Grid16 | `ac5531fa725adb0bb36ef5e47be8c474fb2a306d7d8f6992e6eefd8486c00cb3` | `0896dee39818522e06e6442626a52d9436b9bd901247312d37b4d9fd44840255` |
| Grid32 | `5a391bb3dc8a40e63d3bd2e4033d87022777bf4eab777898e931f193c7eca363` | `0ccd2d542eab9b05290f7e4a7dc16cea5794d550419ae085f9a2e6e333288063` |
| Selector1 | `f4012efc4e0e6c2c3a32044ee028941c32521324cf6ffbf2cb87efbac1796eda` | `a0458efa41ae447c70399e3bb748a84972fa823146378378557e2f24935cbdcb` |
| Selector100 | `884fa1d21e4963c859218b7d9b76b8cdb0108d31bb7de351d437b6f73fb3f0d8` | `1f21bf65d4d00470f12b5a9866e0a634e6da691d95c0e042aeffeab1537907bf` |
| Selector1000 | `f3eff4fe7f39675670a812552606d2ec56e177187594ed202d7f53398967fb02` | `4307930b7c0424243b5f63aca0075a74fc145e3997f185545f6bc615b92df768` |

### Exact selection, state and native validity

All five launches use anchored literal regex matching only their intended one of the six producer methods; actual XML independently contains exactly that one Passed leaf, zero skips, natural exit0. Thus Selector_1 did not accidentally execute _100/_1000. Each used a separate fresh directory. Every readback's sole native JSON log record equals its saved receipt; expected/before/after raw, metadata and frozen-manifest SHA values match actual files, as do counters and CSV. No prefix-filter, metadata, byte-parent or missing-data issue was waived.

For each case: 240 unique native frames, 120 contiguous positive then120 control, K1 for Grid/K100 for Selector, constant offset22 across both phases and gap9. Loaded bounds are0..251; calibration is unique, outside selected frames, one actual event/4128 bytes. Empty/stamp GC is0, unselected operations/empty scopes0. Grid has721 named scopes; each Selector has24481. All named scopes share their capture's persistent nonzero Main Thread identity; per-frame indices and native raw parent records agree with the selected view. All operations/empty/stamp subtrees are disjoint peers. Their K-scope time/call/byte sums equal the native rows and CSV; selected named unions equal disjoint operation totals, without ancestor double-counting. The reviewed native traversal supplies per-sample metadata; this reviewer did not start another raw load or binary parser.

Grid16/32 initial arrays contain256/1024 cells with exact unique content/canonical3×2 patch, type/flags, disabled/blocked/occupied states and down-only rule. All240 outcomes per case are the canonical positive swap or false/default endpoints, with stateUnchanged=true. Source-side legal-edge counts remain1/0 and complete board/cell/payload/config no-mutation assertions Passed. Positive is the directed canonical search, not random/four-direction or worst-case positive; control exhausts a no-legal-swap board.

Each Selector contains24000 saved three-getter group outcomes. All match active count/boundary values and equivalent pure-model control, with stateUnchanged=true and notifications0. Metadata records correct nonempty E/U and isolated configuration: N1 E1/U1/included0/active1/true,true; N100 E25/U26/included75/active51/false,false; N1000 E250/U251/included750/active501/false,false. Fill/unique true, virtual Count/no Items and save/network/activation/child-update/reset/loop/allow-empty flags remain as admitted. The setup isolation probe and after-stop state/JSON checks are reached by the Passed exact source, rather than assumed from metric markers alone.

### Measured baseline and units

| Case | K | Positive p50 ms | Positive bytes / calls | Control p50 ms | Control bytes / calls |
|---|---:|---:|---:|---:|---:|
| Grid8, reused accepted | 1 | 0.065850 | 5312 /53 | 0.356550 | 34280 /335 |
| Grid16 | 1 | 0.335450 | 25728 /101 | 5.224650 | 378792 /1455 |
| Grid32 | 1 | 2.491650 | 174080 /197 | 80.709553 | 5375144 /5999 |
| Selector1 | 100 | 0.522650 | 129600 /2400 | 0.008100 | 0 /0 |
| Selector100 | 100 | 1.036950 | 456000 /2400 | 0.007800 | 0 /0 |
| Selector1000 | 100 | 4.795700 | 4238400 /2400 | 0.008100 | 0 /0 |

Grid rows are **per ONE TryFindValidSwap** call. Their costs are not automatically a per-Update tax. Selector totals are **per100 groups of three getters =300 getter calls per selected frame**, not per one getter. Positive is the component logical branch with E/U; control is an independently prepared SelectorModel snapshot performing the corresponding three pure reads, not an equivalent component Update/Items/native activation/network path. Every reported allocation count/byte value is constant across that phase's120 frames. Control-zero refers only to those pure operation subtrees; all-thread GC remains nonzero context.

Tails are retained: Grid32 no-swap CPU p95/p99/max90.217293/113.570305/115.859299 ms per query; Selector1000 positive p95/p99/max7.392800/37.553700/39.146200 ms per100 groups. Concurrent load/power was not controlled; no timing threshold, frame-rate promise or exact cause of an individual tail is inferred. Main-frame/all-thread metrics are separately exported and are not subtracted or attributed wholesale to product.

### Source localization and remaining boundaries

The unchanged pinned source has concrete repeated work consistent with these measured paths: Match3 TryFindValidSwap loops eligible neighbor pairs and WouldSwapCreateMatch creates a complete BoardSnapshot for each attempt. Selector GetCurrentBounds and virtual logical CountActive call SyncModelFromFields/Configure, including defensive E/U copies; the pure model control performs no such reconfiguration. This localizes candidates for bounded investigation. It does not assign a byte subtotal to one allocation site, prove every cost is unintended, or authorize a changed board/candidate order, state visibility, enumerable isolation, save/event/same-index/network contract.

All ten remaining capture/readback source freezes contain5955 files and match current bytes/SHA; 11 Grid or7 Selector metadata package pins are present under Assets/Neoxider in both frozen maps and retain their exact relative receipt spelling. Producer f125/reader626 and measured product sources remain unchanged. All ten public prefs comparisons show51→51, no added/changed names/private publication; protected12 matched independently. Remaining capture logs have no extra Weaver warnings, while the earlier two Grid8 Weaver warnings remain preserved above. Startup licensing messages are retained; no blanket error-free-log claim is invented.

No new product implementation or after-measurement was inspected/applied here. The separately assigned Grid/Selector work is PLAN ONLY until a fresh source-final implementation and its review/execution gates arrive; an old worker Done record is not a new result. These six baselines admit measured cost ranking/scaling in this Editor/profiler workload, not all69-module performance PASS, zero-GC for the components, Player/FPS/AOT/other SDK acceptance or global closure. Only this report was appended; no source/config/process/Unity/compiler/native-load action was performed by this reviewer.

## Independent after-producer IDENTITY_SOURCE gate — 2026-11-02

**IDENTITY_SOURCE ACCEPTED for Root-controlled test-only producer application between native jobs.** No blocker was found in the entire identity diff or its source receipt. This admits the producer source identity revision only; product/native after functional or performance results are not accepted by this appendix. Root retains its separate functional and execution gates.

The reviewed external `fixtures/Audit20261102LibraryHotpathMeasurementTests-after-sourcepins.cs` is 44,636 bytes, SHA256 `8b7fc1db1c4dc33824f1f6c64c9eb76a1339f29a3405002ab08c0e762352f5eb`. Current Main producer independently remains 44,636 bytes / `f1257e0060a7c909fb771564e4b73bd935687422f21b805eed86f20dc3f90ca3` at this review. Full `fixtures/library-hotpath-after-sourcepins.diff` (2,085 bytes / `5dfc460f97e333a1ea089fa52dcbb9a9b94e3cfd1f01f7c481a4b1fedc6149fe`) and `fixtures/library-hotpath-after-sourcepins-review.json` (1,017 bytes / `3f1df994b27b75bb5ea1dbf1058e1b8a9f3d57f7f844184085196b84e08b7965`) were read. These are under `D:/Temp/Neoxider-LibraryClosure-20261102`.

| Original expected product SHA256 | Accepted replacement / actual Main SHA256 | Package-relative source |
| --- | --- | --- |
| `c107c61b82c0869a1f2e1848afd035e7a2d6a72f9ed983451108738aabf1b370` | `36810f5c4b13a9f961767382cee658e55e52a598318a58f021eb8b440cbd60fc` | `Scripts/GridSystem/Match3/Match3BoardService.cs` |
| `8117a7719e9f3808a7c48e0afc1f2bfeeed4336c106957d0d133b618a6fc3d15` | `0c3280f3c4f881c31265c6e14f3c8656a6ded8c83f7335d9864f0511aea9a737` | `Scripts/Tools/View/Selector.cs` |
| `45928c9c1ab01181de097ce5ca72d3614eabf6015d80c7a051a6fd87619d116e` | `f8999334d87a714daf470141dfca95c6024ed6ac49c5d9f75199cffe7505aabb` | `Scripts/Tools/View/SelectorModel.cs` |

Each old hash occurs exactly once before and zero times after; each new hash occurs zero times before and exactly once after. Replacing just these three new literals with their old literals restores the current Main f125 source **byte for byte**. Thus all other producer bytes, including source-pin paths and remaining expectations, are unchanged: no flags, N, K, frame count, case/phase labels, geometry/setup, action body, oracle, assertion, outcome, input, control, metadata schema, ownership or marker revision is hidden in this change. All three replacement hashes independently matched actual Main product bytes and the accepted product identities recorded by NativeGridSearchImplementationReview (report SHA `297b1a343ebca870a66df17a03311ce55b1590045814b8da436f6d84572b5c73`) and NativeSelectorGetterImplementationReview (report SHA `050d4890da1a62c4ef21c5c68f031186134249f2e995324847d2ca0e0c2e7008`). This comparison does not reclassify their source admission as native execution proof.

Main reader independently remains SHA `626a653c780dfc339f11c5c12abc9252439060e3aa6af092012487de3cf7f1a1`; its admission and protocol are unchanged. Historical f125/raw/capture/readback evidence remains the before cohort. Root may apply the reviewed identity revision only after its current native job has ended and required functional checks have passed, recording the application/source-freeze/fixture identity separately. Retain all six exact Grid/Selector positive/control workloads, K1/K100, N/dimensions, 120+120 frames, empty/calibration controls and native provenance checks. New after raw and readbacks must satisfy the full existing reader protocol; this source-only gate supplies no after CPU/GC number, threshold, speedup, Player result or global closure.

Only this appendix was written. No candidate, Main/product/capture/reader, configuration or preference change, Unity/compiler/process action, raw native load or new agent occurred in this review.
