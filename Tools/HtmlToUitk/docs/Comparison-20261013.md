# HTML/CSS → Unity UI Toolkit: сравнительное исследование

Дата проверки: 2026-10-13. Статус: исследование завершено, source review + ограниченный CLI smoke; внедрение не выполнялось.

## План и метод

1. Проверить текущие README, контракт, исходники конвертера и runtime нашего HtmlToUitk; старое исследование 2026-10-08 использовать только как список кандидатов.
2. Найти прямые HTML/CSS → UXML/USS конвертеры; отдельно проверить Figma-импорт, предпросмотр UXML и JS/React-рантаймы.
3. Проверить первичные источники: репозитории, текущий commit, LICENSE, документацию, ключевые участки кода. Каталог и README не считать доказательством Unity-совместимости или производительности.
4. Сравнить перенос структуры, CSS, поведения, адаптации и workflow; явно отметить потери, ограничения и неподтверждённые пункты.
5. Выдать два вывода: лучший готовый прямой конвертер сегодня и лучший рабочий путь для текущего проекта; предложить проверяемый acceptance fixture.

Границы: только чтение исходников и публичных источников; только этот отчёт изменяется. Чужой код не запускается, зависимости не устанавливаются, приватный код не отправляется во внешние сервисы. Нет Unity/MCP, новых ассетов и изменений пакетов. Совместимость Unity 6000.6, визуальное качество, mobile/performance без реального прогона остаются **NotVerified**.

Обозначения: **Yes** — присутствует в проверенном коде/контракте, **Partial** — ограниченное покрытие или ручной мост, **No** — отсутствует в рассмотренном механизме/явно отвергается, **NotVerified** — нет достаточного подтверждения. Yes не означает пройденную визуальную/runtime-приёмку.

## Вывод для владельца

| Задача | Лучший подход по этому исследованию | Почему / предел |
|---|---|---|
| Готовый внешний CLI для статического HTML + CSS сегодня | **html2uuitk 0.4.0**, только для простого flex-макета | Маленький CLI, исходники и unit tests; не переносит JS, media, keyframes, полноценные тени. Это выбор по workflow, не победитель визуального benchmark |
| Быстро вручную получить UXML/USS из HTML в браузере | **HtmlToUIToolKit** | Вычисляет browser styles, удобно переносит controls/rich text; градиенты заменяет цветом и фиксирует часть размеров в текущем viewport |
| Наш игровой workflow HTML → Unity 6000.6 → C# | **Сохранить основу HtmlToUitk и завершить один сквозной маршрут**, используя существующие UI-системы игры | Диагностика/конвертация работают частично; nav/mq/anim/Bake имеют исходники, но default CLI не соединяет все части. Чужой CLI тоже потребует эти слои. В Unity наш пакет ещё не принят |
| Писать работающий UI на React/TypeScript внутри Unity | **OneJS** как первый кандидат отдельного spike; **ReactUnity** как альтернатива | Это runtime со scripting engine и ручным C# interop, не автоматический конвертер сайта в UXML |
| Дизайнер работает в Figma | **Unity Figma Connector** для нового официального пути; установленный **Neo.UIKit / FUI** для текущего пути | Другой источник дизайна. Не заменяет HTML-конвертер. FUI умеет TextCore text gradients, но его отдельный импортёр не имеет MIT-лицензии |

**Одного готового инструмента, который берёт произвольный работающий сайт и выдаёт проверенный native UI Toolkit с прежним JS-поведением, среди проверенных вариантов не найдено.** Кнопка `<button>` может стать `UnityEngine.UIElements.Button`, но `onclick="startMatch()"` не становится C# gameplay callback. HTML/CSS задают вид, C#-адаптер задаёт игровые действия, committed read-model и жизненный цикл.

## Что фактически есть у нас

Текущий код проверен на диске, а не восстановлен из исследования 2026-10-08:

- `Tools/HtmlToUitk` содержит C# Core/Rules/Convert/Bake/CLI и Unity package `com.neoxider.htmltouitk` **0.1.0**. CLI project сейчас **net9.0**; README с planned net8.0 уже отстаёт. `validate`, `convert`, `run`, `capabilities`, `rules` имеют реализации. Нельзя считать весь C# только будущим планом.
- `UxmlMapper.cs` переводит `id`/`name` в UXML `name`, controls и `data-loc`; `EmitStage.cs` пишет UXML/USS и имеет conditional writers для localization/specs/assets. **Наличие writer не доказывает, что pipeline подал ему nav/anim/mq/PNG:** default registration неполная, это проверено ниже. `HtmlRules.cs` сообщает D005 для event attributes и D009 для обычных scripts: автоматического переноса произвольного JS нет.
- Есть `UitkRouter`, `UitkAnimator`, `BreakpointWatcher`, `ILocalizer`, backend для `UIDocument` и адаптер к **Neo.UIKit**. README Unity-пакета явно отделяет typecheck/pure logic от native compile/Editor tests/визуального QA. Наличие тестовых файлов — не текущий PASS.
- В `Packages/manifest.json` установлен `com.neoxider.uikit` **0.2.0**, Git pin `433b517784d45957fa570c27377d1259d0702633`. Его package.json: Unity 6000.5, MIT. `com.neoxider.htmltouitk` в manifest **нет**. Neo.UIKit — роутер/генератор views поверх FUI-output, не HTML-конвертер; он не требует FUI importer в runtime.
- Актуальная игра уже использует **PanelRenderer**, в том числе `AlphaUiController`, и `AdaptiveLayout`. `AlphaUiController.cs:663` подписывает `button.clicked`, `:665` снимает подписку. Старое утверждение «проект сидит на UIDocument» больше не годится. Подключение нового runtime должно уважать reload/teardown текущих screens; нельзя автоматически добавить второй хозяин навигации/адаптации.
- `CliApp.cs` **preview/compare не реализованы**: выводят TODO и возвращают `ExitCodes.Pass`. Для production CI это дефект наблюдаемости; такой exit 0 не подтверждает изображение или сравнение.
- **Default CLI wiring неполное.** `ConvertServices.DefaultEmulations.Create()` регистрирует 12 базовых layout/value emulations, без gradient-bake, shadow-bake, media-breakpoints, keyframes-animator, page-router. `AnalyseStage` собирает nav/anim, но `TransformPage` создаёт пустой `SpecSink`; вызовы `SetNav`/`AddMediaClass`/`AddAnimation` в Convert найдены только как определения. В Convert не найдены вызовы GradientBaker/ShadowBaker/BlurBaker. Это отсутствие end-to-end соединения, а не отсутствие всех соответствующих библиотек.
- У `linear-gradient()` native-путь подтверждён capability matrix через assembly, **не импортом/рендером**; radial/conic native mapping не реализован этим путём. CSS `background-clip:text` не имеет готового text-gradient backend. Blur в 6000.6 имеет native API, а backdrop blur ограничен screen-space панелью.

Локальные первичные указатели: [README](D:/Git/_Fork/VampireDota/Tools/HtmlToUitk/README.md), [CLI](D:/Git/_Fork/VampireDota/Tools/HtmlToUitk/src/HtmlToUitk.Cli/CliApp.cs), [UXML mapper](D:/Git/_Fork/VampireDota/Tools/HtmlToUitk/src/HtmlToUitk.Convert/Dialect/UxmlMapper.cs), [emit](D:/Git/_Fork/VampireDota/Tools/HtmlToUitk/src/HtmlToUitk.Convert/Pipeline/EmitStage.cs), [HTML diagnostics](D:/Git/_Fork/VampireDota/Tools/HtmlToUitk/src/HtmlToUitk.Rules/Checks/HtmlRules.cs), [Unity integration/status](D:/Git/_Fork/VampireDota/Tools/HtmlToUitk/unity/com.neoxider.htmltouitk/README.md), [manifest](D:/Git/_Fork/VampireDota/Packages/manifest.json), [current controller](D:/Git/_Fork/VampireDota/Assets/_source/Scripts/Presentation/UI/AlphaUiController.cs).

## Проверенные кандидаты, версии и лицензии

Commit/date получены через GitHub API default branch 2026-10-13; ссылки фиксируют прочитанный snapshot. Version ниже — package.json, не обещание опубликованного release. Для официального пакета доступна документация конкретной версии, его код здесь не устанавливался.

| ID | Кандидат / категория | Проверенный snapshot, UTC | Лицензия / деньги | Поддержка / тесты по исходникам |
|---|---|---|---|---|
| OUR | Наш HtmlToUitk — прямой конвертер с optional C# runtime | 0.1.0; локальные файлы на 2026-10-13 | Внутренний исходник; отдельный LICENSE в Tools не найден. Стоимость внедрения — труд, не новая покупка | C# NUnit suites, golden fixtures, typecheck и Editor tests в дереве; полный текущий прогон здесь **NotRun** |
| H2 | [fake-skate/html2uuitk][H2] — прямой HTML/CSS CLI | 0.4.0; [7a31fa0][H2-commit], 2026-04-03 22:28 | package.json **ISC**, но LICENSE отсутствует в snapshot, API license=null; не утверждаем отсутствие разрешения или MIT | `node --test test/convert.test.js`; тесты не запускались. Не archived; после апреля code update не найден |
| JX | [jixinhaoqi/HtmlToUIToolKit][JX] — browser conversion | [4222679][JX-commit], 2026-06-09 12:30 | **MIT**, LICENSE.md прочитан; бесплатно по лицензии | Один HTML-файл ~76.6k chars + Editor menu. Automated tests в прочитанном дереве не найдены |
| RT | [enesunal-m/flexui / RottenUITK][RT] — HTML + Tailwind CLI | 1.0.2; [708d13c][RT-commit], 2025-11-29 14:21 | **MIT**, LICENSE; бесплатно по лицензии | Vitest files, CLI/watch, Unity Editor wrapper. README/npm metadata ссылаются на другое `rotten-games/rotten-uitk`: install URL отдельно не подтверждён |
| UP | [ReuHomi/uxml-preview][UP] — UXML → browser, не HTML-конвертер | 0.5.1; [a095b8e][UP-commit], 2026-10-13 10:43 | **Apache-2.0**, LICENSE + third-party notices | Vitest/golden/roundtrip, Yoga WASM. Авторские измерения на Unity **6000.0.40f1**, не на 6000.6 |
| FUI | [kraupins/FUI_Importer][FUI] — Figma `.fui` → UITK | 1.1.8; [b31555e][FUI-commit], 2026-07-10 11:30 | package.json **UNLICENSED**, API license=null, LICENSE не найден. Условия Figma exporter и его стоимость **NotVerified** | Один Editor importer; README требует Unity 6000.5+. Свой native прогон **NotRun** |
| FK | [ProdaZhang/figkit][FK] — Figma IR → UXML/USS + FlowBinder | [d38063d][FK-commit], 2026-09-21 09:36 | **MIT**, LICENSE прочитан | Python stdlib/golden tests; FlowBinder C#. Старый SKILL/header отстаёт от code/mapping: актуальный код уже печёт gradients/shadows |
| OJ | [Singtaa/OneJS][OJ] — React → live UITK | 3.9.8; [f36ff5e][OJ-commit], 2026-10-11 02:30 | **MIT** в текущем main; бесплатно по этой лицензии. Историческое «только платный Asset Store» не описывает текущий source | React19 + QuickJS, styling diagnostics, Editor tests, standalone build validation, profiler tests в дереве; здесь **NotRun** |
| RU | [ReactUnity/core][RU] — React/HTML-like → live UI | 0.26.0; [2eabb77][RU-commit], 2026-09-30 10:25 | **MIT**, LICENSE; QuickJS/Jint/ClearScript отдельными packages | Monorepo, Unity CI для 6000.0/6000.1 по README, tests/docs. UITK и uGUI имеют разное покрытие |
| UF | [Unity UI Toolkit Connector for Figma][UF] — официальный Figma importer | **0.1.0-exp.1**, объявлен 2026-09-18; docs проверены 2026-10-13 | Плагин заявлен бесплатным; Unity package не проверен как OSS. Figma permissions/paid modes могут ограничить workflow | **Experimental**, Unity6000.3+, dependency `com.unity.pipeline`. Пакет/тесты локально не установлены |

Список покрывает серьёзные прямые конвертеры, найденные целевым поиском, и наиболее полезные альтернативы для задачи. Это не доказательство отсутствия каждого малозаметного/закрытого продукта на рынке. Звёзды, размер репозитория и дата коммита не используются как benchmark качества.

## Прямые конвертеры: 12 критериев

В каждой колонке оценка относится к прочитанному source, кроме явно описанного нашего smoke. OUR — текущая реализация, не весь backlog. Source pointers в заголовках и следующий раздел уточняют потери.

| Критерий | OUR: [исходники](D:/Git/_Fork/VampireDota/Tools/HtmlToUitk/src) | H2: [converter][H2-code] | JX: [browser source][JX-code] | RT: [generators][RT-code] |
|---|---|---|---|---|
| 1. Статический HTML + обычный CSS → UXML/USS | **Yes**, поддерживаемый диалект + diagnostics | **Yes**, tag/property tables | **Yes**, DOM + computed styles | **Partial**, HTML + собственный Tailwind subset; `<style>`/`<link>` игнорируются |
| 2. Работающий сайт: DOM JS, framework, AJAX → прежнее поведение | **No**, scripts/event attrs диагностируются | **No**, script/iframe/canvas и др. исключаются | **No**, browser snapshot не переносит JS в UXML | **No**, scripts исключаются |
| 3. Button → native Button; C# gameplay action | **Partial**, name/control есть, nav triggers wired; gameplay callback вручную | **Partial**, Button mapping есть; атрибуты копируются без надёжного id→name; C# нет | **Partial**, `id`→name и text есть; C# callback нет | **Partial**, Button/C# builder+queries есть; game actions нет; именование расходится с USS |
| 4. Media, viewport units, aspect / responsive | **Partial**, unit/aspect код есть, BreakpointWatcher есть; default CLI mq emulation не зарегистрирована; PanelRenderer/AdaptiveLayout интеграции нет | **No** для media; vw/vh→% меняет смысл | **Partial**, flex; computed snapshot текущего viewport, at-rules удаляются; нет живых media rules | **Partial**, Tailwind breakpoints→classes; helper рассчитан на UIDocument/Screen.width; aspect советует вручную |
| 5. Фоновый gradient | **Partial**, linear native passthrough с assembly-only warning; Bake library есть, default wiring нет; radial smoke потерял gradient при PASS | **No** faithful gradient backend в прочитанном converter | **No**, gradient→average solid color | **No** gradient backend в прочитанном Tailwind pipeline |
| 6. Градиент текста CSS clip / живой текст | **No** готового backend для `background-clip:text` | **No** | **No** | **No** |
| 7. Blur и shadows | **Partial**, native filter/backdrop-filter/text-shadow; Bake/fallback классы есть, default converter wiring не подтверждено; SRP/mobile цена не измерена | **No** box shadow rendering: пишет `--unity-shadow-*`; blur backend нет | **Partial**, text-shadow; box-shadow/blur нет надёжного переноса | **No** blur; `shadow-*` принято, но рисунок тени не создаёт |
| 8. Fonts, images, SVG | **Partial**, font/resource mappings, image/slices, strict assets; SVG/display-path требует Unity import QA | **Partial**, font path map, img→VisualElement; SVG subtree пропускается | **Partial**, fonts/image source + path postprocessing; `<svg>`→Image не равен импорту inline vector | **Partial**, asset manifest для images/fonts; SVG/style/link исключаются |
| 9. Transitions, keyframes | **Partial**, USS transitions + animator library; CLI keyframe smoke дал warnings, но anim.json не создал; native execution **NotVerified** | **Partial**, отдельные USS transition props; keyframes skipped | **Partial**, transition property mapping; at-rule keyframes удаляются | **Partial**, Tailwind transitions; CSS keyframes не переносятся этим pipeline |
| 10. Router, popups и localization keys | **Partial**, names/localization keys есть; router/tabs/modal/focus в runtime source; CLI nav smoke PASS, но nav.json не создал; game integration **NotVerified** | **No** | **No** | **No** готового эквивалента |
| 11. Agent/CI: CLI, repeatability, losses и tests | **Partial**, implemented validate/convert/run, stable output contract, structured rules/tests; preview/compare TODO возвращают 0 | **Partial**, CLI+tests; warnings без strict loss gate, mapped semantics | **No** готового headless deterministic CLI/tests; browser computation зависит от viewport/resources | **Partial**, CLI+Vitest; random node ids нарушают byte repeatability; source defects ниже |
| 12. Native Unity6000.6/PanelRenderer acceptance | **NotVerified**: typecheck docs ≠ native import/runtime; package не зарегистрирован | **NotVerified** | **NotVerified**, Unity6 label не доказывает 6000.6 | **NotVerified**, Unity2021.3 minimum в metadata не доказывает PanelRenderer |

## Конкретные потери и риски из кода

### H2: лучший внешний headless baseline, но не replacement

[css2uss.js][H2-code] предупреждает о пропуске media/keyframes/font-face; mapped-value warnings тоже есть. Поэтому старый отчёт, где все mapping названы «молчаливыми», чрезмерно категоричен. Реальная проблема: warnings не заменяют строгий контракт, и результат меняет смысл — grid/block→flex, overflow scroll→hidden, fixed→absolute, vw/vh→%, em/rem→16px base. `--unity-shadow-*` — пользовательские переменные без renderer; видимой тени это не обеспечивает. [html2uxml.js][H2-html] копирует HTML attributes как есть: native name, text и unknown attributes нужно проверять отдельно. Нет game binding generator, router/localization runtime. Выбор H2 оправдан для маленького CLI-прототипа, с ручным адаптером и запретом неподдерживаемого CSS; отдавать ему финальный сложный UI автоматически нельзя.

### JX: удобный browser snapshot

[Browser source][JX-code] загружает HTML в iframe и берёт `getComputedStyle`. Это помогает с inherited/rich-text styling, но не создаёт Unity-версию работающего website. `removeAtRules()` удаляет at-rules, `getAverageColor()` превращает gradient background в средний цвет; часть textarea размеров берётся из `getBoundingClientRect()` текущего viewport. `id` становится name, Button получает text, есть ScrollView/controls и sprite-path postprocessing. Превью живёт в браузере, но JS/state/router не сериализуются в UXML. Без sandbox iframe также может исполнять HTML-script во время preview: это не перенос игры и не повод кормить инструмент приватным/непроверенным HTML. Для ручного прототипа — полезно; для CI проекта — нужен новый headless/diagnostics слой.

### RT: обещания шире текущей реализации

[parseHtml.ts][RT-html] полностью пропускает script/style/link/svg и задаёт **nanoid()** узлам. [nanoid.ts][RT-id] использует `Math.random()`. [USS generator][RT-uss] по умолчанию пишет селекторы `#node.id`, а [UXML generator][RT-uxml] при preserveNames возвращает authored HTML `id`/`data-name`. Например `<button id="play" class="bg-blue-500">` получает UXML name=`play`, но USS selector по random node id. Это **source-derived defect**, иностранный код не исполнялся. Без authored id стили могут совпасть, но выход меняется на каждом запуске. [Tailwind compiler][RT-tailwind] распознаёт `shadow-sm/md/...`, однако лишь возвращает true, не задавая тень; plain `shadow` задаёт outline-width=0. Генерация C# означает Build/Query, а не игры, подписки или router. README говорит grid, тогда как utility catalogue советует переписать grid в flex. Новый fork поверх этих дефектов сейчас хуже сохранения нашего source.

### OUR: дальше всех по нужному контракту, но не готовый production tool

Прежний reuse-study сравнивал меньший Python oracle; теперь есть C# implementation и runtime. Но ранжирование по каталогу/наличию классов завысило бы готовность: fixture expectations частично **authored**, Unity пакет не установлен, preview/compare C# отсутствуют, text-gradient backend отсутствует, linear-gradient не прошёл native import/render, а **Bake/Media/Anim/Nav default integration отсутствует**. Radial smoke PASS без PNG, animation smoke без anim.json и navigation smoke PASS без nav.json подтверждают практическую опасность. Наличие исходников не считается приёмкой сквозного результата. Адаптер Neo.UIKit игнорирует per-edge duration/transition и отдаёт visual transitions preset-ам страницы. Сохранение API/диалекта разумно, но сначала закрыть wiring/fail-closed gate. [Default registration](D:/Git/_Fork/VampireDota/Tools/HtmlToUitk/src/HtmlToUitk.Convert/Pipeline/ConvertServices.cs), [TransformPage](D:/Git/_Fork/VampireDota/Tools/HtmlToUitk/src/HtmlToUitk.Convert/Pipeline/TransformStage.cs).

## Альтернативы: они решают другую задачу

| Вариант | Что принимает / что выдаёт | Вид и сложные эффекты | Работающий Button / C# | Responsive / flow / animations | CLI / runtime footprint | Unity6000.6 и цена | Применение у нас |
|---|---|---|---|---|---|---|---|
| [uxml-preview][UP] | UXML/USS→Yoga WASM→DOM paint; parse/serialize | **Partial**: пять controls. Transitions/slices/tint сохраняет, но не рисует; font metrics отличаются | **No** gameplay: states — вход для preview, не Unity execution | **Partial** layout; нет реального ScrollView scrolling, полноценных field controls и native animations | **Yes** library для tooling; Node22+; **ноль player footprint**, если только dev-preview | Автор измерял geometry на 6000.0.40f1; **NotVerified** 6000.6; Apache2.0 | Полезный optional preview-движок/метод golden QA, не replacement converter |
| [FUI importer][FUI] + установленный Neo.UIKit | Figma `.fui`→native UXML/USS/assets; UIKit→pages/popups/views | **Partial**: PNG/fonts; **Yes** TextCore text-gradient presets в [C# importer][FUI-code]; первый text-shadow, не все эффекты | **Partial**: native controls + toolkit callbacks; gameplay actions всё равно C# | **Partial** fixed design/PanelSettings scale; UIKit routes/popups/presets, не перенос web media/keyframes | Editor import; импортёр не нужен player, UIKit C# остаётся | Unity6000.5+ declared; наша 6.6 приёмка **NotVerified**; FUI **UNLICENSED** | Сохранять существующий допустимый Figma-путь; не объявлять его HTML-конвертером |
| [FigKit][FK] | Figma capture IR `.ui.json` + `flow.json`→UXML/USS + FlowBinder | **Partial**: latest converter печёт linear/radial gradients и soft shadows, Painter2D vectors; text-gradient/blur не найден | **Partial** `IAppHook.RegisterActions`, name bindings; нужна game adapter implementation | **Partial** absolute geometry + scale, flow guards/modals/lists/checkbox, transition curves; не CSS media engine | **Yes** deterministic Python stdlib compiler + tests; no JS player, C# FlowBinder/FigVector остаются | Author smoke claim на6000.4.8f1 относится import/compile, не визуальному клику; **NotVerified**6.6; MIT | Reference для явных flow contracts/bake; смена authoring source на Figma IR нужна отдельно |
| [OneJS][OJ] | React19/TSX/USS→UITK live elements | **Partial** USS limits; runtime USS compiler/filters; FrostedGlass рендерит 3D scene blur, не весь DOM/browser. Linear gradient прямо **untested** в Styling README | **Yes** callbacks и C# interop; **No** автоматический JS→C# перенос сайта | **Partial** React state/portals/npm; media не native USS, нужны classes/code; transitions да, arbitrary CSS keyframes **NotVerified** | **Yes** headless scaffold/build/offline tools documented; player: QuickJS native binaries + C# bridge + JS bundle. Node dev-only | Unity6000.3+ declared; **NotVerified** игра/6000.6/PanelRenderer/mobile perf; MIT main | Лучший первый spike, если владелец выбирает React-runtime UI как отдельную архитектуру |
| [ReactUnity][RU] | React или HTML-like strings/styles/scripts→uGUI/UITK live components | **Partial**: gradients resolve into texture в UITK, только первый background-image; наличие UGUI WebShadow не доказывает UITK shadow/blur | **Yes** callbacks/C# interop; `HtmlEntryPoint` из website index забирает scripts/styles, а browser DOM игнорирует | **Yes** own media engine width/height/aspect, own CSS keyframes/transitions; backend limits сохраняются | **Yes** dev tooling/CI; player: выбранный JS engine + ReactUnity styles/Yoga/bridges + JS; Node dev-only | README CI6000.0/6000.1; component требует UIDocument. **NotVerified**6000.6/PanelRenderer/mobile perf; MIT | Богатый CSS/React runtime; сложнее renderer/backend contract, не plain UXML exporter |
| [Unity Figma Connector][UF] | Figma frames→UXML/USS/tokens; supported edits обратно | **Partial**: vectors rasterized; frame shadows dropped; screen-space backdrop-filter from6.6. Gradient/TextCore fidelity **NotVerified** | **Partial** реальные mapped controls/custom element types; gameplay C# не генерирует | **Partial** AutoLayout/style states/tokens; gap лишь from6000.7.0a4 по docs, на6.6 dropped; web router/keyframes нет | **Partial** UI/Pipeline connection; headless isolated export не проверен. Source результат обычный UITK, JS-engine в игре нет | **Experimental**6000.3+; free plugin, Figma mode/permissions/seat rules. Native QA у нас **NotRun** | Следить/испытать на нейтральном Figma fixture, если именно Figma authoring выбран |

Отдельная категория — **Coherent Gameface**, коммерческий HTML/CSS/JS renderer с Unity integration. Это ближе к запуску web UI, чем к конверсии в UITK: его DOM/layout/JS/renderer остаются в player, UXML/USS не выдаёт. Даже он поддерживает subset стандартов: документация фиксирует flex defaults, unsupported Grid entries, ограничения SVG/aspect и gradients (linear/radial). C# integration через явные bindings; generic browser website не гарантирован. Unity minimum2020.3.48 и desktop/mobile перечислены vendor-ом, именно6000.6/наш проект **NotVerified**. Цена по title/platform и запросу, исходники add-on; измеренных сравнительных CPU/RAM/APK numbers у нас нет. Это полезный путь лишь если сохранение web stack важнее native UITK. [Differences][GF-diff], [CSS support][GF-css], [requirements][GF-req], [C# bridge][GF-bridge], [pricing][GF-price].

Остальные найденные варианты — **TrackMan FigmaToUnity**, **natsu1211/FigmaToUnity**, **UnityUI-Transformer**, Asset Store Figma converters, **UStyled**, **ruitk/UITKX**, готовые USS design systems. Первые получают Figma, остальные получают UXML/utility/JSX или дают UI library; прямыми HTML/CSS website-конвертерами они не являются. Полный CSS/JS browser embedding (Vuplex/Ultralight/WebView) также другая категория. HTML→uGUI bakers не подходят как замена HTML→UI Toolkit. Их не ранжировали за функции, которых они не обещают.

## Рекомендация: сохранить tool, завершить узкий маршрут

**Лучший готовый внешний direct converter TODAY:** H2 для воспроизводимого простого headless макета; JX для одного ручного browser-снимка. У обоих качество конвертации нашего сложного экрана **не измерялось**, поэтому «лучший» здесь — выбор под конкретную операцию, а не абсолютный рейтинг. Если нужен фон-gradient, text-gradient, responsive animation и C# actions в одном комплекте, ни один не является готовым победителем.

**Лучший TARGET для игры:** HTML mock в ограниченном, проверяемом диалекте → завершённый C# validate/convert/run → UXML/USS/assets + specs → существующий C# screen controller/read-model/PanelRenderer. Для этого сохранить основу нашего tool, а не объявить его уже готовым: сначала доказать wiring одним acceptance spike. Source сохраняет имена controls; игровые callbacks живут в C# и снимаются при teardown. Router, AdaptiveLayout и lifecycle имеют одного владельца. Навигационный mock позволяет щёлкать страницы в браузере, но game authority в браузерный JavaScript не переносится. Если сквозное исправление потребует сопоставимой с replacement переработки, повторно принять решение на измеренном spike, не на количестве готовых классов.

Приоритет до расширения инструмента:

1. **Честный CLI gate и wiring:** unsupported/unregistered emulation должна выдавать FAIL, а не Info/PASS с потерей результата; соединить Bake/Media/Anim/Nav с output/spec sinks и защитить meaningful golden/integration tests. Implemented commands должны быть задокументированы по текущей реализации, preview/compare должны fail явным unsupported exit до реализации. Diagnostics не должны одновременно писать «gradient есть» и старое why «USS gradients запрещены». Это конкретные source-задачи, здесь не исправлялись.
2. **Native acceptance Unity6000.6/PanelRenderer:** импорт двух generated screens, Button callback ровно один раз после reload, teardown, popup/focus, Resize с существующим AdaptiveLayout. Typecheck не заменяет этот gate.
3. **Gradients отдельно:** background linear native syntax проверить импортом/снимком. При отсутствии доказательства — явный asset bake или rejection. Для живого gradient текста — TextCore preset/custom element mapping как отдельная функция; сохранять локализуемый текст, не тихо rasterize весь заголовок. FUI показывает готовый принцип, но его UNLICENSED source не копировать.
4. **Responsive contract:** mq либо остаётся runtime watcher, либо компилируется в классы существующего AdaptiveLayout. Назначить одного владельца размеров, не включать обе независимые системы на один root. Проверить min/max aspect и wide landscape.
5. **Optional preview:** uxml-preview оценить после native эталона; быстрее получить обратную связь агенту, но точность шрифтов/field controls/filters остаётся Unity gate. Python compare oracle можно использовать как текущий отдельный tooling route; C# compare не притворяется готовым.

### Стоимость перехода против доведения

Оценки ниже — **планировочное суждение с низкой уверенностью, не замер труда**. Человеко-дни опытного разработчика, без очереди единственного Unity-оператора, без новой художественной production/полного website support. Уточняются после одного acceptance spike; провал native gradient/lifecycle может увеличить сроки.

| Путь | Узкий первый результат | Дополнительная работа до нашего нужного MVP | Вывод |
|---|---|---|---|
| H2/JX вместо ours | 0.5–2 дня: подключить export и получить простую native структуру | 5–12 дней: strict diagnostics, localization, IDs, media/anim/nav/game lifecycle, gradient gaps и native QA; большая часть уже есть в нашем source | Дешевле только если отказаться от большей части требований |
| Довести части HtmlToUitk | 2–5 дней: fail-closed/CLI/docs и закончить Bake/Media/Anim/Nav default wiring на fixture | Ещё 3–7 дней: PanelRenderer/native integration, устранение import/runtime defects, один adaptive/router backend и background gradient; live text-gradient отдельные 1–3 дня при пригодном TextCore пути | Не доказано дешевле по времени, но сохраняет согласованный диалект/локализацию/C# и уже написанные части. Spike должен подтвердить выбор |
| OneJS | 1–3 дня: isolated React screen + C# callback на target platform | 5–15 дней: перенести реальный screen state/controllers/USS, lifecycle/DI/architecture tests, SDK/backend/device gates. Произвольный сайт не входит в оценку | Меняет architecture/workflow, зато даёт React hot reload и dynamic UI |
| ReactUnity | 1–3 дня: isolated UITK backend fixture + callbacks/media | 5–15 дней: backend feature parity, PanelRenderer host, engine/IL2CPP/mobile, state/controller migration, profiling | CSS runtime богаче; переход не бесплатный |
| Figma routes | 1–2 дня: neutral frame→native import | Design conversion HTML→Figma и игровое поведение заново; объём не оценён без исходного дизайна | Выбирать по источнику дизайна, не ради замены converter name |

Смена на Node/browser converter не устраняет стоимость missing CSS/runtime features. OneJS/ReactUnity могут сократить authoring code, но добавляют JS engine/interop и новые runtime failure modes. Нельзя назвать их «медленными» или «быстрыми для mobile» без профиля одинаковой сцены: все comparative CPU/GPU/RAM/GC/APK metrics здесь **NotVerified**.

## Acceptance fixture для решения о внедрении — DESIGNED, NOT EXECUTED

Нужен маленький нейтральный fixture, не копия реального игрового интерфейса. Один layout с explicit IDs, два landscape viewport **1280×720** и **3440×1440**, RU/EN локализация. Direct converters получают одинаковые static HTML/CSS; альтернативы получают эквивалентный React/Figma input, и это учитывается как цена адаптации. Screenshot browser↔Unity не является доказательством callback/game action.

| Часть | Вход | Обязательный проверяемый результат |
|---|---|---|
| Background gradient | Panel 320×160, `linear-gradient(135deg,#6a58ff 0%,#18d8ff 50%,#ffbd65 100%)` | Native render либо явно marked baked asset; три контрольных цвета, угол и rounded clip видны. Никакой average/first-color silent fallback |
| Text gradient | RU/EN заголовок с `background-clip:text`, transparent text, тот же three-stop gradient | Живой локализуемый текст + подтверждённый TextCore/custom-element путь; если не поддержан — точный reject. PNG текста не считается равным live text |
| Button action | `id="start-match"`, locale key `fixture.start`; browser mock даёт counter | В Unity `Q<Button>("start-match")` существует. Явный C# адаптер вызывает probe-controller `StartMatch()` один раз на pointer click и keyboard/gamepad submit; повторный reload не удваивает callback |
| Layout | Flex main/sidebar; media `max-width:1400px`, media `min-aspect-ratio:21/9`, card `aspect-ratio:2/1` | На двух landscape размерах нет overflow/clipped button; sidebar меняет ширину/позицию согласно spec; resizing не запускает конкурирующие watchers |
| Popup | `open-settings`, modal settings, `close-settings`; declared nav edges | Open/close/escape/back, focus restoration; hidden popup не ловит click; переход не пишет в simulation |
| Motion | opacity/scale 0→50→100% keyframes 800ms alternate + hover transition150ms | Промежуточный frame отличается от двух endpoints; reduced motion; teardown/reload отменяет running animation без stale callbacks |
| Assets | Local PNG, imported SVG icon, real font supporting Cyrillic, shadow/blur pane | Missing file/font diagnostic; icon/font действительно импортированы. Blur screen-space vs world-space различается явно, без ложного PASS |
| Loss/CI contract | Добавить второй вариант с JS onclick, unsupported selector/filter | Losses с местом/пояснением, nonzero strict verdict; `--force` явно записан. Повторный supported input даёт одинаковые bytes; no timestamp/random IDs |

Минимальный control-to-game bridge — не результат HTML-конверсии:

```csharp
var start = root.Q<Button>("start-match");
Action action = controller.StartMatch;
start.clicked += action;
// Screen teardown: start.clicked -= action;
```

В реальной игре controller должен работать через принятый Presentation/read-model port, с существующим lifecycle root. Это пример смысла acceptance, код в проект не добавлялся.

Предлагаемый запуск после source-final: first CLI conversion + schema/byte-repeatability, затем один Unity оператор native import/compile, интерактивный fixture и снимки на двух landscape размерах, затем target mobile/player profiling. Для выбора сравнивать одинаковый экран/ассеты, записать время authoring/import, CPU/GPU/GC/RAM/build-size. Эти измерения **NotRun**.

## Что действительно запускалось

Иностранные repositories не клонировались и не исполнялись; source читался через GitHub API/raw и official docs. Dependencies не устанавливались. Ограниченный smoke выполнен на уже существующем DLL нашего CLI, **не rebuild текущего source**:

- DLL: `Tools/HtmlToUitk/src/HtmlToUitk.Cli/bin/Debug/net9.0/htmltouitk.dll`, SHA256 **BA7AD8C940EEB1313FAA7F7ECDB0789C0553961F3A135575F80209D9D359DFE4**, LastWriteUTC **2026-10-08T22:20:42.3002210Z**. Source сегодня прочитан, binary может отставать; его smoke не считается свежим compile proof.
- `dotnet <dll> rules`: exit0, **110 rules**. `preview` и `compare`: оба exit0 + **not implemented (W3 TODO)**. [Receipt](../QA/2026-10-13-uitk-comparison/own-cli-smoke.txt).
- `convert <S33-linear-gradient-native/input/page.html> --config <S33/config.json> --out C:/AI/outputs/uitk-comparison-20261013/own-gradient-smoke --capabilities <spec/capabilities.6000.6.json> --format json`: exit0, **PASS WITH WARNINGS**, errors0, warnings2/C011. Написал UXML+USS+diagnostics; оба gradient value сохранены в USS. Это conversion proof старого binary, **не native gradient render proof**. `why` у C011 остался старым/противоречивым. [Receipt](../QA/2026-10-13-uitk-comparison/own-gradient-smoke.txt), [generated USS](C:/AI/outputs/uitk-comparison-20261013/own-gradient-smoke/Screens/Gradients.uss).
- S34 radial-gradient: тот же convert/options с own-radial-smoke output, exit0/**PASS**, Info `gradient-bake`, но **PNG отсутствует**, `background-image` исчез из Orb.uss. [Receipt](../QA/2026-10-13-uitk-comparison/own-radial-smoke.txt), [USS](C:/AI/outputs/uitk-comparison-20261013/own-radial-smoke/Screens/Orb.uss).
- S39 keyframes: exit0/**PASS WITH WARNINGS**, warnings2 `keyframes-animator`; только UXML/USS/diagnostics, **anim.json отсутствует**. [Receipt](../QA/2026-10-13-uitk-comparison/own-S39-keyframes-spec.txt).
- S43 pages/nav/modal/tabs: exit0/**PASS**, Info `page-router`; только UXML/USS/diagnostics, **nav.json отсутствует**. [Receipt](../QA/2026-10-13-uitk-comparison/own-S43-pages-nav-modal-tabs.txt).
- S30 media negative-input: exit1/**FAIL** из-за `@media print` (C007), warnings2 `media-breakpoints`; conversion refused, output не создан. Это проверка reject/diagnostics, **не доказательство responsive output**; никаких portrait/layout runtime tests не выполнялось. [Receipt](../QA/2026-10-13-uitk-comparison/own-S30-media-breakpoints.txt).

**NotRun:** fresh build/unit suites всех кандидатов, Unity import/compile/EditMode/PlayMode, screenshots, callbacks, PanelRenderer/router integration, full acceptance fixture, mobile/player performance. Их нельзя считать PASS из README, testcase count, version minimum или одного CLI exit0.


### Долговечные копии receipts

2026-10-13: существующие текстовые smoke receipts скопированы в Docs/QA/2026-10-13-uitk-comparison/; DLL, generated output и внешние байты не копировались. CLI повторно не запускался. SHA256 каждого оригинала и копии совпал побайтно.

| Receipt | Байты | SHA256 оригинала = SHA256 копии |
|---|---:|---|
| [own-cli-smoke.txt](../QA/2026-10-13-uitk-comparison/own-cli-smoke.txt) | 6639 | 12018F3AA5E92CD614A8BC79452237D0B5A0734B2333982D920690F36AC9B4A4 |
| [own-gradient-smoke.txt](../QA/2026-10-13-uitk-comparison/own-gradient-smoke.txt) | 1679 | ACFD7869E596C5A696AB7124EDF708F8CC2B7D9B4AA4D9B1DD55A98AE207578D |
| [own-radial-smoke.txt](../QA/2026-10-13-uitk-comparison/own-radial-smoke.txt) | 1051 | 570CA8932F84C18593649AA414E0288A774FA28BDC2230F4A0EEF2AD54F6FD63 |
| [own-S30-media-breakpoints.txt](../QA/2026-10-13-uitk-comparison/own-S30-media-breakpoints.txt) | 2518 | E63DAD43F542D2F8C5BDE9B66AB802A93D525B5E42A84ED9AC0B87EF7F028E02 |
| [own-S39-keyframes-spec.txt](../QA/2026-10-13-uitk-comparison/own-S39-keyframes-spec.txt) | 1745 | 452CF4932EBEC4FA3D93A1F4DF01A153BE2A0613A7D80952B3A1DE7CB5926BE4 |
| [own-S43-pages-nav-modal-tabs.txt](../QA/2026-10-13-uitk-comparison/own-S43-pages-nav-modal-tabs.txt) | 1098 | 4A04F0BC4F7A7118F642680A4171E93FEB9D87ECA42445DA2B1AF10FEC7C1E8E |

## Первичные источники

Внешние ссылки с commit pin соответствуют code audit 2026-10-13. Official web docs могут обновиться позднее; package version и дата чтения зафиксированы выше. Unity6000.6 manual подтверждает наличие `aspect-ratio`, `filter`, `backdrop-filter`; он **не подтверждает**, что произвольный CSS website воспроизводится в Unity. [USS reference][UNITY-USS], [filter guide][UNITY-FILTER], [PanelRenderer API][UNITY-PANEL].

[H2]: https://github.com/fake-skate/html2uuitk/tree/7a31fa0cb540a3951d60f3bf7a95817a3f9b868c
[H2-commit]: https://github.com/fake-skate/html2uuitk/commit/7a31fa0cb540a3951d60f3bf7a95817a3f9b868c
[H2-code]: https://github.com/fake-skate/html2uuitk/blob/7a31fa0cb540a3951d60f3bf7a95817a3f9b868c/src/css2uss.js
[H2-html]: https://github.com/fake-skate/html2uuitk/blob/7a31fa0cb540a3951d60f3bf7a95817a3f9b868c/src/html2uxml.js
[JX]: https://github.com/jixinhaoqi/HtmlToUIToolKit/tree/4222679ef33ea050b44bd5b160a715f57d7b583b
[JX-commit]: https://github.com/jixinhaoqi/HtmlToUIToolKit/commit/4222679ef33ea050b44bd5b160a715f57d7b583b
[JX-code]: https://github.com/jixinhaoqi/HtmlToUIToolKit/blob/4222679ef33ea050b44bd5b160a715f57d7b583b/Tools/HTMLTools/HTML%E8%BD%ACUIToolKit%E5%B7%A5%E5%85%B7.html
[RT]: https://github.com/enesunal-m/flexui/tree/708d13cd40f9d01f7008f5d7ce9fb5b2b8c2c3a7
[RT-commit]: https://github.com/enesunal-m/flexui/commit/708d13cd40f9d01f7008f5d7ce9fb5b2b8c2c3a7
[RT-code]: https://github.com/enesunal-m/flexui/tree/708d13cd40f9d01f7008f5d7ce9fb5b2b8c2c3a7/src/core/generators
[RT-html]: https://github.com/enesunal-m/flexui/blob/708d13cd40f9d01f7008f5d7ce9fb5b2b8c2c3a7/src/core/html/parseHtml.ts
[RT-id]: https://github.com/enesunal-m/flexui/blob/708d13cd40f9d01f7008f5d7ce9fb5b2b8c2c3a7/src/utils/nanoid.ts
[RT-uss]: https://github.com/enesunal-m/flexui/blob/708d13cd40f9d01f7008f5d7ce9fb5b2b8c2c3a7/src/core/generators/uss.ts
[RT-uxml]: https://github.com/enesunal-m/flexui/blob/708d13cd40f9d01f7008f5d7ce9fb5b2b8c2c3a7/src/core/generators/uxml.ts
[RT-tailwind]: https://github.com/enesunal-m/flexui/blob/708d13cd40f9d01f7008f5d7ce9fb5b2b8c2c3a7/src/core/tailwind/compileTailwind.ts
[UP]: https://github.com/ReuHomi/uxml-preview/tree/a095b8ec7bd65ffaddf0bda1e3357eee54e8a6a8
[UP-commit]: https://github.com/ReuHomi/uxml-preview/commit/a095b8ec7bd65ffaddf0bda1e3357eee54e8a6a8
[FUI]: https://github.com/kraupins/FUI_Importer/tree/b31555e8dcda4f2ff22af0a09dd39439d457ba4d
[FUI-commit]: https://github.com/kraupins/FUI_Importer/commit/b31555e8dcda4f2ff22af0a09dd39439d457ba4d
[FUI-code]: https://github.com/kraupins/FUI_Importer/blob/b31555e8dcda4f2ff22af0a09dd39439d457ba4d/Editor/AiMultiToolKitFuiImporter.cs
[FK]: https://github.com/ProdaZhang/figkit/tree/d38063d2ee4df5c02e7468e112a26af92b24f748/figma2unity
[FK-commit]: https://github.com/ProdaZhang/figkit/commit/d38063d2ee4df5c02e7468e112a26af92b24f748
[OJ]: https://github.com/Singtaa/OneJS/tree/f36ff5e41a16eeddf40e775f28fa7eee856894a0
[OJ-commit]: https://github.com/Singtaa/OneJS/commit/f36ff5e41a16eeddf40e775f28fa7eee856894a0
[RU]: https://github.com/ReactUnity/core/tree/2eabb779defbb5d4c733723f50d13d43004b9cf6
[RU-commit]: https://github.com/ReactUnity/core/commit/2eabb779defbb5d4c733723f50d13d43004b9cf6
[UF]: https://docs.unity3d.com/Packages/com.unity.ui.figma@0.1/manual/index.html
[UNITY-USS]: https://docs.unity3d.com/6000.6/Documentation/Manual/UIE-USS-Properties-Reference.html
[UNITY-FILTER]: https://docs.unity3d.com/6000.6/Documentation/Manual/ui-systems/uss-filter.html
[UNITY-PANEL]: https://docs.unity3d.com/6000.6/Documentation/ScriptReference/UIElements.PanelRenderer.html
[GF-diff]: https://docs.coherent-labs.com/unity-gameface/what_is_gfp/htmlfeaturesupport/
[GF-css]: https://docs.coherent-labs.com/unity-gameface/content_development/supported_features_tables/cssproperties/
[GF-req]: https://docs.coherent-labs.com/unity-gameface/what_is_gfp/requirements/
[GF-bridge]: https://docs.coherent-labs.com/unity-gameface/integration/ui_scripting/js_binding_unity/
[GF-price]: https://coherent-labs.com/pricing/
