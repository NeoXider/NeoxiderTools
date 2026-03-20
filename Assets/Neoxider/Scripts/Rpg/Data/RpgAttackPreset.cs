using UnityEngine;

namespace Neo.Rpg
{
    /// <summary>
    ///     ScriptableObject preset that combines an attack with targeting rules for AI, skills, and spells.
    /// </summary>
    [CreateAssetMenu(fileName = "Rpg Attack Preset", menuName = "Neoxider/RPG/Rpg Attack Preset")]
    public sealed class RpgAttackPreset : ScriptableObject
    {
        [SerializeField] private string _id = string.Empty;
        [SerializeField] private string _displayName = "Attack Preset";
        [SerializeField] private RpgAttackDefinition _attackDefinition;
        [SerializeField] private bool _requireTarget = true;
        [SerializeField] private bool _useSelectorComponentWhenAvailable = true;
        [SerializeField] private bool _aimAtTarget = true;
        [SerializeField] private RpgTargetQuery _targetQuery = new();

        /// <summary>
        ///     Gets the preset id.
        /// </summary>
        public string Id => string.IsNullOrWhiteSpace(_id) ? name : _id;

        /// <summary>
        ///     Gets the display name.
        /// </summary>
        public string DisplayName => _displayName;

        /// <summary>
        ///     Gets the attack definition used by the preset.
        /// </summary>
        public RpgAttackDefinition AttackDefinition => _attackDefinition;

        /// <summary>
        ///     Gets whether a valid target is required before casting.
        /// </summary>
        public bool RequireTarget => _requireTarget;

        /// <summary>
        ///     Gets whether an attached selector should be used first.
        /// </summary>
        public bool UseSelectorComponentWhenAvailable => _useSelectorComponentWhenAvailable;

        /// <summary>
        ///     Gets whether the attack should face the resolved target.
        /// </summary>
        public bool AimAtTarget => _aimAtTarget;

        /// <summary>
        ///     Gets the target query used when the preset resolves a target automatically.
        /// </summary>
        public RpgTargetQuery TargetQuery => _targetQuery;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(_id) && !string.IsNullOrWhiteSpace(name))
            {
                _id = name;
            }
        }
    }
}
