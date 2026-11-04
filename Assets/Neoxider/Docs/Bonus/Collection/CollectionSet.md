# CollectionSet

**What it is:** a plain C# class (`Neo.Bonus.CollectionSet`, `Scripts/Bonus/Collection/CollectionSet.cs`) that holds one collection's lock/unlock state: a fixed number of slots, C# events, an injectable random source for prize picks and optional `SaveProvider` persistence. Every instance is independent, so one game can keep several sets that are claimed, saved and reset separately without a scene singleton. The [Collection](Collection.md) component wraps one instance and exposes it only through the read-only `IReadOnlyCollectionSet` interface (`Collection.Set`), because the component persists and announces every change itself; mutate a component's state through the component.

**How to use:**
1. Create one set per collection: `new CollectionSet(itemCount, "Stickers_")`. Pass `null` as the prefix to keep it in memory only.
2. Call `Load()` once to read saved unlocks (keys `<prefix>Item_<index>`, the same keys `Collection` uses).
3. Unlock with `TryUnlock(index)` or grant a random locked slot with `TryClaimNext(out int index)`; reset with `LockAll()`.
4. Subscribe to `ItemUnlocked`, `ItemLocked` and `CompletionChanged` to update views.

---

## Constructors

| Signature | Description |
|-----------|-------------|
| `CollectionSet(int itemCount, string saveKeyPrefix = null, Predicate<int> isSlotClaimable = null)` | New set of locked slots. With a prefix every change is written immediately. The predicate excludes slots that can never be unlocked (for example an empty catalog entry). |
| `CollectionSet(bool[] unlockedStates, string saveKeyPrefix = null, Predicate<int> isSlotClaimable = null)` | Uses an existing state array directly (no copy). |

## Members

| Member | Description |
|--------|-------------|
| `int ItemCount`, `UnlockedCount`, `LockedCount` | Slot counts. |
| `float CompletionRatio` | 0..1; 0 for an empty set. |
| `bool IsComplete` | Every claimable slot is unlocked. |
| `bool IsUnlocked(int)`, `bool IsClaimable(int)` | Slot queries; out-of-range indices return false. |
| `bool TryUnlock(int)`, `bool TryLock(int)` | Change one slot; false when nothing changed. |
| `int UnlockAll()`, `int LockAll()` | Bulk change; returns how many slots changed. |
| `int PickLockedIndex(bool random = true, Func<int, int> randomIndex = null)` | Locked claimable slot without changing state; `-1` when none is left. `randomIndex(maxExclusive)` replaces `UnityEngine.Random`; a value outside `[0, maxExclusive)` throws `InvalidOperationException` instead of granting a wrong slot. |
| `bool TryClaimNext(out int index, bool random = true, Func<int, int> randomIndex = null)` | Pick and unlock in one step. |
| `int CopyUnlockedIndices(List<int>)` | Non-allocating unlocked index query. |
| `void Load()`, `void Save()` | Read/write every slot through `SaveProvider` (no-op without a prefix). |
| `static string GetItemSaveKey(string prefix, int index)` | The shared key format. |

## Events

| Event | When |
|-------|------|
| `Action<int> ItemUnlocked` | After a slot was unlocked and persisted. |
| `Action<int> ItemLocked` | After a slot was locked again and persisted. |
| `Action<int, int> CompletionChanged` | After any change, and once after `Load()`: (unlockedCount, itemCount). |

State is committed before events run, so a throwing listener cannot undo an unlock.

## Read-only view

`IReadOnlyCollectionSet` (implemented by `CollectionSet`) has the queries (`ItemCount`, `UnlockedCount`,
`LockedCount`, `CompletionRatio`, `IsComplete`, `IsUnlocked`, `IsClaimable`, `PickLockedIndex`,
`CopyUnlockedIndices`) and the three events, but no mutators. Bind views to it so they work with a standalone set
and with `Collection.Set`.

## Example

```csharp
CollectionSet stickers = new(24, "Stickers_");
CollectionSet trophies = new(8, "Trophies_");
stickers.Load();
trophies.Load();

stickers.ItemUnlocked += index => stickerView.Reveal(index);

if (stickers.TryClaimNext(out int sticker))
{
    // A new sticker was unlocked and saved.
}

trophies.LockAll(); // Resets trophies only.
```

Deterministic picks for tests or server-authored prizes:

```csharp
CollectionSet set = new(10);
int index = set.PickLockedIndex(true, maxExclusive => serverRoll % maxExclusive);
```

## See also

- [Collection](Collection.md) — scene component over one set.
- [Collection README](README.md)
