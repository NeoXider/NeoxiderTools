using Neo.Runtime.Features.Wallet.Data;
using Neo.Runtime.Features.Wallet.Infrastructure;
using Neo.Runtime.Features.Wallet.Interfaces;
using VContainer;
using VContainer.Unity;

namespace Neoxider.Runtime.Features.Wallet
{
    public class WalletLifetimeScope : LifetimeScope
    {
        [UnityEngine.SerializeField] private WalletConfig walletConfig;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterInstance(walletConfig);

            // Реализация фабрики теперь из Infrastructure
            builder.Register<ICurrencyFactory, CurrencyFactory>(Lifetime.Singleton);

            // builder.Register<WalletModel>(Lifetime.Singleton);
            //
            // builder.RegisterComponentInHierarchy<MoneyViewWithId>();
            // builder.RegisterEntryPoint<WalletPresenter>(Lifetime.Singleton);
        }
    }
}