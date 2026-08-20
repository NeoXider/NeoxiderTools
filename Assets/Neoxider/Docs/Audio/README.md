# Audio module

The **Audio** module provides tools for sound management in Unity: a central audio manager, volume/mute settings via AudioMixer, and simple play-on-event components.

The system is built around singleton **AM** (Audio Manager) for playback and singleton **AMSettings** for global volume and mute via **AudioMixer**.

Demo: `Samples/Demo/Scenes/Audio/AudioDemo.unity` — runtime-built UI via `NeoDemoShell`, controller `Samples/Demo/Scripts/Shell/AudioDemoController.cs`.

## Main pieces

- **AMSettings** — Singleton for global sound settings (volume, mute, mixer).
- **[AM](AM.md)** — Main singleton for sound effects and music (Scripts/Audio/AudioSimple/AM.cs). Sounds and music share one record contract: an optional id, a **set** of clips (random pick), a volume multiplier and an optional pitch range. Music entries are **pools** with a `Loop` / `Shuffle` mode, and music changes **crossfade** by default.
- **PlayAudio** — Component to play a sound from AM (by id, by index, or from a clip list).
- **PlayAudioBtn** — Plays sound on UI Button click (by id, by index, or from a clip list).
- **[MusicControl](MusicControl.md)** — No-code music: start a pool by id, move to another track of it, stop. Every method is UnityEvent-ready.
- **SettingMixer** — Set/get AudioMixer parameters (normalized 0–1 or dB) with enum presets (`Master/Music/Efx`) and `Custom` mode.
- **AudioControl** — Binds a Toggle or Slider to AMSettings (Master/Music/Efx), supports `Set(bool)` and normalized `Set(float)` in `0..1`, and has a `Custom` mode with UnityEvents; Scripts/Audio/View/AudioControl.cs.
- **RandomMusicController** — Standalone helper for random track playback. Since 10.13 `AM` uses its own pool engine (crossfaded, with a Loop / Shuffle mode); this class remains for direct use.


## See also

- [Animations](../Animations/README.md)
- [UI](../UI/README.md)
