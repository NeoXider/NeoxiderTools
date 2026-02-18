# NPC Navigation

`NpcNavigation` — единый компонент навигации NPC. Он предоставляет режимы (Follow/Patrol/Combined) и переключает поведение через pure C# стратегии (интерфейс + реализации), без дополнительных `MonoBehaviour` на объекте.

Основная логика реализована в pure C# (без корутин), `MonoBehaviour` слой — настройки, UnityEvent и визуализация.

## Быстрый старт

1. На объект NPC добавьте:
   - `NavMeshAgent`
   - `Neoxider/NPC/NpcNavigation`
2. Выберите режим в инспекторе `NpcNavigation`:
   - `FollowTarget`
   - `Patrol`
   - `Combined`
3. Заполните настройки цели/патруля/агро в `NpcNavigation`.

## Режимы

### FollowTarget

Следование за `followTarget` (с поддержкой `triggerDistance` и ограничивающей зоны `followMovementBounds`).

### Patrol

Патруль по точкам `patrolPoints` или по зоне `patrolZone` (если задана, точки игнорируются).

### Combined

Патрулирует (points/zone), но при приближении к `combinedTarget` на расстояние `aggroDistance` переключается на преследование.\nПри удалении на расстояние больше `maxFollowDistance` возвращается к патрулю (0 = не возвращаться).

## Failover (недоступная точка)

Если целевая точка не попадает на NavMesh, используется **ring-search** (поиск по кольцам вокруг точки). Если точка всё равно недоступна:

- в **Patrol/Combined (в режиме патруля)** по умолчанию NPC автоматически пытается перейти к следующей точке (или выбрать другую случайную точку в `patrolZone`), чтобы не застревать;
- в **Combined (в режиме преследования)** при недоступной цели NPC возвращается к патрулю.

## API NpcNavigation

Основные методы:

- `SetMode(NavigationMode)`
- `SetFollowTarget(Transform)`
- `SetCombinedTarget(Transform)`
- `SetDestination(Vector3)`
- `SetRunning(bool)`
- `SetSpeed(float)`
- `Stop()`, `Resume()`

Также `NpcNavigation` поднимает общие события:

- `onMovementStarted`, `onMovementStopped`
- `onDestinationReached`, `onPathBlocked`, `onPathUpdated`, `onPathStatusChanged`
- `onPatrolStarted`, `onPatrolPointReached`, `onPatrolCompleted`
- `onStartFollowing`, `onStopFollowing`
- `onSpeedChanged`

## Debug

- `drawGizmos` / `drawPathGizmos`: визуализация пути, радиусов, зон (bounds / patrolZone) через Gizmos.
- `debugMode`: лог решений (например, старт/стоп преследования, unreachable и т.п.) и поле `lastDecision`.

## События (UnityEvent)

`NpcNavigation` поднимает общие события:

- `onMovementStarted`, `onMovementStopped`
- `onDestinationReached`, `onPathBlocked`, `onPathUpdated`, `onPathStatusChanged`
- `onPatrolStarted`, `onPatrolPointReached`, `onPatrolCompleted`
- `onStartFollowing`, `onStopFollowing`
- `onSpeedChanged`

## Совместимость с AiNavigation

`AiNavigation` остаётся для старых проектов, но помечен как устаревший. Для новых проектов используйте `NpcNavigation`.














