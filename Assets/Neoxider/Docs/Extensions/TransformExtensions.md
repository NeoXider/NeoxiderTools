# Расширения TransformExtensions

## 1. Введение

`TransformExtensions` — это очень большой и полезный набор методов-расширений для `UnityEngine.Transform`. Он предоставляет множество удобных способов для манипуляции позицией, вращением и масштабом объекта, а также другие полезные утилиты.

---

## 2. Описание методов

### TransformExtensions
- **Пространство имен**: `Neo.Extensions`
- **Путь к файлу**: `Assets/Neoxider/Scripts/Extensions/TransformExtensions.cs`

**Статические методы**

#### Позиция (Position)
- `SetPosition(this Transform transform, ...)`: Устанавливает мировую позицию, позволяя изменять все три координаты или только некоторые из них (например, только `x`). Возвращает `void`.
- `AddPosition(this Transform transform, ...)`: Добавляет значения к мировой позиции. Возвращает `void`.
- `SetLocalPosition(this Transform transform, ...)`: Устанавливает локальную позицию. Возвращает `void`.
- `AddLocalPosition(this Transform transform, ...)`: Добавляет значения к локальной позиции. Возвращает `void`.

#### Вращение (Rotation)
- `SetRotation(this Transform transform, ...)`: Устанавливает мировое вращение (через `Quaternion` или углы Эйлера). Возвращает `void`.
- `AddRotation(this Transform transform, ...)`: Добавляет вращение к мировому. Возвращает `void`.
- `SetLocalRotation(this Transform transform, ...)`: Устанавливает локальное вращение. Возвращает `void`.
- `AddLocalRotation(this Transform transform, ...)`: Добавляет вращение к локальному. Возвращает `void`.

#### Масштаб (Scale)
- `SetScale(this Transform transform, ...)`: Устанавливает локальный масштаб. Возвращает `void`.
- `AddScale(this Transform transform, ...)`: Добавляет значения к локальному масштабу. Возвращает `void`.

#### Утилиты
- `LookAt2D(this Transform transform, ...)`: Поворачивает объект в сторону цели на 2D-плоскости (XY). Возвращает `void`.
- `SmoothLookAtRoutine(this Transform transform, ...)`: Возвращает корутину для плавного поворота в сторону цели. Возвращает `IEnumerator`.
- `GetClosest(this Transform transform, ...)`: Находит ближайший `Transform` из переданной коллекции. Возвращает `Transform`.
- `GetChildTransforms(this Transform transform)`: Возвращает массив всех дочерних `Transform` первого уровня. Возвращает `Transform[]`.
- `ResetTransform(this Transform transform)`: Сбрасывает мировую позицию, вращение и масштаб к стандартным значениям. Возвращает `void`.
- `ResetLocalTransform(this Transform transform)`: Сбрасывает локальную позицию, вращение и масштаб. Возвращает `void`.
- `CopyFrom(this Transform transform, Transform source)`: Копирует все параметры `Transform` из другого объекта. Возвращает `void`.
- `DestroyChildren(this Transform transform)`: Уничтожает все дочерние объекты. Возвращает `void`.
