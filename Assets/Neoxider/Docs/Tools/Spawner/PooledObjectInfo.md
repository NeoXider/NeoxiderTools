# PooledObjectInfo

**Назначение:** Служебный скрипт. Вешается автоматически пулом `NeoObjectPool` на каждый заспавненный объект, чтобы тот "помнил", какому пулу он принадлежит. Содержит удобную кнопку `Return to pool`.

## API

| Метод / Свойство | Описание |
|------------------|----------|
| `void Return()` | Возвращает текущий `GameObject` обратно в пул (через `PoolManager.Release()`). Можно вызывать из UnityEvent (например, по окончанию анимации). |

## Примеры

### Пример No-Code (в Inspector)
Вы можете повесить `TimerObject` на префаб, который берется из пула, и в его UnityEvent `OnTimerEnd` перетащить самого себя (свой компонент `PooledObjectInfo`) и выбрать метод `Return`. Объект будет автоматически возвращаться в пул через X секунд.

## См. также
- [PoolManager](PoolManager.md)
- [Despawner](Despawner.md)
- ← [Tools/Spawner](../README.md)
