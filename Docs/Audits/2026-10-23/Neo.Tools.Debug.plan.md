# Neo.Tools.Debug — индивидуальный план реализации

Статус: план фазы 2, продолжение исходной сессии аудитора; предложения ниже ещё не реализованы. Модель/усилие по назначению: `gpt-6.1-sol`, `max`. Основание: [завершённый аудит](./Neo.Tools.Debug.md), актуальный manifest, `PLAN-CONTRACT.md` и повторное чтение текущих исходников, документации, регрессий и границ зависимостей. В фазе 2 изменён только этот файл.

## Уже выполнено

Проверены все три C# файла сборки. В `NeoDebugOverlay.cs` исправлены прерывание обновления при отключённом legacy input и создание/разрешение AM через диагностический вызов `I`. В `ErrorLogger.cs` исправлены пути со скобками в обоих форматах stack trace. У `FPS` изменены только XML-описания; runtime-дефект не подтверждён. Обновлены четыре страницы `Assets/Neoxider/Docs/Tools/Debug/`, добавлены восемь случаев в `Audit20261023NeoToolsDebugRegressionTests.cs`.

Доказательства фазы 1: scoped `git diff --check`, проверки UTF-8, ссылок, GUID и неизменности публичных/serialized объявлений; настоящий .NET Regex распознал 4/4 пути, старые шаблоны пропускали три. Unity-компиляция, исполнение регрессий, F3 и player builds **не выполнялись**. Эти исправления не являются оставшейся работой.

## Приоритеты и границы

Незакрытых подтверждённых runtime-дефектов внутри `Neo.Tools.Debug` сейчас нет. Следующие пункты — долг проверки, внешняя стоимость или явно обозначенные предложения.

| Приоритет / статус | Основание, владелец и минимальное действие | Наблюдаемый результат |
|---|---|---|
| P1, проверка | Координатор после завершения остальных source-аудитов запускает восемь новых случаев и существующий `DebugToolsBehaviorTests` из `Assets/Neoxider/Tests/Edit/Tools/Debug/`. Исправлять только воспроизведённые ошибки в принадлежащих файлах. | Все случаи исполнены; пропуски по `Assume` требуют изолированной audio-сцены и не считаются прохождением. |
| P1, проверка | Координатор + Debug: реальные hotkey/lifecycle проверки `Update`, `OnEnable`, `OnDisable`, `OnDestroy`. Нынешняя регрессия вызывает callback с `KeyCode.None`, поэтому доставку F3 не доказывает. | В Input System-only и legacy/Both меняются видимость и текст; пауза не останавливает счётчик; disable скрывает canvas, enable восстанавливает выбор, уничтожение владельца убирает canvas. |
| P2, подтверждённый внешний путь, влияние не измерено | Владелец `Neo.Tools.Input`: `Assets/Neoxider/Scripts/Tools/Input/KeyInputCompat.cs`, `GetKeyDown/Up/Key`, всё ещё вызывает legacy API и ловит исключение при каждом обращении. Выбирать backend в общем helper, переиспользуя `OptionalInputSystemAdapter.cs`. Debug не копирует клавиатурный mapper. | В Input System-only после выбора backend нет постоянных legacy-исключений; down/up/held и F3 работают. Затем повторить Debug smoke. |
| P2, неподтверждённый риск | Координатор проверяет stripping/IL2CPP: `ResolveType`, отражённые AM/Save свойства и input delegate могут зависеть от сохранённой metadata. Коррекция только после воспроизведения отсутствующего члена. | С установленными optional-модулями видны состояния/F3; без них панель обновляется и показывает `—`, без создания менеджеров. |

Для последнего пункта границы принадлежат `Neo.Tools.Input` (`KeyInputCompat.cs`), `Neo.Audio` (`Assets/Neoxider/Scripts/Audio/AudioSimple/AM.cs`), `Neo.Save` (`Assets/Neoxider/Scripts/Save/SaveManager.cs`) и `Neo.Tools.Managers` (`Assets/Neoxider/Scripts/Tools/Managers/Singleton.cs`). Точечное сохранение metadata или регистрационный адаптер согласовываются владельцами по результату player-проверки; обязательные ссылки Debug на эти модули не добавлять. Shared asmdefs/linker configuration изменяет координатор.

## P2: ограниченная история логов и публичный capture API — предложение

**Проблема.** `ErrorLogger.HandleLog` сохраняет каждый принятый formatted string в `errorList`, включая replace-режим и отключённую дедупликацию. `Contains` сканирует историю, append повторно собирает растущий текст. Лимита и очистки нет. Это подтверждённое ограничение дизайна; автоматически менять существующие сцены не требуется.

**Переиспользование.** Сохранить `LogTypeFilter`, `Neo.Extensions.SetColor`, существующие parser/форматирование и main-thread `Application.logMessageReceived`. В проверенных runtime-кандидатах поиск `DebugLogEntry`/`LogBuffer`/`RingBuffer`/`Bounded` готового ядра не обнаружил. Достаточны стандартные `Queue<T>`, `Dictionary<TKey, int>` и существующая сборка; новый singleton, потоковый logger, пакет или asmdef не нужны.

**Публичный контракт.** Обычные C# `DebugLogEntry` и `DebugLogBuffer` без TMP/MonoBehaviour/scene references; существующий `LogType` остаётся типом severity. Предложенные операции:

- `DebugLogBuffer(int maxEntries = 0, int maxCharacters = 0)`; отрицательные параметры отклоняются без изменения состояния.
- `bool TryAdd(DebugLogEntry entry, bool suppressDuplicates = true)`, read-only `Entries`, `RetainedCharacterCount`, `Clear()`.
- У компонента: `bool Capture(string message, string stackTrace, LogType type)`, `void ConfigureHistory(int maxEntries, int maxCharacters)`, `void ClearHistory()`; Unity callback направляется в `Capture`.

Нулевой лимит означает отсутствие соответствующего ограничения. Счётчик измеряет UTF-16 code units удерживаемых payload/key/format strings, а не весь managed heap. Слишком большая одиночная запись отклоняется атомарно, без вытеснения старых записей и без собственного Unity-лога. При обычном переполнении удаляются старейшие записи и их duplicate keys. Индекс хранит число повторений: capture с отключённой дедупликацией должен корректно переживать её повторное включение. Null message/stack нормализуются в пустые строки.

Ядро использует typed raw identity. Компонент через internal key overload сохраняет прежнюю identity полного formatted entry и оформление на момент захвата; переключение `showScriptPath` не перерисовывает старые записи. Новые serialized лимиты по умолчанию равны нулю. Имена/типы прежних полей, включая неиспользуемый `errorText`, и методы `UpdateText`/`AppendText` сохраняются. При нулевых лимитах сохраняется прежняя последовательность append/replace. В opt-in bounded-режиме очередной `Capture` пересобирает вывод из удерживаемой истории; прямые текстовые вызовы изменяют только label до следующего capture. Это явно документируется. `ClearHistory` очищает историю, индекс и label; disable/re-enable историю сохраняет.

**Точные файлы реализации:**

| Файл | Работа |
|---|---|
| `Assets/Neoxider/Scripts/Tools/Debug/DebugLogEntry.cs` | Новый value type с message/stack/type. |
| `Assets/Neoxider/Scripts/Tools/Debug/DebugLogBuffer.cs` | Новое ограниченное ядро, учёт символов, eviction и индекс. |
| `Assets/Neoxider/Scripts/Tools/Debug/ErrorLogger.cs` | Capture/configuration/clear API, делегирование callback, opt-in Inspector лимиты. |
| `Assets/Neoxider/Tests/Edit/Tools/Debug/Audit20261023NeoToolsDebugLogBufferTests.cs` | Новые проверки ядра без GameObjects. |
| `Assets/Neoxider/Tests/Edit/Tools/Debug/Audit20261023NeoToolsDebugCaptureTests.cs` | Новые проверки публичного capture и реального TMP-вывода. |
| `Assets/Neoxider/Docs/Tools/Debug/ErrorLogger.md` | C# примеры, лимиты, отказ capture, совместимость/UnityEvent `ClearHistory`. |
| `Assets/Neoxider/Docs/Tools/Debug/README.md` | Ссылка на расширенный контракт через существующий entry point. |

Новые package-файлы получают уникальные `.meta`; folder metadata — координатор. Сцены/prefabs не мигрировать автоматически. Sample layout не менять: учитывать активные `Assets/Neoxider/Samples`, `Assets/Neoxider/Samples~` и импортированный `Assets/Samples/NeoxiderTools/<version>/<sample>`. Публичные руководства остаются английскими. Специальные Editor/NoCode компоненты не нужны: лимиты доступны Inspector/C#, очистка — UnityEvent.

**Минимальная приёмка.** После 10 000 уникальных записей при 256 entries/32 768 UTF-16 units история и её индекс ограничены; текст bounded-вывода также укладывается в лимит. Отдельно проверить oversized rejection без изменения вывода, eviction/re-add, duplicates off→on, очистку/повторный capture, null/negative inputs, нулевые лимиты, append/replace и смену форматирования. Проверки должны сравнивать записи/текст, а не только отсутствие исключения. Оценка: 1–2 дня плюс центральная Unity QA; основные риски — совместимость identity, учёт строк и восстановление label.

## Порядок реализации и завершение

1. После стабилизации чужих аудитов зафиксировать актуальный source snapshot и закрыть P1 compile/EditMode debt; воспроизведённые регрессии исправлять прежде новых API. Затем интегрировать изменение Input его владельцем и повторить затронутый smoke.
2. Реализовать и проверить log buffer, затем capture/Inspector adapter; существующие восемь регрессий сохранить. Независимый buffer не зависит от готовности Input-оптимизации.
3. Добавить `Assets/Neoxider/Tests/Play/Tools/Audit20261023NeoToolsDebugLifecycleTests.cs` для subscription/unsubscription и canvas lifetime. Для прямого доступа к Debug координатор добавляет `Neo.Tools.Debug` в `Assets/Neoxider/Tests/Play/Neo.Tests.Play.asmdef`: сейчас этой ссылки нет. EditMode asmdef нужные ссылки уже содержит.
4. Обновить указанные guides, проверить UTF-8, GUID, API/serialization и scoped diff; затем центральные package/import/player проверки. FPS runtime не менять без нового воспроизведения.

Перед завершением нужны реальные compile receipts Unity 6.0–6.4 и 6.5+, focused EditMode и описанный PlayMode/backend matrix. Новых локальных version `#if` не вводить; если понадобятся object IDs, использовать общий `NeoDiagnostics.StableId`, обе его ветки проверяет соответствующий компилятор. Подтверждать сборку одной версии за другую нельзя.

Остановить приёмку конкретного изменения при compile error, failed/skipped обязательном случае, новом singleton/canvas leak или нарушении старой сериализации; несвязанные безопасные пункты могут продолжаться. Изменение defaults/duplicate identity для существующих assets требует отдельного решения координатора о миграции и не входит в этот план. Никакие предложения и неподтверждённые stripping/performance последствия не считать уже исправленными или проверенными. В фазе планирования Unity/build/test процессы не запускались.
