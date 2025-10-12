# Компонент AiNavigation

## 1. Введение

`AiNavigation` — это продвинутый компонент для управления навигацией AI, построенный на основе `NavMeshAgent` от Unity. Он упрощает настройку поведения AI, такого как следование за целью, патрулирование, и автоматически управляет анимацией движения.

Компонент требует, чтобы на том же объекте были `NavMeshAgent` и `Animator`.

---

## 2. Описание класса

### AiNavigation
- **Пространство имен**: `Neo.Tools`
- **Путь к файлу**: `Assets/Neoxider/Scripts/Tools/Other/AiNavigation.cs`

**Описание**
Этот компонент предоставляет полный набор инструментов для управления движением, обновлением пути и анимацией AI-агента. Он может следовать за целью (`Transform`) или двигаться к указанной точке (`Vector3`).

### Ключевые настройки в инспекторе

#### Navigation Settings
- `target` (`Transform`): Цель, за которой будет следовать агент.
- `triggerDistance` (`float`): Дистанция, при превышении которой агент остановится. Если 0, агент будет двигаться всегда, пока есть цель.
- `stoppingDistance` (`float`): Как близко к цели агент должен подойти, чтобы остановиться.

#### Movement Settings
- `baseSpeed` (`float`): Базовая скорость движения.
- `sprintSpeedMultiplier` (`float`): Множитель скорости в режиме спринта.
- `acceleration` (`float`): Ускорение агента.
- `turnSpeed` (`float`): Скорость поворота.

#### Path Settings
- `autoUpdatePath` (`bool`): Если `true`, путь к цели будет автоматически обновляться с заданным интервалом.
- `pathUpdateInterval` (`float`): Интервал в секундах для обновления пути.

#### Animation Settings
- `speedParameterName` (`string`): Имя параметра в `Animator`, отвечающего за скорость (тип `float`).
- `isMovingParameterName` (`string`): Имя параметра в `Animator`, отвечающего за состояние движения (тип `bool`).

### Публичные свойства (Public Properties)
- `HasReachedDestination` (`bool`): Возвращает `true`, если агент достиг точки назначения.
- `IsPathBlocked` (`bool`): Возвращает `true`, если путь агента заблокирован.
- `CurrentSpeed` (`float`): Текущая скорость агента.
- `RemainingDistance` (`float`): Оставшееся расстояние до цели.
- `StoppingDistance` (`float`): Позволяет получить или задать дистанцию остановки агента.
- `BaseSpeed` (`float`): Позволяет получить или задать базовую скорость агента.
- `Acceleration` (`float`): Позволяет получить или задать ускорение агента.
- `TurnSpeed` (`float`): Позволяет получить или задать скорость поворота агента.
- `TriggerDistance` (`float`): Позволяет получить или задать дистанцию, на которой агент начинает движение к цели.
- `AutoUpdatePath` (`bool`): Позволяет включить или выключить автоматическое обновление пути.

### Публичные методы (Public Methods)
- `SetTarget(Transform newTarget)`: Устанавливает новую цель для следования.
- `SetDestination(Vector3 destination)`: Устанавливает точку назначения. Возвращает `true`, если точка доступна на `NavMesh`.
- `SetSpeedMultiplier(float multiplier)`: Устанавливает множитель для `baseSpeed`.
- `SetAbsoluteSpeed(float newSpeed)`: Устанавливает абсолютную скорость агента, игнорируя `baseSpeed` и множители.
- `EnableSprint(bool enable)`: Включает или выключает режим спринта (используя `sprintSpeedMultiplier`).
- `Stop()`: Немедленно останавливает агента.
- `Resume()`: Возобновляет движение агента.
- `WarpToPosition(Vector3 position)`: Мгновенно перемещает агента в указанную точку (если она доступна).
- `IsPositionReachable(Vector3 position)`: Проверяет, можно ли построить путь до указанной точки.

### Unity Events
- `OnDestinationReached`: Вызывается, когда агент достигает цели. Передает `Vector3` — позицию цели.
- `OnPathBlocked`: Вызывается, когда путь к цели заблокирован. Передает `Vector3` — позицию цели.
- `OnSpeedChanged`: Вызывается при изменении скорости агента. Передает `float` — новую скорость.
- `OnPathUpdated`: Вызывается при обновлении пути. Передает `Vector3` — новую позицию цели.
- `OnPathStatusChanged`: Вызывается при изменении статуса пути (`NavMeshPathStatus`).
