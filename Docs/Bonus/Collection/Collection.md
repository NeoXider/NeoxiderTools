# Collection

**What it is:** the scene component for one item collection (`Neo.Bonus.Collection`, `Scripts/Bonus/Collection/Collection.cs`, a `Singleton<Collection>` reachable as `Collection.I`). It owns an ordered `ItemCollectionData` catalog, persists unlocks through `SaveProvider`, raises UnityEvents for views and exposes its plain C# [CollectionSet](CollectionSet.md) state read-only as `Set` (`IReadOnlyCollectionSet`).

**How to use:**
1. Add `Collection`, assign the ordered **Item Collection Datas** catalog and a unique **Save Key Prefix**.
2. Wire `OnItemAdded`, `OnItemRemoved`, `OnCompletionChanged` / `OnCompletionPercentageChanged` to views, or use [CollectionVisualManager](CollectionVisualManager.md).
3. Grant items with `GetPrize()` (random or first locked), `TryAddItem(index)` or `AddItem(data)`; reset with `ClearCollection()`.
4. From code, subscribe to `collection.Set.ItemUnlocked` / `ItemLocked` / `CompletionChanged` instead of UnityEvents if you prefer C# events. `Set` has queries and events only: every change goes through the component so it is saved and raises the UnityEvents.

---

## Main members

| Member | Description |
|--------|-------------|
| `IReadOnlyCollectionSet Set` | Read-only view (queries and C# events, no mutators) of the plain C# core over the same storage as `EnabledItems`; its C# events run after the component persisted the change and invoked the matching UnityEvent. The instance survives `Load()` resizing the catalog. |
| `Func<int, int> RandomIndexSource` | Optional random source for `GetPrize()` returning `[0, maxExclusive)`; null uses `UnityEngine.Random`. A value out of range throws `InvalidOperationException` before anything is unlocked. |
| `ItemCollectionData GetPrize()` | Unlocks a locked, assigned catalog entry (random when **Random Prize** is on) and returns it; null when none is left. |
| `bool TryAddItem(int)`, `AddItem(int)`, `AddItem(ItemCollectionData)` | Unlock one entry. |
| `RemoveItem(int)`, `SetItemEnabled(int, bool)`, `ClearCollection()`, `UnlockAllItems()` | Change state with persistence and events. |
| `bool HasItem(int)`, `int ItemCount`, `UnlockedCount`, `LockedCount`, `float GetCompletionPercentage()` | Queries. |
| `Load()`, `Save()` | Read/write `<prefix>Item_<index>` keys. |

## Example

```csharp
Collection collection = Collection.I;
collection.Set.ItemUnlocked += index => toast.Show(collection.GetItemData(index).ItemName);
collection.RandomIndexSource = maxExclusive => seededRandom.Next(maxExclusive); // deterministic prizes

ItemCollectionData prize = collection.GetPrize();
```

With **Set Instance On Awake** off the component is a non-global collection: it is not registered as `Collection.I`,
but it still loads its saved unlocks in `Awake` and raises `OnLoadItems`. Reference it explicitly from views
(`Box`, `ItemCollection`, `CollectionVisualManager.TargetCollection`) and give it its own **Save Key Prefix**.

For independent sets that need no scene object, create [CollectionSet](CollectionSet.md) instances directly.

## See also

- [Collection README](README.md) — persistence keys, ordering of events and Box settlement.
- [Box](Box.md)
