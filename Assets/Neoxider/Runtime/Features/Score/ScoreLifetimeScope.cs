using Neo.Runtime.Features.Score.Model;
using Neo.Runtime.Features.Score.Presenter;
using Neo.Runtime.Features.Score.View;
using VContainer;
using VContainer.Unity;

namespace Neo.Runtime.Features.Score
{
    public class ScoreLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<ScoreModel>(Lifetime.Singleton);
            builder.RegisterEntryPoint<ScorePresenter>(Lifetime.Singleton);

            builder.RegisterComponentInHierarchy<ScoreView>()
                .As<IScoreView>()
                .AsSelf();
        }
    }
}