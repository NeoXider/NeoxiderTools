using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    public class AttackCollider : MonoBehaviour
    {
        public int attackDamage = 1;

        [SerializeField] private bool _useIDamageable = true;
        [SerializeField] private float _triggerDuration = 0.2f;

        [SerializeField, GetComponent] private Collider2D _collider2D;
        [SerializeField, GetComponent] private Collider _collider3D;

        private HashSet<Collider2D> _hitColliders2D = new HashSet<Collider2D>();
        private HashSet<Collider> _hitColliders3D = new HashSet<Collider>();

        [Space]
        [Space]
        [SerializeField] private Transform _mainTransform;
        [Header("Apply force")]
        [SerializeField] private bool _applyForceCollide = false;
        [SerializeField] private ForceApplier _forceApplier;
        public float force = 20f;
        public float forceTime = 0.3f;

        [Space]
        [Header("Object push attack")]
        [SerializeField] private bool applyForce = false;

        public float pushForce = 3;
        public float pushForceTime = 0.3f;

        [Header("Efx")]
        public GameObject efxAttack;

        [Space]

        public UnityEvent<Collider2D> OnAttackTriggerEnter2D;
        public UnityEvent<Collider> OnAttackTriggerEnter3D;
        public UnityEvent OnDeactivateTrigger;

        void Start()
        {
            EnableCollider(false);
        }

        public void ActivateTrigger()
        {
            _hitColliders2D.Clear();
            _hitColliders3D.Clear();

            EnableCollider(true);

            if (!_applyForceCollide)
                ApplyForceInvoke();

            Invoke(nameof(DeactivateTrigger), _triggerDuration);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (_collider2D == null || _hitColliders2D.Contains(collision))
                return;

            _hitColliders2D.Add(collision);

            if (_useIDamageable)
            {
                var healthComponent = collision.GetComponent<IDamageable>();
                if (healthComponent != null)
                {
                    healthComponent.TakeDamage(attackDamage);
                }
            }

            ApplyForce(collision.gameObject);
            Vector2 contactPosition = collision.ClosestPoint(_mainTransform.position);
            PlayAttackEffectAtContactPoint(contactPosition);

            OnAttackTriggerEnter2D?.Invoke(collision);
        }

        private void ApplyForce(GameObject gameObject)
        {
            if (applyForce)
            {
                if (gameObject.TryGetComponent(out ForceApplier forceApplier))
                {
                    forceApplier.ApplyForceInDirection(GetDirection(gameObject.transform.position), pushForce, pushForceTime);
                }
            }

            if (_applyForceCollide)
                ApplyForceInvoke(gameObject);
        }

        private void ApplyForceInvoke(GameObject gameObject = null)
        {
            Vector3 direction = GetDirection(gameObject != null ? gameObject.transform.position : transform.position);

            if (_forceApplier != null)
            {
                _forceApplier.ApplyForceInDirection(direction, force, forceTime);
            }
        }

        private void OnTriggerEnter(Collider collision)
        {
            if (_collider3D == null || _hitColliders3D.Contains(collision))
                return;

            _hitColliders3D.Add(collision);

            if (_useIDamageable)
            {
                var healthComponent = collision.GetComponent<IDamageable>();
                if (healthComponent != null)
                {
                    healthComponent.TakeDamage(attackDamage);
                }
            }

            ApplyForce(collision.gameObject);
            Vector3 contactPosition = collision.ClosestPoint(_mainTransform.position);
            PlayAttackEffectAtContactPoint(contactPosition);

            OnAttackTriggerEnter3D?.Invoke(collision);
        }

        private Vector3 GetDirection(Vector3 targetPosition, bool revert = false)
        {
            return (targetPosition - _mainTransform.position).normalized * (revert ? -1 : 1);

        }

        private void DeactivateTrigger()
        {
            EnableCollider(false);
            OnDeactivateTrigger?.Invoke();
        }

        private void EnableCollider(bool enable)
        {
            if (_collider2D != null)
                _collider2D.enabled = enable;

            if (_collider3D != null)
                _collider3D.enabled = enable;
        }

        private void PlayAttackEffectAtContactPoint(Vector3 contactPosition)
        {
            if (efxAttack != null)
            {
                Instantiate(efxAttack, contactPosition, Quaternion.identity);
            }
        }
    }
}