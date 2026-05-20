# Урок 31: RPG combat без доверия клиенту

**Навигация:** [Оглавление](README.md) · [Старт](00_START_HERE.md) · [Оформление](LESSON_STYLE.md) · RPG/Progression трек · Mirror `96.x`

| Ключевые слова | RPG, HP, атака, projectile, server authority, VFX |
|----------------|----------------------------------------------------|

---

## Карта урока

| Что | Ответ |
|-----|-------|
| Объект работы | Player combat prefab: input, attack request, server hit, replicated HP. |
| Кто владеет state | Server считает попадание, урон, смерть и награды. |
| Как проверить | Отдельный Client не может передать damage/level/xp, только intent атаки. |
| Артефакт | `NetRpgCombat` scene + таблица attack validation. |

---

## Что должно получиться

Вы собираете минимальный RPG-бой: клиент нажимает кнопку, сервер проверяет cooldown/дистанцию/цель, меняет HP через server-side метод, а клиенты видят результат через `SyncVar`, `SyncList` или snapshot.

---

## Проблема

Опасная схема:

```text
client says: damage=9999 -> server accepts -> target dies
```

Правильная схема:

```text
client input -> CmdRequestAttack(attackId, targetNetId) -> server validates -> ServerApplyDamage -> sync HP/Rpc FX
```

Клиент может показать локальную анимацию сразу, но результат попадания и урон решает сервер.

---

## Минимальная модель

| Слой | Где живёт | Что делает |
|------|-----------|------------|
| Input | local player client | Читает кнопку и отправляет intent. |
| Combat request | player `NetworkBehaviour` | `CmdRequestAttack(attackId, targetNetId)`. |
| Combat state | server | Проверяет cooldown, range, alive, target. |
| HP | spawned target | Меняется только server-side. |
| FX/UI | clients | Показывает результат, не меняет gameplay. |

---

## Практика: безопасная атака

```csharp
using Mirror;
using UnityEngine;

public sealed class NetRpgCombat : NetworkBehaviour
{
    [SerializeField] float attackRange = 2.2f;
    [SerializeField] double cooldown = 0.45;

    double nextAttackTime;

    public void ClientRequestPrimaryAttack(uint targetNetId)
    {
        if (!isLocalPlayer) return;
        CmdRequestAttack("slash", targetNetId);
    }

    [Command]
    void CmdRequestAttack(string attackId, uint targetNetId)
    {
        if (NetworkTime.time < nextAttackTime) return;
        nextAttackTime = NetworkTime.time + cooldown;

        if (!NetworkServer.spawned.TryGetValue(targetNetId, out NetworkIdentity targetIdentity))
            return;

        if (!targetIdentity.TryGetComponent(out NetRpgHealth target))
            return;

        float distance = Vector3.Distance(transform.position, target.transform.position);
        if (distance > attackRange)
            return;

        int damage = ServerResolveDamage(attackId);
        target.ServerApplyDamage(damage);
        RpcPlayAttackFx(targetNetId);
    }

    [Server]
    int ServerResolveDamage(string attackId)
    {
        return attackId == "slash" ? 10 : 0;
    }

    [ClientRpc]
    void RpcPlayAttackFx(uint targetNetId)
    {
        // Только визуал/звук. HP здесь не меняем.
    }
}
```

HP-компонент:

```csharp
using Mirror;
using UnityEngine;

public sealed class NetRpgHealth : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnHpChanged))]
    int hp = 100;

    public bool IsDead => hp <= 0;

    [Server]
    public void ServerApplyDamage(int amount)
    {
        if (amount <= 0 || IsDead) return;
        hp = Mathf.Max(0, hp - amount);
        if (hp == 0)
            ServerDie();
    }

    [Server]
    void ServerDie()
    {
        // Здесь запускается death flow и reward, не на клиенте.
    }

    void OnHpChanged(int oldValue, int newValue)
    {
        // UI обновляет bar.
    }
}
```

---

## Projectile

Projectile не должен быть обычным локальным `Instantiate`, если он влияет на урон.

Server-authoritative вариант:

1. Client отправляет `CmdRequestShoot(attackId, aimPoint)`.
2. Server проверяет cooldown, ammo, направление.
3. Server создаёт projectile prefab.
4. Server вызывает `NetworkServer.Spawn(projectile)`.
5. Projectile на server считает hit и вызывает `ServerApplyDamage`.
6. Clients получают spawn/position/FX.

Минимум для prefab:

- root `NetworkIdentity`;
- registered spawnable prefab в `NetworkManager`;
- movement либо через `NetworkTransform`, либо projectile живёт коротко и server шлёт hit FX.

---

## No-code и UnityEvent wiring

Для no-code кнопок и UnityEvent правило такое же: событие UI не меняет trusted state, а вызывает метод на локальном player object.

Безопасная цепочка:

```text
Button/UnityEvent
-> LocalPlayer.ClientRequestPrimaryAttack(targetNetId)
-> CmdRequestAttack(...)
-> server validation
-> server state change
```

Опасная цепочка:

```text
Button/UnityEvent -> RpgCharacter.NetDamage(9999) / AddXp(500) / SetLevel(10)
```

Если no-code action должен работать и в singleplayer, и в multiplayer, дайте ему два явных входа: local/offline method и network request method. В инспекторе называйте их так, чтобы новичок видел разницу: `ClientRequest...`, `ServerApply...`, `OfflineApply...`.

---

## Проверка себя

- Клиент не передаёт damage.
- Клиент не вызывает `ServerApplyDamage` напрямую.
- Projectile с уроном создаётся сервером через `NetworkServer.Spawn`.
- Remote client не управляет чужим `RpgAttackController`.
- Смерть цели срабатывает один раз на server.

---

## Минимальная диагностика

| Симптом | Что проверить |
|---------|---------------|
| Пуля видна только серверу | Есть `NetworkServer.Spawn` и prefab зарегистрирован? |
| Все игроки атакуют от одной кнопки | Есть `isLocalPlayer`/`isOwned` guard на input? |
| Клиент может задать урон | Command принимает итог вместо `attackId`/intent. |
| Смерть срабатывает дважды | Death flow запускается и на client, и на server. |
| Попадание есть в Host, но нет в Client | Проверяли отдельный Client, а не только Host? |

---

## Лайфхаки

- Называйте команды как request: `CmdRequestAttack`, `CmdRequestShoot`, `CmdTryUsePotion`.
- Для hit FX используйте `ClientRpc`; для HP используйте state sync.
- Для целей передавайте `netId`, а не произвольный `GameObject` из UI.
- Начинайте с melee/raycast, projectile добавляйте после того, как HP уже честно синхронизируется.

---

## Профессиональный минимум

- Damage, death и rewards считаются на server.
- Любой `attackId` проверяется по server catalog.
- Cooldown считается на server time.
- В каждом reject есть причина в server log.
- Gameplay не зависит от `NetworkManagerHUD`.

---

## Домашнее задание

Соберите `NetRpgCombat`:

1. Два клиента подключаются к одной сцене.
2. Игрок A атакует врага.
3. HP врага меняется на обоих клиентах.
4. Клиент не может передать произвольный damage.
5. Projectile, если он есть, спавнится через `NetworkServer.Spawn`.
