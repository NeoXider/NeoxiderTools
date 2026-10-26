# Neo.Tools.Time — индивидуальный план реализации

Основание: [аудит](Neo.Tools.Time.md), [контракт фазы 2](PLAN-CONTRACT.md), текущие manifest/исходники. База: `ca198556`, пакет `10.19.0`. Тот же аудитор; фаза 2 изменяет только этот план. Предложения ниже не реализованы.

## 1. Выполнено и проверено

Просмотрены четыре C# файла. Исправлены отмена и looping `Timer`, пустые циклы, границы длительности, pause/stop/reset, накопленный delta, callbacks, milestones, infinite/day setters, paused infinite RealTime persistence, календарное переполнение, нечисловой timeScale, импорт DOTween и NeoDoc-путь.

Обновлены пять существующих публичных страниц. Написаны 27 NUnit cases в четырёх новых C# файлах, включая typed probe. Выполнены только статические проверки: scoped `git diff --check`, UTF-8, ссылки, GUID, декларации публичных API/serialized fields, баланс конструкций. Компиляция, тесты, Play Mode и фактические Unity logs **не проверены**. Эти исправления не повторять как будущую работу.

## 2. Оставшиеся исправления и долг проверки

**F1 / P2 / владелец Neo.Tools.Time — неполная защита событий при изменении времени.** `TimerObject.cs:693–764` проверяет `playbackGeneration`, но `SetTime`, `SetProgress`, `SetDuration` на строках 989–1081 её не меняют. Воспроизведение: duration=10, countUp=true, updateInterval=0; однократный `OnProgressChanged(1) -> SetTime(5)`; затем `Tick(10)`. Вложенная публикация выдаёт 50%, после неё старая выдаёт 100% и completion при CurrentTime=5. Это подтверждено путём выполнения исходника, без runtime-прогона. Минимально: учитывать typed mutations в версии состояния, прекращать устаревшую публикацию до UI/completion, проверить продолжения Play/Reset/Stop и infinite/day веток. Регрессии: конечные CurrentTime=5, последний percent=50, старый completion отсутствует; аналогичные однократные callbacks для остальных setters. SetTime самостоятельно не должен запускать таймер.

**F2 / P3 / владелец Neo.Tools.Time — null не восстанавливает стандартный clock.** `TrustedClock.cs:199–204`: `SetClockForTests(null)` оставляет `_restored=true`; `Clock` создаёт стандартный clock, но `EnsureRestored` пропускает сохранённую high-water отметку. Минимально использовать `ResetStaticState()` для null, сохраняя `markRestored` для ненулевого clock. Проверка: записанная отметка снова видна через `Clock.HighWaterUtc`; восстановить прежние provider/key/static state в finally. Публичную сигнатуру сохранить.

**V1 / P1 / координатор после завершения source-аудитов.** Выполнить 27 новых cases, существующие `TimerTests`, `AuditFixesTimeTests`, `TrustedClockTests`. Добавить проверки планировщика: zero-duration loop уступает кадры; внешняя отмена, paused cancellation и Stop→Start в одном кадре не завершают новый запуск; после Stop/Dispose счётчики событий перестают расти. Это долг проверки уже написанных fixes, не новые подтверждённые дефекты.

**R1 / P2 / невоспроизведённый риск.** `OnDisable` только сохраняет состояние; `PlayStartAnimation` не хранит Coroutine/Tween handle. Проверить повторный Play/disable/destruction посреди анимации. При подтверждении старых scale-записей отменять собственный handle и восстанавливать scale, без KillAll. Невалидная конфигурация вне защищённых входов остаётся документированной предпосылкой, не доказанным багом.

## 3. Существенное улучшение — предложение, не выполнено

**Проблема.** Два независимых таймера совмещают арифметику с драйверами и событиями. `UpdateTimer` теряет остаток большого Tick при loop; несколько дней дают одно уведомление. Raw UTC persistence расходится с trusted reward clock; serialized running-флаг не восстанавливает async-задачу. Эти особенности нельзя молча менять для существующих игр.

**Дизайн.** Добавить plain C# ядро в существующую `Neo.Tools.Time`, без новой asmdef. Предлагаемый публичный контракт: `TimerState(TimerOptions options)`, `TimerStepResult Advance(double deltaSeconds)`, `Play/Pause/Resume/Stop`, `TimerSnapshot Capture()`, `Restore(TimerSnapshot snapshot)`.

`TimerOptions` задаёт finite/infinite/day режим, длительность и политику переноса остатка. `TimerStepResult` возвращает секунды, progress, число пересечённых циклов и revision; Advance не вызывает UnityEvents. `TimerSnapshot` содержит версию схемы, конфигурацию, elapsed/state и необязательный UTC anchor. Ядро проверяет числовые границы, использует double; float остаётся на существующих публичных границах адаптеров.

`Timer` сохраняет UniTask-драйвер и cancellation semantics; `TimerObject` проецирует elapsed в countUp/countDown, публикует завершённый переход, обслуживает UI и единственного владельца анимации. Подключать адаптеры последовательно с проверкой совместимости. Driver один; snapshot не запускает скрытую async-задачу после reload.

**Reuse.** Сохранить UniTask, `ReactivePropertyFloat`, `SaveProvider`, `INeoClock`, `SystemNeoClock`, `TrustedClock`/`NeoTrustedTime`, UTC helpers из `Neo.Extensions`. Готовых TimerState/Options/Snapshot/StepResult в текущих Scripts поиском не найдено. Не создавать альтернативный save service, global clock или NoCode-движок. Предложить additive `TimerObject.SetPersistenceClock(INeoClock clock)` для детерминированного UTC; default остаётся device UTC. Выбор trusted policy согласуется отдельно с Bonus, без обязательной зависимости Time→Bonus.

## 4. Точные файлы и порядок

Обозначения: `R = Assets/Neoxider/Scripts/Tools/Time`, `E = Assets/Neoxider/Tests/Edit/Tools`, `D = Assets/Neoxider/Docs/Tools/Time`.

1. **Безопасные fixes, 0.5–1 день:** F1 в `R/TimerObject.cs`, F2 в `R/TrustedClock.cs`; регрессии в существующих собственных `E/Audit20261023NeoToolsTimeTimerObjectTests.cs` и `E/Audit20261023NeoToolsTimeClockAndScaleTests.cs`. Обновить `D/TimerObject.md` и `D/README.md` после проверки поведения.
2. **Verification gate:** координатор выполняет V1. Новый `E/Audit20261023NeoToolsTimeSchedulingTests.cs` использует UnityTest/EnterPlayMode в существующей Edit assembly с UniTask reference; Play asmdef сейчас UniTask не содержит. Не менять shared asmdefs ради размещения этих тестов.
3. **Ядро и persistence, 2–3 дня:** новые `R/TimerOptions.cs`, `R/TimerState.cs`, `R/TimerStepResult.cs`, `R/TimerSnapshot.cs`, `R/TimerPersistence.cs`; новые `E/Audit20261023NeoToolsTimeStateTests.cs` и `E/Audit20261023NeoToolsTimePersistenceTests.cs`. Проверять состояние и счётчики, без reflection/source-text assertions.
4. **Адаптеры, 2–3 дня:** `R/Timer.cs`, затем `R/TimerObject.cs`; R1 при подтверждении. Использовать existing typed probe; добавить `E/Audit20261023NeoToolsTimeAnimationTests.cs` только для реального lifecycle-дефекта. `R/GameTimeController.cs` дополнительного менеджера не требует.
5. **Документация и выпуск, 1 день:** `D/README.md`, `D/Timer.md`, `D/TimerObject.md`: программный API, внешний clock, Inspector/UnityEvents, migration examples. Новым package-файлам нужны уникальные `.meta`; folder metadata/shared packaging остаются координатору.

Оценка предложения: 5–8 дней сверх небольших fixes, средний/высокий риск event order, catch-up rewards и сохранений.

## 5. Совместимость, зависимости и приёмка

Сохранить существующие сигнатуры, serialized fields, UnityEvents, Reactive outputs и ключи `_t/_a/_rt`. Legacy snapshots читать без удаления; новую запись версионировать только после успешного чтения. Default сохраняет существующие число уведомлений, raw UTC и offline rate=1; перенос остатка/многократные уведомления вводятся opt-in. Политики reward catch-up, trusted UTC, offline timeScale и reload-resume требуют решения координатора; безопасные fixes от них не зависят.

**Границы:** сначала контракт Time, затем owner `Neo.Bonus` проверяет `Assets/Neoxider/Scripts/Bonus/TimeReward/CooldownReward.cs`, `Assets/Neoxider/Tests/Play/Bonus/BonusPlayModeTests.cs` и собственные lifecycle regressions. Его claim path использует NeoTrustedTime, base persistence — device UTC; это интеграционный риск, не доказанный обход награды. Владельцы `Neo.Save`, `Neo.Reactive`, `Neo.Extensions` сохраняют provider/notification/UTC contracts; при изменении требований обращаться через координатора, не редактировать их файлы. Shared settings/asmdefs, Unity и package validation выполняет координатор.

Приёмка ядра: Advance(25) при duration=10, looping=true даёт два crossing и остаток 5 в opt-in режиме; разбиение на ticks сохраняет результат; paused snapshots не получают offline elapsed; callbacks не публикуют старый revision; legacy defaults сохраняют прежнюю частоту событий. Для UI проверить fill/text и scale при остановке, отключении и уничтожении, не только отсутствие exception.

Финальный gate: EditMode и выбранные PlayMode/Bonus сценарии, компиляция Unity 6.0/6.4 и 6.5+, комбинации включённых/выключенных DG_TWEENING и UNITY_TEXTMESHPRO. ID compatibility остаётся в общих StableId helpers, без локальных version-веток. Проверить UTF-8, GUID, diff, serialized compatibility и текущий sample layout: release `Samples~`, developer `Samples` либо импортированный `Assets/Samples/NeoxiderTools/<version>/...`; ничего не переименовывать без необходимости.

Остановка интеграции: regression, compile error, потеря сохранения, дублирующий reward или невыбранная default policy блокируют соответствующую часть; изолированные fixes продолжаются. Статическая проверка не закрывает Unity gate и не расширяет владение.

Проверка фазы 2: UTF-8, ссылки и whitespace корректны; SHA-256 всех 29 защищённых файлов runtime/tests/docs/аудита совпали. Процессы Unity/build/test не запускались.
