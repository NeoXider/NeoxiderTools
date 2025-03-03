using UnityEngine;

public class PhysicsMovementController : MovementController
{
    private Rigidbody rb3D;
    private Rigidbody2D rb2D;

    protected void Awake()
    {
        // Automatically detect physics components
        rb3D = GetComponent<Rigidbody>();
        rb2D = GetComponent<Rigidbody2D>();
    }

    protected override void ApplyMovement(Vector3 velocity)
    {
        if (rb3D != null)
        {
            rb3D.velocity = velocity;
        }
        else if (rb2D != null)
        {
            rb2D.velocity = velocity;
        }
    }
}