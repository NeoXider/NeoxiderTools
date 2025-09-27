using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Neo.Extensions
{
    /// <summary>
    ///     Extension methods for the Unity Transform component.
    /// </summary>
    public static class TransformExtensions
    {
        #region Position Methods

        /// <summary>
        ///     Sets the transform's position with optional individual components.
        /// </summary>
        public static void SetPosition(this Transform transform, Vector3? position = null, float? x = null,
            float? y = null, float? z = null)
        {
            if (transform == null) return;

            var newPosition = position ?? transform.position;
            if (x.HasValue) newPosition.x = x.Value;
            if (y.HasValue) newPosition.y = y.Value;
            if (z.HasValue) newPosition.z = z.Value;

            transform.position = newPosition;
        }

        /// <summary>
        ///     Adds to the transform's position with optional individual components.
        /// </summary>
        public static void AddPosition(this Transform transform, Vector3? delta = null, float? x = null,
            float? y = null, float? z = null)
        {
            if (transform == null) return;

            var currentPosition = transform.position;
            var finalDelta = delta ?? Vector3.zero;
            if (x.HasValue) finalDelta.x += x.Value;
            if (y.HasValue) finalDelta.y += y.Value;
            if (z.HasValue) finalDelta.z += z.Value;

            transform.position = currentPosition + finalDelta;
        }

        #endregion

        #region Local Position Methods

        /// <summary>
        ///     Sets the transform's local position with optional individual components.
        /// </summary>
        public static void SetLocalPosition(this Transform transform, Vector3? position = null, float? x = null,
            float? y = null, float? z = null)
        {
            if (transform == null) return;

            var newPosition = position ?? transform.localPosition;
            if (x.HasValue) newPosition.x = x.Value;
            if (y.HasValue) newPosition.y = y.Value;
            if (z.HasValue) newPosition.z = z.Value;

            transform.localPosition = newPosition;
        }

        /// <summary>
        ///     Adds to the transform's local position with optional individual components.
        /// </summary>
        public static void AddLocalPosition(this Transform transform, Vector3? delta = null, float? x = null,
            float? y = null, float? z = null)
        {
            if (transform == null) return;

            var currentPosition = transform.localPosition;
            var finalDelta = delta ?? Vector3.zero;
            if (x.HasValue) finalDelta.x += x.Value;
            if (y.HasValue) finalDelta.y += y.Value;
            if (z.HasValue) finalDelta.z += z.Value;

            transform.localPosition = currentPosition + finalDelta;
        }

        #endregion

        #region Rotation Methods

        /// <summary>
        ///     Sets the transform's rotation with optional individual components.
        /// </summary>
        public static void SetRotation(this Transform transform, Quaternion? rotation = null,
            Vector3? eulerAngles = null, float? x = null, float? y = null, float? z = null)
        {
            if (transform == null) return;

            if (rotation.HasValue)
            {
                transform.rotation = rotation.Value;
                return;
            }

            var angles = eulerAngles ?? transform.eulerAngles;
            if (x.HasValue) angles.x = x.Value;
            if (y.HasValue) angles.y = y.Value;
            if (z.HasValue) angles.z = z.Value;

            transform.rotation = Quaternion.Euler(angles);
        }

        /// <summary>
        ///     Adds to the transform's rotation with optional individual components.
        /// </summary>
        public static void AddRotation(this Transform transform, Quaternion? delta = null, Vector3? eulerDelta = null,
            float? x = null, float? y = null, float? z = null)
        {
            if (transform == null) return;

            if (delta.HasValue)
            {
                transform.rotation *= delta.Value;
                return;
            }

            var currentAngles = transform.eulerAngles;
            var finalDelta = eulerDelta ?? Vector3.zero;
            if (x.HasValue) finalDelta.x += x.Value;
            if (y.HasValue) finalDelta.y += y.Value;
            if (z.HasValue) finalDelta.z += z.Value;

            transform.rotation = Quaternion.Euler(currentAngles + finalDelta);
        }

        #endregion

        #region Local Rotation Methods

        /// <summary>
        ///     Sets the transform's local rotation with optional individual components.
        /// </summary>
        public static void SetLocalRotation(this Transform transform, Quaternion? rotation = null,
            Vector3? eulerAngles = null, float? x = null, float? y = null, float? z = null)
        {
            if (transform == null) return;

            if (rotation.HasValue)
            {
                transform.localRotation = rotation.Value;
                return;
            }

            var angles = eulerAngles ?? transform.localEulerAngles;
            if (x.HasValue) angles.x = x.Value;
            if (y.HasValue) angles.y = y.Value;
            if (z.HasValue) angles.z = z.Value;

            transform.localRotation = Quaternion.Euler(angles);
        }

        /// <summary>
        ///     Adds to the transform's local rotation with optional individual components.
        /// </summary>
        public static void AddLocalRotation(this Transform transform, Quaternion? delta = null,
            Vector3? eulerDelta = null, float? x = null, float? y = null, float? z = null)
        {
            if (transform == null) return;

            if (delta.HasValue)
            {
                transform.localRotation *= delta.Value;
                return;
            }

            var currentAngles = transform.localEulerAngles;
            var finalDelta = eulerDelta ?? Vector3.zero;
            if (x.HasValue) finalDelta.x += x.Value;
            if (y.HasValue) finalDelta.y += y.Value;
            if (z.HasValue) finalDelta.z += z.Value;

            transform.localRotation = Quaternion.Euler(currentAngles + finalDelta);
        }

        #endregion

        #region Scale Methods

        /// <summary>
        ///     Sets the transform's scale with optional individual components.
        /// </summary>
        public static void SetScale(this Transform transform, Vector3? scale = null, float? x = null, float? y = null,
            float? z = null)
        {
            if (transform == null) return;

            var newScale = scale ?? transform.localScale;
            if (x.HasValue) newScale.x = x.Value;
            if (y.HasValue) newScale.y = y.Value;
            if (z.HasValue) newScale.z = z.Value;

            transform.localScale = newScale;
        }

        /// <summary>
        ///     Adds to the transform's scale with optional individual components.
        /// </summary>
        public static void AddScale(this Transform transform, Vector3? delta = null, float? x = null, float? y = null,
            float? z = null)
        {
            if (transform == null) return;

            var currentScale = transform.localScale;
            var finalDelta = delta ?? Vector3.zero;
            if (x.HasValue) finalDelta.x += x.Value;
            if (y.HasValue) finalDelta.y += y.Value;
            if (z.HasValue) finalDelta.z += z.Value;

            transform.localScale = currentScale + finalDelta;
        }

        #endregion

        #region Utility Methods

        /// <summary>
        ///     Rotates the transform to look at a specified position in 2D space (on the XY plane).
        /// </summary>
        /// <param name="offset">Additional rotation angle in degrees applied to the final Z rotation.</param>
        public static void LookAt2D(this Transform transform, Vector3 targetPosition, float offset = 0f)
        {
            var direction = (targetPosition - transform.position).normalized;
            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle - 90 + offset);
        }

        /// <summary>
        ///     Returns a coroutine that smoothly rotates the transform to look at a target.
        /// </summary>
        /// <remarks>You must run this with StartCoroutine().</remarks>
        /// <param name="target">The world position to look at.</param>
        /// <param name="speed">The speed of the rotation.</param>
        /// <returns>An IEnumerator to be used in a coroutine.</returns>
        public static IEnumerator SmoothLookAtRoutine(this Transform transform, Vector3 target, float speed)
        {
            var targetRotation = Quaternion.LookRotation(target - transform.position);
            while (Quaternion.Angle(transform.rotation, targetRotation) > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * speed);
                yield return null;
            }

            transform.rotation = targetRotation; // Snap to final rotation
        }

        /// <summary>
        ///     Finds the closest Transform from a collection of other transforms.
        /// </summary>
        /// <param name="others">A collection of transforms to compare against.</param>
        /// <returns>The closest transform, or null if the collection is empty.</returns>
        public static Transform GetClosest(this Transform transform, IEnumerable<Transform> others)
        {
            Transform closest = null;
            var minDistanceSqr = float.MaxValue;

            if (others == null) return null;

            foreach (var other in others.Where(t => t != null && t != transform))
            {
                var distSqr = (transform.position - other.position).sqrMagnitude;
                if (distSqr < minDistanceSqr)
                {
                    minDistanceSqr = distSqr;
                    closest = other;
                }
            }

            return closest;
        }

        /// <summary>
        ///     Returns all immediate child transforms of the current transform.
        /// </summary>
        public static Transform[] GetChildTransforms(this Transform transform)
        {
            if (transform == null) return new Transform[0];

            var children = new Transform[transform.childCount];
            for (var i = 0; i < transform.childCount; i++) children[i] = transform.GetChild(i);
            return children;
        }

        /// <summary>
        ///     Resets the transform's position, rotation, and scale to default values.
        /// </summary>
        public static void ResetTransform(this Transform transform)
        {
            if (transform == null) return;
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        /// <summary>
        ///     Resets the transform's local position, rotation, and scale to default values.
        /// </summary>
        public static void ResetLocalTransform(this Transform transform)
        {
            if (transform == null) return;
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        /// <summary>
        ///     Copies position, rotation, and scale from a source transform to this transform.
        /// </summary>
        public static void CopyFrom(this Transform transform, Transform source)
        {
            if (source == null || transform == null) return;

            transform.position = source.position;
            transform.rotation = source.rotation;
            transform.localScale = source.localScale;
        }

        /// <summary>
        ///     Destroys all child objects of the transform.
        /// </summary>
        public static void DestroyChildren(this Transform transform)
        {
            if (transform == null) return;

            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (Application.isPlaying)
                    Object.Destroy(child.gameObject);
                else
                    Object.DestroyImmediate(child.gameObject);
            }
        }

        #endregion
    }
}