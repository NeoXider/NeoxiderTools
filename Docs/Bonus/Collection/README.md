# Collection

**What it is:** persistent collectible unlocks with optional box rewards and replaceable item views
(`Scripts/Bonus/Collection/`). The state and rules live in plain C# cores — [CollectionSet](CollectionSet.md)
(independent sets, C# events, injectable random) and [BoxProgress](BoxProgress.md) (integer piggy bank with
idempotent claims) — and the components wrap them for scenes.

**Contents:**

| Page | Kind |
|------|------|
| [CollectionSet](CollectionSet.md) | Plain C# set core: unlock/lock/reset, persistence, events. |
| [BoxProgress](BoxProgress.md) | Plain C# accumulate/threshold/claim core and shared `BoxProgressRules`. |
| [Collection](Collection.md) | Scene component over one `CollectionSet` (catalog, UnityEvents, `Collection.I`). |
| [Box](Box.md) | Scene/UI progress box that grants a locked item. |
| [ItemCollectionData](ItemCollectionData.md) | Item ScriptableObject. |
| [ItemCollection](ItemCollection.md), [ItemCollectionInfo](ItemCollectionInfo.md), [CollectionVisualManager](CollectionVisualManager.md) | Views. |

---

## Scene setup

1. Create [ItemCollectionData](ItemCollectionData.md) assets and assign the ordered catalog to `Collection`.
2. Give independent collections different **Save Key Prefix** values. Unlock keys are
   `<prefix>Item_<index>`; preserve catalog indices when shipping updates.
3. Optionally assign [ItemCollection](ItemCollection.md) views to
   [CollectionVisualManager](CollectionVisualManager.md), and wire `OnSetItem(int)` to a details view.
4. Use [Box](Box.md) to exchange accumulated progress for a still-locked item.

`ItemCollection`, `ItemCollectionInfo`, `Box` and `CollectionVisualManager` (**Collection** field /
`TargetCollection`) accept an explicit collection reference, with `Collection.I` as fallback; an `ItemCollection`
without its own reference unlocks into the collection its manager displays. A non-global
collection (**Set Instance On Awake** off) stays out of `Collection.I` but still loads its saved unlocks in its own
`Awake`, so it works with every view. Several collections each need their own **Save Key Prefix**. To show
several collections at once, give each panel its own `CollectionVisualManager` and turn **Set Instance On Awake**
off on every manager except one (see [CollectionVisualManager](CollectionVisualManager.md)).

Every UnityEvent of these components exists on a component added from code (`AddComponent`), so code-built
scenes can subscribe without a SerializedObject pass.

Code-first games that need several independent sets without scene objects create one
[CollectionSet](CollectionSet.md) per set; each is saved under its own prefix and reset with `LockAll()`.

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
- [CollectionVisualManager](CollectionVisualManager.md): view refresh and selection for an assigned or the global collection.

**Demo:** `Scenes/Bonuses/BonusRewardsDemo.unity` in the Demo Scenes sample shows a six-item `Collection`, its
`CollectionVisualManager` slots, an `ItemCollectionInfo` details row and a `Box` chest that unlocks a random locked
item, wired without a demo script.

## See Also

- [Module Root](../README.md)
