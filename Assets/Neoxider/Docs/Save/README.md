# Save

**Что это:** модуль `Save` объединяет provider-based key/value API, сохранение состояния scene-компонентов и отдельное глобальное хранилище. Скрипты лежат в `Scripts/Save/`.

**Оглавление:**
- [`SaveProvider`](./SaveProvider.md)
- [`SaveManager`](./SaveManager.md)
- [`SaveableBehaviour`](./SaveableBehaviour.md)
- [`ISaveIdentityProvider`](./ISaveIdentityProvider.md)
- [`SaveIdentityUtility`](./SaveIdentityUtility.md)
- [`GlobalSave`](./GlobalSave.md)
- остальные ссылки — в разделе ниже

---

## Когда использовать

- Нужен API в стиле `PlayerPrefs`, но со сменяемым backend - используйте `SaveProvider`.
- Нужно сохранять состояние компонентов сцены - используйте `SaveableBehaviour`, `SaveField`, `SaveManager`.
- Нужны общие данные проекта вне сцены - используйте `GlobalSave`.

## Быстрый старт

### Provider API

```csharp
SaveProvider.SetInt("score", 100);
SaveProvider.Save();
int score = SaveProvider.GetInt("score", 0);
```

### Сохранение компонента

1. Наследуйте компонент от `SaveableBehaviour`.
2. Пометьте нужные поля атрибутом `[SaveField]`.
3. Убедитесь, что в сцене есть `SaveManager`.
4. Для нестандартных объектов при необходимости реализуйте `ISaveIdentityProvider`, чтобы задать собственный стабильный ключ сохранения.

## Примечания по идентификации

- `SaveManager` больше не использует `GetInstanceID()` как persistent key для компонентов.
- По умолчанию ключ строится из сцены, пути объекта в иерархии и индекса компонента того же типа.
- Если нужен полностью контролируемый идентификатор, реализуйте `ISaveIdentityProvider`.

## Документация

- [`ISaveableComponent.md`](./ISaveableComponent.md)
- [`ISaveIdentityProvider.md`](./ISaveIdentityProvider.md)
- [`SaveableBehaviour.md`](./SaveableBehaviour.md)
- [`SaveField.md`](./SaveField.md)
- [`SaveIdentityUtility.md`](./SaveIdentityUtility.md)
- [`SaveManager.md`](./SaveManager.md)
- [`SaveProvider.md`](./SaveProvider.md)
- [`SaveProviderSettingsComponent.md`](./SaveProviderSettingsComponent.md)
- [`GlobalData.md`](./GlobalData.md)
- [`GlobalSave.md`](./GlobalSave.md)


## Дополнительные поля

| Поле | Описание |
|------|----------|
| `AllSavedComponents` | All Saved Components. |
| `ComponentKey` | Component Key. |
| `Fields` | Fields. |
| `Items` | Items. |
| `Key` | Key. |
| `TypeName` | Type Name. |
| `Value` | Value. |