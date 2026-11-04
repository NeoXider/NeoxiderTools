# PoolableBehaviour

**What it is:** Abstract `MonoBehaviour` implementing [IPoolable](IPoolable.md) with empty virtual callbacks. `Scripts/Tools/Spawner/PoolableBehaviour.cs`, namespace `Neo.Tools`.

**How to use:**
1. Inherit your pooled script from `PoolableBehaviour` instead of `MonoBehaviour`.
2. Override only the callbacks you need (`OnPoolGet` to reset health/timers, `OnPoolRelease` to stop effects).
3. Spawn the prefab through a pool; `Instantiate` does not invoke these callbacks.

---

## API

| Method / Property | Description |
|-------------------|-------------|
| `virtual void OnPoolCreate()` | Called once when the object is instantiated by the pool for the first time. |
| `virtual void OnPoolGet()` | Called every time the object is retrieved from the pool. Use this to reset state (e.g., enemy health, timers). |
| `virtual void OnPoolRelease()` | Called every time the object is returned to the pool. |

The same callbacks run for manager pools and automatic fallback pools, including prewarming. `OnPoolGet()` precedes activation and final pose assignment; `OnPoolRelease()` sees `IsInPool = true` before automatic Mirror server unspawn and pool deactivation. Components are cached on creation; configure the prefab's component set before pooling it. Inspector spawners invoke these callbacks when **Use Object Pool** is enabled.

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
- [Tools/Spawner](README.md)
