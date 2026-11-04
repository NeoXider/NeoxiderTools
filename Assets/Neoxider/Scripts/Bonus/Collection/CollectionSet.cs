using System;
using System.Collections.Generic;
using Neo.Save;

namespace Neo.Bonus
{
    /// <summary>
    ///     Plain C# collection state: a fixed number of slots, each locked or unlocked, with C# events and an
    ///     injectable random source for prize picks. Every instance is independent, so a game can keep several sets
    ///     (stickers, cards, trophies) that are claimed, saved and reset separately without a scene singleton.
    ///     <see cref="Collection" /> is the scene wrapper over one instance and exposes it only as
    ///     <see cref="IReadOnlyCollectionSet" />.
    /// </summary>
    public sealed class CollectionSet : IReadOnlyCollectionSet
    {
        private readonly Predicate<int> _isSlotClaimable;
        private bool[] _unlocked;

        /// <summary>Creates a set of <paramref name="itemCount" /> locked slots.</summary>
        /// <param name="itemCount">Number of slots; negative counts as 0.</param>
        /// <param name="saveKeyPrefix">
        ///     When not null, every state change is written immediately through <see cref="SaveProvider" /> under
        ///     <c>{prefix}Item_{index}</c> (the same keys as <see cref="Collection" />) and <see cref="Load" /> reads them.
        /// </param>
        /// <param name="isSlotClaimable">
        ///     Optional filter for slots that can never be unlocked (for example an empty catalog entry). Null allows all.
        /// </param>
        public CollectionSet(int itemCount, string saveKeyPrefix = null, Predicate<int> isSlotClaimable = null)
            : this(new bool[Math.Max(0, itemCount)], saveKeyPrefix, isSlotClaimable)
        {
        }

        /// <summary>
        ///     Creates a set over an existing state array, which is used directly (not copied). Scene wrappers pass their
        ///     serialized array so the Inspector and the core always show the same state.
        /// </summary>
        public CollectionSet(bool[] unlockedStates, string saveKeyPrefix = null, Predicate<int> isSlotClaimable = null)
        {
            _unlocked = unlockedStates ?? Array.Empty<bool>();
            SaveKeyPrefix = saveKeyPrefix;
            _isSlotClaimable = isSlotClaimable;
        }

        /// <summary>Raised after a slot becomes unlocked, with its index.</summary>
        public event Action<int> ItemUnlocked;

        /// <summary>Raised after a slot becomes locked again, with its index.</summary>
        public event Action<int> ItemLocked;

        /// <summary>Raised after any change of composition (including <see cref="Load" />): (unlockedCount, itemCount).</summary>
        public event Action<int, int> CompletionChanged;

        /// <summary>Save key prefix used for persistence; null keeps the set in memory only.</summary>
        public string SaveKeyPrefix { get; set; }

        /// <summary>Number of slots.</summary>
        public int ItemCount => _unlocked.Length;

        /// <summary>Number of unlocked slots.</summary>
        public int UnlockedCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _unlocked.Length; i++)
                {
                    if (_unlocked[i])
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        /// <summary>Number of locked slots.</summary>
        public int LockedCount => ItemCount - UnlockedCount;

        /// <summary>Completion ratio 0..1; 0 for an empty set.</summary>
        public float CompletionRatio => _unlocked.Length == 0 ? 0f : (float)UnlockedCount / _unlocked.Length;

        /// <summary>True when the set has slots and every claimable slot is unlocked.</summary>
        public bool IsComplete => _unlocked.Length > 0 && CountClaimableLocked() == 0;

        /// <summary>Save key of one slot, shared with <see cref="Collection" />.</summary>
        public static string GetItemSaveKey(string saveKeyPrefix, int index)
        {
            return saveKeyPrefix + "Item_" + index;
        }

        /// <summary>True when <paramref name="index" /> is in range and unlocked.</summary>
        public bool IsUnlocked(int index)
        {
            return (uint)index < (uint)_unlocked.Length && _unlocked[index];
        }

        /// <summary>True when <paramref name="index" /> is in range and accepted by the claimable filter.</summary>
        public bool IsClaimable(int index)
        {
            return (uint)index < (uint)_unlocked.Length && (_isSlotClaimable == null || _isSlotClaimable(index));
        }

        /// <summary>Unlocks one slot. Returns false when it is out of range, filtered out or already unlocked.</summary>
        public bool TryUnlock(int index)
        {
            if (!SetUnlockedSilently(index, true))
            {
                return false;
            }

            Persist(index);
            ItemUnlocked?.Invoke(index);
            RaiseCompletionChanged();
            return true;
        }

        /// <summary>Locks one slot again. Returns false when it is out of range or already locked.</summary>
        public bool TryLock(int index)
        {
            if (!SetUnlockedSilently(index, false))
            {
                return false;
            }

            Persist(index);
            ItemLocked?.Invoke(index);
            RaiseCompletionChanged();
            return true;
        }

        /// <summary>Locks every unlocked slot (the "reset this set" operation). Returns how many slots changed.</summary>
        public int LockAll()
        {
            int changed = 0;
            for (int i = 0; i < _unlocked.Length; i++)
            {
                if (!_unlocked[i])
                {
                    continue;
                }

                _unlocked[i] = false;
                Persist(i);
                ItemLocked?.Invoke(i);
                changed++;
            }

            if (changed > 0)
            {
                RaiseCompletionChanged();
            }

            return changed;
        }

        /// <summary>Unlocks every claimable slot. Returns how many slots changed.</summary>
        public int UnlockAll()
        {
            int changed = 0;
            for (int i = 0; i < _unlocked.Length; i++)
            {
                if (!SetUnlockedSilently(i, true))
                {
                    continue;
                }

                Persist(i);
                ItemUnlocked?.Invoke(i);
                changed++;
            }

            if (changed > 0)
            {
                RaiseCompletionChanged();
            }

            return changed;
        }

        /// <summary>
        ///     Picks a locked, claimable slot without changing state. Returns -1 when none is left.
        /// </summary>
        /// <param name="random">False picks the lowest index; true picks uniformly among the candidates.</param>
        /// <param name="randomIndex">
        ///     Optional source returning an integer in <c>[0, maxExclusive)</c>; null uses <c>UnityEngine.Random</c>.
        ///     Inject one for deterministic tests, replays or server-authored picks.
        /// </param>
        /// <exception cref="InvalidOperationException"><paramref name="randomIndex" /> returned a value out of range.</exception>
        public int PickLockedIndex(bool random = true, Func<int, int> randomIndex = null)
        {
            int candidates = CountClaimableLocked();
            if (candidates == 0)
            {
                return -1;
            }

            int target = 0;
            if (random)
            {
                target = randomIndex != null ? randomIndex(candidates) : UnityEngine.Random.Range(0, candidates);
                if (target < 0 || target >= candidates)
                {
                    // WHY: silently mapping a bad server/replay roll to slot 0 would grant the wrong item.
                    throw new InvalidOperationException(
                        $"Random index source returned {target}; expected a value in [0, {candidates}).");
                }
            }

            for (int i = 0; i < _unlocked.Length; i++)
            {
                if (_unlocked[i] || !IsClaimable(i))
                {
                    continue;
                }

                if (target == 0)
                {
                    return i;
                }

                target--;
            }

            return -1;
        }

        /// <summary>Picks a locked slot (see <see cref="PickLockedIndex" />) and unlocks it in one step.</summary>
        public bool TryClaimNext(out int index, bool random = true, Func<int, int> randomIndex = null)
        {
            index = PickLockedIndex(random, randomIndex);
            return index >= 0 && TryUnlock(index);
        }

        /// <summary>Appends unlocked slot indices to <paramref name="results" /> without allocating. Returns the count added.</summary>
        public int CopyUnlockedIndices(List<int> results)
        {
            if (results == null)
            {
                return 0;
            }

            int added = 0;
            for (int i = 0; i < _unlocked.Length; i++)
            {
                if (_unlocked[i])
                {
                    results.Add(i);
                    added++;
                }
            }

            return added;
        }

        /// <summary>
        ///     Reads every slot from <see cref="SaveProvider" /> under <see cref="SaveKeyPrefix" /> and raises
        ///     <see cref="CompletionChanged" /> once. Does nothing for an in-memory set.
        /// </summary>
        public void Load()
        {
            if (SaveKeyPrefix == null)
            {
                return;
            }

            for (int i = 0; i < _unlocked.Length; i++)
            {
                _unlocked[i] = SaveProvider.GetInt(GetItemSaveKey(SaveKeyPrefix, i)) == 1;
            }

            RaiseCompletionChanged();
        }

        /// <summary>Writes every slot to <see cref="SaveProvider" />. Does nothing for an in-memory set.</summary>
        public void Save()
        {
            if (SaveKeyPrefix == null)
            {
                return;
            }

            for (int i = 0; i < _unlocked.Length; i++)
            {
                Persist(i);
            }
        }

        internal bool UsesStorage(bool[] storage)
        {
            return ReferenceEquals(_unlocked, storage);
        }

        /// <summary>Re-points the set at a new state array while keeping subscribers (scene wrapper resize/load).</summary>
        internal void AdoptStorage(bool[] storage)
        {
            _unlocked = storage ?? Array.Empty<bool>();
        }

        /// <summary>Changes one slot without persistence or events; the scene wrapper orders those itself.</summary>
        internal bool SetUnlockedSilently(int index, bool unlocked)
        {
            if ((uint)index >= (uint)_unlocked.Length || _unlocked[index] == unlocked)
            {
                return false;
            }

            if (unlocked && !IsClaimable(index))
            {
                return false;
            }

            _unlocked[index] = unlocked;
            return true;
        }

        internal void RaiseItemUnlocked(int index)
        {
            ItemUnlocked?.Invoke(index);
        }

        internal void RaiseItemLocked(int index)
        {
            ItemLocked?.Invoke(index);
        }

        internal void RaiseCompletionChanged()
        {
            CompletionChanged?.Invoke(UnlockedCount, ItemCount);
        }

        private int CountClaimableLocked()
        {
            int count = 0;
            for (int i = 0; i < _unlocked.Length; i++)
            {
                if (!_unlocked[i] && IsClaimable(i))
                {
                    count++;
                }
            }

            return count;
        }

        private void Persist(int index)
        {
            if (SaveKeyPrefix != null)
            {
                SaveProvider.SetInt(GetItemSaveKey(SaveKeyPrefix, index), _unlocked[index] ? 1 : 0);
            }
        }
    }
}
