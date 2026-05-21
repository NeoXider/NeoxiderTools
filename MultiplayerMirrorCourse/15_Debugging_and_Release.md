# Урок 15: дебаг, плохая сеть и чек-лист релиза

**Навигация:** [Оглавление](README.md) · [Старт](00_START_HERE.md) · [Оформление](LESSON_STYLE.md) · базовый трек · урок 15/15 · Mirror `96.x`

| Ключевые слова | logs, latency simulation, release checklist, disconnect, smoke |
|----------------|----------------------------------------------------------------|

---

## Карта урока

| Что | Ответ |
|-----|-------|
| Объект работы | Release checklist, bad network tests, logs. |
| Кто владеет state | Server logs explain authoritative decisions; client logs explain view/input. |
| Как проверить | Один smoke сценарий на нормальной сети и один на плохой. |
| Артефакт | `SMOKE_TESTS.md` и release checklist. |

---

## Что должно получиться

У вас есть короткий release checklist для мультиплеера: что проверить перед тем, как дать билд тестерам.

---

## Проблема

Host в Editor работает - это не релизная проверка. Игроки будут терять пакеты, отключаться, иметь другие версии клиента и нажимать кнопки в неожиданные моменты.

---

## Минимальный чек-лист

| Проверка | Ожидаемый результат |
|----------|---------------------|
| Host + отдельный Client | Подключение, spawn, движение. |
| Dedicated + 2 Clients | Подключение по IP/port. |
| Disconnect во время матча | UI показывает причину, сервер чистит state. |
| Scene change | Оба клиента переходят без зависания. |
| 150 ms latency / 2% loss | Игра остаётся понятной. |
| Version mismatch | Клиент получает отказ до матча. |
| Server full | Понятный отказ. |
| Restart server | Клиент не зависает в loading. |

---

## Логи

Логируйте минимум:

- `connectionId`;
- account/session id, если есть;
- match id;
- scene;
- reason disconnect;
- transport error;
- version;
- build hash.

Не логируйте пароли, raw tokens и приватные данные.

---

## Практика

1. Сделайте `SMOKE_TESTS.md`.
2. Опишите 5 сценариев.
3. Прогоните их на плохой сети.
4. Исправьте 3 проблемы.
5. Добавьте regressions в checklist.

---

## Проверка себя

- Другой человек может повторить smoke по вашему документу.
- Disconnect не оставляет игрока в пустой сцене.
- Логи объясняют причину отказа.
- Плохая сеть проверена до релиза.

---

## Минимальная диагностика

| Симптом | Что проверить |
|---------|---------------|
| Баг "только у клиента" | Есть ли отдельный client log и `connectionId`? |
| После релиза сломался spawn | Smoke не покрывал dynamic prefab registration. |
| Warning игнорируется месяцами | Разделить harmless warnings и blockers в checklist. |
| Нельзя повторить баг | Нет seed/scenario/build version/server log. |

---

## Частые ошибки

- Проверять только Editor Host.
- Не проверять reconnect.
- Не иметь UI для ошибок.
- Считать warning Mirror "шумом" и игнорировать.
- Логировать слишком мало или слишком много секретов.

---

## Лайфхаки

- Сетевые баги чинятся быстрее, когда у каждого smoke-сценария есть expected logs.
- После изменения `NetworkManager`, scene loading, auth или transport прогоняйте smoke всегда.
- Храните "последний хороший build" для быстрого сравнения.

---

## Профессиональный минимум

- Smoke запускается перед каждым release build.
- Плохая сеть тестируется до релиза, не после жалоб игроков.
- Логи содержат build version, Mirror version, scene, transport.
- Known issues записаны явно, а не в голове разработчика.

---

## Домашнее задание

Создайте `SMOKE_TESTS.md` с 5 сценариями:

1. Connect/spawn/move.
2. Attack or interact.
3. Scene change.
4. Disconnect/reconnect.
5. Bad network.

Для каждого укажите входные условия, expected result и что смотреть в логах.
