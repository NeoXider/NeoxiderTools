using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Neo.Cards
{
    /// <summary>
    /// Интерфейс визуального представления руки (набора карт игрока)
    /// </summary>
    public interface IHandView
    {
        /// <summary>
        /// Список визуальных представлений карт в руке
        /// </summary>
        IReadOnlyList<ICardView> CardViews { get; }

        /// <summary>
        /// Количество карт в руке
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Добавляет карту в руку
        /// </summary>
        /// <param name="cardView">Визуальное представление карты</param>
        /// <param name="animate">Анимировать добавление</param>
        UniTask AddCardAsync(ICardView cardView, bool animate = true);

        /// <summary>
        /// Удаляет карту из руки
        /// </summary>
        /// <param name="cardView">Визуальное представление карты</param>
        /// <param name="animate">Анимировать удаление</param>
        UniTask RemoveCardAsync(ICardView cardView, bool animate = true);

        /// <summary>
        /// Переставляет карты в руке
        /// </summary>
        /// <param name="animate">Анимировать перестановку</param>
        UniTask ArrangeCardsAsync(bool animate = true);

        /// <summary>
        /// Очищает руку от всех карт
        /// </summary>
        void Clear();
    }
}

