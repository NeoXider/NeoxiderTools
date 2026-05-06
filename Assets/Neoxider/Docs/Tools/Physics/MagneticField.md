# MagneticField

**Назначение:** Создает магнитное поле, которое притягивает, отталкивает объекты или тянет их в заданном направлении/к конкретной точке. Автоматически работает с объектами, попавшими в его радиус (через `Physics.OverlapSphere`).

## Поля (Inspector)

| Поле | Описание |
|------|----------|
| **Mode** | Режим поля: `Attract` (притягивать к себе), `Repel` (отталкивать), `ToTarget` (притягивать к объекту), `ToPoint` (к точке), `Direction` (постоянная тяга в направлении). |
| **Field Strength** / **Radius** | Сила магнита и радиус его действия. |
| **Falloff Type** | Затухание силы с расстоянием (`Linear`, `Quadratic`, `Constant`). |
| **Affected Layers** | Маска слоев, на которые воздействует магнит. |
| **Toggle** | Автоматически чередовать притяжение и отталкивание (например, 2 сек тянет, 2 сек отталкивает). |
| **Add Rigidbody If Needed** | Если объект попал в поле, но у него нет `Rigidbody`, скрипт добавит его автоматически. |

## API

| Метод / Свойство | Описание |
|------------------|----------|
| `void ToggleMode()` | Ручное переключение между режимами притяжения и отталкивания. |
| `void SetTarget(Transform target)` | Изменить цель для режима `ToTarget`. |
| `void SetDirection(Vector3 newDirection, bool local = true)` | Задать новый вектор для режима `Direction`. |
| `int ObjectsInFieldCount { get; }` | Количество объектов, которые прямо сейчас находятся под влиянием поля. |

## Unity Events

| Событие | Аргументы | Описание |
|---------|-----------|----------|
| `OnObjectEntered` | `GameObject` | Объект вошел в радиус действия магнитного поля. |
| `OnObjectExited` | `GameObject` | Объект покинул магнитное поле. |
| `OnModeChanged` | `bool` | Вызывается при смене фазы (если включен параметр `Toggle`). `True` = Притяжение. |

## Примеры

### Пример No-Code (в Inspector)
Разместите пустой объект перед вентилятором, добавьте `MagneticField`, выберите режим `Direction`. Настройте вектор так, чтобы он указывал от вентилятора. Включите `Falloff Type = Linear`. Вентилятор будет реалистично "сдувать" физические коробки.

### Пример (Код)
```csharp
[SerializeField] private MagneticField _blackHole;

public void SuperchargeBlackHole()
{
    _blackHole.SetStrength(1000f);
    _blackHole.SetRadius(50f);
    Debug.Log("Внимание! Мощность черной дыры увеличена.");
}
```

## См. также
- [ImpulseZone](ImpulseZone.md)
- [ExplosiveForce](ExplosiveForce.md)
- ← [Tools/Physics](../README.md)
