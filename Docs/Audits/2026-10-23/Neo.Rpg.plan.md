# Neo.Rpg: индивидуальный план реализации

Основание: [аудит](Neo.Rpg.md), manifest, PLAN-CONTRACT.md. Фаза 2 изменяет только этот план.

Пути ниже: `R = Assets/Neoxider/Scripts/Rpg/`, `D = Assets/Neoxider/Docs/Rpg/`,
`E = Assets/Neoxider/Tests/Edit/RPG/`, `P = Assets/Neoxider/Tests/Play/RPG/`.
Владелец изменений R/D и новых тестов с префиксом `Audit20261023NeoRpg` — Neo.Rpg.

## Текущий результат

Проверены все 59 C# файлов. Уже исправлены R01–R20: профили/стаки, upgrades/regen,
cast/evade lifecycle, bindings, targeting, множители/revival, ограничения/ID, aura,
неактивные цели, callbacks эффектов и понижение роста.
Изменены 19 source-файлов и 11 руководств; добавлены 32 тестовых метода / 42 случая.

Статические проверки аудита прошли; Unity-компиляция и выполнение тестов не проводились.
При планировании текущий SHA-256 агрегат 59 файлов совпал с квитанцией аудита
`50af8cdb70066794a47151cae945cbb57267999278f78edc71f04ba760216e7a`.

## Подтверждённый остаток и приоритеты

Подтверждение ниже — анализ кода; runtime-воспроизведения не запускались.

| Приоритет / владелец | Доказательство и минимальное изменение | Наблюдаемая приёмка |
| --- | --- | --- |
| P1 / Neo.Rpg | `R/Runtime/RpgCharacterResourceService.cs:RestoreAll, TickRegen, RefreshDerived` обходят живой Dictionary. Callback, вызывающий `Build` с новыми ресурсами, инвалидирует обход; reactive-listeners/resolvers тоже могут заменить состояние. Использовать поколение Build/Clear и прекращать устаревший проход после внешних вызовов. Образцы уже есть в `RpgEffectShelf` и Core. | Callback заменяет A/B ресурсом C=7: C остаётся 7, старые записи больше не получают обновлений/событий, исключения обхода нет. |
| P1 / Neo.Rpg | `RpgCharacterResourceService.TickRegen(+Infinity, false)` с tick-regen получает бесконечный accumulator: вычитание интервала не уменьшает Infinity. Добавить finite-проверки delta/интервала/accumulator, используя подход Core. | NaN/Infinity не запускают цикл, не меняют Current и не публикуют resource events; обычные интервалы и pause-контракт сохраняются. |
| P2 / Neo.Rpg | `R/Components/RpgCharacter.cs:SetMaxResource, CaptureProfile, ApplyProfile` и `R/Runtime/RpgStatResolver.cs:ResolveResourceMax`: Max=160 при BaseMax=100 возвращается к 100 после refresh/load. Исправление требует разделения базы и результата, см. предложение A. | Сохранённая база и временные бонусы переживают load без потери или повторного начисления. |
| P2 / Neo.Rpg | `RpgCharacter.SetLevel/RefreshStat` и `RpgStatResolver.ResolveStat`: manual/deferred рост не применяется при SetLevel, но применяется после постороннего ApplyBuff/refresh. Ввести applied-growth level и явный `ApplyLevelGrowth()`; resolver сохранить с прежней сигнатурой. | Wisdom=10, growth +2/level, manual level 3: пустой buff не превращает 10 в 14. Deferred auto-growth применяется один раз явным вызовом. |
| P2 / Neo.Rpg | `R/Enums/BuffStatType.cs` объявляет `AddResourceCurrentFlat`, `AttackSpeedPercent`; фасад их не потребляет. Legacy `HpRegenPerSecond` учитывается отдельным `RpgCombatMath`, но не текущим resolver. Подключить потребителей в `RpgCharacter.cs`, `RpgStatResolver.cs`, `RpgEffectShelf.cs`, `RpgAttackController.cs`. | Legacy HP regen учитывает stacks при включённом regen; current-бонус не повторяется при refresh/load; attack-speed изменяет следующий cooldown, сохраняя уже запущенный. |

Для безопасных исправлений создать `E/Audit20261023NeoRpgRemainingTests.cs`.
Для неполных modifiers сначала зафиксировать семантику: предлагается current-бонус только при
новом эффекте/реальном приросте stacks; refresh на cap ничего не выдаёт. Это проектное решение,
а не уже реализованный контракт. Добавить `RpgCharacter.GetAttackSpeedMultiplier()`;
`IRpgCombatReceiver` не расширять обязательным методом, прочие receivers сохраняют multiplier=1.

## Долг проверки

Координатор после завершения source-аудитов запускает точные fixtures
`Audit20261023NeoRpgRuntimeTests`, `Audit20261023NeoRpgCharacterTests`,
`Audit20261023NeoRpgLifecycleTests`, затем существующие `RpgCharacterTests`,
`RpgCharacterResourceServiceTests`, `RpgCharacterProfileServiceTests`, `RpgCombatEdgeTests`,
`RpgCombatPlayModeTests`, `RpgCombatTargetingPlayModeTests`.

В `P/Audit20261023NeoRpgIntegrationTests.cs` дополнить фактические пробелы: custom receiver,
2D hurtboxes, внешний origin, detach после уничтожения AuraWeapon, отключение/re-enable bindings.
Считать HP/Mana, количество событий, оставшиеся locks и значения UI; одного «не бросает» недостаточно.
Сортировка/truncation projectile buffer, огромный конечный deltaTime, integer overflow и смены
template/provider остаются рисками: сначала воспроизведение, затем отдельная коррекция.

## Существенное улучшение A: сохраняемая база ресурсов

Проблема разработчика: постоянный апгрейд Max и временный buff сейчас записываются в один результат.
Повторно использовать `RpgResourceRuntime.BaseMax`, resolver, resource/profile services, Reactive и
SaveProvider. `Neo.Core.Resources.ResourcePoolModel` изучен: его case-insensitive ID, ограничения
overfill/below-zero и отсутствие buff/base слоя исключают прямую замену текущей модели.

Публичный API: service `SetBaseMax(string, float)` / `AddBaseMax(string, float)` возвращает bool;
facade `SetResourceBaseMax(string, float)` / `AddResourceBaseMax(string, float)` маршрутизирует команды.
Добавить `RpgResourceSaveEntry.HasBaseMax/BaseMax`, профиль v3 и миграцию **до** Sanitize:
сейчас Sanitize безусловно переписывает Version. V2 без базы использует authored base;
сохранённый final Max нельзя автоматически объявлять базой при активных modifiers.
Старые live-Max setters и существующие serialized actions не перенаправлять молча.

Точные файлы: `R/Runtime/RpgResourceRuntime.cs`, `RpgCharacterResourceService.cs`,
`RpgCharacterProfileData.cs`, `RpgCharacterProfileService.cs`, `RpgCharacterNetworkCommand.cs`;
`R/Components/RpgCharacter.cs`; `R/Bridge/RpgNoCodeAction.cs`, `RpgNoCodeActionType.cs`.
Enum-значения дописывать; поля/версии старых assets не переименовывать.
Добавить `E/Audit20261023NeoRpgBaseMaxTests.cs`.

Минимальная приёмка: base 160 + buff 20 = 180 до/после serialize/load; удаление buff даёт 160;
повторный ApplyProfile не удваивает бонус; percent-regen использует итоговый Max; общий template
не изменяется. V2 golden-payload и нулевая база проверяются отдельно. Оценка: 2–3 дня, риск миграции средний/высокий.

## Существенное улучшение B: транзакции и явные часы

Отдельное предложение, не prerequisite для P1: создать `R/Runtime/RpgCharacterState.cs`,
компонуя существующие services; `R/Runtime/RpgTickContext.cs` с simulation delta/UTC now;
`R/Data/RpgEffectClockMode.cs`. Изменить `R/Runtime/RpgEffectShelf.cs`,
`RpgActiveEffectEntries.cs`, `RpgResourceRuntime.cs`, `RpgStatRuntime.cs`, `RpgCharacterResourceService.cs`,
`RpgCharacterProfileData.cs`, `RpgCharacterProfileService.cs`, `R/Components/RpgCharacter.cs`,
`R/Data/BuffDefinition.cs`, `InlineBuffEntry.cs`, `StatusEffectDefinition.cs`.

API: `RpgCharacterState.Tick(RpgTickContext)`, `ApplyProfile(...)`, `ApplyLevelGrowth()`;
добавочный overload shelf Tick сохраняет исходный float-overload. Батч меняет/вычисляет состояние
без промежуточных reactive notifications, затем публикует события, `StateCommitted` и один snapshot.
Профиль отдельно версионирует applied-growth level и remaining simulation duration. Существующие поля,
facade API и `RuntimeRebuilt` сохраняются; новые clock fields по умолчанию используют legacy UTC.
Simulation clock — явная опция, без scene references в SO и без нового Editor/framework слоя.

`E/Audit20261023NeoRpgStateTests.cs` проверяет одинаковый результат фиксированных часов,
паузы simulation/legacy UTC, удаление эффекта callback-ом, отсутствие частичного профиля
в событиях, applied growth и один snapshot на батч. Оценка: 4–6 дней;
риск изменения порядка событий и сериализации средний/высокий.

## Порядок, владельцы и условия завершения

1. После общего source freeze координатор фиксирует ревизию и выполняет долг проверки.
   Затем Neo.Rpg вносит P1 guards с регрессиями; новые подтверждённые сбои исправляются до API-расширений.
2. Согласовать правила роста/modifiers и v2 миграцию; реализовать узкие исправления, затем A.
   Neo.Rpg сначала публикует additive command/profile контракт; владелец **Neo.Rpg.Network** затем
   меняет `Scripts/RpgNetwork/RpgCharacterNetworkAdapter.cs` и проверяет v2/v3, late join,
   rejection до стоимости через `RpgAttackNetworkAdapter.cs`. Transport не переносить в Neo.Rpg.
3. B реализовывать отдельно. Владелец **Neo.Core** предоставляет текущий finite/identity-подход;
   его source не менять. **Neo.Core.Level.Components** проверяет provider lifecycle;
   **Neo.Rpg.Combat** — `Combat/RpgStatsDamageableBridge.cs`, **Neo.Rpg.ConditionBridge** —
   `Bridge/Condition/RpgConditionAdapter.cs` после фиксации facade-контрактов.
4. Обновить `D/README.md`, `RpgCharacter.md`, `InternalTypes.md`, `RpgNoCodeAction.md`,
   `RpgProgressionDefinition.md`, `Data/BuffDefinition.md`, `Data/StatusEffectDefinition.md`:
   C# примеры, units/migration и Inspector/NoCode эквиваленты. Новым scripts дать уникальные .meta;
   sample layout `Samples~` не менять, помнить developer `Samples` и импортированный root.
5. Координатор компилирует Unity 6.0–6.4 и 6.5+, соответствующие ветки MIRROR/без MIRROR;
   запускает указанные EditMode/PlayMode проверки, проверяет package/sample paths и отсутствие
   обязательной network-зависимости. Версионные ID API остаются в централизованных compatibility helpers.

Готовность: значимые asserts проходят, обе Unity-линии компилируются, сохранены serialized/public
контракты и diff/UTF-8/meta проверки. До этого compilation/runtime остаются unverified.
Неудачная приёмка останавливает соответствующую ветку. Решения legacy Max/v2, one-shot refresh и часов
фиксирует координатор; они не блокируют безопасные P1 исправления. Коммиты, Unity и тестовые процессы
в текущей фазе не запускались.
