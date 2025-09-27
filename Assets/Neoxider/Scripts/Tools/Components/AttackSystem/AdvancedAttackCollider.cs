using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    [AddComponentMenu("Neoxider/Tools/AdvancedAttackCollider")]
    public class AdvancedAttackCollider : MonoBehaviour
    {
        [Header("Настройки атаки")] [SerializeField]
        private int attackDamage = 10; // Урон от атаки

        [SerializeField] private float triggerDuration = 0.2f; // Длительность активации триггера

        [Header("Настройки коллайдера")] [SerializeField]
        private Collider2D collider2D; // 2D коллайдер

        [SerializeField] private Collider collider3D; // 3D коллайдер

        [Header("Применение силы")] [SerializeField]
        private bool applyForceOnHit; // Применять силу при попадании

        [SerializeField] private float forceMagnitude = 20f; // Величина силы
        [SerializeField] private float forceDuration = 0.3f; // Длительность действия силы

        [Header("Эффекты")] [SerializeField] private GameObject attackEffectPrefab; // Префаб эффекта атаки

        [Header("События")] public UnityEvent<Collider2D> OnAttackTriggerEnter2D; // Событие при попадании в 2D
        public UnityEvent<Collider> OnAttackTriggerEnter3D; // Событие при попадании в 3D
        public UnityEvent OnDeactivateTrigger; // Событие при деактивации триггера

        private readonly HashSet<Collider2D> hitColliders2D = new(); // Отслеживание 2D попаданий
        private readonly HashSet<Collider> hitColliders3D = new(); // Отслеживание 3D попаданий

        public int AttackDamage
        {
            get => attackDamage;
            set => attackDamage = value;
        }

        private void Start()
        {
            EnableCollider(false); // Коллайдеры отключены по умолчанию
        }

        private void OnTriggerEnter(Collider collision)
        {
            if (collider3D == null || hitColliders3D.Contains(collision))
                return;

            hitColliders3D.Add(collision);
            HandleAttack(collision.gameObject, collision.ClosestPoint(transform.position));
            OnAttackTriggerEnter3D?.Invoke(collision);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collider2D == null || hitColliders2D.Contains(collision))
                return;

            hitColliders2D.Add(collision);
            HandleAttack(collision.gameObject, collision.ClosestPoint(transform.position));
            OnAttackTriggerEnter2D?.Invoke(collision);
        }

        /// <summary>
        ///     Активирует триггер атаки на заданное время
        /// </summary>
        public void ActivateTrigger()
        {
            hitColliders2D.Clear();
            hitColliders3D.Clear();
            EnableCollider(true);
            Invoke(nameof(DeactivateTrigger), triggerDuration);
        }

        /// <summary>
        ///     Обрабатывает атаку: урон, силу и эффекты
        /// </summary>
        private void HandleAttack(GameObject target, Vector3 contactPosition)
        {
            // Нанесение урона
            if (target.TryGetComponent(out IDamageable damageable)) damageable.TakeDamage(attackDamage);

            // Применение силы, если включено
            if (applyForceOnHit) ApplyForceToTarget(target);

            // Проигрывание эффекта
            if (attackEffectPrefab != null) Instantiate(attackEffectPrefab, contactPosition, Quaternion.identity);
        }

        /// <summary>
        ///     Применяет силу к цели
        /// </summary>
        private void ApplyForceToTarget(GameObject target)
        {
            var direction = (target.transform.position - transform.position).normalized;
            if (target.TryGetComponent(out AdvancedForceApplier forceApplier))
                forceApplier.ApplyForce(forceMagnitude, direction);
        }

        /// <summary>
        ///     Деактивирует триггер
        /// </summary>
        private void DeactivateTrigger()
        {
            EnableCollider(false);
            OnDeactivateTrigger?.Invoke();
        }

        /// <summary>
        ///     Включает или отключает коллайдеры
        /// </summary>
        private void EnableCollider(bool enable)
        {
            if (collider2D != null) collider2D.enabled = enable;
            if (collider3D != null) collider3D.enabled = enable;
        }
    }
}