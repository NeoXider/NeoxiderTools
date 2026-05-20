# Урок 32: XP, уровни, rewards и progression profile

**Навигация:** [Оглавление](README.md) · [Старт](00_START_HERE.md) · [Оформление](LESSON_STYLE.md) · RPG/Progression трек · Mirror `96.x`

| Ключевые слова | XP, level, reward, perk, account state, save |
|----------------|----------------------------------------------|

---

## Карта урока

| Что | Ответ |
|-----|-------|
| Объект работы | XP/reward flow после server-side события. |
| Кто владеет state | Server/backend выдаёт XP и permanent rewards. |
| Как проверить | Client не может вызвать `AddXp`, `SetLevel`, `UnlockPerk` как итог. |
| Артефакт | `PROGRESSION_FLOW.md` + тест boss kill -> XP -> level reward. |

---

## Что должно получиться

Вы понимаете разницу между state матча и долгосрочной прогрессией аккаунта. HP и временные баффы живут в матче. XP, permanent unlocks и купленные перки сохраняются в profile и пишутся только доверенным кодом.

---

## Главная граница

| Данные | Где живут | Кто пишет |
|--------|-----------|-----------|
| Current HP | match object | Server match logic. |
| Temporary buff | match object | Server combat/item logic. |
| Total XP | account/profile | Server/backend/save layer. |
| Level rewards | progression system | Server after level change. |
| Purchased perks | account/profile | Server after validation. |
| XP bar UI | client UI | Только отображает synced/profile state. |

Клиент никогда не отправляет `AddXp(500)`. Клиент может завершить действие, а сервер решает, заслужена ли награда.

---

## Правильный flow награды

```text
Server enemy death
-> Server checks killer/assist/quest
-> ServerAwardXp(playerId, amount)
-> progression adds XP
-> level-up rewards are granted for every crossed level
-> profile saves/flushed
-> UI receives updated level/xp/perk points
```

---

## Практика: награда за смерть врага

```csharp
using Mirror;
using UnityEngine;

public sealed class NetRewardOnDeath : NetworkBehaviour
{
    [SerializeField] int xpReward = 100;

    [Server]
    public void ServerGrantKillReward(NetworkIdentity killer)
    {
        if (killer == null) return;

        if (!killer.TryGetComponent(out NetPlayerProgression progression))
            return;

        progression.ServerAddXp(xpReward);
    }
}
```

Player progression facade:

```csharp
using Mirror;

public sealed class NetPlayerProgression : NetworkBehaviour
{
    [SyncVar] int level = 1;
    [SyncVar] int totalXp;
    [SyncVar] int perkPoints;

    [Server]
    public void ServerAddXp(int amount)
    {
        if (amount <= 0) return;

        int previousLevel = level;
        totalXp += amount;
        level = ServerEvaluateLevel(totalXp);

        for (int next = previousLevel + 1; next <= level; next++)
            ServerGrantLevelReward(next);

        ServerSaveProgressionProfile();
    }

    [Server]
    void ServerGrantLevelReward(int reachedLevel)
    {
        if (reachedLevel == 2) perkPoints += 1;
        if (reachedLevel == 5) perkPoints += 2;
    }

    [Server]
    int ServerEvaluateLevel(int xp)
    {
        return 1 + xp / 100;
    }

    [Server]
    void ServerSaveProgressionProfile()
    {
        // Backend или SaveProvider на server side.
    }
}
```

Важная деталь: если игрок перескочил с 1 на 5 уровень, цикл должен выдать rewards за 2, 3, 4 и 5 уровни. Нельзя выдавать только reward финального уровня.

---

## Покупка перка

```text
Client UI click
-> CmdTryBuyPerk(perkId)
-> Server checks: perk exists, level, perk points, prerequisites, unlock nodes
-> Server spends point and saves profile
-> Sync/UI update
```

Команда:

```csharp
[Command]
void CmdTryBuyPerk(string perkId)
{
    if (!ServerCanBuyPerk(perkId, out string reason))
    {
        TargetPerkRejected(connectionToClient, reason);
        return;
    }

    ServerBuyPerk(perkId);
}
```

Клиент передаёт только `perkId`. Стоимость, prerequisites и доступность находятся на server.

---

## Save

Для Progression важно явно решить, где профиль сохраняется:

| Сценарий | Где сохранять |
|----------|---------------|
| Local co-op / prototype | Server process через `SaveProvider.Save()`. |
| Dedicated public game | Backend/account service. |
| Roguelite без аккаунтов | Host/server save profile, не client UI. |
| Offline singleplayer | Local `SaveProvider`, но multiplayer code всё равно не доверяет client итогам. |

Если используете file-backed save provider, одного `SetString` мало. Нужен `SaveProvider.Save()` или гарантированный flush при shutdown.

---

## Проверка себя

- XP выдаётся server-side событием.
- Level-up rewards выдаются за все перескоченные уровни.
- Perk purchase проверяет cost, level, prerequisites и required unlocks.
- Profile save делает flush.
- UI не вызывает `AddXp` и `SetLevel`.

---

## Минимальная диагностика

| Симптом | Что проверить |
|---------|---------------|
| XP работает только в Host | Награда выдаётся на server или локально в host-client части? |
| Перки исчезли после restart | Profile flush: был `SaveProvider.Save()`/backend write? |
| Reward пропущен при большом XP | Есть цикл по всем crossed levels? |
| Клиент может поднять уровень | Есть публичный Command `CmdSetLevel`/`CmdAddXp` без server-only защиты? |
| UI показывает старый уровень | UI читает synced/profile state, а не кэш старого объекта? |

---

## Лайфхаки

- Разделяйте `LevelCurve` для расчёта уровня и `RewardTrack` для наград.
- Храните stable IDs для perk/unlock nodes, не index в массиве.
- Для premium rewards храните claimed state или понятный одноразовый activation flow.
- Логируйте reward transaction: account, reason, amount, old level, new level.

---

## Профессиональный минимум

- Progression profile не является UI state.
- Client не передаёт XP amount как правду.
- Save имеет явный момент flush.
- Тест есть на multi-level jump.
- Тест есть на missing/invalid perk dependency.

---

## Домашнее задание

Сделайте `PROGRESSION_FLOW.md`:

1. Где хранится total XP.
2. Кто пишет profile.
3. Как выдаются rewards за несколько уровней.
4. Как покупается perk.
5. Как проверяется сохранение после restart.
