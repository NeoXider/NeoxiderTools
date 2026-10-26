# Neo.Cards.Editor — индивидуальный план реализации

Фаза 2, тот же аудитор; `gpt-6.1-sol`, reasoning `max`. Прочитаны [аудит](./Neo.Cards.Editor.md), полный `PLAN-CONTRACT.md`, manifest, текущие source/tests/docs и межсборочные контракты. Записывается только этот план.

## Уже выполнено

В `Assets/Neoxider/Scripts/Cards/Editor/DeckConfigEditor.cs` исправлены F1–F5: custom-deck controls, конфликты рубашки/джокеров, Assets boundary, atlas preview, текущая validation и joker guidance. Serialized fields/runtime API сохранены.

Добавлены `Assets/Neoxider/Docs/Cards/Editor.md` и `Assets/Neoxider/Tests/Edit/Cards/Audit20261023NeoCardsEditorTests.cs`: восемь методов / двенадцать cases с assertions на sprites/data, отчёт и Undo/Redo. Пройдены статические inventory 1/1, 11 bindings, UTF-8/whitespace/GUID/API checks. Это завершённые изменения; Unity compilation, NUnit, импорт и GUI не проверены.

## Остаток: приоритет, свидетельство, владелец

**P1 — verification debt, не новый дефект.** После остановки source-changing аудитов координатор компилирует editor на представителе Unity 6.0–6.4 и отдельно 6.5+, запускает `Neo.Editor.Tests.Audit20261023NeoCardsEditorTests`. Приёмка: 12 проходящих cases, Undo/Redo восстанавливает размер/содержимое suits и рубашку; Assets принимается, AssetsBackup/project root отвергаются. Исправлять только воспроизведённые причины. Неисправленных подтверждённых дефектов собственного C# файла сейчас нет.

**P1 — GUI/discovery debt.** Координатор проверяет standard/custom, редактирование id/face sprite, сохранение данных после повторного открытия, validation и Standard52/Standard54 guidance. Отдельно: null/sliced/tight-atlas sprites, завершение preview, cancel/empty folder, sub-assets в подпапке. Тесты обходят native dialog; `LoadSpritesInFolder` не исполнялся. Приёмка: правильные назначения и изображение, соответствующее artwork. Rendering/repaint/path-normalization остаются неподтверждёнными рисками, не готовыми bug fixes.

**P2 — чужие metadata.** Повторное чтение `Assets/Neoxider/Docs/Cards/DeckConfig.md.meta` подтверждает два `fileFormatVersion`/GUID record: `7838667a5e6940dbbc728b67f5cfb8cd`, `d777606e3f3f94d44b14b741d014f8fe`. `Neo.Cards` (`neo-audit-1023-b02-05`) и координатор определяют правильный GUID по истории/ссылкам, оставляют один importer record без пересоздания asset. Влияние на Unity не измерено. Приёмка: один авторитетный GUID, корректный импорт, сохранённые ссылки. Здесь файл не меняется.

**P3 — docs integration.** В `Assets/Neoxider/Docs/Cards/README.md` и `Assets/Neoxider/Docs/Cards/DeckConfig.md` нет ссылки на `Editor.md`. Владелец `Neo.Cards` добавляет её; приёмка — editor-глава доступна обычной навигацией.

## P2: существенное улучшение — план импорта до изменения asset

Предложение, не реализация. `ApplyAutoFill` очищает suits даже для папки с одними неизвестными именами — документированный replacement workflow. Discovery не задаёт порядок кандидатов; операция private. Нужны C# automation и список замен до применения.

Повторно использовать `CardSpriteNameParser.TryParse`, `CardSpriteParseResult`, `DeckTypeExtensions.GetMinRank/GetCardsPerSuit`, `DeckConfig.GetSprite/Validate`, `SerializedObject`, текущую folder validation и `AssetDatabase` discovery. `AssetPreview` оставить только в UI. Не добавлять runtime manager, NoCode action или новую assembly.

Предлагаемый публичный API в `Neo.Cards.Editor`:

- `DeckSpriteImportPlanner.CreatePlan(IEnumerable<DeckSpriteSource> sources, DeckType spriteType)` возвращает read-only `DeckSpriteImportPlan` с назначениями, неизвестными/out-of-range входами и обеими сторонами конфликтов. `DeckSpriteSource` содержит Sprite, снимок имени, asset path и stable local file id. Планировщик не обращается к AssetDatabase, GUI или Undo.
- `DeckSpriteImporter.Preview(DeckConfig target, DeckSpriteImportPlan plan, DeckSpriteImportMode mode, bool allowStoredStandardDeck = false)` возвращает `DeckSpriteImportPreview`: точный diff старых/новых значений и снимок затронутых настроек. Custom-mode target по умолчанию отвергается без изменений.
- `DeckSpriteImporter.Apply(DeckConfig target, DeckSpriteImportPreview preview)` возвращает `DeckSpriteImportResult` с результатом/диагностикой. До записи проверяет актуальность снимка и Sprite references; устаревший preview требует пересчёта. Применяет все значения одной Undo-операцией.

`ReplaceSuits` остаётся default: очистка suits, сохранение отсутствующих replacement back/jokers и custom entries. Опциональный `FillMissing` заполняет пустые допустимые слоты, сохраняя существующие sprites; избыточные заполненные элементы не обрезаются молча. Кандидаты упорядочиваются ordinal по path/name/local id. Один выбранный кандидат сохраняется на слот, остальные явно показаны. В inspector после выбора папки появляется review и отдельное Apply; открытие/закрытие review ничего не записывает.

### Точные файлы и границы

| Файлы будущей реализации | Назначение / владелец |
| --- | --- |
| `Assets/Neoxider/Scripts/Cards/Editor/DeckSpriteImportPlan.cs` — новый | Source, plan, preview, result и enum mode; `Neo.Cards.Editor`. |
| `Assets/Neoxider/Scripts/Cards/Editor/DeckSpriteImportPlanner.cs` — новый | Разбор, группировка, стабильный порядок; `Neo.Cards.Editor`. |
| `Assets/Neoxider/Scripts/Cards/Editor/DeckSpriteImporter.cs` — новый | Preview diff, проверка актуальности, serialized apply и Undo; `Neo.Cards.Editor`. |
| `Assets/Neoxider/Scripts/Cards/Editor/DeckConfigEditor.cs` | Тонкий адаптер discovery → plan → review → Apply; ручное заполнение остаётся доступно. |
| `Assets/Neoxider/Tests/Edit/Cards/Audit20261023NeoCardsEditorImportPlanTests.cs` — новый | Проверки выходов планировщика без окна; новые uniquely prefixed tests. |
| `Assets/Neoxider/Tests/Edit/Cards/Audit20261023NeoCardsEditorImportApplyTests.cs` — новый | Реальный diff, Undo, stale preview и discovery fixture. |
| `Assets/Neoxider/Tests/Edit/Cards/Audit20261023NeoCardsEditorTests.cs` | При переходе на API заменить private reflection seam; сохранить проверки исправленных контрактов. |
| `Assets/Neoxider/Docs/Cards/Editor.md` | Английские примеры C# automation и inspector workflow; `Neo.Cards.Editor`. |
| `Assets/Neoxider/Tests/Edit/Neo.Editor.Tests.asmdef` | Только координатор/владелец `Neo.Editor.Tests` (`neo-audit-1023-b03-04`): добавить `Neo.Cards.Editor` для typed tests. Текущим reflection tests эта ссылка не нужна. |

Новые файлы получают уникальные `.meta`. `DeckConfig.cs`, `DeckType.cs`, `CardSpriteNameParser.cs` принадлежат `Neo.Cards`: дождаться финальных контрактов; менять runtime для предложения не требуется.

### Совместимость и полезные проверки

Сохранить имена serialized fields, GUID существующих assets, runtime методы и menu/button entry. Не хранить preview как ScriptableObject, не добавлять scene references. Детерминированный порядок меняет выбор только в неоднозначных папках; это явное продуктовое решение для нового workflow и запись в release notes координатора. Тесты таких папок должны проверять первый кандидат после объявленной сортировки, а не прежний порядок входного списка. Уникальные наборы сохраняют прежние назначения.

Минимальные новые регрессии: перестановка входов даёт одинаковые assignments/conflicts; разные sub-assets одного файла не теряются; Standard36 исключает пятёрку; Preview сохраняет все публичные данные target; ReplaceSuits показывает очищаемые слоты; FillMissing сохраняет занятые; Apply+Undo/Redo восстанавливает suits/back/jokers вместе; изменение deck type или уничтожение source после Preview запрещает запись. Discovery проверяется на изолированном временном импортированном sprite-sheet fixture с гарантированной очисткой, без изменения общих samples.

NoCode пользователю достаточно inspector и назначения DeckConfig компонентам; programmer guide показывает новый editor API и неизменные runtime `GenerateDeck/GetSprite/Validate`. Не добавлять обязательные UnityEvent chains. Оценка: 2–3 рабочих дня с тестами и графическим review; риск средний из-за Undo, sub-asset identity и collision ordering.

## Порядок и условия завершения

1. Зафиксировать финальные границы `Neo.Cards`; его владелец устраняет metadata-проблему. Координатор выполняет P1 compile/EditMode/GUI проверки существующих исправлений; обнаруженные регрессии устраняются прежде расширения API.
2. Реализовать чистый planner и проверки его выходов; затем serialized Preview/Apply и Undo tests. Ссылку тестовой assembly централизованно добавить до компиляции typed tests.
3. Подключить review/Apply к inspector; обновить C#/scene guide и навигацию чужим владельцем. Подтвердить выбранную политику ambiguous folders в release notes.
4. Проверить сборки в Unity 6.0–6.4 и 6.5+, включая соответствующие ветки существующих общих compatibility helpers. Local file id не заменять instance id; новых разрозненных `#if` не добавлять. Новые compiler branches нельзя считать рабочими без компиляции соответствующим Unity.
5. Release-проверка координатора: уникальность metadata, корректные docs links, отсутствие editor-зависимости у runtime и неизменный UPM sample layout. `Samples~`, developer `Samples` и imported samples не переименовывать; новые tests не должны требовать конкретного sample layout.

Для этой editor-фичи достаточно EditMode и графической проверки; PlayMode не требуется при неизменном runtime, а изменение runtime поведения создаёт отдельную обязанность его владельца. Остановить продвижение к релизу при compile failure, провале state/Undo assertion, stale preview с частичной записью, изменении GUID или непроверенной версии. Отсутствие графического окружения оставляет UI/atlas verification незакрытой даже при зелёном NUnit. Предложение API можно реализовать отдельным этапом; оно не отменяет завершённых F1–F5. В этой фазе никакие Unity/build/test процессы не запускались.
