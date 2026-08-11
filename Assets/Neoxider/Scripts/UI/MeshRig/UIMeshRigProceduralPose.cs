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
            if (profile == null)
            {
                return UIMeshRigProceduralPose.Identity;
            }

            float duration = Mathf.Max(0.01f, profile.duration);
            float normalizedTime = Mathf.Repeat(timeSeconds * speed / duration + phase, 1f);
            float positionX = EvaluateCurve(profile.positionX, normalizedTime);
            float positionY = EvaluateCurve(profile.positionY, normalizedTime);
            float rotation = EvaluateCurve(profile.rotation, normalizedTime);
            float scaleX = EvaluateCurve(profile.scaleX, normalizedTime);
            float scaleY = EvaluateCurve(profile.scaleY, normalizedTime);

            Vector2 position = new Vector2(
                positionX * profile.positionAmplitudePixels.x,
                positionY * profile.positionAmplitudePixels.y);
            Vector2 scale = Vector2.one + new Vector2(
                scaleX * profile.scaleAmplitude.x,
                scaleY * profile.scaleAmplitude.y);
            scale.x = Mathf.Max(0.001f, scale.x);
            scale.y = Mathf.Max(0.001f, scale.y);

            return new UIMeshRigProceduralPose(
                position,
                rotation * profile.rotationAmplitudeDegrees,
                scale);
        }

        private static float EvaluateCurve(AnimationCurve curve, float normalizedTime)
        {
            return curve != null && curve.length > 0 ? curve.Evaluate(normalizedTime) : 0f;
        }
    }
}
