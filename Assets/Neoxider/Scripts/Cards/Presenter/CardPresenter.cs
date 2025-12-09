using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Neo.Cards
{
    /// <summary>
    ///     Презентер карты - связывает данные карты с её визуальным представлением
    /// </summary>
    public class CardPresenter
    {
        private readonly DeckConfig _config;

        /// <summary>
        ///     Создаёт презентер карты
        /// </summary>
        /// <param name="view">Визуальное представление</param>
        /// <param name="config">Конфигурация колоды</param>
        public CardPresenter(ICardView view, DeckConfig config)
        {
            View = view ?? throw new ArgumentNullException(nameof(view));
            _config = config ?? throw new ArgumentNullException(nameof(config));

            View.OnClicked += HandleClick;
        }

        /// <summary>
        ///     Данные карты
        /// </summary>
        public CardData Data { get; private set; }

        /// <summary>
        ///     Визуальное представление карты
        /// </summary>
        public ICardView View { get; }

        /// <summary>
        ///     Показана ли карта лицом вверх
        /// </summary>
        public bool IsFaceUp { get; private set; }

        /// <summary>
        ///     Transform карты
        /// </summary>
        public Transform Transform => View.Transform;

        /// <summary>
        ///     Событие клика по карте
        /// </summary>
        public event Action<CardPresenter> OnClicked;

        /// <summary>
        ///     Устанавливает данные карты
        /// </summary>
        /// <param name="data">Данные карты</param>
        /// <param name="faceUp">Показать лицом вверх</param>
        public void SetData(CardData data, bool faceUp = true)
        {
            Data = data;
            IsFaceUp = faceUp;
            View.SetData(data, faceUp);
        }

        /// <summary>
        ///     Переворачивает карту
        /// </summary>
        public void Flip()
        {
            IsFaceUp = !IsFaceUp;
            View.Flip();
        }

        /// <summary>
        ///     Переворачивает карту с анимацией
        /// </summary>
        /// <param name="duration">Длительность анимации</param>
        public async UniTask FlipAsync(float duration = 0.3f)
        {
            IsFaceUp = !IsFaceUp;
            await View.FlipAsync(duration);
        }

        /// <summary>
        ///     Перемещает карту в позицию
        /// </summary>
        /// <param name="position">Целевая позиция</param>
        /// <param name="duration">Длительность анимации</param>
        public async UniTask MoveToAsync(Vector3 position, float duration = 0.2f)
        {
            await View.MoveToAsync(position, duration);
        }

        /// <summary>
        ///     Устанавливает интерактивность карты
        /// </summary>
        /// <param name="interactable">Можно ли взаимодействовать</param>
        public void SetInteractable(bool interactable)
        {
            View.SetInteractable(interactable);
        }

        /// <summary>
        ///     Отписывается от событий
        /// </summary>
        public void Dispose()
        {
            View.OnClicked -= HandleClick;
        }

        private void HandleClick(ICardView view)
        {
            OnClicked?.Invoke(this);
        }
    }
}