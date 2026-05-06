# Урок 1: Архитектура Mirror, транспорты и базовая настройка

---

**Навигация:** [Оглавление](README.md) · [Оформление](LESSON_STYLE.md) · **Базовый трек** · урок **1/15** · Mirror **v96.x**

| Ключевые слова | `NetworkManager`, `Transport`, KCP, Host, Dedicated |
|----------------|------------------------------------------------------|

---

## Цели

Понять, зачем в проекте отдельный **транспорт** и **сервер**, выбрать транспорт под платформу, поднять рабочую сцену с `NetworkManager` и написать **кастомный** менеджер с логированием подключений.

---

## Проблема и контекст

| Без ясной архитектуры | Что происходит |
|----------------------|----------------|
| Смешение «кто сервер» в коде | Дубли логики, невозможно отладить host vs client |
| Неверный транспорт для WebGL | Клиенты не подключаются вообще |
| Нет кастомного `NetworkManager` | Нельзя отклонить клиента, выдать токен, залогировать `connectionId` |

Mirror всегда опирается на модель **клиент — сервер**: состояние мира должен вести сервер (или роль сервера на хосте).

---

## Теория: экосистема и топологии

### Почему Mirror

- **UNET** снят с поддержки.
- **NGO** — официальный Netcode for GameObjects; другая модель и объём.
- **Mirror** — зрелый open-source форк идей UNET, предсказуемый для классического server authority.

### Три роли инстанса

1. **Dedicated server** — только сервер, без локального игрока; лучший контроль честности при правильном коде.
2. **Client** — отправляет ввод, отображает состояние.
3. **Host** — сервер и локальный клиент в одном процессе; удобно для коопа, хуже как «честный» арбитр для соревновательного PvP.

### Транспорты

| Транспорт | Когда |
|-----------|--------|
| **KCP** (UDP + надёжность) | Экшены, шутеры по умолчанию |
| **Telepathy** (TCP) | Пошаговые игры, меньше потерь, больше задержек при потере пакета |
| **SimpleWeb** | WebGL / WebSockets |
| **Multiplex** | Несколько транспортов на одном сервере (например KCP + Web) |
| **Encryption** (отдельный пакет/компонент в составе Mirror) | TLS-подобное шифрование TCP; в **v96** реализует **`PortTransport`**, логирует статус аппаратного ускорения; зависимости (Bouncy Castle) лежат рядом с транспортом — не тащите дубликаты в проект |
| **SimpleWeb** (доп. фиксы после v96.0.1) | WebGL / WebSockets: на линии **96.9.x** улучшены аллокации, совместимость **WebAssembly 2023**, меньше шума в консоли на WebGL при unreliable-сообщениях |

Подробная карта изменений по версиям: [CHANGELOG_Course_Mirror96.md](CHANGELOG_Course_Mirror96.md).

---

## Практика

### Шаг 1. Сцена

1. Установите Mirror (Package Manager > Add from git URL: `https://github.com/MirrorNetworking/Mirror.git`).
2. Пустая сцена, объект `NetworkManager`, компоненты **Network Manager** + **Kcp Transport** + **NetworkManagerHUD** для тестов.
3. Включите **Don't Destroy On Load**, при необходимости **Run in Background**.

### Шаг 2. Кастомный менеджер

Создайте `MyGameNetworkManager.cs`:

```csharp
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class MyGameNetworkManager : NetworkManager
{
    public readonly List<NetworkConnectionToClient> ConnectedPlayers = new();

    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        base.OnServerConnect(conn);
        Debug.Log($"[Server] connect id={conn.connectionId} addr={conn.address}");
        ConnectedPlayers.Add(conn);
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        ConnectedPlayers.Remove(conn);
        base.OnServerDisconnect(conn);
    }
}
```

Замените компонент `NetworkManager` на сцене на ваш класс (или наследуйте и переопределяйте точечно).

> **Важно (v96):** уточняйте API в вашей версии: флаг приёма соединений задаётся через **`listen`** (не используйте устаревшее имя `dontListen`).

---

## Типичные ошибки

- Забыли **Player Prefab** или `NetworkIdentity` на префабе игрока — клиент «пустой».
- В `OnServerAddPlayer` переопределили логику и **не вызвали** `base.OnServerAddPlayer(conn)` — игрок не спавнится.
- Тестируют только **Host** и удивляются багам **только у клиента** — держите второй инстанс (см. советы).

---

## Советы и паттерны

- **ParrelSync** — второе окно Unity на клоне проекта: Host в одном, Client в другом ([репозиторий](https://github.com/VeriorPies/ParrelSync)).
- Вешайте **Network Messages** (диагностика) в тестовых билдах для разбора трафика.
- Планируете **Dedicated** — закладывайте `#if UNITY_SERVER` для отключения тяжёлого UI на сервере (урок 14).

---

## Домашнее задание

1. Реализуйте `OnServerConnect` с отказом по условию (например заглушка «бан по IP» с `conn.Disconnect()`).
2. Залогируйте `connectionId` и `address` для двух клиентов через ParrelSync или второй билд.

**Готово, если:** в консоли сервера видны два разных `connectionId`, клиенты спавнят игрока.

