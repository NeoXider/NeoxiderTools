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
            SetRotation();
        }

        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                ResolveCamera();
            }

            SetRotation();
        }

        private void SetRotation()
        {
            Camera camera = ResolveCamera();
            if (camera == null)
            {
                return;
            }

            Vector3 direction = GetDirection();
            if (ignoreY)
            {
                direction.y = 0;
            }

            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }

        private Vector3 GetDirection()
        {
            return billboardMode switch
            {
                BillboardMode.TowardsCamera => targetCamera.transform.position - transform.position,
                BillboardMode.AwayFromCamera => transform.position - targetCamera.transform.position,
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
