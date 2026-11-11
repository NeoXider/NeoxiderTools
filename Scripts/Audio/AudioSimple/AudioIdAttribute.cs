using UnityEngine;

namespace Neo.Audio
{
    /// <summary>Which of <see cref="AM" />'s two lists an <see cref="AudioIdAttribute" /> field points at.</summary>
    public enum AudioIdKind
    {
        /// <summary>Ids of the Sounds list.</summary>
        Sound = 0,

        /// <summary>Ids of the Music list (the pools).</summary>
        Music = 1
    }

    /// <summary>
    ///     Turns a <c>string</c> field into a dropdown of the ids actually configured on the
    ///     <see cref="AM" /> in the scene, so a no-code setup cannot be broken by a typo. When no AM is
    ///     reachable - a prefab edited on its own, a scene without a manager - the field falls back to a
    ///     plain text box with a note, rather than blocking the edit.
    /// </summary>
    public sealed class AudioIdAttribute : PropertyAttribute
    {
        /// <summary>Offers ids from the Sounds list.</summary>
        public AudioIdAttribute()
        {
            Kind = AudioIdKind.Sound;
        }

        /// <summary>Offers ids from the chosen list.</summary>
        public AudioIdAttribute(AudioIdKind kind)
        {
            Kind = kind;
        }

        /// <summary>Which list to offer ids from.</summary>
        public AudioIdKind Kind { get; }
    }
}
