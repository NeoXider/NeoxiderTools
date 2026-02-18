# Предложения по улучшению модуля Collection

Этот документ содержит предложения по улучшению модуля Collection. Предложения разделены по категориям с вариантами реализации.

---

## Реализовано (текущая версия)

- **Статистика и завершение:** `GetCompletionPercentage()` (0–1), `GetCompletionCountText()` ("1/5"), события `OnCompletionChanged` (int, int), `OnCompletionPercentageChanged` (float).
- **Фильтрация:** `GetUnlockedIds()`, `GetLockedIds()`, `GetIdsByCategory(int)`, `GetIdsByRarity(ItemRarity)`, `GetIdsByType(int)`; `GetUnlockedCountByCategory`, `GetUnlockedCountByRarity`, `GetUnlockedCountByType`.
- **Удобные методы:** `AddItem(ItemCollectionData)`, `TryAddItem(int)`; в **ItemCollection** — `SetItemId(int)`, `Unlock()`, поле `Collection` (fallback на `Collection.I`). В **CollectionVisualManager** при обновлении вызывается `SetItemId(id)`.
- **Несколько коллекций:** На втором и далее компоненте `Collection` выставить **Set Instance On Awake = false** (в инспекторе Singleton). Тогда экземпляр не регистрируется как `Collection.I`; работа по ссылке (поле Collection в ItemCollectionInfo / ItemCollection). Свойство `Collection.IsSingleton` — признак глобального экземпляра.

---

## 1. Расширение функциональности: Категории, Редкость, Статистика

### 1.1. Редкость предметов (ItemRarity)

#### Вариант A: Простой enum (рекомендуется)
```csharp
public enum ItemRarity
{
    Common = 0,    // Обычный
    Rare = 1,      // Редкий
    Epic = 2,      // Эпический
    Legendary = 3  // Легендарный
}
```

**Плюсы:**
- Простота использования
- Легко добавить в ItemCollectionData
- Минимальные изменения кода

**Минусы:**
- Фиксированный набор редкостей
- Нет числового значения для сортировки

**Использование:**
```csharp
[SerializeField] private ItemRarity _rarity = ItemRarity.Common;
public ItemRarity rarity => _rarity;
```

---

#### Вариант B: Enum + числовое значение
```csharp
public enum ItemRarity
{
    Common = 0,
    Rare = 1,
    Epic = 2,
    Legendary = 3
}

// В ItemCollectionData:
[SerializeField] private ItemRarity _rarity = ItemRarity.Common;
[SerializeField] private int _rarityValue = 0; // 0-100 для сортировки
```

**Плюсы:**
- Гибкость в сортировке
- Можно задать приоритет внутри одной редкости

**Минусы:**
- Дополнительное поле для поддержки

---

#### Вариант C: ScriptableObject для редкостей
```csharp
[CreateAssetMenu(fileName = "Item Rarity Data", menuName = "Neo/Bonus/Collection/Item Rarity Data", order = 11)]
public class ItemRarityData : ScriptableObject
{
    public string rarityName;
    public Color rarityColor;
    public int rarityValue;
    public Sprite rarityIcon;
}
```

**Плюсы:**
- Максимальная гибкость
- Можно добавить цвета, иконки, эффекты

**Минусы:**
- Сложнее в использовании
- Нужно создавать ассеты для каждой редкости

**Рекомендация:** Вариант A для начала, можно расширить до B при необходимости.

---

### 1.2. Категории предметов (ItemCategory)

#### Вариант A: Числовой ID (рекомендуется)
```csharp
// В ItemCollectionData:
[SerializeField] private int _category = 0; // 0 = без категории
public int category => _category;
```

**Плюсы:**
- Простота
- Быстрая фильтрация
- Легко добавить в код

**Минусы:**
- Нужно помнить числовые ID
- Нет названий категорий в данных

**Использование:**
```csharp
// Категории можно определить как константы:
public static class CollectionCategories
{
    public const int None = 0;
    public const int Weapons = 1;
    public const int Armor = 2;
    public const int Consumables = 3;
}
```

---

#### Вариант B: Строковое название
```csharp
[SerializeField] private string _category = "";
public string category => _category;
```

**Плюсы:**
- Гибкость
- Понятные названия в инспекторе
- Легко добавлять новые категории

**Минусы:**
- Медленнее фильтрация (строковое сравнение)
- Возможны опечатки

---

#### Вариант C: Enum категорий
```csharp
public enum ItemCategory
{
    None = 0,
    Weapons = 1,
    Armor = 2,
    Consumables = 3,
    // ...
}
```

**Плюсы:**
- Типобезопасность
- Автодополнение в IDE
- Нет опечаток

**Минусы:**
- Нужно изменять enum при добавлении категорий
- Перекомпиляция при изменении

---

#### Вариант D: ScriptableObject для категорий
```csharp
[CreateAssetMenu(fileName = "Item Category Data", menuName = "Neo/Bonus/Collection/Item Category Data", order = 12)]
public class ItemCategoryData : ScriptableObject
{
    public string categoryName;
    public Sprite categoryIcon;
    public Color categoryColor;
}
```

**Плюсы:**
- Максимальная гибкость
- Можно добавить иконки, цвета
- Легко расширять

**Минусы:**
- Сложнее в использовании
- Нужно создавать ассеты

**Рекомендация:** Вариант A (числовой ID) для простоты, можно перейти на B (строка) при необходимости гибкости.

---

### 1.3. Теги для фильтрации

#### Вариант A: Массив строк
```csharp
[SerializeField] private string[] _tags = new string[0];
public string[] tags => _tags;
```

**Плюсы:**
- Простота
- Гибкость (любое количество тегов)

**Минусы:**
- Нет автодополнения
- Возможны опечатки

---

#### Вариант B: Enum флаги (если тегов немного)
```csharp
[System.Flags]
public enum ItemTags
{
    None = 0,
    New = 1,
    Featured = 2,
    Limited = 4,
    Seasonal = 8
}
```

**Плюсы:**
- Типобезопасность
- Быстрая проверка (битовые операции)

**Минусы:**
- Ограниченное количество тегов (32 для int)
- Нужно изменять enum

**Рекомендация:** Вариант A для гибкости, если тегов немного - можно использовать B.

---

### 1.4. Статистика и фильтрация

#### Вариант A: Методы в Collection (рекомендуется)

Добавить методы прямо в класс `Collection`:

```csharp
// Фильтрация
public int[] GetItemsByCategory(int category)
public int[] GetItemsByRarity(ItemRarity rarity)
public int[] GetUnlockedItems()
public int[] GetLockedItems()

// Статистика
public float GetCompletionPercentage()
public int GetUnlockedCountByCategory(int category)
public int GetUnlockedCountByRarity(ItemRarity rarity)
public int GetUnlockedCountByType(int type)
```

**Плюсы:
- Простота использования
- Все в одном месте
- Легко найти методы

**Минусы:**
- Класс может стать большим
- Смешивание логики коллекции и статистики

---

#### Вариант B: Отдельный класс CollectionStatistics

```csharp
public class CollectionStatistics
{
    private Collection _collection;
    
    public CollectionStatistics(Collection collection)
    {
        _collection = collection;
    }
    
    public float GetCompletionPercentage() { ... }
    public int GetUnlockedCountByCategory(int category) { ... }
    // и т.д.
}

// В Collection:
public CollectionStatistics Statistics { get; private set; }

// Использование:
Collection.I.Statistics.GetCompletionPercentage();
```

**Плюсы:**
- Разделение ответственности
- Чистый код
- Легко тестировать

**Минусы:**
- Дополнительный класс
- Немного сложнее доступ

---

#### Вариант C: Extension методы

```csharp
public static class CollectionExtensions
{
    public static float GetCompletionPercentage(this Collection collection) { ... }
    public static int[] GetItemsByCategory(this Collection collection, int category) { ... }
    // и т.д.
}

// Использование:
Collection.I.GetCompletionPercentage();
```

**Плюсы:**
- Не засоряет основной класс
- Можно добавлять в разных местах

**Минусы:**
- Менее очевидно для пользователей
- Нужно импортировать namespace

**Рекомендация:** Вариант A для простоты, можно перейти на B при росте функциональности.

---

### 1.5. Дополнительные поля (опционально)

#### Дата получения предмета
```csharp
[System.Serializable]
public class ItemUnlockInfo
{
    public bool isUnlocked;
    public System.DateTime unlockDate;
}

// В Collection:
private ItemUnlockInfo[] _itemUnlockInfos;
```

**Использование:**
- Отслеживание прогресса
- Статистика "первый/последний предмет"
- Аналитика

---

#### Количество предметов (если нужны дубликаты)
```csharp
// В ItemCollectionData:
[SerializeField] private bool _allowMultiple = false; // разрешить несколько экземпляров
[SerializeField] private int _maxQuantity = 1;

// В Collection:
private int[] _itemQuantities; // количество каждого предмета
```

**Использование:**
- Коллекционные карточки (можно иметь несколько)
- Ресурсы (монеты, кристаллы)

---

## 2. Рекомендуемый минимальный набор улучшений

Для быстрого старта рекомендуется реализовать:

1. **ItemRarity enum** (Вариант A)
   - Common, Rare, Epic, Legendary
   - Простое добавление в ItemCollectionData

2. **ItemCategory int** (Вариант A)
   - Числовой ID категории
   - Константы для категорий

3. **Методы фильтрации в Collection** (Вариант A)
   - `GetItemsByCategory(int category)`
   - `GetItemsByRarity(ItemRarity rarity)`
   - `GetUnlockedItems()`
   - `GetLockedItems()`

4. **Методы статистики в Collection** (Вариант A)
   - `GetCompletionPercentage()`
   - `GetUnlockedCountByCategory(int category)`
   - `GetUnlockedCountByRarity(ItemRarity rarity)`

Этот набор даст основную функциональность без излишней сложности.

---

## 3. Примеры использования после улучшений

### Фильтрация по категории
```csharp
var weapons = Collection.I.GetItemsByCategory(CollectionCategories.Weapons);
foreach (var id in weapons)
{
    var item = Collection.I.GetItemData(id);
    Debug.Log($"Оружие: {item.ItemName}");
}
```

### Статистика по редкости
```csharp
var epicCount = Collection.I.GetUnlockedCountByRarity(ItemRarity.Epic);
var legendaryCount = Collection.I.GetUnlockedCountByRarity(ItemRarity.Legendary);
Debug.Log($"Эпических: {epicCount}, Легендарных: {legendaryCount}");
```

### Процент завершения
```csharp
var completion = Collection.I.GetCompletionPercentage();
Debug.Log($"Коллекция завершена на {completion:F1}%");
```

### Визуализация редкости
```csharp
var itemData = Collection.I.GetItemData(id);
switch (itemData.Rarity)
{
    case ItemRarity.Common:
        // Серый цвет рамки
        break;
    case ItemRarity.Rare:
        // Синий цвет рамки
        break;
    case ItemRarity.Epic:
        // Фиолетовый цвет рамки + эффект свечения
        break;
    case ItemRarity.Legendary:
        // Золотой цвет рамки + частицы
        break;
}
```

---

## 4. Дальнейшие улучшения (опционально)

После базовой реализации можно добавить:

1. **Теги** (массив строк) - для более гибкой фильтрации
2. **Дата получения** - для статистики и аналитики
3. **Количество предметов** - если нужны дубликаты
4. **ScriptableObject для категорий** - если нужны иконки и цвета
5. **Отдельный класс Statistics** - если методов станет слишком много

---

## 5. Файлы для изменения

### Обязательные:
- `Assets/Neoxider/Scripts/Bonus/Collection/ItemCollectionData.cs` - добавить поля редкости и категории
- `Assets/Neoxider/Scripts/Bonus/Collection/Collection.cs` - добавить методы фильтрации и статистики

### Опциональные:
- `Assets/Neoxider/Scripts/Bonus/Collection/CollectionStatistics.cs` - новый класс (если выбран Вариант B)
- `Assets/Neoxider/Scripts/Bonus/Collection/ItemRarity.cs` - enum редкости (можно в том же файле)
- `Assets/Neoxider/Scripts/Bonus/Collection/CollectionCategories.cs` - константы категорий

### Документация:
- Обновить `Collection.md` с новыми методами
- Обновить `ItemCollectionData.md` с новыми полями
- Добавить примеры в `README.md`

