# PoolableBehaviour

Базовый класс для объектов из пула. Реализует **IPoolable** с пустыми виртуальными методами — переопределяйте только нужные.

```csharp
public class Bullet : PoolableBehaviour
{
    public override void OnPoolGet()
    {
        // сброс скорости, включение коллайдера и т.д.
    }

    public override void OnPoolRelease()
    {
        // остановить trail, отписаться от событий
    }
}
```

**OnPoolCreate()** вызывается один раз при создании экземпляра пулом — удобно для кэширования GetComponent.

## См. также

- [PoolManager](./PoolManager.md)
- [PooledObjectInfo](./PooledObjectInfo.md)
