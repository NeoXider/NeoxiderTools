# ObjectPool (Legacy Documentation)

**Purpose:** A generic `ObjectPool<T>` class existed in earlier versions of the project. Pools are now implemented via [NeoObjectPool](./NeoObjectPool.md) and [PoolManager](PoolManager.md). The `ObjectPool.cs` script no longer exists.

**How to use:** See [Spawner README](./README.md), [PoolManager](PoolManager.md), [Spawner](Spawner.md).

---

If you need an object pool in code, use the `Neo.Tools.NeoObjectPool` class (constructor accepts a prefab and a size) or the `Spawner`/`PoolManager` scene components.
