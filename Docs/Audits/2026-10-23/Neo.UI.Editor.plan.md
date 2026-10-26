# Neo.UI.Editor — индивидуальный план реализации

Статус: только план, реализация предложений не начата. Продолжение исходного аудитора, `gpt-6.1-sol`, reasoning `max`; база аудита `ca198556`, пакет `10.19.0`. Прочитаны полный PLAN-CONTRACT, собственный аудит, актуальная manifest entry, текущие изменения редактора, документация, регрессионные assertions и граница `Neo.UI`.

## 1. Уже выполнено

Все четыре C# файла inventory проверены в фазе 1. В `Assets/Neoxider/Scripts/UI/Editor/UIMeshRigGraphicEditor.cs` внесены защиты удаления вложенного рига и операций над точкой на корне владельца, сохранение родителя/позы при Duplicate, уникальные owner-scoped ключи новых точек и ветвей, повторная запись Undo перед сменой режима layout. В `Assets/Neoxider/Scripts/UI/Editor/UIMeshRigMenu.cs` retarget Selectable записывается как prefab override. Это исходные исправления F1–F5, не повторный backlog.

Добавлены `Assets/Neoxider/Tests/Edit/UI/Audit20261023NeoUIEditorAuthoringTests.cs` (9 методов, 21 случай) и `Assets/Neoxider/Docs/UI/Editor.md`, оба с metadata. Гайд объясняет C# API, Inspector, preview и конвертацию Image. `AssemblyInfo.cs` и `UIMeshRigAdaptersEditor.cs` изменений не потребовали.

Выполнены статические проверки inventory, UTF-8, пробелов, GUID, ссылок, типов, delimiters и scoped `git diff --check`. Компиляция Unity, тесты и визуальная проверка не выполнялись. Новых подтверждённых незакрытых дефектов исключительно в owned editor-коде сейчас не установлено.

## 2. Приоритетные остатки и долг проверки

### P0 — подтверждённая опасная runtime-замена; владелец Neo.UI

Актуальный `Assets/Neoxider/Scripts/UI/MeshRig/UIMeshRigLayoutPreset.cs:333`, `RemoveExistingPoints`, по-прежнему удаляет целиком GameObject каждой owned-точки. `ApplyToOwner:230` вызывает его до создания layout. Точка на корне удаляет владельца; owned-точка с вложенным ригом удаляет чужой renderer, хотя его собственные точки пропускаются resolver. Это подтверждено трассировкой исходника, не запуском Unity.

Минимальная коррекция: проверить весь набор удаляемых ветвей до первого Destroy, используя `UIMeshRigOwnerResolver`; учитывать inactive descendants, root и вложенные `IUIMeshRigOwner`. Рабочее предположение: отказ без автоматического переподчинения объектов. Для существующего возвращающего массив API — `InvalidOperationException` с причиной до любых изменений; обычные допустимые вызовы сохраняют поведение. Новый Try/result API относится к P2, для безопасного исправления не требуется.

Файлы владельца Neo.UI: указанный runtime-файл; новые `Assets/Neoxider/Tests/Edit/UI/Audit20261023NeoUIReplacementSafetyTests.cs` и `Assets/Neoxider/Tests/Play/UI/Audit20261023NeoUIReplacementSafetyPlayModeTests.cs` с metadata. Наблюдение: после отказа остаются владелец, обе иерархии, ключи, режим и число точек; в PlayMode проверить также следующий кадр, исключив отложенный Destroy. Обычная замена создаёт ожидаемый layout. Сначала этот фикс и регрессии, затем общее API; не переносить код через границу владельцев самостоятельно.

### P1 — верификация внесённых исправлений; координатор и Neo.UI.Editor

После завершения source-changing аудитов координатор запускает 21 новый случай и существующий `Assets/Neoxider/Tests/Edit/UI/UIMeshRigEditorTests.cs`. Проверять реальные координаты всех трёх Transform-адаптеров, сохранность inactive точек/вложенных владельцев, уникальность ключей, Undo исходного режима и prefab override. Падение исправлять в ответственном исходнике по воспроизведению; повторять только затронутые проверки.

Дополнить новым `Assets/Neoxider/Tests/Edit/UI/Audit20261023NeoUIEditorPersistenceTests.cs` с metadata. Неподтверждённые риски: существующие тесты вызывают Undo, но не Redo; prefab-тест проверяет property modifications, а не сохранение/перезагрузку; `ApplyLayout:1103` использует уже текущую Undo group. Проверить полный Undo/Redo поз, rest data, ключей, motion profile и режима, сохранение временной сцены с prefab instance и повторное открытие, независимость соседней несвязанной Undo-операции. Временные assets изолировать и удалять, пользовательские сцены не сохранять. При воспроизводимом сбое минимально исправить запись newly created/configured объектов или изоляцию группы в `UIMeshRigGraphicEditor.cs`; retarget persistence — в `UIMeshRigMenu.cs`. До запуска это долг проверки, не дополнительные доказанные баги.

### P1 — доступность документации; владелец Neo.UI / координатор

Добавить ссылку на готовый `Editor.md` из `Assets/Neoxider/Docs/UI/UIMeshRig.md`. После P0 убрать устаревшее ограничение runtime-замены из editor-гайда только когда новый контракт реализован и проверен.

## 3. P2 — существенное предложение: валидируемые планы authoring

Проблема: runtime builder и editor utility независимо выбирают удаления и ключи; пользовательский C# инструмент не может проверить последствия до изменения сцены. Предложение из аудита остаётся предложением, не условием выпуска небольших безопасных исправлений.

Переиспользовать `IUIMeshRigOwner`, resolver, layout presets, `ConfigurePointOnOwner`, `ApplyPointTransform`, текущий `MarkChanged` и Undo. Проверенные Neo references не содержат готового планировщика. `ObjectNames.GetUniqueName` оставить для editor-имён; общий allocator ключей не должен зависеть от UnityEditor.

В Neo.UI добавить точные файлы:

- `Assets/Neoxider/Scripts/UI/MeshRig/UIMeshRigAuthoringPlan.cs`: immutable descriptors, diagnostics, replace policy, result и key remap.
- `Assets/Neoxider/Scripts/UI/MeshRig/UIMeshRigAuthoringPlanner.cs`: read-only snapshot и общая проверка root, ownership, ключей и структуры.

В Neo.UI.Editor добавить `Assets/Neoxider/Scripts/UI/Editor/UIMeshRigEditorAuthoring.cs`: публичный executor; существующие Inspector-команды в `UIMeshRigGraphicEditor.cs` делегируют ему. Proposed API:

```csharp
UIMeshRigAuthoringPlan PrepareLayout(IUIMeshRigOwner owner,
    UIMeshRigLayoutPreset preset, UIMeshRigReplacePolicy policy);
UIMeshRigAuthoringPlan PrepareDuplicate(IUIMeshRigOwner owner, UIMeshRigPoint source);
UIMeshRigAuthoringPlan PrepareDelete(IUIMeshRigOwner owner, UIMeshRigPoint point);
UIMeshRigAuthoringResult Apply(IUIMeshRigOwner owner, UIMeshRigAuthoringPlan plan);
```

Первые три метода принадлежат planner, последний — editor executor. Публичные diagnostics различают root point, nested owner и stale snapshot; result возвращает созданные точки и карту новых ключей. Чистая политика работает с descriptors; Unity-адаптер собирает снимок. Перед мутацией executor сверяет owner, родителей, точки и authored значения, открывает собственную Undo group, записывает конфигурацию созданных объектов и prefab overrides. Отказ валидации ничего не меняет; произвольные исключения пользовательских Unity callbacks не объявляются транзакционно откатываемыми. Scene references допустимы только в краткоживущем обычном C# объекте, не в ScriptableObject asset.

Runtime `UIMeshRigLayoutBuilder` переиспользует общую политику без ссылки на editor assembly. Существующие сигнатуры, serialized fields, авторские ключи и assets сохраняются; новые ключи детерминированы в пределах владельца, отдельные nested rigs сохраняют свой scope. Новые MonoBehaviour и NoCode bridges не нужны. `UIMeshRigAdaptersEditor.cs` меняется только для отображения типизированной диагностики при необходимости.

Минимальные новые тесты: `Assets/Neoxider/Tests/Edit/UI/Audit20261023NeoUIAuthoringPlanTests.cs` для runtime-политики и `Assets/Neoxider/Tests/Edit/UI/Audit20261023NeoUIEditorAuthoringPlanTests.cs` для executor. Проверить отсутствие мутаций при Prepare, stale-plan rejection, одинаковые решения трёх адаптеров, inactive branches, scoped keys и один Undo/Redo. Документация: C# примеры в `UIMeshRig.md` у Neo.UI; scene/Inspector diagnostics в owned `Editor.md`. Все новые package-файлы получают уникальные `.meta`; существующие asmdef references достаточны, новые зависимости не планируются.

Оценка: 2–4 рабочих дня плюс централизованная Unity-проверка; риск средний из-за публичного контракта, Undo и prefab persistence. Согласовать интерфейс между владельцами на review; это не запрос нового разрешения на рутинные фиксы.

## 4. Порядок, приёмка и границы завершения

1. Neo.UI закрывает P0 и его регрессии; editor-фиксы F1–F5 сохраняются.
2. Координатор выполняет P1, затем адресные исправления подтверждённых падений. Добавляется ссылка на editor-гайд.
3. P2 реализуется отдельно: descriptors/политика и тесты → публичный API → editor executor → тонкие adapters → C#/Inspector документация.
4. Координатор проверяет компиляцию в Unity 6.0, 6.4 и 6.5+, обе ветки UXML-меню; UI Toolkit host smoke использует существующий `UIMeshRigPanelCompat`. Новые scattered `#if` и прямые obsolete identity APIs не вводить; при необходимости использовать общие StableId helpers.
5. Editor-поведение проверяется в EditMode; PlayMode нужен для runtime replacement/deferred destruction и короткой проверки перехода preview в runtime motion. Player build требуется только если общий runtime/API diff выявляет player-зависимость, не для одного editor-гайда.

Приёмка: защищённые иерархии целы; допустимые операции сохраняют позу и восстанавливаются через Undo/Redo; prefab reload сохраняет видимый target; preview не записывает Transform/режим; обе compatibility-ветки реально скомпилированы. Отсутствующая версия Unity или невыполненный тест остаются явно непроверенными. При потере данных, partial mutation на отказе, stale-plan применении или сбое persistence дальнейшее расширение останавливается до адресного исправления; чужой код и пользовательские изменения не откатывать.

Релизные проверки выполняет координатор: scoped diff/UTF-8/GUID/docs links, отсутствие editor dependency в runtime, сохранность release layout `Samples~` и путей package samples. Samples, scenes, shared configuration и версия пакета этим планом не меняются; если будущие проверки используют импортированный sample, разрешать также developer root `Samples` и `Assets/Samples/NeoxiderTools/<version>/<sample name>`.
