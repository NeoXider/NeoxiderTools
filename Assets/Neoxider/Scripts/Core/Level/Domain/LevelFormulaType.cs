namespace Neo.Core.Level
{
    /// <summary>
    ///     Тип формулы для расчёта требуемого XP по уровню (режим Formula).
    ///     RequiredXp(level) — кумулятивный XP для достижения уровня.
    /// </summary>
    public enum LevelFormulaType
    {
        /// <summary>RequiredXp(level) = (level - 1) * xpPerLevel. Линейный рост.</summary>
        Linear = 0,

        /// <summary>RequiredXp(level) = base * level^2. Квадратичный рост.</summary>
        Quadratic = 1,

        /// <summary>RequiredXp(level) = base * factor^level. Экспоненциальный рост.</summary>
        Exponential = 2,

        /// <summary>RequiredXp(level) = base * level^exponent. Степенной рост (настраиваемая степень).</summary>
        Power = 3,

        /// <summary>RequiredXp(level) = constant + (level - 1) * xpPerLevel. Линейный со сдвигом.</summary>
        LinearWithOffset = 4,

        /// <summary>RequiredXp(level) = base * (level^exponent). То же что Power, явное имя.</summary>
        PolynomialSingle = 5
    }
}
