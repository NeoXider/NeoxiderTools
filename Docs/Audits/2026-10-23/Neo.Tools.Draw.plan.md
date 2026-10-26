# Neo.Tools.Draw — индивидуальный план реализации

Основание: [завершённый аудит](./Neo.Tools.Draw.md), полный `PLAN-CONTRACT.md`, актуальная запись manifest и текущие исходники, документация, регрессии и граница Spawner. Продолжение того же аудитора; планирование не меняет реализацию. Приоритеты: P1 — проверка перед интеграцией, P2 — коррекция установленного поведения, P3 — самостоятельное расширение.

## 1. Что уже выполнено

Полностью рассмотрен `Assets/Neoxider/Scripts/Tools/Draw/Drawer.cs`, единственный собственный C# файл. Уже исправлены 11 групп ошибок материалов, pooling/таймеров, коллайдеров, координат, callbacks, очистки, сегментов, smoothing и камеры. Эти исправления повторно не планируются. `InputCamera` и `TryScreenToWorld` уже существуют.

Обновлены `Assets/Neoxider/Docs/Tools/Draw/README.md` и `Assets/Neoxider/Docs/Tools/Draw/Drawer.md`. В `Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsDrawTests.cs` добавлены 23 сценария: 20 синхронных, три с Play Mode. Прошли статические проверки UTF-8, whitespace, ссылок, GUID, структуры, inventory и публичных деклараций. Компиляция и Unity-тесты этим аудитором не выполнялись.

## 2. Оставшиеся коррекции и проверка

| Приоритет / статус / владелец | Основание и воспроизведение | Минимальное действие и наблюдаемый результат |
|---|---|---|
| P1, долг проверки; координатор | Lifecycle-регрессия уничтожает компонент, сохраняя GameObject; новые тесты не запускались. | После завершения source-аудитов скомпилировать сборки, выполнить `AuditFixesDrawerTests` и 23 новых сценария. Добавить полное уничтожение/выгрузку сцены: pooled-линии возвращаются живыми, собственные материалы освобождаются, чужие сохраняются. |
| P2, новый статический вывод при планировании; Draw | `Drawer.cs:286`: Reset уведомляет о нулевом Distance раньше очистки. Нарисовать сегмент; одноразовый Distance-listener на нуле вызывает `BeginLine(Vector3.up)`; вызвать Reset. Внешний Reset продолжает очистку нового штриха. Unity-repro не запускался. | Сначала согласованно очистить геометрию/метрики через `SetValueWithoutNotify`, затем уведомлять изменившиеся значения. После callback проверять версию; защитить продолжение внешнего Begin после Reset/стартовых уведомлений. Регрессия: новый renderer содержит одну точку, PointCount = 1, Distance = 0. |
| P2, подтверждённое удержание; Spawner → Draw | `GetPoolPrefab` создаёт источник; OnDestroy его уничтожает. `PoolManager._pools` / `GetOrCreatePool` не удаляют регистрацию; `NeoObjectPool` сохраняет источник/inactive instances. Повторить 100 циклов создания/рисования/уничтожения Drawer. | Spawner предоставляет освобождение регистрации; Draw использует его после возврата линий. Регистрации, объекты, материалы возвращаются к исходному количеству. Общие prefab-пулы и storage root сохраняются. |
| P2, известная граница неподдерживаемого interop; Spawner + Draw | Внешний `PoolManager.Release` и выдача другому Drawer меняют `SpawnGeneration`, но не передают `_lineMaterials`. Таймер защищён; Delete/OnDestroy первого владельца ещё может затронуть новую выдачу. | Проверять поколение при любом обращении к арендованной линии; материал освобождать единым lease callback. После внешней перевыдачи уничтожение первого Drawer не меняет активность/материал/геометрию второго. Это расширение interop-контракта, не выполненная Unity-проверка. |

Непроверенные риски: live-follow collider, ортографическая камера/параллельный луч, player fallback shader, деактивация coroutine owner. Исправлять после воспроизведения. Legacy Input, незамкнутый EdgeCollider при `loop`, единый радиус и mouse-held drawRelease — документированные ограничения; их изменение является продуктовым расширением.

## 3. Существенное улучшение: ограниченный C# конструктор штриха

**Проблема.** `maxPoints` применяется после `Smooth` (`Drawer.cs:514`); проход удваивает массив (`:843`). 1000 raw points / 10 проходов дают 1 024 000 промежуточных точек. Это алгоритмический вывод, не замер. Ввод, геометрия, ресурсы и события объединены в MonoBehaviour.

**Предложение, не реализовано.** Обычный C# `StrokeBuilder(StrokeOptions options)` без сценовых зависимостей: `bool TryAppend(Vector3 point)`, `Clear()`, `RawCount`, `Distance`, `CopyProcessedPoints(List<Vector3> destination)`, `StrokeSnapshot Complete()`. Snapshot владеет точками/метриками и не меняется после Clear.

Опции: nullable `FixedZ`, spacing/fixed length, passes, `RawPointLimit`, `ProcessedPointLimit`, `WorkingPointLimit`; ноль отключает ограничения. Первая политика RejectNewest не меняет состояние при отказе. Промежуточное сокращение до аллокации сохраняет endpoints и детерминизм. DropOldest/Simplify отложены до определения Distance. Неограниченный режим сохраняет геометрию.

**Точные файлы будущей реализации, сборка `Neo.Tools.Draw`:**

- Новые `Assets/Neoxider/Scripts/Tools/Draw/StrokeOptions.cs`, `StrokeBuilder.cs`, `StrokeSnapshot.cs`, `StrokeGeometry.cs` — последние три имени находятся в том же каталоге Draw.
- Существующий `Assets/Neoxider/Scripts/Tools/Draw/Drawer.cs` — совместимый facade. Чистые алгоритмы переносятся в `StrokeGeometry`; существующие статические методы делегируют ему с прежними сигнатурами.
- Новые `Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsDrawStrokeBuilderTests.cs` и `Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsDrawLifecycleTests.cs`; собственный текущий регрессионный файл расширяется для Reset. Новые package-файлы получают уникальные `.meta`.

Переиспользовать Smooth/LimitPoints/GetDistance, `SafeDestroy`, `GetOrAdd`, coroutine handles, reactive API и `SpawnGeneration`; сверить актуальные helpers перед извлечением. Новый пул/runner/NoCode-ядро не нужны.

Сохранить поля/defaults, `FormerlySerializedAs("colour")`, `useFolow`, UnityEvents и identity `rawPoints`. Внутренний adapter связывает facade с legacy-списком; новый builder отдаёт read-only представление. Прямая мутация legacy-списка обходит инварианты. Inspector-лимиты выключены; `maxPoints <= 1` неограничен. Сначала builder/дифференциальные тесты, затем facade. Новый MonoBehaviour/Editor/Input System adapter пока не нужен.

Приёмка: прежние endpoints/дистанции для unrestricted 0/1/2/N inputs; 100 000 подач соблюдают raw/working limits; RejectNewest не меняет заполненный builder; caller buffer переиспользуется; snapshot неизменен; reentrant completion сохраняет следующий штрих. Аллокации после прогрева измерять профайлером. Оценка: 3–5 дней плюс профилирование; риски — Chaikin output, mutable-list compatibility, события.

## 4. Зависимость от Spawner

Предложение владельцу `Neo.Tools.Spawner`: `RegisterPool(prefab)` → `PoolRegistration : IDisposable`, instance lease с поколением/release callback. Get/Release сохраняются. Последняя регистрация удаляет ключ/inactive объекты; outstanding экземпляры и источник завершаются после возврата. Учесть `IPoolable`/кеширование: поздно добавленный компонент не гарантирует callback.

Файлы Spawner: `Assets/Neoxider/Scripts/Tools/Spawner/PoolManager.cs`, `NeoObjectPool.cs`, `PooledObjectInfo.cs`, новые `PoolRegistration.cs` / `PoolLease.cs` в том же каталоге. Тесты/docs — его владельцу. Draw интегрируется после контракта; перед работой сверить результат параллельного Spawner-аудита. Оценка 2–3 дня совместно; Reset/builder независимы. Новые API — предложение.

## 5. Порядок, документация и условия завершения

1. Зафиксировать результат центральной проверки уже внесённых исправлений; новый failing regression для Reset, минимальная коррекция, повтор только затронутых проверок. Дополнить lifecycle-сценарии полным уничтожением/выгрузкой и наблюдением фактической физики. Использовать Edit assembly, уже ссылающуюся на Draw; три существующих UnityTest сами входят в Play Mode. Перенос в Play asmdef потребует отдельного изменения координатора.
2. Закрыть pool registration/provenance с владельцем Spawner и проверить 100 циклов, двух владельцев, eviction, внешний возврат и уничтожение первого владельца. Не редактировать чужую сборку из Draw-задачи.
3. Реализовать независимый builder, затем совместимый facade; optional adapters добавлять только по выбранному workflow. Не вводить новые asmdef/обязательные зависимости без необходимости.
4. Обновить те же Draw `README.md` и `Drawer.md`: C# builder/snapshot и старые методы, opt-in Inspector limits, события, владение ресурсами. Добавить краткое русское пояснение; не создавать новый конкурирующий entry point или обязательные UnityEvent chains.
5. Координатор компилирует runtime и Editor/tests на Unity 6.0–6.4 и 6.5+; если появится ID API, использовать только `NeoDiagnostics.StableId` / `NeoComponentHealth.StableId`, без локальных версионных веток. Проверить player fallback/назначенный материал и package import. Sample-пути учитывать в release `Samples~`, dev `Samples` и imported `Assets/Samples/NeoxiderTools/<version>/<sample>`; миграция layout не требуется.

Завершение требует записанных результатов компиляции/тестов и приёмки выше. Недоступные версии/замеры оставить непроверенными. При несовместимости legacy output, assets, списка или событий остановить миграцию и устранить причину. Input backend, физическая петля, переменная толщина и другие overflow policies остаются продуктовыми решениями. В фазе 2 создан только план; реализация/Unity-проверки не выполнялись.
