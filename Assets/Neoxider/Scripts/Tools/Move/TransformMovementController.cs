using UnityEngine;
using UnityEngine.Events;


public class TransformMovementController : MovementController
{
    protected override void ApplyMovement(Vector3 velocity)
    {
        // Calculate new position and apply limits
        Vector3 newPosition = transform.position + velocity * Time.deltaTime;
        newPosition = ApplyLimits(newPosition);
        transform.position = newPosition;
    }

    /// <summary>
    /// Clamps the position based on configured axis limits.
    /// </summary>
    private Vector3 ApplyLimits(Vector3 position)
    {
        if (xLimit.move && xLimit.useLimit)
        {
            position.x = Mathf.Clamp(position.x, xLimit.limit.x, xLimit.limit.y);
        }
        if (yLimit.move && yLimit.useLimit)
        {
            position.y = Mathf.Clamp(position.y, yLimit.limit.x, yLimit.limit.y);
        }
        if (zLimit.move && zLimit.useLimit)
        {
            position.z = Mathf.Clamp(position.z, zLimit.limit.x, zLimit.limit.y);
        }
        return position;
    }
}