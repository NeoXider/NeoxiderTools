using System;

namespace Neo.Cards
{
    /// <summary>
    ///     Layout type for hand, deck, or board stacks.
    /// </summary>
    public enum CardLayoutType
    {
        /// <summary>
        ///     Fan — cards along an arc.
        /// </summary>
        Fan,

        /// <summary>
        ///     Line — row with overlap.
        /// </summary>
        Line,

        /// <summary>
        ///     Stack — piled on top of each other.
        /// </summary>
        Stack,

        /// <summary>
        ///     Grid — multiple rows.
        /// </summary>
        Grid,

        /// <summary>
        ///     Slots — fixed slot positions.
        /// </summary>
        Slots,

        /// <summary>
        ///     Random scatter (beat pile / chaos on table).
        /// </summary>
        Scattered
    }

    /// <summary>
    ///     Legacy layout enum name. Kept for backward compatibility.
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
