# Компонент CursorLockController

## 1. Введение

`CursorLockController` управляет видимостью и блокировкой курсора (lock/unlock). Поддерживает начальное состояние, опциональное применение при включении/выключении компонента и переключение по горячей клавише. Предназначен для FPS/TPS, меню и паузы; не конфликтует с `PlayerController3DPhysics` и `PlayerController2DPhysics` при разделении контекстов (например, контроллер курсора на объекте меню, контроллер игрока — в геймплее).

---

## 2. Поведение

- **Start**: при включённом `_lockOnStart` выставляет lock и скрывает курсор.
- **OnEnable** (опционально): если включено `_applyOnEnable`, применяет состояние `_lockOnEnable` (lock/unlock). Удобно, когда объект с контроллером включается при возврате в геймплей.
- **OnDisable** (опционально): если включено `_applyOnDisable`, применяет состояние `_lockOnDisable`. Удобно при открытии меню или паузы.
- **Update**: при `_allowToggle` переключает состояние по клавише `_toggleKey` (по умолчанию Escape).
- **События**: `_onCursorLocked`, `_onCursorUnlocked`.

Вращение камеры при видимом курсоре отключается **в самом** `PlayerController3DPhysics` (опция **Pause Look When Cursor Visible**), без вызовов FindObjectsOfType и без связи между компонентами.

---

## 3. Настройка

1. Добавьте `CursorLockController` на активный объект (камера, GameManager, корень геймплея).
2. **Start State**: `_lockOnStart` — lock курсора при старте.
3. **Lifecycle**: при необходимости включите `_applyOnEnable` / `_applyOnDisable` и задайте `_lockOnEnable` / `_lockOnDisable`.
4. **Toggle**: `_allowToggle`, `_toggleKey` — ручное переключение по клавише.
5. Для паузы/меню можно использовать **PausePage** с опцией **Control Cursor**: курсор показывается при паузе и восстанавливается при закрытии без отключения `CursorLockController`.

---

## 4. Публичный API

| Член | Описание |
|------|----------|
| `IsLocked` | Текущее состояние блокировки курсора. |
| `SetCursorLocked(bool)` | Установить lock/unlock и видимость. |
| `ToggleCursorState()` | Инвертировать текущее состояние. |
| `ShowCursor()` | Показать и разблокировать. |
| `HideCursor()` | Скрыть и заблокировать. |

---

## 5. Совместное использование с PausePage и контроллерами игрока

- **PausePage** с опцией **Control Cursor** при открытии паузы сохраняет состояние курсора и показывает его, при закрытии — восстанавливает. Отдельно вешать/снимать `CursorLockController` не обязательно.
- **PlayerController3DPhysics** сам не вращает камеру при видимом курсоре, если у него включена опция **Pause Look When Cursor Visible** (по умолчанию включена). Связи между CursorLockController и контроллером игрока не требуется.
