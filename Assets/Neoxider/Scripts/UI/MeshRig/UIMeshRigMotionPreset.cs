using System;
using UnityEngine;

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
        SquashStretch = 7
    }

    /// <summary>
    /// Serializable procedural motion authored in normalized cycle time (0..1).
    /// Position is measured in UI pixels, rotation in degrees and scale is additive
    /// around one (an amplitude of 0.02 means two percent).
    /// </summary>
    [Serializable]
    public sealed class UIMeshRigMotionProfile
    {
        [Min(0.01f)] public float duration = 2f;
        public Vector2 positionAmplitudePixels = Vector2.zero;
        public float rotationAmplitudeDegrees;
        public Vector2 scaleAmplitude = Vector2.zero;
        public AnimationCurve positionX = ConstantZero();
        public AnimationCurve positionY = ConstantZero();
        public AnimationCurve rotation = ConstantZero();
        public AnimationCurve scaleX = ConstantZero();
        public AnimationCurve scaleY = ConstantZero();

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

            duration = source.duration;
            positionAmplitudePixels = source.positionAmplitudePixels;
            rotationAmplitudeDegrees = source.rotationAmplitudeDegrees;
            scaleAmplitude = source.scaleAmplitude;
            positionX = CopyCurve(source.positionX);
            positionY = CopyCurve(source.positionY);
            rotation = CopyCurve(source.rotation);
            scaleX = CopyCurve(source.scaleX);
            scaleY = CopyCurve(source.scaleY);
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
                    profile.duration = 3.2f;
                    profile.positionAmplitudePixels = new Vector2(0.8f, 3f);
                    profile.rotationAmplitudeDegrees = 0.45f;
                    profile.positionX = cosine;
                    profile.positionY = sine;
                    profile.rotation = CreateSine(1f, 0.1f);
                    break;

                case UIMeshRigMotionPreset.Breathe:
                    profile.duration = 2.8f;
                    profile.positionAmplitudePixels = new Vector2(0f, 1.2f);
                    profile.scaleAmplitude = new Vector2(0.004f, 0.012f);
                    profile.positionY = CreateBreathCurve();
                    profile.scaleX = CreateBreathCurve();
                    profile.scaleY = CreateBreathCurve();
                    break;

                case UIMeshRigMotionPreset.BodySway:
                    profile.duration = 4.8f;
                    profile.positionAmplitudePixels = new Vector2(2.5f, 1f);
                    profile.rotationAmplitudeDegrees = 1.1f;
                    profile.positionX = sine;
                    profile.positionY = CreateSine(1f, 0.25f);
                    profile.rotation = CreateSine(1f, 0.06f);
                    break;

                case UIMeshRigMotionPreset.HeadSway:
                    profile.duration = 5.2f;
                    profile.positionAmplitudePixels = new Vector2(1.2f, 0.7f);
                    profile.rotationAmplitudeDegrees = 2.2f;
                    profile.positionX = CreateSine(1f, 0.08f);
                    profile.positionY = CreateSine(1f, 0.31f);
                    profile.rotation = sine;
                    break;

                case UIMeshRigMotionPreset.SoftJiggle:
                    profile.duration = 1.15f;
                    profile.positionAmplitudePixels = new Vector2(0.35f, 2.4f);
                    profile.rotationAmplitudeDegrees = 0.4f;
                    profile.scaleAmplitude = new Vector2(0.006f, 0.014f);
                    profile.positionX = CreateDampedJiggle(0.35f);
                    profile.positionY = CreateDampedJiggle(1f);
                    profile.rotation = CreateDampedJiggle(0.6f);
                    profile.scaleX = CreateDampedJiggle(-0.45f);
                    profile.scaleY = CreateDampedJiggle(1f);
                    break;

                case UIMeshRigMotionPreset.Pulse:
                    profile.duration = 1.6f;
                    profile.scaleAmplitude = new Vector2(0.025f, 0.025f);
                    profile.scaleX = CreatePulseCurve();
                    profile.scaleY = CreatePulseCurve();
                    break;

                case UIMeshRigMotionPreset.SquashStretch:
                    profile.duration = 1.4f;
                    profile.positionAmplitudePixels = new Vector2(0f, 1.5f);
                    profile.scaleAmplitude = new Vector2(0.025f, 0.04f);
                    profile.positionY = CreateSine(1f, 0.25f);
                    profile.scaleX = sine;
                    profile.scaleY = Invert(sine);
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
