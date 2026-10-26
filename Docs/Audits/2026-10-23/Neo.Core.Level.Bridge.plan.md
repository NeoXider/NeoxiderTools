# Neo.Core.Level.Bridge — индивидуальный план реализации

Статус: план, без реализации предложений. Продолжение исходного аудитора; профиль `gpt-6.1-sol`, reasoning `max`. Основание: [завершённый аудит](Neo.Core.Level.Bridge.md), полный `PLAN-CONTRACT.md`, актуальный manifest и текущие исходники, документация, регрессии. Запись этой фазы ограничена данным файлом.

## 1. Что уже завершено

Все четыре C# файла инвентаря проверены. В `LevelConditionAdapter.Evaluate` отсутствующий провайдер теперь даёт одинаковые возвращаемый результат и `LastResult`, включая инверсию. В `LevelNoCodeAction.Execute` итоговый уровень сохраняется до `OnSuccess`; последующие изменения провайдера больше не подменяют, не подавляют и не создают событие завершённой операции. Сохранены порядок событий и уведомления о снижении уровня.

Обновлены оба руководства Bridge, исправлено обещание поддержки произвольного `ILevelProvider`: текущий resolver принимает только `LevelComponent`. Добавлены 10 методов / 22 NUnit случая в `Audit20261023NeoCoreLevelBridgeTests.cs`. Статически проверены API, сериализованные поля, enum, UTF-8, ссылки, GUID и scoped `git diff --check`. Компиляция и выполнение тестов не проводились. Эти исправления не являются оставшимися задачами реализации.

## 2. Подтверждённые дефекты и долг проверки

Новых подтверждённых дефектов внутри сборки нет. Следующие приоритеты относятся к проверкам, а не к предполагаемым ошибкам:

| Приоритет / владелец | Основание и минимальное действие | Наблюдаемое завершение |
|---|---|---|
| P1, координатор | Нет компиляции собранного дерева после параллельных аудитов. После завершения изменений зависимостей проверить Bridge и editor tests в Unity 6.0–6.4 и 6.5+. | Обе проверки компиляции успешны, новых ошибок нет; сохранены версия Unity и проверенная ревизия/состояние дерева. |
| P2, координатор; исправление выявленного сбоя — владелец соответствующей сборки | Не выполнен `Neo.Core.Tests.Audit20261023NeoCoreLevelBridgeTests`. Запустить его 22 случая и относящиеся к Bridge существующие проверки в `LevelCurveDefinitionTests.cs`. | Инвертированный отсутствующий провайдер возвращает и сохраняет true; операция 1→4 с success-listener 4→7 сообщает 4; вложенные вызовы дают `success:2, success:3, level:3, level:2`. |
| P2, координатор | Fixtures используют неактивные объекты и не проверяют настоящий Awake/отложенный Destroy. Провести короткую PlayMode проверку активных компонентов, вызова Execute через UnityEvent и fallback после уничтожения назначенного провайдера. | Те же состояния и события, после следующего кадра используется живой sibling/context, новых ошибок Console нет. Это риск интеграции, не установленный дефект. |

Ранее обнаруженное переполнение XP уже устранено владельцем `Neo.Core`: актуальный `LevelModel.AddXp` ограничивает добавление оставшимся диапазоном int. Координатор проверяет его регрессию: 1 XP плюс `int.MaxValue` даёт `int.MaxValue`, без отрицательного XP. Bridge не должен дублировать эту арифметику.

## 3. P3: внедрение провайдера и чистое правило условий

**Предложение, не реализовано.** Разработчик собственного battle pass или модели уровня сейчас не может передать произвольный `ILevelProvider` в Bridge. У адаптера условий также нет публичного конфигурирования mode/threshold/invert. Для исправления ошибок аудита эта возможность не обязательна.

Повторно использовать существующие `ILevelProvider`, `IConditionEvaluator`, оба enum, UnityEvent и `LevelCurveDefinition.SetLinear` в тестах. Не создавать второй level model, контейнер зависимостей, persistence или глобальный поиск.

Предлагаемый публичный API в namespace `Neo.Core.Level`:

```csharp
public readonly struct LevelConditionRule
{
    public LevelConditionRule(LevelConditionEvaluationMode mode, int threshold, bool invert = false);
    public bool Evaluate(ILevelProvider provider);
}
// Дополнения к обоим существующим компонентам:
public void Initialize(ILevelProvider provider);
// Дополнения к LevelConditionAdapter:
public LevelConditionEvaluationMode Mode { get; set; }
public int Threshold { get; set; }
public bool Invert { get; set; }
```

Это схема сигнатур. Rule выполняет существующие включительные сравнения без GameObject, событий и сохранения LastResult. Порог сохраняется как переданный int, без новой скрытой нормализации; `[Min(0)]` остаётся ограничением Inspector. Так перенос dispatch не меняет даже ранее сериализованные отрицательные значения. Null и неизвестный mode дают false до инверсии. Unity lifetime проверяется адаптером до передачи провайдера правилу.

`Initialize` записывает несериализуемый runtime override без немедленного чтения провайдера. Приоритет: живой override → сериализованный `LevelComponent` → прежний sibling/context fallback. `Initialize(null)` очищает только override. Уничтоженный Unity object, удерживаемый через интерфейс, считается отсутствующим; обычный C# объект остаётся допустимым. Неактивный живой компонент сохраняет нынешнюю доступность API. Setter существующего `LevelNoCodeAction.LevelProvider` очищает override; getter продолжает возвращать конкретную serialized reference. Инъекция повторяется bootstrap-кодом после domain reload и не очищается при простом OnDisable.

Rule остаётся в Bridge вместе с enum: это обычная C# логика без сценовых объектов, но не отдельный пакет без Unity-зависимостей. Перенос типов в `Neo.Core` создал бы миграцию assembly identity или обратную зависимость; он исключён из предложения.

## 4. Точные файлы и границы

Префиксы путей: `B = Assets/Neoxider/Scripts/Core/Level/Bridge/`, `T = Assets/Neoxider/Tests/Edit/Level/`, `D = Assets/Neoxider/Docs/Core/Level/Bridge/`.

| Файл | Будущая работа |
|---|---|
| `B/LevelConditionRule.cs` и новый уникальный `.meta` | Добавить immutable rule. |
| `B/LevelConditionAdapter.cs` | Typed accessors, инъекция, делегирование rule, сохранение LastResult. |
| `B/LevelNoCodeAction.cs` | Инъекция и явная семантика существующего setter; сохранить захват результата. |
| `T/Audit20261023NeoCoreLevelBridgeRuleTests.cs` и `.meta` | Сравнения на границе, инверсия, null/неизвестный mode, отрицательный порог; обычный C# test provider. |
| `T/Audit20261023NeoCoreLevelBridgeProviderTests.cs` и `.meta` | Обе операции с injected provider, приоритеты, очистка/замена override, уничтожение Unity provider и fallback. |
| `D/LevelConditionAdapter.md`, `D/LevelNoCodeAction.md` | Реальные C# примеры и прежняя scene/NoCode настройка; runtime lifetime и отсутствие сериализации инъекции. |

Оба enum и существующий regression fixture сохраняются. Новые helpers тестов остаются внутри этих fixtures. Новых папок, editor-компонентов и изменений asmdef не ожидается.

Внешние владельцы: `Neo.Core` — `Interfaces/ILevelProvider.cs`, `Domain/LevelModel.cs`; `Neo.Core.Level.Components` — `Components/LevelComponent.cs` и его initialization/reactive callbacks; `Neo.Core.Level.Data` — данные кривой. Пути первых трёх указаны относительно `Assets/Neoxider/Scripts/Core/Level/`. `Neo.Condition`, `Neo.Reactive`, `Neo.PropertyAttribute` предоставляют существующие контракты, события и атрибуты. Изменения этих сборок не требуются; сначала координатор интегрирует их аудиты, затем проверяет Bridge. Только координатор меняет общие ссылки/config при реально выявленной необходимости.

## 5. Порядок и приёмка

1. Зафиксировать центральную проверку уже сделанных исправлений; сбои устранять по воспроизводимому поведению и владельцу, не реализуя предложение ради обхода проверки.
2. Добавить rule и минимальные поведенческие тесты, затем выполнить сравнение с нынешними тремя режимами.
3. Добавить typed injection/accessors, проверить обычного C# провайдера, Unity lifetime и прежний Inspector путь. Все исходные 22 случая должны остаться успешными, включая снижение уровня и вложенный Execute.
4. Обновить два руководства. Сохранить serialized fields, numeric enum, публичные вызовы и события; сцены/префабы не требуют миграции. Оставить активный sample layout (`Samples~`, developer `Samples` либо imported root) без переименований; новый sample для этой функции не нужен.
5. Повторить scoped static checks, затем центральные compile/EditMode и короткую PlayMode проверку. Новые package-файлы получают уникальные meta GUID. Release/package проверки выполняет координатор; worker не меняет manifest, version или settings.

Приёмка улучшения: C# провайдер без scene object выполняет обе операции и все сравнения; уничтоженный override не блокирует fallback; clearing override восстанавливает прежний источник; старые Inspector настройки и события дают прежний результат. Сериализация не сохраняет runtime injection.

Оценка: 1–2 инженерных дня плюс центральная проверка Unity. Основные риски — Unity-null через интерфейс, приоритеты и незаметное изменение семантики при извлечении rule. Нельзя закрывать release-проверку по лексическому анализу вместо компиляции обеих версий. Новые version-dependent вызовы должны использовать общие compatibility helpers, без распределённых `#if`.

Продуктового решения для закрытия исправлений не требуется. Если реализация потребует переносить типы между сборками, менять serialized contract или политику missing-provider inversion, остановить именно расширение и вынести отдельное архитектурное решение координатору. Непроверенное выполнение остаётся явно непроверенным. На фазе планирования Unity, сборки, тестовые процессы, коммиты и дочерние агенты не запускались.
