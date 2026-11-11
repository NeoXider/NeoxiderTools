using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    ///     Exact weighted table with integer weights, for odds that must not lose precision (for example a jackpot
    ///     at 1 per 1,000,000). Selection maps a uniform ticket in <c>[0, TotalWeight)</c> to an entry through
    ///     cached prefix sums (O(log n)), so every entry's probability is exactly <c>weight / TotalWeight</c>.
    ///     Plain C#, serializable, and independent of <see cref="ChanceManager" /> (whose float weights normalize).
    /// </summary>
    /// <example>
    ///     <code>
    /// IntegerChanceTable odds = new IntegerChanceTable();
    /// odds.Add(1, "Jackpot");          // 1 in 1,000,000
    /// odds.Add(999_999, "Nothing");
    /// int index = odds.Roll();         // or odds.GetIndex(ticket) with your own ticket source
    /// </code>
    /// </example>
    [Serializable]
    public sealed class IntegerChanceTable : ISerializationCallbackReceiver
    {
        [SerializeField] private List<Entry> entries = new();

        // WHY: Default-false validity flag instead of a dirty flag with an initializer, so instances created by
        // Unity's serializer (which may skip field initializers) still rebuild the cache on first use.
        [NonSerialized] private long[] _prefix;
        [NonSerialized] private long _total;
        [NonSerialized] private bool _cacheValid;
        [NonSerialized] private Func<long, long> _randomBelow;

        /// <summary>Creates an empty table.</summary>
        public IntegerChanceTable()
        {
        }

        /// <summary>Creates a table with one entry per weight; negative weights are stored as zero.</summary>
        public IntegerChanceTable(params int[] weights)
        {
            if (weights == null)
            {
                return;
            }

            for (int i = 0; i < weights.Length; i++)
            {
                Add(weights[i]);
            }
        }

        /// <summary>Entries in roll order.</summary>
        public IReadOnlyList<Entry> Entries => entries;

        /// <summary>Number of entries, including zero-weight ones.</summary>
        public int Count => entries.Count;

        /// <summary>Exact sum of all weights.</summary>
        public long TotalWeight
        {
            get
            {
                EnsureCache();
                return _total;
            }
        }

        /// <summary>
        ///     Uniform source returning a value in <c>[0, n)</c> for a positive <c>n</c>. Defaults to an unbiased
        ///     sampler over <see cref="UnityEngine.Random" />, so <c>Random.InitState</c> keeps rolls reproducible.
        ///     Assign a seeded source for deterministic simulations; <c>null</c> restores the default.
        /// </summary>
        public Func<long, long> RandomBelow
        {
            get => _randomBelow;
            set => _randomBelow = value;
        }

        /// <summary>Appends an entry and returns it.</summary>
        public Entry Add(int weight, string label = null, int customId = -1)
        {
            Entry entry = new(weight, label ?? $"Chance {entries.Count + 1}", customId);
            entries.Add(entry);
            _cacheValid = false;
            return entry;
        }

        /// <summary>Changes the weight at <paramref name="index" />; negative weights are stored as zero.</summary>
        public void SetWeight(int index, int weight)
        {
            if (!IsValidIndex(index))
            {
                return;
            }

            entries[index].SetWeight(weight);
            _cacheValid = false;
        }

        /// <summary>Weight at <paramref name="index" />, or 0 for an invalid index.</summary>
        public int GetWeight(int index)
        {
            return IsValidIndex(index) ? entries[index].Weight : 0;
        }

        /// <summary>Removes the entry at <paramref name="index" />.</summary>
        public void RemoveAt(int index)
        {
            if (!IsValidIndex(index))
            {
                return;
            }

            entries.RemoveAt(index);
            _cacheValid = false;
        }

        /// <summary>Removes every entry.</summary>
        public void Clear()
        {
            entries.Clear();
            _cacheValid = false;
        }

        /// <summary>
        ///     Marks the cached prefix sums stale. Rarely needed: table methods and every deserialization (including
        ///     Inspector edits) already do it. Use it after writing the serialized rows by other means, such as
        ///     reflection.
        /// </summary>
        public void Invalidate()
        {
            _cacheValid = false;
        }

        /// <inheritdoc />
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
        }

        // WHY: Unity writes Inspector edits (also in Play Mode) back through deserialization without touching the
        // table's methods. Dropping the cache here keeps Roll/GetIndex on the same odds as the edited rows.
        /// <inheritdoc />
        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            _cacheValid = false;
        }

        /// <summary>Exact probability of <paramref name="index" /> as a fraction; false for an invalid index or empty table.</summary>
        public bool TryGetOdds(int index, out long weight, out long total)
        {
            total = TotalWeight;
            weight = GetWeight(index);
            return IsValidIndex(index) && total > 0;
        }

        /// <summary>Probability of <paramref name="index" /> as a double (0 when invalid or the table is empty).</summary>
        public double GetProbability(int index)
        {
            return TryGetOdds(index, out long weight, out long total) ? (double)weight / total : 0d;
        }

        /// <summary>
        ///     Maps an exact ticket in <c>[0, TotalWeight)</c> to an entry index; out-of-range tickets and empty tables
        ///     return -1. Zero-weight entries are never selected.
        /// </summary>
        public int GetIndex(long ticket)
        {
            EnsureCache();
            if (_total <= 0 || ticket < 0 || ticket >= _total)
            {
                return -1;
            }

            int low = 0;
            int high = _prefix.Length - 1;
            while (low < high)
            {
                int middle = low + ((high - low) >> 1);
                if (ticket < _prefix[middle])
                {
                    high = middle;
                }
                else
                {
                    low = middle + 1;
                }
            }

            return low;
        }

        /// <summary>Rolls once with <see cref="RandomBelow" />; returns -1 when the total weight is zero.</summary>
        public int Roll()
        {
            long total = TotalWeight;
            if (total <= 0)
            {
                return -1;
            }

            long ticket = _randomBelow != null ? _randomBelow(total) : UnityRandomBelow(total);
            return GetIndex(ticket);
        }

        /// <summary>Rolls once and returns the index and entry; false when nothing can be selected.</summary>
        public bool TryRoll(out int index, out Entry entry)
        {
            index = Roll();
            entry = index >= 0 ? entries[index] : null;
            return entry != null;
        }

        /// <summary>
        ///     Unbiased uniform value in <c>[0, n)</c> built from 30-bit <see cref="UnityEngine.Random" /> draws with
        ///     rejection sampling, so even large totals carry no modulo bias. Accepts every positive <c>long</c> up to
        ///     <see cref="long.MaxValue" />; <c>n &lt;= 1</c> returns 0.
        /// </summary>
        public static long UnityRandomBelow(long n)
        {
            if (n <= 1)
            {
                return 0;
            }

            const long chunk = 1L << 30;
            if (n <= chunk)
            {
                long limit30 = chunk - chunk % n;
                while (true)
                {
                    long value30 = UnityEngine.Random.Range(0, (int)chunk);
                    if (value30 < limit30)
                    {
                        return value30 % n;
                    }
                }
            }

            const long range60 = 1L << 60;
            if (n <= range60)
            {
                long limit60 = range60 - range60 % n;
                while (true)
                {
                    long high = UnityEngine.Random.Range(0, (int)chunk);
                    long low = UnityEngine.Random.Range(0, (int)chunk);
                    long value60 = (high << 30) | low;
                    if (value60 < limit60)
                    {
                        return value60 % n;
                    }
                }
            }

            // WHY: Above 2^60 a 60-bit draw cannot cover the range (the rejection limit would be 0 and the loop would
            // never end), so the top path draws 63 bits; the limit stays at least 2^62, so each try accepts at least half.
            const ulong range63 = 1UL << 63;
            ulong bound = (ulong)n;
            ulong limit63 = range63 - range63 % bound;
            while (true)
            {
                ulong high = (ulong)UnityEngine.Random.Range(0, (int)chunk);
                ulong middle = (ulong)UnityEngine.Random.Range(0, (int)chunk);
                ulong low = (ulong)UnityEngine.Random.Range(0, (int)chunk) >> 27;
                ulong value63 = (high << 33) | (middle << 3) | low;
                if (value63 < limit63)
                {
                    return (long)(value63 % bound);
                }
            }
        }

        private void EnsureCache()
        {
            entries ??= new List<Entry>();
            if (_cacheValid && _prefix != null && _prefix.Length == entries.Count)
            {
                return;
            }

            if (_prefix == null || _prefix.Length != entries.Count)
            {
                _prefix = new long[entries.Count];
            }

            long running = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                Entry entry = entries[i];
                running += entry != null ? entry.Weight : 0;
                _prefix[i] = running;
            }

            _total = running;
            _cacheValid = true;
        }

        private bool IsValidIndex(int index)
        {
            return index >= 0 && index < entries.Count;
        }

        /// <summary>One weighted outcome.</summary>
        [Serializable]
        public sealed class Entry
        {
            [SerializeField] [Min(0)] private int weight = 1;
            [SerializeField] private string label = "Chance";
            [SerializeField] private int id = -1;

            /// <summary>Creates an entry with weight 1 (used by serialization).</summary>
            public Entry()
            {
            }

            /// <summary>Creates an entry; negative weights are stored as zero.</summary>
            public Entry(int weight, string label = "Chance", int customId = -1)
            {
                this.weight = Math.Max(0, weight);
                this.label = label;
                id = customId;
            }

            /// <summary>Non-negative integer weight. Change it through <see cref="IntegerChanceTable.SetWeight" />.</summary>
            public int Weight => Math.Max(0, weight);

            /// <summary>Display label.</summary>
            public string Label
            {
                get => label;
                set => label = value;
            }

            /// <summary>Caller-defined id (for example a loot item id); -1 when unused.</summary>
            public int CustomId
            {
                get => id;
                set => id = value;
            }

            internal void SetWeight(int value)
            {
                weight = Math.Max(0, value);
            }
        }
    }
}
