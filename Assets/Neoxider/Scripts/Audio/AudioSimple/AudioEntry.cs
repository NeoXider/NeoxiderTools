using System;
using UnityEngine;

namespace Neo.Audio
{
    /// <summary>
    ///     One addressable cue in <see cref="AM" /> - the shape shared by sound effects and music.
    ///     <para>
    ///         An entry is a <b>set</b> of clips, not a single one. Every play picks a random clip from the
    ///         set (never the same one twice in a row while more than one is available), which is the cheap
    ///         way to stop a repeated cue - a blade hit, a footstep - from sounding like one sample on a
    ///         loop. Random pitch is the second, orthogonal way; both can be on at once.
    ///     </para>
    ///     <para>
    ///         <see cref="Id" /> is optional. An entry can always be addressed by its index in the list;
    ///         giving it an id additionally allows <c>AM.I.Play("hit")</c>, which survives reordering.
    ///     </para>
    ///     <para>
    ///         <see cref="Volume" /> is a multiplier, not an absolute level: the audible volume is
    ///         <c>channel volume x entry volume</c>. A music channel at <c>0.3</c> with an entry at
    ///         <c>1</c> plays at <c>0.3</c>.
    ///     </para>
    /// </summary>
    [Serializable]
    public abstract class AudioEntry
    {
        /// <summary>
        ///     Upper bound of every entry- and clip-level volume. Deliberately above 1: these are
        ///     MULTIPLIERS of the channel, so capping them at 1 would only ever let a clip be quieter than
        ///     the channel. A sample that was mastered too quietly could never be brought up to the others
        ///     without re-exporting the file. Two is enough headroom to rescue one, and low enough that a
        ///     stack of entries cannot run away into clipping.
        /// </summary>
        public const float MaxVolume = 2f;

        [Tooltip("Optional name. Leave it empty to address this entry by index only; fill it in to also " +
                 "play it as AM.I.Play(\"id\"), which survives reordering the list.")]
        [SerializeField]
        private string _id = string.Empty;

        [Tooltip("Clips of this entry. Two or more -> one is picked at random on every play, avoiding an " +
                 "immediate repeat. Put every variation of the same cue in here.")]
        [SerializeField]
        private AudioClip[] _clips = Array.Empty<AudioClip>();

        [Tooltip("Volume of this entry as a multiplier of the channel volume. 1 = as loud as the channel " +
                 "allows. Final volume = channel x entry x the picked clip's own trim.")]
        [Range(0f, MaxVolume)]
        [SerializeField]
        private float _volume = 1f;

        // WHY a parallel array rather than a Clip+volume struct: the clip list shipped as a plain
        // AudioClip[] and is referenced by AM, by tests and by user code. A parallel trim array adds the
        // per-clip level without a serialized-data break - an entry saved before trims existed simply has a
        // shorter (or absent) array, and every missing index reads as 1. The two cannot meaningfully
        // desync either, because ClipVolume() treats any out-of-range index as "no trim".
        [Tooltip("Per-clip volume trim, aligned with Clips by index. Recorded takes are rarely matched in " +
                 "level; this pulls the loud one down without re-exporting the file.")]
        [SerializeField]
        private float[] _clipVolumes = Array.Empty<float>();

        [Tooltip("Detune every play of this entry slightly, so repeats stop sounding identical.")]
        [SerializeField]
        private bool _randomizePitch;

        [Tooltip("Lowest pitch multiplier. 1 = the clip's own pitch.")]
        [Range(0.1f, 3f)]
        [SerializeField]
        private float _pitchMin = 0.94f;

        [Tooltip("Highest pitch multiplier.")]
        [Range(0.1f, 3f)]
        [SerializeField]
        private float _pitchMax = 1.06f;

        // WHY a flag and not "empty means quiet": an entry that lost its clip by accident is a bug worth a
        // warning, while an entry created ahead of its sound (the designer has not delivered it yet) is not.
        // Only the author knows which one it is, so the author says so. False is the serialized default,
        // which keeps every entry saved before the flag existed exactly as loud about being empty as before.
        [Tooltip("Placeholder: the entry is intentionally empty for now. While it has no clip, playing it does " +
                 "nothing and logs nothing. Once clips are added it plays normally.")]
        [SerializeField]
        private bool _placeholder;

        [NonSerialized] private int _lastClipIndex = -1;

        /// <summary>Optional id. Empty means "index only".</summary>
        public string Id
        {
            get => _id;
            set => _id = value ?? string.Empty;
        }

        /// <summary>Clips this entry chooses from.</summary>
        public AudioClip[] Clips
        {
            get => _clips ??= Array.Empty<AudioClip>();
            set => _clips = value ?? Array.Empty<AudioClip>();
        }

        /// <summary>Per-entry volume multiplier, combined with the channel volume.</summary>
        public float Volume
        {
            get => Mathf.Clamp(_volume, 0f, MaxVolume);
            set => _volume = Mathf.Clamp(value, 0f, MaxVolume);
        }

        /// <summary>Whether every play of this entry is detuned.</summary>
        public bool RandomizePitch
        {
            get => _randomizePitch;
            set => _randomizePitch = value;
        }

        /// <summary>
        ///     Marks the entry as intentionally empty for now. While it has no usable clip every play is a
        ///     silent no-op (no warning); once clips are assigned it plays normally.
        /// </summary>
        public bool IsPlaceholder
        {
            get => _placeholder;
            set => _placeholder = value;
        }

        /// <summary>Lowest pitch multiplier used when <see cref="RandomizePitch" /> is on.</summary>
        public float PitchMin => Mathf.Min(_pitchMin, _pitchMax);

        /// <summary>Highest pitch multiplier used when <see cref="RandomizePitch" /> is on.</summary>
        public float PitchMax => Mathf.Max(_pitchMin, _pitchMax);

        /// <summary>Number of clips in this entry.</summary>
        public int ClipCount => Clips.Length;

        /// <summary>
        ///     Volume trim of the clip the last <see cref="NextClip" /> / <see cref="ClipAt" /> call
        ///     returned. <c>1</c> when nothing has been picked yet or the clip carries no trim.
        /// </summary>
        public float LastClipVolume => ClipVolume(_lastClipIndex);

        /// <summary>
        ///     Volume trim of one clip of the set, as a multiplier. Any index without a stored trim - which
        ///     includes every clip of an entry authored before trims existed - reads as <c>1</c>.
        /// </summary>
        public float ClipVolume(int index)
        {
            if (_clipVolumes == null || index < 0 || index >= _clipVolumes.Length)
            {
                return 1f;
            }

            return Mathf.Clamp(_clipVolumes[index], 0f, MaxVolume);
        }

        /// <summary>Sets one clip's volume trim, growing the trim array as needed.</summary>
        public void SetClipVolume(int index, float volume)
        {
            if (index < 0)
            {
                return;
            }

            if (_clipVolumes == null || _clipVolumes.Length <= index)
            {
                float[] grown = new float[index + 1];
                // WHY fill with 1: a freshly grown slot means "no trim", and zero would silence clips that
                // the user never touched.
                for (int i = 0; i < grown.Length; i++)
                {
                    grown[i] = i < (_clipVolumes?.Length ?? 0) ? _clipVolumes[i] : 1f;
                }

                _clipVolumes = grown;
            }

            _clipVolumes[index] = Mathf.Clamp(volume, 0f, MaxVolume);
        }

        /// <summary>True when this entry cannot produce a sound (no non-null clip).</summary>
        public bool IsEmpty
        {
            get
            {
                AudioClip[] clips = Clips;
                for (int index = 0; index < clips.Length; index++)
                {
                    if (clips[index] != null)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>Sets the pitch range. The order of the arguments is normalised for you.</summary>
        public void SetPitchRange(float min, float max)
        {
            _pitchMin = Mathf.Clamp(Mathf.Min(min, max), 0.1f, 3f);
            _pitchMax = Mathf.Clamp(Mathf.Max(min, max), 0.1f, 3f);
        }

        /// <summary>
        ///     The pitch this play should use: a value inside the range when
        ///     <see cref="RandomizePitch" /> is on, otherwise exactly <c>1</c>.
        /// </summary>
        public float NextPitch()
        {
            return _randomizePitch ? UnityEngine.Random.Range(PitchMin, PitchMax) : 1f;
        }

        /// <summary>
        ///     Picks a clip at random, skipping the clip returned by the previous call while the entry
        ///     holds more than one usable clip. Returns null only when the entry has no usable clip.
        /// </summary>
        public AudioClip NextClip()
        {
            AudioClip[] clips = Clips;
            if (clips.Length == 0)
            {
                return null;
            }

            if (clips.Length == 1)
            {
                // WHY record the index even here: LastClipVolume reads it, and a single-clip entry must
                // still report that clip's trim rather than a default 1.
                _lastClipIndex = 0;
                return clips[0];
            }

            // WHY: two passes. The first prefers a clip that is neither null nor the previous one; if the
            // entry has exactly one usable clip that happens to be the previous one, the second pass
            // returns it rather than going silent.
            for (int attempt = 0; attempt < 12; attempt++)
            {
                int index = UnityEngine.Random.Range(0, clips.Length);
                if (index == _lastClipIndex || clips[index] == null)
                {
                    continue;
                }

                _lastClipIndex = index;
                return clips[index];
            }

            for (int index = 0; index < clips.Length; index++)
            {
                if (index == _lastClipIndex || clips[index] == null)
                {
                    continue;
                }

                _lastClipIndex = index;
                return clips[index];
            }

            // Sparse lists can exhaust the random attempts. Repeat only when no alternative exists.
            return _lastClipIndex >= 0 && _lastClipIndex < clips.Length ? clips[_lastClipIndex] : null;
        }

        /// <summary>
        ///     Returns a specific clip of the entry, for callers that need a deterministic order rather
        ///     than a random pick. Out-of-range indices wrap, so walking a counter upwards is safe.
        ///     Returns null only when the entry has no clips at all.
        /// </summary>
        public AudioClip ClipAt(int index)
        {
            AudioClip[] clips = Clips;
            if (clips.Length == 0)
            {
                return null;
            }

            int wrapped = ((index % clips.Length) + clips.Length) % clips.Length;
            _lastClipIndex = wrapped;
            return clips[wrapped];
        }

        /// <summary>Forgets which clip played last, so the next pick is unconstrained.</summary>
        public void ResetClipHistory()
        {
            _lastClipIndex = -1;
        }
    }

    /// <summary>
    ///     The volume channel a sound-effect entry plays on. Each channel has its own volume, mute and
    ///     optional <see cref="UnityEngine.Audio.AudioMixerGroup" />, so a settings menu can offer separate
    ///     "Effects" and "Interface" sliders. See <see cref="AM.SetChannelVolume" />.
    /// </summary>
    public enum SoundChannel
    {
        /// <summary>
        ///     Sound effects - the channel every entry used before channels existed. Its volume, mute and
        ///     mixer group ARE the Efx <see cref="AudioSource" /> (<see cref="AM.Efx" />), so
        ///     <see cref="AM.SetEfxVolume" /> and <c>AMSettings</c> keep driving it. Zero on purpose: it is the
        ///     value every entry saved before channels existed deserializes to.
        /// </summary>
        Sfx = 0,

        /// <summary>
        ///     Interface sounds (clicks, hovers, notifications). A separate 2D source with its own volume,
        ///     mute and optional mixer group, created on first use.
        /// </summary>
        Ui = 1
    }

    /// <summary>
    ///     A sound-effect entry. Random pitch defaults to <b>on</b>: effects are the cues that repeat,
    ///     and a repeated sample is exactly what the detune exists to hide.
    ///     <para>
    ///         <see cref="Channel" /> picks the volume channel (<see cref="SoundChannel.Sfx" /> by default);
    ///         <see cref="SpatialBlend" /> is used only by the positional <c>AM.PlayAt</c> calls.
    ///     </para>
    /// </summary>
    [Serializable]
    public sealed class SoundEntry : AudioEntry
    {
        [Tooltip("Volume channel. Sfx = the Efx AudioSource (what every entry used before channels existed); " +
                 "Ui = the interface channel with its own volume and mixer group.")]
        [SerializeField]
        private SoundChannel _channel = SoundChannel.Sfx;

        [Tooltip("Spatial blend used when this entry is played at a world position (AM.PlayAt): 0 = 2D, " +
                 "1 = fully 3D. Ordinary Play calls are not affected.")]
        [Range(0f, 1f)]
        [SerializeField]
        private float _spatialBlend = 1f;

        public SoundEntry()
        {
            RandomizePitch = true;
        }

        public SoundEntry(string id, params AudioClip[] clips) : this()
        {
            Id = id;
            Clips = clips;
        }

        /// <summary>Volume channel this entry plays on.</summary>
        public SoundChannel Channel
        {
            get => _channel;
            set => _channel = value;
        }

        /// <summary>Spatial blend (0 = 2D, 1 = 3D) used by <c>AM.PlayAt</c>; clamped to 0..1.</summary>
        public float SpatialBlend
        {
            get => Mathf.Clamp01(_spatialBlend);
            set => _spatialBlend = Mathf.Clamp01(value);
        }
    }

    /// <summary>How a music pool behaves once its current track reaches the end.</summary>
    public enum MusicPoolMode
    {
        /// <summary>
        ///     The chosen track loops and never changes on its own. The game decides when to move on, by
        ///     calling <c>AM.I.NextMusicTrack()</c> or switching to another pool. This is the default: a
        ///     track change usually belongs to a game beat - entering a boss fight, leaving a screen -
        ///     and not to wherever the audio file happened to end.
        /// </summary>
        Loop = 0,

        /// <summary>
        ///     When the track ends, the pool crossfades into another random track from the same entry,
        ///     never repeating the one that just played. This is what the legacy
        ///     <c>EnableRandomMusic()</c> did, and it stays available - it is just no longer the only
        ///     option.
        /// </summary>
        Shuffle = 1
    }

    /// <summary>
    ///     A music entry - which is also a <b>pool</b>. Several clips in one entry are the pool's tracks;
    ///     one is picked at random when the pool starts. What happens next is
    ///     <see cref="Mode" />: <see cref="MusicPoolMode.Loop" /> keeps that track going until the game
    ///     says otherwise, <see cref="MusicPoolMode.Shuffle" /> rolls on to another track by itself.
    ///     <para>
    ///         A "menu / gameplay / boss" set-up is therefore three entries with three ids, configured in
    ///         the inspector, instead of an <c>AudioClip[]</c> that game code swaps from outside.
    ///     </para>
    ///     <para>Random pitch defaults to <b>off</b>: a detuned music bed reads as a fault, not as variety.</para>
    /// </summary>
    [Serializable]
    public sealed class MusicEntry : AudioEntry
    {
        [Tooltip("Loop: the chosen track repeats until the game calls NextMusicTrack() or switches pool. " +
                 "Shuffle: when the track ends, another random track of this pool crossfades in.")]
        [SerializeField]
        private MusicPoolMode _mode = MusicPoolMode.Loop;

        [Tooltip("Crossfade length used when switching TO this entry, in seconds. Negative = use the " +
                 "Music Fade Duration configured on AM.")]
        [Range(-1f, 10f)]
        [SerializeField]
        private float _fadeDuration = -1f;

        public MusicEntry()
        {
            RandomizePitch = false;
        }

        public MusicEntry(string id, params AudioClip[] clips) : this()
        {
            Id = id;
            Clips = clips;
        }

        /// <summary>Whether the pool holds its track or rolls on to the next one by itself.</summary>
        public MusicPoolMode Mode
        {
            get => _mode;
            set => _mode = value;
        }

        /// <summary>True while the current track should repeat instead of advancing at its end.</summary>
        public bool IsLooping => _mode == MusicPoolMode.Loop;

        /// <summary>Per-entry crossfade length; negative means "use the AM default".</summary>
        public float FadeDuration
        {
            get => _fadeDuration;
            set => _fadeDuration = value;
        }

        /// <summary>True when this entry overrides the manager's crossfade length.</summary>
        public bool HasFadeOverride => _fadeDuration >= 0f;
    }

    /// <summary>
    ///     How a music change should sound. <c>default</c> means "whatever <see cref="AM" /> is configured
    ///     to do" - which is a crossfade out of the box.
    /// </summary>
    /// <example>
    ///     <code>
    /// AM.I.PlayMusic("boss");                            // default crossfade
    /// AM.I.PlayMusic("boss", MusicTransition.Instant);   // hard cut
    /// AM.I.PlayMusic("boss", MusicTransition.Fade(2f));  // 2 second crossfade
    /// </code>
    /// </example>
    public readonly struct MusicTransition
    {
        /// <summary>False when the transition defers to the manager's configured default.</summary>
        public readonly bool HasDuration;

        /// <summary>Explicit crossfade length in seconds. Only meaningful when <see cref="HasDuration" />.</summary>
        public readonly float Duration;

        private MusicTransition(float duration)
        {
            HasDuration = true;
            Duration = Mathf.Max(0f, duration);
        }

        /// <summary>Use the crossfade configured on <see cref="AM" />.</summary>
        public static MusicTransition Default => default;

        /// <summary>Cut straight to the new music with no fade.</summary>
        public static MusicTransition Instant => new MusicTransition(0f);

        /// <summary>Crossfade over an explicit number of seconds.</summary>
        public static MusicTransition Fade(float seconds)
        {
            return new MusicTransition(seconds);
        }
    }
}
