# Урок 9: серверный авторитет и античит-мышление

**Навигация:** [Оглавление](README.md) · [Старт](00_START_HERE.md) · [Оформление](LESSON_STYLE.md) · базовый трек · урок 9/15 · Mirror `96.x`

| Ключевые слова | server authority, validation, speedhack, cooldown, trust boundary |
|----------------|-------------------------------------------------------------------|

---

## Карта урока

| Что | Ответ |
|-----|-------|
| Объект работы | Таблица validation для всех gameplay commands. |
| Кто владеет state | Server решает HP, валюту, урон, победу и rewards. |
| Как проверить | Невалидные requests отклоняются и логируются. |
| Артефакт | `SECURITY_CHECKLIST.md` или раздел validation/rate limits. |

---

## Что должно получиться

Вы умеете смотреть на каждое действие игрока как на недоверенный запрос. Сервер проверяет возможность действия и только потом меняет состояние.

---

## Проблема

Клиентский build находится у игрока. Его можно изменить. Поэтому любой параметр из клиента считается подозрительным: урон, позиция, время, цена, победа, количество валюты.

---

## Теория коротко

Правильная схема:

```text
client input -> Command(intent) -> server validation -> server state change -> SyncVar/Rpc/UI
```

Неправильная схема:

```text
client decides result -> server accepts -> cheater wins
```

---

## Что проверять на сервере

| Действие | Минимальная проверка |
|----------|----------------------|
| Атака | cooldown, дистанция, line of sight, жива ли цель. |
| Движение | max speed, teleport threshold, grounded/allowed state. |
| Покупка | item exists, price, currency, inventory capacity. |
| Ready | фаза лобби, debounce, владелец slot. |
| Использование предмета | наличие предмета, cooldown, разрешённая цель. |

---

## Практика: серверная атака

```csharp
using Mirror;
using UnityEngine;

public sealed class PlayerCombat : NetworkBehaviour
{
    [SerializeField] float attackRange = 2f;
    [SerializeField] float cooldown = 0.5f;

    double nextAttackTime;

    [Command]
    public void CmdMeleeAttack()
    {
        if (NetworkTime.time < nextAttackTime) return;
        nextAttackTime = NetworkTime.time + cooldown;

        if (!Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, attackRange))
            return;

        if (!hit.collider.TryGetComponent(out Health target))
            return;

        target.ApplyDamage(10);
    }
}
```

Клиент не отправляет урон и не сообщает "я попал". Он просит атаковать; сервер проверяет.

---

## Проверка себя

- Атака вне дистанции не наносит урон.
- Спам команды не ускоряет атаку.
- Клиент не может передать произвольный damage.
- Серверный лог показывает подозрительные превышения без спама.

---

## Минимальная диагностика

| Симптом | Что проверить |
|---------|---------------|
| Клиент может выдать себе ресурс | Server принимает итог вместо намерения. |
| Reject непонятен | Нет structured reason в логе и TargetRpc. |
| Ban вместо фикса бага | Сначала закрыть trust boundary, потом думать о наказаниях. |
| Server CPU растёт от спама | Нет rate limit, cooldown или queue limit. |

---

## Частые ошибки

- Доверять `clientPosition` без проверки.
- Делать экономику через `ClientRpc`.
- Хранить цены только в клиентском ScriptableObject.
- Не ограничивать частоту `Command`.
- Считать, что anti-cheat plugin исправит плохую архитектуру.

---

## Лайфхаки

- Для каждого `Command` добавьте комментарий: "сервер проверяет ...".
- Логи нарушений агрегируйте по `connectionId`.
- Не баньте сразу за один странный пакет: сеть и баги тоже бывают. Но state не меняйте.
- В PvE можно быть мягче; в PvP серверная проверка жёстче.

---

## Профессиональный минимум

- Все публичные requests имеют validation и abuse scenario.
- Любой client-provided ID проверяется на существование, ownership и доступность.
- Все лимиты измеримы: cooldown, max distance, max amount, max requests/sec.
- Ошибки безопасности не скрываются UI-слоем.

---

## Домашнее задание

Выберите три команды вашей игры и сделайте таблицу:

| Command | Что просит клиент | Что проверяет сервер | Что логируем при отказе |
|---------|-------------------|----------------------|--------------------------|

Добавьте rate limit хотя бы к одной команде.
