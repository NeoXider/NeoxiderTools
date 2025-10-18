using System;
using Neoxider.Runtime.Features.Health.Model;
using VContainer;
using VContainer.Unity;

namespace Neoxider.Runtime.Core
{
    public class GameManager
    {
        private static IObjectResolver _resolver;

        private static IObjectResolver Resolver
        {
            get
            {
                if (_resolver != null) return _resolver;

                var core = LifetimeScope.Find<CoreLifetimeScope>();
                if (core == null)
                    throw new InvalidOperationException("CoreLifetimeScope не найден в сцене.");
                if (core.Container == null)
                    throw new InvalidOperationException("DI-контейнер CoreLifetimeScope ещё не инициализирован.");

                _resolver = core.Container; // кэшируем только Scope/Resolver
                return _resolver;
            }
        }

        public static HealthModel Health => Resolver.Resolve<HealthModel>();

        // Если меняешь сцены/контейнер, можно руками сбросить кэш
        public static void InvalidateScopeCache() => _resolver = null;
    }
}