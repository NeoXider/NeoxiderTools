# Collection

Persistent collectible unlocks with optional box rewards and replaceable item views.

## Scene setup

1. Create [ItemCollectionData](ItemCollectionData.md) assets and assign the ordered catalog to `Collection`.
2. Give independent collections different **Save Key Prefix** values. Unlock keys are
   `<prefix>Item_<index>`; preserve catalog indices when shipping updates.
3. Optionally assign [ItemCollection](ItemCollection.md) views to
   [CollectionVisualManager](CollectionVisualManager.md), and wire `OnSetItem(int)` to a details view.
4. Use [Box](Box.md) to exchange accumulated progress for a still-locked item.

`ItemCollection` and `ItemCollectionInfo` accept an explicit collection reference, with `Collection.I`
as fallback. `Box` and `CollectionVisualManager` currently use `Collection.I` directly.

## C# API

```csharp
collection.SaveKeyPrefix = "Outfits_";
collection.ItemCollectionDatas = catalog; // ItemCollectionData[] supplied by the caller.
collection.Load();
bool newlyUnlocked = collection.TryAddItem(2);
ItemCollectionData prize = collection.GetPrize(); // null when no valid locked item remains.
float completion = collection.GetCompletionPercentage();
```

`TryAddItem` returns true only for a newly unlocked, assigned catalog entry. `GetPrize` chooses a
valid locked entry, persists its unlock, emits `OnItemAdded` and completion events, then emits
`OnGetItem`. **Random Prize** chooses randomly; disabling it chooses the first eligible index.
Empty catalog slots cannot be unlocked or granted. Keep the catalog fully assigned for meaningful
completion totals: `ItemCount` remains the array length to preserve index-based save compatibility.
After replacing the catalog at runtime, call `Load()` to resize and reload its unlock state.

Unlocks are committed before collection UnityEvents; a listener exception does not undo an unlock.
When called by [Box](Box.md), the associated progress price is also settled before these events.
Exceptions propagate and may prevent later notifications, so listeners must not interpret an exception
as proof that no item was granted. Public `GetPrize()` / `TryAddItem` signatures and index-based keys
remain unchanged; the Box settlement seam is internal to this assembly.

Открытие предмета фиксируется до UnityEvents. Вызов из `Box` также заранее списывает прогресс;
исключение подписчика не отменяет выдачу и не разрешает повторную бесплатную награду.

`RemoveItem`, `SetItemEnabled`, `ClearCollection`, and `UnlockAllItems` update persistence and events.
`GetUnlockedIds`, `GetLockedIds`, and the category/rarity/type queries support custom views.
`OnCompletionChanged(int unlocked, int total)` and `OnCompletionPercentageChanged(float)` are UI hooks.

## Components

- [Box](Box.md): progress, threshold events, and one prize per accepted take.
- [ItemCollection](ItemCollection.md): icon, enabled state, and `Unlock()` for UnityEvent bindings.
- [ItemCollectionInfo](ItemCollectionInfo.md): item name, description, and optional icon.
- [CollectionVisualManager](CollectionVisualManager.md): singleton-backed view refresh and selection.

## See Also

- [Module Root](../README.md)
