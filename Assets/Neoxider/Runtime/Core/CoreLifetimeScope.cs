using VContainer.Unity;
using VContainer;
using Neo.Runtime.Features.Health.Model;

namespace Neo.Runtime.Core
{
    public class CoreLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<GameManager>(Lifetime.Singleton);
        }
    }
}