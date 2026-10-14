# HTML/CSS → UI Toolkit

HtmlToUitk преобразует поддерживаемое подмножество HTML/CSS в UXML, USS, текстовые ключи и данные для C# runtime. Кнопки, навигация, модальные окна, вкладки, media queries и анимации работают через UI Toolkit. Браузер и JavaScript-движок в player не требуются.

Модуль перенесён из собственного HtmlToUitk в VampireDota. Канонический CLI и его лицензии, конфигурация и примеры находятся в `Tools/HtmlToUitk`. Runtime — `Assets/Neoxider/Scripts/UI/HtmlToUitk/Runtime`, editor — соседняя папка `Editor`; это отдельные сборки `Neo.HtmlToUitk.Runtime` и `Neo.HtmlToUitk.Editor`. Зависимость от Neo.UIKit не обязательна.

## Быстрый старт в редакторе

1. Подготовьте CLI по README в `Tools/HtmlToUitk`. Опубликованный файл — `Tools/HtmlToUitk/bin/htmltouitk.dll`; .NET 9 нужен внешнему инструменту, а не игровому runtime.
2. Откройте **Neoxider → UI → HTML to UI Toolkit**. Через **New** создайте `.htujob.json`, либо **Open...** откройте существующий job.
3. Укажите HTML/CSS inputs и output root. Относительные пути отсчитываются от папки job. Для импорта Unity output должен находиться внутри `Assets`.
4. Выполните Validate, изучите findings, затем Run. Отчёт содержит правило, исходный файл и строку, причину и предложение по исправлению. Watch следит за inputs с debounce; одновременно выполняется один job.
5. Назначьте созданный UXML в `UIDocument`, настройте `PanelSettings`, добавьте `UitkScreen` и назначьте необходимые `.nav.json`, `.mq.json`, `.anim.json` как `TextAsset`.

CLI определяется автоматически по каноническому publish path; следующий вариант — Release build в `Tools/HtmlToUitk/src/HtmlToUitk.Cli/bin/Release/net9.0/htmltouitk.dll`, затем совместимые старые имена и команда `htmltouitk` в PATH. Кнопка **CLI...** задаёт внешний executable или DLL; явный путь имеет приоритет. DLL запускается отдельным процессом `dotnet`, без загрузки CLI/Core DLL в Unity player. Отсутствие executable/DLL/.NET, timeout, ненулевой exit и FAIL diagnostics видны в результате. FAIL и ошибочный отчёт не запускают успешный refresh assets.

Пример job:

```json
{
  "schema": 1,
  "name": "menu",
  "inputs": { "html": ["pages/menu.html"], "css": ["pages/menu.css"] },
  "output": { "root": "../Assets/UI/MenuGenerated" }
}
```

Сверяйте поля job и conversion config с README CLI: неизвестные CSS/JS-конструкции не получают обещания браузерного паритета. Arbitrary JavaScript в runtime не исполняется.

## Выходные файлы

| Файл | Назначение |
|---|---|
| `.uxml`, `.uss` | UI tree и стили; относительные ссылки сохраняются при совместном переносе output |
| `.mq.json` | Viewport width/height/aspect/orientation и классы media queries |
| `.nav.json` | Страницы, переходы, история, модальные окна, вкладки, focus targets |
| `.anim.json` | Keyframe definitions и bindings onLoad/onShow/onHide/manual |
| `.runtime.json` | Манифест требуемых компонентов; не автоматическое исполнение JavaScript |
| `Generated/gradient-<hash>.png` | Запечённые градиенты; путь может быть настроен в CLI |
| `generated-assets.json` | Точные PNG paths, SHA256, размеры и texture policy |
| `diagnostics.json` | PASS / PASS WITH WARNINGS / FAIL, counts и findings |

Идентификаторы runtime-компонентов сохраняются: `BreakpointWatcher`, `UitkRouter`, `UitkAnimator`. Для страницы обычно достаточно одного `UitkScreen`, который создаёт необходимые C# объекты из назначенных metadata.

## C# кнопки и жизненный цикл

`RegisterAction` принимает UXML `name` или `#id`, а не произвольный CSS selector. Native `Button` вызывает обработчик через `clicked`; отдельный второй обработчик submit не добавляется. Disabled button не выполняет action. Повторная регистрация того же имени заменяет предыдущую; disposal старого токена не удаляет новую регистрацию.

```csharp
using System;
using Neo.HtmlToUitk.Navigation;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class MenuActions : MonoBehaviour
{
    [SerializeField] private UitkScreen screen;
    private IDisposable _play;

    private void OnEnable() => _play = screen.RegisterAction("play", Play);
    private void OnDisable() { _play?.Dispose(); _play = null; }
    private void Play(Button button) => Debug.Log("Play requested by " + button.name);
}
```

Без MonoBehaviour wrapper можно использовать `UitkActionBindings`:

```csharp
using System;
using Neo.HtmlToUitk.Actions;

UitkActionBindings actions = new UitkActionBindings(root);
IDisposable token = actions.Bind("save", () => Save());
IDisposable typed = actions.Bind("select", () => selectedId, id => Select(id));
actions.Rebind(replacementRoot);
// При закрытии владельца:
token.Dispose();
typed.Dispose();
actions.Dispose();
```

Один binder переносит регистрации на новый root; старые callbacks снимаются. Detach root приостанавливает actions, attach восстанавливает их. Удаление дочернего subtree отключает его callbacks и сохраняет остальные кнопки. `Refresh()` повторно ищет заменённые кнопки внутри того же root.

`UitkScreen.Configure(UIDocument, nav, anim, media, ownResponsiveLayout)` поддерживает обычный документ. `ConfigureRoot(VisualElement, ...)` и `ReloadRoot(VisualElement)` поддерживают внешний host. На отключённом компоненте root только сохраняется до OnEnable. OnDisable снимает проекции, callbacks и responsive ownership; OnDestroy освобождает action registrations. `LastError` сообщает ошибку binding, `MissingElements` — ненайденные page/modal names.

## UIDocument и PanelRenderer

`UitkRootConnection` — единственный адаптер root/reload. На Unity 6000.3 он наблюдает `UIDocument.rootVisualElement`. При наличии `PanelRenderer` в поддерживающей его Unity используется native reload subscription; Unity 6000.6 использует versioned callback. Замена tree вызывает rebuild с освобождением старого владельца.

Не создавайте параллельно несколько screen/watchers для одного root. Когда layout контролирует внешняя система, передайте `ownResponsiveLayout: false` и выберите одного владельца media classes. Сборка не требует наличия `Neo.UIKit` или игровых компонентов.

## Media queries

```json
{
  "root": "app",
  "classes": [
    { "class": "wide", "query": { "minWidth": 1280, "minHeight": 720 } },
    { "class": "card-wide", "selector": ".card",
      "query": { "minAspect": 1.7, "maxAspect": 2.5 } }
  ]
}
```

Без `selector` класс применяется к root — прежний формат сохранён. С `selector` класс применяется к совпавшим элементам, включая root, если он совпадает. Generated USS несёт конкретные значения и правила cascade; controller только переключает классы. Неподдерживаемые media selectors, неверные диапазоны и неизвестная orientation отклоняются.

`BreakpointController.Refresh()` использует размеры UI Toolkit viewport в координатах panel. Геометрия panel/root и attach обновляют состояние; начальная нулевая геометрия не активирует queries. Для render texture или внешнего host можно вызвать `ApplyViewport(width, height)` с актуальными размерами panel. Пороговые значения включительны; NaN, Infinity и неположительные размеры игнорируются. Один root имеет один responsive controller. Dispose снимает только добавленные им классы, сохраняя исходные authored classes.

## Навигация и анимации

`screen.Router` предоставляет `Navigate(pageId)`, `Back()`, `OpenModal(id)`, `CloseModal()`, `SelectTab(group, tab)` и события переходов. `WireTriggers()` берёт стабильные UXML names из nav spec. `NavigationCancelEvent` вызывает Back, если включён backOnCancel. Focus возвращается по targets/истории; modal получает focusable элемент. Эти механизмы не подменяют игровую логику: actions вызывают ваши C# команды.

`UitkAnimator` выполняет поддерживаемые keyframe channels через UI Toolkit scheduler. `ReducedMotion` и `SystemReducedMotion` применяют policy skip/simplify. `PlayManual`, `Stop`, `StopAll`, `AttachRouter` и `Dispose` позволяют управлять временем жизни. Повторный AttachRouter снимает предыдущие подписки. Dispose прекращает scheduled work. Отменённый или заменённый router run не публикует поздние completion/focus; переход восстанавливает прежние inline layout/transform values.

Локализуемый текст остаётся TextElement. Реализуйте `ILocalizer.Resolve(key)`, назначьте `screen.Localizer`, вызовите `Relocalize()`. Исходные ключи сохраняются в `LocalizationKey` после перевода. Unsupported text effects диагностируются конвертером; растеризация строки не используется как скрытый обход.

## Градиенты и импорт PNG

По умолчанию CLI запекает поддерживаемые linear/radial/conic gradients в PNG. Для неподдерживаемой адаптивной геометрии bake должен сообщить явную диагностику, а не растянуть неверный растр. Возможности и ограничения каждого CSS случая указаны в CLI docs; наличие файла само по себе не доказывает визуальную точность.

Importer всегда требует `generated-assets.json` schema 1 с exact path/SHA256, положительными размерами, `kind: gradient`, `sRGB: true`, `wrap: clamp`, `mipmaps: false`, в том числе при наличии `HtmlToUitkConfig`. Проверяются boundary пути, PNG signature и hash. Папка с именем Generated сама по себе не даёт права менять её текстуры. `HtmlToUitkConfig` может только сузить подтверждённый manifest набор через managed folders или отключить texture management; он не заменяет доказательство ownership. При управляемом импорте: sRGB, clamp, no mipmaps, uncompressed, configured filtering/alpha и Sprite/Default.

## Проверка и текущие границы

На source-final 2026-10-14: pure logic **217 passed, 0 failed, 0 skipped**. Runtime/editor и все native test sources проходят проверку типов с установленными managed DLL Unity **6000.3.14f1** и **6000.6.0f1**, без ошибок. Это не заменяет native import/PlayMode/visual QA. Одна compiler warning о private serialized reducedMotion без присваивания относится к обычному Unity serialization field.

Native fixtures проверяют action owner/disable/child/root detach, повторную регистрацию/reload, per-element media classes, stale transition completion/focus, подписки animator, восстановление authored styles и actual disabled MonoBehaviour ConfigureRoot. Последняя проверка входит в Play Mode и выходит через owned UnityTearDown.

До результата независимого Unity-оператора native rendering, PNG texture assignment, pointer/focus, screenshots 1280×720 / 1920×1080 / 3440×1440, player build и performance остаются **непроверенными**. Совместимость с другими Unity 6.0+ не объявляется протестированной.

В `Assets/Neoxider/Tests/Edit/HtmlToUitk/Verification~` лежат воспроизводимые source-check projects, pure-logic test project и SHA receipt `source-final.json`. Source checks используют установленные Unity managed DLL; DLL/obj этого каталога не импортируются Unity и не входят в поставку runtime. Запуск из корня репозитория:

```powershell
dotnet test Assets/Neoxider/Tests/Edit/HtmlToUitk/Verification~/PureLogic.Tests.csproj
dotnet build Assets/Neoxider/Tests/Edit/HtmlToUitk/Verification~/6000.3.14f1.Tests.csproj
dotnet build Assets/Neoxider/Tests/Edit/HtmlToUitk/Verification~/6000.6.0f1.Tests.csproj
```

Пути source checks отражают текущую Windows QA-машину; на другой машине настройте reference paths. Unity tests запускайте через Test Runner с единственным Editor operator. Рабочие reports/screenshots храните вне runtime module; статус Accepted определяется всеми требуемыми gates, а не одним успешным тестом.
