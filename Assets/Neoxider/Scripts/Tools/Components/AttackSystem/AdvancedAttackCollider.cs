using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    [AddComponentMenu("Neoxider/Tools/AdvancedAttackCollider")]
    public class AdvancedAttackCollider : MonoBehaviour
    {
        [Header("Настройки атаки")] [SerializeField]
        private int attackDamage = 10; // Урон от атаки по умолчанию

        public float triggerDuration = 0.2f; // Длительность активации триггера

        [Header("Настройки авто-управления")] [SerializeField]
        [Tooltip("Если включено, компонент сам включает/выключает коллайдеры на время активации триггера.")]
        private bool autoManageColliders = false; // По умолчанию не управляем коллайдерами автоматически

        [SerializeField] private new Collider2D collider2D; // 2D коллайдер
        [SerializeField] private Collider collider3D; // 3D коллайдер

        [Header("Режимы обработки")]
        [Tooltip("Обрабатывать 2D столкновения/триггеры")] public bool use2D = true;
        [Tooltip("Обрабатывать 3D столкновения/триггеры")] public bool use3D = true;
        [Tooltip("Обрабатывать события триггеров")] public bool useTrigger = true;
        [Tooltip("Обрабатывать события коллизий")] public bool useCollision = true;

        [Header("Фильтрация целей")]
        public LayerMask hittableLayers = -1; // Слои, на которые реагирует атака. По умолчанию - все

        [Header("Применение силы")] public bool applyForceOnHit; // Применять силу при попадании
        public float forceMagnitude = 20f; // Величина силы
        public float forceDuration = 0.3f; // Длительность действия силы (зарезервировано)
        [Tooltip("Режим силы для 3D Rigidbody")] public ForceMode forceMode3D = ForceMode.Impulse;
        [Tooltip("Режим силы для 2D Rigidbody")] public ForceMode2D forceMode2D = ForceMode2D.Impulse;
        [Tooltip("Масштабировать силу по массе Rigidbody")] public bool scaleForceByMass = true;
        [Tooltip("Использовать AdvancedForceApplier как запасной вариант")] public bool useAdvancedForceApplier = true;

        [Header("Эффекты")] public GameObject attackEffectPrefab; // Префаб эффекта атаки

        [Header("Игнор цели")]
        [Tooltip("Список объектов, которым НЕ наносится урон и не триггерится попадание")]
        [SerializeField] private GameObject[] ignoreObjects;

        [Header("Уничтожение при попадании")]
        [Tooltip("Если true — уничтожает этот объект при столкновении/попадании")] public bool destroySelfOnHit = false;
        [Tooltip("Если true — уничтожает объект цели при столкновении/попадании")] public bool destroyTargetOnHit = false;

        [Header("Настройки Gizmo")]
        [SerializeField] private bool _showGizmo = true; // Показывать ли Gizmo в редакторе
        [SerializeField] private Color _gizmoColor = new Color(1f, 0f, 0f, 0.2f); // Цвет Gizmo

        [Header("События")] public UnityEvent<Collider2D> OnAttackTriggerEnter2D; // Событие при попадании в 2D (триггер)
        public UnityEvent<Collider> OnAttackTriggerEnter3D; // Событие при попадании в 3D (триггер)
        public UnityEvent OnDeactivateTrigger; // Событие при деактивации триггера
        [Tooltip("Единое событие попадания: цель GameObject")]
        public UnityEvent<GameObject> OnHit;

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
            _currentDamage = AttackDamage;
            if (collider2D == null) collider2D = GetComponent<Collider2D>();
            if (collider3D == null) collider3D = GetComponent<Collider>();
            if (autoManageColliders)
            {
                EnableCollider(false); // При авто-режиме коллайдеры отключены при запуске
            }
        }

        private bool PassesLayer(int layer)
        {
            return (hittableLayers.value & (1 << layer)) != 0;
        }

        private void OnTriggerEnter(Collider collision)
        {
            if (!use3D || !useTrigger) return;
            // Проверка на null, повторное попадание или неверный слой
            if (collider3D == null || hitColliders3D.Contains(collision) || !PassesLayer(collision.gameObject.layer))
            {
                return;
            }

            hitColliders3D.Add(collision);

            var contactPoint = collision.ClosestPoint(transform.position);
            var approxNormal = (contactPoint - transform.position).normalized;
            HandleAttack(collision.gameObject, contactPoint, approxNormal, false);
            OnAttackTriggerEnter3D?.Invoke(collision);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (!use2D || !useTrigger) return;
            // Проверка на null, повторное попадание или неверный слой
            if (collider2D == null || hitColliders2D.Contains(collision) || !PassesLayer(collision.gameObject.layer))
            {
                return;
            }

            hitColliders2D.Add(collision);

            var contactPoint2D = collision.ClosestPoint(transform.position);
            var approxNormal2D = ((Vector3)contactPoint2D - transform.position).normalized;
            HandleAttack(collision.gameObject, contactPoint2D, approxNormal2D, false);
            OnAttackTriggerEnter2D?.Invoke(collision);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!use3D || !useCollision) return;
            if (collider3D == null || collision.collider == null || hitColliders3D.Contains(collision.collider) || !PassesLayer(collision.gameObject.layer))
            {
                return;
            }

            hitColliders3D.Add(collision.collider);

            var contact = collision.contacts.Length > 0 ? collision.contacts[0].point : collision.transform.position;
            var normal = collision.contacts.Length > 0 ? collision.contacts[0].normal : (collision.transform.position - transform.position).normalized;
            HandleAttack(collision.gameObject, contact, normal, true);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!use2D || !useCollision) return;
            if (collider2D == null || collision.collider == null || hitColliders2D.Contains(collision.collider) || !PassesLayer(collision.gameObject.layer))
            {
                return;
            }

            hitColliders2D.Add(collision.collider);

            var contact = collision.contacts.Length > 0 ? (Vector3)collision.contacts[0].point : (Vector3)collision.transform.position;
            var normal = collision.contacts.Length > 0 ? (Vector3)collision.contacts[0].normal : ((Vector3)collision.transform.position - transform.position).normalized;
            HandleAttack(collision.gameObject, contact, normal, true);
        }

        /// <summary>
        /// Активирует триггер атаки на заданное время.
        /// </summary>
        /// <param name="damage">Переопределяет урон для этой конкретной атаки. Если -1, используется урон по умолчанию.</param>
        public void ActivateTrigger(int damage)
        {
            _currentDamage = damage;
            hitColliders2D.Clear();
            hitColliders3D.Clear();
            if (autoManageColliders)
            {
                EnableCollider(true);
                Invoke(nameof(DeactivateTrigger), triggerDuration);
            }
        }

        public void ActivateTrigger()
        {
            ActivateTrigger(attackDamage);
        }

        private void HandleAttack(GameObject target, Vector3 contactPosition, Vector3 contactNormal, bool hasContactNormal)
        {
            if (ignoreObjects != null && ignoreObjects.Length > 0)
            {
                for (int i = 0; i < ignoreObjects.Length; i++)
                {
                    if (ignoreObjects[i] == null) continue;
                    if (ReferenceEquals(ignoreObjects[i], target))
                    {
                        return;
                    }
                }
            }
            int finalDamage = _currentDamage == -1 ? attackDamage : _currentDamage;

            if (target.TryGetComponent(out IDamageable damageable))
            {
                damageable.TakeDamage(finalDamage);
            }

            if (applyForceOnHit)
            {
                Vector3 direction = hasContactNormal ? contactNormal : (target.transform.position - transform.position).normalized;
                ApplyForceToTarget(target, direction);
            }

            if (attackEffectPrefab != null)
            {
                Instantiate(attackEffectPrefab, contactPosition, Quaternion.identity);
            }

            OnHit?.Invoke(target);

            if (destroyTargetOnHit && target != null)
            {
                Destroy(target);
            }

            if (destroySelfOnHit)
            {
                Destroy(gameObject);
            }
        }

        private void ApplyForceToTarget(GameObject target, Vector3 direction)
        {
            // Если используется AdvancedForceApplier, то силы через Rigidbody НЕ применяем
            if (useAdvancedForceApplier)
            {
                if (target.TryGetComponent(out AdvancedForceApplier forceApplier))
                {
                    forceApplier.ApplyForce(forceMagnitude, direction);
                }
                return;
            }

            if (target.TryGetComponent(out Rigidbody rb3D))
            {
                float magnitude = scaleForceByMass ? forceMagnitude * rb3D.mass : forceMagnitude;
                rb3D.AddForce(direction * magnitude, forceMode3D);
                return;
            }

            if (target.TryGetComponent(out Rigidbody2D rb2D))
            {
                float magnitude = scaleForceByMass ? forceMagnitude * rb2D.mass : forceMagnitude;
                rb2D.AddForce((Vector2)(direction.normalized) * magnitude, forceMode2D);
                return;
            }
        }

        private void DeactivateTrigger()
        {
            if (autoManageColliders)
            {
                EnableCollider(false);
            }

            OnDeactivateTrigger?.Invoke();
        }

        private void EnableCollider(bool enable)
        {
            if (collider2D != null)
            {
                collider2D.enabled = enable;
            }

            if (collider3D != null)
            {
                collider3D.enabled = enable;
            }
        }

        private void OnDrawGizmos()
        {
            if (!_showGizmo) return;

            bool isEnabled = (collider2D != null && collider2D.enabled) || (collider3D != null && collider3D.enabled);
            if (!isEnabled)
            {
                return;
            }

            Gizmos.color = _gizmoColor;
            Gizmos.matrix = transform.localToWorldMatrix;

            if (collider2D != null)
            {
                if (collider2D is BoxCollider2D box)
                {
                    Gizmos.DrawCube(box.offset, box.size);
                }
                else if (collider2D is CircleCollider2D circle)
                {
                    Gizmos.DrawSphere(circle.offset, circle.radius);
                }
            }

            if (collider3D != null)
            {
                if (collider3D is BoxCollider box)
                {
                    Gizmos.DrawCube(box.center, box.size);
                }
                else if (collider3D is SphereCollider sphere)
                {
                    Gizmos.DrawSphere(sphere.center, sphere.radius);
                }
            }
        }
    }
}