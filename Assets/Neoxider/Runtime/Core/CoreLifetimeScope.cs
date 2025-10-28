using VContainer.Unity;
using VContainer;
using Neo.Runtime.Features.Health.Model;
using Neo.Runtime.Features.Score.Model;
using Neo.Runtime.Features.Wallet.Data;
using Neo.Runtime.Features.Wallet.Domain;
using Neo.Runtime.Features.Wallet.Infrastructure;
using Neo.Runtime.Features.Wallet.Interfaces;
using Neo.Runtime.Logging;
using UnityEngine;

namespace Neo.Runtime.Core
{
    public class CoreLifetimeScope : LifetimeScope
    {
        [Header("Configuration")]
        [SerializeField] private WalletConfig walletConfig;
        [SerializeField] private LoggingConfig loggingConfig;

        [Header("Default Model Values")]
        [SerializeField] private int defaultMaxHealth = 100;
        [SerializeField] private int defaultMaxScore = 1000;

        protected override void Configure(IContainerBuilder builder)
        {
            RegisterLogging(builder);
            RegisterManagers(builder);
            RegisterConfiguration(builder);
            RegisterFactories(builder);
            RegisterModels(builder);
        }

        private void RegisterLogging(IContainerBuilder builder)
        {
            var logger = LoggerFactory.CreateLogger(loggingConfig);
            builder.RegisterInstance(logger);
        }

        private void RegisterManagers(IContainerBuilder builder)
        {
            builder.Register<GameManager>(Lifetime.Singleton);
        }

        private void RegisterConfiguration(IContainerBuilder builder)
        {
            builder.RegisterInstance(walletConfig);
        }

        private void RegisterFactories(IContainerBuilder builder)
        {
            builder.Register<ICurrencyFactory, CurrencyFactory>(Lifetime.Singleton);
        }

        private void RegisterModels(IContainerBuilder builder)
        {
            builder.Register<HealthModel>(Lifetime.Singleton)
                .WithParameter(defaultMaxHealth)
                .WithParameter(defaultMaxHealth);

            builder.Register<ScoreModel>(Lifetime.Singleton)
                .WithParameter(defaultMaxScore)
                .WithParameter(0);

            builder.Register<WalletModel>(Lifetime.Singleton);
        }
    }
}