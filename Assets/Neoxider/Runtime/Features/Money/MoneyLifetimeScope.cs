using Neo.Runtime.Features.Money.Model;
using Neo.Runtime.Features.Money.Presenter;
using Neo.Runtime.Features.Money.View;
using VContainer;
using VContainer.Unity;

namespace Neo.Runtime.Features.Money
{
    public class MoneyLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<MoneyModel>(Lifetime.Singleton);
            builder.RegisterEntryPoint<MoneyPresenter>(Lifetime.Singleton);

            builder.RegisterComponentInHierarchy<MoneyView>()
                .As<IMoneyView>()
                .AsSelf();
        }
    }
}