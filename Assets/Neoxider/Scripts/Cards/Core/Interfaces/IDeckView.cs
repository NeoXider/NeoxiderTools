using UnityEngine;

namespace Neo.Cards
{
    /// <summary>
    /// Интерфейс визуального представления колоды
    /// </summary>
    public interface IDeckView
    {
        /// <summary>
        /// Точка спавна карт
        /// </summary>
        Transform SpawnPoint { get; }

        /// <summary>
        /// Количество видимых карт в стопке
        /// </summary>
        int VisibleCardCount { get; set; }

        /// <summary>
        /// Обновляет визуальное отображение колоды
        /// </summary>
        /// <param name="remainingCount">Количество оставшихся карт</param>
        void UpdateVisual(int remainingCount);

        /// <summary>
        /// Показывает верхнюю карту колоды (козырь)
        /// </summary>
        /// <param name="card">Карта для отображения</param>
        void ShowTopCard(CardData card);

        /// <summary>
        /// Скрывает верхнюю карту
        /// </summary>
        void HideTopCard();
    }
}

