using UnityEngine;


namespace Neo
{
    public static class TransformExtensions
    {
        public static void AddPosition(this Transform transform, Vector3? deltaPosition = null, float? x = null, float? y = null, float? z = null)
        {
            Vector3 currentPosition = transform.position;

            Vector3 finalDelta = deltaPosition ?? Vector3.zero;
            finalDelta.x += x ?? 0;
            finalDelta.y += y ?? 0;
            finalDelta.z += z ?? 0;

            transform.position = currentPosition + finalDelta;
        }

        public static void AddRotate(this Transform transform, Vector3? deltaRotation = null, float? x = null, float? y = null, float? z = null)
        {
            Vector3 finalDelta = deltaRotation ?? Vector3.zero;
            finalDelta.x += x ?? 0;
            finalDelta.y += y ?? 0;
            finalDelta.z += z ?? 0;

            transform.Rotate(finalDelta);
        }

        public static void AddScale(this Transform transform, Vector3? deltaScale = null, float? x = null, float? y = null, float? z = null)
        {
            Vector3 currentScale = transform.localScale;

            Vector3 finalDelta = deltaScale ?? Vector3.zero;
            finalDelta.x += x ?? 0;
            finalDelta.y += y ?? 0;
            finalDelta.z += z ?? 0;

            transform.localScale = currentScale + finalDelta;
        }

        public static void LookAt2D(this Transform transform, Vector3 target)
        {
            Vector3 direction = target - transform.position;
            direction.z = 0;
            transform.up = direction.normalized;
        }

        public static void LookAt2D(this Transform transform, Transform target)
        {
            transform.LookAt2D(target.position);
        }

        public static void SetLocalPosition(this Transform transform, float? x = null, float? y = null, float? z = null)
        {
            Vector3 localPosition = transform.localPosition;
            transform.localPosition = new Vector3(
                x ?? localPosition.x,
                y ?? localPosition.y,
                z ?? localPosition.z
            );
        }

        public static void SetLocalRotation(this Transform transform, float? x = null, float? y = null, float? z = null)
        {
            Vector3 eulerAngles = transform.localEulerAngles;
            transform.localRotation = Quaternion.Euler(
                x ?? eulerAngles.x,
                y ?? eulerAngles.y,
                z ?? eulerAngles.z
            );
        }

        public static void ResetLocalTransform(this Transform transform)
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        public static void CopyTransform(this Transform source, Transform target)
        {
            target.position = source.position;
            target.rotation = source.rotation;
            target.localScale = source.localScale;
        }

        public static void Clear(this Transform transform)
        {
#if UNITY_EDITOR
            GameObject parent = GameObject.Find("Clear");

            if (parent == null)
            {
                parent = GameObject.CreatePrimitive(PrimitiveType.Plane);
                parent.transform.position = new Vector3(-1000, -1000, -1000);
                parent.name = "Clear";
                parent.SetActive(false);
            }
#endif

            while (transform.childCount > 0)
            {
                Transform child = transform.GetChild(0);
#if UNITY_EDITOR
                child.SetParent(parent.transform);
                child.position = parent.transform.position;
#endif
                Object.DestroyImmediate(child.gameObject, true);
            }
        }
    }
}
