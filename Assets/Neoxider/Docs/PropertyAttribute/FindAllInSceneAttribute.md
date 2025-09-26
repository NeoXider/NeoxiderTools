
# [FindAllInScene]

**Пространство имен:** `(global)`
**Путь:** `Scripts/PropertyAttribute/InjectAttribute/FindAllInSceneAttribute.cs`

## Описание

Атрибут `[FindAllInScene]` автоматизирует поиск **всех** компонентов указанного типа на текущей активной сцене и автоматически назначает их в поле типа массив или `List`.

Это чрезвычайно полезно, когда вам нужно собрать коллекцию однотипных объектов, например, всех врагов на уровне, все точки маршрута, все интерактивные предметы и т.д., не назначая каждый из них вручную.

## Как использовать

Поместите атрибут над полем, которое является массивом или списком (`List<T>`) компонентов.

```csharp
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    [FindAllInScene]
    public List<EnemyController> allEnemies;

    [FindAllInScene]
    public Transform[] allWaypoints;
}
```

Когда вы выберете `GameObject` с этим компонентом в инспекторе, если поле `allEnemies` или `allWaypoints` будет пустым, редактор автоматически найдет все активные `EnemyController` и все `Transform` с именем "Waypoint" (предположим) на сцене и заполнит ими соответствующий список/массив.

## Параметры конструктора

- `sortMode` (`FindObjectsSortMode`): Необязательный параметр, который позволяет указать, как сортировать найденные объекты. 
  - `FindObjectsSortMode.InstanceID` (по умолчанию): Сортировка по ID экземпляра.
  - `FindObjectsSortMode.None`: Без сортировки.
  - `FindObjectsSortMode.Name`: Сортировка по имени `GameObject`.

**Пример:**

```csharp
// Найти все точки и отсортировать их по имени
[FindAllInScene(FindObjectsSortMode.Name)]
public Transform[] sortedWaypoints;
```

## Важные замечания

- Атрибут работает только с полями, которые являются массивами (`T[]`) или списками (`List<T>`).
- Поиск и автозаполнение происходят только в том случае, если поле пустое (не содержит элементов). Это сделано для того, чтобы случайно не перезаписать ссылки, которые были назначены вручную.
