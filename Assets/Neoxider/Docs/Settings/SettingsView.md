# SettingsView

**Что это:** `Neo.Settings.SettingsView` — MonoBehaviour для привязки **Unity UI** (`Slider`, `Toggle`, `Dropdown`) к **`GameSettings`**. Локализация подписей — через **`ISettingsLocalization`** и ключи наподобие `settings.preset_high`. Файл: `Scripts/Settings/SettingsView.cs`.

**Как использовать:**

1. Разместите панель настроек и добавьте компонент **SettingsView** (меню **Neoxider/Settings/Settings View Panel** или **Add Component**).
2. Назначьте ссылки на виджеты в инспекторе; при необходимости задайте **`Resolution Block Root`** для скрытия блока на WebGL/консолях.
3. Реализуйте **`ISettingsLocalization`** в игре и вызовите **`GameSettings.SetLocalizationProvider`** при старте (или передайте провайдер позже).

---

## Поля

| Поле | Назначение |
|------|------------|
| Commit mode | **DebouncedLive**: слайдер мыши → `Deferred`; остальное → `Immediate` |
| Resolution block root | Корень UI, скрываемый на ограниченных платформах |
| Sliders / Dropdowns / Toggles | Стандартные **UnityEngine.UI** элементы |
| Reset buttons | Опционально: сброс групп **Graphics / Input / Display / Performance** |

## Поведение

- При **`OnEnable`**: **`BindAll`**, **`RefreshFromSettings`**, подписка на **`OnAfterSettingsLoaded`**.
- При **`OnDisable`**: **`FlushPendingSettingsSave`**.
- Ключи локализации по умолчанию возвращаются как есть, если провайдер **null**.

## См. также

- [GameSettings.md](./GameSettings.md)


## Дополнительные поля

| Поле | Описание |
|------|----------|
| `SettingsViewCommitMode` | Settings View Commit Mode. |
| `_commitMode` | Commit Mode. |
| `_framerateDropdown` | Framerate Dropdown. |
| `_fullScreenToggle` | Full Screen Toggle. |
| `_graphicsPresetDropdown` | Graphics Preset Dropdown. |
| `_mouseSensitivitySlider` | Mouse Sensitivity Slider. |
| `_qualityDropdown` | Quality Dropdown. |
| `_resetDisplayButton` | Reset Display Button. |
| `_resetGraphicsButton` | Reset Graphics Button. |
| `_resetInputButton` | Reset Input Button. |
| `_resetPerformanceButton` | Reset Performance Button. |
| `_resolutionBlockRoot` | Resolution Block Root. |
| `_resolutionDropdown` | Resolution Dropdown. |
| `_vSyncToggle` | V Sync Toggle. |