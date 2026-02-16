# Компонент PlayerController3DAnimatorDriver

## 1. Введение

`PlayerController3DAnimatorDriver` — отдельный анимационный драйвер для `PlayerController3DPhysics`.

Скрипт не двигает персонажа: он только обновляет параметры `Animator` (idle/walk/run/jump и BlendTree), чтобы логика движения и анимации были разделены.

---

## 2. Что умеет

- Обновляет базовые параметры:
  - `IsGrounded`
  - `IsMoving`
  - `IsRunning`
  - `Speed`
  - `LocomotionState` (0 idle, 1 walk, 2 run, 3 jump)
  - `Jump` (trigger при старте прыжка)
- Поддерживает directional BlendTree:
  - `MoveX`
  - `MoveY`
- Поддерживает разные пространства скорости для blend:
  - `World`
  - `Local`
  - `CameraRelative`

---

## 3. Быстрая настройка

1. На объект игрока добавьте:
   - `PlayerController3DPhysics`
   - `Rigidbody`
   - `Animator`
2. Добавьте `PlayerController3DAnimatorDriver`.
3. Проверьте ссылки:
   - `_animator`
   - `_controller`
   - `_rigidbody`
   - `_cameraTransform` (если нужен `CameraRelative`)
4. В Animator добавьте параметры с именами из драйвера (или переименуйте поля в инспекторе).

---

## 4. Настройка под разные графы

### Вариант A: простые состояния

- Выключите `_useDirectionalBlendTree`.
- Оставьте `IsMoving`, `IsRunning`, `IsGrounded`, `Jump`, `LocomotionState`.
- Настройте переходы в Animator Controller.

### Вариант B: BlendTree (направления)

- Включите `_useDirectionalBlendTree`.
- Используйте BlendTree с `MoveX` и `MoveY`.
- Для контроля амплитуды настройте `_blendMaxSpeed`.
- Для smoothing параметров настройте `_blendDampTime`.

---

## 5. Пример параметров Animator

- Bool: `IsGrounded`, `IsMoving`, `IsRunning`
- Float: `Speed`, `MoveX`, `MoveY`
- Int: `LocomotionState`
- Trigger: `Jump`

---

## 6. Советы

- Для third-person/fps чаще всего:
  - `_velocitySpace = Local` или `CameraRelative`
- Для top-down-like 3D:
  - `_velocitySpace = World`
- Если прыжок часто триггерится лишний раз:
  - увеличьте порог вертикальной скорости в графе переходов
  - или отключите `_useJumpTrigger` и делайте переход по `IsGrounded`.

