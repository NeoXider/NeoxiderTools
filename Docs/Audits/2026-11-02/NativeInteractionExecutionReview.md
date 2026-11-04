# Native physics and interaction execution review — 2026-11-02

Independent bounded QA by native_interaction_execution_review_1102. This report reviews the root operator's actual Unity evidence and exact frozen source. The reviewer did not launch Unity, a compiler, tests, a Player, CLI workers, or process controls; the only reviewer write is this report. Root retains operation and source-application ownership.

## Verdict at initial review

- **ACCEPTED, bounded:** all eight current native phase cases passed on Windows Editor Unity 6000.3.14f1, offline and optional-network solo only. Both formerly failing 3D collision cases now admit a plain MonoBehaviour native stay before asserting relay stay.
- **CONFIRMED DEFECT / correction pending:** both same-frame filtered 2D global-trigger-policy directions return the cached A result after native B has changed. The explicit 3D Collide control passed.
- **Not yet accepted:** a correction candidate and post-correction native green. Source-final notification is required before reading the author's external candidate. No current policy-fix green is inferred.

## Evidence and exact run

Primary run: [physics-policy-u63-current-r1](D:/Temp/Neoxider-LibraryClosure-20261102/runs/physics-policy-u63-current-r1/results.xml), with [launch](D:/Temp/Neoxider-LibraryClosure-20261102/runs/physics-policy-u63-current-r1/launch.json), [editor log](D:/Temp/Neoxider-LibraryClosure-20261102/runs/physics-policy-u63-current-r1/editor.log), [summary](D:/Temp/Neoxider-LibraryClosure-20261102/runs/physics-policy-u63-current-r1/summary.json), [leaf index](D:/Temp/Neoxider-LibraryClosure-20261102/runs/physics-policy-u63-current-r1/testcase-index.json), and [preference comparison](D:/Temp/Neoxider-LibraryClosure-20261102/runs/physics-policy-u63-current-r1/editorprefs-comparison.json). Full XML and log were inspected; no PRIVATE preference/configuration values were opened or copied.

Launch was 2026-11-02 07:52:52.791 UTC, native PID 18288, completion 07:53:17.859 UTC, PlayMode, Neo.Tests.Play, selected phase/probe fixtures, D3D11 graphics batch. Native exit **2** matches XML **11 executed = 9 Passed / 2 Failed / 0 skipped**. Script compilation completed with Tundra success; the log records natural test-run exit2, rather than bootstrap abort. The 6 GiB launch guard was recorded with 56,853,860,352 bytes commit headroom.

The reviewer recomputed raw SHA-256/byte counts and matched summary receipts:

| Input | Bytes | SHA-256 |
|---|---:|---|
| launch.json | 2,017 | d636314108b37c7ccf3c15b487b5a0a671d8e705f7a1fc076f5eae17437100a9 |
| results.xml | 18,607 | debcc46ee11c9929eb0ae18397a9f3681007b3026b317e34bca57bed70baa9e2 |
| editor.log | 47,566 | 68a93ddb2ff672d116e7cd0ac980d7c3ef2382b93ea29178023bb7a69d44d82a |

All 11 XML leaf names/results agree exactly and in order with testcase-index.json. Frozen manifest: 5,931 identities, 1,528 copied sources, HEAD 3e795e8c6f4d1c89e3fac5f83a79d7c02d988831. The summary reports no changed frozen paths and no protected differences. Preference comparison reports 51 before/51 after, no added names, no changed originals, no root registry modification; private values are unpublished. Selected frozen C# bytes were independently hashed against the manifest and compared with project bytes during review; all matched.

## Native phase acceptance

All combinations of 2D/3D × trigger/collision × Offline/OptionalSolo passed. OptionalSolo sets isNetworked=true while SetUp/TearDown explicitly require NeoNetworkState.IsNetworkActive=false; no server, host, client, remote peer, or transport is started.

Exact sources:

| Source | SHA-256 |
|---|---|
| current phase fixture | c70f19a8280077d40d2fb05160ef32a1ed1ed5d8726c15723841d682f4cc3fea |
| plain 3D collision control | 15344361f690467239b81df5d3ca4fcfd78d7bde038073195001df67d8f65311 |
| previous failing phase fixture | 3ac912cff9012f17577b5de5d15b5ce327a24ae5a5dbf5f0e5a5e791f22116da |
| PhysicsEvents2D runtime | 93e0d4036b376fc4fb273b595bd3653a583da4f6b2adc2e5bcdf2cec9d7474d5 |
| PhysicsEvents3D runtime | e3f3852b0a9512e4f393219be542b28c0e29004953e74c20f65b78c1da9c488f |

The full fixture diff was checked against frozen play-main-u63-baseline-r1 inputs. Run2D and AssertPhases are byte-identical. 3D trigger mechanics remain unchanged. Only 3D collision admission gains a live Y contact axis (rotation/X/Z frozen), inward velocity before collision steps, raw MonoBehaviour OnCollisionEnter/Stay/Exit observations, awake checks, and diagnostics. The step budget is still 12, with 0.02 s local simulations. No SendMessage or manual collision-dispatch call admits native coverage.

For both native 3D collision leaves, raw XML reports:

| Stage | native enter/stay/exit | Inspector | typed C# |
|---|---|---|---|
| enter | 1/0/0 | 1/0/0 | 1/0/0 |
| native-control stay | 1/1/0 | 1/1/0 | 1/1/0 |
| exit and final admission | 1/1/1 | 1/1/1 | 1/1/1 |

At the stay stage the actor is awake before/after simulation, dynamic, sleepThreshold=0, collision constraints=122. The coroutine first waits for raw native Stay; the subsequent assertion requires both relay surfaces already to contain Stay. Final assertions additionally require raw cardinality/order/collider/object correspondence and positive native non-exit contact counts.

The retained AssertPhases still requires at least three events; exactly one Enter and one Exit; at least one Stay; Enter first/Exit last; equal UnityEvent/C# counts and phase sequence; exact expected actor/collider identity; nonnull payload and shared payload identity between surfaces; positive non-exit collision contact count and matching typed contact counts. Separated bodies must settle without repeat exit/stay. The fixture correction strengthens admission without weakening these contracts.

Historical [play-main-u63-baseline-r1 XML](D:/Temp/Neoxider-LibraryClosure-20261102/runs/play-main-u63-baseline-r1/results.xml) is retained: the previous fixture produced six passes plus two 3D collision failures specifically at bounded 3D stay observation. Those failures lacked the new raw-control admission and are not independently established runtime relay defects. Both runtime relays have identical old/current source hashes. ProjectSettings DynamicsManager, Physics2DSettings and TimeManager identities match old/current; every common Packages/ProjectSettings manifest identity compared also matched. No physics-settings collateral change was observed.

## Actual filtered-policy defect

Probe SHA-256: **6b596850535931da23da7bcfc39ad47f6973a423ff5084af6baa5229af9af726**. Runtime query source SHA-256: **bb19f00bff9da709c21e396b204f57828cbafa4495d41410d4048265d07d5885**.

The complete probe was read. Before A, it establishes both trigger/solid native hits with global=true/filter.useTriggers=true and solid-only with filter.useTriggers=false. Each native reference uses both an eight-slot array and List overload: count is below array capacity, the two overload counts agree, solid remains positive, all hits belong to the two owned colliders, collider identities agree, and distance/point parity is checked. No empty-result premise can pass admission.

A and B each use native references from the actual running SDK. The fixture does not assert that an explicit ContactFilter2D overrides the global setting: either native outcome is accepted if Shared matches. ReleaseBuffers and ResetCounters occur only before A and after cleanup; no reset, yield, invalidation, transform mutation or callback boundary occurs between A/B/B. Every shared request explicitly supplies QueryMode.Shared; FreshQueries and callback-related counters must remain zero, and the real Time.frameCount must stay fixed.

Raw failures occur at AssertMatches(b, expectedB), probe line88 (count assertion line252), after all native A/B controls and same-frame/counter checks:

| Direction | Real frame | Native expected A→B | Shared actual A→B | Epoch before/A/B | afterB native/cache/fresh/invalidations/frame advances/callbacks |
|---|---:|---|---|---|---|
| false→true | 16 | 1→2 | 1→1 | 6/6/6 | 1/1/0/0/0/0 |
| true→false | 19 | 2→1 | 2→2 | 9/9/9 | 1/1/0/0/0/0 |

This is a functional stale-cache defect on this SDK. Existing Key constructor line216 includes global Physics2D.queriesHitTriggers for Intersection2D/Ray2D while omitting FilteredRay2D. Equality and GetHashCode already include GlobalTriggers2D. Filtered queries execute the actual Physics2D.Raycast(ContactFilter2D,array/List) paths affected in the native run, so reusing the existing key boolean for the missing kind is the bounded reuse path. No new backend, cache replacement, forced Fresh mode, global settings change or broad epoch churn is needed.

Both failed leaves terminate at B parity; their filtered unchanged-B reuse assertions have **not executed** in this red run. Those assertions remain required after correction: valid/complete B, exact native parity, unchanged epoch/frame/invalidation/native calls on B→B, and exactly one additional cache hit. An always-fresh workaround cannot satisfy them.

The explicit 3D Collide control passed, native hits remained the owned trigger under global false→true, frame13 and epoch3/3/3/3. A/B/repeatedB counters were native1 with cache0/1/2, Fresh0, invalidations0, frameAdvances0 and callbacks0. This validates the unaffected explicit-3D control and unchanged-B reuse only in its executed scope.

## Remaining acceptance gates

1. Read the same-author external candidate only after source-final notification. Verify exact base/diff and existing-field reuse; public API, serialized settings, execution/fallback/buffering, native counters, callback/frame/lifetime logic, and equality/hash mechanics should remain unchanged apart from the missing kind's captured flag.
2. Root applies the accepted exact candidate with a source receipt and executes current three policy leaves plus all existing 112 interaction regressions. Required old selection is 19 Interaction + 33 Lifecycle + 12 PhysicsEvents dispatch + 48 SharedQuery, preserving their exact test inputs and all 12 functional PlayerLoop matrix cases. Baseline leaf/index counts and old/current fixture identities were independently confirmed; old passes alone do not accept the new runtime candidate.
3. In actual green, require both policy directions to reach B→B reuse, not merely B parity. Check native controls, native/cache counts, source manifest, raw XML/index/exit, protected paths and nonprivate preference comparison.
4. Unity 6.6, Player/other platforms, active-network peers/transport, OS input, profiler CPU/GC/calibration/external-EventSystem attribution and device/performance certification remain unexecuted by this scope. Physics8 is local/optional-solo native event proof on 6000.3.14f1, not an all-SDK or remote-network result.

Initial report recorded 2026-11-02T08:01:56.160Z.

## Source-final policy candidate review

Reviewed after root's explicit source-final DONE/exit0 notification. **ACCEPTED FOR ROOT APPLICATION, source scope only; no blocker found.** This supersedes the initial candidate-pending status without converting the red run into green.

External payload: [candidate source](D:/Temp/Neoxider-LibraryClosure-20261102/candidates/lib-1102-02-policy/Assets/Neoxider/Scripts/Tools/InteractableObject/InteractionPhysicsQueries.cs), [author review](D:/Temp/Neoxider-LibraryClosure-20261102/candidates/lib-1102-02-policy/REVIEW.md), [base receipt](D:/Temp/Neoxider-LibraryClosure-20261102/candidates/lib-1102-02-policy/base-inputs.json). Recomputed identities match root notification:

| Payload | SHA-256 |
|---|---|
| InteractionPhysicsQueries.cs, 27,476 bytes | 91aa73bfb100dbe6b2581bb4434e716cac6f53d0cac061f87f28f487193e2e1b |
| REVIEW.md | f3b97d7edc98fafc763773c1748637e3c7a6c83f09be33bd47dba9c874c802e1 |
| base-inputs.json | e480bfba102e96f00b813d00f2a74a622907b36bd6a5e07736a30aee427c6ab2 |

The entire narrow diff and author report were read. The candidate is exactly the frozen bb19f00... source with one line replacement at Key constructor line216:

`GlobalTriggers2D = (kind == Kind.Intersection2D || kind == Kind.Ray2D || kind == Kind.FilteredRay2D) &&`

All remaining bytes equal the reviewed base. UTF-8 without BOM and LF are preserved. All 11 author-listed project input byte counts/hashes match actual project bytes and the root frozen manifest; all six listed immutable-evidence byte counts/hashes match their actual files. The reviewer independently checked those values rather than trusting metadata booleans.

This directly reuses GlobalTriggers2D and its existing equality/hash participation. It preserves public APIs, key equality/hash code, native overloads and arguments, explicit-filter contents, buffer allocation/fallbacks, frame/callback invalidation, epoch lifetime and diagnostics. It neither changes global settings nor forces Fresh/invalidation. Source reasoning: A→B selects the second policy key and may add one native call; unchanged B→B identifies the same entry and can reuse it. Actual counter/parity proof remains the root green gate.

The existing SharedQueries.md contract lines93–99 was checked and accurately qualifies native policy and the unchanged explicit filter; its SHA-256 a634e35ba969f97b5dda47798eaa8fe7d4c9f2a7834c227bb0a11789be77e3e3 matches the frozen/current receipt. A documentation candidate is unnecessary. The author report correctly retains both functional red failures, acknowledges unreached filtered B→B, labels candidate compile/tests NotRun, and does not treat Physics8 as candidate validation.

**Next owner: root.** Apply only exact source SHA91aa73... after guarding current base bb19f00..., then obtain current policy3 + relevant old112 native green (19/33/12/48), raw receipts, and filtered B→B reuse. Retain Physics8's prior qualified acceptance unless an affected change creates a reason to rerun; this single key predicate does not change relay/fixture/native-contact logic. Unity6.6/Player/remote peers/OS input/quantitative performance remain open. No Unity, tests, compiler, process controls or product edits were performed by this reviewer.

Source-final review recorded 2026-11-02T08:05:48.600Z.

## Actual post-application green — final bounded acceptance

**ACCEPTED: the reproduced filtered 2D stale-policy defect is corrected in the tested Unity 6000.3.14f1 Windows Editor context. No evidence blocker remains for this scoped correction.** This actual green supersedes the earlier correction-pending / NotRun statuses. It does not close the unexecuted SDK, Player, remote-network, input or performance scopes below.

Root's [application receipt](D:/Temp/Neoxider-LibraryClosure-20261102/candidates/lib-1102-02-policy/project-application-receipt.json) records old bb19f00... → exact accepted 91aa73... at 2026-11-02 08:07:00 UTC (receipt SHA-256 61251523d7f936e7de8374749c546d59e53643bcce8b7b413a573eb116df017e). The green-frozen runtime bytes independently equal the external accepted candidate, and current project selected C# bytes match their green-frozen copies.

Actual [interaction-policy-u63-green-r1 XML](D:/Temp/Neoxider-LibraryClosure-20261102/runs/interaction-policy-u63-green-r1/results.xml), [launch](D:/Temp/Neoxider-LibraryClosure-20261102/runs/interaction-policy-u63-green-r1/launch.json), [log](D:/Temp/Neoxider-LibraryClosure-20261102/runs/interaction-policy-u63-green-r1/editor.log), [summary](D:/Temp/Neoxider-LibraryClosure-20261102/runs/interaction-policy-u63-green-r1/summary.json), [leaf index](D:/Temp/Neoxider-LibraryClosure-20261102/runs/interaction-policy-u63-green-r1/testcase-index.json) and [preference comparison](D:/Temp/Neoxider-LibraryClosure-20261102/runs/interaction-policy-u63-green-r1/editorprefs-comparison.json) were independently checked. PID8460 ran 08:07:06.079–08:07:48.062 UTC, PlayMode Neo.Tests.Play with six named fixture filters, D3D11 and a recorded 6 GiB guard. Native exit **0** agrees with raw **123 executed / 123 Passed / 0 Failed / 0 skipped**. The complete log records actual recompilation of Neo.Tools.InteractableObject and Neo.Tests.Play, Tundra success, Play Mode execution, result saving and natural exit0. Mirror IL postprocessor resolver warnings for DOTween/BouncyCastle are present; this is not represented as a warning-free run or dependency-absence certification. No C# compile error or selected test failure is recorded.

Recomputed raw identities match summary receipts:

| Input | Bytes | SHA-256 |
|---|---:|---|
| launch.json | 2,590 | 9fe7ea2e4e7c5aa8de43bf280d48e3ece1d386eb0fd9f70ddcb57b7df2636f5d |
| results.xml | 125,647 | 945500f9949ad0c506ba0d074061eb3aaae5dc08a8e9a79b839c6d12833eed02 |
| editor.log | 55,396 | 24d9527530cf2f54f2a94752513f9347ae6de0b2b4f3a0d616f728683b0fe52c |

Every XML leaf name/result was checked against the JSON index, including parameterized names after XML entity decoding. The exact observed-name set equals baseline old112 plus the same red-run policy3 and nativephase8; no missing, duplicate-by-substitution, unexpected or skipped leaf was used. Counts: **19 Interaction + 33 Lifecycle + 12 PhysicsEvents dispatch + 48 SharedQuery + 3 policy + 8 native phases = 123**. All 12 functional PlayerLoop matrix cases (2D/3D × camera/keyboard × N=1/100/1000) are actual Passed leaves and their positive/miss raw outputs were inspected. Stable native-query counts remain functional diagnostics: N=1000 still reports 1,000,000 classified hit visits on the positive workload, so no CPU/GC speedup is inferred.

### Decisive A→B→B proof

Both previously red leaves now execute the complete native-reference, B-parity and unchanged-B-reuse assertions on the unchanged probe SHA6b596850...:

| Direction | Real frame | Native A→B | Shared A→B→B | Epoch before/A/B/B | Native calls A/B/B | Cache hits A/B/B |
|---|---:|---|---|---|---|---|
| false→true | 313 | 1→2 | 1→2→2 | 17923/17923/17923/17923 | 1/2/2 | 0/0/1 |
| true→false | 316 | 2→1 | 2→1→1 | 17926/17926/17926/17926 | 1/2/2 | 0/0/1 |

FreshQueries, Invalidations, FrameAdvances and callback counters remain zero throughout each A/B/B sequence. B adds the one native call required by a different policy key; repeated B adds exactly one cache hit with no additional native call, epoch or frame churn. Both results retain complete-array/List admission and collider/distance/point parity through unchanged assertions; no fixture reset or yield intervenes. Thus the green does not admit an always-fresh workaround.

The explicit3D Collide control also remains Passed: frame310, epoch17920 throughout A/B/B, native1/1/1 and cache0/1/2. The same three policy test identities appear in red and green.

### Preserved fixture/runtime boundaries

The reviewer recomputed frozen C# hashes and checked manifest/current byte identity for the runtime query source, both relays, phase/probe/raw-control fixtures and all four old112 fixture sources. The old112 fixture hashes are unchanged from the original baseline through red and green; their assertions and variants were not weakened. PlayerLoop helper, input driver SHA9b57e2... and Neo.Tests.Play.asmdef SHA2061be... also retain exact input identities. The phase fixture c70f19..., raw control153443... and probe6b596850... are unchanged from the reviewed red run. Both 3D collision green leaves again record awake raw native/Inspector/C# enter/stay/exit 1/1/1 and complete strict final assertions. All eight native phase leaves remain Passed with the initial local/optional-solo qualification.

Green manifest has 5,933 identities and 1,529 copied sources. Independently comparing red→green manifests finds exactly three common changed files: the accepted one-line query correction and the two Cards prose documents BoardComponent.md / Examples/Drunkard.md. Added files are the Edit/UI persistence test and its meta; no file was removed. No common Packages/ProjectSettings identity changed, and no selected assertion source changed. Those unrelated Edit/UI additions and their separate results are outside this PlayMode selection; they are neither policy evidence nor policy failures.

Green summary reports changedFrozenPaths=[] and protectedDifferences=[]. Nonprivate preference comparison remains 51 before/51 after, no added/changed originals and no root registry modification. No PRIVATE preference/configuration values were opened. The earlier red XML/index and functional failure remain preserved as historical reproduction evidence.

**Retained limits:** this accepts the one-line policy correction and its relevant functional regressions on Unity6000.3.14f1 Editor only. Unity6.6, standalone/IL2CPP/other Players, real remote-peer transport, physical OS input, CPU/GC profiler calibration and attribution, external-EventSystem performance and device/platform certification remain unexecuted in this scope. No all-library/all246 closure follows from these123 passes. The reviewer did not execute or mutate Unity, tests, compilers, processes, product files or configuration; only this existing report was appended.

Final bounded green review recorded 2026-11-02T08:11:52.247Z.

## First calibrated native performance readbacks — Camera3D N=1 / N=1000

**ACCEPTED AS MEASURED DATA AND BOTTLENECK LOCALIZATION, not a performance-success threshold:** actual completed Camera3D N=1 and N=1000 native readbacks are valid in the recorded Unity6000.3.14f1 Editor context. The dense positive N=1000 workload exposes a substantial target-local classification cost despite one shared native cast. This is not global performance certification, a Player FPS result, or an optimization-green claim. N=1000 output was not opened until root reported its natural completion.

### Provenance and execution

The [capture manifest](D:/Temp/Neoxider-LibraryClosure-20261102/profiles/capture-manifest-u63-r1.json) links twelve completed raw/counter pairs. Actual capture XML has exactly three Passed Camera3D leaves in interaction-profile-u63-smoke-r1 and nine Passed Camera2D/KeyboardLook2D/KeyboardLook3D leaves in interaction-profile-u63-remaining-r1; both native exits0 and no skips. The smoke filter ending Capture_Camera3D_1 actually selected N=1,100,1000, confirmed from raw XML rather than inferred from filter text. Every manifest raw/counter pair occurs in exactly one Passed leaf's output. All twelve counter CSV hashes and raw byte counts were independently checked; each has 240 rows =120 positive +120 stable-miss. CPU/GC acceptance in this review is limited to the two readbacks below; the other ten captures are not converted into calibrated measurements.

Capture smoke PID29180 ran 08:10:15.964–08:13:07.504 UTC on 2026-11-02. The N=1 and N=1000 leaves output the exact raw paths linked below. Both use the same owned, warmed synthetic rig with N collider targets along one camera ray, checkObstacles=false, observer events null, continuous camera query-bearing frames, eight warm frames, 120 selected positive frames, a separated transition/warm interval, and 120 selected stable-miss frames. No per-target reflection/assert/logging occurs inside the selected capture loop; counter export and final assertions run after recording. This is a controlled no-observer workload, not an external EventSystem/user-input scenario.

Primary provenance in the capture log: Windows11 Professional build26200, x64 process/system, physical memory40264MB; Unity6000.3.14f1 d68c3f99a318, Editor Mono, D3D11, RTX3050 Laptop GPU (3962MB) driver32.0.16.2101; batch job-worker-count1, no nographics. CPU model, frequency/power/thermal state, concurrent machine load and full Profiler preference state are not captured here, so cross-machine/Player extrapolation and controlled benchmark comparisons are not admitted. No private preference values were inspected to fill those gaps.

The full capture source, unchanged reader, driver and README were read. Exact frozen/current fingerprints agree:

| Source | SHA-256 |
|---|---|
| Capture workload | a747d304c1757d354133d4c304aacef1e027709ed9a8aae06a13364d1e4ccc7a |
| Existing reader | ca85a496e03cb644c2440ae945818c38cee726911bdeb78e26a9e87c2dafa4ee |
| Native batch adapter | 746d315f9ef9e5a6f5eade27ef23bdb42793e773c62ba379a958cbe8f82829e2 |
| InteractiveObject runtime | bcb49d73786346f38f7b369396e82e16ad146e37e76b2dfb6d5b73807780b105 |
| Query runtime | 91aa73bfb100dbe6b2581bb4434e716cac6f53d0cac061f87f28f487193e2e1b |

The adapter directly reuses the reader; no second parser, fake data or surrogate test XML is introduced. Actual native LoadProfile(raw,false) succeeds in an owned idle batch Editor, one profileLoaded event occurs, and native first/last bounds are0/252 in both receipts. It validates reader fingerprint, completed raw hash before/after under a read-only file guard, fresh outside-project outputs, version/project/owner and idle/no-history preconditions. It does not clear borrowed history or change recording preferences. The compiled native API assembly is UnityEditor.CoreModule SHA30019c1fad50b70962ead76b716ba30b78b4d98e874203d2e434dbe7d0385b11. This is actual successful native API use on the installed version, not only a source/API claim.

Readback N=1: [launch/log](D:/Temp/Neoxider-LibraryClosure-20261102/runs/profile-readback-u63-camera3d-n1-r1/launch.json), PID29748, 08:28:27.439–08:28:54.281 UTC, exit0. Readback N=1000: [launch/log](D:/Temp/Neoxider-LibraryClosure-20261102/runs/profile-readback-u63-camera3d-n1000-r1/launch.json), PID31796, 08:31:19.898–08:31:41.324 UTC, exit0. Both receipts are EXPORTED with no failure and exact expected target count; their process/native-load receipts agree with logs. Readback runs are export operations and have no invented test XML. Nonprivate preference comparisons show51 before/after and no added/changed originals.

### Raw/output identity and calibration

| Camera3D size | Raw bytes | Recomputed raw SHA-256 | CSV SHA-256 |
|---|---:|---|---|
| N=1 | 7,829,734 | c308b6871680f1be0cb82022d5932e42ab0e09ba6e3dd36f8ee9a991beb0f2ba | 3700299d332ddbf81b0d085beea5d54ff1065058d6c71e1b896e634513d16a3b |
| N=1000 | 53,697,753 | 049f1c1218bf693d0ec4d503225bcbc676e53e969ee8f3b532bedcab320cb018 | 4130eb7d0085c08f1a01ead8019fdfc2fc8944718b8f4e193b2957debce03dc2 |

Actual CSVs: [N=1](D:/Temp/Neoxider-LibraryClosure-20261102/profiles/readback-u63-r1/camera3d-n1.csv), [N=1000](D:/Temp/Neoxider-LibraryClosure-20261102/profiles/readback-u63-r1/camera3d-n1000.csv), each with its .batch-readback.json, .calibration.txt and .external-coverage.csv sidecars. Both raw hashes match capture manifest and receipt before/after values. Both counter hashes and all three exported output hashes were independently recomputed and matched receipts.

Both captures contain an explicit AllocationCalibration marker around a real new byte[4096]. Reader GC.Alloc metadata yields **4128 allocated bytes** (actual header/alignment included), not an assumed4096. It requires allocation-size metadata on every counted GC.Alloc sample, rejects invalid sizes, and refuses zero-allocation acceptance without calibration>=4096. Calibration SHA-256 is1a1e88446f2fd6a92a473b6e9512af459fcf926acea22bb0c2beae72b996ba9a in both outputs. This admits the captured managed-allocation channel and zero recorded Automatic-subtree bytes in these selected windows; it does not prove zero native/GPU/all-session allocations.

Both native exports have exact phase labels **positive** and **stable-miss**, 120 rows each, ordinals0..119, profiler frames3–122 and132–251. A lookup for literal miss is an absent-label query, not missing measurement frames. All240 rows contain exactly N AutomaticUpdate invocations. Counter CSV and readback phase/ordinal agree row by row; timeFrameCount minus profilerFrame is constantly22 for N=1 and552 for N=1000 across both phases. Thus neither phase skew nor a dropped/first-only Automatic sample explains the dense result.

### Independently calculated measurements

Times below are **milliseconds per selected frame summed across all N top-level AutomaticUpdate invocations**. Median is the middle-pair average for120 rows; p95 uses nearest rank ceil(.95*120)=114. Inclusive Classify/Native/Callback are contained scopes and are not added again to Automatic totals or to each other as a new total. The reader deduplicates nested Automatic subtrees, walks all expected invocations, and sums their GC descendants; main-thread frame time and all-thread GC are distinct contextual columns.

| N / phase | Frames × Automatic scopes | Automatic median ms | Automatic p95 ms | Classify inclusive median ms | Native inclusive median ms | Main-thread frame median ms | Automatic GC bytes, all frames |
|---|---|---:|---:|---:|---:|---:|---:|
| 1 positive |120×1|0.019300|0.038100|0.002100|0.004550|1.003700|0|
| 1 stable-miss |120×1|0.014650|0.026400|0.000200|0.002900|0.923750|0|
|1000 positive |120×1000|955.598798|1169.387396|937.925648|0.266900|959.499451|0|
|1000 stable-miss |120×1000|3.085700|4.630000|0.287500|0.006750|5.272250|0|

N=1 all-thread GC is1384bytes on every selected frame, with102 observed threads. N=1000 positive all-thread GC ranges5118–10420bytes (median6930), with83–90 observed threads; stable-miss is1796bytes on all120 rows with83 observed threads. Those allocations are outside the measured Automatic subtrees (which record0), but this export does not identify their individual producer. They must not be attributed to library Update, subtracted from a total to invent an optimization, or reported as global zero GC. Fixture/coroutine/input/Editor/profiler costs can remain in whole-frame/all-thread context.

No exact external root names were requested/discovered for these exports. ExternalMainThreadTopLevelMs, AutomaticExternalMainThreadUnionMs, external scopes and coverage are **NOT_MEASURED on all240 rows in each export**. Both external sidecars explicitly retain positive/stable-miss NOT_REQUESTED/NOT_MEASURED (SHAab86bae934dd7e018de2db1f797ad2c0b4ac5e71c1ed9f3bce13ce57e7079dae). No EventSystem or external union timing is accepted.

### Supported hotspot conclusion and correction scope

In all N=1000 positive rows, counters record native1/cache999/classificationPasses1000/classifiedHits1,000,000, with callbacks/LOS/fallback0 and one frame advance. Stable-miss has the same1000 classification passes but0 hit visits. Source at BuildInteractionHits3D lines1349–1373 processes the complete shared hit set separately for each target, including collider lookup, hierarchy exclusion/target ownership checks and InteractionRayHit projection. With1000 consumers sharing1000 hits, this produces **O(consumers × raw hits)** work. Shared native-call reduction does not remove that target-local pass. The source contains corresponding 2D logic, but this readback alone does not measure its cost.

There is no Automatic double sum in this attribution: exactly1000 expected Update scopes are observed per frame, each protected by runtime recursion depth; the reader sums disjoint top-level subtrees. Summed Automatic time is99.36–99.64% of that same raw main-thread frame on positive N=1000 rows. Summed Classify is98.13% of summed Automatic over the120 positive rows, and never exceeds Automatic on an individual row. The approximately955.6ms Automatic median agrees with approximately959.5ms main-frame median, while native median isapproximately0.267ms. These checks support a real classification hotspot in the synthetic dense positive workload, rather than a milliseconds/seconds conversion, first-sample-only error, GC spike, fixture timing or phase-selection artifact. The export does not isolate which individual hierarchy/property operation dominates within Classify; that narrower micro-cause remains unmeasured.

A bounded source correction should return to the same author with these raw/source identities, preserve per-target target/blocker/hierarchy semantics and exact native arguments/cache lifetime, and be verified with unchanged functional parity tests plus paired affected native captures using this calibrated reader. There is no performance-fix candidate or post-fix measurement accepted in this section. Other routes/sizes need their own actual readback; physical input, external EventSystem, Player/IL2CPP, SDK6.6 and broad game/device performance remain unverified.

Performance evidence review recorded 2026-11-02T08:37:46.052Z. No Unity/compiler/test/Player/process-control or product/config write was performed by the reviewer; only this existing report was appended.

## Source QA preparation for a dense-classification candidate

**Preparation only; no performance candidate was read or accepted.** Root's performance-02-correction.md and current query/component source, typed Core helpers, all original112 fixture sources (including the12-case PlayerLoop partial), and the unchanged policy3/nativephase8 contracts were reviewed. Current affected bases remain InteractiveObject bcb49d73786346f38f7b369396e82e16ad146e37e76b2dfb6d5b73807780b105 and InteractionPhysicsQueries91aa73bfb100dbe6b2581bb4434e716cac6f53d0cac061f87f28f487193e2e1b. No partial writer output, active readback output, Unity or compiler was accessed. No new execution/measurement is claimed.

Reuse the existing Entry/buffer/borrowed-hit lifetime and pure InteractionRayHit / InteractionQueryMath contracts. InteractionQueryMath and IInteractiveTarget remain independently usable without a reverse Core dependency on the component/network layer. A fast path must have the same observable result as today's live classification of the same borrowed raw hits; same-frame raw reuse does not imply immutable target ownership, ancestor relationships or trigger properties.

### Concrete compatibility obligations

| Boundary | Current observable contract a candidate must retain |
|---|---|
| Target membership | IsTargetHierarchyCollider lines1424–1453: a live explicit targetCollider3D/2D admits **only that collider**; otherwise a collider on the target transform or any current descendant is target-owned. cachedCollider is merely primary availability/position/dimension input, not the complete membership set. Do not replace this with attachedRigidbody identity, common-root membership, or the first collider only. Compound child hits and multiple colliders on one transform remain distinct hits. 3D availability still takes precedence over2D when both exist. |
| Live exclusions | ShouldIgnoreHitCollider lines1411–1422 never excludes a target collider. For a foreign collider it reads current checkpoint/view hierarchy; IsSameHierarchy lines1478–1487 includes equality, either ancestor direction, and shared rig root only when that root differs from the target root. Camera/target/wall under the same level root must keep a sibling wall blocking. DistanceCheckPoint's setter does not invalidate the raw epoch; new reference/ancestry must be observed. A root-only or one-parent-only cache does not validate arbitrary deeper ancestry changes. Do not clear or repurpose Transform.hasChanged. |
| Liveness / mutable properties | The present projection reads RaycastHit.collider and Unity fake-null each pass: a destroyed foreign collider is skipped. A merely disabled/inactive foreign collider already in a borrowed raw set is **not separately filtered by enabled/active**; its geometry is stale until the caller refreshes/invalidates. Preserve this distinction rather than silently adding a new filter. Existing hitCollider.isTrigger reads are live, including2D post-filtering and foreign-blocker classification; a cached trigger bit needs equivalent validation/fallback. Destroyed explicit target refs become Unity-null; ReferenceEquals alone is not sufficient. |
| Selection / point / ties | Reuse pure TrySelectTarget and TryGetNearestHit semantics: reject negative/nonfinite distance; float.MaxValue remains valid; nearest target uses strict-less updates so equal target distances retain original raw order/point; equal-distance foreign blocker does not reject a target (targetDistance<=blockerDistance). A blocked targetHit still returns the nearest target data. Range LOS uses nearest candidate of either kind and therefore retains original raw-order tie behavior. Sorting/indexing must keep that stable tie key and must not reorder public/raw buffers accidentally. |
| Range / policy | Keep inclusive double-intermediate IsWithinRange, nonpositive unlimited range, obstacle padding0.1 clamped tozero, near-source short circuits, exact native ray/mask/filter/original planar arguments, target-specific LOS and obstacleLayers, component include-trigger options, global2D trigger/start-inside policy and explicit3D policy. Preserve nearest target's actual physics hit point for mouse range/click/debug behavior, rather than substituting bounds center, root position or a convenient first collider. |
| Epoch / callbacks / API | Keep frame/Fresh/invalidation/callback lease expiration; stale borrowed views remain invalid and index access throws. Release cached Unity references with Entry lifecycle and respect generation on any derived data. Callback observers can mutate/throw/reenter; entry metadata must not survive invalidation or be published after reentrant replacement. Preserve mouse/keyboard reset generation and down/up revalidation, Both-mode distinct click/hover rays, disabled-target manual API, availability/camera behavior, serialized fields and public signatures. Do not convert these operations to per-target Fresh casts or move preprocessing outside measured AutomaticUpdate. |

There is no target-tag filter in BuildInteractionHits: tag compatibility means retaining live MainCamera/provider lookup and the unchanged PhysicsEvents relay tag/layer filters, not inventing per-hit tag rules. Raw membership from a layer-mask query is fixed for that native result; arbitrary geometry/layer membership mutations still use the existing explicit world-change/Fresh contract. Candidate metadata must preserve live properties the old classifier actually rereads without promising automatic new physics queries for every mutation.

### What existing123 actually protect

SharedQuery48 already cover four complete raw-query kinds with17/64/1000 hits and overflow warming; exact masks/camera/viewport/sub-epsilon/planar arguments; trigger/global/start-inside behavior; dense target/blocker parity; independent target obstacle policies sharing raw data; explicit invalidation/expired leases; fresh public range after blocker mutation; callback-created wall/exception and cached-click invalidation. However DenseTargetAndBlockerSelection uses an explicit selected collider and disables its wall only after SyncTransforms + Invalidate. PublicRangeGetter uses Fresh. Those cases do not establish live reparent/destroy/trigger reevaluation within an unchanged warmed raw lease.

Interaction19 include real root-collider hover, current camera/provider/tag changes, distinct pointer/center rays, availability/reentry, UI click and a common-level-root sibling wall configured **before** its query. Lifecycle33 include actual2D/3D camera/keyboard PlayerLoop N=1/100/1000 positive/transition/stable miss, reset/reopen/destroy operation tails and tap aim/range changes. These protect real automatic dispatch and root-collider success; they do not create compound descendants, reparent a cached foreign collider, or destroy a cached foreign blocker between two same-frame Shared classifications. PhysicsEvents12 are handler/filter dispatch cases; nativephase8 protect actual local phases/payloads and policy3 protect native policy parity/reuse. They are retained regression scope, not substitutes for projection-ownership tests.

Existing reusable **EditMode** InteractionQueryMathTests and Audit20261023NeoToolsInteractableObjectCoreQueryMathTests already specify equal blocker/target acceptance, first equal target point, blocked target output, invalid/maximum finite distances, inclusive range across float scales and standalone Core. They were read as reusable contracts; no new execution of them is claimed here and they are outside the original123 selection. Rerun them if the candidate bypasses or changes selection math; do not duplicate their synthetic numeric tests gratuitously.

### Minimal added parity needed if a candidate caches/indexes classification metadata

Use the existing owned native ray rigs and typed Core oracle; no new framework or counters-only proof is needed. Candidate-specific regression families should be limited to the newly introduced cache boundary:

- **Live same-lease ownership / rig exclusion, parameterized2D/3D.** Warm a nonempty complete raw query with null observers, pin real frame/epoch and native-call count, then world-position-preserving reparent a nonprimary collider into/out of the target descendants and into/out of the checkpoint/view rig while keeping a primary target collider eligible. Reclassify without fixture reset, yield, invalidation or Fresh. Include nested-ancestor movement with unchanged top root and an explicit target-collider control. Assert native-positive raw identity/completeness plus public hover/click/range result against current live ownership, not just cached counters. A test that invalidates on every reparent misses the new risk.
- **Cached foreign blocker/trigger liveness, parameterized2D/3D.** Warm target+closer blocker; destroy that blocker and require the same-lease classifier to skip its fake-null hit and use the next live candidate/target without a new cast. Separately change an already-present foreign collider's trigger status, and include the2D include-trigger gate. Keep a disabled-but-not-destroyed control to prevent an unsolicited enabled filter; compare fresh results only after the explicit refresh boundary. These protect properties the current projection rereads.
- **Compound/nearest point branch only if the candidate shortcuts selection.** An implicit target with a root collider plus nearer child/compound hits must select the nearest eligible target hit; an explicit override must not widen to siblings/children. Include a closer solid versus foreign trigger and range admission based on actual hit point. Reuse the existing Core equal-distance tests; if indexed selection changes ordering, add one native-control path whose expected result derives from the same original raw order plus current live classification, so a different RaycastAll enumeration cannot conceal a tie-order regression.

Existing callback-wall/exception, destroyed-target lifecycle, fresh range, all123 regressions and unchanged measurement capture remain necessary affected verification; the families above fill the unprotected metadata boundary rather than recreate those scenarios. Additional cases depend on the actual final diff, which remains unread until source-final.

### Diagnostic-contract issue to resolve honestly

Current Statistics.ClassifiedHits is documented as **raw hit entries inspected**, and both the original12 PlayerLoop tests and unchanged a747 capture assert N×H (1,000,000 in the dense positive workload). A genuinely cheaper pass that still visits those entries may retain that count honestly. An index that no longer visits N×H entries must not continue reporting that value merely to satisfy old assertions or move visits out of measured scopes. If the author proposes such a true indexed path, root must consciously resolve the diagnostic/acceptance contract before applying it; silently changing semantic labels, weakening fixtures, or reporting logical available hits as actual inspected work is not accepted. Automatic CPU/GC before/after remains the comparison, with all new preprocessing inside it; reduced Classify timing alone cannot establish a total improvement.

Prepared 2026-11-02T08:51:45.398Z. **Next: wait for source-final candidate handoff; source acceptance and post-fix functional/performance green remain open.** Only this report was appended.

## Independent plan admissibility — indexed positive path

**BOUNDED INDEXED DIRECTION ACCEPTED FOR IMPLEMENTATION, subject to source review and native verification.** The plan is not a source fix or a passing optimization. Root's explicit diagnostic-migration authorization in performance-02-root-decisions.md supersedes the plan's earlier N×H blocker and the preceding preparation section's requirement to resolve that decision. No user approval is required. A constant-factor full scan alone does not demonstrate removal of the measured dense positive hotspot.

Reviewed completed external REVIEW.md SHA2bdd2c4d6b77320aacc3280dcb13d4e71c93bb1adc6a2723dc042b092d3e1d59 and base-inputs.json SHAdc5f1fd4d91911d699f3671f92ff5761e2f340ab50f827926892c49bc32a01c3. The directory contains only those two plan files, no code. All16 listed current input hashes match. Of24 evidence hashes,23 match unchanged; the sole drift is performance-02-correction.md's explicit root-clarification paragraph. Removing exactly that new paragraph reconstructs the plan-era SHA dddda2ad52d1e69259d31ea8bdf648abc374aa747c86c974e0a6f1cdb9df8db2, so this is an attributed authorization update, not raw-evidence drift.

The proposed positive-only branch can avoid foreign projection safely when **checkObstacles=false and foreignTriggersBlock=false**, at least one valid target survives exact Core selection, and current primary-collider/dimension eligibility remains unchanged. In that branch foreign hits cannot affect target acceptance, while a target is never self-ignored. Current explicit collider or current GetComponentsInChildren<T>(true,reusedList) enumeration can supply membership without caching ancestry. Keep complete legacy classification for blockers/LOS, no-valid-target/debug-nearest output, unsupported cases and any uncertain predicate. Safe call-local hoisting may improve that legacy path without cross-call ancestry assumptions.

Admissibility conditions for the actual candidate:

1. **Index identity only.** Entry-owned collider→all original hit indices must include every hit/duplicate and preserve raw distance/point. Do not cache ancestry, root/rig membership, enabled/active state or trigger bits as immutable for an epoch. Live target enumeration includes inactive descendants, respects explicit-override identity and Unity fake-null, and must not accidentally replace the existing cached-primary eligibility contract. Only2D currently applies a live include-trigger post-filter; adding that new post-filter to a previously cached3D hit would change behavior.
2. **Preserve exact ordering/math.** Child enumeration order differs from native hit order. Gather/match by original index or an equivalent stable tie key before unchanged Core selection, so equal nearest targets retain the original point and range/debug behavior. One collider→one hit is insufficient. If Core finds no valid target (including invalid-distance candidates), use complete legacy output rather than a target-only empty result.
3. **Ownership and leases.** Build lazily inside the existing Classify/Automatic scope, publish only a complete index for the current Entry generation, and guard internal synchronous buffer access with the current lease. No user callback/yield may intervene. Release/Clear all collider/transform references on Entry.Release and static/scene/memory reset; clear per-consumer reusable collider scratch references after the call while retaining capacity. Public expired-view behavior stays unchanged.
4. **No warmed allocations.** A new List<int> per collider on each dictionary rebuild is not acceptable merely because dictionary capacity is retained. Reuse index/value storage and target/projection lists across epochs with zero measured warmed Automatic GC; release references separately from retained capacity. Cold growth remains observable inside the same measured scopes.
5. **Honest stricter diagnostic migration.** Count actual raw entries inspected during index construction, selected-hit projection and fallback; keep ClassificationPasses=N and native/cache/epoch/callback oracles exact. For the canonical dense positive geometry (H=N, one matching raw hit per target), a once-built index plus N selected projections has a derivable H+N raw-inspection budget, rather than old N×H. The final budget must follow actual implementation work, including build/fallback; arbitrary permissive bounds or fake N×H are rejected. Explicitly update only diagnostic assertions/docs and archive unchanged baseline capture a747; objects, inputs, density, observers, phases120+120, warmup, marker names and all functional completeness/payload/positive assertions remain intact.
6. **Protect new boundary.** Add the previously identified2D/3D warmed-same-lease child/rig reparent (including nested ancestry and explicit override), destroyed-foreign-blocker/live-trigger/disabled control, and compound nearest-point parity scenarios as applicable to the final diff. Reuse Core tie/range/finite tests and retain existing123 functional regressions. Every expected result needs complete native/raw and live-classification controls; a test that always invalidates before mutation does not protect this index.
7. **Prove total benefit.** Root must compare identical affected captures through unchanged calibrated reader, checking all N Automatic scopes and whole Automatic CPU/GC with index build included; lower Classify time alone or cold setup work moved elsewhere is insufficient. Preserve fallback costs honestly and qualify overlapping/deep target-subtree limits. No measured improvement, all-route benefit or Player claim exists yet.

No plan-level blocker remains after root's authorization; the concrete source candidate must satisfy these conditions before application. Classification ownership stays with CLI02; OptionalInputSystemAdapter/KeyInputCompat allocation work belongs to CLI01 and is outside this candidate. Reviewed 2026-11-02T09:34:48.024Z; report-only append, no source/process execution.

## Classifier SOURCE-FINAL gate — five-file candidate

**ACCEPTED FOR ROOT APPLICATION: no source blocker found in the exact five C# payloads below.** This is independent source/diagnostic admission only. Compile,129-case functional acceptance, Core checks and actual after-performance/GC acceptance remain open; previous native123 results belong to the old91aa/bcb49 runtime.

| Payload | SHA-256 |
|---|---|
| Query runtime | ff5caa6b7142455e2e89ba8ac011267594be87ff2049a1fa3e49a972c20767f1 |
| InteractiveObject | 574d29962bf48d3b72ec283795c4e48d917279f1f80b0bc33f4feaf71c9175f4 |
| PlayerLoop diagnostic | 035d5c78c62a1378885b26d14c1686cd039056dfa8620ed6fc6d225b7827a513 |
| Capture diagnostic | be18142d8aee5ef0d879f96e2db367761e54cc32ce1cd8641928e96f3e44471a |
| New indexed parity cases | 04f3d231e6f8f136359d98fe8d90be3f5d928a1684d49506aedcb51ef3d8c68b |

Candidate root: [lib-1102-02-performance](D:/Temp/Neoxider-LibraryClosure-20261102/candidates/lib-1102-02-performance/REVIEW.md). Final REVIEW SHA7b5b46e82b1873869a50cf0946ffdb9517df0a8fa4b7e69e3ae2d06341efc447 and base-inputs SHA8cccce81ad1e459ab201001505f926d047b47f0df7ead11a6b10045424cb0b5c match root's handoff. All five complete payload hashes/byte counts match metadata and are byte-identical to timeout-step-r1 source snapshots. All16 current/base frozen inputs,23 immutable evidence identities and seven authority/baseline identities independently match. Strict UTF-8 without BOM, original LF/CRLF and explicit local types are preserved. No code from the input-adapter ownership area is included.

All four existing-file diffs and the full new22,783-byte regression file were read, with the complete modified index/projection bodies reviewed against their base context. Query Key/static/callback/frame section and Get/Execute/overflow/reset tail are byte-identical to policy91; component code before Build (apart from new private scratch fields/import) and from EnsureInteractionHitCapacity onward is byte-identical to bcb49. Public/serialized APIs, Core math, ray arguments, availability/dimension precedence and hierarchy/ignore helpers are unchanged. Internal ProfileClassification callers are the two adapted component Build methods; signature migration does not leave another caller in the module.

The Entry dictionaries map collider identity to a reverse-built chain of **all original indices**; chain links ascend original raw order and raw hit arrays are never sorted. The ready bit is set only after complete construction, cleared on every Entry.Release, and public borrowed views retain their previous validity/indexer checks. Internal GetCompleteEntry validates a complete current lease for the synchronous operation. There is no callback/yield/user observer between lease acquisition and use. Existing release/static/frame/fresh/callback mechanisms clear dictionary keys and ready flags; retained next-index arrays contain only integers. No per-collider List is allocated on rebuild.

Fast classification is gated by !checkObstacles && !foreignTriggersBlock. Live explicit target refs or GetComponentsInChildren(true,reusedList) determine membership each call; selected hits are reread, Unity-null and current ownership checked. Target-only positive projection may omit foreign hits because Core selection does not require a clear path in this branch and target hits are never self-ignored. Sorting the gathered original indices preserves equal-target order/points across a different child-enumeration order. Exact raw distance/point feeds unchanged Core selection.2D retains the live include-trigger gate;3D does not acquire a new live-trigger postfilter. If no finite valid target survives, full legacy classification executes; blockers/LOS always retain that path, including disabled-vs-destroyed and live rig/trigger semantics. Per-consumer collider scratch clears in finally and retains capacity; private ints/projection structs retain no scene-object references.

The counter migration is narrow and honest: one ClassificationPass per Build; CountInspectedHits runs on each raw index-build inspection, each selected projection even if rejected, and each fallback scan entry. It does not invent N×H after skipping foreign projection. Cold failed attempts include build+selected+fallback work. Both existing diagnostic-file edits were independently reversed (one assertion plus explanatory comment each), producing **exact original base bytes**. Therefore every geometry/input/backend/observer/warmup/stamp/window/native/cache/epoch/functional oracle remains intact. Canonical H=N positive work is exactly H+N=2N; H=0 stable miss remains0. All lazy work is inside Classify/Automatic; ordinary Entry release cost is still included in Automatic at the frame/query boundary. Actual warmed allocations and total speed remain native measurement gates, not source conclusions. The old external a747 capture and reader ca85 still hash unchanged.

The six new cases are three methods×2D/3D and extend the existing Lifecycle partial. Their raw controls are complete and nonempty, comparing unsaturated native arrays with all-hit overloads. Decisive same-lease observations pin real frame, epoch, native1/Fresh0/callback0 and invalidation/frame counts, with no intervening caller invalidate/reset/yield/Fresh. New ownership test moves a deeper ancestor while immediate parent/common root remain fixed, including current explicit overrides and public hover. The liveness test distinguishes disabled/inactive foreign snapshots from destroyed fake-null and tests2D rejection/fallback versus3D cached Ignore behavior. Compound test admits root+two child+solid+trigger raw hits, range from actual hit point, explicit-root exclusion and exact H5+T3=8 / warmT3 / explicitT1 diagnostic controls.

The forced legacy oracle toggles only the owned checkObstacles field during direct projection reflection, restores it in finally, and applies the saved original selection policy to both outputs. This disables only the shortcut while retaining the unchanged live legacy classifier; no query/callback occurs during that comparison. Hard expected selection booleans, native controls and separate public hover/range outcomes prevent vacuous self-parity. Actual projected result structs/nearest data are saved before the legacy array overwrite. The two coincident native child hits establish an actual distance tie but have coincident points, so unique tie-point discrimination still comes from retained distinct-point Core tests plus source proof of original-index sorting; no stronger native tie-identity claim is made.

**Required root admission after application:** compile the exact runtime/test source vector; select old123 functional identities plus six new cases =**129**, explicitly excluding the12 Capture_* methods now sharing the Lifecycle partial; include existing standalone Core tie/finite/range controls as affected validation. Then collect all12 identical-workload after captures with diagnostic-only be1814 and unchanged calibrated ca85 reader, checking total Automatic CPU/GC including lazy build/release, N invocation counts, complete raw/native parity,2N/0 work budgets and actual source hashes. Keep keyboard GC attribution separate from CLI01 input changes. No performance improvement,0GC, SDK6.6/Player/physical input/EventSystem acceptance is granted from this source gate.

Source-final review recorded 2026-11-02T10:29:39.973Z. Reviewer actions were read/hash/diff analysis and this report append only; no compiler/test/Unity/process/product/config edits.

## Fixture compile correction after zero-test native abort

The initial five-file source review missed the new fixture's inaccessible internal InteractionRayProvider.TryCreateRay call. Root's actual interaction-index-u63-functional-r1 native launch stopped at **CS0117, exit1,0 executed, no XML**. This is a fixture compile failure and reopens fixture admission; it is neither129 functional failures nor runtime/performance red/green. [Compile-abort receipt](D:/Temp/Neoxider-LibraryClosure-20261102/runs/interaction-index-u63-functional-r1/compile-abort-receipt.json) SHA3de7971bdee40eaa8e0db25f678bccf3c598bd55bb2b704b3b6d4773d091906c and raw log SHA445d7ecf47a3701cb42ff45708c57dbb52c9f48a7b308d72d3f54ff876aa12e4 were verified; results.xml is absent. Runtime source admission remains scoped to the prior reviewed implementation, not successful native execution.

**Corrected fixture source ACCEPTED for root copy:** c1c660d40bcc428878a2ce379478c91feabdc4e48fe0e314c4797b8886cc9661 (22,943bytes). The entire correction is one old internal helper assertion replaced with Unity-nonnull camera, pixelWidth>0 and pixelHeight>0 assertions followed by public Camera.ViewportPointToRay(new Vector3(0.5f,0.5f,0f)). This exactly reproduces the existing provider center branch/prerequisites at lines94–106 (provider SHA0504766e4853f5a4442fe1b32ec49cd614660cbfae7df7fd284794560dfc3f8e). Reverse replacement reconstructs exact old04f3d fixture bytes. Geometry, native controls, Shared lease, all six parity/diagnostic assertions and workloads remain unchanged; no runtime API/friend/asmdef visibility change or new reflection is introduced.

The other four candidate hashes and all five current root-applied base hashes match the previous source gate; all seven revision-before-compile-fix archived artifacts match their prior recorded identities. Updated REVIEW SHA2182e8ee6c3de3028977b7beb99b66eb1de2187f54f89f3256d2b54887cace15 and base-inputs SHAad8f5c59fa9a8ac20498fb3b373f6e2e544f0a8d37d5e771530396c46c8a148a accurately retain original bases, current applied vector, prior zero-test abort and NotRun-by-author qualification. No source blocker remains for this narrow fixture revision. Fresh compilation, exact functional129 excluding Capture_*, Core checks and actual after captures/readback remain pending.

Fixture correction reviewed 2026-11-02T10:41:41.522Z; only this report was appended, with no compiler/Unity/test/process/product/config action.

## Actual indexed-classifier functional green — r2

**ACCEPTED, Unity6000.3.14f1 Editor functional scope:** [interaction-index-u63-functional-r2](D:/Temp/Neoxider-LibraryClosure-20261102/runs/interaction-index-u63-functional-r2/results.xml) completed native exit0 with **129 executed /129 Passed /0 Failed /0 skipped**. This closes the prior fixture compile interruption for this selection. It is not after-performance/GC acceptance, Core acceptance or an all-SDK/Player result.

Actual [launch](D:/Temp/Neoxider-LibraryClosure-20261102/runs/interaction-index-u63-functional-r2/launch.json) records PID27632, 2026-11-02 10:42:18.255–10:43:13.620 UTC, PlayMode Neo.Tests.Play, D3D11 graphics batch/job-worker-count1 and6GiB guard. Log records successful Csc/IL postprocess for Neo.Tests.Play, Tundra success and natural test exit0, with no C# error. Audio obsolete-property and Mirror DOTween resolver warnings remain in the log; this is not a warning-free/dependency-profile claim.

Recomputed launch/XML/log identities match summary:

| Raw file | Bytes | SHA-256 |
|---|---:|---|
| launch.json |9230|511ced3875ca8380eee77aad787180c617b25dc11edc3f169943910be1fa1c51|
| results.xml |132285|9152a7407b05e35b3ddcbbdc6287a582ba081a623cae443f9c591a7642476330|
| editor.log |56542|d911e7f252c78dbfc4a4536d27d597c1616858ab286d90558a8949b61d24bf83|

The complete raw leaf inventory after entity decoding agrees exactly with [testcase-index.json](D:/Temp/Neoxider-LibraryClosure-20261102/runs/interaction-index-u63-functional-r2/testcase-index.json). Its names are exactly the prior accepted123 plus all six IndexedProjection method×False/True identities; no missing, substituted, unexpected, skipped or Capture_* case is used. Fixture counts are Interaction19, Lifecycle39(33 old+6 new), PhysicsEvents dispatch12, SharedQuery48, policy3 and nativephase8. Launch's33 explicit old Lifecycle methods exactly equal the prior old33 name set; only IndexedProjection_ is added by prefix, so the12 profile capture methods sharing the partial class are excluded.

The run's5,945 frozen identities/1,535 source copies include exact admitted runtime ff5caa6b... /574d2996..., PlayerLoop035d5c78..., capturebe18142d... and corrected new fixturec1c660d4.... Those five frozen hashes match candidate metadata, actual frozen bytes and current project bytes. Original Interaction/Lifecycle/SharedQuery/PhysicsEvents fixtures, input driver, policy/native-phase/raw-control fixtures, Core math/hit types, provider, relays, reader and Play asmdef retain their admitted hashes. The only original functional-fixture change is the previously accepted diagnostic assertion/comment in PlayerLoop; reverse-diff source proof remains valid. No common Packages/ProjectSettings identity differs from the earlier accepted policy run.

[Summary](D:/Temp/Neoxider-LibraryClosure-20261102/runs/interaction-index-u63-functional-r2/summary.json) reports changedFrozenPaths=[] and protectedDifferences=[]. The [nonprivate preference comparison](D:/Temp/Neoxider-LibraryClosure-20261102/runs/interaction-index-u63-functional-r2/editorprefs-comparison.json) retains51 monitored names before/after, no additions/changed originals and no root registry modification. Private values were not opened.

All six new compiled native cases actually Passed. With unchanged c1c source, this admits their nonempty complete native controls, same-frame/epoch/native1/Fresh0/callback0 invariants and live legacy/public hover outcomes: nested nonprimary descendant/rig reparent and explicit override; disabled/inactive versus destroyed blocker and live trigger distinctions; compound nearest point/range/override. Their exact work assertions complete as part of the passed tests: cold H1 index+H1 fallback=2 and warm selected1;2D selected rejection1+fallbackH2=3; compound coldH5+T3=8, warmT3 and explicitT1. No separate per-case stdout measurement is invented; these are assertions in the exact executed frozen fixture. Prior native tie-point qualification remains.

All12 real PlayerLoop matrix outputs were inspected, across camera/keyboard,2D/3D and N1/100/1000. Positive rows explicitly report **ClassifiedHits=2/200/2000** respectively, ClassificationPasses=N, native1, cacheN−1, fallback/LOS/callback/native-inside-callback0 and one frame advance. Stable-miss rows report inspected0 with the same strict query/pass conditions. Camera transition-miss retains its expected N callback epochs/nativeN rather than being misreported as a stable cached budget. Thus the narrower H+N diagnostic budget is observed natively without losing the functional positives. No CPU or GC value is derived from test durations/counters.

Policy3 also remain actual Passed with full A→B→B reuse: false→true native/shared1→2→2 in frame322/epoch17974; true→false2→1→1 in frame325/epoch17977; native calls1/2/2 and cache0/0/1 with Fresh/invalidation/frame/callback counters0 in each sequence. Explicit3D remains native1/cache0/1/2 in frame319/epoch17971. Physics8 retain their bounded local/optional-solo acceptance; both3D collision outputs again admit awake raw/Inspector/C#1/1/1 enter/stay/exit with strict payload/contact assertions. Remote networking is not inferred.

Input runtime source during this classifier functional run is still OptionalInputSystemAdapter d2498b1e6635fe8e3560d7a15b65d4ab6d8b81a202bfcc1f21ce7b8d96f90d0c, KeyInputCompat44bfa760fabc48f8cbef61b842003e40d74f527c5a90639b5344674ff03726e3 and InputBackendCompat7239385234ae3ee189d3339e4cd6f10701ae6a657dbe7bcc0c70c08e51b09eb8. Separate input test/calibration work is not counted in129 and is not credited as classifier GC improvement.

**Still open:** actual dedicated Core result (root's separate calibration job was pending when this review started), identical-workload after-profile CPU/GC/readback for the classifier, separate input allocation correction, SDK6.6/Player, physical input and external/EventSystem performance. No after-profile file or active calibration output was read in this review. Recorded 2026-11-02T10:48:27.262Z; only this report was appended, no process/compiler/test/product/config action.

## Actual Core30 and first indexed after-profile — Camera3D N1000

**ACCEPTED:** the required Core30 checks passed; the first indexed Camera3D N1000 native readback is valid measured data and verifies a large dense-positive improvement in this Editor capture. **Stable-miss is observably slower and remains a qualification.** This does not accept the full12 after matrix, an input fix, Player/FPS, external EventSystem or universal performance. Only completed launches were inspected; no active input calibration/red-run output or native profiler query was used.

### Core scope

[interaction-core-input-calibration-u63-r1](D:/Temp/Neoxider-LibraryClosure-20261102/runs/interaction-core-input-calibration-u63-r1/results.xml), PID33736, Unity6000.3.14f1 EditMode, ran 10:44:18.823–10:44:56.646 UTC on2026-11-02, natural exit0. Raw31Passed leaves/index agree exactly: Audit...CoreQueryMathTests18 + InteractionQueryMathTests12 + one separate input Recorder calibration. The **30 classifier Core names match the original EditMode baseline exactly**; both test-source and Core-source hashes remain unchanged. This admits their finite/max-distance, equal-distance/first-point, range, target-contract and assembly-leaf assertions on this SDK. The31st input case is not counted as classifier acceptance and is not a byte-allocation/input-fix proof.

Core raw XML SHA543bad6424b32735284d52a041aacefc84c060b8b1908b56f945eaafe58beb82 (34,464bytes), launch SHA3ba7773cb2b4348a788ec740addb53efb3ca90a479efce54061a4e513d8d7baf and log SHA2034849bce1848e06b9bfec19c05243c3eef9f19607e403cf1c6e6a78b737498 were recomputed and matched summary. Core/capture own summaries retain no frozen/protected differences and their immediate nonprivate51-preference comparisons are unchanged.

### After capture/native provenance and workload identity

Exactly one actual Capture_Camera3D_1000 Passed in [interaction-index-u63-profile-camera3d1000-r1](D:/Temp/Neoxider-LibraryClosure-20261102/runs/interaction-index-u63-profile-camera3d1000-r1/results.xml), PID34732,10:46:03.585–10:46:53.940 UTC, PlayMode exit0. Recomputed XML SHA5b7c7b6327f6af4a44bb9c3f5ae7339ad7f5c4c241ea0ae66b7a37b2e4e01cdc (4433bytes), launch SHAb263d1e7387a957fd8a249f74103e04d87817ec28ff09a0a75688104fe46b7ff and log SHA30fe3493c6f6c07d4a463b3b516529a073fe448df988f672f9f3a9f410945b7a match receipts. Its output names the exact after raw/counter pair; there is no substring-selected extra density in this run.

Actual [native after readback](D:/Temp/Neoxider-LibraryClosure-20261102/runs/profile-readback-u63-index-camera3d-n1000-r1/launch.json), PID29024,10:47:46.810–10:48:24.177 UTC, natural exit0, uses the same746d315f driver and ca85 reader/API assembly30019c1f. EXPORTED JSON matches the log, one profileLoaded event occurs and native frame bounds are0/252. Raw file camera3d-n1000-ad76ad97a8af4d6090d33958234516e2.raw is **48,170,380bytes, SHA055d8a6491c0461b27187290d54239ee45a0527f4a2727db08afcfb331c00ab2**, recomputed equal to expected/before/after hashes. Counter CSV is9901bytes, SHAb47041977a9a79f6906c61901b3865f90dbd050449de83f8a29a418e49f4977e. [After CSV](D:/Temp/Neoxider-LibraryClosure-20261102/profiles/readback-index-u63-r1/camera3d-n1000.csv) is39,793bytes, SHAb6c9b5c8fef22a6ac00a6bf41f3b5f48c567564dafff2ada6b212e0866788576. All exported hashes match native receipt.

Frozen capture/readback copies of all five admitted files match ff5caa/574d29/035d5c/be1814/c1c660 identities. The after capturebe1814 reverses only its diagnostic assertion/comment to exact archiveda747; unchanged Lifecycle/input-driver setup therefore preserves geometry,N1000, input mode, null observers,8warm frames,120+120 stamps and state/native/cache/epoch assertions. Readerca85 is unchanged. Relevant input runtime still has oldd2498b1e adapter,44bfa760 KeyInputCompat and72393852 InputBackendCompat, identical to baseline. Common non-Editor gameplay-source changes since baseline are the two classifier files; a coexisting unrelated UIMeshRigGraphicEditor change is an Editor-source delta. No common Packages/ProjectSettings hash changed. No keyboard/input-GC correction is credited to this Camera profile.

Hardware recorded by both capture logs remains Windows11 build26200/x64,40264MB, RTX3050 Laptop GPU3962MB/driver32.0.16.2101, Unity6000.3.14f1/D3D11/job-worker-count1. CPU model/power/thermal/concurrent machine load and full profiler preferences are not controlled by these captures. The magnitude is observed under profiler-enabled Editor, not a portable FPS guarantee or a controlled per-operation microbenchmark.

Readback public preference comparison was initially absent. Root later recorded51 names/types/data equal with no registry restore/mutation; [timing qualification](D:/Temp/Neoxider-LibraryClosure-20261102/runs/profile-readback-u63-index-camera3d-n1000-r1/prefs-comparison-timing-qualification.json),10:53:48.829 UTC, explicitly says comparison was taken after a subsequent owned input calibration. **This is a known later equality comparison, not an immediate post-readback preservation snapshot**. No private preference values were opened and no missing snapshot was fabricated.

### Exact metrics and retained slower miss

After export has240 finite rows:120 positive/120 stable-miss, ordinals0..119, profiler frames3–122/132–251, exactly1000 AutomaticUpdate scopes per row. Counter/readback phase+ordinal match row by row; Time.frameCount minus profiler frame is constantly22. Baseline used the same frame-index windows with its own constant offset552. No missing-frame, phase-skew, first-invocation-only, milliseconds/seconds or nested-sum substitution explains the difference.

Every after counter row retains native1/cache999/classificationPasses1000, callbacks/LOS/fallback0 and one frame advance. Positive raw inspections are exactly2000 (=H1000 index+T1000 selected work) rather than baseline1,000,000; stable-miss inspections remain0. All lazy index/selection/release work is included in the summed Automatic scope. Classify/Native are inclusive diagnostics already contained in Automatic, not additional totals.

| Metric (ms per selected frame, median unless p95) | Baseline positive | After positive | Baseline stable-miss | After stable-miss |
|---|---:|---:|---:|---:|
| Automatic |955.598798|7.466500|3.085700|4.573150|
| Automatic p95, nearest rank114/120 |1169.387396|10.977600|4.630000|5.979700|
| Classify inclusive |937.925648|3.042350|0.287500|0.377300|
| Native inclusive |0.266900|0.416000|0.006750|0.009400|
| Main-thread frame context |959.499451|10.797050|5.272250|8.231250|

The observed dense-positive Automatic median ratio is **127.98× before/after** (99.22% less measured time), with original workload and strict functional129/Core30 retained. This supports removal of the measured repeated dense projection hotspot for this case. However stable-miss Automatic median rises **48.20%** (3.0857→4.57315ms), its p95 also rises, and native time is higher in this capture. Those slower observations are preserved, not averaged away or attributed conclusively to code versus uncontrolled host conditions. No all-scenario speedup or whole12 performance acceptance is granted.

Automatic managed GC is0 on all240 after rows. Actual GC.Alloc calibration again observes4096 payload/4128 metadata bytes; calibration SHA1a1e88446f2fd6a92a473b6e9512af459fcf926acea22bb0c2beae72b996ba9a matches before. This verifies no added recorded warmed allocations **inside Automatic in this captured workload**. All-thread GC remains nonzero: after positive1384–5118bytes (median1796), stable-miss1796–5118bytes (median1796),92 observed threads. It is not global zero GC and is not assigned to library Update. External/EventSystem scope and Automatic/external union remain NOT_MEASURED on every row and in the unchanged coverage sidecar.

**Remaining gate:** collect/read the other11 after profiles with exact source-vector attribution, retain slower-miss qualifications, and assess the complete affected matrix before an overall performance verdict. Separate input native-counter calibration is not byte proof, and an input allocation fix has not been accepted here. Player/SDK6.6/physical-input/external EventSystem/cross-device claims stay open. Reviewed 2026-11-02T10:57:29.235Z; report-only append, no process/compiler/test/profile load/product/config write.

## Input polling native-byte helper — independent SOURCE gate

**ACCEPTED FOR ROOT IMPORT/COMPILE, source scope only; no source blocker found.** Reviewed all468 source lines and README: [Audit20261102InputPollingRawReadback.cs](D:/Temp/Neoxider-LibraryClosure-20261102/fixtures/Audit20261102InputPollingRawReadback.cs),28,115bytes SHA1b4f221872ed02914bc1986199ff539600270fb492a9ef3a933ab4e932ddcd00; README SHA724008824d9bf06ccd0247038d323fa9b2fe8b79a1cc898faec9282770ff8965. Compile/native-byte/calibration/old-new product admission remain pending. The reviewer did not read/load/interpret either large raw file, execute the helper, or launch a process.

Owned native-load lifecycle reuses the proven746d pattern: explicit owned batch/entry/project/version, idle/no-recording/no-history preconditions, one profileLoaded event and bounded actual native frame range, outside-project task paths rejecting reparse/ADS, absent output, read-only raw guard and SHA before/after, fresh CreateNew UTF-8 receipt, failure status/exit2 and event/update unsubscription. No history clearing, recorder/global setting reset, private preference read, registry operation or borrowed Editor control is introduced. Blocking native load/large-history resource limits remain the root watchdog/guard responsibility, not a fabricated readback pass.

Pinned current5fbde0d9 Edit andde0541a5 Play fixtures, protocol8cc60cb6, ca85 reader and746d driver were independently hashed and match. Product mode accepts only explicit reviewedd2498b1e old or29ed698b new and checks mode/current source agreement. The current subject is stilld249. Current capturebe1814 is recorded as unrelated context; historicala747 is provenance, not an erroneous old-current admission pin. The imported helper records its own identity; root still must verify exact imported/frozen helper bytes.

The actual producer sources were read. Their synchronous counter constructor emits exactly12 named scopes:6 NativeWindow,3 EmptyWindow,2 Retained4096 and1 outside Excluded4096; a single polling leaf adds one Window/Polling pair for14. Play's input preparation yields occur before counter construction, not between admission and polling. The reader requires that exact sequence, one actual native frame and persistent native thread ID/name/group/index; nonzero caller ID and exact declared expected name; direct raw/native and named Window parent for each child; matching parent/child inclusive totals and no merged/nested windows. Installed exact-version UnityEditor.xml documents the public persistent thread fields, recursive child count and long metadata methods; no nonexistent parent API or inaccessible runtime helper is used. Compile remains the next actual gate.

Raw traversal reconstructs preorder parent ranges with checked/noncrossing/bounded subtree validation and disposes each native view. It enumerates valid thread views until the first invalid and rejects a valid thread at cap4096. Each GC.Alloc with an open named scope reads mandatory nonnegative size metadata, increments the union once, and increments each enclosing scope's inclusive fields. Inclusive NativeWindow/child totals are never added as a new union. Two retained and one excluded payloads each require exactly1 event and metadata>=4096; all3 Empty children and their parents require calls/bytes0. Discarded warmup is recorded without a zero budget. Old polling requires positive calls+bytes; new polling requires **both exactly0**. Missing/extra/order/thread/parent/range/metadata/unknown-prefix data fails rather than becoming a zero. Calibration-only polling fields remain−1/NOT_APPLICABLE.

**Intentional selection boundary:** calibration mode supports one exact calibration leaf; old/new support one exact recognized polling leaf,14 scopes. A full Edit fixture capture containing repeated admission/polling groups is rejected, not guessed or merged. Root must use fresh single-leaf polling captures or separately source-review a counted multi-group reader. Current file pins and caller-declared test name are not proof of raw origin: root must independently match raw path/hash to actual XML, complete source freeze, admission/state/count oracles and the intended allocation-only old failure/new pass without skips. RAW_SCOPES_VALIDATED means calibrated named-byte scopes only, not automatic product or performance closure.

This source gate leaves ca85/746d and all workload/product/config files unchanged. No byte total, zero-GC green, old causal byte red, CPU/Player/global/EventSystem claim is admitted from inspection. Reviewed 2026-11-02T11:16:05.692Z; only this existing report was appended.

## Bounded Input capture wrappers — independent SOURCE gate

**ACCEPTED FOR TEST-ONLY ROOT IMPORT, calibration first; no concrete source blocker found for the exact installed context.** Both complete C# wrappers, README, review and structured metadata were read. No wrapper was compiled/run by this reviewer; capture/native bytes remain pending. Import only the two new test files with fresh metas, not product/fixture/archive/reader copies. Main remains current29ed; no historical restoration is authorized here.

| Source-final artifact | Bytes | SHA-256 |
|---|---:|---|
| Edit/Core/Audit20261102NeoToolsInputAllocationCaptureTests.cs |24651|084be5b576d6efc4cc353f24b924e48e60b9014d27bea77db338edeb90daacf4|
| Play/Core/Audit20261102NeoToolsInputAllocationCapturePlayTests.cs |24327|cb6662562c6844fe31066bce013734d39c309ad564754394cc9e27335d250339|
| README-bounded-capture.md |11627|8681e6304b0e3f31254b6c98d90a287a4f2076f5cb7aedf2a8ca9ace53ec62be|
| REVIEW.md |4815|1d556484d84b94d19f7a40d439f3ac2e07ee55f70e1a639dfcca027bf74be1f5|
| base-inputs.json |36674|0c1d5a2f4bd7f6cc174167b83f420530373099714cfead8b3d18ef0c637d59e6|

All declared current-file observations and nine protected external input identities match actual files. Both C# payloads are byte-identical to timeout-bounded-capture-r1; that timeout was not a native product failure. Import targets are still absent. Pinned Edit5fbde0d9/Playde0541a5, helper1b4f2218, protocol8cc60cb6, ca85/746d and current capturebe1814 are unchanged; historicala747 is provenance rather than a stale current pin. The two private OwnedCapture guard/receipt tails are byte-identical and stay within their existing separate test assemblies. No runtime API/friend/asmdef/dependency broadening occurs, and added code uses explicit types.

### Exact Unity NUnit concern resolved from installed primary artifact

A possible old-mode issue was checked against **the installed com.unity.ext.nunit2.0.5 Unity custom fork**, not assumed from NUnit3.13+. Local package metadata says based on3.5. Exact net40/unity-custom/nunit.framework.dll is291,840bytes, SHA25466626abfea654a068abc579be018d8ef164fbb5415856230de2b8fc4795d5. Read-only PE/metadata/CIL inspection was performed; the assembly was never loaded/invoked and no process/test/compiler was started. The public source URL in package README was unavailable, so a different public NUnit release was not substituted as evidence.

Assert.That core MethodDef0x060000EA (RVA13652) and0x060000F0 (RVA13800) resolve the constraint, increment assert count, apply/read IsSuccess, format a TextMessageWriter, then construct AssertionException and throw. They do not call result/failure-recording APIs. IncrementAssertCount0x060000C6 calls TestExecutionContext.IncrementAssertCount0x060004F1, whose body only System.Threading.Interlocked.Increment's field0x040000DB **_assertCount**. AssertionException constructor0x06000261 calls ResultStateException0x0600026D, which only calls System.Exception's constructor; no context/result mutation is hidden there. Thus a caught ordinary failed That does not itself mark CurrentResult Failed before the runner catches it in this exact artifact. No isolation/clearing workaround is required by that concern. Public TestContext CurrentContext/Test/Result, TestAdapter ID/FullName, ResultAdapter Outcome and ResultState Status members also exist with public method flags in this installed binary. This is source/artifact evidence, not an executed old-wrapper PASS; a changed NUnit artifact requires renewed qualification.

### Fixture preservation and rejection boundaries

Editor directly binds one actual public Edit v2 method and calls actual public SetUp before recording/TearDown after stop. Play creates the original public iterator once, forwards every actual MoveNext/Current yield unchanged, and disposes it in finally. Both reuse the real calibration/private recorder/input/state/positive controls; none of that behavior is copied or rewritten. Two header yields precede the body; two actual flush yields follow completion or caught old budget before stop. Fixed fixture source proves calibration12 scopes and focused polling14 remain in one synchronous native frame after Play's preparatory yields, ready for unchanged strict helper1b4.

Old mode accepts only exact AssertionException type, full case-specific allocation-budget message, positive integer actual count and the original method/state-machine stack anchor. In pinned fixtures, correctness/state/calibration checks precede that budget; any different assertion, setup/cleanup/skip/capability/parse/pin/closed-raw mismatch fails. Calibration/new require actual return without exception. An existing wrapper Failed context is rejected and never cleared. The caught old assertion is retained as ORIGINAL_ALLOCATION_ASSERTION_CAUGHT with type/message/stack/count, never original-test PASS or synthetic original XML. Statements after the original old exception are correctly not claimed executed. Actual wrapper XML is still mandatory; a receipt alone, particularly on cancellation/kill, does not admit a result.

Ownership checks bind fresh batch/project6000.3.14f1, exact wrapper fullname/filter/assembly/platform, caller owner/ID/mode/source pins and one-use static guard. No startup-profiler/executeMethod/-quit invocation is accepted. Profiles must be idle; only the owned API log/binary/enabled state is saved/restored in finally, without history clearing. A changed foreign log target is not stopped or overwritten and yields failure. Fresh GUID raw/receipt/data names under task profiles reject UNC/ADS/reparse/out-of-root paths; source hashes are checked before/after. Artifact hashing/CreateNew JSON occur only after recording stops and a nonempty raw opens against concurrent writers/deletion. Root's existing watchdog/prefs/source guard remains responsible for killed-process cleanup and actual native serialization. No native-load success follows merely from raw size or wrapper PASS.

The previous giant startup calibration raw failure is retained: public receipt SHA406de4de57884b2e8da959928bc02e6e1e3d998a7086fa2476c6e744b7165ac4/log SHAf90a06972bdcc7ad6c3fb1072f48393da8cc99c6a17931d6d110ae988607c5af were verified; LoadProfile returned false before Scan, events/views/scopes0 and polling−1. Those are failed admission fields, not zero bytes. The1.267GB raw was not read/retried by this reviewer.

**Root next gate:** current29ed Editor calibration wrapper actual PASS and closed raw first, then a separate fresh helper1b4 native readback requiring12 exact named scopes, two retained one-call allocations>=4096, outside excluded payload and empty/stale-window zero with real frame/thread/parent identity. Only after that, focused original allocation leaves under explicit source/mode with14 scopes; readback declaration is originalDeclaredTest, never wrapperTest. Old-positive requires calls **and** bytes>0; new-zero requires both exactly0. Missing/deserialization/capability failures remain no admission. Root-reported prior15 native-call/affected/profile greens do not replace this focused byte gate. No wrapper compile proof, byte red/green, input product closure or global CPU/GC/Player claim is granted here.

Reviewed 2026-11-02T12:36:04.677Z; only NativeInteractionExecutionReview.md was appended. CLI01 code/product/parser and this author's generic library reader263a/README6327 were not changed; the separate package-relative resolver correction awaits its complete independent review/followup.

Wrapper-source supplemental witness: the completed historical input-native-u63-edit-red-r2 and -play-red-r2 XML were inspected after their finished exit2 receipts. All six original allocation-budget failures match the exact anchored message shape used by the wrappers and retain the required original stack anchors: Edit bool86016/mapping294912/missing12288 calls; Play released/down/up294923 calls each. These are historical native-call assertion payloads only, not new wrapper execution or byte metadata. No large raw was read.

## Owned TMP Essentials bridge — independent SOURCE gate

**ACCEPTED FOR TEST-ONLY STAGING in the exact owned short host, no concrete source blocker.** Full source/README were reviewed: Audit20261102OwnedTmpEssentialsPreparation.cs28,128bytes SHA2a652b64f95f274effc2dfcd8f6530b4d1305ca02e9376d1223b273230aa771a; README813a264ef94af036168bf82d027eac5b68449832dbce59c0e84aa1c907e79dad. The reviewer did not stage, compile, run, import TMP, modify a host/configuration, or inspect private prefs. This is not native TMP or sample acceptance.

The bridge host/company/product constants exactly match the admitted R2 short profile: D:/Temp/NeoLibQA1102/Local/Audits/assembly-2026-10-23/unity/final/u, NeoQA_20261023_LibClosureU63UpmShort20261102 / NeoTools_ConsumerU63UpmShort20261102, Both2 and DOTWEEN. The existing NativeConsumerContextReview dependency-only admission is retained; its NoNeo success is not recast as Neo/package/sample/loaded-Pro proof. Long-host exit0/UXML exceptions remain failed dependency evidence and no universal path-limit explanation is asserted.

Main-project official uGUI2.0.0 primary source was independently read. package.json8e66169c... and importerSourceca99679d... match pins; the exact public static three-Boolean ImportResources method delegates Essentials to native ImportPackage, examples only when its second flag is true. The bridge has exactly one ImportResources(true,false,false) call, with hooks registered first. Its private GetPackageFullPath resolver exists with that exact signature and only reads paths; no missing-API guess, interactive menu, custom extraction or fallback import/copy is used. Native path resolution remains an actual preflight gate, not claimed working from source alone.

Official Essentials archive SHA58564ec64cef8a8cea3d0b8ffb5e6fecf4176d544a2f6a29a2b43a0a33256431 /941,722bytes was inspected in memory without extraction or writes:45pathname entries, all under Assets/TextMesh Pro, no.cs/.dll/.asmdef/.asmref payload. Actual sample text references intersect its GUID map in exactly three GUIDs and file counts: SDF shader68e6db...×5, default font8f5863...×32, Drop Shadowe73a58...×1. Settings points to that font, spritec41005... and stylef952c0...; default font and Drop Shadow material point to mobile shaderfe393a... and the same embedded atlas. Both required notice hashes4b65f896.../37f8552e... match exact archive bytes. This confirms the ready-made payload/reference choice, not imported native assets.

Ownership preflight is strict before import: activated batch/exact project and cwd/entry/SDK6000.3.14f1/D3D11/company/product, no test/quit/nographics, helper sourceSHA argument against actual staged bytes, NoNeo manifest/Assets/registered+loaded+compilation graph, compiled Both/DOTWEEN plus actual Both2/Standalone defines, NoPro names/references/payload, and absent TMP folder/meta/settings. Main, long host, other profiles and borrowed TMP are not admitted. Package/version/source/archive pins are checked against the actual registered package and the official resolver's archive. No code writes PlayerSettings/Packages/settings/fonts/materials/scenes/prefabs or global preferences; only the official import will write its pinned resource payload in the owned host.

Receipt is reserved CreateNew and later updates require the same random runId. Native started/completed/failed/cancelled and error-log callbacks/watchdog are hooked before one invocation. Success needs exactly one matching start+complete, no failure/cancel/error, return from importer,3idle updates, actual persistent GUID/path/type loads and Resources.Load settings/default-font/sprite/style/material/mobile-shader/atlas/notices checks, then3more idle updates plus unchanged top-level ProjectSettings/Packages hashes. Settings/native asset migration bytes are recorded rather than incorrectly forced to equal old serialized archive bytes; notices remain exact. Shader.isSupported is readiness only, not pixels/all variants/Player. Root must inspect the complete log, including pre-entry errors.

Unexpected assembly reload resumes only into failure using task-scoped SessionState ownership, never a second import;300s normal and60s reload watchdogs are fail-closed. Completion finally unhooks callbacks/update/delay hooks and erases those task SessionState keys. No EditorPrefs or history clearing is introduced. A native main-thread hang still needs root's outer timeout; ownership failure outside the exact activated host throws without intentionally exiting another Editor. Existing/failed receipt or partial TMP state is not deleted to fabricate freshness.

**Root next:** after full root source read and between owned jobs, stage only the exact temporary Editor source/meta into the admitted short host, preserve pre/post configuration/prefs/protected-state evidence, run the one owned official import, and accept only actual verified-tmp-essentials/nativePreparationVerified + exit0 + complete raw log/native assets. Compile/import/events/asset/prefs outcome remain NotRun here. Genuine Neo registration, final package freeze, three UPM samples and their runtime/profile claims remain later consumer gates. Reviewed 2026-11-02T12:59:44.243Z; only this report appended.
