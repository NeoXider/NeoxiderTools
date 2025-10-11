using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    [AddComponentMenu("Neoxider/Tools/AdvancedAttackCollider")]
    public class AdvancedAttackCollider : MonoBehaviour
    {
        [Header("Настройки атаки")]
        [SerializeField] private int attackDamage = 10; // Урон от атаки по умолчанию
        public float triggerDuration = 0.2f; // Длительность активации триггера

        [Header("Настройки коллайдера")]
        [SerializeField] private bool disableColliderAfterAttack; // Если true, коллайдер будет выключен после атаки
        [SerializeField] private Collider2D collider2D; // 2D коллайдер
        [SerializeField] private Collider collider3D; // 3D коллайдер

        [Header("Фильтрация целей")]
        public LayerMask hittableLayers = -1; // Слои, на которые реагирует атака. По умолчанию - все

        [Header("Применение силы")]
        public bool applyForceOnHit; // Применять силу при попадании
        public float forceMagnitude = 20f; // Величина силы
        public float forceDuration = 0.3f; // Длительность действия силы

        [Header("Эффекты")]
        public GameObject attackEffectPrefab; // Префаб эффекта атаки

        [Header("События")]
        public UnityEvent<Collider2D> OnAttackTriggerEnter2D; // Событие при попадании в 2D
        public UnityEvent<Collider> OnAttackTriggerEnter3D; // Событие при попадании в 3D
        public UnityEvent OnDeactivateTrigger; // Событие при деактивации триггера

        private readonly HashSet<Collider2D> hitColliders2D = new();
        private readonly HashSet<Collider> hitColliders3D = new();
        private int _currentDamage;

        public int AttackDamage
        {
            get => attackDamage;
            set => attackDamage = value;
        }

        private void Awake()
        {
            EnableCollider(false); // Коллайдеры отключены при запуске
        }

        private void OnTriggerEnter(Collider collision)
        {
            // Проверка на null, повторное попадание или неверный слой
            if (collider3D == null || hitColliders3D.Contains(collision) || (hittableLayers.value & (1 << collision.gameObject.layer)) == 0)
                return;

            hitColliders3D.Add(collision);
            HandleAttack(collision.gameObject, collision.ClosestPoint(transform.position));
            OnAttackTriggerEnter3D?.Invoke(collision);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            // Проверка на null, повторное попадание или неверный слой
            if (collider2D == null || hitColliders2D.Contains(collision) || (hittableLayers.value & (1 << collision.gameObject.layer)) == 0)
                return;

            hitColliders2D.Add(collision);
            HandleAttack(collision.gameObject, collision.ClosestPoint(transform.position));
            OnAttackTriggerEnter2D?.Invoke(collision);
        }

        /// <summary>
        /// Активирует триггер атаки на заданное время.
        /// </summary>
        /// <param name="damage">Переопределяет урон для этой конкретной атаки. Если -1, используется урон по умолчанию.</param>
        public void ActivateTrigger(int damage = -1)
        {
            _currentDamage = damage;
            hitColliders2D.Clear();
            hitColliders3D.Clear();
            EnableCollider(true);
            Invoke(nameof(DeactivateTrigger), triggerDuration);
        }

        private void HandleAttack(GameObject target, Vector3 contactPosition)
        {
            int finalDamage = (_currentDamage == -1) ? attackDamage : _currentDamage;

            if (target.TryGetComponent(out IDamageable damageable))
                damageable.TakeDamage(finalDamage);

            if (applyForceOnHit)
                ApplyForceToTarget(target);

            if (attackEffectPrefab != null)
                Instantiate(attackEffectPrefab, contactPosition, Quaternion.identity);
        }

        private void ApplyForceToTarget(GameObject target)
        {
            var direction = (target.transform.position - transform.position).normalized;
            if (target.TryGetComponent(out AdvancedForceApplier forceApplier))
                forceApplier.ApplyForce(forceMagnitude, direction);
        }

        private void DeactivateTrigger()
        {
            if (disableColliderAfterAttack)
            {
                EnableCollider(false);
            }
            OnDeactivateTrigger?.Invoke();
        }

        private void EnableCollider(bool enable)
        {
            if (collider2D != null) collider2D.enabled = enable;
            if (collider3D != null) collider3D.enabled = enable;
        }

        private void OnDrawGizmos()
        {
            bool isEnabled = (collider2D != null && collider2D.enabled) || (collider3D != null && collider3D.enabled);
            if (!isEnabled) return;

            Gizmos.color = Color.red;
            Gizmos.matrix = transform.localToWorldMatrix;

            if (collider2D != null)
            {
                if (collider2D is BoxCollider2D box)
                    Gizmos.DrawCube(box.offset, box.size);
                else if (collider2D is CircleCollider2D circle)
                    Gizmos.DrawSphere(circle.offset, circle.radius);
            }

            if (collider3D != null)
            {
                if (collider3D is BoxCollider box)
                    Gizmos.DrawCube(box.center, box.size);
                else if (collider3D is SphereCollider sphere)
                    Gizmos.DrawSphere(sphere.center, sphere.radius);
            }
        }
    }
}
