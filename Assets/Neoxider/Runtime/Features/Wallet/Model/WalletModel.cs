using System;
using System.Collections.Generic;
using Neo.Runtime.Features.Money.Model;
using Neo.Runtime.Features.Wallet.Interfaces;

namespace Neo.Runtime.Features.Wallet.Domain
{
    /// <summary>
    /// Хранит модели валют по CurrencyId, создаёт их через фабрику при первом обращении.
    /// </summary>
    public class WalletModel
    {
        private readonly ICurrencyFactory _factory;
        private readonly Dictionary<string, MoneyModel> _accounts = new();

        public WalletModel(ICurrencyFactory factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public bool Contains(string currencyId)
        {
            return _accounts.ContainsKey(currencyId);
        }

        public MoneyModel Get(string currencyId)
        {
            if (_accounts.TryGetValue(currencyId, out MoneyModel m))
            {
                return m;
            }

            m = _factory.Create(currencyId);
            _accounts[currencyId] = m;
            return m;
        }

        public IEnumerable<KeyValuePair<string, MoneyModel>> All()
        {
            return _accounts;
        }

        // Удобные шорткаты
        public void Add(string currencyId, float amount)
        {
            Get(currencyId).Add(amount);
        }

        public bool Spend(string currencyId, float amount)
        {
            return Get(currencyId).Spend(amount);
        }

        public void SetBalance(string currencyId, float value)
        {
            Get(currencyId).SetBalance(value);
        }

        public void SetMax(string currencyId, float max)
        {
            Get(currencyId).SetMax(max);
        }
    }
}