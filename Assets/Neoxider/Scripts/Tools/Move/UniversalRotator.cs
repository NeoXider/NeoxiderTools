#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using UnityEditor;
using UnityEngine;

namespace Neo.Tools
{
    [AddComponentMenu("Neo/Tools/UniversalRotator")]
    public class UniversalRotator : MonoBehaviour
    {
        public enum AimSource
        {
            None,
            Transform,
            WorldPoint,
            Mouse
        }

        public enum Axis
        {
            X,
            Y,
            Z
        }

        public enum Mouse3DMode
        {
            PlaneThroughObject,
            PhysicsRaycast
        }

        public enum RotationMode
        {
            Mode3D,
            Mode2D
        }

        public enum UpdateMode
        {
            Update,
            FixedUpdate,
            LateUpdate
        }

        // ========== РЕЖИМЫ ==========
        [Tooltip("Режим вращения: 3D (по forward) или 2D (в плоскости XY, поворот по Z).")]
#if ODIN_INSPECTOR
        [LabelText("Режим вращения")]
#endif
        [SerializeField]
        private RotationMode rotationMode = RotationMode.Mode3D;

        [Tooltip("Где исполнять логику вращения.")]
        public UpdateMode updateMode = UpdateMode.Update;

        [Tooltip("Использовать нескалированное время.")]
        public bool useUnscaledTime;

        // ========== СКОРОСТЬ И ОФСЕТ ==========
#if ODIN_INSPECTOR
[LabelText("Скорость (°/сек)")]
#endif
        [Tooltip("Скорость вращения (град/сек).")] [Min(0f)]
        public float rotationSpeed = 360f;

        [Tooltip("Офсет вращения. В 3D — полный Euler. В 2D используется только Z.")]
#if ODIN_INSPECTOR
        [ShowIf("@rotationMode == RotationMode.Mode3D")]
        [LabelText("Офсет (Euler)")]
#endif
        public Vector3 rotationOffsetEuler = Vector3.zero;

        // ========== ОГРАНИЧЕНИЯ ==========
#if ODIN_INSPECTOR
        [FoldoutGroup("Ограничения")]
        [InfoBox("Если диапазон [0..360] — ограничения отключены.", InfoMessageType.None)]
        [LabelText("Ось ограничения (локальная)")]
        [ShowIf("@rotationMode == RotationMode.Mode3D")]
#endif
        public Axis limitedAxis3D = Axis.Y;

#if ODIN_INSPECTOR
        [FoldoutGroup("Ограничения")] [LabelText("Диапазон (°)")] [MinMaxSlider(-360f, 360f, true)]
#endif
        public Vector2 limitRange = new(0f, 360f);

#if ODIN_INSPECTOR
        [FoldoutGroup("Ограничения")]
        [LabelText("Относительно стартовой позы")]
#endif
        [Tooltip("Если включено — диапазон считается от локальных эйлеров при старте, иначе — от 0.")]
        public bool limitsRelativeToInitial = true;

        // ========== НАВЕДЕНИЕ ==========
#if ODIN_INSPECTOR
        [FoldoutGroup("Наведение")]
        [ToggleLeft]
        [LabelText("Использовать мировые координаты мыши")]
        [OnValueChanged(nameof(OnUseMouseToggled))]
#endif

        public bool useMouseWorld;

#if ODIN_INSPECTOR
        [FoldoutGroup("Наведение")] [ShowIf("@!useMouseWorld")] [LabelText("Цель (Transform)")]
#endif
        public Transform target;

#if ODIN_INSPECTOR
        [FoldoutGroup("Наведение")]
        [ShowIf("@useMouseWorld")]
        [InlineButton(nameof(UseMainCamera), "Main")]
#endif

        [Tooltip("Камера для режима мыши. Если пусто — будет подставлена Camera.main.")]
        public Camera targetCamera;

#if ODIN_INSPECTOR
        [FoldoutGroup("Наведение/MOUSE 3D")]
        [ShowIf("@useMouseWorld && rotationMode == RotationMode.Mode3D")]
        [LabelText("Режим 3D мыши")]
#endif

        public Mouse3DMode mouse3DMode = Mouse3DMode.PlaneThroughObject;

#if ODIN_INSPECTOR
        [FoldoutGroup("Наведение/MOUSE 3D")]
        [ShowIf("@useMouseWorld && rotationMode == RotationMode.Mode3D")]
        [LabelText("Нормаль плоскости")]
#endif

        [Tooltip("Для PlaneThroughObject. Y = горизонтальная плоскость.")]
        public Axis planeAxis = Axis.Y;

#if ODIN_INSPECTOR
        [FoldoutGroup("Наведение/MOUSE 3D")]
        [ShowIf("@useMouseWorld && rotationMode == RotationMode.Mode3D")]
        [LabelText("Слои Raycast (3D)")]
#endif

        public LayerMask mouseRaycastMask = Physics.DefaultRaycastLayers;

#if ODIN_INSPECTOR
        [FoldoutGroup("Наведение/3D")]
        [ShowIf("@rotationMode == RotationMode.Mode3D")]
        [LabelText("World Up")]
#endif

        [Tooltip("Up-вектор для LookRotation в 3D.")]
        public Vector3 worldUp = Vector3.up;

        private Transform cachedParent;

#if ODIN_INSPECTOR
        [ShowInInspector] [ReadOnly] [FoldoutGroup("Отладка")] [LabelText("Текущий источник наведения")]
#endif
        // ========== ВНУТРЕННЕЕ СОСТОЯНИЕ ==========
        private AimSource currentAim = AimSource.None;

        private Vector3 initialLocalEuler;

        private Vector3 manualWorldPoint;

        // В 2D показываем отдельный слайдер для Z, который мапится на rotationOffsetEuler.z
#if ODIN_INSPECTOR
        [ShowInInspector]
        [ShowIf("@rotationMode == RotationMode.Mode2D")]
        [LabelText("Офсет Z (2D)")]
        [PropertyRange(-180, 180)]
#endif

        [Tooltip("Офсет по Z для 2D (спрайтов).")]
        public float rotationOffsetZ2D
        {
            get => rotationOffsetEuler.z;
            set => rotationOffsetEuler.z = value;
        }

        private void Awake()
        {
            cachedParent = transform.parent;
            initialLocalEuler = transform.localEulerAngles;

            TryAssignMainCameraIfNull();

            currentAim = useMouseWorld ? AimSource.Mouse
                : target != null ? AimSource.Transform
                : AimSource.None;
        }

        // ========= ЖИЗНЕННЫЙ ЦИКЛ =========
        private void Reset()
        {
            TryAssignMainCameraIfNull(); // сразу заполним в инспекторе
        }

        private void Update()
        {
            if (updateMode == UpdateMode.Update)
            {
                Tick(GetDeltaTime());
            }
        }

        private void FixedUpdate()
        {
            if (updateMode == UpdateMode.FixedUpdate)
            {
                Tick(GetDeltaTime());
            }
        }

        private void LateUpdate()
        {
            if (updateMode == UpdateMode.LateUpdate)
            {
                Tick(GetDeltaTime());
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (rotationMode == RotationMode.Mode3D && useMouseWorld && mouse3DMode == Mouse3DMode.PlaneThroughObject)
            {
                Vector3 n = AxisToWorldVector(planeAxis);
                Handles.color = new Color(0.2f, 0.7f, 1f, 0.3f);
                Handles.DrawSolidDisc(transform.position, n, 0.5f);
            }
        }
#endif

        private void OnValidate()
        {
            TryAssignMainCameraIfNull();
            // Нормализуем диапазон (исправляем NaN/Inf)
            limitRange.x = Mathf.Clamp(limitRange.x, -360f, 360f);
            limitRange.y = Mathf.Clamp(limitRange.y, -360f, 360f);
        }

        // ========== КНОПКИ ==========
#if ODIN_INSPECTOR
        [FoldoutGroup("Наведение")]
        [DisableInEditorMode]
#endif

        [Button]
        private void ClearTargetButton()
        {
            ClearTarget();
        }

#if ODIN_INSPECTOR
        [FoldoutGroup("Наведение")]
        [ShowIf("@target != null && !useMouseWorld")]
#endif

        [Button("Look at Target Instantly")]
        private void LookAtTargetInstant()
        {
            SetTarget(target);
            if (target != null)
            {
                RotateTo(target.position, true);
            }
        }

        // ========= ПУБЛИЧНЫЕ API =========

        /// <summary>Установить цель (Transform). Отключает режим мыши.</summary>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            useMouseWorld = false;
            currentAim = target ? AimSource.Transform : AimSource.None;
        }

        /// <summary>Убрать цель. Если включен режим мыши — будет использована мышь.</summary>
        public void ClearTarget()
        {
            target = null;
            currentAim = useMouseWorld ? AimSource.Mouse : AimSource.None;
        }

        /// <summary>Поворот к мировой точке. Если instant=true — сразу установить, иначе — плавно доворачивать.</summary>
        public void RotateTo(Vector3 worldPoint, bool instant = false)
        {
            manualWorldPoint = worldPoint;
            currentAim = AimSource.WorldPoint;

            if (instant)
            {
                Quaternion desired = ComputeDesiredWorldRotation(worldPoint, out _);
                ApplyRotationInstant(desired);
            }
        }

        /// <summary>Поворот к направлению (world direction).</summary>
        public void RotateToDirection(Vector3 worldDirection, bool instant = false)
        {
            if (worldDirection.sqrMagnitude < 1e-8f)
            {
                return;
            }

            RotateTo(transform.position + worldDirection, instant);
        }

        /// <summary>Повернуть на deltaDegrees вокруг рабочей оси (в 2D — всегда Z, в 3D — limitedAxis3D).</summary>
        public void RotateBy(float deltaDegrees)
        {
            Axis axisUsed = GetActiveAxis();
            Vector3 delta = AxisToVector(axisUsed) * deltaDegrees;
            RotateBy(delta);
        }

        /// <summary>Повернуть на eulerDelta (локальные Эйлеры). Ограничения учитываются.</summary>
        public void RotateBy(Vector3 eulerDelta)
        {
            Vector3 local = transform.localEulerAngles;
            local += eulerDelta;

            if (LimitsActive())
            {
                Axis axisUsed = GetActiveAxis();
                int i = AxisIndex(axisUsed);
                float baseline = limitsRelativeToInitial ? initialLocalEuler[i] : 0f;
                float relative = Normalize180(local[i] - baseline);
                float clamped = ClampAngleToRange(relative, limitRange.x, limitRange.y);
                local[i] = baseline + clamped;
            }

            transform.localRotation = Quaternion.Euler(local);
        }

        // ========= ОСНОВНАЯ ЛОГИКА =========
        private void Tick(float dt)
        {
            Vector3? maybePoint = GetAimPointWorld();
            if (!maybePoint.HasValue)
            {
                return;
            }

            Quaternion desiredWorld =
                ComputeDesiredWorldRotation(maybePoint.Value, out Quaternion desiredLocalBeforeLimits);

            if (LimitsActive())
            {
                Quaternion limitedLocal = ApplyAxisLimit(desiredLocalBeforeLimits);
                desiredWorld = ToWorld(limitedLocal);
            }

            float maxStep = rotationSpeed * dt;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, desiredWorld, maxStep);
        }

        private Vector3? GetAimPointWorld()
        {
            if (useMouseWorld)
            {
                currentAim = AimSource.Mouse;
            }

            switch (currentAim)
            {
                case AimSource.Mouse:
                    return rotationMode == RotationMode.Mode2D ? GetMouseWorldPoint2D() : GetMouseWorldPoint3D();
                case AimSource.Transform:
                    return target ? target.position : null;
                case AimSource.WorldPoint:
                    return manualWorldPoint;
                default:
                    return null;
            }
        }

        private Quaternion ComputeDesiredWorldRotation(Vector3 aimPoint, out Quaternion desiredLocalBeforeLimits)
        {
            if (rotationMode == RotationMode.Mode2D)
            {
                Vector3 to = aimPoint - transform.position;
                Vector2 to2D = new(to.x, to.y);
                if (to2D.sqrMagnitude < 1e-8f)
                {
                    desiredLocalBeforeLimits = transform.localRotation;
                    return transform.rotation;
                }

                float angle = Mathf.Atan2(to2D.y, to2D.x) * Mathf.Rad2Deg;
                float z = angle + rotationOffsetEuler.z;

                desiredLocalBeforeLimits = Quaternion.Euler(0f, 0f, z);
                return ToWorld(desiredLocalBeforeLimits);
            }

            // 3D
            Vector3 dir = aimPoint - transform.position;
            if (dir.sqrMagnitude < 1e-10f)
            {
                desiredLocalBeforeLimits = transform.localRotation;
                return transform.rotation;
            }

            Quaternion look = Quaternion.LookRotation(dir.normalized,
                worldUp.sqrMagnitude > 0.0001f ? worldUp.normalized : Vector3.up);
            Quaternion offset = Quaternion.Euler(rotationOffsetEuler);
            Quaternion worldDesired = look * offset;

            desiredLocalBeforeLimits = ToLocal(worldDesired);
            return worldDesired;
        }

        private Quaternion ApplyAxisLimit(Quaternion desiredLocal)
        {
            Vector3 euler = desiredLocal.eulerAngles;

            Axis axisUsed = GetActiveAxis();
            int i = AxisIndex(axisUsed);
            float baseline = limitsRelativeToInitial ? initialLocalEuler[i] : 0f;
            float relative = Normalize180(euler[i] - baseline);
            float clamped = ClampAngleToRange(relative, limitRange.x, limitRange.y);
            euler[i] = baseline + clamped;

            return Quaternion.Euler(euler);
        }

        private bool LimitsActive()
        {
            return !(Approximately(limitRange.x, 0f) && Approximately(limitRange.y, 360f));
        }

        private void ApplyRotationInstant(Quaternion desiredWorld)
        {
            if (LimitsActive())
            {
                Quaternion desiredLocal = ToLocal(desiredWorld);
                Quaternion limitedLocal = ApplyAxisLimit(desiredLocal);
                desiredWorld = ToWorld(limitedLocal);
            }

            transform.rotation = desiredWorld;
        }

        // ========= МЫШЬ =========
        private Vector3? GetMouseWorldPoint2D()
        {
            Camera cam = GetCamera();
            if (!cam)
            {
                return null;
            }

            Vector3 sp = Input.mousePosition;
            float depth = Mathf.Abs(cam.WorldToScreenPoint(transform.position).z);
            Vector3 wp = cam.ScreenToWorldPoint(new Vector3(sp.x, sp.y, depth));
            wp.z = transform.position.z;
            return wp;
        }

        private Vector3? GetMouseWorldPoint3D()
        {
            Camera cam = GetCamera();
            if (!cam)
            {
                return null;
            }

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);

            if (mouse3DMode == Mouse3DMode.PhysicsRaycast)
            {
                if (Physics.Raycast(ray, out RaycastHit hit, 10000f, mouseRaycastMask, QueryTriggerInteraction.Ignore))
                {
                    return hit.point;
                }
            }

            // фоллбэк ниже
            Vector3 normal = AxisToWorldVector(planeAxis);
            Plane plane = new(normal, transform.position);

            if (plane.Raycast(ray, out float dist))
            {
                return ray.GetPoint(dist);
            }

            return null;
        }

        // ========= ВСПОМОГАТЕЛЬНОЕ =========
        private Axis GetActiveAxis()
        {
            return rotationMode == RotationMode.Mode2D ? Axis.Z : limitedAxis3D;
        }

        private static int AxisIndex(Axis a)
        {
            return a == Axis.X ? 0 : a == Axis.Y ? 1 : 2;
        }

        private static Vector3 AxisToVector(Axis a)
        {
            return a == Axis.X ? new Vector3(1, 0, 0) : a == Axis.Y ? new Vector3(0, 1, 0) : new Vector3(0, 0, 1);
        }

        private static Vector3 AxisToWorldVector(Axis a)
        {
            return a == Axis.X ? Vector3.right : a == Axis.Y ? Vector3.up : Vector3.forward;
        }

        private float GetDeltaTime()
        {
            if (updateMode == UpdateMode.FixedUpdate)
            {
                return useUnscaledTime ? Time.fixedUnscaledDeltaTime : Time.fixedDeltaTime;
            }

            return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        }

        private Camera GetCamera()
        {
            if (targetCamera)
            {
                return targetCamera;
            }

            return Camera.main;
        }

        private void TryAssignMainCameraIfNull()
        {
            if (targetCamera == null)
            {
                Camera cam = Camera.main;
                if (cam != null)
                {
                    targetCamera = cam;
                }
            }
        }

        private void UseMainCamera()
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                targetCamera = cam;
            }
        }

        private void OnUseMouseToggled()
        {
            currentAim = useMouseWorld ? AimSource.Mouse : target ? AimSource.Transform : AimSource.None;
        }

        private Quaternion ToLocal(Quaternion worldRot)
        {
            if (cachedParent != null)
            {
                return Quaternion.Inverse(cachedParent.rotation) * worldRot;
            }

            return worldRot;
        }

        private Quaternion ToWorld(Quaternion localRot)
        {
            if (cachedParent != null)
            {
                return cachedParent.rotation * localRot;
            }

            return localRot;
        }

        private static float Normalize180(float angle)
        {
            angle %= 360f;
            if (angle > 180f)
            {
                angle -= 360f;
            }

            if (angle < -180f)
            {
                angle += 360f;
            }

            return angle;
        }

        private static float ClampAngleToRange(float a, float min, float max)
        {
            a = Normalize180(a);
            min = Normalize180(min);
            max = Normalize180(max);

            if (min <= max)
            {
                return Mathf.Clamp(a, min, max);
            }

            bool inside = a >= min || a <= max;
            if (inside)
            {
                return a;
            }

            float dMin = Mathf.Abs(Mathf.DeltaAngle(a, min));
            float dMax = Mathf.Abs(Mathf.DeltaAngle(a, max));
            return dMin < dMax ? min : max;
        }

        private static bool Approximately(float a, float b)
        {
            return Mathf.Abs(a - b) <= 0.0001f;
        }
    }
}