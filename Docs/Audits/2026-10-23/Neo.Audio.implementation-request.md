# Neo.Audio: approved implementation request

Original actor: `neo-audit-1023-b02-03`. Target `demo` / `11.0.0`. Root has read your complete audit and plan.

Read `IMPLEMENTATION-CONTRACT.md` first. Do not execute tests/build/validators during this phase.

Keep nearest-assembly ownership. Finish bounded confirmed fixes and owned canonical documentation/backlinks. No optional broad framework is required.

## Assigned review actions

### AUDIO-EDITOR-DOC (P3; approved-source-correction)

new narrow Audio/Editor.md needs canonical link

### AUDIO-LATE-BINDING (P2; approved-source-correction)

Audio plan: Start without settings permanently unstarted; replacement settings retains stale subscription; AMSettings cannot recover from AM absent at Start.

### AUDIO-REBIND-STOP-REENTRY (P2; approved-source-correction)

C-07: Initialize(B) Stop callback Start restarts old A before rebinding; bounded generation/rebind ownership, no orphan source.

## Relevant full native sections

Source: NativeReview-C.md

### C-04 / P2 — AudioControl cannot recover if Start had no settings

Current source: `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Audio/View/AudioControl.cs:96–156`, `OnEnable`/`Start`. Trigger: a built-in control starts while AMSettings is absent (singleton auto-create defaults false), then settings is created and the control re-enabled. Start returns before `_started = true` and input setup; OnEnable always returns while `_started` is false. UI remains disabled/unbound. Smallest repair: one-time UI initialization independent of successful binding, idempotent bind/retry on enable or explicit initialization, restoring only interactability the component itself changed. Add real late-settings/re-enable/input-cardinality regression. Owner: **Neo.Audio / `neo-audit-1023-b02-03`**. This is already confirmed pending work in its plan, not an unfinished claim about the separate lazy-source fix.
Source: NativeReview-C.md

### C-05 / P2 — AudioControl replaces its settings reference without moving its subscription

Current source: `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Audio/View/AudioControl.cs:159–230`, `TryResolveSettings`, `SubscribeMuteEventsForToggle`, `UnsubscribeMuteEventsForToggle`, OnEnable/OnDestroy. Trigger: bind toggle to settings A, destroy A, create B and reopen control. OnEnable resolves B and silently refreshes once, but only Start subscribes; future B mute changes do not update it. Unsubscribe uses the current reference, so an independently retained old reactive event can keep the old delegate. Smallest repair: retain/dispose the actual subscribed owner/handle, bind on enable, unbind on disable/destroy, avoid duplicate UI listeners. Assert A/B notifications and silent one-input/one-callback behavior. Owner: **Neo.Audio / `neo-audit-1023-b02-03`**; reuse Neo.Reactive subscription handles, no playback-session extraction prerequisite.
Source: NativeReview-C.md

### C-06 / P2 — AMSettings permanently abandons source binding when AM arrives later

Current source: `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Audio/AMSettings.cs:115–123`, `Start`; source-dependent setters at 202 onward. Trigger: AMSettings Start runs with no AM, then AM is created. The one-shot Start returns, no retry binds `efx`/`music`, setters continue returning early. Smallest repair: idempotent explicit `Initialize(AM)` plus enable-time fallback; apply loaded startup values once per successful binding, without replaying persistence or resetting active playback on every enable. Observe late binding and normalized/mute state on new sources. Owner: **Neo.Audio / `neo-audit-1023-b02-03`**. Current singleton implementation confirms CreateInstance defaults false; the completed cold-*sources* fix is not a late-*manager* fix.
Source: NativeReview-C.md

### C-07 / P2 — reentrant OnStopped during standalone Initialize leaves the old source running

Current source: `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Audio/RandomMusicController.cs:51–63` (`Initialize`), `Start:64–89`, `Stop:96–118`. Trigger: play A; attach a one-shot `OnStopped` handler calling `controller.Start()`; call `Initialize(B, tracksB)`. Stop cancels old state/stops A, publishes OnStopped, and the handler synchronously starts A again with a new token before Initialize assigns B. The new loop/state then shares the overwritten B fields while A keeps playing outside the reported current source. Expected, matching the existing reinitialize test contract: Initialize finishes stopped and rebound to B; a later explicit Start plays only B. Smallest repair: bounded rebind/generation guard or equivalent ownership reconciliation around Stop notification, explicitly define reentrant Start during rebind; no session/framework extraction is needed. Add a two-source real-audio/public-state regression with a one-shot subscriber, owned-controller Stop and source cleanup. Owner: **Neo.Audio / `neo-audit-1023-b02-03`**. This is a source trace; no new audio execution occurred.
Source: NativeReview-C.md

### Neo.Audio — reviewed

Original owner/task: `neo-audit-1023-b02-03`. Full audit/plan, all six runtime changed-file diffs, affected selection/fade/transport/settings/control bodies, both complete new fixtures and all eight documentation diffs read. Per-voice independent gain/envelope state, failed clip selection before commit, outgoing mute fan-out, sparse fallback and enum migration are locally coherent. Public/serialized changes are private runtime state only. The tests meaningfully observe gain products, clip identity, resource ownership, mute/input events and elapsed behavior; Play cleanup stops controllers, restores listener pause, destroys owned objects/clips and yields for destruction. Hardware/audio-enabled assumptions are explicit; these are unexecuted here. Current tests do not cover C-04–C-07.

Aggregate work: AM has fixed two outgoing voices, request/fade-driven small allocations and one watchdog/session loop; no per-target Physics query. AudioFocusMute performs a scene lookup on explicit Install, not each frame, and still requires one listener-volume owner. AudioControl does no per-frame scene search; binding fixes should retain that property. Singleton lookup caches misses/instances. Do not replace explicit retry with a FindObjects scan per control per Update. The proposed shared session/observable channel state is optional and substantially larger than required binding fixes; preserve clamping order, unscaled fade policy and source ownership if later adopted. Repeating the same AudioClip at different entry indices and observer-mutated payload/state need explicit contracts before changing selection weighting or replaying state; not counted as confirmed new defects here. The StartMusicClip XML paragraph saying it takes no volume argument is stale after its private signature change and should be corrected in the owner's next documentation pass.

Read fingerprints:

| Full artifact/source path | SHA-256 |
|---|---|
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Audio.md` | `56e817afbda42acc006b88fbc0bc368a745a363aced81a3166c95de93827f7d0` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Audio.plan.md` | `d80cf88da14dabc1381aeb5267f09b339da11429a3e4afe25557df6bb6142612` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Audio/AMSettings.cs` | `b32ed4e37e0f5e04963d2adf7c6ce7abd925cd72bc7d48607b1f76f22c2389d6` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Audio/AudioSimple/AM.cs` | `f17cdcfd99bc074d672e3a70200328544ff35822066ce3a6daa44214bce5586e` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Audio/AudioSimple/AudioEntry.cs` | `c36c741558f11d70d0088ddfae592834c17b627e30e609402d6e6cb155c2c8d9` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Audio/RandomMusicController.cs` | `6df673738b27e3f7a25c374408ca07237983b7f7663c06f3f90bab3ad958c418` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Audio/SettingMixer.cs` | `ec4ca224ce277eae3680baa3ce5684b84349f06fc1bf6ae237cdcc66aa76bd74` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Audio/View/AudioControl.cs` | `c28959f45a5cdfef434115836c6fdf2e90fe454144314b84f6a03ca9c61db35b` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Audio/Audit20261023NeoAudioRegressionTests.cs` | `81472623e514c164bbd34eb37d289d05104480b8f5b36525f7815ca33e9b48ab` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Play/Audit20261023NeoAudioLifecycleTests.cs` | `bd58c79fec82d5cbd64c545a250a666f8a5e0a404ab4c1bc02db8f5acb3b48d1` |
Source: NativeReview-C.md

### C-34 / P2 — demo shell/controllers leak their generated UI and native assets

`D:/Git/NeoxiderTools/Assets/Neoxider/Samples~/Demo/Scripts/Shell/NeoDemoShell.cs:54–81` builds unparented Canvas; `GetGradient:191–219` creates Texture2D/Sprite every call and overwrites static reference; Context:226 has no lifetime API. AudioDemoController BuildClips:156 makes six clips, OnDestroy:147 only unhooks events; LevelDemoController creates curve:86, OnDestroy:69 only unhooks. Create then remove only the demo controller repeatedly: old UI/callback targets/native assets survive their owner (some audio still referenced by persistent AM). Expected generated session resources release while unrelated shared camera/manager/audio/assets survive.

Minimum additive owned Context.Dispose/owner overload and controller OnDestroy disposal, plus exact created clips/curve consumer teardown before object destruction. Stop only demo-owned playback, including outgoing voices if necessary via narrow Audio-owned release, never global Stop as ownership inference. No generic resource/session framework prerequisite. Test twenty create/dispose cycles after shared-cache warmup, deferred-destroy counts, component removal, two concurrent demos and double Dispose. Owner **Neo.Rpg.Demo / `neo-audit-1023-b05-05`**, Audio **`neo-audit-1023-b02-03`** only if existing playback API cannot release exact owned voices; Quest registry detach contract is separate before quest-definition disposal. Current plan correctly treats native ownership as required despite calling broader session work proposal.

## Completion receipt

Write `Docs/Audits/2026-10-23/Neo.Audio.implementation.md` with exact actions/files/additive API/dependencies, migration details and authored acceptance (UNEXECUTED). If blocked on another additive API, complete independent files and identify it precisely for root continuation. Do not edit shared review-action/manifest/other actor reports.

## Snapshot3 actual AUDIO-VALIDATE-NATIVE-ADMISSION

SAME original source actor, gpt-6.1-sol/max. Root closed execution after all owned Unity jobs stopped. NO compiler/Unity/checkers/tests until ALL writers finish and a new global freeze. Root personally read all98 graphical Edit failure messages/counts and all43 Play groups, independent full-edit-failure-triage.md and full-play-failure-triage.md. Read exact assigned cases in Local/Audits/assembly-2026-10-23/unity/final/s3/u63/Receipts/22-full-edit-graphics.xml (SHA7c6d24e1...) and21-full-play-graphics.xml (SHA2b152164...) plus Local/Audits/assembly-2026-10-23/full-edit-failure-action-index.json. PRIMARY b03-04 exclusively owns ALL helper moves/extractions, Fixtures assembly/shared refs and files in Local/Audits/assembly-2026-10-23/runtime-fixture-relocation-reservations.json. Check map before every edit. Do not race/extract helpers or change reserved fixture callers; work only disjoint source now, explicitly report deferred reserved work for root to resume after release. Preserve public/serialized API/settings/GUIDs and EVERY substantive positive/negative/count/ownership/payload oracle. Establish a real positive precondition before inferring runtime defect. Native mode failures cannot be hidden with ExecuteAlways, manual Update/private lifecycle as native evidence, blanket Ignore, expected-error whitelist, arbitrary tolerance/default change, reflection UserCode bypass or lost cases. Use supported TOP-LEVEL UnitySetUp/UnityTearDown/UnityTest Enter/ExitPlayMode for true lifecycle, or existing public API for explicit pure contract tests; nested ExitPlayMode is unsupported. No user/foreign scenes/providers/sessions may be reset/deleted; only owned test resources. No other owners/shared asmdefs/vendor314 migration targets, no children/commits/push. Update own canonical docs if actual contract changes and own honest implementation receipt; no test pass inferred from source. Stop writes when done; name unresolved native evidence. Seven new AudioRegressionTests MixerMigration cases call forbidden Edit SendMessage OnValidate, causing ShouldRunBehaviour assertions. Admit actual serialization/native authoring validation or supported top-level Play lifecycle where required; an explicit pure API/migration test must not claim fake native callback coverage. Preserve explicit nondefault target/legacy migration/volume/save controls. Legacy AmEditModeTests trim case is PRIMARY-owned and its setup writes stale _currentMusicEntryVolume instead of actual _musicSourceGain; do NOT edit legacy case or alter production volume policy for it.
