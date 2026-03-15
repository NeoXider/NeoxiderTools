using System;
using UnityEngine;

namespace Neo.Rpg
{
    /// <summary>
    /// Query used to locate a target for AI, skills, and spell presets.
    /// </summary>
    [Serializable]
    public sealed class RpgTargetQuery
    {
        [SerializeField] [Min(0.1f)] private float _range = 10f;
        [SerializeField] private LayerMask _targetLayers = -1;
        [SerializeField] private bool _use2D = true;
        [SerializeField] private bool _use3D = true;
        [SerializeField] private bool _ignoreSelf = true;
        [SerializeField] private bool _includeDeadTargets;
        [SerializeField] private bool _requireCanPerformActions;
        [SerializeField] private RpgTargetSelectionMode _selectionMode = RpgTargetSelectionMode.Nearest;

        public float Range => Mathf.Max(0.1f, _range);
        public LayerMask TargetLayers => _targetLayers;
        public bool Use2D => _use2D;
        public bool Use3D => _use3D;
        public bool IgnoreSelf => _ignoreSelf;
        public bool IncludeDeadTargets => _includeDeadTargets;
        public bool RequireCanPerformActions => _requireCanPerformActions;
        public RpgTargetSelectionMode SelectionMode => _selectionMode;
    }
}
