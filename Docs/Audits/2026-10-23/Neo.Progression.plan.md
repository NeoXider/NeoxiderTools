# Neo.Progression — индивидуальный план реализации

План второй фазы, без реализации. Основание: `PLAN-CONTRACT.md`, завершённый аудит, manifest и текущий код.

Пути: **S** = `Assets/Neoxider/Scripts/Progression/`, **T** = `Assets/Neoxider/Tests/Edit/Progression/`, **D** = `Assets/Neoxider/Docs/Progression/`. Внешние source-пути относительны `Assets/Neoxider/Scripts/`; другие сборки остаются у своих владельцев.

## 1. Выполнено в аудите

Проверены все 13 C# файлов. Исправлены F1–F11: переполнение points, premium replay при XP/save callbacks, повторное назначение провайдера, состояние при его замене, null-списки/клонирование, ошибки NoCode и ленивой инициализации, отрицательный XP legacy-helper. Сохранены serialized fields, enum values и профиль V2; добавлен `TryAddXp`. Обновлены девять существующих публичных руководств. Новая `T/Audit20261023NeoProgressionRegressionTests.cs` содержит 16 методов/22 сценария.

Пройдены статические проверки: UTF-8, delimiters/явные типы, serialized field sets, 23 ссылки, GUID, `git diff --check`. Unity/тесты/PlayMode **не запускались**. F1–F11 не являются оставшимися исправлениями.

## 2. Приоритеты и оставшиеся исправления

### P0 — закрыть долг проверки

Владелец: координатор после завершения конкурентных source-аудитов. Скомпилировать актуальные `Neo.Progression` и зависимости; выполнить 22 новых сценария и существующий `ProgressionManagerTests`. Проверить точные результаты: максимум points, premium-итог 12, неизменные 5 points после повторного назначения провайдера, правильные события отказа. Любой провал сначала воспроизвести и исправлять узко; статический успех не заменяет выполнение.

### P1 — watermark теряется при загрузке того же профиля

**Подтверждено исходником при планировании, не запуском.** `S/Components/ProgressionManager.cs:609` вызывает загрузку, а `ApplyProfile` на строке 696 безусловно присваивает `_lastRewardedLevel = CurrentLevel`. При линейной Core-кривой 100 XP и наградах уровней 2/3 по 2/3 points: `SetLevel(3)` → 5; `SetLevel(1)` → `LoadProfile()` того же сохранённого ключа → `SetLevel(3)` → 10. Это остаточный путь повторной выдачи, отдельный от исправленного F3.

Владелец: Neo.Progression. Минимум: явно разделить обычный reload того же активного ключа, смену профиля и `ResetProgression`. Для того же ключа сохранять максимум прежнего watermark и входящего уровня; reset/новый профиль устанавливают новую исходную отметку. Идентичность активного ключа хранить отдельно в runtime; простая смена свойства `SaveKey` ещё не загружает профиль. Публичные сигнатуры и V2 JSON не менять.

Файлы: `S/Components/ProgressionManager.cs`, новая `T/Audit20261023NeoProgressionProfileReloadTests.cs`, `D/ProgressionManager.md`. Приёмка: reload не увеличивает 5 points; повторные reload не меняют результат; явный reset позволяет заново получить 5; другой ключ не наследует watermark предыдущего профиля. Это исправление защищает текущую сессию; устойчивость после перезапуска требует отдельной ledger-политики.

### P2 — синхронизация прямых изменений Core

Подтверждённое документированное ограничение: manager подписан только на `OnLevelUp` (`:648`), а ветка `level <= watermark` возвращается (`:767`). После внешнего `provider.AddXp(25)` getter показывает 25, но `XpState` остаётся 0. Аналогичны снижение уровня, `Load()` и `ResetProgress()`.

Владелец: Neo.Progression; интерфейсную стабильность подтвердить с Neo.Core.Level.Components/Neo.Reactive. Использовать уже существующий `ReactivePropertyInt.Subscribe`, возвращающий `IDisposable`, для Core XP/level/remaining-XP. Отделить обновление зеркала от выдачи наград; сравнивать снимки, объединять уведомления собственных операций, освобождать подписки при замене/destroy. Одних `OnXpGained`/`OnProfileLoaded` недостаточно: Core reset уведомляет reactive-поля.

Файлы: тот же manager, новая `T/Audit20261023NeoProgressionProviderSyncTests.cs`, `D/ProgressionManager.md`. Приёмка: внешний XP 25 даёт XP/level/remaining = 25/1/75; load/reset/понижение отражаются без новых наград и дополнительных сохранений Progression; старый провайдер после detach не уведомляет manager. Изменять Core или вводить polling не требуется.

## 3. Существенное улучшение — самостоятельный runtime и доставка наград

**Предложение, не подтверждённый новый дефект.** Сейчас manager объединяет правила, singleton, UI, сохранение и сервисы. SO-награды содержат scene-target поля; dispatcher возвращает `void`, пропуская отсутствующие сервисы. Покупка уже списывает points (`:541`), premium уже сохраняет entitlement (`:588`). Неатомарность подтверждена; сценарий потери после аварии не исполнялся. Стоимость прохода по каждому числовому уровню и глубокой XP-рекурсии пока не измерена.

**Этап A:** внутри существующей Neo.Progression добавить обычные C# классы без GameObject-зависимостей:

- `S/Runtime/ProgressionCatalog.cs` и `ProgressionCatalogBuilder.cs`: неизменяемые ordinal ID maps и индекс reward levels из существующих assets/validators; требования предоставляются адаптером существующих `ConditionEntry`, новая condition-система не нужна.
- `S/Runtime/ProgressionSession.cs`, `ProgressionSnapshot.cs`, `ProgressionOperationResult.cs`, `ProgressionRewardCommand.cs`: профиль, правила и очередь переходов. Предлагаемый API: `TryUnlockNode(string)`, `TryBuyPerk(string)`, `ApplyLevel(int)`, `ActivatePremium(int achievedLevel)`, `GetSnapshot()`. Операции возвращают typed failure, изменения состояния и команды наград.
- Существующие manager и оба Bridge-компонента делегируют ядру; строки ошибок/UnityEvents сохраняются. Core остаётся источником XP; сохраняем существующий `ILevelProvider`, не создаём параллельную level-модель.

**Этап B:** `S/Runtime/IProgressionRewardSink.cs` с `TryApply(ProgressionRewardCommand, out ProgressionRewardFailure)` и `S/Components/ProgressionRewardTargets.cs` для явных сценовых Money/Collection/Quest bindings. Адаптер переиспользует текущие `Money.Add`, `Collection.AddItem`, `QuestManager.AcceptQuest`; существующий dispatcher остаётся совместимым facade. Пропуск сервиса становится typed result.

**Отдельный этап C:** после выбора политики повторной доставки — claim/pending ledger, стабильные track/reward IDs, V3 migration. Точные дополнительные файлы: `S/Runtime/ProgressionProfileData.cs`, `S/Runtime/ProgressionClaimData.cs`, `S/Data/ProgressionReward.cs`, `S/Data/LevelCurveDefinition.cs`. Переупорядочивание наград не должно менять claim IDs. Без идемпотентности получателей нельзя обещать exactly-once при аварии между выдачей и сохранением; локальный ledger один этого не гарантирует.

Минимальные новые тесты: `T/Audit20261023NeoProgressionSessionTests.cs` — два независимых профиля на одном каталоге, typed failures без изменений состояния, sparse levels 2/1 000 000, точные результаты цепочки XP; `T/Audit20261023NeoProgressionDeliveryTests.cs` — отсутствующий сервис, retry, повторный claim, миграция V2 без повторной выдачи. Оценка: A — 4–6 дней; B/C — ещё 3–5 при доступных контрактах получателей. Риск A умеренный, C высокий; не объединять с P1 одним изменением.

## 4. Зависимости, совместимость и порядок

1. P0, затем P1 с регрессиями; далее P2 и проверка lifecycle. После этого A → B; C отдельно после согласования профиля/claim identity и retry-политики.
2. Владельцы зависимостей: Neo.Core — `Core/Level/Interfaces/ILevelProvider.cs`, `Domain/LevelModel.cs`; Neo.Core.Level.Components — `Core/Level/Components/LevelComponent.cs`; Neo.Reactive — `Reactive/ReactiveProperty.cs`; Neo.Condition — `Condition/ConditionEntry.cs`. Их изменять только соответствующим владельцам, если адаптеру действительно не хватает текущего API.
3. До C согласовать delivery с Neo.Save (`Save/SaveProvider.cs`), Neo.Shop (`Shop/Money.cs`), Neo.Bonus (`Bonus/Collection/Collection.cs`), Neo.Quest (`Quest/QuestManager.cs`). Сначала контракты/тесты получателей, затем интеграция. Neo.Tools.Managers остаётся владельцем `Singleton<T>`; новые sessions от него независимы.
4. Neo.Editor при необходимости расширяет только существующие `Assets/Neoxider/Editor/Progression/{LevelCurveDefinitionEditor,PerkTreeDefinitionEditor,UnlockTreeDefinitionEditor}.cs` для диагностики/предпросмотра migration. Перегенерация сцен не нужна.
5. Сохранить имена serialized fields, публичные manager API, asset GUID, enum ordering и V2 mapping. Scene references legacy-полей не удалять автоматически. Новым package-файлам нужны уникальные `.meta`; shared asmdef меняет координатор только при доказанной необходимости.
6. Обновить `D/{README,ProgressionManager,ProgressionConditionAdapter,ProgressionNoCodeAction,InternalTypes,Scenarios}.md` и `D/Data/{LevelCurveDefinition,PerkTreeDefinition,UnlockTreeDefinition}.md`: C# session API, компоненты/NoCode, русский/английский без второго канонического entry point. Layout сохранить: release `Samples~`, dev `Samples`, импорт `Assets/Samples/NeoxiderTools/<version>/<sample>`; проверки учитывают все варианты.

## 5. Приёмка и условия остановки

Координатор фиксирует revision проверяемого кода и запускает минимальные EditMode-наборы после завершения writers. В PlayMode проверить реальные callbacks, provider swap/destroy, NoCode failure и UI reactive binding. Затем компиляция в Unity 6.0–6.4 и отдельно 6.5+; неподключённая ветка не считается проверенной. Не добавлять локальные version `#if`: ID-совместимость остаётся в `NeoDiagnostics.StableId`/`NeoComponentHealth.StableId`.

При регрессии баланса, повторной награде или несовместимой сериализации остановить соответствующий этап и исправить узкий воспроизводимый сценарий. C не начинать с невыбранной migration/idempotency-политикой; это продуктовое решение, не повод задерживать P1/P2. Финализация требует scoped diff/UTF-8/GUID/doc-link проверки и проверки активного sample layout координатором. До реального запуска compile/tests остаются непроверенными.

В этой фазе создаётся только данный план; runtime, тесты, публичные docs, завершённый аудит и конфигурация не изменяются.
