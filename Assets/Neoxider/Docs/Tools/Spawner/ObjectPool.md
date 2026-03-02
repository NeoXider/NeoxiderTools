# ObjectPool (устаревшая документация)

**Что это:** ранее в проекте был generic-класс `ObjectPool<T>`. Сейчас пулы реализованы через [NeoObjectPool](NeoObjectPool.md) и [PoolManager](PoolManager.md). Скрипта `ObjectPool.cs` больше нет.

**Как использовать:** см. [README спавнеров](./README.md), [PoolManager](PoolManager.md), [Spawner](Spawner.md).

---

Если нужен пул объектов в коде — используйте класс `Neo.Tools.NeoObjectPool` (конструктор с префабом и размером) или компоненты Spawner/PoolManager на сцене.
