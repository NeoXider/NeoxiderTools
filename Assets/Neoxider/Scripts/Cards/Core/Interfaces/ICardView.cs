using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Neo.Cards
{
    /// <summary>
    /// Интерфейс визуального представления карты
    /// </summary>
    public interface ICardView
    {
        /// <summary>
        /// Данные карты
        /// </summary>
        CardData Data { get; }

        /// <summary>
        /// Показана ли карта лицом вверх
        /// </summary>
        bool IsFaceUp { get; }

        /// <summary>
        /// Transform компонента
        /// </summary>
        Transform Transform { get; }

        /// <summary>
        /// Событие клика по карте
        /// </summary>
        event Action<ICardView> OnClicked;

        /// <summary>
        /// Событие наведения на карту
        /// </summary>
        event Action<ICardView> OnHovered;

        /// <summary>
        /// Событие ухода курсора с карты
        /// </summary>
        event Action<ICardView> OnUnhovered;

        /// <summary>
        /// Устанавливает данные карты и обновляет визуал
        /// </summary>
        /// <param name="data">Данные карты</param>
        /// <param name="faceUp">Показать лицом вверх</param>
        void SetData(CardData data, bool faceUp = true);

        /// <summary>
        /// Переворачивает карту
        /// </summary>
        void Flip();

        /// <summary>
        /// Переворачивает карту с анимацией
        /// </summary>
        /// <param name="duration">Длительность анимации в секундах</param>
        UniTask FlipAsync(float duration = 0.3f);

        /// <summary>
        /// Перемещает карту в указанную позицию с анимацией
        /// </summary>
        /// <param name="position">Целевая позиция</param>
        /// <param name="duration">Длительность анимации в секундах</param>
        UniTask MoveToAsync(Vector3 position, float duration = 0.2f);

        /// <summary>
        /// Устанавливает интерактивность карты
        /// </summary>
        /// <param name="interactable">Можно ли взаимодействовать с картой</param>
        void SetInteractable(bool interactable);
    }
}

