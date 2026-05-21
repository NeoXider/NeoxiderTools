# Урок 1: архитектура Mirror и первый NetworkManager

**Навигация:** [Оглавление](README.md) · [Старт](00_START_HERE.md) · [Оформление](LESSON_STYLE.md) · базовый трек · урок 1/15 · Mirror `96.x`

| Ключевые слова | `NetworkManager`, `Transport`, Host, Client, Server, KCP |
|----------------|-----------------------------------------------------------|

---

## Карта урока

| Что | Ответ |
|-----|-------|
| Объект работы | Сцена `NetSandbox`, GameObject `Network`, player prefab. |
| Кто владеет state | `NetworkManager` управляет сессией; transport переносит bytes; player state пока минимальный. |
| Как проверить | Host в Editor + отдельный Client через ParrelSync, Multiplayer Play Mode или build. |
| Артефакт | Лог connect/disconnect с `connectionId` и записанный transport. |

---

## Что должно получиться

В конце урока у вас есть тестовая сцена, где:

- Host запускается из Editor;
- отдельный Client подключается к Host;
- сервер логирует `connectionId` и адрес;
- player prefab появляется у подключившихся клиентов.

---

## Проблема

Новички часто начинают со "скрипта игрока", хотя сеть ещё не поднята. В результате непонятно, что сломалось: transport, prefab, сцена, authority или movement.

Сначала нужна минимальная сетевая сцена без игровой логики.

---

## Теория коротко

Mirror состоит из трёх базовых слоёв:

| Слой | Задача |
|------|--------|
| `Transport` | Соединение, отправка и получение байтов. |
| `NetworkManager` | Старт/стоп сервера и клиента, сцены, player prefab, callbacks. |
| `NetworkBehaviour` | Ваши сетевые скрипты: SyncVar, Command, Rpc. |

Transport выбирается под платформу:

| Transport | Когда начинать с него |
|-----------|----------------------|
| KCP | Desktop/mobile action, обычный старт для большинства проектов. |
| Telepathy | TCP-сценарии, простые пошаговые прототипы. |
| SimpleWeb | WebGL/WebSocket. |
| Multiplex | Нужно поддержать несколько transport одновременно. |

Важный ориентир из документации Mirror: `NetworkManager` должен быть единственным активным менеджером. Не вешайте на тот же GameObject `NetworkIdentity`.

---

## Практика

1. Создайте сцену `NetSandbox`.
2. Создайте пустой GameObject `Network`.
3. Добавьте `NetworkManager`.
4. Добавьте `KcpTransport`.
5. Для первого теста добавьте `NetworkManagerHUD`.
6. Создайте prefab `Player`.
7. На корень `Player` добавьте `NetworkIdentity`.
8. Назначьте `Player` в поле `NetworkManager.Player Prefab`.
9. Запустите Host.
10. Запустите второй Client через ParrelSync, Unity Multiplayer Play Mode или отдельный build.

Минимальный менеджер с логом:

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

Замените стандартный `NetworkManager` на наследника только после того, как обычный manager уже завёлся.

---

## Проверка себя

- В логе сервера видны разные `connectionId`.
- У каждого подключившегося клиента создан player object.
- Если закрыть Client, сервер пишет disconnect.
- Если удалить `Player Prefab`, ошибка становится понятной: игрок не спавнится.

---

## Минимальная диагностика

| Симптом | Что проверить |
|---------|---------------|
| Client не подключается | Адрес, port, transport на `NetworkManager`, firewall. |
| Host работает, Client нет | Запускался ли отдельный процесс, а не только Host? |
| Player не появился | `Player Prefab` назначен и имеет `NetworkIdentity` на root. |
| WebGL не подключается | Для браузера нужен WebSocket/SimpleWeb path, не KCP-only. |

---

## Частые ошибки

- На `Network` висит `NetworkIdentity`.
- Player prefab не назначен в `NetworkManager`.
- `NetworkIdentity` добавлен не на корень prefab.
- Тест идёт только в Host, отдельный Client ни разу не запускался.
- Для WebGL пытаются использовать KCP без SimpleWeb/Multiplex.

---

## Лайфхаки

- Держите `NetSandbox` отдельной сценой. Не тестируйте первую сеть на большом уровне.
- В логах всегда пишите `connectionId`, scene и transport.
- `NetworkManagerHUD` - временная учебная панель, а не UI для релиза.
- Для dedicated-сервера уже сейчас думайте, как он стартует без кнопок.

---

## Профессиональный минимум

- В сцене один активный `NetworkManager`.
- На GameObject с `NetworkManager` нет `NetworkIdentity`.
- Transport выбран под платформу и записан в `NETWORKING_DECISIONS.md`.
- Есть лог connect/disconnect с `connectionId`, scene и transport.

---

## Домашнее задание

Создайте `NETWORKING_DECISIONS.md` и запишите:

- выбранный transport;
- как вы запускаете второй client;
- имя тестовой сцены;
- где лежит player prefab;
- пример лога подключения двух клиентов.
