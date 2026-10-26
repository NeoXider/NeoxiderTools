# NativeImplementationReview-44 — Editor / Level / NPC S3 corrections

Reviewer: SAME original `/root/verify_unity`, gpt-6.1-sol/max. Independent SOURCE-ONLY review. Product/native execution remains stopped; no compiler, Unity/test/checker/service/process launch, product edit, commit or push. This report is the only written source-review artifact; Local d274 recovery reports are separate read-only operational bookkeeping.

**Disposition: scoped SOURCE CLOSURE for the three pinned S3 corrections; no confirmed remaining bounded source defect found. Native acceptance remains UNEXECUTED.** This does not reapprove unchanged whole modules or convert historical S3 failures/interrupted actor exits into passes.

## Exact receipt gate and coverage

Read every line of all three current receipts, actual current changed callers/full Level+NPC fixture source, and every changed source hunk against immutable source3 manifest `2d12ae058b989ce064e86a8d7db8223ecb65241d5dcd19babfb2fd7082adfc70`. Included exact historical failure/oracle context, current shared preview/runtime helper declarations and decisive runtime callback/configuration paths. No still-writing source owner is approved by this review.

| SAME original owner | Receipt lines | Current receipt SHA256 |
|---|---:|---|
|Neo.Editor / `neo-audit-1023-b03-03`|158|`9ea4ae927e5c06604e10638591173d9fd2d7b49b090aba03b04528bc62e0326d`|
|Neo.Level / `neo-audit-1023-b04-02`|160|`865d4124b35d47355a8b988e1cc18eade0fce8c5aa60d941f6412b4485d232ec`|
|Neo.NPC / `neo-audit-1023-b04-04`|185|`57225b60f87717c9d825895d4ba0bde545ce28b8f258676814a812b182434b23`|

## Neo.Editor — E12–E15 caller source closure

`Tests/Edit/Editor/Audit20261023NeoEditorBindingInspectorTests.cs:88–137` retains exact second-collider occurrence1, independent legacy indices91/92, first collider false/second true, post-serialization1f and rejection after second removal. It now proves the original attachable runtime BindingProbe can EnsureCache/read1f BEFORE serialization, then requires restored root/type/member/occurrences plus its own EnsureCache/read. This checks the actual public binding contract rather than assuming TryReadFloat initializes a cache. Primary-extracted helper aliases remain one-way dependencies; no helper was moved by this correction.

Lobby SetUp31–69, MissingScripts SetUp24–67 and SceneSaver CreateSource142–162 no longer create the first scene with additive NewScene against the borrowed untitled context. They save only a NEW owned preview as a scene asset (saveAsCopy=true), close the preview, force synchronous import, assert SceneAsset and open/additively select the saved owned scene before dirty work. The Lobby context folder is separate from its destination, preserving the cancellation assertion that destination is empty.

Original three Lobby/five MissingScripts/one SceneSaver test bodies and nonempty/payload/persistence/repair/backup oracles remain. SetUp catch performs owned partial cleanup; scanner cleanup is null-safe. Borrowed ordered Scene values, loaded/dirty flags, active Scene and preview count are captured before side effects and asserted after cleanup; Lobby also restores exact Selection. Only owned scenes/assets/folders are closed/deleted, no foreign save/close/default reset. Same-basename GUID backups still check both contents/source bytes/dirty paths and repeat-backup isolation.

Production binding resolver, SceneSaverGUI and LobbyDemoSceneCreator bytes match source3; this is fixture correction, not a new runtime/editor API or migration. Existing primary preview scope and runtime BindingProbe are present at the hashes below. Actual preview-save/open viability and the SUTs native additive scene creation while borrowed untitled work stays open cannot be established from this source review.

## Neo.Level — native frame/Start/tween/subscription source closure

`Audit20261023NeoLevelLifecycleTests.cs:26–72` keeps top-level EnterPlayMode/ExitPlayMode and adds a real frame-count barrier, exact loader/diagnostics capture and a GUID-owned runtime scene. Objects move only into that scene; labels/buttons are configured inactive before activation. Helpers364–439 require runtime-world membership, native didAwake/didStart and actual owned manager registration via TryGetInstance. Existing managers fail the arrangement without being reset/deleted; valid finite maps make ordinary selected levels meaningful.

All eight original UnityTest method names remain; both SceneFlow loader/reentry/null-operation/no-completion/next-request cases remain. The six signed integer/offset scenarios, exact Plain/Grouped digits/affixes/last event payloads and selected/public-event paths remain. No manual lifecycle/Update invocation or global DOTween.ManualUpdate remains in this fixture. PrepareOwnedTween463–473 requires exactly one actual active owned tween, its expected playing state, then applies independent Normal/linear updates to that tween alone; five-second unscaled native-frame bounds replace Editor tick assumptions. This is not evidence for unrelated production pause/timeScale policies.

Partial0<value<80, disabled number/text/update-count freeze, re-enable90 and later91 remain. New owned-manager replacement checks native destruction, replacement12, zero old-event writes/tweens and subsequent13. Queued disabled42 still has exact zero/text/count freeze, actual OnEnable resumption and later deferred-manager/level7 positives. Runtime LevelButton still starts with null serialized Button and proves exactly one native Start subscription; after destruction a further click cannot add to `[1,0]`. No assertion is made optional and missing tween/frame is a failed prerequisite.

LevelButton/LevelManager/TextLevel/Map/SceneFlowController runtime bytes and canonical docs match source3. Read actual TextLevel Start/enable/disable/deferred callback and SetText hook/tween-target paths, plus LevelManager selection/public event. No pending source/API/helper handoff found; all scheduling/callback semantics still require actual frozen native execution.

## Neo.NPC — native NavMesh/partial/arrival/rebind source closure

`Audit20261023NeoNPCNavigationTests.cs:23–117` enters real Play, requires native frame progress, builds/registers only its two-island NavMesh for the selected native agent type, samples both islands with that filter, and activates an agent before requiring isOnNavMesh/Warp. CreateAgent530–547 preserves exact registration/type/sample/Warp assertions instead of manufacturing success. TearDown removes only tracked objects/own data and restores captured Random state; no RemoveAllNavMeshData or foreign surface deletion.

All16 old case identities/boolean variants remain. Actual component enabled changes, native movement/positive velocity and target destruction replace private lifecycle reflection; ordinary public core-contract calls stay ordinary tests. The current runtime OnValidate282–305 reconfigures the existing follow/patrol cores in Play when the fixture applies serialized path settings after Awake, so this setup does not silently keep the pre-edit configuration.

Partial follow/patrol still require real CalculatePath success/PathPartial/SetPath, finite settled remainingDistance within the unchanged100 stopping distance, and zero false reached callbacks. Combined manual/update/target-replace/destroy/retry exact counts remain; the unreachable case pauses via the public stop listener only after the first failed attempt, then Resume exposes exactly attempt2. No lower path tolerance was widened; original0.2 destination bounds remain.

Two meaningful controls add18 total navigation cases: complete native path plus actual movement and exactly one requested arrival payload; then owned-data removal proves rejected update/no extra path/arrival, restoration of the SAME data/agent proves rebinding/Warp and second accepted complete path. A foreign surface makes the missing-surface precondition fail; it is not deleted. Three-second unscaled waits require native frames. Runtime NpcNavigation, existing11 component/two initial-mode/eight decision cases and docs remain source3 bytes or outside this bounded correction; historical passes are not new acceptance.

## Current caller/helper fingerprints

Paths below are under `Assets/Neoxider/`; full SHA256 identifies the exact reviewed source. Meta identities were retained per source diff/receipts; no new source asset migration is claimed.

| Source | SHA256 |
|---|---|
|`Tests/Edit/Editor/Audit20261023NeoEditorBindingInspectorTests.cs`|`4dfc8e2deea816d7694d73f7c4eb7a87412d6adced1638b43bf5a35d70cdd03b`|
|`Tests/Edit/Editor/Audit20261023NeoEditorLobbyTests.cs`|`048983d64773438d184d45df98e22522577a95868ea6ab623beb3c92281f7e1b`|
|`Tests/Edit/Editor/Audit20261023NeoEditorMissingScriptsTests.cs`|`fd7043540064ffb94f0e69bb2c01eec52a22fc8daa42ce626adcb49a6a8523c9`|
|`Tests/Edit/Editor/Audit20261023NeoEditorSceneSaverTests.cs`|`9172bc58553a69a11ea835edfff57896e94ffc0444d54a4e5b3da8aad3720c4c`|
|`Tests/Edit/Level/Audit20261023NeoLevelLifecycleTests.cs`|`8c24fad161f980c52050847c96c6423134ad40a8042a0d7ef9330f7578ca4181`|
|`Tests/Edit/NPC/Audit20261023NeoNPCNavigationTests.cs`|`11e972b8b05e7901dcc38ba554d86983d8a7b80a7a9f56be677c0d5fac05eaf9`|
|`Tests/Edit/Editor/Audit20261023NeoEditorTestsPreviewSceneScope.cs`|`5d201b58807f061d2d6076fb49550a891b41c12edae7147a19acd72b585b68cb`|
|`Tests/Fixtures/Inspector/Audit20261023FixtureNeoEditorBindingInspectorBindingProbe.cs`|`c8431097eafb973bee63b94d2043d7b782535747fc7202e6c3530d3014dd54e9`|

Read-only runtime dependencies: binding resolver `3f1a109533c06acf265fc5062bf2154899ea49c6cfc9636795adc3b317ba894e`; SceneSaverGUI `1501ddf7ea9e1914eeba97533f966c52141d4d2ef27e214003ae8d9f79d536a4`; Lobby creator `94c8a2908f16e651ac2f29365e857237d9ebf3f7df8d94be2c17f53fe6cbb018`; LevelButton `106830c3459814f61cda10d12591df4856d0a9e7bc8852d23dbe46b8cf0a4bd5`; LevelManager `7fc63bc89138c80f1079a458d6c89181a08aafb8267c9295fbf3e9be930c7ed9`; TextLevel `4b0374b7385db2fa9ee2db210d0366d383b89925903acee6424fed44e26aaf8a`; NpcNavigation `e477ea8e98d67a415dfa270ff5ff03fc5471ac8d9c1d121d211f4a009d8dec47`. All those bytes match source3. Shared helper/fixture/reference ownership remains primary-exclusive; no further relocation requested.

Also read installed UTF1.6 EnumerableSetUpTearDownCommand and BeforeAfterTestCommandBase: its source catches before-action failures and still executes after actions, and rejects nested IEditModeTestYieldInstruction. Current Enter/Exit placement is top-level; ordinary nested wait helpers contain no transition. This supports source admission only, not a native teardown-pass claim.

## Mandatory remaining native evidence — no launch requested here

After all writers stop/new root freeze/resource boundary: compile both installed6.3/6.6 branches; actual SDK discovery must retain all10 E12–15 cases, eight Level methods and all18 NPC navigation cases/old variants. Execute native Editor serialization, preview save-as-copy/import/open with dirty borrowed untitled scenes, both cancellations, saved/reopened assets and failure cleanup. Check generated lobby room/game prefab/persistent UI wiring/backup bytes and original foreign Scene order/identity/dirty state exactly; real peer workflow remains separate.

Level requires native Awake/Start/frame positives, nonempty unique tween/play-pause progression, all exact integer/text/event/freezing/old-new manager/button/SceneFlow controls, and no missing-target DOTween logs. NPC requires both native islands/agent registration, path-complete/partial/finite-distance positives, movement/arrival payloads, exact stop/retry/destroy/replacement counts and remove/rebind recovery. Rerun unchanged boundary/progress/component/lifecycle/decision controls and full integrated suites; inspect all skips and success/failure teardown/Play exit. No physical input, full-suite pass, supported-platform parity or source3 job recovery completion is claimed.

Unfinalized d274 diagnostics with shader OOM and current resource pressure remain operational blockers, not source defects introduced by these three corrections. No Unity/compile/test rerun, product validation, asset/helper/main edit or push occurred during NI44. Root alone resumes execution at a new explicit boundary.
