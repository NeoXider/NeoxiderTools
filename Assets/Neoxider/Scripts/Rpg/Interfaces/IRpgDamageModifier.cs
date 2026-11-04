using Neo.Rpg.Components;

namespace Neo.Rpg
{
    /// <summary>
    ///     Code-side hook into the incoming damage pipeline of an <see cref="RpgCharacter" />: shields, armor
    ///     formulas, damage caps, thorns bookkeeping, difficulty scaling and similar rules that buff percentages
    ///     cannot express.
    /// </summary>
    /// <remarks>
    ///     Register with <see cref="RpgCharacter.AddDamageModifier" />. Modifiers run in registration order after
    ///     invulnerability and buff/status resist multipliers, and before HP is reduced. Each one receives the amount
    ///     produced by the previous modifier; negative results clamp to 0 and non-finite results are ignored.
    ///     Modifiers run on the simulating peer only (the server when the character is networked).
    /// </remarks>
    public interface IRpgDamageModifier
    {
        /// <summary>Returns the damage amount to continue with.</summary>
        /// <param name="target">Character receiving the damage.</param>
        /// <param name="info">Original hit: raw amount, damage type and optional source.</param>
        /// <param name="amount">Amount after multipliers and earlier modifiers.</param>
        float ModifyIncomingDamage(RpgCharacter target, RpgDamageInfo info, float amount);
    }
}
