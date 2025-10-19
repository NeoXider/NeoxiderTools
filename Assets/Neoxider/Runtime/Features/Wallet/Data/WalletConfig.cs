using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neo.Runtime.Features.Wallet.Data
{
    /// <summary>
    /// Configuration asset for wallet currencies.
    /// </summary>
    [CreateAssetMenu(fileName = "WalletConfig", menuName = "Neo/Wallet Config")]
    public class WalletConfig : ScriptableObject
    {
        /// <summary>
        /// All currency items as-is for inspector/debugging purposes.
        /// </summary>
        [SerializeField] private List<CurrencyDefinition> currencies = new();

        /// <summary>
        /// Gets all currency definitions as read-only list.
        /// </summary>
        public IReadOnlyList<CurrencyDefinition> Currencies => currencies;

        [NonSerialized] private Dictionary<string, CurrencyDefinition> _map;

        /// <summary>
        /// Attempts to get a currency definition by its ID.
        /// The dictionary is built on first call.
        /// </summary>
        /// <param name="currencyId">The unique identifier of the currency.</param>
        /// <param name="def">When this method returns, contains the currency definition if found; otherwise, null.</param>
        /// <returns>True if the currency was found; otherwise, false.</returns>
        public bool TryGet(string currencyId, out CurrencyDefinition def)
        {
            if (string.IsNullOrEmpty(currencyId))
            {
                def = null;
                return false;
            }

            if (_map == null)
            {
                BuildMap();
            }

            return _map.TryGetValue(currencyId, out def);
        }


        private void BuildMap()
        {
            _map = new Dictionary<string, CurrencyDefinition>(StringComparer.Ordinal);

            foreach (CurrencyDefinition item in currencies)
            {
                if (item == null)
                {
                    continue;
                }

                string id = item.CurrencyId?.Trim();
                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }

                if (!_map.ContainsKey(id))
                {
                    _map.Add(id, item);
                }
            }
        }

        private void OnValidate()
        {
            _map = null;
        }
    }
}
