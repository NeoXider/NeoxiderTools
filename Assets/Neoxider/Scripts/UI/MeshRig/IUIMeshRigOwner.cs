using System.Collections.Generic;
using UnityEngine;

namespace Neo.UI
{
    /// <summary>
    /// Everything a rig point, an authoring tool or an inspector needs from the component that owns a rig,
    /// independent of how that component finally renders (uGUI, world mesh, SpriteRenderer, UI Toolkit).
    /// Adding an adapter means implementing this once instead of copying an editor per renderer.
    /// </summary>
    public interface IUIMeshRigOwner
    {
        Transform RigTransform { get; }

        /// <summary>Converts pixel-authored motion presets into the owner's own units.</summary>
        float MotionUnitScale { get; }

        UIMeshRigAuthoringMode AuthoringMode { get; }

        /// <summary>Points bound to this owner, nearest-owner resolved.</summary>
        IReadOnlyList<UIMeshRigPoint> RigPoints { get; }

        Vector2 WorldToNormalized(Vector3 worldPosition);
        Vector3 NormalizedToWorld(Vector2 normalizedPosition);
        Vector2 WorldToLocal(Vector3 worldPosition);
        Vector2 NormalizedToLocal(Vector2 normalizedPosition);
        float GetRelativeRotationDegrees(Transform point);

        void SetAuthoringMode(UIMeshRigAuthoringMode mode);
        void CaptureRestPose();
        void ResetPose();
        void NotifyPointChanged();
        void NotifyBindingChanged();
        void NotifyPoseChanged();
    }

    /// <summary>
    /// Finds the rig that owns a point. Nearest ancestor wins, so nested rigs behave the way the
    /// hierarchy reads. Renderer-agnostic on purpose: a new adapter is picked up without editing this.
    /// </summary>
    public static class UIMeshRigOwnerResolver
    {
        public static IUIMeshRigOwner Find(Transform start)
        {
            Transform current = start;
            while (current != null)
            {
                Component[] components = current.GetComponents<Component>();
                for (int index = 0; index < components.Length; index++)
                {
                    if (components[index] is IUIMeshRigOwner owner)
                    {
                        return owner;
                    }
                }

                current = current.parent;
            }

            return null;
        }

        /// <summary>True when <paramref name="candidate"/>'s nearest owner is <paramref name="owner"/>.</summary>
        public static bool OwnsPoint(IUIMeshRigOwner owner, UIMeshRigPoint candidate)
        {
            return candidate != null && ReferenceEquals(Find(candidate.transform), owner);
        }
    }
}
