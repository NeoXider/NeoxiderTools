# PoolableBehaviour

**Purpose:** A base class for scripts intended to work with pools. Implements the `IPoolable` interface. If your script inherits from this (instead of `MonoBehaviour`), it automatically receives callbacks when taken from or returned to a pool.

## API

| Method / Property | Description |
|-------------------|-------------|
| `virtual void OnPoolCreate()` | Called once when the object is instantiated by the pool for the first time. |
| `virtual void OnPoolGet()` | Called every time the object is retrieved from the pool. Use this to reset state (e.g., enemy health, timers). |
| `virtual void OnPoolRelease()` | Called every time the object is returned to the pool. |

## Examples

### Code Example
```csharp
public class Enemy : PoolableBehaviour
{
    private int _health;

    // Called when the enemy is taken from the pool
    public override void OnPoolGet()
    {
        base.OnPoolGet();
        _health = 100; // Reset health to maximum
    }
}
```

## See Also
- [PoolManager](PoolManager.md)
- ← [Tools/Spawner](../README.md)
