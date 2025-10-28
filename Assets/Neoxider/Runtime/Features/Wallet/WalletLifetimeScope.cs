using Neo.Runtime.Features.Wallet.Presentation;
using Neo.Runtime.Features.Wallet.Presenter;
using VContainer;
using VContainer.Unity;

namespace Neo.Runtime.Features.Wallet
{
    /// <summary>
    /// LifetimeScope for Wallet feature UI components.
    /// WalletModel, WalletConfig, and ICurrencyFactory are registered in CoreLifetimeScope and resolved from parent scope.
    /// </summary>
    public class WalletLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            // WalletModel, WalletConfig, ICurrencyFactory are resolved from parent CoreLifetimeScope
            builder.RegisterComponentInHierarchy<MoneyViewWithId>();
            builder.RegisterEntryPoint<WalletPresenter>(Lifetime.Singleton);
        }
    }
}