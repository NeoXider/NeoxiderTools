# Neo.Tools.Dialogue — индивидуальный план реализации

Статус: план, без реализации в фазе 2. Продолжена исходная сессия аудитора. Основание: [завершённый аудит](Neo.Tools.Dialogue.md), актуальный manifest и повторное чтение текущих исходников, изменений документации и регрессий.

## 1. Уже выполнено

Проверены все три файла сборки. В `DialogueController.cs` исправлены возобновление автопереходов отменённой печатью, рекурсивные автоматические переходы с нулевой задержкой, продолжение старого вызова после навигации из событий, завершение последней строки, состояние пропущенной печати, неуспешный повторный старт и подписки уничтоженного контроллера. В `DialogueUI.cs` исправлено обновление имени при совпадении кеша. `DialogueData.cs` сохранён. Playback-кнопки ограничены Play Mode; публичные объявления и serialized fields сохранены.

Обновлены четыре существующих руководства; добавлены 12 EditMode и 7 PlayMode случаев с префиксом `Audit20261023NeoToolsDialogue`. Инвентарь, декларации относительно `ca198556`, UTF-8, ссылки, метаданные и scoped `git diff --check` проверены статически. Хеши трёх текущих исходников совпадают с аудитом. Unity-компиляция, выполнение тестов, визуальная проверка и измерения производительности не проводились. Перечисленные исправления не являются будущими задачами.

## 2. Приоритеты и оставшийся долг

**P1 — интеграционная проверка, владелец: координатор.** После завершения source-changing аудитов выполнить обе новые Dialogue test classes, существующие `DialogueControllerEditModeTests`, `DialoguePlayModeTests`, `AuditFixesTypewriterTests` и актуальные регрессии владельца `Neo.Tools.Components`. Исходники: `Assets/Neoxider/Tests/{Edit,Play}/Dialogue/`; точные новые файлы указаны ниже. Наблюдаемый результат: отмена не двигает выключенный/заменённый диалог; skip оставляет полный текст, progress 1 и один запланированный переход; callbacks не перезаписывают новый курсор; completion и unsubscribe дают ожидаемые количества событий. При падении — минимальная коррекция соответствующего владельца и повтор затронутых случаев. TMP-фикстуре требуется валидный default font/material; отсутствие шрифта является проблемой фикстуры, а не доказательством дефекта навигации.

**P2 — подтверждённый неограниченный рост стека на пустых контейнерах, владелец: Dialogue.** При `autoNextMonolog/Dialogue == false` остаются цепочки `ShowCurrentSentence → GoToNextDialogue → NextDialogue → ShowCurrentSentence` и аналогичный обход пустых monolog. Нулевая автоматическая задержка уже исправлена, но эти ветки её не используют. Воспроизводитель: последовательность ненулевых `Dialogue` с пустыми `monologues`, затем одна валидная строка; отдельно пустые `sentences`. Рост глубины следует из исходника; фактическое переполнение стека не запускалось. Минимальная коррекция — итеративный внутренний обход пустых массивов в `DialogueController.cs`, без обязательного нового публичного ядра. Сохранить синхронность публичной навигации, порядок boundary/data событий, version guards и остановку при запланированной задержке. Приёмка: после обхода 10 000 контейнеров показана конечная строка ровно один раз; restart из boundary-события прекращает старый обход; полностью пустой граф завершается однократно. Нулевые элементы сохранить согласно текущему контракту, не превращать незаметно в автоматически пропускаемые.

**P2 — межсборочная проверка, не повторный фикс.** В текущем `Assets/Neoxider/Scripts/Tools/Components/TypewriterEffect.cs` уже появились cancellation checks после callbacks, generation guard в `Complete()` и освобождение typing-state до `OnComplete`. Прежняя cross-boundary рекомендация аудита частично реализована другим владельцем; её нельзя считать всё ещё отсутствующим кодом. Владелец `Neo.Tools.Components` (`neo-audit-1023-b06-05`) завершает свой аудит/регрессии; затем Dialogue проверяет restart из native completion/progress callback через реальный контроллер. Нужны полная сохранность текста новой строки, живой новый run и отсутствие старых progress/completion уведомлений. До центрального запуска это verification debt, не подтверждённый оставшийся сбой.

**P3 — подтверждённый побочный эффект, предложение изменения контракта.** `DialogueUI.ValidateFallbackFonts` удаляет элементы из общего `TMP_FontAsset.fallbackFontAssetTable`. Изменение shared data подтверждено; ошибка рендеринга TMP от него не доказана. Сначала предложить `InspectFonts()` с read-only `DialogueFontValidationReport`; текущий `ValidateFonts()` и его поведение не менять молча. Проверка: два UI с одним font asset получают диагностический результат без изменения порядка/ссылок fallback list. Явное editor repair — отдельный шаг владельца `Neo.Editor`, после решения о миграции legacy sanitation.

## 3. Существенное предложение: итеративное C# ядро и заменяемый view

Предложение не реализовано. Проблема разработчика: навигация, отображение, UnityEvents и async scheduling связаны одним стеком MonoBehaviour; сложно использовать другой UI, проверять длинные графы и различать показ строки и завершение печати.

Переиспользовать существующие `Dialogue/Monolog/Sentence`, методы `DialogueUI`, `TypewriterEffect.PlayAsync/Complete/Stop`, UniTask и `NeoDiagnostics`. Новые scheduler, singleton, NoCode bridges и ScriptableObject со сценовыми ссылками не нужны.

Публичный дизайн:

- Неизменяемые `DialogueScript`, `DialogueLine`, `DialogueCursor`; строки содержат текст, speaker и presentation key, без Unity-объектов/UnityEvents.
- `DialogueSession.Cursor`, `State`, `Start(DialogueScript, DialogueCursor)`, `Advance(): DialogueStep`, `Stop()`. Шаг возвращает одну границу либо строку/окончание; адаптер обрабатывает следующий шаг после callback и проверки поколения. Так события не получают заранее вычисленную устаревшую пачку переходов.
- `DialogueScriptCompiler` преобразует существующие массивы и сохраняет отдельную привязку cursor к исходным events/sprites; диагностика содержит координаты. Строгая проверка графа опциональна.
- `IDialogueView` повторяет `Reset`, `SetCharacterName`, `SetCharacterSprite(Sprite)`, `SetDialogueText`. Это Unity-адаптерный контракт; core не зависит от Sprite. `DialogueUI` реализует интерфейс; `DialogueController.SetView(IDialogueView)` даёт явный C# путь, `_dialogueUI` остаётся serialized default.
- Typed controller events `SentencePresented` и `SentenceCompleted` с `DialogueCursor`: immediate/empty line завершается сразу, typing — естественно либо через skip; ранний `Advance` отменяет печать без completion. Legacy `OnSentenceEnd` сохраняет момент presentation.

Снимок создаётся на `StartDialogue`; изменения исходных массивов применяются при следующем старте. Такое snapshot-поведение сначала opt-in: немедленно менять наблюдаемость публичных mutable arrays у существующих пользователей нельзя. Первый этап оставляет core в текущей сборке; отдельная dependency-free asmdef не требуется. Сериализованные имена, UnityEvents, компоненты и assets не мигрируют автоматически.

## 4. Точные файлы и порядок

Корни: `R = Assets/Neoxider/Scripts/Tools/Dialogue`, `D = Assets/Neoxider/Docs/Tools/Dialogue`, `E = Assets/Neoxider/Tests/Edit/Dialogue`, `P = Assets/Neoxider/Tests/Play/Dialogue`.

1. Центральная проверка завершённых fixes: существующие `E/Audit20261023NeoToolsDialogueEditModeTests.cs`, `P/Audit20261023NeoToolsDialoguePlayModeTests.cs`. Дождаться согласованного source-final состояния Components.
2. Без публичного redesign: исправить пустой обход в `R/DialogueController.cs`; добавить `E/Audit20261023NeoToolsDialogueTraversalTests.cs`. Оценка: 0,5–1 день плюс проверка callbacks.
3. Если предложение ядра включено в реализацию: новые `R/DialogueScript.cs`, `R/DialogueCursor.cs`, `R/DialogueStep.cs`, `R/DialogueSession.cs`, `R/DialogueScriptCompiler.cs`, затем `E/Audit20261023NeoToolsDialogueSessionTests.cs`. Только после pure-state тестов подключать controller adapter.
4. Добавить `R/IDialogueView.cs`, адаптировать `R/DialogueController.cs`, `R/DialogueUI.cs`; добавить `P/Audit20261023NeoToolsDialogueViewPlayModeTests.cs`. Текущий `R/DialogueData.cs` сохранять совместимым. Core/view этап: 4–6 дней, основной риск — порядок callbacks и snapshot semantics.
5. Font inspection отдельно: `R/DialogueFontValidationReport.cs`, `R/DialogueUI.cs`, `E/Audit20261023NeoToolsDialogueFontValidationTests.cs`. Возможный repair-файл `Assets/Neoxider/Editor/Tools/Dialogue/DialogueFontValidationMenu.cs` принадлежит `Neo.Editor` (`neo-audit-1023-b03-03`); runtime не получает editor dependency. Новый editor helper необязателен для core/view.
6. Обновить существующие `D/README.md`, `D/DialogueController.md`, `D/DialogueData.md`, `D/DialogueUI.md`: C# session/view example, прежний Inspector/button workflow, точные события, snapshot и font migration. Новым файлам нужны уникальные `.meta`; shared asmdefs/package settings меняет только координатор при действительной необходимости.

## 5. Приёмка и условия остановки

Минимальный набор для core: порядок строк/границ, clamp/start cursors, пустые и null данные согласно выбранному режиму, 10 000 пустых контейнеров, однократный terminal state. Для адаптера: весь существующий набор из 19 случаев, natural/skip/cancel completion, restart из native typewriter callback, headless controller и два разных `IDialogueView`, disable/destroy, scaled auto-delay при unscaled typing. Проверять реальные текст, курсор, progress и последовательность событий.

Координатор подтверждает компиляцию Editor/runtime/player-кода в Unity 6.0–6.4 и 6.5+, включая `UNITY_EDITOR` reflection path. Новые version switches не разносить: при необходимости использовать централизованные `NeoDiagnostics.StableId` / `NeoComponentHealth.StableId`. Полный matrix-pass нельзя заменить балансом скобок или прошлым прогоном.

Package validation сохраняет активный sample layout: `Samples~`, developer `Samples` либо импортированный `Assets/Samples/NeoxiderTools/<version>/<sample name>`. Новые sample scenes этому предложению не требуются; переименований, принудительного импорта и конфигурационных миграций нет.

Не закрывать verification debt без фактических результатов. При изменении Components повторно проверить затронутые интеграционные случаи. Независимые fixes продолжать; переключение legacy font sanitation и snapshot-default выделить как продуктовые решения. Фаза 2 ограничена записью этого плана: runtime, тесты, public docs, отчёт и конфигурация не изменялись, процессы Unity/build/test и дочерние агенты не запускались.
