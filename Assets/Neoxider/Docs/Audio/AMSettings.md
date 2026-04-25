# AMSettings

**Назначение:** Глобальный менеджер настроек аудио (Singleton). Контролирует `AudioMixer`, управляет громкостью каналов (Master, Music, Efx) и автоматически сохраняет/загружает настройки между сессиями (через `SaveProvider`). Также предоставляет реактивные свойства (состояния Mute) для привязки к UI.

## Подключение

1. Добавьте `Add Component > Neoxider > Audio > AMSettings` на глобальный объект (рядом с `AM`).
2. Назначьте `AudioMixer` проекта (если он используется) в поле `audioMixer`.
3. Убедитесь, что параметры (MasterVolume, MusicVolume, EfxVolume) выставлены в микшере (Exposed Parameters).

## Основные настройки (Inspector)

| Поле | Описание |
|------|----------|
| `audioMixer` | Главный `AudioMixer` проекта. Если его нет, громкость меняется напрямую у `AudioSource` внутри `AM`. |
| `MasterVolume`, `MusicVolume`, `EfxVolume` | Имена параметров (Exposed) в AudioMixer (по умолчанию MasterVolume и т.д.). |
| `persistVolume` | Если `true`, настройки звука автоматически сохраняются. |
| `saveKeyMaster`, `saveKeyMusic`, `saveKeyEfx` | Ключи для сохранения в `SaveProvider`. |
| `startEfxVolume`, `startMusicVolume` | Громкость по умолчанию (от 0 до 1), если сохранений еще нет. |

## API 

```csharp
// Выключить музыку
AMSettings.I.SetMusic(false);

// Установить общую громкость на 80%
AMSettings.I.SetMasterVolume(0.8f);

// Получить текущее состояние (включен ли звук) для UI
bool isEfxMuted = AMSettings.I.MuteEfx.Value;
```

## См. также
- [AM](AM.md) - Воспроизведение звуков.
- [AudioControl](View\AudioControl.md) - Готовый UI-компонент для чекбоксов и ползунков.
- [Корень модуля](../README.md)
