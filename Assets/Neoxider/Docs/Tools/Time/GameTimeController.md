# GameTimeController

**Назначение:** Утилита для управления скоростью времени в игре (`Time.timeScale`). Удобно использовать для постановки игры на паузу или создания эффекта замедления времени (slow-mo) через `UnityEvent`.

## Поля (Inspector)

| Поле | Описание |
|------|----------|
| **Reset On Awake** | Автоматически возвращать `Time.timeScale` в 1 при активации или удалении этого скрипта (защита от вечной паузы при загрузке новой сцены). |

## API

| Метод / Свойство | Описание |
|------------------|----------|
| `void PauseGame()` | Жестко устанавливает `Time.timeScale = 0`. |
| `void ResumeGame()` | Возвращает нормальный ход времени (`Time.timeScale = 1`). |
| `void SetTimeScale(float scale)` | Устанавливает кастомную скорость времени (например, `0.5` для замедления). |

## Примеры

### Пример No-Code (в Inspector)
На кнопке "Пауза" в UI добавьте `GameTimeController.PauseGame()` в событие `OnClick`. На кнопке "Продолжить" вызовите `GameTimeController.ResumeGame()`.

### Пример (Код)
```csharp
[SerializeField] private GameTimeController _timeController;

public void EnterBulletTime()
{
    // Замедляем время в 5 раз для красивого выстрела
    _timeController.SetTimeScale(0.2f);
}
```

## См. также
- [TimerObject](TimerObject.md)
- ← [Tools/Time](../README.md)
