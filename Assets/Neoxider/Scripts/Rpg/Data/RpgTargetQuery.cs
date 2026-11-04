using System;
using UnityEngine;

namespace Neo.Rpg
{
    /// <summary>
    ///     Query used to locate a target for AI, skills, and spell presets.
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

        /// <summary>Search radius around the origin (minimum 0.1).</summary>
        public float Range
        {
            get => Mathf.Max(0.1f, _range);
            set => _range = Mathf.Max(0.1f, value);
        }

        /// <summary>Layers searched for candidates.</summary>
        public LayerMask TargetLayers
        {
            get => _targetLayers;
            set => _targetLayers = value;
        }

        /// <summary>Whether 2D colliders are searched.</summary>
        public bool Use2D
        {
            get => _use2D;
            set => _use2D = value;
        }

        /// <summary>Whether 3D colliders are searched.</summary>
        public bool Use3D
        {
            get => _use3D;
            set => _use3D = value;
        }

        /// <summary>Whether the source's own receiver is excluded.</summary>
        public bool IgnoreSelf
        {
            get => _ignoreSelf;
            set => _ignoreSelf = value;
        }

        /// <summary>Whether dead receivers remain candidates.</summary>
        public bool IncludeDeadTargets
        {
            get => _includeDeadTargets;
            set => _includeDeadTargets = value;
        }

        /// <summary>Whether stunned/blocked receivers (CanPerformActions = false) are skipped.</summary>
        public bool RequireCanPerformActions
        {
            get => _requireCanPerformActions;
            set => _requireCanPerformActions = value;
        }

        /// <summary>How the best candidate is chosen.</summary>
        public RpgTargetSelectionMode SelectionMode
        {
            get => _selectionMode;
            set => _selectionMode = value;
        }
    }
}
