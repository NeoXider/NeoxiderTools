
# [RequireInterface]

**What it is:** The `[RequireInterface]` attribute is a tool for improving the reliability and architectural integrity of your code. It lets you specify that an inspector field, even if it has a generic type (for example,...

**How to use:** see the sections below.

---


**Namespace:** `Neo`
**Path:** `Scripts/PropertyAttribute/RequireInterface.cs`

## Description

The `[RequireInterface]` attribute is a tool for improving the reliability and architectural integrity of your code. It lets you specify that an inspector field, even if it has a generic type (for example, `GameObject` or `ScriptableObject`), can only accept objects that implement a specific interface.

This prevents erroneous inspector assignments that could lead to runtime errors, and helps you build a more flexible, loosely coupled architecture based on contracts (interfaces) rather than concrete classes.

## How to Use

1.  **Define an interface** that will serve as the contract.
2.  In your `MonoBehaviour`, create a public field (for example, of type `GameObject`).
3.  Apply the `[RequireInterface]` attribute to this field, passing your interface type to it.

Now, when you try to drag an object onto this field in the inspector, a special editor checks whether the object (or one of its components) implements the specified interface. If not, the assignment is rejected.

## Example

**1. Define the interface:**
```csharp
public interface IDamageable
{
    void TakeDamage(int amount);
}
```

**2. Create a component that implements it:**
```csharp
public class Player : MonoBehaviour, IDamageable
{
    public void TakeDamage(int amount)
    {
        Debug.Log($"Player takes {amount} damage!");
    }
}
```

**3. Use the attribute in another component:**
```csharp
public class Turret : MonoBehaviour
{
    [Tooltip("Only an object with a component implementing IDamageable can be dropped here")]
    [RequireInterface(typeof(IDamageable))]
    public GameObject target;

    private IDamageable _damageableTarget;

    void Start()
    {
        // We can be sure that target has the required component
        _damageableTarget = target.GetComponent<IDamageable>();
    }

    public void Shoot()
    {
        _damageableTarget?.TakeDamage(10);
    }
}
```

## Compatibility

The `[RequireInterface]` attribute can be used together with other attributes, such as `[FindInScene]`. In that case, the automatic search will look not just for a `GameObject`, but for a `GameObject` that also satisfies the interface requirement.
