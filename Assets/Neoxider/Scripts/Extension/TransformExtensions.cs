using UnityEngine;

namespace Neo
{
    /// <summary>
    /// Extension methods for Unity Transform component
    /// </summary>
    public static class TransformExtensions
    {
        /// <summary>
        /// Adds to the transform's world position
        /// </summary>
        /// <param name="transform">Transform to modify</param>
        /// <param name="deltaPosition">Optional vector to add to current position</param>
        /// <param name="x">Optional X coordinate to add</param>
        /// <param name="y">Optional Y coordinate to add</param>
        /// <param name="z">Optional Z coordinate to add</param>
        /// <exception cref="System.ArgumentNullException">Thrown when transform is null</exception>
        public static void AddPosition(this Transform transform, Vector3? deltaPosition = null, float? x = null, float? y = null, float? z = null)
        {
            if (transform == null)
                throw new System.ArgumentNullException(nameof(transform));

            Vector3 currentPosition = transform.position;
            Vector3 finalDelta = deltaPosition ?? Vector3.zero;
            finalDelta.x += x ?? 0;
            finalDelta.y += y ?? 0;
            finalDelta.z += z ?? 0;

            transform.position = currentPosition + finalDelta;
        }

        /// <summary>
        /// Adds rotation to the transform
        /// </summary>
        /// <param name="transform">Transform to modify</param>
        /// <param name="deltaRotation">Optional vector of Euler angles to add</param>
        /// <param name="x">Optional X rotation to add</param>
        /// <param name="y">Optional Y rotation to add</param>
        /// <param name="z">Optional Z rotation to add</param>
        /// <exception cref="System.ArgumentNullException">Thrown when transform is null</exception>
        public static void AddRotation(this Transform transform, Vector3? deltaRotation = null, float? x = null, float? y = null, float? z = null)
        {
            if (transform == null)
                throw new System.ArgumentNullException(nameof(transform));

            Vector3 finalDelta = deltaRotation ?? Vector3.zero;
            finalDelta.x += x ?? 0;
            finalDelta.y += y ?? 0;
            finalDelta.z += z ?? 0;

            transform.Rotate(finalDelta);
        }

        /// <summary>
        /// Adds to the transform's local scale
        /// </summary>
        /// <param name="transform">Transform to modify</param>
        /// <param name="deltaScale">Optional vector to add to current scale</param>
        /// <param name="x">Optional X scale to add</param>
        /// <param name="y">Optional Y scale to add</param>
        /// <param name="z">Optional Z scale to add</param>
        /// <exception cref="System.ArgumentNullException">Thrown when transform is null</exception>
        public static void AddScale(this Transform transform, Vector3? deltaScale = null, float? x = null, float? y = null, float? z = null)
        {
            if (transform == null)
                throw new System.ArgumentNullException(nameof(transform));

            Vector3 currentScale = transform.localScale;
            Vector3 finalDelta = deltaScale ?? Vector3.zero;
            finalDelta.x += x ?? 0;
            finalDelta.y += y ?? 0;
            finalDelta.z += z ?? 0;

            transform.localScale = currentScale + finalDelta;
        }

        /// <summary>
        /// Makes the transform look at a target position in 2D space (XY plane)
        /// </summary>
        /// <param name="transform">Transform to modify</param>
        /// <param name="target">Target position to look at</param>
        /// <exception cref="System.ArgumentNullException">Thrown when transform is null</exception>
        public static void LookAt2D(this Transform transform, Vector3 target)
        {
            if (transform == null)
                throw new System.ArgumentNullException(nameof(transform));

            Vector3 direction = target - transform.position;
            if (direction.sqrMagnitude < float.Epsilon)
                return;

            direction.z = 0;
            transform.up = direction.normalized;
        }

        /// <summary>
        /// Makes the transform look at a target transform in 2D space (XY plane)
        /// </summary>
        /// <param name="transform">Transform to modify</param>
        /// <param name="target">Target transform to look at</param>
        /// <exception cref="System.ArgumentNullException">Thrown when transform or target is null</exception>
        public static void LookAt2D(this Transform transform, Transform target)
        {
            if (target == null)
                throw new System.ArgumentNullException(nameof(target));

            transform.LookAt2D(target.position);
        }

        /// <summary>
        /// Sets specific components of the transform's local position
        /// </summary>
        /// <param name="transform">Transform to modify</param>
        /// <param name="x">Optional X coordinate to set</param>
        /// <param name="y">Optional Y coordinate to set</param>
        /// <param name="z">Optional Z coordinate to set</param>
        /// <exception cref="System.ArgumentNullException">Thrown when transform is null</exception>
        public static void SetLocalPosition(this Transform transform, float? x = null, float? y = null, float? z = null)
        {
            if (transform == null)
                throw new System.ArgumentNullException(nameof(transform));

            Vector3 localPosition = transform.localPosition;
            transform.localPosition = new Vector3(
                x ?? localPosition.x,
                y ?? localPosition.y,
                z ?? localPosition.z
            );
        }

        /// <summary>
        /// Sets specific components of the transform's local rotation
        /// </summary>
        /// <param name="transform">Transform to modify</param>
        /// <param name="x">Optional X rotation to set (in degrees)</param>
        /// <param name="y">Optional Y rotation to set (in degrees)</param>
        /// <param name="z">Optional Z rotation to set (in degrees)</param>
        /// <exception cref="System.ArgumentNullException">Thrown when transform is null</exception>
        public static void SetLocalRotation(this Transform transform, float? x = null, float? y = null, float? z = null)
        {
            if (transform == null)
                throw new System.ArgumentNullException(nameof(transform));

            Vector3 eulerAngles = transform.localEulerAngles;
            transform.localRotation = Quaternion.Euler(
                x ?? eulerAngles.x,
                y ?? eulerAngles.y,
                z ?? eulerAngles.z
            );
        }

        /// <summary>
        /// Resets the transform's local position, rotation, and scale to their default values
        /// </summary>
        /// <param name="transform">Transform to reset</param>
        /// <exception cref="System.ArgumentNullException">Thrown when transform is null</exception>
        public static void ResetLocalTransform(this Transform transform)
        {
            if (transform == null)
                throw new System.ArgumentNullException(nameof(transform));

            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        /// <summary>
        /// Copies position, rotation, and scale from one transform to another
        /// </summary>
        /// <param name="source">Source transform to copy from</param>
        /// <param name="target">Target transform to copy to</param>
        /// <exception cref="System.ArgumentNullException">Thrown when source or target is null</exception>
        public static void CopyTransform(this Transform source, Transform target)
        {
            if (source == null)
                throw new System.ArgumentNullException(nameof(source));
            if (target == null)
                throw new System.ArgumentNullException(nameof(target));

            target.position = source.position;
            target.rotation = source.rotation;
            target.localScale = source.localScale;
        }

        /// <summary>
        /// Destroys all child objects of the transform
        /// </summary>
        /// <param name="transform">Transform to clear</param>
        /// <exception cref="System.ArgumentNullException">Thrown when transform is null</exception>
        public static void DestroyChildren(this Transform transform)
        {
            if (transform == null)
                throw new System.ArgumentNullException(nameof(transform));

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (Application.isPlaying)
                    Object.Destroy(child.gameObject);
                else
                    Object.DestroyImmediate(child.gameObject);
            }
        }
    }
}
