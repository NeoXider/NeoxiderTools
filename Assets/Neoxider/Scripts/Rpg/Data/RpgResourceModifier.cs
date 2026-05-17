using System;
using UnityEngine;

namespace Neo.Rpg
{
    /// <summary>
    ///     How a single resource pool is affected by an upgrade or equipment piece.
    /// </summary>
    public enum RpgResourceModifierKind
    {
        /// <summary>Add a flat number to the pool's Max (e.g. Vitality +1 → Max HP +15).</summary>
        AddMaxFlat = 0,

        /// <summary>Add a percentage to the pool's Max (1.0 = +1% of base max).</summary>
        AddMaxPercent = 1,

        /// <summary>Add a flat number to the regen value (e.g. Endurance +1 → Stamina regen +0.5/s).</summary>
        AddRegenFlat = 2
    }

    /// <summary>
    ///     A single resource-side modifier driven by upgrades or stat changes.
    ///     <para>Used inside <see cref="RpgStatUpgradeRule"/>: when the player invests in
    ///     <c>Vitality</c> we add <see cref="AddMaxFlat"/> +15 to the <c>Hp</c> resource.</para>
    /// </summary>
    [Serializable]
    public sealed class RpgResourceModifier
    {
        [Tooltip("Resource this modifier applies to (e.g. Hp / Mana / custom DarkMana).")]
        public RpgStatId resourceId;

        [Tooltip("How the value is interpreted.")]
        public RpgResourceModifierKind kind = RpgResourceModifierKind.AddMaxFlat;

        [Tooltip("Amount per upgrade point invested. Multiplied by the upgrade level at runtime.")]
        public float value = 1f;
    }
}
