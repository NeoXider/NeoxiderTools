using System;
using UnityEngine;

namespace Neo.Rpg
{
    /// <summary>
    /// ScriptableObject definition for a status effect (damage over time, slow, stun, etc.).
    /// </summary>
    [CreateAssetMenu(fileName = "Status Effect Definition", menuName = "Neoxider/RPG/Status Effect Definition")]
    public sealed class StatusEffectDefinition : ScriptableObject
    {
        [SerializeField] private string _id = string.Empty;
        [SerializeField] private string _displayName = "Status Effect";
        [SerializeField] [Min(0.01f)] private float _duration = 5f;
        [SerializeField] [Min(0f)] private float _tickDamagePerSecond;
        [SerializeField] [Range(0f, 2f)] private float _movementSpeedMultiplier = 1f;
        [SerializeField] private bool _stackable;
        [SerializeField] [Min(1)] private int _maxStacks = 1;
        [SerializeField] private float _tickInterval = 1f;
        [SerializeField] private bool _blocksActions;

        /// <summary>
        /// Gets the unique identifier for this status effect.
        /// </summary>
        public string Id => string.IsNullOrWhiteSpace(_id) ? name : _id;

        /// <summary>
        /// Gets the display name.
        /// </summary>
        public string DisplayName => _displayName;

        /// <summary>
        /// Gets the effect duration in seconds.
        /// </summary>
        public float Duration => _duration;

        /// <summary>
        /// Gets the damage dealt per second.
        /// </summary>
        public float TickDamagePerSecond => _tickDamagePerSecond;

        /// <summary>
        /// Gets the movement speed multiplier (1 = normal, 0 = immobilized).
        /// </summary>
        public float MovementSpeedMultiplier => _movementSpeedMultiplier;

        /// <summary>
        /// Gets whether the effect can stack.
        /// </summary>
        public bool Stackable => _stackable;

        /// <summary>
        /// Gets the maximum number of stacks when stackable.
        /// </summary>
        public int MaxStacks => _maxStacks;

        /// <summary>
        /// Gets the interval between damage ticks in seconds.
        /// </summary>
        public float TickInterval => _tickInterval <= 0 ? 1f : _tickInterval;

        /// <summary>
        /// Gets whether the status blocks the target from performing actions.
        /// </summary>
        public bool BlocksActions => _blocksActions;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(_id) && !string.IsNullOrWhiteSpace(name))
            {
                _id = name;
            }
        }
    }
}
