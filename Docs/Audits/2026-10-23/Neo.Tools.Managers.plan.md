# Neo.Tools.Managers — индивидуальный план реализации

Фаза 2 той же auditor/session. Основание: [завершённый аудит](./Neo.Tools.Managers.md), полный `PLAN-CONTRACT.md`, собственная запись manifest и актуальные исходники, изменения документации и три регрессионных файла. На этом этапе записан только данный план; предложения ниже не реализованы.

## 1. Уже выполнено

Проверены все шесть C# файлов сборки. В `Bootstrap.cs` исправлены повторный вход, потеря регистраций из `Init()`, выполнение отменённых и уничтоженных целей. В `GM.cs` исправлены владение паузой времени, порядок применения времени перед уведомлениями и потеря предыдущего состояния. В `Singleton.cs` исправлены persistence до `Awake` и повторное разрешение объекта после `DestroyInstance()`. В GM/EM ограничены 20 Inspector-кнопок режимом Play Mode. `SingletonById.cs` и `SingletonRuntimeReset.cs` проверены без изменения реализации.

Обновлены шесть канонических страниц Managers; добавлены 6 Bootstrap, 13 GM и 3 singleton-сценария. Пройдены статические проверки UTF-8, деклараций API/полей, ссылок, GUID, inventory и scoped `git diff --check`. **Unity-компиляция и все эти тесты не выполнялись.** Семь исправленных findings не являются будущими задачами. Дополнительных подтверждённых неисправленных дефектов сейчас нет.

## 2. Приоритеты оставшейся проверки

| Приоритет / статус | Доказательство и воспроизведение | Владелец, минимальное действие, наблюдаемый результат |
| --- | --- | --- |
| **P0 — долг проверки исправлений** | 22 authored cases; существующие `BootstrapTests` и singleton sweep в `AuditFixesStaticsTests`. | Координатор после завершения source-аудитов запускает focused EditMode/PlayMode. При сбое Managers-worker исправляет конкретную причину в своей сборке. Ожидания: `high, low, late`; родитель/ребёнок по одному вызову; отменённая цель не вызывается; точное восстановление времени; новый singleton остаётся после уничтожения старого. |
| **P1 — реальный Unity lifecycle не проверен** | `GM.cs:220` восстанавливает время, но EditMode-тест вызывает protected handler через seam. Persistence проверяется принадлежностью DDOL-сцене без настоящего unload. | Managers-worker добавляет PlayMode-проверки; координатор выполняет. Реальный `Destroy` и unload сцены после паузы при исходных `0.5` должны вернуть `0.5`; persistent singleton переживает unload и имеет один `Init`. Временные сцены создаются тестом, tracked scenes не меняются; восстанавливаются time scale/FPS. |
| **P1 — reload-disabled риск** | `SingletonById.cs:18–20` очищает registry, регистрация только в `Awake:34–49`. Если сохранённый объект не получает нового `Awake`, `Get(id)` остаётся пустым. Сам Unity-сценарий не подтверждён запуском. | Координатор проверяет четыре сочетания domain/scene reload, два входа подряд. При воспроизведении Managers-worker исправляет `SingletonById.cs` / `SingletonRuntimeReset.cs`: отдельный runtime-этап восстановления ранее зарегистрированных живых объектов после очистки. Не вводить поиск в `Get`, не регистрировать впервые неактивированные объекты, сохранить new-wins и очистку уничтоженных записей. |
| **P2 — IL2CPP риск** | `SingletonRuntimeReset.cs:18–65` получает типы и вызывает методы по имени через reflection. Данных о stripping failure нет. | Координатор выполняет минимальный player smoke с прямым и двухуровневым наследником. Если linker действительно удаляет reset, Managers-worker сначала применяет минимальное сохранение reset helpers в `Singleton.cs` / `SingletonById.cs`; linker-конфигурация только у координатора. Наблюдение: сбрасываются cache/miss/Id, следующий живой экземпляр разрешается. |

Несколько владельцев `Time.timeScale` и произвольные вложенные переходы GM остаются ограничением существующего контракта, не новым подтверждённым багом. Общий pause arbiter и изменение порядка UnityEvent требуют отдельного продуктового контракта; они не входят в извлечение очереди.

## 3. P2 — существенное улучшение: наблюдаемая C# очередь инициализации

**Проблема подтверждена кодом, но это предложение расширения API.** При исключении начального `Init` строка `Bootstrap.cs:65` не достигается; публичного восстановления нет. Неограниченный поток новых регистраций удерживает синхронный drain. Для проверки самой очереди сейчас нужен MonoBehaviour.

**Повторное использование:** сохранить `IInit`, алгоритм batch/order/cancellation, текущие коллекции и equality semantics; Unity-lifetime адаптер использует существующий `Neo.Extensions.ObjectExtensions.IsValid`. Bootstrap остаётся сборщиком scene targets; новое ядро использует только C# и переданный `Predicate<IInit>`, без Unity-поиска, singleton или UnityEvent.

Предлагаемый API, namespace `Neo.Tools`, та же `Neo.Tools.Managers`:

```csharp
public sealed class InitializationQueue
{
    public InitializationQueue(Predicate<IInit> isAlive = null);
    public void Register(IInit target);
    public bool Unregister(IInit target);
    public InitializationReport RunPending(InitializationRunOptions options);
    public bool RetryFailed(IInit target);
    public IReadOnlyList<InitializationEntry> Entries { get; }
}
```

`InitializationEntry` — неизменяемый снимок target/priority/status/error; статусы Pending, Running, Succeeded, Failed. `InitializationReport` содержит completed/remaining/failed и ошибки. Опции задают положительный `MaxInvocations` (unlimited через `int.MaxValue`), Throw/StopAndReport и `RetryFailedEntries`. Failure записывается до выхода по исключению. Повторный drain из callback возвращает текущий снимок без вложенного исполнения. Unregister освобождает target/error и отменяет ещё не начатый вызов.

Важное уточнение совместимости исходного предложения: **старый** `Bootstrap.Register` после завершённого startup сохраняет unlimited/fail-fast поведение и повторную попытку ранее упавших регистраций при следующем новом Register, как текущий код. **Новый** явный run/retry workflow по умолчанию не повторяет Failed без запроса. Не менять это поведение молча при введении статусов. `Bootstrap` получает additive `RunPending(InitializationRunOptions)`, `RetryFailed(IInit)` и snapshot property; успешный recovery завершает startup без повторного сбора сцены.

Бюджет сохраняет текущий batch и позицию продолжения: если high добавил late с более высоким priority, low из начатого batch всё равно выполняется раньше late, включая следующий RunPending. Успешные targets не запускаются повторно. Реализация синхронная; async, dependency graph и service locator не нужны.

## 4. Точные файлы и границы

- В `Assets/Neoxider/Scripts/Tools/Managers/`: изменить `Bootstrap.cs`; добавить `InitializationQueue.cs`, `InitializationEntry.cs`, `InitializationReport.cs`, `InitializationRunOptions.cs` и уникальные `.meta`. Связанные enum находятся в соответствующих файлах. `IInit` остаётся на месте; asmdef не меняется.
- В `Assets/Neoxider/Tests/Edit/Core/`: новый `Audit20261023NeoToolsManagersInitializationQueueTests.cs` для plain-C# сценариев; существующий `Audit20261023NeoToolsManagersBootstrapTests.cs` расширить проверкой публичного startup recovery.
- В `Assets/Neoxider/Tests/Play/`: новый `Audit20261023NeoToolsManagersLifecycleTests.cs` для настоящего destruction/unload и воспроизведённых reset-сценариев. Каждому новому asset — уникальный `.meta`.
- В `Assets/Neoxider/Docs/Tools/Managers/`: расширить существующие `Bootstrap.md` и `README.md` примерами C# run/report/retry и manual/Inspector workflow; при подтверждённом reset-fix обновить `Singleton.md` / `SingletonById.md`. Отдельного NoCode bridge или обязательного editor-window не добавлять. Нужен ли scene report-event, решать по конкретному потребителю.

Зависимости: владелец **Neo.Extensions** поддерживает `ObjectExtensions.IsValid` и `NeoDiagnostics.StableId`; владелец **Neo.PropertyAttribute** — `ButtonAttribute.cs` / `ButtonAttributeDrawer.cs`; владелец **Neo.Editor** — `NeoCustomEditor.cs` и editor compatibility gates. Сначала координатор интегрирует их стабильные контракты, затем проверяет Managers/Inspector вместе. Изменения чужих файлов не требуются. Test asmdefs уже содержат нужные ссылки; новые references/settings — только координатору при фактической необходимости.

## 5. Порядок, приёмка и условия остановки

1. Закрыть P0, воспроизвести P1; подтверждённые lifecycle fixes выполнять раньше расширения API с регрессией на реальный результат.
2. Извлечь покрытое поведение очереди без нового режима; затем добавить snapshot/failure/retry/budget. Подключить Bootstrap и публичный recovery, после этого обновить документацию.
3. Минимальные новые тесты: parent→child один раз; descending/stable batches; unregister/destroy до хода; failure→retry без повторения успешных; продолжение с budget=1 сохраняет `high, low, late`; бесконечно порождающий callback возвращает управление по бюджету; startup recovery доступен через публичный API.
4. Сохранить serialized names/defaults, enum values, aliases, UnityEvents, `CreateInstance`, synchronous legacy behavior. Не менять scenes/prefabs и release layout `Samples~`; документация и будущие sample-checks учитывают также dev `Samples` и импортированный `Assets/Samples/NeoxiderTools/<version>/<sample name>`.
5. Координатор компилирует на Unity 6.0–6.4 и отдельно 6.5+ с receipts обоих окружений: один компилятор не проверяет другую ветку. Новых distributed `#if`/instance-ID calls не добавлять; использовать централизованные runtime/editor gates. Затем focused tests, Inspector smoke, reload matrix, необходимый IL2CPP smoke и package/UTF-8/meta/diff validation.

Оценка предложения: 2–4 developer days плюс центральная Unity-проверка; риск средний из-за failure compatibility, продолжения batch и удержания targets. Остановить расширение при потере старой трассы/сериализации или необходимости чужих незавершённых API, зафиксировать конкретный конфликт координатору. Неподтверждённые reset/stripping риски не превращать в профилактический rewrite. Без Unity receipts результат остаётся «source/static complete, runtime unverified»; бюджетный режим по умолчанию для старых сцен не включать.
