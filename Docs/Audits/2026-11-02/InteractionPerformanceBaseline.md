# Interaction: исходная производительность до оптимизации

Это измеренный baseline, а не подтверждение успешной оптимизации. Unity **6000.3.14f1**, Editor, D3D11, включённый Profiler; виртуальный ввод. Плотный положительный сценарий содержит H=N попаданий. CPU модели/питание/загрузка во время захвата не фиксировались; позднее наблюдался AMD engineering sample, 8 ядер / 16 потоков. Результаты не являются обещанием FPS в Player или на другом устройстве.

Все 12 capture cases прошли. Затем все 12 raw-файлов загружены настоящим `ProfilerDriver.LoadProfile` и разобраны неизменённым reader: по **120 positive + 120 stable-miss** кадров, ровно N AutomaticUpdate scopes на кадр, совпадающие фазы/ordinals и постоянный offset относительно counter CSV. Native metadata калибровка: payload4096 → фактические4128 байт.

| Режим | N | Positive Auto p50, мс | Positive Auto p95, мс | Classify p50, мс | Stable-miss Auto p50, мс | Auto GC на кадр, байт |
|---|---:|---:|---:|---:|---:|---:|
| camera2d | 1 | 0.01670 | 0.02530 | 0.00150 | 0.01430 | 0 |
| camera2d | 100 | 8.70865 | 9.63540 | 8.11270 | 0.31990 | 0 |
| camera2d | 1000 | 970.08515 | 1154.01990 | 949.65115 | 3.60565 | 0 |
| camera3d | 1 | 0.01930 | 0.03810 | 0.00210 | 0.01465 | 0 |
| camera3d | 100 | 8.73870 | 9.89050 | 8.25475 | 0.26950 | 0 |
| camera3d | 1000 | 955.59880 | 1169.38740 | 937.92565 | 3.08570 | 0 |
| keyboard2d | 1 | 0.04320 | 0.06420 | 0.00160 | 0.04105 | 3384 |
| keyboard2d | 100 | 11.61750 | 14.84010 | 8.32615 | 2.98075 | 338400 |
| keyboard2d | 1000 | 954.78440 | 1223.12360 | 904.01575 | 27.80455 | 3384000 |
| keyboard3d | 1 | 0.04035 | 0.06010 | 0.00190 | 0.03615 | 3384 |
| keyboard3d | 100 | 11.30470 | 12.08750 | 8.19350 | 2.67520 | 338400 |
| keyboard3d | 1000 | 1029.90765 | 1452.62390 | 975.96880 | 26.75855 | 3384000 |

Automatic CPU — сумма всех N внешних scopes; Classify и Native — вложенные диагностические затраты, их нельзя складывать с Automatic. All-thread GC измеряется отдельно и не равен library GC. External/EventSystem и его union остаются **NOT_MEASURED**. Числа GC в таблице подтверждены на каждом измеренном кадре обоих состояний, а не только как p50.

Подтверждены два остатка:

- Классификация повторно обрабатывает полный набор H для каждого из N потребителей. На Camera3D/N1000 Classify p50=937.92565мс, Automatic=955.59880мс; сам native ray p50=0.26690мс. Счётчики: native1/cache999/classificationPasses1000/inspectedHits1000000. Это локализованный плотный O(N×H) путь; распространение результата на разреженную сцену не заявляется.
- В клавиатурных режимах внутри Automatic выделяется ровно3384×N байт/кадр. В OptionalInputSystemAdapter обнаружены повторное создание ключей reflection-cache и boxing; доля каждой операции требует отдельного причинного теста. Выбор backend и поведение held/down/up менять ради цифр нельзя.

Общие функциональные регрессии interaction123/123 уже приняты, но они не подменяют исправление измеренных затрат. До принятия новых кандидатов сохраняются source/native review, новые live-lease и warm-allocation controls и идентичные измерения после изменения. Для индексированного пути root разрешил честную миграцию только диагностического счётчика inspected work; workload, positive parity, native/cache/epoch, frame stamps и временные окна сохраняются.

Основные источники:

- [Независимая проверка interaction](NativeInteractionExecutionReview.md).
- [Независимая проверка input polling и всех 12 данных](NativeInputPollingPerformanceReview.md).
- [Полный baseline JSON](D:/Temp/Neoxider-LibraryClosure-20261102/profiles/performance-baseline-u63-r1.json), [manifest raw/counter пар](D:/Temp/Neoxider-LibraryClosure-20261102/profiles/capture-manifest-u63-r1.json).
- Raw, source freezes, native launch/log и exporter receipts пока находятся в task-owned Temp. Их сохранение в финальном переносимом evidence-архиве ещё требуется; приватные preferences/configuration в публикацию не входят.

12 source/post comparisons и защищённые12 пользовательских путей совпали; Neo preferences51→51 без изменений. Ранняя ошибка queueR1 относилась к чтению nullable ExitCode оболочки; соответствующий native export был успешным и повторно не запускался. QueueR2 завершила остальные экспорты, сохранив исходные receipts.

## Повторное измерение после двух исправлений — 12 native readbacks

После принятых source/runtime изменений классификатора и Input adapter `29ed698b…` выполнены новые **12/12 capture PASS** и **12/12 завершённых native Load/readback**. Root и независимый reviewer проверили все 2880 строк, отдельные 120 positive +120 stable-miss кадров каждого режима, точные N scopes, постоянный frame offset и калибровку4128 байт. Frozen inputs5947, protected12 и публичные prefs51→51 совпали. [Независимая приёмка матрицы](NativeInputPollingPerformanceReview.md) завершена в этом ограниченном Editor scope; глобальная приёмка остаётся открытой.

| Режим | N | Positive Auto p50 до → после, мс | Stable-miss Auto p50 до → после, мс | Auto GC до → после, байт/кадр |
|---|---:|---:|---:|---:|
| camera2d | 1 | 0.01670 → 0.01760 | 0.01430 → 0.01235 | 0 → 0 |
| camera2d | 100 | 8.70865 → 0.48125 | 0.31990 → 0.30715 | 0 → 0 |
| camera2d | 1000 | 970.08515 → 6.22205 | 3.60565 → 3.35515 | 0 → 0 |
| camera3d | 1 | 0.01930 → 0.01840 | 0.01465 → 0.01410 | 0 → 0 |
| camera3d | 100 | 8.73870 → 0.38930 | 0.26950 → 0.24875 | 0 → 0 |
| camera3d | 1000 | 955.59880 → 4.56825 | 3.08570 → 2.66285 | 0 → 0 |
| keyboard2d | 1 | 0.04320 → 0.01875 | 0.04105 → 0.01280 | 3384 → 0 |
| keyboard2d | 100 | 11.61750 → 0.52635 | 2.98075 → 0.38275 | 338400 → 0 |
| keyboard2d | 1000 | 954.78440 → 6.18025 | 27.80455 → 3.71870 | 3384000 → 0 |
| keyboard3d | 1 | 0.04035 → 0.01940 | 0.03615 → 0.01365 | 3384 → 0 |
| keyboard3d | 100 | 11.30470 → 0.46435 | 2.67520 → 0.31030 | 338400 → 0 |
| keyboard3d | 1000 | 1029.90765 → 5.51915 | 26.75855 → 3.28170 | 3384000 → 0 |

GC0 подтверждён на **каждом** выбранном кадре всех24 окон внутри AutomaticUpdate; whole-frame/all-thread GC остаётся отдельным и ненулевым. Native1/cacheN−1 и classificationPassesN сохранены. Фактически просмотрено2N entries в positive и0 в miss, вместо старого N×H; размер сцены, попадания, mixed press/release edges, warmup и frame counts не уменьшались. Для N1000 positive наблюдается ускорение Automatic примерно154–209 раз. Для N1 camera2d наблюдается небольшое ухудшение0.0009мс; при неконтролируемой нагрузке его причина не установлена. Здесь нет утверждения об улучшении каждого percentile/устройства.

Ранний classifier-only Camera3D/N1000 замер сохраняется отдельно: positive955.59880→7.46650мс, stable-miss3.08570→4.57315мс (+48.2%). В более позднем combined замере miss=2.66285мс; это показывает вариативность наблюдений, но **не доказывает причину** первого ухудшения. Нагрузку и питание не контролировали; ни ранний результат, ни новый результат не заменяются друг другом.

Полные распределения не скрыты за медианами. Camera2D/N1 positive p95 вырос0.0253→0.0289мс; miss p95 —0.0193→0.0220мс, p99 —0.0237→0.0358мс, max —0.0279→0.0413мс. Camera2D/N100 miss p95 вырос0.4280→0.4624мс; Camera3D/N1 miss p99 —0.0303→0.0330мс. Это наблюдаемые ухудшения хвостов при нефиксированной нагрузке, а не подтверждённая причина в product-коде; blanket all-performance-PASS не заявляется.

Source vectors отражены по двум исходным cohort: для первых Camera3D3 случаев семь изменённых путей, для оставшихся9 — шесть; дополнительно10 новых regression/helper paths, удалений нет. Разница между cohort — уже принятое UI editor Undo исправление, применённое до оставшихся9 baseline captures. Runtime deltas — classifier и Input; capture/PlayerLoop изменили только честный диагностический inspected-work oracle. UI docs/editor и новые test/readback helpers отдельно перечислены в [comparison receipt](D:/Temp/Neoxider-LibraryClosure-20261102/profiles/performance-comparison-combined-u63-r1.json).

Данные: [after summary](D:/Temp/Neoxider-LibraryClosure-20261102/profiles/performance-combined-u63-r1.json), [after raw/counter manifest](D:/Temp/Neoxider-LibraryClosure-20261102/profiles/capture-manifest-combined-u63-r1.json), [завершённая очередь](D:/Temp/Neoxider-LibraryClosure-20261102/profiles/readback-queue-combined-u63-r2.json). Первый combined supervisor остановился **до Unity** из-за недоступного Get-FileHash в дочернем Windows PowerShell; сохранён failed receipt. R2 использовал текущий runtime pwsh, без изменения native reader и без повторения baseline.

Это Editor/D3D11 с профайлером, виртуальным вводом и ограниченным workload. External/EventSystem остаётся NOT_MEASURED. Отдельные Input calibration/focused old/new12/14-scope bytes, NewOnly/отсутствие Input System, AOT/stripping, Player, Unity6.6, clean consumers и остальные модули требуют своих результатов. Функциональная приёмка после этих двух исправлений: Input10Edit+5Play PASS и affected190Play PASS; Edit17 содержит16PASS и1обязательный NewOnly skip, который не превращён в pass.
