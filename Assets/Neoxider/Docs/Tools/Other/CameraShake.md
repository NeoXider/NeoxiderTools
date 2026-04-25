# CameraShake

**Назначение:** Компонент тряски камеры (или любого объекта) на базе DOTween. Поддерживает раздельную тряску позиции и/или поворота, настраиваемую силу, вибрацию, затухание и независимость от `Time.timeScale`.

## Поля (Inspector)

| Поле | Описание |
|------|----------|
| **Shake Type** | Что трясти: `Position`, `Rotation` или `Both`. |
| **Duration** | Длительность тряски (секунды). |
| **Strength** | Амплитуда тряски. |
| **Vibrato** | Количество вибраций (1–50). |
| **Randomness** | Случайность направления (0 = линейная, 180 = полный хаос). |
| **Fade Out** | Плавное затухание тряски к концу. |
| **Shake X / Y / Z** | Какие оси трясти (позиция). |
| **Rotate X / Y / Z** | Какие оси трясти (поворот). |
| **Use Unscaled Time** | Игнорировать `Time.timeScale` (для тряски в паузе). |

## API

| Метод / Свойство | Описание |
|------------------|----------|
| `void StartShake()` | Запустить тряску с настройками из инспектора. |
| `void StartShake(float duration, float strength)` | Запустить с кастомными параметрами. |
| `void StopShake()` | Остановить тряску и вернуть объект в исходное положение. |
| `void ResetTransform()` | Сбросить позицию/поворот в оригинальные значения. |
| `bool IsShaking { get; }` | Трясется ли объект прямо сейчас. |

## Unity Events

| Событие | Аргументы | Описание |
|---------|-----------|----------|
| `OnShakeStart` | *(нет)* | Тряска началась. |
| `OnShakeComplete` | *(нет)* | Тряска закончилась естественным путём. |
| `OnShakeStop` | *(нет)* | Тряска была остановлена вручную (`StopShake()`). |

## Примеры

### Пример No-Code (в Inspector)
Повесьте `CameraShake` на камеру. В событии `OnCollisionEnter` (или в вашем скрипте взрыва) добавьте вызов `CameraShake.StartShake()`. Настройте `Strength = 0.5`, `Duration = 0.3`. При каждом попадании камера будет дрожать.

### Пример (Код)
```csharp
[SerializeField] private CameraShake _cameraShake;

public void OnPlayerHit(float damage)
{
    _cameraShake.StartShake(0.2f, damage * 0.1f);
}
```

## См. также
- ← [Tools/Other](README.md)
