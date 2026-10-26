# Unity verification plan

Owner: the SAME single Unity operator, `/root/verify_unity`, `gpt-6.1-sol` / `max`.

Status: **PLAN ONLY; SOURCE GATE CLOSED.** This update authorizes no Unity/MCP, compiler, tests, checker, installer, build, service, peer process, resync or isolated-project mutation. It changes only this plan. Root has personally read the original artifacts and native reviews; narrow B/C contract follow-ups and the later approved implementation/integration work must all finish before execution. Read [IMPLEMENTATION-CONTRACT.md](IMPLEMENTATION-CONTRACT.md) and the final follow-up decisions again at resume.

The old preliminary imports/failed compiles and the uncompiled second preliminary snapshot remain diagnostic history in [UnityVerification.md](UnityVerification.md). Neither existing isolate is a current final host. Keep their logs and frozen snapshots intact; do not relabel historical evidence as acceptance of later source.

## 1. Release the global gate and identify the frozen input

Execution starts only after root explicitly resumes this operator with a final verification request identifying a frozen source snapshot. The gate covers EVERY source writer, root's package/migration/changelog/metadata edits, public docs and test/harness/asmdef writers. An individual source-final receipt or the completion of one batch is insufficient. Outstanding additive API consumers, native B/C follow-ups and implementation receipts must be reconciled by root first.

At resume, read the actual approval/state marker at the path root supplies, relevant `<module>.implementation.md` receipts and acceptance/filter map. Record branch, full HEAD, dirty/new/deleted paths, capture UTC, per-file content hashes and a manifest SHA256 under `Local/Audits/assembly-2026-10-23/unity/snapshots/final-<UTC>-<id>/`. Include product/runtime/editor/tests, assets/metas, root guides/images used by tests, package manifests/lock, ProjectSettings and the existing .NET/checker sources. Copy ordinary files and detect capture-time drift. A moving source tree is not a final snapshot.

The target is 11.0.0; read the final metadata rather than substituting the old 10.19.0 or guessing a version. Record snapshot identity in EVERY run, XML/summary, peer transcript and capability receipt. Hash source inputs after import/tests as well. Any host mutation must be explained by a narrowly scoped transformation ledger; unexplained source/meta/asset drift is a failure requiring investigation.

If a failure needs product/test source changes, return it to root and the SAME original owner. Close execution while writers are active. Resume from a new frozen snapshot after root confirms all writers finished again; rerun affected checks/dependencies, retaining unaffected valid receipts with an explicit reason. No operator patch, shim or suppression of product defects.

## 2. Host matrix and process ownership

Use installed `C:/Program Files/Unity/Hub/Editor/6000.3.14f1/Editor/Unity.exe` and `6000.6.0f1/Editor/Unity.exe`. Recheck installation, free disk, license/readiness and current Unity/process ownership only after the gate opens. Never drive/stop a user Editor, including VampireDota. Serialize task-owned Editor imports/tests; a separate-peer run uses owned player processes, not competing automation in user Editors.

Create fresh final host roots under `Local/Audits/assembly-2026-10-23/unity/final/<snapshotId>/<editor>/<profile>/`, or explicitly resync an old task host only with an exact ledger and compatible cache. Each host uses independent mutable Assets/Packages/ProjectSettings/Library; no hardlinks/junctions/shared Library to main or another host. Preserve historical evidence. Before recursive moves/deletes, resolve and verify absolute targets stay inside the intended isolate/task-owned output root; use native PowerShell operations end to end.

| Profile on BOTH Editors | Purpose and required evidence |
|---|---|
| `mirror-on-dev` | Integrated import/compile, nonempty full EditMode and PlayMode, focused approved regressions, active developer samples only inside the isolate. |
| `mirror-absent` | Actual absence of Mirror dependency/assemblies and symbols; compile and discovery of applicable fallback contracts. Missing references are a real dependency/packaging failure, not permission to edit product asmdefs in the host. |
| `release-consumer` | Frozen release package with `Samples~` retained, real Unity package/sample import into a clean consumer, sample/native integration and actual imported paths. Keep this separate from developer sample activation. |

Record actual target/backend, test-framework/NUnit/UniTask/DOTween/Mirror/transport versions, all resolved package/lock changes and editor migrations per profile. Resolve only existing declared dependencies. No new package choices/global installs. Reuse the exact locked optional MCP cache if valid; prior root authorization permits omitting ONLY the unused development MCP package from an isolate if exact restoration remains externally blocked after two serious attempts. Record the omission and dependency check; it cannot hide a Neo dependency or substitute for capability evidence.

Launch hidden with captured arguments, working directory, environment relevant to the run, PID/start time, logs, deadline and exit status. Track only owned child PIDs and descendants. Cleanup uses that exact ownership ledger, graceful shutdown first, and verifies ports/owned players stopped; never kill by executable name or stop an unrelated session. Do not claim the earlier free-space/process inventory is current.

## 3. Persistence isolation BEFORE first Unity/test initialization

Distinct projectPath does not isolate saves. BEFORE first launch set unique companyName/productName in EACH ISOLATE, incorporating audit/run, Editor and profile identity; for example company `NeoQA_20261023_<runId>`, product `NeoTools_u6000_3_14f1_mirror_on_dev`. Record exact effective values rather than the example. Main ProjectSettings and user profiles/registry remain untouched.

An existing/owner-authored environment probe must emit `Application.companyName`, `Application.productName`, `Application.persistentDataPath`, platform, effective PlayerPrefs namespace and any overridden SaveProvider/file-store roots BEFORE save-mutating cases. On Windows verify the effective Editor/player registry branch rather than inferring it solely from the folder name. A task-owned sentinel round trip may use a unique key and delete only that key. Confirm that no save provider/test bootstrap redirects to main/user persistence. If this proof is missing or a path/key namespace overlaps main/user data, stop persistence-dependent execution.

The effective persistent directory may live under the user profile's Unity LocalLow hierarchy; it must be uniquely task-owned and recorded, not silently treated as being under projectPath. Check resolved paths before cleanup. Never clear user PlayerPrefs/entire registry roots or shared save directories. Fixture DeleteAll/Reset behavior is admissible only within a proven task-owned namespace or its private in-memory provider. Restore global engine/provider state between fixtures according to the actual test isolation contract.

Server and both remote clients also need separate save identities. If the existing peer driver fully injects ALL stores touched by the scenario, record per-peer roots/namespaces. Otherwise use separately built peer identities from isolated settings; passing distinct network ports/log paths alone does not isolate PlayerPrefs. Do not invent an unsupported command-line persistentDataPath override.

## 4. Prove Mirror on/off and test discovery

Record actual compiler defines per relevant runtime/Edit/Play/integration assembly and named build target, loaded assembly/dependency identities, Mirror Weaver result and discovered conditional tests. Source `#if` counts are not compiler evidence. The current baseline has global Standalone MIRROR defines, a vendored `Mirror.CompilerSymbols` initializer that adds them, per-asmdef versionDefines and hard Mirror references; re-read the final graph at freeze.

`MIRROR` removed once while its installer/vendor remains is not a durable off profile. Likewise a false symbol with Mirror still loaded is not dependency-absent evidence. A true absent host excludes the external Mirror dependency and its auto-symbol installer and removes its host defines through the recorded profile; it does not remove Neo product references or hide fallback consumers to manufacture a pass. If final package/asmdef design cannot import without Mirror, report the exact graph failure and original owner; label a present-dependency/disabled-branch probe separately if it is supported at all.

Require nonempty expected discovery before execution. Keep per-profile assembly/fixture/test names, total discovered, selected, executed, passed, failed, ignored/explicit/inconclusive/unsupported counts and reasons. Compare the selected conditional network/fallback cases with the owner-provided acceptance map. Zero selection, absent woven body or an unexpected Ignore is unverified/blocked for that contract. An overall green exit does not prove Mirror networking ran. Do not demand the same network case count in an honestly dependency-absent profile; its fallback selection must still be nonempty and intentional.

## 5. Samples and CMF: two distinct import paths

Main/frozen release layout stays `Assets/Neoxider/Samples~`, with `package.json.samples[].path` matching. In the developer host only, activate `Samples` and transform that host's sample paths, recording source-to-host mapping. This compiles sample scripts but is NOT proof of Package Manager sample import.

In `release-consumer`, consume the exact frozen package through the existing supported local-package path; avoid also embedding a second Assets/Neoxider copy. Use Unity's real package/sample import path through the existing or approved owner-authored driver. Record resolved package ID/version, each imported sample name/path/GUID/reference result under `Assets/Samples/NeoxiderTools/<actualVersion>/<sampleName>/...`, and run relevant sample smoke/lifecycle scenarios there. Manual folder copying is not a native-import receipt. Only necessary existing vendored dependencies enter this clean consumer; do not copy an old imported sample tree as another implementation.

CMF needs one active copy of each sample script. The preliminary host had seven byte-identical showcase scripts in both the existing `Assets/Samples/CharacterMovementFundamentals` and activated package sample. For final input compare current paths, full bytes, metas/GUID/reference ownership first. Only an exactly identified redundant IMPORTED copy may be quarantined outside Assets, inside the SAME isolate, with a reversible mapping/hashes; retain canonical runtime code and scene/prefab references. Do not delete arbitrary duplicate-looking classes, modify package sources, use a shared Library or treat divergent copies as a host-only collision. Divergence/reference breakage goes to the source/sample owner. The release-consumer run should import into a clean namespace and establish which copy Unity actually uses.

## 6. Import, focused cases, then nonempty Edit/Play suites

After preflight and isolation, perform final import/compile on 6.6 first, then 6.3; record full logs/exit codes/console diagnostics, resolved dependency graph and transformed-source integrity. Both version-specific API branches require their actual compiler; a preprocessor scan is not acceptance. No full test run while import/compile fails.

For each successful host, run owner-mapped critical regressions first: reentry/generation, exact deltas/typed final text, bounded ticks, quantity/card conservation, callback-fault persistence, pause/cursor ownership, handler/authority/counter/selector, query completeness/cache invalidation and the runtime-to-editor additive API consumers. Then run the requested FULL nonempty EditMode and PlayMode suites on BOTH Editors, in distinct jobs/log/XML paths. A fixture that uses EnterPlayMode from EditMode must finish/exit cleanly before the next job; do not overlap Editor-state suites or hide a real lifecycle case as a synchronous reflection pass.

Use stable real-time deadlines and supported sequential execution for shared engine/global-state fixtures; no unsupported NUnit attributes/shims. Record timed-out/crashed jobs separately from assertions. Keep baseline/new console diagnostics and fixture teardown faults. Read XML failures and logs, not only exit code. Changes after failure trigger affected rechecks after the global source gate closes again; do not repeatedly broaden/repeat passing suites without a new reason.

Root also coordinates the existing .NET tool suite and package/docs/UTF8/meta/GUID/reference checks AFTER all writers finish. Capture exact commands, tool versions, stdout/stderr, exit codes and snapshot identity; no checker is run by preparing this plan. Package validators/static scans remain separate evidence from Unity runtime behavior.

## 7. Separate peers over a production transport

Use the APPROVED supplemental source harness from the SAME `Tests-Play-Network` owner (`neo-audit-1023-extra-05`), integrated with the primary Play/runtime owners. Require its final source/entrypoint, existing production transport selection, build/launch parameters, ports, schema, scenarios and cleanup contract before running. The older host fixture/DummyTransport/private Command body is not this harness.

The real run has one dedicated server and TWO independent client OS processes. Use existing KCP/Telepathy or the final owner-declared production transport, never install another choice. Root's final request must include any necessary task-scoped standalone peer builds; no release build/deployment is implied. Confirm installed compatible player-build support; if unavailable, report that capability gap rather than install globally or substitute host loopback. Build outputs and per-peer persistence/logs stay task-owned. No permanent scene/prefab builder is introduced by this operator.

Reserve/record free task-owned server/auxiliary ports without disturbing existing listeners. Pass one run correlation ID, snapshot SHA and unique peer IDs. Establish observed Connected -> Authenticated -> Ready -> PlayerSpawned barriers with real-time deadlines, record process role/connection/netId and receiver acknowledgments. Aggregate structured RECEIVER payloads/counts, not merely sends, server state or process-alive status.

Required scenario map includes owner A allowed / B denied; unknown authority enums fail closed; local-only queued ingress rejected before budget; independent per-peer limits; two concurrent Counter Add(1) from10 converge to12; routing ServerOnly/AllClients/OthersOnly exact recipients; selector complete/additive snapshots and late join; property/reactive sync including initially null instance, unchanged polling cadence and pause; handler replacement/disposal/reconnect; Money/RPG late state; actual context relay target and collider events. Runtime owners define each supported expected payload/order in their receipts; unresolved scenario setup is a harness gap, not permission to infer success. Host-only tests can retain their narrower labels.

Each peer flushes an end/result record, exits or is owned-cleaned on deadline, and releases its own transport/objects/ports. Preserve first failure plus cleanup failures and confirm no task-owned peer remains. Required network evidence is blocked if only one client, shared memory/private received-method invocation or no production transport actually ran.

## 8. Interaction aggregate cost and parity

Apply [INTERACTION-QUERY-DESIGN.md](INTERACTION-QUERY-DESIGN.md) and final B/C contract decisions. Existing/owner-authored instrumentation must count REAL native physics calls, cache hits, growth retries/fallbacks, invalidations and unique LOS separately, plus relevant allocations/retained contexts. A counter increment in a mock selector is insufficient.

Run 1/100/1000 targets for stable warmed epochs. Identical raw contexts cost K queries independent of N; Both, distinct ray/camera/viewport, dimension, physics world, true query mask and applicable trigger/global filter are separate contexts. Different target tags/eligibility/rig post-filters do not split identical queries. Callbacks/invalidation create legitimate new epochs and their extra calls remain in total counters; do not disable range events/checkObstacles/unique LOS or defer UnityEvents to improve the number. Report CPU/allocation observations separately from a functional K-vs-N assertion.

Compare outcomes/event sequence to a COMPLETE uncached native-query reference, not the old truncated-16 result. Exercise 17/64/1000 hits with target/blocker ordering, growth/reuse and all retry/fallback calls visible; never accept a saturated prefix as complete. Cover 2D/3D actual overloads/global trigger flags, masks/tags/blocker layers, source-rig/explicit/nested targets, ties, missing/disabled/split cameras and Mouse/Center/Both. Preserve independent directed LOS. Include before/after-callback invalidation, nested requests/throws, blocker disable/layer/tag change, pooling/re-enable, explicit Simulate/SyncTransforms invalidation, manual fresh range calls and bounded cache retention/subsystem reset. Borrowed arrays cannot be read after callbacks reuse them.

Ability Target routing separately needs real 1/100/1000-unit invocation/snapshot counters and global/matching registration order, snapshot capture after specific callbacks/before internal reactions, nested Clear/unsubscribe/reentry parity. It is not a physics-query benchmark.

## 9. Real capability receipts and limits

Batch import and logic tests do not establish visual/native/device outcomes. Import/logic jobs may use headless flags; graphics evidence requires an actual non-null graphics device/render target and a reviewed capture, not `-nographics`. Keep background/hidden operation and use existing capability drivers; no visible user-owned Editor automation. Capture transient tweens/VFX at a measured active state and assert the actual transition; a resting screenshot does not prove animation.

Maintain a compact outcome matrix for native enable/disable/destroy/domain-reload modes; actual 2D/3D physics callbacks; UI raycast/hit/input-module interaction; Input Legacy/Both/New-only and missing/reconnected devices; camera/material/URP render targets; audio lifecycle/mixing versus audible output; NavMesh; actual Editor binding/Undo; real imported sample flows. Public handler invocation is labelled as a seam, not real input. AudioSource state alone does not prove audible output; physical touch/gamepad/haptics/audio devices and optional HDRP/Spine/Cinemachine/vendor/AOT platforms need their actual capabilities. Do not install absent vendors or silently change main handling/render/project settings. Unsupported/unavailable checks remain explicitly unverified with owner/action.

Use isolated profile changes for input/reload/render variants and record them. Existing native fixtures/approved harnesses drive behavior; missing automation goes back to its original test/module owner. No new ad hoc operator harness, scene builder, source rewriting or low-value no-throw tests.

## 10. Critical integration conditions to close before final execution

1. Root publishes the final global freeze and exact snapshot/receipts AFTER all writers and B/C follow-ups, including package version/docs/metadata. Current approval of a contract is not that freeze.
2. Primary test owners provide an early environment/persistence receipt and a runnable, nonempty selection map; effective company/product/store roots must be proven before mutation.
3. Root/runtime/test owners resolve the REAL optional Mirror dependency graph and auto-symbol installer behavior. Current hard references/global defines mean off cannot be promised by deleting one symbol. No host-only asmdef workaround.
4. Supplemental peer driver/build entrypoint, supported transport, three-process persistence isolation, installed build target and real receiver schema must exist before the peer stage. Its former plan remains a proposal until the implementation receipt/source exists.
5. Interaction owner supplies complete uncached parity driver and native instrumentation with stable-epoch versus callback/retry/LOS budgets; per-target tests alone do not satisfy aggregate acceptance.
6. Release-consumer driver performs native UPM/sample import and all sample/CMF paths resolve without duplicate source or broken GUID references. Developer folder activation alone is insufficient.
7. Cross-owner APIs/fixtures are integrated: Text base consumers, UTC/clock restore, pool generation/Draw/Demo ownership, Quest unregister/Demo cleanup, Condition occurrence/Editor authoring, Core/Abilities/RPG mirrors and handler/counter/selector/peer scenarios. Missing consumer compilation or a skipped whole fixture blocks that boundary.
8. Record available real graphics/input/audio/NavMesh/optional-backend/build capabilities and exact .NET/checker commands. An unavailable capability must have an honest unverified disposition; do not convert source review, host-only routing or zero tests into a pass.

After the gate opens, attempt a failing/unavailable required path at most twice, with a concrete corrective action before retry. Return exact evidence/owner/blocker and continue independent safe checks; a required unverified path prevents full acceptance. Root personally reads the updated `UnityVerification.md`, XML/log/peer/query/capability receipts and original implementation reports. Keep verdicts scoped to actual version/profile/snapshot. No commit, release, publish or main/user settings change is part of verification.
