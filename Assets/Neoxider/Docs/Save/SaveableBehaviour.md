# Базовый класс SaveableBehaviour

## 1. Введение

`SaveableBehaviour` — это удобный абстрактный базовый класс, который упрощает создание сохраняемых компонентов. Вместо того чтобы вручную реализовывать интерфейс `ISaveableComponent` и регистрировать компонент в `SaveManager`, достаточно просто унаследовать свой класс от `SaveableBehaviour`.

Он автоматически обрабатывает регистрацию в `SaveManager` при включении объекта, что избавляет от лишнего повторяющегося кода.

---

## 2. Описание класса

### SaveableBehaviour
- **Пространство имен**: `Neo.Save`
- **Путь к файлу**: `Assets/Neoxider/Scripts/Save/SaveableBehaviour.cs`

**Описание**
Абстрактный класс, реализующий `ISaveableComponent` и автоматическую регистрацию в `SaveManager`.

**Ключевые особенности**
- **Авто-регистрация**: В методе `OnEnable` компонент автоматически регистрирует себя в `SaveManager`.
- **Готовая реализация**: Предоставляет пустую виртуальную реализацию метода `OnDataLoaded()`, которую можно переопределить (`override`) в дочернем классе, если требуется выполнить действия после загрузки данных.

**Пример использования**
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
