using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;

namespace Neo.Cards
{
    /// <summary>Owns the concrete tween awaited by one cancellable card operation.</summary>
    internal sealed class CardTweenScope : IDisposable
    {
        private readonly CancellationTokenSource _cancellation;
        private CancellationTokenRegistration _registration;
        private Tween _tween;

        internal CardTweenScope(CancellationToken lifetime, CancellationToken caller)
        {
            _cancellation = CancellationTokenSource.CreateLinkedTokenSource(lifetime, caller);
        }

        internal CancellationToken Token => _cancellation.Token;

        internal void Cancel()
        {
            _cancellation.Cancel();
        }

        internal async UniTask WaitAsync(Tween tween)
        {
            _registration.Dispose();
            tween.SetRecyclable(false);
            _tween = tween;
            _registration = Token.Register(() => tween.Kill());
            Token.ThrowIfCancellationRequested();
            await UniTask.WaitUntil(() => !tween.IsActive(), cancellationToken: Token);
            Token.ThrowIfCancellationRequested();
        }

        public void Dispose()
        {
            _registration.Dispose();
            _tween?.Kill();
            _cancellation.Dispose();
        }
    }
}
