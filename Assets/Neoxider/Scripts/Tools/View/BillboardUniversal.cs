using UnityEngine;

namespace Neo
{
    namespace Tools
    {
        [NeoDoc("Tools/View/BillboardUniversal.md")]
        [AddComponentMenu("Neo/" + "Tools/" + nameof(BillboardUniversal))]
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

            private void Start()
            {
                if (targetCamera == null)
                {
                    targetCamera = Camera.main;
                }
            }

            private void LateUpdate()
            {
                SetRotation();
            }

            private void OnValidate()
            {
                targetCamera ??= Camera.main;
                SetRotation();
            }

            private void SetRotation()
            {
                if (targetCamera == null)
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
            }
        }
    }
}