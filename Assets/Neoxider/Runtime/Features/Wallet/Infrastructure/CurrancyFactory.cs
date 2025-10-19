using System;
using Neo.Runtime.Features.Money.Model;
using Neo.Runtime.Features.Wallet.Data;
using Neo.Runtime.Features.Wallet.Interfaces;
using VContainer;

namespace Neo.Runtime.Features.Wallet.Infrastructure
{
    /// <summary>
    /// Implementation of currency factory that creates MoneyModel instances from WalletConfig.
    /// </summary>
    public class CurrencyFactory : ICurrencyFactory
    {
        private readonly WalletConfig _config;
        private readonly IObjectResolver _resolver; // for future use (buffs/loggers, etc.)

        /// <summary>
        /// Initializes a new instance of the <see cref="CurrencyFactory"/> class.
        /// </summary>
        /// <param name="config">The wallet configuration.</param>
        /// <param name="resolver">The object resolver for dependency injection.</param>
        public CurrencyFactory(WalletConfig config, IObjectResolver resolver)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        }

        /// <summary>
        /// Creates a new money model with the specified currency identifier.
        /// </summary>
        /// <param name="currencyId">The unique identifier of the currency to create.</param>
        /// <returns>A new instance of MoneyModel for the specified currency.</returns>
        public MoneyModel Create(string currencyId)
        {
            if (!_config.TryGet(currencyId, out CurrencyDefinition def))
            {
                throw new ArgumentException($"Unknown currencyId: {currencyId}");
            }

            return new MoneyModel(def.StartAmount, def.MaxAmount);
        }
    }
}
