using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neo.Bonus
{
    /// <summary>
    ///     Per-slot-machine symbol weight table layered over a shared <see cref="SlotEconomyDefinition"/>.
    ///     The definition's weights stay the default; when <see cref="Enabled"/> is on, the picker uses
    ///     the local weight stored here for each symbol id instead, without modifying the shared asset.
    ///     Entries are matched by symbol id, so reordering or extending the definition's symbol list is
    ///     safe: <see cref="SyncWith"/> keeps one editable entry per symbol, and any symbol without an
    ///     entry falls back to its definition weight.
    /// </summary>
    [Serializable]
    public sealed class SlotSymbolWeightOverrides
    {
        [Serializable]
        public sealed class Entry
        {
            [Tooltip("Display copy of the symbol name from the economy definition (read-only intent).")]
            public string SymbolName = "";

            [Tooltip("Symbol id this weight applies to; matched against SlotEconomyDefinition.Symbol.Id.")]
            public int SymbolId;

            [Tooltip("Local drop weight for this machine; 0 disables the symbol.")] [Min(0f)]
            public float Weight;
        }

        [Tooltip("When on, the picker uses these weights instead of the SlotEconomyDefinition weights.")]
        [SerializeField]
        private bool _enabled;

        [SerializeField] private List<Entry> _entries = new();

        /// <summary>When false the shared definition weights are used untouched.</summary>
        public bool Enabled
        {
            get => _enabled;
            set => _enabled = value;
        }

        /// <summary>One entry per known symbol id (after <see cref="SyncWith"/>).</summary>
        public IReadOnlyList<Entry> Entries => _entries;

        /// <summary>
        ///     Rebuilds the entry list against <paramref name="economy"/>: keeps existing weights matched
        ///     by symbol id, adds missing symbols with their definition weight, drops entries whose id no
        ///     longer exists, refreshes display names, and orders entries like the definition. Returns
        ///     true when anything changed (useful for editor dirty-marking).
        /// </summary>
        public bool SyncWith(SlotEconomyDefinition economy)
        {
            if (economy == null || economy.Symbols == null)
            {
                if (_entries.Count == 0)
                {
                    return false;
                }

                _entries.Clear();
                return true;
            }

            var rebuilt = new List<Entry>(economy.Symbols.Length);
            bool changed = false;

            foreach (SlotEconomyDefinition.Symbol symbol in economy.Symbols)
            {
                if (symbol == null)
                {
                    continue;
                }

                Entry existing = FindEntry(symbol.Id);
                if (existing == null)
                {
                    rebuilt.Add(new Entry
                    {
                        SymbolName = symbol.Name,
                        SymbolId = symbol.Id,
                        Weight = Mathf.Max(0f, symbol.Weight)
                    });
                    changed = true;
                    continue;
                }

                if (existing.SymbolName != symbol.Name)
                {
                    existing.SymbolName = symbol.Name;
                    changed = true;
                }

                if (existing.Weight < 0f)
                {
                    existing.Weight = 0f;
                    changed = true;
                }

                rebuilt.Add(existing);
            }

            if (rebuilt.Count != _entries.Count)
            {
                changed = true;
            }
            else
            {
                for (int i = 0; i < rebuilt.Count; i++)
                {
                    if (!ReferenceEquals(rebuilt[i], _entries[i]))
                    {
                        changed = true;
                        break;
                    }
                }
            }

            _entries = rebuilt;
            return changed;
        }

        /// <summary>Local weight for a symbol id; false when no entry exists.</summary>
        public bool TryGetWeight(int symbolId, out float weight)
        {
            Entry entry = FindEntry(symbolId);
            if (entry == null)
            {
                weight = 0f;
                return false;
            }

            weight = entry.Weight;
            return true;
        }

        /// <summary>Sets (or adds) the local weight for a symbol id; negative values clamp to 0.</summary>
        public void SetWeight(int symbolId, float weight)
        {
            weight = Mathf.Max(0f, weight);
            Entry entry = FindEntry(symbolId);
            if (entry != null)
            {
                entry.Weight = weight;
                return;
            }

            _entries.Add(new Entry { SymbolId = symbolId, Weight = weight });
        }

        /// <summary>
        ///     Effective weight for <paramref name="symbol"/>: the local entry when the override is
        ///     enabled and an entry exists, otherwise the definition weight. Weights below 0 count as 0.
        /// </summary>
        public float ResolveWeight(SlotEconomyDefinition.Symbol symbol)
        {
            if (symbol == null)
            {
                return 0f;
            }

            if (_enabled && TryGetWeight(symbol.Id, out float weight))
            {
                return Mathf.Max(0f, weight);
            }

            return Mathf.Max(0f, symbol.Weight);
        }

        /// <summary>
        ///     Normalizes all positive local weights so they sum to 1 (entries at 0 stay disabled).
        ///     Returns false when there is no positive weight to normalize.
        /// </summary>
        public bool NormalizeWeights()
        {
            float total = 0f;
            foreach (Entry entry in _entries)
            {
                if (entry != null && entry.Weight > 0f)
                {
                    total += entry.Weight;
                }
            }

            if (total <= 0f)
            {
                return false;
            }

            foreach (Entry entry in _entries)
            {
                if (entry == null)
                {
                    continue;
                }

                entry.Weight = entry.Weight > 0f ? entry.Weight / total : 0f;
            }

            return true;
        }

        /// <summary>
        ///     Weighted random symbol id from <paramref name="economy"/> honoring this override table
        ///     (definition weights when disabled). Same fallback rules as
        ///     <see cref="SlotEconomyDefinition.PickWeightedId()"/>.
        /// </summary>
        public int PickWeightedId(SlotEconomyDefinition economy)
        {
            if (economy == null)
            {
                return 0;
            }

            return economy.PickWeightedId(ResolveWeight);
        }

        /// <summary>Deterministic pick for tests/replays; <paramref name="normalizedRoll"/> in [0..1].</summary>
        public int PickWeightedId(SlotEconomyDefinition economy, float normalizedRoll)
        {
            if (economy == null)
            {
                return 0;
            }

            return economy.PickWeightedId(ResolveWeight, normalizedRoll);
        }

        private Entry FindEntry(int symbolId)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                Entry entry = _entries[i];
                if (entry != null && entry.SymbolId == symbolId)
                {
                    return entry;
                }
            }

            return null;
        }
    }
}
