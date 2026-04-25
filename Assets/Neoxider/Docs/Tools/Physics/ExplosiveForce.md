# ExplosiveForce

**Назначение:** Одноразовый взрыв, который физически раскидывает объекты в заданном радиусе (использует `Rigidbody.AddExplosionForce`). Поддерживает задержки, случайный разброс силы и автоматическое уничтожение после срабатывания.

## Поля (Inspector)

| Поле | Описание |
|------|----------|
| **Activation Mode** | Когда взрываться: `OnStart`, `OnAwake`, `Delayed` (с задержкой), `Manual` (только из кода/события). |
| **Delay** | Задержка перед взрывом (если выбран режим `Delayed` или `OnStart` с задержкой). |
| **Force** / **Force Randomness** | Базовая сила взрыва и случайный разброс (добавочная сила `±Randomness`). |
| **Force Mode** | `AddExplosionForce` (стандартный физический взрыв) или `AddForce` (просто линейный импульс от центра). |
| **Falloff Type** | Затухание силы к краям радиуса: `Linear` или `Quadratic`. |
| **Destroy After Explosion** | Удалить этот GameObject со сцены сразу после того, как он взорвался (или через `Destroy Delay`). |

## API

| Метод / Свойство | Описание |
|------------------|----------|
| `void Explode()` | Вызвать взрыв немедленно (с базовой силой). |
| `void Explode(float customForce)` | Взорвать с переданной силой, проигнорировав настройки инспектора. |
| `void ResetExplosion()` | Сбросить флаг `HasExploded`, чтобы взрыв мог сработать снова. |

## Unity Events

| Событие | Аргументы | Описание |
|---------|-----------|----------|
| `OnExplode` | *(нет)* | Вызывается в момент взрыва (удобно для спавна частиц и звуков). |
| `OnObjectAffected` | `GameObject` | Вызывается для каждого отдельного объекта, которого откинуло взрывом. |

## Примеры

### Пример No-Code (в Inspector)
Создайте префаб гранаты (без гравитации, просто пустышку). Повесьте на него `ExplosiveForce`. Настройте `Activation Mode = Delayed`, `Delay = 3`, `Destroy After = true`. В событии `OnExplode` вызовите метод `SimpleSpawner.Spawn()` для создания визуального эффекта взрыва. При спавне этой гранаты она взорвется через 3 секунды, раскидает бочки и удалится.

### Пример (Код)
```csharp
[SerializeField] private ExplosiveForce _mineExplosion;

public void StepOnMine()
{
    // Принудительно вызываем взрыв мины
    _mineExplosion.Explode();
}
```

## См. также
- [MagneticField](MagneticField.md)
- [ImpulseZone](ImpulseZone.md)
- ← [Tools/Physics](../README.md)