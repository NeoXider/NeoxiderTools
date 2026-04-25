# PoolManager

**Назначение:** Центральный менеджер пулов объектов. Автоматически создает пулы при первом обращении (или загружает преднастроенные на старте), предотвращая лишние вызовы `Instantiate` и `Destroy`.

## Поля (Inspector)

| Поле | Описание |
|------|----------|
| **Default Initial Size** | Базовый стартовый размер пула, если он создается "на лету" (без предварительной настройки). |
| **Default Expand Pool** | Разрешено ли пулу автоматически увеличиваться, если все объекты заняты. |
| **Preconfigured Pools** | Список префабов (с их лимитами), которые нужно загрузить в пул сразу при старте сцены. |

## API

| Метод / Свойство | Описание |
|------------------|----------|
| `static GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)` | Запрашивает объект из пула. Если пула для этого префаба нет — он создается автоматически. |
| `static void Release(GameObject instance)` | Возвращает объект обратно в пул. Если объект не из пула — уничтожает его (`Destroy`). |

## Примеры

### Пример No-Code (в Inspector)
Добавьте компонент `PoolManager` на пустой объект `Managers`. Раскройте `Preconfigured Pools` и добавьте туда префаб пули с `Initial Size = 50`. При старте уровня игра сразу создаст 50 пуль, и стрельба не вызовет лагов.

### Пример (Код)
```csharp
[SerializeField] private GameObject _bulletPrefab;
[SerializeField] private Transform _firePoint;

public void Shoot()
{
    // Берем пулю из пула (создаст пул автоматически, если его еще нет)
    GameObject bullet = PoolManager.Get(_bulletPrefab, _firePoint.position, _firePoint.rotation);
}
```

## См. также
- [Spawner](Spawner.md)
- [PooledObjectInfo](PooledObjectInfo.md)
- [Despawner](Despawner.md)
- ← [Tools/Spawner](../README.md)
