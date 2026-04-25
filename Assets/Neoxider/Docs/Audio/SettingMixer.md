# SettingMixer

**Назначение:** Утилита для прямого управления параметрами `AudioMixer` через Unity Events (например, из обычных UI слайдеров). Автоматически переводит нормализованную громкость (0-1) в децибелы (-80 до 20 dB).

## Подключение

1. Добавьте компонент `Add Component > Neoxider > Audio > SettingMixer`.
2. Настройте тип параметра (`Master`, `Music`, `Efx`, или `Custom`).
3. Привяжите `Slider.OnValueChanged(float)` к методу `SettingMixer.Set(float)`.

## Основные настройки (Inspector)

| Поле | Описание |
|------|----------|
| `parameterType` | Тип встроенного канала (`Master`, `Music`, `Efx`) или `Custom` для своего параметра. |
| `customParameterName` | Имя параметра в микшере (используется, если `parameterType` = `Custom`). |
| `audioMixer` | Ссылка на микшер, которым нужно управлять. |

## См. также
- [AudioControl](View\AudioControl.md) - Более умный компонент, который сам находит Slider и сам синхронизируется.
- [Корень модуля](../README.md)
