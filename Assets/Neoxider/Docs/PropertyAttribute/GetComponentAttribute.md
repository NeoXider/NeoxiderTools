
# [GetComponent]

**Пространство имен:** `(global)`
**Путь:** `Scripts/PropertyAttribute/InjectAttribute/GetComponentAttribute.cs`

## Описание

Атрибут `[GetComponent]` — это удобная замена стандартному вызову `GetComponent<T>()` в методах `Awake` или `Start`. Он автоматически находит и назначает в поле ссылку на другой компонент, который находится на этом же `GameObject` или в его дочерних объектах.

Это помогает уменьшить количество однотипного кода в `Awake`/`Start` и сделать зависимости между компонентами более наглядными прямо в объявлении полей.

## Как использовать

Поместите атрибут над полем, тип которого является компонентом, который вы хотите найти.

**Поиск на том же объекте:**

```csharp
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [GetComponent]
    private Rigidbody rb;

    void FixedUpdate()
    {
        rb.AddForce(Vector3.forward);
    }
}
```

**Поиск в дочерних объектах:**

Чтобы найти компонент на этом `GameObject` или на любом из его дочерних объектов, передайте `true` в конструктор атрибута.

```csharp
using UnityEngine;

public class Player : MonoBehaviour
{
    // Найдет компонент Animator на этом объекте или на его детях
    [GetComponent(true)]
    private Animator animator;
}
```

## Параметры конструктора

- `searchInChildren` (`bool`): Необязательный параметр.
  - `false` (по умолчанию): Поиск выполняется только на том же `GameObject` (эквивалентно `GetComponent<T>()`).
  - `true`: Поиск выполняется на этом `GameObject` и на всех его дочерних объектах рекурсивно (эквивалентно `GetComponentInChildren<T>()`).

## Важные замечания

- **Перезапись**: Поиск и автозаполнение происходят только в том случае, если поле равно `null`. Это сделано для того, чтобы случайно не перезаписать ссылку, которая была назначена вручную.
- **Первый найденный**: Атрибут находит только **первый** попавшийся компонент, что особенно важно помнить при поиске в дочерних объектах (`searchInChildren = true`).
