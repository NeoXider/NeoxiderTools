using System;
using UnityEngine;

namespace Neo.Rpg
{
    /// <summary>
    ///     Defines a temporary buff with duration and stat modifiers.
    ///     <para>For LEGACY <see cref="BuffStatType"/> values (DamagePercent / DefensePercent / …):
    ///     <c>_specificDamageType</c> is used for SpecificDefensePercent; <c>_targetId</c> is ignored.</para>
    ///     <para>For UNIVERSAL types (Add*/Regen*/IncomingDamagePercent/…): <c>_targetId</c> picks the stat
    ///     or resource id to modify; <c>_specificDamageType</c> picks the damage type for
    ///     <see cref="BuffStatType.DamageTypeResistPercent"/>.</para>
    /// </summary>
    [Serializable]
    public sealed class BuffStatModifier
    {
        [SerializeField] private BuffStatType _statType = BuffStatType.DamagePercent;
        [SerializeField] private string _specificDamageType;
        [SerializeField] private float _value;
        [SerializeField] private RpgStatId _targetId;

        public BuffStatType StatType => _statType;
        public string SpecificDamageType => _specificDamageType;
        public float Value => _value;

        /// <summary>Universal target id (stat or resource). Empty for legacy types.</summary>
        public RpgStatId TargetId => _targetId;

        /// <summary>Canonical target id string (delegates to <see cref="RpgStatId.Value"/>).</summary>
        public string TargetIdValue => _targetId.Value;
    }
}
