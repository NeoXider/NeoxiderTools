using UnityEngine;
using UnityEngine.Events;

public abstract class MovementController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;              // Speed of movement
    public bool inputMouse = false;           // Enable mouse input
    public bool inputKeyboard = false;        // Enable keyboard input
    public bool isNormalized = true;          // Normalize combined inputs for consistent speed
    public Transform target = null;           // Target to move toward
    public float stopDistance = 0f;           // Distance at which to stop when approaching target

    [Header("Movement Limits (Non-Physics Only)")]
    public AxisLimit xLimit = new AxisLimit(true, false, new Vector2(-10f, 10f));
    public AxisLimit yLimit = new AxisLimit(true, false, new Vector2(-10f, 10f));
    public AxisLimit zLimit = new AxisLimit(false, false, new Vector2(-10f, 10f));

    [Header("Events")]
    public UnityEvent OnMove;                 // Invoked when movement starts
    public UnityEvent OnStop;                 // Invoked when movement stops

    [Header("Input Keys")]
    [SerializeField] private KeyCode moveUpKey = KeyCode.W;
    [SerializeField] private KeyCode moveDownKey = KeyCode.S;
    [SerializeField] private KeyCode moveLeftKey = KeyCode.A;
    [SerializeField] private KeyCode moveRightKey = KeyCode.D;

    protected bool isMoving = false;          // Tracks movement state

    protected virtual void Update()
    {
        // Calculate velocity based on inputs or target
        Vector3 velocity = CalculateVelocity();

        // Apply movement (implemented by derived classes)
        ApplyMovement(velocity);

        // Trigger movement events
        if (velocity != Vector3.zero && !isMoving)
        {
            isMoving = true;
            OnMove?.Invoke();
        }
        else if (velocity == Vector3.zero && isMoving)
        {
            isMoving = false;
            OnStop?.Invoke();
        }
    }

    /// <summary>
    /// Calculates desired velocity based on enabled input sources or target.
    /// </summary>
    public Vector3 CalculateVelocity()
    {
        // Prioritize target-following movement
        if (target != null)
        {
            Vector3 targetDirection = (target.position - transform.position).normalized;
            float distance = Vector3.Distance(transform.position, target.position);
            if (distance > stopDistance)
            {
                return targetDirection * moveSpeed;
            }
            return Vector3.zero;
        }

        Vector3 velocity = Vector3.zero;

        // Accumulate keyboard input
        if (inputKeyboard)
        {
            if (Input.GetKey(moveUpKey)) velocity += Vector3.up;
            if (Input.GetKey(moveDownKey)) velocity += Vector3.down;
            if (Input.GetKey(moveLeftKey)) velocity += Vector3.left;
            if (Input.GetKey(moveRightKey)) velocity += Vector3.right;
        }

        // Add mouse input (move toward click position)
        if (inputMouse && Input.GetMouseButton(0))
        {
            Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePosition.z = 0; // Assuming 2D; adjust for 3D if needed
            Vector3 mouseDirection = (mousePosition - transform.position).normalized;
            velocity += mouseDirection;
        }

        // Apply speed and normalization
        if (isNormalized && velocity != Vector3.zero)
        {
            velocity = velocity.normalized * moveSpeed;
        }
        else
        {
            velocity *= moveSpeed;
        }

        return velocity;
    }

    /// <summary>
    /// Abstract method to apply the calculated velocity. Implemented by derived classes.
    /// </summary>
    protected abstract void ApplyMovement(Vector3 velocity);
}

[System.Serializable]
public class AxisLimit
{
    public bool move;        // Whether movement is allowed on this axis
    public bool useLimit;    // Whether to clamp movement within limits
    public Vector2 limit;    // Min and max values for the axis

    public AxisLimit(bool move, bool useLimit, Vector2 limit)
    {
        this.move = move;
        this.useLimit = useLimit;
        this.limit = limit;
    }
}