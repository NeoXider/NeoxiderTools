using UnityEngine;

namespace Neo.Rpg
{
    /// <summary>
    /// Defines a temporary buff with duration and stat modifiers.
    /// </summary>
    [System.Serializable]
    public sealed class BuffStatModifier
    {
        [SerializeField] private BuffStatType _statType = BuffStatType.DamagePercent;
        [SerializeField] private float _value;

        public BuffStatType StatType => _statType;
        public float Value => _value;
    }
}
