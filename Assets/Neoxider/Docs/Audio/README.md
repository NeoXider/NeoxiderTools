# Audio module

The **Audio** module provides tools for sound management in Unity: a central audio manager, volume/mute settings via AudioMixer, and simple play-on-event components.

The system is built around singleton **AM** (Audio Manager) for playback and singleton **AMSettings** for global volume and mute via **AudioMixer**.

Demo in the release layout: `Assets/Neoxider/Samples~/Demo/Scenes/Audio/AudioDemo.unity`; controller:
`Assets/Neoxider/Samples~/Demo/Scripts/Shell/AudioDemoController.cs`. During sample development the root
is `Assets/Neoxider/Samples` instead. Package Manager imports the **Demo Scenes** sample into
`Assets/Samples/NeoxiderTools/<version>/Demo Scenes/`; open the copied scene there.

## Main pieces

- **AMSettings** — Singleton for global sound settings (volume, mute, mixer).
- **[AM](AM.md)** — Main singleton for sound effects and music (Scripts/Audio/AudioSimple/AM.cs). Sounds and music share one record contract: an optional id, a **set** of clips (random pick), a volume multiplier and an optional pitch range. Music entries are **pools** with a `Loop` / `Shuffle` mode, and music changes **crossfade** by default. Sound entries play on the **Sfx** or **UI** channel (own volume, mute, mixer group); `TryPlay` / placeholder entries keep optional cues silent; `PlayAt` plays pooled positional one-shots.
- **PlayAudio** — Component to play a sound from AM (by id, by index, or from a clip list).
- **PlayAudioBtn** — Plays sound on UI Button click (by id, by index, or from a clip list).
- **[MusicControl](MusicControl.md)** — No-code music: start a pool by id, move to another track of it, stop. Every method is UnityEvent-ready.
- **SettingMixer** — Set/get AudioMixer parameters (normalized 0–1 or dB) with enum presets (`Master/Music/Efx`) and `Custom` mode.
- **AudioControl** — Binds a Toggle or Slider to AMSettings (Master/Music/Efx), supports `Set(bool)` and normalized `Set(float)` in `0..1`, and has a `Custom` mode with UnityEvents; Scripts/Audio/View/AudioControl.cs.
- **[AudioFocusMute](AudioFocusMute.md)** — Silences all game audio while the application has no focus (minimized, another tab or app) and restores the volume on return; one `AudioFocusMute.Install()` at startup.
- **RandomMusicController** — Standalone helper for random track playback. Since 10.13 `AM` uses its own pool engine (crossfaded, with a Loop / Shuffle mode); this class remains for direct use.

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


## See also

- [Audio editor authoring](Editor.md) — entry lists, clip controls and id drawers.
- [Animations](../Animations/README.md)
- [UI](../UI/README.md)
