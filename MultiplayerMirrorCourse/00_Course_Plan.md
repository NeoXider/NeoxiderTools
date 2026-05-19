# План курса Mirror

Курс построен блоками. Каждый блок закрывает конкретный риск мультиплеерной игры: первый connect, state, действия игрока, честность, UI, dedicated, интернет, эксплуатация и выбор стека.

---

## Как учиться без каши

Не проходите курс как энциклопедию. В мультиплеере слишком легко прочитать 20 тем и не собрать ни одного работающего соединения.

Правильный цикл:

1. Собрать минимальный рабочий пример.
2. Проверить Host + отдельный Client.
3. Записать вывод в документ проекта.
4. Только потом усложнять механику.

После каждого урока должен остаться один из артефактов: сцена, prefab, script, лог, таблица решений, smoke checklist или build command.

---

## Быстрый маршрут новичка

Если нужен рабочий прототип за короткое время:

`00 -> 00a -> 01 -> 02 -> 03 -> 04 -> 06 -> 09 -> 12 -> 15 -> 22`

Этот путь даёт connect, spawn, movement, state, commands, server validation, UI и smoke tests.

Что сознательно пропущено в быстром маршруте:

| Пропуск | Почему можно позже |
|---------|--------------------|
| `05` Sync-коллекции | Сначала достаточно одного HP/score через `SyncVar`. |
| `07` Interest Management | Нужен, когда объектов/игроков становится много или важна скрытая информация. |
| `08` Rooms | Сначала проще подключаться напрямую к sandbox. |
| `10` Lag compensation | Не нужен, пока нет action/PvP требований. |
| `13-14` Dedicated/profiling | Сначала докажите gameplay flow, потом оптимизируйте и выносите сервер. |

---

## Полный маршрут

| Блок | Уроки | Результат |
|------|-------|-----------|
| Вход | 00, 00a | Термины, среда, первый запуск. |
| Фундамент | 01-02 | `NetworkManager`, transport, spawn. |
| State/actions | 03-06 | Movement, SyncVar, Sync-коллекции, RPC/Commands. |
| Сессии | 07-08 | Interest Management, lobby/room flow. |
| Честность | 09-10 | Server validation, lag compensation decisions. |
| Клиент | 11-12 | Catalog IDs, UI model/view split. |
| Релизная база | 13-15 | Profiling, dedicated, release checklist. |
| Интернет | 16-19 | NAT, relay/backend, Steam, auth. |
| Продакшн | 20-25 | Tick, animation, smoke, CI, logs, hosting. |
| Зрелость | 26-30 | Protocol, scenes, upgrade, security, stack decision. |

---

## Как понять, что блок закрыт

| Блок | Проверяемый артефакт |
|------|----------------------|
| Вход | `NetSandbox`, Host + Client. |
| Фундамент | Spawn/destroy сетевого prefab. |
| State/actions | HP, inventory, purchase command. |
| Сессии | `ROOM_FLOW.md`. |
| Честность | Таблица validation/rate limits. |
| Клиент | HUD без авторитетного state. |
| Релизная база | `SMOKE_TESTS.md`, `SERVER_RUN.md`. |
| Интернет | `MATCH_FLOW.md`, `AUTH_FLOW.md`. |
| Продакшн | CI artifact, logs, hosting contract. |
| Зрелость | Upgrade/security/stack decision docs. |

---

## Контроль перед переходом к следующему блоку

Перед следующим блоком ответьте письменно:

- какой объект менялся;
- кто владеет state;
- какая команда/SyncVar/RPC использована;
- какой лог доказывает, что работал отдельный Client;
- что сломается, если клиент начнёт спамить действие.

Если ответы расплывчатые, блок не закрыт.

---

## Рекомендуемый календарь

| Неделя | Уроки | Фокус |
|--------|-------|-------|
| 1 | 00-02 | Первый connect и spawn. |
| 2 | 03-06 | Movement, SyncVar, Commands. |
| 3 | 07-10 | Visibility, lobby, server validation, latency. |
| 4 | 11-15 | UI, profiling, dedicated, release checklist. |
| 5 | 16-19 | Интернет-подключение и auth. |
| 6 | 20-24 | Tick, animation, testing, CI, logs. |
| 7 | 25-30 | Hosting, protocol, scenes, upgrade, security, stack. |

---

## Контрольные вопросы

1. Где живёт правда для HP, валюты и победы?
2. Какие `Command` есть в проекте и что они проверяют?
3. Как проверить игру отдельным Client?
4. Как запустить dedicated server без UI?
5. Что произойдёт при disconnect во время loading?
6. Как обновить Mirror и откатиться?
7. Что будет, если игрок за CGNAT?
8. Где хранятся секреты?
9. Какие документы уже появились после курса?

---

## Когда не углубляться

| Ситуация | Что делать |
|----------|------------|
| Не работает первый Client | Не читать prediction. Вернуться к `00-02`. |
| `Command` не вызывается | Проверить ownership и `NetworkIdentity`, не менять transport. |
| HP не синхронизируется | Проверить, что state меняется на сервере и объект spawned. |
| Плохо выглядит движение | Сначала измерить latency/loss, потом выбирать interpolation/prediction. |
| WebGL не подключается | Проверить transport: KCP не подходит для браузерного клиента. |
| Dedicated не стартует | Убрать зависимость от HUD и добавить CLI bootstrap. |
