using System;

namespace Neo.Cards
{
    /// <summary>
    ///     Общий тип раскладки карт для Hand/Deck/Board.
    /// </summary>
    public enum CardLayoutType
    {
        /// <summary>
        ///     Веер - карты расположены дугой
        /// </summary>
        Fan,

        /// <summary>
        ///     Линия - карты расположены в ряд с перекрытием
        /// </summary>
        Line,

        /// <summary>
        ///     Стопка - карты сложены друг на друга
        /// </summary>
        Stack,

        /// <summary>
        ///     Сетка - карты расположены в несколько рядов
        /// </summary>
        Grid,

        /// <summary>
        ///     Слоты - карты размещаются в фиксированные позиции.
        /// </summary>
        Slots,

        /// <summary>
        ///     Случайное расположение карт (для биты/хаоса на столе).
        /// </summary>
        Scattered
    }

    /// <summary>
    ///     Устаревшее имя типа раскладки. Оставлено для обратной совместимости.
    /// </summary>
    [Obsolete("Use CardLayoutType instead.")]
    public enum HandLayoutType
    {
        Fan = CardLayoutType.Fan,
        Line = CardLayoutType.Line,
        Stack = CardLayoutType.Stack,
        Grid = CardLayoutType.Grid
    }
}