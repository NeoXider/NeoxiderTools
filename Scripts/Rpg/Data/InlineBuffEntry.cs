using System;
using UnityEngine;

namespace Neo.Rpg
{
    /// <summary>
    ///     Inline buff entry — same shape as <see cref="BuffDefinition"/> but lives in the inspector of
    ///     <c>RpgCharacter</c> instead of a separate ScriptableObject asset. Use for one-off
    ///     scene effects ("pickup gives +20 max HP for 30s") where creating an SO is overkill.
    /// </summary>
    [Serializable]
    public sealed class InlineBuffEntry
    {
        [Tooltip("Unique id. If empty, displayName is used. NoCode triggers can apply by index OR by id.")]
        [SerializeField]
        private string _id = string.Empty;

        [SerializeField] private string _displayName = "Inline Buff";
        [SerializeField] [Min(0.01f)] private float _duration = 10f;
        [SerializeField] private bool _stackable;
        [SerializeField] [Min(1)] private int _maxStacks = 1;
        [SerializeField] private BuffStatModifier[] _modifiers = Array.Empty<BuffStatModifier>();

        public string Id => string.IsNullOrWhiteSpace(_id)
            ? string.IsNullOrWhiteSpace(_displayName) ? "InlineBuff" : _displayName
            : _id;

        public string DisplayName => _displayName;
        public float Duration => _duration;
        public bool Stackable => _stackable;
        public int MaxStacks => _maxStacks;
        public BuffStatModifier[] Modifiers => _modifiers;
    }
}
