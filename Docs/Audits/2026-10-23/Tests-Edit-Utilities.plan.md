# Tests-Edit-Utilities — индивидуальный план реализации

Дата: 2026-10-23. Продолжение исходной сессии аудитора. План сверён с текущим кодом; исторический аудит не изменён. Ниже `T` = `Assets/Neoxider/Tests/Edit`, `S` = `Assets/Neoxider/Scripts`, `E` = `Assets/Neoxider/Editor`, `D` = `Assets/Neoxider/Docs`.

## Выполнено и актуализировано

Моя работа: просмотрены 67 назначенных файлов; добавлены 22 метода / 43 случая в трёх файлах:

- `T/Extensions/Audit20261023TestUtilitiesEnumerableTests.cs`;
- `T/Extensions/Audit20261023TestUtilitiesSerializationTests.cs`;
- `T/Tools/Audit20261023TestUtilitiesSceneContractsTests.cs`.

Добавлены metadata и `D/Testing/Utilities.md`: C# контракты, scene/NoCode пути, очистка, skips, sample layouts. F07 закрыт дополнительными проверками dispatch/disposal, копирования, UTC ticks, JSON, точного TMP текста/events и реального поворота. Это подготовленные проверки: Unity-компиляция и выполнение отсутствуют; успешны только статические проверки, UTF-8/GUID/whitespace и scoped `git diff --check`.

Повторное чтение показало изменения других владельцев, которые **не следует реализовывать повторно**:

| Замечание | Текущее исправление; остаётся проверка |
| --- | --- |
| F01 | `T/Tools/InteractiveObjectTests.cs` хранит `_cameraObject` и уничтожает эту ссылку. |
| F02 | `T/Editor/SampleAssetValidationTests.cs`, `SampleSceneCoverageTests.cs` используют `Audit20261023NeoEditorTestsPreviewSceneScope`; открытия `Single` заменены preview-сценами. |
| F03 | `T/Tools/Inventory/InventorySystemTests.cs`, `T/Tools/SelectorTests.cs` используют `T/Save/Audit20261023NeoEditorTestsSaveScope.cs`; Inventory восстанавливает provider в `finally` после объектов. |
| F04, основная утечка | В `T/Audio/AmChannelEditModeTests.cs`, `AmEditModeTests.cs`, `AmEntryEditModeTests.cs`, `RandomMusicControllerEditModeTests.cs` клипы зарегистрированы и уничтожаются teardown; probe тоже использует фабрику. |
| F10 | Оба missing-script пути `SampleSceneCoverageTests.cs` вызывают рекурсивный обход из `SampleAssetValidationTests.cs`. |
| R01 | `S/Extensions/RandomExtensions.cs:223` проверяет finite-веса и суммирует в `double`; `T/Extensions/Audit20261023NumericRandomAndPrimitiveTests.cs` содержит проверки overflow/NaN/infinity. |

Эти изменения подтверждены чтением, а не запуском. Уже существует `T/Editor/Audit20261023NeoEditorTestsIsolationTests.cs` с проверками камеры, клипов, provider, preview-сцен и inactive descendants: использовать его при центральной проверке.

## Оставшиеся исправления

Все существующие тесты принадлежат **Neo.Editor.Tests**; supplemental-владение не разрешает мне менять их самостоятельно. Указанные владельцы согласуют последовательность с координатором, без изменения чужих файлов в фазе планирования.

| Приоритет / evidence | Минимальная коррекция и наблюдаемая приёмка |
| --- | --- |
| **P2 F05:** `T/Extensions/ExtensionsTests.cs:768–780` удаляет фиксированные ключи и вызывает `PlayerPrefs.Save`; `T/Tools/TimerTests.cs` удаляет фиксированные save-ключи. | Уникальные ключи либо snapshot значения/существования; Timer использует существующий SaveScope. Предварительно записанные sentinel-значения сохраняются после успеха и исключения. |
| **P2 F05:** `T/Tools/CursorLockPlayerControlTests.cs:24,34`, `T/UI/AuditFixesAnimationFlyTests.cs:23`, `T/Audio/AmChannelEditModeTests.cs:52` принудительно задают/сбрасывают глобальное состояние. | Захватить и восстановить изменяемые cursor/diagnostics/RNG настройки; не подменять восстановление defaults. Нестандартные начальные значения остаются прежними, включая failure/skip. Использовать публичные configure/property API. |
| **P2 F06:** `T/PropertyAttribute/NeoInspectorAnimationTests.cs:188` продвигает общий ticker на 60 секунд; `T/UI/UIMeshRigEditorTests.cs` вызывает `StopAllPreviews`. | Проверять отдельную lease без изменения чужих клиентов; **Neo.Editor** при необходимости выделяет internal predicate в `E/PropertyAttribute/CustomEditorBase.cs`, затем test owner обновляет тест. **Neo.UI.Editor** предоставляет существующий адресный stop-preview путь. Чужой клиент/preview сохраняется; общие timestamps не уходят в будущее. |
| **P2 F04, остаток:** `T/Audio/RandomMusicControllerEditModeTests.cs` останавливает controller после assertions; `T/UI/UIMeshRigEditorTests.cs` освобождает sprite/texture после assertions. | `Stop`/освобождение в `finally`; аналогично failure-cleanup в `T/Extensions/UnityExtensionsTests.cs`, `T/Tools/Move/TransformMoveTests.cs`, `T/Tools/NeoConditionTests.cs`. Исключение между созданием и проверкой не оставляет ресурс/работающий controller. |
| **P2 F08:** `T/Audio/AmEditModeTests.cs:294,315,336` пропускает тест по `AM.Ambience.isPlaying`; `T/PropertyAttribute/InspectorControlIdStabilityTests.cs:159–161` допускает отсутствие repaint-метода. | Независимый capability probe; state assertions выполнять всегда; отсутствие отражаемого метода проверять отдельно. При работоспособном native probe поломка AM даёт failure. Зависимости: **Neo.Audio**, **Neo.Editor**, без обязательного изменения их API. |
| **P2 F09:** фиксированные roots / пустые проходы в `T/Editor/SampleAssetValidationTests.cs`, `SampleSceneCoverageTests.cs`, `T/Tools/InteractionQueryMathTests.cs`, `MoverPrefabColliderOffsetTests.cs`, `T/PropertyAttribute/InspectorControlIdStabilityTests.cs`. | Общий test-only resolver package/sample root, явная версия импортированных samples, число проверенных объектов. Hidden release проверяется как packaging data; runtime-инспекция требует импортированных assets. Нет тихого успеха при нулевой фактической проверке. |
| **P3 F11:** `T/Tools/NeoConditionThrottleTests.cs:57` вызывает `update?.Invoke`; `AuditFixesTypewriterTests.cs` игнорирует async результат; `TimerTests.cs` проверяет dispose idle-таймера; `AuditFixesAttackSystemTests.cs` вызывает lifecycle напрямую. | Assert наличия reflection seam и положительный контроль throttle; typed await и finally-stop; проверка cancellation работающего таймера/реального disable. **Neo.Condition**, **Neo.Tools.Components**, **Neo.Tools.Time**, **Neo.Rpg.Combat** уточняют runtime контракты, **Neo.Tests.Play** владеет Play Mode интеграцией. `UniTask` уже указан в asmdef: новый reference не нужен. |

Это подтверждённые ограничения тестов. Физический input, DOTween manual updates, сохранность Undo и поведение native объектов после исключений остаются рисками исполнения; новых runtime-дефектов из них не утверждается.

## Существенное улучшение: компонуемая изоляция тестов

**Предложение, не реализация.** Разработчик должен запускать utility-тест без потери текущего scene/provider/настроек. Повторяющиеся ручные teardown заменить небольшими test-only leases. Повторно использовать существующие SaveScope, PreviewSceneScope, IsolationTests и публичные `SaveProvider.SetProvider`, `NeoDiagnostics.Configure`, `Random.state`; новый runtime manager не требуется.

Точные новые файлы, владелец **Neo.Editor.Tests**:

- `T/Editor/Audit20261023NeoEditorTestsUtilityScope.cs`: internal `Own<T>(T resource)`, `CreatePreferenceKey(string purpose)`, `UseRandomSeed(int seed)`, `UseCulture(CultureInfo culture)`, composition существующих save/preview scopes.
- `T/Editor/Audit20261023NeoEditorTestsUtilityCapabilities.cs`: независимые probes, результат `Supported` + `Reason`; probe не вызывает SUT.
- `T/Editor/Audit20261023NeoEditorTestsSampleRoots.cs`: явное разрешение package/version/sample identity и диагностика discovery counts.
- `T/Editor/Audit20261023TestUtilitiesEnvironmentTests.cs`: новые проверки failure-cleanup и восстановления состояния, дополняющие существующие IsolationTests.

`NeoxiderEditorAssets.FindAssetPath` уже ищет Assets/Packages, но возвращает первый match: это кандидат поиска anchor, не достаточная политика выбора версии. Resolver обязан проверить принадлежность текущему пакету; при неоднозначности указать кандидатов.

Порядок disposal: остановить принадлежащие fixture операции, уничтожить принадлежащие GameObjects, освободить отдельные native assets, восстановить provider/settings. Продолжать cleanup после ошибки одного lease, сообщать совокупность ошибок. Не сохранять/сбрасывать чужие singletons через универсальную reflection-очистку.

Production public C# API, serialized fields и assets сохраняются. Scene/NoCode авторинг не меняется: тесты вызывают те же typed APIs. Документация: дополнить `D/Testing/Utilities.md`; владелец основного `D/Testing.md` добавляет ссылку — entry теперь существует. Координатор создаёт отсутствующий `D/Testing.meta`. Оценка: 2–4 дня на leases и пилот, ещё 2–4 на миграцию/capabilities/layouts; риски — callbacks при destruction, provider restoration, Unity preview/Undo и параллельные editor updates.

## Порядок реализации и завершение

1. Сверить изменения владельцев; закрытые F01/F02/F03/F04/F10/R01 оставить в verification debt. Сначала локальные finally/snapshots/ключи и положительные assertions, затем internal lease seam.
2. Реализовать scope минимально, мигрировать Audio/Extensions/Cursor пилот; добавить probes/resolver и регрессии. Полную миграцию остальных fixtures делать постепенно; не требовать framework для простого безопасного исправления.
3. После завершения исходных аудитов координатор запускает 43 supplemental случая, существующие IsolationTests/numeric regressions, затем затронутые Edit Mode suites. Для cancellation, disable/destroy и NoCode binding dispatch — отдельный Play Mode прогон с владельцами соответствующих сборок.
4. Приёмка: sentinel camera/dirty additive и untitled scenes/selection/provider/preferences переживают успех, failure и skip; созданные ресурсы исчезают; повторный запуск и перестановка fixtures не меняют результаты. Audio/IMGUI доступны — интеграция обязательна; недоступны — явный skip только capability-зависимых случаев.
5. Проверить development `Samples`, release `Samples~`, UPM `Packages/com.neoxider.tools`, импорт `Assets/Samples/NeoxiderTools/10.19.0/Demo Scenes` и две импортированные версии. Layout не переименовывать ради теста.
6. Обе ветви Unity 6.0–6.4 и 6.5+ должны компилироваться соответствующими компиляторами. Identity API использовать только через `NeoDiagnostics.StableId` / `NeoComponentHealth.StableId`. Записать версии, filters, pass/fail/skip; статический просмотр этого не заменяет.

Завершение: исправления подтверждены observable assertions, оба compile-контура проверены, skips объяснены, docs/metadata согласованы. Недоступная Unity-версия или graphics/audio среда оставляет явный долг, не статус «пройдено». Нужен выбор поддерживаемых CI capabilities и политики явной версии samples; безопасные локальные исправления от этого выбора не зависят. В фазе 2 изменён только этот план; код, тесты, public docs и аудит не редактировались, процессы Unity/build/test и дочерние агенты не запускались.
