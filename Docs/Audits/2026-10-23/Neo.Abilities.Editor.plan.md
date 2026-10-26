# Neo.Abilities.Editor: индивидуальный план реализации

Фаза 2 исходного аудитора, `gpt-6.1-sol max`. Прочитаны [аудит](./Neo.Abilities.Editor.md), полный `PLAN-CONTRACT.md`, manifest entry и текущий контекст. Изменён только этот план; изменения кода ниже — будущие задачи, учитывающие параллельные аудиты.

## Выполненный результат

Все четыре C# файла просмотрены. F1–F7 уже исправлены: инициализация эффекта, обновление Inspector/библиотеки, сохранение активной карточки, null-модификаторы, сводка chain, радиусы по уровням и освобождение SerializedObject. Повторная реализация не требуется.

Добавлены `Assets/Neoxider/Docs/Abilities/Editor.md` и `Assets/Neoxider/Tests/Edit/Abilities/Audit20261023NeoAbilitiesEditorTests.cs`: 14 случаев. Публичные методы/serialized-поля сохранены. Статические UTF-8/signature/link/GUID проверки и scoped `git diff --check` прошли. Компиляция, тесты, настоящий ввод и native profiling не выполнялись.

## Оставшиеся задачи по приоритету

**V1, P1 — проверочный долг, не дефект.** После source-changing аудитов координатор компилирует Editor/runtime/tests на Unity 6.0–6.4 и отдельно 6.5+, запускает 14 случаев. Reflection-команды и отправленные `SerializedPropertyChangeEvent` не доказывают штатную генерацию событий, panel timing/focus. Дополнение в существующий regression-файл: настоящий bound TextField, сохранённые данные после ввода и обновлённая доска после editor update. Сбой исправлять по воспроизведению, не ослаблять assertion.

**R1, P2 — подтверждённое исходниками несовпадение регистра.** В `Assets/Neoxider/Scripts/Abilities/Domain/AbilitySystem.cs:18` каталоги используют `OrdinalIgnoreCase`; `Domain/Effects/EffectOpRegistry.cs` так же разрешает операции, исполняемые через `Ops.TryGet`. Но `Editor/AbilityDesignerUI.cs` использует `Ordinal` для трёх коллекций id и точные сравнения операций. Ссылка `burn` на `Burn` получает ложное dangling-предупреждение; `Fireball`/`fireball` не распознаются как overrides; `CHAIN` пропускает hop-radius проверку и показывается как custom.

Владелец `Neo.Abilities.Editor`: согласовать comparers/распознавание операций с runtime, не переписывая serialized-строки. Файлы: `Assets/Neoxider/Scripts/Abilities/Editor/AbilityDesignerUI.cs` и существующий regression-файл. Проверки: mixed-case modifier reference, duplicate ability/modifier ids, uppercase chain с нулевым радиусом, apply/remove_modifier, lowercase/custom controls. Регистр не должен менять lookup, warnings/summary; custom остаётся custom. Новое наблюдение планирования: исправление и Unity-воспроизведение не выполнены. Runtime-правки не нужны.

**R2, P2 — подтверждённый долг документации другого владельца.** `Assets/Neoxider/Docs/Abilities/AbilityDesignerWindow.md:30` всё ещё разрешает положительный flat radius либо любую положительную запись массива. Владелец `Neo.Abilities`/координатор заменяет правило на приоритет непустого массива и положительность каждого уровня; добавляет ссылку на `Editor.md` в этот guide и `Assets/Neoxider/Docs/Abilities/README.md`. Проверка: `[0,3]` при flat `6` описан как warning, `[2,3]` при flat `0` — как допустимый случай. Эти чужие документы здесь не изменяются.

**V2, P2 — непроверенный lifecycle.** Координатор проверяет ввод в `CustomParam`, focus/foldout, переключение/удаление ассета, Undo/Redo, закрытие/открытие и отмену создания. Данные/UI должны совпадать без остановки редактирования и обращений к освобождённому wrapper. Остаточная утечка не подтверждена; profiling проводить при воспроизводимом подозрении.

## Существенное улучшение P2: типизированная проверка выбранного каталога

**Предложение, не реализация.** Project-wide `AbilityValidation.Scan` принимает modifier из постороннего каталога и смешивает независимые библиотеки. Строковый `AbilityIssue` не адресует поле; одного id недостаточно для дубликата/определения без id.

Переиспользовать blueprints, `AbilityLibrary.Abilities/Modifiers`, порядок `RegisterInto`, comparers, `EffectContext.TryGetSpecial`, `LeveledValueResolver.SampleByLevel`, снимок `EffectOpRegistry.RegisteredIds`. Не создавать симуляцию и не обращаться к `AbilitySystemBehaviour.I/System`, создающим/инициализирующим сценовую систему.

Предлагаемый C# API:

```csharp
// Neo.Abilities.AbilityDefinitionValidator
public static IReadOnlyList<AbilityDiagnostic> Validate(AbilityValidationContext context);
// Neo.Abilities.Editor.AbilityDesignerWindow: дополнительный entry point
public static void OpenForLibraries(IReadOnlyList<AbilityLibrary> libraries);
```

`AbilityValidationContext` содержит упорядоченные списки blueprints и опциональные объявленные operation/resource/archetype ids. `null` dependency-каталог означает «неизвестен», пустой предоставленный — «известно, что пуст»; открытые реестры нельзя объявлять сломанными без такого контекста. Diagnostics неизменяемы: `Code`, `Severity`, `DefinitionKind`, `DefinitionIndex`, отображаемый `DefinitionId`, относительный `PropertyPath`, `Message`. Индекс адресует исходную запись даже при пустом/повторном id. Unity Object и scene references в core отсутствуют. Некорректное содержимое даёт diagnostics; null самого context — явный `ArgumentNullException`.

Существующие регистрации остаются case-insensitive, last-registration-wins. Валидатор только сообщает о перекрытиях; не запрещает runtime-регистрацию. Editor SourceMap связывает kind/index с ассетом и переводит путь вроде `ImpactEffects[0].RadiusByLevel[1]` в SerializedProperty path. Project остаётся исходным режимом; Library scope использует выбранную упорядоченную последовательность. Существующие `Open` сохраняют совместимый Project workflow. Scene bindings первоначально передаются только явным набором ids; новый компонент/NoCode bridge не нужен.

### Точные файлы и владельцы будущей реализации

| Владелец | Файлы |
| --- | --- |
| `Neo.Abilities`, сначала | NEW `Assets/Neoxider/Scripts/Abilities/Domain/Validation/AbilityValidationContext.cs`, `AbilityDiagnostic.cs`, `AbilityDefinitionValidator.cs` в той же папке. Каталоги/регистрация в `Domain/AbilitySystem.cs` и `Data/AbilityLibrary.cs` служат контрактом; их поведение не менять. |
| `Neo.Abilities.Editor`, затем | NEW `Assets/Neoxider/Scripts/Abilities/Editor/AbilityValidationAdapter.cs`; MODIFY `AbilityDesignerUI.cs` и `AbilityDesignerWindow.cs` в этой папке: адаптация Scan, выбор scope, адресация поля, сохранение активных контролов. Два asset inspectors сохраняют прежний shortcut. |
| Этот модуль / координатор тестирования | NEW `Assets/Neoxider/Tests/Edit/Abilities/Audit20261023NeoAbilitiesEditorValidationTests.cs`; существующий fixture сохраняется для F1–F7 и R1. Core-тесты проверяют коды/пути/отсутствие мутации, UI — выбранный asset/property и bound edits. |
| Документация по ownership | MODIFY `Assets/Neoxider/Docs/Abilities/Editor.md`; владелец `Neo.Abilities` обновляет `README.md`, `AbilityLibrary.md`, `AbilityDesignerWindow.md` в этой папке: пример Validate для C#, Library scope для сценового/NoCode авторинга. |

## Порядок реализации и совместимость

1. Закрыть V1, исправить R1 с регрессиями; согласовать R2 с владельцем документации. Между сборками нет зависимости для этих малых исправлений.
2. После стабилизации аудита `Neo.Abilities` добавить uncached validator и контрактные тесты; затем подключать editor adapter. Порядок библиотек не сортировать: он определяет overrides. Результаты выдавать детерминированно по kind/index/path/code.
3. Добавить Library scope и переход к полю; сохранить существующие Undo/binding и вначале оставить внутренний Scan как адаптер. Обратный dependency index — отдельный P3 этап только после измерения стоимости полного сканирования, не условие первого релиза.
4. Обновить English docs и краткое русское пояснение, затем центральные compile/EditMode/package checks. Базовой фиче PlayMode не нужен: cast pipeline и сцены не меняются. Если позднее появится сценовый binding-adapter, ему нужен отдельный PlayMode smoke регистрации/каста.

Оценка: R1 — до половины дня + QA; validator/adapter — 3–5 дней, индекс ещё 1–2. Риски: override/case semantics, неполные plugin-каталоги, stale SourceMap. Definition-поля, GUID/данные, asmdef не мигрируют; Editor уже ссылается на runtime. Новым файлам нужны уникальные `.meta`, folder metadata ведёт координатор. `Samples~`, developer `Samples`, imported samples не перемещать/не требовать. Unity-version различия держать в существующих централизованных helpers, без локальных `#if`.

## Наблюдаемая приёмка и условия остановки

- Библиотека A ссылается на modifier только из B: Library scope A указывает точную отсутствующую ссылку, Project scope явно показывает свой охват. Независимые каталоги не смешиваются.
- Повторные/пустые id адресуют правильный ассет через индекс; overrides соответствуют runtime. Case-варианты дают одинаковый результат; named specials используют существующую семантику `TryGetSpecial`.
- Радиус `[0,3]` указывает конкретный проблемный уровень; declared custom op принимается; неизвестный dependency-каталог не создаёт ложную ошибку. Входные blueprints не мутируют.
- Клик диагностики раскрывает правильное поле; реальный ввод `audit` остаётся в том же активном контроле, данные и сводка обновляются; Undo возвращает оба состояния.
- Оба семейства Unity скомпилированы, scoped EditMode прошёл, player compilation не требует Editor assembly; центральная package-проверка не требует импортированных samples.

Compile/behavioral failure останавливает выпуск соответствующей части. Строгость overrides и включение scene/plugin descriptors — решения объёма координатора; базовый advisory/last-wins вариант не требует дополнительного разрешения на обратимые работы. Profiling/индекс не заменяют обязательную проверку.
