using System;
using System.Collections.Generic;

namespace Neo.Tools
{
    /// <summary>
    ///     Pure selection state and rules used by <see cref="Selector"/>.
    ///     It has no Unity object dependencies, so it can be reused in services, tests, UI view-models, and non-MonoBehaviour code.
    /// </summary>
    public sealed class SelectorModel
    {
        private static readonly Random SharedRandom = new();

        // WHY: a cached delegate; converting the method group on every SetRandom call allocated a new delegate.
        private static readonly Func<int, int, int> DefaultRandomRangeFunc = DefaultRandomRange;

        private readonly HashSet<int> _excludedIndices = new();
        private readonly HashSet<int> _usedIndicesForUnique = new();

        public int Count { get; set; }
        public int CurrentIndex { get; private set; }
        public int IndexOffset { get; set; }
        public bool Loop { get; set; } = true;
        public bool FillMode { get; set; }
        public bool AllowEmptyEffectiveIndex { get; set; }
        public bool UniqueSelectionMode { get; set; }
        public bool ResetUniqueWhenCycleComplete { get; set; } = true;

        public int IndexWithOffset => checked(CurrentIndex + IndexOffset);
        public int ExcludedCount => _excludedIndices.Count;
        public IReadOnlyCollection<int> ExcludedIndices => _excludedIndices;
        public IReadOnlyCollection<int> UsedIndicesForUnique => _usedIndicesForUnique;

        public bool IsAtStart
        {
            get
            {
                (int min, _) = GetBounds();
                return CurrentIndex <= min;
            }
        }

        public bool IsAtEnd
        {
            get
            {
                (_, int max) = GetBounds();
                return CurrentIndex >= max;
            }
        }

        public int UniqueRemainingCount
        {
            get
            {
                if (!UniqueSelectionMode || Count <= 0)
                {
                    return 0;
                }

                (int min, int max) = GetBounds();
                if (min > max)
                {
                    return 0;
                }

                long remaining = 0;
                for (long candidate = min; candidate <= max; candidate++)
                {
                    int i = (int)candidate;
                    if (!_usedIndicesForUnique.Contains(i) && !_excludedIndices.Contains(i))
                    {
                        remaining++;
                    }
                }

                return (int)Math.Min(remaining, int.MaxValue);
            }
        }

        public void Configure(
            int count,
            int currentIndex,
            int indexOffset,
            bool loop,
            bool fillMode,
            bool allowEmptyEffectiveIndex,
            bool uniqueSelectionMode,
            bool resetUniqueWhenCycleComplete,
            IEnumerable<int> excludedIndices,
            IEnumerable<int> usedIndicesForUnique)
        {
            ConfigureScalars(count, currentIndex, indexOffset, loop, fillMode, allowEmptyEffectiveIndex,
                uniqueSelectionMode, resetUniqueWhenCycleComplete);
            SetExcludedIndices(excludedIndices);
            SetUsedIndicesForUnique(usedIndicesForUnique);
        }

        /// <summary>
        ///     <see cref="Configure"/> for an owner that passes its own private sets: same validation and result,
        ///     but the sets are copied in place instead of through a defensive intermediate collection.
        /// </summary>
        /// <remarks>
        ///     WHY: <see cref="Selector"/> re-synchronizes the model before every command; materializing two
        ///     replacement sets each time made every command allocate. A private <see cref="HashSet{T}"/> cannot be
        ///     a deferred, self-derived or throwing input, so the defensive copy buys nothing there.
        /// </remarks>
        internal void ConfigureFromOwnedSets(
            int count,
            int currentIndex,
            int indexOffset,
            bool loop,
            bool fillMode,
            bool allowEmptyEffectiveIndex,
            bool uniqueSelectionMode,
            bool resetUniqueWhenCycleComplete,
            HashSet<int> excludedIndices,
            HashSet<int> usedIndicesForUnique)
        {
            ConfigureScalars(count, currentIndex, indexOffset, loop, fillMode, allowEmptyEffectiveIndex,
                uniqueSelectionMode, resetUniqueWhenCycleComplete);
            CopySet(excludedIndices, _excludedIndices);
            CopySet(usedIndicesForUnique, _usedIndicesForUnique);
        }

        /// <summary>
        ///     Replaces the contents of <paramref name="destination"/> with the excluded indices, in place.
        /// </summary>
        internal void CopyExcludedIndicesTo(HashSet<int> destination)
        {
            CopySet(_excludedIndices, destination);
        }

        /// <summary>
        ///     Replaces the contents of <paramref name="destination"/> with the used unique indices, in place.
        /// </summary>
        internal void CopyUsedIndicesForUniqueTo(HashSet<int> destination)
        {
            CopySet(_usedIndicesForUnique, destination);
        }

        private void ConfigureScalars(
            int count,
            int currentIndex,
            int indexOffset,
            bool loop,
            bool fillMode,
            bool allowEmptyEffectiveIndex,
            bool uniqueSelectionMode,
            bool resetUniqueWhenCycleComplete)
        {
            int normalizedCount = Math.Max(0, count);
            GetBounds(normalizedCount, indexOffset, allowEmptyEffectiveIndex);
            Count = normalizedCount;
            CurrentIndex = currentIndex;
            IndexOffset = indexOffset;
            Loop = loop;
            FillMode = fillMode;
            AllowEmptyEffectiveIndex = allowEmptyEffectiveIndex;
            UniqueSelectionMode = uniqueSelectionMode;
            ResetUniqueWhenCycleComplete = resetUniqueWhenCycleComplete;
        }

        private static void CopySet(HashSet<int> source, HashSet<int> destination)
        {
            if (ReferenceEquals(source, destination))
            {
                return;
            }

            destination.Clear();
            if (source == null)
            {
                return;
            }

            foreach (int index in source)
            {
                destination.Add(index);
            }
        }

        public (int min, int max) GetBounds()
        {
            return GetBounds(Count, IndexOffset, AllowEmptyEffectiveIndex);
        }

        internal static (int min, int max) GetBounds(int count, int indexOffset, bool allowEmptyEffectiveIndex)
        {
            if (count <= 0)
            {
                return (0, 0);
            }

            long min = (allowEmptyEffectiveIndex ? -1L : 0L) - indexOffset;
            long max = (long)count - 1L - indexOffset;
            if (min < int.MinValue || max > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(indexOffset), indexOffset,
                    "Logical selection bounds must fit in Int32.");
            }

            return ((int)min, (int)max);
        }

        public bool IsValidIndex(int index)
        {
            if (Count <= 0)
            {
                return false;
            }

            (int min, int max) = GetBounds();
            return index >= min && index <= max;
        }

        public int GetEffectiveIndex()
        {
            return GetEffectiveIndex(Count, CurrentIndex, IndexOffset, AllowEmptyEffectiveIndex);
        }

        internal static int GetEffectiveIndex(int count, int currentIndex, int indexOffset,
            bool allowEmptyEffectiveIndex)
        {
            if (count <= 0)
            {
                return 0;
            }

            int minEffective = allowEmptyEffectiveIndex ? -1 : 0;
            GetBounds(count, indexOffset, allowEmptyEffectiveIndex);
            long effectiveIndex = (long)currentIndex + indexOffset;
            if (effectiveIndex < minEffective)
            {
                return minEffective;
            }

            if (effectiveIndex >= count)
            {
                return count - 1;
            }

            return (int)effectiveIndex;
        }

        public int GetLogicalActiveCount()
        {
            return GetLogicalActiveCount(Count, CurrentIndex, IndexOffset, FillMode, AllowEmptyEffectiveIndex);
        }

        internal static int GetLogicalActiveCount(int count, int currentIndex, int indexOffset, bool fillMode,
            bool allowEmptyEffectiveIndex)
        {
            if (count <= 0)
            {
                return 0;
            }

            int effectiveIndex = GetEffectiveIndex(count, currentIndex, indexOffset, allowEmptyEffectiveIndex);
            if (effectiveIndex < 0)
            {
                return 0;
            }

            return fillMode ? Math.Min(count, effectiveIndex + 1) : 1;
        }

        /// <summary>
        ///     Whether the item at a zero-based item position is on for the current selection: only the effective item,
        ///     or every item up to and including it in fill mode. Nothing is on when the effective index is -1 or
        ///     <see cref="Count"/> is zero. This is the rule <see cref="Selector"/> applies to its items.
        /// </summary>
        /// <param name="itemIndex">Item position (logical index plus offset), not a logical index.</param>
        /// <exception cref="ArgumentOutOfRangeException">The logical bounds do not fit in Int32.</exception>
        public bool IsItemActive(int itemIndex)
        {
            if (Count <= 0)
            {
                return false;
            }

            return IsItemActive(itemIndex, GetEffectiveIndex(), FillMode);
        }

        internal static bool IsItemActive(int itemIndex, int effectiveIndex, bool fillMode)
        {
            if (effectiveIndex < 0 || itemIndex < 0)
            {
                return false;
            }

            return fillMode ? itemIndex <= effectiveIndex : itemIndex == effectiveIndex;
        }

        public SelectorModelResult Next()
        {
            if (Count <= 0)
            {
                return SelectorModelResult.NoItems;
            }

            bool finished = false;
            (int min, int max) = GetBounds();
            long next = (long)CurrentIndex + 1L;
            if (next > max)
            {
                if (Loop)
                {
                    CurrentIndex = min;
                }
                else
                {
                    CurrentIndex = max;
                }

                finished = true;
            }
            else
            {
                CurrentIndex = (int)next;
            }

            return SelectorModelResult.Changed(finished);
        }

        public SelectorModelResult Previous()
        {
            if (Count <= 0)
            {
                return SelectorModelResult.NoItems;
            }

            (int min, int max) = GetBounds();
            long previous = (long)CurrentIndex - 1L;
            if (previous < min)
            {
                CurrentIndex = Loop ? max : min;
            }
            else
            {
                CurrentIndex = (int)previous;
            }

            return SelectorModelResult.Changed();
        }

        public SelectorModelResult Set(int index)
        {
            if (Count <= 0)
            {
                return SelectorModelResult.NoItems;
            }

            (int min, int max) = GetBounds();
            if (Loop)
            {
                long range = (long)max - min + 1L;
                CurrentIndex = (int)(PositiveModulo((long)index - min, range) + min);
            }
            else
            {
                CurrentIndex = Math.Clamp(index, min, max);
            }

            MarkIndexUsedInUniqueMode(CurrentIndex);
            return SelectorModelResult.Changed();
        }

        public SelectorModelResult SetFirst()
        {
            if (Count <= 0)
            {
                return SelectorModelResult.NoItems;
            }

            (int min, _) = GetBounds();
            CurrentIndex = min;
            return SelectorModelResult.Changed();
        }

        public SelectorModelResult SetLast()
        {
            if (Count <= 0)
            {
                return SelectorModelResult.NoItems;
            }

            (_, int max) = GetBounds();
            CurrentIndex = max;
            return SelectorModelResult.Changed();
        }

        public SelectorModelResult Reset()
        {
            (int min, _) = GetBounds();
            CurrentIndex = min;
            return SelectorModelResult.Changed();
        }

        public SelectorModelResult ToggleFillMode()
        {
            FillMode = !FillMode;
            return SelectorModelResult.Changed();
        }

        public SelectorModelResult SetRandom(bool deactivateOthers, Func<int, int, int> randomRange)
        {
            if (Count <= 0)
            {
                return SelectorModelResult.NoItems;
            }

            randomRange ??= DefaultRandomRangeFunc;
            (int min, int max) = GetBounds();
            if (min > max)
            {
                return SelectorModelResult.NoChange;
            }

            int availableCount = CountCandidates(min, max, false);
            if (availableCount == 0)
            {
                return SelectorModelResult.NoChange;
            }

            if (UniqueSelectionMode)
            {
                return SetRandomUnique(min, max, deactivateOthers, randomRange);
            }

            if (availableCount == 1)
            {
                CurrentIndex = GetCandidate(min, max, 0, false);
                MarkIndexUsedInUniqueMode(CurrentIndex);
                return SelectorModelResult.Changed(deactivateOthers: deactivateOthers);
            }

            int pick = randomRange(0, availableCount);
            pick = Math.Clamp(pick, 0, availableCount - 1);
            int newIndex = GetCandidate(min, max, pick, false);
            if (newIndex == CurrentIndex)
            {
                newIndex = GetCandidate(min, max, (pick + 1) % availableCount, false);
            }

            CurrentIndex = newIndex;
            return SelectorModelResult.Changed(deactivateOthers: deactivateOthers);
        }

        public bool IsExcluded(int index)
        {
            return _excludedIndices.Contains(index);
        }

        public void ExcludeIndex(int index)
        {
            _excludedIndices.Add(index);
        }

        public void IncludeIndex(int index)
        {
            _excludedIndices.Remove(index);
        }

        public void IncludeAllIndices()
        {
            _excludedIndices.Clear();
        }

        public void SetExcludedIndices(IEnumerable<int> indices)
        {
            if (ReferenceEquals(indices, _excludedIndices))
            {
                return;
            }

            HashSet<int> replacement = indices == null ? null : new HashSet<int>(indices);
            _excludedIndices.Clear();
            if (replacement == null)
            {
                return;
            }

            _excludedIndices.UnionWith(replacement);
        }

        public void SetUsedIndicesForUnique(IEnumerable<int> indices)
        {
            if (ReferenceEquals(indices, _usedIndicesForUnique))
            {
                return;
            }

            HashSet<int> replacement = indices == null ? null : new HashSet<int>(indices);
            _usedIndicesForUnique.Clear();
            if (replacement == null)
            {
                return;
            }

            _usedIndicesForUnique.UnionWith(replacement);
        }

        public bool ResetUnique()
        {
            if (_usedIndicesForUnique.Count == 0)
            {
                return false;
            }

            _usedIndicesForUnique.Clear();
            return true;
        }

        public string SerializeExcludedIndices()
        {
            return SerializeIndices(_excludedIndices);
        }

        public static string SerializeIndices(IEnumerable<int> indices)
        {
            if (indices == null)
            {
                return string.Empty;
            }

            List<int> sorted = new(indices);
            if (sorted.Count == 0)
            {
                return string.Empty;
            }

            sorted.Sort();
            return string.Join(",", sorted);
        }

        public static HashSet<int> DeserializeIndices(string data)
        {
            HashSet<int> result = new();
            if (string.IsNullOrWhiteSpace(data))
            {
                return result;
            }

            string[] parts = data.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                if (int.TryParse(parts[i].Trim(), out int value))
                {
                    result.Add(value);
                }
            }

            return result;
        }

        public List<int> GetIncludedIndices()
        {
            List<int> result = new();
            if (Count <= 0)
            {
                return result;
            }

            (int min, int max) = GetBounds();
            for (long candidate = min; candidate <= max; candidate++)
            {
                int i = (int)candidate;
                if (!_excludedIndices.Contains(i))
                {
                    result.Add(i);
                }
            }

            return result;
        }

        private SelectorModelResult SetRandomUnique(int min, int max, bool deactivateOthers,
            Func<int, int, int> randomRange)
        {
            int availableCount = CountCandidates(min, max, true);
            bool cycleComplete = false;
            bool uniqueReset = false;
            if (availableCount == 0)
            {
                cycleComplete = true;
                if (!ResetUniqueWhenCycleComplete)
                {
                    return SelectorModelResult.CycleCompleteNoChange;
                }

                _usedIndicesForUnique.Clear();
                uniqueReset = true;
                availableCount = CountCandidates(min, max, true);
            }

            if (availableCount == 0)
            {
                return new SelectorModelResult(false, false, cycleComplete, uniqueReset, deactivateOthers);
            }

            int pick = randomRange(0, availableCount);
            pick = Math.Clamp(pick, 0, availableCount - 1);
            CurrentIndex = GetCandidate(min, max, pick, true);
            _usedIndicesForUnique.Add(CurrentIndex);
            return new SelectorModelResult(true, false, cycleComplete, uniqueReset, deactivateOthers);
        }

        // WHY: random picks count the candidates and then walk to the picked one instead of materializing a candidate
        // list, so SetRandom does not allocate per call. The walk visits indices in ascending order, which is the
        // order the former list had, so the same randomRange result picks the same index.
        private int CountCandidates(int min, int max, bool skipUsed)
        {
            int count = 0;
            for (long candidate = min; candidate <= max; candidate++)
            {
                if (IsRandomCandidate((int)candidate, skipUsed))
                {
                    count++;
                }
            }

            return count;
        }

        // WHY: the walk falls back to the last candidate (or the current index) instead of throwing, because a
        // randomRange callback that changes the sets between counting and walking must not crash a selection.
        private int GetCandidate(int min, int max, int ordinal, bool skipUsed)
        {
            int last = CurrentIndex;
            for (long candidate = min; candidate <= max; candidate++)
            {
                int index = (int)candidate;
                if (!IsRandomCandidate(index, skipUsed))
                {
                    continue;
                }

                if (ordinal == 0)
                {
                    return index;
                }

                last = index;
                ordinal--;
            }

            return last;
        }

        private bool IsRandomCandidate(int index, bool skipUsed)
        {
            return !_excludedIndices.Contains(index) && (!skipUsed || !_usedIndicesForUnique.Contains(index));
        }

        private void MarkIndexUsedInUniqueMode(int index)
        {
            if (UniqueSelectionMode)
            {
                _usedIndicesForUnique.Add(index);
            }
        }

        private static long PositiveModulo(long value, long modulo)
        {
            return (value % modulo + modulo) % modulo;
        }

        private static int DefaultRandomRange(int minInclusive, int maxExclusive)
        {
            return SharedRandom.Next(minInclusive, maxExclusive);
        }
    }

    public readonly struct SelectorModelResult
    {
        public static SelectorModelResult NoItems => new(false, false, false, false, true);
        public static SelectorModelResult NoChange => new(false, false, false, false, true);
        public static SelectorModelResult CycleCompleteNoChange => new(false, false, true, false, true);

        public SelectorModelResult(
            bool selectionChanged,
            bool reachedEnd,
            bool uniqueCycleComplete,
            bool uniqueReset,
            bool deactivateOthers)
        {
            SelectionChanged = selectionChanged;
            ReachedEnd = reachedEnd;
            UniqueCycleComplete = uniqueCycleComplete;
            UniqueReset = uniqueReset;
            DeactivateOthers = deactivateOthers;
        }

        public bool SelectionChanged { get; }
        public bool ReachedEnd { get; }
        public bool UniqueCycleComplete { get; }
        public bool UniqueReset { get; }
        public bool DeactivateOthers { get; }

        public static SelectorModelResult Changed(bool reachedEnd = false, bool deactivateOthers = true)
        {
            return new SelectorModelResult(true, reachedEnd, false, false, deactivateOthers);
        }
    }
}
