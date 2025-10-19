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

        /// <summary>
        /// Initializes a new instance of the <see cref="WalletModel"/> class.
        /// </summary>
        /// <param name="factory">The currency factory used to create new money models.</param>
        public WalletModel(ICurrencyFactory factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        /// <summary>
        /// Determines whether the wallet contains a specific currency.
        /// </summary>
        /// <param name="currencyId">The unique identifier of the currency to check.</param>
        /// <returns>True if the currency exists in the wallet; otherwise, false.</returns>
        public bool Contains(string currencyId)
        {
            return _accounts.ContainsKey(currencyId);
        }

        /// <summary>
        /// Gets a money model for the specified currency. Creates it if it doesn't exist yet.
        /// </summary>
        /// <param name="currencyId">The unique identifier of the currency.</param>
        /// <returns>The money model for the specified currency.</returns>
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

        /// <summary>
        /// Returns all currency models in the wallet.
        /// </summary>
        /// <returns>An enumerable collection of key-value pairs where keys are currency IDs and values are money models.</returns>
        public IEnumerable<KeyValuePair<string, MoneyModel>> All()
        {
            return _accounts;
        }

        // Удобные шорткаты
        /// <summary>
        /// Adds funds to the specified currency.
        /// </summary>
        /// <param name="currencyId">The unique identifier of the currency.</param>
        /// <param name="amount">The amount to add.</param>
        public void Add(string currencyId, float amount)
        {
            Get(currencyId).Add(amount);
        }

        /// <summary>
        /// Spends funds from the specified currency.
        /// </summary>
        /// <param name="currencyId">The unique identifier of the currency.</param>
        /// <param name="amount">The amount to spend.</param>
        /// <returns>True if the transaction was successful; otherwise, false.</returns>
        public bool Spend(string currencyId, float amount)
        {
            return Get(currencyId).Spend(amount);
        }

        /// <summary>
        /// Sets the balance of the specified currency directly.
        /// </summary>
        /// <param name="currencyId">The unique identifier of the currency.</param>
        /// <param name="value">The new balance value.</param>
        public void SetBalance(string currencyId, float value)
        {
            Get(currencyId).SetBalance(value);
        }

        /// <summary>
        /// Sets the maximum limit for the specified currency.
        /// </summary>
        /// <param name="currencyId">The unique identifier of the currency.</param>
        /// <param name="max">The new maximum limit.</param>
        public void SetMax(string currencyId, float max)
        {
            Get(currencyId).SetMax(max);
        }
    }
}
