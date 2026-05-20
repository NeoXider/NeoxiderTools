# Урок 4: SyncVar и состояние мира

**Навигация:** [Оглавление](README.md) · [Старт](00_START_HERE.md) · [Оформление](LESSON_STYLE.md) · базовый трек · урок 4/15 · Mirror `96.x`

| Ключевые слова | `[SyncVar]`, hook, server state, dirty bits, `netId` |
|----------------|------------------------------------------------------|

---

## Карта урока

| Что | Ответ |
|-----|-------|
| Объект работы | `NetworkBehaviour` на spawned object: HP, score или state двери. |
| Кто владеет state | Server меняет значение; clients получают копию. |
| Как проверить | Server меняет `SyncVar`, оба клиента видят hook/update. |
| Артефакт | Пример `SyncVar` с hook и UI, который только отображает. |

---

## Что должно получиться

Вы умеете сделать поле, которое меняется на сервере и автоматически приходит клиентам. Например HP игрока, счёт матча, состояние двери, выбранный класс персонажа.

---

## Проблема

Новички часто ставят `[SyncVar]` на UI, GameManager без `NetworkIdentity` или обычный `MonoBehaviour`. Такой объект не является сетевым spawned object, поэтому Mirror не реплицирует поле.

---

## Теория коротко

`SyncVar` работает так:

1. Поле находится в `NetworkBehaviour`.
2. GameObject имеет `NetworkIdentity`.
3. Объект существует в сетевой сессии (`netId != 0`).
4. Сервер меняет поле.
5. Mirror отправляет изменение observers.
6. Клиенты получают новое значение и вызывают hook.

Направление только server -> clients. Если клиент хочет изменить state, он вызывает `Command`, а сервер уже меняет `SyncVar`.

---

## Где держать SyncVar

| State | Где хранить |
|-------|-------------|
| HP игрока | На player prefab или компоненте stats. |
| HP врага | На enemy prefab. |
| Открыта ли дверь | На сетевом prefab двери или сценовом NetworkIdentity. |
| Текст HUD | Не SyncVar. UI читает state из сетевой модели. |
| Инвентарь | Часто SyncList/SyncDictionary, см. урок 5. |

---

## Практика

```csharp
using Mirror;
using UnityEngine;

public sealed class Health : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnHealthChanged))]
    int current = 100;

    public int Current => current;

    [Server]
    public void ApplyDamage(int amount)
    {
        if (amount <= 0) return;
        current = Mathf.Max(0, current - amount);
    }

    void OnHealthChanged(int oldValue, int newValue)
    {
        Debug.Log($"[HP] {oldValue} -> {newValue} netId={netId}");
        // Здесь можно поднять event для UI.
    }
}
```

Кнопка атаки не должна менять `current` на клиенте. Она должна вызвать `Command` из урока 6.

---

## Проверка себя

- Сервер вызывает `ApplyDamage`.
- Client hook пишет лог.
- В runtime у объекта `netId != 0`.
- UI обновляется через hook/event, а не хранит авторитетное HP.

---

## Минимальная диагностика

| Симптом | Что проверить |
|---------|---------------|
| Hook не вызывается | Значение реально изменилось на сервере? Объект spawned? |
| Видит только Host | Проверяли отдельный Client, а не host view? |
| Late join видит старые данные | State хранится в `SyncVar`, а не только отправлен RPC-событием? |
| UI меняет HP напрямую | UI должен вызвать request, server меняет `SyncVar`. |

---

## Частые ошибки

- `[SyncVar]` в `MonoBehaviour`, а не `NetworkBehaviour`.
- Объект не spawned, `netId == 0`.
- Клиент меняет SyncVar и ждёт репликации.
- Hook делает тяжёлую логику или вызывает `Command`, создавая петлю.
- Для списка используется `SyncVar List<T>` вместо Sync-коллекций.

---

## Лайфхаки

- Если SyncVar "не приходит", сначала проверьте `netId`, потом server log записи, потом client hook.
- Для дробных значений думайте о квантизации: не всегда нужен float с высокой точностью.
- Для приватного state используйте owner sync mode там, где это подходит.
- Для одноразового эффекта используйте Rpc/message, не SyncVar.

---

## Профессиональный минимум

- Long-lived state хранится в Sync, а не только в RPC.
- Hook не содержит server-only game logic.
- Клиентские значения считаются отображением, не источником правды.
- Для приватных данных продуманы owner-only sync или отдельный `TargetRpc`.

---

## Для RPG/Progression

HP и текущие ресурсы можно показывать через replicated state, но XP, уровень, купленные перки и награды нельзя менять из UI hook. После этого урока вернитесь к доменному маршруту: [31_RPG_Combat_Server_Authority.md](31_RPG_Combat_Server_Authority.md) -> [32_Progression_XP_Rewards.md](32_Progression_XP_Rewards.md).

---

## Домашнее задание

Сделайте HP игрока:

1. HP меняет только сервер.
2. Hook обновляет UI локального игрока.
3. При HP ниже 30% UI меняет цвет.
4. В логе есть `netId` объекта.

В заметках напишите, почему UI Slider не является источником правды.
