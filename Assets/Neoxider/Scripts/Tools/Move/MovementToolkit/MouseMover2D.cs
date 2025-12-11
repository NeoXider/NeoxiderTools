using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    /// <summary>
    ///     Mouse-based movement component supporting 2D and 3D planes.
    ///     Provides multiple movement modes for different gameplay mechanics.
    /// </summary>
    [RequireComponent(typeof(Transform))]
    [AddComponentMenu("Neo/" + "Tools/" + nameof(MouseMover2D))]
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

        [Header("Point Offset")] [SerializeField]
        private Vector2 offset;

        public UnityEvent OnMoveStart;
        public UnityEvent OnMoveStop;

        // internals
        private Camera cam;
        private Vector2 clickPoint; // Direction mode
        private Vector2 desiredVelocity; // m/s  (только Delta-режимы)
        private bool hasTarget;
        private Vector2 lastMouse;
        private Rigidbody2D rb;
        private Vector2 targetPoint; // ClickToPoint mode
        private bool wasMoving;

        private void Awake()
        {
            cam = Camera.main;
            rb = GetComponent<Rigidbody2D>();
            lastMouse = Input.mousePosition;
        }

        // ------------ Update : читаем ввод, но НЕ двигаем -----------------
        private void Update()
        {
            switch (mode)
            {
                case MoveMode.DeltaNormalized:
                case MoveMode.DeltaRaw:
                    ReadDeltaInput();
                    break;

                case MoveMode.MoveToPointHold:
                    // просто обновляем позицию курсора – сама цель вычислится в FixedUpdate
                    break;

                case MoveMode.ClickToPoint:
                    if (Input.GetMouseButtonDown(0))
                    {
                        targetPoint = ScreenToWorld(Input.mousePosition) + offset;
                        hasTarget = true;
                    }

                    break;

                case MoveMode.Direction:
                    if (Input.GetMouseButtonDown(0))
                    {
                        clickPoint = ScreenToWorld(Input.mousePosition);
                    }

                    break;
            }
        }

        // ------------ FixedUpdate : ДВИГАЕМ физически --------------------
        private void FixedUpdate()
        {
            Vector2 delta = ComputeStep(Time.fixedDeltaTime);
            ApplyDelta(delta);
        }

        // IMover
        public bool IsMoving { get; private set; }

        public void MoveDelta(Vector2 d)
        {
            DirectTranslate(ApplyMask(d));
        }

        public void MoveToPoint(Vector2 p)
        {
            DirectTranslate(ApplyMask(p - (Vector2)transform.position));
        }

        // ---------- вычисляем шаг за fixedDT ------------------------------
        private Vector2 ComputeStep(float dt)
        {
            switch (mode)
            {
                case MoveMode.DeltaNormalized:
                case MoveMode.DeltaRaw:
                    return desiredVelocity * dt;

                case MoveMode.MoveToPointHold:
                {
                    if (!Input.GetMouseButton(0))
                    {
                        return Vector2.zero;
                    }

                    Vector2 world = ScreenToWorld(Input.mousePosition) + offset;
                    return StepTo(world, dt);
                }

                case MoveMode.ClickToPoint:
                {
                    if (!hasTarget)
                    {
                        return Vector2.zero;
                    }

                    Vector2 d = StepTo(targetPoint, dt);
                    if (d == Vector2.zero)
                    {
                        hasTarget = false;
                    }

                    return d;
                }

                case MoveMode.Direction:
                {
                    if (!Input.GetMouseButton(0))
                    {
                        return Vector2.zero;
                    }

                    Vector2 dir = ApplyMask(ScreenToWorld(Input.mousePosition) - clickPoint);
                    if (dir.sqrMagnitude < 1e-4f)
                    {
                        return Vector2.zero;
                    }

                    return dir.normalized * speed * dt;
                }
            }

            return Vector2.zero;
        }

        // ---------- вспомогательные методы --------------------------------
        private void ReadDeltaInput()
        {
            Vector2 cur = Input.mousePosition;
            Vector2 deltaPx = cur - lastMouse;
            lastMouse = cur;

            Vector2 vel = deltaPx * pxToWorld / Time.deltaTime; // px→м, /dt => m/s
            if (mode == MoveMode.DeltaNormalized && vel.sqrMagnitude > 1e-3f)
            {
                vel = vel.normalized * speed;
            }

            desiredVelocity = ApplyMask(vel);
        }

        private Vector2 StepTo(Vector2 worldTarget, float dt)
        {
            Vector2 diff = ApplyMask(worldTarget - (Vector2)transform.position);
            float dist = diff.magnitude;
            if (dist < .02f)
            {
                return Vector2.zero; // уже почти там
            }

            float step = speed * dt;
            return diff.normalized * Mathf.Min(step, dist); // не перепрыгнем
        }

        private void ApplyDelta(Vector2 delta)
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
            return cam ? cam.ScreenToWorldPoint(scr) : scr;
        }
    }
}