# Save

Модуль `Save` объединяет три уровня сохранения данных: provider API, компонентные сохранения на сцене и глобальное хранилище.

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

## Документация

- [`ISaveableComponent.md`](./ISaveableComponent.md)
- [`SaveableBehaviour.md`](./SaveableBehaviour.md)
- [`SaveField.md`](./SaveField.md)
- [`SaveManager.md`](./SaveManager.md)
- [`GlobalData.md`](./GlobalData.md)
- [`GlobalSave.md`](./GlobalSave.md)
