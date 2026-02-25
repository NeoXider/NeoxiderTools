# Inventory

`Inventory` — модуль инвентаря и подбираемых предметов, который одинаково удобно использовать через код и No-Code (Inspector + UnityEvent).

## Что входит

| Элемент | Назначение |
|--------|------------|
| **InventoryComponent** | Основной компонент инвентаря в сцене: Add/Remove, TryConsume, события, автосохранение в SaveProvider по Save Key (по умолчанию; на диск — `SaveProvider.Save()` при выходе/паузе), свойства для NeoCondition и `GetCount(itemId)`. |
| **PickableItem** | Подбираемый предмет в мире: триггеры 2D/3D, ручной вызов Collect, выдача в InventoryComponent. |
| **InventoryDropper** | Отдельный модуль дропа: удаляет предметы из инвентаря и спавнит объект в мире с опциональной физикой. |
| **InventoryHand** | Система «руки»: один выбранный предмет в Hand Anchor; переключение влево/вправо, интеграция с Selector; при наличии HandView на предмете применяются офсеты и масштаб вьюшки, затем общий масштаб (по умолчанию дельта). |
| **HandView** | Вьюшка руки на префабе предмета: смещение позиции/поворота и базовый масштаб в руке; InventoryHand читает с экземпляра и применяет первой. |
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
7. Для дропа по клавише: добавьте `InventoryDropper`. Если есть **InventoryHand** (рука с предметом), назначьте этот Dropper в Hand → по **G** будет сбрасываться предмет из руки; у Dropper ввод по клавише при этом временно отключается. Без Hand оставьте у Dropper настройки по умолчанию (`G`, `CanDrop = true`) — дропается выбранный предмет.
8. При включённом Auto Save инвентарь сам пишет данные в SaveProvider при изменениях. Для записи на диск вызовите `SaveProvider.Save()` при выходе или смене сцены.

## Быстрый старт (код)

```csharp
[SerializeField] private InventoryComponent inventory;
[SerializeField] private InventoryItemData coin;

private void GiveCoins(int amount)
{
    inventory.AddItemData(coin, amount);
}

private bool TryBuy(int price)
{
    return inventory.TryConsume(coin.ItemId, price);
}

// Условие по количеству: inventory.GetCount(itemId) >= N
if (inventory.GetCount(coin.ItemId) >= 10) { ... }
```

## Документация

- [InventoryComponent](./InventoryComponent.md)
- [InventoryHand](./InventoryHand.md) — рука, применение по E, дроп по G через Dropper; интеграция с Selector; HandView на предметах (офсеты и масштаб), масштаб руки — HandScaleMode (по умолчанию Relative, дельта).
- [HandView](./HandView.md) — вьюшка руки на префабе предмета: позиция/поворот/масштаб в руке.
- [План: один предмет / много предметов](./InventoryHand_Plan.md)
- [PickableItem](./PickableItem.md)
- [InventoryDropper](./InventoryDropper.md) — дроп в мир; при подключении через Hand обрабатывает DropEquipped, свойство AllowDropInput.
- [InventoryView](./InventoryView.md)
