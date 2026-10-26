# Neo.Core.Level.Components — индивидуальный план реализации

Фаза 2 того же аудитора. Основание: [завершённый аудит](Neo.Core.Level.Components.md), полный `PLAN-CONTRACT.md`, manifest и текущие исходники. Предложения не реализованы.

## 1. Что уже выполнено

Полностью проверен единственный собственный исходник `Assets/Neoxider/Scripts/Core/Level/Components/LevelComponent.cs`. Исправлены F1–F4: повторная инициализация из обработчиков, восстановление уровня под старым лимитом, некорректные reset/snapshot до Awake и устаревшие reactive-значения внутри событий. Повторять эти исправления как будущую работу не требуется.

Сохранены 25 публичных объявлений и 13 явно serialized-полей. Обновлён канонический `Assets/Neoxider/Docs/Core/Level/Components/LevelComponent.md`; добавлены 14 NUnit-сценариев в `Assets/Neoxider/Tests/Edit/Level/Audit20261023NeoCoreLevelComponentsTests.cs` с уникальной metadata.

Прошли только проверки исходного текста: совместимость объявлений/полей, UTF-8, пробелы, delimiters, ссылки, зависимости и GUID, scoped `git diff --check`. Компиляция, Unity-события и NUnit фактически не запускались.

## 2. Приоритеты и оставшийся долг

Подтверждённых неисправленных дефектов собственной сборки не осталось.

| Приоритет / статус | Основание, владелец и минимальное действие | Наблюдаемый результат |
|---|---|---|
| P1 — непогашенный долг проверки | Координатор после завершения source-аудитов: выполнить существующий новый fixture, `LevelCurveDefinitionTests`, `ProgressionManagerTests` на объединённых изменениях. Исправлять только воспроизведённые отклонения у владельца соответствующей сборки. | Все 14 регрессий проходят; callback читает согласованный XP/level; reset и cap сохраняются после Save/Load. |
| P1 — непроверенный lifecycle | Аудитор Components готовит отдельный PlayMode fixture, координатор запускает: неактивный объект → инициализация → активация → изменение XP → отключение → повторная активация. | Awake не повторяет загрузку; OnDisable сохраняет актуальный профиль один раз за отключение; повторная активация сохраняет прогресс. |
| P1 — совместимость не скомпилирована | Координатор: runtime и test assemblies в Unity 6.0–6.4 и 6.5+. | Нет новых ошибок компиляции/консоли; проверки обеих поддерживаемых ветвей подтверждены соответствующими компиляторами. |
| P2 — предложение | Типизированная настройка и отдельное хранилище профиля, описанные ниже. | Создание независимых runtime-профилей без reflection и смены глобального SaveProvider. |

Повторные reactive-уведомления, `OnLevelUp` при прямом понижении и инициализация перед ранним Save/Load — документированные контракты; не менять их без отдельного требования. Мутации из callback не проверены: аудит исправил согласованность чтения.

## 3. Существенное улучшение: явная конфигурация и внедряемое хранилище

**Проблема.** Режим XP, стартовые значения, лимит, ключ и загрузка настраиваются только через Inspector. Фабрики игроков/battle pass требуют prefab-вариантов или reflection; стандартный ключ объединяет профили, тесты подменяют глобальный SaveProvider.

**Переиспользование.** Сохранить `LevelModel`, `ILevelProvider`, `ILevelCurveDefinition`, `LevelProfileData.Clone/Sanitize`, `SaveProvider`, `JsonUtility`, reactive API. Core уже ограничивает переполнение XP. `ISaveProvider` содержит лишние для профиля primitive-операции; typed adapter делегирует существующему SaveProvider без нового формата/движка.

Предлагаемый добавочный C# API:

```csharp
public bool TryInitialize(LevelComponentOptions options, ILevelProfileStore store = null);
public ILevelCurveDefinition ActiveCurveDefinition { get; }

public interface ILevelProfileStore
{
    bool TryLoad(string key, out LevelProfileData profile);
    void Save(string key, LevelProfileData profile);
}
```

`LevelComponentOptions` — immutable class с конструктором и read-only свойствами `Curve`, `UseXp`, `StartXp`, `StartLevel`, `MaxLevel`, `SaveKey`, `LoadOnInitialize`, `AutoSaveOnDisable`. Числа нормализуются по существующим правилам; `MaxLevel = 0` означает отсутствие лимита. Null options отклоняется до I/O. Null store выбирает внутренний adapter. Store принадлежит вызывающему коду; компонент его не уничтожает и не dispose-ит.

`TryInitialize` применяется до Awake/первого lazy-доступа, например на неактивном объекте. Возвращает false при уже начатой/завершённой инициализации, не меняя состояние. Ошибка I/O распространяется; повторная попытка использует выбранные options/store без повторного подключения обработчиков. Чтение из initialization-callback безопасно, попытка переконфигурации из него возвращает false.

Reset, Save, Load и OnDisable используют выбранную конфигурацию. Legacy-путь продолжает читать существующие serialized-поля как раньше; explicit-путь использует options. Default adapter обращается к актуальному SaveProvider при операции, не вызывает SetProvider. Входящие DTO клонируются перед Sanitize; пустой ключ не вызывает store. Missing/malformed profile сохраняет текущее состояние и прежнюю семантику OnProfileLoaded; I/O failure не маскируется под отсутствие данных.

`ActiveCurveDefinition` показывает фактическую interface-кривую; существующая SO-ссылка `LevelCurveDefinition` сохраняет authoring-назначение. Её setter переключает активную кривую, включая null после custom-кривой; проверка равенства учитывает обе ссылки.

### Точные будущие файлы

| Действие | Путь |
|---|---|
| Изменить адаптер | `Assets/Neoxider/Scripts/Core/Level/Components/LevelComponent.cs` |
| Добавить options, контракт, внутренний default adapter | `Assets/Neoxider/Scripts/Core/Level/Components/LevelComponentOptions.cs`, `Assets/Neoxider/Scripts/Core/Level/Components/ILevelProfileStore.cs`, `Assets/Neoxider/Scripts/Core/Level/Components/SaveProviderLevelProfileStore.cs` |
| Добавить функциональные EditMode-тесты | `Assets/Neoxider/Tests/Edit/Level/Audit20261023NeoCoreLevelComponentsInitializationTests.cs` |
| Добавить lifecycle PlayMode-тесты | `Assets/Neoxider/Tests/Play/Audit20261023NeoCoreLevelComponentsLifecycleTests.cs` |
| Расширить существующую документацию | `Assets/Neoxider/Docs/Core/Level/Components/LevelComponent.md` |

Каждому новому package-файлу нужен уникальный `.meta`. Новая сборка, custom editor, NoCode-компонент и изменение asmdef не требуются: текущие Edit/Play test assemblies уже имеют необходимые ссылки.

### Межсборочные границы

- **Neo.Core:** владеет `Domain/LevelModel.cs`, `LevelProfileData.cs` и `Interfaces/ILevelCurveDefinition.cs` внутри `Scripts/Core/Level/`. После стабилизации его изменений проверить интеграцию; переносить туда новые контракты не требуется.
- **Neo.Core.Level.Data:** сохраняет SO `Data/LevelCurveDefinition.cs`; дополнительных scene-ссылок или полей не добавлять.
- **Neo.Save / Neo.Reactive:** использовать существующие provider/notification API; сначала интегрировать изменения этих владельцев, затем запускать component-регрессии. Новые публичные типы остаются в Components, без обратных зависимостей.
- **Neo.Core.Level.Bridge / Neo.Progression:** проверить прежние `LevelNoCodeAction.Execute` и потребителей `ILevelProvider`/событий. Их исходники и документация принадлежат другим владельцам; изменений для нового API не ожидается.

## 4. Порядок реализации и приёмка

1. Координатор фиксирует итоговый набор source-изменений, выполняет P1-проверки уже исправленных F1–F4. Воспроизведённые безопасные регрессии исправляются первыми, до добавления API.
2. Добавить options/store/default adapter и их интеграцию в существующий компонент; сохранить защиту callback-reentry и порядок cap → state. Отдельно проверить retry и curve setter.
3. Добавить минимальные EditMode-проверки: два ключа/два независимых профиля; explicit XP/direct modes без reflection; повторная инициализация без изменения прогресса; ошибка чтения и retry; reset к explicit-старту после загруженного cap; отсутствие I/O при пустом ключе; snapshot/DTO изоляция; переключение custom curve → null. Проверять реальные значения и обращения к store.
4. Добавить lifecycle fixture из P1. Координатор запускает EditMode, затем узкий PlayMode и интеграцию Bridge/Progression. Сценарий: 250 XP → 25 XP добавления → disable сохраняет 275 XP; повторный enable не загружает старые 250 XP. Полная игра/физика/визуальный QA для этой функции не нужны.
5. Обновить каноническую English-документацию: factory до активации, C# API, ownership store, ошибки; сохранить Inspector/UnityEvent/NoCode-путь и добавить краткое русское пояснение там же. Не создавать конкурирующий guide.
6. Проверить сохранность serialized-полей, событий, существующего JSON и prefab-совместимости; UTF-8, `.meta`, ссылки, package inclusion. Samples/Samples~/импортированные sample roots не перемещать и не изменять.

Оценка: 2–3 дня плюс центральные проверки. Риски: двойная загрузка, потеря explicit-config при retry, изменение legacy-политик, неоднозначность активной кривой. Новые version-ветки не нужны; Unity compatibility helpers остаются централизованными.

Остановить выпуск этой функции при провале регрессий, несовместимом serialized/JSON изменении или отсутствии подтверждённой компиляции поддерживаемых Unity-версий. При необходимости правки чужой сборки передать воспроизведение её владельцу, не расширять scope самостоятельно. Изменение event-count semantics, формата профиля и reconfiguration после успешной инициализации остаются отдельными решениями, не условиями выполнения текущих исправлений.

В этой фазе изменён только план; Unity/build/test не запускались.
