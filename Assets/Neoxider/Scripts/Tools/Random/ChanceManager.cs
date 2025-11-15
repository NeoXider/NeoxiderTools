using System;
using System.Collections.Generic;
using System.Linq;
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

        private Func<float> randomProvider;

        public ChanceManager(params float[] weights)
        {
            if (weights != null)
            {
                foreach (float weight in weights)
                {
                    entries.Add(new Entry(Mathf.Max(0f, weight), false));
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
                normalizeTarget = Mathf.Max(0.001f, value);
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

        public float TotalWeight
        {
            get
            {
                float sum = 0f;
                for (int i = 0; i < entries.Count; i++)
                {
                    sum += Mathf.Max(0f, entries[i].Weight);
                }

                return sum;
            }
        }

        public float GetNormalizedWeight(int index)
        {
            if (!IsValidIndex(index))
            {
                return 0f;
            }

            float total = TotalWeight;
            return total <= 0f ? 0f : Mathf.Max(0f, entries[index].Weight) / total;
        }

        public Entry AddEntry(float weight = 1f, string label = null, bool locked = false, int customId = -1)
        {
            if (entries == null)
            {
                entries = new List<Entry>();
            }

            Entry entry = new(Mathf.Max(0f, weight), locked, customId, label ?? $"Chance {entries.Count + 1}");

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
                entries.Add(new Entry(Mathf.Max(0f, weight), false));
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

            entries[index].Weight = Mathf.Max(0f, weight);
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
            float total = TotalWeight;
            if (entries.Count == 0 || total <= 0f)
            {
                return -1;
            }

            float value = randomProvider != null ? Mathf.Clamp01(randomProvider()) : Random.value;
            return GetId(value);
        }

        public int GetId(float randomValue)
        {
            if (entries.Count == 0)
            {
                return -1;
            }

            float total = TotalWeight;
            if (total <= 0f)
            {
                return -1;
            }

            float value = Mathf.Clamp01(randomValue) * total;
            float cumulative = 0f;

            for (int i = 0; i < entries.Count; i++)
            {
                cumulative += Mathf.Max(0f, entries[i].Weight);
                if (value <= cumulative)
                {
                    return i;
                }
            }

            return entries.Count - 1;
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

        public void Normalize()
        {
            Normalize(normalizeTarget);
        }

        public void Normalize(float targetSum)
        {
            targetSum = Mathf.Max(0f, targetSum);
            if (entries.Count == 0)
            {
                return;
            }

            List<Entry> lockedEntries = new();
            List<Entry> unlockedEntries = new();
            for (int i = 0; i < entries.Count; i++)
            {
                Entry entry = entries[i];
                entry.Weight = Mathf.Max(0f, entry.Weight);
                if (entry.Locked)
                {
                    lockedEntries.Add(entry);
                }
                else
                {
                    unlockedEntries.Add(entry);
                }
            }

            float lockedSum = lockedEntries.Sum(e => e.Weight);
            float remaining = targetSum - lockedSum;

            if (unlockedEntries.Count == 0)
            {
                return;
            }

            if (remaining <= 0f)
            {
                foreach (Entry entry in unlockedEntries)
                {
                    entry.Weight = 0f;
                }

                return;
            }

            float unlockedSum = unlockedEntries.Sum(e => e.Weight);
            if (unlockedSum <= 0f)
            {
                if (!distributeEvenlyWhenZero)
                {
                    return;
                }

                float even = remaining / unlockedEntries.Count;
                foreach (Entry entry in unlockedEntries)
                {
                    entry.Weight = even;
                }

                return;
            }

            float scale = remaining / unlockedSum;
            foreach (Entry entry in unlockedEntries)
            {
                entry.Weight *= scale;
            }
        }

        public void CopyFrom(ChanceManager source)
        {
            if (source == null)
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
            if (entries == null)
            {
                entries = new List<Entry>();
            }

            for (int i = 0; i < entries.Count; i++)
            {
                Entry entry = entries[i];
                entry.Weight = Mathf.Max(0f, entry.Weight);
                if (string.IsNullOrEmpty(entry.Label))
                {
                    entry.Label = $"Chance {i + 1}";
                }
            }

            if (autoNormalize)
            {
                Normalize();
            }
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
                this.weight = Mathf.Max(0f, weight);
                this.locked = locked;
                id = customId;
                this.label = label;
            }

            public Entry(Entry other)
            {
                label = other.label;
                weight = Mathf.Max(0f, other.weight);
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
                set => weight = Mathf.Max(0f, value);
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