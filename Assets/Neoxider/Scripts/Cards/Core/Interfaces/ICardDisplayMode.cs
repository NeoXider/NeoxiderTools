namespace Neo.Cards
{
    /// <summary>
    ///     Опциональный интерфейс: режим отображения карты (переворот / всегда открыта / всегда закрыта).
    ///     Если не реализован, считается <see cref="CardDisplayMode.WithFlip" />.
    /// </summary>
    public interface ICardDisplayMode
    {
        /// <summary>Режим отображения карты.</summary>
        CardDisplayMode Mode { get; }
    }
}