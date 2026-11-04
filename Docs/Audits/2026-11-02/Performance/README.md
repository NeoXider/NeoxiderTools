# Проверка производительности всей библиотеки

Это продолжение существующих функциональных аудитов 69 областей. Владелец отдельно подтвердил, что проверка узких мест нужна во всей библиотеке, включая raycast/interaction. Зелёные функциональные тесты и уменьшенное число native-запросов не заменяют измерения CPU, GC и повторной обработки результатов.

## Измеренные исправления и открытая приёмка

**Статус на 2026-11-04 (12.0.0).** After-измерения выполнены: два прогона явной категории `Performance` (Unity 6000.3.14f1 Editor, Mono, batch, profiler off) записаны в [MeasuredResults-12.0.md](MeasuredResults-12.0.md) вместе с калибровочными строками, а сырые строки `NEO_PERF` лежат в [Raw](Raw/). Измерены Input, Interaction, Grid/Selector, StateMachine, Cards, ResourcePool, Reactive, Abilities, UI Mesh Rig и пулы; срезы без timing suite (Drawer smoothing, NoCode bindings и Condition evaluate, Quest, Inventory autosave bursts, Shop refresh, network due polls) перечислены там как [NOT_MEASURED](MeasuredResults-12.0.md#not_measured). Повторный прогон на дереве после wave 4 оставлен в том же файле отдельной таблицей-заготовкой. Абзацы ниже — история приёмки до этих замеров.

- Shared physics query сохраняет один запрос на стабильный **точный контекст**, разные маски/лучи/режимы и законные callback invalidations. Индексированный classifier применён после source review: функциональные129/129 +Core30 прошли, включая шесть новых live-lease parity случаев и полный legacy fallback. В новом combined12 замере denseN1000 Automatic p50 примерно4.6–6.2мс против прежних955–1030мс; фактический inspection2N вместоN×H при прежнем workload. Это наблюдение Editor с Profiler, не универсальное обещание Player FPS.
- Optional Input adapter29ed применён после настоящего calibrated native-call RED. Прежние allocation failures6 стали PASS:10Edit+5Play; affectedPlay190/190 и Edit16PASS/1NewOnlySkip независимо приняты. Все2880 выбранных кадров combined12 native readbacks дали0байт внутри AutomaticUpdate против прежних3384×N в keyboard routes. Отдельный focused12/14-scope Input raw-byte gate остаётся открыт; invalid managed-counter calibration4096→0 не используется как доказательство.

Источники: [baseline и полная after-матрица](../InteractionPerformanceBaseline.md), [interaction review](../NativeInteractionExecutionReview.md), [input/data review](../NativeInputPollingPerformanceReview.md). Baseline12 и combined12 — настоящие completed native capture/readback наборы. Все медианы miss позднего combined ниже исходных, однако отдельные tail percentiles ухудшились; ранний classifier-only Camera3D miss+48.2% сохранён, причина вариативности не установлена. CPU/GC других модулей, External/EventSystem, Player/AOT, Unity6.6 и clean consumer contexts этим не закрыты.

## Распределение повторной проверки горячих путей

| Автор | Области | Отчёт | Стадия |
|---|---:|---|---|
| SAME CLI03 | 22 gameplay/presentation owners | [GameplayPresentation.md](GameplayPresentation.md) | Source follow-up прочитан root и принят независимым reviewer; измеренные срезы — в [MeasuredResults-12.0.md](MeasuredResults-12.0.md), остальные NOT_MEASURED |
| SAME CLI04 | 16 tools/extensions owners | [Tools.md](Tools.md) | Source follow-up принят; известные Input/interaction fixes не дублируются |
| SAME CLI05 | 31 foundation/infrastructure owners | [FoundationInfrastructure.md](FoundationInfrastructure.md) | Source follow-up принят; test/README/editor-only owners выделены явно |

Все69 owners перечислены без повторного подсчёта cross-owner actions. Root прочитал три отчёта; независимые [31-owner review](NativeFoundationInfrastructureReview.md) и [38-owner review](NativeGameplayToolsReview.md) подтвердили coverage и source-cost claims. Audio.Editor сложность исправлена на O(F×C²). Это source-приёмка, не измерения69 модулей. Новые implementation-задачи открываются после локализации причины и проверки плана.

Первые bounded slices для измерений: Grid valid-swap search, Selector getters, Drawer smoothing, MeshRig Build и Ability EventBus.Publish; для foundation — Quest/Health/Reactive, autosave, runtime bindings, Inventory/Shop и network due polls. У каждого нужны собственные marker/phase/native-context controls: фиксированный Interaction reader нельзя использовать как готовую приёмку другого модуля.

Для первых Grid/Selector slices source/SDK corrections завершены, actual6captures+6native readbacks дали [полный baseline](MeasuredGridSelectorBaseline.md). На32×32 поиск без хода показал80.71мс/5.38МБ за вызов; Selector1000 —4.80мс/4.24МБ за100групп из3getters. Root проверил counts/calibration/source/outcomes; независимая actual приёмка матрицы продолжается. Повторное использование snapshot/pure-getter core реализовано, after-измерения записаны в [разделе After](MeasuredGridSelectorBaseline.md#after-1200) (32×32 без хода: 37.3 → 0.52 мс, 4978 → 0 аллокаций; Selector1000: 4.77 → 0.0075 мс за 100 групп). Срезы без timing suite перечислены как [NOT_MEASURED](MeasuredResults-12.0.md#not_measured).

## Что считается достаточной проверкой

Для каждого горячего пути фиксируются фактический вызывающий код, частота, размер данных и конфигурация. Повторные physics/overlap/pathfinding queries, обходы N×N/N×H, scene search/GetComponents, reflection, строки/serialization, snapshot allocation и ненужные UI/mesh/material rebuilds рассматриваются вместе с guards и реальной частотой вызова. Явное сохранение по кнопке не становится Update-багом из-за наличия serialization.

Объектные сценарии обычно проверяются на1/100/1000 объектов; плотность попаданий, подписчики, вершины и сообщения масштабируются отдельно, когда это соответствует контракту. Нужны прогретые положительный и отрицательный сценарии, no-change контроль, точные functional/native payloads и корректная обработка полного набора данных. Для frame-профилей сохраняются120 выбранных кадров и raw/calibration/stamp alignment, если такой формат применим.

CPU библиотеки отделяется маркерами/поддеревьями от setup, отражения теста, записи файлов, Editor и ввода стенда. Вложенные diagnostic times не складываются с inclusive total. GC требует прошедшей калибровки; all-thread GC отделяется от измеренного участка. Отсутствующий внешний контекст обозначается `NOT_MEASURED`. Source reasoning, functional PASS, отсутствующие samples/пакеты и capability skips не превращаются в performance PASS.

Оптимизация сохраняет публичные API, serialized settings, полные попадания, порядок событий, reentry/exception/snapshot semantics и ownership. Для меньших реальных inspection counts допускается явная честная миграция диагностического oracle с более строгим обоснованным бюджетом; подмена workload, пропуск положительных проверок и вымышленные счётчики запрещены.

Проверка не объявляет каждое отложенное улучшение дефектом. В итогах отдельно отмечаются измеренный дефект, повторная работа с ещё не измеренной стоимостью, гипотеза и допустимая цена существующего контракта. Утверждение «вся библиотека оптимизирована» до фактической приёмки не используется.
