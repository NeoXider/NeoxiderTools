# SaveManager

**Что это:** `SaveManager` — синглтон модуля `Save`, который регистрирует компоненты с `ISaveableComponent`, находит поля с атрибутом `SaveField`, сериализует их в общий JSON-контейнер и восстанавливает значения при загрузке. Файл: `Scripts/Save/SaveManager.cs`, пространство имён: `Neo.Save`.

**Как использовать:**
1. Добавьте `SaveManager` на сцену один раз.
2. Наследуйте сохраняемые компоненты от [`SaveableBehaviour`](./SaveableBehaviour.md) или реализуйте `ISaveableComponent` вручную.
3. Пометьте нужные поля атрибутом [`SaveField`](./SaveField.md).
4. Если объекту нужен собственный стабильный ключ, реализуйте [`ISaveIdentityProvider`](./ISaveIdentityProvider.md).
5. При необходимости вызывайте `SaveManager.Save()` и `SaveManager.Load()` вручную.

---

## Что делает менеджер

- При инициализации находит все активные и неактивные `MonoBehaviour`, реализующие `ISaveableComponent`.
- Кэширует только те поля, которые помечены `[SaveField]`.
- Сохраняет значения в единый JSON под ключом `SaveData_All` через [`SaveProvider`](./SaveProvider.md).
- После загрузки вызывает `OnDataLoaded()` на каждом успешно обработанном компоненте.
- После загрузки новой сцены регистрирует только новые объекты и очищает разрушенные регистрации.

## Идентификация компонентов

Раньше для ключа компонента использовался `GetInstanceID()`, но такой идентификатор не подходит для межсессионного сохранения.

Текущая схема:
- сначала используется пользовательский `SaveIdentity`, если компонент реализует [`ISaveIdentityProvider`](./ISaveIdentityProvider.md);
- иначе ключ строится через [`SaveIdentityUtility`](./SaveIdentityUtility.md) из пути сцены, пути объекта в иерархии и индекса компонента одного типа;
- итоговый `ComponentKey` включает `FullName` типа и стабильную identity-часть.

Это делает загрузку устойчивой между перезапусками игры, если объект остаётся в той же сцене и не меняет своё место в иерархии.

## Жизненный цикл

### Автоматический путь

1. `SaveableBehaviour.OnEnable()` вызывает `SaveManager.Register(this)`.
2. `SaveManager` собирает список полей с `[SaveField]`.
3. В `Init()` менеджер вызывает `Load()` для всех зарегистрированных компонентов.
4. На `OnApplicationQuit()` вызывается `Save()`.
5. На `sceneLoaded` менеджер повторно сканирует сцену и подгружает только новые компоненты.

### Ручной путь

- `SaveManager.Save(monoObj)` сохраняет один компонент.
- `SaveManager.Load(monoObj)` загружает один компонент.
- `Register()` и `Unregister()` можно вызывать вручную, если компонент не наследуется от `SaveableBehaviour`.

## Публичный API

| API | Описание |
|-----|----------|
| `bool IsLoad` | Показывает, завершил ли менеджер начальную загрузку. |
| `Register(MonoBehaviour monoObj)` | Регистрирует компонент и кэширует его сохраняемые поля. |
| `Unregister(MonoBehaviour monoObj)` | Удаляет компонент из текущего реестра. |
| `Save()` | Сохраняет все зарегистрированные компоненты. |
| `Load(List<MonoBehaviour> componentsToLoad = null)` | Загружает переданный список компонентов или все зарегистрированные компоненты. |
| `Save(MonoBehaviour monoObj, bool isSave = false)` | Сохраняет только один компонент в общий контейнер. |
| `Load(MonoBehaviour monoObj)` | Загружает только один компонент. |

## Практические замечания

- Перезагрузка домена / Enter Play Mode: статические кэши регистраций сбрасываются через `SaveManagerSubsystemRegistration` и `SaveManager.ClearSubsystemCaches()`. Атрибут `[RuntimeInitializeOnLoadMethod]` намеренно **не** висит на самом `SaveManager`, так как Unity запрещает такие хуки на типах, унаследованных от generic-баз вроде `Singleton<T>`.
- Не полагайтесь на auto-save как на единственный сценарий. Для важных пользовательских действий полезно вызывать `SaveManager.Save()` явно.
- Если объект создаётся динамически и должен иметь предсказуемый ключ между сессиями, дайте ему собственный `SaveIdentity`.
- Если вы меняете структуру сцены, проверьте, не сломает ли это scene-based identity для уже выпущенных сохранений.
- Для глобальных данных, не привязанных к конкретному компоненту сцены, используйте `GlobalSave`, а не `SaveManager`.

## Пример

```csharp
using Neo.Save;
using UnityEngine;

public class PlayerStats : SaveableBehaviour, ISaveIdentityProvider
{
    [SaveField("health")] [SerializeField] private int _health = 100;
    [SaveField("coins")] [SerializeField] private int _coins;

    public string SaveIdentity => "player-main";

    public override void OnDataLoaded()
    {
        Debug.Log($"Loaded stats: health={_health}, coins={_coins}");
    }
}
```

## См. также

- [`SaveableBehaviour`](./SaveableBehaviour.md)
- [`SaveField`](./SaveField.md)
- [`SaveProvider`](./SaveProvider.md)
- [`ISaveIdentityProvider`](./ISaveIdentityProvider.md)
- [`SaveIdentityUtility`](./SaveIdentityUtility.md)
