# Neo.Tests.Play — индивидуальный план реализации

Фаза 2, 2026-10-23. Основание: [завершённый аудит](Neo.Tests.Play.md), PLAN-CONTRACT.md и актуальная запись manifest.json. `P` ниже означает `Assets/Neoxider/Tests/Play/`. Это план; перечисленные дальнейшие изменения ещё не реализованы.

## Текущее состояние

Проверены 28/28 исходных C# файлов; исправлены 22. Уже выполнены F1–F7: изоляция saves и восстановление provider после уничтожения объектов, сохранение чужих объектов/EventSystem, ограниченные ожидания, приоритет видимых samples, invariant float parsing, реальные проверки потери HP, очистка clips/templates/player prefabs. Повторять эти исправления как будущую работу не требуется.

Добавлены три файла `P/Audit20261023NeoTestsPlayFixtureIsolationTests.cs`, `P/Audit20261023NeoTestsPlayNetworkWaitTests.cs`, `P/Audit20261023NeoTestsPlaySampleRootTests.cs`: 17 cases. Создан `Assets/Neoxider/Docs/PlayModeTesting.md`. В аудите зафиксированы успешные статические проверки 31 C# файла, UTF-8, GUID и scoped diff; Unity, компиляция и runners не запускались. Новых подтверждённых неисправленных runtime-дефектов в этом контракте нет.

При планировании повторно прочитаны актуальные regression tests, cleanup/provider/wait реализации, sample resolver, asmdef, публичная документация и релевантные Save/Singleton/Input границы. Чужие параллельные изменения не отменяются.

## Приоритеты и проверяемые результаты

| Приоритет / статус | Доказательство и минимальное действие | Владелец / результат |
| --- | --- | --- |
| P1: долг верификации | Все 17 cases пока только исходники. После завершения source-changing аудитов скомпилировать текущую ревизию, выполнить их и затронутые fixtures; исправлять только воспроизведённые сбои. | Координатор запускает; Neo.Tests.Play исправляет harness, владелец runtime-модуля — его поведение. Чужой provider/объекты сохраняются, owned ресурсы освобождаются, timeout действительно падает при pause. |
| P2: неподтверждённый timing-риск | `P/RPG/RpgCombatTargetingPlayModeTests.cs` использует ожидания 0.05/0.3 секунды; `P/Bonus/BonusPlayModeTests.cs`, `P/UI/AnimationFlyPlayModeTests.cs` также проверяют короткие интервалы. Проверить на медленном worker; менять только нестабильный сценарий на наблюдаемое событие с real-time deadline. | Neo.Tests.Play; сохраняются точные HP/event/pose assertions, отсутствие исключения не становится критерием успеха. |
| P2: подтверждённый пробел покрытия | `P/Core/InputPlayModeTests.cs` проверяет создание; `P/Network/PhysicsEventsNetworkTests.cs` вызывает callback reflection; DummyTransport в `P/Network/NetworkSingletonTests.cs` не передаёт remote packets. | Neo.Tests.Play совместно с Neo.Tools.Input, Neo.Tools.InteractableObject, Neo.Network; реальные key events, collider enter/exit counts и отдельный peer проверяются отдельными сценариями. Это не доказанные runtime-баги. |
| P2: неподтверждённая изоляция сцен | `P/SampleRuntimeSceneSmokeTests.cs` загружает Single; `P/Network/MultSceneNetworkTests.cs` останавливает найденные managers. Проверять в выделенной сцене; заимствованные managers не считать собственностью fixture. | Neo.Tests.Play + координатор; последовательные fixtures не меняют чужую сессию/настройки. |
| P3: проектное ограничение | `FindSampleDemoRoot` использует Directory.EnumerateDirectories без выбора версии. При нескольких imports результат не гарантирует нужную версию. | Neo.Tests.Play; явный root/version даёт один воспроизводимый выбор, отсутствующий явно выбранный root сообщает ошибку. Автовыбор не объявлять сломанным без утверждённой политики. |

## Существенное улучшение: компонуемый scope тестовой среды

Проблема разработчика — ручное дублирование владения ресурсами и глобальными настройками. Сейчас Inventory/Bonus/Money зависят даже от вложенного `ShopPurchasePlayModeTests.MemorySaveProvider`. Общий scope должен устранить эту связанность без нового gameplay-менеджера.

**Reuse:** перенести существующий provider, а не писать другой storage; использовать `ISaveProvider`, `SaveProvider.CurrentProvider/SetProvider`, существующие списки owned объектов, bounded `NetworkTestHelper.WaitUntil` и DummyTransport/TestNetworkManager. `Singleton<T>.OnDestroy` уже очищает собственную instance. Поиск готового test scope в Tests не выявил общей реализации. Runtime coroutine host не заменяет assertion-producing wait.

Предлагаемый C# контракт test-support слоя:

```csharp
public T Own<T>(T value) where T : UnityEngine.Object;
public void UseTimeScale(float value);
public IEnumerator WaitFor(Func<bool> condition, TimeSpan timeout, string operation);
public IEnumerator Cleanup();
public void Dispose();
```

Сначала тип `PlayModeTestScope` остаётся internal внутри существующей сборки; gameplay API не расширяется. Отдельный save adapter регистрирует восстановление provider последней фазой. Cleanup сначала останавливает принадлежащие scope операции, уничтожает roots/components, затем native assets, затем восстанавливает заимствованное состояние в finally. Повторный вызов безопасен. Dispose выполняет синхронный fallback, а не прокручивает IEnumerator без ожидания. Чужие объекты не ищутся для массового удаления.

**Точные файлы первого этапа:** новые `P/Support/PlayModeTestScope.cs`, `P/Support/InMemorySaveProvider.cs`, `P/Support/SaveProviderTestScopeExtensions.cs`, `P/Audit20261023NeoTestsPlayScopeTests.cs`; миграция `P/ShopPurchasePlayModeTests.cs`, `P/RPG/InventoryPlayModeTests.cs`, `P/Bonus/BonusPlayModeTests.cs`, `P/AmTeardownPlayModeTests.cs`, `P/Network/MoneyNetworkTests.cs`. Обновить существующий FixtureIsolation regression file, сохранив наблюдаемые контракты.

Следующий адаптер: новый `P/Network/MirrorHostTestScope.cs`, существующие `P/Network/NetworkTestHelper.cs`, `P/Network/NetworkEventDispatcherTests.cs`, NetworkWait regression file. Он владеет manager/transport/template, ограничивает shutdown, восстанавливает Transport.active и изменённые targetFrameRate/vSync/runInBackground; чужую сессию не перехватывает. Массовая миграция network fixtures следует после пилота.

Минимальные новые проверки: ошибка посередине setup; save из OnDisable; повторный cleanup; отмена coroutine cleanup; pause; сохранение чужого объекта; одинаковое состояние после двух последовательных запусков. Проверять ссылки/значения/количество owned объектов, а не приватное устройство scope.

## Межсборочные зависимости и совместимость

Neo.Save владеет provider contract; Neo.Tools.Managers — singleton lifecycle; Neo.Network/Neo.Network.Contracts — session/authority; Neo.Rpg/Neo.Rpg.Network — combat/adapter; Neo.Tools.Input и Neo.Tools.InteractableObject — ввод/physics. Их правки завершаются до интеграционной проверки. Helper `Tests-Play-Network` добавляет собственные tests: согласовать coverage, не редактировать их файлы.

Neo.Editor.Tests владеет `Assets/Neoxider/Tests/Edit/Editor/SampleSceneCoverageTests.cs` и `SampleAssetValidationTests.cs`: согласовать root policy без копирования очередного resolver. Возможный новый `P/Support/SampleDemoRootResolver.cs` принимает explicit root/version; callers и SampleRoot regressions обновляются совместно.

Разделение optional networking — отдельный последующий шаг координатора: `P/Neo.Tests.Play.asmdef`, новый `P/Network/Neo.Tests.Network.Play.asmdef`, перенос NetworkWait файла вместе с его meta в Network. Сначала проверить все параллельно добавленные callers и транзитивные runtime references; одного переноса Network-папки недостаточно. Межсборочно используемый support API публиковать только после определения общей test-support сборки с Neo.Editor.Tests, без циклических references.

Сохранить NUnit имена, runtime/serialized APIs, GUID существующих файлов; новые assets получают meta. Samples, Samples~ и imported `Assets/Samples/NeoxiderTools/<version>/Demo Scenes` сохраняют поддержку. Новые Editor/NoCode components не нужны. Unity-version gates остаются централизованными; для diagnostics IDs использовать существующие StableId helpers, без локальных веток версий.

## Порядок, документация и завершение

1. Координатор фиксирует интеграционную ревизию после аудитов; выполняет P1 проверки и минимальные подтверждённые коррекции с регрессиями.
2. Внедрить scope/provider API и failure-path tests; мигрировать перечисленные fixtures; затем пилот Mirror adapter.
3. Новые `P/Audit20261023NeoTestsPlayInputBehaviorTests.cs` и `P/Audit20261023NeoTestsPlayPhysicsDeliveryTests.cs`: один onKeyPressed на down, отсутствие повторов при hold, повтор после release; движение collider даёт ровно один enter/exit. MultiKeyEventTrigger не требует нового chord API. Использовать имеющийся input driver; отсутствие отметить долгом. Remote-peer harness планирует Neo.Network с координатором.
4. Обновить `Assets/Neoxider/Docs/PlayModeTesting.md`: C# scope example, cleanup phases, категории доказательств; для scene/NoCode пользователей — импорт sample и проверка UnityEvent результата. Shared index меняет координатор.
5. Централизованно: EditMode sample/asset validation; PlayMode regressions и affected fixtures; compile/import на Unity 6.0–6.4 и 6.5+, затем package test-discovery и release-layout validation. Hidden-only sample skip учитывать отдельно от pass.

Scope/provider этап оценивается в 2–3 дня; Mirror pilot и согласованное разделение — ещё 1–2. Риск средний: порядок destruction, статические listeners, чужая session и зависимости новых параллельных tests. Выбор публичной support-сборки и remote backend остаётся архитектурным решением координатора; рутинные обратимые фиксы дополнительного разрешения не требуют.

Завершение: именованные acceptance-сценарии проходят на зафиксированной ревизии; чужие saves/объекты/settings неизменны, owned ресурсы освобождены, timeout bounded, scene и remote результаты разделены. На compile error, ownership conflict или отсутствии driver зафиксировать точный repro/владельца и остановить зависимый этап; не заменять failed assert расширенным timeout. До обоих version checks совместимость не считать доказанной. В фазе 2 выполнены только чтение и запись этого плана.
