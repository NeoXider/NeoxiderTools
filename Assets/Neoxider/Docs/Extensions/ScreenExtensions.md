# Расширения ScreenExtensions

## 1. Введение

`ScreenExtensions` — это набор методов-расширений для работы с экранными координатами и камерой. Они помогают решать такие частые задачи, как проверка видимости объекта на экране или получение мировых координат краев экрана.

---

## 2. Описание методов

### ScreenExtensions
- **Пространство имен**: `Neo.Extensions`
- **Путь к файлу**: `Assets/Neoxider/Scripts/Extensions/ScreenExtensions.cs`

**Статические методы**
- `IsOnScreen(this Vector3 position, ...)`: Проверяет, видна ли точка, находящаяся в мировых координатах, на экране основной камеры.
- `IsOutOfScreen(this Vector3 position, ...)`: Проверяет, находится ли точка за пределами экрана.
- `IsOutOfScreenSide(this Vector3 position, ScreenEdge side, ...)`: Проверяет, находится ли точка за пределами экрана с определенной стороны (слева, справа, сверху, снизу).
- `GetClosestScreenEdgePoint(this Vector3 position, ...)`: Для точки за пределами экрана находит ближайшую к ней точку на краю экрана.
- `GetWorldPositionAtScreenEdge(this Camera camera, ScreenEdge edge, ...)`: Возвращает мировую позицию для указанного края экрана (например, `ScreenEdge.TopLeft`).
- `GetWorldScreenBounds(this Camera camera, float distance)`: Возвращает `Bounds`, представляющий собой видимую область камеры на указанной дистанции.
