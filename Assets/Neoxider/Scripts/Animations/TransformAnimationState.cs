using UnityEngine;

namespace Neo.Animations
{
    /// <summary>Evaluated local transform state produced by <see cref="TransformAnimationEvaluator" />.</summary>
    public struct TransformAnimationState
    {
        /// <summary>Local position.</summary>
        public Vector3 LocalPosition;

        /// <summary>Local rotation as Euler angles.</summary>
        public Vector3 LocalEulerAngles;

        /// <summary>Local scale.</summary>
        public Vector3 LocalScale;
    }
}
