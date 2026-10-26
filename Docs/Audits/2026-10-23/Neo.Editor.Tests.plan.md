# Neo.Editor.Tests — индивидуальный план реализации

Дата: 2026-10-23. Продолжение той же сессии аудитора. Основания: `PLAN-CONTRACT.md`, актуальная запись manifest, завершённый `Neo.Editor.Tests.md` и текущие исходники. На этой стадии создаётся только данный план; реализации ниже ещё не выполнены.

## Текущий результат

Все 169 исходников совпадают с SHA-префиксами завершённого отчёта. F1–F9 уже исправлены в 23 существующих файлах: preview-сцены вместо замены открытых сцен, очистка принадлежащих фикстурам объектов, изоляция сохранений и восстановление provider, рекурсивная sample-валидация, локальный поиск Player, освобождение AudioClip/ScriptableObject. Добавлены два support-класса, 27 NUnit cases в `Editor/Audit20261023NeoEditorTestsIsolationTests.cs` и канонический `Assets/Neoxider/Docs/Testing.md`. Повторять эти исправления как незавершённые задачи не нужно.

Подтверждены только статические проверки: структура 172 C# файлов, UTF-8, GUID, 40 ссылок на методы фикстур, наличие девяти smoke-сцен и scoped diff. Unity-компиляция, discovery и выполнение тестов отсутствуют. Текущий общий asmdef уже содержит `Neo.Haptics`: это интеграционное изменение других участников, его нельзя затереть старой копией.

Далее пути тестовых файлов указаны относительно `Assets/Neoxider/Tests/Edit/`, остальные — полностью.

## Приоритеты оставшихся работ

**P1 — долг проверки исправлений, не новый подтверждённый баг.** Координатор после завершения изменяющих исходники аудитов компилирует текущую совокупность изменений и запускает isolation fixture. Особое внимание: `SampleMissingScriptCheck_ReportsAMissingComponentOnAnInactiveDescendant` использует сериализованный null-slot `m_Component`, а preview-регрессия пока создаёт пустую preview-сцену. Затем выполнить реальные `Editor/SampleAssetValidationTests.cs`, `Editor/SampleSceneCoverageTests.cs`, `UI/UIMeshRigEditorTests.cs` и затронутые persistence/audio slices. Минимальная коррекция — исправить только воспроизведённую несовместимость фикстуры; не ослаблять содержательные assertions. Наблюдать сохранность исходных данных/объектов, закрытие preview при исключении и отсутствие оставшихся native assets.

**P2 — подтверждённое исходниками несовпадение часов теста и runtime.** `Bonus/BonusSystemTests.cs:53,70,87` и `Bonus/CooldownRewardAutoClaimTests.cs` (`MakeClaimableNow`) используют системный UTC; `Assets/Neoxider/Scripts/Bonus/TimeReward/TimeReward.cs:252` и `CooldownReward.cs:191,242` используют `NeoTrustedTime`. Воспроизведение для первого Bonus-теста: установить через публичный API clock на сутки впереди системного, затем выполнить проверку «25 секунд / 10 = 2» с unlimited cap; runtime вычисляет другой elapsed. Также диапазон 3–7 секунд зависит от пауз выполнения. Это дефект детерминизма тестов; Unity-воспроизведение ещё не выполнено.

Исправление владельцем `Neo.Editor.Tests`: в этих двух фикстурах использовать существующие `INeoClock`, `TrustedClock(INeoClock)` и `NeoTrustedTime.SetClockForTests` из `Neo.Tools.Time`. До замены provider получить прежний `NeoTrustedTime.Clock`, затем установить временный provider и управляемый clock. После уничтожения компонентов восстановить clock, затем provider в `finally`. Проверять точный сохранённый timestamp относительно исходного и явное продвижение времени без sleep. Дополнить isolation fixture сценариями исходного clock впереди UTC и исключения; исходная ссылка clock/provider и sentinel должны сохраниться. Runtime владельцев `Neo.Bonus` и `Neo.Tools.Time` менять не требуется.

**P2 — известная граница изоляции, не переоткрытие F6.** `Save/SaveProviderTests.cs` и `Save/SaveManagerReadModifyWriteTests.cs` намеренно вызывают reset; `Assets/Neoxider/Scripts/Save/SaveProvider.cs:30–32` очищает события. Provider scope восстанавливает экземпляр, а не подписчиков. Аналогичные global-reset тесты требуют отдельного host/job и категории `GlobalStateReset`; обычным фикстурам нужен контракт владения ниже. Владелец тестов добавляет категорию, координатор разделяет задания; `Neo.Save` сохраняет корректную runtime-семантику reset.

## Существенное улучшение: поддержка владения и независимый model slice

**Предложение, не реализовано.** Общий test asmdef связывает независимые модели с Mirror и сценовыми интеграциями. Повторяющиеся cleanup-классы и жёсткие sample paths мешают повторному запуску и проверке UPM.

Переиспользовать оба новых audit scope, обратную очистку `Abilities/AbilityTestSupport.cs`, `FileSaveProviderOptions.PersistenceRoot`, существующий обход sample roots и Unity preview API. Не добавлять runtime-менеджер, singleton или MonoBehaviour.

Точные новые файлы первого этапа:

- В каталоге `Assets/Neoxider/Tests/Support/Editor/`: `Neo.Tests.Support.asmdef`, `OwnedUnityObjects.cs`, `PreviewSceneLease.cs`, `TestAssetFolderLease.cs`, `PackageSampleRoots.cs`.
- В каталоге `Assets/Neoxider/Tests/Support/Save/`: `Neo.Save.Tests.Support.asmdef`, `SaveProviderLease.cs`.
- `Editor/Audit20261023NeoEditorTestsSupportContractsTests.cs`.

Предлагаемые публичные C# контракты ограничены Editor/test assemblies: `OwnedUnityObjects.Own<T>(T) where T : UnityEngine.Object` и идемпотентный `Dispose()`; `PreviewSceneLease.Open(string)` с `Scene`; `SaveProviderLease.UseTemporary()`/`Capture()`; `PackageSampleRoots.Resolve(string packageRoot, string importedRoot, string version)` возвращает `SampleRootResolution` с `IReadOnlyList<string> Roots` и диагностикой отсутствия, объявленный в том же файле. Базовый support не зависит от gameplay; save-support отдельно зависит от `Neo.Save`. Asset lease удаляет только собственную уникальную папку. Не пытаться автоматически клонировать все статические события через reflection.

Мигрировать sample validators и два audit scope через временные forwarding wrappers. Четыре `Audio/*` фикстуры, изменённые аудитом, и `Bonus/SpinControllerPaylineTests.cs` переводить на ownership API после остановки playback/контроллеров. Resolver учитывает package root, `Samples`, `Samples~`, импорт `<version>/Demo Scenes` и legacy package name; выбор версии детерминированный. Никаких rename/import в validation. `Core/ModulePrinciplesTests.cs` пока остаётся явно repository-only: его переносимость не заявляется.

Пилот разделения: создать `Cards/Model/Neo.Cards.Model.Tests.asmdef`; перенести туда `Cards/CardSpriteNameParserTests.cs` и `Cards/DeckAndHandModelTests.cs` вместе с неизменными `.meta`, именами тестов и namespaces. Проверенная текущая closure `Neo.Cards` содержит `Neo.Extensions`, `Neo.PropertyAttribute`, UniTask и TMP, без Mirror. Остальные Cards/integration cases остаются на месте. Отсутствие Mirror проверять в минимальном host этой closure; работоспособность всего пакета без Mirror отдельно зависит от владельца `Neo.Network`, чей asmdef сейчас ссылается на Mirror напрямую.

## Владельцы, порядок, совместимость

1. Координатор фиксирует интеграционный snapshot; `Neo.Editor.Tests` закрывает воспроизводимые test failures и UTC mismatch, затем повторяются затронутые регрессии.
2. Координатор назначает владельцев новых support/model assemblies и меняет общий `Neo.Editor.Tests.asmdef`/CI filters. До этого никаких перемещений между ownership. `Neo.Cards` подтверждает границу pilot; `Neo.Save`, `Neo.Tools.Time`, `Neo.Bonus` согласуют существующие API. `Neo.Editor` и `Neo.UI.Editor` подключаются только при доказанном дефекте preview/menu adapter, без дублирования их реализации.
3. Добавить support contracts, мигрировать sample/asset ownership, затем Cards pilot; runtime публичные API, serialized fields и prefab/scene GUID не менять. Новым файлам нужны уникальные `.meta`.
4. Обновить `Assets/Neoxider/Docs/Testing.md`: C# model tests, typed component/NoCode actions и проверка UnityEvents, ownership, reset categories и sample layouts. Ссылку в `Assets/Neoxider/Docs/README.md` добавляет координатор. Затем проверить UPM/release layout без смены layout текущего checkout.

Оценка: UTC/проверки — до дня плюс доступность редакторов; support, resolver и pilot — 3–5 дней. Основные риски: discovery после перемещений, межтестовые зависимости и optional-package compilation.

## Приёмка и условия остановки

Минимум: 27 cases аудита; два clock-сценария; support-проверки reverse cleanup/повторного Dispose с внешним sentinel, preview normal/throw с реальным asset, provider restore после ошибки, матрица трёх sample layouts и двух импортированных версий. Поломанная ссылка на скрипт у inactive descendant обязана дать path/count, чужой Player — игнорироваться.

После source freeze координатор запускает минимум одну конкретную Unity 6.0–6.4 и одну 6.5+, записывая версии, compile result и pass/fail/skip counts. Версионные ID-различия остаются в `NeoDiagnostics.StableId`/`NeoComponentHealth.StableId`; новых разрозненных `#if` не вводить. Отдельно выполнить существующие EnterPlayMode/ExitPlayMode cases из `Tools/ImageFillAmountAnimatorTests.cs` и соответствующие UnityTests; чистые model/preview проверки остаются EditMode. Два повторных запуска выявляют state leakage; random-order запуск — там, где его поддерживает runner.

Без компиляции обеих веток или при непроверенном missing-slot/preview поведении интеграционную приёмку не закрывать; skips не заменяют pass. Не менять runtime reset-семантику ради тестового удобства. Расширение разделения всех модулей и целого пакета без Mirror — отдельное решение координатора после pilot; оно не блокирует уже безопасные исправления. В фазе планирования Unity/build/test процессы не запускались.
