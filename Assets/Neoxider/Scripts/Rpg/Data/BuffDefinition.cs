using System;
using UnityEngine;

namespace Neo.Rpg
{
    /// <summary>
    ///     ScriptableObject definition for a temporary buff.
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

        public string Id => string.IsNullOrWhiteSpace(_id) ? name : _id;
        public string DisplayName => _displayName;
        public float Duration => _duration;
        public bool Stackable => _stackable;
        public int MaxStacks => _maxStacks;
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
