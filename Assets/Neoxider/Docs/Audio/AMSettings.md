# AMSettings

Синглтон настроек аудио: привязка к AudioMixer (Master, Music, Efx), имена параметров громкости, события mute.

**Добавить:** Neoxider → Audio → AMSettings.

## Поля

- **Audio Mixer** — ссылка на микшер.
- **MasterVolume**, **MusicVolume**, **EfxVolume** — имена параметров в микшере.
- **OnMuteEfx**, **OnMuteMusic** — события при включении/выключении звука.

Используется вместе с **AM** и UI настроек громкости.
