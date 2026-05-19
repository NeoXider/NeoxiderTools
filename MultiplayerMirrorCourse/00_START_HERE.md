# 00. Старт здесь

Этот файл нужен, чтобы получить первый рабочий результат до погружения в SyncVar, RPC, prediction и хостинг.

---

## Что сейчас важно

Сначала докажите, что два процесса видят один матч. Не добавляйте оружие, инвентарь, магазин, lobby и красивый UI, пока базовый connect не стабилен.

Минимальный результат первого вечера:

```text
Editor Host -> listens on port
Second Client -> connects
Server log -> connectionId
Both clients -> see player objects
```

Если этого нет, все следующие темы будут только маскировать проблему.

---

## Цель первого вечера

К концу старта у вас должно быть:

- тестовая сцена `NetSandbox`;
- `NetworkManager` + transport;
- player prefab с `NetworkIdentity`;
- Host в одном окне;
- отдельный Client во втором окне;
- лог подключения с `connectionId`;
- понимание, что клиент не меняет авторитетный state напрямую.

---

## Минимальная среда

| Что | Рекомендация |
|-----|--------------|
| Unity | LTS или Unity 6, точную версию записать. |
| Mirror | В проекте сейчас `96.0.1`. |
| Второй клиент | ParrelSync, Unity Multiplayer Play Mode или отдельный build. |
| Первая сцена | Пустая `NetSandbox`, без большого уровня. |
| Первый transport | KCP для desktop, SimpleWeb для WebGL. |

Unity Multiplayer Play Mode удобен для быстрой проверки, но не заменяет отдельный build и dedicated test. По документации Unity он рассчитан на небольшую локальную проверку и имеет ограничения по authoring в дополнительных instance.

---

## Словарь

| Термин | Объяснение |
|--------|------------|
| Server | Процесс, который решает правду матча. |
| Client | Процесс игрока: ввод, UI, отображение. |
| Host | Server + Client в одном процессе. |
| Dedicated | Сервер без локального игрока и UI. |
| Authority | Право менять state. |
| `NetworkIdentity` | Паспорт сетевого объекта. |
| `netId` | Runtime ID spawned-объекта. |
| `SyncVar` | Server -> clients state. |
| `Command` | Client -> server request. |
| `Rpc` | Server -> client(s) event. |
| `NetworkMessage` | Сообщение без привязки к объекту. |

---

## Первый запуск

1. Создайте сцену `NetSandbox`.
2. Создайте GameObject `Network`.
3. Добавьте `NetworkManager`.
4. Добавьте transport.
5. Временно добавьте `NetworkManagerHUD`.
6. Создайте prefab `Player`.
7. На root `Player` добавьте `NetworkIdentity`.
8. Назначьте `Player` в `NetworkManager.Player Prefab`.
9. Запустите Host.
10. Подключите второй Client.

Не добавляйте стрельбу, инвентарь и UI, пока это не работает стабильно.

### Минимальный лог

Добавьте лог в наследник `NetworkManager`, когда базовый manager уже запускается:

```csharp
using Mirror;
using UnityEngine;

public sealed class GameNetworkManager : NetworkManager
{
    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        base.OnServerConnect(conn);
        Debug.Log($"[NET] connect id={conn.connectionId} address={conn.address}");
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        Debug.Log($"[NET] disconnect id={conn.connectionId}");
        base.OnServerDisconnect(conn);
    }
}
```

Не начинайте с кастомного manager, если стандартный `NetworkManager` ещё не проверен.

---

## Диагностика

| Симптом | Проверка |
|---------|----------|
| Client подключился, игрока нет | Назначен ли `Player Prefab`? Есть ли `NetworkIdentity` на root? |
| `Command called without authority` | Команда вызывается с owned/local object? |
| `SyncVar` не приходит | Объект spawned? `netId != 0`? Поле в `NetworkBehaviour`? |
| Работает только Host | Запускали отдельный Client? |
| WebGL не подключается | Используется WebSocket/SimpleWeb path? |
| Пуля видна только серверу | Спавн идёт через `NetworkServer.Spawn`? Prefab зарегистрирован? |

---

## Первая схема владения

| Данные | Кто решает | Почему |
|--------|------------|--------|
| Нажата кнопка движения | Client | Это локальный input. |
| Позиция игрока в честном PvP | Server или server-validated client input | Иначе speedhack станет gameplay. |
| HP | Server | Клиент не должен назначать себе здоровье. |
| Текст ошибки покупки | Client показывает, server решает причину | UI отображает, но не решает. |
| Победитель | Server/backend | Это итог матча. |

---

## Минимальный маршрут

Для быстрого playable prototype:

1. `00_START_HERE`
2. `00a`
3. `01`
4. `02`
5. `03`
6. `04`
7. `06`
8. `09`
9. `12`
10. `15`
11. `22`

Потом возвращайтесь к остальным урокам по мере задач проекта.

---

## Когда можно идти дальше

- Host и отдельный Client подключаются.
- В логе есть `connectionId` для второго клиента.
- Player prefab появляется у обоих.
- Вы можете объяснить, почему `Host` не является достаточной проверкой.
- В `NETWORKING_DECISIONS.md` записаны Unity version, Mirror version, transport и способ запуска второго клиента.
