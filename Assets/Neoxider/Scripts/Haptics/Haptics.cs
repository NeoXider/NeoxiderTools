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
    ///         Haptics on iOS and VibrationEffect on Android, so a type or a duration feels the same on both.
    ///         Without it the module still works on Android through <see cref="Handheld.Vibrate" /> for the
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
        private static bool _enabled = true;
        private static float _lastPlayTime = float.NegativeInfinity;
        private static HapticType _lastType;

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
        ///     in one frame would otherwise queue ten pulses that the motor plays as one long buzz.
        /// </summary>
        public static float MinRepeatInterval { get; set; } = 0.035f;

        /// <summary>Raised for every pulse that passed the gates, on every platform including the editor.</summary>
        public static event Action<HapticType> Played;

        /// <summary>True when this device and build can actually vibrate.</summary>
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

        /// <summary>Plays a pulse of the given feel. Does nothing when disabled, throttled or unsupported.</summary>
        public static void Play(HapticType type)
        {
            if (!PassesGates(type))
            {
                return;
            }

            Played?.Invoke(type);
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
            // Handheld.Vibrate is one fixed, long buzz: only worth it for the moments meant to be felt.
            if (type == HapticType.Heavy || type == HapticType.Success || type == HapticType.Error ||
                type == HapticType.Warning)
            {
                Handheld.Vibrate();
            }
#endif
        }

        /// <summary>
        ///     One pulse with an explicit shape. <paramref name="sharpness" /> is honoured where the platform
        ///     supports it (iOS Core Haptics) and ignored elsewhere.
        /// </summary>
        public static void Play(float intensity, float sharpness, float durationSeconds)
        {
            if (!PassesGates(HapticType.Medium))
            {
                return;
            }

            Played?.Invoke(HapticType.Medium);
#if NEO_MOBILE_HAPTICS
            if (IsSupported)
            {
                Native.PlayImpact(Mathf.Clamp01(intensity), Mathf.Clamp01(sharpness), Math.Max(0d, durationSeconds));
            }
#endif
        }

        /// <summary>A waveform: paired segment durations (seconds) and amplitudes (0..1).</summary>
        public static void PlayPattern(float[] durationsSeconds, float[] amplitudes)
        {
            if (durationsSeconds == null || amplitudes == null || !PassesGates(HapticType.Heavy))
            {
                return;
            }

            Played?.Invoke(HapticType.Heavy);
#if NEO_MOBILE_HAPTICS
            if (!IsSupported)
            {
                return;
            }

            // Paired, not parallel arrays: a length mismatch inside the native call fails silently.
            int count = Math.Min(durationsSeconds.Length, amplitudes.Length);
            if (count == 0)
            {
                return;
            }

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

        /// <summary>Test seam: forget the last pulse so the repeat throttle starts clean.</summary>
        public static void ResetThrottle()
        {
            _lastPlayTime = float.NegativeInfinity;
        }

        private static bool PassesGates(HapticType type)
        {
            if (!Enabled)
            {
                return false;
            }

            float now = Time.unscaledTime;
            if (type == _lastType && now - _lastPlayTime < MinRepeatInterval)
            {
                return false;
            }

            _lastType = type;
            _lastPlayTime = now;
            return true;
        }
    }
}
