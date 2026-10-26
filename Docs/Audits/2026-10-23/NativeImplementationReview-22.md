# Native implementation review 22 — Audio / NPC

Status: **COMPLETE — scoped source approval; no new confirmed correction. All execution UNEXECUTED.** Independent source-only QA, same native `gpt-6.1-sol` / `max` session. Only this report was written; no product/tests/public-doc/config/other-report changes, Unity/compiler/tests/checkers/services/process management/children/commits.

## Intake gate

Complete latest SOURCE-FINAL receipts and approved requests read. Audio receipt SHA256 `884a0e154d5a0647539b01cd47286a879cfa63a4a67bba05ae238ed586976bc0`; NPC receipt `1ac9d9c893abd8f6136aae3db773b93a4e1cfb82ec49dd03e9605ff31b479aa8`. Correct assembly owner is `Neo.NPC`, not `Neo.Tools.NPC`.

Current `Local/Audits/assembly-2026-10-23/cli-liveness.json`, timestamp `2026-10-23T21:15:20Z`, SHA256 `169e5ed14e5c07ec124a9249249b807eacdb012e01593fb94d27b59824edf554`, contained neither exact original actor Audio `neo-audit-1023-b02-03` nor NPC `neo-audit-1023-b04-04`. Read-only tasklist at 21:15:04Z found NPC 57816/25704 and Audio 71668 absent; former Audio recovery PID 38816 was conhost.exe, not the original CLI writer. Combined with root's explicit fresh exact-owner absence confirmation, both scopes were admitted. No scope that is actively writing is approved by this gate.

Closing liveness, timestamp `2026-10-23T21:20:44Z`, SHA256 `2b3c764cb0c0dabc24b06c0eed2994430d01a1568e3a07d05aa0aec869514fd9`, again contained neither exact owner. Read-only tasklist at `2026-10-23T21:21:38Z` found all four recorded PIDs absent. Receipts, phase runtime and fixture hashes stayed stable. This is an exact-owner source gate, not a declaration that all global writers have finished.

## Complete read coverage and ownership

Read the complete original Audio/NPC audits and plans, current approved requests, latest full source-final receipts and relevant same-session implementation/native contracts. Truncated batched artifact reads were reread in bounded portions. Reports were used as leads; current control flow and deltas were inspected independently.

- **Audio:** all six base-diff changed runtime files were read completely: `AMSettings.cs` (626), `View/AudioControl.cs` (548), `RandomMusicController.cs` (265), `AudioSimple/AM.cs` (2798), `AudioSimple/AudioEntry.cs` (471) and `SettingMixer.cs` (218 lines). Every current runtime hunk against `ca198556` was read. Both complete prefixed fixtures (Edit 454 / Play 614 lines), all new and retained case bodies/helpers/cleanup, and all eight changed guides were read. Four runtime files and six guides belong to the latest implementation phase; AudioEntry, SettingMixer and their guides retain the earlier audited corrections.
- **NPC:** all seven base-diff changed runtime files were read completely: `NpcNavigation.cs` (580), `NpcNavigation.Behaviours.cs` (156), `Combat/NpcRpgCombatBrain.cs` (427), `Navigation/NpcFollowTargetCore.cs` (255), `NpcPatrolCore.cs` (290), `NpcAnimationCore.cs` (53) and `NpcTargetFinder.cs` (158 lines). Every current runtime hunk was read. All three complete prefixed fixtures (Component 286 / Navigation 411 / Lifecycle 199 lines), all seven changed guides and the new lifecycle metadata were read. The latest implementation change is the initial-mode reconciliation, its two native cases and guide; the other six runtime files and first two fixtures match the prior native-C fingerprints in the approved request. The 58-line resolver and Singleton lifecycle/lookup members were dependency context reads.

Scoped name-status enumeration against `ca198556` found **no deleted file** in Audio/NPC source/tests/docs; removed runtime lines implement the declared fixes, not undeclared public/serialized removals. Nested Audio.Editor changes and four primary legacy Audio fixture changes appear in the wider diff but belong to their other owners; NI22 does not approve or attribute those edits to these runtime actors. No scene/prefab/sample/config mutation was required by the reviewed implementation.

## Audio disposition — source-consistent

**AUDIO-EDITOR-DOC:** the canonical README links the existing nested Editor guide, retains runtime/scene entry points and release/development/imported sample paths. No nested editor source/topic is changed by this runtime owner.

**C-04/C-05, late/replacement AudioControl:** one-time Start marks/setup happens before a missing dependency can terminate binding. Re-enable and additive `Initialize(AMSettings)` retry through the same binding path. Null explicitly restores singleton fallback. The actual reactive subscription is captured as IDisposable, disposed before replacement and on disable/destroy; an old property cannot remain subscribed merely because the current settings reference changed or became Unity-null. View refresh uses silent setters; input listeners are installed once, removed on destruction and inactive UI handlers do not apply input. The temporary interactability flags only restore a value this component changed from true to false; authored false stays false. Public Set remains a callable C#/UnityEvent path, including the existing Custom cache workflow. No Update polling/search was introduced.

**C-06, late AMSettings:** explicit `Initialize(AM)` performs lazy AM source initialization before caching sources; startup persistence is loaded once and writes are suppressed within initialization with exact prior suppression restored in finally. The same successful live manager binding returns without replaying startup gain/unmute, and enable retries a missing binding. Null/nested initialization retains the accepted binding. Existing singleton/source/mixer paths and save keys remain. Startup state on a newly accepted manager is intentional; no automatic per-frame replacement scan or general settings framework was added.

**C-07, standalone rebind/stop:** `Initialize` owns a bounded `_rebinding` interval, advances generation and clears/stops/cancels the old session before OnStopped. Nested Start/Initialize cannot revive or steal the old binding during that notification; finally installs the accepted target even when the observer throws, then the exception propagates. Outside rebind, Start captures its generation before StopPlayback and rechecks after its notification, so a newer observer-created/paused session supersedes the old Start. Each asynchronous loop captures source, track array and generation; its finalizer cannot stop the replacement. Listener pause, controller pause, zero/resumed pitch and destroyed-source cleanup preserve the earlier unscaled contract. This closes the specific orphan-source defect without promising arbitrary callback rollback.

**C-34, exact owned-clip release:** `AM.ReleaseClips(params AudioClip[])` compares exact references and does not initialize/create sources or destroy clips/source objects. It clears matching primary music, selected outgoing voices, ambience and positional clips, zeros their corresponding gains, detaches matching legacy random references through clone-on-write without shifting surviving indices, and only then publishes one primary OnMusicStopped when applicable. Outgoing-only release does not announce an unrelated current track as stopped. The shared fade routine remains alive so unrelated outgoing music keeps its normal fade lifetime; matching ambience cancels only its ambience fade. Authored entry catalogs, caller arrays and channel settings remain. Repeated release is harmless in the reviewed paths.

The API deliberately cannot selectively stop overlapping PlayOneShot calls on a shared source; independently releasable 2D effects use the existing `PlayAt(..., spatialBlend: 0f)`. Catalog registration cleanup and generated-clip destruction remain the caller's responsibility. The Audio API is ready for the Demo consumer; **NI22 does not independently approve or assert the current completion state of Demo teardown integration**.

Earlier music fixes remain coherent on current source: selection succeeds before committing pool/gain/start events; failed explicit track leaves the old sound intact; sparse fallback skips the old index while alternatives exist; channel gain and per-voice envelopes remain separate through zero gain/stop/crossfade; late mute fans out to outgoing voices. Explicit SettingMixer enum choices survive legacy-name inference. No existing public signature, serialized field, enum number, id/index migration or save key is removed; the three published initialization/release APIs are additive. Proposed playback-session/continuous-volume observation remains a proposal, not a prerequisite.

## Audio authored acceptance and limits

All **20 Edit + 16 Play = 36 cases** were read; this is authored inventory, not discovery. The five new Edit bodies cover rebind restart/nested Initialize, synchronous observer failure with accepted binding, superseding paused restart, selective positional/ambience release and clone-on-write legacy-array/index preservation. They retain exact public/source identities, exception/cardinality and ordinary subsequent Start controls. Edit source/clip assignment is not proof of audible native playback.

The seven new Play bodies cover missing settings recovery, an authored-disabled toggle, replacement subscription ownership, explicit/idempotent late manager, enable-time manager retry, actual two-source rebind and current/outgoing clip release. They use real Start/enable/disable/destruction and public operations, nonempty current/outgoing clip identity and isPlaying preconditions, exact old/new listener counts, silent refresh and functioning input. The release case requires one retained unrelated outgoing source while releasing first an outgoing owned clip, then primary owned music twice, with exactly one stop notification. The rebind case proves old playback stopped and later explicit Start plays only the accepted new source. No skip/no-exception substitute supplies these oracles.

Retained cases exercise rejected selection/gain, successful next-track overrides, sparse alternatives/trims, cold source binding, ordinary mute toggles, outgoing mute/gain/envelope changes, listener pause/pitch, destruction, silent Toggle/Slider input and Custom re-enable. Some Edit settings/migration controls invoke lifecycle methods manually; these remain simulation. UI observer callback counts show actual input/publication behavior; the source's Start guard is also inspected for single listener installation rather than treating a UI counter as a runtime invocation profiler.

Play cleanup stops owned standalone controllers, restores captured listener pause, destroys only listed objects/clips, yields for native destruction, then restores exact singleton auto-create flags and Random state. Settings fixtures disable persistence before normal activation; no shared save keys are deleted. Exclusive isolated execution is still required for ambient singleton/audio state. Native audio-enabled execution, real mixer routing/persisted reload, driver/headless behavior and teardown resource counts remain unexecuted.

## NPC disposition — source-consistent

**NPC-INITIAL-MODE-REENTRY / C-26:** Awake captures the initial mode, selects/notifies without entering, then reconciles a changed deferred selection without another event or entry before marking initialized. SetMode calls during the first notification only assign the last requested mode. OnEnable therefore enters the matching final behaviour once. The initial event argument still describes the initial mode; normal post-initialization mode changes retain their path. No new API, mode default, field or asset migration is introduced. The reconciliation's exit of a never-entered prior behaviour uses the existing quiet disable/stop paths and does not introduce another startup notification.

The two new cases enter Play Mode through the existing Editor test runner and prepare inactive objects/listeners before activation. They require exact initial-event/callback-return/patrol-start ordering or last-deferred Follow selection, zero superseded entries, distinct patrol/follow destinations and a real complete native path with endpoint/corner checks. Path settlement has a three-second unscaled deadline, and counts remain unchanged afterwards. They do not invoke private lifecycle callbacks. The fixture builds one remote transient NavMesh for the configured agent type, destroys owned GameObjects first, removes only its own NavMeshDataInstance/data and exits Play Mode. Its new metadata exists; no global NavMesh removal, shared agent-type/settings edit or unsupported NonParallelizable attribute is used.

The retained 27 bodies were also read completely: first/origin/stationary/retarget path acceptance; persistent Stop/Resume; real partial paths with blocked/no-arrival; bounded failed patrol starts; Combined manual-path entry, miss recovery, destroyed/replaced targets; pre-Awake/off-mesh setup; finder override/name fallback; missing controller; repaired selector; immediate hold/chase snapshot restoration; supplied animation time. Their manual Edit callback invocations are explicitly distinguished from the two new activation-driven cases. NavMesh/velocity/remainingDistance/Animator preconditions assert actual state and remain to be executed, not inferred from source. Cleanup removes only owned data/objects/resources and restores borrowed Random where changed.

Current follow/patrol/Combined source supports the approved path contracts: successful SetDestination bool is propagated, partial routes cannot announce arrival, failed start does not emit Started, one Combined state controls retry/target loss, and postcombat restore retains documented mode/target/activity. The brain's missing-controller path returns false, destroyed references are repaired with Unity-null checks and the duplicate empty acquisition call is removed. The limited navigation snapshot, attack input takeover and general event reentry limitations remain disclosed proposals; no broader framework/design change is required for C-26.

NPC's **29 prefixed cases plus the existing eight decision cases** require central discovery/execution; NI22 inspected all three prefixed fixture bodies and does not claim a new execution of the separate decision fixture. Actual Edit asmdef references include Neo.NPC, both test runners, Neo.Rpg and Unity UI. The new real lifecycle cases stay in that Editor assembly via EnterPlayMode; they do not need a new Neo.NPC reference in the Play asmdef. Current Audio fixtures' Audio/Reactive/UI runner references are present. Configuration was read only.

## Aggregate work, compatibility and evidence debt

Audio's LateUpdate handles the fixed two outgoing slots and optional ambience; fades/watchdog/standalone loops run for active playback. Channel refresh/release work scales with configured voice/clip-array sizes; pool allocation is on creation/resize and selective legacy detachment, not a new per-control polling service. No new per-object physics queries or recurring UI scene search was introduced. These are source bounds, not GC/FPS/profile measurements or a guarantee against arbitrarily modified configuration.

NPC brain decisions and target-finder misses retain their configured cadence; a bound target suppresses finder search. Follow automatic planning retains its interval. Resolver performs at most one direct plus six×ten ring samples per request, patrol can retry five selections, and Combined misses may retry each tick. This remaining aggregate cost is disclosed; agent paths/destinations are distinct and cannot be collapsed into one shared chosen route. Agent-type/connectivity/strict containment, optional backoff and full takeover snapshots remain proposals. NI22 does not silently change mechanics/settings or turn an existing disclosed limitation into a new approved framework.

**Confirmed additional residuals: none in this reviewed scope.** Source approval covers the approved bounded runtime/test/doc contracts above. All compilation/import, NUnit discovery/execution, audio driver/mixer/persistence/native cleanup, activation/NavMesh/Animator results, supported Unity branches, package/docs/metadata/GUID/UTF-8 validation, imported samples and aggregate profiling remain **UNEXECUTED**. Historical static results and authored counts are not current passes. Original owners must handle observed failures after root's global source freeze; no execution was requested/launched here.

## Read hashes

Artifact paths below are relative to `Docs/Audits/2026-10-23/`; each listed audit/plan/request/receipt was read completely.

| Artifact | SHA256 |
|---|---|
| Neo.Audio.md | `56e817afbda42acc006b88fbc0bc368a745a363aced81a3166c95de93827f7d0` |
| Neo.Audio.plan.md | `d80cf88da14dabc1381aeb5267f09b339da11429a3e4afe25557df6bb6142612` |
| Neo.Audio.implementation-request.md | `e4294be02eb33aa701b84be9e955b1b46c8feddd1e695fc10263217cf1ed387b` |
| Neo.Audio.implementation.md | `884a0e154d5a0647539b01cd47286a879cfa63a4a67bba05ae238ed586976bc0` |
| Neo.NPC.md | `eb7790c1b02b62aec90b753bd4550885a3e9aa9162d0f091d311838e8dd181b5` |
| Neo.NPC.plan.md | `c1a563cd468ced09fb71fdc328b4290a15db5298f6a6eb4885264dd3e9498394` |
| Neo.NPC.implementation-request.md | `9062dc517dcc5506103ee29de8285025ebf9060dfb4bd839a98f270cedde32b3` |
| Neo.NPC.implementation.md | `1ac9d9c893abd8f6136aae3db773b93a4e1cfb82ec49dd03e9605ff31b479aa8` |

Paths below are relative to `Assets/Neoxider/`. All listed runtime files, prefixed fixtures and guides were read completely; asmdefs are ownership/visibility context only.

| Current file | SHA256 |
|---|---|
| Scripts/Audio/AMSettings.cs | `5de2fb3936a85fcda6114151851a96519a8385e39c3c683627c858be94983cf2` |
| Scripts/Audio/View/AudioControl.cs | `e92f7a4c8b6d6fe0f4a412fc3846868584168f510329783a640aa99e07a17e57` |
| Scripts/Audio/RandomMusicController.cs | `5f365e4ceaf50334ae45d93f0ed986129e5f3d86042556b6e1d326c3d28dcafd` |
| Scripts/Audio/AudioSimple/AM.cs | `6f39f3bb90404bba1e3d27baa9fd82ecbb107ec3ac02331d8a386bc8d83a1f3b` |
| Scripts/Audio/AudioSimple/AudioEntry.cs | `c36c741558f11d70d0088ddfae592834c17b627e30e609402d6e6cb155c2c8d9` |
| Scripts/Audio/SettingMixer.cs | `ec4ca224ce277eae3680baa3ce5684b84349f06fc1bf6ae237cdcc66aa76bd74` |
| Scripts/NPC/NpcNavigation.cs | `e477ea8e98d67a415dfa270ff5ff03fc5471ac8d9c1d121d211f4a009d8dec47` |
| Scripts/NPC/NpcNavigation.Behaviours.cs | `d515e3dc339e124b810b6b15f3e1a4c80bca44993f6854531869319cfa02e916` |
| Scripts/NPC/Combat/NpcRpgCombatBrain.cs | `c4770527c802ecc4a773dbc7059fe86def72f5d93a5b62283fb4438133dc3ea8` |
| Scripts/NPC/Navigation/NpcAnimationCore.cs | `074a2cc76ccdf2a7bf54dace95bee8e6aa06a8dc4979580a82afe96f4cf3f1fa` |
| Scripts/NPC/Navigation/NpcFollowTargetCore.cs | `44b4a8563939c8ea5eebd46fd8ef6caa4bbeb3ac62b17c77aa6bff850c1a6869` |
| Scripts/NPC/Navigation/NpcPatrolCore.cs | `230b996e6822c64763dc70fda0d0b204040fbd7284b4956cd735f26c787b3a52` |
| Scripts/NPC/NpcTargetFinder.cs | `eec324f98dc790a0799e16b7c2bb1a5ad0ff46321c16b6b417141ba642ced2a4` |
| Tests/Edit/Audio/Audit20261023NeoAudioRegressionTests.cs | `c0b40b89dfbac9a77315957641812e0b717def598faaab32230e676355ab0253` |
| Tests/Play/Audit20261023NeoAudioLifecycleTests.cs | `b5c0863acee794087bbfba51b1cc1fd771fce7c39f555da9edbc7e24f1a92ca4` |
| Tests/Edit/NPC/Audit20261023NeoNPCComponentTests.cs | `8d3889a7e237b0726c5d7b3086441aefaba85b87f042d7a7ba4c977362eff855` |
| Tests/Edit/NPC/Audit20261023NeoNPCNavigationTests.cs | `2530695e09fb9899e9424359e4c7bd80d7693352542d9c6ea3ca28dc04585f25` |
| Tests/Edit/NPC/Audit20261023NeoNPCLifecycleTests.cs | `3117544612afd1642ca975f19e030c2433f90e4f284791eee8d9cebd716b0b51` |
| Tests/Edit/NPC/Audit20261023NeoNPCLifecycleTests.cs.meta | `4c1b3f8c3c478d9aa2249730117bf71efd3efc2fc1e3c15548a85bbd34c0cf83` |
| Docs/Audio/README.md | `0cd7e6f4dd29330480fb88dd1746158850f5ac56b96e9b1ddbead02fd53a2a5c` |
| Docs/Audio/AMSettings.md | `f83d080a9e2246d1e105aedcaa3913ee1fca23458385d908e815f3408f9bc718` |
| Docs/Audio/AudioControl.md | `ce955cf3e85ba3037bc21fe39705e59cd1f674949efb5bfc651d220086c4d044` |
| Docs/Audio/View/AudioControl.md | `67674fe36012f6ba9e4a300efe8145e11da5a2969833cf71d874458657977ccc` |
| Docs/Audio/RandomMusicController.md | `3d0188f99f56ebfb14727455be8fdceb3921854af38941fe1e0708d7ea83ee89` |
| Docs/Audio/AM.md | `023c58d3e4d89c21123b0b6461292e8aef7b44f7312ab5b34e3578b9da881a2b` |
| Docs/Audio/PlayAudio.md | `53cbc3884e950287f5b478440d6034b6e820a972244b02562ea3ee04f56341ba` |
| Docs/Audio/SettingMixer.md | `2527a4fe01a308e6560010e8982d3c1ae4ae5b31040139557a01940413b24d2b` |
| Docs/NPC/README.md | `3f8eb9858bab62f34edd9afd2287fb4c965eaa60c16cf6ee279746e4da110de9` |
| Docs/NPC/Navigation/NPCNavigation.md | `9d9e89df615c55e5e03deae64706e9ddbac45275f004e8fa98f9a4ebe56dc276` |
| Docs/NPC/InternalTypes.md | `e76178ad90cdbc74b571e21d4ff57d4ed51a160f9e26c3c6341954e586698031` |
| Docs/NPC/NpcAnimatorDriver.md | `9cd80f02c0f5ce781fc227a05649258ee496ce9a490d8e40fd28d4a9fdad1904` |
| Docs/NPC/NpcTargetFinder.md | `7d56cad47e2d3e51a5c829c9310cb2e07b76d1f9b9ec4bcfb7eba39028e6be75` |
| Docs/NPC/Combat/NpcRpgCombatBrain.md | `53a0e43b899ba982d062f12b0daf220a6608c41a9e2625c21676a771eaca6057` |
| Docs/NPC/Combat/NpcCombatScenarios.md | `7e265984fb08e1ff73a0e04f0f1f57e8021d68b31a4785fd28fb5a39c01324d1` |
| Tests/Edit/Neo.Editor.Tests.asmdef (context) | `204a5ba6382792131c48cae74f38e27f5431909c0c0866c3b2905be0f2c4b4a5` |
| Tests/Play/Neo.Tests.Play.asmdef (context read) | `2518b0df939933514e55de55be5313226107a2382fe89d31b5361565aea50fe4` |
