using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools{
/// <summary>
///     Moves a 2‑D object based on keyboard / joystick axes.
///     If a Rigidbody2D is present the script moves in <c>FixedUpdate</c>
///     via <c>MovePosition</c>; otherwise it translates the Transform in
///     <c>Update</c>.  Fires UnityEvents when motion begins or ends and
///     implements <see cref="IMover" /> for external control.
/// </summary>
public class KeyboardMover2D : MonoBehaviour, IMover
{
    #region === public configuration ===

    public enum AxisMode
    {
        AxisNormalized, // normalised Vector2 * speed
        AxisRaw // raw axis values * speed
    }

    [Header("Mode")] [Tooltip("How the axis vector is treated.")] [SerializeField]
    private AxisMode axisMode = AxisMode.AxisRaw;

    [Header("Speed")] [Tooltip("Units per second.")] [SerializeField]
    private float speed = 5f;

    [Header("Events")] public UnityEvent OnMoveStart;
    public UnityEvent OnMoveStop;

    #endregion

    //--------------------------------------------------------------------

    #region === IMover ===

    /// <inheritdoc />
    public bool IsMoving { get; private set; }

    /// <inheritdoc />
    public void MoveDelta(Vector2 delta)
    {
        transform.Translate(delta, Space.World);
    }

    /// <inheritdoc />
    public void MoveToPoint(Vector2 worldPoint)
    {
        transform.position = worldPoint;
    }

    #endregion

    //--------------------------------------------------------------------

    #region === private fields ===

    private Rigidbody2D _rb;
    private Vector2 _cachedDelta;
    private bool _wasMovingLast;

    #endregion

    //--------------------------------------------------------------------

    #region === unity callbacks ===

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        _cachedDelta = ComputeDelta(Time.deltaTime);

        // If kinematic (no Rigidbody2D) – move right away
        if (!_rb) ApplyDelta(_cachedDelta);
    }

    private void FixedUpdate()
    {
        if (!_rb) return;

        var k = Time.fixedDeltaTime / Time.deltaTime;
        ApplyDelta(_cachedDelta * k); // preserves speed in physics step
    }

    #endregion

    //--------------------------------------------------------------------

    #region === movement logic ===

    private Vector2 ComputeDelta(float dt)
    {
        var h = Input.GetAxisRaw("Horizontal");
        var v = Input.GetAxisRaw("Vertical");
        var dir = new Vector2(h, v);

        if (axisMode == AxisMode.AxisNormalized && dir.sqrMagnitude > 0.001f)
            dir.Normalize();

        return dir * speed * dt;
    }

    private void ApplyDelta(Vector2 delta)
    {
        var movingNow = delta.sqrMagnitude > 0.0001f;

        if (movingNow && !_wasMovingLast) OnMoveStart?.Invoke();
        if (!movingNow && _wasMovingLast) OnMoveStop?.Invoke();

        IsMoving = movingNow;
        _wasMovingLast = movingNow;

        if (!movingNow) return;

        if (_rb) _rb.MovePosition(_rb.position + delta);
        else transform.Translate(delta, Space.World);
    }

    #endregion
}
}