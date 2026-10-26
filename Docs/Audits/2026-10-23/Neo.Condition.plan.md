# Neo.Condition — индивидуальный план реализации

Статус: план исходного аудитора, предложения не реализованы. Профиль по заданию: `gpt-6.1-sol`, `max`. Прочитаны PLAN-CONTRACT, завершённый аудит, manifest и актуальные изменения/границы. Перед реализацией сверить окончательные версии соседних сборок.

## 1. Уже выполнено

Проверены все шесть файлов инвентаря. CND-01–07 исправлены в `Assets/Neoxider/Scripts/Condition/{ConditionEntry,ConditionValueSource,ReflectionCache,NeoCondition}.cs`: смена контекста, BindingFlags, числовые аргументы, generic-методы, уничтоженный второй источник, серверные проверки и повторная серверная оценка. Повторно реализовывать эти исправления не требуется.

Обновлены `Assets/Neoxider/Docs/Condition/{README,NeoCondition,Condition_Reuse,InternalTypes}.md`. Добавлены `Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoConditionRegressionTests.cs` и `Audit20261023NeoConditionValueProbe.cs` с metadata: 25 случаев, включая три проверки woven command body.

Фактически прошли только статические проверки: инвентарь, сохранение публичных объявлений/serialized fields, UTF-8/BOM, GUID, ссылки, whitespace и scoped diff-check. Компиляция, weaving, EditMode, PlayMode и player build **не выполнялись**. Написанные тесты не считаются пройденными.

## 2. Оставшиеся исправления и долг проверки

| Приоритет / статус / владелец | Доказательство и минимальное следующее действие | Наблюдаемая приёмка |
|---|---|---|
| P1, долг проверки; координатор + Neo.Network | В `NeoCondition.Check` изменён порядок dispatch, в command handlers — authority/cooldown. Три текущих ingress-теста проверяют только отклонение. Добавить `Assets/Neoxider/Tests/Play/Audit20261023NeoConditionNetworkTests.cs`; переиспользовать `Assets/Neoxider/Scripts/Network/Core/NeoNetworkComponent.cs:RateLimitCheck(sender)`. | При неизменном локальном bool повторный Check получает изменившийся серверный результат; ServerRevalidate отвергает клиентское значение, TrustClient принимает; offline ingress не меняет LastResult/события; два отправителя независимы; host вызывает событие один раз; late join получает последнее состояние. |
| P2, подтверждённый межсборочный дефект; Neo.Editor | `Assets/Neoxider/Editor/Condition/NeoConditionEditor.cs:GetCurrentValueFromComponent` передаёт boxed int напрямую; `Assets/Neoxider/Editor/Binding/ComponentBindingInspectorShared.cs:BuildConditionMemberLists` не исключает открытые generic. Владельцу исправить preview через `ReflectionCache.GetMethod`, преобразование к фактическому параметру и фильтр ContainsGenericParameters; добавить `Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoConditionEditorParityTests.cs`. | ReadShort(12) показывает 13 и совпадает с runtime; byte=256 не вызывает метод; generic отсутствует среди доступных кандидатов. Проверять обе стороны сравнения. |
| P2, подтверждённая неточность документации; Neo.Condition | Мой `Assets/Neoxider/Docs/Condition/README.md` обещает все четыре события; `NeoCondition.cs:InvokeEvents` вызывает два bool-события и одну ветвь OnTrue/OnFalse. Поправить фразу. В `Assets/Neoxider/Docs/Condition/Condition_Reuse.md` уточнить текущий `Assets/Neoxider/Scripts/StateMachine/ConditionEntryPredicate.cs`: fallback к state GameObject разрешён только для Owner, отсутствующий Override не подменяется владельцем. | Документация описывает `(OnResult, OnInvertedResult, OnTrue/OnFalse)`; повтор без изменения подавлен. Описанный ContextSlot совпадает с текущим кодом владельца Neo.StateMachine. |
| P2, общий долг проверки; координатор | Выполнить 25 новых случаев, существующие `NeoConditionTests.cs`, `NeoConditionThrottleTests.cs`, актуальные StateMachine/NoCode binding suites. Добавить PlayMode lifecycle-сценарий в `Assets/Neoxider/Tests/Play/Audit20261023NeoConditionLifecycleTests.cs`, только если существующий harness его не покрывает. | A→B→null→A не сохраняет старого владельца; уничтожение/замена источника восстанавливается корректно; Interval останавливается при disable и возобновляется единственным циклом; ResetState и OnlyOnChange дают ожидаемые счётчики событий. |
| P2, неподтверждённый runtime-риск; Neo.Condition | `ConditionEntry.EvaluateInternal` вызывает EnsureCache вне read-catch; `ConditionValueSource` принимает индексируемые свойства. Сначала воспроизвести два перегруженных индексатора с именем Item. При подтверждении — узкая обработка AmbiguousMatchException и отказ от индексаторов в `ConditionValueSource.cs`; regression в `Audit20261023NeoConditionRegressionTests.cs`. | Некорректный источник даёт false при Invert=false; следующий корректный источник читается нормально. Не ограничиваться проверкой отсутствия исключения. |

Владельцы внешних границ: `Neo.Editor`, `Neo.Network`, `Neo.StateMachine`, `Neo.NoCode` по manifest. Их файлы этот план не поручает изменять текущему worker. Сверить соответствующие планы перед интеграцией.

## 3. Существенное улучшение: typed core и явный результат

**Предложение P2, не выполнено.** Сейчас ConditionEntry объединяет сериализацию, scene bindings, reflection, сравнение и изменяемый кэш. Метод/Other Object трудно настроить публичным C# API. Отсутствующий источник и отрицательное сравнение неразличимы; Invert превращает оба в true. Исправление контекста устраняет неправильное значение, но не разделяет сессии нескольких потребителей.

Переиспользовать `CompareOp`, `ReflectionCache`, `BindingSourceGameObjectResolver`, `NeoDiagnostics`, BCL comparers. Ordinal/Mathf.Approximately semantics передавать comparer из Unity-адаптера: core не вызывает Mathf. `StatePredicate`, числовые predicates и `CustomPredicate` привязаны к StateMachine/bool; обратная зависимость создаст цикл: Neo.StateMachine уже ссылается на Neo.Condition. `ComponentFloatBinding.TryResolveSupportedSourceMember` — образец отказа от неоднозначных/indexed members, не зависимость Condition→NoCode.

Предлагаемый добавочный публичный контракт:

```csharp
public interface IConditionValueSource<TContext, TValue>
{
    bool TryRead(TContext context, out TValue value);
}
// ConditionComparison<TContext, TValue>: left/right providers,
// CompareOp, IComparer<TValue>, IEqualityComparer<TValue>.
// ConditionResult Evaluate(TContext context, bool invert = false).
// ConditionEntry: EvaluateDetailed(GameObject), TryEvaluate(GameObject, out bool),
// CreateSession() -> ConditionBindingSession.
```

`ConditionResult.Status`: Passed, Failed, Unavailable, InvalidConfiguration, ReadError; `IsMatch` истинен только для Passed. Инверсия нового API применяется только к состоявшемуся сравнению. TryEvaluate возвращает true только для Passed/Failed; при ошибке out-result=false. Legacy Evaluate сохраняет текущие bool/inversion semantics; IConditionEvaluator и события NeoCondition не менять.

Точные файлы реализации в `Assets/Neoxider/Scripts/Condition/`:

- новые `IConditionValueSource.cs`, `ConditionResult.cs`, `ConditionComparison.cs`: обычный C#, без GameObject, MonoBehaviour и сетевого транспорта;
- новый `ConditionBindingSession.cs`: snapshot binding-настроек и отдельные кэши потребителя; изменение настроек требует новой сессии, Invalidate очищает её разрешённые ссылки/metadata;
- существующие `ConditionEntry.cs`, `ConditionValueSource.cs`, `ReflectionCache.cs`: адаптер, детальные исходы, подготовка reflection без runtime code generation; совместимые legacy paths;
- новые `Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoConditionCoreTests.cs` и `Audit20261023NeoConditionResultTests.cs`: plain-context сравнения, состояния ошибок, две независимые сессии, legacy inversion parity.

Сначала оставить всё в Neo.Condition: чистота классов не означает уже выполненное отделение assembly от Unity/Neo.Network. Новый asmdef/dependency split — отдельная координируемая работа. Scene bindings остаются на компонентах, новые общие определения не содержат scene references. Сессии не подписываются на глобальные события; потребитель освобождает ссылки при завершении использования.

**Отдельное продолжение P3:** `ConditionMemberDescriptor.cs` плюс адаптация `ConditionEntry.cs`, `ConditionValueSource.cs`, `ReflectionCache.cs` и двух editor-файлов выше. Версионированный descriptor хранит exact parameter type/declaring type и идентификатор component binding. Старые name/kind/type/index поля сохраняются; заполнение descriptor только явным редактированием. Приёмка: int/long overload и два одинаковых компонента различаются после reload. Это расширение представления, а не уже исправленный дефект.

Оценка: 4–6 дней core/session/API/tests/docs, ещё 2–3 дня descriptor/editor/AOT. Риски: legacy ambiguity, удержание ссылок, stripping и несовпадение comparers. Минимальные core-тесты проверяют реальные значения plain-context, недоступность под инверсией, уничтожение Unity-источника и независимость сессий.

## 4. Порядок, совместимость и условия завершения

1. Сверить финальные изменения соседних сборок; выполнить точечные docs/editor исправления, затем их regressions. Подтверждать reflection-риск отдельным воспроизведением до исправления.
2. После завершения source-аудитов координатор собирает baseline, выполняет EditMode и сетевые/lifecycle сценарии. Ошибки исправляются в соответствующем ownership; уже сделанные CND-01–07 не дублируются.
3. Реализовать additive core, затем Unity sessions/adapters; descriptor/editor расширение выпускается отдельно. Обновить четыре существующих документа Condition: C# настройка/статусы и scene/NoCode workflow с прежними UnityEvents.
4. Сохранить serialized names, enum values, prefab/scene GUID и legacy asset поведение. Новым Assets-файлам дать уникальные `.meta`; shared asmdef/package edits выполняет координатор. Samples layout не переименовывать; при demo-проверке распознавать активный `Samples`, `Samples~` или импортированный `Assets/Samples/NeoxiderTools/<version>/<sample>`.
5. Компилировать Unity 6.0–6.4 и 6.5+ с Mirror/без Mirror; API идентификаторов остаются централизованными в NeoDiagnostics/NeoComponentHealth. Успех одной ветки не доказывает другую. Для нового reflection API нужен небольшой IL2CPP smoke со stripping и зафиксированным preservation-контрактом.

Приёмка исправлений требует наблюдаемых результатов таблицы; три woven-теста в Mirror-конфигурации обязаны реально выполняться. Skip, ошибка compile, неподтверждённый AOT или отсутствие client/server-прогона оставляют соответствующий пункт открытым. Новые API/descriptor остаются предложениями до реализации; продуктовый выбор миграции descriptor не блокирует безопасные исправления. Сейчас записан только этот план; runtime, тесты, публичные документы, аудит и конфигурация не изменялись.
