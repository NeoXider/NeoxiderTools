# UnityEventDelegateCache

## 1. Введение

`UnityEventDelegateCache` — класс для корректной отписки от `UnityEvent` при динамических подписках по индексу. В Unity при вызове `RemoveListener` нужно передать **тот же экземпляр** делегата, что и в `AddListener`; если каждый раз создавать новую лямбду `() => Handler(i)`, отписаться по ссылке нельзя. Кэш хранит ссылки на делегаты и позволяет снимать подписки по индексу.

Типичные сценарии: кнопки покупки в магазине (по индексу товара), списки кнопок, вкладки, элементы UI по id.

---

## 2. Описание класса

- **Пространство имён:** `Neo.Extensions`
- **Путь:** `Assets/Neoxider/Scripts/Extensions/UnityEventDelegateCache.cs`

### Свойства

| Член | Описание |
|------|----------|
| `Count` | Количество закэшированных делегатов. |
| `this[int index]` | Возвращает делегат по индексу (для ручного `RemoveListener`). |

### Методы

| Метод | Описание |
|-------|----------|
| `Add(UnityAction action)` | Добавляет делегат в кэш. Далее его можно передать в `AddListener` и снять через `UnsubscribeAt` по индексу. |
| `SubscribeAt(int index, UnityEvent evt, UnityAction action)` | Подписывает `evt` на `action` и сохраняет делегат в кэше по `index` (при необходимости расширяет кэш). |
| `UnsubscribeAt(int index, UnityEvent evt)` | Отписывает от `evt` делегат, сохранённый по `index`. |
| `Clear()` | Очищает кэш. Подписки с событий не снимает — перед `Clear()` нужно вызвать `UnsubscribeAt` для каждого индекса. |

---

## 3. Пример использования

### Вариант 1: SubscribeAt / UnsubscribeAt

```csharp
private UnityEventDelegateCache _cache = new UnityEventDelegateCache();

void SubscribeButtons()
{
    _cache.Clear();
    for (int i = 0; i < _buttons.Length; i++)
    {
        int id = i;
        _cache.SubscribeAt(i, _buttons[i].onClick, () => OnButtonClicked(id));
    }
}

void UnsubscribeButtons()
{
    for (int i = 0; i < _buttons.Length && i < _cache.Count; i++)
        _cache.UnsubscribeAt(i, _buttons[i].onClick);
    _cache.Clear();
}
```

### Вариант 2: Add + индексер

```csharp
private UnityEventDelegateCache _cache = new UnityEventDelegateCache();

void Subscribe()
{
    _cache.Clear();
    for (int i = 0; i < _items.Length; i++)
    {
        int id = i;
        UnityAction a = () => Buy(id);
        _cache.Add(a);
        _items[i].buttonBuy.onClick.AddListener(a);
    }
}

void Unsubscribe()
{
    for (int i = 0; i < _items.Length && i < _cache.Count; i++)
        _items[i].buttonBuy.onClick.RemoveListener(_cache[i]);
    _cache.Clear();
}
```

---

## 4. См. также

- [Shop](../../Scripts/Shop/Shop.cs) — использование кэша для кнопок покупки (`Subscriber(true/false)`).
- [Extensions README](./README.md) — список утилит модуля.
