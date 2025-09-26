# Класс Object Pool<T>

## 1. Введение

`ObjectPool<T>` — это реализация паттерна "Пул объектов", который является одним из важнейших инструментов оптимизации в Unity. Вместо того чтобы постоянно создавать (`Instantiate`) и уничтожать (`Destroy`) игровые объекты (например, пули, частицы, врагов), пул объектов позволяет переиспользовать уже созданные экземпляры.

Это значительно снижает нагрузку на процессор и сборщик мусора (Garbage Collector), что особенно критично для мобильных игр и проектов с большим количеством динамически создаваемых объектов.

---

## 2. Описание класса

### ObjectPool<T>
- **Пространство имен**: `Neo`
- **Путь к файлу**: `Assets/Neoxider/Scripts/Tools/Spawner/ObjectPool.cs`
- **Тип**: Generic-класс (не `MonoBehaviour` или `ScriptableObject`)

**Описание**
Это обычный C#-класс, который управляет коллекцией объектов типа `T` (где `T` должен быть наследником `UnityEngine.Object`). Он хранит неактивные объекты в очереди и выдает их по запросу, а затем принимает обратно, деактивируя их.

**Ключевые поля**
- `_item` (`T`): Префаб или объект, который будет использоваться для создания экземпляров в пуле.
- `_initialPoolSize` (`int`): Количество объектов, которые будут созданы при инициализации пула.
- `_expandPool` (`bool`): Если `true`, пул будет автоматически создавать новые объекты, если все существующие заняты. Если `false`, `GetObject()` вернет `null`, когда пул пуст.

**Публичные методы (Public Methods)**
- `Init(T item)`: Инициализирует пул. **Этот метод должен быть вызван перед использованием пула.**
- `GetObject(Vector3 position = default, Quaternion rotation = default)`: Извлекает объект из пула. Если это `GameObject`, он активируется и устанавливается в заданную позицию/поворот.
- `ReturnObject(T obj)`: Возвращает объект обратно в пул. Если это `GameObject`, он деактивируется.
- `SetPrefab(T newPrefab)`: Изменяет префаб, используемый пулом. При этом текущий пул очищается и инициализируется заново с новым префабом.
- `ClearPool()`: Возвращает все активные объекты обратно в пул.
- `GetPoolSize()`: Возвращает количество неактивных объектов в пуле.

---

## 3. Как использовать

`ObjectPool<T>` — это не компонент, который вы добавляете на `GameObject`. Вместо этого вы объявляете его как поле в своем `MonoBehaviour` или другом классе.

```csharp
using Neo;
using Neo.Tools;
using UnityEngine;

public class BulletSpawner : MonoBehaviour
{
    [SerializeField] private GameObject _bulletPrefab; // Префаб пули
    private ObjectPool<GameObject> _bulletPool; // Объявляем пул для GameObject

    void Awake()
    {
        // Инициализируем пул с нашим префабом
        _bulletPool = new ObjectPool<GameObject>();
        _bulletPool.Init(_bulletPrefab);
    }

    public void SpawnBullet(Vector3 position, Quaternion rotation)
    {
        // Получаем пулю из пула
        GameObject bullet = _bulletPool.GetObject(position, rotation);

        if (bullet != null)
        {
            // Дополнительная логика для пули, например, запуск движения
            // bullet.GetComponent<BulletLogic>().Fire();
        }
    }

    public void ReturnBullet(GameObject bullet)
    {
        // Возвращаем пулю в пул, когда она больше не нужна
        _bulletPool.ReturnObject(bullet);
    }
}
```
