# Урок 24: эксплуатация сервера, логи и метрики

**Навигация:** [Оглавление](README.md) · [Старт](00_START_HERE.md) · [Оформление](LESSON_STYLE.md) · продвинутый трек · урок 9/15 · Mirror `96.x`

| Ключевые слова | structured logs, metrics, health, disconnect reason, runbook |
|----------------|--------------------------------------------------------------|

---

## Карта урока

| Что | Ответ |
|-----|-------|
| Объект работы | Server logs, metrics, health/readiness, runbook. |
| Кто владеет state | Server emits authoritative events; ops layer observes them. |
| Как проверить | Reproduce connect/auth fail/disconnect and read cause from logs. |
| Артефакт | `SERVER_LOGS.md` with event schema and runbook. |

---

## Что должно получиться

Вы умеете понять по логам, почему игрок не подключился, вылетел или получил отказ. Без запуска Unity Editor.

---

## Проблема

Когда игра у тестера "не работает", простой `Debug.Log("error")` бесполезен. Нужны структурированные события с контекстом матча, соединения и версии.

---

## События, которые стоит логировать

| Event | Поля |
|-------|------|
| `server_ready` | version, port, region, scene. |
| `connect` | connectionId, address, version. |
| `auth_fail` | connectionId, reason, version. |
| `spawn_player` | connectionId, netId, account/session. |
| `disconnect` | connectionId, reason, duration. |
| `command_rate_limit` | connectionId, command, count. |
| `match_end` | matchId, duration, players, result. |

---

## Health

Хостингу полезно знать:

- процесс жив;
- server started;
- transport слушает порт;
- матч принимает игроков или закрыт;
- сколько игроков подключено.

Это можно делать через логи, lightweight HTTP endpoint или интеграцию провайдера.

---

## Проверка себя

- По disconnect-логу понятна причина.
- В логе нет raw token/password.
- Можно связать события одного игрока по session/account/connection.
- Есть runbook: что делать при росте disconnect/timeouts.

---

## Минимальная диагностика

| Симптом | Что проверить |
|---------|---------------|
| Игрок жалуется, а server log пуст | Логируется ли server-side connect/auth/spawn/disconnect? |
| События нельзя связать | Нет correlation id/match id/session id. |
| Alert бесполезен | Нет threshold и runbook action. |
| В логах секреты | Sanitization и запрет raw tokens/passwords. |

---

## Частые ошибки

- Логировать только на клиенте.
- Не писать build version.
- Писать секреты в logs.
- Не иметь match id.
- Alert без инструкции, что делать.

---

## Лайфхаки

- Структурированный JSON log легче парсить, чем свободный текст.
- Для dev можно логировать больше, для prod меньше и безопаснее.
- Счётчики rate limit помогают увидеть чит/DoS раньше жалоб.
- У каждого alert должен быть владелец и runbook.

---

## Профессиональный минимум

- Logs пригодны для расследования без Unity Editor.
- Metrics отделяют networking, gameplay и infrastructure symptoms.
- Health/readiness понятны хостингу.
- Sensitive data redaction проверяется отдельно.

---

## Домашнее задание

Опишите `SERVER_LOGS.md`:

- формат строки;
- список обязательных полей;
- 5 событий;
- что нельзя логировать;
- пример disconnect troubleshooting.
