using System;
using UnityEngine;
using UnityEngine.Events;

public enum MovementType
{
    DirectionBased,
    PositionBased
}

public class MovementController : MonoBehaviour
{
    [Header("Movement Settings")] [SerializeField]
    private MovementType movementType = MovementType.DirectionBased;

    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private bool useNormalization = true;
    [SerializeField] private Vector3 positionOffset;

    [Header("Constraints")] [SerializeField]
    private AxisConstraint xConstraint = new();

    [SerializeField] private AxisConstraint yConstraint = new();
    [SerializeField] private AxisConstraint zConstraint = new();

    [Header("Events")] public UnityEvent OnMovementStart;
    public UnityEvent OnMovementStop;

    private IMovementInputProvider _inputProvider;
    private bool _isMoving;
    private Vector3 _referencePosition;

    private void Awake()
    {
        _inputProvider = GetComponent<IMovementInputProvider>();
        InitializeReferencePosition();
    }

    private void Update()
    {
        UpdateMovementState();
        HandleMovement();
    }

    private void InitializeReferencePosition()
    {
        _referencePosition = transform.position - positionOffset;
    }

    private void UpdateMovementState()
    {
        var isMovingNow = _inputProvider.IsInputActive();

        if (isMovingNow != _isMoving)
        {
            _isMoving = isMovingNow;
            RaiseMovementEvents();
        }
    }

    private void RaiseMovementEvents()
    {
        if (_isMoving) OnMovementStart?.Invoke();
        else OnMovementStop?.Invoke();
    }

    private void HandleMovement()
    {
        if (!_isMoving) return;

        switch (movementType)
        {
            case MovementType.DirectionBased:
                MoveByDirection(_inputProvider.GetDirection());
                break;

            case MovementType.PositionBased:
                MoveToPosition(_inputProvider.GetTargetPosition());
                break;
        }

        ApplyConstraints();
        UpdateTransformPosition();
    }

    private void MoveByDirection(Vector3 direction)
    {
        if (useNormalization && direction != Vector3.zero)
            direction.Normalize();

        _referencePosition += direction * (moveSpeed * Time.deltaTime);
    }

    private void MoveToPosition(Vector3 targetPosition)
    {
        _referencePosition = Vector3.MoveTowards(
            _referencePosition,
            targetPosition - positionOffset,
            moveSpeed * Time.deltaTime
        );
    }

    private void ApplyConstraints()
    {
        if (xConstraint.enabled)
            _referencePosition.x = Mathf.Clamp(_referencePosition.x, xConstraint.min, xConstraint.max);

        if (yConstraint.enabled)
            _referencePosition.y = Mathf.Clamp(_referencePosition.y, yConstraint.min, yConstraint.max);

        if (zConstraint.enabled)
            _referencePosition.z = Mathf.Clamp(_referencePosition.z, zConstraint.min, zConstraint.max);
    }

    private void UpdateTransformPosition()
    {
        transform.position = _referencePosition + positionOffset;
    }
}

[Serializable]
public class AxisConstraint
{
    public bool enabled;
    public float min = -10f;
    public float max = 10f;
}

public interface IMovementInputProvider
{
    bool IsInputActive();
    Vector3 GetDirection();
    Vector3 GetTargetPosition();
}