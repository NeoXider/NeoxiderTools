using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    /// <summary>
    ///     Mouse-based movement component supporting 2D and 3D planes.
    ///     Provides multiple movement modes for different gameplay mechanics.
    /// </summary>
    [RequireComponent(typeof(Transform))]
    [NeoDoc("Tools/Move/MovementToolkit/MouseMover2D.md")]
    [CreateFromMenu("Neoxider/Tools/Movement/MouseMover2D")]
    [AddComponentMenu("Neoxider/" + "Tools/" + nameof(MouseMover2D))]
    public class MouseMover2D : MonoBehaviour, IMover
    {
        public enum AxisMask
        {
            XY,
            XZ,
            YZ,
            X,
            Y,
            Z
        }

        public enum MoveMode
        {
            DeltaNormalized,
            DeltaRaw,
            MoveToPointHold,
            ClickToPoint,
            Direction
        }

        [Header("Mode")] [SerializeField] private MoveMode mode = MoveMode.DeltaNormalized;

        [Header("Axis Mask")] [Tooltip("Movement plane/axis restriction.")] [SerializeField]
        private AxisMask axes = AxisMask.XY;

        [Header("Speed")] [Tooltip("Units per second.")] [SerializeField]
        private float speed = 5f;

        [Header("Δ-sensitivity")] [Tooltip("Pixel to world unit conversion factor.")] [SerializeField]
        private float pxToWorld = .01f;

        [Header("Mouse")] [Tooltip("Mouse button index (0=left, 1=right, 2=middle).")] [SerializeField]
        private int mouseButton;

        [Tooltip(
            "If true, Delta modes only move while this mouse button is held. If false, movement follows mouse constantly.")]
        [SerializeField]
        private bool deltaOnlyWhenButtonHeld = true;

        [Tooltip("Invert horizontal axis in Delta modes (e.g. mouse right -> move left).")] [SerializeField]
        private bool invertDeltaX;

        [Tooltip("Invert vertical axis in Delta modes (e.g. mouse up -> move down).")] [SerializeField]
        private bool invertDeltaY;

        [Tooltip("Distance to target below which movement is considered arrived.")] [SerializeField]
        private float arrivalThreshold = 0.02f;

        [Header("Point Offset")] [SerializeField]
        private Vector2 offset;

        public UnityEvent OnMoveStart;
        public UnityEvent OnMoveStop;

        [Header("Diagnostics")] [SerializeField]
        private bool _logMissingCameraWarning;

        private bool _cameraWarningShown;

        private Camera cam;
        private Vector2 clickPoint; // WHY: used by Direction mode
        private Vector2 desiredVelocity; // WHY: m/s (Delta modes only)
        private bool hasTarget;
        private Vector2 lastMouse;
        private Rigidbody2D rb;
        private Vector2 targetPoint; // WHY: used by ClickToPoint mode
        private bool wasMoving;

        private void Awake()
        {
            if (cam == null)
            {
                cam = Camera.main;
            }

            if (cam == null && _logMissingCameraWarning && !_cameraWarningShown)
            {
                _cameraWarningShown = true;
                NeoDiagnostics.LogWarning(
                    "[MouseMover2D] No camera assigned and Camera.main is null. ScreenToWorld will return screen coordinates.",
                    this);
            }

            rb = GetComponent<Rigidbody2D>();
            MouseInputCompat.TryGetPosition(out Vector3 mousePosition);
            lastMouse = mousePosition;
        }

        // WHY: Update only reads input; movement is applied in FixedUpdate
        private void Update()
        {
            bool buttonDown = MouseInputCompat.TryGetButtonDown(mouseButton, out bool pressed) && pressed;
            switch (mode)
            {
                case MoveMode.DeltaNormalized:
                case MoveMode.DeltaRaw:
                    ReadDeltaInput();
                    break;

                case MoveMode.MoveToPointHold:
                    // WHY: update cursor only; target resolves in FixedUpdate
                    break;

                case MoveMode.ClickToPoint:
                    if (buttonDown && MouseInputCompat.TryGetPosition(out Vector3 targetMousePosition))
                    {
                        targetPoint = ScreenToWorld(targetMousePosition) + offset;
                        hasTarget = true;
                    }

                    break;

                case MoveMode.Direction:
                    if (buttonDown && MouseInputCompat.TryGetPosition(out Vector3 directionMousePosition))
                    {
                        clickPoint = ScreenToWorld(directionMousePosition);
                    }

                    break;
            }
        }

        private void FixedUpdate()
        {
            Vector2 delta = ComputeStep(Time.fixedDeltaTime);
            ApplyDelta(delta);
        }

        public bool IsMoving { get; private set; }

        public void MoveDelta(Vector2 d)
        {
            DirectTranslate(ApplyMask(d));
        }

        public void MoveToPoint(Vector2 p)
        {
            DirectTranslate(ApplyMask(p - WorldToPlane(transform.position)));
        }

        private Vector2 ComputeStep(float dt)
        {
            switch (mode)
            {
                case MoveMode.DeltaNormalized:
                case MoveMode.DeltaRaw:
                    return desiredVelocity * dt;

                case MoveMode.MoveToPointHold:
                {
                    if (!MouseInputCompat.TryGetButton(mouseButton, out bool held) || !held ||
                        !MouseInputCompat.TryGetPosition(out Vector3 mousePosition))
                    {
                        return Vector2.zero;
                    }

                    Vector2 world = ScreenToWorld(mousePosition) + offset;
                    return StepTo(world, dt);
                }

                case MoveMode.ClickToPoint:
                {
                    if (!hasTarget)
                    {
                        return Vector2.zero;
                    }

                    Vector2 d = StepTo(targetPoint, dt);
                    if (d.sqrMagnitude == 0f)
                    {
                        hasTarget = false;
                    }

                    return d;
                }

                case MoveMode.Direction:
                {
                    if (!MouseInputCompat.TryGetButton(mouseButton, out bool held) || !held ||
                        !MouseInputCompat.TryGetPosition(out Vector3 mousePosition))
                    {
                        return Vector2.zero;
                    }

                    Vector2 dir = ApplyMask(ScreenToWorld(mousePosition) - clickPoint);
                    if (dir.sqrMagnitude < 1e-4f)
                    {
                        return Vector2.zero;
                    }

                    return dir.normalized * speed * dt;
                }
            }

            return Vector2.zero;
        }

        private void ReadDeltaInput()
        {
            if (!MouseInputCompat.TryGetPosition(out Vector3 mousePosition))
            {
                desiredVelocity = Vector2.zero;
                return;
            }

            Vector2 cur = mousePosition;
            bool held = MouseInputCompat.TryGetButton(mouseButton, out bool pressed) && pressed;
            if (deltaOnlyWhenButtonHeld && !held)
            {
                lastMouse = cur;
                desiredVelocity = Vector2.zero;
                return;
            }

            // WHY: Time.deltaTime is 0 while timeScale == 0; dividing by it would store an
            // Infinity velocity that teleports the object on the first physics step after unpause.
            if (Time.deltaTime <= 0f)
            {
                lastMouse = cur;
                desiredVelocity = Vector2.zero;
                return;
            }

            Vector2 deltaPx = cur - lastMouse;
            if (invertDeltaX)
            {
                deltaPx.x = -deltaPx.x;
            }

            if (invertDeltaY)
            {
                deltaPx.y = -deltaPx.y;
            }

            lastMouse = cur;

            Vector2 vel = deltaPx * pxToWorld / Time.deltaTime; // WHY: px to m, /dt => m/s
            if (mode == MoveMode.DeltaNormalized && vel.sqrMagnitude > 1e-3f)
            {
                vel = vel.normalized * speed;
            }

            desiredVelocity = ApplyMask(vel);
        }

        private Vector2 StepTo(Vector2 worldTarget, float dt)
        {
            Vector2 diff = ApplyMask(worldTarget - WorldToPlane(transform.position));
            float dist = diff.magnitude;
            if (dist < arrivalThreshold)
            {
                return Vector2.zero;
            }

            float step = speed * dt;
            return diff.normalized * Mathf.Min(step, dist); // WHY: do not overshoot
        }

        private void ApplyDelta(Vector2 delta)
        {
            bool movingNow = delta.sqrMagnitude > 0f;
            if (movingNow && !wasMoving)
            {
                OnMoveStart?.Invoke();
            }

            if (!movingNow && wasMoving)
            {
                OnMoveStop?.Invoke();
            }

            IsMoving = movingNow;
            wasMoving = movingNow;

            if (!movingNow)
            {
                return;
            }

            if (rb)
            {
                rb.MovePosition(rb.position + delta);
            }
            else
            {
                DirectTranslate(delta);
            }
        }

        private void DirectTranslate(Vector2 d)
        {
            Vector3 delta3D = MapToPlane(d);
            transform.Translate(delta3D, Space.World);
        }

        private Vector2 ApplyMask(Vector2 v)
        {
            return axes switch
            {
                AxisMask.XY => v,
                AxisMask.XZ => v,
                AxisMask.YZ => v,
                AxisMask.X => new Vector2(v.x, 0),
                AxisMask.Y => new Vector2(0, v.y),
                AxisMask.Z => new Vector2(0, v.y),
                _ => v
            };
        }

        private Vector3 MapToPlane(Vector2 input)
        {
            return axes switch
            {
                AxisMask.XY => new Vector3(input.x, input.y, 0f),
                AxisMask.XZ => new Vector3(input.x, 0f, input.y),
                AxisMask.YZ => new Vector3(0f, input.x, input.y),
                AxisMask.X => new Vector3(input.x, 0f, 0f),
                AxisMask.Y => new Vector3(0f, input.y, 0f),
                AxisMask.Z => new Vector3(0f, 0f, input.y),
                _ => new Vector3(input.x, input.y, 0f)
            };
        }

        private Vector2 ScreenToWorld(Vector3 scr)
        {
            if (cam == null)
            {
                return scr;
            }

            Vector3 normal = axes switch
            {
                AxisMask.XZ or AxisMask.Z => Vector3.up,
                AxisMask.YZ => Vector3.right,
                _ => Vector3.forward
            };
            Plane movementPlane = new Plane(normal, transform.position);
            Ray ray = cam.ScreenPointToRay(scr);
            return movementPlane.Raycast(ray, out float distance)
                ? WorldToPlane(ray.GetPoint(distance))
                : WorldToPlane(transform.position);
        }

        private Vector2 WorldToPlane(Vector3 position)
        {
            return axes switch
            {
                AxisMask.XZ => new Vector2(position.x, position.z),
                AxisMask.YZ => new Vector2(position.y, position.z),
                AxisMask.Z => new Vector2(0f, position.z),
                _ => new Vector2(position.x, position.y)
            };
        }
    }
}
