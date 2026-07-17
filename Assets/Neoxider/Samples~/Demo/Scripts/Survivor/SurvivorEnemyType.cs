using System;
using Neo.Abilities;
using UnityEngine;

namespace Neo.Samples.Survivor
{
    /// <summary>One enemy archetype in the spawn table: a unit template plus survivor-specific tuning.</summary>
    [Serializable]
    public class SurvivorEnemyType
    {
        [Tooltip("Unit template (health pool, team, base properties).")]
        public UnitTemplate Template;

        [Tooltip("Body color of the spawned enemy.")]
        public Color Color = new Color(0.98f, 0.36f, 0.48f);

        [Tooltip("World radius of the enemy sprite / contact.")]
        public float Radius = 0.45f;

        [Tooltip("Touch damage per second dealt to the player on contact.")]
        public float ContactDps = 12f;

        [Tooltip("XP granted to the player when killed.")]
        public int XpReward = 1;

        [Tooltip("Relative spawn weight against the other enemy types.")]
        public float SpawnWeight = 1f;

        [Tooltip("Earliest survival time (seconds) at which this type starts appearing.")]
        public float UnlockTime;
    }
}
