# MouseInputManager

**Что это:** `MouseInputManager` — singleton-компонент для обработки мышиного ввода без лишних аллокаций в кадре. Он собирает `Press`, `Hold`, `Release`, `Click`, делает 2D/3D raycast по слоям и сохраняет последнее событие в `MouseEventData`. Файл: `Scripts/Tools/Input/MouseInputManager.cs`.

**Как использовать:**
1. Добавьте `MouseInputManager` на сцену один раз или позвольте ему создаться автоматически.
2. Настройте `targetCamera`, `interactableLayers`, `fallbackDepth`.
3. Подписывайтесь на `OnPress`, `OnHold`, `OnRelease`, `OnClick` или их `in`-версии.
4. При необходимости используйте `LastEventData` и `HasEventData` для polling-подхода.

---

## Основное

- **LastEventData**, **HasEventData** — последнее событие для опроса.
- Подписка через делегаты/события на нажатие, удержание, отпускание, клик.
- 3D raycast идёт через `Physics.RaycastNonAlloc`.
- 2D raycast идёт через `Physics2D.GetRayIntersection`.
- Если попадания нет, используется `fallbackDepth` и `ScreenToWorldPoint`.

## События

- `OnPress`
- `OnHold`
- `OnRelease`
- `OnClick`
- `OnPressIn`
- `OnHoldIn`
- `OnReleaseIn`
- `OnClickIn`

## MouseEventData

Структура содержит:
- `ScreenPosition`
- `WorldPosition`
- `HitObject`
- `Hit3D`
- `Hit2D`

## Важное замечание

Текущая реализация **не** содержит встроенной блокировки ввода поверх UI через `EventSystem`. Если такая логика нужна, её нужно добавлять отдельно поверх `MouseInputManager`.

## Жизненный цикл

- Через `RuntimeInitializeOnLoadMethod` включает `CreateInstance = true`, поэтому менеджер может быть создан автоматически.
- В `Init()` пытается взять `Camera.main`, если `targetCamera` не задан.
- При domain reload очищает `LastEventData` и `HasEventData`.

## См. также

- [MouseEffect](./MouseEffect.md)
- [README](./README.md)
