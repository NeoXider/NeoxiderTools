namespace Neo.Cards
{
    /// <summary>
    ///     Режим отображения карты: переворот по запросу или всегда открыта/закрыта.
    /// </summary>
    public enum CardDisplayMode
    {
        /// <summary>Переворот по запросу (Flip/FlipAsync).</summary>
        WithFlip,

        /// <summary>Всегда показывать лицом (рубашка не используется).</summary>
        AlwaysFaceUp,

        /// <summary>Всегда показывать рубашку.</summary>
        AlwaysFaceDown
    }
}