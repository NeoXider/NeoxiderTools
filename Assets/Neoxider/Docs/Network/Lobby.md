# Лобби и LAN Discovery

**Что это:** Три NoCode компонента для организации лобби, ready-проверки и автоматического поиска серверов в LAN. Обёртки вокруг встроенных Mirror компонентов (`NetworkRoomManager`, `NetworkRoomPlayer`, `NetworkDiscovery`). Путь: `Scripts/Network/Lobby/`, пространство имён `Neo.Network`.

**Как использовать:**
1. Добавьте `NeoNetworkDiscovery` + `NetworkDiscovery` на объект менеджера — для LAN-обнаружения.
2. Замените `NeoNetworkManager` на `NeoLobbyManager` — если нужно лобби с ready-проверкой.
3. Создайте префаб с `NeoLobbyPlayer` + `NetworkIdentity` — как Room Player Prefab.
4. Подключите UI через UnityEvents (кнопки Host/Join/Ready).

---

## NeoNetworkDiscovery

**Что это:** `MonoBehaviour`, обёртка над Mirror `NetworkDiscovery`. Автоматически ищет серверы в LAN и предоставляет UnityEvents. Путь: `Scripts/Network/Lobby/NeoNetworkDiscovery.cs`.

### Поля

| Поле | Тип | Описание |
|------|-----|----------|
| `_autoAdvertiseOnHost` | `bool` | Автоматически начать рекламу при хостинге (по умолчанию true) |
| `_autoDiscoverOnClient` | `bool` | Автоматически искать серверы (по умолчанию true) |
| `_refreshInterval` | `float` | Интервал обновления списка серверов (секунды) |

### Методы

| Метод | Возврат | Описание |
|-------|---------|----------|
| `StartAdvertising()` | `void` | Начать рекламу сервера в LAN. Вызовите после `StartHost()` |
| `StartDiscovery()` | `void` | Начать поиск серверов |
| `StopDiscovery()` | `void` | Остановить поиск |
| `ConnectToServer(string)` | `void` | Подключиться к серверу по адресу |
| `ConnectToFirstServer()` | `void` | Подключиться к первому найденному серверу |

### События

| Событие | Тип | Описание |
|---------|-----|----------|
| `OnServerFound` | `UnityEvent<string>` | Найден новый сервер (string = IP адрес) |
| `OnServerListUpdated` | `UnityEvent<int>` | Обновлён список серверов (int = кол-во) |
| `OnAdvertisingStarted` | `UnityEvent` | Хост начал рекламировать себя |
| `OnDiscoveryStarted` | `UnityEvent` | Клиент начал поиск |

### Свойства

| Свойство | Тип | Описание |
|----------|-----|----------|
| `ServerCount` | `int` | Количество найденных серверов |
| `DiscoveredServers` | `IReadOnlyDictionary` | Словарь найденных серверов |

---

## NeoLobbyManager

**Что это:** `NetworkRoomManager` наследник. Управляет лобби с ready-проверкой и переходом между Room-сценой и Game-сценой. Путь: `Scripts/Network/Lobby/NeoLobbyManager.cs`.

### Поля

| Поле | Тип | Описание |
|------|-----|----------|
| `_minPlayersToStart` | `int` | Минимум игроков для начала игры (по умолчанию 1) |

### Методы

| Метод | Возврат | Описание |
|-------|---------|----------|
| `HostLobby()` | `void` | Создать лобби (Host). Привяжите к кнопке |
| `JoinLobby(string)` | `void` | Подключиться к лобби по адресу |
| `LeaveLobby()` | `void` | Выйти из лобби / остановить хост |
| `CanStartGame()` | `bool` | Возвращает `true` если достаточно игроков и все готовы |

### События

| Событие | Тип | Описание |
|---------|-----|----------|
| `OnPlayerJoinedRoom` | `UnityEvent<NetworkConnectionToClient>` | Игрок зашёл в комнату |
| `OnPlayerLeftRoom` | `UnityEvent<NetworkConnectionToClient>` | Игрок вышел |
| `OnAllPlayersReady` | `UnityEvent` | Все игроки готовы, игра начинается |
| `OnGameSceneLoaded` | `UnityEvent` | Игровая сцена загружена |
| `OnReturnedToLobby` | `UnityEvent` | Возврат в лобби |
| `OnPlayerCountChanged` | `UnityEvent<int>` | Изменилось кол-во игроков |
| `OnPlayerConnectedInfo` | `UnityEvent<string>` | Информация о подключившемся |

### Свойства

| Свойство | Тип | Описание |
|----------|-----|----------|
| `PlayerCount` | `int` | Текущее кол-во игроков |
| `AllReady` | `bool` | Все ли готовы |

---

## NeoLobbyPlayer

**Что это:** `NetworkRoomPlayer` наследник. Игрок в лобби с NoCode готовностью и UnityEvents. Путь: `Scripts/Network/Lobby/NeoLobbyPlayer.cs`.

### Методы

| Метод | Возврат | Описание |
|-------|---------|----------|
| `ToggleReady()` | `void` | Переключить готовность. Привяжите к кнопке |
| `SetReady(bool)` | `void` | Установить готовность явно |

### События

| Событие | Тип | Описание |
|---------|-----|----------|
| `OnReadyChanged` | `UnityEvent<bool>` | Изменилось состояние готовности |
| `OnBecameLocalPlayer` | `UnityEvent` | Этот объект стал локальным игроком |
| `OnGameSceneReady` | `UnityEvent` | Игровая сцена готова |

### Свойства

| Свойство | Тип | Описание |
|----------|-----|----------|
| `IsLocal` | `bool` | Это локальный игрок? |
| `IsReady` | `bool` | Этот игрок готов? |
| `ConnectionId` | `int` | ID подключения |

---

## Пример: LAN Party Game

```
Scene: MainMenu
├── NeoNetworkDiscovery
│   ├── OnServerFound → ServerList.AddEntry()
│   └── Auto Discover On Client = true
├── Button "Host" → NeoLobbyManager.HostLobby()
└── Button "Join" → NeoNetworkDiscovery.ConnectToFirstServer()

Scene: Lobby (Room Scene)
├── NeoLobbyManager (Gameplay Scene = "Game", Min Players = 2)
├── For each player: NeoLobbyPlayer prefab
│   ├── Button "Ready" → ToggleReady()
│   └── OnReadyChanged → UpdatePlayerCard()
└── NeoLobbyManager.OnAllPlayersReady → show "Starting..."

Scene: Game (Gameplay Scene)
└── Standard gameplay with NeoNetworkPlayer
```

## См. также
- [Multiplayer Guide](Multiplayer_Guide.md) — основной гайд
- [NeoNetworkManager](NeoNetworkManager.md) — базовый менеджер (без лобби)
- [NoCode Network Spec](NoCode_Network_Spec.md) — Правило 10
