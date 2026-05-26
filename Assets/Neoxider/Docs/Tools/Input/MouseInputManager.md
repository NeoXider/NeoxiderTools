# MouseInputManager

**Что это:** singleton-компонент для мышиного ввода без лишних аллокаций в кадре. Он собирает `Press`, `Hold`, `Release`, `Click`, делает 2D/3D raycast по слоям и сохраняет последнее событие в `MouseEventData`.

Файл: `Assets/Neoxider/Scripts/Tools/Input/MouseInputManager.cs`

## Принцип модуля

`MouseInputManager` можно использовать как scene component или как автоматически создаваемый runtime singleton. Для production-сцен лучше явно назначать `targetCamera` в Inspector или через `SetTargetCamera(Camera)`. `Camera.main` остается только отключаемым fallback и не дергается каждый кадр без retry interval.

## Настройка

1. Добавьте `MouseInputManager` на сцену один раз или позвольте bootstrap создать его автоматически.
2. Назначьте `targetCamera` явно. Для простых сцен можно оставить `useMainCameraFallback`.
3. Настройте `interactableLayers` и `fallbackDepth`.
4. Включите нужные режимы: `enablePress`, `enableHold`, `enableRelease`, `enableClick`.
5. Подпишитесь на события или используйте `LastEventData` / `HasEventData` для polling.

## Camera Binding

| Поле/API | Назначение |
|----------|------------|
| `targetCamera` | Камера для `ScreenPointToRay` и `ScreenToWorldPoint`. |
| `useMainCameraFallback` | Разрешает поиск `Camera.main`, если явная камера не задана. |
| `cameraFallbackRetryInterval` | Интервал между попытками `Camera.main`, пока камера отсутствует. |
| `logMissingCamera` | Разрешает warning через `NeoDiagnostics`; глобальный diagnostics gate все равно контролирует вывод. |
| `SetTargetCamera(Camera)` | Явная injection-точка из C# или scene setup. |
| `TargetCamera` | Текущая активная ссылка. |

## События

- `OnPress`, `OnHold`, `OnRelease`, `OnClick`
- `OnPressIn`, `OnHoldIn`, `OnReleaseIn`, `OnClickIn`

`MouseEventData` содержит `ScreenPosition`, `WorldPosition`, `HitObject`, `Hit3D`, `Hit2D`.

## Жизненный цикл

- `MouseInputManagerSubsystemRegistration` перед загрузкой сцены включает `CreateInstance = true`.
- При subsystem/domain reload очищаются `LastEventData` и `HasEventData`.
- Runtime singleton cache очищается общим `SingletonRuntimeReset`.
- Встроенной блокировки ввода поверх UI через `EventSystem` нет; если она нужна, добавляйте отдельный фильтр поверх событий.

## См. также

- [MouseEffect](./MouseEffect.md)
- [README](./README.md)
