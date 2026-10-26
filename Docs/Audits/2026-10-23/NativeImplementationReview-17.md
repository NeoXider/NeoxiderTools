# Native implementation review 17 — corrected interaction and primary Editor tests

Status: **SOURCE REVIEW COMPLETE; one bounded correction remains (NI17-01/P2)**. I6-01/I6-02 and the conservative E6-01 correction are supported in source. NI10's named native/YAML split, native asset identity selection, async failure cleanup and two legacy state-duration migrations are supported; the generic native scene case still bypasses identity selection. No compiler/import/discovery/test/native/profiler/network acceptance was executed.

Reviewer: SAME `/root/audit_abilities`, native `gpt-6.1-sol` / `max`, repository `D:/Git/NeoxiderTools`, target `demo` / `11.0.0`. Only this Markdown is written. No product/test/public-doc/other-report/config edit, Unity/MCP, checker/validator, service/process management, installer, child, commit or push was used. The global writer/execution barrier remains closed.

## Entry gate, coverage and limits

The latest complete receipts were read first:

- `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Tools.InteractableObject.implementation.md` declares source-final; original owner `neo-audit-1023-b07-03`.
- `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Editor.Tests.implementation.md` declares SOURCE-FINAL/no pending source API; original owner `neo-audit-1023-b03-04`.

Then read the complete NI6/NI10 reports, current authoritative interaction design, current appended interaction request and native review contract. Receipt/report identities were first captured at **2026-10-23T20:29:47.112746+00:00**. Prior-fingerprint reconciliation at **2026-10-23T20:30:42.629795+00:00** found **60 of 75 scoped prior entries unchanged**. Sixteen additionally admitted fixture/metadata/report paths were captured by **2026-10-23T20:31:09.254480+00:00**, producing **91 unique gated files**. The post-read gate at **2026-10-23T20:38:56.850086+00:00** and final handoff gate at **2026-10-23T20:43:32.838548+00:00** each matched **91/91** raw identities. The final gate also matched all nine additional context fingerprints. No scoped source-final identity changed during review; a later edit requires renewed scoped review.

Changed interaction source read: complete current `InteractiveObject.cs`, `InteractionRayProvider.cs` and `InteractionPhysicsQueries.cs`; all changed/new SharedQuery, Lifecycle, PlayerLoop and InputDriver bodies/setup/cleanup; all three changed guides and admitted metadata. The target's unrelated network tail was read as surrounding dispatch context, without renewed remote approval. Unchanged relays, Toggle, SceneSetup, Core/math, original interaction fixture and four other guides retain NI6 coverage by exact matching hashes.

Changed primary-test source read: complete SampleAssetValidation, SampleSceneCoverage, Paths, new SampleEvidence fixture, Typewriter fixture and StateDurationPredicate fixture; their phase changes, all new meaningful controls, Testing guide and metadata. Both existing test asmdefs were read as reference/visibility context. The remaining matching primary fixtures/supports/host retain NI10 coverage; no full unrelated re-audit or renewed approval of broader Play/Network tests is implied. Runtime StateMachine setter, Bonus override and Input compatibility / installed optional Input System members were read only for exact dependency context.

All cases described here are **authored source controls, UNEXECUTED**. Abilities/RPG and other owners' runtime behavior, separate-peer transport, actual native capability and pending provider/consumer integration are outside approval.

## Confirmed residual — NI17-01 / P2: generic native scene case bypasses identity-selected roots

**Owner:** SAME primary Editor test worker `neo-audit-1023-b03-04`. Root independently read the current case/guide and accepted this bounded correction during NI17. Reviewer makes no product edit.

**Exact current path/method:** `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Editor/SampleAssetValidationTests.cs:180`, `SampleScenes_HaveNoMissingMonoBehaviours`. Line 185 consumes `EnumerateSampleScenes()`; that helper at line 286 delegates to `Audit20261023NeoEditorTestsPaths.EnumerateSampleFiles`, which uses all discovered SampleRoots rather than the exact native identity selector. Hidden paths are skipped; lines 197/210 require only a positive aggregate inspected count.

**Concrete reproduction:** current-version Demo Scenes has valid visible developer/imported scenes; NeoxiderPages and Character Movement Fundamentals exist only in hidden Samples~. The generic native scene case opens Demo scenes, skips the two hidden identities and can report success. A developer plus imported Demo duplicate repeats that same identity. The corrected all-identity selector at `SampleAssetValidationTests.cs:317–333` is never used by this case.

**Expected / actual:** the Testing guide's full native selection contract requires one current root for each exact manifest identity, with imported preference and deduplication. Missing a required visible identity must be explicit Ignore/inconclusive. Actual generic scene admission can succeed after only one visible identity. This is a source-confirmed false-evidence path, not an executed result or a gameplay defect.

**Smallest approved repair:** route only this native scene case through the existing full exact current-version identity-selected native-root/AssetDatabase helper, preserving imported preference/deduplication and nonempty actual native scenes. Missing identity => Ignore; actual visible scene load/type/script failures => fail. Keep separate hidden YAML packaging nonempty. Reuse existing resolver controls and add a narrow scene-path positive/missing/duplicate control where feasible; the nine current SampleEvidence controls test the selector but do not catch this bypassing caller. No framework, layout rename/copy or unrelated asset-validation expansion is required.

**Unexecuted acceptance:** all identities visible => selected once and actual native scenes inspected; duplicate imported/developer roots cannot cover a missing other identity; missing identity is inconclusive; hidden-only YAML remains separately validated; admitted broken native assets/scripts still fail. Return the SAME owner's later source-final delta for review before execution.

## Interaction correction disposition

| Item | Source conclusion | Boundary |
| --- | --- | --- |
| I6-01 automatic lifetime/reset ownership | Corrected in the reviewed local automatic path | Actual native input/lifecycle and remote dispatch remain unexecuted |
| I6-02 live disabled rendering camera | Corrected without a new camera-eligibility setting | Null/zero viewport and disabled-provider selection remain separate |
| E6-01 planar argument round-trip | Original Vector2 is retained in key and both native/fallback calls | Previous NI6 concern was unproven native failure; do not relabel it as a demonstrated blocker bug |
| 1/100/1000 measurement source | Twelve actual PlayerLoop scenarios and direct native/counter controls authored | No measured physics budget, allocations, CPU or FPS result exists yet |
| Existing shared raw context / policies | Preserved in changed-source trace; unchanged leaf/relay identities match NI6 | External world invalidation and unique LOS remain explicit costs |

### I6-01 — reset generation, commit ordering and keyboard tail

`D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/InteractableObject/InteractiveObject.cs:232` bounds reentrant automatic Update with depth/finally. UpdateInteraction captures the target-local `_inputResetGeneration` at line 255. Continuation checks after hover/mouse/range/keyboard stages require Unity liveness, the same reset generation, interactable and active/enabled status (`CanContinueAutomaticInteraction`, line 326). A disable→enable callback therefore ends the old operation even when availability is true again. Ordinary physics-epoch invalidation is deliberately not that cancellation token.

`ResetInputState` at line 342 advances reset generation and clears range, press/held, hover-hit and double-click bookkeeping before hover notification. Range state is committed before its observer at lines 295–301. Mouse input at line 492 commits sampled held/press state before down/up observers; mouse-up keeps accepted press locally, clears stored press state, checks lifetime/input enablement, then refreshes geometry before click if callbacks ended the query epoch. The caller also stops before range/keyboard/debug work on a destroyed/reset target.

`ProcessClickEvent` at line 980 commits frame/double-click history before TriggerClick; a nested OnDisable reset is no longer overwritten afterward. `UpdateKeyboardInput` at line 689 captures actual down/up edges, checks lifetime/input enablement after down and reevaluates aim/range for a same-frame up. Manual `InteractDown` / `InteractUp` / `Click` still use only their documented interactable gate; their disabled/range/aim bypass was not accidentally removed.

The new Lifecycle source exercises real automatic frames, not a fabricated input result: nine mouse down/up/click × disable/disable-reenable/DestroyImmediate cases, two range-enter reset cases, range-exit reset cancelling an otherwise eligible keyboard tail, four same-frame keyboard-tap mutations, ordinary hover-epoch input continuation, two double-click reset cases and disabled manual-API positive/negative controls. The retained hover-disable case gives **21 authored lifecycle Play cases**; all were read. A working prior mouse-down / native keyboard edge, ordinary down+up tap and ordinary double-click before/after reset prevent a no-input/no-op implementation from satisfying the cases. Assertions observe final range/click/down/up counts and liveness, including next enabled lifetime entering range again. Existing UnityEvent listener iteration is not claimed cancellable.

### I6-02 — geometry remains usable with rendering disabled

`D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/InteractableObject/InteractionRayProvider.cs`, `TryCreateRay`, now tests live reference and positive pixel dimensions, without Camera.isActiveAndEnabled. Current component/provider selection policy remains distinct: FindOnMainCamera still requires an enabled provider; Core camera retention is unchanged by hash.

`D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsInteractableObjectSharedQueryTests.cs:294` first proves working enabled-camera provider hover/click rays and cached target hover, disables only the Camera, compares exact ray geometry and target hover, then checks zero viewport and camera destruction. The prior disabled-provider control remains in the unchanged interaction fixture. These are useful parity controls but not native pass evidence.

### E6-01 — exact planar arguments and native parity controls

`D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/InteractableObject/InteractionPhysicsQueries.cs:194` stores original PlanarOrigin/PlanarDirection separately; equality/hash include exact Vector2 components at lines 247/268. Both planar overloads at lines 398/410 construct a default 3D-ray field and pass the original vectors. Execute at lines 505–552 sends them unchanged to legacy/nonalloc or explicit-filter native calls and the corresponding complete fallback. No additional normalization occurs. Camera intersection queries still use their original 3D Ray.

The ten cases at SharedQueryTests line 338 cover legacy/explicit filter × unit/nonunit/tiny/zero/projected large-Z-small-XY. Their oracle directly calls the original adequately sized native Physics2D array overload, then compares actual collider identities, hit distances and points with cold/warm/cached backend results. Unit/nonunit controls require 17 actual colliders. Tiny/zero outcomes and cold fallback counts follow the actual native oracle rather than assuming a particular engine result. The actual large-Z checkpoint/Collider2D public range case at line 403 compares original explicit-filter native LOS with cold/warm target range; line 450 ensures distinct original planar directions partition keys. Together with the disabled-camera case, these are **13 additional SharedQuery Edit cases**, all read and unexecuted.

Existing complete-query 17/64/1000 fallback and target/blocker controls, explicit-filter/global trigger distinction, actual masks, different cameras, fresh range, callback mutation/fault and unique LOS remain substantive in the current fixture. They were read as surrounding controls, not replaced by test-presence assertions.

### Actual PlayerLoop measurement source, and what it cannot prove yet

`D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Play/Tools/Audit20261023NeoToolsInteractableObjectPlayerLoopTests.cs` adds **12 cases**: 1/100/1000 distinct physical collider targets for 3D/2D camera and keyboard look. It never manually invokes target Update. All targets are enabled and physically collinear; obstacles are explicitly off to preserve the existing multi-target mechanic. Keyboard length is configured identically, and distance checkpoints are explicitly removed for the separate stable-context measurement. Camera cases do not require the optional input backend.

After a real warm frame, positive frames require all targets hovered/accepted-down, one actual native query, N−1 cache hits, N local classification passes and N×N raw hit visits. Moving every target off-ray retains consumers while requiring false states, one query and zero visited hits. Unique LOS, callback epochs and inside-callback native counts are separately asserted zero in that stable measurement; other existing cases cover their genuine extra work. These controls would reject a single-selected-target cache, an empty-result fake or merely sharing Ray construction. They still need actual run/profiler evidence.

`Audit20261023NeoToolsInteractableObjectInputDriver.cs` queues native optional Input System KeyboardState/MouseState to owned virtual devices, with real Dynamic PlayerLoop processing. It does not supply fake query/input results or invoke target Update. Reflection member source context was checked in the installed package: AddDevice(string,string,string), QueueStateEvent<TState>, KeyboardState(Key[]), MouseState fields, MakeCurrent and Dynamic update mode exist. The driver restores previous update mode, compatibility selection and current devices, removes only its own devices, and explicitly skips if the optional assembly is absent. Absence is no lifecycle/keyboard pass. Legacy hardware/EventSystem delivery remains unproven.

New native markers enclose the actual initial/fallback Physics calls; CountNative is immediately beside each call. ClassificationPasses/ClassifiedHits and projection markers cover target-local traversal/growth; total automatic Update and callback markers distinguish other work. Current exact key still preserves kind, mask, trigger/filter/global/default-world/camera/viewport distinctions; purpose is diagnostic, not a key. Callback/Fresh/external invalidation and unique LOS legitimately add physical calls. Borrowed view lifetime, spare-capacity fallback and high-water retention remain as reviewed; no unmeasured constant total CPU/memory claim is accepted.

The three changed guides accurately state reset/click order, disabled-camera geometry, original planar arguments, marker/counter boundaries, N×H local work and unexecuted PlayerLoop acceptance. Unchanged canonical navigation/relay docs retain NI6 identities. No new required scene wiring/interface/serialized setting or general controller/session framework was introduced.

## Primary Editor correction disposition

### NI10-02 — named native/YAML split and exact selector are corrected; generic scene caller remains open

`D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Editor/SampleSceneCoverageTests.cs:49` adds distinct nonempty YAML packaging for all nine smoke scenes. The RPG packaging case at line 112 requires both actual scene files plus every existing playable component marker/reference. ValidateSceneYamlPackaging at line 353 requires script references, rejects missing/unknown scripts and checks the required marker/GUID. These file checks do not claim scene deserialization.

Named Open/Playable cases call RequireNativeContext at line 459 and require actual SceneAsset loading at line 470; hidden-only/missing visible roots or compiled sample types are explicit Ignore. Admitted wrong active types or missing native assets fail. Existing preview-scene component/wiring/missing-script assertions remain. No manual copy or YAML substitution can turn those named cases into native success.

`D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Editor/Audit20261023NeoEditorTestsPaths.cs:92` selects one exact current-version root per manifest identity, preferring imported over development, reporting missing identities and ignoring unrelated/stale/duplicate roots. FindAssetsInAssetDatabaseSampleRoots now uses it before native asset discovery. The **nine new SampleEvidence cases** cover duplicate roots plus a missing other identity, complete identity preference, stale version, hidden packaging/native separation, four invalid YAML/reference inputs, and missing prerequisites versus invalid active types/assets. The pure helper fixtures are correctly bounded controls, not real imported sample evidence. Their failure to exercise the generic scene caller is the remaining NI17-01 gap.

The Testing guide and receipt accurately describe the intended all-identity policy, but that broad statement is not yet true for the generic native scene test at line 180. This is the sole source correction requested by NI17; no unrelated asset-validation expansion is required.

### NI10-03 — preserve observation failure after yielded mode restoration

`D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Tools/AuditFixesTypewriterTests.cs:217`, StopAndObserveOwnedRuns, catches the first stop/IsCompleted/GetResult fault or bounded nonsettlement while continuing every owned run. It removes each observed awaiter, clears tracking in finally, then actually yields ExitPlayMode before reporting captured ExceptionDispatchInfo. UnityTearDown at line 208 throws that preserved failure only after the helper resumes. Reflection unwraps the original GetResult exception rather than replacing it with TargetInvocationException. An observation failure no longer bypasses the normal yielded exit.

Two new real controls at lines 142/175 retain an actual faulted TypewriterEffect operation and a genuinely pending UniTaskCompletionSource. Each keeps another real typing run, yields the common cleanup, requires Editor mode, empty ownership and the later run stopped, then asserts the preserved original fault/timeout. Pending control is explicitly settled in finally. No teardown enumerator is manually exhausted and no failed awaiter is treated as success. Original restart/cancellation/GetResult controls retain their substantive assertions. Source closure is supported; actual runner teardown/domain reload and fault delivery remain unexecuted.

### StateDuration integration and dependency/reference context

`D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/StateMachine/StateDurationPredicateTests.cs:77` and line 91 now assert successful machine.SetCurrentStateEnterTime(Time.time−10/60) before threshold/reentry checks. The rest of each assertion remains intact; ordinary entry, zero duration and composite-duration controls were read. They no longer mutate only the standalone shared predicate while active machines use their own anchors.

The exact public bool setter exists at `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/StateMachine/StateMachine.cs:172` and updates the active machine's anchor without lifecycle callbacks. This is dependency presence/source consumption, not a full fresh runtime StateMachine approval. The Bonus GetUtcNow override returning NeoTrustedTime.UtcNow is likewise present at CooldownReward.cs:154; NI10-01's former missing override is no longer absent. Complete Bonus/Time behavior and deterministic reward execution remain their own provider/consumer verification scope.

Existing Edit asmdef has UniTask, UI, StateMachine and relevant interaction references; Play asmdef has Input and InteractableObject references. No assembly reference was edited. Optional Input System is reflected, not made a hard test dependency. The broad legacy Mirror/CMF test graph, actual compile/weaving, capability availability and execute-method/sample import host remain unexecuted. Matching ValidationHost/ClockScope/SaveScope/preview and other primary fixture identities preserve NI10 source coverage rather than receiving an invented new execution pass.

## Required acceptance after the source barrier

First return NI17-01 to the SAME primary owner and review its bounded source-final correction. Then, only after root declares all writers finished, obtain frozen-snapshot compilation/import/discovery and actual runs on the intended Unity/MIRROR profiles.

Required focused evidence includes interaction automatic mouse/range/click/reset/keyboard tails, live disabled Camera parity, direct native planar cases, all four real PlayerLoop matrices, dense complete results, distinct masks/policies/LOS, frame/callback/Fresh invalidation and aggregate profiler/native/cold-warm allocation/classification measurements. External EventSystem physics is outside backend counters and must be counted separately. Isolated/exclusive cleanup, owned input devices/camera tags/global physics flags, domain/scene-reload-disabled lifetimes and optional-backend skips need real verification.

Primary evidence must include nine resolver/packaging/native-prerequisite controls, nine+two actual scene YAML packages separated from native Open/Playable/identity-selected scene runs, both async failure cleanup controls with actual Play Mode exit, two migrated state-duration cases, dependency-integrated deterministic reward cases, and genuine UPM imported/native script/reference results. A helper/source assertion or test inventory is not native evidence. Existing historical diagnostics/passes do not certify these bytes. Large controller/transaction/support/session proposals remain optional; none is a prerequisite for the one bounded residual.

## Frozen scoped identities

Raw SHA-256 includes BOM/newlines. All **91** identities matched the initial-to-post-read gate. First table means changed versus the prior report, or newly admitted to this follow-up ledger; it does not mean every listed path was newly created. Second table is the **60 exact prior matches**, whose earlier NI6/NI10 coverage is retained rather than reaudited. Any new writer invalidates the affected scope.

### Changed or additionally admitted read identities (31)

| Full artifact path | Bytes | SHA-256 |
| --- | ---: | --- |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Tools.InteractableObject.implementation.md` | 18772 | `ee62cac2c9e789aa9ad6ed79eafbb29e642dde77f9087bdf0ac43a20dea7fc2a` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Tools.InteractableObject.implementation-request.md` | 11753 | `19fe667e91354ec9797ec29438890b2cb837a0d7085bca8c44243466aa74e7ee` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/InteractableObject/InteractionPhysicsQueries.cs` | 27446 | `bb19f00bff9da709c21e396b204f57828cbafa4495d41410d4048265d07d5885` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/InteractableObject/InteractionRayProvider.cs` | 6529 | `0504766e4853f5a4442fe1b32ec49cd614660cbfae7df7fd284794560dfc3f8e` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/InteractableObject/InteractiveObject.cs` | 62445 | `bcb49d73786346f38f7b369396e82e16ad146e37e76b2dfb6d5b73807780b105` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsInteractableObjectSharedQueryTests.cs` | 39582 | `5095512a8cba91b5b7f7db8429b8917dbbbc53ec3caa019a786567c88128d245` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Play/Tools/Audit20261023NeoToolsInteractableObjectLifecycleTests.cs` | 21631 | `3b571173e50e8be200cf291c33320a991df009d17bc3bf4f67474395dcd9282f` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Tools/InteractableObject/SharedQueries.md` | 8619 | `393feff776efe31037a42d0190e32caf2144903db7edd1218475e505c7f623f8` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Tools/InteractableObject/InteractiveObject.md` | 14187 | `8f848ec2bfb164085f24bac88b1181d8dfc29234d521d166540a88e26a5a75f5` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Tools/InteractableObject/InteractionRayProvider.md` | 3336 | `d314b2d11715739530764c29ab5f97dcdff83af6f429c99cf5d12e9ef9fe8a5f` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Editor/SampleAssetValidationTests.cs` | 17514 | `15473e798e47209707dba5f400da22c421b3e415fe6b447df08e7f9b75303e21` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Editor/SampleSceneCoverageTests.cs` | 19718 | `a29767467faeb659d5d5697bc5d88b06b47d9c70d69b63004188f601b9379ec9` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Editor/Audit20261023NeoEditorTestsPaths.cs` | 7339 | `586593af17694e6bfe3cb922392139aab830602e57c6f05d501c629a0f1188eb` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Tools/AuditFixesTypewriterTests.cs` | 13370 | `71450b95a7a168b2920b371524aa08b9848a9a9b7bbe62b8e60e9b205ec20b89` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Testing.md` | 11956 | `4391acbd0bd7e0ab2801fe648b7641691297fdcf8362dd39bf2c1e755d0e3945` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Editor.Tests.implementation.md` | 16725 | `4f0b7ad624e8ceb0b92866d2497fe127c19059e5a59d5a24b771a6aae53ba46b` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/NativeImplementationReview-6.md` | 30922 | `1a4a9f89dcd2a2ccc7b21e2dcf999485d4f6992674f9b3044f6082dbb2ae7765` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/NativeImplementationReview-10.md` | 32172 | `22f47f49809d2e981e759fc552acdb5299fe1cead143d1ff4ee1319c7292ac17` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/NATIVE-REVIEW-CONTRACT.md` | 4739 | `c1bb559c242ee21a44542abacc013cba2f7955d64c154350a4f336cbd11f1edd` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Play/Tools/Audit20261023NeoToolsInteractableObjectInputDriver.cs` | 6674 | `f76f3a3a7b9cf9577d2098b413c91169799e4fd4b1f4045080343dc1ffb18e86` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Play/Tools/Audit20261023NeoToolsInteractableObjectInputDriver.cs.meta` | 240 | `5de1c00dd5e0f54b8483b81478e33c0fe95c0175f9f585ac9d5ca0d51fd7453b` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Play/Tools/Audit20261023NeoToolsInteractableObjectPlayerLoopTests.cs` | 8000 | `7a2165ff657f59e1549877981c6d19c32cbad71398d26053f0582b938b2dee0f` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Play/Tools/Audit20261023NeoToolsInteractableObjectPlayerLoopTests.cs.meta` | 240 | `1b3f425e4e5cacc2ff1c4058ae358b6bf057a521221fdc21acee4248eeb95c5c` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Play/Tools/Audit20261023NeoToolsInteractableObjectLifecycleTests.cs.meta` | 251 | `df4ed3213d6d42290e2bb0ed522225e56cbf57e88342c17a8529280471aa5680` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/InteractableObject/InteractionPhysicsQueries.cs.meta` | 251 | `2334ff4748f58982f95f36b0bd15f0fd0a9b8ee7e62cc15e34afa4649defde89` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsInteractableObjectSharedQueryTests.cs.meta` | 251 | `401fe8ca85e933c10ff1eec9461fe0c65e24943b4b39d985ef86dd5ee06f866b` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Tools/InteractableObject/SharedQueries.md.meta` | 159 | `ea01732e729ad9ab40ae7e736042576f666295ffc59ca7ff95e3021399929a16` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Editor/Audit20261023NeoEditorTestsSampleEvidenceTests.cs` | 7739 | `23b5f3eb4e79c1de389cd08b7eddc8cd6c96672b1b730fa2b4f3facd047bdbcf` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Editor/Audit20261023NeoEditorTestsSampleEvidenceTests.cs.meta` | 240 | `f71bf93ff5df7156f75378f083a4276265c13291b887fc004370abd4f777d10f` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/StateMachine/StateDurationPredicateTests.cs` | 4661 | `87fea940630326d7efe1ac7b4d3ea9dc3deb54e320f13c4bd192c47a04829ad6` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/StateMachine/StateDurationPredicateTests.cs.meta` | 60 | `63de94f273de7c97a2d1f230e48983e8a4fef67fd507982c9d23ad835945e7e7` |

### Exact prior matches (60)

| Full artifact path | Bytes | SHA-256 |
| --- | ---: | --- |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/INTERACTION-QUERY-DESIGN.md` | 18600 | `d64726990331b14b8e6614d231a482378fa9cee85ee63e4ef095d76c1a75f6f1` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/IMPLEMENTATION-CONTRACT.md` | 15329 | `d5bc7a2d934e20f40647c45d53746c3734a1dbbfe95f182600d66977e9bbcbab` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/NativeReview-B-extra.md` | 25342 | `95b1df741f09abdd60a69522b162fa3947bdd98660401bb3e175bef2cf5670d2` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/InteractableObject/PhysicsEvents2D.cs` | 10437 | `93e0d4036b376fc4fb273b595bd3653a583da4f6b2adc2e5bcdf2cec9d7474d5` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/InteractableObject/PhysicsEvents3D.cs` | 10862 | `e3f3852b0a9512e4f393219be542b28c0e29004953e74c20f65b78c1da9c488f` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/InteractableObject/ToggleObject.cs` | 1703 | `1455dea38f77dfc697d719ac2a2aaa65551f792b8bd6b82d2cfed957e3164a13` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/InteractableObject/Internal/InteractiveObjectSceneSetup.cs` | 6954 | `e31f36afde2d160779e9068f5e278b6cab82f6186fd046ac7921f689857d0168` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/InteractableObject/Core/InteractionCameraResolver.cs` | 804 | `bfc77d156d998c289411e258c6af661631331e10f288f7a9f511e8f3cd82eca0` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/InteractableObject/Core/InteractionQueryMath.cs` | 4234 | `50b93c0856073f866ba10c58c52dc3b79dc176b8115c0ab5a14156578e082423` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsInteractableObjectInteractionTests.cs` | 17571 | `a51994352c18c83f9dd3cc856cd0fae46449ea59b4512e6ec41930e8ff03f222` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Tools/InteractableObject/README.md` | 3820 | `8f12a12004a90192b6a98b59d23a4d46b3197429b5bb20106b9cfcf19fddf247` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Tools/InteractableObject/PhysicsEvents2D.md` | 3575 | `98f6f94cc729f95a04e9f62a16df15e7d7d354607cdc8c6daf4a9b6429a216d5` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Tools/InteractableObject/PhysicsEvents3D.md` | 3679 | `650658397755c8a8f9e97cbc2e13c55ba4c5c4e7e60b12a80917df6c2e000f27` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Tools/InteractableObject/ToggleObject.md` | 2906 | `7a0581a1504a334c0eaab6bc973da58f2223cfa548a4f7857deb80d6585b7e5f` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Animations/TransformAnimatorTests.cs` | 14972 | `f8b0030739c10ce0048b0624edb88906f3983281037cc1706e2f47c4e9c65eb7` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Audio/AmChannelEditModeTests.cs` | 37011 | `99ce808027bbf6645c9134511649cae1702278a4ff6b29ea06b41ea408af9376` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Audio/AmEditModeTests.cs` | 22215 | `1dd971be286936741af7ec4224c52dd2b008cf9707f8a9097edde082ffc9b792` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Audio/AmEntryEditModeTests.cs` | 26390 | `9b1eb77ee55126a9978bcf784cb3236d0378263b4f9239ebf536e14ef29e6225` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Audio/RandomMusicControllerEditModeTests.cs` | 4058 | `f101829e97d5b68751f676e1d591ee8464830d851824835199f1e4c3aaf69702` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Bonus/BonusSystemTests.cs` | 4430 | `551d82244154c08944608da42a2c7c16937dda83bf72465c684e0ed65af6a433` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Bonus/CooldownRewardAutoClaimTests.cs` | 3466 | `a32baff5eadf78c788d967d9d8be29cc9427c64f2241ad4810d9617d0e94e75d` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Bonus/SlotEconomyDefinitionTests.cs` | 7178 | `918f39b6a6baee8bb1f756cf3f28e84faded35f6dfb1602e73683e504ca73616` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Bonus/Audit20261023NeoEditorTestsClockScope.cs` | 2356 | `9e5852c9a2c9ff63d94f6e6a78c88b50e0f30fb0eea1157a5ba27a77c497d4cb` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Cards/CardSystemTests.cs` | 28018 | `ee095b18a7eb07cd2b25c192380280de52465f889a3a839e2d9f0d5986b113b0` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Editor/Audit20261023NeoEditorTestsIsolationTests.cs` | 29711 | `2f1344626b2c815ca385e4454a6456464902656d25e0ce79bbfa236009da3d86` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Editor/Audit20261023NeoEditorTestsValidationHost.cs` | 24358 | `95d5e0628c6186ac954407a91323617e3258174f56ebe31464c2b040cd8736c7` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Extensions/ExtensionsTests.cs` | 32233 | `27b183641dbb6bdafcb9cf8549be3268f3503bbe0e8d2b644e69684adedd5c66` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Extensions/UnityExtensionsTests.cs` | 11906 | `5a1693313ec3ee0e0154fe289e0fcab1841307d990f0ecbd696a486ddb808aa6` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/GridSystem/AuditFixesMatch3BoardServiceTests.cs` | 5350 | `52d9639aecf36a41f1b7d689b4435eba465d733429f88c98303463bbc0807c6c` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/GridSystem/Match3BoardServiceCoverageTests.cs` | 1858 | `3ab7e250a76ac1b15024b3b7f1ac2ee9494caf46504af3b23fe2466937206533` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Level/SceneFlowControllerTests.cs` | 6315 | `09df5a629e24472eb2eafa2afe7485ee770877e7ae26aaf2d2c5af8f26c34c53` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Progression/ProgressionManagerTests.cs` | 28132 | `04829a82ccfb2a9389a5000c3114e113002b77e6c7539b9784ced661c1eee537` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/PropertyAttribute/InspectorControlIdStabilityTests.cs` | 8662 | `69ad3fb2554b4105a05f7a80693f0d5d282d0883192bd8335eacf848f42ce7f6` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/PropertyAttribute/NeoInspectorAnimationTests.cs` | 12324 | `ab8d517b5ecdd31d3c406eff5b6444b517cbadca564a94b91114861b853e4b4e` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Shop/MoneyPersistenceEditModeTests.cs` | 6993 | `9e12573e187dcfb564b963a64cfd2ed16e6d4ceb6db46830d12a7e1e80f13220` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Shop/ShopAffordabilityTests.cs` | 13204 | `3b2437b9360d657852af2181533acae6e4514c320e5013c225fffe00b003d2e5` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Shop/ShopPurchaseRollbackTests.cs` | 8203 | `dadb9d4f48c3b7eeae97b40683f75026aafb6feacf15cec25e5322e412b750cd` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Shop/ShopResolverTests.cs` | 2434 | `7fb2d8cf9d865efe32fddf8db9e7bc078af01e3d394501da2030557f2d38ccdd` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Shop/ShopVariantsPanelTests.cs` | 8560 | `3493f983e7ef8c3ed8c88198bb18ca25cdf5f854da8ceec2b9111c9dec7642e0` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Tools/CursorLockPlayerControlTests.cs` | 8428 | `4770cf38d762318b2a54de2a4b6cc273572e180ce0aeace012bb857142a6a718` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Tools/TimerTests.cs` | 9371 | `ab6f4f664a355212196e5d176ae04fe59e306368f7b316033a25be23c74d9c98` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Tools/NeoConditionThrottleTests.cs` | 4000 | `3563347dcf957cc65f1a7e6d3ccbf9109a22ea45d339a84fcb3a07669401bddd` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Tools/NeoConditionTests.cs` | 10035 | `a337c79a92d8a42d3f4668b109c45b7f6bb5a8d8fe89002c4c652fa3372a74b6` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Tools/AuditFixesSpawnerTests.cs` | 3801 | `ff83dc7d44e8a37bfa7eb7d0dc487c8b1a237ecef48e4bae49706f810e99b840` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Tools/Move/TransformMoveTests.cs` | 4967 | `c924b5eda451ff0e381a2d2dff3086a8f22349e1721076cc1eb74a8ced0c04b0` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Tools/InteractionQueryMathTests.cs` | 6452 | `210e83f506d641186521d540f3e8e59f912b3716e78adfb8e6c9eccc47beb079` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Tools/MoverPrefabColliderOffsetTests.cs` | 4220 | `2f239ca34d4a55ebdb14bac313b31c610ba02fff06578213246d0dae31c21f10` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Tools/SelectorTests.cs` | 25594 | `f41b801873281e10f3e3fd77c8d1cd078876a5bb802a181f180637c7407bfffa` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Tools/Other/OtherToolsBehaviorTests.cs` | 5828 | `586f21ce787c131097334be5ee061d81b7850909efc7b5ab511b0b248ed4a92c` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Tools/FakeLeaderboard/FakeLeaderboardBehaviorTests.cs` | 4600 | `e57604896b744538cb4b4673e1ee20ab933564a544db8abf90f5232a2dbf56da` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Tools/ToolsComponentsTests.cs` | 3645 | `a628de80a34c12e734f704800d106da705e7068dbad40e15dc6753b6bd93acf2` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Tools/Random/ChanceManagerTests.cs` | 4801 | `8c1a652db7f008288c2014b71fcbc296849cb844b11d35a6403272a48843a8c0` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/UI/AuditFixesAnimationFlyTests.cs` | 3317 | `07c0f6f8e35c2f465d7d8e97e33f8cb8c90f4c729c06cb95e948daed32ba257d` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/UI/UIMeshRigEditorTests.cs` | 24576 | `ffc6f68f2ecc0248a35747045cbee5ad7da7f96493365a887883e1dbfd540d6e` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Save/Audit20261023NeoEditorTestsSaveScope.cs` | 1562 | `91353881cf6b61059bc59d3f7421241ccf1aa1a3b54db2a5ce0c96152cb3d43c` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Editor/Audit20261023NeoEditorTestsPreviewSceneScope.cs` | 788 | `5d201b58807f061d2d6076fb49550a891b41c12edae7147a19acd72b585b68cb` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Bonus/Audit20261023NeoEditorTestsClockScope.cs.meta` | 240 | `8b8de342878076e7f2988024671e0d3768e091161e888e4e76adbc99b114ee13` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Editor/Audit20261023NeoEditorTestsPaths.cs.meta` | 240 | `ae7e33058f16d538a76e33c90e8768b9f8add8940a1eefce1731e7bb64818139` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Editor/Audit20261023NeoEditorTestsValidationHost.cs.meta` | 240 | `661a0e8d6fe8fa725956875664a8005bc496a4aa562041a9e5707d035704e96f` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Neo.Editor.Tests.asmdef` | 2098 | `204a5ba6382792131c48cae74f38e27f5431909c0c0866c3b2905be0f2c4b4a5` |

## Additional context-only identities

Post-read checkpoint **2026-10-23T20:39:53.585860+00:00**, rechecked unchanged at the final handoff gate above. The Play asmdef was read fully; other paths were read only at the dependency/backend/member bodies described above. These hashes do not claim full independent runtime/package-cache audit or execution. No dependency still in another owner's implementation is approved here.

| Full context path | Bytes | SHA-256 |
| --- | ---: | --- |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Input/InputBackendCompat.cs` | 1556 | `7239385234ae3ee189d3339e4cd6f10701ae6a657dbe7bcc0c70c08e51b09eb8` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Input/KeyInputCompat.cs` | 2158 | `44bfa760fabc48f8cbef61b842003e40d74f527c5a90639b5344674ff03726e3` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/StateMachine/StateMachine.cs` | 23056 | `26211741759424ad2e06943b0f918198823f15259feae48360e1a3909f84d50c` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Bonus/TimeReward/CooldownReward.cs` | 16487 | `0ff8635f120bfb92ab7f3262f8c802160485a8c0e53c9aa53dea33d28025e32a` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Play/Neo.Tests.Play.asmdef` | 1424 | `1856ba6d704ec0852b65cf22f63fdc0427818c49259f6f2032488b02745d62ad` |
| `D:/Git/NeoxiderTools/Library/PackageCache/com.unity.inputsystem@21a28c3a6c83/InputSystem/InputSystem.cs` | 202413 | `c6a32a4cd7af54de398e3d71ee89317d7b69af78176087c409890c7973b731eb` |
| `D:/Git/NeoxiderTools/Library/PackageCache/com.unity.inputsystem@21a28c3a6c83/InputSystem/Devices/Keyboard.cs` | 115601 | `94fd2325b42b93b65530989f955df3a2db09b55dd3aea0c1ae20f6fc11f65df2` |
| `D:/Git/NeoxiderTools/Library/PackageCache/com.unity.inputsystem@21a28c3a6c83/InputSystem/Devices/Mouse.cs` | 15406 | `8129e19978608aadb10f074223700299c9af6c0060f165d71088b1a71738d9b8` |
| `D:/Git/NeoxiderTools/Library/PackageCache/com.unity.inputsystem@21a28c3a6c83/InputSystem/InputSettings.cs` | 55294 | `7f40a1cc06d0f842e36eaa775643b3a07b021c18ff63d7304c0b052f1646eb2d` |
