using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neo.Runtime.Features.Wallet.Data
{
    [CreateAssetMenu(fileName = "WalletConfig", menuName = "Neo/Wallet Config")]
    public class WalletConfig : ScriptableObject
    {
        [SerializeField] private List<CurrencyDefinition> currencies = new();

        /// <summary>Все элементы как есть для инспектора/отладки.</summary>
        public IReadOnlyList<CurrencyDefinition> Currencies => currencies;

        [NonSerialized] private Dictionary<string, CurrencyDefinition> _map;

        /// <summary>Попытаться получить валюту по ID. Словарь строится при первом вызове.</summary>
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

#if UNITY_EDITOR
        private void OnValidate()
        {
            _map = null;
        }
#endif
    }
}