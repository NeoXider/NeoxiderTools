#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    [AddComponentMenu("Neoxider/Tools/AdvancedForceApplier")]
    public class AdvancedForceApplier : MonoBehaviour
    {
        public enum BodyType
        {
            Auto,
            Rigidbody3D,
            Rigidbody2D
        }

        public enum DirectionMode
        {
            Velocity,
            TransformForward,
            CustomVector,
            ToTarget
        }

#if ODIN_INSPECTOR
        [FoldoutGroup("Components")] [SerializeField]
#endif
        private Rigidbody rigidbody3D;

#if ODIN_INSPECTOR
        [FoldoutGroup("Components")] [SerializeField]
#endif
        private Rigidbody2D rigidbody2D;

#if ODIN_INSPECTOR
        [FoldoutGroup("General")]
        [LabelText("Body Type")]
#endif
        [Tooltip("Auto will try to find 3D first, then 2D. Can be fixed manually.")]
        public BodyType bodyType = BodyType.Auto;

#if ODIN_INSPECTOR
[FoldoutGroup("Force")] 
[LabelText("Base Force (N)")]
#endif
        [Min(0f)] [SerializeField] private float defaultForce = 10f;

#if ODIN_INSPECTOR
        [FoldoutGroup("Force")] [ToggleLeft] [LabelText("Randomize Force")]
#endif
        public bool randomizeForce;

#if ODIN_INSPECTOR
        [FoldoutGroup("Force")] [ShowIf("randomizeForce")] [MinMaxSlider(0f, 10000f, true)]
#endif
        public Vector2 forceRange = new(5f, 15f);

#if ODIN_INSPECTOR
        [FoldoutGroup("Force")] [ToggleLeft]
#endif
        public bool playOnAwake = true;

#if ODIN_INSPECTOR
        [FoldoutGroup("Force")] [ShowIf("Is3DActive")] [LabelText("Force Mode (3D)")] [EnumToggleButtons]
#endif
        public ForceMode forceMode3D = ForceMode.Impulse;

#if ODIN_INSPECTOR
        [FoldoutGroup("Force")] [ShowIf("Is2DActive")] [LabelText("Force Mode (2D)")] [EnumToggleButtons]
#endif
        public ForceMode2D forceMode2D = ForceMode2D.Impulse;

#if ODIN_INSPECTOR
        [FoldoutGroup("Limits")] [ToggleLeft] [LabelText("Clamp Max Speed")]
#endif

        public bool clampMaxSpeed;

#if ODIN_INSPECTOR
      [FoldoutGroup("Limits")] [ShowIf("clampMaxSpeed")] [Min(0f)] [LabelText("Max Speed")]
#endif

        public float maxSpeed = 20f;

#if ODIN_INSPECTOR
       [FoldoutGroup("Direction")] [LabelText("Direction Source")]
#endif

        public DirectionMode directionMode = DirectionMode.Velocity;

#if ODIN_INSPECTOR
        [FoldoutGroup("Direction")]
        [ShowIf("directionMode == DirectionMode.TransformForward")]
        [LabelText("Use Local Forward (2D=Right, 3D=Forward)")]
#endif
        public bool useLocalForward = true;

#if ODIN_INSPECTOR
        [FoldoutGroup("Direction")] [ToggleLeft] [LabelText("Invert Direction")]
#endif

        public bool invertDirection;

#if ODIN_INSPECTOR
        [FoldoutGroup("Direction")] [ShowIf("directionMode == DirectionMode.CustomVector")] [LabelText("Custom Vector")]

#endif
        public Vector3 customDirection = Vector3.forward;

#if ODIN_INSPECTOR
        [FoldoutGroup("Direction")]
        [ShowIf("directionMode == DirectionMode.ToTarget")]
        [LabelText("Target (Transform)")]
#endif

        public Transform target;

#if ODIN_INSPECTOR
   [FoldoutGroup("Events")]
#endif
        public UnityEvent OnApplyForce;

#if ODIN_INSPECTOR
        [FoldoutGroup("Debug")]
        [InfoBox("No suitable Rigidbody found. Component won't be able to apply force.", InfoMessageType.Warning,
            VisibleIf = nameof(ShowNoRigidbodyWarning))]
        [ShowInInspector]
        [ReadOnly]
        [LabelText("Active Body Type")]
#endif

        private string ActiveBodyInfo => Is3DActive() ? "Rigidbody (3D)" : Is2DActive() ? "Rigidbody2D (2D)" : "None";

        private void Awake()
        {
            // Auto-detect components if not assigned
            if (rigidbody3D == null)
            {
                rigidbody3D = GetComponent<Rigidbody>();
            }

            if (rigidbody2D == null)
            {
                rigidbody2D = GetComponent<Rigidbody2D>();
            }
        }

        private void Start()
        {
            if (playOnAwake)
            {
                ApplyForce(defaultForce);
            }
        }

#if ODIN_INSPECTOR
[FoldoutGroup("Controls")]
[DisableInEditorMode]
#endif

        [Button("Apply Now")]
        private void ApplyNowButton()
        {
            ApplyForce();
        }

        /// <summary>
        ///     Применяет силу к телу.
        /// </summary>
        /// <param name="force">Величина силы (если 0, используется defaultForce)</param>
        /// <param name="direction">Направление силы (если null, используется GetDirection())</param>
        public void ApplyForce(float force = 0f, Vector3? direction = null)
        {
            float chosenForce = force > 0f ? force :
                randomizeForce ? Random.Range(forceRange.x, forceRange.y) : defaultForce;
            Vector3 dir = (direction ?? ComputeDirection()).normalized;
            if (dir.sqrMagnitude < 1e-6f)
            {
                dir = transform.forward; // fallback
            }

            if (Resolve3D())
            {
                rigidbody3D.AddForce(dir * chosenForce, forceMode3D);
                if (clampMaxSpeed && rigidbody3D.velocity.sqrMagnitude > maxSpeed * maxSpeed)
                {
                    rigidbody3D.velocity = rigidbody3D.velocity.normalized * maxSpeed;
                }
            }
            else if (Resolve2D())
            {
                rigidbody2D.AddForce(dir * chosenForce, forceMode2D);
                if (clampMaxSpeed && rigidbody2D.velocity.sqrMagnitude > maxSpeed * maxSpeed)
                {
                    rigidbody2D.velocity = rigidbody2D.velocity.normalized * maxSpeed;
                }
            }

            OnApplyForce?.Invoke();
        }

        /// <summary>
        ///     Получает направление для применения силы.
        /// </summary>
        private Vector3 ComputeDirection()
        {
            Vector3 result = Vector3.zero;

            switch (directionMode)
            {
                case DirectionMode.Velocity:
                {
                    if (Resolve3D())
                    {
                        result = rigidbody3D.velocity;
                    }
                    else if (Resolve2D())
                    {
                        result = rigidbody2D.velocity;
                    }

                    break;
                }
                case DirectionMode.TransformForward:
                {
                    if (Resolve3D())
                    {
                        result = useLocalForward ? transform.forward : transform.TransformDirection(Vector3.forward);
                    }
                    else if (Resolve2D())
                        // In 2D it's more convenient to consider "forward" as local right in XY plane
                    {
                        result = useLocalForward ? transform.right : transform.TransformDirection(Vector3.right);
                    }

                    break;
                }
                case DirectionMode.CustomVector:
                {
                    result = customDirection;
                    break;
                }
                case DirectionMode.ToTarget:
                {
                    if (target != null)
                    {
                        result = target.position - transform.position;
                    }

                    break;
                }
            }

            if (invertDirection)
            {
                result = -result;
            }

            return result.sqrMagnitude > 1e-8f ? result.normalized : transform.forward;
        }

        // ===== HELPER METHODS =====
        private bool Resolve3D()
        {
            if (bodyType == BodyType.Rigidbody3D)
            {
                return rigidbody3D != null;
            }

            if (bodyType == BodyType.Rigidbody2D)
            {
                return false;
            }

            return rigidbody3D != null; // Auto
        }

        private bool Resolve2D()
        {
            if (bodyType == BodyType.Rigidbody2D)
            {
                return rigidbody2D != null;
            }

            if (bodyType == BodyType.Rigidbody3D)
            {
                return false;
            }

            return rigidbody3D == null && rigidbody2D != null; // Auto: if no 3D but has 2D
        }

        private bool Is3DActive()
        {
            return Resolve3D();
        }

        private bool Is2DActive()
        {
            return Resolve2D();
        }

        private bool ShowNoRigidbodyWarning()
        {
            return !Resolve3D() && !Resolve2D();
        }
    }
}