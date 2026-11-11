namespace Neo.Cards
{
    /// <summary>
    ///     Optional: fixed display mode (flip / always up / always down).
    ///     If not implemented, <see cref="CardDisplayMode.WithFlip" /> is assumed.
    /// </summary>
    public interface ICardDisplayMode
    {
        /// <summary>Current display mode.</summary>
        CardDisplayMode Mode { get; }
    }
}
