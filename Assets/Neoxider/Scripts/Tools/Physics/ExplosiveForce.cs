using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    /// <summary>
    ///     Компонент взрывной силы, который применяет взрывную силу ко всем объектам в заданном радиусе.
    ///     Поддерживает фильтрацию по слоям, опциональное добавление физики и различные режимы активации.
    /// </summary>
    [AddComponentMenu("Neo/" + "Tools/" + "Physics/" + nameof(ExplosiveForce))]
    public class ExplosiveForce : MonoBehaviour
    {
        /// <summary>
        ///     Режим активации взрыва.
        /// </summary>
        public enum ActivationMode
        {
            /// <summary>Активация при старте (Start)</summary>
            OnStart,

            /// <summary>Активация при пробуждении (Awake)</summary>
            OnAwake,

            /// <summary>Активация с задержкой</summary>
            Delayed,

            /// <summary>Только по вызову метода</summary>
            Manual
        }

        /// <summary>
        ///     Тип затухания силы по расстоянию.
        /// </summary>
        public enum FalloffType
        {
            /// <summary>Линейное затухание</summary>
            Linear,

            /// <summary>Квадратичное затухание</summary>
            Quadratic
        }

        /// <summary>
        ///     Режим применения силы.
        /// </summary>
        public enum ForceMode
        {
            /// <summary>AddForce - постоянная сила</summary>
            AddForce,

            /// <summary>AddExplosionForce - взрывная сила с затуханием</summary>
            AddExplosionForce
        }

        [Header("Activation")] [Tooltip("Режим активации взрыва")] [SerializeField]
        private ActivationMode activationMode = ActivationMode.OnStart;

        [Tooltip("Задержка перед взрывом (используется при режиме Delayed)")] [SerializeField]
        private float delay;

        [Header("Explosion Force")] [Tooltip("Базовая сила взрыва")] [Min(0f)] [SerializeField]
        private float force = 100f;

        [Tooltip("Случайная вариация силы (добавляется к базовой силе)")] [SerializeField]
        private float forceRandomness;

        [Tooltip("Режим применения силы")] [SerializeField]
        private ForceMode forceMode = ForceMode.AddExplosionForce;

        [Tooltip("Тип затухания силы по расстоянию")] [SerializeField]
        private FalloffType falloffType = FalloffType.Quadratic;

        [Header("Radius & Filtering")] [Tooltip("Радиус действия взрыва")] [Min(0f)] [SerializeField]
        private float radius = 5f;

        [Tooltip("Слои объектов, на которые будет воздействовать взрыв")] [SerializeField]
        private LayerMask affectedLayers = -1;

        [Header("Options")] [Tooltip("Автоматически добавлять Rigidbody на объекты без физики")] [SerializeField]
        private bool addRigidbodyIfNeeded;

        [Tooltip("Уничтожить этот объект после взрыва")] [SerializeField]
        private bool destroyAfterExplosion;

        [Tooltip("Задержка перед уничтожением (если destroyAfterExplosion = true)")] [SerializeField]
        private float destroyDelay;

        [Tooltip("Вызывается при взрыве")] public UnityEvent OnExplode = new();

        [Tooltip("Вызывается для каждого затронутого объекта")]
        public UnityEvent<GameObject> OnObjectAffected = new();

        /// <summary>
        ///     Получить текущую силу взрыва.
        /// </summary>
        public float CurrentForce => force;

        /// <summary>
        ///     Получить текущий радиус взрыва.
        /// </summary>
        public float CurrentRadius => radius;

        /// <summary>
        ///     Проверка, произошел ли уже взрыв.
        /// </summary>
        public bool HasExploded { get; private set; }

        private void Awake()
        {
            if (activationMode == ActivationMode.OnAwake)
            {
                if (delay > 0f)
                {
                    StartCoroutine(ExplodeDelayed(delay));
                }
                else
                {
                    Explode();
                }
            }
        }

        private void Start()
        {
            if (activationMode == ActivationMode.OnStart)
            {
                if (delay > 0f)
                {
                    StartCoroutine(ExplodeDelayed(delay));
                }
                else
                {
                    Explode();
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, radius);

            Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
            Gizmos.DrawSphere(transform.position, radius);
        }

        /// <summary>
        ///     Вызвать взрыв вручную с базовой силой из компонента.
        /// </summary>
        public void Explode()
        {
            Explode(0f);
        }

        /// <summary>
        ///     Вызвать взрыв вручную с кастомной силой.
        /// </summary>
        /// <param name="customForce">Кастомная сила взрыва (если 0, используется базовая)</param>
        public void Explode(float customForce)
        {
            if (HasExploded && activationMode != ActivationMode.Manual)
            {
                return;
            }

            HasExploded = true;

            float finalForce = customForce > 0f ? customForce : force;
            if (forceRandomness > 0f)
            {
                finalForce += Random.Range(-forceRandomness, forceRandomness);
            }

            Collider[] colliders = Physics.OverlapSphere(transform.position, radius, affectedLayers);

            foreach (Collider col in colliders)
            {
                if (col == null || col.gameObject == gameObject)
                {
                    continue;
                }

                Rigidbody rb = col.attachedRigidbody;
                if (rb == null)
                {
                    if (addRigidbodyIfNeeded)
                    {
                        rb = col.gameObject.AddComponent<Rigidbody>();
                    }
                    else
                    {
                        continue;
                    }
                }

                Vector3 direction = col.transform.position - transform.position;
                float distance = direction.magnitude;

                if (distance < 0.01f)
                {
                    direction = Random.onUnitSphere;
                    distance = 0.01f;
                }

                direction.Normalize();

                float forceAtDistance = CalculateForceAtDistance(distance, finalForce);

                if (forceMode == ForceMode.AddExplosionForce)
                {
                    rb.AddExplosionForce(forceAtDistance, transform.position, radius, 0f,
                        UnityEngine.ForceMode.Impulse);
                }
                else
                {
                    rb.AddForce(direction * forceAtDistance, UnityEngine.ForceMode.Impulse);
                }

                OnObjectAffected?.Invoke(col.gameObject);
            }

            OnExplode?.Invoke();

            if (destroyAfterExplosion)
            {
                if (destroyDelay > 0f)
                {
                    Destroy(gameObject, destroyDelay);
                }
                else
                {
                    Destroy(gameObject);
                }
            }
        }

        /// <summary>
        ///     Установить силу взрыва.
        /// </summary>
        public void SetForce(float newForce)
        {
            force = Mathf.Max(0f, newForce);
        }

        /// <summary>
        ///     Установить радиус взрыва.
        /// </summary>
        public void SetRadius(float newRadius)
        {
            radius = Mathf.Max(0f, newRadius);
        }

        /// <summary>
        ///     Сбросить состояние взрыва (позволяет взорваться снова).
        ///     Полезно для режимов OnStart, OnAwake, Delayed - после автоматического взрыва можно взорваться снова.
        ///     Для режима Manual этот метод не обязателен, так как можно взрываться многократно.
        /// </summary>
        public void ResetExplosion()
        {
            HasExploded = false;
        }

        private float CalculateForceAtDistance(float distance, float baseForce)
        {
            if (distance >= radius)
            {
                return 0f;
            }

            float normalizedDistance = distance / radius;
            float falloff = falloffType == FalloffType.Linear
                ? 1f - normalizedDistance
                : 1f - normalizedDistance * normalizedDistance;

            return baseForce * falloff;
        }

        private IEnumerator ExplodeDelayed(float delayTime)
        {
            yield return new WaitForSeconds(delayTime);
            Explode();
        }

        [Button("Explode Now")]
        private void ExplodeButton()
        {
            Explode();
        }

        [Button("Reset Explosion")]
        private void ResetExplosionButton()
        {
            ResetExplosion();
        }
    }
}