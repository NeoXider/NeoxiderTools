using System;
using UnityEngine;

namespace Neo.Rpg
{
    /// <summary>
    ///     Defines a temporary buff with duration and stat modifiers.
    /// </summary>
    [Serializable]
    public sealed class BuffStatModifier
    {
        [SerializeField] private BuffStatType _statType = BuffStatType.DamagePercent;
        [SerializeField] private string _specificDamageType;
        [SerializeField] private float _value;

        public BuffStatType StatType => _statType;
        public string SpecificDamageType => _specificDamageType;
        public float Value => _value;
    }
}
