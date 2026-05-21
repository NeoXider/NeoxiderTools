# Урок 25: хостинг, Edgegap и облако

**Навигация:** [Оглавление](README.md) · [Старт](00_START_HERE.md) · [Оформление](LESSON_STYLE.md) · продвинутый трек · урок 10/15 · Mirror `96.x`

| Ключевые слова | Edgegap, orchestration, Docker, health, region, cost |
|----------------|------------------------------------------------------|

---

## Карта урока

| Что | Ответ |
|-----|-------|
| Объект работы | Hosting contract, Docker/server image, cloud deployment flow. |
| Кто владеет state | Orchestrator starts/stops process; Mirror server owns match; backend coordinates sessions. |
| Как проверить | Server image starts, logs READY, exposes mapped port, client connects. |
| Артефакт | `HOSTING_CONTRACT.md` with args/env/ports/readiness/shutdown. |

---

## Что должно получиться

Вы понимаете контракт между облаком и вашим server build: как процесс запускается, какой порт слушает, как сообщает readiness и как завершается.

---

## Проблема

Хостинг не знает правил вашей игры. Он запускает процесс. Если сервер ждёт UI-кнопку, не читает порт и не пишет health, оркестратор не сможет стабильно поднимать матчи.

---

## Контракт запуска

Документируйте:

| Пункт | Пример |
|-------|--------|
| Binary | `GameServer.x86_64` |
| Args | `-port 7777 -matchId abc -region eu` |
| Env | `BACKEND_URL`, `AUTH_AUDIENCE` |
| Protocol | UDP/TCP/WebSocket |
| Ready signal | log `READY port=7777` или health endpoint |
| Shutdown | перестать принимать игроков, сохранить итог, выйти |

---

## Edgegap / облачный flow

```text
backend requests deployment
orchestrator starts server
server logs READY
backend gives endpoint to clients
clients connect
match ends
server shuts down
orchestrator frees instance
```

Документация Edgegap для Mirror показывает тот же контракт: собрать Linux Dedicated Server, знать game port, открыть нужный TCP/UDP port, развернуть image и отдать клиенту host + external port из deployment summary/API.

---

## Проверка себя

- Server build стартует одной командой.
- Порт передаётся снаружи.
- Есть readiness.
- Есть graceful shutdown.
- Стоимость idle-instance понятна.

---

## Минимальная диагностика

| Симптом | Что проверить |
|---------|---------------|
| Deployment ready, client не подключается | External host/port mapping, protocol, transport port. |
| Container стартует и сразу падает | Missing libs, wrong executable path, no execute permission, bad args. |
| Сервер висит после матча | Нет shutdown condition или backend cleanup. |
| Cost растёт | Idle timeout, seat/session cleanup, overprovisioning. |

---

## Частые ошибки

- Хардкод порта и региона.
- Нет health/readiness.
- Секреты запечены в Docker image.
- Мультирегион включён до стабильного single region.
- Сервер не закрывается после матча.

---

## Лайфхаки

- Начинайте с одного региона.
- Тег server image связывайте с client build.
- Считайте стоимость пустых серверов.
- Документацию Edgegap/Mirror сверяйте под актуальную версию plugin.

---

## Профессиональный минимум

- Hosting contract не зависит от ручных действий в Unity Editor.
- Server image tag связан с client build/version.
- Secrets передаются через env/secret store, не запекаются в image.
- Readiness и shutdown проверены до деплоя в несколько регионов.

---

## Домашнее задание

Создайте `HOSTING_CONTRACT.md`:

- команда запуска;
- args/env;
- ports/protocols;
- readiness;
- shutdown;
- кто создаёт и кто удаляет server instance.
