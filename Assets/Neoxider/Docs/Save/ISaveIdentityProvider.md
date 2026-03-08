# ISaveIdentityProvider

**Что это:** `ISaveIdentityProvider` — интерфейс для компонентов, которым нужен собственный стабильный идентификатор в системе сохранений. Файл: `Scripts/Save/ISaveIdentityProvider.cs`, пространство имён: `Neo.Save`.

**Как использовать:**
1. Реализуйте интерфейс на компоненте, который уже участвует в `SaveManager`.
2. Верните из `SaveIdentity` строку, которая остаётся стабильной между сессиями.
3. Убедитесь, что значение уникально в рамках набора объектов, которые могут быть загружены одновременно.

---

## Зачем он нужен

По умолчанию `SaveManager` использует scene-based identity через [`SaveIdentityUtility`](./SaveIdentityUtility.md). Этого достаточно для большинства статичных объектов сцены, но иногда нужен полностью контролируемый ключ:

- у главного игрока;
- у runtime-объектов, которые пересоздаются, но должны читать один и тот же save;
- у компонентов, которые могут менять место в иерархии;
- при миграции старых сохранений на новую структуру сцены.

В таких случаях `ISaveIdentityProvider` позволяет явно задать identity без привязки к иерархии.

## Контракт

| Член | Описание |
|------|----------|
| `string SaveIdentity { get; }` | Стабильная identity-часть ключа, которую использует `SaveManager`. |

`SaveManager` проверяет этот интерфейс первым. Если `SaveIdentity` пустой или состоит только из пробелов, менеджер вернётся к стандартной scene-based схеме.

## Рекомендации к значению

- Используйте детерминированные строки, а не случайные значения на каждом запуске.
- Не используйте `GetInstanceID()`, `Guid.NewGuid()` в runtime или другие нестабильные источники.
- Делайте строку понятной: `player-main`, `ui-settings`, `quest-log`.
- Если компонент может существовать в нескольких экземплярах, включайте в ключ доменный идентификатор.

## Пример

```csharp
using Neo.Save;
using UnityEngine;

public class PlayerSaveAnchor : SaveableBehaviour, ISaveIdentityProvider
{
    [SaveField("xp")] [SerializeField] private int _xp;

    public string SaveIdentity => "player-main";
}
```

## Когда не нужен

Не реализуйте `ISaveIdentityProvider`, если:
- объект статичен и стабильно живёт в сцене;
- вам достаточно identity по сцене и иерархии;
- вы не управляете совместимостью старых сохранений вручную.

## См. также

- [`SaveManager`](./SaveManager.md)
- [`SaveIdentityUtility`](./SaveIdentityUtility.md)
- [`SaveableBehaviour`](./SaveableBehaviour.md)
