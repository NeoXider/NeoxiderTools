# Audio module

<!-- module-art:begin -->
<p align="center"><img src="https://raw.githubusercontent.com/NeoXider/NeoxiderTools/main/Images/modules/audio.jpg" alt="Concept art for the Audio module" width="100%" /></p>
<!-- module-art:end -->

**What it is:** sound management for Unity under `Scripts/Audio/` (namespace `Neo.Audio`): the singleton **AM** (Audio Manager) for effects, music pools and ambience, the singleton **AMSettings** for persisted volume and mute (optionally through an **AudioMixer**), and small play-on-event and UI components.

<!-- demo-shots:begin -->
<p align="center"><img src="https://raw.githubusercontent.com/NeoXider/NeoxiderTools/main/Images/demos/AudioDemo.jpg" alt="AudioDemo: AM sound entries, Loop and Shuffle music pools, persisted volumes and mutes" width="100%" /></p>

*Demo scene: `Scenes/Audio/AudioDemo.unity`, in the Demo download (`Assets/NeoxiderDemos/Demo`). The scenes are saved with their UI and wiring, so you can inspect them without entering Play Mode.*
<!-- demo-shots:end -->

**Navigation:** [← Docs](../README.md)

**How to work with it:**
1. Add `AM` to a global object and fill its sound and music entries ([AM](AM.md)).
2. Add `AMSettings` for persisted volume and mute, and `AudioControl` on the settings sliders and toggles.
3. Play sounds with `AM.I.Play("id")` from code, or with `PlayAudio`, `PlayAudioBtn` and `MusicControl` from
   UnityEvents.

**Contents:** the component list below; the main guides are [AM](AM.md), [AMSettings](AMSettings.md) and [AudioControl](View/AudioControl.md).

---

## Main pieces

- **[AM](AM.md)** — main singleton for effects, music pools (`Loop` / `Shuffle`, crossfaded) and ambience, with Sfx and UI channels, optional cues and positional one-shots.
- **[AMSettings](AMSettings.md)** — Singleton for global sound settings (volume, mute, mixer). Volumes and mutes persist through `SaveProvider`; binding never forces a muted channel back on.
- **[PlayAudio](PlayAudio.md)** — Component to play a sound from AM (by id, by index, or from a clip list).
- **[PlayAudioBtn](PlayAudioBtn.md)** — Plays sound on UI Button click (by id, by index, or from a clip list).
- **[MusicControl](MusicControl.md)** — No-code music: start a pool by id, move to another track of it, stop. Every method is UnityEvent-ready.
- **[SettingMixer](SettingMixer.md)** — Set/get AudioMixer parameters (normalized 0–1 or dB) with enum presets (`Master/Music/Efx`) and `Custom` mode.
- **AudioControl** — Binds a Toggle or Slider to AMSettings (Master/Music/Efx), supports `Set(bool)` and normalized `Set(float)` in `0..1`, and has a `Custom` mode with UnityEvents; Scripts/Audio/View/AudioControl.cs.
- **[AudioFocusMute](AudioFocusMute.md)** — Silences all game audio while the application has no focus (minimized, another tab or app) and restores the volume on return; one `AudioFocusMute.Install()` at startup.
- **[RandomMusicController](RandomMusicController.md)** — Standalone plain C# helper for random track playback. `AM` has its own pool engine; use this class only for direct use.

## Runtime and scene entry points

```csharp
using Neo.Audio;

AM.I.SetSoundEntries(new SoundEntry("hit", hitClip));
AM.I.SetMusicEntries(new MusicEntry("gameplay", trackA, trackB));
AM.I.Play("hit");
AM.I.PlayMusicPool("gameplay");
AM.I.SetMusicVolume(0.6f);
```

For scene authoring, add `AM` and `AMSettings`, configure entries and optional mixer routing, then use
`PlayAudio`, `PlayAudioBtn` and `MusicControl` from UnityEvents. Add `AudioControl` to a Toggle or Slider;
select `AudioSourceAndMixer` when controlling source volumes without a mixer. Its synchronization is
silent: refreshing the view does not invoke the UI element's value-change callbacks.

`RandomMusicController` is a plain C# class, not an addable component; see its
[initialization and ownership example](RandomMusicController.md).

## Demo scene

`Assets/NeoxiderDemos/Demo/Scenes/Audio/AudioDemo.unity` (Demo download). Look at **Audio Demo** (`AM` with four
sound entries and the `theme` Loop and `pads` Shuffle pools), **[Demo] Services** (`AMSettings` with demo-only
save keys `Neo.Demo.Audio.*`, and a `MusicControl`) and **[Demo] Frame** (buttons, sliders and toggles wired to
`AM`, `PlayAudioBtn`, `MusicControl` and `AMSettings`). Everything is saved in the scene.

## See also

- [Audio editor authoring](Editor.md) — entry lists, clip controls and id drawers.
- [Animations](../Animations/README.md)
- [UI](../UI/README.md)
