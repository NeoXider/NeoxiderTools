# CollectionVisualManager

**Назначение:** Синглтон визуализации коллекции. Связывает данные из `Collection` (логика) с массивом `ItemCollection` (UI-элементы). При изменении коллекции автоматически обновляет иконки и состояние кнопок.

**Добавить:** Neoxider → Bonus → CollectionVisualManager.

---

## Поля (Inspector)

| Поле | Описание |
|------|----------|
| **Items** (`_items`) | Массив `ItemCollection` — UI-элементы для каждого предмета коллекции. Заполняется автоматически через `[GetComponents]`. |
| **Enable Set Item** (`_enableSetItem`) | Разрешить выбор предмета по нажатию кнопки. По умолчанию `true`. |

---

## API

| Метод / Свойство | Описание |
|------------------|----------|
| `ItemCollection[] Items { get; }` | Массив всех UI-элементов коллекции. |
| `int ItemsCount { get; }` | Количество элементов в массиве. |
| `bool EnableSetItem { get; set; }` | Разрешён ли выбор предмета по клику. |
| `void Visual()` | Обновить отображение всех предметов по данным `Collection`. |
| `void UpdateItemVisibility(int id)` | Обновить отображение одного предмета по индексу. |
| `void SetItem(int id)` | Выбрать предмет по индексу (вызывает `OnSetItem`). |
| `void SetItem(ItemCollection item)` | Выбрать предмет по ссылке. |
| `ItemCollection GetItem(int id)` | Получить UI-элемент по индексу. |
| `void RefreshAllItems()` | Принудительно обновить все элементы. |
| `void RefreshItem(int id)` | Принудительно обновить один элемент. |

---

## Unity Events

| Событие | Описание |
|---------|----------|
| **On Set Item** (`UnityEvent<int>`) | Вызывается при выборе предмета. Параметр — индекс предмета. Срабатывает только для собранных предметов (если `EnableSetItem = true`). |

---

## Примеры

### No-Code (Inspector)
1. Добавить `CollectionVisualManager` на объект через меню.
2. Массив **Items** заполнится автоматически дочерними `ItemCollection`.
3. В **On Set Item** привязать метод, например `OpenItemDetails(int id)`.
4. Предметы обновятся автоматически при изменении коллекции.

### Код
```csharp
// Обновить все элементы
CollectionVisualManager.I.Visual();

// Проверить количество
int count = CollectionVisualManager.I.ItemsCount;

// Получить конкретный элемент
ItemCollection item = CollectionVisualManager.I.GetItem(3);
```

---

## См. также
- [Collection](./README.md) — синглтон логики коллекции
- [ItemCollection](./ItemCollection.md) — UI-элемент одного предмета
- ← [Bonus](../README.md)