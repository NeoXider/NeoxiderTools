namespace Neo.Cards
{
    /// <summary>
    ///     Тип раскладки карт в руке
    /// </summary>
    public enum HandLayoutType
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
        Grid
    }
}