namespace Neo.Cards
{
    /// <summary>
    ///     How a card is shown: flip on demand, or always face up / face down.
    /// </summary>
    public enum CardDisplayMode
    {
        /// <summary>Flip on demand (Flip/FlipAsync).</summary>
        WithFlip,

        /// <summary>Always face up (back sprite unused).</summary>
        AlwaysFaceUp,

        /// <summary>Always face down (back sprite).</summary>
        AlwaysFaceDown
    }
}
