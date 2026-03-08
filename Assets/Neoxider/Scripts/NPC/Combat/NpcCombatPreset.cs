using UnityEngine;

namespace Neo.NPC.Combat
{
    /// <summary>
    /// ScriptableObject preset for modular NPC combat behavior.
    /// </summary>
    [CreateAssetMenu(fileName = "Npc Combat Preset", menuName = "Neoxider/NPC/Npc Combat Preset")]
    public sealed class NpcCombatPreset : ScriptableObject
    {
        [SerializeField] private string _id = string.Empty;
        [SerializeField] private string _displayName = "NPC Combat Preset";
        [SerializeField] private Neo.Rpg.RpgAttackPreset _attackPreset;
        [SerializeField] [Min(0.1f)] private float _preferredAttackDistance = 2f;
        [SerializeField] [Min(0.1f)] private float _loseTargetDistance = 15f;
        [SerializeField] private bool _runWhileChasing = true;
        [SerializeField] private bool _stopMovementInsideAttackRange = true;
        [SerializeField] private bool _faceTargetBeforeAttack = true;
        [SerializeField] private bool _autoRestoreNavigationMode = true;

        public string Id => string.IsNullOrWhiteSpace(_id) ? name : _id;
        public string DisplayName => _displayName;
        public Neo.Rpg.RpgAttackPreset AttackPreset => _attackPreset;
        public float PreferredAttackDistance => Mathf.Max(0.1f, _preferredAttackDistance);
        public float LoseTargetDistance => Mathf.Max(0.1f, _loseTargetDistance);
        public bool RunWhileChasing => _runWhileChasing;
        public bool StopMovementInsideAttackRange => _stopMovementInsideAttackRange;
        public bool FaceTargetBeforeAttack => _faceTargetBeforeAttack;
        public bool AutoRestoreNavigationMode => _autoRestoreNavigationMode;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(_id) && !string.IsNullOrWhiteSpace(name))
            {
                _id = name;
            }
        }
    }
}
