# Neo.Audio implementation plan

Phase 2, 2026-10-23: future work only. Based on the complete PLAN-CONTRACT, [audit](Neo.Audio.md), manifest
and current source/docs/tests. Baseline: `ca198556`, package `10.19.0`. Owner: Neo.Audio; nested Editor excluded.

## Current outcome

The audit reviewed 12 files, changed six runtime files/eight guides, and authored 15 EditMode/nine PlayMode
cases. Completed fixes include lazy source initialization, mixed mute
toggles, rejected music requests preserving state/events, sparse-list no-repeat selection, crossfade
mute/gain/envelopes, standalone controller cleanup/timing, mixer migration and silent UI synchronization.
Public and serialized declarations were preserved. UTF-8,
declaration comparison, delimiter/link/GUID checks and scoped `git diff --check` passed. **No Unity
compilation or behavioral tests ran.**

Paths below use `R = Assets/Neoxider/Scripts/Audio`, `D = Assets/Neoxider/Docs/Audio`,
`E = Assets/Neoxider/Tests/Edit/Audio`, `P = Assets/Neoxider/Tests/Play`.

## Remaining fixes and verification debt

These additional binding defects are source-confirmed during planning, without Unity reproductions.

| Priority / owner | Evidence and reproduction | Smallest correction and observable acceptance |
|---|---|---|
| P2 / Neo.Audio: late settings | `R/View/AudioControl.cs:119`: missing settings causes Start to return before listener setup and `_started = true`; OnEnable at line 96 then always returns. Create the control, let Start finish without settings, add settings and re-enable. | Initialize UI once independently of binding. Retry binding on enable or explicit initialization. The control recovers and one user change updates settings once; preserve authored interactability rather than blindly enabling it. |
| P2 / Neo.Audio: replacement subscriptions | `AudioControl.TryResolveSettings` can replace a destroyed owner, but subscriptions are added only in Start; OnEnable merely refreshes. Destroy settings A, create B, reopen the existing toggle, then mute through B. | Dispose the old subscription before binding B; subscribe on enable and dispose on disable/destroy. B updates the toggle silently; A no longer calls it; repeated reopens never duplicate callbacks. |
| P2 / Neo.Audio: late AM | `R/AMSettings.cs:115-123`: absent AM terminates Start permanently. Create settings first, allow Start, then add AM. This differs from the fixed case where AM already exists with lazy sources. | Add idempotent `Initialize(AM manager)` plus enable-time resolution. Apply loaded startup values once when binding succeeds. Volume/mute setters reach the new sources without repeating persistence or resetting an active session on every enable. |
| P1 integration gate / coordinator | All 24 authored cases and real mixer/persistence behavior remain unexecuted. | After source-changing audits finish, compile and run the new fixtures plus existing Audio coverage. Record actual outcomes; audio-disabled/headless failures are unresolved coverage, not passes. |
| P2 verification / Neo.Audio + coordinator | Rapid voice reuse, observer reentrancy, negative pitch, listener-pause/fade interaction and overlapping focus-mute owners are unverified risks. | Reproduce before prescribing more fixes. Add only regressions that observe a confirmed contract failure; retain the documented single focus-mute owner. |

Binding work changes `R/View/AudioControl.cs`, `R/AMSettings.cs` and
`P/Audit20261023NeoAudioLifecycleTests.cs`. Add explicit initialization and optional serialized scene
references; keep singleton lookup as fallback, without per-frame scene searches.

## Substantial proposal: shared playback session and observable settings

**Problem:** AM's transport/envelope logic remains embedded in a large MonoBehaviour, while
RandomMusicController duplicates transport scheduling. Enabled sliders do not observe external
volume-only changes. This is a documented limitation; continuous observation is a proposed feature.

**Reuse:** retain AudioEntry selection/trims/history, entries, options, transitions, channels, AM pools,
UniTask, SaveProvider and NeoDiagnostics. Reuse Neo.Reactive's `Subscribe(...): IDisposable` handles.
Deterministic core scenarios can specify Track(index).

**Design/API:** introduce ordinary C# `AudioPlaybackSession : IDisposable`, constructed with
`IAudioPlaybackBackend`. Proposed surface: `TryPlay(MusicEntry, MusicOptions)`,
`TryNext(MusicOptions)`, `Stop(MusicTransition)`, `SetChannelState(float volume, bool muted)`,
`Tick(float unscaledDeltaTime)`, `Snapshot`, and `Changed`. The backend starts/configures/stops voices
and queries playback position using logical handles, not Unity instance IDs. The session owns transport
generations and gain/envelope state; adapters own Unity sources and frame/cancellation scheduling.
Snapshots contain entry/clip identity and value state, not scene references. Notify semantic changes
without allocating an observer snapshot every frame.

AM and the standalone controller become adapters, preserving their distinct events. Add
`AM.SetMusicMute(bool)` while retaining direct-source compatibility. AMSettings exposes immutable
`AudioSettingsSnapshot` and `Subscribe(UnityAction<AudioSettingsSnapshot>, bool invokeImmediately)`.
Publish normalized state after supported AM/AMSettings setter paths; direct AudioMixer/AudioSource writes
remain outside automatic notification unless explicitly refreshed. AudioControl consumes snapshots with
silent setters. Existing UnityEvent methods and Custom events remain available.

**Exact future files:** new `R/AudioSimple/AudioPlaybackSession.cs`,
`R/AudioSimple/IAudioPlaybackBackend.cs`, `R/AudioSimple/AudioPlaybackSnapshot.cs`,
`R/AudioSettingsSnapshot.cs`, and unique `.meta` files; adapt `R/AudioSimple/AM.cs`,
`R/RandomMusicController.cs`, `R/AMSettings.cs`, `R/View/AudioControl.cs`. Add
`E/Audit20261023NeoAudioPlaybackSessionTests.cs` with a fake backend; extend the existing nine-case
PlayMode fixture for adapters and live settings observation. All remain in Neo.Audio/existing test
assemblies; no new package or asmdef is required.

**Compatibility:** preserve serialized names/defaults, enum numbers, ids/index migration, legacy methods,
save keys, reactive mute properties, authored sources and ownership boundaries. New scene-reference
fields are optional; no ScriptableObject stores scene objects. Preserve
`Clamp01(channel * entry * trim) * envelope`, same-pool idempotence and callback cardinality/order.
Keep current unscaled fade behavior; freezing fades during global listener pause is a separate product
choice, not an implicit extraction change.

**Effort/risk:** binding 0.5-1 day; extraction 3-5 days; integration one day, excluding tool/version
failures. Medium risk: callback reentrancy, clamp ordering, cancellation
generations and owned-versus-authored source disposal.

## Order, boundaries and acceptance

1. Coordinator freezes audited sources and establishes compile/test results. Fix demonstrated failures;
   then implement the three binding corrections and their lifecycle regressions before extraction.
2. Add the session/fake backend and prove rejected requests leave identity/gain/events unchanged;
   old cancelled generations cannot affect replacements; disposal releases each owned voice once.
3. Adapt AM, then the standalone controller. Verify two outgoing fades, rapid pool changes, stop/restart,
   channel zero/restoration, entry/trim headroom, local/listener pause and zero/resumed pitch. Preserve
   existing audible behavior; no steady-playback scene searches or recurring allocations.
4. Add observable settings and component binding. An external supported volume change updates an enabled
   slider silently; actual input emits one callback; disable/re-enable and owner replacement leak none.
   Verify a saved normalized 0.35 survives reload independently of track trim, using isolated save data.
5. Update `D/README.md`, `AM.md`, `AMSettings.md`, `AudioControl.md`, `View/AudioControl.md` and
   `RandomMusicController.md` with C# initialization/subscription examples and scene/UnityEvent setup.
   Preserve `Samples~` release layout, document development `Samples` and imported sample paths.

Dependencies/owners: Neo.Reactive (subscriptions), Neo.Tools.Managers (singletons), Neo.Save (persistence),
Neo.Extensions (random), Neo.PropertyAttribute (authoring). Consume finalized APIs before integration;
route reproduced boundary defects to their owners. Neo.Audio.Editor reviews settled runtime contracts;
its `Editor/` sources and `D/Editor.md` remain excluded. No mandatory editor edits are planned.

Release acceptance requires the new and existing Audio EditMode/PlayMode coverage, an audio-enabled
crossfade run, real mixer routing and persistence checks, and compilation on a Unity 6.0-6.4 version
and 6.5+. Exercise both compatibility branches centrally through NeoDiagnostics/NeoComponentHealth;
do not scatter version directives. Coordinator owns package/import validation. Stop release completion
for compile failures, lost serialized data, duplicate events, leaked voices/subscriptions or missing
required evidence. This planning phase executes none of those processes and changes only this file.
