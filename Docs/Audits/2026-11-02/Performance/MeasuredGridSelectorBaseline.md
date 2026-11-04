# Grid / Selector: измеренный baseline

Unity6000.3.14f1 Editor/D3D11, включённый Profiler, нагрузка/питание вне стенда не контролировались. Все6 отдельных capture cases прошли; все6 закрытых raw загружены настоящим ProfilerDriver и экспортированы fixed-protocol reader. Root проверил по120positive+120control кадров, точное K операций/кадр, пустой контроль GC0, native calibration1event/4128bytes, фактические outcomes/state/purity и совпадение5955frozen inputs/protected12/preferences51→51. Независимое review первой Grid8 цепочки и последующей матрицы ведётся в [отчёте](NativeLibraryHotPathCaptureReview.md); source acceptance сама по себе не считается измерением.

| Сценарий | K | Positive p50, мс | Positive GC, байт / calls за выбранный кадр | Control p50, мс | Control GC, байт / calls |
|---|---:|---:|---:|---:|---:|
| Grid8×8 | 1 | 0.06585 | 5312 / 53 | 0.35655 | 34280 / 335 |
| Grid16×16 | 1 | 0.33545 | 25728 / 101 | 5.22465 | 378792 / 1455 |
| Grid32×32 | 1 | 2.49165 | 174080 / 197 | 80.70955 | 5375144 / 5999 |
| Selector1 | 100 | 0.52265 | 129600 / 2400 | 0.00810 | 0 / 0 |
| Selector100 | 100 | 1.03695 | 456000 / 2400 | 0.00780 | 0 / 0 |
| Selector1000 | 100 | 4.79570 | 4238400 / 2400 | 0.00810 | 0 / 0 |

Grid operation — один TryFindValidSwap. Positive содержит направленный вниз канонический обмен из существующей регрессии; это не worst-case positive или типичная random/four-direction доска. Control — поле без допустимого обмена. Disabled/blocked/occupied cells и payload identity включены, поиск не меняет поле. Все выбранные результаты сохраняются и проверяются после остановки записи. Стоимость34КБ/379КБ/5.38МБ и рост controlCPU относятся к одному вызову, не к безусловному Update каждого кадра.

В текущем Match3BoardService.WouldSwapCreateMatch полный BoardSnapshot создаётся для каждой соседней пары. Это локализованный источник повторного копирования: план исследует reuse снимка внутри одного вызова поиска и гарантированный undo для каждой попытки. Существующие snapshot/rule/match helpers переиспользуются; gameplay-правила, candidate order, purity и API не меняются без принятого решения. Реализация и after-измерения ещё не выполнены.

Selector operation — **группа из трёх getter**, IsAtStart+IsAtEnd+логический CountActive; K100 означает300getter calls за выбранный кадр. Поэтому4.24МБ в последней строке — за100групп, не за один getter. Positive — component с virtual Count, непустыми E/U, unique/fill и выключенными saving/network/Items-активациями; control — отдельный неизменяемый SelectorModel snapshot с теми же выходами. Отдельные Items.activeSelf/SelectorItem/native-network пути этим не измерены.

GetCurrentBounds и ComputeLogicalCountActive повторно вызывают SyncModelFromFields/Configure, включая defensive E/U snapshots. План ограничен подтверждённым read-only путём и переиспользованием существующей pure bounds/effective-index логики. Mutating Set, same-index notifications/save/network, public enumerable isolation и live serialized-field semantics остаются обязательными. Новая реализация/zeroGC обещание пока отсутствуют.

CPU/GC считаются только внутри ProductOperation. Stamp/empty/fixture checks и весь frame/all-thread GC экспортированы отдельно; они не вычитаются, чтобы создать искусственную производительность. Ни один arbitrary CPU threshold не использован, zeroGC для Grid/Selector не требуется для признания записи корректной. Это baseline для локализованных оптимизаций, не all-library performance PASS, Player FPS, AOT или другая Unity-ветка.

Исходные profiler captures этого baseline (root summary, очередь пяти последующих случаев, launch/XML/log/raw/metadata/counters/native JSON/CSV и source freezes) остались на машине автора и в репозиторий не входят; повторяемое измерение — явные NEO_PERF-наборы `Neo.Editor.Tests.GridSystem.Match3SearchPerformanceTests` и `Neo.Tools.Tests.SelectorPerformanceTests` (раздел After ниже). Первая ошибка resolver была исправлена до импорта; initial Grid8 compile abort CS0246 сохранён с0executed и не считается product red. После удаления отсутствующего NUnit attribute actual Grid8R2 скомпилирован/исполнен; остальные5 используют тот же неизменённый f125/626 protocol.

## After (12.0.0)

Реализация из плана выполнена: `Match3BoardService` переиспользует один снимок поля внутри поиска с гарантированным undo каждой попытки, а логические getter-ы `Selector` (`IsAtStart`, `IsAtEnd`, логический `CountActive`) считают границы и счётчик чистыми функциями `SelectorModel` без повторной конфигурации модели и защитных снимков E/U. Замер — явные NEO_PERF-наборы, а не profiler capture выше: Unity 6000.3.14f1 Editor, Mono, batch, **Profiler выключен**, Stopwatch-батчи плюс отдельный проход GC.Alloc recorder; калибровочные строки `grid.match3.*.calibration.gc_allocs_byte4096` и `view.selector.*.calibration.gc_allocs_byte4096` дают ровно один вызов на удерживаемый `byte[4096]`. «До» — прежний алгоритм (`legacy_emulated`) в том же прогоне и том же harness. Числа baseline-таблицы выше сняты с включённым Profiler и напрямую с этими не сравниваются; сравнивается только пара внутри одного прогона.

| Сценарий | До, p50 / GC.Alloc calls | После, p50 / calls | Прогон 2: до → после |
|---|---:|---:|---:|
| Grid8×8 без хода, 1 вызов | 0.178 мс / 274 | 0.027 мс / 0 | 0.287 → 0.041 мс |
| Grid16×16 без хода | 2.41 мс / 1202 | 0.119 мс / 0 | 4.11 → 0.186 мс |
| Grid32×32 без хода | 37.34 мс / 4978 | 0.518 мс / 0 | 74.5 → 0.874 мс |
| Grid32×32, канонический ход | 1.17 мс / 163 | 0.058 мс / 0 | 1.98 → 0.095 мс |
| Selector1, 100 групп по 3 getter | 0.374 мс / 2400 | 0.0043 мс / 0 | 0.64 → 0.0068 мс |
| Selector100, 100 групп | 0.886 мс / 2400 | 0.0042 мс / 0 | 1.47 → 0.0073 мс |
| Selector1000, 100 групп | 4.77 мс / 2400 | 0.0075 мс / 0 | 4.46 → 0.0063 мс |

Прогон 1 — дерево после волн 1-2, прогон 2 — после волны 3 (машина была загружена параллельной компиляцией, поэтому абсолютные числа шумнее; отношения сохраняются). Сырые строки: [прогон 1](Raw/neo-perf-run1-editmode.txt), [прогон 2](Raw/neo-perf-run2-editmode.txt); полная запись и NOT_MEASURED — в [MeasuredResults-12.0.md](MeasuredResults-12.0.md). Это по-прежнему не Player FPS, не AOT и не другая версия Unity.
