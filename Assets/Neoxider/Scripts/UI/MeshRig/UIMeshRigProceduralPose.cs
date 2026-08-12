using UnityEngine;

namespace Neo.UI
{
    /// <summary>
    /// Additive pose evaluated independently from a point's Transform/Animator pose.
    /// Scale uses multiplicative values, where one is the identity.
    /// </summary>
    public readonly struct UIMeshRigProceduralPose
    {
        public static readonly UIMeshRigProceduralPose Identity =
            new UIMeshRigProceduralPose(Vector2.zero, 0f, Vector2.one);

        public UIMeshRigProceduralPose(Vector2 position, float rotationDegrees, Vector2 scale)
        {
            Position = position;
            RotationDegrees = rotationDegrees;
            Scale = scale;
        }

        public Vector2 Position { get; }
        public float RotationDegrees { get; }
        public Vector2 Scale { get; }
    }

    /// <summary>
    /// Pure evaluator shared by runtime playback, editor preview and tests.
    /// </summary>
    public static class UIMeshRigMotionEvaluator
    {
        public static UIMeshRigProceduralPose Evaluate(
            UIMeshRigMotionProfile profile,
            float timeSeconds,
            float speed,
            float phase)
        {
            return Evaluate(profile, timeSeconds, speed, phase, Vector2.zero, 0);
        }

        public static UIMeshRigProceduralPose Evaluate(
            UIMeshRigMotionProfile profile,
            float timeSeconds,
            float speed,
            float phase,
            Vector2 pointPositionNormalized,
            int seed)
        {
            if (profile == null)
            {
                return UIMeshRigProceduralPose.Identity;
            }

            float duration = Mathf.Max(0.01f, profile.Duration);
            float spatialPhase = Vector2.Dot(pointPositionNormalized, profile.SpatialPhaseCycles);
            float normalizedTime = Mathf.Repeat(timeSeconds * speed / duration + phase + spatialPhase, 1f);
            float positionX;
            float positionY;
            float rotation;
            float scaleX;
            float scaleY;

            if (profile.Algorithm == UIMeshRigMotionAlgorithm.SmoothNoise)
            {
                float noiseTime = timeSeconds * speed * Mathf.Max(0.01f, profile.NoiseFrequency);
                int pointSeed = CombinePointSeed(seed, pointPositionNormalized);
                positionX = EvaluateSmoothNoise(noiseTime, pointSeed, 0);
                positionY = EvaluateSmoothNoise(noiseTime, pointSeed, 1);
                rotation = EvaluateSmoothNoise(noiseTime, pointSeed, 2);
                scaleX = EvaluateSmoothNoise(noiseTime, pointSeed, 3);
                scaleY = EvaluateSmoothNoise(noiseTime, pointSeed, 4);
            }
            else
            {
                positionX = EvaluateCurve(profile.PositionX, normalizedTime);
                positionY = EvaluateCurve(profile.PositionY, normalizedTime);
                rotation = EvaluateCurve(profile.Rotation, normalizedTime);
                scaleX = EvaluateCurve(profile.ScaleX, normalizedTime);
                scaleY = EvaluateCurve(profile.ScaleY, normalizedTime);
            }

            Vector2 position = new Vector2(
                positionX * profile.PositionAmplitudePixels.x,
                positionY * profile.PositionAmplitudePixels.y);
            Vector2 scale = Vector2.one + new Vector2(
                scaleX * profile.ScaleAmplitude.x,
                scaleY * profile.ScaleAmplitude.y);
            scale.x = Mathf.Max(0.001f, scale.x);
            scale.y = Mathf.Max(0.001f, scale.y);

            return new UIMeshRigProceduralPose(
                position,
                rotation * profile.RotationAmplitudeDegrees,
                scale);
        }

        private static float EvaluateCurve(AnimationCurve curve, float normalizedTime)
        {
            return curve != null && curve.length > 0 ? curve.Evaluate(normalizedTime) : 0f;
        }

        private static int CombinePointSeed(int seed, Vector2 pointPositionNormalized)
        {
            int pointX = Mathf.RoundToInt(pointPositionNormalized.x * 10000f);
            int pointY = Mathf.RoundToInt(pointPositionNormalized.y * 10000f);
            unchecked
            {
                int hash = seed;
                hash = hash * 397 ^ pointX;
                hash = hash * 397 ^ pointY;
                return hash;
            }
        }

        private static float EvaluateSmoothNoise(float time, int seed, int channel)
        {
            int sample = Mathf.FloorToInt(time);
            float fraction = time - sample;
            float smooth = fraction * fraction * fraction * (fraction * (fraction * 6f - 15f) + 10f);
            float first = HashToSignedUnit(sample, seed, channel);
            float second = HashToSignedUnit(sample + 1, seed, channel);
            return Mathf.LerpUnclamped(first, second, smooth);
        }

        private static float HashToSignedUnit(int sample, int seed, int channel)
        {
            unchecked
            {
                uint value = (uint)sample;
                value ^= (uint)seed + 0x9E3779B9u + (value << 6) + (value >> 2);
                value ^= (uint)(channel + 1) * 0x85EBCA6Bu;
                value ^= value >> 16;
                value *= 0x7FEB352Du;
                value ^= value >> 15;
                value *= 0x846CA68Bu;
                value ^= value >> 16;
                return (value & 0x00FFFFFFu) / 8388607.5f - 1f;
            }
        }
    }
}
