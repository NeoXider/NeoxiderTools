using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neo.Rpg
{
    /// <summary>
    /// Runtime-targeted buff references for attacks.
    /// </summary>
    [Serializable]
    public sealed class RpgAttackEffectRefs
    {
        [SerializeField] private string[] _targetBuffIds = Array.Empty<string>();
        [SerializeField] private string[] _targetStatusIds = Array.Empty<string>();
        [SerializeField] private string[] _selfBuffIds = Array.Empty<string>();

        public IReadOnlyList<string> TargetBuffIds => _targetBuffIds;
        public IReadOnlyList<string> TargetStatusIds => _targetStatusIds;
        public IReadOnlyList<string> SelfBuffIds => _selfBuffIds;
    }
}
