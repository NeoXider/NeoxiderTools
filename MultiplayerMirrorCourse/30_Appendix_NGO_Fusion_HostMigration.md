# Урок 30: лимиты Mirror, host migration, NGO и Fusion

**Навигация:** [Оглавление](README.md) · [Старт](00_START_HERE.md) · [Оформление](LESSON_STYLE.md) · продвинутый трек · урок 15/15 · Mirror `96.x`

| Ключевые слова | stack decision, host migration, dedicated, NGO, Fusion, rollback |
|----------------|------------------------------------------------------------------|

---

## Карта урока

| Что | Ответ |
|-----|-------|
| Объект работы | Netcode stack decision and plan B. |
| Кто владеет state | Architecture decision owns tradeoffs; gameplay should not be locked into callbacks. |
| Как проверить | Requirements table compares Mirror, dedicated, backend, migration needs. |
| Артефакт | `NETCODE_STACK_DECISION.md` with risks and rollback/Plan B. |

---

## Что должно получиться

Вы честно фиксируете, почему проект остаётся на Mirror или почему ему нужен другой стек/архитектура.

---

## Проблема

Нельзя выбирать сетевой стек по спору в интернете. Нужно сравнить требования игры с возможностями команды, бюджета, платформ и инфраструктуры.

---

## Mirror хорошо подходит, если

- нужна классическая client-server модель;
- команда хочет open-source стек;
- проект готов сам решать backend/hosting;
- нет жёсткого требования к автоматической host migration;
- команда понимает server authority.

---

## Когда стоит пересмотреть стек

| Требование | Вопрос |
|------------|--------|
| Бесшовная host migration | Есть ли готовое решение или нужен другой стек? |
| Очень строгий tick/prediction | Хватает ли Mirror и команды? |
| Полный managed backend | Не дешевле ли managed solution? |
| Cross-platform matchmaking | Кто поддерживает платформенные пути? |
| MMO scale | Есть ли прототип нагрузки? |

---

## Host migration

Для большинства Mirror-first проектов практичнее:

- dedicated server;
- быстрый reconnect;
- сохранение match state на backend;
- честный UI "host disconnected";
- смена архитектуры до релиза, если migration критична.

Не обещайте бесшовную host migration, если не построили и не протестировали её.

---

## Проверка себя

У вас есть таблица:

| Требование | Mirror закрывает? | Риск | План B |
|------------|-------------------|------|--------|

Если в `План B` пусто, решение ещё не принято.

---

## Минимальная диагностика

| Симптом | Что проверить |
|---------|---------------|
| Команда спорит "Mirror vs X" | Вернуться к requirements: genre, scale, authority, hosting, budget. |
| Host migration всплыла поздно | Решить dedicated/reconnect/backend save или смену стека до релиза. |
| Gameplay сложно перенести | Core logic слишком привязана к Mirror callbacks. |
| MMO обещан без теста | Нет load prototype, interest management и ops plan. |

---

## Частые ошибки

- Начинать MMO без прототипа нагрузки.
- Выбирать стек ради модного названия.
- Откладывать host migration до конца разработки.
- Привязать всё ядро игры напрямую к callbacks конкретного netcode.

---

## Лайфхаки

- Держите правила игры в чистом C# там, где можно.
- Сетевой слой должен быть адаптером, а не местом всей gameplay-логики.
- Прототипируйте самый рискованный сетевой сценарий первым.

---

## Профессиональный минимум

- Stack decision основан на требованиях, а не на популярности.
- Самый рискованный сценарий прототипируется до production content.
- Gameplay core максимально отделён от netcode callbacks.
- Plan B включает cost, migration work и deadline для решения.

---

## Домашнее задание

Соберите финальный пакет документов:

- `NETWORKING_DECISIONS.md`;
- `MOVEMENT_NET.md`;
- `ROOM_FLOW.md`;
- `SERVER_RUN.md`;
- `SMOKE_TESTS.md`;
- `MIRROR_UPGRADE.md`;
- `SECURITY_CHECKLIST.md`.

Добавьте страницу `NETCODE_STACK_DECISION.md` с таблицей требований и планом B.
