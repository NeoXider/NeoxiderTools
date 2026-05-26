# SelectorModel

`SelectorModel` - обычный C# класс с правилами выбора, которые использует `Selector`.

## Когда использовать

- Нужен выбор индекса без `MonoBehaviour`.
- Логика живет в сервисе, view-model, тесте, UI presenter или runtime-системе без прямых ссылок на сцену.
- Нужны random, unique, excluded indices, fill mode, loop, offset и empty effective index без управления `GameObject`.

## Что осталось в Selector

`Selector` остается совместимой MonoBehaviour-оберткой. Старые сериализованные поля, public API, UnityEvents, `GameObject`/`SelectorItem` применение, SaveProvider и Mirror-синхронизация остаются на компоненте.

## Минимальный пример

```csharp
var model = new SelectorModel();
model.Configure(
    count: 5,
    currentIndex: 0,
    indexOffset: 0,
    loop: true,
    fillMode: false,
    allowEmptyEffectiveIndex: false,
    uniqueSelectionMode: true,
    resetUniqueWhenCycleComplete: true,
    excludedIndices: null,
    usedIndicesForUnique: null);

model.Set(2);
model.ExcludeIndex(4);
model.SetRandom(deactivateOthers: true, randomRange: null);

int index = model.CurrentIndex;
int activeCount = model.GetLogicalActiveCount();
```

## Интеграция с компонентом

`Selector.CreateModelSnapshot()` возвращает чистый снимок текущего состояния компонента. Изменение снимка не меняет сценовый компонент; для сцены используйте прежние методы `Set`, `Next`, `SetRandom`, `ExcludeIndex` и т.д.
