# Урок 27: additive-сцены, подзоны и порталы

**Навигация:** [Оглавление](README.md) · [Старт](00_START_HERE.md) · [Оформление](LESSON_STYLE.md) · продвинутый трек · урок 12/15 · Mirror `96.x`

| Ключевые слова | additive scene, portal, scene interest, loading, subscene |
|----------------|-----------------------------------------------------------|

---

## Карта урока

| Что | Ответ |
|-----|-------|
| Объект работы | Scene graph, portals, loading flow, persistent network objects. |
| Кто владеет state | Server controls transitions; clients load/display scenes. |
| Как проверить | Two clients transition, one slow/disconnect case handled. |
| Артефакт | `SCENE_FLOW.md` with transitions and failure policy. |

---

## Что должно получиться

Вы умеете описать, кто загружает сцены, где живёт player object и как не потерять state при переходе между зонами.

---

## Проблема

В singleplayer можно вызвать `LoadScene`. В мультиплеере смена сцены должна быть согласована с сервером, spawned objects, observers и состоянием клиента.

---

## Базовая схема

| Часть | Роль |
|-------|------|
| Bootstrap scene | NetworkManager, сервисы, persistent objects. |
| Lobby scene | UI/room state. |
| Gameplay additive scenes | Уровни, зоны, подземелья. |
| Portal | Серверно проверенный переход. |

Сервер должен решать, когда игрок может перейти.

---

## Практика

1. Нарисуйте граф сцен.
2. Отметьте, где находится `NetworkManager`.
3. Отметьте, какие объекты persistent.
4. Для каждого перехода укажите initiator: server/client/backend.
5. Для portal укажите validation: дистанция, state, cooldown, match phase.

---

## Проверка себя

- Медленный клиент не отправляет gameplay commands до готовности сцены.
- Player state не теряется при выгрузке subscene.
- Observers корректно меняются после перехода.
- Disconnect во время loading обработан.

---

## Минимальная диагностика

| Симптом | Что проверить |
|---------|---------------|
| Client завис в loading | Timeout, ready signal, scene in build, async load result. |
| Player потерял state | State лежал в выгружаемой scene object. |
| Другие видят игрока в старой зоне | Interest/scene membership не обновлены server-side. |
| Portal abuse | Client инициирует transition без server validation. |

---

## Частые ошибки

- Обычный `LoadScene` ломает сетевой flow.
- Сценовый объект хранит важный state и выгружается.
- Нет loading timeout.
- Portal принимает клиента без серверной проверки.

---

## Лайфхаки

- Bootstrap-сцену держите маленькой и стабильной.
- Для зон используйте Interest Management вместе со сценовой логикой.
- Сначала сделайте один портал, потом масштабируйте.
- Любой переход добавляйте в smoke tests.

---

## Профессиональный минимум

- Scene transitions имеют server authority и failure path.
- Persistent state не зависит от выгружаемой additive scene.
- Loading state блокирует недопустимые commands.
- Scene flow покрыт smoke-тестом disconnect/timeout.

---

## Домашнее задание

Сделайте `SCENE_FLOW.md`:

- список сцен;
- persistent objects;
- transitions;
- кто инициирует;
- что происходит при disconnect/loading timeout.
