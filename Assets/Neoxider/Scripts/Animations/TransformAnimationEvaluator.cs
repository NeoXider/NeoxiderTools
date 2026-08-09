using UnityEngine;

namespace Neo.Animations
{
    /// <summary>
    ///     Pure evaluation of <see cref="TransformAnimationSettings" /> against a base transform and time.
    /// </summary>
    public static class TransformAnimationEvaluator
    {
        /// <summary>Evaluates all enabled channels at the given time.</summary>
        public static TransformAnimationState Evaluate(
            TransformAnimationSettings settings,
            Vector3 basePosition,
            Vector3 baseEulerAngles,
            Vector3 baseScale,
            float time,
            float randomSeed,
            float impulseTime = -1f,
            float impulseStrength = 1f)
        {
            TransformAnimationState state = new()
            {
                LocalPosition = basePosition,
                LocalEulerAngles = baseEulerAngles,
                LocalScale = baseScale
            };

            if (settings == null)
            {
                return state;
            }

            if (settings.RotateEnabled)
            {
                state.LocalEulerAngles = baseEulerAngles + settings.RotationSpeed * time;
            }

            if (settings.FloatEnabled && settings.FloatHeight > 0f)
            {
                float phase = CyclePhase(time, settings.FloatDuration);
                float eased = EvaluateCurve(settings.FloatCurve, phase);
                Vector3 direction = settings.FloatDirection.sqrMagnitude > 0.0001f
                    ? settings.FloatDirection.normalized
                    : Vector3.up;
                state.LocalPosition += direction * (settings.FloatHeight * eased);
            }

            if (settings.ScaleEnabled && settings.ScaleAmplitude > 0f)
            {
                float phase = CyclePhase(time, settings.ScaleDuration);
                float eased = EvaluateCurve(settings.ScaleCurve, phase);
                float relativeScale = Mathf.LerpUnclamped(-settings.ScaleAmplitude, settings.ScaleAmplitude, eased);
                state.LocalScale = baseScale * (1f + relativeScale);
            }

            if (settings.ShakeEnabled)
            {
                state.LocalPosition += ShakeVector(time, settings.ShakeSpeed, randomSeed,
                    settings.ShakePositionStrength, 0f);
                state.LocalEulerAngles += ShakeVector(time, settings.ShakeSpeed, randomSeed,
                    settings.ShakeRotationStrength, 100f);
            }

            if (settings.ImpulseDuration > 0f && impulseTime >= 0f &&
                impulseTime <= settings.ImpulseDuration && impulseStrength > 0f)
            {
                float normalizedImpulseTime = impulseTime / settings.ImpulseDuration;
                float decay = EvaluateCurve(settings.ImpulseDecayCurve, normalizedImpulseTime);
                if (decay > 0f)
                {
                    float impulseNoiseSpeed = Mathf.Max(settings.ShakeSpeed * 3f, 20f);
                    state.LocalPosition += ShakeVector(impulseTime, impulseNoiseSpeed, randomSeed,
                        settings.ImpulsePositionStrength * impulseStrength * decay, 200f);
                    state.LocalEulerAngles += ShakeVector(impulseTime, impulseNoiseSpeed, randomSeed,
                        settings.ImpulseRotationStrength * impulseStrength * decay, 300f);
                }
            }

            return state;
        }

        /// <summary>Ping-pong phase 0..1 over a full cycle of the given duration.</summary>
        public static float CyclePhase(float time, float cycleDuration)
        {
            if (cycleDuration <= 0f)
            {
                return 0f;
            }

            return Mathf.PingPong(time * 2f / cycleDuration, 1f);
        }

        private static float EvaluateCurve(AnimationCurve curve, float phase)
        {
            return curve != null ? curve.Evaluate(phase) : phase;
        }

        private static Vector3 ShakeVector(float time, float speed, float seed, float strength, float axisOffset)
        {
            if (strength <= 0f)
            {
                return Vector3.zero;
            }

            float noiseTime = time * speed + seed + axisOffset;
            float x = Mathf.PerlinNoise(noiseTime, seed + axisOffset + 0.13f) * 2f - 1f;
            float y = Mathf.PerlinNoise(seed + axisOffset + 31.7f, noiseTime) * 2f - 1f;
            float z = Mathf.PerlinNoise(noiseTime * 0.83f, seed + axisOffset + 57.1f) * 2f - 1f;
            return new Vector3(x, y, z) * strength;
        }
    }
}
