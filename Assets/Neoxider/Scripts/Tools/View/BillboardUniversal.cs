using UnityEngine;

namespace Neo.Tools
{
    [NeoDoc("Tools/View/BillboardUniversal.md")]
    [CreateFromMenu("Neoxider/Tools/View/BillboardUniversal")]
    [AddComponentMenu("Neoxider/" + "Tools/" + nameof(BillboardUniversal))]
    public class BillboardUniversal : MonoBehaviour
    {
        public enum BillboardMode
        {
            TowardsCamera,
            AwayFromCamera,
            TowardsDirection
        }

        [Header("References")] [SerializeField]
        private Camera targetCamera;

        [Header("Settings")] [SerializeField] private BillboardMode billboardMode = BillboardMode.AwayFromCamera;
        [SerializeField] private bool ignoreY = true;
        [SerializeField] private Vector3 customDirection = Vector3.forward;

        [Header("Fallback")]
        [Tooltip(
            "Resolve Camera.main only when Target Camera is empty. Disable when the camera is injected by scene setup.")]
        [SerializeField]
        private bool useMainCameraFallback = true;

        [SerializeField] private bool logMissingCamera;
        private bool _missingCameraLogged;

        private void Start()
        {
            ResolveCamera();
        }

        private void LateUpdate()
        {
            ApplyRotation(ResolveCamera());
        }

#if UNITY_EDITOR
        /// <summary>
        /// Explicit edit-mode preview. There is deliberately no <c>OnValidate</c>: Unity calls it on
        /// prefab assets as they load, and rotating there baked a <c>Camera.main</c>-dependent rotation
        /// into every prefab that holds this component the next time the project saved.
        /// </summary>
        [ContextMenu("Face Camera Now")]
        private void FaceCameraNowInEditor()
        {
            Camera camera = targetCamera != null ? targetCamera : useMainCameraFallback ? Camera.main : null;
            UnityEditor.Undo.RecordObject(transform, "Billboard Face Camera");
            ApplyRotation(camera);
        }
#endif

        private void ApplyRotation(Camera camera)
        {
            if (camera == null)
            {
                return;
            }

            Vector3 direction = GetDirection(camera);
            if (ignoreY)
            {
                direction.y = 0;
            }

            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }

        private Vector3 GetDirection(Camera camera)
        {
            return billboardMode switch
            {
                BillboardMode.TowardsCamera => camera.transform.position - transform.position,
                BillboardMode.AwayFromCamera => transform.position - camera.transform.position,
                BillboardMode.TowardsDirection => customDirection,
                _ => Vector3.zero
            };
        }

        public void SetCustomDirection(Vector3 direction)
        {
            customDirection = direction;
        }

        public void SetBillboardMode(BillboardMode mode)
        {
            billboardMode = mode;
        }

        public void SetIgnoreY(bool ignore)
        {
            ignoreY = ignore;
        }

        public void SetTargetCamera(Camera camera)
        {
            targetCamera = camera;
            _missingCameraLogged = false;
        }

        private Camera ResolveCamera()
        {
            if (targetCamera != null)
            {
                return targetCamera;
            }

            if (useMainCameraFallback)
            {
                targetCamera = Camera.main;
            }

            if (targetCamera == null && logMissingCamera && !_missingCameraLogged)
            {
                _missingCameraLogged = true;
                NeoDiagnostics.LogWarning($"[{nameof(BillboardUniversal)}] Target camera is not assigned.", this);
            }

            return targetCamera;
        }
    }
}
