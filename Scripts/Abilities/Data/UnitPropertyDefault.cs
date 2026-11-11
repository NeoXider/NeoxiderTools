using System;
using UnityEngine;

namespace Neo.Abilities
{
    /// <summary>
    ///     One base property value of a unit template (before modifiers).
    /// </summary>
    [Serializable]
    public struct UnitPropertyDefault
    {
        [Tooltip("Property id (see AbilityProperties for built-ins).")]
        public string PropertyId;

        [Tooltip("Base value before modifiers.")]
        public float Value;
    }
}
