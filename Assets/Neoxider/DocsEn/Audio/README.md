# Audio module

The **Audio** module provides tools for sound management in Unity: a central audio manager, volume/mute settings via AudioMixer, and simple play-on-event components.

The system is built around singleton **AM** (Audio Manager) for playback and singleton **AMSettings** for global volume and mute via **AudioMixer**.

## Main pieces

- **AMSettings** — Singleton for global sound settings (volume, mute, mixer).
- **AM** — Main singleton for sound effects and music (Scripts/Audio/AudioSimple/AM.cs).
- **PlayAudio** — Component to play a sound from AM (by ID or clip list).
- **PlayAudioBtn** — Plays sound on UI Button click.
- **SettingMixer** — Set/get a single AudioMixer parameter (normalized 0–1 or dB).
- **AudioControl** — Binds a Toggle or Slider to AMSettings (Master/Music/Efx); Scripts/Audio/View/AudioControl.cs.
- **RandomMusicController** — Used internally by AM for random track playback.

For per-script pages (fields, API, examples), see the Russian documentation: [Docs/Audio](../../Docs/Audio/README.md).

## See also

- [Animations](../Animations/README.md)
- [UI](../UI/README.md)
