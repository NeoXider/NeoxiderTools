using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Sirenix.OdinInspector;

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

        [FoldoutGroup("Components")] [SerializeField]
        private Rigidbody rigidbody3D;

        [FoldoutGroup("Components")] [SerializeField]
        private Rigidbody2D rigidbody2D;

        [FoldoutGroup("General")]
        [LabelText("Body Type")]
        [Tooltip("Auto will try to find 3D first, then 2D. Can be fixed manually.")]
        public BodyType bodyType = BodyType.Auto;

        [FoldoutGroup("Force")] [Min(0f)] [LabelText("Base Force (N)")] [SerializeField]
        private float defaultForce = 10f;

        [FoldoutGroup("Force")] [ToggleLeft] [LabelText("Randomize Force")]
        public bool randomizeForce = false;

        [FoldoutGroup("Force")] [ShowIf("randomizeForce")] [MinMaxSlider(0f, 10000f, true)]
        public Vector2 forceRange = new(5f, 15f);

        [FoldoutGroup("Force")] [ToggleLeft] public bool playOnAwake = true;

        [FoldoutGroup("Force")] [ShowIf("Is3DActive")] [LabelText("Force Mode (3D)")] [EnumToggleButtons]
        public ForceMode forceMode3D = ForceMode.Impulse;

        [FoldoutGroup("Force")] [ShowIf("Is2DActive")] [LabelText("Force Mode (2D)")] [EnumToggleButtons]
        public ForceMode2D forceMode2D = ForceMode2D.Impulse;

        [FoldoutGroup("Limits")] [ToggleLeft] [LabelText("Clamp Max Speed")]
        public bool clampMaxSpeed = false;

        [FoldoutGroup("Limits")] [ShowIf("clampMaxSpeed")] [Min(0f)] [LabelText("Max Speed")]
        public float maxSpeed = 20f;

        [FoldoutGroup("Direction")] [LabelText("Direction Source")]
        public DirectionMode directionMode = DirectionMode.Velocity;

        [FoldoutGroup("Direction")]
        [ShowIf("directionMode == DirectionMode.TransformForward")]
        [LabelText("Use Local Forward (2D=Right, 3D=Forward)")]
        public bool useLocalForward = true;

        [FoldoutGroup("Direction")] [ToggleLeft] [LabelText("Invert Direction")]
        public bool invertDirection = false;

        [FoldoutGroup("Direction")]
        [ShowIf("directionMode == DirectionMode.CustomVector")]
        [LabelText("Custom Vector")]
        public Vector3 customDirection = Vector3.forward;

        [FoldoutGroup("Direction")]
        [ShowIf("directionMode == DirectionMode.ToTarget")]
        [LabelText("Target (Transform)")]
        public Transform target;

        [FoldoutGroup("Debug")]
        [InfoBox("No suitable Rigidbody found. Component won't be able to apply force.", InfoMessageType.Warning,
            VisibleIf = nameof(ShowNoRigidbodyWarning))]
        [ShowInInspector]
        [ReadOnly]
        [LabelText("Active Body Type")]
        private string ActiveBodyInfo => Is3DActive() ? "Rigidbody (3D)" : Is2DActive() ? "Rigidbody2D (2D)" : "None";

        [FoldoutGroup("Controls")]
        [Button("Apply Now")]
        [DisableInEditorMode]
        private void ApplyNowButton()
        {
            ApplyForce();
        }

        [FoldoutGroup("Events")] public UnityEvent OnApplyForce;

        private void Awake()
        {
            // Auto-detect components if not assigned
            if (rigidbody3D == null) rigidbody3D = GetComponent<Rigidbody>();
            if (rigidbody2D == null) rigidbody2D = GetComponent<Rigidbody2D>();
        }

        private void Start()
        {
            if (playOnAwake)
                ApplyForce(defaultForce);
        }

        /// <summary>
        /// Применяет силу к телу.
        /// </summary>
        /// <param name="force">Величина силы (если 0, используется defaultForce)</param>
        /// <param name="direction">Направление силы (если null, используется GetDirection())</param>
        public void ApplyForce(float force = 0f, Vector3? direction = null)
        {
            var chosenForce = force > 0f ? force :
                randomizeForce ? UnityEngine.Random.Range(forceRange.x, forceRange.y) : defaultForce;
            var dir = (direction ?? ComputeDirection()).normalized;
            if (dir.sqrMagnitude < 1e-6f) dir = transform.forward; // fallback

            if (Resolve3D())
            {
                rigidbody3D.AddForce(dir * chosenForce, forceMode3D);
                if (clampMaxSpeed && rigidbody3D.velocity.sqrMagnitude > maxSpeed * maxSpeed)
                    rigidbody3D.velocity = rigidbody3D.velocity.normalized * maxSpeed;
            }
            else if (Resolve2D())
            {
                rigidbody2D.AddForce(dir * chosenForce, forceMode2D);
                if (clampMaxSpeed && rigidbody2D.velocity.sqrMagnitude > maxSpeed * maxSpeed)
                    rigidbody2D.velocity = rigidbody2D.velocity.normalized * maxSpeed;
            }

            OnApplyForce?.Invoke();
        }

        /// <summary>
        /// Получает направление для применения силы.
        /// </summary>
        private Vector3 ComputeDirection()
        {
            var result = Vector3.zero;

            switch (directionMode)
            {
                case DirectionMode.Velocity:
                {
                    if (Resolve3D()) result = rigidbody3D.velocity;
                    else if (Resolve2D()) result = (Vector3)rigidbody2D.velocity;
                    break;
                }
                case DirectionMode.TransformForward:
                {
                    if (Resolve3D())
                        result = useLocalForward ? transform.forward : transform.TransformDirection(Vector3.forward);
                    else if (Resolve2D())
                        // In 2D it's more convenient to consider "forward" as local right in XY plane
                        result = useLocalForward ? transform.right : transform.TransformDirection(Vector3.right);
                    break;
                }
                case DirectionMode.CustomVector:
                {
                    result = customDirection;
                    break;
                }
                case DirectionMode.ToTarget:
                {
                    if (target != null) result = target.position - transform.position;
                    break;
                }
            }

            if (invertDirection) result = -result;
            return result.sqrMagnitude > 1e-8f ? result.normalized : transform.forward;
        }

        // ===== HELPER METHODS =====
        private bool Resolve3D()
        {
            if (bodyType == BodyType.Rigidbody3D) return rigidbody3D != null;
            if (bodyType == BodyType.Rigidbody2D) return false;
            return rigidbody3D != null; // Auto
        }

        private bool Resolve2D()
        {
            if (bodyType == BodyType.Rigidbody2D) return rigidbody2D != null;
            if (bodyType == BodyType.Rigidbody3D) return false;
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