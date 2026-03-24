# AMSettings

**Что это:** синглтон настроек аудио (пространство имён `Neo.Audio`, файл `Scripts/Audio/AMSettings.cs`). Привязка к AudioMixer, имена параметров громкости (Master, Music, Efx), события mute. Используется [AM](AM.md) и [AudioControl](AudioControl.md).

**Как использовать:** добавить на сцену один объект с AMSettings (Neoxider → Audio → AMSettings). Задать **Audio Mixer** и имена параметров. [AudioControl](AudioControl.md) на слайдерах/тоглах автоматически синхронизируется с AMSettings.

---

## Поля

- **Audio Mixer** — ссылка на микшер.
- **MasterVolume**, **MusicVolume**, **EfxVolume** — имена параметров в микшере.
- **MuteEfx**, **MuteMusic**, **MuteMaster** — реактивные свойства mute (см. код / инспектор).

---

## Сохранение громкости (SaveProvider)

Громкость хранится как **float в диапазоне 0..1** через **`Neo.Save.SaveProvider`** (`GetFloat` / `SetFloat` / `HasKey`), а не напрямую через `PlayerPrefs`.

### Секция инспектора «Persist (0..1)»

| Параметр | Описание |
|----------|----------|
| **Persist Volume** | **По умолчанию включено.** При включении: чтение из `SaveProvider` выполняется в **`Init()`** наследника `Singleton<AMSettings>` (см. [Singleton](../Tools/Managers/Singleton.md)) — один раз для зарегистрированного экземпляра; в **`Start`** применяется в том числе Master к микшеру/источникам; при изменении громкости и после **`ToggleMaster`** / **`ToggleAllAudio`** значения **записываются** в активный провайдер сохранений. **Если выключить** — загрузка и запись не выполняются, используются только поля сцены (**startMusicVolume**, **startEfxVolume** и поведение микшера без подстановки из сейва). |
| **Save Key Master** | Строковый ключ для Master. Значение по умолчанию: `Neo.Audio.AMSettings.MasterVolume`. **Можно изменить** (свой префикс, несколько профилей, избежание коллизий). |
| **Save Key Music** | Ключ для музыки. По умолчанию: `Neo.Audio.AMSettings.MusicVolume`. |
| **Save Key Efx** | Ключ для эффектов (SFX). По умолчанию: `Neo.Audio.AMSettings.EfxVolume`. |

Смена ключей полезна при нескольких сборках с общим слотом сохранения или при миграции — старые ключи перестанут читаться, пока вы сами не скопируете значения.

### Когда пишется сейв

Запись через `SaveProvider` выполняется из **`SetMasterVolume`**, **`SetMusicVolume`**, **`SetEfxVolume`**, **`SetMusicMixerVolume`**, **`SetEfxMixerVolume`**, а также после **`ToggleMaster`** и **`ToggleAllAudio`**. При заглушённом Master в файл уходит последняя **нормализованная** громкость до mute (`_savedMasterVolume`), а не «ноль с микшера».

Используется вместе с **AM** и UI настроек громкости.
