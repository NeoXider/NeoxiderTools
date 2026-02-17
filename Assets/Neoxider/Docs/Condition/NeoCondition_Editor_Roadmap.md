# NeoCondition — Roadmap развития Editor/No-Code системы

## Цель

Сделать `NeoCondition` профессиональной, расширяемой и переиспользуемой системой:
- удобной для no-code пользователей;
- устойчивой к рефакторингу кода;
- пригодной для повторного использования в других модулях (`StateMachine`, `Trigger`, `Quest`, `AI Rules`).

---

## Проблемы текущей реализации

1. `NeoConditionEditor` содержит много логики в одном классе (монолитный инспектор).
2. Выбор полей/свойств основан на reflection без явных контрактов на стороне runtime-классов.
3. Сериализация выбора члена (`_componentTypeName` + `_propertyName`) хрупкая при переименованиях.
4. Логику выбора членов сложно переиспользовать в других no-code системах.
5. UI расширяется, но нет общего infrastructure-слоя для editor-инструментов условий.

---

## Архитектурное направление

### 1) Метаданные через атрибуты

Ввести систему атрибутов для явного описания переменных/полей условий.

Предлагаемые атрибуты:
- `ConditionValueAttribute` — поле/свойство доступно для `NeoCondition`.
- `ConditionLabelAttribute(string label)` — человекочитаемое имя в dropdown.
- `ConditionGroupAttribute(string group)` — группировка в меню (например, `Stats/HP`).
- `ConditionOpsAttribute(params CompareOp[])` — разрешенные операторы для значения.
- `ConditionOrderAttribute(int order)` — приоритет в списке.

Плюсы:
- явные контракты;
- меньше шумных полей в списке;
- проще UX для дизайнеров.

---

### 2) Общий реестр членов условий (Editor)

Создать editor-сервис, например `ConditionMemberRegistry`, который:
- собирает доступные члены по типу;
- кэширует результат (`Type -> MemberDescriptor[]`);
- поддерживает инвалидацию кеша на domain reload/refresh;
- учитывает атрибуты и fallback-режим для legacy.

`MemberDescriptor` (пример):
- `TypeFullName`
- `MemberName`
- `MemberKind` (`Field` / `Property`)
- `ValueType`
- `DisplayLabel`
- `Group`
- `AllowedOps`
- `Order`

Плюсы:
- переиспользуемость;
- единая логика валидации;
- ускорение инспектора.

---

### 3) Надежная сериализация выбора члена

Перейти от пары строк к структуре идентификатора:
- `AssemblyQualifiedTypeName`
- `MemberName`
- `MemberKind`
- `ResolvedValueType`
- `Version` (для миграций)

Добавить миграцию:
- при загрузке старых данных автоматически заполнять новый формат;
- сохранить обратную совместимость со старыми сценами и префабами.

---

### 4) Модульный editor UI (drawer-пайплайн)

Разбить текущий `NeoConditionEditor` на независимые блоки:
- `ConditionSourceDrawer`
- `ConditionMemberDrawer`
- `ConditionCompareDrawer`
- `ConditionRuntimePreviewDrawer`
- `ConditionListDrawer`
- `ConditionValidationDrawer`

Плюсы:
- проще поддерживать;
- можно переиспользовать в других инспекторах;
- меньше риск регрессий при добавлении новых фич.

---

### 5) Переменные контекста через provider-интерфейс

Добавить новый режим источника:
- `SourceMode.Provider`

Ввести интерфейс:

```csharp
public interface IConditionVariableProvider
{
    bool TryGet(string key, out int value);
    bool TryGet(string key, out float value);
    bool TryGet(string key, out bool value);
    bool TryGet(string key, out string value);
}
```

Это позволит:
- работать без reflection там, где данные вычисляются динамически;
- подключать условия к внешним системам (экономика, AI, квесты, серверные данные);
- делать сложные no-code сценарии без хрупких ссылок на конкретные поля.

---

## UX улучшения (Inspector)

1. Поисковая строка по компонентам/свойствам.
2. Группировка (`Stats`, `Combat`, `UI`, `Meta`).
3. Избранные поля (pin/favorites).
4. Инлайн-валидация проблем:
   - член не найден;
   - тип изменился;
   - оператор не поддерживается.
5. Быстрые действия:
   - `Ping Source Object`;
   - `Select Source Object`;
   - `Open Script` (если возможно).
6. Режим preview:
   - текущие значения в play mode;
   - подсветка условий, которые дают `false`.

---

## Обратная совместимость

Обязательные правила:
- не ломать существующие сцены/префабы;
- старые `ConditionEntry` должны продолжать работать;
- все новые поля добавлять с безопасными default-значениями;
- migration-on-load + warning только при реально нерешаемых конфликтах.

---

## Этапы реализации

### Phase 1 — Foundation (без breaking changes)

1. Добавить атрибуты:
   - `ConditionValueAttribute`
   - `ConditionLabelAttribute`
   - `ConditionGroupAttribute`
   - `ConditionOpsAttribute`
   - `ConditionOrderAttribute`
2. Реализовать `ConditionMemberRegistry` и `MemberDescriptor`.
3. Подключить реестр в `NeoConditionEditor` (fallback на текущий reflection).
4. Добавить базовые unit-tests для резолва членов.

Ожидаемый результат:
- UI работает как раньше, но уже через новый слой данных.

### Phase 2 — Data Model hardening

1. Добавить новый сериализуемый идентификатор члена.
2. Реализовать миграцию старых `ConditionEntry`.
3. Добавить детальную диагностику несовпадений.

Ожидаемый результат:
- устойчивость к переименованию/переносу компонентов.

### Phase 3 — Editor UX

1. Разбить инспектор на drawer-модули.
2. Добавить поиск/группы/избранное.
3. Улучшить runtime preview и отладочные подсказки.

Ожидаемый результат:
- заметно более удобный no-code workflow для дизайнеров.

### Phase 4 — Provider Mode

1. Добавить `SourceMode.Provider`.
2. Добавить поддержку `IConditionVariableProvider`.
3. Реализовать UI выбора key + типа значения + операторов.

Ожидаемый результат:
- универсальные условия для любых систем без привязки к reflection.

---

## Риски и как их снизить

1. **Риск:** рост сложности инспектора.  
   **Митигировать:** модульные drawer-классы и тесты.

2. **Риск:** поломка старых сохраненных условий.  
   **Митигировать:** миграция + fallback + интеграционные тесты префабов.

3. **Риск:** избыточная гибкость (сложно для новичка).  
   **Митигировать:** базовый режим UI + advanced foldout.

4. **Риск:** деградация производительности editor.  
   **Митигировать:** кэш реестра + lazy-обновление.

---

## Критерии готовности (Definition of Done)

- Все существующие демо и сцены работают без изменений.
- Новый editor-пайплайн покрыт минимум smoke-тестами и ручным чек-листом.
- Время открытия инспектора не ухудшилось заметно.
- Пользователь может настроить condition быстрее за счет поиска/групп.
- Документация обновлена:
  - `Docs/Condition/NeoCondition.md`
  - changelog
  - примеры использования provider режима (после Phase 4).

---

## Минимальный MVP для старта (рекомендуется)

Сделать в первую очередь:
1. Атрибут `ConditionValueAttribute`.
2. `ConditionMemberRegistry`.
3. Подключение registry в текущий `NeoConditionEditor` без изменения runtime-модели.

Это даст максимальную пользу с минимальным риском и заложит фундамент для всех следующих этапов.
