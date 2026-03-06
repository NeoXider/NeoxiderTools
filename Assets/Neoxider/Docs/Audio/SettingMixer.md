# SettingMixer

**Что это:** компонент управления параметрами экспозиции в AudioMixer с enum-режимом (`Master`, `Music`, `Efx`, `Custom`). Нормализованная громкость `0–1` или дБ. Файл: в `Scripts/Audio/`.

**Как использовать:** добавить на объект, задать `audioMixer` и `parameterType`; для `Custom` указать `customParameterName`. Вызывать `SetVolume(normalized)`, `SetVolumeDb(db)` или `SetVolumeEnabled(bool)` из кода/UnityEvent.

---

## Поля

| Поле | Описание |
|------|----------|
| **parameterType** | Выбор параметра микшера через enum: `Master`, `Music`, `Efx`, `Custom`. |
| **customParameterName** | Имя параметра экспозиции в AudioMixer, используется только при `parameterType = Custom`. |
| **audioMixer** | Ссылка на AudioMixer. |

---

## Публичные методы (три режима ввода)

| Метод | Описание |
|-------|----------|
| **SetVolumeDb(float volumeDb)** | Режим дБ: значение −80…20 для `parameterName`. Удобно для слайдера в дБ или UnityEvent с одним float. |
| **SetVolumeDb(string name, float volumeDb)** | То же в дБ для произвольного параметра. Если `name` пустой — используется `parameterName`. |
| **SetVolume(float normalizedVolume)** | Режим 0–1: нормализованная громкость. Ноль гарантированно ставит mute (−80 дБ). Для слайдера: `SetVolume(slider.value)`. |
| **SetVolumeEnabled(bool enabled)** | Режим bool: `true` — полная громкость, `false` — mute. Для чекбокта/переключателя. |
| **GetVolume()** | Возвращает текущую нормализованную громкость (0–1). Для NeoCondition: Property = GetVolume, сравнение с порогом. |
| **Set(MixerParameterType type, float normalized)** | Устанавливает громкость `0..1` для выбранного enum-типа. |
| **Set(MixerParameterType type, bool enabled)** | Вкл/выкл для выбранного enum-типа. |
| **SetCustom(string parameterName, float normalized)** | Устанавливает громкость `0..1` для произвольного custom-параметра. |
| **SetCustom(string parameterName, bool enabled)** | Вкл/выкл для произвольного custom-параметра. |

---

## Примеры

**Из кода / UnityEvent (слайдер):**
- Один компонент SettingMixer с `parameterType = Music`.
- В событии слайдера: `SettingMixer.SetVolume(slider.value)`.

**NeoCondition (проверка громкости):**
- Source = Component → SettingMixer.
- Property = **GetVolume** (без аргумента).
- Compare ≥ 0.5 — условие «громкость не меньше половины».

**Несколько групп:**
- Три объекта с SettingMixer: `parameterType` = `Master`, `Music`, `Efx` — каждый управляет своей группой через `SetVolume(float)`.

---

## См. также

- [AMSettings](./AMSettings.md) — настройки музыки/эффектов, ToggleAudio по группе, микшер.
- [Audio README](./README.md) — обзор аудио-модуля.
