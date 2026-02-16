# Компонент Camera Constraint

## 1. Введение

`CameraConstraint` — компонент для ограничения движения камеры в пределах уровня. Поддерживает 2D/3D камеры, 3 варианта источника границ и безопасную работу по осям (по умолчанию не меняет `Z`).

Это незаменимый инструмент для игр с камерой, следующей за игроком, чтобы она не показывала пустое пространство за пределами игровой зоны.

---

## 2. Описание класса

### CameraConstraint
- **Пространство имен**: `Neo.Tools`
- **Путь к файлу**: `Assets/Neoxider/Scripts/Tools/Move/CameraConstraint.cs`

**Описание**
Компонент размещается на объекте камеры и в `LateUpdate` ограничивает позицию камеры в пределах рассчитанного диапазона. Для перспективной камеры размер видимой области считается на глубине центра bounds, чтобы ограничение по `X/Y` работало предсказуемо.

**Ключевые поля**

### Типы границ (BoundsType)
- `SpriteRenderer`: Использует границы спрайта (для 2D-игр)
- `BoxCollider2D`: Использует границы `BoxCollider2D`
- `BoxCollider`: Использует границы `BoxCollider` (3D)

### Настройки
- `boundsType`: Тип источника границ
- `spriteRenderer` / `boxCollider2D` / `boxCollider`: Ссылка на источник границ (в зависимости от типа)
- `cam`: Камера для ограничения (автоматически берётся с компонента, если не указана)
- `edgePadding`: Дополнительный отступ от краёв границ
- `constraintX`, `constraintY`, `constraintZ`: Включение/отключение ограничения по каждой оси
- `autoUpdateBounds`: Автопересчет bounds каждый кадр (нужно для движущихся/анимированных источников границ)
- `showDebugGizmos`: Визуализация границ в редакторе

**Публичные методы (API для кода)**

### Управление границами
- `UpdateBounds()`: Пересчитать границы вручную
- `SetBoundsSource(BoundsType type, Object source)`: Изменить источник границ в runtime
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

// Смена источника на BoxCollider2D
constraint.SetBoundsSource(CameraConstraint.BoundsType.BoxCollider2D, levelBounds2D);
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
2. Выберите `BoundsType` = `BoxCollider`
3. Добавьте невидимый `BoxCollider` на уровень и перетащите его в поле `boxCollider`
5. Настройте ограничения по осям (`constraintX/Y/Z`)
6. Если глубина камеры не должна меняться, оставьте `constraintZ = false` (рекомендуется по умолчанию)

### С использованием BoxCollider2D
Удобно для сложных форм уровня:
1. Создайте GameObject с `BoxCollider2D`
2. Настройте размер коллайдера под границы уровня
3. Выберите `BoundsType` = `BoxCollider2D`
4. Перетащите коллайдер в поле `boxCollider2D`

---

## 4. Особенности

### Адаптация к типу камеры
- **Ортографическая**: Использует `orthographicSize` и `aspect` для расчёта видимой области
- **Перспективная**: Использует `fieldOfView` и расстояние до уровня для расчёта области

### Визуализация
В режиме редактора (когда `showDebugGizmos` включен):
- Голубая рамка показывает исходные границы источника (`SpriteRenderer`/`BoxCollider2D`/`BoxCollider`)
- Зеленая рамка показывает допустимую область движения камеры после учета размера видимой области и `edgePadding`
- Желтая сфера показывает clamp-позицию камеры
- Для перспективной камеры объёмная рамка по `Z` рисуется только когда включен `constraintZ`

### Работа с другими компонентами
Используйте вместе с `Follow` для создания камеры, следующей за игроком с ограничением по уровню. `CameraConstraint` выполняется в `LateUpdate`, поэтому работает после `Follow`.

---

## 5. Практические рекомендации

- Для 2D почти всегда используйте `constraintX = true`, `constraintY = true`, `constraintZ = false`.
- Для динамического зума или смены aspect включайте `autoUpdateBounds`, чтобы ограничения пересчитывались автоматически.
- Если уровень статичный и важна производительность, можно выключить `autoUpdateBounds` и вызывать `UpdateBounds()` вручную после изменения параметров камеры/границ.
