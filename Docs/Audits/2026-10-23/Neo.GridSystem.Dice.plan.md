# Neo.GridSystem.Dice — индивидуальный план реализации

Фаза 2, 2026-10-23; тот же аудитор, `gpt-6.1-sol max`. Прочитаны [аудит](./Neo.GridSystem.Dice.md), manifest и актуальные source/docs/tests. Изменён только этот план будущей работы.

Для точного указания файлов: `D` = `Assets/Neoxider/Scripts/GridSystem/Dice`, `G` = `Assets/Neoxider/Scripts/GridSystem`, `T` = `Assets/Neoxider/Tests/Edit/GridSystem/Dice`, `U` = `Assets/Neoxider/Docs/GridSystem/Dice`. Все пути ниже раскрываются относительно этих каталогов.

## 1. Что уже сделано

Проверены все 7 C# файлов manifest. DICE-01–05 **исправлены в исходниках**, повторно реализовывать их не нужно:

- `D/DicePieceGenerator.cs`: конечный обход inclusive-диапазонов с `int.MaxValue`, проверка размерности через `long`, разные значения пары при повторяющихся элементах, проверка суммы положительных весов до RNG.
- `D/DicePiece.cs`: одиночная клетка со смещением вращается относительно anchor.
- `D/DiceBoard.cs`: сложение merge в `long`, насыщение до положительного cap либо `int.MaxValue`.
- `D/DiceBoardService.cs`: уточнена XML-документация; поведение wrapper не менялось. Обновлены `U/README.md` и `U/DiceBoardService.md` для C#, Inspector/UnityEvents и sample layouts.

Добавлен `T/Audit20261023NeoGridSystemDiceRegressionTests.cs` с `.meta`: 9 методов, 17 cases. Статически проверены diff/UTF-8/inventory/API/serialization/GUID/ссылки/sample paths. **Компиляция, NUnit, Play Mode, консоль Unity не проверены.** Подтверждённых неисправленных локальных Dice-дефектов нет.

## 2. Приоритеты, оставшиеся дефекты и долг проверки

P1 — до релизной приёмки; P2 — отдельная следующая фича; P3 — исследование при реальном сценарии.

| Приоритет / владелец | Доказательство и минимальное действие | Наблюдаемая приёмка |
|---|---|---|
| P1, `Neo.GridSystem`, worker `neo-audit-1023-b03-05` | В актуальных `G/FieldGenerator.cs:ValidateContentFootprint` и `G/GridPlacementService.cs:Validate` складываются два `Vector3Int` без проверки. При `anchor.x = offset.x = int.MinValue` получается X=0 вместо отклонения. Проверить каждую координату в `long` до преобразования; использовать один общий путь проверки в этой сборке. | Оба placement API и `DiceBoard` отклоняют такой footprint; контент/occupancy неизменны, событий записи и успешного размещения нет. Обычные отрицательные offsets остаются допустимыми, если итоговая координата допустима. |
| P1, тот же владелец | В `G/Merge/GridMergeRequest.cs:Increment` остаётся `value + step`, в `G/Merge/GridMergeResolver.cs` — fallback `value + 1`. `int.MaxValue + 1` переполняется. Владелец задаёт общий saturating-контракт через `long`, включая нижнюю границу для отрицательного generic step. Dice уже защищён собственным override. | Прямые generic вызовы возвращают ограниченный representable результат без wrap; обычные значения и каскады прежние. Проверить обе границы и оба entry point. |
| P1, координатор / Unity QA | 17 новых cases пока не запускались. После стабилизации общих файлов запустить их, `DiceBoardCoreTests` и Dice cases из `GridMergeAndDiceTests`. Ошибки исправлять в соответствующей owned сборке. | Все ожидаемые результаты ниже подтверждены исполняемыми тестами; есть import/compile и console evidence с ревизией и версией Unity. |

Доказательства — текущий исходник/арифметика, не Unity run. Координатор сверяет итоговые параллельные правки владельца. Dice-интеграция: `T/Audit20261023NeoGridSystemDiceGridIntegrationTests.cs`; прямые generic tests пишет `Neo.GridSystem` со своим testPrefix.

P3, **не подтверждённый дефект**: внешний `service.Board` удерживает closures/поле; это ещё не утечка. QA проверяет Board → уничтожение wrapper → вызов core до изменения lifetime-контракта. Memory limits, корректный RNG и резервирование `EmptyContentId` документированы. Аллокацию кандидатов оптимизировать только после профиля.

## 3. P2: опциональное состояние доски без scene objects

**Проблема.** `DiceBoard` требует `FieldGenerator`/GameObject, усложняя симуляции/replay. Предложение не реализовано и не блокирует приёмку пяти исправлений.

**Повторное использование.** `G/FieldCell.cs` — обычный C# объект, используемый `GridMergeResult`/`GridMergeGroupResult`. Уточнение аудита: сохранить эти типы вместо новых `DiceCellState` DTO. Переиспользовать `DicePiece`, `GridPlacementEntry/Request/Result`, `MovementRule`, `Neo.Merge.MergeResolver`; не копировать flood-fill/каскады/validation.

**Общий контракт, владелец `Neo.GridSystem`.** Новый `G/IGridCellSource.cs`: `FieldCell GetCell(Vector3Int position)` и `IEnumerable<FieldCell> GetAllCells(bool includeDisabled)`. `FieldGenerator` реализует его существующими методами. В `G/GridPlacementService.cs` добавить constructor `(IGridCellSource cells, Action<FieldCell> notifyChanged)` с двумя обязательными аргументами. В `G/Merge/GridMergeResolver.cs` добавить provider-overload `Resolve(IGridCellSource cells, GridMergeRequest request, IEnumerable<Vector3Int> directions, Action<FieldCell> notifyChanged)`. Внутри переиспользовать существующие validation, generic merge и преобразование результатов. Старые overloads сохраняются; обратная ссылка на Dice недопустима.

**Dice API и точные файлы.** Новые `D/IDiceBoardState.cs`, `D/DiceBoardState.cs`, `D/FieldGeneratorDiceState.cs`, `D/DiceBoardRules.cs`:

- `IDiceBoardState : IGridCellSource` предоставляет directions, `CanPlace`/`Place` для anchor + `IReadOnlyList<GridPlacementEntry>` + requireWalkable и уведомление изменённых клеток.
- `DiceBoardState(IEnumerable<FieldCell> cells, IReadOnlyList<Vector3Int> directions)`: произвольная форма, стабильный обход, уникальные фиксированные координаты; payload принадлежит потребителю.
- `FieldGeneratorDiceState` адаптирует существующее поле, читает актуальные movement directions и сохраняет legacy placement/notification path. Data-only backend использует provider placement service.
- `D/DiceBoard.cs`: добавить `DiceBoard(IDiceBoardState state, DiceBoardRules rules)` и `State`. Обязательный второй аргумент сохраняет однозначность `new DiceBoard(null)`. Rules копируются; setters работают. `Generator` прежний для legacy, nullable только для data-only, что документируется.

## 4. Порядок реализации и совместимость

1. Владелец `Neo.GridSystem` закрывает P1 arithmetic и добавляет свои регрессии; координатор проверяет актуальные общие файлы.
2. Централизованно выполнить 17 cases и соседние контракты, исправить реальные сбои.
3. Согласовать provider-сигнатуры с владельцем `Neo.GridSystem`, добавить общий provider путь с сохранением старых overloads; `Neo.Merge` остаётся переиспользуемой зависимостью без запланированных изменений.
4. Добавить backend и `T/Audit20261023NeoGridSystemDiceStateTests.cs`, адаптер и `T/Audit20261023NeoGridSystemDiceAdapterTests.cs`. Переключать core после сравнения результатов. Сохранить legacy порядок/количество cell events и callback timing.
5. В `D/DiceBoardService.cs` нужны только соответствующие XML/forwarding уточнения, новый компонент/editor/NoCodeAction не нужен. Сохранить пять serialized настроек, UnityEvents, defaults, методы и тип `DicePlacementResult.MergeResult`.
6. Обновить `U/README.md` и `U/DiceBoardService.md`: C# пример без GameObject, nullable Generator нового пути, владение состоянием, прежний scene/UnityEvent workflow. Новым package `.cs` нужны уникальные `.meta`; asmdef/config меняет только координатор при доказанной необходимости.

Сцены/prefabs/assets не мигрируют; layouts `Samples~/Demo`, `Samples/Demo`, `Assets/Samples/NeoxiderTools/<version>/Demo Scenes` сохраняются. Оценка: 4–6 дней Dice + 1–2 дня shared API/QA. Средний риск: seeds, mutable FieldCell, callbacks, nullable Generator. Несогласованный shared API откладывает только P2.

## 5. Приёмка и остановка

- Исправления: диапазоны у обеих int-границ завершаются; oversized cardinality отклоняется до allocation; пары из повторов различны; один distinct value даёт single; rotation сохраняет Z/value/source; oversized weights дают `ArgumentException("weights")` до RNG, ровно `int.MaxValue` допустим; merge `(int.MaxValue−1)+int.MaxValue` даёт cap либо `int.MaxValue`, правильную occupancy и один финальный board event.
- Минимальные P2 EditMode fixtures запускают одинаковые действия на data-only и FieldGenerator backend: rejected footprint без записи, masks/disabled/blockers, `RequireWalkable=false`, custom directions, null seeds, пары, каскад, clear, caps. Сравниваются final state, ordered seeds/results и board/merge events. Data-only fixture не создаёт GameObject; UnityEngine.Vector3Int dependency остаётся допустимой.
- Play Mode: существующая Dice sample smoke-проверка и один placement → merge → clear → reload сценарий с runtime-состоянием и консолью. Screenshot сам по себе не подтверждает механику. Новые PlayMode tests с прямыми Dice типами требуют центрального добавления references: текущий `Neo.Tests.Play.asmdef` их не содержит.
- Координатор компилирует runtime и EditMode tests на Unity 6.0–6.4 и 6.5+; release-проверка включает sample import. Новые version guards не нужны; если появится identity API, использовать централизованные StableId helpers, обе ветки реально компилировать.

Приёмка требует выполненных checks. При нарушении legacy serialization/result/event contracts остановить API-рефакторинг и устранить несовместимость. Lifetime/performance остаются непроверенными рисками. В фазе 2 Unity/build/test процессы не запускались.
