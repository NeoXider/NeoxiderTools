# AiNavigation (Legacy)

**Назначение:** Компонент ИИ-навигации на основе `NavMeshAgent`. Поддерживает три режима: преследование цели (`FollowTarget`), патрулирование по точкам или зоне (`Patrol`), и комбинированный (`Combined` — патруль с автоматическим переключением на преследование при приближении цели).

> ⚠️ **Устарел.** Рекомендуется использовать `Neo.NPC.NpcNavigation + модули`.

## Поля (Inspector)

| Поле | Описание |
|------|----------|
| **Movement Mode** | `FollowTarget`, `Patrol` или `Combined`. |
| **Target** | Цель для преследования (Transform). |
| **Trigger Distance** | Минимальная дистанция, на которой начнётся движение. |
| **Stopping Distance** | Дистанция остановки от цели. |
| **Patrol Points** | Массив точек патрулирования. |
| **Patrol Zone** | BoxCollider для случайного патрулирования (если задан, точки игнорируются). |
| **Aggro Distance** | Расстояние, на котором агент переключается с патруля на преследование (Combined). |
| **Walk / Run Speed** | Скорость ходьбы и бега. |
| **Animator** | Опциональный аниматор для параметров `Speed` и `IsMoving`. |

## API

| Метод / Свойство | Описание |
|------------------|----------|
| `void SetTarget(Transform newTarget)` | Установить новую цель. |
| `bool SetDestination(Vector3 destination)` | Перейти к точке. |
| `void Stop()` / `void Resume()` | Остановить / возобновить движение. |
| `void SetRunning(bool enable)` | Включить/выключить бег. |
| `void StartPatrol()` / `void StopPatrol()` | Запустить / остановить патрулирование. |
| `bool IsMoving { get; }` | Движется ли агент. |
| `float RemainingDistance { get; }` | Оставшееся расстояние до цели. |

## Unity Events

| Событие | Аргументы | Описание |
|---------|-----------|----------|
| `onDestinationReached` | `Vector3` | Агент добрался до цели. |
| `onPathBlocked` | `Vector3` | Путь заблокирован. |
| `onPatrolPointReached` | `int` | Достигнута точка патруля (индекс). |
| `onStartFollowing` / `onStopFollowing` | *(нет)* | Переключение между патрулём и преследованием (Combined). |

## Примеры

### Пример No-Code (в Inspector)
Добавьте `NavMeshAgent` + `AiNavigation` на врага. Перетащите игрока в поле `Target`. Выберите `Movement Mode = FollowTarget`. Запеките NavMesh. При запуске враг побежит к игроку.

### Пример (Код)
```csharp
[SerializeField] private AiNavigation _guard;

public void AlertGuard(Transform intruder)
{
    _guard.SetTarget(intruder);
    _guard.SetRunning(true);
}
```

## См. также
- ← [Tools/Other](README.md)
