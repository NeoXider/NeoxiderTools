# Урок 18: Steam, P2P и lobby metadata

**Навигация:** [Оглавление](README.md) · [Старт](00_START_HERE.md) · [Оформление](LESSON_STYLE.md) · продвинутый трек · урок 3/15 · Mirror `96.x`

| Ключевые слова | Steam lobby, P2P, transport, metadata, invite |
|----------------|-----------------------------------------------|

---

## Карта урока

| Что | Ответ |
|-----|-------|
| Объект работы | Steam lobby metadata, transport choice, invite/connect flow. |
| Кто владеет state | Steam помогает найти/соединить; Mirror server/host владеет gameplay. |
| Как проверить | Invite/join из двух Steam users или dev accounts, не только localhost. |
| Артефакт | Таблица metadata vs gameplay state. |

---

## Что должно получиться

Вы понимаете, что Steam lobby помогает найти матч и пригласить игроков, но не заменяет авторитетный state Mirror.

---

## Проблема

Lobby metadata удобно хранить, но его нельзя превращать в базу данных матча. HP, победа, инвентарь и результат боя должны жить на сервере/host-процессе Mirror.

---

## Что где хранить

| Данные | Где хранить |
|--------|-------------|
| Название комнаты | Steam lobby metadata. |
| Карта/режим/регион | Steam lobby metadata. |
| Количество мест | Steam lobby metadata. |
| HP игрока | Mirror server state. |
| Победитель | Mirror server state/backend. |
| Секреты/tokens | Не в lobby metadata. |

---

## Практика

1. Определите, какой Steam transport/plugin используете.
2. Проверьте поддержку вашей версии Unity и Mirror.
3. Опишите, какие поля попадут в lobby metadata.
4. Опишите, какие поля останутся на сервере.
5. Добавьте version check до входа в матч.

---

## Проверка себя

- Игрок находит lobby и подключается.
- Lobby metadata не содержит секретов.
- Изменение HP/победы не идёт через Steam metadata.
- Есть путь для non-Steam платформ, если они планируются.

---

## Минимальная диагностика

| Симптом | Что проверить |
|---------|---------------|
| Join нашёл lobby, но connect failed | Transport endpoint/P2P id/port и steam init order. |
| Игроки видят неверный режим | Lobby metadata устарела или не обновляется owner/server. |
| Crossplay невозможен | Steam transport/path завязан на Steam-only assumptions. |
| Gameplay можно изменить из lobby | Metadata содержит авторитетный state. |

---

## Частые ошибки

- Класть auth token в lobby.
- Считать Steam P2P одинаковым для всех платформ.
- Не проверять версию клиента до подключения.
- Смешивать friend invite и матчевый backend.

---

## Лайфхаки

- Lobby metadata должна быть короткой и публичной по смыслу.
- Для crossplay заранее отделите "как найти матч" от "как Mirror подключается".
- В `NETWORKING_DECISIONS.md` запишите, что зависит от Steam, а что нет.

---

## Профессиональный минимум

- Steam lobby используется для discovery/invite, не для game truth.
- Metadata ограничена публичной, безопасной информацией.
- Transport initialization order покрыт smoke-тестом.
- Crossplay decision зафиксирован до глубокой интеграции Steam-only flow.

---

## Домашнее задание

Сделайте таблицу:

| Поле | Steam lobby | Mirror state | Secret? |
|------|-------------|--------------|---------|

Заполните её для вашей игры.
