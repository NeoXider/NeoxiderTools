using Neo.Runtime.Features.Health.Presenter;
using VContainer.Unity;
using Neo.Runtime.Features.Health.View;
using VContainer;

namespace Neo.Runtime.Features.Health
{
    /// <summary>
    /// LifetimeScope for Health feature UI components.
    /// HealthModel is registered in CoreLifetimeScope and resolved from parent scope.
    /// </summary>
    public class HealthLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            // HealthModel is resolved from parent CoreLifetimeScope
            builder.RegisterEntryPoint<HealthPresenter>(Lifetime.Singleton);
            builder.RegisterComponentInHierarchy<HealthView>()
                .As<IHealthView>()
                .AsSelf();
        }
    }
}