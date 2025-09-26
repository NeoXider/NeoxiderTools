# Расширения RandomShapeExtensions

## 1. Введение

`RandomShapeExtensions` — это набор методов-расширений для получения случайных точек внутри или на поверхности различных геометрических фигур, таких как `Bounds`, `Circle` и `Sphere`.

---

## 2. Описание методов

### RandomShapeExtensions
- **Пространство имен**: `Neo.Extensions`
- **Путь к файлу**: `Assets/Neoxider/Scripts/Extensions/RandomShapeExtensions.cs`

**Статические методы**
- `RandomPointInBounds(this Bounds bounds)`: Возвращает случайную точку внутри `Bounds`. Возвращает `Vector3`.
- `RandomPointOnBounds(this Bounds bounds)`: Возвращает случайную точку на поверхности `Bounds`. Возвращает `Vector3`.
- `RandomPointInCircle(this Circle circle)`: Возвращает случайную точку внутри 2D-круга. Возвращает `Vector2`.
- `RandomPointOnCircle(this Circle circle)`: Возвращает случайную точку на окружности 2D-круга. Возвращает `Vector2`.
- `RandomPointInSphere(this Sphere sphere)`: Возвращает случайную точку внутри сферы. Возвращает `Vector3`.
- `RandomPointOnSphere(this Sphere sphere)`: Возвращает случайную точку на поверхности сферы. Возвращает `Vector3`.
- `RandomPointInBounds(this Collider collider)`: Возвращает случайную точку внутри `Bounds` 3D-коллайдера. Возвращает `Vector3`.
- `RandomPointInBounds(this Collider2D collider)`: Возвращает случайную точку внутри `Bounds` 2D-коллайдера. Возвращает `Vector2`.
