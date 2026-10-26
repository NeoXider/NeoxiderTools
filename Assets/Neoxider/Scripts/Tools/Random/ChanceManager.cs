using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Neo.Tools
{
    /// <summary>
    ///     Probability helper that works both in-editor and at runtime. Stores a list of weighted entries,
    ///     provides normalization with support for locked items, and can generate random indices using any random source.
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

        private Func<float> randomProvider;

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

        public IReadOnlyList<Entry> Entries => entries;
        public int Count => entries.Count;

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

        public bool DistributeEvenlyWhenZero
        {
            get => distributeEvenlyWhenZero;
            set => distributeEvenlyWhenZero = value;
        }

        public Func<float> RandomProvider
        {
            get => randomProvider;
            set => randomProvider = value;
        }

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

        public float GetNormalizedWeight(int index)
        {
            if (!IsValidIndex(index))
            {
                return 0f;
            }

            double total = GetTotalWeight();
            return total <= 0d ? 0f : (float)(SanitizeWeight(entries[index].Weight) / total);
        }

        public Entry AddEntry(float weight = 1f, string label = null, bool locked = false, int customId = -1)
        {
            if (entries == null)
            {
                entries = new List<Entry>();
            }

            Entry entry = new(weight, locked, customId, label ?? $"Chance {entries.Count + 1}");

            if (entry.CustomId < 0)
            {
                HashSet<int> used = new();
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

        public int AddChance(float chance)
        {
            return entries.IndexOf(AddEntry(chance));
        }

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

        public void RemoveChance(int index)
        {
            if (!IsValidIndex(index))
            {
                return;
            }

            entries.RemoveAt(index);
            OnCollectionChanged();
        }

        public void Clear()
        {
            entries.Clear();
            _totalWeightDirty = true;
        }

        public Entry GetEntry(int index)
        {
            return IsValidIndex(index) ? entries[index] : null;
        }

        public float GetChanceValue(int index)
        {
            return IsValidIndex(index) ? entries[index].Weight : 0f;
        }

        public void SetChanceValue(int index, float weight)
        {
            if (!IsValidIndex(index))
            {
                return;
            }

            entries[index].Weight = weight;
            OnCollectionChanged();
        }

        public void SetLocked(int index, bool locked)
        {
            if (!IsValidIndex(index))
            {
                return;
            }

            entries[index].Locked = locked;
            OnCollectionChanged();
        }

        public int GetChanceId()
        {
            if (entries.Count == 0 || GetTotalWeight() <= 0d)
            {
                return -1;
            }

            float value = randomProvider != null ? randomProvider() : Random.value;
            return GetId(value);
        }

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

        public Entry Evaluate()
        {
            int index = GetChanceId();
            return index >= 0 ? entries[index] : null;
        }

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

        public void Normalize()
        {
            Normalize(normalizeTarget);
        }

        public void Normalize(float targetSum)
        {
            _totalWeightDirty = true;
            targetSum = SanitizeWeight(targetSum);
            if (entries.Count == 0)
            {
                return;
            }

            List<Entry> unlockedEntries = new();
            double lockedSum = 0d;
            double unlockedSum = 0d;
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
                    unlockedEntries.Add(entry);
                    unlockedSum += entry.Weight;
                }
            }

            double remaining = targetSum - lockedSum;

            if (unlockedEntries.Count == 0)
            {
                return;
            }

            if (remaining <= 0d)
            {
                foreach (Entry entry in unlockedEntries)
                {
                    entry.Weight = 0f;
                }

                return;
            }

            if (unlockedSum <= 0d)
            {
                if (!distributeEvenlyWhenZero)
                {
                    return;
                }

                float even = (float)(remaining / unlockedEntries.Count);
                foreach (Entry entry in unlockedEntries)
                {
                    entry.Weight = even;
                }

                return;
            }

            double scale = remaining / unlockedSum;
            foreach (Entry entry in unlockedEntries)
            {
                entry.Weight = (float)(entry.Weight * scale);
            }
        }

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

        public void EnsureUniqueIds()
        {
            HashSet<int> usedIds = new();
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
            [SerializeField] [Range(0f, 1f)] private float weight = 1f;
            [SerializeField] private bool locked;
            [SerializeField] private int id = -1;
            [SerializeField] private string label = "Chance";
            [SerializeField] [TextArea] private string notes;

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
