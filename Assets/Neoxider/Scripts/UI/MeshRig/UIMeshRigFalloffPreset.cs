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
