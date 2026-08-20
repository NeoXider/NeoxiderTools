using UnityEngine;

namespace Neo.Audio
{
    /// <summary>
    ///     Per-call overrides for one sound effect. The entry configured in the inspector supplies the
    ///     defaults; anything set here replaces them <b>for this play only</b> and never writes back.
    ///     <para>
    ///         The simple case stays a single line - <c>AM.I.Play("hit")</c>. Reach for this only when one
    ///         particular play has to differ, and skip the <c>SetVolume</c> / play / <c>SetVolume</c> dance
    ///         that leaks state whenever something throws in between.
    ///     </para>
    ///     <para>
    ///         <b>Volume still multiplies.</b> An override of <c>0.5</c> against an effects channel at
    ///         <c>0.8</c> is heard at <c>0.4</c>. It replaces the <i>entry</i> volume, not the channel - so
    ///         the player's own volume slider keeps working. The ceiling is
    ///         <see cref="AudioEntry.MaxVolume" />, not 1, for the same reason the entry's own volume goes
    ///         above 1: a quietly mastered clip has to be liftable.
    ///     </para>
    /// </summary>
    /// <example>
    ///     <code>
    /// AM.I.Play("hit");                                   // entry defaults
    /// AM.I.Play("hit", 0.4f);                             // just quieter
    /// AM.I.Play("step", SoundOptions.Clip(stepIndex));    // a specific clip of the set
    /// AM.I.Play("ui", SoundOptions.Volume(0.6f).WithoutPitch());
    /// AM.I.Play("charge", SoundOptions.Pitch(1f + stage * 0.1f));
    /// </code>
    /// </example>
    public struct SoundOptions
    {
        /// <summary>Entry-volume override (0..2). Null keeps the entry's own volume.</summary>
        public float? VolumeOverride;

        /// <summary>Forces pitch randomisation on or off. Null keeps the entry's setting.</summary>
        public bool? RandomizePitchOverride;

        /// <summary>Pitch range override. Null keeps the entry's range.</summary>
        public float? PitchMinOverride;

        /// <inheritdoc cref="PitchMinOverride" />
        public float? PitchMaxOverride;

        /// <summary>
        ///     Index of the clip to play inside the entry, instead of a random one. Useful for cues that
        ///     have to walk a set in order - a rising combo, a progress chime.
        /// </summary>
        public int? ClipIndexOverride;

        /// <summary>Play at this entry volume instead of the configured one.</summary>
        public static SoundOptions Volume(float volume)
        {
            return new SoundOptions { VolumeOverride = Mathf.Clamp(volume, 0f, AudioEntry.MaxVolume) };
        }

        /// <summary>Play this clip of the entry instead of a random one.</summary>
        public static SoundOptions Clip(int clipIndex)
        {
            return new SoundOptions { ClipIndexOverride = clipIndex };
        }

        /// <summary>Play at exactly this pitch, whatever the entry says.</summary>
        public static SoundOptions Pitch(float pitch)
        {
            return Pitch(pitch, pitch);
        }

        /// <summary>Randomise the pitch inside this range, whatever the entry says.</summary>
        public static SoundOptions Pitch(float min, float max)
        {
            return new SoundOptions
            {
                RandomizePitchOverride = true,
                PitchMinOverride = Mathf.Min(min, max),
                PitchMaxOverride = Mathf.Max(min, max)
            };
        }

        /// <summary>Play at the clip's own pitch, even if the entry randomises it.</summary>
        public static SoundOptions NoPitch => new SoundOptions { RandomizePitchOverride = false };

        /// <inheritdoc cref="Volume" />
        public SoundOptions WithVolume(float volume)
        {
            VolumeOverride = Mathf.Clamp(volume, 0f, AudioEntry.MaxVolume);
            return this;
        }

        /// <inheritdoc cref="Clip" />
        public SoundOptions WithClip(int clipIndex)
        {
            ClipIndexOverride = clipIndex;
            return this;
        }

        /// <inheritdoc cref="Pitch(float,float)" />
        public SoundOptions WithPitch(float min, float max)
        {
            RandomizePitchOverride = true;
            PitchMinOverride = Mathf.Min(min, max);
            PitchMaxOverride = Mathf.Max(min, max);
            return this;
        }

        /// <inheritdoc cref="NoPitch" />
        public SoundOptions WithoutPitch()
        {
            RandomizePitchOverride = false;
            return this;
        }
    }

    /// <summary>
    ///     Per-call overrides for one music change. Same rules as <see cref="SoundOptions" />: the pool
    ///     supplies the defaults, these replace them for this change only, and the volume override is a
    ///     multiplier of the music channel rather than an absolute level.
    /// </summary>
    /// <example>
    ///     <code>
    /// AM.I.PlayMusicPool("boss");                                    // configured crossfade
    /// AM.I.PlayMusicPool("boss", MusicTransition.Instant);           // hard cut
    /// AM.I.PlayMusicPool("boss", MusicOptions.Volume(0.5f).WithFade(2f));
    /// AM.I.PlayMusicPool("menu", MusicOptions.Track(0));             // a specific track of the pool
    /// </code>
    /// </example>
    public struct MusicOptions
    {
        /// <summary>Entry-volume override (0..2). Null keeps the pool's own volume.</summary>
        public float? VolumeOverride;

        /// <summary>Transition override. <c>default</c> means "use the pool / manager setting".</summary>
        public MusicTransition Transition;

        /// <summary>Index of the track to start inside the pool, instead of a random one.</summary>
        public int? TrackIndexOverride;

        /// <summary>Start the pool at this entry volume instead of the configured one.</summary>
        public static MusicOptions Volume(float volume)
        {
            return new MusicOptions { VolumeOverride = Mathf.Clamp(volume, 0f, AudioEntry.MaxVolume) };
        }

        /// <summary>Crossfade over this many seconds instead of the configured length.</summary>
        public static MusicOptions Fade(float seconds)
        {
            return new MusicOptions { Transition = MusicTransition.Fade(seconds) };
        }

        /// <summary>Cut straight in, with no fade.</summary>
        public static MusicOptions Instant => new MusicOptions { Transition = MusicTransition.Instant };

        /// <summary>Start this track of the pool instead of a random one.</summary>
        public static MusicOptions Track(int trackIndex)
        {
            return new MusicOptions { TrackIndexOverride = trackIndex };
        }

        /// <inheritdoc cref="Volume" />
        public MusicOptions WithVolume(float volume)
        {
            VolumeOverride = Mathf.Clamp(volume, 0f, AudioEntry.MaxVolume);
            return this;
        }

        /// <inheritdoc cref="Fade" />
        public MusicOptions WithFade(float seconds)
        {
            Transition = MusicTransition.Fade(seconds);
            return this;
        }

        /// <inheritdoc cref="Instant" />
        public MusicOptions WithInstant()
        {
            Transition = MusicTransition.Instant;
            return this;
        }

        /// <inheritdoc cref="Track" />
        public MusicOptions WithTrack(int trackIndex)
        {
            TrackIndexOverride = trackIndex;
            return this;
        }
    }
}
