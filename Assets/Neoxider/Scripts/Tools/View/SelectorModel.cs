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

        public int IndexWithOffset => CurrentIndex + IndexOffset;
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
                if (!UniqueSelectionMode)
                {
                    return 0;
                }

                (int min, int max) = GetBounds();
                if (min > max)
                {
                    return 0;
                }

                int remaining = 0;
                for (int i = min; i <= max; i++)
                {
                    if (!_usedIndicesForUnique.Contains(i) && !_excludedIndices.Contains(i))
                    {
                        remaining++;
                    }
                }

                return remaining;
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
            Count = Math.Max(0, count);
            CurrentIndex = currentIndex;
            IndexOffset = indexOffset;
            Loop = loop;
            FillMode = fillMode;
            AllowEmptyEffectiveIndex = allowEmptyEffectiveIndex;
            UniqueSelectionMode = uniqueSelectionMode;
            ResetUniqueWhenCycleComplete = resetUniqueWhenCycleComplete;
            SetExcludedIndices(excludedIndices);
            SetUsedIndicesForUnique(usedIndicesForUnique);
        }

        public (int min, int max) GetBounds()
        {
            if (Count <= 0)
            {
                return (0, 0);
            }

            int effectiveMin = AllowEmptyEffectiveIndex ? -1 : 0;
            int min = effectiveMin - IndexOffset;
            int max = Count - 1 - IndexOffset;
            return (min, max);
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
            if (Count <= 0)
            {
                return 0;
            }

            int minEffective = AllowEmptyEffectiveIndex ? -1 : 0;
            int effectiveIndex = CurrentIndex + IndexOffset;
            if (effectiveIndex < minEffective)
            {
                return minEffective;
            }

            if (effectiveIndex >= Count)
            {
                return Count - 1;
            }

            return effectiveIndex;
        }

        public int GetLogicalActiveCount()
        {
            if (Count <= 0)
            {
                return 0;
            }

            int effectiveIndex = GetEffectiveIndex();
            if (effectiveIndex < 0)
            {
                return 0;
            }

            return FillMode ? Math.Min(Count, effectiveIndex + 1) : 1;
        }

        public SelectorModelResult Next()
        {
            if (Count <= 0)
            {
                return SelectorModelResult.NoItems;
            }

            CurrentIndex++;
            bool finished = false;
            (int min, int max) = GetBounds();
            if (CurrentIndex > max)
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

            return SelectorModelResult.Changed(finished);
        }

        public SelectorModelResult Previous()
        {
            if (Count <= 0)
            {
                return SelectorModelResult.NoItems;
            }

            CurrentIndex--;
            (int min, int max) = GetBounds();
            if (CurrentIndex < min)
            {
                CurrentIndex = Loop ? max : min;
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
                int range = max - min + 1;
                CurrentIndex = range <= 0 ? min : PositiveModulo(index - min, range) + min;
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

            randomRange ??= DefaultRandomRange;
            (int min, int max) = GetBounds();
            if (min > max)
            {
                return SelectorModelResult.NoChange;
            }

            List<int> availableNonExcluded = GetAvailableNonExcluded(min, max);
            if (availableNonExcluded.Count == 0)
            {
                return SelectorModelResult.NoChange;
            }

            if (availableNonExcluded.Count == 1)
            {
                CurrentIndex = availableNonExcluded[0];
                MarkIndexUsedInUniqueMode(CurrentIndex);
                return SelectorModelResult.Changed(deactivateOthers: deactivateOthers);
            }

            if (UniqueSelectionMode)
            {
                return SetRandomUnique(min, max, deactivateOthers, randomRange);
            }

            int pick = randomRange(0, availableNonExcluded.Count);
            pick = Math.Clamp(pick, 0, availableNonExcluded.Count - 1);
            int newIndex = availableNonExcluded[pick];
            if (newIndex == CurrentIndex && availableNonExcluded.Count > 1)
            {
                int next = (pick + 1) % availableNonExcluded.Count;
                newIndex = availableNonExcluded[next];
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
            _excludedIndices.Clear();
            if (indices == null)
            {
                return;
            }

            foreach (int index in indices)
            {
                _excludedIndices.Add(index);
            }
        }

        public void SetUsedIndicesForUnique(IEnumerable<int> indices)
        {
            _usedIndicesForUnique.Clear();
            if (indices == null)
            {
                return;
            }

            foreach (int index in indices)
            {
                _usedIndicesForUnique.Add(index);
            }
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
            for (int i = min; i <= max; i++)
            {
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
            List<int> available = new();
            for (int i = min; i <= max; i++)
            {
                if (!_usedIndicesForUnique.Contains(i) && !_excludedIndices.Contains(i))
                {
                    available.Add(i);
                }
            }

            bool cycleComplete = false;
            bool uniqueReset = false;
            if (available.Count == 0)
            {
                cycleComplete = true;
                if (ResetUniqueWhenCycleComplete)
                {
                    _usedIndicesForUnique.Clear();
                    uniqueReset = true;
                    for (int i = min; i <= max; i++)
                    {
                        if (!_excludedIndices.Contains(i))
                        {
                            available.Add(i);
                        }
                    }
                }
                else
                {
                    return SelectorModelResult.CycleCompleteNoChange;
                }
            }

            if (available.Count == 0)
            {
                return new SelectorModelResult(false, false, cycleComplete, uniqueReset, deactivateOthers);
            }

            int pick = randomRange(0, available.Count);
            pick = Math.Clamp(pick, 0, available.Count - 1);
            CurrentIndex = available[pick];
            _usedIndicesForUnique.Add(CurrentIndex);
            return new SelectorModelResult(true, false, cycleComplete, uniqueReset, deactivateOthers);
        }

        private List<int> GetAvailableNonExcluded(int min, int max)
        {
            List<int> result = new(max - min + 1);
            for (int i = min; i <= max; i++)
            {
                if (!_excludedIndices.Contains(i))
                {
                    result.Add(i);
                }
            }

            return result;
        }

        private void MarkIndexUsedInUniqueMode(int index)
        {
            if (UniqueSelectionMode)
            {
                _usedIndicesForUnique.Add(index);
            }
        }

        private static int PositiveModulo(int value, int modulo)
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
