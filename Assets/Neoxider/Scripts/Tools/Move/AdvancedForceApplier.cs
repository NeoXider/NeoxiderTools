using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    [AddComponentMenu("Neoxider/Tools/AdvancedForceApplier")]
    public class AdvancedForceApplier : MonoBehaviour
    {
        [Header("Компоненты")]
        [SerializeField] private Rigidbody rigidbody3D;
        [SerializeField] private Rigidbody2D rigidbody2D;

        [Header("Настройки силы")]
        [SerializeField] private float defaultForce = 10f;
        [SerializeField] private ForceMode forceMode3D = ForceMode.Impulse;
        [SerializeField] private ForceMode2D forceMode2D = ForceMode2D.Impulse;

        [Header("Направление силы")]
        [SerializeField] private bool useVelocityDirection = true; // Использовать направление скорости
        [SerializeField] private bool isDirectionForward = true;   // Применять силу вперёд или назад
        [SerializeField] private Vector3 customDirection = Vector3.forward; // Пользовательское направление

        [Header("События")]
        public UnityEvent OnApplyForce;

        private void Awake()
        {
            // Автоматическое определение компонентов, если не заданы
            if (rigidbody3D == null) rigidbody3D = GetComponent<Rigidbody>();
            if (rigidbody2D == null) rigidbody2D = GetComponent<Rigidbody2D>();
        }

        /// <summary>
        /// Применяет силу к телу.
        /// </summary>
        /// <param name="force">Величина силы (если 0, используется defaultForce)</param>
        /// <param name="direction">Направление силы (если null, используется GetDirection())</param>
        public void ApplyForce(float force = 0f, Vector3? direction = null)
        {
            float appliedForce = (force > 0f) ? force : defaultForce;
            Vector3 appliedDirection = direction ?? GetDirection();

            if (rigidbody3D != null)
            {
                rigidbody3D.AddForce(appliedDirection * appliedForce, forceMode3D);
            }
            else if (rigidbody2D != null)
            {
                rigidbody2D.AddForce(appliedDirection * appliedForce, forceMode2D);
            }

            OnApplyForce?.Invoke();
        }

        /// <summary>
        /// Получает направление для применения силы.
        /// </summary>
        private Vector3 GetDirection()
        {
            if (useVelocityDirection)
            {
                Vector3 velocity = rigidbody3D != null ? rigidbody3D.velocity : (Vector3)rigidbody2D.velocity;
                if (velocity != Vector3.zero)
                {
                    return isDirectionForward ? velocity.normalized : -velocity.normalized;
                }
            }
            return customDirection.normalized;
        }
    }
}