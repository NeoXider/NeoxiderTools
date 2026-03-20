using System.Threading;
using Cysharp.Threading.Tasks;

namespace Neo.Cards
{
    /// <summary>
    ///     Опциональный интерфейс: воспроизведение готовых анимаций карты (разовые и зацикленные).
    /// </summary>
    public interface ICardViewAnimations
    {
        /// <summary>
        ///     Воспроизводит разовую анимацию.
        /// </summary>
        /// <param name="type">Тип анимации</param>
        /// <param name="duration">Длительность в секундах (null = из конфига/по умолчанию)</param>
        /// <param name="cancellation">Отмена</param>
        UniTask PlayOneShotAsync(CardViewAnimationType type, float? duration = null,
            CancellationToken cancellation = default);

        /// <summary>
        ///     Запускает зацикленную анимацию.
        /// </summary>
        /// <param name="type">Тип анимации (PulseLooped, Idle и т.д.)</param>
        /// <param name="duration">Длительность одного цикла (null = из конфига)</param>
        void PlayLooped(CardViewAnimationType type, float? duration = null);

        /// <summary>
        ///     Останавливает зацикленную анимацию указанного типа.
        /// </summary>
        void StopLooped(CardViewAnimationType type);

        /// <summary>
        ///     Останавливает все зацикленные анимации.
        /// </summary>
        void StopAllLooped();
    }
}
