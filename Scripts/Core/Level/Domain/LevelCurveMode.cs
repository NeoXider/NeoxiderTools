namespace Neo.Core.Level
{
    /// <summary>
    ///     Level curve mode in LevelCurveDefinition: formula, curve (graph), or manual table.
    /// </summary>
    public enum LevelCurveMode
    {
        /// <summary>Level from formula (Linear, Quadratic, Exponential, Polynomial, Power, etc.).</summary>
        Formula = 0,

        /// <summary>Level from AnimationCurve (X = level, Y = cumulative XP to that level).</summary>
        Curve = 1,

        /// <summary>Manual table: list of (level, required XP) pairs.</summary>
        Custom = 2
    }
}
