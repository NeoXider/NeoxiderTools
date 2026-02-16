# Компонент PlayerController3DPhysics

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

---

## 5. Публичный API

- `IsGrounded` — текущее состояние земли.
- `IsRunning` — активен ли спринт.
- `SetMovementEnabled(bool)` — включение/выключение движения.
- `SetLookEnabled(bool)` — включение/выключение обзора.
- `SetCursorLocked(bool)` — lock/unlock курсора.
- `Teleport(Vector3)` — телепорт с очисткой скорости.

---

## 6. Быстрый тест

1. Добавьте плоскость (`Plane`) как землю.
2. Поместите игрока выше земли (`Y > 1`).
3. Запустите Play:
   - `WASD` — движение
   - `Shift` — бег
   - `Space` — прыжок
   - мышь — обзор
   - курсор должен скрыться/залочиться, если включен `_lockCursorOnStart`.

> В этой версии нет жесткой compile-time зависимости от `Unity.InputSystem`: если пакет отсутствует, контроллер автоматически использует Legacy Input Manager и пишет предупреждение в лог один раз.

