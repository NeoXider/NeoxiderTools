using System;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    [AddComponentMenu("Neoxider/Tools/ForceApplier")]
    public class ForceApplier : MonoBehaviour
    {
        [SerializeField, GetComponent] private MoveSimple _moveSimple;

        public float force = 10f;
        public float forceTime = 0;
        public ForceMode forceMode = ForceMode.Impulse;
        public ForceMode2D forceMode2D = ForceMode2D.Impulse;
        public Vector3 direction;
        [SerializeField] private bool _isDirectionForward = true;
        [SerializeField, GetComponent] private Rigidbody _rigidbody3D;
        [SerializeField, GetComponent] private Rigidbody2D _rigidbody2D;

        [Space]
        public bool active = true;

        public bool isForce = false;

        public UnityEvent OnStartForce;
        public UnityEvent OnStopForce;

        private void Awake()
        {
            if (_moveSimple != null)
            {
                OnStartForce.AddListener(_moveSimple.StopMove);
                OnStopForce.AddListener(_moveSimple.StartMove);
            }
        }

        private void OnDestroy()
        {
            if (_moveSimple != null)
            {
                OnStartForce.RemoveListener(_moveSimple.StopMove);
                OnStopForce.RemoveListener(_moveSimple.StartMove);
            }
        }

        private Vector3 GetDirection()
        {
            Vector3 velocity = _rigidbody3D != null ? _rigidbody3D.velocity : (_rigidbody2D != null ? (Vector3)_rigidbody2D.velocity : Vector3.zero);

            Vector3 dir = _isDirectionForward ? velocity.normalized : -velocity.normalized;
            if (direction != Vector3.zero)
                dir = _isDirectionForward ? velocity.normalized : -velocity.normalized;
            return dir;
        }

        public void ApplyForce()
        {
            ApplyForce(force);
        }

        public void ApplyForce(float force)
        {
            Vector3 dir = GetDirection();

            ApplyForceInDirection(dir, force);
        }

        public void ApplyForceInDirection(Vector3 direction, float force = 0, float forceTime = 0)
        {
            if (!active) return;

            CancelInvoke(nameof(StopForce));

            float curForce = force > 0 ? force : this.force;
            float curTime = forceTime > 0 ? forceTime : this.forceTime;

            print("force: " + curForce + ", time:" + curTime);

            isForce = true;

            OnStartForce?.Invoke();

            if (curTime > 0)
                Invoke(nameof(StopForce), curTime);

            if (_rigidbody3D != null)
            {
                _rigidbody3D.AddForce(direction * curForce, forceMode);
            }

            if (_rigidbody2D != null)
            {
                _rigidbody2D.AddForce(direction * curForce, forceMode2D);
            }
        }

        private void StopForce()
        {
            if (isForce)
                OnStopForce?.Invoke();
        }
    }
}