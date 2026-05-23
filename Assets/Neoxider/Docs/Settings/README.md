# Модуль Settings

**Что это:** runtime-модуль **Neo.Settings** — статический API **`GameSettings`**, синглтон **`GameSettingsComponent`** (сейв через **Neo.Save.SaveProvider**, пресеты качества `QualitySettings`), и UI-биндер **`SettingsView`**. Скрипты: `Assets/Neoxider/Scripts/Settings/`.

**Оглавление:**

| Документ | Описание |
|----------|----------|
| [GameSettings.md](./GameSettings.md) | Статические свойства, `Set…`, Load/Save/Flush |
| [GameSettingsComponent.md](./GameSettingsComponent.md) | Сервис в сцене: префикс ключей, пресеты, debounce |
| [SettingsView.md](./SettingsView.md) | Привязка UI к `GameSettings` |
| [GraphicsPreset.md](./GraphicsPreset.md) | Именованные уровни графики |

**Как использовать:**

1. Добавьте **Game Settings Service** (`GameObject → Neoxider/Settings/Game Settings Service`) в первую загрузочную сцену; при необходимости включите **Dont Destroy On Load** на `Singleton`.
2. Читайте настройки из **`GameSettings`** (свойства); меняйте через **`GameSettings.Set…(…, SettingsPersistMode)`** из кода или через **`SettingsView`** / обёртки на компоненте.
3. Звук и громкости остаются в **`AMSettings`** — не дублируйте их в этом модуле.

**Требования:** модуль опирается на **`QualitySettings`** и не требует URP. Если проект использует URP-специфичные настройки, добавьте `com.unity.render-pipelines.universal` в свой проект отдельно.

---

## См. также

- [Save/README.md](../Save/README.md) — `SaveProvider`
- [Audio/AMSettings.md](../Audio/AMSettings.md) — громкости
