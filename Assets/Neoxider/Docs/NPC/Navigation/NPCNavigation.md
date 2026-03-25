# NpcNavigation

**Что это:** Компонент Unity (`MonoBehaviour`) для навигации NPC поверх `NavMeshAgent`: Follow / Patrol / Combined, переключение поведения без дополнительных `MonoBehaviour` на объекте (стратегии в pure C#). Файл: `Assets/Neoxider/Scripts/NPC/NpcNavigation.cs`.

**Как использовать:**
1. Добавьте на NPC `NavMeshAgent` и `NpcNavigation`.
2. Выберите режим **Mode**: `FollowTarget`, `Patrol` или `Combined`.
3. Настройте соответствующие поля (цель, патрульные точки/зону, агро-дистанции).
4. Чтобы включать/выключать бота в рантайме, используйте **IsActive**: `true` — ходит, `false` — стоит.

---

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

## IsActive

- **IsActive** (Inspector / свойство `IsActive`) — глобальный переключатель. Если `false`, компонент каждый кадр останавливает агент (`agent.isStopped = true`) и не тикает поведение.
- `Stop()` — выключает `IsActive` и останавливает агент.
- `Resume()` — включает `IsActive` и снимает `agent.isStopped`.

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

## Анимации

Чтобы аниматор NPC автоматически реагировал на движение (ходьба/бег по скорости агента), добавьте на тот же объект компонент **[NpcAnimatorDriver](../NpcAnimatorDriver.md)** и укажите Animator и имена параметров (по умолчанию `Speed`, `IsMoving`).

## Интеграция с RPG NPC

Если нужен автоматический боевой NPC, не расширяйте `NpcNavigation` отдельным большим custom-скриптом. Рекомендуемый путь:

- движение оставить в `NpcNavigation`
- таргетинг вынести в `RpgTargetSelector`
- атаку исполнять через `RpgAttackController`
- orchestration держать в **[NpcRpgCombatBrain](../Combat/NpcRpgCombatBrain.md)**

Так навигация остаётся изолированным модулем и не превращается в боевой монолит.

## Совместимость с AiNavigation

`AiNavigation` остаётся для старых проектов, но помечен как устаревший. Для новых проектов используйте `NpcNavigation`.














