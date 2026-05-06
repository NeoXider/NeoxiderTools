# GameSettingsComponent

**Что это:** `Neo.Settings.GameSettingsComponent` — **`Singleton<MonoBehaviour>`**, точка входа Unity: префикс ключей **`SaveProvider`**, таблица **пресет → QualitySettings**, списки разрешений и FPS, debounce для **`SettingsPersistMode.Deferred`**, дефолты для **`ResetGroup`**. Файл: `Scripts/Settings/GameSettingsComponent.cs`.

**Как использовать:**

1. Создайте объект через **Neoxider/Settings/Game Settings Service**.
2. Настройте **маппинг пресетов**, при необходимости **кастомный список разрешений** и массив **Framerate Cap Presets** (первое значение **-1** = без лимита).
3. Включите флаги **Persist** по группам (аудио в модуль не входит).

---

## Поля (группы)

| Группа | Назначение |
|--------|------------|
| Save keys | Префикс для всех ключей |
| Persist | Запись Input / Graphics / Display / Performance |
| Graphics preset → Quality | Соответствие **Minimal…Maximum** индексам качества |
| Framerate cap presets | Значения `Application.targetFrameRate` для dropdown |
| Resolution | Опционально фиксированный список **Width×Height**; иначе — из `Screen.resolutions` |
| Debounce | Задержка отложенного `SaveState` |
| Default values | Значения для **ResetGroup** |

## Методы

| Метод | Назначение |
|--------|------------|
| `ReloadFromDisk` | `GameSettings.LoadState()` |
| `SaveNow` | `GameSettings.SaveState()` |
| `SetMouseSensitivityForMenu` | `Set…(Deferred)` |
| `SetMouseSensitivityForMenuImmediate` | `Set…(Immediate)` |

## См. также

- [GameSettings.md](./GameSettings.md)


## Дополнительные поля

| Поле | Описание |
|------|----------|
| `2f` | 2f. |
| `QualityLevelIndex` | Quality Level Index. |
| `_customResolutionPresets` | Custom Resolution Presets. |
| `_defaultCustomQualityLevel` | Default Custom Quality Level. |
| `_defaultFramerateCap` | Default Framerate Cap. |
| `_defaultFullScreenMode` | Default Full Screen Mode. |
| `_defaultGraphicsPreset` | Default Graphics Preset. |
| `_defaultResolutionIndex` | Default Resolution Index. |
| `_defaultVSync` | Default VSync. |
| `_deferredSaveDelaySeconds` | Deferred Save Delay Seconds. |
| `_framerateCapPresets` | Framerate Cap Presets. |
| `_saveKeyPrefix` | Save Key Prefix. |
| `true` | True. |