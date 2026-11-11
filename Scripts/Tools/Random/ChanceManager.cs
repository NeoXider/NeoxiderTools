using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Neo.Tools
{
    /// <summary>
    ///     Probability helper that works both in-editor and at runtime. Stores a list of weighted entries,
    ///     provides normalization with support for locked items, and can generate random indices using any random source.
    ///     Weights are floats; use <see cref="IntegerChanceTable" /> when odds must stay exact (for example 1 per million).
    /// </summary>
    [Serializable]
    public class ChanceManager
    {
        [SerializeField] private List<Entry> entries = new();
        [SerializeField] private bool autoNormalize = true;
        [SerializeField] [Min(0.001f)] private float normalizeTarget = 1f;
        [SerializeField] private bool distributeEvenlyWhenZero = true;
        private double _cachedTotalWeight;
        private bool _totalWeightDirty = true;
        [NonSerialized] private HashSet<int> _idScratch;

        private Func<float> randomProvider;

        /// <summary>Creates a table with one unlocked entry per weight.</summary>
        public ChanceManager(params float[] weights)
        {
            if (weights != null)
            {
                foreach (float weight in weights)
                {
                    entries.Add(new Entry(weight, false));
                }
            }

            Sanitize();
        }

        /// <summary>Entries in roll order.</summary>
        public IReadOnlyList<Entry> Entries => entries;

        /// <summary>Number of entries.</summary>
        public int Count => entries.Count;

        /// <summary>When true, every structural or weight change rescales unlocked weights to <see cref="NormalizeTarget" />.</summary>
        public bool AutoNormalize
        {
            get => autoNormalize;
            set
            {
                autoNormalize = value;
                if (autoNormalize)
                {
                    Normalize();
                }
            }
        }

        /// <summary>Sum that <see cref="Normalize()" /> scales all weights to (minimum 0.001).</summary>
        public float NormalizeTarget
        {
            get => normalizeTarget;
            set
            {
                normalizeTarget = Mathf.Max(0.001f, SanitizeWeight(value));
                if (autoNormalize)
                {
                    Normalize();
                }
            }
        }

        /// <summary>When every unlocked weight is zero, normalization splits the remaining budget evenly.</summary>
        public bool DistributeEvenlyWhenZero
        {
            get => distributeEvenlyWhenZero;
            set => distributeEvenlyWhenZero = value;
        }

        /// <summary>Source of uniform values in [0..1] used by rolls; <c>null</c> uses <see cref="UnityEngine.Random.value" />.</summary>
        public Func<float> RandomProvider
        {
            get => randomProvider;
            set => randomProvider = value;
        }

        /// <summary>Sum of sanitized weights (cached until the table changes).</summary>
        public float TotalWeight => (float)GetTotalWeight();

        private double GetTotalWeight()
        {
            if (_totalWeightDirty)
            {
                _cachedTotalWeight = 0d;
                for (int i = 0; i < entries.Count; i++)
                {
                    _cachedTotalWeight += SanitizeWeight(entries[i].Weight);
                }

                _totalWeightDirty = false;
            }

            return _cachedTotalWeight;
        }

        /// <summary>
        ///     Marks TotalWeight cache as stale.
        /// </summary>
        public void InvalidateTotalWeight()
        {
            _totalWeightDirty = true;
        }

        /// <summary>Probability of <paramref name="index" /> in [0..1]; 0 for an invalid index or an empty table.</summary>
        public float GetNormalizedWeight(int index)
        {
            if (!IsValidIndex(index))
            {
                return 0f;
            }

            double total = GetTotalWeight();
            return total <= 0d ? 0f : (float)(SanitizeWeight(entries[index].Weight) / total);
        }

        /// <summary>Appends an entry; a negative <paramref name="customId" /> gets the lowest free id.</summary>
        public Entry AddEntry(float weight = 1f, string label = null, bool locked = false, int customId = -1)
        {
            if (entries == null)
            {
                entries = new List<Entry>();
            }

            Entry entry = new(weight, locked, customId, label ?? $"Chance {entries.Count + 1}");

            if (entry.CustomId < 0)
            {
                HashSet<int> used = RentIdScratch();
                for (int i = 0; i < entries.Count; i++)
                {
                    used.Add(entries[i].CustomId);
                }

                entry.CustomId = GenerateFreeId(used);
            }

            entries.Add(entry);
            OnCollectionChanged();
            return entry;
        }

        /// <summary>Appends an entry and returns its index.</summary>
        public int AddChance(float chance)
        {
            AddEntry(chance);
            return entries.Count - 1;
        }

        /// <summary>Appends one unlocked entry per weight.</summary>
        public void AddChances(IEnumerable<float> weights)
        {
            if (weights == null)
            {
                return;
            }

            foreach (float weight in weights)
            {
                entries.Add(new Entry(weight, false));
            }

            OnCollectionChanged();
        }

        /// <summary>Removes the entry at <paramref name="index" />.</summary>
        public void RemoveChance(int index)
        {
            if (!IsValidIndex(index))
            {
                return;
            }

            entries.RemoveAt(index);
            OnCollectionChanged();
        }

        /// <summary>Removes every entry.</summary>
        public void Clear()
        {
            entries.Clear();
            _totalWeightDirty = true;
        }

        /// <summary>Entry at <paramref name="index" />, or null.</summary>
        public Entry GetEntry(int index)
        {
            return IsValidIndex(index) ? entries[index] : null;
        }

        /// <summary>Raw weight at <paramref name="index" />, or 0.</summary>
        public float GetChanceValue(int index)
        {
            return IsValidIndex(index) ? entries[index].Weight : 0f;
        }

        /// <summary>Sets the raw weight at <paramref name="index" /> (then normalizes when <see cref="AutoNormalize" /> is on).</summary>
        public void SetChanceValue(int index, float weight)
        {
            if (!IsValidIndex(index))
            {
                return;
            }

            entries[index].Weight = weight;
            OnCollectionChanged();
        }

        /// <summary>Locks or unlocks the weight at <paramref name="index" /> against normalization.</summary>
        public void SetLocked(int index, bool locked)
        {
            if (!IsValidIndex(index))
            {
                return;
            }

            entries[index].Locked = locked;
            OnCollectionChanged();
        }

        /// <summary>Rolls once with <see cref="RandomProvider" />; returns -1 when nothing can be selected.</summary>
        public int GetChanceId()
        {
            if (entries.Count == 0 || GetTotalWeight() <= 0d)
            {
                return -1;
            }

            float value = randomProvider != null ? randomProvider() : Random.value;
            return GetId(value);
        }

        /// <summary>Deterministic selection for a uniform value in [0..1]; zero-weight entries are skipped.</summary>
        public int GetId(float randomValue)
        {
            if (entries.Count == 0 || float.IsNaN(randomValue))
            {
                return -1;
            }

            double total = GetTotalWeight();
            if (total <= 0d)
            {
                return -1;
            }

            double value = Mathf.Clamp01(randomValue) * total;
            double cumulative = 0d;
            int lastPositiveIndex = -1;

            for (int i = 0; i < entries.Count; i++)
            {
                float weight = SanitizeWeight(entries[i].Weight);
                if (weight <= 0f)
                {
                    continue;
                }

                lastPositiveIndex = i;
                cumulative += weight;
                if (value < cumulative)
                {
                    return i;
                }
            }

            return lastPositiveIndex;
        }

        /// <summary>
        ///     Draws up to <paramref name="count" /> different indices by weight, without repeats, into
        ///     <paramref name="result" /> (cleared first). Entries with zero weight, and entries whose
        ///     <see cref="Entry.MaxPicks" /> is reached in <paramref name="picksTaken" />, are skipped. Returns the number
        ///     drawn (fewer than <paramref name="count" /> when the table runs out). Allocation-free; uses
        ///     <see cref="RandomProvider" />.
        /// </summary>
        /// <param name="picksTaken">How often each index was taken so far (null = never). Same order as entries.</param>
        public int PickUnique(int count, List<int> result, IReadOnlyList<int> picksTaken = null)
        {
            result.Clear();
            for (int draw = 0; draw < count; draw++)
            {
                double total = 0d;
                for (int i = 0; i < entries.Count; i++)
                {
                    total += AvailableWeight(i, result, picksTaken);
                }

                if (total <= 0d)
                {
                    break;
                }

                float roll = randomProvider != null ? randomProvider() : Random.value;
                double value = Mathf.Clamp01(roll) * total;
                int chosen = -1;
                for (int i = 0; i < entries.Count; i++)
                {
                    double weight = AvailableWeight(i, result, picksTaken);
                    if (weight <= 0d)
                    {
                        continue;
                    }

                    chosen = i;
                    value -= weight;
                    if (value < 0d)
                    {
                        break;
                    }
                }

                if (chosen < 0)
                {
                    break;
                }

                result.Add(chosen);
            }

            return result.Count;
        }

        /// <summary>True when entry <paramref name="index" /> still has picks left (no limit, or fewer taken).</summary>
        public bool HasPicksLeft(int index, IReadOnlyList<int> picksTaken)
        {
            if (!IsValidIndex(index))
            {
                return false;
            }

            int max = entries[index].MaxPicks;
            int taken = picksTaken != null && index < picksTaken.Count ? picksTaken[index] : 0;
            return max <= 0 || taken < max;
        }

        private double AvailableWeight(int index, List<int> alreadyDrawn, IReadOnlyList<int> picksTaken)
        {
            if (alreadyDrawn.Contains(index) || !HasPicksLeft(index, picksTaken))
            {
                return 0d;
            }

            return SanitizeWeight(entries[index].Weight);
        }

        /// <summary>Rolls once and returns the selected entry, or null.</summary>
        public Entry Evaluate()
        {
            int index = GetChanceId();
            return index >= 0 ? entries[index] : null;
        }

        /// <summary>Deterministic variant of <see cref="Evaluate()" /> for a value in [0..1].</summary>
        public Entry Evaluate(float randomValue)
        {
            int index = GetId(randomValue);
            return index >= 0 ? entries[index] : null;
        }

        /// <summary>
        ///     Evaluates and returns both index and entry. Returns false if no entries or zero total weight.
        /// </summary>
        public bool TryEvaluate(out int index, out Entry entry)
        {
            index = GetChanceId();
            if (index >= 0 && index < entries.Count)
            {
                entry = entries[index];
                return true;
            }

            entry = null;
            return false;
        }

        /// <summary>
        ///     Same as TryEvaluate but uses the given random value in [0..1] for deterministic roll.
        /// </summary>
        public bool TryEvaluate(float randomValue, out int index, out Entry entry)
        {
            index = GetId(randomValue);
            if (index >= 0 && index < entries.Count)
            {
                entry = entries[index];
                return true;
            }

            entry = null;
            return false;
        }

        /// <summary>Scales unlocked weights so the table sums to <see cref="NormalizeTarget" />.</summary>
        public void Normalize()
        {
            Normalize(normalizeTarget);
        }

        /// <summary>
        ///     Scales unlocked weights so all weights sum to <paramref name="targetSum" />. Locked weights are kept;
        ///     when they already exceed the target, unlocked weights become zero.
        /// </summary>
        public void Normalize(float targetSum)
        {
            _totalWeightDirty = true;
            targetSum = SanitizeWeight(targetSum);
            if (entries.Count == 0)
            {
                return;
            }

            double lockedSum = 0d;
            double unlockedSum = 0d;
            int unlockedCount = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                Entry entry = entries[i];
                entry.Weight = SanitizeWeight(entry.Weight);
                if (entry.Locked)
                {
                    lockedSum += entry.Weight;
                }
                else
                {
                    unlockedCount++;
                    unlockedSum += entry.Weight;
                }
            }

            double remaining = targetSum - lockedSum;

            if (unlockedCount == 0)
            {
                return;
            }

            if (remaining <= 0d)
            {
                SetUnlockedWeights(0f, 0d);
                return;
            }

            if (unlockedSum <= 0d)
            {
                if (!distributeEvenlyWhenZero)
                {
                    return;
                }

                SetUnlockedWeights((float)(remaining / unlockedCount), 0d);
                return;
            }

            SetUnlockedWeights(0f, remaining / unlockedSum);
        }

        /// <summary>Writes a constant (when <paramref name="scale" /> is 0) or a scaled weight into every unlocked entry.</summary>
        private void SetUnlockedWeights(float constant, double scale)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                Entry entry = entries[i];
                if (entry.Locked)
                {
                    continue;
                }

                entry.Weight = scale > 0d ? (float)(entry.Weight * scale) : constant;
            }
        }

        /// <summary>Replaces entries and normalization settings with copies from <paramref name="source" />.</summary>
        public void CopyFrom(ChanceManager source)
        {
            if (source == null || ReferenceEquals(source, this))
            {
                return;
            }

            entries.Clear();
            foreach (Entry entry in source.entries)
            {
                entries.Add(new Entry(entry));
            }

            autoNormalize = source.autoNormalize;
            normalizeTarget = source.normalizeTarget;
            distributeEvenlyWhenZero = source.distributeEvenlyWhenZero;

            Sanitize();
        }

        /// <summary>Repairs non-finite/negative weights, empty labels and the normalization target.</summary>
        public void Sanitize()
        {
            normalizeTarget = Mathf.Max(0.001f, SanitizeWeight(normalizeTarget));
            if (entries == null)
            {
                entries = new List<Entry>();
            }

            for (int i = 0; i < entries.Count; i++)
            {
                Entry entry = entries[i];
                entry.Weight = SanitizeWeight(entry.Weight);
                if (string.IsNullOrEmpty(entry.Label))
                {
                    entry.Label = $"Chance {i + 1}";
                }
            }

            _totalWeightDirty = true;

            if (autoNormalize)
            {
                Normalize();
            }
        }

        /// <summary>
        ///     Validates weights and returns a list of issues.
        /// </summary>
        public List<string> ValidateWeights()
        {
            List<string> issues = new();

            if (entries == null || entries.Count == 0)
            {
                issues.Add("Entry list is empty");
                return issues;
            }

            float total = TotalWeight;
            if (total <= 0f)
            {
                issues.Add("Total weight is 0 — no entry can be selected");
            }

            int zeroWeightCount = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                Entry entry = entries[i];
                if (float.IsNaN(entry.Weight) || float.IsInfinity(entry.Weight))
                {
                    issues.Add($"[{i}] '{entry.Label}': non-finite weight ({entry.Weight})");
                }
                else if (entry.Weight < 0f)
                {
                    issues.Add($"[{i}] '{entry.Label}': negative weight ({entry.Weight})");
                }
                else if (entry.Weight == 0f && !entry.Locked)
                {
                    zeroWeightCount++;
                }
            }

            if (zeroWeightCount > 0 && zeroWeightCount < entries.Count)
            {
                issues.Add($"{zeroWeightCount} entries with zero weight will never be selected");
            }

            return issues;
        }

        /// <summary>Assigns the lowest free id to entries with a negative or duplicate custom id.</summary>
        public void EnsureUniqueIds()
        {
            HashSet<int> usedIds = RentIdScratch();
            foreach (Entry entry in entries)
            {
                if (entry.CustomId < 0 || usedIds.Contains(entry.CustomId))
                {
                    entry.CustomId = GenerateFreeId(usedIds);
                }

                usedIds.Add(entry.CustomId);
            }
        }

        private void OnCollectionChanged()
        {
            _totalWeightDirty = true;

            if (autoNormalize)
            {
                Normalize();
            }

            EnsureUniqueIds();
        }

        private HashSet<int> RentIdScratch()
        {
            _idScratch ??= new HashSet<int>();
            _idScratch.Clear();
            return _idScratch;
        }

        private static int GenerateFreeId(HashSet<int> used)
        {
            int id = 0;
            while (used.Contains(id))
            {
                id++;
            }

            return id;
        }

        private static float SanitizeWeight(float weight)
        {
            return float.IsNaN(weight) || float.IsInfinity(weight) ? 0f : Mathf.Max(0f, weight);
        }

        private bool IsValidIndex(int index)
        {
            return index >= 0 && index < entries.Count;
        }

        [Serializable]
        public class Entry
        {
            // WHY: Min instead of Range(0,1): with AutoNormalize off or a NormalizeTarget above 1, valid weights
            // exceed 1 and a 0..1 slider would clamp them on the first Inspector edit.
            [SerializeField] [Min(0f)] private float weight = 1f;
            [SerializeField] private bool locked;
            [SerializeField] private int id = -1;
            [SerializeField] private string label = "Chance";
            [SerializeField] [TextArea] private string notes;

            [Tooltip("How many times this entry may be taken (ChanceSystemBehaviour.ConsumePick) before unique draws " +
                     "skip it. 0 = unlimited.")]
            [SerializeField] [Min(0)] private int maxPicks;

            public Entry()
            {
            }

            public Entry(float weight, bool locked, int customId = -1, string label = "Chance")
            {
                this.weight = SanitizeWeight(weight);
                this.locked = locked;
                id = customId;
                this.label = label;
            }

            public Entry(Entry other)
            {
                label = other.label;
                weight = SanitizeWeight(other.weight);
                locked = other.locked;
                id = other.id;
                notes = other.notes;
                maxPicks = other.maxPicks;
            }

            /// <summary>Picks allowed before unique draws skip this entry; 0 = unlimited.</summary>
            public int MaxPicks
            {
                get => maxPicks;
                set => maxPicks = Mathf.Max(0, value);
            }

            public string Label
            {
                get => label;
                set => label = value;
            }

            public float Weight
            {
                get => weight;
                set => weight = SanitizeWeight(value);
            }

            public bool Locked
            {
                get => locked;
                set => locked = value;
            }

            public int CustomId
            {
                get => id;
                set => id = value;
            }

            public string Notes
            {
                get => notes;
                set => notes = value;
            }
        }
    }
}
