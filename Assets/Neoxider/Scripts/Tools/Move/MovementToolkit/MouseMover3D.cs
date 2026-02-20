/*  MouseMover3D.cs
 *  Legacy-Input universal **3-D** mouse mover
 *  (physics-friendly, без дрожаний)
 *
 *  Modes ───────────────────────────────────────────────────────────
 *  • DeltaNormalized / DeltaRaw  – движение по мышиной Δ
 *  • MoveToPointHold             – LMB зажат → едем к курсору
 *  • ClickToPoint                – клик → цель, едем пока не доедем
 *  • Direction                   – клик точку, мышь задаёт вектор
 *
 *  AxisPlane ограничивает плоскость (XZ = топ-даун, XY, YZ)
 *  либо единственную ось X / Y / Z.
 *
 *  Движение выполняется ОДИН раз за phys-тик:
 *  _desiredVel вычисляется в Update (только Δ-режимы),
 *  в FixedUpdate → Rigidbody.MovePosition -> нет jitter.
 */

using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    [RequireComponent(typeof(Transform))]
    [NeoDoc("Tools/Move/MovementToolkit/MouseMover3D.md")]
    [CreateFromMenu("Neoxider/Tools/Movement/MouseMover3D")]
    [AddComponentMenu("Neoxider/" + "Tools/" + nameof(MouseMover3D))]
    public class MouseMover3D : MonoBehaviour, IMover
    {
        public enum AxisPlane
        {
            XZ,
            XY,
            YZ,
            X,
            Y,
            Z
        }

        // ── PUBLIC CONFIG ───────────────────────────────────────────────
        public enum MoveMode
        {
            DeltaNormalized,
            DeltaRaw,
            MoveToPointHold,
            ClickToPoint,
            Direction
        }

        [Header("Mode")] [SerializeField] private MoveMode mode = MoveMode.DeltaNormalized;

        [Header("Plane / Axis")] [SerializeField]
        private AxisPlane plane = AxisPlane.XZ;

        [Header("Speed (u/s)")] [SerializeField]
        private float speed = 6f;

        [Header("Δ-sensitivity")] [SerializeField]
        private float pxToWorld = 0.01f; // 1 px → m

        [Header("Raycast mask (optional ground)")] [SerializeField]
        private LayerMask groundMask;

        [Header("Mouse")] [Tooltip("Mouse button index (0=left, 1=right, 2=middle).")] [SerializeField]
        private int mouseButton = 0;

        [Tooltip("If true, Delta modes only move while this mouse button is held. If false, movement follows mouse constantly.")]
        [SerializeField] private bool deltaOnlyWhenButtonHeld = true;

        [Tooltip("Invert horizontal axis in Delta modes (e.g. mouse right → move left).")]
        [SerializeField] private bool invertDeltaX;

        [Tooltip("Invert vertical axis in Delta modes (e.g. mouse up → move down).")]
        [SerializeField] private bool invertDeltaY;

        [Tooltip("Distance to target below which movement is considered arrived.")] [SerializeField]
        private float arrivalThreshold = 0.05f;

        public UnityEvent OnMoveStart;
        public UnityEvent OnMoveStop;

        private Camera cam;
        private Vector3 clickPoint; // Direction-mode

        private Vector3 desiredVel; // m/s (только Delta-режимы)
        private bool hasTarget;
        private Vector3 lastMousePos; // px
        private Rigidbody rb;
        private Vector3 targetPoint; // ClickToPoint-mode
        private bool wasMoving;
        private bool _cameraWarningShown;

        // ── STATE ───────────────────────────────────────────────────────
        public bool IsMoving { get; private set; }

        // ── IMover ──────────────────────────────────────────────────────
        public void MoveDelta(Vector2 delta)
        {
            Vector3 d = MapVector2ToPlaneDelta(delta);
            if (rb)
                rb.MovePosition(rb.position + d);
            else
                transform.Translate(d, Space.World);
        }

        public void MoveToPoint(Vector2 worldTarget)
        {
            Vector3 pos = Vector2ToPlanePosition(worldTarget);
            if (rb)
                rb.MovePosition(pos);
            else
                transform.position = pos;
        }

        // ── UNITY ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (cam == null)
                cam = Camera.main;
            if (cam == null && !_cameraWarningShown)
            {
                _cameraWarningShown = true;
                Debug.LogWarning("[MouseMover3D] No camera assigned and Camera.main is null. Raycast and plane projection may fail.", this);
            }
            rb = GetComponent<Rigidbody>();
            lastMousePos = Input.mousePosition;
        }

        private void Update()
        {
            ReadInput(); // не двигаемся здесь
        }

        private void FixedUpdate()
        {
            Vector3 step = ComputeStep(Time.fixedDeltaTime);
            ApplyStep(step);
        }

        // ── INPUT --------------------------------------------------------
        private void ReadInput()
        {
            if (Input.GetMouseButtonDown(mouseButton))
            {
                if (mode == MoveMode.ClickToPoint)
                {
                    if (RaycastCursor(out Vector3 pt))
                    {
                        targetPoint = pt;
                        hasTarget = true;
                    }
                }
                else if (mode == MoveMode.Direction)
                {
                    RaycastCursor(out clickPoint);
                }
            }

            if (mode == MoveMode.DeltaNormalized || mode == MoveMode.DeltaRaw)
            {
                Vector3 cur = Input.mousePosition;
                if (deltaOnlyWhenButtonHeld && !Input.GetMouseButton(mouseButton))
                {
                    lastMousePos = cur;
                    desiredVel = Vector3.zero;
                }
                else
                {
                    Vector2 px = (Vector2)cur - (Vector2)lastMousePos;
                    if (invertDeltaX) px.x = -px.x;
                    if (invertDeltaY) px.y = -px.y;
                    lastMousePos = cur;

                    Vector3 vel = PxToWorld(px) / Time.deltaTime; // m/s
                    if (mode == MoveMode.DeltaNormalized && vel.sqrMagnitude > 1e-4f)
                    {
                        vel = vel.normalized * speed;
                    }

                    desiredVel = RestrictAxes(vel);
                }
            }
        }

        // ── STEP CALCULATION --------------------------------------------
        private Vector3 ComputeStep(float dt)
        {
            switch (mode)
            {
                case MoveMode.DeltaNormalized:
                case MoveMode.DeltaRaw:
                    return desiredVel * dt;

                case MoveMode.MoveToPointHold:
                {
                    if (!Input.GetMouseButton(mouseButton) || !RaycastCursor(out Vector3 cur))
                    {
                        return Vector3.zero;
                    }

                    return MoveTowards(cur, dt);
                }

                case MoveMode.ClickToPoint:
                {
                    if (!hasTarget)
                    {
                        return Vector3.zero;
                    }

                    Vector3 d = MoveTowards(targetPoint, dt);
                    if (d == Vector3.zero)
                    {
                        hasTarget = false;
                    }

                    return d;
                }

                case MoveMode.Direction:
                {
                    if (!Input.GetMouseButton(mouseButton) || !RaycastCursor(out Vector3 cur))
                    {
                        return Vector3.zero;
                    }

                    Vector3 dir = RestrictAxes(cur - clickPoint);
                    if (dir.sqrMagnitude < 1e-4f)
                    {
                        return Vector3.zero;
                    }

                    return dir.normalized * speed * dt;
                }
            }

            return Vector3.zero;
        }

        // ── APPLY STEP ---------------------------------------------------
        private void ApplyStep(Vector3 delta)
        {
            bool movingNow = delta.sqrMagnitude > 1e-4f;
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
                transform.Translate(delta, Space.World);
            }
        }

        // ── HELPERS ------------------------------------------------------
        private Vector3 MoveTowards(Vector3 target, float dt)
        {
            Vector3 diff = RestrictAxes(target - transform.position);
            if (diff.magnitude < arrivalThreshold)
            {
                return Vector3.zero;
            }

            float step = speed * dt;
            return diff.normalized * Mathf.Min(step, diff.magnitude);
        }

        private Vector3 PxToWorld(Vector2 px)
        {
            return plane switch
            {
                AxisPlane.XZ => new Vector3(px.x, 0, px.y) * pxToWorld,
                AxisPlane.XY => new Vector3(px.x, px.y, 0) * pxToWorld,
                AxisPlane.YZ => new Vector3(0, px.x, px.y) * pxToWorld,
                AxisPlane.X => new Vector3(px.x, 0, 0) * pxToWorld,
                AxisPlane.Y => new Vector3(0, px.y, 0) * pxToWorld,
                AxisPlane.Z => new Vector3(0, 0, px.y) * pxToWorld,
                _ => Vector3.zero
            };
        }

        private Vector3 RestrictAxes(Vector3 v)
        {
            return plane switch
            {
                AxisPlane.XZ => new Vector3(v.x, 0, v.z),
                AxisPlane.XY => new Vector3(v.x, v.y, 0),
                AxisPlane.YZ => new Vector3(0, v.y, v.z),
                AxisPlane.X => new Vector3(v.x, 0, 0),
                AxisPlane.Y => new Vector3(0, v.y, 0),
                AxisPlane.Z => new Vector3(0, 0, v.z),
                _ => v
            };
        }

        private Vector3 MapVector2ToPlaneDelta(Vector2 d)
        {
            return plane switch
            {
                AxisPlane.XZ => new Vector3(d.x, 0, d.y),
                AxisPlane.XY => new Vector3(d.x, d.y, 0),
                AxisPlane.YZ => new Vector3(0, d.x, d.y),
                AxisPlane.X => new Vector3(d.x, 0, 0),
                AxisPlane.Y => new Vector3(0, d.y, 0),
                AxisPlane.Z => new Vector3(0, 0, d.y),
                _ => new Vector3(d.x, 0, d.y)
            };
        }

        private Vector3 Vector2ToPlanePosition(Vector2 p)
        {
            Vector3 pos = transform.position;
            return plane switch
            {
                AxisPlane.XZ => new Vector3(p.x, pos.y, p.y),
                AxisPlane.XY => new Vector3(p.x, p.y, pos.z),
                AxisPlane.YZ => new Vector3(pos.x, p.x, p.y),
                AxisPlane.X => new Vector3(p.x, pos.y, pos.z),
                AxisPlane.Y => new Vector3(pos.x, p.y, pos.z),
                AxisPlane.Z => new Vector3(pos.x, pos.y, p.y),
                _ => new Vector3(p.x, pos.y, p.y)
            };
        }

        private bool RaycastCursor(out Vector3 world)
        {
            if (cam == null)
            {
                world = Vector3.zero;
                return false;
            }
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);

            // 1) Physics raycast, если указан mask
            if (groundMask.value != 0 && Physics.Raycast(ray, out RaycastHit hit, 1000f, groundMask))
            {
                world = hit.point;
                return true;
            }

            // 2) Пересекаем геометрическую плоскость
            Plane pl = plane switch
            {
                AxisPlane.XZ => new Plane(Vector3.up, transform.position),
                AxisPlane.XY => new Plane(Vector3.forward, transform.position),
                AxisPlane.YZ => new Plane(Vector3.right, transform.position),
                _ => new Plane(Vector3.up, transform.position)
            };
            if (pl.Raycast(ray, out float t))
            {
                world = ray.GetPoint(t);
                return true;
            }

            world = Vector3.zero;
            return false;
        }
    }
}