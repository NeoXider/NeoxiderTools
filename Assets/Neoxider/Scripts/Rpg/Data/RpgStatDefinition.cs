using System;
using UnityEngine;

namespace Neo.Rpg
{
    /// <summary>
    ///     Inspector entry that describes one runtime stat (a single value with no Current/Max).
    ///     <para>Stats represent character attributes (Strength, Dexterity, Defense, FireResist, ...)
    ///     that influence derived calculations. They're modified by buffs, equipment, level growth
    ///     and Dark-Souls-style upgrade points — but they never deplete via gameplay actions
    ///     (use <see cref="RpgResourceDefinition"/> for that).</para>
    /// </summary>
    [Serializable]
    public sealed class RpgStatDefinition
    {
        [Tooltip("Stat identifier (preset or Custom string).")]
        public RpgStatId id = new(RpgStatPreset.Strength);

        [Tooltip("Optional human-readable label for UI. Falls back to id.Value when empty.")]
        public string displayName;

        [Header("Value")] [Tooltip("Base value before any modifiers (level growth, buffs, upgrades, equipment).")]
        public float baseValue = 10f;

        [Tooltip("Hard floor for the final value after all modifiers. -1 = unlimited.")]
        public float minValue = -1f;

        [Tooltip("Hard ceiling for the final value after all modifiers. -1 = unlimited.")]
        public float maxValue = -1f;

        [Header("Level Growth (optional)")]
        [Tooltip("When true, the level growth rule is added to baseValue at each level.")]
        public bool affectedByLevel;

        [Tooltip("Growth rule applied each level when affectedByLevel is on.")]
        public RpgStatGrowthRule growth = new() { BaseValue = 0f, AddPerLevel = 1f };

        public string DisplayLabel => string.IsNullOrWhiteSpace(displayName) ? id.Value : displayName;
    }
}
