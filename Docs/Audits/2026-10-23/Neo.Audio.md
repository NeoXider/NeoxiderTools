# Neo.Audio: audit

Status: full source review complete; focused fixes, regressions and public documentation written.
Compilation and Unity execution remain centrally unverified.
Phase: audit; the manifest's plan entry is still awaiting audit phase completion. No plan was written.

- Base revision: `ca198556` (package 10.19.0).
- Model: `gpt-6.1-sol`, reasoning `max`.
- Assembly: `Assets/Neoxider/Scripts/Audio/Neo.Audio.asmdef`.
- Scope: all 12 manifest C# files, 5,372 baseline lines; nested `Neo.Audio.Editor` excluded.
- No commits, shared configuration changes, Unity/MCP calls, builds, test runners, installs or child agents.
- Read the complete root `AGENTS.md`, worker contract, exact `Neo.Audio` manifest entry and original report.
  Applied the file-only portions of the NeoxiderTools and Unity Game Agent skills; checked the module and
  extension references before changing helpers. Existing entries, options, channels, source pools,
  `NeoDiagnostics`, reactive properties and `SaveProvider` were reused.

## Scope

Paths below are relative to `Assets/Neoxider/Scripts/Audio`. Every file was reviewed, including unchanged
files. Line counts identify the baseline inventory, not the post-fix size.

| File | Baseline lines | Review coverage and outcome |
|------|---------------:|-----------------------------|
| `AMSettings.cs` | 574 | Singleton/start ordering, lazy source references, saved startup levels, mixer/source normalized readings, reactive mute, channel fan-out, all toggle variants. Fixed cold initialization and mixed-channel toggle decisions. |
| `AudioFocusMute.cs` | 143 | Installation/search, focus and pause callbacks, enable/disable restoration, runtime switches, editor gate and listener volume ownership. No edit; intended deployment remains one global focus-mute owner. |
| `AudioSimple/AM.cs` | 2,628 | One-way serialized migration and legacy fallbacks; id/index/direct-clip/quiet/placeholder paths; gain and trim products; pitched/UI/positional pooling, resize and ownership cleanup; startup, pool identity/events, shuffle watchdog, crossfades, ambience and legacy random playback. Fixed rejected music requests, outgoing mute, stop-fade levels and channel changes during fades. |
| `AudioSimple/AudioEntry.cs` | 470 | Serializable fields/defaults, clip and trim arrays, sparse/null lists, random history, wrapped indices, pitch ordering/clamping, channel/mode enums and transition options. Fixed fallback repeat of the previous clip. |
| `AudioSimple/AudioIdAttribute.cs` | 38 | Attribute constructors, enum defaults and runtime/editor boundary. No defect found. Drawer implementation is outside ownership. |
| `AudioSimple/AudioOptions.cs` | 223 | Default structs, optional overrides, fluent value semantics, channel/spatial selection, volume headroom and transition construction. No edit. |
| `AudioSimple/MusicControl.cs` | 142 | OnEnable playback, id normalization, singleton fallback, null manager handling, fade/instant/volume mapping and UnityEvent signatures. No edit. |
| `AudioSimple/PlayAudio.cs` | 99 | Start-only automatic playback, clips/id/index precedence, random/empty/null clips and negative-volume sentinel. Preserved behavior; corrected the enable/start documentation. |
| `AudioSimple/PlayAudioBtn.cs` | 158 | All pointer/selection/submit/manual modes, click button filter, explicit clip/id/index precedence and legacy index volume compatibility. No edit. |
| `RandomMusicController.cs` | 209 | Initialization/reinitialization, asynchronous cancellation ownership, stop events, local/global pause, pitch timing, invalid tracks and destroyed source lifetime. Fixed terminal cleanup, source replacement and timing. |
| `SettingMixer.cs` | 211 | Normalized/dB boundaries, missing mixer/parameter behavior, preset/custom dispatch and former serialized-name migration. Fixed explicit enum targets being overwritten during first migration. |
| `View/AudioControl.cs` | 477 | UI detection, Start/re-enable synchronization, subscriptions/cleanup, custom state cache, source/mixer backends, mute versus positive volume, UnityEvents and runtime Set calls. Fixed silent sync, mute display/reactivation and custom re-enable. |

Context-only reads: existing Audio EditMode fixtures, relevant teardown PlayMode fixture, the singleton
base, runtime/test asmdefs and all existing Audio guides. No existing tests or nested assembly files changed.

## Findings and fixes

Findings were demonstrated by source control flow and concrete inputs below. These are not claims of
executed Unity reproductions. Regression fixtures are described in the next section.

| ID / severity | Trigger and observable consequence before the fix | Source and completed fix |
|---------------|--------------------------------------------------|--------------------------|
| A1 / P2 | AMSettings starts before an AM with unassigned/lazy sources. It caches null sources, then `ApplyStartVolumes()` creates them. Subsequent settings volume/mute calls return early permanently. | `AMSettings.Start` (current line 115): apply configured startup levels/initialize AM before caching `Efx` and `Music`. |
| A2 / P2 | Only Sfx or UI is muted. `ToggleAllAudio()` previously tested master/music only and turned the still-active channels off instead of restoring all channels. | `AMSettings.ToggleAllAudio` (line 504): include AM's current Sfx and UI mute flags. Existing all-on/all-off behavior is retained. |
| A3 / P2 | A regular empty pool, or `Track(1)` pointing to a null slot, is requested while another pool plays. The pool identity/gain changed before selection failed, and `OnMusicStarted` could announce the previous clip. A failed next-track volume override also contaminated the playing gain. | `AM.PlayMusicEntry`, `TryNextMusicTrack`, `StartMusicClip`: select first; commit identity/gain and emit start only on success. Reject an all-null legacy random pool before it claims the current session. |
| A4 / P2 | Twelve random probes hit null slots or the previous index. The linear fallback returned that same previous clip even with another playable clip available, violating the no-repeat contract. Sparse lists make this common. | `AudioEntry.NextClip` (line 216): fallback searches alternatives first; only a lone usable previous clip may repeat. Trim history remains aligned with the selected index. |
| A5 / P2 | Music is muted after a crossfade hands the old track to another source. Only the primary source/ambience followed later mute changes; the old track remained audible through the fade. | `AM.LateUpdate`: mirror primary mute onto every outgoing fade voice, matching existing ambience synchronization. Unmute follows the same path. |
| A6 / P2 | `StopMusic(Fade(...))` interrupts a crossfade whose outgoing/incoming tracks have different levels. Every outgoing source was reset to the incoming source's level, producing a jump or swell. | `AM.FadeOutAndStopRoutine`: retain each voice's own gain and capture its current envelope before fading it down. |
| A7 / P2 | A positive-volume channel is muted externally, or a view is refreshed with other UI listeners attached. The toggle stayed on because sync ignored mute; sync invoked other listeners because only AudioControl's own listener was detached. Custom controls also skipped re-enable refresh. | `AudioControl.OnEnable`, `SyncToggleState`, `SyncSliderState`, `ApplyActiveForBuiltIn`: use mute-aware silent setters, refresh custom caches, and clear the selected channel mute when explicitly turned on. Real user events remain connected. |
| A8 / P2 | A standalone controller encounters a null track/source destruction, or is initialized with a replacement source during an active/paused session. Terminal paths left an active state/cancellation owner; replacement could leave the old source playing. | `RandomMusicController.Initialize` (line 51), `PlayMusicLoop` (line 159): stop before rebinding; reset history; observe source lifetime and clean terminal state/resources in finally. An older cancelled loop cannot stop its replacement. |
| A9 / P2 | Listener pause is active, or the standalone source has pitch zero/non-unit pitch. The unscaled timer advanced independently of paused or pitch-adjusted playback, skipping tracks or leaving gaps. | `RandomMusicController.PlayMusicLoop`: suspend elapsed track time during listener pause unless `ignoreListenerPause`; scale elapsed audio time by absolute pitch. Existing controller-local pause and unscaled clock remain. |
| A10 / P2 | A newly constructed inactive `SettingMixer` has its public enum set to Music/Efx/Custom before Awake, while the legacy name remains its default. Migration overwrote the explicit enum with Master. | `SettingMixer.TryMigrateLegacyParameter` (line 185): respect an explicit non-default enum; retain name inference for legacy/default-target data. |
| A11 / P2 | `SetMusicVolume(0)` during a crossfade updated only the incoming source, leaving outgoing music audible. Nonzero changes also reset the incoming fade to full target volume for a frame. | `AM.HandOffToFadeSource`, `CrossfadeRoutine`, `FadeOutAndStopRoutine`, `ApplyMusicVolumes`, `SetVolume`, `ApplyStartVolumes`: store channel-independent gain and envelope per live voice, apply the same channel gain to every voice, and preserve progress even through zero volume and stop fades. Clamp gain before the envelope to preserve entry/trim headroom. |

No serialized fields, public signatures, enum values, asmdefs, scenes, prefabs or sample layout changed.
Only private helper signatures changed. Added fade state is runtime-only. Existing NoCode entry points remain callable.

Кратко: проверены все 12 файлов сборки; исправлены ошибки состояния, жизненного цикла, выбора клипов и
синхронизации UI. Проверки Unity не запускались: они остаются задачей координатора.

## Added regressions

- `Assets/Neoxider/Tests/Edit/Audio/Audit20261023NeoAudioRegressionTests.cs`: 15 cases covering sparse
  no-repeat/trim selection and single-clip/null boundaries; rejected pool/track requests preserving
  identity, gain and event counts; successful next-track override retention; all-null legacy random
  requests; cold source binding; independently muted Sfx/UI; standalone terminal/reinitialization state;
  explicit and legacy mixer migration.
- `Assets/Neoxider/Tests/Play/Audit20261023NeoAudioLifecycleTests.cs`: 9 cases covering real outgoing
  crossfade mute and relative fade levels; channel volume changes through zero, clamping, envelope
  preservation, rapid pool replacement and stop fades; listener pause and zero/resumed pitch; destroyed source
  termination; silent UI initialization with functioning user input; external mute reactivation; custom
  re-enable state. Fixtures restore listener pause and stop owned asynchronous sessions during cleanup.
- Assertions observe clip/source state, volume products, identities and event counts. No source-text,
  no-throw-only or duplicate implementation tests were added. SerializedObject/JSON is setup for private
  serialized authoring fields; runtime actions use public APIs and normal PlayMode lifecycle.
- Both filenames have the exact manifest prefix. Both `.meta` GUIDs were checked unique. UI lifecycle
  tests use the existing Play assembly's `UnityEngine.UI` reference; no shared asmdef edit is required.
- Authored: **24 cases. Executed here: 0**, as required by the worker contract.

## Documentation

Updated only existing owned guides (English):

- `Assets/Neoxider/Docs/Audio/README.md`: canonical runtime/scene entry points, standalone-helper distinction,
  silent UI refresh and verified release/development/imported sample paths.
- `AM.md`: failed selection contract, outgoing mute synchronization, per-voice stop fades and immediate
  channel gain changes without losing fade progress.
- `AMSettings.md`: lazy initialization order, mixed-channel all-audio toggles and UnityEvent usage.
- `AudioControl.md` and `View/AudioControl.md`: actual lifecycle/mute refresh boundaries, silent setters,
  external mute reactivation, custom cache and mixer-free backend choice. Removed the unsupported claim
  of continuous external volume observation.
- `RandomMusicController.md`: replaced the incorrect addable-component/Inspector workflow with plain C#
  initialization, lifetime ownership, state/events, listener pause, pitch and terminal behavior.
- `SettingMixer.md`: accurate normalized versus dB mapping, C# API and migration behavior.
- `PlayAudio.md`: automatic playback runs once in Start, rather than on every enable.

No root/package docs, excluded nested-assembly topics, DocsEn files or shared changelog were edited.

## Substantial improvement proposal

### Proposal P1: explicit playback session and observable channel state (not implemented)

**Problem.** AM mixes selection, transport, gain/envelopes, pooled Unity sources, serialization and
singleton behavior in one 2,600+ line component. The standalone controller duplicates transport logic.
This makes cancellation, pause and event order hard to verify independently of Unity's player loop.
This audit fixes missing per-voice gain/envelope state locally, but that state still lives inside the
component and coroutine implementation. AudioControl has reactive mute notifications but no equivalent
channel-volume notification; replacing a settings instance also requires rebuilding observer ownership.

**Proposed architecture / public API.** Reuse `MusicEntry`, `SoundEntry`, `MusicOptions`, `SoundOptions`
and existing channel contracts; do not introduce a parallel catalog. Extract an ordinary C#
`AudioPlaybackSession : IDisposable` with an injected clock/random picker and a narrow voice backend
implemented by AM's existing pools. Expose `TryPlay(MusicEntry, MusicOptions)`, `TryNext(MusicOptions)`,
`Stop(MusicTransition)`, `SetChannelState(float volume, bool muted)` and `Tick(float unscaledDeltaTime)`.
Publish an immutable playback snapshot/change event with entry/clip identity, transport state, channel
gain and per-voice fade envelopes. Move the now-explicit gain/envelope state and
`Clamp01(channel * entry * trim) * envelope` calculation into that testable session rather than
duplicating it between Unity source owners.

AM remains the scene singleton/authoring wrapper and Unity source backend. The standalone controller
adapts the same session in Shuffle mode. AMSettings remains the persistence adapter and publishes
channel-state changes through the existing reactive layer. AudioControl subscribes on enable and
unsubscribes on disable to observe both volume and mute, retaining silent view updates. These are typed
C# APIs; current UnityEvent components delegate to the same contracts.

**Compatibility / migration.** Introduce the session behind existing AM APIs without renaming/removing
serialized fields or changing enum values/ids/index migration. Keep the legacy methods and current
reactive mute properties as adapters. Preserve event cardinality and document event ordering. Seed the
new session from the currently authored source/channel values. No scene references enter a
ScriptableObject; no mandatory replacement of existing views or saving keys. Roll out transport and
channel observation in separate reviewed steps with old contract fixtures retained.

**Acceptance scenarios.**

1. Change channel gain to 0, 0.25 and 1 during two overlapping outgoing fades: every voice immediately
   obeys channel gain without resetting its envelope; silence and restored gain work even from zero.
2. Reject an empty/missing explicit track: clip, pool identity, gain and start-event count are unchanged.
3. Pause/resume and cancellation/replacement are deterministic under a fake clock/backend. A cancelled
   old session cannot mutate a replacement; destruction releases all owned resources once.
4. Source and mixer backends produce matching normalized state; existing serialized legacy data and
   save keys retain their documented behavior.
5. An external volume/mute change updates enabled UI silently. Disable/re-enable, replacement of the
   settings instance and listener removal leave no stale subscriptions or duplicated input callbacks.
6. Existing regression fixtures pass centrally; add deterministic core tests without requiring an audio
   device, plus a small real-source PlayMode integration set.

**Effort / risk.** Approximately 3-5 development days plus Unity 6.0-6.4 and 6.5+ integration validation.
Medium risk: fade clamping order, legacy callback ordering, observer reentrancy and source ownership need
explicit compatibility assertions. No implementation of this architecture is claimed in this audit.

## Verification and remaining risks

Completed checks (actual execution):

- Scoped `git diff --check -- <changed owned source/docs/new fixtures>`: exit 0; no whitespace errors.
  Git emitted only the existing LF-to-CRLF worktree policy notice for `RandomMusicController.cs`.
- Python strict UTF-8 reads, replacement-character scan, explicit-type (`var`) scan and prohibited old
  Unity instance-id API scan: passed on all 12 inventory C# files plus both new fixtures.
- Static delimiter sanity after masking comments/strings: passed for all 14 C# files. This is not a
  compiler or behavioral test.
- Public declaration and `[SerializeField]` declaration comparison against `ca198556`: unchanged in all
  12 inventory files. Final static checks and scoped diff whitespace validation passed after the gain/
  envelope fix and the 24-case fixture update.
- New fixture final newlines/whitespace and `.meta` presence: passed. `rg` GUID lookup found each of the
  two new GUIDs only in its own `.meta`.
- Relative Markdown links resolve in all eight changed guides. `rg --files` confirmed the documented
  Audio demo scene/controller exist under the release `Samples~` root.
- Inspected existing runtime/Edit/Play asmdef references: fixtures fit existing references; none edited.

**Coordinator actions / unverified risks:** compile/import and run the two new fixtures and existing Audio
EditMode/PlayMode coverage centrally. The three outgoing-crossfade tests need enabled Unity audio and a
running player loop. Also verify real mixer routing/persistence and the Unity version matrix centrally;
file-level checks cannot establish their runtime behavior. No test or build passed here is claimed.

**Remaining design limits:** continuous external UI volume observation and settings-instance subscription
replacement are deferred to proposal P1. They are not represented as completed features. AudioFocusMute
still assumes a single global listener-volume owner. New fade gain/envelope state needs central PlayMode
validation for rapid repeated switches, source volume headroom and stop/restart transitions.

No cross-assembly code edit/reference request is required. Nested editor code and all shared operations
remain with their owners/coordinator.
