# ImpulseZone

**Назначение:** Зона-триггер (Collider), которая придает мгновенный физический импульс объектам, вошедшим в нее. Идеально подходит для батутов, трамплинов, потоков ветра или ускоряющих площадок.

## Поля (Inspector)

| Поле | Описание |
|------|----------|
| **Impulse Force** | Сила импульса (ForceMode.Impulse). |
| **Direction** | Направление толчка: `AwayFromCenter` (отталкивать от центра), `TowardsCenter` (притягивать), `TransformForward` (вперед), `Custom` (заданный вектор). |
| **Affected Layers** | Слои объектов, которые зона может толкать. |
| **Required Tag** | (Опционально) Толкать только объекты с конкретным тегом (например, `Player`). |
| **One Time Only** | Одноразовое срабатывание для каждого объекта (повторно зайдя, он не получит импульс). |
| **Cooldown** | Задержка в секундах, прежде чем один и тот же объект снова сможет получить импульс. |

## API

| Метод / Свойство | Описание |
|------------------|----------|
| `void ApplyImpulseToObject(GameObject target)` | Принудительно толкнуть переданный объект, как если бы он вошел в триггер. |
| `void SetImpulseForce(float newForce)` | Изменить силу импульса. |
| `void ClearProcessedObjects()` | Сбрасывает историю (для `One Time Only`), позволяя объектам снова получить толчок. |

## Unity Events

| Событие | Аргументы | Описание |
|---------|-----------|----------|
| `OnObjectEntered` | `GameObject` | Объект вошел в триггер, но до проверки фильтров/кулдаунов. |
| `OnImpulseApplied` | `GameObject` | Импульс был успешно применен к объекту. |

## Примеры

### Пример No-Code (в Inspector)
Разместите BoxCollider на земле в форме батута. Поставьте галочку `Is Trigger`. Добавьте `ImpulseZone`, выберите направление `TransformForward` (и поверните триггер вверх), силу `20`. Теперь любой объект с Rigidbody, упавший на эту зону, подпрыгнет.

### Пример (Код)
```csharp
[SerializeField] private ImpulseZone _jumpPad;

public void DisableJumpPad()
{
    // Отключаем трамплин, просто убрав силу
    _jumpPad.SetImpulseForce(0f);
}
```

## См. также
- [MagneticField](MagneticField.md)
- [ExplosiveForce](ExplosiveForce.md)
- ← [Tools/Physics](../README.md)