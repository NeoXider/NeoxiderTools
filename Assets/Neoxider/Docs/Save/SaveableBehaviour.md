# SaveableBehaviour

**Что это:** `SaveableBehaviour` — базовый `MonoBehaviour` для компонентов, которые должны участвовать в системе сохранений. Файл: `Scripts/Save/SaveableBehaviour.cs`, пространство имён: `Neo.Save`.

**Как использовать:**
1. Наследуйте свой компонент от `SaveableBehaviour`.
2. Пометьте сохраняемые поля атрибутом `[SaveField("key")]`.
3. При необходимости переопределите `OnDataLoaded()`.
4. Если нужен собственный стабильный ключ, дополнительно реализуйте `ISaveIdentityProvider`.

---

## Что делает базовый класс

- Автоматически вызывает `SaveManager.Register(this)` в `OnEnable()`.
- Автоматически вызывает `SaveManager.Unregister(this)` в `OnDisable()`.
- Уже реализует `ISaveableComponent`, поэтому вам остаётся только переопределить `OnDataLoaded()` при необходимости.

Это самый простой способ подключить компонент к `SaveManager` без ручной регистрации.

## Когда использовать

Используйте `SaveableBehaviour`, если:
- компонент живёт на объекте сцены;
- у компонента есть сериализуемые поля, которые должны сохраняться;
- вам нужен минимальный boilerplate.

Если регистрация должна управляться вручную или компонент не должен зависеть от базового класса, можно реализовать `ISaveableComponent` самостоятельно.

## Поведение при disable/destroy

Текущая версия класса не только регистрирует компонент при включении, но и корректно удаляет его из save-реестра при выключении. Это важно для:
- временно отключаемых объектов;
- объектов, которые уничтожаются в runtime;
- избежания устаревших ссылок в статическом реестре `SaveManager`.

## Типичный сценарий

```csharp
using Neo.Save;
using UnityEngine;

public class PlayerScore : SaveableBehaviour
{
    [SaveField("score")] [SerializeField] private int _score;
    [SaveField("best-score")] [SerializeField] private int _bestScore;

    public override void OnDataLoaded()
    {
        Debug.Log($"Loaded score: {_score}, best: {_bestScore}");
    }
}
```

## Что не делает класс

- Не сохраняет поля автоматически без `[SaveField]`.
- Не создаёт `SaveManager` сам по себе.
- Не даёт custom identity автоматически. Для этого нужен `ISaveIdentityProvider`.

## См. также

- [`SaveManager`](./SaveManager.md)
- [`SaveField`](./SaveField.md)
- [`ISaveableComponent`](./ISaveableComponent.md)
- [`ISaveIdentityProvider`](./ISaveIdentityProvider.md)
