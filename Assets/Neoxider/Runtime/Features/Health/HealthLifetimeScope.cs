using VContainer.Unity;
using Neo.Runtime.Features.Health.View;
using Neoxider.Runtime.Features.Health.Model;
using Neoxider.Runtime.Features.Health.Presenter;
using VContainer;

namespace Neoxider.Runtime.Features.Health
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
