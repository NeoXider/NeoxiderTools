# KeyInputCompat

**Что это:** `KeyInputCompat` — статический адаптер для чтения клавиатуры через старую `Input Manager` и новую `Input System` без изменения вызывающего кода. Файл: `Scripts/Tools/Input/KeyInputCompat.cs`, пространство имён: `Neo.Tools`.

**Как использовать:**
1. Вызывайте `KeyInputCompat.GetKeyDown`, `GetKeyUp`, `GetKey` вместо прямого `Input.GetKey*`, если компонент должен работать с разными backend-вариантами Unity input.
2. Никакой дополнительной настройки компонента не требуется.
3. Если legacy input недоступен, класс автоматически попробует прочитать состояние через `OptionalInputSystemAdapter`.

---

## Основные методы

| Метод | Описание |
|-------|----------|
| `GetKeyDown(KeyCode keyCode)` | Нажатие клавиши в текущем кадре. |
| `GetKeyUp(KeyCode keyCode)` | Отпускание клавиши в текущем кадре. |
| `GetKey(KeyCode keyCode)` | Удержание клавиши. |

## Как работает совместимость

Алгоритм такой:

1. Сначала вызывается обычный `Input.GetKey*`.
2. Если Unity выбрасывает `InvalidOperationException`, класс считает, что legacy input отключён.
3. Тогда чтение переходит в `OptionalInputSystemAdapter`, который пытается получить состояние клавиши через новый Input System.

Это позволяет использовать один и тот же код в проектах с:
- только `Input Manager`;
- только `Input System`;
- гибридной конфигурацией.

## Когда использовать

Используйте `KeyInputCompat`, если:
- вы пишете reusable runtime-компонент;
- не хотите жёстко привязывать пакет к одному input backend;
- компонент должен пережить разные Player Settings у конечного пользователя пакета.

## Ограничения

- Поддержка зависит от того, может ли `OptionalInputSystemAdapter` сопоставить `KeyCode` с Input System key property.
- Для сложных input action map сценариев лучше использовать полноценный Input System API, а не этот совместимый слой.

## См. также

- [README](./README.md)
- [SwipeController](./SwipeController.md)
- [MouseInputManager](./MouseInputManager.md)
