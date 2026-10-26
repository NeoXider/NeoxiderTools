# Neo.Tools.Inventory — индивидуальный план реализации

Фаза 2: план, без реализации. Основание: [завершённый аудит](Neo.Tools.Inventory.md), текущий manifest и повторная проверка релевантных участков исходников. Назначение: `gpt-6.1-sol`, reasoning `max`.

Пути далее: `S = Assets/Neoxider/Scripts/Tools/Inventory`, `T = Assets/Neoxider/Tests/Edit/Tools/Inventory`, `D = Assets/Neoxider/Docs/Tools/Inventory`. Все предлагаемые изменения будущие; в этой фазе записан только данный файл.

## Уже выполнено

Проверены все 39 C# файлов. Исправлены F01–F13: создание лишних предметов, переполнения, обход ограничений, переносы и события, восстановление layout, частичный/reentrant pickup, drop gating, UI и владение созданными объектами. Изменены 18 исходников и девять страниц; добавлены 60 NUnit cases в трёх fixtures и support-файле с префиксом `Audit20261023NeoToolsInventory`. Повторять эти исправления не требуется.

UTF-8, GUID, ссылки, объявления API/serialized fields, лексическая структура и scoped `git diff --check` проверены. Компиляция и исполнение тестов не выполнялись. Это исходная точка плана, а не подтверждение работоспособности Unity.

## Остаточные исправления и долг проверки

**V0, P0 — центральная верификация.** Владелец: координатор совместно с владельцами `Neo.Editor.Tests` и `Neo.Tests.Play`. После завершения параллельных source-аудитов выполнить новые fixtures, существующие `InventorySystemTests.cs` и `InventoryPlayModeTests.cs`. Проверить cleanup, JSON, события и глобальные SaveProvider/NeoDiagnostics. Текущий `Audit20261023NeoToolsInventoryFixture.cs` не содержит `NonParallelizable`; требование последовательной изоляции проверить для реально установленного NUnit/режима запуска, не объявлять это доказанным сбоем. Нативные lifecycle-сценарии при необходимости перенести в PlayMode, сохранив проверки состояния вместо ослабления assertions.

**R1, P1 — возврат предмета после ошибки drop меняет физический слот.** Подтверждено source trace: `S/Runtime/InventoryDropper.cs:288,414,423`. Поместить instance в слот 3 при пустом слоте 0; вызвать `DropSlot(3)` с исключением RestoreInventoryState. Cleanup уже исправлен, однако ReAddRecord использует обычное добавление и возвращает предмет в слот 0. Существующий restore-failure тест проверяет количество/identity, но не исходную позицию. Владелец: Inventory. Минимальная коррекция: сохранять физическое происхождение при извлечении, включая packed/by-id маршруты; сначала восстанавливать в исходный слот через существующий constrained insert. Файлы: `InventoryDropper.cs`, `InventoryComponent.Queries.cs`, при необходимости внутренний helper в `InventoryComponent.Grid.cs`; не расширять публичный unchecked SetSlot. При занятом обработчиком слоте не перетирать чужое содержимое: явно обработать конфликт и проверить количество возвращённого остатка. Приёмка без конкурирующей мутации: возвращено 0, исходные слот/Count/InstanceId/JSON сохранены, OnDropFailed один, world-клон отсутствует. Конфликтная ветка не теряет и не дублирует предметы.

**R2, P1 — исключение callback прерывает сохранение уже committed состояния.** `S/Runtime/InventoryComponent.Operations.cs:239` вызывает события раньше Save; `.Grid.cs:127` последовательно финализирует endpoints. Обработчик источника с исключением оставляет обе записи памяти изменёнными, но может предотвратить оба autosave. Это остаток F06, не повторный дефект сохранения количества. Владелец: Inventory. Файлы: `.Operations.cs`, `.Grid.cs`, `.Persistence.cs`. Разделить определение изменения, уведомление и обязательную попытку persistence; использовать finally для каждого endpoint, сохранить первое исключение после завершения обоих финализаторов. Проверить также исключение OnSaved. Приёмка: при отказавшем пользовательском callback оба JSON соответствуют committed данным, суммарное количество сохранено; при отказе самого provider ошибка явно остаётся видимой. Продолжение всех подписчиков внутри одного UnityEvent и атомарность двух дисковых записей этим не обещаются. Обычный порядок событий сохранять.

**P2, ограничение привязки:** SelectedItemId меняется без inventory mutation, а `InventoryTotalCountText` слушает только OnInventoryChanged. Сейчас предусмотрен ручной Refresh. Опционально добавить отдельный `OnSelectedItemChanged(int)` в `InventoryComponent.cs` и подписку в `UI/InventoryTotalCountText.cs`; не имитировать item mutation/autosave. Приёмка: переключение между id с количествами 2/5 сразу меняет текст 2→5 без add/remove/save. Это улучшение контракта, не блокирующее исправление.

## P01, P2 — редактирование состояния instance и экипировки

Предложение, не реализовано. `InventoryHand.UseEquippedItem` может изменить ammo/durability визуального экземпляра; `RefreshHandVisual` уничтожает визуал и восстанавливает старый payload из storage. Чистого identity-based update API для обоих backends нет.

Повторно использовать IInventoryStorage, InventoryItemInstance.Clone, CaptureInstance/RestoreInstance, существующие constraints, FinalizeMutation(contentChanged: true), SaveProvider и SafeDestroy. Новую общую inventory/save/event framework не вводить.

Предлагаемый чистый C# capability:

```csharp
bool TryGetInstance(string instanceId, out InventoryItemInstance snapshot);
bool TryReplaceInstanceState(string instanceId, int expectedRevision,
    IReadOnlyList<InventoryItemComponentState> states,
    out InventoryInstanceUpdateResult result);
```

Добавить `S/Core/IInventoryInstanceEditor.cs`, `InventoryInstanceUpdateResult.cs` и `IInventoryStateSource.cs`. Result содержит Applied/NoChange/NotFound/RevisionMismatch/AmbiguousIdentity/InvalidState и новую revision; отдельный общий ChangeSet пока избыточен. `AggregatedInventory.cs` и `SlotGridInventory.cs` реализуют capability, атомарно клонируют payload, сохраняют id/count/StateVersion и отклоняют неоднозначную identity/дублирующиеся ключи. Изменить `InventoryItemInstance.cs`: отдельная Revision, default 0, с копированием; StateVersion остаётся пользовательской версией схемы.

Адаптеры: `InventoryComponent.cs`, `.Operations.cs`, `.Queries.cs`, `.Persistence.cs`, `InventoryHand.cs`, при необходимости общий capture-path в `InventoryItemStateUtility.cs`. Фасад публикует OnInstanceChanged(instanceId), один OnInventoryChanged и autosave без count delta. Hand хранит identity/revision именно показанного визуала; CaptureEquippedState не использует уже переключённый SlotIndex. Перед заменой визуала, drop и save коммитит payload через явно зарегистрированный IInventoryStateSource. Регистрация/отписка — через assigned inventory и lifecycle, без scene search. Для Save предусмотреть guarded commit-фазу, исключающую рекурсивный autosave/пересоздание руки; failed capture не должен молча записывать устаревшее состояние. Сохранить void Save, при необходимости добавить TrySave с typed результатом.

Совместимость: auto-capture opt-in, новые serialized поля только аддитивные; старые имена/enum/identity/Count не менять. Старые v1/legacy JSON читаются с Revision 0; миграцию проверить до изменения writer. Не перенумеровывать повторяющиеся InstanceId из старых saves автоматически; ambiguous records отклонять. SO остаются данными без scene references. Риск средний/высокий: reentrancy, удалённый во время capture instance, split Count>1, payload keys; ориентир 3–5 дней плюс центральная QA.

Минимум новых regressions: `T/Audit20261023NeoToolsInventoryRecoveryTests.cs` для R1/R2, `T/Audit20261023NeoToolsInventoryInstanceEditingTests.cs` для обоих backends; `Assets/Neoxider/Tests/Play/RPG/Audit20261023NeoToolsInventoryHandStatePlayTests.cs` для адаптера. Наблюдать независимые ammo двух одинаковых типов; use двух патронов → switch/move/drop/pickup/save/reload сохраняет остаток и identity; stale revision/ambiguous id не меняют payload; удалённый instance не воскресает; payload-only commit даёт одно изменение/сохранение и ноль count deltas. Новые файлы получают уникальные GUID metadata.

## Границы, порядок и критерии завершения

R1/R2/P01 принадлежат Inventory. `Neo.Save` владеет `SaveProvider.cs`/`Providers/ISaveProvider.cs`: настоящая транзакция нескольких ключей — отдельная задача его владельца, сначала контракт Save, затем inventory adapter. `Neo.Extensions` владеет SafeDestroy/NeoDiagnostics; `Neo.Tools.View` — Selector. Их готовые API использовать без копирования. `Neo.Shop.Bridges` остаётся единственным opt-in мостом; Inventory не получает ссылку на Shop. Shared asmdefs меняет координатор; Network/Mirror cleanup не требуется без отдельного анализа dependency graph.

Порядок: V0 → focused failing regressions R1/R2 → небольшие исправления и повтор affected tests → чистый P01 capability → guarded component/hand adapters → документация. Обновлять `D/InventoryDropper.md` для R1, `InventoryComponent.md` для R2; для P01 — `InternalTypes.md`, `InventoryItemState.md`, `InventoryHand.md`, `InventoryComponent.md`, канонический README с C# примером и Inspector/NoCode wiring. Возврат результатов в Inspector — через void adapter; важная логика остаётся typed API.

Координатор проверяет компиляцию Unity 6.0–6.4 и 6.5+ отдельно, EditMode и PlayMode, реальные 3D/2D pickup/drop, disable/destroy, reload и domain-reload-disabled. Новые identity-вызовы используют общий NeoDiagnostics.StableId, без локальных version gates. Затем scoped diff/UTF-8/GUID/link/public-API/package checks. Samples не нужны; не переименовывать Samples~/Samples и не менять assets. Если позже нужен пример, учитывать также imported root `Assets/Samples/NeoxiderTools/<version>/<sample name>`.

Завершение — только после наблюдаемых acceptance и приложенных receipts; непроверенная Unity-ветка остаётся долгом. До публичного P01 релиза координатор фиксирует opt-in и migration/identity policy. Это не причина останавливать обратимые R1/R2. Не расширять контракт до автоматического ремонта legacy identities или транзакционного Save без отдельного согласованного дизайна. В этой фазе процессы Unity/build/test не запускались; исходники, тесты, публичные документы, аудит и конфигурация не изменялись.
