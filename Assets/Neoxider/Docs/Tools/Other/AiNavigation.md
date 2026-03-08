# Компонент AI Navigation

**Что это:** Следование за целью. Классический режим преследования.

**Как использовать:** см. разделы ниже.

---


## 1. Введение

`AiNavigation` — компонент для управления навигацией AI с поддержкой патрулирования, следования за целью и комбинированного режима. Построен на Unity NavMeshAgent с автоматическим управлением анимацией.

**Статус:** `deprecated`.

**Замена:** [`NpcNavigation`](../../NPC/Navigation/NPCNavigation.md).

> Важно: `AiNavigation` помечен как устаревший и скрыт из меню добавления компонентов. Для новых проектов используйте модульную систему [`NpcNavigation`](../../NPC/Navigation/NPCNavigation.md) (`NpcNavigation` + модули).

**Требования**: `NavMeshAgent` на том же объекте. `Animator` опционален.

---

## 2. Режимы движения (MovementMode)

### FollowTarget (по умолчанию)
Следование за целью. Классический режим преследования.

### Patrol
Патрулирование по заданным точкам с остановками и зацикливанием.

### Combined
Комбинированный режим: патрулирует, но при приближении цели переключается на преследование.

**Логика Combined режима:**
1. Патрулирует по точкам, останавливаясь на `stoppingDistance` от каждой
2. Каждый кадр проверяет расстояние до `initialTarget` (установленного в инспекторе)
3. При цели ≤ `aggroDistance` → АГРО → преследование (onStartFollowing)
4. При цели > `maxFollowDistance` → ДЕАГРО → останавливается, ждёт `patrolWaitTime`, продолжает патруль (onStopFollowing)
5. Продолжает патруль с текущей точки, не сбрасывая прогресс

**Важно для Combined:**
- Установите `target` в инспекторе (например, Player)
- `aggroDistance` обязательно > 0 (по умолчанию 10м)
- `maxFollowDistance` > `aggroDistance` или 0 (0 = преследует бесконечно)

---

## 3. Настройки

### Movement Mode
- `movementMode`: Режим движения (FollowTarget / Patrol / Combined)

### Follow Target Settings
- `target`: Цель для следования (**обязательно для Combined режима**)
- `triggerDistance`: Минимальная дистанция для начала движения (0 = всегда двигается)
- `stoppingDistance`: Дистанция остановки (по умолчанию 2м, работает везде)

### Patrol Settings
- `patrolPoints`: Массив точек патруля
- `patrolZone`: BoxCollider для случайного патрулирования (если задан, patrolPoints игнорируется)
- `patrolWaitTime`: Время ожидания на точке (по умолчанию 1 сек)
- `loopPatrol`: Зацикливание маршрута (по умолчанию true)

### Combined Mode Settings
- `aggroDistance`: Дистанция начала преследования (по умолчанию 10м)
- `maxFollowDistance`: Дистанция прекращения преследования (по умолчанию 20м, 0 = бесконечно)

### Movement Settings
- `walkSpeed`: Скорость ходьбы (по умолчанию 3 м/с)
- `runSpeed`: Скорость бега (по умолчанию 6 м/с)
- `acceleration`: Ускорение (по умолчанию 8)
- `turnSpeed`: Скорость поворота (по умолчанию 260 град/сек)

### Path Settings
- `autoUpdatePath`: Автообновление пути
- `pathUpdateInterval`: Интервал обновления (0.5 сек)

### Animation Settings
- `animator`: Аниматор (опционально)
- `speedParameter`: Параметр float для скорости (0-1, по умолчанию "Speed")
- `isMovingParameter`: Параметр bool для движения (по умолчанию "IsMoving")

### Debug
- `debugMode`: Детальное логирование для отладки

---

## 4. Публичный API

### Свойства (Properties)

#### Состояние
- `bool IsOnNavMesh` - На NavMesh ли агент
- `bool HasPath` - Есть ли валидный путь
- `bool IsMoving` - Двигается ли
- `bool IsRunning` - Бежит ли
- `bool IsPatrolling` - Патрулирует ли
- `bool UsesPatrolZone` - Использует ли зону патрулирования вместо точек
- `bool HasReachedDestination` - Достиг цели
- `bool IsPathBlocked` - Путь заблокирован

#### Информация
- `Transform Target` - Текущая цель
- `float RemainingDistance` - Оставшееся расстояние
- `float CurrentSpeed` - Текущая скорость
- `int CurrentPatrolIndex` - Индекс текущей точки патруля
- `MovementMode CurrentMode` - Текущий режим
- `NavMeshPathStatus PathStatus` - Статус пути

#### Настройки
- `float WalkSpeed`
- `float RunSpeed`
- `float StoppingDistance`
- `float Acceleration`
- `float TurnSpeed`
- `float TriggerDistance`
- `bool AutoUpdatePath`

### Методы

```csharp
// Управление целью
void SetTarget(Transform newTarget)
bool SetDestination(Vector3 destination)

// Управление движением
void SetRunning(bool enable)
void SetSpeed(float speed)
void Stop()
void Resume()
bool WarpToPosition(Vector3 position)

// Управление патрулем
void StartPatrol()
void StopPatrol()
void SetMovementMode(MovementMode mode)
void SetPatrolPoints(Transform[] points)
void SetPatrolZone(BoxCollider zone)
void ClearPatrolZone()

// Проверки
bool IsPositionReachable(Vector3 position)
NavMeshPath GetPathToPosition(Vector3 position)
```

---

## 5. События (UnityEvent)

### Основные
- `onDestinationReached<Vector3>` - Достигнута цель
- `onPathBlocked<Vector3>` - Путь заблокирован
- `onSpeedChanged<float>` - Изменена скорость
- `onPathUpdated<Vector3>` - Путь обновлён
- `onPathStatusChanged<NavMeshPathStatus>` - Статус пути

### Патруль
- `onPatrolPointReached<int>` - Достигнута точка (индекс)
- `onPatrolStarted` - Патруль начат
- `onPatrolCompleted` - Патруль завершён

### Combined режим
- `onStartFollowing` - Начал преследование (агро)
- `onStopFollowing` - Прекратил преследование (деагро)

---

## 6. Визуализация Gizmos

### Всегда (OnDrawGizmos)
- 🟡 Жёлтая линия - текущий путь NavMesh
- 🔴 Красная сфера - stoppingDistance
- 🔵 Синяя сфера - triggerDistance (FollowTarget)
- 🟡 Жёлтая сфера - aggroDistance (Combined)
- 🔵 Голубая сфера - maxFollowDistance (Combined)

### При выборе (OnDrawGizmosSelected)
- 🟢 Зелёные сферы - точки патруля
- 🟢 Зелёные линии - маршрут патруля
- 🟡 Жёлтая сфера - текущая точка
- 🟢 Зелёный полупрозрачный куб - зона патрулирования (patrolZone)

---

## 7. Примеры использования

### Простое следование
```csharp
// В инспекторе:
movementMode = FollowTarget
target = Player

ai.SetTarget(player);
ai.SetRunning(true);
```

### Патрулирование по точкам
```csharp
// В инспекторе:
movementMode = Patrol
patrolPoints = [Point1, Point2, Point3]
patrolWaitTime = 2f
loopPatrol = true
stoppingDistance = 2f

ai.onPatrolPointReached.AddListener(index => 
{
    Debug.Log($"Точка {index}");
});
```

### Патрулирование в зоне (BoxCollider)
```csharp
// В инспекторе:
movementMode = Patrol
patrolZone = BoxCollider на сцене
patrolWaitTime = 2f
stoppingDistance = 2f

// Агент будет выбирать случайные точки внутри BoxCollider
// и перемещаться к ним бесконечно

// Программное управление:
ai.SetPatrolZone(boxCollider);  // Установить зону
ai.ClearPatrolZone();           // Очистить (использовать точки)
```

### Combined режим (охранник)
```csharp
// В инспекторе:
movementMode = Combined
target = Player            // ОБЯЗАТЕЛЬНО!
patrolPoints = [Point1, Point2, Point3]
aggroDistance = 10f
maxFollowDistance = 20f
stoppingDistance = 2f
patrolWaitTime = 1f

ai.onStartFollowing.AddListener(() => 
{
    Debug.Log("Заметил!");
    ai.SetRunning(true);
});

ai.onStopFollowing.AddListener(() => 
{
    Debug.Log("Потерял");
    ai.SetRunning(false);
});
```

### Динамическое переключение режимов
```csharp
// Переключить на патруль
ai.SetMovementMode(AiNavigation.MovementMode.Patrol);

// Переключить на преследование
ai.SetMovementMode(AiNavigation.MovementMode.FollowTarget);
ai.SetTarget(player);

// Изменить маршрут
ai.SetPatrolPoints(newRoute);
```

---

## 8. Настройка анимации

В Animator создайте параметры:
- `Speed` (float) - нормализованная скорость 0-1
- `IsMoving` (bool) - двигается ли агент

Компонент автоматически обновит их.

---

## 9. Типичные проблемы

### Target is null в Combined режиме
**Решение**: Установите `target` в инспекторе перед запуском.

### Агент не начинает преследовать
**Решение**: 
- Установите `target` в инспекторе
- Проверьте `aggroDistance > 0`
- Включите `debugMode`

### Агент не возвращается к патрулю
**Решение**: Проверьте `maxFollowDistance > 0` и `> aggroDistance`

### Агент застревает
**Решение**: Проверьте что все точки патруля на NavMesh

---

## 10. Debug режим

Включите `debugMode = true` для логирования:

```
[AI NPC] Starting patrol
[AI NPC] Combined: dist=8.5, aggroDistance=10, isFollowing=false, target=Player
[AI NPC] AGGRO! Starting to follow Player at distance 8.5m
[AI NPC] DE-AGGRO! Returning to patrol at distance 21.2m
[AI NPC] Waiting 1s before resuming patrol
[AI NPC] Resuming patrol, moving to point 2
```

---

## 11. Лучшие практики

1. Всегда устанавливайте `target` для Combined режима
2. `aggroDistance < maxFollowDistance` для стабильности
3. Подписывайтесь на события для звуков и анимаций
4. Визуализируйте Gizmos при настройке зон
5. Используйте `patrolZone` для открытых пространств (враги в зоне)
6. Используйте `patrolPoints` для структурированных маршрутов (обход постов)
