# Neo.Tools.Random — индивидуальный план реализации

Статус: план второго этапа; новых изменений реализации нет. Основание: завершённый [аудит](Neo.Tools.Random.md), актуальная запись manifest и повторная сверка текущих трёх исходников, регрессий, документации и границы `Neo.Extensions`.

## Текущий результат

Выполнены RND-01–06: инвалидирование суммы при нормализации; исключение нулевых весов и корректные границы; double-промежуточные вычисления; обработка неконечных входов; безопасный self-copy; возврат собственного результата при вложенном уведомлении. Изменены `ChanceManager.cs` и `ChanceSystemBehaviour.cs`; `Data/ChanceData.cs` полностью проверен без изменения. Все пути исходников ниже относятся к `Assets/Neoxider/Scripts/Tools/Random/`, сборка `Neo.Tools.Random`.

Обновлены пять существующих страниц `Assets/Neoxider/Docs/Tools/Random/`: `README.md`, `ChanceManager.md`, `ChanceSystemBehaviour.md`, `Data/README.md`, `Data/ChanceData.md`. Добавлен `Assets/Neoxider/Tests/Edit/Tools/Random/Audit20261023NeoToolsRandomRegressionTests.cs`: 16 методов, 24 случая NUnit, уникальная `.meta`.

Пройдены только статические проверки: полный инвентарь 3/3, UTF-8, whitespace, объявления публичного API/serialized fields относительно `ca198556`, ссылки, GUID. Числовые демонстрации Python не заменяют выполнение C#. Unity, компиляция, EditMode, PlayMode и build не запускались. Повторно реализовывать исправленные RND-01–06 не требуется.

## Приоритеты, оставшиеся дефекты и проверка

**Подтверждённых неисправленных дефектов внутри сборки не найдено.** Следующие пункты являются долгами проверки, а не доказанными новыми ошибками.

| Приоритет / владелец | Основание и минимальное действие | Наблюдаемый результат |
| --- | --- | --- |
| P1, координатор | После завершения изменяющих код аудитов скомпилировать интегрированный пакет; выполнить `Neo.Editor.Tests.Audit20261023NeoToolsRandomRegressionTests` и существующий `ChanceManagerTests`. Исправлять только реальные отказы в соответствующих исходниках/регрессиях. | Все 24 новых случая и существующие тесты проходят; новые compile/runtime errors отсутствуют. |
| P1, координатор и владелец Random | Реальный lifecycle `ChanceSystemBehaviour.Awake` и копирование `ChanceData` пока не проверены. Добавить `Assets/Neoxider/Tests/Play/Audit20261023NeoToolsRandomLifecycleTests.cs` и `.meta`; использовать временные объекты с очисткой, без сохранения сцен/ассетов. | После активации компонента таблица скопирована; изменение веса/метаданных экземпляра не меняет asset. Component roll не вызывает asset event. |
| P1, координатор | В `Assets/Neoxider/Tests/Play/Neo.Tests.Play.asmdef` сейчас отсутствует ссылка `Neo.Tools.Random`. Добавить её централизованно перед включением предложенного PlayMode-файла. В EditMode ссылка уже имеется. | Новый lifecycle fixture обнаруживается и компилируется; shared asmdef не изменяется работником Random. |

В lifecycle-проверке также наблюдать порядок index → probability → per-index → completion; пустая таблица даёт `-1`/`null`, вероятность `0`, один completion и ни одного per-index события. При вложенном `SetResultAndNotify(1)` внешний roll возвращает исходную entry, а last-result отражает вложенную. Это закрывает непроверенные event/lifecycle контракты, не вводит новую игровую функциональность.

**Межсборочная сверка:** исходный cross-boundary finding уже исправлен в текущем `Assets/Neoxider/Scripts/Extensions/RandomExtensions.cs:GetRandomWeightedIndex`: сумма и random point используют `double`, NaN/infinity отклоняются. Владелец — `Neo.Extensions` (`/root/audit_extensions`). Это выполненная правка другого аудитора, её интегрированная проверка ещё не подтверждена. Координатор сверяет его аудит/регрессии до общего запуска; повторная правка RandomExtensions из этого плана не нужна. Контракты различаются намеренно: extension отклоняет некорректную таблицу, manager санитизирует веса.

Прямая запись `Entry.Weight` по-прежнему требует `InvalidateTotalWeight()`/`Normalize()`. Это документированный контракт. Автораспределение полностью нулевой таблицы, locked sum выше target и float `TotalWeight == +Infinity` при огромной сырой сумме также сохранены намеренно.

## P2 — предложение: immutable snapshot и типизированный результат

**Проблема:** изменяемые entry затрудняют безопасное повторное использование таблиц; выбор линейный; после изменения порядка разработчики могут спутать индекс награды с `CustomId`. Снимок делает время обновления явным и позволяет многократно выбирать без доступа к mutable asset.

**Reuse:** использовать текущие `Entries`, `Entry.CustomId`, правила `GetId(float)`, `EnsureUniqueIds()` по явному выбору вызывающего кода и `CopyFrom()` для независимой конфигурации. `RandomExtensions.GetRandomWeightedIndex` повторно проверен: он использует глобальный RNG, не возвращает метаданные и не создаёт снимок. Готового snapshot API в проверенном модуле/каталоге нет. Новые интерфейсы RNG, сервисы и отдельная сборка не нужны.

Предлагаемый минимальный публичный API:

```csharp
// Дополнение ChanceManager:
public ChanceTableSnapshot Compile();

// Новый immutable класс:
public int SelectableCount { get; }
public double TotalWeight { get; }
public bool TryEvaluate(float randomValue, out ChanceSelection selection);

// Новый readonly struct ChanceSelection:
public int Index { get; }
public int CustomId { get; }
public float NormalizedWeight { get; }
```

`Compile()` читает фактические веса заново, независимо от mutable cache; копирует положительные cumulative weights, исходные индексы, IDs и вероятности в закрытые массивы. Не вызывает `Normalize`/`EnsureUniqueIds`, не меняет источник и не хранит ссылки на entry/Unity objects. Пустая таблица создаёт пустой снимок. Sampling выполняет binary search первой границы строго больше значения; `1` выбирает последний положительный вес, NaN возвращает false и результат `(-1, -1, 0)`. Использовать те же clamp/sanitization правила, что у manager, без Unity API в snapshot-классе. Создание — O(n), выбор — O(log n), без выделений при sampling.

IDs в первом инкременте остаются метаданными: дубликаты/`-1` не исправляются молча. Lookup с обязательной уникальностью и новый `OnCustomIdSelected` — отдельные возможные расширения, не обязательная часть этой реализации. Текущие UnityEvents продолжают передавать индексы. Не вводить скрытый snapshot cache: прямые изменения entry не обеспечивают надёжной автоматической инвалидации.

**Точные будущие файлы:** изменить `ChanceManager.cs`; добавить `ChanceTableSnapshot.cs`, `ChanceSelection.cs` и уникальные `.meta` в том же runtime-каталоге; добавить `Assets/Neoxider/Tests/Edit/Tools/Random/Audit20261023NeoToolsRandomSnapshotTests.cs` и `.meta`. Обновить существующие `README.md`, `ChanceManager.md`, `Data/ChanceData.md` примерами C# compilation/recompilation. `ChanceSystemBehaviour.cs`/его guide менять только при появлении конкретного запроса на scene adapter; текущий NoCode workflow остаётся рабочим без snapshot. `ChanceData.cs` менять не требуется.

**Минимальная приёмка:** raw `[0,2,6,0]` на `0/0.25/1` возвращает исходные индексы `1/2/2`, верные IDs и вероятности; изменение/удаление исходных entry не меняет старый снимок, recompilation отражает правку; source cache специально оставлен stale, но новый снимок использует новые веса; пустой/нулевой снимок и NaN дают false; большие и subnormal веса работают; одинаковые снимки на одинаковой последовательности значений дают одинаковый результат. Выделения после прогрева проверить профилировщиком существующего окружения, без установки performance-пакетов; не обещать ускорение по непроверенному wall-clock benchmark.

Оценка: 2–3 дня, средний риск на floating-point границах и ожиданиях freshness. API additive; существующие serialized fields, asset GUID, scene/prefab ссылки и random-provider семантика сохраняются.

## Порядок и условия завершения

1. Закрыть P1 compile/regressions после стабилизации общей рабочей копии; реальные отказы исправлять минимально и повторять затронутые проверки.
2. Координатор добавляет PlayMode reference; выполнить lifecycle/event проверки. Независимый snapshot не должен блокировать выпуск уже исправленного модуля.
3. Если P2 включён в объём реализации: добавить value/snapshot API, focused tests, затем документацию. Component/editor adapters добавлять только под конкретный workflow.
4. Координатор проверяет компиляцию на Unity 6.0–6.4 и отдельно 6.5+, записывает точные версии, исходную ревизию и результаты. Новых version branches не требуется; при появлении object-ID API использовать центральные `NeoDiagnostics.StableId`/`NeoComponentHealth.StableId`, без локальных `#if`.
5. При упаковке проверить новые `.meta`, включение исходников в UPM и неизменность публичных serialized контрактов. Не менять активный `Samples~` layout; если позднее появится sample, учитывать developer `Samples` и импортированный `Assets/Samples/NeoxiderTools/<version>/<sample name>` централизованно.

Завершение P1 допускается только при успешных фактических compile/EditMode/lifecycle проверках. Недоступная Unity-версия или неисполненный fixture остаётся явным долгом, а не pass. P2 считается выполненным только после перечисленных наблюдаемых сценариев; без его включения это предложение. При необходимости сменить event identity, сериализацию или межсборочную зависимость остановить только расширение и передать координатору конкретный выбор; routine reversible corrections дополнительного разрешения не требуют.

Проверка самого phase-2 артефакта: UTF-8, whitespace и scoped diff check; код, тесты, публичные документы и исходный аудит этим этапом не меняются.
