# PooledObjectInfo

Компонент, автоматически вешаемый на объекты, созданные пулом. Хранит ссылку на пул и даёт метод **Return()** для возврата в пул.

Не добавляется вручную — пул ставит его при первом создании экземпляра.

## API

- **OwnerPool** — пул, которому принадлежит объект.
- **Return()** — вернуть объект в пул (то же, что `PoolManager.Release(gameObject)`).

## Пример

```csharp
// Возврат по таймеру или при коллизии
if (gameObject.TryGetComponent(out PooledObjectInfo info))
    info.Return();
// или
gameObject.ReturnToPool(); // Neo.Tools.PoolExtensions
```

## См. также

- [PoolManager](./PoolManager.md)
- [IPoolable](./IPoolable.md) / [PoolableBehaviour](./PoolableBehaviour.md)
