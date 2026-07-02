# EquipmentManager

**Что это:** `Neo.Shop.EquipmentManager` — мультикатегорийный экип (dress-up/скины): по одной вещи на категорию, спрайт применяется к слоту (`SpriteRenderer` или `Image` + опц. `SetNativeSize`), надетое персистится через `SaveProvider` (ключ `Equip_<категория>`).

**Как использовать:** каталог = массив `EquipItemDefinition`; слоты = `categoryId` → цель-визуал (+ `defaultItemId`). API: `EquipById(id)` (NoCode-вход — сюда вешается покупка/клик по ячейке), `Equip(item)`, `Unequip(categoryId)`, `ToggleById(id)`, `GetEquippedId(categoryId)`, `IsEquipped(id)`, событие `OnEquipChanged(categoryId, itemId)`. Восстановление надетого — в `Start()`.

**Связка с Shop:** владение и покупка — на `Shop`/`ShopItemData`; сцепите событие покупки/экипа Shop с `EquipById` для потока «купил → надел».
