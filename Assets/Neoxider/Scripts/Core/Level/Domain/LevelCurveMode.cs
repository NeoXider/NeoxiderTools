namespace Neo.Core.Level
{
    /// <summary>
    ///     Режим кривой уровня в LevelCurveDefinition: формула, кривая (график) или ручная таблица.
    /// </summary>
    public enum LevelCurveMode
    {
        /// <summary>Уровень по формуле (Linear, Quadratic, Exponential, Polynomial, Power и т.д.).</summary>
        Formula = 0,

        /// <summary>Уровень по графику AnimationCurve (ось X = уровень, Y = кумулятивный XP до уровня).</summary>
        Curve = 1,

        /// <summary>Ручная таблица: список пар (уровень, требуемый XP).</summary>
        Custom = 2
    }
}
