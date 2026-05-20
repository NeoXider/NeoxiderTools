# Урок 33: итоговый RPG/Progression vertical slice

**Навигация:** [Оглавление](README.md) · [Старт](00_START_HERE.md) · [Оформление](LESSON_STYLE.md) · RPG/Progression трек · Mirror `96.x`

| Ключевые слова | capstone, RPG loop, XP, perks, smoke |
|----------------|--------------------------------------|

---

## Карта урока

| Что | Ответ |
|-----|-------|
| Объект работы | `NetRpgSandbox`: бой, смерть врага, XP, level reward, perk UI. |
| Кто владеет state | Server владеет gameplay и progression writes. |
| Как проверить | Host + отдельный Client + reconnect/late join smoke. |
| Артефакт | `SMOKE_RPG_PROGRESSION.md` и рабочая сцена. |

---

## Что должно получиться

Вы собираете один законченный loop:

```text
Player attacks enemy
-> server applies damage
-> enemy dies once
-> server grants XP
-> level/perk points update
-> UI refreshes
-> profile saves
-> smoke test catches Host-only bugs
```

---

## Состав сцены

| Object | Components |
|--------|------------|
| `Network` | `NetworkManager`, transport, registered player/enemy/projectile prefabs. |
| `Player` prefab | `NetworkIdentity`, movement, combat request, health, progression facade. |
| `Enemy` prefab | `NetworkIdentity`, health, reward-on-death. |
| `Projectile` prefab | `NetworkIdentity` if it affects damage, optional `NetworkTransform`. |
| `HUD` | Reads local player state only, sends UI intents to local player. |

---

## Обязательные правила

- UI не меняет HP, XP, perks.
- Client не отправляет damage/XP/level как итог.
- Server death flow защищён от повторного reward.
- Projectile with damage is server spawned.
- Profile save flushes.
- Perk IDs and unlock IDs stable.

---

## Smoke checklist

Создайте `SMOKE_RPG_PROGRESSION.md`:

| Step | Expected |
|------|----------|
| Host starts match | Server log has mode, scene, version. |
| Client connects | Player spawned, `netId` visible. |
| Client attacks enemy | Server log has `CmdRequestAttack`, not `CmdDealDamage`. |
| Enemy loses HP | Both clients see same HP. |
| Enemy dies | Death log appears once. |
| XP granted | Server log has reward transaction. |
| Level jumps | Rewards for every crossed level are granted. |
| Perk purchased | Server validates cost/prerequisites. |
| Client reconnects | UI shows current synced/profile state. |
| Spam attack | Cooldown/rate limit rejects extra requests. |

---

## Тестовые abuse-сценарии

| Попытка | Ожидаемый результат |
|---------|---------------------|
| Client sends fake damage | Нет такого public API или request rejected. |
| Client sends fake XP | Rejected/server-only. |
| Client buys locked perk | Target reject reason. |
| Client spams attack | Cooldown/rate limit. |
| Projectile instantiated locally | Не наносит урон, server path required. |
| Enemy death called twice | Reward grants once. |

---

## Минимальные логи

```text
[RPG] attack request conn=2 playerNetId=17 attack=slash target=41
[RPG] attack reject conn=2 reason=cooldown
[RPG] damage source=17 target=41 amount=10 hp=20
[RPG] death target=41 killer=17
[PROGRESSION] xp account=local-2 reason=enemy_kill amount=100 oldLevel=1 newLevel=2
[PROGRESSION] reward level=2 perkPoints=1
[PROGRESSION] perk buy account=local-2 perk=damage_up_1 result=ok
```

Не логируйте raw tokens, passwords и приватные персональные данные.

---

## Проверка себя

- Сцена проходит не только в Host.
- Отдельный Client не может изменить trusted state напрямую.
- XP/perk state не теряется после save/load.
- Late join/reconnect не видит нулевой UI.
- Security checklist из урока 29 покрывает RPG commands.

---

## Частые ошибки

- Считать Host тестом.
- Делать `CmdAddXp(amount)` доступным клиенту.
- Выдавать reward в `ClientRpc`.
- Хранить perk cost только на клиенте.
- Спавнить projectile через обычный локальный `Instantiate`.
- Использовать index массива как ability/perk ID.

---

## Лайфхаки

- Перед красивым UI добейтесь текстовых логов reward flow.
- Для первого capstone сделайте одного врага и одну атаку.
- Death reward защищайте флагом `rewardGranted`.
- В smoke тесте проверяйте invalid action, а не только happy path.

---

## Профессиональный минимум

- Есть один воспроизводимый vertical slice.
- Есть таблица trust boundary для всех RPG/Progression commands.
- Есть тест на multi-level rewards.
- Есть тест на save flush.
- Есть отдельный smoke для Host + Client.

---

## Домашнее задание

Закройте capstone:

1. `NetRpgSandbox` запускается Host + Client.
2. Client убивает врага честной атакой.
3. Server выдаёт XP и perk point.
4. UI показывает обновление.
5. Save/load восстанавливает progression.
6. `SMOKE_RPG_PROGRESSION.md` приложен к проекту.
