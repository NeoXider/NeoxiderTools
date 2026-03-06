# Audio module

The **Audio** module provides tools for sound management in Unity: a central audio manager, volume/mute settings via AudioMixer, and simple play-on-event components.

The system is built around singleton **AM** (Audio Manager) for playback and singleton **AMSettings** for global volume and mute via **AudioMixer**.

## Scripts

- **[AMSettings](AMSettings.md)** — Singleton for global sound settings (volume, mute, mixer).
- **[AM](AM.md)** — Main singleton for sound effects and music.
- **[PlayAudio](PlayAudio.md)** — Component to play a sound from AM (by ID or clip list).
- **[PlayAudioBtn](PlayAudioBtn.md)** — Plays sound on UI Button click.
- **[SettingMixer](SettingMixer.md)** — Set/get a single AudioMixer parameter (normalized 0–1 or dB).
- **[AudioControl](View/AudioControl.md)** — Binds a Toggle or Slider to AMSettings (Master/Music/Efx).
- **RandomMusicController** — Used internally by AM for random track playback.

## See also

- [Animations](../Animations/README.md)
- [UI](../UI/README.md)
