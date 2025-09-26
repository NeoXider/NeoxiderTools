# Утилиты DebugGizmos

## 1. Введение

`DebugGizmos` — это статический класс-помощник для отрисовки отладочных гизмо в редакторе Unity. Он предоставляет простые методы для визуализации таких вещей, как границы (`Bounds`), точки и линии связей, которые можно вызывать из методов `OnDrawGizmos` или `OnDrawGizmosSelected`.

---

## 2. Описание методов

### DebugGizmos
- **Пространство имен**: `Neo.Extensions`
- **Путь к файлу**: `Assets/Neoxider/Scripts/Extensions/DebugGizmos.cs`

**Статические методы**
- `DrawBounds(Bounds bounds, ...)`: Рисует контур объекта `Bounds`.
- `DrawAveragePosition(Vector3 center, ...)`: Рисует сферу в указанной точке.
- `DrawLineToClosest(Vector3 from, GameObject closest, ...)`: Рисует линию от точки до ближайшего объекта.
- `DrawConnections(Vector3 from, IEnumerable<GameObject> targets, ...)`: Рисует линии от точки ко всем объектам в коллекции.
