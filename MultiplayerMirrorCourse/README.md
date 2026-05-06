# 🚀 Продвинутый курс: Mirror Networking (от новичка до профессионала)

Добро пожаловать в структурированный и глубокий курс по созданию мультиплеерных игр на базе Unity и **Mirror Networking**! 🌐 Этот курс создан для тех, кто хочет не просто «подключить два кубика по сети», а готов строить надежные, безопасные и масштабируемые сетевые проекты.

Курс состоит из **вводного модуля**, **15 базовых уроков** (фундамент) и **15 продвинутых уроков** (реальный продакшн). Каждая тема вынесена в отдельный файл для удобства навигации и изучения. 📚

**📌 Ориентир по технологиям:** 
* **Mirror:** v96.x
* **Unity:** LTS / Unity 6 
*(Важно: тексты курса актуальны для архитектуры 2026 года. Ссылка на GitBook `…/changelog/2025-change-log` — штатное имя раздела у Mirror, а не устаревший год).*

**⚙️ Важные стандарты и документы:**
- 🎨 **Оформление уроков:** [LESSON_STYLE.md](LESSON_STYLE.md) 
- 🔄 **Соответствие версии v96:** [CHANGELOG_Course_Mirror96.md](CHANGELOG_Course_Mirror96.md) 
- 🗺️ **План блоков:** [00_Course_Plan.md](00_Course_Plan.md)

---

## 🛠️ Эталон версий и документация

Для успешного прохождения курса и работы над своим проектом всегда держите под рукой официальную документацию:

- 📖 [Changelog Mirror (GitBook)](https://mirror-networking.gitbook.io/docs/manual/general/changelog/2025-change-log) — крупный свод для **v96.0.1**. *(Для работы в 2026 году обязательно сверяйтесь с GitHub).*
- 📦 [Релизы GitHub](https://github.com/MirrorNetworking/Mirror/releases) — ежемесячные миноры (`v96.9.x`, `v96.10.x` и т.д.). Исправления появляются здесь раньше, чем в GitBook.
- 📋 Наша сводка для курса: [CHANGELOG_Course_Mirror96.md](CHANGELOG_Course_Mirror96.md) — не забывайте обновлять этот файл при поднятии версии пакета в вашем проекте!
- 💡 **Лайфхак:** Если пример кода не компилируется — ищите примеры в репозитории Mirror строго **на теге вашей версии** пакета.

---

## 🏗️ Как устроены уроки

Мы ценим ваше время, поэтому каждый урок имеет строгую структуру без лишней воды. Всё направлено на решение практических задач! 🎯

| Сегмент | Структура |
|---------|-----------|
| **00a** | 🏁 Вводный модуль. Блок «Проблема» предшествует теории. |
| **1–2** | 🛠️ Базовый трек. Короткий и емкий формат для быстрого старта. |
| **3–14** | 🧠 Базовый трек (основной). **Строгий стандарт:** Цели → Проблема → Теория → Практика → Ошибки → Советы → ДЗ ([LESSON_STYLE](LESSON_STYLE.md)). |
| **15** | 🚀 Базовый трек (финал). Чек-лист перед релизом + итоговое ДЗ. |
| **16–29** | ⚙️ Продвинутый трек. Тот же строгий шаблон, но темы посвящены узким **продакшн-задачам** (DevOps, античит, оптимизация). |
| **30** | 🔮 Продвинутый трек (приложение). Честный разбор лимитов Mirror, архитектурная миграция, сравнение с NGO/Fusion + итоговое ДЗ. |

---

## 📖 Оглавление: Вводный и Базовый трек (Уроки 1–15)

Здесь закладывается фундамент. Вы поймете архитектуру, научитесь синхронизировать данные и управлять состояниями. 🧱

| № | Файл | Тема |
|---|------|------|
| 00a | [00a_Prerequisites_and_Networking_Basics.md](00a_Prerequisites_and_Networking_Basics.md) | 🔌 Предпосылки: C#, TCP/UDP, клиент–сервер |
| 1 | [01_Mirror_Architecture.md](01_Mirror_Architecture.md) | 🏛️ Архитектура, транспорты, топологии |
| 2 | [02_NetworkIdentity_and_Spawning.md](02_NetworkIdentity_and_Spawning.md) | 👻 NetworkIdentity, спавн, authority |
| 3 | [03_Movement_and_Physics.md](03_Movement_and_Physics.md) | 🏃‍♂️ NetworkTransform, движение, физика |
| 4 | [04_State_Synchronization_SyncVars.md](04_State_Synchronization_SyncVars.md) | 🔄 State Synchronization: SyncVar, hooks |
| 5 | [05_Complex_Data_Structures.md](05_Complex_Data_Structures.md) | 🗃️ Сложные структуры: SyncList, SyncDictionary |
| 6 | [06_RPC_and_Commands.md](06_RPC_and_Commands.md) | ⚡ Удаленные вызовы: Command, ClientRpc, TargetRpc |
| 7 | [07_Interest_Management.md](07_Interest_Management.md) | 👁️ Interest Management (управление видимостью) |
| 8 | [08_Matchmaking_and_Rooms.md](08_Matchmaking_and_Rooms.md) | 🎮 NetworkRoomManager, лобби и матчмейкинг |
| 9 | [09_Server_Authority_AntiCheat.md](09_Server_Authority_AntiCheat.md) | 🛡️ Серверный авторитет и античит-мышление |
| 10 | [10_Lag_Compensation.md](10_Lag_Compensation.md) | ⏱️ Lag compensation и client prediction |
| 11 | [11_ScriptableObjects_and_Network.md](11_ScriptableObjects_and_Network.md) | 📜 ScriptableObjects и сетевые каталоги |
| 12 | [12_Network_UI.md](12_Network_UI.md) | 🖥️ Сетевой UI и паттерн MVC |
| 13 | [13_Optimization_and_Profiling.md](13_Optimization_and_Profiling.md) | 📈 Оптимизация сети и профилирование трафика |
| 14 | [14_Dedicated_Server.md](14_Dedicated_Server.md) | 🖥️ Выделенный сервер (Headless / batchmode) |
| 15 | [15_Debugging_and_Release.md](15_Debugging_and_Release.md) | 🛠️ Дебаг, симуляция сети и чек-лист релиза |

---

## 🚀 Оглавление: Продвинутый трек (Уроки 16–30)

Материал для тех, кто готовит игру к реальному релизу в Steam или мобильных сторах. Безопасность, CI/CD, облачный хостинг и кастомная сериализация. 🌍

| № | Файл | Тема |
|---|------|------|
| 16 | [16_NAT_Direct_Connect.md](16_NAT_Direct_Connect.md) | 🖧 NAT, прямое подключение и проброс портов |
| 17 | [17_Relay_and_Backends.md](17_Relay_and_Backends.md) | 🔄 Relay, бэкенд матча и сигнальные серверы |
| 18 | [18_Steam_Transports_P2P.md](18_Steam_Transports_P2P.md) | 🚂 Steamworks, FizzySteamworks, P2P и Lobby |
| 19 | [19_Authentication_Sessions_Tokens.md](19_Authentication_Sessions_Tokens.md) | 🔐 Аутентификация, токены и безопасность сессий |
| 20 | [20_Network_Tick_FixedUpdate.md](20_Network_Tick_FixedUpdate.md) | ⏱️ Сетевой тик, FixedUpdate и send rate |
| 21 | [21_NetworkAnimator_Alternatives.md](21_NetworkAnimator_Alternatives.md) | 🏃 NetworkAnimator и ручные альтернативы |
| 22 | [22_Testing_ParrelSync_Smoke.md](22_Testing_ParrelSync_Smoke.md) | 🧪 Тестирование мультиплеера с ParrelSync |
| 23 | [23_CI_Headless_Builds.md](23_CI_Headless_Builds.md) | 🤖 CI/CD пайплайны и автоматические headless-сборки |
| 24 | [24_Server_Ops_Logs_Metrics.md](24_Server_Ops_Logs_Metrics.md) | 📊 Эксплуатация: логи, метрики, persistence БД |
| 25 | [25_Hosting_Edgegap_Cloud.md](25_Hosting_Edgegap_Cloud.md) | ☁️ Хостинг серверов (Edgegap, оркестрация, AWS) |
| 26 | [26_Custom_NetworkMessages.md](26_Custom_NetworkMessages.md) | 📨 Кастомные NetworkMessage и сериализация |
| 27 | [27_Additive_Scenes_Subscenes.md](27_Additive_Scenes_Subscenes.md) | 🗺️ Additive-сцены, подзоны и порталы |
| 28 | [28_Mirror_Upgrade_Playbook.md](28_Mirror_Upgrade_Playbook.md) | 🔄 Безопасное обновление версии Mirror (Playbook) |
| 29 | [29_Network_Security_Checklist.md](29_Network_Security_Checklist.md) | 🛡️ Кибербезопасность: DDoS, rate limits, секреты |
| 30 | [30_Appendix_NGO_Fusion_HostMigration.md](30_Appendix_NGO_Fusion_HostMigration.md) | ⚖️ Приложение: Лимиты Mirror, NGO, Fusion, Host Migration |

---
*«Хороший сетевой код невидим для игрока. Поехали!»* ✨
