# SettingMixer

Компонент для управления **одним** параметром экспозиции в `AudioMixer`. Задаётся имя параметра строкой (`parameterName`), громкость выставляется нормализованным значением 0–1.

Низкоуровневый вариант по сравнению с `AMSettings`: удобен, когда нужен один конкретный параметр микшера (Master, Music, Sfx и т.д.). Для нескольких групп можно добавить несколько компонентов SettingMixer с разными `parameterName`.

---

## Поля

| Поле | Описание |
|------|----------|
| **parameterName** | Имя параметра экспозиции в AudioMixer (например `MasterVolume`, `MusicVolume`, `EfxVolume`). По умолчанию `MasterVolume`. |
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

---

## Примеры

**Из кода / UnityEvent (слайдер):**
- Один компонент SettingMixer с `parameterName = "MusicVolume"`.
- В событии слайдера: `SettingMixer.SetVolume(slider.value)`.

**NeoCondition (проверка громкости):**
- Source = Component → SettingMixer.
- Property = **GetVolume** (без аргумента).
- Compare ≥ 0.5 — условие «громкость не меньше половины».

**Несколько групп:**
- Три объекта с SettingMixer: `parameterName` = `MasterVolume`, `MusicVolume`, `EfxVolume` — каждый управляет своей группой через `SetVolume(float)`.

---

## См. также

- [AMSettings](./AMSettings.md) — настройки музыки/эффектов, ToggleAudio по группе, микшер.
- [Audio README](./README.md) — обзор аудио-модуля.
