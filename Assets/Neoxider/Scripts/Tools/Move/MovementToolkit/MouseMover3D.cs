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

[RequireComponent(typeof(Transform))]
public class MouseMover3D : MonoBehaviour
{
    // ── PUBLIC CONFIG ───────────────────────────────────────────────
    public enum MoveMode  { DeltaNormalized, DeltaRaw, MoveToPointHold, ClickToPoint, Direction }
    public enum AxisPlane { XZ, XY, YZ, X, Y, Z }

    [Header("Mode")]          [SerializeField] MoveMode  mode      = MoveMode.DeltaNormalized;
    [Header("Plane / Axis")]  [SerializeField] AxisPlane plane     = AxisPlane.XZ;
    [Header("Speed (u/s)")]   [SerializeField] float     speed     = 6f;
    [Header("Δ-sensitivity")] [SerializeField] float     pxToWorld = 0.01f;   // 1 px → m
    [Header("Raycast mask (optional ground)")]
    [SerializeField] LayerMask groundMask;

    [Header("Events")]  public UnityEvent OnMoveStart; public UnityEvent OnMoveStop;

    // ── STATE ───────────────────────────────────────────────────────
    public bool IsMoving { get; private set; }

    Camera      cam;
    Rigidbody   rb;

    Vector3     desiredVel;       // m/s (только Delta-режимы)
    Vector3     clickPoint;       // Direction-mode
    Vector3     targetPoint;      // ClickToPoint-mode
    bool        hasTarget;
    Vector3     lastMousePos;     // px
    bool        wasMoving;

    // ── UNITY ───────────────────────────────────────────────────────
    void Awake()
    {
        cam          = Camera.main;
        rb           = GetComponent<Rigidbody>();
        lastMousePos = Input.mousePosition;
    }

    void Update()
    {
        ReadInput();              // не двигаемся здесь
    }

    void FixedUpdate()
    {
        Vector3 step = ComputeStep(Time.fixedDeltaTime);
        ApplyStep(step);
    }

    // ── INPUT --------------------------------------------------------
    void ReadInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (mode == MoveMode.ClickToPoint)
            {
                if (RaycastCursor(out var pt)) { targetPoint = pt; hasTarget = true; }
            }
            else if (mode == MoveMode.Direction)
            {
                RaycastCursor(out clickPoint);
            }
        }

        if (mode == MoveMode.DeltaNormalized || mode == MoveMode.DeltaRaw)
        {
            Vector3 cur = Input.mousePosition;
            Vector2 px  = (cur - lastMousePos);
            lastMousePos = cur;

            Vector3 vel = PxToWorld(px) / Time.deltaTime;   // m/s
            if (mode == MoveMode.DeltaNormalized && vel.sqrMagnitude > 1e-4f)
                vel = vel.normalized * speed;
            desiredVel = RestrictAxes(vel);
        }
    }

    // ── STEP CALCULATION --------------------------------------------
    Vector3 ComputeStep(float dt)
    {
        switch (mode)
        {
            case MoveMode.DeltaNormalized:
            case MoveMode.DeltaRaw:
                return desiredVel * dt;

            case MoveMode.MoveToPointHold:
            {
                if (!Input.GetMouseButton(0) || !RaycastCursor(out var cur)) return Vector3.zero;
                return MoveTowards(cur, dt);
            }

            case MoveMode.ClickToPoint:
            {
                if (!hasTarget) return Vector3.zero;
                Vector3 d = MoveTowards(targetPoint, dt);
                if (d == Vector3.zero) hasTarget = false;
                return d;
            }

            case MoveMode.Direction:
            {
                if (!Input.GetMouseButton(0) || !RaycastCursor(out var cur)) return Vector3.zero;
                Vector3 dir = RestrictAxes(cur - clickPoint);
                if (dir.sqrMagnitude < 1e-4f) return Vector3.zero;
                return dir.normalized * speed * dt;
            }
        }
        return Vector3.zero;
    }

    // ── APPLY STEP ---------------------------------------------------
    void ApplyStep(Vector3 delta)
    {
        bool movingNow = delta.sqrMagnitude > 1e-4f;
        if (movingNow && !wasMoving) OnMoveStart?.Invoke();
        if (!movingNow && wasMoving) OnMoveStop?.Invoke();

        IsMoving  = movingNow;
        wasMoving = movingNow;

        if (!movingNow) return;

        if (rb) rb.MovePosition(rb.position + delta);
        else    transform.Translate(delta, Space.World);
    }

    // ── HELPERS ------------------------------------------------------
    Vector3 MoveTowards(Vector3 target, float dt)
    {
        Vector3 diff = RestrictAxes(target - transform.position);
        if (diff.magnitude < 0.05f) return Vector3.zero;

        float step = speed * dt;
        return diff.normalized * Mathf.Min(step, diff.magnitude);
    }

    Vector3 PxToWorld(Vector2 px)
    {
        return plane switch
        {
            AxisPlane.XZ => new Vector3(px.x, 0, px.y) * pxToWorld,
            AxisPlane.XY => new Vector3(px.x, px.y, 0) * pxToWorld,
            AxisPlane.YZ => new Vector3(0, px.x, px.y) * pxToWorld,
            AxisPlane.X  => new Vector3(px.x, 0, 0)   * pxToWorld,
            AxisPlane.Y  => new Vector3(0, px.y, 0)   * pxToWorld,
            AxisPlane.Z  => new Vector3(0, 0, px.y)   * pxToWorld,
            _            => Vector3.zero
        };
    }

    Vector3 RestrictAxes(Vector3 v) => plane switch
    {
        AxisPlane.XZ => new Vector3(v.x, 0, v.z),
        AxisPlane.XY => new Vector3(v.x, v.y, 0),
        AxisPlane.YZ => new Vector3(0,  v.y, v.z),
        AxisPlane.X  => new Vector3(v.x, 0,   0),
        AxisPlane.Y  => new Vector3(0,   v.y, 0),
        AxisPlane.Z  => new Vector3(0,   0,   v.z),
        _            => v
    };

    bool RaycastCursor(out Vector3 world)
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        // 1) Physics raycast, если указан mask
        if (groundMask.value != 0 && Physics.Raycast(ray, out var hit, 1000f, groundMask))
        {
            world = hit.point; return true;
        }

        // 2) Пересекаем геометрическую плоскость
        Plane pl = plane switch
        {
            AxisPlane.XZ => new Plane(Vector3.up, transform.position),
            AxisPlane.XY => new Plane(Vector3.forward, transform.position),
            AxisPlane.YZ => new Plane(Vector3.right, transform.position),
            _            => new Plane(Vector3.up, transform.position)
        };
        if (pl.Raycast(ray, out float t))
        {
            world = ray.GetPoint(t); return true;
        }

        world = Vector3.zero; return false;
    }
}
