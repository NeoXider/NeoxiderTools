# Урок 6: Command, ClientRpc и TargetRpc

**Навигация:** [Оглавление](README.md) · [Старт](00_START_HERE.md) · [Оформление](LESSON_STYLE.md) · базовый трек · урок 6/15 · Mirror `96.x`

| Ключевые слова | `[Command]`, `[ClientRpc]`, `[TargetRpc]`, authority, rate limit |
|----------------|------------------------------------------------------------------|

---

## Карта урока

| Что | Ответ |
|-----|-------|
| Объект работы | Player action: buy, attack, ready, interact. |
| Кто владеет state | Client отправляет request; server проверяет и меняет state. |
| Как проверить | Валидный request проходит, невалидный получает reject через `TargetRpc`. |
| Артефакт | Команда с validation, reject reason и rate limit. |

---

## Что должно получиться

Вы понимаете направления вызовов и умеете сделать безопасное действие: клиент просит, сервер проверяет, клиенты получают результат.

---

## Проблема

Самая опасная ошибка: клиент отправляет итог. Например `CmdDealDamage(9999)` или `CmdSetGold(100000)`. Сервер не должен принимать готовую правду от клиента.

---

## Матрица вызовов

| API | Направление | Для чего |
|-----|-------------|----------|
| `Command` | client -> server | Запрос действия игрока. |
| `ClientRpc` | server -> observers | Эффект или событие для всех, кто видит объект. |
| `TargetRpc` | server -> один client | Персональный ответ: отказ, причина kick, reward. |

`Command` по умолчанию требует authority. Обычно команда вызывается с player object владельца. `requiresAuthority = false` используйте только осознанно и всегда валидируйте отправителя.

---

## Практика: покупка предмета

```csharp
using Mirror;

public sealed class PlayerShopActions : NetworkBehaviour
{
    [SyncVar] int gold = 100;

    [Command]
    public void CmdBuyItem(int itemId)
    {
        if (!ShopCatalog.TryGetPrice(itemId, out int price))
        {
            TargetBuyRejected(connectionToClient, "unknown_item");
            return;
        }

        if (gold < price)
        {
            TargetBuyRejected(connectionToClient, "not_enough_gold");
            return;
        }

        gold -= price;
        ServerAddItem(itemId);
    }

    [TargetRpc]
    void TargetBuyRejected(NetworkConnectionToClient target, string reason)
    {
        // Показать UI ошибку только этому игроку.
    }

    [Server]
    void ServerAddItem(int itemId)
    {
        // Изменить серверный инвентарь.
    }
}
```

Клиент отправляет только `itemId`. Цена, наличие золота и выдача предмета находятся на сервере.

---

## Rate limit

Каждый `Command` должен иметь ответ на вопрос: "Что будет, если клиент вызовет его 100 раз в секунду?"

Простая схема:

| Команда | Лимит |
|---------|-------|
| Покупка | 5-10 раз/сек максимум, лучше меньше. |
| Атака | По cooldown оружия. |
| Чат | Окно сообщений и mute/ignore. |
| Ready в лобби | Debounce 0.3-1 сек. |

---

## Проверка себя

- Команда вызывается с owned/local player.
- Сервер может отказать.
- Отказ приходит через `TargetRpc`.
- Клиент не передаёт цену, урон или итог.
- При спаме команда не ломает сервер.

---

## Минимальная диагностика

| Симптом | Что проверить |
|---------|---------------|
| `Command called without authority` | Команда вызывается с owned player или нужен осознанный `requiresAuthority = false`? |
| Урон можно подделать | Клиент отправляет итог вместо target/action id. |
| Отказ видят все | Используйте `TargetRpc`, а не общий `ClientRpc`. |
| Сервер лагает от кнопки | Нет cooldown/rate limit/debounce. |

---

## Частые ошибки

- `Command` вызывается с UI object без authority.
- `requiresAuthority = false` используется как быстрый фикс.
- `ClientRpc` меняет HP или валюту.
- В RPC передают огромные списки.
- Нет логов отказов и лимитов.

---

## Лайфхаки

- Команды называйте как намерение: `CmdRequestAttack`, `CmdTryBuy`, `CmdSetReady`.
- Валидацию держите на сервере рядом с изменением state.
- Для приватного результата используйте `TargetRpc`, не общий `ClientRpc`.
- Для протокольных событий без объекта смотрите `NetworkMessage` в уроке 26.

---

## Профессиональный минимум

- Название команды описывает намерение, а не итог.
- Валидация и изменение state находятся рядом на server side.
- Для каждого `Command` есть лимит и лог отказа.
- `requiresAuthority = false` требует проверки `sender` и угрозы abuse.

---

## Для RPG/Progression

Команды RPG должны звучать как request: `CmdRequestAttack`, `CmdTryBuyPerk`, `CmdTryUseItem`. Не делайте публичные `CmdAddXp`, `CmdSetLevel`, `CmdSetDamage` для клиента. Готовый вертикальный пример разобран в [31_RPG_Combat_Server_Authority.md](31_RPG_Combat_Server_Authority.md) и [32_Progression_XP_Rewards.md](32_Progression_XP_Rewards.md).

---

## Домашнее задание

Сделайте покупку предмета:

1. UI вызывает метод на локальном player.
2. Player отправляет `CmdBuyItem(itemId)`.
3. Сервер проверяет цену и валюту.
4. Успех меняет Sync-коллекцию инвентаря.
5. Отказ приходит через `TargetRpc`.
6. Есть простой rate limit.
