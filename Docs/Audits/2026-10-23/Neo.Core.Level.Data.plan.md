# Neo.Core.Level.Data — индивидуальный план реализации

Статус: планирование после завершённого аудита; реализация предложений не начата. Основа: [аудит](Neo.Core.Level.Data.md), текущие исходники и manifest от 2026-10-23. В этой фазе изменяется только данный файл.

## 1. Уже выполнено

Полностью проверены `LevelCurveDefinition.cs` и `LevelCurveEntry.cs`. В `Assets/Neoxider/Scripts/Core/Level/Data/LevelCurveDefinition.cs` исправлены округление дробного XP вверх, переполнение преобразования порогов, расчёт остатка до преобразования в `int`, общий предел Curve, преждевременный уровень из-за преобразования XP в `float` и поиск в отсутствующей Custom-таблице. Предел Curve теперь равен `max(1, min(10000, floor(lastKey.time)))`. Эти исправления повторно не планируются; `LevelCurveEntry.cs` менять не требуется.

Созданы `Assets/Neoxider/Tests/Edit/Level/Audit20261023NeoCoreLevelDataTests.cs` — 11 методов/20 случаев — и `Assets/Neoxider/Docs/Core/Level/Data/CurveEvaluation.md`; `NeoDoc` направлен на этот документ. Статически проверены UTF-8, ссылки, GUID, сохранение 11 сериализованных полей и публичных объявлений, `git diff --check`. Компиляция, NUnit и Play Mode не выполнялись.

## 2. Остаточные задачи и владельцы

**P1 — долг проверки, координатор.** После завершения параллельных изменений выполнить EditMode fixture `Neo.Core.Tests.Audit20261023NeoCoreLevelDataTests` и существующий `Neo.Core.Tests.LevelCurveDefinitionTests`. Наблюдаемые результаты: порог 12.25 требует 13 XP; при 16,777,219 XP порог 16,777,220 ещё не достигнут; остаток от 3,000,000,000 при 2,000,000,000 XP равен 1,000,000,000; конец X=2.4/2.6 даёт уровень 2 и нулевой остаток; отсутствующая таблица возвращает `false/null`. При сбое исправлять минимальную причину в соответствующей сборке, сохраняя эти публичные результаты.

**P1 — межсборочная проверка, владелец `Neo.Core`, затем координатор.** Исторический overflow из аудита уже исправлен в текущем `Scripts/Core/Level/Domain/LevelCurveEvaluator.cs`: `EvaluateLinear` насыщает число шагов, `GetXpDifference` вычитает до преобразования. `LevelModel` также ограничивает накопление XP. Это выполненная другим владельцем правка, ещё не доказанная запуском. Проверить через `LevelCurveDefinition.SetLinear(1)`: при `int.MaxValue` XP уровень равен `int.MaxValue`, остаток нулевой; дальнейшее начисление не уменьшает XP. Не создавать второй вычислитель в Data и не дублировать исправление.

**P2 — документационная интеграция, координатор/владелец `Neo.Core`.** В `Assets/Neoxider/Docs/Core/LevelCurveDefinition.md` пока нет ссылки на новый узкий документ; добавить её. `Assets/Neoxider/Docs/Core/Level/Data.meta` уже существует: создавать повторно не требуется.

Дополнительных подтверждённых неисправностей в принадлежащих сборке файлах после аудита не обнаружено. NaN, убывающие пороги и повреждённые сериализованные записи остаются рисками некорректного authoring, а не доказанными регрессиями исправлений. Совместимость дробных конечных X с проектами, полагавшимися на старое округление вверх, требует проверки потребителей.

## 3. P2: валидируемые неизменяемые таблицы порогов — предложение

**Задача разработчика.** Curve выполняет линейный поиск при каждом запросе; mutable-данные делают неявный кэш ненадёжным. Дизайнер не получает структурированного объяснения дубликатов, убывания XP и недостижимых порогов. Насыщенный `int` в preview не отличает настоящий порог от ограничения представления.

**Повторное использование.** Сохранить `ILevelCurveDefinition`, `LevelModel.SetCurveDefinition`, существующий аналитический `LevelCurveEvaluator`, `ILevelCurveEntry` для чтения Custom и текущий `Neo.Editor.Core.LevelCurveDefinitionEditor`. Поиск в Core/Level и Progression не выявил готовых `LevelThresholdDefinition`, `LevelCurveIssue` или `TryCreateSnapshot`; одноимённую систему Progression не объединять с Core.

Предлагаемые аддитивные контракты:

```csharp
// Neo.Core: immutable value, Level >= 1, RequiredXp >= 0.
public readonly struct LevelThreshold
{
    public LevelThreshold(int level, long requiredXp);
    public int Level { get; }
    public long RequiredXp { get; }
}

// Neo.Core: implements the existing two evaluation methods.
public sealed class LevelThresholdDefinition : ILevelCurveDefinition
{
    public static bool TryCreate(IReadOnlyList<LevelThreshold> thresholds,
        out LevelThresholdDefinition definition, out IReadOnlyList<LevelCurveIssue> issues);
    public bool TryGetRequiredXp(int level, out long requiredXp);
}

// Addition to LevelCurveDefinition in Neo.Core.Level.Data.
public bool TryCreateSnapshot(out LevelThresholdDefinition definition,
    out IReadOnlyList<LevelCurveIssue> issues, int maxSampledLevel = 10000);
```

Это эскиз API, не готовый компилируемый код. `LevelCurveIssue` содержит код, severity и индекс/уровень без Unity-зависимостей. Factory копирует входные значения, проверяет уникальные возрастающие ID и неубывающий XP; базовый уровень 1 при нулевом XP подразумевается. Равные пороги и пропуски ID допустимы; дубликаты ID отклоняются. Поиск возвращает последний достигнутый порог двоичным поиском, учитывая положительный cap. `TryGetRequiredXp` возвращает `false` для отсутствующего уровня, сохраняя различие между отсутствием и большим значением.

Data-адаптер поддерживает Curve/Custom, сэмплирует только целые уровни, округляет вверх и проверяет диапазон `long` до cast. NaN/infinity и убывание — ошибки; XP сверх `int.MaxValue` — предупреждение недостижимости существующим XP API. Formula возвращает диагностированный `UnsupportedMode`: её существующее аналитическое использование сохраняется. Размер сэмплирования проверяется до выделения памяти; явный лимит 100,000 поддерживает расширенный сценарий, дефолт 10,000 сохраняет старую границу. Усечение домена отмечается диагностикой. На ошибке snapshot равен null; asset не переписывается.

Snapshot не меняется после редактирования исходной Curve/таблицы; перестроение явное. Никакого автоматического кэша поверх публичных mutable-ссылок. Новый API предназначен для C#-потребителей через существующий `LevelModel`. Сценовый `LevelComponent` продолжает использовать asset напрямую; обязательная snapshot-настройка или новый NoCode-компонент не нужны.

## 4. Точные файлы и порядок реализации

Пути ниже относительно `Assets/Neoxider/`; чужие сборки меняет их владелец после согласования порядка с координатором.

| Порядок | Файл | Владелец/действие |
| --- | --- | --- |
| 1 | `Scripts/Core/Level/Domain/LevelThreshold.cs` | `Neo.Core`: новый immutable value. |
| 1 | `Scripts/Core/Level/Domain/LevelCurveIssue.cs` | `Neo.Core`: диагностические коды/severity/value. |
| 1 | `Scripts/Core/Level/Domain/LevelThresholdDefinition.cs` | `Neo.Core`: проверка, копирование, двоичный поиск. |
| 2 | `Scripts/Core/Level/Data/LevelCurveDefinition.cs` | Data: factory-адаптер после появления Core-контрактов. |
| 2 | `Tests/Edit/Level/Audit20261023NeoCoreLevelDataSnapshotTests.cs` | Data: новые публичные регрессии factory, snapshot и адаптера. |
| 3 | `Editor/Core/LevelCurveDefinitionEditor.cs` | `Neo.Editor`: показывать диагностику при изменении asset, без пересэмплирования каждого repaint и без автоматического сохранения. |
| 4 | `Docs/Core/Level/Data/CurveEvaluation.md` | Data: C# snapshot/rebuild и Inspector-диагностика. |
| 4 | `Docs/Core/LevelCurveDefinition.md` | Координатор/`Neo.Core`: ссылка и краткое описание opt-in пути. |

Сначала закрыть P1 проверками существующих исправлений; затем Core factory и её поведенческие тесты, Data-адаптер, минимальную editor-интеграцию, документацию и централизованную package-проверку. `ILevelCurveDefinition.cs`, `LevelModel.cs`, `LevelComponent.cs`, Bridge и asmdef менять для предложения не требуется. Новым package-файлам нужны уникальные `.meta`; folder metadata ведёт координатор.

Все 11 полей Data, существующие свойства, конструкторы, asset GUID и прямой режим вычислений сохраняются. Новых scene references в ScriptableObject нет. Sample layout `Samples~`/`Samples` не меняется; проверять текущий активный root, включая импортированные UPM samples. Публичные документы остаются на английском. Оценка: 2–3 дня плюс централизованная проверка; риск средний, прежде всего семантика невалидных данных и ограничения памяти.

## 5. Приёмка и условия остановки

- **Минимальные новые EditMode проверки:** snapshot совпадает с прямым вычислением валидных Curve/Custom до/на/после порогов; равный XP даёт ожидаемый скачок уровней; sparse ID и caller cap сохранены; ошибочный ввод даёт адресную диагностику без изменения asset; mutation исходных данных не меняет snapshot, rebuild меняет; `long`-порог не подменяется насыщенным `int`; явный домен 100,000 разрешается корректно. Сложность поиска подтверждать реализацией upper-bound, без хрупкого таймингового NUnit-порога.
- **Сценовый smoke после интеграции:** существующий `LevelComponent` с Curve/Custom отражает корректный уровень и остаток в reactive-состоянии; используемый `LevelNoCodeAction` вызывает тот же typed API. Для чистого factory достаточно EditMode; Play Mode нужен для централизованной проверки component/scene связи. Editor-диагностика не должна менять serialized asset.
- **Версии:** координатор компилирует runtime/editor/test assemblies в Unity 6.0–6.4 и отдельно 6.5+, выполняет целевые EditMode cases. Локальные `#if` и вызовы obsolete instance-ID API не добавляются; при необходимости использовать только общие compatibility helpers.
- **Стоп:** не объявлять Unity-проверку пройденной по статическому анализу. Не начинать Data/editor реализацию до доступности Core-контрактов; не менять чужие файлы для обхода ошибки. При несогласованной семантике диагностики зафиксировать вариант и зависимость координатору; routine fixes отдельного разрешения не требуют. До появления результатов запуска остаётся verification debt, а всё из раздела 3 остаётся предложением.

Проверка этой фазы: только чтение контекста и статическая проверка данного плана. Runtime, тесты, публичные документы, исходный аудит, конфигурация и Unity state в фазе планирования не изменяются.
