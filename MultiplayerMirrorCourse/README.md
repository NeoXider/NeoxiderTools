# Mirror Networking: практический курс для Unity

Курс переписан как маршрут для человека, который впервые делает мультиплеер в Unity. Каждый урок отвечает на три вопроса:

- что должно заработать в игре;
- кто владеет данными: server, client, backend или UI;
- как проверить результат двумя процессами, а не только в Host.

Профессиональная планка курса: серверная правда, минимальная ручная магия, понятные логи, smoke-тесты, version notes и честное разделение между локальной версией пакета и актуальной документацией.

**Локальная база проекта:** Mirror `96.0.1` (`Assets/Mirror/version.txt`).

**Актуальная справка:** Mirror GitHub показывает релиз `96.10.0` от 2026-04-02. Курс ориентирован на линию `96.x`, но перед обновлением пакета всегда проходите [CHANGELOG_Course_Mirror96.md](CHANGELOG_Course_Mirror96.md) и [28_Mirror_Upgrade_Playbook.md](28_Mirror_Upgrade_Playbook.md).

**Главное правило:** если урок нельзя проверить отдельным Client или dedicated/server build, урок ещё не закрыт.

---

## Как проходить

1. Откройте [00_START_HERE.md](00_START_HERE.md) и соберите `NetSandbox`.
2. Пройдите короткий маршрут новичка: `00a -> 01 -> 02 -> 03 -> 04 -> 06 -> 09 -> 12 -> 15 -> 22`.
3. Вернитесь к пропущенным урокам `05`, `07`, `08`, `10`, `11`, `13`, `14`, когда базовый gameplay уже работает.
4. Уроки `16-30` проходите, когда проект выходит за localhost: интернет, auth, CI, logs, cloud, upgrade, security.
5. Если цель - RPG/Progression, после базового state/actions пройдите доменный маршрут `31-33`.

Каждый урок должен дать артефакт: сцену, кодовый прототип, таблицу решения, лог, smoke-сценарий или документ проекта. Если после урока ничего нельзя показать и проверить, урок не пройден.

Если застряли, сначала откройте [TROUBLESHOOTING.md](TROUBLESHOOTING.md). Он связывает типовые симптомы с уроками, куда нужно вернуться.

## Маршруты

| Цель | Уроки | Что получите |
|------|-------|--------------|
| Первый playable prototype | `00 -> 00a -> 01 -> 02 -> 03 -> 04 -> 06 -> 09 -> 12 -> 15 -> 22` | Connect, spawn, movement, HP, command, UI, smoke. |
| Кооператив без интернета | `01-15`, затем `22` | Локальный co-op test flow и release checklist. |
| PvP/action | `01-15`, затем `20`, `21`, `29` | Tick/movement decisions, animation state, anti-cheat checks. |
| RPG/Progression | `00-06`, затем `09`, `11`, `12`, `31-33`, `22`, `29` | Честный бой, XP, level rewards, perks, UI и smoke. |
| Игра через интернет | `16-19`, затем `23-25` | NAT/relay/backend/auth, CI, hosting contract. |
| Поддержка проекта | `24`, `28`, `29`, `30` | Logs, upgrade playbook, security, stack decision. |

---

## Правила, которые держат курс

| Правило | Практический смысл |
|---------|--------------------|
| Сервер владеет правдой | HP, валюта, победа, инвентарь и важная позиция меняются на сервере. |
| Клиент отправляет намерение | Клиент просит атаковать/купить/открыть, сервер проверяет. |
| UI не авторитетен | Canvas показывает state, но не решает gameplay. |
| `NetworkIdentity` обязателен | Сетевой объект должен быть spawned и иметь `netId != 0`. |
| Host не равен тесту | Всегда проверяйте отдельный Client и, позже, Dedicated. |
| Версия Mirror фиксируется | Старые туториалы часто не совпадают с вашим API. |
| Документация важнее привычки | Если API или inspector отличается, сверяйте installed package и docs. |
| Логи пишутся с первого дня | `connectionId`, scene, netId, command name и reject reason экономят часы. |

---

## Минимальный словарь

| Термин | Смысл без академичности |
|--------|-------------------------|
| Server | Процесс, который решает итог матча. |
| Client | Процесс игрока: ввод, UI, картинка, звук. |
| Host | Server и Client в одном процессе. Удобно, но маскирует ошибки. |
| Dedicated | Server без локального игрока и UI. Главный формат для честного публичного матча. |
| Authority | Право менять state. Важные данные почти всегда server-authoritative. |
| Observer | Клиент, которому Mirror отправляет объект. Не каждый клиент обязан видеть всё. |
| Spawn | Создание сетевого runtime instance через Mirror, а не обычный локальный `Instantiate`. |
| Sync | Передача state. Event и state не путать. |

---

## Уроки

| № | Файл | Результат |
|---|------|-----------|
| 00 | [00_START_HERE.md](00_START_HERE.md) | Среда, словарь, первый запуск. |
| 00a | [00a_Prerequisites_and_Networking_Basics.md](00a_Prerequisites_and_Networking_Basics.md) | Понимание client/server/authority. |
| 01 | [01_Mirror_Architecture.md](01_Mirror_Architecture.md) | `NetworkManager`, transport, подключение. |
| 02 | [02_NetworkIdentity_and_Spawning.md](02_NetworkIdentity_and_Spawning.md) | Spawn/destroy сетевых объектов. |
| 03 | [03_Movement_and_Physics.md](03_Movement_and_Physics.md) | Локальный ввод и синхронизация движения. |
| 04 | [04_State_Synchronization_SyncVars.md](04_State_Synchronization_SyncVars.md) | `SyncVar` server -> clients. |
| 05 | [05_Complex_Data_Structures.md](05_Complex_Data_Structures.md) | Sync-коллекции и ID вместо тяжёлых данных. |
| 06 | [06_RPC_and_Commands.md](06_RPC_and_Commands.md) | `Command`, `ClientRpc`, `TargetRpc`, validation. |
| 07 | [07_Interest_Management.md](07_Interest_Management.md) | Observers и уменьшение лишнего трафика. |
| 08 | [08_Matchmaking_and_Rooms.md](08_Matchmaking_and_Rooms.md) | Лобби, ready, room/game player. |
| 09 | [09_Server_Authority_AntiCheat.md](09_Server_Authority_AntiCheat.md) | Серверные проверки и rate limits. |
| 10 | [10_Lag_Compensation.md](10_Lag_Compensation.md) | Уровни lag compensation/prediction. |
| 11 | [11_ScriptableObjects_and_Network.md](11_ScriptableObjects_and_Network.md) | ScriptableObject как локальный каталог. |
| 12 | [12_Network_UI.md](12_Network_UI.md) | UI как View, не источник правды. |
| 13 | [13_Optimization_and_Profiling.md](13_Optimization_and_Profiling.md) | Замеры до/после оптимизации. |
| 14 | [14_Dedicated_Server.md](14_Dedicated_Server.md) | CLI/headless server запуск. |
| 15 | [15_Debugging_and_Release.md](15_Debugging_and_Release.md) | Release checklist и плохая сеть. |
| 16 | [16_NAT_Direct_Connect.md](16_NAT_Direct_Connect.md) | NAT, port forwarding, relay/dedicated выбор. |
| 17 | [17_Relay_and_Backends.md](17_Relay_and_Backends.md) | Backend/relay flow. |
| 18 | [18_Steam_Transports_P2P.md](18_Steam_Transports_P2P.md) | Steam lobby metadata vs Mirror state. |
| 19 | [19_Authentication_Sessions_Tokens.md](19_Authentication_Sessions_Tokens.md) | Auth, token, session, version checks. |
| 20 | [20_Network_Tick_FixedUpdate.md](20_Network_Tick_FixedUpdate.md) | Update/FixedUpdate/network tick. |
| 21 | [21_NetworkAnimator_Alternatives.md](21_NetworkAnimator_Alternatives.md) | NetworkAnimator или ручной state. |
| 22 | [22_Testing_ParrelSync_Smoke.md](22_Testing_ParrelSync_Smoke.md) | Smoke-сценарии для нескольких клиентов. |
| 23 | [23_CI_Headless_Builds.md](23_CI_Headless_Builds.md) | CI server build и artifact. |
| 24 | [24_Server_Ops_Logs_Metrics.md](24_Server_Ops_Logs_Metrics.md) | Structured logs, metrics, health. |
| 25 | [25_Hosting_Edgegap_Cloud.md](25_Hosting_Edgegap_Cloud.md) | Контракт запуска в облаке. |
| 26 | [26_Custom_NetworkMessages.md](26_Custom_NetworkMessages.md) | Custom protocol messages. |
| 27 | [27_Additive_Scenes_Subscenes.md](27_Additive_Scenes_Subscenes.md) | Additive scenes и переходы. |
| 28 | [28_Mirror_Upgrade_Playbook.md](28_Mirror_Upgrade_Playbook.md) | Playbook обновления Mirror. |
| 29 | [29_Network_Security_Checklist.md](29_Network_Security_Checklist.md) | Security checklist. |
| 30 | [30_Appendix_NGO_Fusion_HostMigration.md](30_Appendix_NGO_Fusion_HostMigration.md) | Решение по стеку и план B. |
| 31 | [31_RPG_Combat_Server_Authority.md](31_RPG_Combat_Server_Authority.md) | RPG combat без доверия клиенту. |
| 32 | [32_Progression_XP_Rewards.md](32_Progression_XP_Rewards.md) | XP, уровни, rewards и profile. |
| 33 | [33_RPG_Progression_Capstone.md](33_RPG_Progression_Capstone.md) | Итоговый RPG/Progression vertical slice. |

---

## Документы, которые появятся после курса

- `NETWORKING_DECISIONS.md`
- `MOVEMENT_NET.md`
- `ROOM_FLOW.md`
- `MATCH_FLOW.md`
- `AUTH_FLOW.md`
- `SERVER_RUN.md`
- `SMOKE_TESTS.md`
- `MIRROR_UPGRADE.md`
- `SECURITY_CHECKLIST.md`
- `NETCODE_STACK_DECISION.md`
- `SMOKE_RPG_PROGRESSION.md`
- `PROGRESSION_FLOW.md`

Внутренние учебные документы курса:

- [LESSON_STYLE.md](LESSON_STYLE.md)
- [CHANGELOG_Course_Mirror96.md](CHANGELOG_Course_Mirror96.md)
- [TROUBLESHOOTING.md](TROUBLESHOOTING.md)

---

## Основные источники

- [Mirror documentation](https://mirror-networking.gitbook.io/docs/)
- [Mirror GitHub Releases](https://github.com/MirrorNetworking/Mirror/releases)
- [Unity Multiplayer Play Mode package docs](https://docs.unity3d.com/Packages/com.unity.multiplayer.playmode@1.6/manual/index.html)
- [Unity Dedicated Server build documentation](https://docs.unity3d.com/Manual/dedicated-server-build.html)
- [Edgegap Mirror guide](https://docs.edgegap.com/docs/sample-projects/unity-netcodes/mirror-on-edgegap)
