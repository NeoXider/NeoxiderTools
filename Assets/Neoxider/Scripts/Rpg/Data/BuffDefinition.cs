using System;
using UnityEngine;

namespace Neo.Rpg
{
    /// <summary>
    /// Defines a temporary buff with duration and stat modifiers.
    /// </summary>
    [Serializable]
    public sealed class BuffStatModifier
    {
        [SerializeField] private BuffStatType _statType = BuffStatType.DamagePercent;
        [SerializeField] private float _value;

        /// <summary>
        /// Gets the affected stat type.
        /// </summary>
        public BuffStatType StatType => _statType;

        /// <summary>
        /// Gets the modifier value (flat or percent depending on stat type).
        /// </summary>
        public float Value => _value;
    }

    /// <summary>
    /// Stat types that can be modified by buffs.
    /// </summary>
    public enum BuffStatType
    {
        DamagePercent,
        DefensePercent,
        HpRegenPerSecond,
        MovementSpeedPercent,
        Custom
    }

    /// <summary>
    /// ScriptableObject definition for a temporary buff.
    /// </summary>
    [CreateAssetMenu(fileName = "Buff Definition", menuName = "Neoxider/RPG/Buff Definition")]
    public sealed class BuffDefinition : ScriptableObject
    {
        [SerializeField] private string _id = string.Empty;
        [SerializeField] private string _displayName = "Buff";
        [SerializeField] [Min(0.01f)] private float _duration = 10f;
        [SerializeField] private bool _stackable;
        [SerializeField] [Min(1)] private int _maxStacks = 1;
        [SerializeField] private BuffStatModifier[] _modifiers = Array.Empty<BuffStatModifier>();

        /// <summary>
        /// Gets the unique identifier for this buff.
        /// </summary>
        public string Id => string.IsNullOrWhiteSpace(_id) ? name : _id;

        /// <summary>
        /// Gets the display name.
        /// </summary>
        public string DisplayName => _displayName;

        /// <summary>
        /// Gets the buff duration in seconds.
        /// </summary>
        public float Duration => _duration;

        /// <summary>
        /// Gets whether the buff can stack.
        /// </summary>
        public bool Stackable => _stackable;

        /// <summary>
        /// Gets the maximum number of stacks when stackable.
        /// </summary>
        public int MaxStacks => _maxStacks;

        /// <summary>
        /// Gets the stat modifiers applied by this buff.
        /// </summary>
        public BuffStatModifier[] Modifiers => _modifiers;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(_id) && !string.IsNullOrWhiteSpace(name))
            {
                _id = name;
            }
        }
    }
}
