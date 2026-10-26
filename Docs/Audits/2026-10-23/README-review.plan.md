# План развития README и входов в документацию

Основание: `README-review.md`, `PLAN-CONTRACT.md` и запись `README-review` в `manifest.json`. Базовая версия пакета — `10.19.0`. В фазе 2 изменяется только этот план.

## Текущий результат

В первой фазе исправлены `README.md`, `Assets/Neoxider/README.md` и четыре страницы `Assets/Neoxider/Docs/`: `README.md`, `GettingStarted.md`, `PackageCompatibility.md`, `Samples.md`. Уже исправлены имя `-System--.prefab`, зависимости и bundled CMF, версия входных страниц, три sample, пути source/development/imported, канонические ссылки и аудиопример с ID. Добавлены маршруты C# / scene / NoCode и полный `ResourcePoolModel` пример. SVG-структура и содержание HTML/CSS-раздела сохранены.

В фазе 2 координатор централизованно обновил до `10.19.0` ещё три ранее устаревших упоминания: imported sample example в `AGENTS.md`, current version в `Assets/Neoxider/PROJECT_SUMMARY.md` и metadata в `Assets/Neoxider/Skill/neoxider-tools/SKILL.md`. Они входят в `PackageHealthCheck.VersionMentions`; Unity parity test ещё не запускался.

Выполнены только проверки исходников, метаданных, UTF-8, whitespace, Markdown-ссылок/якорей и sample-путей: 228 локальных целей и девять repository URL разрешились. Unity, компиляция примеров, импорт UPM, PlayMode и HTTP-доступность не проверялись. Их успешность не следует из наличия тестов или старого HTML → UITK отчёта.

## Приоритеты оставшейся работы

| Приоритет / статус | Основание и минимальное исправление | Владелец / наблюдаемый результат |
|---|---|---|
| **P2, подтверждённый пробел автоматизации** | `scripts/check_package.py` проверяет manifest/layout/UTF-8, но не Markdown-граф, package boundary и sample-таблицы. Добавить постоянную проверку. | Документация + координатор CI. Изменение версии, sample name или якоря даёт точную диагностику `файл:строка`, а корректная документация проходит без Unity. |
| **P2, долг проверки** | Новые C#-примеры сверены с исходниками, но не компилировались как опубликованный текст. Связать текст с компилируемыми примерами и выполнить минимальные сценарии. | `Neo.Editor.Tests`, `Neo.Core`, `Neo.Audio`; после завершения их аудитов. Пример расходует 25 из 100 и возвращает 75; Audio API и компонентный маршрут работают согласно инструкции. |
| **P2, долг совместимости** | Нет свежей матрицы установки/компиляции для текущего общего diff. Проверить два Unity compiler branch и импорт samples, затем записать фактические результаты в `PackageCompatibility.md`. | Координатор интеграции и владельцы затронутых модулей. В таблице указаны версия Editor, commit, зависимости, режим и pass/fail/skip. |
| **P3, неподтверждённый риск / улучшение** | SVG используют raw GitHub `main`; недоступность сети или изменение main может изменить отображение старого релиза. Сначала проверить HTTP и выбрать политику: release-pinned URL либо bundled SVG. Нет доказательства, что URL сейчас сломаны. | Координатор релиза. Текстовая навигация доступна всегда; версия изображения соответствует опубликованному пакету. Offline-упаковка — отдельное продуктовое решение. |

## Существенное улучшение: единый проверяемый контракт документации

Проблема — одни package facts повторяются в нескольких входах, а проверка версии уже существует только в Unity Editor. Переиспользовать `Assets/Neoxider/Editor/PackageHealthCheck.cs`, `Assets/Neoxider/Tests/Edit/Core/PackageVersionParityTests.cs`, `Assets/Neoxider/Tests/Edit/Core/ModulePrinciplesTests.cs` и существующий package job в `.github/workflows/tests.yml`, не создавать конкурирующие списки версий.

Предлагаемые файлы:

- `Assets/Neoxider/Editor/Documentation/DocumentationContract.json`: schema version, список version mentions с форматами, канонические module entries, область repository/package и реестр snippet ID. Реальные version, UPM dependencies и sample displayName/path брать из `package.json`, не дублировать числа в JSON. Внешние host requirements DOTween/UniTask учитывать отдельно от UPM dependencies, с опорой на `THIRD-PARTY-NOTICES.md`.
- `Assets/Neoxider/Editor/Documentation/DocumentationContract.cs`: небольшой внутренний loader для `Neo.Editor`. Сохранить существующие публичные `VersionMention`, `VersionMentions` и `FindVersionParityProblems()`; менять источник данных, а не контракт вызывающего кода. Ошибка чтения/schema должна сообщаться, а не превращаться в успешную пустую проверку.
- `scripts/check_docs.py`, `scripts/tests/test_check_docs.py`: read-only проверка того же JSON, UTF-8 и Markdown. Подключить её из `scripts/check_package.py`; package job уже вызывает этот script. `.github/workflows/tests.yml` менять только для запуска fixture-тестов и сохранения диагностик.

Проверять inline/reference Markdown links, HTML `src`/`href`, percent-encoded пути, повторяющиеся заголовки и явные anchors; исключать code fences и inline code. Разделять repo-only ссылки и цели внутри UPM payload. Repository URL можно сопоставлять файлам checkout без HTTP; внешнюю доступность проверять отдельно, чтобы сетевой сбой не выдавался за дефект C#.

Fixtures должны ловить stale badge/imported version, отсутствующий sample, неверный anchor, выход за package boundary и неверный scope. Layout cases: `Samples~`, developer `Samples`, imported `Assets/Samples/NeoxiderTools/<version>/<displayName>`. Переиспользовать разрешение корней из существующих sample validators; не требовать одновременного наличия всех трёх.

Оценка: 2–3 рабочих дня вместе с владельцем `Neo.Editor`. Главные риски — расхождение Markdown slug rules и неверное определение repository против installed package. Сначала зафиксировать fixtures и root resolution, затем переключать существующий Editor guard на общий JSON.

## Компилируемые примеры и RU/EN

Добавить в существующий `Neo.Editor.Tests` файлы `Assets/Neoxider/Tests/Edit/Documentation/ManaExample.cs`, `SoundTest.cs`, `DocumentationExamplesTests.cs` в той же папке. В первых двух хранить целые примеры с выделенными regions; `check_docs.py` сравнивает regions с маркированными C# blocks в `Assets/Neoxider/Docs/GettingStarted.md`. Никакой динамической компиляции Markdown или копии runtime-алгоритмов. Новый тест вызывает сам `ManaExample.SpendMana()` и проверяет 75; Audio-пример проверяет компиляцию используемой сигнатуры. Assembly уже ссылается на `Neo.Core` и `Neo.Audio`, новая runtime assembly не нужна.

Для сценового поведения переиспользовать `Assets/Neoxider/Tests/Play/Audit20261023NeoAudioLifecycleTests.cs` и минимально расширить его владельцем Audio: один `SoundEntry` с ID, явная AM-ссылка, вызов `Play(string)`, затем `PlayAudio.AudioPlay()` без неожиданных логов. Проверку слышимого воспроизведения проводить с включённым audio, не объявлять headless skip успешным playback.

В `Assets/Neoxider/Docs/README.md` добавить матрицу «runtime API → component → NoCode → sample» для Core/Merge/StateMachine/Audio со ссылками на существующие канонические README. Для русского входа добавить короткие парные RU/EN ориентиры там и в `Assets/Neoxider/Docs/GettingStarted.md`; сохранить английский основной текст, API names и общие snippets. Не создавать второй `DocsEn` tree. Проверять обе языковые навигации и реальные UTF-8 bytes существующим encoding-контрактом. `DocsEn.meta` остаётся собственностью пользователя.

## Порядок, приёмка и остановка

1. Дождаться финальных API владельцев Core/Audio/Editor и зафиксировать общий snapshot. Не повторять уже выполненные README- и metadata-исправления; проверить их существующим parity guard на этапе интеграции.
2. Добавить fixture-проверки и общий контракт; подключить offline CI; затем синхронизировать snippets и RU/EN маршруты. Не добавлять сценовые адаптеры или Inspector UI без отдельной необходимости.
3. Координатор после окончания source-аудитов компилирует тестовые примеры на фактической Unity 6.0–6.4 и фактической 6.5+, запускает узкие EditMode/Audio PlayMode сценарии и UPM sample-import smoke. Использовать только `NeoDiagnostics.StableId` / `NeoComponentHealth.StableId`; новых рассыпанных `#if` не вводить.
4. Финальная приёмка: намеренно испорченные fixtures отвергаются; актуальные facts проходят; canonical module links разрешаются в checkout и UPM; оба C#-примера компилируются; source/imported sample пути соответствуют manifest; фактическая verification matrix опубликована. Новые package assets, folders и test sources получают уникальные `.meta`; runtime API, serialized fields, prefab/scene GUID и sample layout не мигрируют.

Остановить расширение после этого минимального набора: не переводить весь пакет и не генерировать всю prose автоматически. При отсутствии нужного Editor, лицензии или audio backend записать конкретный непроверенный gate; не объявлять поддержку версии или playback подтверждёнными. Выбор bundled/offline SVG и полнота дальнейшего перевода не блокируют независимые исправления и offline validation.
