# AMSettings

**Что это:** синглтон настроек аудио (пространство имён `Neo.Audio`, файл `Scripts/Audio/AMSettings.cs`). Привязка к AudioMixer, имена параметров громкости (Master, Music, Efx), события mute. Используется [AM](AM.md) и [AudioControl](AudioControl.md).

**Как использовать:** добавить на сцену один объект с AMSettings (Neoxider → Audio → AMSettings). Задать **Audio Mixer** и имена параметров. [AudioControl](AudioControl.md) на слайдерах/тоглах автоматически синхронизируется с AMSettings.

---

## Поля

- **Audio Mixer** — ссылка на микшер.
- **MasterVolume**, **MusicVolume**, **EfxVolume** — имена параметров в микшере.
- **OnMuteEfx**, **OnMuteMusic** — события при включении/выключении звука.

Используется вместе с **AM** и UI настроек громкости.
