using System;
using UnityEngine;

namespace Neo.Rpg
{
    /// <summary>
    ///     Universal id of an RPG resource or stat.
    ///     <para>Pick a known <see cref="RpgStatPreset"/> from the dropdown OR set
    ///     <see cref="preset"/> to <see cref="RpgStatPreset.Custom"/> and write any
    ///     project-specific string into <see cref="customId"/> (e.g. <c>"DarkMana"</c>,
    ///     <c>"BloodMana"</c>, <c>"AbyssalEnergy"</c>).</para>
    ///     The runtime id (used as the dictionary key everywhere) comes from
    ///     <see cref="Value"/>, so dropdown choice and free text are interchangeable.
    /// </summary>
    [Serializable]
    public struct RpgStatId : IEquatable<RpgStatId>
    {
        [Tooltip("Pick a common preset or choose Custom to type any id.")]
        public RpgStatPreset preset;

        [Tooltip("Used only when Preset = Custom. Free-form id string (e.g. \"DarkMana\").")]
        public string customId;

        /// <summary>Constructs a preset id (no custom string).</summary>
        public RpgStatId(RpgStatPreset preset)
        {
            this.preset = preset;
            customId = string.Empty;
        }

        /// <summary>Constructs a custom id (preset is set to <see cref="RpgStatPreset.Custom"/>).</summary>
        public RpgStatId(string customId)
        {
            preset = RpgStatPreset.Custom;
            this.customId = customId ?? string.Empty;
        }

        /// <summary>Canonical id string used at runtime / network / save.</summary>
        public string Value =>
            preset == RpgStatPreset.Custom
                ? (string.IsNullOrWhiteSpace(customId) ? string.Empty : customId.Trim())
                : PresetToId(preset);

        public bool IsValid => !string.IsNullOrEmpty(Value);

        public override string ToString() => Value;

        public bool Equals(RpgStatId other) =>
            string.Equals(Value, other.Value, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is RpgStatId other && Equals(other);

        public override int GetHashCode() => Value?.GetHashCode() ?? 0;

        public static bool operator ==(RpgStatId a, RpgStatId b) => a.Equals(b);
        public static bool operator !=(RpgStatId a, RpgStatId b) => !a.Equals(b);

        /// <summary>Implicit conversion from preset for convenient construction.</summary>
        public static implicit operator RpgStatId(RpgStatPreset preset) => new(preset);

        /// <summary>Implicit conversion to the runtime id string.</summary>
        public static implicit operator string(RpgStatId id) => id.Value;

        private static string PresetToId(RpgStatPreset value)
        {
            return value switch
            {
                RpgStatPreset.Hp => Neo.Core.Resources.RpgResourceId.Hp,
                RpgStatPreset.Mana => Neo.Core.Resources.RpgResourceId.Mana,
                RpgStatPreset.Stamina => Neo.Core.Resources.RpgResourceId.Stamina,
                RpgStatPreset.Shield => Neo.Core.Resources.RpgResourceId.Shield,
                _ => value.ToString()
            };
        }
    }
}
