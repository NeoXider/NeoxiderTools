using System;
using Neo.Runtime.Features.Health.Model;
using Neo.Runtime.Features.Money.Model;
using Neo.Runtime.Features.Score.Model;
using Neo.Runtime.Features.Wallet.Domain;
using VContainer;
using VContainer.Unity;

namespace Neo.Runtime.Core
{
    /// <summary>
    /// Central game manager that provides access to all game systems through dependency injection.
    /// </summary>
    public class GameManager
    {
        private static IObjectResolver _resolver;

        public static IObjectResolver Resolver
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

        /// <summary>
        /// Invalidates the cached scope resolver, forcing a fresh lookup on next access.
        /// Use this when changing scenes or containers to avoid stale references.
        /// </summary>
        public static void InvalidateScopeCache()
        {
            _resolver = null;
        }

        //  =====================================================================

        /// <summary>
        /// Gets the health model instance.
        /// </summary>
        public static HealthModel Health => Resolver.Resolve<HealthModel>();

        /// <summary>
        /// Gets the score model instance.
        /// </summary>
        public static ScoreModel Score => Resolver.Resolve<ScoreModel>();

        /// <summary>
        /// Gets the wallet model instance.
        /// </summary>
        public static WalletModel Wallet => Resolver.Resolve<WalletModel>();

        /// <summary>
        /// Gets the main money model (shortcut for Wallet.Get("money")).
        /// </summary>
        public static MoneyModel Money => Wallet.Get("money");
        
        /// <summary>
        /// Gets the gems for money model (shortcut for Wallet.Get("gems")).
        /// </summary>
        public static MoneyModel Gems => Wallet.Get("gems");
    }
}
