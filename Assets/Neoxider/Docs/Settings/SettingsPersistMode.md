# SettingsPersistMode

**Назначение:** Enum, определяющий способ записи изменения настройки в `SaveProvider`.

## Значения

| Значение | Описание |
|----------|----------|
| `Immediate` | Сохранить сразу (по правилам группы). |
| `Deferred` | Отложить запись до истечения debounce (для слайдеров). |
| `SkipUntilFlush` | Не сохранять до ручного вызова `FlushPendingSettingsSave()`. |

## См. также
- [GameSettingsComponent](GameSettingsComponent.md)
- ← [Settings](README.md)
