
## [Unreleased]

## [10.13.4] - 2026-08-21

### Fixed

- **The 0..2 volume headroom introduced in 10.13.1 was unreachable.** The entry slider and the per-clip
  trims were authored up to `AudioEntry.MaxVolume`, but every playback path ran the multiplier through
  `Mathf.Clamp01` before using it - so an entry set to `2` played at exactly `1` and the slider moved
  with nothing to hear. Entry volume, clip trim and the per-call `SoundOptions` / `MusicOptions`
  overrides now keep their headroom. The *audible* music level is still capped at `1`, because
  `AudioSource.volume` genuinely is `0..1`; the headroom pays off against a channel the player has
  turned down.
- **`MusicControl`'s volume override was capped at 1** for the same reason - a no-code music trigger
  could only ever make a pool quieter. Its slider now spans `-1..2` like the entry it overrides.
- **`PlayAudio` / `PlayAudioBtn` threw away the entry's volume.** Their `_volume` field *replaces* the
  entry's own multiplier, and it defaulted to `1` - so pointing a fresh component at a sound entry
  authored at `0.5` played it at full, with the entry's slider apparently doing nothing. The default is
  now `-1`, meaning "use the entry's volume"; set it to `0..2` to override deliberately. Components
  serialized before this carry an explicit `1` and go on overriding exactly as they did.
- **A new `AM` entry inherited the previous entry's per-clip trims.** Unity copies the last element when
  an array grows, and `ResetEntry` cleared everything except `_clipVolumes`. Because trims are keyed by
  clip *index*, the leftovers did not read as stale data - they silently re-levelled whichever clip
  landed in the copied slot, so a freshly dropped clip could play at 40% with nothing in the inspector
  to explain it.

### Added

- **Per-clip volume trims are editable.** `_clipVolumes` has been serialized since 10.13.1 and is
  described in its own tooltip, but the entry drawer never drew it, so the only way to set a trim was
  from code. The expanded entry now lists each clip as a row - the clip on the left, its trim slider on
  the right - and add/remove keeps the two index-aligned arrays in step. Untouched clips still store no
  trim at all, so entries authored earlier load unchanged.

### Changed

- Runtime audio code no longer uses `var`, per the explicit-types rule in `AGENTS.md`.

## [10.13.3] - 2026-08-20

### Fixed

- **Selecting an `AM` spammed the console with `GetLastRect` errors.** The new drop-on-the-list handler
  measured the list with `GUILayoutUtility.GetLastRect()`, which logs an error on the Layout pass because
  nothing has been measured yet. The list is now wrapped in a vertical scope, which reports the same area
  and stays silent during Layout.

## [10.13.2] - 2026-08-20

### Changed

- **The AM `Authoring` block is gone; its drop behaviour moved onto the lists.** A separate block with
  `+ Sound` / `+ Music Pool` buttons and two drop boxes sat below the fields it operated on, and its two
  buttons duplicated the `+` the entry lists already carry. Dropping clips is now done on the
  **Sound Entries** / **Music Entries** list itself — anywhere on the list adds one entry per clip, on an
  existing entry's row adds them as variations of that entry. One gesture, aimed where the result lands.

### Fixed

- **A section holding a single list rendered as a bare label instead of a folder.** The one-field
  shortcut in `CustomEditorBase` ran before the force-foldout rule, so `Sounds` (one array) and `Music`
  (an array plus two settings) were styled differently in the same inspector. The shortcut now yields to
  the force-foldout rule.

### Added

- **`CustomEditorBase.DrawCustomProperty`** — a derived inspector can take over one field's rendering and
  call `DrawStandardProperty` to keep the standard look while adding to it.

## [10.13.1] - 2026-08-20

### Fixed

- **`CustomEditorBase` did not compile.** Extracting the section bar into `NeoxiderEditorGUI` dropped the
  trailing `countColor` argument, but `Docs`, `Events` and `Actions` still passed it — three CS1501s, so
  the whole editor assembly was dead. The colour is now derived from the accent inside the shared method,
  and the three call sites pass six arguments like everyone else.

### Changed

- **`AudioEntry` volume range is now 0..2**, for the entry and for per-clip trims. These are multipliers of
  the channel, so a ceiling of 1 could only ever make a clip quieter than the channel — a sample mastered
  too quietly could never be lifted without re-exporting the file.
- **The collapsed entry row leads with the clip, not the volume.** An empty or single-clip entry shows its
  object field right on the row, so a fresh entry is filled by one drag with nothing to expand; a multi-clip
  entry offers a "Show clips" button instead. Volume moved into the body: it is tuned once, clips are
  assigned constantly.
- **`Randomize Pitch` is drawn as the standard ON/OFF pill** through the shared `DrawPillToggleField`,
  instead of a raw checkbox that read as a foreign island in an otherwise styled inspector.

## [10.13.0] - 2026-08-20

### Added

- **`AM`: one record contract for sounds and music.** A cue is now an `AudioEntry`: an optional **id**, a
  **set of clips** (a random one plays each time, never the same twice in a row), a **volume multiplier**
  and an optional **pitch range**. Entries are addressable by index *and* by id, so `AM.I.Play("hit")`
  survives reordering the list. Two ready ways to break up a repeated cue - several clips, or a pitch
  spread - and both work together.

  Volume **multiplies**: what you hear is `channel volume x entry volume`. A music channel at `0.3`
  playing an entry at `1` comes out at `0.3`. Pitch randomisation defaults to **on** for sounds and
  **off** for music.

- **`AM`: music entries are pools, with two modes.** A music entry holding several clips starts on a
  random one; `MusicPoolMode.Loop` (the default) then holds that track until the game says otherwise,
  and `MusicPoolMode.Shuffle` crossfades on to another track when the clip ends - what the old
  `EnableRandomMusic()` did, now one option among two rather than the only behaviour.

  A menu / gameplay / boss soundtrack is therefore three entries with three ids configured in the
  inspector, and the game only ever says `AM.I.PlayMusicPool("boss")` or `AM.I.NextMusicTrack()`. The
  component each project used to write on top of `SetRandomMusicTracks` is no longer needed.

- **`AM`: music crossfades by default.** Every music change - pool switch, `PlayMusic`,
  `PlayMusicByClip`, a shuffle advance - fades across a configurable `Music Fade Duration` (default
  `0.8s`), overridable per pool and per call. `MusicTransition.Instant` cuts hard,
  `MusicTransition.Fade(2f)` sets a one-off length.

  A crossfade needs two sources, so the outgoing track is handed to a hidden second `AudioSource` at its
  exact playback position while the primary `_music` source takes the incoming one. Keeping the primary
  source on the *incoming* track is what lets `Music`, `GetCurrentMusicClip()` and every existing volume
  tweak go on pointing at the track you can actually hear. Fades run on `Time.unscaledDeltaTime`, so
  `Time.timeScale = 0` does not freeze them, and re-asserting the pool that is already playing is a
  no-op instead of a fade into itself.

- **`AM`: per-call overrides.** `SoundOptions` and `MusicOptions` override an entry's volume, pitch and
  clip choice for **one play only**, without touching the configured entry -
  `AM.I.Play("ui", SoundOptions.Volume(0.6f).WithoutPitch())`,
  `AM.I.PlayMusicPool("boss", MusicOptions.Volume(0.5f).WithFade(2f))`. A volume override replaces the
  *entry* volume and is still multiplied by the channel, so the player's own volume slider keeps
  working - which is the point of the `SetVolume` / play / `SetVolume` dance it replaces.

- **`AM`: no-code parity.** The inspector gains a bulk drop zone (drop N clips, get N entries named
  after them), drag-several-clips-onto-a-row to fill one entry, a collapsed row that already shows id,
  clip count and volume slider, duplicate-id and empty-entry warnings, and a live "now playing" readout.
  `PlayAudio` and `PlayAudioBtn` gained a **Sound Id** field with a dropdown of the ids actually
  configured on `AM`, and a new `MusicControl` component starts a pool, moves to the next track or stops
  the music from a UnityEvent. `Play(string)`, `PlayMusicPool(string)`, `NextMusicTrack()` and
  `StopMusic()` all take zero or one argument on purpose, so a UnityEvent can call them directly.

  The `AM` inspector derives from `CustomEditorBase` and only adds blocks after the standard property
  pass, so the banner, docs foldout, health panel and section rails are untouched.

### Changed

- **`AM`: `PlayMusic(int)` and `PlayMusicByClip` now respect the music channel volume.** Previously they
  wrote the requested volume straight onto the `AudioSource`, so `SetMusicVolume(0.3f)` followed by
  `PlayMusic(0)` played at full volume and quietly discarded the player's setting. They now follow the
  `channel x entry` contract like everything else. With the channel at its default `1` the result is
  identical to before; a project that lowered the music channel will now actually hear it.

- **`AM`: the music `AudioSource` volume authored in the inspector is now the music channel.** It is
  adopted once, at runtime init, and everything else multiplies against it. Before, the channel started
  at `1` regardless of what the source said - so a project that had turned the source down and relied on
  `EnableRandomMusic()`, which never wrote the volume, would suddenly have played its soundtrack at full.
  This also collapses two competing numbers into one: `SetMusicVolume` and the authored value.

- **`AM`: `StopMusic()` fades out** instead of cutting, on the same default duration.
  `StopMusic(MusicTransition.Instant)` restores the old behaviour. `OnMusicStopped` still fires
  immediately, so event counts are unchanged.

### Deprecated

- **`AM.EnableRandomMusic()` and `AM.SetRandomMusicTracks(...)`.** Both still work exactly as before -
  the track list is played as a shuffle pool - but the modern shape is a music entry with several clips
  and `MusicPoolMode.Shuffle`, addressed by id. Nothing is removed in this release.

### Compatibility

- **Old scenes keep playing.** `_sounds`, `_musicClips`, `_randomMusicTracks` and `_useRandomMusic` are
  still serialized and are migrated into the new entry lists on first load, once, guarded by a stamped
  data version so an intentionally emptied list is not repopulated. Legacy `Sound` records inherit the
  manager's old global pitch switch, and the old `volume == 0` meaning "full" is folded into the
  migrated value rather than carried into the new contract, where zero means zero. Music indices are
  preserved (one entry per clip, in order) and the random list is appended after them as a pool, so
  `PlayMusic(int)` still resolves to the same track.

- Every pre-10.13 member - `Play(int)`, `Play(int, float)`, `Play(AudioClip)`, `Play(AudioClip, float)`,
  `PlayMusic(int)`, `PlayMusic(int, float)`, `PlayMusicByClip(...)`, `StopMusic()`,
  `SetRandomMusicTracks`, `EnableRandomMusic`, `DisableRandomMusic`, `IsRandomMusicEnabled`,
  `GetCurrentMusicClip`, `SetVolume`, `SetMusicVolume`, `SetEfxVolume`, `ApplyStartVolumes`, `Efx`,
  `Music`, `RandomizePitch`, `SetPitchRange`, `OnMusicStarted`, `OnMusicStopped`,
  `OnRandomMusicTrackChanged` - is still present and behaves as documented above.
## [10.12.0] - 2026-08-20

### Fixed

- **Scene Saver re-saved the same scene forever and did it inside the editor tick.** The backup is
  written with `EditorSceneManager.SaveScene(..., saveAsCopy: true)`, which deliberately leaves the
  edited scene **dirty**, while `SceneSaverGUI.SaveSceneClone` used that same dirty flag as its "not
  backed up yet" trigger. A scene that stays dirty — the normal state while you work, and the permanent
  state when some tool keeps dirtying it — was therefore re-serialized in full every 3 minutes for as
  long as the editor stayed open. It was the amplifier behind the UI Mesh Rig freeze fixed below
  (`SceneSaver.BackgroundSaveCheck` in the editor's "Hold on" dialog), and it fires from any dirty
  scene, not only from a rig preview. Scheduling now runs on a scene **revision** (scene path +
  `Undo.GetCurrentGroup()` + observed clean-to-dirty transitions) held by the new
  `SceneSaverAutoSaveScheduler`: the same revision is never written twice, in any mode, so another copy
  is only possible after the scene actually changes.
- **The auto-save no longer blocks the editor tick.** `SceneSaver.BackgroundSaveCheck` used to call
  `SaveScene` straight from `EditorApplication.update`. The tick callback now only compares numbers and
  strings, and hands the save to `EditorApplication.delayCall` (one queued save at a time). Checks are
  additionally skipped in Play mode, while the editor compiles or imports assets, and while a prefab
  stage is open — in prefab isolation the "active scene" is the stage, so the old code could write a
  bogus `<prefab>_AutoSave.unity`. Batch mode is skipped entirely: there is no user to protect there,
  and a backup written into the repository during a CI run is pure side effect.
- **Auto-save can finally be switched off for good.** The settings existed only inside the window's GUI
  instance, and the background checker owned a *second* instance of its own — so the toggle in the
  window did not stop the background saver at all, and nothing survived a domain reload or a restart.
  Settings now live in `SceneSaverSettings`, persisted in `EditorPrefs`
  (`Neoxider.SceneSaver.Enabled`, `.IntervalMinutes`, `.SaveEvenIfNotDirty`) and shared by the window
  and the background check. Defaults are unchanged: enabled, 3 minutes.
- **A failing auto-save no longer hurts the editor.** `BackgroundSaveCheck` is wrapped: an exception
  detaches the callback (instead of repeating every tick) and logs how to re-arm it; the deferred save
  catches its own failures and still marks the revision handled, so a scene that cannot be written is
  not retried on every tick. `EditorApplication.update`, `EditorSceneManager.sceneOpened`,
  `AssemblyReloadEvents.beforeAssemblyReload` and `EditorApplication.quitting` are subscribed with a
  paired `-=` and released on assembly reload and on quit. Reading scene state moved out of the
  `[InitializeOnLoad]` static constructor into `delayCall`.
- **A zero interval is no longer accepted.** `Interval (minutes)` is clamped to 0.25; the field used to
  take `0`, which means "save on every editor tick".

- **UI Mesh Rig `Start Preview` could freeze the Unity Editor.** `UIMeshRigMotionPreviewDriver` held a
  permanent `EditorApplication.update` subscription created at `[InitializeOnLoad]`, and every tick — in
  every project, with or without a rig in the scene — ran `Resources.FindObjectsOfTypeAll<UIMeshRigPointMotion>()`
  and then `SceneView.RepaintAll()` + `EditorApplication.QueuePlayerLoopUpdate()`. The preview also wrote
  serialized state each tick, so the scene stayed permanently dirty and the package's own auto-save
  (`SceneSaver.BackgroundSaveCheck`, itself an `EditorApplication.update` callback) kept writing a full
  scene copy — the callback named in the editor's "Hold on / busy" dialog. Now the driver keeps a registry
  of active previews, subscribes to `EditorApplication.update` **only** while one of them actually needs
  ticks, and unsubscribes as soon as the last one stops.
- **Edit Mode preview stopping is now guaranteed.** Previews of objects that are no longer selected are
  dropped on selection change *and* whenever any rig inspector is disabled, so closing or re-targeting an
  Inspector cannot orphan a running preview. Previews also stop on
  `AssemblyReloadEvents.beforeAssemblyReload`, on `EditorApplication.playModeStateChanged` and on
  `EditorApplication.quitting`; every one of those handlers is unsubscribed again on shutdown. A motion
  that throws is dropped from the registry instead of taking the shared editor callback down with it.
  The teardown is deliberately selection-based and never reads `Editor.target`: an Editor created through
  `ScriptableObject.CreateInstance` has no target array, and Unity's own `target` getter throws on it
  while `OnDisable` runs inside `DestroyImmediate`.
- **Preview no longer writes the point Transform.** Dragging the bind anchor used to run
  `point.transform.position = NormalizedToWorld(RestCenter)` unconditionally, which snapped an authored
  pose back onto its anchor — the reported "moving the anchor resets the position". The write is now
  skipped while the point is previewed or the rig is in `Pose / Animate`. `UIMeshRigLayoutBuilder` (a
  runtime API) no longer flips the rig's serialized authoring mode as a preview side effect; the explicit
  editor action `Apply Layout & Preview` still sets `Pose / Animate`, inside its own Undo group.
- **The rig no longer re-binds a previewed point behind the user.** `SynchronizePointTransforms` writes
  bind centre, anchors and rest TRS from `LateUpdate` with no Undo entry; the preview forces a player-loop
  update every editor tick, so any stray `Transform.hasChanged` was silently baked into the asset and left
  the scene dirty. Points carrying a procedural pose are skipped in `UIMeshRigGraphic`,
  `UIMeshRigWorldRenderer` and `UIMeshRigSpriteRenderer`.
- **`UIMeshRigPointMotion.PreviewInEditMode` is no longer serialized.** As a `[SerializeField]` the
  Start Preview button dirtied the scene and the preview survived save, domain reload and Play Mode.
- **Preview registry could skip entries or index past its end.** Stopping a preview raises
  `EditModePreviewStateChanged`, whose handler edits the same list the tick and selection loops were
  walking by index. Both loops now run over a copy.
- **Inspector repaint loop.** `UIMeshRigPointEditor` called `SceneView.RepaintAll()` from
  `OnInspectorGUI` while `RequiresConstantRepaint()` was true, so the Inspector and Scene view repainted
  each other for as long as a motion component existed — including while the preview was paused or
  stopped. Constant repaint is now bound to a preview that is actually advancing.
- **`AM`: the effects volume slider stopped working for pitched one-shots.** A voice from the pitch pool
  introduced in `10.11.0` copied `_efx.volume` once, when it was created. `SetEfxVolume` writes
  `_efx.volume`, so turning effects down silenced the plain one-shots and left every pitched one at the
  volume that happened to be current when its voice was first needed. Routing, volume, mute, spatial blend
  and the bypass flags are now mirrored from `_efx` on every shot. Resizing the pool from the Inspector in
  Play Mode also no longer orphans the voices it already built.

### Added

- `Assets/Neoxider/Tests/Edit/Editor/SceneSaverAutoSaveTests.cs` — nine EditMode tests covering the
  scheduler (an unchanged scene is never written twice, a changed one still is, a scene dirtied again
  earns exactly one more copy, an unsaved scene is skipped) and the persisted settings (the disabled
  state, the interval and the not-dirty option survive a restart, the interval is clamped, reset clears
  the keys).
- `Reset Settings` button in the Scene Saver window.

### Changed

- The Mesh Rig Point Motion transport shows preview state (`Stopped` / `Playing` / `Paused` with the
  current time), disables `Start Preview` while a preview is already running, and states the preview
  contract next to the buttons instead of hiding it inside one preset's hint.
- `Assets/Neoxider/Docs/UI/UIMeshRig.md` is now English like every other page under `Docs/`, and
  documents the Edit Mode preview contract.

## [10.11.0] - 2026-08-20

### Fixed

- **UI Mesh Rig preview could peg the editor.** The edit-mode preview driver called
  `EditorApplication.QueuePlayerLoopUpdate()` on every tick; that schedules another editor tick,
  which re-enters the driver, which queues another - a self-sustaining loop running as fast as the
  machine allows. Nothing ended it, because the motion presets are continuous and `IsPlaying` never
  returns to false on its own, and `PreviewInEditMode` is serialized, so a scene saved with a live
  preview restarted the loop on load with nothing on screen to explain the load. The queue call is
  gone - `SceneView.RepaintAll()` already animates the view - and the driver now steps at most sixty
  times a second.

- **Package version parity.** `README.md`, `Assets/Neoxider/README.md`, `PROJECT_SUMMARY.md`,
  `Docs/README.md`, `Docs/PackageCompatibility.md`, `Docs/Samples.md`, `AGENTS.md` and
  `Skill/neoxider-tools/SKILL.md` still advertised `10.10.0` (the repo-root README `10.8.4`) against
  a `10.10.2` package, so `PackageVersionParityTests` was already failing before this release.

### Added

- **`AM`: random pitch for sound effects.** New `_randomizePitch` toggle with a `_pitchMin` /
  `_pitchMax` range (default `0.94`-`1.06`) and `_pitchVoices` pool size, plus the `RandomizePitch`
  property and `SetPitchRange(min, max)` for code. Repeated cues - a blade hit, a button, a coin -
  stop sounding like the same sample on a loop.

  The pitch is NOT set on the shared `_efx` source: `AudioSource.pitch` applies to the whole source,
  so it would also retune the one-shots still ringing on it - and overlapping shots are precisely
  the case the feature exists for. Each pitched shot takes a voice from a small round-robin pool
  parented to `_efx`, copying its mixer group, spatial blend and volume. With the toggle off the
  path is byte-for-byte the old one and nothing is allocated. Music is never pitched.

## [10.10.2] - 2026-08-13

### Fixed

- **`UIMeshRigMenu` broke compilation on Unity 6.5+.** `ProjectWindowUtil.CreateAssetWithContent` is
  an ERROR-level obsolete there, so the call fails the build instead of warning. Now guarded by
  `UNITY_6000_5_OR_NEWER` and routed to `CreateAssetWithTextContent`; older editors keep the old call.

## [10.10.1] - 2026-08-13

### Fixed

- **`UIMeshRigGraphic` broke every player build.** `Graphic.OnValidate` is declared under
  `UNITY_EDITOR`, so overriding it unconditionally compiles fine in the editor and fails the player
  build with `CS0115: no suitable method found to override`. The override is now wrapped in
  `#if UNITY_EDITOR`. This is invisible to edit-mode tests and to the console — only a real build
  catches it, which is exactly how it was found (a WebGL build of a consuming project).

## [10.10.0] - 2026-08-12

### Added

- **UI Mesh Rig now supports a plain `SpriteRenderer`** (`UIMeshRigSpriteRenderer`), so sorting layers,
  2D lights, sprite masks and SRP batching keep working while the artwork deforms. The imported Sprite
  asset is never touched: the component renders a runtime clone and hands the original back on disable.
  10.9.0 had documented this adapter as impossible — it is not.
  - The clone is written through the public `UnityEngine.U2D.SpriteDataAccessExtensions`
    (`SetVertexCount` / `SetVertexAttribute` / `SetIndices`), which ships in `UnityEngine.CoreModule` and
    needs no 2D Animation package. `Sprite.OverrideGeometry` is deliberately *not* used: measured in a live
    6000.3.14f1 editor it is a silent no-op on a runtime sprite (vertex count and positions unchanged) and
    only takes effect on the imported asset — exactly the shared-state mutation to avoid.
  - `Bounds Headroom` inflates the clone's culling bounds, which Unity derives from the sprite rect and
    never grows with written geometry, so a strongly warped sprite is not culled early at the screen edge.
- `GameObject > 2D Object > Neoxider UI Mesh Rig (Sprite Renderer)` creation menu.
- Scene-view overlay for every rig: Setup / Pose mode switch, Move / Rotate / Scale tool, and `Labels` /
  `All rings` readability toggles.

### Changed

- **The UI Toolkit host binds to `PanelRenderer`, not `UIDocument`.** From Unity 6.4 world-space UI Toolkit
  renders through `PanelRenderer`, so the host subscribes to its UI-reload callback and adds the element to
  the root it hands out; `UIDocument` is now only the fallback for editors that have no `PanelRenderer`
  (verified by reflection: the type does not exist in 6000.3). `[RequireComponent(typeof(UIDocument))]` is
  gone — it forced the legacy component onto projects that had already migrated.
- **Rig inspectors are attribute-driven again.** Fields carry ordinary `[Header]` / `[Tooltip]` and are
  drawn by `CustomEditorBase`, which is what produces the collapsible sections with counts, the ON/OFF
  switches and the coloured rails everywhere else in the package; the custom editors keep only what no
  attribute can express (layout button, diagnostics, point list, Scene handles). `Raycast Target`,
  `Raycast Padding` and `Maskable` are visible again instead of being buried inside a collapsed
  `Advanced Rig Controls` foldout, and the authored raycast padding moved to a hidden field so the same
  value is no longer shown twice. The `Script` field is left exactly where Unity puts it.
- **Scene handles are readable and usable in both modes.** Bind-pose handles (anchor and radii) used to
  exist only in Setup, so Pose looked like the anchor and the rings could not be moved at all. Unselected
  points now draw one faint outer ring without a label instead of two solid ellipses plus overlapping
  names, radius handles exist on all four sides (±X, ±Y), and the anchor is larger with a contrast ring.
  Labels are dimmed through their own `GUIStyle` because `Handles.Label` ignores `Handles.color`.
- One owner-generic implementation (`IUIMeshRigOwner`, `UIMeshRigOwnerResolver`) replaces the per-renderer
  copies of point resolution, layout application, undo bookkeeping and inspector blocks. A point finds its
  rig by nearest ancestor implementing the interface instead of a hard-coded list of component types.
- `UIMeshRigMotionProfile` fields are PascalCase per the package rule, with `[FormerlySerializedAs]` so
  profiles authored under the old camelCase names keep their values.
- The remaining `EditorUtility.DisplayDialog` guards in the Image conversion menu became console warnings,
  matching the conversion notice — a modal dialog hangs MCP-driven automation.

## [10.9.0] - 2026-08-12

### Added

- UI Mesh Rig now has one renderer-neutral geometry/deformation core and three output adapters: existing
  uGUI `UIMeshRigGraphic`, Unity 6 UI Toolkit `UIMeshRigElement`, and world-space
  `UIMeshRigWorldRenderer` for `MeshFilter`/`MeshRenderer` scenes without Canvas.
- Added UI Toolkit UXML/UI Builder support, atlas-safe UV handling, world/UI Toolkit creation menus,
  matching Module inspectors, reusable layout presets, adapter-equivalence tests, and a six-example demo.

### Changed

- Documented why plain `SpriteRenderer` is intentionally unsupported: Unity's supported deformable Sprite
  workflow requires the optional 2D Animation package and sprites authored with bones and weights.

## [10.8.4] - 2026-08-12

### Fixed

- Added the getter-only `ILayoutElement.maxWidth` and `maxHeight` members required by the uGUI version
  shipped with Unity 6.6. UI Mesh Rig remains unconstrained by maximum layout size and continues to
  compile on earlier Unity versions where these ordinary public properties are not interface members.

## [10.8.3] - 2026-08-12

### Fixed

- **Static state left over between Play Mode sessions when Domain Reload is disabled.** Consumer projects
  that enable Enter Play Mode Options never reload the domain, so package statics survive into the next
  session. Four remaining holders now reset themselves, joining the modules that already did:
  - `AbilitySystemBehaviour` releases its scene hub, which had been keeping the previous session's
    `AbilitySystem` and every registered unit alive and handing a destroyed component to `InstanceOrNull`;
  - `DamageService` clears its shield scratch pool and zeroes the re-entrancy depth, so a session cannot
    start on top of the previous session's `ModifierInstance` graph or a misaligned nesting level;
  - `StateMachineEvaluationContext` drops the current context and its push/pop stacks, so an evaluation
    aborted by an exception cannot leave a destroyed `GameObject` as the transition context;
  - `PrefabPreviewExtensions` clears its preview-sprite cache — the old `[InitializeOnLoadMethod]` hook
    only fires on a domain reload and therefore never ran for these projects.

  Each reset runs from `SubsystemRegistration` on every editor, and additionally from `[OnExitingPlayMode]`
  on Unity 6.5+ so the editor stops holding destroyed objects the moment play stops.
- Types using the `[OnExitingPlayMode]` lifecycle attribute are now declared partial, as its source
  generator requires.

### Changed

- Documented Unity 6.5/6.6/6.7 forward compatibility in
  [Docs/PackageCompatibility.md](Docs/PackageCompatibility.md): the package carries no legacy
  `UxmlTraits`/`UxmlFactory` UI Toolkit elements, no `com.unity.inputsystem` `versionDefines`, no
  serializable reference cycles and no APIs from the Unity 6.7 obsolete-to-error sets.
- Version parity restored across every file `PackageHealthCheck` guards: the repo-root and package README
  badges, `PROJECT_SUMMARY.md`, `Docs/README.md`, `Docs/PackageCompatibility.md`, `Docs/Samples.md`,
  `AGENTS.md` and the skill metadata, which had been left at `10.6.0`/`10.6.2` while `10.7.0`–`10.8.2` shipped.

## [10.8.2] - 2026-08-12

### Changed

- UI Mesh Rig Graphic, Point and Point Motion inspectors now inherit the shared `CustomEditorBase`, so they
  use the complete Neoxider Tools chrome, mascot/version/update status, documentation and module styling.

## [10.8.1] - 2026-08-12

### Fixed

- UI Mesh Rig editor motion preview now switches the owner into Pose mode, so Start and Restart always
  produce visible deformation.
- Removing a rig Sprite clears its cached hit mesh and restores authored raycast padding; an invisible rig
  no longer intercepts pointer input in any hit-test mode.
- Image conversion now preserves disabled rendering and retargets same-object Selectables in both in-place
  and non-destructive workflows, including a single-step Undo.
- Scene handles clamp the point Transform to the same normalized center shown by the dedicated center gizmo.
- The Animator workflow in the UI Mesh Rig demo now loops as documented, and click feedback restores the
  authored tint when interrupted.

## [10.8.0] - 2026-08-12

### Added

- **Professional UI Mesh Rig point authoring.** Every point has independent inner FULL and outer ZERO
  ellipse gizmos, a dedicated center handle, preset/custom falloff curves and a full-smooth mode.
- **Interactive deformed graphics.** Rectangular, deformed-mesh and Sprite-alpha hit tests integrate with
  standard uGUI Buttons, masks and raycast padding.
- **Purpose-built inspectors.** Rendering, interaction, mesh, influence, deformation and animation controls
  are grouped into clear sections with contextual guidance and quick actions.

## [10.7.0] - 2026-08-11

### Added

- **UI Mesh Rig.** `UIMeshRigGraphic` renders a subdivided Sprite inside uGUI and blends any number of
  child `UIMeshRigPoint` transforms. The points use native RectTransform position/rotation/scale, so Unity
  Animator can record them without a proprietary clip format.
- **Two-mode Scene authoring.** Setup mode edits bind positions, elliptical influence radii and falloff;
  Pose / Animate mode previews move, rotate and scale deformation directly in Scene View. Reset and Capture
  Rest Pose commands support iteration, while a saved Pose can be used as a permanent static deformation.
- **Image conversion and creation menus.** Create a rig from `GameObject/UI`, or convert an existing Image
  to a stretch-aligned rig child while preserving sprite, color, material and raycast behavior.
- **Non-destructive point motion.** `UIMeshRigPointMotion` adds editable Position X/Y, Rotation and Scale X/Y
  curves on top of a point's Transform pose, so procedural motion and Unity Animator can run together without
  writing the same Transform. Built-in editable presets cover Float, Breathe, Body/Head Sway, Soft Jiggle,
  Pulse and Squash/Stretch, with scaled/unscaled time and safe Edit Mode preview.
- **Production authoring contracts.** Bind points use responsive normalized anchors, disabled points stop
  influencing the mesh, nested rigs are isolated, nested point reset is deterministic, and bind weights are
  cached separately from runtime pose updates.

## [10.6.2] - 2026-08-10

### Fixed

- **Disabling a slot no longer dispatches result callbacks into an inactive object graph.** An accepted spin
  keeps its planned result while disabled and completes exactly once after reactivation. Durable owners can
  explicitly call `CancelActiveSpin()` when they will void/refund the transaction themselves.

## [10.6.1] - 2026-08-10

### Fixed

- **Slot spins can no longer strand the lifecycle while gameplay time is paused.** `Row` movement and
  `SpinController` column staggering now use unscaled time. A configurable unscaled safety deadline settles
  non-updating reels to the already planned outcome, and disabling a row/controller completes an accepted spin
  through the normal result callbacks exactly once. The controller becomes idle before callbacks, so the next
  spin can be accepted immediately without application-side state repair.

## [10.6.0] - 2026-08-09

- **Module namespace cleanup.** Flattened legacy nested namespace declarations without changing their CLR
  type names. `AnimationFly` / `FakeLoad` now live in `Neo.UI`, and `ParallaxLayer` in `Neo.Parallax`, with
  Unity `MovedFrom` metadata preserving serialized component references. Root `Neo` remains intentionally
  limited to shared Inspector/authoring attributes for source compatibility.
- **RPG networking is now optional.** `Neo.Rpg` no longer references the `Neo.Network` runtime
  implementation or Mirror; it depends only on the transport-neutral `Neo.Network.Contracts` leaf
  assembly, and
  `RpgCharacter` is a local `MonoBehaviour` instead of a `NeoNetworkComponent`. Mirror command routing,
  authority checks, rate limiting, late-join snapshots, and network projectile policy now live in the
  optional `Neo.Rpg.Network` adapter assembly. Existing networked prefabs must add
  `RpgCharacterNetworkAdapter`; local/offline prefabs keep their API and serialized data.
- **RPG dependency closure is now network-free.** Removed unused runtime references from `Neo.Rpg` and
  removed the unused `Neo.Network`/Mirror define from `Neo.Save`. The condition-dependent
  `RpgConditionAdapter` now compiles in `Neo.Rpg.ConditionBridge`, preserving its script GUID, namespace,
  and Unity assembly-migration metadata. `RpgNoCodeAction` remains in `Neo.Rpg` to preserve external
  UnityEvent type names. An EditMode architecture test walks the complete named-asmdef closure and prevents
  the base RPG assembly from reaching Mirror or a `Neo.Network` implementation again.
- **`RpgCharacterProfileService` plain-C# core.** Profile validation/serialization is reusable by save
  persistence and network adapters; `RpgCharacter` now exposes `CaptureProfile()` / `ApplyProfile(...)`.
- **`RpgCharacterResourceService` plain-C# core.** Resource dictionaries, clamped mutations, reactive
  queries, derived max/regen updates, spend/damage pause windows, and regen tick timing moved out of the
  scene component. Existing `RpgCharacter` serialized fields, public APIs, UnityEvents, death handling,
  and network snapshot routing remain compatible delegates.
- **Assembly boundaries hardened.** All package asmdef references now use assembly names instead of GUIDs;
  the broad `Neo` assembly was removed; `Neo.Core` is a leaf with separate Level data/components/bridge and
  Resources component assemblies. Legacy AttackSystem sources moved to `Rpg/Combat` in the optional
  `Neo.Rpg.Combat` compatibility assembly, and optional
  Shop/Inventory integration moved to `Neo.Shop.Bridges`, preserving script GUIDs and public type names.
- **CI and behavioral coverage expanded.** CI now runs PlayMode tests as a separate job. EditMode tests are
  organized by module, Cards deck/hand edge cases are stronger, and behavioral smoke coverage now includes
  FakeLeaderboard, Tools.Other, and Tools.Debug.

- **`InteractiveObject` Inspector testing API.** Added Play Mode-only `Test Interact Down`,
  `Test Interact Up`, `Test Click` and `Invalidate Colliders` buttons. The corresponding public
  `InteractDown()`, `InteractUp()` and `Click(...)` methods use the same local/Mirror dispatch path as
  real input and respect `interactable`.
- **Reusable `InteractiveObject` runtime core.** `IInteractiveTarget` lets custom input, AI, XR, and
  proximity controllers drive interaction without depending on the scene component. Pure, allocation-free
  `InteractionQueryMath` / `InteractionRayHit` APIs now own range and ray-hit ordering rules, while
  `InteractionCameraResolver` centralizes the existing main-camera/fallback lookup. These APIs now live in
  the leaf `Neo.Tools.InteractableObject.Core` assembly, which has no Mirror or `Neo.Network` dependency;
  their `Neo.Tools` namespace and asset GUIDs remain compatible. Serialized fields, public methods,
  Inspector behavior, and Mirror dispatch remain compatible.
- **More complete runtime test controls.** Added missing Play Mode-only Pause/Resume actions to the
  animator and random-music components, plus practical Jump, AI Resume, CameraShake Stop/Reset, Typewriter
  Clear, NPC navigation and toggle controls. Existing runtime buttons in these components are now
  disabled outside Play Mode.

## [10.5.0] - 2026-08-09

Inspector test buttons for the components you reach for when tuning a scene by hand, plus a new
`TransformAnimator` runtime component. The runtime buttons are Play-Mode-only: starting coroutines,
spawning objects or shaking a transform takes effect in Play Mode.

### Added — authoring

- **`[Button]` inspector buttons on trigger-style methods across the package.** Spin a roulette, fire a fly
  animation, shake a button or start a patrol straight from the Inspector, without wiring a temporary UI button:
  - `LineRoulett` — `StartRolling`, plus `Update Visual` (rebuilds the reel layout; the `updateSetting`
    checkbox toggle still works);
  - `SpinController` — `StartSpin`, `AddLine` / `RemoveLine`, `AddBet` / `RemoveBet`, `SetMaxBet`;
  - `AnimationFly` — `RefreshPrefabCache`, and the new `TestFlyByType` below;
  - `SimpleSpawner.Spawn`, `FieldSpawner.SpawnOnAllWalkable`, `ButtonShake.Shake` / `StopShake`,
    `RandomMusicController.Start` (labelled "Play Random Music") / `Stop`, `Play` / `Stop` on `ColorAnimator`,
    `FloatAnimator`, `Vector3Animator` and `LightAnimator`, `TypewriterEffectComponent.PlayAutoText` / `Stop`,
    `AiNavigation.StartPatrol` / `StopPatrol` / `Stop`, `SpineController.PlayDefault` / `PlayDefaultForced` /
    `NextSkin` / `Stop`.
- **`AnimationFly.TestFlyByType(int type, int bonusCount = 5)`** — one-click smoke test for a fly setup:
  spawns items of the given type from the spawn parent (or the component's own transform) towards the end
  point configured for that type in `Bonus Prefab List`.

### Added — Animations

- **`TransformAnimator` — universal transform animator** (`Neo.Animations`). Generalizes ad-hoc
  "rotating pickup" scripts (asset-store gem animators and the like) into one component with combinable
  channels on a shared clock: constant **rotation** (deg/s per axis), curve-eased **float/bob** along any
  direction, **scale pulse**, continuous **Perlin shake**, and one-shot **impulse shake** via
  `Shake(strength)`. Every eased channel takes an `AnimationCurve`; `RandomizeStartTime` desyncs rows of
  items. The math lives in the pure, scene-free `TransformAnimationEvaluator` with edit-mode tests;
  the MonoBehaviour only captures the base pose and applies the evaluated state. Trigger methods carry
  `[Button]`. Docs: `Docs/Animations/TransformAnimator.md`.

## [10.4.1] - 2026-07-31

The character controller's collider is a generated value, and nothing said so. Fixed at the source.

### Fixed

- **`Character First Person` and `Character Third Person` presets stood 1 m above the floor.** Both shipped with
  `Mover.colliderOffset = (0, 0, 0)` while their children are authored feet-at-origin — `Model` at `y = 1` (the
  built-in capsule mesh is 2 m tall around its own centre, so its feet land on `y = 0`) and `CameraPivot` at
  `y = 1.6` first person / `y = 1.5` third person. `colliderOffset` is normalised — it is multiplied by
  `colliderHeight` — and ground detection parks the transform origin at `colliderHeight * (0.5 - colliderOffset.y)`
  above the floor, so with `0` the whole rig, camera included, hung half a body height in the air. Both presets now
  use `(0, 0.5, 0)` like every CMF prefab and every animated preset in this package already did. Standing on flat
  ground the collider still occupies `stepHeightRatio * colliderHeight … colliderHeight` above the floor whatever
  the offset is, so walking, slopes and steps are unchanged.

  ⚠ **Breaking for existing scenes that use these two prefabs.** `colliderOffset` lives on the `Mover` of the
  prefab root and is unlikely to be overridden per instance, so the new value propagates on upgrade and every such
  character settles 1 m lower relative to the floor than before.

  **You are affected if, after upgrading, the camera sits about a metre too low or the character model is buried
  up to the waist in the floor.** That means you had compensated for the old defect somewhere in your own scene,
  and the compensation is now counted twice. Undo it — the presets are authored feet-at-origin, so the shipped
  values are the correct ones: `Model` at `y = 1` (built-in capsule mesh, pivot at its centre), `CameraPivot` at
  `y = 1.6` first person / `y = 1.5` third person, and spawn points at floor level rather than a metre above it.
  If instead your character simply dropped a metre and now stands correctly, you had not compensated and there is
  nothing to do.

  One more consequence, easy to miss because it only shows at spawn time: a character created in mid-air now
  starts 1 m higher relative to its own origin, so it falls that extra metre before ground detection catches it.

### Added — authoring

- **The Mover inspector now shows the collider it generates, read-only.** `Mover.RecalculateColliderDimensions()`
  overwrites the attached collider's height, radius and centre from `Collider Height` / `Collider Thickness` /
  `Collider Offset` / `Step Height Ratio` in `Awake` *and* in `OnValidate` — so any value typed into the
  `CapsuleCollider` component is silently discarded on the next inspector change. `MoverInspector` now prints the
  resulting dimensions in a disabled block, states where the numbers come from, reports where the transform origin
  rests on flat ground, and warns with a one-click fix when `Collider Offset` Y is not `0.5` while children are
  authored feet-at-origin.
- **Tooltips on the four collider fields of `Mover`**, including the fact that `Collider Offset` is normalised.
  Attributes only — no behaviour or API change.
- **`Docs/Tools/Move/CharacterController/README.md`** gained a "Character size" section with the formulas.

## [10.4.0] - 2026-07-31

A full audit pass over the package: runtime defects found by reading every module, plus asset, prefab and
test hygiene. Nothing was removed and no public API changed shape.

### Fixed — runtime defects

- **`SingletonRuntimeReset` threw on any two-level singleton subclass.** `class MyGm : Gm` (where `Gm : Singleton<Gm>`)
  made the reset sweep call `MakeGenericType` on an already-closed type; the `ArgumentException` aborted the whole
  sweep, so *every* singleton kept its statics across play sessions with domain reload disabled. The sweep now walks
  the inheritance chain to the closed base instead of reconstructing it.
- **Statics surviving between play sessions.** Added the package's `ResetStaticState` hook to `FieldGenerator.I`,
  `InventorySlotGridView`'s pending selection, `Money.Registry`, `GlobalSave` (`_data`/`IsReady`) and `UI.I`.
  `FieldGenerator` also clears `I` in `OnDestroy` when it owns the reference.
- **`Money.AddOverflow` bypassed Mirror authority** — it wrote the balance locally on clients instead of going through
  the command/RPC path `Add` already used. Added `MoneyOp.AddOverflow` and wired it into the dispatch.
- **`Money.SetMoneyForLevel` skipped the shared deposit path**, so the level payout ignored `Max Money`, never grew
  `AllMoney` and never updated `LastChangeMoney`. It now goes through the same local add as everything else.
- **`Money.ReloadBalanceFromSave` did not notify.** The public reload wrote with `SetValueWithoutNotify`, so UI bound
  to `CurrentMoney` silently kept the stale balance.
- **`AbilitySystem.Revive()` published no event.** Death publishes a receipt, revive did not, so nothing downstream
  could react. Added `AbilityEvents.Revive`, published with the restored health as `Amount`.
- **`TypewriterEffect` restarted mid-run killed its own successor.** The finishing run disposed the shared
  `CancellationTokenSource` in its `finally`, cancelling the run that had just replaced it. Guarded with a run
  generation counter, the same pattern `Timer` uses.
- **`Evade` left `IsEvading` stuck true when disabled mid-evade**, permanently blocking further evades.
  `OnDisable` now clears the state; `OnEvadeCompleted` is deliberately *not* raised — the evade was interrupted,
  not completed.
- **`AdvancedAttackCollider` silently dropped knockback.** With `Use Advanced Force Applier` on, a target without an
  `AdvancedForceApplier` returned early instead of falling back to the Rigidbody path.
- **`TimerObject.Reset()` sent countdown timers to 0** instead of back to full duration, so a reset countdown
  reported 100% progress and could fire immediately.
- **`InteractiveObject` clicks were dead when `Use Hover Detection` was off** — the target raycast only ran for hover,
  and the mouse path then had no target to act on.
- **`Spawner` never recovered from being disabled.** The spawn coroutine died with the component but `isSpawning`
  stayed true, so re-enabling produced nothing forever. Added `OnDisable`; pending destroy coroutines are left
  alone on purpose so disabling the component does not leak spawned objects.
- **`Drawer` destroyed pooled `LineRenderer`s** on discard and on `DeleteAll`, draining the pool it was supposed to
  reuse.
- **`Match3BoardService` stayed locked** if it was disabled mid-cascade — the resolve routine handle was never cleared.
- **`Selector` ignored `result.UniqueReset`**, so `OnUniqueReset` never fired on an automatic cycle reset.
- **`AnimationFly.WorldToCanvasPosition`/`CanvasToWorldPosition` threw** when called with no canvas and no instance
  in the scene; they now log and return zero like the sibling helpers.
- **`DrunkardGame` destroyed captured cards** when only one side used a hand — the war-pile branch tested both hand
  flags together instead of the winner's.
- **`Bonus/Slot/Row` returned two strays swapped** in the top-down fallback ordering.

### Fixed — packaging and assets

- **12 dead asset references across 7 shipped prefabs.** `UI/ButtonPageSwitch` and `Shop/ButtonPrice` pointed
  at TMP font assets and materials that exist nowhere in the project (those labels rendered no text at all);
  `Bonus/LineRoulett`, `Bonus/Slot/SlotElement`, `FakeLeaderboard/Page LeaderBoard`, `UI/Page/Fake Load` and
  `Tools/First Person Controller` had missing sprites and an audio clip. Fonts now use the TMP default the
  rest of the package already uses, sprites fall back to Unity's built-in `UISprite`, and the dead clip
  reference is cleared.
- **The package no longer needs Mirror to be installed.** `-System--`, `First Person Controller`,
  `Interactive Sphere` and `Toggle Interactive` shipped with a `Mirror.NetworkIdentity` component, which is a
  missing script for everyone who does not use Mirror — `-System--` being the system root made it the widest
  case. Multiplayer stays an opt-in layer: add `NetworkIdentity` yourself when you go online.
- Sample sprites with Cyrillic and Chinese filenames were renamed to ASCII — non-ASCII asset names break CI
  runners and cross-OS checkouts.

### Fixed — tests and infrastructure

- The two play-mode test assemblies were merged: `Neo.Tests.PlayMode` (a single file on the deprecated
  `optionalUnityReferences` config) folded into `Neo.Tests.Play`.
- `DialoguePlayModeTests` contained one placeholder asserting nothing; replaced with real coverage of
  `DialogueController` start/advance/end-event behaviour.
- Added a GitHub Actions workflow that runs the EditMode suite on push and pull request — the repository had
  no CI at all.

## [10.3.0] - 2026-07-31

### Added
- **New 3D character controller** (`Tools/Move/CharacterController`). [Character Movement Fundamentals](https://github.com/Jan-Ott/CharacterMovementFundamentals) (MIT, ex-commercial asset) is bundled in `ThirdParty/CharacterMovementFundamentals/` as the motor, camera and animation layer, wrapped by Neoxider components. What it brings over the legacy controller: `Slope Limit` with slide-off (no more climbing walls), stair and ledge traversal without losing ground contact, moving platforms, momentum-based movement so external forces (`AddMomentum`) actually push the character, arbitrary gravity direction (wall/ceiling/planet walking), first- **and** third-person cameras with camera collision, and `AnimationControl` for animator-driven characters.
- `NeoCharacterInput` — movement and jump input for CMF controllers through the Neoxider input stack: New Input System or legacy Input Manager (auto-detected, per-read fallback), plus `SetMoveInput`/`SetJumpInput`/`SetRunInput` injection for on-screen joysticks, AI and network drivers. Jump is exposed as a *held* state, which is what CMF needs for variable jump height.
- `NeoCameraInput` — look input for CMF cameras with `GameSettings.MouseSensitivity`, per-axis inversion, `EM.OnPause`/`OnResume` handling and a cursor gate. It never writes `Cursor` state: `CursorLockController` stays the single cursor owner and this component only reads the result. Pointer delta and gamepad stick are converted separately (`NeoLookRate`) so sensitivity does not drift with frame rate.
- `NeoCharacterSprint` — sprint on top of CMF's single-speed walker, scaling from the controller's authored `movementSpeed`, with optional ramp and `On Sprint Start`/`On Sprint Stop` events.
- `NeoCharacterCameraBridge` — keeps movement camera-relative when Cinemachine (or any external rig) drives the camera, by pointing `AdvancedWalkerController.cameraTransform` at the camera actually being rendered through. References no Cinemachine API, so it compiles with or without the package and works with Cinemachine 2 and 3.
- `NeoCharacterNetworkBinding` — Mirror support. CMF simulates on every instance, so remote copies are reduced to `NetworkTransform`-driven proxies: controller, mover, input and camera rig disabled, Rigidbody kinematic. `NetworkTransform.target` and `syncDirection = ClientToServer` are wired automatically. Compiles to a no-op without Mirror. Known gap, documented on the component page: remote proxies do not animate, because CMF's `AnimationControl` reads velocity from the disabled controller.
- `OptionalInputSystemAdapter`/`Bridge`: `ReadJumpHeld()`, `ReadPointerDelta()` and `ReadLookStick()` — the last two let callers treat a frame-accumulated pointer delta and a continuous stick rate differently instead of summing them.
- **Ready-made character prefabs**: `Prefabs/Tools/Character Controller/Character First Person.prefab` and `Character Third Person.prefab`, wired to the Neoxider input layer (Mover + AdvancedWalkerController + NeoCharacterInput + NeoCharacterSprint, camera pivot with NeoCameraInput; third person adds ThirdPersonCameraController, CameraDistanceRaycaster and TurnTowardControllerVelocity). Listed in the **Create Neoxider Object** window under *Presets → Player*, in **GameObject → Neoxider → Presets**, and as a component entry via `NeoCharacterInput`. The superseded `First Person Controller` prefab moved to a new **Legacy** preset category, collapsed by default.
- Sample: **Character Movement Fundamentals** (`Samples~/CharacterMovementFundamentals/`, importable from Package Manager) — **demo scenes only**: the upstream showcase, top-down, gravity-tunnel, planet-walker and click-to-move levels with their environment art, CMF's own controller prefabs and showcase scripts. Everything the shipped presets need (Capguy model + animator + materials, the platform mesh, the physic material, the footstep sounds) lives in the package itself under `ThirdParty/CharacterMovementFundamentals/Art/` and `Audio/Character Controller/`, so no preset depends on the sample being imported.
- **CMF showcase controllers as presets**, rewired from CMF's legacy-Input scripts to `NeoCharacterInput`/`NeoCameraInput`/`NeoCharacterSprint`: `Character Third Person (Animated)`, `Character Top Down (Animated)`, `Character Side Scroller (Animated)`, `Character Click To Move (Animated)` (keeps its mouse-raycast input) in *Presets → Player*, and `Moving Platform` in a new *Presets → Environment* category. The animated ones reference Capguy/audio assets from the imported CMF sample — see the CharacterController README.
- **Character prefabs are drop-in now**: the two capsule prefabs got footstep/jump/land audio (`AudioControl`, CMF clips vendored into `Audio/Character Controller/` — no sample dependency), every character prefab carries exactly one `AudioListener` on its camera (footsteps were silent without a listener in the scene), and mouse-look prefabs (First/Third Person, Third Person/Top Down Animated) carry a `CursorLockController` (lock on start, Escape toggles), so look works in an empty scene without extra wiring.
- 20 new EditMode tests (`CharacterControllerInputTests`) covering the backend decision rule, the look-rate conversion and the gating/injection contracts.

### Changed
- `PlayerController3DPhysics` and `PlayerController3DAnimatorDriver` are now **legacy**, tagged `[LegacyComponent]` with the replacement recorded. They stay listed in the Create window (`hideFromCreateMenu: false`) and the old prefab moved to **Presets → Legacy → First Person Controller**. Nothing is removed: serialized fields, public API and behavior are unchanged, and existing scenes keep working. Their menu entries moved to `Neoxider/Tools/Legacy/*` and their XML docs point at the new module. The 2D controllers are unaffected.
- Docs: new `Docs/Tools/Move/CharacterController/` section with a migration table from `PlayerController3DPhysics`; `Docs/Tools/Move/README.md` updated.
- `THIRD-PARTY-NOTICES.md` now distinguishes bundled code from referenced dependencies, and records the CMF MIT notice plus the three deviations from upstream.

### Fixed
- **Bundled CMF did not compile on Unity 6.** `Sensor.cs` carried a stray `[SerializeField]` on an *enum declaration*. `SerializeField` is declared as `AttributeTargets.Field`, and since Unity 6000.0.3 applying it to anything else is a hard compiler error (CS0592) rather than being ignored — so the whole assembly would have failed to build. The attribute never did anything there and was removed.
- Bundled CMF `Mover.SetVelocity` writes `Rigidbody.linearVelocity` (the Unity 6 name for `velocity`).
- **Look sensitivity was ~10x too fast on the New Input System backend.** `Mouse.delta` is raw pixels, while the legacy `"Mouse X"`/`"Mouse Y"` axes are pixels scaled by the Input Manager's default 0.1 sensitivity. `NeoCameraInput` now normalizes the pointer delta by that factor, so both backends produce the same look speed for the same physical mouse move. The default `Mouse Input Multiplier` is 0.0025 (a quarter of CMF's 0.01), tuned by feel at the default `GameSettings.MouseSensitivity` of 2.
- **Third-person camera collapsed onto the character.** In `Character Third Person.prefab`, `CameraDistanceRaycaster.cameraTargetTransform` pointed at the camera pivot itself, so the obstruction cast had zero direction: the camera distance lerped to 0 on spawn and never recovered. The raycaster now targets a dedicated `CameraPivot/CameraTarget` marker at the design distance (0, 0, -5), and the character's own capsule is in its `ignoreList`. Caught during live Play Mode verification.

- `MultSceneNetworkTests` hard-failed for anyone without the development project's `Assets/Scenes/Mult.unity`; it now skips with a clear message, like the sample smoke tests already did.
- Build settings listed a single disabled scene under the tilde-hidden `Samples~/` path, which Unity cannot import — replaced with the two real project scenes.

The two CMF source patches above are marked with a `NEOXIDER PATCH` comment in place.

## [10.2.0] - 2026-07-22

### Added
- **`CursorLockController` now drives player controllers** (new *Player Control* section): reference `PlayerController3DPhysics` instances (plus an optional off-by-default auto-find fallback) and the controller suspends their look — and optionally movement — while the cursor is visible, restoring them when it locks again. Only what the controller itself suspended is restored, so a player disabled externally (pause, cutscene) is never force-enabled. Runtime API: `RegisterPlayer`/`UnregisterPlayer` (network-spawned players), `DisableLookWhileCursorVisible`/`DisableMovementWhileCursorVisible`.
- **Full cursor opt-out for games with their own cursor system**: per-instance `Manage Cursor` master switch on `CursorLockController` (automatic key/lifecycle/start behavior off; explicit method calls still work) and a static `CursorLockController.GlobalCursorManagement` kill-switch that silently stops every instance from writing cursor state or driving players. `PlayerController3DPhysics.CursorControlEnabled = false` remains the player-side opt-out and covers every cursor path.
- `PlayerController3DPhysics`: public `HasExternalCursorControl()`, `ExternalCursorLockController` and `SetExternalCursorLockController()` for explicit ownership wiring; internal `ShouldHandleEscape()`/`ShouldLockCursorOnStart()` decision seams covered by tests.

### Changed
- **Single Escape owner.** With both components present, only `CursorLockController` handles Escape; `PlayerController3DPhysics` defers automatically (referenced players are auto-bound to the controller in `Awake`, so no manual wiring is needed). The player's `SetCursorLocked` forwards to the owning controller instead of fighting it with direct `Cursor` writes. Scenes without a `CursorLockController` keep the standalone player behavior unchanged.
- `CursorLockController` inspector regrouped (*Cursor Ownership* / *Keys* / *Player Control*); presets set sensible values for the new toggles. Docs for both components describe the ownership rule and a "Choosing a cursor setup" matrix.

### Fixed
- Escape-handling collision between `PlayerController3DPhysics` and `CursorLockController`: both reacted to Esc in the same frame (double toggle — cursor unlocked while look re-enabled, or vice versa). All cursor paths now flow through the single owner, and player look/movement follow the cursor state via the controller's one apply choke point (toggle key, access key, `ShowCursor`, stack release re-apply, lifecycle and scene-load paths included).
- 16 new EditMode regression tests (`CursorLockPlayerControlTests`).

## [10.1.0] - 2026-07-18

### Added
- **Inspector mascot as a live "slime linter".** The banner slime now reflects the inspected component's health: a remembered console-error count (attributed to the component type by parsing stack traces, kept for the session) plus a cheap cached scan for missing object references and NaN/Infinity float fields. Faces: neutral/blink when healthy, worried on missing references, angry on errors/invalid numbers, a brief "surprised" reaction the moment a new error appears (auto-opening a compact issue list with a **Clear** action), and a "watching" face in Play Mode. The spectrum half-frame matches the mood (amber shimmer / red pulse) and flows faster in a healthy Play Mode.
- **Poke the slime.** Clicking the mascot plays a springy bounce and a startled face — a bit of life in the inspector. The issue-count badge on the chip still opens/closes the problem list.
- Performance: the console hook is O(1) per error (dedup + type cap), the validation scan runs only for inspected objects, is throttled (~2 s) and property-capped, and remembered errors persist via `SessionState` (survive domain reloads, reset with the editor).
- **Abilities go NoCode.** `AbilityNoCodeAction` bridge (cast/grant/revoke/level/modifier/damage/heal from UnityEvents, uniform with the Level/Rpg/Progression bridges), `AbilityAutoCaster` (Survivor-style auto-cast with nearest-target lock-on, interval mode and failure backoff) and `AbilityCooldownSource` (poll-friendly `CooldownNormalized`/`SecondsRemaining` for `SetProgress`/`NoCodeBindText` bindings). `AbilityUnitBehaviour.ApplyHeal(float)` added, mirroring the heal effect op. The Abilities quick start is now genuinely no-code.
- Inspector mascot Play Mode gaze: the "watching" face turns toward the Game view (mirrors horizontally when the Game view sits left of the inspector).
- New **Trigger Cube** preset in the GameObject creation menu; UI presets created without a Canvas now auto-create Canvas + EventSystem; 3D presets spawn at the Scene view pivot (honoring "Create Objects at Origin").
- **Abilities NoCode demo** (`Samples~/Demo/Scenes/Abilities/AbilitiesNoCodeDemo.unity`) — live-verified showcase of the NoCode trio: auto-cast at the training dummy, bridge-driven grant/cast/heal buttons, cooldown bar via `AbilityCooldownSource` + `SetProgress`, health bindings, and +XP → level → bigger zap damage through `LevelNoCodeAction` + `SetAbilityLevel`.
- **Scene-authored NoCode demos** — `NoCodeBindingDemo` (one reactive `Counter.Value` drives three inspector-wired bindings: raw text, formatted text and a progress fill) and `NoCodeActionsDemo` (Level/Progression/Quest/Rpg wired in the inspector — buttons trigger the module action, module events drive the readout). No runtime construction: everything is authored in the scene, matching the reference example scenes.

### Changed
- **All editor menus consolidated under a single top-level `Neoxider` menu** — Windows (Ability Designer, Dialogue Editor, Prefab To Sprite, Create Neoxider Object), Tools (Scene Saver, Texture Max Size, Save Project Zip, missing-script utilities, Fix Editor Assembly References), Network, Samples, Settings, Visual Settings and Health Check; old `Tools/Neoxider`, `Tools/UIKit` and `Window/Neoxider` paths removed. Docs updated to the new paths.
- GameObject/Neoxider creation menu tidied: flat Presets group with separators, validated Create/Sort Scene Hierarchy entries (disabled in prefab mode / with nothing to sort), consistent naming, no creation log spam.
- Clicking the mascot no longer collapses the other components (the relayout made the inspector jump); it is now a pure animation. A future click action is left as a TODO.
- **"Create Neoxider Object" window** redesigned — gradient banner, category pills, folder icons and count badges, and distinct raised component rows (fixed the low-contrast rows that blended into the dark window background). The catalogue now includes the previously-absent Abilities module and every `[CreateFromMenu("Neoxider/…")]` component.

### Fixed
- Mascot error memory now mirrors the Unity Console: clearing it (Clear button, Clear on Play, or any console wipe) calms the mascot within half a second — event-driven when the Console window is visible, throttled O(1) count-poll backstop when it is hidden. NUnit frames are excluded from error attribution.
- Spectrum half-frame corners no longer look crooked: the corner arcs swept the wrong quadrants; they now follow the card radius exactly and are drawn as mitred anti-aliased arcs.
- GameObject creation entries now register undo, align under the right-click context target, keep prefab links (`PrefabUtility.InstantiatePrefab`), give unique sibling names, work inside prefab stages, and respect the container list configured in Neoxider settings without duplicating inactive containers.
- `AutoBuildName` renames only Android `.apk`/`.aab` outputs — Windows/WebGL and other build targets are untouched (renaming a Windows `.exe` broke its `_Data` folder link); colliding artifacts get a unique suffix instead of being deleted, and mixed path separators no longer confuse the same-path guard.
- Review pass over the new code (8 finder angles, 21 confirmed findings, all fixed): `AbilityAutoCaster` searched targets around its own transform instead of the caster unit (silent mis-targeting on manager objects) and re-sorted/re-queried every frame with allocations; `AbilityNoCodeAction` could leak its cast-failure capture listener when a user handler threw; GameObject creation multi-selection created one instance per selected object; every created object got a spurious " (1)" suffix; the "Create Objects at Origin" preference read a dead Unity-5-era EditorPrefs key; `Sort Scene Hierarchy` reindexed nested/additive-scene containers and its menu validator deep-scanned scenes on every context-menu open; Canvas-rooted presets no longer nest under another Canvas and UI reroute prefers screen-space canvases; EditMode ability tests pin the hub singleton so an open user scene cannot hijack registration; inspector frame drawing and the anchor-restore loop no longer allocate or force-repaint per tick.
- Package-wide module audit (every module reviewed): `ResourcePoolModel.Increase` now returns the delta actually applied (headroom-aware), so `HealthComponent.OnHeal` reports the real heal instead of an overheal; sample `FakeLoad` initializes its UnityEvents inline so a runtime-spawned instance is subscribable; the Progression demo no longer self-destructs in Play Mode (its NoCode bridge resolved the `ProgressionManager` singleton via `AddComponent`, whose duplicate guard killed the demo object); plus assorted per-module correctness fixes, ~48 documentation pages corrected against code, and NoCode-bridge coverage added to the Level, Progression, Quest and StateMachine demo controllers. Regression tests added across Core, Audio, Grid, Bonus, UI and NoCode (EditMode total 978, PlayMode 109).
- **Unity 6.5 compatibility**: the inspector health backend no longer casts `EntityId` to `int` (obsolete-as-error / CS0619 on 6.5) — it keys its validation cache by `EntityId` on 6.5 (`int` on 6.0–6.4 via a conditional alias) and detects dangling references with `EntityId.IsValid()`. Fixes the package failing to compile in Unity 6.5 projects.
- Card views keep their artwork aspect ratio in adaptive UI (`Image.preserveAspect`) instead of stretching, and UI cards hover/return by `anchoredPosition` (RectTransform-aware) so a scaled or camera-space canvas no longer warps their size or position.
- Under `MIRROR`, exact-type Neo inspectors for `NetworkBehaviour`-conditional components (physics player controllers, `NeoNetworkComponent` subclasses) now win over Mirror's `[CustomEditor(typeof(NetworkBehaviour), true)]`, restoring the Neo styling on those components.
- `RpgNoCodeAction` exposes public `OnSuccess` / `OnFailed` / `OnResultMessage` accessors, matching the other NoCode bridges so code can subscribe, not only inspector wiring.
- `RpgCharacter` inspector no longer shows doubled section titles: its editor drew its own section foldouts (Template, Resources, Progression…) and then re-drew each field's built-in `[Header]`. Fields are now drawn through a `DrawPropertyFieldNoHeader` helper that strips the `[Header]` decorator, so each title appears once. The header-suppression in the shared `CustomEditorBase` was also extended to cover array/generic fields (previously they returned early and doubled their header).

## [10.0.1] - 2026-07-18

Patch release: three audit-fix cycles over the whole package — 52 independently verified correctness bugs fixed, plus a consistency pass over asmdefs, docs and package metadata.

### Fixed
- **Abilities**: nested event-driven effects no longer corrupt shared scratch lists (area damage + thorns-style reactions now hit every target; stacked reactive modifiers all fire; shields survive re-entrant damage). Team override survives `UnitTemplate.ApplyTo`; piercing projectiles hit distinct units instead of re-hitting the first; per-hit RNG is decorrelated; spawn ops fall back to the cast point instead of world origin; play-mode teardown no longer resurrects the system hub; unrealized projectile casts expire instead of leaking; caster grants are order-independent; `max_health_bonus` / `max_mana_bonus` now actually resize resource pools.
- **Core/Level**: `GetXpToNextLevel` off-by-one on all curve types; `HealthComponent.Load` no longer clamped by `MaxDecreaseAmount` (and no longer fires death events while loading); `Increase()` on unlimited pools no longer computes negative headroom; `TextLevel` resubscribes after disable/enable; removed per-frame forced reactive sync; `SetLevel` overflow guard; weighted-random zero-weight fallback; `StringExtension.Truncate` small-length guard.
- **Tools**: `Timer` stop/start async race; `Spawner.Clear` double-release and stale delayed-destroy handles; `KeyboardMover` fixed-update compensation; `SwipeController` swapped start/end positions; `TimerObject` RealTime mode persists `isActive`; `MouseInputManager` picks the nearest hit; mouse movers guard `deltaTime == 0`; pooled-component cache and additive-scene pool lifetime leaks.
- **Save/Quest/Audio/Shop/Network**: global `Save()` no longer deletes data of fields with `autoSaveOnQuit` off; file saves flush on pause/focus loss (mobile); `QuestManager` tolerates registry mutation from completion handlers and migrates saved states when objectives are added; music track timer is pause-aware and unscaled; networked wallet rate limit is per-connection; `GlobalSave` initializes on first launch; `NetworkReactiveSync` validates the reactive property type; saveable behaviours persist on disable.
- **Bonus/Cards/NoCode/UI**: slot line evaluation only pays contiguous runs; `SetRewardAvailableNow` makes rewards claimable immediately; `SpinController` window-size bounds; `DeckModel.Draw` preserves duplicate-card order; `NoCodeFormattedText` retries late-spawned sources; `LineRoulett` honors its serialized slow-down time; `FakeLoad` static state resets between plays.
- **Survivor demo**: enemies freeze during the level-up pause; upgrade-granted projectile abilities get archetypes; pooled templates are cleaned across reloads; Shop demo guards a missing wallet.
- TMP smeared-text fix follow-up: obsolete `enableWordWrapping` replaced in sample UI.

### Changed
- 30 new regression tests (EditMode total 889).
- Dead asmdef references removed (`Neo.Tools`, `Neo.Network.Core`, an old Odin GUID); PlayMode test assembly renamed to `Neo.Tests.PlayMode` to avoid consumer name collisions; deprecated `com.unity.textmeshpro` dependency dropped (TMP ships in `com.unity.ugui` on Unity 6).
- Docs: pages matched to real APIs (SaveProvider static facade, StatePredicate family, StateMachineEvaluationContext, QuestStatus values), hidden-sample paths corrected, ~120 generator-artifact rows purged from 61 pages, `[NeoDoc]` added to the last 5 undocumented components (+ new AuraWeapon page).
- Inspector polish: the property block is a real rounded card (accent tint + 1px edge); the segmented left rainbow line is replaced by a continuous HSV spectrum half-frame hugging the card (rounded corners, fading arms, seamless animated hue); the banner mascot is drawn as a close-up that nearly fills its chip.

## [10.0.0] - 2026-07-18

Major release. Headline: a new data-driven combat core (`Neo.Abilities`) that supersedes `Neo.Rpg`, a redesigned inspector, and a modular survivor demo built on the new system.

### Added
- **`Neo.Abilities` module — a Dota-derived, data-driven ability/modifier system.** Author new abilities entirely in data (ScriptableObjects), no code required. Pure-C# deterministic domain plus Unity wrappers:
  - Units with teams and resource pools (reuses `Neo.Core.Resources.ResourcePoolModel`), and an **open property registry** aggregated `base -> +Add -> xMul -> Max`.
  - **Modifiers** unify buffs/debuffs/DoTs/auras/shields/stuns: typed property contributions, boolean states (any-true-wins), interval ticks with guaranteed expiry, stack policies (Independent/Refresh/Stack + per-stack scaling), and declarative event reactions (e.g. absorb-on-take-damage).
  - **Cast pipeline** with cost/cooldown/charges/targeting/team-filter/range validation, instant or homing-projectile delivery, area effects, and an open effect-op registry (damage/heal/apply-modifier/remove-modifier/dispel/resource/spawn) plus motion/utility atoms: **knockback, pull, teleport** (routed through the `IAbilityWorldAdapter.TryMoveUnit` seam so navigation/physics stay pluggable), **execute** (health-fraction damage over missing/max/current HP), and **chain** (deterministic nearest-first bounces with per-hop falloff).
  - **Leveled values and specials**: every effect amount is a `LeveledValue` (per-level array plus property scaling from caster or target, driven by ability or unit level); named `Specials` on an `AbilityDefinition` are reusable Dota-style `%value%` entries referenced by key from any effect node.
  - **Live combat properties** read from the property registry at damage time by `DamageService`: `crit_chance` / `crit_multiplier`, `lifesteal_percent`, and physical-only `evasion_chance` — modifiers change them mid-fight with no extra wiring.
  - **Receipt-driven**: everything observable flows through one event bus; casts carry deterministic seeds and serializable ids (authority-ready seams for a future Mirror bridge).
  - Authoring assets: `AbilityDefinition`, `ModifierDefinition`, `UnitTemplate`, `AbilityLibrary`. Scene components: `AbilitySystemBehaviour`, `AbilityUnitBehaviour`, `AbilityCasterBehaviour`, `AbilityProjectileBehaviour`.
  - **UI Toolkit "Ability Designer"** window (`Tools -> Neoxider -> Ability Designer`) plus SO inspectors.
  - Docs under `Docs/Abilities/` and 93 EditMode tests.
  - `Neo.Rpg` is **superseded by `Neo.Abilities`** and slated for removal in a later release.
- **Modular Survivor demo** (`Samples/Demo/Scenes/SurvivorDemo.unity`) — a complete Vampire-Survivors-style game assembled entirely from a single `SurvivorConfig` data asset on top of `Neo.Abilities` + Core level/resource systems (waves, auto-cast weapons, XP, level-up upgrade cards, escalating difficulty). Swap the data to clip a different game. Bright uGUI, no IMGUI.
- **Demo-shell scenes** — eight module demos rebuilt as bright, self-explaining uGUI scenes on a shared `NeoDemoShell` frame (procedural sprites, header, content card, action log; zero imported assets): Audio, Save, Settings, LevelFlow, StateMachine, NoCode binding, Parallax, and Quest.
- `AbilitySystemBehaviour.Paused` to freeze the ability tick for menus/level-up screens.
- `AbilityUnitBehaviour.SetTemplate` / `SetTeamOverride` for runtime/pooled unit spawning.
- `ResourcePoolModel.SetCurrent(id, value)` — a direct clamped setter (loads/revives/scripted adjustments) that bypasses the heal gate.

### Changed
- **Redesigned the Neoxider custom inspector** with a modern theme (`NeoInspectorTheme`): gradient hero banner, themed rounded section cards with accent rails, gradient action buttons, a readable version pill, a themed property-panel card, and a new avatar logo with an occasional eye-blink. Dark and light editor skins supported. All functionality preserved; still IMGUI.
- **Package-wide comment cleanup**: comments reduced to XML `<summary>` docs plus `WHY:` / `TODO:` / `HACK:` markers, all in English; banner/section dividers and comments restating the code removed across runtime and editor sources.

### Fixed
- **Re-imported the TMP essential resources** shipped with the project (`Assets/TextMesh Pro`): the stale `LiberationSans SDF` font asset and materials rendered smeared/blurry SDF text in Unity 6; demo scenes now show crisp text.
- `PoolManager` threw a `NullReferenceException` when created at runtime (its preconfigured-pools list was null) — now null-guarded, so a runtime-instantiated `PoolManager` works.
- `AbilitySystem.Revive` could not restore HP after a lethal-damage death (the resource pool's heal-from-zero gate); it now sets health directly and never revives at 0 HP.
- `UnitTemplate` pool regeneration never fired (missing regen interval); templates with `RegenPerSecond > 0` now tick.

## [9.13.1] - 2026-07-17

### Fixed
- **Shop / purchase failure event never fired when out of money (solo mode):** `Shop.Buy` resolved the wallet through `IMoneySpendAuthority.CanConfirmSpendNow`, which returns false for *both* insufficient funds and pending-server-confirmation. Insufficient funds were misread as "awaiting server authority", so `Buy` returned silently and `OnPurchaseFailed` / `OnPurchaseFailedId` never fired for a non-networked wallet. Now affordability is checked first (via `IMoneyCanSpend`): a shortfall reports as a normal failed purchase (and, on a networked client, no longer sends a doomed spend command to the server), while a genuine pending-server case still short-circuits without a failure event. Found by the real Unity Test Runner (735-test suite), not the standalone compile check.
- **Shop / EquipmentManager required a visual slot to equip:** `Equip` / `Unequip` bailed out when a category had no `CategorySlot`, so state-only equipment (driven by `ShopVariantsPanel` or headless logic) silently did nothing. Equipment state is now always tracked, persisted, and broadcast; the `CategorySlot` is optional and only drives the sprite target. Persisted slotless categories are restored on load from the item catalog. `Unequip` on an already-empty slotless category is a no-op (no spurious `OnEquipChanged`).

### Tests
- `EquipmentManagerTests`: slotless equip/unequip state tracking + event, and the empty-category no-op. The existing `ShopAffordabilityTests.Buy_InsufficientFunds_FiresFailedAndDoesNotOwn` now guards the Money regression. Fixed two of the new 9.11.0 tests that only surfaced under the real runner (a grid default-content assumption and an editor-folder scan in `ModulePrinciplesTests`).

## [9.13.0] - 2026-07-17

### Added
- **Bonus / one-call economy spin:** `SpinController.StartEconomySpin()` builds the whole outcome from the assigned `SlotEconomyDefinition` (weighted pick per cell honoring the per-machine overrides, then the special/wild conversion along each active payline), queues it via `ForceNextOutcome`, and starts the spin. The building blocks are public: `BuildEconomyOutcomeMatrix()` (+ a deterministic `Func<int>`-picker overload for tests/replays/server outcomes) and `EvaluateActivePaylinesWithEconomy()` returning one `LineResult` per active payline of the settled grid. Before this the economy asset could only be wired by hand-rolling the pick → special-rule → force-outcome → evaluate chain in game code.

### Tests
- EditMode: `SpinControllerEconomyTests` — weighted fill, empty-economy guard, deterministic special-line conversion on the active payline (off-line cells untouched), and loss reporting on an unsettled grid.

## [9.12.1] - 2026-07-17

### Fixed
- **Tools / SpineController:** the `[NeoDoc]` attribute was declared twice on the class. `NeoDocAttribute` does not allow multiples, so any project with Spine installed (`SPINE_UNITY` defined) failed to compile with CS0579; without Spine the file is compiled out, which is why the error stayed invisible.

### Editor tooling
- **PackageHealthCheck** (`Tools → Neoxider → Package Health Check`) now also catches the two doc-drift classes that path checking alone could never see: (1) public, non-abstract `MonoBehaviour`/`ScriptableObject` types in `Neo.*` runtime assemblies that carry no `[NeoDoc]` attribute at all (the 9.8.2 audit found 39 such gaps by hand), and (2) dead relative `.md` links inside `Docs/` (URL-encoded paths supported; six dead links shipped in 9.8.1 alone). Obsolete types and editor/test/demo assemblies are excluded.

## [9.12.0] - 2026-07-17

### Added
- **GridSystem/Dice / plain C# `DiceBoard` core:** all dice placement/merge logic moved out of the scene component into `Neo.GridSystem.Dice.DiceBoard` — constructible over any generated `FieldGenerator` (tests, server/replay logic, custom loops), with C# events `BoardChanged`/`MergesResolved`. `DiceBoardService` keeps the identical scene API, forwards Inspector settings into the core (exposed via the new `Board` property), and re-raises the core events as its UnityEvents. Closes the remaining GridSystem TODO item.
- **Shop / `ShopListViewCategoryBar` auto categories:** `Build Categories From Shop` fills the bar from the Shop catalog on enable — one entry per distinct `ShopItemData.Category` (first-seen order) with an optional show-all entry (`Include All Entry` / `All Entry Name`); `BuildCategoriesFromShop()` re-runs it after catalog swaps.

### Tests
- EditMode: `DiceBoardCoreTests` (placement, occupied-cell rejection, merge with content cap, single-notification contract, clear, service→core settings/event forwarding), `ShopListViewCategoryBarTests` (selection drives the list view, auto-built categories with and without the All entry).

## [9.11.0] - 2026-07-17

### Added
- **Bonus / per-machine slot weights (`SlotSymbolWeightOverrides`):** `SpinController` now takes an optional `SlotEconomyDefinition` (`Economy`) plus a local weight table layered over it — enable the override to change drop weights for one machine without touching the shared asset. Entries match symbols by id (reordering/extending the definition's symbol list is safe; unmatched symbols fall back to their definition weight), weight `0` disables a symbol, negatives clamp to `0`. New `PickEconomySymbolId()` picks through the override; Inspector `⋮` menu **Normalize Weights** rescales all positive local weights to a total of `1`. `SlotEconomyDefinition.PickWeightedId` gained weight-selector and deterministic-roll overloads for tests/replays/server outcomes.
- **UI / `CategoryBar` + `CategoryBarItem`:** reusable horizontal/tab category bar that owns selection state — initial selection, select by index/id, `Next`/`Prev` with configurable wrap, disabled entries, Inspector-authored or runtime `SetCategories(...)` lists. Item views are authored children or spawned from a prefab; the shared selection marker is re-parented onto the selected item with an offset, never resizing or repositioning authored graphics. Reports through `OnCategorySelected(int)` / `OnCategoryIdSelected(string)` and has no Shop dependency; the optional `ShopListViewCategoryBar` adapter (in `Neo.Shop`) drives a `ShopListView` from the bar.
- **Shop / reactive affordability:** public `Shop.CanAfford(item/id)` — owned and free items are always affordable; priced items query the same wallet the purchase would use (per-item `Currency Override Save Key` included). New `Shop.ResolveCurrencyMoney(itemId)` exposes that wallet for balance subscriptions; new optional `IMoneyCanSpend` interface lets custom wallets answer affordability (`Money` implements it). `ButtonPrice` gained an explicit `Unaffordable` state (optional visual group, label, `OnUnaffordable` event, `CurrentType` accessor) — old prefabs keep showing the Buy visuals. New `ShopPurchaseButtonView` subscribes to shop refreshes and wallet balance while enabled, drives `ButtonPrice` (Buy/Select/Selected/Unaffordable) and `Button.interactable` immediately, and re-subscribes on slot rebinding; it unsubscribes safely on disable.
- **Shop / `ShopVariantsPanel` + `ShopVariantStateView`:** furniture/equipment variants panel over `ShopListView`/`ShopItem` with optional `EquipmentManager`: renders unowned/owned/equipped per slot through the small `IShopVariantView` interface (visuals stay prefab-driven), equips after successful purchase, forwards Shop selection into the equipment manager, refreshes on ownership/equipment/list changes, and supports an empty/unequip control (`Unequip()`). `ShopListView` exposes `Views` and `ButtonAction`.
- **GridSystem / `GridPlacementService` + `GridPlacementRequest`:** plain-C# rule-driven placement over the `FieldGenerator` placement API — `RequireEnabled`/`RequireWalkable`/`RequireUnoccupied`, custom `CellPredicate`, `GridOverwritePolicy` (Reject/Overwrite), `Notify` toggle, single-cell factory `GridPlacementRequest.Single(...)`, atomic multi-cell writes with readable failure reasons.

### Fixed
- **Shop / currency resolution before `Start`:** `Shop` now lazily resolves its default wallet on first use, so `CanAfford`/`Buy` called before `Start` (e.g. from a view's `OnEnable`) use the configured `moneySpendSource` instead of falling back to `Money.I`.

### Repo
- Removed accidentally committed dev debris from version control (`TestRunner.cs`, `test_extensions*.cs`, `debug.log`, `memory.db`, `msp_server.log`, `replay_pid*.log`) and extended `.gitignore` so it cannot return.

### Docs
- New pages: `UI/CategoryBar.md`, `Shop/ShopListViewCategoryBar.md`, `Shop/ShopPurchaseButtonView.md`, `Shop/ShopVariantsPanel.md`, `GridSystem/GridPlacementService.md`; updated `SlotEconomyDefinition.md`, `SpinController.md`, `ButtonPrice.md`, `Shop.md`, and the Shop/UI/GridSystem READMEs.

### Tests
- EditMode coverage: `SlotSymbolWeightOverridesTests` (disabled override, reordered/changed symbol lists, zero/negative weights, normalization, deterministic weighted selection), `CategoryBarTests` (initial/runtime selection, wrap and non-wrap navigation, disabled entries, runtime category lists, events), `ShopAffordabilityTests` (balance changes, multi-currency wallets, owned/free items, failed purchases, `ButtonPrice` state rules, `ShopPurchaseButtonView` subscription/rebinding/lifecycle), `ShopVariantsPanelTests` (state rendering, buy-then-equip, failed purchase, unequip, `EquipmentManager` bridge), `GridPlacementServiceTests` (rule toggles, predicate, overwrite policy, atomic footprints, notifications).

## [9.10.0] - 2026-07-16

### Added
- **Cards / `CardSpriteNameParser`** (runtime, `Neo.Cards`): parses card sprite/file names into suit, rank, card back, or joker. Understands English and Russian tokens (`ace_of_spades`, `дама_червы`), numeric ranks 2–14 (`hearts_02`, `spades_14`), compact forms (`AS`, `KH`, `10c`), and common separators. `GetCanonicalName(suit, rank)` returns the recommended file name (`hearts_02` … `spades_14`), so the same convention works for editor auto-fill and runtime sprite loading.
- **Cards / DeckConfig inspector auto-fill:** new "Auto-Fill From Folder..." button assigns all four suit lists, the back sprite, and both jokers from sprite names in a selected `Assets/` folder (multi-sprite sheets supported). Suit slots are cleared first, so the folder is the source of truth; unrecognized and conflicting names are reported in a summary dialog and the console.

### Changed
- **Cards / DeckConfig validation:** a missing back sprite is now a warning instead of an error — the deck generates and face sprites resolve normally; only face-down display is unavailable.

### Fixed
- **Cards / DeckConfigEditor deck-type casts:** `DeckType` was cast from `enumValueIndex` (0/1/2) instead of the stored enum value (36/52/54), so a `Standard36` sprite deck was previewed and validated as 13 cards per suit instead of 9, and the 54-card joker requirement never triggered from the correct enum member.

### Tests
- EditMode coverage for `CardSpriteNameParser`: standard/compact/Russian names, back and joker detection, invalid names, and canonical-name formatting (also verified standalone against .NET with 37 passing cases).

## [9.9.0] - 2026-07-15

### Added
- **Tools / TimerObject:** public `Tick(float deltaTime)` advances a timer deterministically through the same active-state, pause, time-scale, update-interval, event, milestone, completion, and looping pipeline used by Unity's frame update. This supports tests, replay/server clocks, and custom update loops without reflection or duplicate timer logic.
- **UI / AnimationFly request overrides:** individual flights can now override `Duration`, `SpeedMultiplier`, and `DelayBetweenItems`, copy their rendered UI size from a `RectTransform` with `UiSizeSource` (or use an explicit `UiSize`), and tween from `ScaleMultiplier` to `EndScaleMultiplier` with an optional `ScaleEase`.

### Fixed
- **Tools / TimerObject inheritance:** Unity lifecycle hooks (`Awake`, `OnEnable`, `Update`, `OnDisable`, and `OnValidate`) are now `protected virtual`. Derived timers such as `CooldownReward` reliably inherit the frame update in player builds and may extend lifecycle behavior by overriding and calling `base`. Previously the private base `Update` could leave a derived countdown frozen at `00:00`.
- **Bonus / CooldownReward:** validation now overrides and chains to `TimerObject.OnValidate`, preserving both cooldown-specific synchronization and base timer validation.
- **UI / AnimationFly pooling:** pooled visuals now restore their original scale, rotation, and `RectTransform.sizeDelta` before reuse, preventing size or scale compounding across repeated reward flights.
- **Bonus / CooldownReward runtime creation:** dynamically added components now initialize their completion event before subscribing, avoiding a null event when configured entirely from code.

### Tests
- Added deterministic EditMode coverage for `TimerObject.Tick`, API-contract coverage for all inheritable lifecycle hooks, and a PlayMode regression proving that `CooldownReward` advances through the inherited Unity update without a project-side driver.
- Added EditMode and PlayMode coverage for request-level UI sizing, duration/speed overrides, end-scale tweening, arrival ordering, and pooling-safe visual reuse.

## [9.8.2] - 2026-07-03

### Fixed
- **Missing `[NeoDoc]` attributes:** 32 public `MonoBehaviour`/`ScriptableObject` types (e.g. `LevelComponent`, `ShopItemData`, `RpgAttackDefinition`, `StatusEffectDefinition`, `SaveProviderSettings`, `InteractionRayProvider`, and 26 others) had a matching `.md` page in `Docs/` but no `[NeoDoc(...)]` attribute pointing at it — the Inspector showed "No documentation linked" even though the page existed. All 32 fixed. `PackageHealthCheck` only verified that *existing* `[NeoDoc]` paths resolve; it didn't catch a component missing the attribute entirely — this class of bug was invisible to automation until inspected manually.
- **Duplicate auto-generated stub docs:** 4 `.md` pages (`Cards/Config/DeckConfig.md`, `Core/Level/Data/LevelCurveDefinition.md`, `Rpg/Data/RpgAttackDefinition.md`, `Rpg/Data/RpgAttackPreset.md`) were low-quality auto-generated field dumps (including bogus "fields" like literal `true`/`100f` picked up by whatever tool scaffolded them) duplicating a better hand-written page elsewhere for the same class. Deleted; `[NeoDoc]` now points at the real page. Fixed a doc link in `Core/README.md` left dangling by the deletion.
- **7 more classes had zero doc coverage** (no `[NeoDoc]`, no matching page): `GridCellMarker`, `NoCodeFloatBindingBehaviour` (+ its embedded `ComponentFloatBinding`), `InventoryDatabase`, `InventoryInitialStateData`, `InventoryItemStateBehaviour`, `PageId`, `NeoDebugOverlay`. Wrote real pages for all 7 and linked them.

### Notes
- 21 of the 32 fixed classes above still only have an auto-generated field-dump page as their *only* doc (no hand-written alternative existed to fall back on) — the link now resolves and something real is shown, but the prose quality is low. Flagged as follow-up debt, not rewritten in this pass to keep scope bounded.

## [9.8.1] - 2026-07-03

### Added
- **`link.xml`** — preserves `Neo.*` assemblies and `Assembly-CSharp` from IL2CPP managed code stripping. `NeoCondition`, `ComponentFloatBinding`, `[SaveField]`, `NetworkPropertySync`, and `NetworkReactiveSync` all resolve members by name via reflection (including private, non-serialized fields) — under IL2CPP that member can be legally stripped if nothing else references it, silently breaking the NoCode binding in a release build while working fine in the Editor. See `Docs/IL2CPP.md` for the full explanation and the escape hatch for custom asmdef setups.
- **`THIRD-PARTY-NOTICES.md`** — lists every optional/required third-party dependency (UniTask, DOTween, Mirror, Spine, Odin, MarkdownRenderer), why it's referenced, and where to find its license. None of them are bundled inside the package.

### Changed
- **Minimum Unity version raised to `6000.0`** (was `2022.1`). The package is now developed and validated against Unity 6 only; projects on Unity 2022 LTS should stay on the last `9.7.x` release.

### Fixed
- **`Docs/Tools/Spawner/Spawner.md`** described deny zones (`_denyAreas`/`_denyAreas2D`, `IsPositionAllowed`) as a "Planned (TODO)" feature — they shipped in `9.7.0`. Doc rewritten to match the current API (also documents `Spawn Area`, `Max Waves`, `Spawn On Awake`, `Parent Transform`, which were undocumented).
- **`Docs/StateMachine/README.md`** had a malformed module table (missing separator row, a stray bullet line instead of the README's own entry) that broke rendering.
- Six dead internal doc links fixed (`Bonus/Slot/*` cross-links and `GridSystem/Dice/DiceBoardService.md` → `Merge/README.md`), left over from the RU→EN docs folder reorganization.
- **`Samples~/NeoxiderPages/Runtime/API/UIKitAPI.cs`** — removed unconditional `Debug.Log` calls on every `G.Pause/GoMenu/Start/Restart/Win/Lose/End` call. This is a reference-implementation sample meant to be copied into real projects; the logging was leftover debug instrumentation with no gate, so every game using the pattern verbatim would spam the console on every state transition.

## [9.8.0] - 2026-07-03

### Changed
- **Docs are English-only.** The `Docs/` (RU) tree has been removed and `DocsEn/` renamed to `Docs/`; `[NeoDoc(...)]` attributes and all package/README links now point at the single English tree. `PackageHealthCheck` was rewritten to verify every `[NeoDoc]` path resolves under `Docs/` instead of checking RU/EN parity.
- **README rewritten** around four audiences: NoCode beginners, professional C# API users, multiplayer, and AI-agent development. Added `Docs/NoCode/GettingStarted.md`, a beginner-facing tour of every no-code building block.
- `DOCUMENTATION.md` / `DOCUMENTATION_GUIDELINES.md` rewritten in English (they previously mandated Russian for `.md` pages).
- Removed the root `README_RU.md`; the repo now ships a single English README.
- Filled in genuinely missing `Network/*` documentation pages (`NeoNetworkComponent`, `NetworkSingleton`, `NeoNetworkManager`, `NetworkOwnerFilter`, `NetworkActionRelay`, `NetworkPropertySync`, `NoCode_Network_Spec`, `Lobby`, `Multiplayer_Guide`) and `NeoxiderPages/PM.md`, which were English placeholder stubs.

### Fixed
- **Save / FileSaveProvider:** removed the GC finalizer that called `Save()` (and therefore `JsonUtility`, a main-thread-only Unity API) from the finalizer thread.
- **Save / SaveManager:** `OnApplicationQuit` now calls `SaveProvider.Save()` after writing, so file-backed providers actually flush to disk on quit instead of relying on the removed finalizer.
- **Shop.cs:** translated two legacy-field tooltips from Russian to English.
- **Cards/Editor/DeckConfigEditor.cs:** fixed a mojibake character in a HelpBox warning string.
- **StateMachine.ChangeState:** `OnExit` now runs on the previous state before `CurrentState` is reassigned, so exit-handlers and re-entrant `ChangeState` calls see consistent state.
- **Network/NeoNetworkComponent.cs:** removed a stray Russian word left in an XML doc comment (last remaining Cyrillic string in the runtime/editor code).

### Tests
- No behavior changes to test coverage this release; full EditMode (631 tests) and PlayMode (106 tests) suites verified green after the docs/audit pass.

## [9.7.1] - 2026-07-02

### Fixed
- **Naming:** new 9.7.0 public serialized fields renamed to PascalCase per package convention (`ShopCategorySelector.Category`: `Id/DisplayName/Icon`; `SlotEconomyDefinition.Symbol`: `Name/Id/MoneyReward/BonusReward/IsSpecial/Weight`; `EquipmentManager.CategorySlot`: `CategoryId/SpriteTarget/ImageTarget/ApplyNativeSize/DefaultItemId`). `[FormerlySerializedAs]` keeps existing scene/asset data intact.
- **Tests:** `NetworkRateLimitTests` no longer triggers Mirror's "requires a NetworkIdentity" `OnValidate` error — the probe object now carries a `NetworkIdentity`.

### Tests
- Edit-mode coverage for the 9.7.0 additions: `SlotEconomyDefinition` (payline evaluation, special-line conversion, weighted picker), `EquipmentManager` (equip/unequip/toggle, category replacement, slot visuals), `ShopCategorySelector` (wrap-around cycling, select-by-id, empty-list safety), `Spawner` deny zones (3D/2D rejection, null entries).

## [9.7.0] - 2026-07-02

### Added
- **Shop / ShopCategorySelector:** NoCode category pill with prev/next arrows cycling a serialized category list into `ShopListView.SetCategory` — complements `ShopCategoryButton` for shops browsed sequentially (pattern extracted from a shipped dress-up game).
- **Shop / Equipment (new):** `EquipmentManager` + `EquipItemDefinition` — multi-category dress-up/skins: one item per category, sprite applied to a `SpriteRenderer`/`Image` slot (optional `SetNativeSize`), worn set persisted via `SaveProvider`, `OnEquipChanged` event, `EquipById/Unequip/ToggleById` NoCode API. Pairs with `Shop` ownership for buy-then-wear flows.
- **Bonus / SlotEconomyDefinition:** slot-machine economy SO — weighted symbol table (money/bonus payouts, special flag), `PickWeightedId()`, `ApplySpecialRule()` (one special converts the payline) and `EvaluateLine()` returning a typed `LineResult`. Removes the per-game hand-rolled economy layer over `SpinController`.
- **Bonus / ResourceRegen:** one-component regenerating resource — couples `CooldownReward` (auto-claim forced on) with a capped `Money` wallet and an optional `TimeToText` countdown (shows 0 while full).
- **Network / NetworkReactiveSync:** NoCode replication for `ReactivePropertyFloat/Int/Bool` — inspector counterpart of `NetworkReactivePropertyBridge`; multiplayer wallets/score/HP without hand-written SyncVar code. Inert without Mirror.
- **Network / NetworkPlayerName:** replicated player nickname (trimmed + length-capped server-side, rate-limited command, `OnNameChanged` for TMP labels). Works locally without Mirror.
- **Network / NeoNetworkDiscovery Quick Play:** `QuickPlay()` — one-button LAN flow: auto-join the first server found, or host after `Host If None Found After` seconds; `OnQuickPlayResolved(bool becameHost)`.
- **Network / NetworkEventDispatcher payloads:** `DispatchGlobalInt/Float/String` + matching UnityEvents (rate-limited, authority-checked like the parameterless event).
- **Network / NeoNetworkComponent:** per-connection `RateLimitCheck(sender)` overload — one spamming client no longer starves other clients' commands on shared scene objects (used by `NetworkEventDispatcher`).
- **Network / NetworkPropertySync:** `Skip Hook On Owner` option — the owner ignores the server echo of its own values in `OwnerToServer` mode (prevents rubber-banding).
- **Network / NeoNetworkManager:** inspector toggles for gated `NetworkDiagnostics` runtime logs/warnings (NoCode network debugging).
- **Tools / Spawner deny zones:** `_denyAreas`/`_denyAreas2D` + `Max Rejection Tries` + `IsPositionAllowed(Vector3)` — random points inside deny zones are re-rolled (closes the long-standing in-code TODO documented in 9.5.1).
- **Pages (sample):** `PM.ChangePageByName` now falls back to the page GameObject name and lists known PageId names in its error; `UIPage` gained an inspector `Open` button.
- **Editor / Package Health Check** (`Tools → Neoxider → Package Health Check`): verifies package version parity (package.json ↔ README ↔ PROJECT_SUMMARY ↔ CHANGELOG) and Docs/DocsEn parity — both drifts have shipped before.

### Tests
- `CooldownReward` auto-claim re-arm (covers the 9.6.1 fix), `Money` soft cap (`Add` clamps / `AddOverflow` ignores / 0 = unlimited), network command rate limit.

### Performance
- **Network / NeoMirrorSceneReactivator:** walks scene roots instead of `Resources.FindObjectsOfTypeAll` (no longer touches prefab assets on every scene load).

### Docs
- RU+EN pages for every new component; "Lobby on Neo.Pages" recipe in `Multiplayer_Guide`; deprecated types now have an explicit removal target (10.0); `ReactiveProperty` performance/naming notes; missing `Cookbook.md` metas restored.

## [9.6.2] - 2026-07-02

### Fixed
- **Network / NetworkEventDispatcher:** `CmdDispatchEvent` is now rate-limited (`RateLimitCheck`), closing a spam-amplification hole — any client could flood the global RPC broadcast (the command is `requiresAuthority = false` with default authority `None` by design).
- **Network / NetworkPropertySync:** `Sync Interval` gained a `[Min(0.1)]` floor. An interval below the server rate limit (0.05 s) caused silent Cmd drops in `OwnerToServer` mode: the owner marked the value as sent while every client stayed stuck on the stale value until the next change. Also: a missing target/field no longer poisons the reflection cache — a target assigned later at runtime is picked up (warning logged once).
- **Reactive / ReactiveProperty:** `NotifySubscribers` now takes a real snapshot of code listeners (reusable buffer, no per-notify allocation). Previously removing an earlier listener inside a callback shifted indices and the next listener silently skipped that notification. New edit-mode test covers the case.
- **Network / NetworkSingleton:** `IsInitialized` now actually reflects initialization instead of duplicating `HasInstance`.
- **Network / NeoNetworkManager:** one-time warning when Mirror's private `NetworkIdentity.hasSpawned` field is missing (Mirror upgrade guard) instead of silent scene-player-template degradation.

### Docs
- RU/EN pages updated for the fixes: `NetworkPropertySync` (interval floor + owner rubber-band caveat), `NeoNetworkComponent` (rate limit is per object, not per client), `ReactiveProperty` (snapshot semantics, main-thread only), `NetworkEventDispatcher` (RU; command rate limit).

## [9.6.1] - 2026-07-02

### Fixed
- **Bonus / CooldownReward + Tools / TimerObject:** continuous auto-claim (`_autoClaim`) now re-arms after each grant. Previously the underlying non-looping timer deactivated itself right after the completion event, so auto-claim fired once and `RemainingTime` stopped ticking. `TimerObject` now deactivates a non-looping timer **before** invoking `OnTimerCompleted`, so completion handlers may restart it with `Play()`; `CooldownReward.TakeReward()` restarts the cooldown timer (mirroring `RestartTime()`) and resets the availability flag so `OnRewardAvailable` fires on every cycle.

### Docs
- Synchronized version references in `README.md` and `PROJECT_SUMMARY.md` (were still `9.5.2`).

## [9.6.0] - 2026-06-27

### Added
- **Shop / Money:** optional soft cap `_maxMoney` (0 = unlimited). `Add()` and `SetMoney()` now clamp to it; new `AddOverflow(float)` deposits **ignoring** the cap for bonus/overflow rewards allowed to exceed it. Enables capped resources (energy / stamina / lives) without custom code. Runtime get/set via `MaxMoney`.
- **Bonus / CooldownReward:** `_autoClaim` option — automatically claims the reward the moment it becomes available (continuous regen) without manual `TakeReward()` / event wiring. Stays decoupled from wallets (no `Money` dependency); deposit via `OnRewardClaimed → Money.Add(...)`. Capped-regen recipe: `_autoClaim` + `OnRewardClaimed → Money.Add(1)` + `Money._maxMoney` cap (+ `AddOverflow` for sources allowed past the cap). Runtime get/set via `AutoClaim`, `CooldownSeconds`, `MaxRewardsPerTake`.

### Docs
- **Cookbook (new):** added a cross-module recipes page (RU + EN) consolidating end-to-end examples — capped energy + auto-regen, capped currency, daily reward, slot → wallet, shop buy/equip, reward fly — linked from the docs index.
- **Shop / Money:** documented `_maxMoney` and `AddOverflow`. **Bonus / CooldownReward:** documented `_autoClaim` and the capped-regen recipe.

## [9.5.2] - 2026-06-27

### Fixed
- **Samples / Demo Scenes / Network:** imported Demo Scenes now compile in projects without the optional Mirror package. `TestStart` follows the `Neo.Network` optional-Mirror pattern: Mirror `NetworkBehaviour`/`Command`/`ClientRpc` code is compiled only under `MIRROR`, while non-Mirror projects get a local solo-mode fallback.

### Docs
- **Package docs:** synchronized package version references and sample import paths for `9.5.2`.

## [9.5.1] - 2026-06-27

### Docs
- **Tools / Spawner:** documented planned **deny zones** (areas where spawning is forbidden) as a TODO — `_denyAreas`/`_denyAreas2D` + `_maxRejectionTries` reject candidates inside a deny zone and re-roll, plus `IsPositionAllowed(Vector3)`. "Where allowed" = spawn points / spawn area; "where forbidden" is planned. (#3)

## [9.5.0] - 2026-06-27

### Changed
- **Tools / Spawner:** the single `_spawnTransform` spawn point is now a `Transform[] _spawnPoints` array. Empty → spawn from the spawner's own transform (previous default); with one or more points, a random non-null point is picked per spawn (position and rotation stay consistent for that spawn). New public `ResolveSpawnPoint()`. ⚠ **Breaking:** scenes that referenced the old single `Spawn Transform` field lose that reference on upgrade and fall back to the spawner's own transform — re-assign points in `Spawn Points`. (#3)

## [9.4.0] - 2026-06-25

### Added
- **Extensions / Random:** `GetRandomWeighted<T>(items, weights)` and `GetRandomWeighted<T>(items, weightSelector)` — return the weighted random **element** directly instead of just the index (`GetRandomWeightedIndex`).
- **Extensions / Dictionary:** new `DictionaryExtensions` with `GetOrCreate(key)`, `GetOrCreate(key, factory)`, and `Increment(key, amount = 1)` for `int`/`float` counters — replaces the repeated get-or-create and `dict[k] = dict.GetValueOrDefault(k) + 1` boilerplate. (`GetValueOrDefault` is intentionally left to the BCL to avoid overload ambiguity.)
- **Extensions / Primitive (Math):** `Snap(step)` (float/int), `Wrap(min, max)` (negative-safe cyclic index), `PingPong(length)` (int), and `Approximately(b)` / `Approximately(b, tolerance)` float-equality helpers.

## [9.3.0] - 2026-06-23

### Added
- **Audio / AM:** `Play(AudioClip)` and `PlayMusicByClip(AudioClip)` convenience overloads; `SetMusicVolume`/`SetEfxVolume` (replacing the boolean-trap `SetVolume(volume, bool)`); `startVolumeEfx`/`startVolumeMusic` renamed to PascalCase with `[Obsolete]` forwarders.
- **Bonus / Slot:** `SpinController.SpinResult` value type with `GetLastResult()` and `LastPayout` — one coherent spin outcome (symbol grid, winning lines, payout).
- **Tools / Debug:** `NeoDebugOverlay` — drop-in IMGUI runtime overlay (FPS, active scene, time scale, AM/SaveManager status), toggled with F3.
- **Tests:** `AmEditModeTests`, `SpinControllerPaylineTests`, and a public-PascalCase convention check in `ModulePrinciplesTests`.
- **Docs:** EN parity for Quest and Tools, bilingual Getting Started, and a NeoDoc link checker that now verifies the DocsEn mirror.
- **GridSystem / Dice:** added serializable `DiceValueWeight` and `DicePieceGenerator.GenerateWeighted(...)` for designer-controlled dice value pools, explicit invalid-weight validation, and non-duplicating weighted pairs.
- **GridSystem:** expanded `GridSlotAllocator` with `Capacity`, `HasAvailableSlot`, slot-index preferred allocation, slot-index release, and `Clear(...)` for reusable autobattler benches, tactical rows, backpack rails, and compact board lifecycle management.
- **GridSystem:** extended `GridSlotAllocator` with linear slot-index helpers (`TryGetSlotPosition`, `TryGetSlotIndex`, `IsAvailable(int)`, `Allocate(int, int)`) for rectangular 2D boards such as autobattler benches, tactical rows, hotbars, and card-game lanes.
- **Cards:** added optional finite `HandModel.Capacity`, `RemainingCapacity`, `IsFull`, `TryAdd(...)`, and `AddRangeUntilFull(...)` so CCG hands, autobattler benches, backpack rails, and market rows can reject overflow explicitly while unlimited hands remain the default.
- **GridSystem / Dice:** added `DicePieceGenerator.CreateD6Pool()` and `CreateSequentialPool(minValue, maxValue)` for classic dice rolls and custom numbered pools without duplicating pool construction in games.
- **GridSystem:** added `GridSlotAllocator` for ordered one-cell slot allocation on top of `FieldGenerator`, covering benches, tactical rows, autobattler boards, hotbars, and market rows without duplicating occupancy checks.
- **UI / AnimationFly:** added reusable motion presets for typed requests (`Arc`, `Fountain`, `Magnet`, `FountainMagnet`, `Scatter`) with burst and magnet tuning fields, plus PlayMode coverage for fountain trajectory and deterministic fountain+magnet rewards.
- **Samples / UI:** added an `AnimationFlyDemo` scene with runtime buttons, a real sample sprite asset example, and labeled sliders for count, duration, delay, arc, scale, and rotation so fly-effect flows can be inspected without manual scene editing.

### Fixed
- **Editor scene-dirtying:** `ParallaxLayer`, `CameraAspectRatioScaler`, and `AM` no longer mark the open scene dirty on load (perpetual `*`): removed unconditional `SetDirty` in editor delay-calls, made preview generation transient (`HideAndDontSave`), and drive the aspect-ratio camera only at runtime.
- **Runtime performance:** `RpgProjectile` and `MagneticField` now use NonAlloc physics queries + reused buffers instead of per-frame heap allocations; `Drawer` releases its owned/cloned Materials in `OnDestroy`; `InteractiveObject` caches colliders and reuses the cached camera; `Singleton.I` no longer re-runs `FindObjectsByType` on every access when no instance exists.
- **Cards (async lifetime):** `DrunkardGame`, `BoardComponent`, `DeckComponent`, `HandComponent`, `HandView`, and `CardView` bind UniTask awaits to `GetCancellationTokenOnDestroy` and link tweens to the GameObject, preventing `MissingReferenceException` on scene change mid-animation.
- **Audio / AM:** `OnDestroy` now stops `RandomMusicController` so its looping UniTask cannot run after teardown.
- **Bonus / Slot:** aligned slot element scene gizmo coordinate labels with the `SpinController` console grid index base and guarded `VisualSlotLines` against missing line references.
- **Tools / Compatibility:** switched `MouseInputManager`, `MouseEffect`, `ParallaxLayer`, and `NetworkContextActionRelay` debug IDs to `Object.GetEntityId()` under `UNITY_6000_5_OR_NEWER` (Unity 6.5+), while preserving `GetInstanceID()` for older versions.
- **Samples / NeoxiderPages:** removed hard DOTween/DOTween Pro runtime dependencies from `UIPage` and `BtnChangePage`, stripped legacy `DOTweenAnimation` components from sample prefabs, and declared `com.unity.ugui` as a package dependency so imported page prefabs resolve standard uGUI scripts.
- **Samples / NeoxiderPages:** fixed `UIPageEditor` null-reference spam after removing the legacy `_animation` field and cleaned stale `_animation` serialized references from sample page prefabs.
- **Samples / NeoxiderPages:** hardened `UIPageEditor` against missing serialized fields so partial imports or stale sample objects render warnings instead of throwing inspector `NullReferenceException`s.
- **Samples / NeoxiderPages:** removed dangling prefab component references from `_Page base` and `Shop Page` so Unity no longer reports corrupt prefab imports after the legacy tween cleanup.

## [9.2.0] - 2026-06-04

### Added
- **GridSystem:** added `FieldGenerator.TryGetCellPositionFromWorld`, `TrySnapWorldToCellCenter`, and `SnapWorldToCellCenter` so grid drag/drop and preview snapping can use the generator's own origin-aware nearest-cell conversion API.
- **GridSystem:** added reusable `GridPlacementEntry`, `GridPlacementResult`, `FieldGenerator.CanPlaceContentFootprint`, and `FieldGenerator.PlaceContentFootprint` for writing multi-cell pieces/items/shapes into grid cells.
- **GridSystem / Merge:** added `GridMergeRequest.Increment(...)` factory preset so the common "merge equal content into content+step at the seed" rule no longer needs ~10 delegates wired by hand, plus a `NotifyOnContentChanged` toggle so callers can apply extra state before notifying.
- **Merge:** added `MergeRequest.MaxCascadeIterations` and `MergeResult.CascadeLimitReached` (mirrored on `GridMergeResult`) so the cascade safety limit is configurable and surfaced instead of stopping silently.
- **GridSystem / Dice:** exposed `DiceBoardService.MinMergeGroupSize`, `MergeStep`, `MaxContentId`, and `RequireWalkable` so dice merge rules are tunable without editing the service; `DicePiece` now exposes `CellCount`.
- **Samples / Dice:** added a `Dice.prefab` visual used by the Dice Merge demo instead of constructing dice visuals in code.
- **Docs:** added RU/EN API pages for `GridPlacementEntry` and `GridPlacementResult`, placement examples in `FieldGenerator` docs, and current TODO/Ideas notes for GridSystem placement follow-ups.
- **Tests:** added EditMode coverage for cascade-limit flagging, multi-cell `DicePiece` rotation, single consistent merge notifications, single `OnBoardChanged` per merging placement, and configurable merge step/cap.

### Changed
- **GridSystem / Dice:** `DicePiece.RotateClockwise`/`RotateCounterClockwise` now rotate footprints of any size around the anchor (not just pairs), and the demo controller enumerates orientations / allows rotation for any multi-cell piece.

### Fixed
- **Build / Runtime:** guarded runtime assembly references to `UnityEditor` APIs in prefab previews and button drawers so player builds do not pull editor-only namespaces; added an architecture test for unguarded runtime `UnityEditor` usage.
- **Save:** changed `SaveManager.Save()` to read-modify-write the shared `SaveData_All` container so data for unloaded scene objects is preserved; added EditMode coverage for absent component entries, multiple preserved unloaded entries, and current-component load round-trip.
- **Bonus / Slot:** split paid and free spin payment logic so positive-price spins require an `IMoneySpend` wallet while zero-price spins remain explicitly free; added EditMode coverage for paid/free payment paths.
- **Shop / Money:** added `MoneySpendResult` / `IMoneySpendWithResult` / `IMoneySpendAuthority` so callers can distinguish confirmed, rejected, and pending server-authority spends; `Shop` no longer performs wallet-only server spend requests for pending item/bundle purchases without a matching server-side grant path; added EditMode coverage for detailed spend statuses and pending authority item/bundle purchases.
- **Core / Level & Resources:** fixed XP-backed `SetLevel` threshold sync, `LevelNoCodeAction` false level-up events, duplicate `OnDeath` dispatch, custom resource depleted events, and regen-from-zero contracts; added targeted EditMode coverage.
- **RPG:** fixed buff stack application/projection/snapshot persistence, required-target presets spending resources before target resolution, hit dedup/target resolution for arbitrary `IRpgCombatReceiver` implementations, and reusable projectile initialization state; added targeted EditMode coverage.
- **Samples / Package Validation:** removed a sample-only page-switch component reference from the package `ButtonPageSwitch` prefab, restored the active `DemoLevelCurve` validation asset, made sample scene coverage resolve `Samples~` scene files by filesystem path/YAML, and made PlayMode sample smoke skip hidden `Samples~` runtime scenes that Unity cannot compile directly.
- **Tools / Move:** made `FreeFlyCameraController` movement tests deterministic by pinning external look input when verifying local-forward movement.
- **UI / AnimationFly:** added a typed request/result API, sprite-only fly visuals, built-in disable-and-pool completion, reward timing callbacks, and parent-local Canvas coordinate conversion so world-to-UI rewards spawn in UI while visually starting at the world pickup position; pooled fly visuals now kill/link tweens and reset base transform state before reuse; added EditMode/PlayMode coverage for coordinate, reward timing, and pooled scale contracts.
- **Cards:** fixed duplicate-card removal by index so `HandModel`, `HandPresenter`, and `HandComponent` remove the same indexed card/presenter/component instead of the first equal card data; `HandComponent` now removes only its own card click listeners, `DeckComponent` detaches old model events on reinitialize, and card views own/kill hover tweens; added EditMode coverage for duplicate card ordering and lifecycle contracts.
- **NoCode / Lifecycle:** exposed explicit editor/runtime refresh and subsystem reset hooks used by EditMode validation across NoCode bindings, Save, MouseInputManager, and SwipeController.
- **GridSystem / Dice:** fixed double `OnCellStateChanged` notifications with stale `IsOccupied` during merges — the resolver no longer notifies mid-mutation; `DiceBoardService` applies occupancy and raises one fully-consistent notification per cell.
- **GridSystem / Dice:** fixed `DiceBoardService.Place` raising `OnBoardChanged` twice on a merging placement; placement and follow-up merges now raise it exactly once.
- **GridSystem / Merge:** stopped allocating a full board cell list on every `GridMergeResolver.Resolve` when explicit seeds are supplied.
- **Samples / Dice:** fixed drag preview/drop behavior so the tray dice is hidden while dragging, the preview can be offset above the pointer, optional snap preview uses `FieldGenerator`, final release uses the current snapped/nearest grid cell, and placed dice keep the prefab world scale instead of shrinking under scaled cell prefabs.
- **Samples / Dice:** fixed placed dice visuals disappearing after drag/drop by syncing from `DiceBoardService.OnBoardChanged`, resolving missing demo view references within the view's own hierarchy (no global `FindObjectOfType` that could bind to another board), rebuilding missing cell views before board refresh, keeping board dice under a dedicated `DicePlacedPiecesView` root, refreshing placed visuals after drag preview cleanup, and reusing placed die views across refreshes instead of destroying/recreating them every frame.
- **Samples / Dice:** made the demo empty-content fallback consistent with `DiceBoardService.EmptyContentId` (-1) and cached the fallback solid sprite instead of allocating a new `Sprite` per cell/die.
- **Samples / Dice:** fixed placed dice being destroyed on mouse release. Two reinforcing fixes: (1) the view's visual roots are now deterministic — cached and never recovered via `transform.Find` (which, with play-mode deferred `Destroy` and runtime roots persisted in the saved scene, could return a stale/duplicate root and orphan placed dice), and the view rebuilds cleanly instead of adopting persisted scene cells; (2) on a successful drop the dragged preview dice are now *promoted* into their destination cells and reused as the persistent placed visuals (registered before the model mutates, so `OnCellStateChanged`/merges reuse the same objects), and the preview is only destroyed on a failed drop.
- **GridSystem / Dice:** guarded Dice placement and demo previews against missing `DicePiece.Cells` data so startup/drag/drop cannot throw when a piece is empty or partially initialized.
- **GridSystem / Merge:** hardened `GridMergeResult`, `GridMergeGroupResult`, and `DicePlacementResult` collections as read-only-reference lists so callers cannot reassign result buffers.
- **Docs:** updated `PROJECT_SUMMARY.md` as a compact module/reuse map and linked it more prominently from the main README entry points.

## [9.1.0] - 2026-06-02

### Added
- **Merge:** added `Neo.Merge`, a standalone pure C# connected-group merge engine for grids, inventories, lists, graphs, and custom board-like mechanics.
- **GridSystem:** added `GridMergeResolver` adapter for applying generic merge rules to `FieldGenerator` / `FieldCell.ContentId`.
- **GridSystem / Dice:** added `Neo.GridSystem.Dice` with dice pieces, pool-based piece generation, dice placement, and dice merge resolution.
- **Samples:** added a playable 5x5 Dice Merge demo scene using the Dice sprites under `Assets/Neoxider/Sprites/Dice`.
- **Tests:** added EditMode coverage for generic merge, GridMerge, Dice mechanics, combined Dice/GridMerge behavior, and PlayMode smoke coverage for the Dice demo.

## [9.0.0] - 2026-05-26

### Added
- **Diagnostics:** added `NeoDiagnostics` in the shared `Neo.Extensions` assembly as the package logging gate. Info and warning output is disabled by default, errors remain visible, throttled warnings are supported, and static state resets under domain-reload-disabled play mode.
- **Tests:** added EditMode coverage for `NeoDiagnostics` and architecture coverage that keeps aligned runtime roots from reintroducing raw `Debug.Log*` calls.
- **Samples:** added required smoke scenes for Audio, Level, Network, NoCode, Parallax, Save, Settings, and StateMachine under the active development samples root. Each scene has a `ModuleDemoSceneInfo` marker and minimal module wrapper setup.
- **Tests:** added sample scene coverage that opens required smoke scenes, checks `ModuleDemoSceneInfo`, and verifies missing scripts. Sample validation now supports both active development `Samples` and release/UPM `Samples~` roots.

### Changed
- **Package samples:** aligned `displayName` to `NeoxiderTools` so Unity imports samples under `Assets/Samples/NeoxiderTools/<version>/<sample>`. Validation still accepts the legacy `Assets/Samples/Neoxider Tools` root for already-imported samples.
- **Cards / Bonus / UI / Tools Move:** moved remaining direct runtime logs in the aligned roots behind `NeoDiagnostics` or explicit component debug flags. `AnimationFly` and `UniversalRotator` now cache their `Camera.main` fallback instead of resolving it on every conversion/aim call.
- **Parallax / Tools Input:** added explicit camera injection APIs and throttled/optional `Camera.main` fallback paths for `ParallaxLayer` and `MouseInputManager`, with RU/EN docs and EditMode coverage.
- **Samples:** routed demo setup feedback through one sample diagnostics helper instead of direct setup-script `Debug.Log` calls.
- **Docs:** documented the current `Samples` development path, the `Samples~` UPM source path, and Unity's imported `Assets/Samples/NeoxiderTools/<version>/<sample>` path in `AGENTS.md`, Samples docs, package compatibility docs, and README navigation.
- **Docs / GridSystem:** restored the top-level RU/EN GridSystem docs to readable UTF-8 and documented its constructor role for Match3, TicTacToe, 2048-like SlidingMerge, pathfinding, views, and spawners.

## Legacy History

Entries before `9.0.0` were trimmed from the package changelog to keep the release notes focused (earlier versions were also cleaned during a prior UTF-8 pass). Use git history or release tags for the exact older notes.
