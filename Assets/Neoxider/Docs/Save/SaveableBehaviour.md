# SaveableBehaviour

**Что это:** абстрактный класс (пространство имён `Neo.Save`, файл `Assets/Neoxider/Scripts/Save/SaveableBehaviour.cs`). Базовый класс для компонентов, данные которых сохраняет [SaveManager](SaveManager.md). Реализует `ISaveableComponent`, при `OnEnable` сам регистрируется в SaveManager.

**Как с ним работать:**
1. Наследовать свой компонент от `SaveableBehaviour` вместо `MonoBehaviour`.
2. Поля, которые нужно сохранять, пометить атрибутом `[SaveField("ключ")]` (ключ — уникальное имя в рамках компонента).
3. При необходимости переопределить `OnDataLoaded()` — он вызывается после загрузки данных (обновить UI, состояние).
4. На сцене должен быть [SaveManager](SaveManager.md).

---

## Ключевые особенности
- **Авто-регистрация**: В методе `OnEnable` компонент автоматически регистрирует себя в `SaveManager`.
- **Готовая реализация**: Предоставляет пустую виртуальную реализацию метода `OnDataLoaded()`, которую можно переопределить (`override`) в дочернем классе, если требуется выполнить действия после загрузки данных.

## Пример использования
```csharp
// Просто наследуемся от SaveableBehaviour вместо MonoBehaviour
public class PlayerScore : SaveableBehaviour
{
    [SaveField("score")]
    private int _score;

    // Переопределяем метод, если нужно что-то сделать после загрузки
    public override void OnDataLoaded()
    {
        Debug.Log($"Score loaded: {_score}");
        // UpdateScoreUI();
    }
}
```
