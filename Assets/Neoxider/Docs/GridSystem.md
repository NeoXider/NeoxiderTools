# GridSystem

## Описание

`GridSystem` — универсальный модуль сетки для Unity, который закрывает типовые задачи:
- генерация поля 2D/3D;
- настройка формы уровня (прямоугольник, hex-подобная форма, custom-маска);
- управление состояниями клетки (`enabled`, `walkable`, `occupied`, `content`);
- поиск пути с диагностикой причин, почему путь не найден;
- быстрый no-code workflow через Inspector;
- готовые прикладные слои для `Match3` и `TicTacToe`.

Модуль проектировался как базовая инфраструктура для головоломок, тактических игр и настольных механик.

---

## Архитектура модуля

### Базовое ядро (`Neo.GridSystem`)

- `FieldGenerator` — центральный runtime-компонент:
  - генерирует клетки;
  - дает API доступа к ячейкам/соседям;
  - хранит текущее состояние поля;
  - предоставляет фасад pathfinding API.
- `FieldGeneratorConfig` — конфигурация поля:
  - размер;
  - правило перемещения;
  - базовый тип формы (`GridType`);
  - shape-mask и ручные override;
  - origin-позиционирование поля относительно объекта (`Origin2D`, `OriginDepth`, `OriginOffset`).
- `FieldCell` — модель клетки:
  - `Position`, `Type`, `IsWalkable`, `IsEnabled`, `IsOccupied`, `ContentId`, `Flags`.
- `GridShapeMask` — ScriptableObject-маска формы.
- `GridPathfinder` — отдельный сервис pathfinding с `GridPathRequest`/`GridPathResult`.
- `MovementRule` — наборы направлений соседей (4/8/6/18/26, hex-like).
- `FieldSpawner` / `FieldObjectSpawner` — размещение префабов на сетке.
- `FieldDebugDrawer` — визуализация сетки и статусов клеток через Gizmos.

### Игровые надстройки

- `Neo.GridSystem.Match3`
  - `Match3BoardService`
  - `Match3MatchFinder`
  - `Match3TileState`
- `Neo.GridSystem.TicTacToe`
  - `TicTacToeBoardService`
  - `TicTacToeWinChecker`
  - `TicTacToeCellState`

---

## Состояния клетки

Каждая клетка (`FieldCell`) поддерживает несколько независимых измерений состояния:

- `IsEnabled` — входит ли клетка в форму текущего уровня.
- `IsWalkable` — можно ли проходить через клетку при pathfinding.
- `IsOccupied` — занята ли клетка объектом.
- `ContentId` — произвольное игровое состояние (тип фишки Match3, X/O в TicTacToe и т.д.).
- `Type` — дополнительный пользовательский тип клетки.

Это разделение позволяет:
- отключать клетки геометрически, не ломая данные;
- держать разные правила проходимости для разных механик;
- использовать один и тот же грид в нескольких игровых режимах.

---

## Формы поля (Shape)

Поддерживается каскад формирования финального `IsEnabled`:

1. Базовая форма (`GridType`):
   - `Rectangular`
   - `Hexagonal` (аппроксимация через axial distance)
   - `Custom` (база выключена, включение только через маску/override)
2. `GridShapeMask`:
   - `EnabledCells` — whitelist;
   - `DisabledCells` — blacklist.
3. Ручные override в `FieldGeneratorConfig`:
   - `DisabledCells`
   - `ForcedEnabledCells`

Дополнительно для проходимости:
- `BlockedCells`
- `ForcedWalkableCells`

Это удобно для уровней Match3 с «дырками», асимметричными контурами и особыми тайлами.

---

## Origin и позиционирование поля

`FieldGenerator` теперь поддерживает якорное размещение поля относительно Transform объекта.

Параметры:
- `Origin2D`:
  - нижний ряд (`Bottom*`),
  - центр (`*Center*`),
  - верхний ряд (`Top*`);
- `OriginDepth`: `Front`, `Center`, `Back`;
- `OriginOffset`: дополнительный ручной сдвиг в координатах клетки.

Практика:
- Для UI/пазлов чаще всего удобно `Origin2D = Center` (по умолчанию), чтобы поле появлялось по центру объекта.
- Для «роста от угла» выбирайте `BottomLeft` или другой крайний якорь.

---

## Pathfinding

`GridPathfinder` работает через `GridPathRequest` и возвращает `GridPathResult`.

В запросе можно:
- передать `Directions` override (например, временно перейти с 4-way на 8-way);
- игнорировать отдельные состояния (`IgnoreOccupied`, `IgnoreDisabled`, `IgnoreWalkability`);
- подключить `CustomPassabilityPredicate`.

Результат дает:
- `Path` (список клеток от старта до цели);
- `Reason` (`NoPathReason`) при неудаче.

`NoPathReason`:
- `InvalidStartOrEnd`
- `StartNotPassable`
- `EndNotPassable`
- `NoPathFound`

Это удобно для UI/дебага: можно показывать игроку не только «пути нет», но и почему.

---

## Match3 API (кратко)

`Match3BoardService`:
- `InitializeBoard()` — заполнить поле случайными тайлами.
- `TrySwapAndResolve(a, b)` — обмен соседних клеток с валидацией и каскадным resolve.
- `FindMatches()` — получить текущие матч-группы.

Логика учитывает:
- только `IsEnabled` клетки;
- недоступные и занятые клетки не используются как валидные позиции.

---

## Match3 Runtime View (Demo)

Для рабочей demo-сцены Match3 используется runtime-представление поля:

- `GridSystemMatch3BoardView` (`Neo.Demo.GridSystem`)
  - рисует фишки в world-space;
  - поддерживает выбор клеток мышью;
  - делает swap пары соседних клеток через `Match3BoardService.TrySwapAndResolve(...)`;
  - обновляет визуал при изменениях борда.

---

## TicTacToe API (кратко)

`TicTacToeBoardService`:
- `ResetBoard()`
- `TryMakeMove(Vector2Int/Vector3Int)`
- `IsBoardFull()`

События:
- `OnPlayerChanged`
- `OnWinnerDetected`
- `OnDrawDetected`
- `OnBoardReset`

Проверка победы вынесена в `TicTacToeWinChecker.GetWinner(...)`.

---

## TicTacToe Runtime View (Demo)

Для рабочей demo-сцены TicTacToe используется runtime-представление поля:

- `GridSystemTicTacToeBoardView` (`Neo.Demo.GridSystem`)
  - рисует кликабельные клетки в world-space;
  - показывает `X/O` по `ContentId`;
  - отправляет клик в `TicTacToeBoardService.TryMakeMove(...)`;
  - автоматически обновляется при reset/ходе/победе.

Это demo-компонент. Он не обязателен для core API, но нужен для визуальной и интерактивной сцены без дополнительной ручной сборки UI-поля.

---

## Спавн и занятость

- `FieldSpawner` — простой спавнер на проходимых клетках.
- `FieldObjectSpawner` — спавн + трекинг по клеткам + управление занятостью.

Если `occupiesSpace = true`, `FieldObjectSpawner` синхронизирует `FieldCell.IsOccupied`.
Это напрямую влияет на pathfinding при `PassabilityMode = WalkableEnabledAndUnoccupied`.

---

## Визуальная отладка

`FieldDebugDrawer` рисует:
- сетку;
- клетки по цветам состояния:
  - walkable
  - blocked
  - disabled
  - occupied
- координаты;
- debug-path.

Для работы включите `DebugEnabled` у `FieldGenerator`.

---

## Быстрый старт

1. Создайте `GameObject`, добавьте:
   - `Grid` (Unity),
   - `FieldGenerator`.
2. Настройте `FieldGenerator.Config`:
   - `Size`,
   - `MovementRule`,
   - `GridType`,
   - `Origin2D` (обычно `Center`),
   - `OriginDepth` (обычно `Center`).
3. Сгенерируйте поле:
   - auto в `Awake/OnValidate`, либо `GenerateField()`.
4. По желанию добавьте:
   - `FieldDebugDrawer`,
   - `FieldSpawner`/`FieldObjectSpawner`,
   - `Match3BoardService` или `TicTacToeBoardService`.

Для удобной ручной работы в Inspector у `FieldGenerator` доступны кнопки:
- `Regenerate Field`
- `Apply Shape && Overrides`
- `Clear Manual Overrides`
- `Set Origin Center`

---

## Примеры

### Проверка пути

```csharp
var result = fieldGenerator.FindPathDetailed(
    start: new Vector3Int(0, 0, 0),
    end: new Vector3Int(7, 7, 0),
    ignoreOccupied: false
);

if (result.HasPath)
{
    Debug.Log($"Path length: {result.Path.Count}");
}
else
{
    Debug.LogWarning($"No path: {result.Reason}");
}
```

### Ручное изменение состояния клетки

```csharp
fieldGenerator.SetEnabled(new Vector3Int(2, 3, 0), false);
fieldGenerator.SetWalkable(new Vector3Int(1, 1, 0), false);
fieldGenerator.SetOccupied(new Vector3Int(4, 5, 0), true);
fieldGenerator.SetContentId(new Vector3Int(0, 0, 0), 2);
```

### Match3 swap

```csharp
bool success = match3Board.TrySwapAndResolve(
    new Vector3Int(1, 2, 0),
    new Vector3Int(2, 2, 0)
);
```

### TicTacToe move

```csharp
bool moveApplied = tttBoard.TryMakeMove(new Vector2Int(1, 1));
```

---

## Демо

Сцены:
- `Assets/Neoxider/~Samples/Demo/Scenes/GridSystem/GridSystemMatch3Demo.unity`
- `Assets/Neoxider/~Samples/Demo/Scenes/GridSystem/GridSystemTicTacToeDemo.unity`

Setup-скрипты:
- `Assets/Neoxider/~Samples/Demo/Scripts/GridSystem/GridSystemMatch3DemoSetup.cs`
- `Assets/Neoxider/~Samples/Demo/Scripts/GridSystem/GridSystemTicTacToeDemoSetup.cs`
- `Assets/Neoxider/~Samples/Demo/Scripts/GridSystem/GridSystemMatch3BoardView.cs` (runtime view для интерактивного Match3 поля)
- `Assets/Neoxider/~Samples/Demo/Scripts/GridSystem/GridSystemTicTacToeBoardView.cs` (runtime view для кликабельного поля)

Если сцена пустая:
1. Создайте объект.
2. Добавьте соответствующий `*DemoSetup`.
3. Нажмите `Setup Scene` в Inspector.

---

## Рекомендации по использованию

- Для новых игровых режимов используйте `ContentId` как базовый state-id и держите собственный сервис-слой логики.
- Не смешивайте «форму» (`IsEnabled`) и «проходимость» (`IsWalkable`) — это разные уровни правил.
- Для сложных уровней (Match3) используйте `GridType.Custom` + `GridShapeMask`.
- Для больших полей обновляйте только измененные клетки, а не полную регенерацию поля.
