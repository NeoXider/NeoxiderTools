# Bootstrap

**Что это:** `Bootstrap` — синглтон инициализации для сервисов и менеджеров, которые реализуют `IInit`. Компонент собирает объекты из ручного списка и/или через поиск по сцене, затем вызывает `Init()` в порядке `InitPriority`. Файл: `Scripts/Tools/Managers/Bootstrap.cs`, пространство имён: `Neo.Tools`.

**Как использовать:**
1. Добавьте `Bootstrap` на сцену.
2. Для явного контроля добавьте нужные компоненты в `Manual Initializables`.
3. Для автоматического поиска включите `Auto Find Components`.
4. Реализуйте `IInit` на компонентах, которым нужна упорядоченная инициализация.
5. Для runtime-регистрации вызывайте `Register(IInit)` и `Unregister(IInit)`.

---

## Поля

- **Manual Initializables** — список компонентов для инициализации вручную.
- **Auto Find Components** — искать в сцене все IInit и вызывать Init() по приоритету.

## IInit

- **InitPriority** — чем больше, тем раньше вызов.
- **Init()** — вызывается один раз при старте.

## Runtime регистрация

- `Register(IInit)` добавляет новый сервис в bootstrap и после завершения первоначальной инициализации запускает его через общий priority-проход.
- `Unregister(IInit)` удаляет сервис из текущего набора bootstrap.
- Для необязательных менеджеров и интеграций безопаснее проверять наличие singleton через `HasInstance` или `TryGetInstance(out T)`, а не принудительно ходить в `I`.

## Порядок инициализации

1. `Bootstrap` собирает компоненты из `Manual Initializables`.
2. Если включён `Auto Find Components`, он ищет в сцене все `MonoBehaviour`, реализующие `IInit`.
3. Все найденные компоненты сортируются по `InitPriority` по убыванию.
4. Для каждого ещё не инициализированного объекта вызывается `Init()`.
5. После стартового прохода поздние регистрации через `Register()` тоже проходят через тот же механизм.

## Когда использовать

Используйте `Bootstrap`, если:
- у вас несколько менеджеров, которые должны запускаться в определённом порядке;
- `Awake()` и `Start()` уже недостаточно прозрачны;
- есть сервисы, которые регистрируются после старта сцены.

## Пример

```csharp
using Neo.Tools;
using UnityEngine;

public class SaveStartupService : MonoBehaviour, IInit
{
    public int InitPriority => 100;

    public void Init()
    {
        Debug.Log("Save system initialized before gameplay services.");
    }
}
```

## См. также

- [`Singleton`](./Singleton.md)
- [`GM`](./GM.md)
- [`EM`](./EM.md)
