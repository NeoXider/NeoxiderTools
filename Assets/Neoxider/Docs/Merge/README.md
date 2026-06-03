# Merge

`Neo.Merge` - универсальное pure C# ядро для merge-механик на связанных группах. Модуль не зависит от Unity-сцен, GridSystem, инвентаря или конкретной игры.

Используйте его, когда нужно найти связанные одинаковые элементы, выбрать элемент-результат, вычислить новое значение, очистить остальные элементы и при необходимости продолжить cascade от результата.

## Runtime API

- `MergeRequest<TItem, TValue>` задает items, seeds, чтение/запись value, соседей, правила совпадения, выбор result item, новое значение, cascade mode и dry-run/apply режим.
- `MergeResolver.Resolve(request)` возвращает `MergeResult<TItem, TValue>`.
- `MergeResult` содержит группы и измененные элементы.

## Примеры

- Grid games: используйте `Neo.GridSystem.Merge.GridMergeResolver` для `FieldGenerator`.
- Dice games: используйте `Neo.GridSystem.Dice.DiceBoardService` для размещения кубиков и dice merge.
- Custom systems: передайте любые graph/list/inventory nodes как `TItem` и задайте neighbor callback.

## См. также

- [GridSystem](../GridSystem/README.md)
- [Dice](../GridSystem/Dice/README.md)
