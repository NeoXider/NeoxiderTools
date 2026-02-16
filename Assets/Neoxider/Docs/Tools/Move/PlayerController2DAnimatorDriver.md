# Компонент PlayerController2DAnimatorDriver

## 1. Введение

`PlayerController2DAnimatorDriver` — отдельный анимационный драйвер для `PlayerController2DPhysics`.

Подходит для платформера и 2D top-down: можно использовать как базовые state-параметры (idle/walk/run/jump), так и BlendTree.

---

## 2. Что умеет

- Пишет в Animator:
  - `IsGrounded`
  - `IsMoving`
  - `IsRunning`
  - `Speed`
  - `LocomotionState`
  - `Jump` trigger
- BlendTree режимы:
  - `HorizontalOnly` (одна ось, удобно для платформера)
  - `TwoAxis` (две оси для 2D направлений)

---

## 3. Быстрая настройка

1. На игроке должны быть:
   - `PlayerController2DPhysics`
   - `Rigidbody2D`
   - `Animator`
2. Добавьте `PlayerController2DAnimatorDriver`.
3. Проверьте ссылки:
   - `_animator`
   - `_controller`
   - `_rigidbody`
4. Синхронизируйте названия параметров Animator с полями драйвера.

---

## 4. Варианты использования

### Платформер

- `_useBlendTree = true`
- `_blendMode = HorizontalOnly`
- blend по `MoveX`, прыжок по `Jump`/`IsGrounded`

### Top-down 2D

- `_useBlendTree = true`
- `_blendMode = TwoAxis`
- blend по `MoveX` и `MoveY`

### Без BlendTree

- `_useBlendTree = false`
- используйте bool/int/trigger параметры для обычной машины состояний.

---

## 5. Пример параметров Animator

- Bool: `IsGrounded`, `IsMoving`, `IsRunning`
- Float: `Speed`, `MoveX`, `MoveY`
- Int: `LocomotionState`
- Trigger: `Jump`

