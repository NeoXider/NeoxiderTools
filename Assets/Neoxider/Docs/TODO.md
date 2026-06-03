# TODO

Актуальные технические задачи, которые стоит держать отдельно от changelog. Этот список не заменяет планы релиза, а фиксирует ближайшие улучшения публичного API.

## GridSystem

- Сделать generic `GridPlacementService` / rule config поверх текущего `FieldGenerator` placement API. Хорошая форма следующего шага: `GridPlacementRequest` с `RequireEnabled`, `RequireWalkable`, `RequireUnoccupied`, custom predicate и overwrite policy, чтобы gameplay-сервисы могли переиспользовать одинаковые правила placement без разрастания overloads в `FieldGenerator`.
- Рассмотреть non-Mono `DiceBoard` plain C# service над `IGridPlacementBoard` или adapter для `FieldGenerator`, оставив текущий `DiceBoardService` как MonoBehaviour wrapper. Это улучшит тестируемость и позволит использовать Dice-механику вне сцены, но требует аккуратно сохранить текущий сценовый API.

## См. также

- [Ideas](IDEAS.md)
- [GridSystem](GridSystem/README.md)
