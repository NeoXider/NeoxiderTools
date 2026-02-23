# InventoryHand

Компонент **«рука»** (equipped slot): один выбранный предмет из инвентаря отображается в заданной точке сцены (например рука персонажа). Переключение влево/вправо по слотам с ненулевым количеством. Удобная интеграция с **Selector** для No-Code переключения (кнопки, колёсико мыши).

## Назначение

- Выбор одного предмета из инвентаря как «текущий в руке».
- Отображение 3D-модели (префаба) этого предмета в точке `Hand Anchor`.
- Переключение слота: **SelectNext** / **SelectPrevious** (с зацикливанием).
- Синхронизация с **InventoryComponent.SelectedItemId** (для дропа, условий, UI).
- Опциональная связка с **Selector**: переключение через Selector.Next/Previous (например с UI или вводом).

## Поля

| Поле | Описание |
|------|----------|
| **Inventory** | Инвентарь. Если не назначен и включён Auto Find — `InventoryComponent.FindDefault()`. |
| **Hand Anchor** | Transform, в котором появляется экземпляр префаба выбранного предмета (например рука). Если пусто — используется transform компонента. |
| **Selector** | Опционально. Selector в режиме Count: количество слотов = число предметов в инвентаре с count > 0. При изменении выбора в Selector обновляется слот руки и визуал. |
| **Fallback Hand Prefab** | Префаб по умолчанию, если у предмета в базе нет WorldDropPrefab. |
| **Sync Selector On Inventory Changed** | При изменении инвентаря обновлять Selector.Count и зажимать текущий индекс. |
| **Allow Empty Slot** | При пустом инвентаре разрешить «пустой» слот (Selector.Count = 1, в руке ничего не показывается). |

## События

Оба события передают один параметр **int itemId** (удобно для NoCode-подписок в инспекторе). Данные предмета по itemId: `inventory.GetItemData(itemId)` или `InventoryHand.EquippedItemData`.

| Событие | Аргументы | Когда |
|---------|-----------|--------|
| **OnEquippedChanged** | `(int itemId)` | После смены выбранного слота (в т.ч. через SelectNext/Previous/SetSlotIndex). |
| **OnUseItemRequested** | `(int itemId)` | При вызове UseEquippedItem(). Подпишите для эффекта (здоровье, дверь и т.д.); при расходе — inventory.TryConsume(itemId, 1). |

## API

| Метод | Описание |
|-------|----------|
| **UseEquippedItem()** | «Использовать» предмет в руке: вызывает OnUseItemRequested(itemId). Логику (трата, эффект) реализуйте в подписчике; данные — GetItemData(itemId) или EquippedItemData. |
| **SelectNext()** | Следующий слот (по порядку снимка инвентаря), с зацикливанием. |
| **SelectPrevious()** | Предыдущий слот, с зацикливанием. |
| **SetSlotIndex(int index)** | Установить слот по индексу (0 .. GetNonEmptySlotCount()-1). |
| **RefreshSlotFromInventory()** | Пересчитать слот из инвентаря, синхронизировать Selector и отобразить предмет в руке. |

| Свойство | Описание |
|----------|----------|
| **SlotIndex** | Текущий индекс слота. |
| **EquippedItemId** | ItemId выбранного предмета (-1 если пусто). |
| **EquippedItemData** | InventoryItemData выбранного предмета. |

## Интеграция с Selector

1. Добавьте на объект **Selector** (без заполнения Items — используется виртуальный Count).
2. В **InventoryHand** укажите этот Selector в поле **Selector**.
3. Включите **Sync Selector On Inventory Changed**.
4. Кнопки/ввод: вызывайте **Selector.Next()** и **Selector.Previous()** (или подпишитесь на них в инспекторе). Selector выдаёт индекс, InventoryHand переводит его в выбранный слот и обновляет Hand Anchor.

Рекомендуется для Selector: **Loop** = true, **Allow Empty Effective Index** по желанию (при пустом инвентаре).

## Пример настройки

1. Инвентарь: **InventoryComponent** на персонаже или менеджере.
2. Рука: пустой GameObject (например «Hand») как дочерний к персонажу.
3. На тот же объект (или на персонажа) добавьте **InventoryHand**: Hand Anchor = Hand, Inventory = ссылка на инвентарь.
4. Для переключения с клавиш/геймпада: добавьте **Selector** (Count задаётся автоматически), в поле InventoryHand → Selector укажите этот Selector. На кнопки «след. / пред.» вызывайте `Selector.Next()` и `Selector.Previous()`.

## Связанные компоненты и сценарии

- **InventoryDropper** — выброс текущего предмета (SelectedItemId): по клавише или вызов DropSelected().
- **PickableItem** — подбор в тот же инвентарь; при одном слоте (Max Unique = 1) получается режим «одна вещь в руке».
- **План сценариев** (один предмет в руке / много предметов + Selector): [InventoryHand_Plan.md](./InventoryHand_Plan.md).

## Связанные методы InventoryComponent

- **GetNonEmptySlotCount()** — число слотов с count > 0.
- **GetItemIdAtSlotIndex(int slotIndex)** — itemId по индексу слота.
- **SelectedItemId** / **SelectedItemCount** — синхронизируются с текущим слотом руки (для дропа и условий).
- **TryConsume(itemId, 1)** — в подписчике OnUseItemRequested для расходуемых предметов.
