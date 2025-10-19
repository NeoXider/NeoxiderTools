using System;
using Neo.Runtime.Features.Money.Model;
using Neo.Runtime.Features.Wallet.Data;
using Neo.Runtime.Features.Wallet.Interfaces;
using VContainer;

namespace Neo.Runtime.Features.Wallet.Infrastructure
{
    /// <summary>
    /// Реализация фабрики: создает MoneyModel из WalletConfig.
    /// </summary>
    public class CurrencyFactory : ICurrencyFactory
    {
        private readonly WalletConfig _config;
        private readonly IObjectResolver _resolver; // на будущее (баффы/логгеры и т.п.)

        public CurrencyFactory(WalletConfig config, IObjectResolver resolver)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        }

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