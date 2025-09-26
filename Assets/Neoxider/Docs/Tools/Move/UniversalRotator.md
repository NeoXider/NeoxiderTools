# UniversalRotator

Гибкий компонент для плавного поворота объектов в 2D и 3D. Поддерживает ограничение по одной оси, работу с мышью (2D/3D), поворот к `Transform`, мировой точке или направлению, а также выбор режима обновления и использование нескалированного времени.

- **Namespace**: `Neo.Tools`
- **Путь**: `Assets/Neoxider/Scripts/Tools/Move/UniversalRotator.cs`

## Введение
`UniversalRotator` решает типовую задачу «повернуться к цели» с учетом разных сценариев: 2D (вращение по Z), 3D (LookRotation), ограничения по углам, наведение на мышь (через плоскость или физический Raycast) и выбор источника цели. Компонент настроен так, чтобы использоваться без кода, но при этом имеет удобное публичное API.

## Ключевые особенности
- Поворот в режимах 2D и 3D
- Наведение на: `Transform`, мировую точку, направление или позицию мыши
- Ограничение угла поворота по одной (рабочей) оси с относительной/абсолютной базой
- Настраиваемая скорость (°/сек) и офсет эйлеров
- Выбор `Update/FixedUpdate/LateUpdate` и опция `useUnscaledTime`
- Поддержка родителя: корректный учет локальных/мировых поворотов

## Настройки инспектора (основное)
- **Rotation Mode**: `Mode3D` или `Mode2D`
- **Update Mode**: `Update`, `FixedUpdate`, `LateUpdate`
- **Use Unscaled Time**: использовать нескалированное время
- **Rotation Speed**: скорость поворота (°/сек)
- **Rotation Offset (Euler)**: офсет в градусах
  - В 2D доступен слайдер `Offset Z (2D)`

### Ограничения
- **Ось ограничения (локальная)**: в 3D — `X/Y/Z`, в 2D рабочая ось всегда `Z`
- **Диапазон (°)**: если `[0..360]`, ограничения отключены
- **Относительно стартовой позы**: считать диапазон от локальных эйлеров на старте или от 0

### Наведение
- **Use Mouse World**: использовать позицию мыши как цель
- **Target (Transform)**: цель, если мышь не используется
- **Target Camera**: камера для расчета мыши (по умолчанию `Camera.main`)
- 3D режим мыши:
  - **Mouse 3D Mode**: `PlaneThroughObject` или `PhysicsRaycast`
  - **Plane Axis**: нормаль плоскости для режима `PlaneThroughObject`
  - **Mouse Raycast Mask**: маска слоев для `PhysicsRaycast`
- 3D только: **World Up** — up-вектор для `Quaternion.LookRotation`

## Публичные методы
- `void SetTarget(Transform newTarget)`
  - Устанавливает цель-`Transform`, отключая режим мыши. Если `newTarget == null`, источник сбрасывается.
- `void ClearTarget()`
  - Очищает цель. Если включен режим мыши, источником становится мышь, иначе — `None`.
- `void RotateTo(Vector3 worldPoint, bool instant = false)`
  - Наводит на мировую точку. При `instant == true` сразу применяет итоговый поворот (с учетом ограничений), иначе поворачивает плавно.
- `void RotateToDirection(Vector3 worldDirection, bool instant = false)`
  - Наводит по мировому направлению (вектор); внутри конвертируется в точку `transform.position + worldDirection`.
- `void RotateBy(float deltaDegrees)`
  - Поворачивает на заданные градусы вокруг рабочей оси (в 2D — Z; в 3D — ось `limitedAxis3D`). Ограничения учитываются.
- `void RotateBy(Vector3 eulerDelta)`
  - Поворачивает на локальные эйлеры. Ограничения учитываются.

Возвращаемые значения отсутствуют; методы выполняют действие над `transform`.

## Unity Events
Компонент не экспонирует публичные `UnityEvent`.

## Примеры использования
### 1) 2D-турель, наведение на мышь
```csharp
public class Turret2D : MonoBehaviour
{
    [SerializeField] private Neo.Tools.UniversalRotator rotator;

    private void Awake()
    {
        // Режим 2D, работаем по Z
        rotator.useMouseWorld = true;
        // Скорость поворота
        rotator.rotationSpeed = 720f;
        // Ограничим сектор, например, от -60 до 60 относительно старта
        rotator.limitRange = new Vector2(-60f, 60f);
        rotator.limitsRelativeToInitial = true;
    }
}
```

### 2) 3D-страж, смотрит на цель
```csharp
public class Guard3D : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Neo.Tools.UniversalRotator rotator;

    private void Start()
    {
        rotator.SetTarget(target);
        rotator.rotationSpeed = 180f;
        rotator.worldUp = Vector3.up;
    }
}
```

### 3) Мгновенно повернуться к направлению
```csharp
// Повернуться сразу туда, куда «вправо» от объекта (world)
rotator.RotateToDirection(transform.right, instant: true);
```

## Замечания по производительности
- Используйте `FixedUpdate` только если поворот участвует в физике; иначе достаточно `Update`/`LateUpdate`.
- Старайтесь не ставить избыточные raycast-режимы для мыши без необходимости.

