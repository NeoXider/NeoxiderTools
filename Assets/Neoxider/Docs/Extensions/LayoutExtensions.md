# Расширения LayoutExtensions

## 1. Введение

`LayoutExtensions` — это набор методов-расширений для `IEnumerable<Transform>`, который позволяет процедурно **размещать** коллекцию объектов в пространстве в виде различных фигур (линия, сетка, круг и т.д.).

Эти методы используют `LayoutUtils` для расчета целевых позиций, а затем применяют эти позиции к `Transform`'ам из коллекции.

---

## 2. Описание методов

### LayoutExtensions
- **Пространство имен**: `Neo.Extensions`
- **Путь к файлу**: `Assets/Neoxider/Scripts/Extensions/LayoutExtensions.cs`

**Статические методы**
- `ArrangeInLine(this IEnumerable<Transform> elements, ...)`: Размещает объекты в линию.
- `ArrangeInGrid(this IEnumerable<Transform> elements, ...)`: Размещает объекты в 2D-сетке.
- `ArrangeInCircle(this IEnumerable<Transform> elements, ...)`: Размещает объекты по кругу.
- `ArrangeInGrid3D(this IEnumerable<Transform> elements, ...)`: Размещает объекты в сетке на произвольной 3D-плоскости.
- `ArrangeInCircle3D(this IEnumerable<Transform> elements, ...)`: Размещает объекты по кругу на произвольной 3D-плоскости.
- `ArrangeOnSphereSurface(this IEnumerable<Transform> elements, ...)`: Размещает объекты на поверхности сферы.
- `ArrangeInSpiral(this IEnumerable<Transform> elements, ...)`: Размещает объекты по спирали.
- `ArrangeOnSineWave(this IEnumerable<Transform> elements, ...)`: Размещает объекты вдоль синусоиды.
