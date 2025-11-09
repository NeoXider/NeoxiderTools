### Класс CollectionVisualManager
- **Пространство имен**: `Neo.Bonus`
- **Путь к файлу**: `Assets/Neoxider/Scripts/Bonus/Collection/CollectionVisualManager.cs`

**Краткое описание**:
`CollectionVisualManager` — это синглтон, который управляет визуальным отображением всех предметов коллекции. Он связывает данные из `Collection` с визуальными элементами `ItemCollection` и обрабатывает их выбор. Наследуется от `Singleton<CollectionVisualManager>` для обеспечения единственного экземпляра в сцене.

**Ключевые особенности**:
- **Синглтон**: Обеспечивает глобальный доступ к менеджеру визуализации через `CollectionVisualManager.I` (наследуется от `Singleton<CollectionVisualManager>`).
- **Автоматическое обновление**: Обновляет состояние всех видимых предметов при включении объекта и при изменении коллекции.
- **Синхронизация с Collection**: Автоматически подписывается на события `Collection` (`OnItemAdded`, `OnItemRemoved`, `OnLoadItems`) для обновления визуализации в реальном времени.
- **Обработка выбора**: Подписывается на события нажатия кнопок у `ItemCollection` и вызывает событие `OnSetItem` при выборе предмета.
- **Безопасная инициализация**: Ожидает инициализации `Collection` перед обновлением визуализации, предотвращая баги с отображением.

**Публичные свойства**:
- `I`: `static CollectionVisualManager` - Глобальная статическая ссылка на экземпляр класса (через `Singleton<T>`).
- `IsInitialized`: `static bool` - Проверка инициализации синглтона.
- `Items`: `ItemCollection[]` (read-only) - Массив всех визуальных элементов коллекции.
- `ItemsCount`: `int` - Количество визуальных элементов.
- `EnableSetItem`: `bool` - Разрешение выбора предметов. Если `false`, предметы нельзя выбрать.

**Публичные методы**:
- `Visual()`: Обновляет визуальное состояние всех предметов коллекции, синхронизируя их с данными из `Collection`. Проверяет инициализацию `Collection` перед обновлением.
- `UpdateItemVisibility(int id)`: Обновляет визуальное состояние одного конкретного предмета по его ID. Включает проверки на валидность ID и инициализацию.
- `SetItem(int id)`: Обрабатывает выбор предмета. Вызывает событие `OnSetItem`, если предмет доступен (уже получен) и `EnableSetItem` разрешает выбор.
- `SetItem(ItemCollection itemCollection)`: Перегрузка метода для выбора предмета по ссылке на его компонент `ItemCollection`.
- `GetItem(int id)`: Возвращает визуальный элемент (`ItemCollection`) по ID или `null`, если ID невалиден.
- `RefreshAllItems()`: Обновляет все элементы коллекции (аналог `Visual()`).
- `RefreshItem(int id)`: Обновляет конкретный элемент коллекции (аналог `UpdateItemVisibility()`).

**Unity Events**:
- `OnSetItem`: `UnityEvent<int>` - Вызывается при выборе доступного предмета коллекции. Передает `int` (ID выбранного предмета).

**Внутренние механизмы**:
- Автоматически подписывается на события `Collection` при инициализации.
- Использует корутину `WaitForCollectionAndVisual()` для ожидания инициализации `Collection`, если она еще не завершена.
- Автоматически обновляет визуализацию при изменении коллекции через события `OnItemAdded` и `OnItemRemoved`.

**Примеры использования**:
```csharp
// Обновление всех элементов
CollectionVisualManager.I.RefreshAllItems();

// Обновление конкретного элемента
CollectionVisualManager.I.RefreshItem(2);

// Получение элемента
var item = CollectionVisualManager.I.GetItem(0);

// Подписка на выбор предмета
CollectionVisualManager.I.OnSetItem.AddListener((id) => {
    Debug.Log($"Выбран предмет {id}");
});
```
