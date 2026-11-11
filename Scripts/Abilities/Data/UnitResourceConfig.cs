using System;
using UnityEngine;

namespace Neo.Abilities
{
    /// <summary>
    ///     One resource pool of a unit template (health, mana, custom).
    /// </summary>
    [Serializable]
    public struct UnitResourceConfig
    {
        [Tooltip("Resource pool id (health, mana, or custom).")]
        public string ResourceId;

        [Tooltip("Maximum value; the pool starts full.")]
        public float Max;

        [Tooltip("Built-in pool regeneration per second (property-driven regen adds on top).")]
        public float RegenPerSecond;
    }
}
