using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Neo.UI
{
    /// <summary>
    /// Authoring starting points for procedural UI mesh motion. Applying a preset copies
    /// regular editable curves and values; runtime evaluation never depends on this enum.
    /// </summary>
    public enum UIMeshRigMotionPreset
    {
        Custom = 0,
        Float = 1,
        Breathe = 2,
        BodySway = 3,
        HeadSway = 4,
        SoftJiggle = 5,
        Pulse = 6,
        SquashStretch = 7,
        Wave = 8,
        Noise = 9
    }

    /// <summary>
    /// Evaluation algorithm used by a motion profile. Curves remain the default for serialized compatibility.
    /// </summary>
    public enum UIMeshRigMotionAlgorithm
    {
        Curves = 0,
        SmoothNoise = 1
    }

    /// <summary>
    /// Serializable procedural motion authored in normalized cycle time (0..1).
    /// Position is measured in UI pixels, rotation in degrees and scale is additive
    /// around one (an amplitude of 0.02 means two percent).
    /// </summary>
    [Serializable]
    public sealed class UIMeshRigMotionProfile
    {
        // WHY: PascalCase is the package rule for public members (ModulePrinciplesTests guards it).
        // [FormerlySerializedAs] keeps every profile authored under the previous camelCase names — the demo
        // scene among them — loading its values instead of silently resetting to defaults.
        [FormerlySerializedAs("duration")]
        [Min(0.01f)] [Tooltip("Length of one motion cycle in seconds.")]
        public float Duration = 2f;

        [FormerlySerializedAs("positionAmplitudePixels")]
        [Tooltip("Peak offset in UI pixels, scaled into world units by non-canvas adapters.")]
        public Vector2 PositionAmplitudePixels = Vector2.zero;

        [FormerlySerializedAs("rotationAmplitudeDegrees")]
        [Tooltip("Peak rotation in degrees.")]
        public float RotationAmplitudeDegrees;

        [FormerlySerializedAs("scaleAmplitude")]
        [Tooltip("Peak scale offset around one. 0.02 means two percent.")]
        public Vector2 ScaleAmplitude = Vector2.zero;

        [FormerlySerializedAs("positionX")]
        [Tooltip("Horizontal position over one normalized cycle.")]
        public AnimationCurve PositionX = ConstantZero();

        [FormerlySerializedAs("positionY")]
        [Tooltip("Vertical position over one normalized cycle.")]
        public AnimationCurve PositionY = ConstantZero();

        [FormerlySerializedAs("rotation")]
        [Tooltip("Rotation over one normalized cycle.")]
        public AnimationCurve Rotation = ConstantZero();

        [FormerlySerializedAs("scaleX")]
        [Tooltip("Horizontal scale over one normalized cycle.")]
        public AnimationCurve ScaleX = ConstantZero();

        [FormerlySerializedAs("scaleY")]
        [Tooltip("Vertical scale over one normalized cycle.")]
        public AnimationCurve ScaleY = ConstantZero();

        [FormerlySerializedAs("algorithm")]
        [Tooltip("Curves replay the authored shapes; Smooth Noise ignores them and wanders deterministically.")]
        public UIMeshRigMotionAlgorithm Algorithm = UIMeshRigMotionAlgorithm.Curves;

        [FormerlySerializedAs("spatialPhaseCycles")]
        [Tooltip("Adds cycle phase from the point's normalized X/Y position. Use this to make motion travel across a rig.")]
        public Vector2 SpatialPhaseCycles = Vector2.zero;

        [FormerlySerializedAs("noiseFrequency")]
        [Min(0.01f)] [Tooltip("Smooth-noise samples per second. Only used by Smooth Noise profiles.")]
        public float NoiseFrequency = 1f;

        public UIMeshRigMotionProfile Clone()
        {
            UIMeshRigMotionProfile clone = new UIMeshRigMotionProfile();
            clone.CopyFrom(this);
            return clone;
        }

        public void CopyFrom(UIMeshRigMotionProfile source)
        {
            if (source == null)
            {
                return;
            }

            Duration = source.Duration;
            PositionAmplitudePixels = source.PositionAmplitudePixels;
            RotationAmplitudeDegrees = source.RotationAmplitudeDegrees;
            ScaleAmplitude = source.ScaleAmplitude;
            PositionX = CopyCurve(source.PositionX);
            PositionY = CopyCurve(source.PositionY);
            Rotation = CopyCurve(source.Rotation);
            ScaleX = CopyCurve(source.ScaleX);
            ScaleY = CopyCurve(source.ScaleY);
            Algorithm = source.Algorithm;
            SpatialPhaseCycles = source.SpatialPhaseCycles;
            NoiseFrequency = source.NoiseFrequency;
        }

        internal static AnimationCurve ConstantZero()
        {
            return new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 0f));
        }

        internal static AnimationCurve CopyCurve(AnimationCurve source)
        {
            if (source == null)
            {
                return ConstantZero();
            }

            AnimationCurve copy = new AnimationCurve(source.keys)
            {
                preWrapMode = source.preWrapMode,
                postWrapMode = source.postWrapMode
            };
            return copy;
        }
    }

    /// <summary>
    /// Creates editable motion profiles with restrained, UI-friendly defaults.
    /// </summary>
    public static class UIMeshRigMotionPresets
    {
        public static UIMeshRigMotionProfile Create(UIMeshRigMotionPreset preset)
        {
            UIMeshRigMotionProfile profile = new UIMeshRigMotionProfile();
            AnimationCurve sine = CreateSine(1f, 0f);
            AnimationCurve cosine = CreateSine(1f, 0.25f);

            switch (preset)
            {
                case UIMeshRigMotionPreset.Float:
                    profile.Duration = 3.2f;
                    profile.PositionAmplitudePixels = new Vector2(0.8f, 3f);
                    profile.RotationAmplitudeDegrees = 0.45f;
                    profile.PositionX = cosine;
                    profile.PositionY = sine;
                    profile.Rotation = CreateSine(1f, 0.1f);
                    break;

                case UIMeshRigMotionPreset.Breathe:
                    profile.Duration = 2.8f;
                    profile.PositionAmplitudePixels = new Vector2(0f, 1.2f);
                    profile.ScaleAmplitude = new Vector2(0.004f, 0.012f);
                    profile.PositionY = CreateBreathCurve();
                    profile.ScaleX = CreateBreathCurve();
                    profile.ScaleY = CreateBreathCurve();
                    break;

                case UIMeshRigMotionPreset.BodySway:
                    profile.Duration = 4.8f;
                    profile.PositionAmplitudePixels = new Vector2(2.5f, 1f);
                    profile.RotationAmplitudeDegrees = 1.1f;
                    profile.PositionX = sine;
                    profile.PositionY = CreateSine(1f, 0.25f);
                    profile.Rotation = CreateSine(1f, 0.06f);
                    break;

                case UIMeshRigMotionPreset.HeadSway:
                    profile.Duration = 5.2f;
                    profile.PositionAmplitudePixels = new Vector2(1.2f, 0.7f);
                    profile.RotationAmplitudeDegrees = 2.2f;
                    profile.PositionX = CreateSine(1f, 0.08f);
                    profile.PositionY = CreateSine(1f, 0.31f);
                    profile.Rotation = sine;
                    break;

                case UIMeshRigMotionPreset.SoftJiggle:
                    profile.Duration = 1.15f;
                    profile.PositionAmplitudePixels = new Vector2(0.35f, 2.4f);
                    profile.RotationAmplitudeDegrees = 0.4f;
                    profile.ScaleAmplitude = new Vector2(0.006f, 0.014f);
                    profile.PositionX = CreateDampedJiggle(0.35f);
                    profile.PositionY = CreateDampedJiggle(1f);
                    profile.Rotation = CreateDampedJiggle(0.6f);
                    profile.ScaleX = CreateDampedJiggle(-0.45f);
                    profile.ScaleY = CreateDampedJiggle(1f);
                    break;

                case UIMeshRigMotionPreset.Pulse:
                    profile.Duration = 1.6f;
                    profile.ScaleAmplitude = new Vector2(0.025f, 0.025f);
                    profile.ScaleX = CreatePulseCurve();
                    profile.ScaleY = CreatePulseCurve();
                    break;

                case UIMeshRigMotionPreset.SquashStretch:
                    profile.Duration = 1.4f;
                    profile.PositionAmplitudePixels = new Vector2(0f, 1.5f);
                    profile.ScaleAmplitude = new Vector2(0.025f, 0.04f);
                    profile.PositionY = CreateSine(1f, 0.25f);
                    profile.ScaleX = sine;
                    profile.ScaleY = Invert(sine);
                    break;

                case UIMeshRigMotionPreset.Wave:
                    profile.Duration = 2.4f;
                    profile.PositionAmplitudePixels = new Vector2(1f, 5f);
                    profile.RotationAmplitudeDegrees = 1.2f;
                    profile.PositionX = CreateSine(1f, 0.25f);
                    profile.PositionY = sine;
                    profile.Rotation = CreateSine(1f, 0.12f);
                    profile.SpatialPhaseCycles = new Vector2(-1.25f, 0.15f);
                    break;

                case UIMeshRigMotionPreset.Noise:
                    profile.Duration = 1f;
                    profile.PositionAmplitudePixels = new Vector2(2.2f, 2.8f);
                    profile.RotationAmplitudeDegrees = 1.1f;
                    profile.ScaleAmplitude = new Vector2(0.006f, 0.009f);
                    profile.Algorithm = UIMeshRigMotionAlgorithm.SmoothNoise;
                    profile.NoiseFrequency = 0.75f;
                    break;

                case UIMeshRigMotionPreset.Custom:
                default:
                    break;
            }

            return profile;
        }

        private static AnimationCurve CreateSine(float amplitude, float phase)
        {
            const int sampleCount = 8;
            Keyframe[] keys = new Keyframe[sampleCount + 1];
            int index;
            for (index = 0; index <= sampleCount; index++)
            {
                float time = (float)index / sampleCount;
                float angle = (time + phase) * Mathf.PI * 2f;
                float value = Mathf.Sin(angle) * amplitude;
                float tangent = Mathf.Cos(angle) * Mathf.PI * 2f * amplitude;
                keys[index] = new Keyframe(time, value, tangent, tangent);
            }

            return new AnimationCurve(keys);
        }

        private static AnimationCurve CreateBreathCurve()
        {
            return new AnimationCurve(
                new Keyframe(0f, -0.25f, 0f, 0f),
                new Keyframe(0.42f, 1f, 0f, 0f),
                new Keyframe(0.62f, 0.82f, 0f, 0f),
                new Keyframe(1f, -0.25f, 0f, 0f));
        }

        private static AnimationCurve CreateDampedJiggle(float amplitude)
        {
            return new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.12f, amplitude),
                new Keyframe(0.29f, -amplitude * 0.62f),
                new Keyframe(0.47f, amplitude * 0.34f),
                new Keyframe(0.68f, -amplitude * 0.14f),
                new Keyframe(1f, 0f));
        }

        private static AnimationCurve CreatePulseCurve()
        {
            return new AnimationCurve(
                new Keyframe(0f, 0f, 0f, 0f),
                new Keyframe(0.18f, 1f, 0f, 0f),
                new Keyframe(0.42f, -0.2f, 0f, 0f),
                new Keyframe(0.62f, 0.08f, 0f, 0f),
                new Keyframe(1f, 0f, 0f, 0f));
        }

        private static AnimationCurve Invert(AnimationCurve source)
        {
            Keyframe[] keys = source.keys;
            int index;
            for (index = 0; index < keys.Length; index++)
            {
                Keyframe key = keys[index];
                key.value = -key.value;
                key.inTangent = -key.inTangent;
                key.outTangent = -key.outTangent;
                keys[index] = key;
            }

            return new AnimationCurve(keys);
        }
    }
}
