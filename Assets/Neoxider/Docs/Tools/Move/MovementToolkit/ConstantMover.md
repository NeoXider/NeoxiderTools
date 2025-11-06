# ConstantMover

Компонент для постоянного перемещения объекта в заданном направлении. Поддерживает режимы Transform, Rigidbody и Rigidbody2D. По умолчанию перемещает вперёд (LocalForward) в режиме Transform.

## Параметры
- Mode
  - `MovementMode mode` — Transform | Rigidbody | Rigidbody2D (по умолчанию Transform)
  - `bool spaceLocal` — использовать локальное пространство для направления (default: true)
  - `bool useDeltaTime` — умножать скорость на deltaTime (default: true)
- Direction
  - `DirectionSource directionSource` — LocalForward3D | Up2D | Right2D | Custom
  - `Vector3 customDirection` — пользовательское направление
- Speed
  - `float speed` — ед/сек
- Axis Locks (world)
  - `lockX`, `lockY`, `lockZ` — обнулить соответствующие компоненты смещения в мировом пространстве

## Поведение
- Transform: смещает `transform.position` на `dirWorld * speed * deltaTime`.
- Rigidbody: `Rigidbody.MovePosition(position + delta)` в FixedUpdate.
- Rigidbody2D: `Rigidbody2D.MovePosition(position + delta)` в FixedUpdate.
- Направление:
  - LocalForward3D → `transform.forward` (или `Vector3.forward` если spaceLocal=false)
  - Up2D → `transform.up` | `Vector3.up`
  - Right2D → `transform.right` | `Vector3.right`
  - Custom → трансформируется в мир при `spaceLocal=true`

## Примеры
- Снаряд летит вперёд:
  - mode = Transform, direction = LocalForward3D, speed = 10
- Движение вверх в 2D c физикой:
  - mode = Rigidbody2D, direction = Up2D, speed = 5
