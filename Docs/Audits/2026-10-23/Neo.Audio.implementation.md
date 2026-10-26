# Neo.Audio implementation receipt

Status: **SOURCE-FINAL for Neo.Audio**, 2026-10-24. This is an individual source receipt, not a global
source freeze or verification pass. Target: `demo`, package `11.0.0`.
Original actor: `neo-audit-1023-b02-03`.

## Completed assigned changes

| Request | Source result | Observable acceptance authored |
| --- | --- | --- |
| `AUDIO-EDITOR-DOC` | Canonical Audio README links to `Editor.md`; nested Editor ownership is unchanged. | Entry lists, clip controls and id-drawer documentation are reachable from the module entry point. |
| `AUDIO-LATE-BINDING`, C-04/C-05 | AudioControl completes one-time UI setup without settings, supports explicit replacement, retries on enable, and owns an `IDisposable` mute subscription. Disable/destroy/replacement detaches the previous handle. | Late settings recover a disabled input; authored disabled inputs stay disabled; old mute properties have zero listeners after replacement; refresh is silent; disabled views ignore UI input and mute updates. |
| `AUDIO-LATE-BINDING`, C-06 | AMSettings has explicit idempotent manager binding and enable-time retry. Persistence loads once; successful initialization suppresses writes and applies startup state once per manager binding. | Missing-AM startup recovers through explicit initialization or re-enable. Repeating a live binding preserves changed volume, mute and current music. |
| `AUDIO-REBIND-STOP-REENTRY`, C-07 | Rebinding blocks nested Start/Initialize, clears old playback ownership before notification and installs the accepted binding in `finally`. Source/track generations isolate asynchronous loops and superseded starts. | A stopped observer cannot restart the old source during rebind. A throwing observer still leaves the accepted target bound and stopped. Outside rebind, a newer observer-started/paused session supersedes the older Start. |
| C-34, Audio dependency | Added exact-clip release for current/outgoing music, ambience, positional voices and legacy random references. | Releasing owned clips retains other current/outgoing tracks, retains source/clip objects, publishes one primary-stop event, preserves caller arrays and surviving legacy indices. |

Also corrected the stale `StartMusicClip` XML: the proposed volume is committed only after a usable
clip is selected. Earlier audit fixes and regressions are retained. The broad playback-session/settings
framework from the plan remains a proposal and is not part of this implementation.

## Additive API ready for dependency integration

- `AMSettings.Initialize(AM manager)`: explicit late manager binding; same successful live binding is idempotent.
- `AudioControl.Initialize(AMSettings audioSettings)`: explicit late/replacement settings binding; active started
  controls silently refresh. Null restores singleton fallback. Enable-time retries remain available.
- `AM.ReleaseClips(params AudioClip[] clips)`: releases exact matching current music, outgoing crossfade,
  ambience and positional voices; detaches matching legacy random-track references. Other clips/voices,
  authored sources, channel settings and source GameObjects survive. Clips themselves are not destroyed.
  Main music release raises `OnMusicStopped` once after cleanup; outgoing-only release does not announce
  an unrelated current track as stopped. Authored sound/music entry catalogs are unchanged.

**C-34 / Neo.Rpg.Demo handoff:** send independently owned 2D effects through
`am.PlayAt(clip, Vector3.zero, 1f, 0f)` rather than shared `Play(clip)`/`PlayOneShot`.
Before destroying generated clips, unhook demo observers, then call `am.ReleaseClips(ownedClips)` and
destroy only those owned clips. Do not call global `StopMusic`/`StopAmbience` as inferred ownership.
Unity cannot selectively cancel overlapping `PlayOneShot` calls on the same AudioSource; the new API
does not claim to do so. PlayAt is the existing bounded pooled path, including fully 2D playback.

The Demo owner must consume this API in its own generated-clip shutdown path. No sample source was
changed by this worker. This is a remaining cross-owner integration/verification action, not an absent
Audio API. No other assembly implementation is required to use the new Audio APIs: disposable
`Neo.Reactive.Subscribe` already exists, and existing Save/Tools/diagnostics dependencies are reused.

## Exact files written in this implementation phase

Runtime, under `Assets/Neoxider/Scripts/Audio/`:

- `AMSettings.cs`
- `View/AudioControl.cs`
- `RandomMusicController.cs`
- `AudioSimple/AM.cs`

Public documentation, under `Assets/Neoxider/Docs/Audio/`:

- `README.md`
- `AMSettings.md`
- `AudioControl.md`
- `View/AudioControl.md`
- `RandomMusicController.md`
- `AM.md`

Regression source:

- `Assets/Neoxider/Tests/Edit/Audio/Audit20261023NeoAudioRegressionTests.cs`
- `Assets/Neoxider/Tests/Play/Audit20261023NeoAudioLifecycleTests.cs`

Receipt: `Docs/Audits/2026-10-23/Neo.Audio.implementation.md`.
The completed audit and plan are unchanged. Other workers' nested Editor code/docs, legacy fixtures,
samples, shared trackers/configuration, assembly definitions and package settings were not edited.

## Compatibility and ownership

- All new public APIs are additive. Existing public/serialized members and scene singleton fallback
  remain available. No serialized migration or assembly-reference change is introduced.
- There is no polling loop. A dependency arriving after Start requires explicit Initialize or re-enable.
  Null AM initialization leaves the current manager binding alone; null AudioControl initialization
  restores settings fallback. Input callbacks are registered once at Start and removed at destruction;
  while disabled, their handlers do not apply input. Mute handles are disposed on disable.
- AudioControl only restores interactability when its own missing-settings gate changed that value.
- Clip release uses exact references, creates/destroys no Unity objects and leaves authored catalogs
  unchanged. Owners must unregister their own runtime catalog entries and detach their observers before
  releasing/destroying clips. Independent shared-source one-shots remain outside selective release.
- AM's existing bounded positional pool, outgoing voices, channel gains and fade routines are reused.
  Releasing one outgoing voice does not cancel the shared fade routine for unrelated music.

## Regression evidence and deferred verification

Authored fixture inventory: **20 Edit cases and 16 Play cases**, including **5 new Edit tests and
7 new Play tests** in this implementation phase. All 36 cases are **UNEXECUTED on this source snapshot**.
The new cases cover late/replacement dependencies, subscription disposal, authored interactability,
silent refresh, idempotent startup, stop-observer reentry/exception ordering, superseding starts, and
selective release across positional/ambience/current/outgoing/legacy-random paths. The legacy-array
case also checks that the caller's arrays and surviving track indices are preserved.

The fixtures snapshot/restore the singleton auto-create flags, listener pause and random state they
change. Deliberate missing-dependency tests expect the gated error messages when diagnostics are
enabled; they do not suppress or reconfigure diagnostics. No `NonParallelizable` attribute was added.

Validation performed during implementation: manual read-only review of the edited source, related
existing dependency APIs, regression assertions and documentation diffs. No Unity, compiler, test
runner, product checker, installer/service, commit/push or child agent was started. Historical audit
checks are not presented as passes for these new edits.

After every writer finishes, root must compile with the repository's installed editor/configuration
matrix (including Unity 6.3/6.6), run the two owned fixtures with audio enabled, and perform the required
global checks. Play assertions intentionally require real audio playback; an audio-disabled run does
not verify the outgoing/source-lifetime cases. Integration must also exercise Demo shutdown after
switching music while unrelated music is fading and while generated 2D effects overlap, checking that
only owned clips are detached before destruction and that repeated scene teardown leaves no native
clip growth. That integration is jointly dependent on the Demo owner's consumer change.

No assigned Audio-side source fix remains pending. Compilation, runtime timing, package-wide
compatibility and native-resource behavior remain unverified until the central verification phase.

## Snapshot3 AUDIO-VALIDATE-NATIVE-ADMISSION correction — SOURCE-FINAL

Assigned index group **E11**, action **L**: seven cases in
`Assets/Neoxider/Tests/Edit/Audio/Audit20261023NeoAudioRegressionTests.cs`.
The complete assigned failure messages/stacks and the owned Play results were read from:

| Frozen artifact under `Local/Audits/assembly-2026-10-23/unity/final/s3/u63/Receipts/` | SHA-256 | Historical owned-fixture result |
| --- | --- | --- |
| `22-full-edit-graphics.xml` | `7c6d24e11012ed5903fff8f24d603e8130ec4bcae262af3761f4cf772a6e57d2` | 20 Edit cases: 13 passed, 7 failed with `ShouldRunBehaviour()` from Edit `SendMessage`. |
| `21-full-play-graphics.xml` | `2b15216472bdc287418567ed947899346c6a134b33658b07533fa9a1d157dc3a` | All 16 owned Play cases passed on that frozen snapshot. |

Those are historical results, not passes for this correction. The index's fixture/precondition
classification is consistent with the actual source: the four mixer cases manually sent `Awake`,
and the settings helper manually sent `Start`. These calls cannot admit native runtime lifecycle
in Edit mode; no production audio defect is inferred from those failures.

Only the owned Edit fixture and this receipt were changed in this correction:

- The three explicit nondefault target cases (`Music`, `Efx`, `Custom`) and the legacy music-name case
  apply their inputs through `SerializedObject.ApplyModifiedPropertiesWithoutUndo`. Unity's native
  authoring validation must set the initially false migration marker before the existing target
  assertions are accepted. The negative cases explicitly retain `MasterVolume` as the competing
  legacy name; the positive legacy case retains `Master` plus `MusicVolume`. Both also assert the
  authored name survives. No lifecycle method is called manually.
- `SettingsStart_BindsSourcesCreatedByRuntimeInitialization` is renamed to
  `SettingsInitialize_BindsSourcesCreatedByRuntimeInitialization`: this is explicitly the existing
  public `AMSettings.Initialize(AM)` contract. Cold source references must actually be null before
  initialization. Its source-identity, 0.25 effects/0.35 music volume and mute assertions remain.
  This Edit case does not claim automatic `Start` delivery.
- `CreateSettings` now calls that public API, verifies live source binding, and retains and verifies
  `persistVolume = false` before initialization. No save provider is replaced, cleared or written by
  these settings tests. Both Sfx/UI toggle cases retain every original mute/unmute assertion and now
  require the selected channel to be genuinely muted while the other channels remain on beforehand.

Case cardinality remains **20 Edit + 16 Play**; none of the seven failing parameterized cases was
dropped. Original substantive selection, gain, event-count, ownership, array and lifecycle assertions
elsewhere in the fixtures are unchanged. There is no tolerance/default/log-whitelist change,
`ExecuteAlways`, reflection lifecycle bypass or new helper extraction. No runtime/public API,
serialized field, GUID, canonical public documentation or shared configuration changed in this pass;
there is no new public contract requiring a documentation migration.

The current relocation reservation map was checked before each edit. It contains no Audio fixture
or receipt reservation; no helper, reserved caller, Fixtures assembly or shared reference was edited.
The legacy `AmEditModeTests.AmSettingsMusicSlider_WithAnEntryTrim_SavesTheChannelNotTheProduct`
remains **PRIMARY-owned and deferred to PRIMARY**. Its historical 0.4/0.8 failure is not addressed by
changing AM's production gain policy or this worker editing that legacy test.

Actual work verification was read-only artifact/source inspection and review of the fixture diff
against the frozen source archive. No compiler, Unity, tests or product checkers ran. **All 36 cases
remain unexecuted on the corrected current snapshot.** After a new global freeze, root must rerun E11
and the existing native Play acceptance. In particular, actual native `OnValidate` delivery through
serialized authoring remains to be observed; if it does not occur, the new positive marker assertion
must fail rather than be suppressed. This Edit correction does not establish prefab-import/legacy
field-name roundtrip coverage or native `Awake`/`Start` coverage. Existing Play lifecycle/audio results
must be reproduced on the new frozen snapshot and prescribed editor matrix. No further owned source
write is pending for E11; execution is still closed.
