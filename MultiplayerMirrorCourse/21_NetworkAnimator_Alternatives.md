# Урок 21: NetworkAnimator и ручная синхронизация анимаций

**Навигация:** [Оглавление](README.md) · [Старт](00_START_HERE.md) · [Оформление](LESSON_STYLE.md) · продвинутый трек · урок 6/15 · Mirror `96.x`

| Ключевые слова | `NetworkAnimator`, Animator parameters, stateId, root motion |
|----------------|--------------------------------------------------------------|

---

## Карта урока

| Что | Ответ |
|-----|-------|
| Объект работы | Animator sync strategy for player/NPC. |
| Кто владеет state | Server/gameplay model owns meaningful animation state; client may play cosmetics. |
| Как проверить | Remote client sees correct states; traffic compared between approaches. |
| Артефакт | Animation sync decision: NetworkAnimator, stateId, RPC, local-only. |

---

## Что должно получиться

Вы выбираете, что реально нужно отправлять по сети: Animator-параметры, state ID или событие gameplay.

---

## Проблема

Анимация часто кажется сетевой проблемой, хотя большая часть visual state восстанавливается локально из movement и gameplay state.

---

## Варианты

| Подход | Когда подходит |
|--------|----------------|
| `NetworkAnimator` | Быстрый старт, немного параметров. |
| `SyncVar stateId` | Явные состояния: idle/run/attack/dead. |
| Rpc event | Разовый эффект: удар, эмоция, звук. |
| Локальная анимация | Cosmetic, зависит от локальных данных. |

Gameplay-critical окна удара не должны зависеть только от красивого trigger в Animator.

---

## Практика: ручной stateId

```csharp
using Mirror;
using UnityEngine;

public sealed class NetworkAnimationState : NetworkBehaviour
{
    [SerializeField] Animator animator;

    [SyncVar(hook = nameof(OnStateChanged))]
    int stateId;

    [Server]
    public void ServerSetState(int newStateId)
    {
        stateId = newStateId;
    }

    void OnStateChanged(int oldValue, int newValue)
    {
        animator.CrossFade(newValue, 0.1f);
    }
}
```

---

## Проверка себя

- Remote client видит переходы анимации.
- Трафик сравнен с `NetworkAnimator`.
- Root motion не двигает авторитетную позицию в обход movement-схемы.
- Gameplay damage считается сервером, а не animation event на клиенте.

---

## Минимальная диагностика

| Симптом | Что проверить |
|---------|---------------|
| Анимация едет, позиция откатывается | Root motion конфликтует с movement authority. |
| Трафик высокий | Слишком много Animator parameters sync. |
| Урон не совпадает с visual | Gameplay window не отделён от animation event. |
| Host красивый, Client сломан | Проверка была только host-side. |

---

## Частые ошибки

- Синхронизировать все Animator параметры.
- Root motion двигает объект, а NetworkTransform пытается догнать.
- Атака засчитывается клиентским animation event.
- Тестировать только Host.

---

## Лайфхаки

- Сначала отправляйте смысл: `isMoving`, `weaponId`, `stateId`.
- Visual-only layers держите локальными.
- Для PvP отделяйте "окно урона" от визуальной анимации.

---

## Профессиональный минимум

- Синхронизируется смысл, а не каждый cosmetic parameter.
- Animation event не является единственным источником gameplay damage.
- Root motion strategy согласована с movement strategy.
- Сравнение подходов записано в profiling notes.

---

## Домашнее задание

Сравните два варианта на одном персонаже:

1. `NetworkAnimator`.
2. `SyncVar stateId`.

Запишите байты/сек и качество визуала.
