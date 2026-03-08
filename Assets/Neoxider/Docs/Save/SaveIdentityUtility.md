# SaveIdentityUtility

**Что это:** `SaveIdentityUtility` — статический helper, который строит стабильные ключи для компонентов, участвующих в сохранении. Файл: `Scripts/Save/SaveIdentityUtility.cs`, пространство имён: `Neo.Save`.

**Как использовать:**
1. Обычно напрямую вызывать не нужно — его использует `SaveManager`.
2. Если вы пишете свой save-flow поверх `SaveManager`, используйте `GetComponentKey()` или `GetStableIdentity()`.
3. Если нужна собственная схема, реализуйте `ISaveIdentityProvider` — utility автоматически учтёт её.

---

## Основные методы

| Метод | Описание |
|-------|----------|
| `GetComponentKey(MonoBehaviour monoBehaviour)` | Возвращает полный ключ компонента: `FullName типа + стабильная identity`. |
| `GetStableIdentity(MonoBehaviour monoBehaviour)` | Возвращает только identity-часть без имени типа. |

## Как строится identity

Алгоритм такой:

1. Если компонент реализует [`ISaveIdentityProvider`](./ISaveIdentityProvider.md) и возвращает непустой `SaveIdentity`, используется он.
2. Иначе identity собирается из:
   - пути сцены;
   - пути объекта в иерархии;
   - sibling index каждого узла пути;
   - индекса компонента того же типа на объекте.

Такой подход делает ключ стабильнее обычного `GetInstanceID()` и позволяет надёжно загружать данные между перезапусками игры.

## Ограничения

- Если объект переносится в другое место иерархии, scene-based identity изменится.
- Если внутри одного объекта поменяется порядок однотипных компонентов, индекс изменится.
- Для динамически создаваемых объектов лучше использовать `ISaveIdentityProvider`.

## Пример полного ключа

```text
MyGame.PlayerStats:Assets/Scenes/Main.unity:Root#0/Player#1:0
```

Здесь:
- `MyGame.PlayerStats` — тип компонента;
- `Assets/Scenes/Main.unity:Root#0/Player#1` — путь сцены и трансформа;
- `0` — индекс компонента данного типа на объекте.

## Когда вызывать вручную

Ручной вызов полезен, если:
- вы пишете отладку для save-системы;
- строите инструменты проверки конфликтов identity;
- хотите показать текущий save key в editor tooling.

## См. также

- [`SaveManager`](./SaveManager.md)
- [`ISaveIdentityProvider`](./ISaveIdentityProvider.md)
- [`SaveableBehaviour`](./SaveableBehaviour.md)
