using System.Collections.Generic;
using Neo.Abilities;
using UnityEngine;

namespace Neo.Samples.Survivor
{
    /// <summary>
    ///     The whole survivor game as data. Swap this asset (player, enemies, abilities, upgrades,
    ///     tuning) and you have a different survivor game on the same code — that is the point of the
    ///     kit. <see cref="SurvivorGame" /> reads everything from here.
    /// </summary>
    [CreateAssetMenu(menuName = "Neoxider/Survivor Demo/Config", fileName = "SurvivorConfig")]
    public sealed class SurvivorConfig : ScriptableObject
    {
        [Header("Content")]
        [Tooltip("Ability/modifier catalog registered into the ability system.")]
        public AbilityLibrary Library;

        [Tooltip("Player unit template (health, move_speed, mana).")]
        public UnitTemplate PlayerTemplate;

        [Tooltip("Player body color.")]
        public Color PlayerColor = new Color(0.31f, 0.78f, 1f);

        [Tooltip("Ability ids the player starts with (auto-cast).")]
        public List<string> StartingAbilities = new List<string>();

        [Tooltip("Enemy archetypes and their spawn tuning.")]
        public List<SurvivorEnemyType> Enemies = new List<SurvivorEnemyType>();

        [Tooltip("Upgrades offered on level-up (3 random each level).")]
        public List<SurvivorUpgrade> Upgrades = new List<SurvivorUpgrade>();

        [Header("Arena")]
        [Tooltip("Half-size of the square play area in world units.")]
        public float ArenaExtent = 9f;

        [Tooltip("Orthographic camera size.")]
        public float CameraSize = 7f;

        [Header("Spawning")]
        [Tooltip("Seconds between spawns at the start.")]
        public float StartSpawnInterval = 1.1f;

        [Tooltip("Minimum seconds between spawns at peak difficulty.")]
        public float MinSpawnInterval = 0.25f;

        [Tooltip("Seconds of survival to reach the minimum spawn interval.")]
        public float RampDuration = 180f;

        [Tooltip("Extra enemy health multiplier added per minute survived.")]
        public float HealthRampPerMinute = 0.5f;

        [Header("Progression")]
        [Tooltip("XP needed for the first level-up.")]
        public int BaseXp = 5;

        [Tooltip("Extra XP required per level (linear ramp).")]
        public int XpPerLevel = 4;

        [Tooltip("World radius within which XP orbs are magnetized to the player.")]
        public float PickupRadius = 2.2f;

        [Header("Player")]
        [Tooltip("Player collision radius (contact / pickups).")]
        public float PlayerRadius = 0.45f;

        public float SpawnIntervalAt(float survivedSeconds)
        {
            float t = RampDuration > 0f ? Mathf.Clamp01(survivedSeconds / RampDuration) : 1f;
            return Mathf.Lerp(StartSpawnInterval, MinSpawnInterval, t);
        }

        public float EnemyHealthMultiplier(float survivedSeconds)
        {
            return 1f + HealthRampPerMinute * (survivedSeconds / 60f);
        }

        public int XpForLevel(int level)
        {
            return BaseXp + XpPerLevel * Mathf.Max(0, level - 1);
        }
    }
}
