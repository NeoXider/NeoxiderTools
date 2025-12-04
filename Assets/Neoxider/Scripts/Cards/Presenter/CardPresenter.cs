using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Neo.Cards
{
    /// <summary>
    /// Презентер карты - связывает данные карты с её визуальным представлением
    /// </summary>
    public class CardPresenter
    {
        private readonly ICardView _view;
        private readonly DeckConfig _config;
        private CardData _data;
        private bool _isFaceUp;

        /// <summary>
        /// Данные карты
        /// </summary>
        public CardData Data => _data;

        /// <summary>
        /// Визуальное представление карты
        /// </summary>
        public ICardView View => _view;

        /// <summary>
        /// Показана ли карта лицом вверх
        /// </summary>
        public bool IsFaceUp => _isFaceUp;

        /// <summary>
        /// Transform карты
        /// </summary>
        public Transform Transform => _view.Transform;

        /// <summary>
        /// Событие клика по карте
        /// </summary>
        public event Action<CardPresenter> OnClicked;

        /// <summary>
        /// Создаёт презентер карты
        /// </summary>
        /// <param name="view">Визуальное представление</param>
        /// <param name="config">Конфигурация колоды</param>
        public CardPresenter(ICardView view, DeckConfig config)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _config = config ?? throw new ArgumentNullException(nameof(config));

            _view.OnClicked += HandleClick;
        }

        /// <summary>
        /// Устанавливает данные карты
        /// </summary>
        /// <param name="data">Данные карты</param>
        /// <param name="faceUp">Показать лицом вверх</param>
        public void SetData(CardData data, bool faceUp = true)
        {
            _data = data;
            _isFaceUp = faceUp;
            _view.SetData(data, faceUp);
        }

        /// <summary>
        /// Переворачивает карту
        /// </summary>
        public void Flip()
        {
            _isFaceUp = !_isFaceUp;
            _view.Flip();
        }

        /// <summary>
        /// Переворачивает карту с анимацией
        /// </summary>
        /// <param name="duration">Длительность анимации</param>
        public async UniTask FlipAsync(float duration = 0.3f)
        {
            _isFaceUp = !_isFaceUp;
            await _view.FlipAsync(duration);
        }

        /// <summary>
        /// Перемещает карту в позицию
        /// </summary>
        /// <param name="position">Целевая позиция</param>
        /// <param name="duration">Длительность анимации</param>
        public async UniTask MoveToAsync(Vector3 position, float duration = 0.2f)
        {
            await _view.MoveToAsync(position, duration);
        }

        /// <summary>
        /// Устанавливает интерактивность карты
        /// </summary>
        /// <param name="interactable">Можно ли взаимодействовать</param>
        public void SetInteractable(bool interactable)
        {
            _view.SetInteractable(interactable);
        }

        /// <summary>
        /// Отписывается от событий
        /// </summary>
        public void Dispose()
        {
            _view.OnClicked -= HandleClick;
        }

        private void HandleClick(ICardView view)
        {
            OnClicked?.Invoke(this);
        }
    }
}

