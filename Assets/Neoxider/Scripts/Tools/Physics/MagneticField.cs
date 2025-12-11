using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace Neo.Tools
{
    /// <summary>
    ///     Магнитное поле, которое притягивает или отталкивает объекты в радиусе.
    ///     Поддерживает постоянное воздействие, фильтрацию по слоям и опциональное добавление физики.
    /// </summary>
    [AddComponentMenu("Neo/" + "Tools/" + "Physics/" + nameof(MagneticField))]
    public class MagneticField : MonoBehaviour
    {
        /// <summary>
        ///     Режим работы магнитного поля.
        /// </summary>
        public enum FieldMode
        {
            /// <summary>Притяжение объектов к себе</summary>
            Attract,

            /// <summary>Отталкивание объектов</summary>
            Repel,

            /// <summary>Переключение между притяжением и отталкиванием</summary>
            Toggle,

            /// <summary>Притяжение к Transform цели</summary>
            ToTarget,

            /// <summary>Притяжение к точке в пространстве</summary>
            ToPoint
        }

        /// <summary>
        ///     Тип затухания силы по расстоянию.
        /// </summary>
        public enum FalloffType
        {
            /// <summary>Линейное затухание</summary>
            Linear,

            /// <summary>Квадратичное затухание</summary>
            Quadratic,

            /// <summary>Без затухания</summary>
            Constant
        }

        [Header("Settings")] [Tooltip("Режим работы поля")] [SerializeField]
        private FieldMode mode = FieldMode.Attract;

        [Tooltip("Сила магнитного поля")] [Min(0f)] [SerializeField]
        private float fieldStrength = 10f;

        [Tooltip("Радиус действия поля")] [Min(0f)] [SerializeField]
        private float radius = 5f;

        [Tooltip("Тип затухания силы по расстоянию")] [SerializeField]
        private FalloffType falloffType = FalloffType.Quadratic;

        [Header("Filtering")] [Tooltip("Слои объектов, на которые будет воздействовать поле")] [SerializeField]
        private LayerMask affectedLayers = -1;

        [Header("Toggle Mode")] [Tooltip("Время притяжения в режиме Toggle (секунды)")] [Min(0.1f)] [SerializeField]
        private float attractDuration = 2f;

        [Tooltip("Время отталкивания в режиме Toggle (секунды)")] [Min(0.1f)] [SerializeField]
        private float repelDuration = 2f;

        [Tooltip("Начальный режим для Toggle (с чего начинать)")] [SerializeField]
        private bool startWithAttract = true;

        [Header("Attraction Target")] [Tooltip("Transform цели (используется при режиме ToTarget)")] [SerializeField]
        private Transform targetTransform;

        [Tooltip("Точка в пространстве (используется при режиме ToPoint)")] [SerializeField]
        private Vector3 targetPoint = Vector3.zero;

        [Header("Options")] [Tooltip("Автоматически добавлять Rigidbody на объекты без физики")] [SerializeField]
        private bool addRigidbodyIfNeeded;

        [Tooltip("Использовать FixedUpdate вместо Update для более стабильной физики")] [SerializeField]
        private bool useFixedUpdate = true;

        [Tooltip("Интервал обновления объектов в поле (0 = каждый кадр)")] [Min(0f)] [SerializeField]
        private float updateInterval;

        [Tooltip("Вызывается при входе объекта в поле")]
        public UnityEvent<GameObject> OnObjectEntered = new();

        [Tooltip("Вызывается при выходе объекта из поля")]
        public UnityEvent<GameObject> OnObjectExited = new();

        [Tooltip("Вызывается при изменении режима (для Toggle)")]
        public UnityEvent<bool> OnModeChanged = new();

        private readonly HashSet<GameObject> objectsInField = new();
        private readonly Dictionary<GameObject, Rigidbody> cachedRigidbodies = new();
        private float lastUpdateTime;
        private float toggleStartTime;

        /// <summary>
        ///     Получить текущую силу поля.
        /// </summary>
        public float CurrentStrength => fieldStrength;

        /// <summary>
        ///     Получить текущий радиус поля.
        /// </summary>
        public float CurrentRadius => radius;

        /// <summary>
        ///     Получить количество объектов в поле.
        /// </summary>
        public int ObjectsInFieldCount => objectsInField.Count;

        /// <summary>
        ///     Получить текущее активное состояние в режиме Toggle (true = притяжение, false = отталкивание).
        /// </summary>
        public bool CurrentToggleState { get; private set; }

        /// <summary>
        ///     Переключить режим поля (Attract/Repel).
        /// </summary>
        public void ToggleMode()
        {
            mode = mode == FieldMode.Attract ? FieldMode.Repel : FieldMode.Attract;
        }

        /// <summary>
        ///     Установить режим поля.
        /// </summary>
        public void SetMode(FieldMode newMode)
        {
            mode = newMode;
        }

        /// <summary>
        ///     Установить силу поля.
        /// </summary>
        public void SetStrength(float newStrength)
        {
            fieldStrength = Mathf.Max(0f, newStrength);
        }

        /// <summary>
        ///     Установить радиус поля.
        /// </summary>
        public void SetRadius(float newRadius)
        {
            radius = Mathf.Max(0f, newRadius);
        }

        /// <summary>
        ///     Установить время притяжения для режима Toggle.
        /// </summary>
        public void SetAttractDuration(float duration)
        {
            attractDuration = Mathf.Max(0.1f, duration);
        }

        /// <summary>
        ///     Установить время отталкивания для режима Toggle.
        /// </summary>
        public void SetRepelDuration(float duration)
        {
            repelDuration = Mathf.Max(0.1f, duration);
        }

        /// <summary>
        ///     Сбросить таймер режима Toggle (начать цикл заново).
        /// </summary>
        public void ResetToggleTimer()
        {
            toggleStartTime = Time.time;
            CurrentToggleState = startWithAttract;
        }

        /// <summary>
        ///     Установить цель притяжения (Transform).
        /// </summary>
        public void SetTarget(Transform target)
        {
            targetTransform = target;
            if (target != null)
            {
                mode = FieldMode.ToTarget;
            }
        }

        /// <summary>
        ///     Установить точку притяжения.
        /// </summary>
        public void SetTargetPoint(Vector3 point)
        {
            targetPoint = point;
            mode = FieldMode.ToPoint;
        }

        private void Awake()
        {
            if (mode == FieldMode.Toggle)
            {
                toggleStartTime = Time.time;
                CurrentToggleState = startWithAttract;
            }
        }

        private void Update()
        {
            if (useFixedUpdate)
            {
                return;
            }

            UpdateToggleState();
            UpdateField();
        }

        private void FixedUpdate()
        {
            if (!useFixedUpdate)
            {
                return;
            }

            UpdateToggleState();
            UpdateField();
        }

        private void UpdateToggleState()
        {
            if (mode != FieldMode.Toggle)
            {
                return;
            }

            float elapsed = Time.time - toggleStartTime;
            float currentDuration = CurrentToggleState ? attractDuration : repelDuration;

            if (elapsed >= currentDuration)
            {
                CurrentToggleState = !CurrentToggleState;
                toggleStartTime = Time.time;
                OnModeChanged?.Invoke(CurrentToggleState);
            }
        }

        private void UpdateField()
        {
            if (updateInterval > 0f && Time.time - lastUpdateTime < updateInterval)
            {
                return;
            }

            lastUpdateTime = Time.time;

            Vector3 center = GetAttractionPoint();
            Collider[] colliders = Physics.OverlapSphere(center, radius, affectedLayers);
            HashSet<GameObject> currentObjects = new();

            foreach (Collider col in colliders)
            {
                if (col == null || col.gameObject == gameObject)
                {
                    continue;
                }

                GameObject obj = col.gameObject;
                currentObjects.Add(obj);

                if (!objectsInField.Contains(obj))
                {
                    objectsInField.Add(obj);
                    OnObjectEntered?.Invoke(obj);
                }

                ApplyMagneticForce(obj, col);
            }

            List<GameObject> toRemove = new();
            foreach (GameObject obj in objectsInField)
            {
                if (!currentObjects.Contains(obj))
                {
                    toRemove.Add(obj);
                }
            }

            foreach (GameObject obj in toRemove)
            {
                objectsInField.Remove(obj);
                cachedRigidbodies.Remove(obj);
                OnObjectExited?.Invoke(obj);
            }
        }

        private void ApplyMagneticForce(GameObject obj, Collider col)
        {
            Rigidbody rb = GetOrAddRigidbody(obj, col);
            if (rb == null)
            {
                return;
            }

            Vector3 direction = CalculateForceDirection(obj.transform.position);
            Vector3 center = GetAttractionPoint();
            float distance = Vector3.Distance(center, obj.transform.position);
            float forceAtDistance = CalculateForceAtDistance(distance, fieldStrength);

            rb.AddForce(direction * forceAtDistance, ForceMode.Force);
        }

        private Rigidbody GetOrAddRigidbody(GameObject obj, Collider col)
        {
            if (cachedRigidbodies.TryGetValue(obj, out Rigidbody cached))
            {
                return cached;
            }

            Rigidbody rb = col.attachedRigidbody;
            if (rb == null && addRigidbodyIfNeeded)
            {
                rb = obj.AddComponent<Rigidbody>();
            }

            if (rb != null)
            {
                cachedRigidbodies[obj] = rb;
            }

            return rb;
        }

        private Vector3 CalculateForceDirection(Vector3 targetPosition)
        {
            Vector3 attractionPoint = GetAttractionPoint();
            Vector3 direction = (targetPosition - attractionPoint).normalized;

            if (direction.sqrMagnitude < 0.01f)
            {
                direction = Random.onUnitSphere;
            }

            bool shouldAttract = mode == FieldMode.Attract ||
                                 mode == FieldMode.ToTarget ||
                                 mode == FieldMode.ToPoint ||
                                 (mode == FieldMode.Toggle && CurrentToggleState);

            return shouldAttract ? -direction : direction;
        }

        private Vector3 GetAttractionPoint()
        {
            switch (mode)
            {
                case FieldMode.ToTarget:
                    return targetTransform != null ? targetTransform.position : transform.position;

                case FieldMode.ToPoint:
                    return targetPoint;

                default:
                    return transform.position;
            }
        }

        private float CalculateForceAtDistance(float distance, float baseForce)
        {
            if (distance >= radius)
            {
                return 0f;
            }

            if (falloffType == FalloffType.Constant)
            {
                return baseForce;
            }

            float normalizedDistance = distance / radius;
            float falloff = falloffType == FalloffType.Linear
                ? 1f - normalizedDistance
                : 1f - normalizedDistance * normalizedDistance;

            return baseForce * falloff;
        }

#if ODIN_INSPECTOR
        [FoldoutGroup("Testing")]
        [Button("Toggle Mode")]
#else
        [Button("Toggle Mode")]
#endif
        private void ToggleModeButton()
        {
            ToggleMode();
        }

#if ODIN_INSPECTOR
        [FoldoutGroup("Testing")]
        [Button("Reset Toggle Timer")]
        [ShowIf("mode == FieldMode.Toggle")]
#else
        [Button("Reset Toggle Timer")]
#endif
        private void ResetToggleTimerButton()
        {
            ResetToggleTimer();
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 center = GetAttractionPoint();
            bool isAttracting = mode == FieldMode.Attract ||
                                mode == FieldMode.ToTarget ||
                                mode == FieldMode.ToPoint ||
                                (mode == FieldMode.Toggle && CurrentToggleState);
            Gizmos.color = isAttracting ? Color.blue : Color.red;
            Gizmos.DrawWireSphere(center, radius);

            Gizmos.color = new Color(isAttracting ? 0f : 1f, 0f, isAttracting ? 1f : 0f, 0.1f);
            Gizmos.DrawSphere(center, radius);

            if (mode == FieldMode.ToTarget && targetTransform != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, targetTransform.position);
            }
        }
    }
}