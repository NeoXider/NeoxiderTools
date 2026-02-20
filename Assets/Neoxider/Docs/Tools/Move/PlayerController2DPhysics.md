# Компонент PlayerController2DPhysics

## 1. Введение

`PlayerController2DPhysics` — удобный 2D-контроллер на `Rigidbody2D` для платформера/сайд-скроллера: ходьба, бег, прыжок, coyote time, jump buffer и опциональный follow камеры.

---

## 2. Что делает

- Горизонтальное перемещение через `Horizontal`.
- Бег по `LeftShift`.
- Прыжок через `Jump`.
- Поддержка forgiving-механик:
  - `coyote time` (прыжок сразу после схода с платформы);
  - `jump buffer` (нажатие прыжка чуть заранее).
- Опциональный follow камеры с плавным сглаживанием.

---

## 3. Настройка в сцене

1. Создайте объект `Player2D`:
   - `Rigidbody2D`
   - `BoxCollider2D` (или другой 2D коллайдер)
2. Добавьте `PlayerController2DPhysics`.
3. Создайте дочерний `GroundCheck` у ног и назначьте в `_groundCheck`.
4. Настройте `_groundMask` на слой земли.
5. Если нужен встроенный follow камеры:
   - назначьте камеру в `_followCamera`
   - включите `_followCameraEnabled`.

---

## 4. Полезные параметры

- `_inputBackend` — выбор системы ввода:
  - `AutoPreferNew` (по умолчанию): New Input System, если пакет доступен; иначе fallback на Legacy;
  - `NewInputSystem`;
  - `LegacyInputManager`.
- `_walkSpeed`, `_runSpeed` — скорости движения.
- `_acceleration`, `_deceleration` — разгон/торможение.
- `_jumpImpulse` — сила прыжка.
- `_coyoteTime`, `_jumpBufferTime` — отзывчивость прыжка.
- `_flipByVelocityX` — разворот спрайта по направлению движения.
- `_cameraOffset`, `_cameraFollowSpeed` — поведение follow камеры.

---

## 5. Публичный API

- `IsGrounded` — текущее состояние земли.
- `IsRunning` — активен ли спринт.
- `SetMovementEnabled(bool)` — включение/выключение движения.
- `SetCameraFollowEnabled(bool)` — включение/выключение follow камеры.
- `Teleport(Vector3, bool)` — телепорт с опциональным сбросом скорости.
- События **OnMoveStart** / **OnMoveStop** — вызываются при начале и окончании движения (переход ввода в ноль и из нуля).

Если трансформ **Ground Check** не назначен, в Awake выводится однократное предупреждение; проверка земли выполняется от позиции объекта.

---

## 6. Быстрый тест

1. Создайте платформу с `BoxCollider2D`.
2. Поместите игрока над платформой.
3. Запустите Play:
   - `A/D` или `Left/Right` — движение
   - `Shift` — бег
   - `Space` — прыжок
4. Проверьте:
   - прыжок с края платформы (coyote);
   - прыжок при раннем нажатии до приземления (buffer);
   - плавность follow камеры.

