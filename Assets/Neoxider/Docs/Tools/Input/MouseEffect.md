# MouseEffect

`MouseEffect` - компонент визуальных эффектов курсора, который подписывается на события `MouseInputManager`: press, hold, release и click. Он не содержит правил ввода сам по себе, а работает как удобная сценовая обертка для трейла, follow-объекта и спавна prefab.

## Возможности

- включает и ведет `TrailRenderer` за курсором;
- перемещает отдельный `followObject` в позицию курсора;
- спавнит `spawnPrefab` на `Press`, `Hold`, `Release` или `Click`;
- поддерживает одноразовый и периодический spawn во время удержания;
- отдает события `onStartFollow`, `onStopFollow`, `onSpawn`.

## Настройка

1. Добавьте на сцену `MouseInputManager`.
2. Добавьте `MouseEffect` на объект с эффектом.
3. Назначьте `trail`, `followObject` и/или `spawnPrefab`.
4. При необходимости задайте `Target Camera` явно через инспектор или `SetTargetCamera(Camera)`.

## Камера

Для перевода позиции курсора из screen space в world space используется такой порядок:

1. `Target Camera`, заданная прямо в `MouseEffect`;
2. `MouseInputManager.TargetCamera`;
3. `Camera.main`, если включен `Use Main Camera Fallback`.

`Camera.main` не ищется каждый кадр: повторные попытки ограничены `Camera Fallback Retry Interval`. Предупреждение об отсутствующей камере выключено по умолчанию и включается через `_logMissingCameraWarning`, чтобы runtime не спамил консоль.

## Основные Поля

| Поле | Назначение |
| --- | --- |
| `interactable` | Если выключено, компонент игнорирует события. |
| `disableOnRelease` | Отключать trail/follow object при отпускании. |
| `trail` | `TrailRenderer`, который ведется за курсором. |
| `followObject` | Объект, который перемещается за курсором. |
| `spawnPrefab` | Prefab для спавна по событию. |
| `spawnTrigger` | Событие спавна: `Press`, `Hold`, `Release`, `Click`. |
| `spawnDuringHold` | Спавнить повторно во время удержания. |
| `holdInterval` | Интервал повторного спавна при удержании. |
| `spawnLifetime` | Автоудаление spawned prefab. `0` - не удалять. |
| `followInterval` | Интервал обновления позиции trail/follow object. |
| `followDepth` | Z-depth для `ScreenToWorldPoint`. |
| `spawnParent` | Родитель spawned объектов. Если пусто, используется `transform`. |

## API

```csharp
public Camera TargetCamera { get; }
public void SetTargetCamera(Camera camera);
```

Используйте `SetTargetCamera` в scene setup или bootstrap-коде, если камера создается динамически. Это лучше, чем полагаться на `Camera.main`.

## Зависимости

`MouseEffect` требует активный `MouseInputManager` на сцене. Если менеджера нет, эффект не подписывается на события. Warning об этом выключен по умолчанию и включается через `_logMissingManagerWarning`.
