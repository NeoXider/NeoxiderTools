# NativeImplementationReview-48 — Parallax / Rpg.Combat / Rpg S3 fixtures

SAME independent native reviewer `/root/verify_unity`, gpt-6.1-sol/max. SOURCE-ONLY: no product/fixture/helper edit, compiler, Unity/test/checker/process launch, installation, commit or push. Current product execution stays closed. Sealed source3 manifest `2d12ae058b989ce064e86a8d7db8223ecb65241d5dcd19babfb2fd7082adfc70` supplies exact old-oracle comparison. No historical source-final paragraph or S3 pass is relabeled current acceptance.

**Disposition:** one bounded Parallax fixture-quality finding NI48-01/P2; Combat and RPG corrections have scoped SOURCE CLOSURE, native evidence outstanding. Parallax runtime/Edit contract corrections are retained; its new frame-observation acceptance needs the small same-owner repair below.

## Receipt pins and read coverage

| SAME owner | Full receipt lines | SHA256 |
|---|---:|---|
|Neo.Parallax|114|`f476e1f783fa83f36fa4e3d11982c54c082bc9538879ca1f8b2bd499611e15cb`|
|Neo.Rpg.Combat|138|`3ba1a418ce6d4fa032e874eec99a0c904b0ac01746350196ea5de4fe6c8ba4fc`|
|Neo.Rpg|323|`c135d90bcf150ef483505587b12f38e5a57c57d8d7e4c6e126a0703d78839f0d`|

Read full114/138/323 receipts, actual changed source hunks against source3, all new/changed native scenario bodies and decisive existing runtime paths. Current Combat hit/lifecycle and RPG binding fixtures read completely; Character review is bounded to the changed initial-publication body/primary probe and its actual public routing/init path. Parallax changed Edit release oracles and complete new native fixture were read; unchanged earlier arithmetic/runtime review is not rerun here. Runtime fingerprints below match source3. No still-writing unassigned owner is approved.

## NI48-01 — P2 — native frame observation can be vacuous

SAME original owner/task: **Neo.Parallax / neo-audit-1023-b01-02**. Exact file `Assets/Neoxider/Tests/Edit/Parallax/Audit20261023NeoParallaxRegressionTests.cs`, new `Audit20261023NeoParallaxNativeLifecycleTests.StationaryCamera_DoesNotRerollPaddingTilesEveryFrame`, lines748–777, especially settled RNG capture766/yield767/assert769. Other settling/deferred-destruction waits in that new fixture likewise count Editor yields rather than observing native frames.

Trigger: execute this Edit-assembly UnityTest while admitted Play Mode exists but the runner resumes multiple Editor coroutine steps within one native frame. Expected: the RNG/sprite/pool negative is observed across at least one actual native LateUpdate opportunity. Actual source: activation positively builds100 tiles, then snapshots RNG/sprites and performs only `yield return null` before asserting unchanged; no Time.frameCount/phase progress check exists in the fixture. It can pass the negative without any intervening native LateUpdate. The separate CameraTeleport case has a real movement positive but does not establish progress inside this stationary case. This is a source-confirmed evidence gap, not a newly measured Parallax runtime regression.

Smallest repair: SAME owner adds/reuses a bounded real native-frame/phase barrier in this fixture (unscaled deadline and explicit Time.frameCount progress; enough completed frames to include LateUpdate, e.g. two native-frame advances), applies it to motion/settling/observation/deferred destruction boundaries, and keeps every original100-tile, position, sprite, RNG, source-state, immediate retirement and final destruction assertion. No manual/private LateUpdate, fake scheduler, ExecuteAlways, relaxed renderer enumeration, new skip or runtime-policy change. Positive native motion and ordinary lifecycle geometry must remain nonempty. Authored source correction and actual native rerun remain pending.

## Parallax: retained old/new oracle mapping

The39 Edit cases now explicitly release with public SetTargetCamera(null), with main-camera fallback disabled, instead of treating disable of a never-Awake Edit object as native cleanup. All count/local/world-extrema/rejection/recovery/source-enabled/authored-child/sprite/RNG/negative-spacing assertions remain; two camera-release names are explicit renames, not dropped cases. Added live8-tile positives make source restoration nonvacuous. Runtime Source3 ParallaxLayer and canonical guides are not changed by this S3 recovery.

The two old motion names move from manual SendMessage(LateUpdate) into the new Play-admitted native class, and four source-enabled/direct/nested lifecycle controls add45 total authored scenarios (39 Edit+6 UnityTests). Their active8/6/7 pools, actual bounds/stride/anchor, restoration and borrowed-child hierarchy/transform checks are meaningful; immediate inactive retirement and later Unity-null destruction remain separate. Only the missing native frame barrier prevents full source closure of that observation slice. No numeric tolerance/settings/algorithm change requested.

## Rpg.Combat: scoped source closure

Old ignored-trigger False/True map exactly to named3D/2D UnityTests; disabled-hit case remains. Shared top-level Enter/Exit owns only its Play transition, captures/restores actual timeScale, requires native fixedTime advancement and cleans only its two actors. Actual Rigidbody/Collider contact replaces forbidden Edit SendMessage. Positive HP10→7/one hit/one trigger/exact target+collider/ownerHP10 precedes ignored/disabled negatives; clearing ignore in the SAME reset swing admits one hit, later re-entry stays suppressed, and re-enable has a normal positive. No manual physics/lifecycle simulation, skip or whitelist is introduced.

Lifecycle fixture keeps12 cases, original durations/tolerances and cooldown-start cancellation ordinary-cycle controls. Scaled-clock admission and ObserveScaledDuration use native Time.timeAsDouble with real-time bounds; IsInvoking proves native scheduling before managed-window/evade completion assertions. Restarted window still stays open/no early completion, normal end occurs exactly once, disable/re-enable suppresses stale completion. Deferred cooldown is captured inside completion before reentrant restart, retaining one completion/rejected restart/active cooldown. Existing Health12 cases remain, total27. Runtime AdvancedAttackCollider/Evade/other Combat bytes remain source3. No confirmed remaining bounded source defect found; source admission is not real contact/timer acceptance.

## RPG: public initialization and nonvacuous binding source closure

Resource/stat InFlight now register observers before explicit public Character admission on enabled views. Old listenerCount2, initial50, ordinary51 and earlier-listener invocation are mandatory before disable/rebind. Then old-tail suppression and exact empty/7 output remain; resource repeated-old88 rebind7/7 control remains. InitialCurrent still asserts exact Current1/2 and Max/Percent0/1, adds50 or50/7 payloads, and uses public setter rather than claiming Edit SetActive ran native OnEnable.

Destruction cases keep actual source DestroyImmediate, Unity-null source while managed old reactive stores52, enabled independent view, no stale52, replacement7→8, old listenerCount1 and zero old99 output. The public Character setters genuinely Unsubscribe/SubscribeIfReady; current session predicates retain Unity liveness/state/version/identity. All5 methods/9 binding cases and old substantive oracles remain. No helper/shared-ref/runtime code was changed by these corrections.

E94 retains the inactive actor, template/unique saved profile and all observers before trigger. Primary-extracted runtime probe is present/unchanged and returns false from TryRoute. Setting public isNetworked allows actual adapter discovery; Damage(0f) follows DamageType532–545: TryRoute first, EnsureInitialized before the nonpositive check. EnsureInitialized loads full derived state, publishes RuntimeRebuilt, auto-loads, then final snapshot; zero input returns0 without a damage debit. The test retains EXACT rebuilt→loaded→snapshot, MaxHp100 during rebuilt, one saved snapshot, XP17/HP130/Max160/Stats.Count2. It is explicitly public initialization-contract evidence, NOT native Awake or network-wire acceptance. No production overload/fake route/lifecycle reflection added. No confirmed remaining bounded source defect found.

## Current source fingerprints

Paths under Assets/Neoxider; full hashes identify reviewed caller bytes. Root must reread after any correction before a new execution freeze.

| File | SHA256 |
|---|---|
|`Tests/Edit/Parallax/Audit20261023NeoParallaxRegressionTests.cs`|`756bb75817895e2d5468aa141df2a1daec4d0321d6e73752772b2a7bcbfac9f7`|
|`Tests/Edit/RPG/Audit20261023NeoRpgCombatHitFilterTests.cs`|`e4033430037e305a3d189b1b9607489724d3f4383d532aac474829c7c6b33d36`|
|`Tests/Edit/RPG/Audit20261023NeoRpgCombatLifecycleTests.cs`|`53c68d171b3bccda404a4b031de85ab59984aaa7c04c95c868ab10ed502ef94b`|
|`Tests/Edit/RPG/Audit20261023NeoRpgImplementationBindingTests.cs`|`678849266852b98cca80fb241005c4fe20c10a36abbc82d22ef071b30ef02904`|
|`Tests/Edit/RPG/Audit20261023NeoRpgImplementationCharacterTests.cs`|`4153e23ddd468b7558e11a1a8643a7806897d49e5acf6d5ef75a5b851a397f31`|
|`Tests/Fixtures/RPG/Audit20261023NeoRpgSnapshotProbe.cs`|`486aa3b22e4842b1ffc0a28bad47806a645dee093f577339c65fb6ffa805c01a`|

Runtime unchanged-versus-source3 pins: ParallaxLayer `cd3ee3ee2f9d7bd72c282f07549e0ea366a15a827e3a5b0dc8185961d3331e6d`; AdvancedAttackCollider `c8144fc7d3c1ae1e39e703b8d75e1a400a3e84708e3bf683a60b376936042cee`; Evade `06725944900d4f491b72600dfb9697b77466a4ba8005ac6f25ef853311da4ba2`; RpgCharacter `9dfadfbaac83b7797da38ff0064979648ef3214b1fad98a7018a1e733bfd637c`; RpgResourceBinding `542aad718b19a7adb065d87e6a5194ef826b1afc137925f15161831e7ad7aa23`; RpgStatBinding `91e070f366d19ebbe37dc26347f68d85382f857b696aa04676ce87493af9829a`. Primary owns relocated probe/type/GUID/shared refs. No relocation or production change is requested here.

## Remaining actual execution requirements

After NI48-01 correction/all writers/new reviewed freeze: real both-version discovery must map all45 Parallax scenarios/renames, all27 Combat cases and all9 binding/E94 cases. Require actual native frames/LateUpdate/nonempty geometry/cleanup, fixed-step3D/2D contact payloads, admitted scaled intervals/Invoke completions/timeScale restoration, exact listener/session/liveness and saved snapshot payload/count/order. Public Damage0 must not be relabeled native Awake or remote peer evidence. Old S3 results and current source-only counters do not meet those requirements. Preserve full result/skip/error/teardown data and ordinary positive controls.

Fresh ba515... asset diagnostics are separately authorized readonly-only and have NOT been launched by this review. Product tests/schema upgrade/copyback/push remain forbidden; immediate resource/ownership guards apply at that next native boundary. This review writes only NI48 and does not claim a converter/metadata/native pass.
