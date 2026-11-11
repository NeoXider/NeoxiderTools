using System;
using UnityEngine;
#if NEO_MOBILE_HAPTICS
using Native = tsyk5.MobileHapticFeedback.MobileHapticFeedback;
using NativeImpact = tsyk5.MobileHapticFeedback.ImpactStyle;
using NativeNotification = tsyk5.MobileHapticFeedback.NotificationType;
using Segment = tsyk5.MobileHapticFeedback.PatternSegment;
#endif

namespace Neo.Haptics
{
    /// <summary>The feel of a pulse. Names follow the iOS haptic vocabulary; Android maps them to the closest effect.</summary>
    public enum HapticType
    {
        /// <summary>Crisp tick for picking or moving through something (a dot grabbed, a slider step).</summary>
        Selection,

        /// <summary>Small confirmed action (a connection made, a button accepted).</summary>
        Light,

        /// <summary>Ordinary confirmed action.</summary>
        Medium,

        /// <summary>Big moment (a level won, a boss down).</summary>
        Heavy,

        /// <summary>Rounded, cushioned bump.</summary>
        Soft,

        /// <summary>Short, hard knock.</summary>
        Rigid,

        /// <summary>Something went right (reward granted).</summary>
        Success,

        /// <summary>Attention needed (time almost up).</summary>
        Warning,

        /// <summary>Something failed (time up, purchase refused).</summary>
        Error
    }

    /// <summary>
    ///     Single entry point for device haptics. Callers never ask whether vibration is allowed or whether the
    ///     device has a motor; they request a pulse and this class stays silent when it must not fire.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         With the optional <c>com.tsyk5.mobilehapticfeedback</c> package installed, pulses go through Core
    ///         Haptics on iOS and VibrationEffect on Android for richer styles and shapes.
    ///         Without it the module still works on iOS/Android through <see cref="Handheld.Vibrate" /> for the
    ///         stronger types and stays silent for the subtle ones, rather than turning every tick into a
    ///         half-second buzz.
    ///     </para>
    ///     <para>
    ///         The editor and desktop never vibrate, but <see cref="Played" /> still fires, so tests and debug
    ///         overlays can observe exactly which pulses the game asked for.
    ///     </para>
    /// </remarks>
    [NeoDoc("Haptics/Haptics.md")]
    public static class Haptics
    {
        private const float DefaultMinRepeatInterval = 0.035f;

        // WHY: indexed by HapticType; an array lookup is cheaper than an enum-keyed dictionary on the pulse path.
        private static readonly float[] LastPlayTimes = CreateThrottleTable();
        private static bool _enabled = true;

        private static readonly object ObserverGate = new();

        /// <summary>Backing delegate of <see cref="Played" />.</summary>
        private static Action<HapticType> _played;

        /// <summary>Invocation list of <see cref="_played" />; null after every subscriber change until the next pulse.</summary>
        private static Delegate[] _observers;

        // WHY: with Enter Play Mode domain reload disabled these statics survive into the next session, where
        // Time.unscaledTime restarts near zero, so stale timestamps would throttle pulses.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            _enabled = true;
            ResetThrottle();
            EnabledProvider = null;
            MinRepeatInterval = DefaultMinRepeatInterval;
            lock (ObserverGate)
            {
                _played = null;
                _observers = null;
            }
        }

        /// <summary>Master switch, typically bound to a "Vibration" setting. On by default.</summary>
        public static bool Enabled
        {
            get => _enabled && (EnabledProvider == null || EnabledProvider());
            set => _enabled = value;
        }

        /// <summary>
        ///     Optional live source for <see cref="Enabled" />, e.g. <c>() =&gt; settings.Vibration</c>. Lets a
        ///     settings owner stay the single source of truth instead of pushing every change here.
        /// </summary>
        public static Func<bool> EnabledProvider { get; set; }

        /// <summary>
        ///     The same type fired again within this many seconds (unscaled) is dropped. A drag across ten cells
        ///     in one frame would otherwise queue ten pulses that the motor plays as one long buzz. Other types
        ///     do not clear a type's repeat history.
        /// </summary>
        public static float MinRepeatInterval { get; set; } = DefaultMinRepeatInterval;

        /// <summary>
        ///     Raised for every pulse that passed the gates, on every platform including the editor. Each observer
        ///     runs in isolation: an exception it throws is logged through <see cref="NeoDiagnostics" />, and it
        ///     neither skips the observers after it, nor cancels the pulse, nor reaches the caller of
        ///     <see cref="Play(HapticType)" />. Unsubscribing releases the observer at once.
        /// </summary>
        public static event Action<HapticType> Played
        {
            add
            {
                lock (ObserverGate)
                {
                    _played += value;
                    _observers = null;
                }
            }
            remove
            {
                // WHY: drop the cached invocation list together with the delegate, so a static cache never keeps
                // an unsubscribed observer (and the overlay that owns it) alive until the next pulse.
                lock (ObserverGate)
                {
                    _played -= value;
                    _observers = null;
                }
            }
        }

        /// <summary>Backend capability, or iOS/Android platform availability without a backend. False in the editor.</summary>
        public static bool IsSupported
        {
            get
            {
                if (Application.isEditor)
                {
                    return false;
                }
#if NEO_MOBILE_HAPTICS
                return Native.IsSupported;
#elif UNITY_ANDROID || UNITY_IOS
                return true;
#else
                return false;
#endif
            }
        }

        /// <summary>Warms the haptic engine; the first pulse after a cold engine arrives late on iOS.</summary>
        public static void Prepare()
        {
#if NEO_MOBILE_HAPTICS
            if (IsSupported && Enabled)
            {
                Native.Prepare();
            }
#endif
        }

        /// <summary>Requests a named pulse. Undefined types are ignored; valid requests raise Played after the gates.</summary>
        public static void Play(HapticType type)
        {
            if (!PassesGates(type))
            {
                return;
            }

            NotifyPlayed(type);
            if (!IsSupported)
            {
                return;
            }

#if NEO_MOBILE_HAPTICS
            switch (type)
            {
                case HapticType.Selection: Native.PlaySelection(); break;
                case HapticType.Light: Native.PlayImpact(NativeImpact.Light); break;
                case HapticType.Medium: Native.PlayImpact(NativeImpact.Medium); break;
                case HapticType.Heavy: Native.PlayImpact(NativeImpact.Heavy); break;
                case HapticType.Soft: Native.PlayImpact(NativeImpact.Soft); break;
                case HapticType.Rigid: Native.PlayImpact(NativeImpact.Rigid); break;
                case HapticType.Success: Native.PlayNotification(NativeNotification.Success); break;
                case HapticType.Warning: Native.PlayNotification(NativeNotification.Warning); break;
                case HapticType.Error: Native.PlayNotification(NativeNotification.Error); break;
            }
#elif UNITY_ANDROID || UNITY_IOS
            // WHY: Handheld.Vibrate is one fixed, long buzz: only worth it for the moments meant to be felt.
            if (type == HapticType.Heavy || type == HapticType.Success || type == HapticType.Error ||
                type == HapticType.Warning)
            {
                Handheld.Vibrate();
            }
#endif
        }

        /// <summary>
        ///     One pulse with an explicit shape. <paramref name="sharpness" /> is honoured where the platform
        ///     supports it (iOS Core Haptics) and ignored elsewhere. Non-finite parameters are ignored without
        ///     notifying observers or consuming the repeat interval.
        /// </summary>
        public static void Play(float intensity, float sharpness, float durationSeconds)
        {
            if (!IsFinite(intensity) || !IsFinite(sharpness) || !IsFinite(durationSeconds) ||
                !PassesGates(HapticType.Medium))
            {
                return;
            }

            NotifyPlayed(HapticType.Medium);
#if NEO_MOBILE_HAPTICS
            if (IsSupported)
            {
                Native.PlayImpact(Mathf.Clamp01(intensity), Mathf.Clamp01(sharpness), Math.Max(0d, durationSeconds));
            }
#endif
        }

        /// <summary>
        ///     A waveform: paired segment durations (seconds) and amplitudes (0..1), up to the shorter array.
        ///     Null, empty or non-finite paired inputs are ignored without notifying observers or throttling.
        /// </summary>
        public static void PlayPattern(float[] durationsSeconds, float[] amplitudes)
        {
            if (durationsSeconds == null || amplitudes == null)
            {
                return;
            }

            // WHY: a request with no paired segments must not consume the next real Heavy pulse's interval.
            int count = Math.Min(durationsSeconds.Length, amplitudes.Length);
            if (count == 0)
            {
                return;
            }

            for (int i = 0; i < count; i++)
            {
                if (!IsFinite(durationsSeconds[i]) || !IsFinite(amplitudes[i]))
                {
                    return;
                }
            }

            if (!PassesGates(HapticType.Heavy))
            {
                return;
            }

            NotifyPlayed(HapticType.Heavy);
#if NEO_MOBILE_HAPTICS
            if (!IsSupported)
            {
                return;
            }

            // WHY: paired, not parallel arrays: a length mismatch inside the native call fails silently.
            Segment[] segments = new Segment[count];
            for (int i = 0; i < count; i++)
            {
                segments[i] = new Segment(durationsSeconds[i], amplitudes[i]);
            }

            Native.PlayPattern(segments);
#endif
        }

        /// <summary>Stops whatever is playing, including mid-pattern.</summary>
        public static void Stop()
        {
#if NEO_MOBILE_HAPTICS
            if (IsSupported)
            {
                Native.Stop();
            }
#endif
        }

        /// <summary>Forgets all pulse timestamps, e.g. between test cases or after a scene reset.</summary>
        public static void ResetThrottle()
        {
            for (int i = 0; i < LastPlayTimes.Length; i++)
            {
                LastPlayTimes[i] = float.NegativeInfinity;
            }
        }

        private static float[] CreateThrottleTable()
        {
            float[] table = new float[(int)HapticType.Error + 1];
            for (int i = 0; i < table.Length; i++)
            {
                table[i] = float.NegativeInfinity;
            }

            return table;
        }

        private static void NotifyPlayed(HapticType type)
        {
            Delegate[] observers = _observers;
            if (observers == null)
            {
                // WHY: one invocation-list copy per subscriber change instead of one per pulse; no observers caches
                // the shared empty array, so a pulse with nobody listening takes no lock either.
                lock (ObserverGate)
                {
                    observers = _observers ??= _played?.GetInvocationList() ?? Array.Empty<Delegate>();
                }
            }

            for (int i = 0; i < observers.Length; i++)
            {
                try
                {
                    ((Action<HapticType>)observers[i])(type);
                }
                catch (Exception exception)
                {
                    // WHY: observers are debug overlays and tests; a faulty one must not swallow the device pulse
                    // or silence the observers subscribed after it.
                    NeoDiagnostics.LogException(exception);
                }
            }
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static bool PassesGates(HapticType type)
        {
            if ((int)type < (int)HapticType.Selection || (int)type > (int)HapticType.Error || !Enabled)
            {
                return false;
            }

            float now = Time.unscaledTime;
            int index = (int)type;
            // WHY: a never-played type holds -Infinity, so the elapsed time is +Infinity and always passes.
            if (now - LastPlayTimes[index] < MinRepeatInterval)
            {
                return false;
            }

            LastPlayTimes[index] = now;
            return true;
        }
    }
}
