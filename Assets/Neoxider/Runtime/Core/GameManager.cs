using System;
using Neo.Runtime.Features.Health.Model;
using Neo.Runtime.Features.Money.Model;
using Neo.Runtime.Features.Score.Model;
using Neo.Runtime.Features.Wallet.Domain;
using VContainer;
using VContainer.Unity;

namespace Neo.Runtime.Core
{
    public class GameManager
    {
        private static IObjectResolver _resolver;

        private static IObjectResolver Resolver
        {
            get
            {
                if (_resolver != null)
                {
                    return _resolver;
                }

                LifetimeScope core = LifetimeScope.Find<CoreLifetimeScope>();
                if (core == null)
                {
                    throw new InvalidOperationException("CoreLifetimeScope не найден в сцене.");
                }

                if (core.Container == null)
                {
                    throw new InvalidOperationException("DI-контейнер CoreLifetimeScope ещё не инициализирован.");
                }

                _resolver = core.Container; // кэшируем только Scope/Resolver
                return _resolver;
            }
        }

        // Если меняешь сцены/контейнер, можно руками сбросить кэш
        public static void InvalidateScopeCache()
        {
            _resolver = null;
        }

        //  =====================================================================

        public static HealthModel Health => Resolver.Resolve<HealthModel>();

        public static ScoreModel Score => Resolver.Resolve<ScoreModel>();

        public static WalletModel Wallet => Resolver.Resolve<WalletModel>();

        public static MoneyModel Money => Wallet.Get("money");
    }
}