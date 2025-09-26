
# [RequireInterface]

**Пространство имен:** `Neo`
**Путь:** `Scripts/PropertyAttribute/RequireInterface.cs`

## Описание

Атрибут `[RequireInterface]` — это инструмент для повышения надежности и архитектурной целостности вашего кода. Он позволяет указать, что поле в инспекторе, даже если оно имеет общий тип (например, `GameObject` или `ScriptableObject`), может принимать только те объекты, которые реализуют определенный интерфейс.

Это предотвращает ошибочные назначения в инспекторе, которые могли бы привести к ошибкам во время выполнения, и помогает строить более гибкую и слабосвязанную архитектуру, основанную на контрактах (интерфейсах), а не на конкретных классах.

## Как использовать

1.  **Определите интерфейс**, который будет служить контрактом.
2.  В вашем `MonoBehaviour` создайте публичное поле (например, типа `GameObject`).
3.  Примените к этому полю атрибут `[RequireInterface]`, передав в него тип вашего интерфейса.

Теперь, при попытке перетащить объект на это поле в инспекторе, специальный редактор проверит, реализует ли этот объект (или один из его компонентов) указанный интерфейс. Если нет, назначение будет невозможным.

## Пример

**1. Определяем интерфейс:**
```csharp
public interface IDamageable
{
    void TakeDamage(int amount);
}
```

**2. Создаем компонент, который его реализует:**
```csharp
public class Player : MonoBehaviour, IDamageable
{
    public void TakeDamage(int amount)
    {
        Debug.Log($"Player takes {amount} damage!");
    }
}
```

**3. Используем атрибут в другом компоненте:**
```csharp
public class Turret : MonoBehaviour
{
    [Tooltip("Сюда можно перетащить только объект с компонентом, реализующим IDamageable")]
    [RequireInterface(typeof(IDamageable))]
    public GameObject target;

    private IDamageable _damageableTarget;

    void Start()
    {
        // Мы можем быть уверены, что у target есть нужный компонент
        _damageableTarget = target.GetComponent<IDamageable>();
    }

    public void Shoot()
    {
        _damageableTarget?.TakeDamage(10);
    }
}
```

## Совместимость

Атрибут `[RequireInterface]` можно использовать совместно с другими атрибутами, например, с `[FindInScene]`. В этом случае автоматический поиск будет искать не просто `GameObject`, а `GameObject`, который также удовлетворяет требованию интерфейса.
