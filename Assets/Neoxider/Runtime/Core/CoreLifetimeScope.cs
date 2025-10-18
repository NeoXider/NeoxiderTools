using VContainer.Unity;
using VContainer;
using Neoxider.Runtime.Features.Health.Model;

namespace Neoxider.Runtime.Core
{
    public class CoreLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<GameManager>(Lifetime.Singleton);
        }
    }
}
