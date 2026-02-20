# Inventory

`Inventory` — модуль инвентаря и подбираемых предметов, который одинаково удобно использовать через код и No-Code (Inspector + UnityEvent).

## Что входит

| Элемент | Назначение |
|--------|------------|
| **InventoryComponent** | Основной компонент инвентаря в сцене: Add/Remove, события, сохранение через SaveProvider, свойства для NeoCondition. |
| **PickableItem** | Подбираемый предмет в мире: триггеры 2D/3D, ручной вызов Collect, выдача в InventoryComponent. |
| **InventoryDropper** | Отдельный модуль дропа: удаляет предметы из инвентаря и спавнит объект в мире с опциональной физикой. |
| **InventoryPickupBridge** | Вспомогательный мост для вызовов Collect из UnityEvent (InteractiveObject, PhysicsEvents и т.д.). |
| **InventoryItemData** | ScriptableObject с описанием предмета (id, name, icon, maxStack, category). |
| **InventoryDatabase** | ScriptableObject-база предметов для lookup по id и ограничений maxStack. |
| **InventoryInitialStateData** | ScriptableObject с начальным заполнением инвентаря. |
| **InventoryManager** | Чистый C# API без MonoBehaviour для операций инвентаря. |
| **InventoryView / InventoryItemView** | UI-визуализация инвентаря (режим auto-spawn или manual), TextMeshPro + Image (все поля опциональны). |

`InventoryItemData.MaxStack`: `-1` = бесконечный стак (по умолчанию), `1` = нестакаемый предмет.

Если у `InventoryItemData.Icon` не задана иконка, автоматически используется превью из `WorldDropPrefab`.

## Быстрый старт (No-Code)

1. Создайте `InventoryDatabase` и добавьте в него `InventoryItemData`.
2. Добавьте `InventoryComponent` на объект в сцене.
3. Назначьте `Database`, `Save Key` и (опционально) `Initial State Data`.
4. Выберите `Load Mode`:
   - `UseSaveIfExists` — приоритет save, иначе initial;
   - `MergeSaveWithInitial` — merge initial + save;
   - `InitialOnlyIgnoreSave` — только initial.
5. Добавьте `PickableItem` на префаб предмета:
   - укажите `Item Data` или `itemId`,
   - настройте `Amount`,
   - включите сбор через `Collect On Trigger 3D/2D` или вызывайте `Collect` через UnityEvent.
6. Подпишите события `OnItemAdded`, `OnInventoryChanged`, `OnLoaded` для UI/логики.
7. Для дропа по клавише добавьте `InventoryDropper` и оставьте настройки по умолчанию (`G`, `CanDrop = true`).

## Быстрый старт (код)

```csharp
[SerializeField] private InventoryComponent inventory;
[SerializeField] private InventoryItemData coin;

private void GiveCoins(int amount)
{
    inventory.AddItemData(coin, amount);
}

private bool CanBuy(int price)
{
    return inventory.HasItemAmount(coin.ItemId, price);
}
```

## Документация

- [InventoryComponent](./InventoryComponent.md)
- [PickableItem](./PickableItem.md)
- [InventoryDropper](./InventoryDropper.md)
- [InventoryView](./InventoryView.md)
