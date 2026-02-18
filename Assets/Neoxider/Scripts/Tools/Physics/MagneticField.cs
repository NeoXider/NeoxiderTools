using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    /// <summary>
    ///     Магнитное поле, которое притягивает или отталкивает объекты в радиусе.
    ///     Поддерживает постоянное воздействие, фильтрацию по слоям и опциональное добавление физики.
    /// </summary>
    [NeoDoc("Tools/Physics/MagneticField.md")]
    [CreateFromMenu("Neoxider/Tools/Physics/MagneticField")]
    [AddComponentMenu("Neoxider/" + "Tools/" + "Physics/" + nameof(MagneticField))]
    public class MagneticField : MonoBehaviour
    {
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

        /// <summary>
        ///     Режим работы магнитного поля.
        /// </summary>
        public enum FieldMode
        {
            /// <summary>Притяжение объектов к себе</summary>
            Attract,

            /// <summary>Отталкивание объектов</summary>
            Repel,

            /// <summary>Притяжение к Transform цели</summary>
            ToTarget,

            /// <summary>Притяжение к точке в пространстве</summary>
            ToPoint,

            /// <summary>Притяжение по направлению (вектор)</summary>
            Direction
        }

        [Header("Settings")] [Tooltip("Field mode")] [SerializeField]
        private FieldMode mode = FieldMode.Attract;

        [Tooltip("Magnetic field strength")] [Min(0f)] [SerializeField]
        private float fieldStrength = 10f;

        [Tooltip("Field radius")] [Min(0f)] [SerializeField]
        private float radius = 5f;

        [Tooltip("Force falloff type over distance")] [SerializeField]
        private FalloffType falloffType = FalloffType.Quadratic;

        [Header("Filtering")] [Tooltip("Layers of objects affected by the field")] [SerializeField]
        private LayerMask affectedLayers = -1;

        [Header("Toggle")]
        [Tooltip(
            "Включить переключение направления (работает с любым режимом). Сначала в одну сторону, потом в обратную.")]
        [SerializeField]
        private bool _toggle;

        [Tooltip("Duration in forward direction (seconds)")] [Min(0.1f)] [SerializeField]
        private float attractDuration = 2f;

        [Tooltip("Duration in reverse direction (seconds)")] [Min(0.1f)] [SerializeField]
        private float repelDuration = 2f;

        [Tooltip("Start with forward direction (true) or reverse (false)")] [SerializeField]
        private bool startWithAttract = true;

        [Header("Attraction Target")] [Tooltip("Target transform (used in ToTarget mode)")] [SerializeField]
        private Transform targetTransform;

        [Tooltip("Point in space (used in ToPoint mode)")] [SerializeField]
        private Vector3 targetPoint = Vector3.zero;

        [Header("Attraction Direction")]
        [Tooltip(
            "Направление вектора (используется при режиме Direction). Если включено Local Direction — задаётся в локальных координатах объекта.")]
        [SerializeField]
        private Vector3 direction = Vector3.forward;

        [Tooltip("If true — direction is in local space (TransformDirection).")]
        [SerializeField]
        private bool directionIsLocal = true;

        [Tooltip("Direction visualization length in scene (and handle for Direction mode).")]
        [Min(0.01f)]
        [SerializeField]
        private float directionGizmoDistance = 10f;

        [Header("Options")] [Tooltip("Automatically add Rigidbody to objects without physics")] [SerializeField]
        private bool addRigidbodyIfNeeded;

        [Tooltip("Use FixedUpdate instead of Update for more stable physics")] [SerializeField]
        private bool useFixedUpdate = true;

        [Tooltip("Update interval for objects in field (0 = every frame)")] [Min(0f)] [SerializeField]
        private float updateInterval;

        [Tooltip("Invoked when object enters field")]
        public UnityEvent<GameObject> OnObjectEntered = new();

        [Tooltip("Invoked when object exits field")]
        public UnityEvent<GameObject> OnObjectExited = new();

        [Tooltip("Invoked when mode changes (for Toggle)")]
        public UnityEvent<bool> OnModeChanged = new();

        private readonly Dictionary<GameObject, Rigidbody> cachedRigidbodies = new();

        private readonly HashSet<GameObject> objectsInField = new();
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

        private void Awake()
        {
            if (_toggle)
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

        private void OnDrawGizmosSelected()
        {
            Vector3 center = GetFieldCenter();
            bool isRepel = mode == FieldMode.Repel;
            bool isAttracting = !isRepel;
            // Если toggle включён и сейчас обратная фаза — инвертируем визуал
            if (_toggle && !CurrentToggleState)
            {
                isAttracting = !isAttracting;
            }

            Gizmos.color = isAttracting ? Color.blue : Color.red;
            Gizmos.DrawWireSphere(center, radius);

            Gizmos.color = new Color(isAttracting ? 0f : 1f, 0f, isAttracting ? 1f : 0f, 0.1f);
            Gizmos.DrawSphere(center, radius);

            if (mode == FieldMode.ToTarget && targetTransform != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, targetTransform.position);
                Gizmos.DrawSphere(targetTransform.position, 0.075f);
            }

            if (mode == FieldMode.ToPoint)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, targetPoint);
                Gizmos.DrawSphere(targetPoint, 0.075f);
            }

            if (mode == FieldMode.Direction)
            {
                Vector3 end = transform.position +
                              GetDirectionWorldNormalized() * Mathf.Max(0.01f, directionGizmoDistance);
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, end);
                Gizmos.DrawSphere(end, 0.075f);
            }
        }

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

        /// <summary>
        ///     Установить направление притяжения (вектор).
        /// </summary>
        public void SetDirection(Vector3 newDirection, bool local = true)
        {
            direction = newDirection;
            directionIsLocal = local;
            mode = FieldMode.Direction;
        }

        private void UpdateToggleState()
        {
            if (!_toggle)
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

            Vector3 fieldCenter = GetFieldCenter();
            Collider[] colliders = Physics.OverlapSphere(fieldCenter, radius, affectedLayers);
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

            Vector3 forceDirection = CalculateForceDirection(obj.transform.position);
            Vector3 fieldCenter = GetFieldCenter();
            float distance = Vector3.Distance(fieldCenter, obj.transform.position);
            float forceAtDistance = CalculateForceAtDistance(distance, fieldStrength);

            rb.AddForce(forceDirection * forceAtDistance, ForceMode.Force);
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

        private Vector3 CalculateForceDirection(Vector3 objectPosition)
        {
            // Определяем базовое направление по режиму
            Vector3 baseDir;
            if (mode == FieldMode.Direction)
            {
                baseDir = GetDirectionWorldNormalized();
            }
            else
            {
                Vector3 attractionPoint = GetAttractionPoint();
                Vector3 toAttractionPoint = attractionPoint - objectPosition;

                if (toAttractionPoint.sqrMagnitude < 0.0001f)
                {
                    toAttractionPoint = Random.onUnitSphere;
                }

                baseDir = toAttractionPoint.normalized;
            }

            // В режиме Repel инвертируем
            bool isRepel = mode == FieldMode.Repel;
            if (isRepel)
            {
                baseDir = -baseDir;
            }

            // Если toggle включён и сейчас обратная фаза — инвертируем
            if (_toggle && !CurrentToggleState)
            {
                baseDir = -baseDir;
            }

            return baseDir;
        }

        private Vector3 GetFieldCenter()
        {
            return transform.position;
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

        private Vector3 GetDirectionWorldNormalized()
        {
            Vector3 dir = directionIsLocal ? transform.TransformDirection(direction) : direction;
            if (dir.sqrMagnitude < 0.0001f)
            {
                dir = transform.forward;
            }

            return dir.normalized;
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

        [Button("Toggle Mode")]
        private void ToggleModeButton()
        {
            ToggleMode();
        }

        [Button("Reset Toggle Timer")]
        private void ResetToggleTimerButton()
        {
            ResetToggleTimer();
        }
    }
}
