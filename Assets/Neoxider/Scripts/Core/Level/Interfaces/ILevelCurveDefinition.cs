namespace Neo.Core.Level
{
    /// <summary>
    ///     Контракт для определения кривой уровня (формула, кривая, custom).
    ///     Позволяет LevelModel не зависеть от UnityEngine и получать уровень/XP через интерфейс.
    /// </summary>
    public interface ILevelCurveDefinition
    {
        /// <summary>Вычисляет уровень по накопленному XP.</summary>
        /// <param name="totalXp">Накопленный опыт</param>
        /// <param name="maxLevel">Макс. уровень (0 = без ограничения)</param>
        int EvaluateLevel(int totalXp, int maxLevel = 0);

        /// <summary>Возвращает XP до следующего уровня (0 если на макс. уровне).</summary>
        int GetXpToNextLevel(int totalXp, int maxLevel = 0);
    }
}
