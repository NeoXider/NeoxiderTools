# Урок 17: Relay и backend матча

**Навигация:** [Оглавление](README.md) · [Старт](00_START_HERE.md) · [Оформление](LESSON_STYLE.md) · продвинутый трек · урок 2/15 · Mirror `96.x`

| Ключевые слова | relay, backend, match ticket, endpoint, token |
|----------------|-----------------------------------------------|

---

## Карта урока

| Что | Ответ |
|-----|-------|
| Объект работы | Match flow between client, backend, relay/orchestrator and Mirror server. |
| Кто владеет state | Backend владеет matchmaking/session; Mirror server владеет gameplay. |
| Как проверить | Sequence diagram + failure paths до интеграции SDK. |
| Артефакт | `MATCH_FLOW.md` с endpoint/token lifecycle. |

---

## Что должно получиться

Вы разделяете две задачи: backend решает, кто и куда подключается; relay помогает пакетам пройти через сеть.

---

## Проблема

Без backend/relay игра часто работает только в локальной сети или при ручном IP. Для нормального matchmaking клиенту нужен адрес сервера или временный token, а не поле "введите IP друга".

---

## Слои

| Слой | Отвечает за |
|------|-------------|
| Game client | Логин, запрос матча, подключение. |
| Backend | Auth, подбор матча, выдача endpoint/token. |
| Relay | Сетевой проход между сторонами. |
| Mirror server | Игровой state и validation. |

---

## Типовой flow

```text
client -> backend: хочу матч
backend -> orchestrator/relay: создать/найти endpoint
backend -> client: endpoint/token
client -> Mirror transport: connect
server -> backend/auth: проверить игрока
server: spawn player
```

---

## Практика

Сначала опишите интеграцию без кода:

1. Как клиент получает auth token.
2. Как backend создаёт матч.
3. Что выдаётся клиенту: IP/port, relay token или lobby id.
4. Как server проверяет подключение.
5. Что происходит при недоступном регионе.

---

## Проверка себя

- Клиент не выбирает "честный сервер" сам.
- Token имеет срок жизни.
- Ошибка relay/backend отображается в UI.
- Сервер всё равно валидирует игрока после подключения.

---

## Минимальная диагностика

| Симптом | Что проверить |
|---------|---------------|
| Игрок подключается не туда | Backend endpoint, region, match id, expired token. |
| Secret утёк | Secret лежит в client build, а должен быть только server/backend. |
| Relay работает, чит всё равно возможен | Relay не валидирует gameplay state. |
| Нельзя расследовать матч | В логах нет match id, account id, endpoint. |

---

## Частые ошибки

- Хранить relay/backend secret в клиенте.
- Считать relay античитом.
- Не иметь fallback при недоступном регионе.
- Не логировать match id и endpoint.

---

## Лайфхаки

- На старте выберите один регион и один provider.
- Token делайте короткоживущим и привязанным к match/account.
- Сначала нарисуйте sequence diagram, потом пишите код.

---

## Профессиональный минимум

- Token короткоживущий и привязан к account/match.
- Backend не заменяет server-side validation.
- Failure paths описаны до UI реализации.
- Все стороны логируют общий correlation id/match id.

---

## Домашнее задание

Создайте `MATCH_FLOW.md`:

- login;
- request match;
- receive endpoint/token;
- connect;
- server auth;
- disconnect/failure paths.
