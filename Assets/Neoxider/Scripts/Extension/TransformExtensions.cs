using UnityEngine;

namespace Neo
{
    /// <summary>
    /// Extension methods for Unity Transform component
    /// </summary>
    public static class TransformExtensions
    {
        #region Position Methods

        /// <summary>
        /// Sets the transform's position with optional individual components
        /// </summary>
        /// <param name="transform">Transform to modify</param>
        /// <param name="position">Optional new position vector</param>
        /// <param name="x">Optional X coordinate</param>
        /// <param name="y">Optional Y coordinate</param>
        /// <param name="z">Optional Z coordinate</param>
        public static void SetPosition(this Transform transform, Vector3? position = null, float? x = null, float? y = null, float? z = null)
        {
            if (transform == null) return;

            Vector3 newPosition = position ?? transform.position;
            if (x.HasValue) newPosition.x = x.Value;
            if (y.HasValue) newPosition.y = y.Value;
            if (z.HasValue) newPosition.z = z.Value;

            transform.position = newPosition;
        }

        /// <summary>
        /// Adds to the transform's position with optional individual components
        /// </summary>
        /// <param name="transform">Transform to modify</param>
        /// <param name="delta">Optional position delta vector</param>
        /// <param name="x">Optional X delta</param>
        /// <param name="y">Optional Y delta</param>
        /// <param name="z">Optional Z delta</param>
        public static void AddPosition(this Transform transform, Vector3? delta = null, float? x = null, float? y = null, float? z = null)
        {
            if (transform == null) return;

            Vector3 currentPosition = transform.position;
            Vector3 finalDelta = delta ?? Vector3.zero;
            if (x.HasValue) finalDelta.x += x.Value;
            if (y.HasValue) finalDelta.y += y.Value;
            if (z.HasValue) finalDelta.z += z.Value;

            transform.position = currentPosition + finalDelta;
        }

        #endregion

        #region Local Position Methods

        /// <summary>
        /// Sets the transform's local position with optional individual components
        /// </summary>
        /// <param name="transform">Transform to modify</param>
        /// <param name="position">Optional new local position vector</param>
        /// <param name="x">Optional X coordinate</param>
        /// <param name="y">Optional Y coordinate</param>
        /// <param name="z">Optional Z coordinate</param>
        public static void SetLocalPosition(this Transform transform, Vector3? position = null, float? x = null, float? y = null, float? z = null)
        {
            if (transform == null) return;

            Vector3 newPosition = position ?? transform.localPosition;
            if (x.HasValue) newPosition.x = x.Value;
            if (y.HasValue) newPosition.y = y.Value;
            if (z.HasValue) newPosition.z = z.Value;

            transform.localPosition = newPosition;
        }

        /// <summary>
        /// Adds to the transform's local position with optional individual components
        /// </summary>
        /// <param name="transform">Transform to modify</param>
        /// <param name="delta">Optional local position delta vector</param>
        /// <param name="x">Optional X delta</param>
        /// <param name="y">Optional Y delta</param>
        /// <param name="z">Optional Z delta</param>
        public static void AddLocalPosition(this Transform transform, Vector3? delta = null, float? x = null, float? y = null, float? z = null)
        {
            if (transform == null) return;

            Vector3 currentPosition = transform.localPosition;
            Vector3 finalDelta = delta ?? Vector3.zero;
            if (x.HasValue) finalDelta.x += x.Value;
            if (y.HasValue) finalDelta.y += y.Value;
            if (z.HasValue) finalDelta.z += z.Value;

            transform.localPosition = currentPosition + finalDelta;
        }

        #endregion

        #region Rotation Methods

        /// <summary>
        /// Sets the transform's rotation with optional individual components
        /// </summary>
        /// <param name="transform">Transform to modify</param>
        /// <param name="rotation">Optional new rotation quaternion</param>
        /// <param name="eulerAngles">Optional new euler angles</param>
        /// <param name="x">Optional X rotation in degrees</param>
        /// <param name="y">Optional Y rotation in degrees</param>
        /// <param name="z">Optional Z rotation in degrees</param>
        public static void SetRotation(this Transform transform, Quaternion? rotation = null, Vector3? eulerAngles = null, float? x = null, float? y = null, float? z = null)
        {
            if (transform == null) return;

            if (rotation.HasValue)
            {
                transform.rotation = rotation.Value;
                return;
            }

            Vector3 angles = eulerAngles ?? transform.eulerAngles;
            if (x.HasValue) angles.x = x.Value;
            if (y.HasValue) angles.y = y.Value;
            if (z.HasValue) angles.z = z.Value;

            transform.rotation = Quaternion.Euler(angles);
        }

        /// <summary>
        /// Adds to the transform's rotation with optional individual components
        /// </summary>
        /// <param name="transform">Transform to modify</param>
        /// <param name="delta">Optional rotation delta quaternion</param>
        /// <param name="eulerDelta">Optional euler angles delta</param>
        /// <param name="x">Optional X rotation delta in degrees</param>
        /// <param name="y">Optional Y rotation delta in degrees</param>
        /// <param name="z">Optional Z rotation delta in degrees</param>
        public static void AddRotation(this Transform transform, Quaternion? delta = null, Vector3? eulerDelta = null, float? x = null, float? y = null, float? z = null)
        {
            if (transform == null) return;

            if (delta.HasValue)
            {
                transform.rotation *= delta.Value;
                return;
            }

            Vector3 currentAngles = transform.eulerAngles;
            Vector3 finalDelta = eulerDelta ?? Vector3.zero;
            if (x.HasValue) finalDelta.x += x.Value;
            if (y.HasValue) finalDelta.y += y.Value;
            if (z.HasValue) finalDelta.z += z.Value;

            transform.rotation = Quaternion.Euler(currentAngles + finalDelta);
        }

        #endregion

        #region Local Rotation Methods

        /// <summary>
        /// Sets the transform's local rotation with optional individual components
        /// </summary>
        /// <param name="transform">Transform to modify</param>
        /// <param name="rotation">Optional new local rotation quaternion</param>
        /// <param name="eulerAngles">Optional new local euler angles</param>
        /// <param name="x">Optional X rotation in degrees</param>
        /// <param name="y">Optional Y rotation in degrees</param>
        /// <param name="z">Optional Z rotation in degrees</param>
        public static void SetLocalRotation(this Transform transform, Quaternion? rotation = null, Vector3? eulerAngles = null, float? x = null, float? y = null, float? z = null)
        {
            if (transform == null) return;

            if (rotation.HasValue)
            {
                transform.localRotation = rotation.Value;
                return;
            }

            Vector3 angles = eulerAngles ?? transform.localEulerAngles;
            if (x.HasValue) angles.x = x.Value;
            if (y.HasValue) angles.y = y.Value;
            if (z.HasValue) angles.z = z.Value;

            transform.localRotation = Quaternion.Euler(angles);
        }

        /// <summary>
        /// Adds to the transform's local rotation with optional individual components
        /// </summary>
        /// <param name="transform">Transform to modify</param>
        /// <param name="delta">Optional local rotation delta quaternion</param>
        /// <param name="eulerDelta">Optional local euler angles delta</param>
        /// <param name="x">Optional X rotation delta in degrees</param>
        /// <param name="y">Optional Y rotation delta in degrees</param>
        /// <param name="z">Optional Z rotation delta in degrees</param>
        public static void AddLocalRotation(this Transform transform, Quaternion? delta = null, Vector3? eulerDelta = null, float? x = null, float? y = null, float? z = null)
        {
            if (transform == null) return;

            if (delta.HasValue)
            {
                transform.localRotation *= delta.Value;
                return;
            }

            Vector3 currentAngles = transform.localEulerAngles;
            Vector3 finalDelta = eulerDelta ?? Vector3.zero;
            if (x.HasValue) finalDelta.x += x.Value;
            if (y.HasValue) finalDelta.y += y.Value;
            if (z.HasValue) finalDelta.z += z.Value;

            transform.localRotation = Quaternion.Euler(currentAngles + finalDelta);
        }

        #endregion

        #region Scale Methods

        /// <summary>
        /// Sets the transform's scale with optional individual components
        /// </summary>
        /// <param name="transform">Transform to modify</param>
        /// <param name="scale">Optional new scale vector</param>
        /// <param name="x">Optional X scale</param>
        /// <param name="y">Optional Y scale</param>
        /// <param name="z">Optional Z scale</param>
        public static void SetScale(this Transform transform, Vector3? scale = null, float? x = null, float? y = null, float? z = null)
        {
            if (transform == null) return;

            Vector3 newScale = scale ?? transform.localScale;
            if (x.HasValue) newScale.x = x.Value;
            if (y.HasValue) newScale.y = y.Value;
            if (z.HasValue) newScale.z = z.Value;

            transform.localScale = newScale;
        }

        /// <summary>
        /// Adds to the transform's scale with optional individual components
        /// </summary>
        /// <param name="transform">Transform to modify</param>
        /// <param name="delta">Optional scale delta vector</param>
        /// <param name="x">Optional X scale delta</param>
        /// <param name="y">Optional Y scale delta</param>
        /// <param name="z">Optional Z scale delta</param>
        public static void AddScale(this Transform transform, Vector3? delta = null, float? x = null, float? y = null, float? z = null)
        {
            if (transform == null) return;

            Vector3 currentScale = transform.localScale;
            Vector3 finalDelta = delta ?? Vector3.zero;
            if (x.HasValue) finalDelta.x += x.Value;
            if (y.HasValue) finalDelta.y += y.Value;
            if (z.HasValue) finalDelta.z += z.Value;

            transform.localScale = currentScale + finalDelta;
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Resets the transform's position, rotation, and scale to default values
        /// </summary>
        /// <param name="transform">Transform to reset</param>
        public static void ResetTransform(this Transform transform)
        {
            if (transform == null) return;

            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        /// <summary>
        /// Resets the transform's local position, rotation, and scale to default values
        /// </summary>
        /// <param name="transform">Transform to reset</param>
        public static void ResetLocalTransform(this Transform transform)
        {
            if (transform == null) return;

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

        #endregion
    }
}
