using UnityEngine;

namespace Neo.UI
{
    public enum UIMeshRigFalloffPreset
    {
        Linear,
        Smooth,
        Soft,
        Sharp,
        Custom
    }

    public static class UIMeshRigFalloffPresets
    {
        private static readonly AnimationCurve[] SharedCurves = new AnimationCurve[5];

        /// <summary>
        ///     Read-only curve for <paramref name="preset" />, created once and shared. Evaluation paths use it
        ///     to avoid allocating a curve per point per frame; never modify the returned curve.
        /// </summary>
        internal static AnimationCurve GetShared(UIMeshRigFalloffPreset preset)
        {
            int index = (int)preset;
            if (index < 0 || index >= SharedCurves.Length)
            {
                return Create(preset);
            }

            AnimationCurve curve = SharedCurves[index];
            if (curve == null)
            {
                curve = Create(preset);
                SharedCurves[index] = curve;
            }

            return curve;
        }

        /// <summary>Creates a new editable falloff curve for <paramref name="preset" /> (Custom gets EaseInOut).</summary>
        public static AnimationCurve Create(UIMeshRigFalloffPreset preset)
        {
            switch (preset)
            {
                case UIMeshRigFalloffPreset.Linear:
                    return AnimationCurve.Linear(0f, 0f, 1f, 1f);
                case UIMeshRigFalloffPreset.Soft:
                    return new AnimationCurve(
                        new Keyframe(0f, 0f, 0f, 1.6f),
                        new Keyframe(1f, 1f, 0.35f, 0f));
                case UIMeshRigFalloffPreset.Sharp:
                    return new AnimationCurve(
                        new Keyframe(0f, 0f, 0f, 0.25f),
                        new Keyframe(1f, 1f, 2.2f, 0f));
                default:
                    return AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            }
        }
    }
}
