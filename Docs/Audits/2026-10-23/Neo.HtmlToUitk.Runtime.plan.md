# Neo.HtmlToUitk.Runtime — индивидуальный план, фаза 2

Статус: предложение реализации и долг проверки; реализация в этой фазе не выполнялась. Продолжение исходной сессии `gpt-6.1-sol`, reasoning `max`. Прочитаны PLAN-CONTRACT, manifest, завершённый аудит, текущие изменения source/docs/tests и относящиеся к предложению контракты.

Обозначения точных корней: `R = Assets/Neoxider/Scripts/UI/HtmlToUitk/Runtime/`; `T = Assets/Neoxider/Tests/Edit/HtmlToUitk/`; `D = Assets/Neoxider/Docs/UI/`.

## 1. Уже выполнено

Аудит просмотрел все 29 C# файлов. F1–F10 исправлены в `R/Logic/Json/MiniJson.cs`, `R/Logic/Selectors/SimpleSelector.cs`, `R/Logic/Animation/Easing.cs`, `R/Animation/UitkAnimator.cs`, `R/Navigation/UitkRouter.cs`, `R/Effects/BackdropBlur.cs`, `R/Effects/BackdropBlurPass.cs`: точность/синтаксис JSON, селекторы, callback reentry, modal focus, конечные значения/база/отмена анимаций, конечность bezier и владение blur-ресурсами.

Обновлён `D/HtmlToUitk.md`; добавлены четыре fixture с префиксом `Audit20261023NeoHtmlToUitkRuntime`, 59 объявленных cases: 43 pure и 16 editor/native. Проверены manifest ownership, структура, UTF-8/GUID, сохранение public/serialized declarations и `git diff --check`. Компиляция, выполнение тестов и Unity QA не проводились. F1–F10 не являются будущими задачами реализации.

## 2. Оставшиеся исправления и долг проверки

Новых подтверждённых неисправленных runtime-дефектов не установлено. Следующие пункты — долг проверки; условные корректировки выполняются только после воспроизведения.

| Приоритет / владелец | Основание, действие и наблюдаемый результат |
|---|---|
| P1 / координатор + `Neo.HtmlToUitk.Tests.Editor` | Все 59 cases пока не исполнены. После завершения source-changing аудитов запустить их и существующие HtmlToUitk regressions. Ошибка получает конкретный runtime-файл, минимальную коррекцию и повтор упавшего сценария; отсутствие запуска не считается PASS. |
| P1 / координатор + Runtime | `R/Hosting/UitkRootConnection.cs:53` содержит разные PanelRenderer API; native blur зависит от `HTU_BACKDROP_FILTER`. Скомпилировать матрицу ниже. Исправлять несовместимость только в `PanelCompatibility` или `BackdropBlur`, сохраняя централизованные gates. |
| P1 / Runtime + оператор Unity | Router-fixture проверяет записанные focus targets, а не реальный focus controller; анимационные UnityTest работают в EditorWindow. Проверить modal→modal→page, redirect, disable/re-enable, reload/detach. Ожидание: одна актуальная страница/подписка, правильный `focusedElement`, ноль остановленных runs после Dispose. При воспроизведении позднего focus корректировать generation/cancellation в `R/Navigation/ElementFocus.cs` и `UitkRouter.cs`. Это пока риск. |
| P2 / Runtime + оператор Unity | F10 не проверен на native filter-value restoration и camera/Blit exception paths; GPU-case может быть skipped. Проверить `R/Effects/BackdropBlur*.cs`: исходные image/size/filter, camera target и active RT восстановлены, временные/owned RT освобождены при смене target/mode и исключении. Корректировать только подтверждённый путь cleanup. |

Точные существующие regression-файлы: `T/PureLogic/Audit20261023NeoHtmlToUitkRuntimeJsonSelectorTests.cs`; `T/Editor/Audit20261023NeoHtmlToUitkRuntimeRouterTests.cs`, `Audit20261023NeoHtmlToUitkRuntimeAnimationTests.cs`, `Audit20261023NeoHtmlToUitkRuntimeBlurTests.cs`. Новые native lifecycle сценарии размещать в отдельном `T/Editor/Audit20261023NeoHtmlToUitkRuntimeNativeLifecycleTests.cs`, не переписывая чужой `RuntimeOwnershipTests.cs`.

## 3. P2: строгая компиляция неизменяемой спецификации — предложение

**Проблема и evidence.** `R/Logic/Navigation/NavSpec.cs:154` и `R/Logic/Animation/AnimSpec.cs:142` сохраняют переданные коллекции; одинаковые IDs перезаписывают lookup, оставаясь в списках. `AnimSpec.FromJson` нормализует значения до общей валидации. `R/Navigation/UitkScreen.cs:97` удаляет текущую проекцию до разбора всех metadata. Это основания для opt-in улучшения контракта; менять forgiving legacy API автоматически не предлагается.

**Публичный C# путь:**

```csharp
public static bool TryCompile(RuntimeSpecInput input,
    out CompiledScreenSpec specification,
    out IReadOnlyList<RuntimeSpecDiagnostic> diagnostics);

public bool TryConfigureCompiledRoot(VisualElement root,
    CompiledScreenSpec specification, bool ownResponsiveLayout = true);
```

Первый метод принадлежит `RuntimeSpecCompiler`, второй — `UitkScreen`. Отдельное имя сохраняет однозначность существующего `ConfigureRoot(root, null)`. Input содержит необязательные nav/anim/media JSON и имена источников. Diagnostic содержит severity/code/source/property path, например `nav.pages[2].id`, и доступную позицию syntax error.

Compiler сначала проверяет исходный JSON до coercion: schema, типы, непустые/уникальные IDs, ссылки/default tabs, известные enum/easing/selectors, конечные timing/bounds и заявленные properties/units. Затем формирует закрытые неизменяемые данные. Не выдавать наружу mutable `BreakpointQuery` или массивы `AnimValue`. Для component-free C# предоставить `CompiledScreenSpec.CreateNavigationSpec()`, `CreateAnimationSpec()`, `CreateBreakpointSet()` с независимыми копиями для существующих сервисов. Playback, focus, callbacks и style ownership остаются у каждого root.

**Reuse.** Использовать `MiniJson`, `NavSpec.Validate`, `FromJson`, `SelectorList`, `AnimValue`, `Easing`, `PreparedAnim` и существующие контроллеры; не создавать второй parser/animator. Кандидат `Tools/HtmlToUitk/src/HtmlToUitk.Convert/HtmlToUitk.Convert.csproj` действительно netstandard2.1, но тянет Core/Bake/Rules/Capabilities: не переносить converter dependency в player. Capability/schema сверить с converter owner. Кэширование mutable sampled values исключить; дополнительные prepared caches вводить после измерения.

**Точные будущие файлы Runtime:** новые `R/Logic/Compilation/RuntimeSpecInput.cs`, `RuntimeSpecDiagnostic.cs`, `CompiledScreenSpec.cs`, `RuntimeSpecCompiler.cs`, `RuntimeSpecCapabilities.cs`; адаптер — существующий `R/Navigation/UitkScreen.cs`. Catalog остаётся pure C#; native тест сравнивает его заявленные свойства с `StyleApplier.SupportedProperties`. Новые tests: `T/PureLogic/Audit20261023NeoHtmlToUitkRuntimeCompilationTests.cs`, `T/Editor/Audit20261023NeoHtmlToUitkRuntimeCompiledScreenTests.cs`. Документация — `D/HtmlToUitk.md`.

**Совместимость/authoring.** Сохранить constructors, Parse, serialized fields и legacy defaults. Добавить opt-in `strictMetadataValidation = false` и read-only diagnostics у screen; TextAssets остаются конфигурацией. На неизменённом root компилировать metadata до teardown: ошибка сохраняет прежние действия/страницу. У выключенного component только сохранять конфигурацию до OnEnable. Не обещать rollback дерева, уже уничтоженного внешним host, или любых GPU exceptions. Новые package-файлы получают уникальные `.meta`; folder metadata согласует координатор. Scenes/prefabs и Samples~/Samples/imported sample layout не мигрируются.

## 4. Границы и порядок

1. Координатор фиксирует source-final состояние после остальных аудитов; выполняет P1/P2 проверки, Runtime исправляет воспроизведённые сбои с regression.
2. Runtime реализует pure compiler, закрытые данные и минимальные тесты; затем named C# adapter и Inspector opt-in, затем английскую C#/scene документацию с кратким русским пояснением.
3. После стабильного runtime API владелец `Neo.HtmlToUitk.Editor` (`neo-audit-1023-b03-07`) может отдельно адаптировать `Editor/Logic/DiagnosticsReport.cs` и `Editor/Window/FindingsView.cs`. Обратная Runtime→Editor ссылка запрещена; текущая Editor→Runtime уже есть.
4. Владелец `Neo.HtmlToUitk.Tests.Editor` (`neo-audit-1023-b04-01`) интегрирует discovery/receipts. Внешний converter owner назначается координатором: сверка `Tools/HtmlToUitk/src/HtmlToUitk.Convert/Dialect/NavCollector.cs`, `AnimCollector.cs` предшествует strict schema defaults. Изменения этих файлов не обязательны для opt-in runtime этапа.

Оценка предложения: 3–5 дней реализации + 1–2 дня интеграционной QA. Риски: legacy metadata без schema, неизвестные enum/units, расходы копирования. Решения о strict-by-default и новых CSS возможностях остаются отдельными продуктными решениями.

## 5. Acceptance и условия остановки

Минимум предложения: валидный generated fixture сохраняет routes/tabs/samples; duplicate ID/unknown tab/selector возвращают точный diagnostic до изменений root; изменение выданной legacy-копии не меняет compiled spec/второй root; Dispose одного root не отключает другой; плохой reload сохраняет работающий action ровно с одним вызовом; legacy `(root, null)` компилируется.

Матрица: Unity 6.0 и 6.4 — UIDocument ветка; 6.5 — двухаргументный PanelRenderer callback; 6000.6+ — versioned callback и native backdrop gate. Исторические source-check проекты 6000.3.14f1/6000.6.0f1 не покрывают всю матрицу; недоступная версия остаётся unverified. EditMode: pure contracts, EditorWindow runs, GPU ownership. PlayMode: реальный UIDocument/PanelRenderer, disabled ConfigureRoot, reload, cancel/focus и teardown; один оператор, console before/after. Source-check не заменяет эти наблюдения.

Не принимать релизную совместимость при compile failure, неизученной runtime exception или skipped GPU-only evidence. Устранять только подтверждённые причины, повторять затронутые проверки; после PASS координатор обновляет receipts и проверяет package/docs/sample paths без layout rename. На этапе планирования разрешена только запись этого файла; Unity/build/test процессы не запускались.
