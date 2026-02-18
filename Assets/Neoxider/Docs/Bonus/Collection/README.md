# Модуль "Коллекции" (Collection)

Этот модуль предоставляет систему для создания и управления коллекционными предметами. Он позволяет отслеживать собранные предметы, отображать их в UI, а также включает механику открытия "коробок" или "контейнеров" с предметами.

## Ключевые возможности

- ✅ **Singleton-паттерн**: Использует `Singleton<T>` для глобального доступа через `Collection.I` и `CollectionVisualManager.I`
- ✅ **Полный API для работы через код**: Добавление, удаление, проверка предметов программно
- ✅ **Автоматическая синхронизация**: `CollectionVisualManager` автоматически обновляется при изменении коллекции
- ✅ **Безопасная инициализация**: Предотвращает баги с отображением при неправильном порядке инициализации
- ✅ **События**: Подписка на изменения коллекции через UnityEvents; при изменении состава вызываются `OnCompletionChanged` (unlocked, total) и `OnCompletionPercentageChanged` (0–1)
- ✅ **Сохранение прогресса**: Автоматическое сохранение через **SaveProvider** (единое хранилище с остальной системой сохранений)
- ✅ **Несколько коллекций**: На дополнительных GameObject с `Collection` выставите **Set Instance On Awake = false** — экземпляр не будет регистрироваться как `Collection.I`. Работайте с ним по ссылке (поле Collection в ItemCollectionInfo, ItemCollection). Свойство `IsSingleton` показывает, является ли экземпляр глобальным.

## Оглавление

### Основные компоненты
- [**Collection**](./Collection.md): Главный класс-синглтон, управляющий логикой всей коллекции. Предоставляет полный API для работы через код.
- [**CollectionVisualManager**](./CollectionVisualManager.md): Синглтон, управляющий визуальным отображением всех предметов коллекции. Автоматически синхронизируется с `Collection`.
- [**ItemCollection**](./ItemCollection.md): Визуальное представление одного коллекционного предмета в UI.
- [**Box**](./Box.md): Реализует логику для "коробки" или "контейнера", который можно открыть.

### Данные и структуры
- [**ItemCollectionData**](./ItemCollectionData.md): `ScriptableObject` для хранения данных о коллекционных предметах.
- [**ItemCollectionInfo**](./ItemCollectionInfo.md): Компонент для отображения детальной информации о предмете (название, описание, изображение).

## Быстрый старт

1. **Создайте данные предметов**: Создайте `ItemCollectionData` через меню `Create > Neoxider > Bonus > Collection > Item Collection Data`
2. **Настройте Collection**: Добавьте компонент `Collection` на GameObject в сцене и заполните массив `ItemCollectionDatas`
3. **Настройте UI**: Создайте UI элементы с компонентом `ItemCollection` и добавьте `CollectionVisualManager` для управления ими
4. **Используйте через код**:
```csharp
// Добавление предмета
Collection.I.AddItem(0);

// Проверка наличия
if (Collection.I.HasItem(0)) { ... }

// Получение статистики
Debug.Log($"Разблокировано: {Collection.I.UnlockedCount}/{Collection.I.ItemCount}");
Debug.Log(Collection.I.GetCompletionCountText()); // "1/5"
Debug.Log(Collection.I.GetCompletionPercentage()); // 0–1

// Подписка на изменение прогресса (без кода)
// Collection.I.OnCompletionChanged (int unlocked, int total)
// Collection.I.OnCompletionPercentageChanged (float 0–1)
```

## Примеры использования

### Работа через код
```csharp
// Проверка и добавление предметов
if (!Collection.I.HasItem(5))
{
    Collection.I.AddItem(5);
    Debug.Log("Предмет 5 добавлен!");
}

// Подписка на события
Collection.I.OnItemAdded.AddListener((id) => {
    Debug.Log($"Новый предмет {id} добавлен в коллекцию!");
    CollectionVisualManager.I.RefreshItem(id);
});

// Разблокировка всех предметов (для тестирования)
Collection.I.UnlockAllItems();

// Добавление по данным или по индексу
Collection.I.AddItem(itemCollectionData);
bool added = Collection.I.TryAddItem(3);

// Разблокировка из UI: на кнопке карточки вызовите ItemCollection.Unlock() через UnityEvent
```

### Работа с визуализацией
```csharp
// Обновление всех элементов
CollectionVisualManager.I.RefreshAllItems();

// Обновление конкретного элемента
CollectionVisualManager.I.RefreshItem(2);

// Получение элемента
var item = CollectionVisualManager.I.GetItem(0);
if (item != null && item.IsEnabled)
{
    Debug.Log("Предмет 0 разблокирован и отображается");
}

// Фильтрация
int[] unlocked = Collection.I.GetUnlockedIds();
int[] locked = Collection.I.GetLockedIds();
int[] byCategory = Collection.I.GetIdsByCategory(1);
int[] byRarity = Collection.I.GetIdsByRarity(ItemRarity.Epic);
int countByRarity = Collection.I.GetUnlockedCountByRarity(ItemRarity.Legendary);
```
