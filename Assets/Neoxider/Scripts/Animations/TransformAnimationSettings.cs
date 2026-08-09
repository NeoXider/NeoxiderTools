using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Neo.Animations
{
    /// <summary>
    ///     Serializable settings for <see cref="TransformAnimator" />: combinable rotate, float (bob),
    ///     scale pulse, continuous Perlin shake and one-shot impulse shake channels.
    /// </summary>
    [Serializable]
    public class TransformAnimationSettings
    {
        [Header("Rotate")]
        [Tooltip("Enable constant rotation")]
        [FormerlySerializedAs("rotateEnabled")]
        public bool RotateEnabled = true;

        [Tooltip("Rotation speed in degrees per second, per axis")]
        [FormerlySerializedAs("rotationSpeed")]
        public Vector3 RotationSpeed = new(0f, 90f, 0f);

        [Header("Float (bob)")]
        [Tooltip("Enable floating up and down along a direction")]
        [FormerlySerializedAs("floatEnabled")]
        public bool FloatEnabled;

        [Tooltip("Float direction (normalized before use)")]
        [FormerlySerializedAs("floatDirection")]
        public Vector3 FloatDirection = Vector3.up;

        [Tooltip("Max offset from the base position")]
        [Min(0f)]
        [FormerlySerializedAs("floatHeight")]
        public float FloatHeight = 0.25f;

        [Tooltip("Full cycle duration in seconds (there and back)")]
        [Min(0.01f)]
        [FormerlySerializedAs("floatDuration")]
        public float FloatDuration = 2f;

        [Tooltip("Easing over the cycle phase (0..1). Default: smooth up-down")]
        [FormerlySerializedAs("floatCurve")]
        public AnimationCurve FloatCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Scale Pulse")]
        [Tooltip("Enable scale pulsing symmetrically around the base scale")]
        [FormerlySerializedAs("scaleEnabled")]
        public bool ScaleEnabled;

        [Tooltip("Max relative scale deviation (0.1 = from -10% to +10%)")]
        [Min(0f)]
        [FormerlySerializedAs("scaleAmplitude")]
        public float ScaleAmplitude = 0.1f;

        [Tooltip("Full pulse cycle duration in seconds")]
        [Min(0.01f)]
        [FormerlySerializedAs("scaleDuration")]
        public float ScaleDuration = 1f;

        [Tooltip("Pulse shape over the cycle phase (0..1); values 0..1 map to -amplitude..+amplitude")]
        [FormerlySerializedAs("scaleCurve")]
        public AnimationCurve ScaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Shake (continuous)")]
        [Tooltip("Enable continuous Perlin-noise shake")]
        [FormerlySerializedAs("shakeEnabled")]
        public bool ShakeEnabled;

        [Tooltip("Position shake amplitude in local units")]
        [Min(0f)]
        [FormerlySerializedAs("shakePositionStrength")]
        public float ShakePositionStrength = 0.05f;

        [Tooltip("Rotation shake amplitude in degrees")]
        [Min(0f)]
        [FormerlySerializedAs("shakeRotationStrength")]
        public float ShakeRotationStrength = 5f;

        [Tooltip("Shake noise speed")]
        [Range(0.1f, 30f)]
        [FormerlySerializedAs("shakeSpeed")]
        public float ShakeSpeed = 8f;

        [Header("Impulse Shake (one-shot)")]
        [Tooltip("Impulse duration in seconds; non-positive values disable the impulse")]
        [Min(0.01f)]
        [FormerlySerializedAs("impulseDuration")]
        public float ImpulseDuration = 0.4f;

        [Tooltip("Impulse position amplitude in local units")]
        [Min(0f)]
        [FormerlySerializedAs("impulsePositionStrength")]
        public float ImpulsePositionStrength = 0.3f;

        [Tooltip("Impulse rotation amplitude in degrees")]
        [Min(0f)]
        [FormerlySerializedAs("impulseRotationStrength")]
        public float ImpulseRotationStrength = 15f;

        [Tooltip("Decay over impulse lifetime. Default: starts at 1, fades to 0")]
        [FormerlySerializedAs("impulseDecayCurve")]
        public AnimationCurve ImpulseDecayCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
    }
}
