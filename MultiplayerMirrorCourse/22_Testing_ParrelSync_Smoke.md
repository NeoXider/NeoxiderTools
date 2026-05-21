# Урок 22: тестирование мультиплеера и smoke-сценарии

**Навигация:** [Оглавление](README.md) · [Старт](00_START_HERE.md) · [Оформление](LESSON_STYLE.md) · продвинутый трек · урок 7/15 · Mirror `96.x`

| Ключевые слова | ParrelSync, Multiplayer Play Mode, smoke test, regression |
|----------------|------------------------------------------------------------|

---

## Карта урока

| Что | Ответ |
|-----|-------|
| Объект работы | `SMOKE_TESTS.md`, local multi-instance workflow. |
| Кто владеет state | Tests verify server truth and client display separately. |
| Как проверить | ParrelSync/MPPM/separate build + optional dedicated build. |
| Артефакт | 5 smoke scenarios with expected logs. |

---

## Что должно получиться

У вас есть короткий набор проверок, который ловит грубые сетевые поломки после каждого важного изменения.

---

## Проблема

Сетевой код может компилироваться и работать в single Editor, но ломаться на отдельном Client, Dedicated или при смене сцены.

---

## Инструменты

| Инструмент | Для чего |
|------------|----------|
| ParrelSync | Второй Unity Editor-клон проекта. |
| Unity Multiplayer Play Mode | Виртуальные игроки внутри workflow Unity. |
| Separate build | Ближе к реальному клиенту. |
| Dedicated build | Проверка server-only веток. |

Актуальная документация Unity Multiplayer Play Mode описывает его как быстрый способ тестировать несколько player/editor instances без выхода из среды разработки. Но это small-scale local testing: дополнительные editor instances имеют ограничения authoring, поэтому для релиза всё равно нужен separate build/dedicated smoke.

---

## Smoke-сценарий

Один сценарий должен иметь:

- сколько клиентов;
- какая сцена;
- что сделать;
- что должно произойти;
- какие логи ожидаются;
- что считается провалом.

Пример:

```text
ConnectSpawnMove
Clients: Host + 1 Client
Scene: NetSandbox
Steps: Host, Client connect, both move
Expected: two player objects, no authority warnings
Logs: connect id=..., spawn player
```

---

## Проверка себя

- Smoke можно пройти за 5 минут.
- Сценарии написаны так, что их повторит другой человек.
- Есть отдельный сценарий для disconnect/reconnect.
- Есть проверка Dedicated + Client, если проект идёт к dedicated.

---

## Минимальная диагностика

| Симптом | Что проверить |
|---------|---------------|
| В MPPM работает, в build нет | Editor-only code, package differences, build scenes. |
| ParrelSync clone странно ведёт assets | Проверить clone sync/settings и не править assets в clone. |
| Smoke слишком долгий | Оставить 5 минут critical path, остальное в nightly/regression. |
| Баг вернулся | Для него не добавили smoke step. |

---

## Частые ошибки

- Проверять только happy path.
- Не сохранять expected logs.
- Не тестировать scene change.
- Думать, что compile без ошибок равен multiplayer test.

---

## Лайфхаки

- После изменений в `NetworkManager`, auth, transport, scene loading прогоняйте smoke обязательно.
- Держите один "плохой network profile": 150 ms, jitter, packet loss.
- У каждого бага добавляйте новый smoke step, если он был важным.

---

## Профессиональный минимум

- Smoke покрывает Host + Client и Dedicated + Client, если есть dedicated target.
- Expected logs записаны, а не проверяются "на глаз".
- Bad network profile повторяемый.
- Smoke запускается после изменений в NetworkManager/auth/transport/scenes.

---

## Домашнее задание

Создайте `SMOKE_TESTS.md` с минимум 5 сценариями:

1. connect/spawn/move;
2. attack/interact;
3. scene change;
4. disconnect/reconnect;
5. bad network.
