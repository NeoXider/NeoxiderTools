using System;
using UnityEngine;

namespace Neo.Rpg
{
    /// <summary>
    ///     Inspector entry that describes one runtime resource pool on a <see cref="Components.RpgCharacter"/>
    ///     or a <see cref="RpgCharacterTemplate"/>: id, start values, regen, limits.
    ///     <para>Resources have <c>Current</c> / <c>Max</c> / regen, in contrast to <see cref="RpgStatDefinition"/>
    ///     which has only a single value. HP / Mana / Stamina / Shield / Rage / custom — all are resources.</para>
    /// </summary>
    [Serializable]
    public sealed class RpgResourceDefinition
    {
        [Tooltip("Resource identifier (preset or Custom string).")]
        public RpgStatId id = new(RpgStatPreset.Hp);

        [Tooltip("Optional human-readable label for UI. Falls back to id.Value when empty.")]
        public string displayName;

        [Header("Start Values")] [Tooltip("Starting current value when the character is initialized.")] [Min(0f)]
        public float startCurrent = 100f;

        [Tooltip("Starting maximum value.")] [Min(0f)]
        public float startMax = 100f;

        [Tooltip("Reset current to startCurrent (or to max when restoreToFull is true) every Awake.")]
        public bool restoreOnAwake = true;

        [Tooltip("If true and restoreOnAwake is on, current is set to Max instead of startCurrent.")]
        public bool restoreToFull = true;

        [Header("Regeneration")] public RpgRegenDefinition regen = new();

        [Header("Limits")] [Tooltip("If true, current can dip below 0 (for buffer mechanics).")]
        public bool canGoBelowZero;

        [Tooltip("If true, current can exceed Max (for over-shield mechanics).")]
        public bool canOverfill;

        [Tooltip("Hard floor. -1 = no extra floor (uses 0 unless canGoBelowZero is on).")]
        public float minValue = 0f;

        [Tooltip("Hard ceiling beyond which Max cannot grow (via buffs/upgrades). -1 = unlimited.")]
        public float maxCap = -1f;

        public string DisplayLabel => string.IsNullOrWhiteSpace(displayName) ? id.Value : displayName;
    }
}
