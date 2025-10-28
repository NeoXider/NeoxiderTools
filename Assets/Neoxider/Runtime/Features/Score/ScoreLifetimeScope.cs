using Neo.Runtime.Features.Score.Presenter;
using Neo.Runtime.Features.Score.View;
using VContainer;
using VContainer.Unity;

namespace Neo.Runtime.Features.Score
{
    /// <summary>
    /// LifetimeScope for Score feature UI components.
    /// ScoreModel is registered in CoreLifetimeScope and resolved from parent scope.
    /// </summary>
    public class ScoreLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            // ScoreModel is resolved from parent CoreLifetimeScope
            builder.RegisterEntryPoint<ScorePresenter>(Lifetime.Singleton);

            builder.RegisterComponentInHierarchy<ScoreView>()
                .As<IScoreView>()
                .AsSelf();
        }
    }
}