# Neo.Tools.View — индивидуальный план реализации

План той же сессии аудитора: `gpt-6.1-sol`, reasoning `max`. Предложения не реализованы.
Прочитаны PLAN-CONTRACT, аудит `Neo.Tools.View.md`, manifest и текущий source.

Каталоги:

- `V` = `Assets/Neoxider/Scripts/Tools/View/`.
- `E` = `Assets/Neoxider/Tests/Edit/Tools/`.
- `P` = `Assets/Neoxider/Tests/Play/Tools/`.
- `D` = `Assets/Neoxider/Docs/Tools/View/`.

## 1. Уже выполнено

Рассмотрены десять C# файлов. Исправлены загрузка сохранения до первой записи,
singleton unique-цикл, прямое присваивание собственных коллекций модели, offset/removal/empty selection,
маршрутизация bulk exclusions клиента, Light2D delegates, сброс readbacks освещения, StarView subscriptions/nulls,
владение emission material и billboard без камеры. Изменены шесть source-файлов и девять публичных страниц.
Добавлены `E/Audit20261023NeoToolsViewSelectorTests.cs` и
`P/Audit20261023NeoToolsViewLifecycleTests.cs`: 21 сценарий с уникальными `.meta`.

Статически проверены inventory, публичные объявления/serialized fields относительно `ca198556`, UTF-8,
пробелы, ссылки, GUID и `git diff --check`. Unity, компиляция, тесты, Mirror, URP/HDRP и IL2CPP
не запускались. Исправленное не включено в backlog реализации.

## 2. Остаточные подтверждённые дефекты

Владелец — `Neo.Tools.View`; подтверждение по control flow, без исполнения.

1. **P1: deferred enumerable теряется при замене.** `V/SelectorModel.cs:331–366` защищает только
   ReferenceEquals. Вызов `model.SetExcludedIndices(model.ExcludedIndices.Where(i => i != 2))`
   очищает источник раньше enumeration; аналогично used history. Минимум: материализовать вход до Clear,
   сохранить прямой alias fast path и null-as-clear. Добавить
   `E/Audit20261023NeoToolsViewSelectorInputTests.cs`: после фильтра остаётся ожидаемый набор,
   следующий random соблюдает exclusions/history. Если enumeration бросает, прежний набор остаётся целым.
   Публичные сигнатуры сохраняются; учитывать стоимость копирования, пока Configure вызывается из getters.

2. **P1: события клиента опережают полный snapshot.** `V/Selector.cs:288–298,394–400` вызывает
   ApplyUpdateSelection с событиями до ApplyActiveIndicesSnapshot. При additive snapshot `{0,2}` и
   выключенных клиентских items callback может увидеть CountActive=1 вместо итоговых 2.
   Общий путь network snapshot: сначала индекс/режим/полный active set,
   затем события Selector и итоговый active count. Подавлять промежуточные уведомления/сохранение,
   не менять локальную последовательность событий. В новом
   `P/Audit20261023NeoToolsViewNetworkTests.cs` callback должен читать `{0,2}` и CountActive=2,
   включая late join. Атомарность всех Unity OnEnable/SelectorItem callbacks этим не обещается.

3. **P1: общий rate budget между клиентами.** Все Commands в `V/Selector.cs` используют
   RateLimitCheck() без sender. `Assets/Neoxider/Scripts/Network/Core/NeoNetworkComponent.cs:47–82` хранит общий
   timestamp, уже предоставляет RateLimitCheck(sender). Два разрешённых клиента в пределах 50 ms
   блокируют друг друга. После стабилизации API владельцем `Neo.Network` использовать sender-overload
   во всех Selector Commands, сохранив authority policy. Проверить отдельные бюджеты двух клиентов,
   отказ повторному запросу одного клиента и отсутствие изменений от unauthorized sender.

4. **P2: переполнение допустимых int-входов.** `V/SelectorModel.cs:207–218` вычисляет index-min в int:
   Count=3, offset=1, Loop=true, Set(int.MaxValue) даёт 0 вместо математически корректного 1.
   Inclusive loops модели и `V/Selector.cs:1112` также переполняют счётчик при max=int.MaxValue,
   достижимом уже с Count=1, offset=-int.MaxValue. Минимум: long для промежуточной арифметики/шага,
   ограниченная числом элементов итерация. Дополнить InputTests: правильный wrap и завершающийся
   singleton query. Для логических bounds вне int заранее определить явный отказ, не молчаливое wrapping;
   обычные конфигурации сохраняются.

## 3. Долг проверки и границы владельцев

**P0 перед интеграционным закрытием:** координатор после завершения source-аудитов запускает 10 Edit
и 11 Play случаев выше, существующие SelectorTests, AuditFixesSelectorTests,
ImageFillAmountAnimatorTests и BillboardUniversalEditModeTests. Ожидаются сохранённые ключи до Start,
правильные реальные active objects, единственная/снятая подписка, согласованные light readbacks,
возвращённый source material и уничтоженный clone после конца кадра.

**P1 network:** host, remote client и late join согласуют bulk/null/empty exclusions и additive state;
отказы authority/rate limit сохраняют состояние сервера. Повторно использовать
`Assets/Neoxider/Tests/Play/Network/NetworkTestHelper.cs` для bounded waits/setup;
DummyTransport сам по себе не доказывает доставку remote RPC.

Внешние зависимости относительно `Assets/Neoxider/`, без изменений чужого кода:

- `Neo.Network`: `Scripts/Network/Core/NeoNetworkComponent.cs`, `NeoNetworkState.cs` — lifecycle,
  sender rate limit, IsAuthorized; сначала стабилизация этих API, затем Selector/network проверки.
- `Neo.Network.Contracts`: `Scripts/NetworkContracts/NetworkAuthorityMode.cs`,
  `INeoOptionalNetworked.cs` — сохранить значения/контракты, новых режимов не требуется.
- `Neo.Save`: `Scripts/Save/SaveProvider.cs`, `Providers/ISaveProvider.cs` — использовать backend,
  проверить смену provider и startup; не создавать параллельную save-систему.
- `Neo.Reactive`, `Neo.Tools.Components`, `Neo.UI`, `Neo.Animations`: существующие listener APIs,
  ScoreManager, VisualToggle, AnimationUtils/ILightAccessor; повторить lifecycle после интеграции их аудитов.

**Неподтверждённые риски:** reflection stripping Light2D под IL2CPP и фактическое HDRP rendering.
Уже написанные optional tests должны пройти в соответствующих проектах; skip не равен pass backend.
Ранние Set до Start не образуют очередь, а LoadState может восстановить сохранённый индекс поверх них:
зафиксировать политику saved-wins или явную инициализацию. Saved-empty против отсутствующего ключа —
решение формата/продукта, а не автоматически объявленная ошибка.

## 4. Существенное предложение: модель как единственный источник правил

**Проблема:** `V/Selector.cs:1612–1668` повторяет Configure/copy/sort через getters;
scene activation, persistence и network одновременно управляют состоянием, усложняя переиспользование.

**Reuse и публичный дизайн:** расширить существующий SelectorModel, сохранить SelectorModelResult,
Set/Next/Previous/SetRandom, injected random и CreateModelSnapshot. Добавить:

```csharp
public SelectorModelResult Apply(in SelectorCommand command);
public SelectorSnapshot CaptureSnapshot();
public void RestoreSnapshot(SelectorSnapshot snapshot);
public long Revision { get; }
```

SelectorCommand — typed операции, SelectorSnapshot — копируемые primitive/index collections без scene refs.
Revision меняется при мутации; getters не вызывают
Configure и не пересобирают sets. Модель определяет логические правила; реальный CountActive компонента
по-прежнему учитывает внешнюю активацию GameObjects/SelectorItems, не подменяется формулой модели.

**Будущие файлы:** изменить `V/SelectorModel.cs`, `V/Selector.cs`; добавить
`V/SelectorCommand.cs`, `V/SelectorSnapshot.cs`, внутренние обычные C# адаптеры
`V/SelectorPersistenceAdapter.cs`, `V/SelectorNetworkAdapter.cs`. Mirror Commands/SyncVars остаются
в существующем компоненте; новые MonoBehaviour, singleton, Editor или NoCode bridges не нужны.
Новые файлы получают уникальные GUID; изменение inventory оформляет координатор.

Сначала snapshot/Revision и mutation tests, затем typed Apply, затем перенос persistence/network glue.
Использовать ISaveProvider и Neo network contracts; test asmdefs уже имеют необходимые ссылки.
Новый `E/Audit20261023NeoToolsViewSelectorSnapshotTests.cs` проверяет независимость snapshot,
restore exclusions/history, неизменность Revision от чтения и соответствие фасадов.
Измерить повторные чтения: без Configure/пересборки Inspector lists; оптимизацию не объявлять до измерения.

**Совместимость:** сохранить serialized names/defaults, UnityEvents, namespace, публичные методы,
legacy `_Index/_Excluded` и существующие RPC поля. Versioned persistence вводить только opt-in
с чтением legacy, явно различая saved-empty и missing. Inspector snapshots обновлять лениво,
authoring fields синхронизировать на initialization/реальном изменении. Обновить `D/README.md`,
`Selector.md`, `SelectorModel.md`, `SelectorItem.md`: C# примеры и прежний scene workflow.
Сцены/prefabs не переписывать; release `Samples~` сохраняется. Возможные будущие примеры должны
поддерживать developer `Samples` и импортированный `Assets/Samples/NeoxiderTools/<version>/...`.

Оценка при готовом harness: 1–2 дня fixes, 3–5 дней архитектуры плюс интеграция.
Риск средний/высокий: callback order, external activation, save/provider, wire semantics.

## 5. Порядок, приёмка и остановка

1. Стабилизировать зависимости; внести минимальные остаточные fixes с поведением из пункта 2.
2. Выполнить исходные и новые регрессии; только затем snapshot/API и внутренние adapters.
3. Проверить pure C# empty/offset/fill/unique/excluded и scene/virtual equivalence, затем network/persistence.
4. Обновить публичные guides; координатор выполняет package/reference/sample validation.
5. Скомпилировать Unity 6.0–6.4 и 6.5+, MIRROR on/off; отдельно URP/HDRP и IL2CPP smoke.
   Баланс #if не доказывает компиляцию. Version switching остаётся централизованным:
   NeoDiagnostics.StableId / NeoComponentHealth.StableId; новых локальных version gates не добавлять.

Закрывать только наблюдаемые результаты: final state внутри callback, независимые client budgets,
точное восстановление snapshot и сохранённые assets/API. При провале остановить зависимую интеграцию,
сохранить минимальный repro/владельца; обычные обратимые fixes не требуют нового согласования.
Без доступного backend зафиксировать его как unverified, архитектурное предложение не считать реализованным.

В этой фазе создан только данный план; source, tests, public docs, audit и configuration не изменялись.
