# GameSettings

**Что это:** статический класс `Neo.Settings.GameSettings` — текущие значения пользовательских настроек (мышь, графика, экран, FPS, vSync), запись на диск через **`SaveProvider`**, методы **`Set…` с `SettingsPersistMode`**. Файл: `Scripts/Settings/GameSettings.cs`.

**Как использовать:**

1. Убедитесь, что в сцене создан **`GameSettingsComponent`** и выполнен **`GameSettings.Attach`** (это делает сервис в `Awake`).
2. Читайте **`GameSettings.MouseSensitivity`**, **`GraphicsPreset`**, **`ResolutionAuto`** и т.д.
3. Для изменения вызывайте **`GameSettings.SetMouseSensitivity`**, **`SetGraphicsPreset`**, **`SetResolutionIndex`** и др.; для слайдеров с debounce используйте **`SettingsPersistMode.Deferred`** и при закрытии панели — **`FlushPendingSettingsSave()`**.

---

## Методы (фрагмент)

| Метод | Назначение |
|--------|------------|
| `Attach` / `Detach` | Привязка контекста к сервису |
| `LoadState` / `SaveState` | Чтение/запись SaveProvider по правилам `persist*` на компоненте |
| `FlushPendingSettingsSave` | Сброс отложенного сейва и немедленная запись |
| `SetMouseSensitivity` | Ввод мыши; чаще `Deferred` из UI |
| `SetGraphicsPreset` / `SetQualityLevel` | Качество; ручной уровень может перевести пресет в **Custom** |
| `SetResolutionAuto` / `SetResolutionIndex` | Разрешение; **Auto** не вызывает `SetResolution` |
| `SetFramerateCap` / `SetVSync` | Производительность |
| `ResetGroup` | Сброс группы к дефолтам из инспектора сервиса |

## События

| Событие | Когда |
|---------|--------|
| `OnSettingsChanged` | После применения любого `Set*` или по завершении `LoadState` |
| `OnAfterSettingsLoaded` | После `LoadState` применил значения |

## Примеры

```csharp
GameSettings.SetMouseSensitivity(2.5f, SettingsPersistMode.Immediate);
GameSettings.SetGraphicsPreset(GraphicsPreset.High, SettingsPersistMode.Immediate);
```

## См. также

- [GameSettingsComponent.md](./GameSettingsComponent.md)
- [SettingsView.md](./SettingsView.md)
