# Урок 5: Sync-коллекции и сериализация данных

**Навигация:** [Оглавление](README.md) · [Старт](00_START_HERE.md) · [Оформление](LESSON_STYLE.md) · базовый трек · урок 5/15 · Mirror `96.x`

| Ключевые слова | `SyncList`, `SyncDictionary`, `SyncHashSet`, Reader, Writer, ID |
|----------------|------------------------------------------------------------------|

---

## Карта урока

| Что | Ответ |
|-----|-------|
| Объект работы | Inventory, scoreboard, party list или match settings. |
| Кто владеет state | Server меняет коллекцию; clients получают операции. |
| Как проверить | Добавить/удалить элемент на server и увидеть одинаковый список у двух clients. |
| Артефакт | Таблица типов: что sync напрямую, что передаём через stable ID. |

---

## Что должно получиться

Вы умеете синхронизировать не одно поле, а набор данных: инвентарь, список игроков, открытые квесты, таблицу счёта.

---

## Проблема

Плохой вариант для инвентаря: сериализовать весь список в JSON и класть строку в `SyncVar`. Это даёт лишний трафик, слабую типобезопасность и дорогой UI update.

---

## Теория коротко

Sync-коллекции передают изменения как операции:

| Коллекция | Когда использовать |
|-----------|--------------------|
| `SyncList<T>` | Упорядоченный список: слоты, очередь, игроки. |
| `SyncDictionary<TKey,TValue>` | Быстрый доступ по ID: предметы, статы, счёт. |
| `SyncHashSet<T>` | Уникальный набор: открытые флаги, достижения. |

Коллекция должна жить в `NetworkBehaviour` на spawned-объекте. Сервер изменяет коллекцию, клиенты получают операции.

---

## Практика

```csharp
using Mirror;

public readonly struct ItemStack
{
    public readonly int itemId;
    public readonly int amount;

    public ItemStack(int itemId, int amount)
    {
        this.itemId = itemId;
        this.amount = amount;
    }
}

public sealed class InventoryState : NetworkBehaviour
{
    public readonly SyncList<ItemStack> slots = new();

    public override void OnStartClient()
    {
        slots.OnChange += OnSlotsChanged;

        for (int i = 0; i < slots.Count; i++)
            RedrawSlot(i, slots[i]);
    }

    public override void OnStopClient()
    {
        slots.OnChange -= OnSlotsChanged;
    }

    [Server]
    public void ServerSetSlot(int index, ItemStack stack)
    {
        slots[index] = stack;
    }

    void OnSlotsChanged(SyncList<ItemStack>.Operation op, int index, ItemStack oldItem, ItemStack newItem)
    {
        RedrawSlot(index, newItem);
    }

    void RedrawSlot(int index, ItemStack stack)
    {
        // UI слой, не авторитетная логика.
    }
}
```

Если Mirror не умеет сериализовать ваш тип, добавьте `NetworkWriter`/`NetworkReader` extension methods или отправляйте стабильный ID.

---

## Проверка себя

- Сервер меняет один слот.
- Клиент перерисовывает один слот, а не весь инвентарь.
- Новый клиент после подключения видит текущее состояние.
- Приватный инвентарь не отправляется всем игрокам без необходимости.

---

## Минимальная диагностика

| Симптом | Что проверить |
|---------|---------------|
| Элемент не появляется у Client | Мутация коллекции была на server? |
| Null вместо объекта | Не передаёте ли `GameObject`, `ScriptableObject` или scene reference слишком рано? |
| Большой traffic spike | Не отправляете ли весь список вместо diff/ID? |
| Ошибка serialization | Тип поддерживается Mirror или есть `NetworkWriter`/`NetworkReader`? |

---

## Частые ошибки

- `readonly SyncList` заменяют новым экземпляром в runtime.
- Не отписываются от events.
- Начальное состояние не отрисовывают после подписки.
- В коллекцию кладут тяжёлые ScriptableObject вместо `itemId`.
- Секретный инвентарь синхронизируется всем observers.

---

## Лайфхаки

- По сети отправляйте ID и количество; описание предмета берите из локального каталога.
- Для UI делайте diff-update, а не полную перерисовку.
- Для больших коллекций сначала подумайте, кому они нужны: всем или только owner.
- В Mirror `96.x` учитывайте изменения сериализации и hash utilities из changelog при обновлении версии.

---

## Профессиональный минимум

- По сети идут IDs и компактные structs, а не большие asset/data objects.
- Все ID стабильны между client/server build.
- У custom data есть версия протокола или миграционный план.
- После изменения схемы коллекции прогоняется reconnect/late join test.

---

## Домашнее задание

Сделайте инвентарь из 8 слотов:

1. Сервер добавляет предмет.
2. Клиент видит изменение.
3. UI перерисовывает только изменённый слот.
4. В заметках указано, почему по сети идёт `itemId`, а не ScriptableObject.
