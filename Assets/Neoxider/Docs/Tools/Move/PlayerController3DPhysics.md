# Компонент PlayerController3DPhysics

**Что это:** Компонент подходит для FPS/TPS-прототипов, где нужна физическая реакция и предсказуемое управление без сложной зависимости от внешних систем.

**Как использовать:** см. разделы ниже.

---


## 1. Введение

`PlayerController3DPhysics` — контроллер персонажа для 3D через `Rigidbody`: движение, бег, прыжок, mouse-look и базовая работа с курсором.

Компонент подходит для FPS/TPS-прототипов, где нужна физическая реакция и предсказуемое управление без сложной зависимости от внешних систем.

---

## 2. Что делает

- Перемещает игрока по осям `Horizontal/Vertical`.
- Поддерживает бег (`LeftShift`) и прыжок (`Jump`).
- Проверяет землю через `CheckSphere`.
- Управляет обзором:
  - `pitch` ограниченно вращает `cameraPivot`;
  - `yaw` может вращать:
    - только персонажа,
    - только `cameraPivot`,
    - и персонажа, и `cameraPivot`.
- Поддерживает режимы направления движения:
  - от поворота персонажа,
  - от yaw камеры,
  - по мировым осям (стратегический/RTS стиль).
- Есть смещение угла движения (`_movementYawOffset`) для тонкой настройки направлений WASD.
- Может блокировать курсор на старте.
- **Pause Look When Cursor Visible** (по умолчанию включено): при видимом курсоре (`Cursor.visible == true`) не обрабатывает ввод мыши и не вращает камеру. Удобно для паузы/меню без связи с `CursorLockController`.
- Если на том же объекте активен `CursorLockController`, встроенные `_lockCursorOnStart` и `_toggleCursorOnEscape` автоматически не используются, чтобы не было двойного управления курсором.
- Можно назначить **External Cursor Lock Controller**: тогда контроллер игрока будет уважать `CursorLockController`, который находится не на игроке, а, например, на странице меню/паузы или на общем UI-root.
- Внешний `CursorLockController` может работать как в automatic-, так и в manual-сценариях. Рекомендуемый дефолтный режим для него — **AutomaticAndManual**.
- В `CursorLockController` можно опционально включить отдельный `Cursor Access Key` (например `Z`) для быстрого показа курсора в hold- или toggle-режиме, если игроку нужен временный доступ к UI без открытия отдельного меню. По умолчанию этот режим выключен.

---

## 3. Настройка в сцене

1. Создайте объект `Player`:
   - `CapsuleCollider`
   - `Rigidbody` (`Freeze Rotation` по X/Y/Z можно оставить выключенным, скрипт сам фиксирует вращение)
2. Добавьте `PlayerController3DPhysics`.
3. В поле `_cameraPivot` укажите камеру (или pivot камеры как дочерний объект игрока).
4. Создайте дочерний `GroundCheck` у ног игрока и назначьте в `_groundCheck`.
5. Настройте `_groundMask` на слои земли.
6. Убедитесь, что в Input Manager есть оси:
   - `Horizontal`
   - `Vertical`
   - `Mouse X`
   - `Mouse Y`
   - `Jump`

---

## 4. Полезные параметры

- `_inputBackend` — выбор системы ввода:
  - `AutoPreferNew` (по умолчанию): New Input System, если пакет доступен; иначе fallback на Legacy;
  - `NewInputSystem`;
  - `LegacyInputManager`.
- `_movementReference` — режим базиса движения.
- `_movementYawOffset` — сдвиг направления движения (в градусах).
- `_walkSpeed`, `_runSpeed` — базовые скорости.
- `_groundAcceleration`, `_airAcceleration` — отзывчивость на земле/в воздухе.
- `_jumpImpulse` — сила прыжка.
- `_coyoteTime`, `_jumpBufferTime` — устойчивый прыжок в динамике.
- `_extraGravityMultiplier` — ускоренное падение.
- `_lookYawMode` — как применять горизонтальный поворот.
- `_minPitch`, `_maxPitch` — лимиты вертикального взгляда.
- `_lockCursorOnStart` — блокировка курсора при запуске.
- `_toggleCursorOnEscape` — локальный toggle по Escape. Используйте его, только если **нет** `CursorLockController` на том же объекте.
- `_externalCursorLockController` — внешний `CursorLockController`. Нужен, если курсором управляет не сам игрок, а отдельный UI-объект.

События **OnMoveStart** / **OnMoveStop** вызываются при начале и окончании движения. Если **Ground Check** не назначен, в Awake выводится однократное предупреждение.

---

## 5. Публичный API

- `IsGrounded` — текущее состояние земли.
- `IsRunning` — активен ли спринт.
- `SetMovementEnabled(bool)` — включение/выключение движения.
- `SetLookEnabled(bool)` — включение/выключение обзора.
- `SetCursorLocked(bool)` — lock/unlock курсора.
- `Teleport(Vector3)` — телепорт с очисткой скорости.

---

## 6. Сценарий: меню или пауза как отдельная UI-страница

Если меню/пауза живут отдельным объектом страницы, удобная схема такая:

1. На игроке:
   - `Pause Look When Cursor Visible = true`
   - `_toggleCursorOnEscape = false`, если курсором будет управлять UI-страница
   - при использовании внешней страницы назначьте её `CursorLockController` в поле **External Cursor Lock Controller**
2. На объекте страницы:
   - `CursorLockController`
   - `Apply On Enable = true`, `Lock On Enable = false`
   - `Apply On Disable = true`, `Lock On Disable = true`
3. В события открытия/закрытия страницы добавьте:
   - открыть страницу → `PlayerController3DPhysics.SetLookEnabled(false)`
   - закрыть страницу → `PlayerController3DPhysics.SetLookEnabled(true)`

Так страница берёт мышь под UI, а игрок возвращает обзор при закрытии страницы. Если внешний `CursorLockController` назначен в поле игрока, локальный Escape-toggle у игрока не будет конфликтовать с этим UI-источником управления.

Если у вас несколько UI-страниц или временных механик, которые хотят забирать курсор, используйте отдельные `CursorLockController` и отпускайте временное владение через `ReleaseControl()`. Подробная схема описана в [`CursorLockController.md`](./CursorLockController.md).

---

## 7. Быстрый тест

1. Добавьте плоскость (`Plane`) как землю.
2. Поместите игрока выше земли (`Y > 1`).
3. Запустите Play:
   - `WASD` — движение
   - `Shift` — бег
   - `Space` — прыжок
   - мышь — обзор
   - курсор должен скрыться/залочиться, если включен `_lockCursorOnStart`.

> В этой версии нет жесткой compile-time зависимости от `Unity.InputSystem`: если пакет отсутствует, контроллер автоматически использует Legacy Input Manager и пишет предупреждение в лог один раз.

