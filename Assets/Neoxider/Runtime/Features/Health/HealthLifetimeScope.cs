using Neo.Runtime.Features.Health.Model;
using Neo.Runtime.Features.Health.Presenter;
using VContainer.Unity;
using Neo.Runtime.Features.Health.View;
using VContainer;

namespace Neo.Runtime.Features.Health
{
    public class HealthLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<HealthModel>(Lifetime.Singleton);
            builder.RegisterEntryPoint<HealthPresenter>(Lifetime.Singleton);
            builder.RegisterComponentInHierarchy<HealthView>()
                .As<IHealthView>()
                .AsSelf();
        }
    }
}