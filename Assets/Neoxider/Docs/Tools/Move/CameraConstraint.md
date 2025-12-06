# Компонент Camera Constraint

## 1. Введение

`CameraConstraint` — универсальный компонент для ограничения движения камеры в пределах игрового уровня. Поддерживает как 2D, так и 3D камеры, работает с различными типами границ (спрайты, коллайдеры, ручные настройки).

Это незаменимый инструмент для игр с камерой, следующей за игроком, чтобы она не показывала пустое пространство за пределами игровой зоны.

---

## 2. Описание класса

### CameraConstraint
- **Пространство имен**: `Neo.Tools`
- **Путь к файлу**: `Assets/Neoxider/Scripts/Tools/Move/CameraConstraint.cs`

**Описание**
Компонент размещается на объекте камеры и автоматически ограничивает её перемещение в пределах заданных границ. В `LateUpdate` проверяет позицию камеры и корректирует её при выходе за границы.

**Ключевые поля**

### Типы границ (BoundsType)
- `SpriteRenderer`: Использует границы спрайта (для 2D-игр)
- `Collider2D`: Использует границы 2D-коллайдера
- `Collider`: Использует границы 3D-коллайдера
- `Manual`: Ручная настройка границ через параметр `manualBounds`

### Настройки
- `boundsType`: Тип источника границ
- `spriteRenderer` / `collider2D` / `collider`: Ссылка на источник границ (в зависимости от типа)
- `manualBounds`: Ручные границы (используется при `BoundsType.Manual`)
- `cam`: Камера для ограничения (автоматически берётся с компонента, если не указана)
- `edgePadding`: Дополнительный отступ от краёв границ
- `constraintX`, `constraintY`, `constraintZ`: Включение/отключение ограничения по каждой оси
- `showDebugGizmos`: Визуализация границ в редакторе

**Публичные методы (API для кода)**

### Управление границами
- `UpdateBounds()`: Пересчитать границы вручную
- `SetBoundsSource(BoundsType type, Object source)`: Изменить источник границ в runtime
- `SetManualBounds(Bounds bounds)`: Установить ручные границы
- `SetEdgePadding(float padding)`: Изменить отступ от края

### Управление ограничениями
- `SetAxisConstraint(bool x, bool y, bool z)`: Включить/выключить ограничение по осям
- `GetConstraintBounds(out Vector3 min, out Vector3 max)`: Получить текущие границы
- `IsAtEdge(out bool minX, out bool maxX, out bool minY, out bool maxY)`: Проверить, у края ли камера

---

### Примеры использования из кода

```csharp
CameraConstraint constraint = GetComponent<CameraConstraint>();

// Сменить уровень динамически
constraint.SetBoundsSource(CameraConstraint.BoundsType.SpriteRenderer, newLevelSprite);

// Увеличить отступ при зуме
constraint.SetEdgePadding(2f);

// Отключить ограничение по Y (для вертикальных уровней)
constraint.SetAxisConstraint(true, false, false);

// Проверить, у края ли камера (для UI подсказок)
if (constraint.IsAtEdge(out bool atMinX, out bool atMaxX, out bool atMinY, out bool atMaxY))
{
    if (atMinX) ShowHint("Достигнут левый край карты");
    if (atMaxX) ShowHint("Достигнут правый край карты");
}

// Динамические границы для процедурно генерируемых уровней
Bounds levelBounds = new Bounds(center, size);
constraint.SetManualBounds(levelBounds);
```

---

## 3. Настройка и использование

### Для 2D-игр (ортографическая камера)
1. Добавьте компонент `CameraConstraint` на камеру
2. Выберите `BoundsType` = `SpriteRenderer`
3. Перетащите фоновый спрайт уровня в поле `spriteRenderer`
4. Настройте `edgePadding` при необходимости
5. Включите `showDebugGizmos` для визуальной проверки

### Для 3D-игр (перспективная камера)
1. Добавьте компонент `CameraConstraint` на камеру
2. Выберите `BoundsType` = `Collider` или `Manual`
3. Если используете коллайдер, добавьте невидимый Box Collider на уровень и перетащите его в поле `collider`
4. Если используете `Manual`, настройте `manualBounds` вручную
5. Настройте ограничения по осям (`constraintX/Y/Z`)

### С использованием Collider2D
Удобно для сложных форм уровня:
1. Создайте GameObject с `PolygonCollider2D` или `BoxCollider2D`
2. Настройте форму коллайдера под границы уровня
3. Выберите `BoundsType` = `Collider2D`
4. Перетащите коллайдер в поле `collider2D`

---

## 4. Особенности

### Адаптация к типу камеры
- **Ортографическая**: Использует `orthographicSize` и `aspect` для расчёта видимой области
- **Перспективная**: Использует `fieldOfView` и расстояние до уровня для расчёта области

### Визуализация
В режиме редактора (когда `showDebugGizmos` включен):
- Зелёная рамка показывает область, в которой может двигаться камера
- Жёлтые сферы отмечают углы области
- Для 3D камер рисуется объёмная рамка

### Работа с другими компонентами
Используйте вместе с `Follow` для создания камеры, следующей за игроком с ограничением по уровню. `CameraConstraint` выполняется в `LateUpdate`, поэтому работает после `Follow`.
