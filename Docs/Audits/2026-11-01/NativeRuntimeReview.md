# Runtime HTML/CSS/JS: независимое ревью границ и контрактов

Дата: 2026-11-01. База: `d271531103210ebcd71c3a5d5bbafdb06ff425a5` (`demo`, merge `main11.0.1`). Область записи ревьюера — только этот файл. Исходники, пользовательские сайты, сцены, пакеты и настройки не изменялись.

**Вердикт:** разделение «native UITK только для поддержанного IR; опциональный платформенный WebView для браузерного поведения» совместимо с текущим API библиотеки. Реального runtime-загрузчика сайта и WebView-адаптера в проверенной базе нет. Готовность Player, IL2CPP, сайтов и производительности этим ревью не подтверждается. Штатный pinned gree Windows имеет подтверждённые пробелы, препятствующие обещанию общей политики навигации и переключения возможностей без расширения адаптера/плагина.

Проверены `AGENTS.md`, runtime plan/TODO/research, текущие runtime hosting/actions/navigation/animation/responsive/effects, переносимые contracts/core converter и исходники локального pinned gree snapshot. Unity, Player, сборки, тесты, установка и загрузка DLL не запускались по ограничению задачи. Исторические результаты в plan/research не переименованы в свежую проверку.

## 1. Граница, которую нужно сохранить

- Существующий `IPageBackend` возвращает `VisualElement`, принимает `NavSpec` и выполняет page/modal/tab transitions. Это контракт уже созданного нативного UI, а не общий браузерный backend. Сохранить его API и serialized-поля `UitkScreen`; новый web surface/session должен быть отдельным контрактом. Источники: `Runtime/Navigation/IPageBackend.cs:35`, `UitkScreen.cs:17–26`.
- WebView исполняет HTML/CSS/JS в браузерном движке. Наличие WebView не доказывает преобразование DOM/JS в UXML/C# и не является реализацией native IR projector. `Auto` обязан возвращать выбранный backend, причины и неподдержанные требования; при отсутствии подходящего backend нужен явный отказ/ограниченный результат.
- Для native runtime переиспользовать модель и parser/style contracts в `Tools/HtmlToUitk/src/HtmlToUitk.Contracts` и `HtmlToUitk.Core`: их проекты уже на `netstandard2.1`. Это исходники вне Unity `Assets`, поэтому текущая runtime assembly их не получает автоматически. `UiDocument/UxmlElement`, `IHtmlParser`, `IStyleEngine` — кандидаты для адаптации; runtime projection/diff, DOM event loop и JS semantics в них не подтверждены. Нельзя просто сослаться из Player на весь CLI/Editor pipeline.
- Создавать поддержанные живые `VisualElement` через C# и явный style mapper; Editor может экспортировать UXML/USS из того же IR. Unity прямо указывает, что создание `VisualTreeAsset` из сырого UXML в runtime не поддерживается. [Unity VisualTreeAsset API](https://docs.unity.com/en-us/engine/6000.0/script-reference/unityengine/uielements/visualtreeasset).
- Browser DOM/CSS/Canvas следует сохранять вместе в одном контексте первого WebView spike. Разделение на несколько hybrid surfaces требует отдельного доказательства координат, перекрытий, focus, ввода, layout и общего состояния.

## 2. Готовые API для реального повторного использования

| Имеющийся API | Как использовать | Ограничение |
|---|---|---|
| `UitkRootConnection` | Один root/reload owner; `Poll` для UIDocument, callbacks для PanelRenderer, `Dispose` | Только native tree. Не маскирует browser surface под `VisualElement`. |
| `UitkActionBindings.Bind/Rebind/Refresh`, disposable token | Native named Button actions; смена root без двойной подписки, снятие старого lease | Это не DOM listener bridge и не общий DOM mutation observer. |
| `UitkScreen.ConfigureRoot/ReloadRoot` | Подключить созданный projector-ом root, текущие nav/anim/media metadata | Не выполняет parsing HTML/CSS/JS или загрузку URL. Не заменять повторным screen/router owner. |
| `UitkRouter` + `UiDocumentPageBackend` | Нативные page/modal/tab/focus сценарии из подтверждённого NavSpec | История браузерной SPA требует browser/session API. |
| `BreakpointController.ApplyViewport` | Явный viewport для texture hosts; существующее управление media classes | Один responsive owner на root. Не пересоздавать контроллер каждый кадр. |
| `UitkAnimator`, `StyleApplier.SupportedProperties` | Ограниченный native animation spec; существующие reduced-motion и disposal | Поддержанные style channels не равны произвольному CSS keyframes/WAAPI/JS animation. |
| gree `Gree.UnityWebView.WebViewObject` | Отдельный typed adapter к assembly `unity-webview`; штатные load/history/viewport/callback APIs | Платформенные методы и их enforcement проверять отдельно, см. ниже. |

Нативные implementation sources: `Runtime/Hosting/UitkRootConnection.cs`, `Actions/UitkActionBindings.cs`, `Navigation/UitkScreen.cs`, `Responsive/BreakpointController.cs`, `Animation/UitkAnimator.cs`, `Animation/StyleApplier.cs`. В текущей runtime assembly нет ссылок на `UnityEditor`, CLI, JS-engine или WebView; это полезная существующая граница.

## 3. Подтверждённые блокеры pinned gree

Снимок: `950cfbac14dbbf72e87548e77ef11ecc8843988e`; локальная копия вне Assets — `D:/Temp/Neoxider-HtmlDemo/spikes/gree-inspection/snapshot`. В дополнительном [offline PE-проходе](GreeBackendInspection.md) этот reviewer подтвердил SHA-256/Git blob, AMD64 PE32+ и все 36 имён Windows P/Invoke среди 38 exports. Это не execution/IL2CPP PASS: найдены UTF-8/bool/returned-string ABI hazards и source lifecycle blocker. DLL не загружалась.

### NR-01 — P1: Windows URL policy сообщает успех без enforcement

`_CWebViewPlugin_SetURLPattern` игнорирует параметры и возвращает `true`; `NavigationStarting` сообщает URL без отмены перехода. [Pinned Windows source, строки 852 и 1535](https://github.com/gree/unity-webview/blob/950cfbac14dbbf72e87548e77ef11ecc8843988e/plugins/Windows/WebViewPlugin.cpp#L1535).

Дополнительная проверка самой DLL: export RVA `0xc460` содержит `b0 01 c3` (`mov al,1; ret`), что согласуется с constant-true stub. Полный manifest и hashes — в [GreeBackendInspection.md](GreeBackendInspection.md).

**Последствие:** проверка URL в managed `started` callback не гарантирует блокировку до запроса/исполнения документа, включая redirect. Не объявлять policy enforced по успешному `SetURLPattern`. **Действие:** поддержка отмены top-level/redirect до navigation в небольшом upstream extension либо явная неподдержка требуемого ограничения. Проверять реальные requests, не только callback log.

### NR-02 — P1: browser capability не равна управлению настройкой

Windows source принудительно включает JS и внедряет `Unity.call` для документов. Сообщение моста передаётся без source origin; требуются явная граница доверия и ограниченный host API. [Pinned Windows source, строки 764–791](https://github.com/gree/unity-webview/blob/950cfbac14dbbf72e87548e77ef11ecc8843988e/plugins/Windows/WebViewPlugin.cpp#L764).

В wrapper нет общего JS toggle; `Pause/Resume` на Windows/macOS и `SetInteractionEnabled` на Windows/macOS — TODO/no-op; на iOS pause относится только к media. Текстура Windows приватна, display/input обслуживает `OnGUI`, стандартный `bitmapRefreshCycle` равен 10. [Pinned WebViewObject, строки 958, 1220, 2345](https://github.com/gree/unity-webview/blob/950cfbac14dbbf72e87548e77ef11ecc8843988e/dist/package/Assets/Plugins/WebViewObject.cs#L958).

**Действие:** различать «исполняет JS/рисует Canvas» и «умеет отключить/остановить/перенастроить». Неприменённый requested setting должен возвращаться как неподдержанный либо требующий reload; запрещён скрытый no-op. `Animations=false`, reduced motion, JS execution и media pause имеют разные semantics. Нельзя отменять весь `requestAnimationFrame`, считая его только декоративной анимацией.

WebView2 имеет собственное `IsScriptEnabled`, но смена многих settings после `NavigationStarting` действует лишь на следующий top-level документ. Это кандидат для узкого расширения, а не доказательство наличия настройки в gree. [Microsoft settings API](https://learn.microsoft.com/en-us/microsoft-edge/webview2/reference/win32/icorewebview2settings?view=webview2-1.0.3800.47).

### NR-03 — P1: optional package требует отдельной поставки и platform filters

`Neo.HtmlToUitk.Runtime.asmdef` сейчас ссылается только на `Neo.Extensions`. Не добавлять обязательную ссылку на `unity-webview` в базовый runtime. Typed adapter размещать в отдельном опциональном пакете/assembly с проверяемым install/remove workflow; одна декларация optional intent не заменяет проверку компиляции без зависимости.

У pinned UPM `unity-webview.asmdef` есть ссылка на `Unity.InputSystem`, а `package.json.dependencies` пуст. Поэтому до установки выяснить поведение consumer-проекта без Input System; не делать его обязательным для других Neo-модулей. У Windows x64 `WebView.dll.meta` явно включены Linux64 и OSXUniversal. Это ошибка фильтра поставки, не наличие backend для этих ОС. Источники: локальные `dist/package/unity-webview.asmdef`, `dist/package/package.json`, `dist/package/Assets/Plugins/Windows/x64/WebView.dll.meta`; [pinned metadata](https://github.com/gree/unity-webview/blob/950cfbac14dbbf72e87548e77ef11ecc8843988e/dist/package/Assets/Plugins/Windows/x64/WebView.dll.meta).

**Действие:** исправить filters в поставляемом optional integration, сохранить provenance и явно отметить изменённую копию. Проверить чистый base-only consumer, supported platform consumer с adapter и удаление adapter. Linux/WebGL/server нельзя определять как готовые по одному `IsInitialized()`.

### NR-04 — P1: native initialization не обеспечивает безопасный timeout/cancel

Дополнительный static source pass обнаружил stack `CreateParams`, захваченный asynchronous WebView2 callbacks, и bounded waits без подтверждённой отмены/завершения перед освобождением params/instance. Медленная/ошибочная инициализация может оставить поздние обращения к освобождённым данным; native `Init` также блокирует вызывающий поток на этих ожиданиях. Это source-level blocker для обещания safe lifetime/cancellation, а не воспроизведённое падение. Точная цепочка и teardown-event gap описаны в `GB-04` [GreeBackendInspection.md](GreeBackendInspection.md). C# wrapper над существующим Init не исправляет внутренний lifetime.

## 4. Требования к новым минимальным contracts

Это acceptance контракта, не требование строить браузер в текущем этапе.

1. Default: текущая страница; JS, поддержанные animations/interactions/assets включены. Requested settings, effective settings и unsupported requirements различимы. Capabilities фиксируются для backend + платформы + версии/режима surface, а не одной глобальной галочкой.
2. Native UITK и browser backend имеют отдельные capability IDs/явный kind. Canvas2D, WebGL, video, network, DOM/JS и управление анимациями не объединяются в универсальное «JS supported». Наличие capability не означает верифицированную изоляцию.
3. `Auto` выбирает backend до исполнения side effects. Динамический fallback не повторяет POST, формы, игровой раунд или JS initialization молча. Cancellation/Dispose не означают успешную загрузку.
4. Open/session API определяет owner, состояние открытия/ошибки, cancellation, late callbacks, viewport, Back и disposal; Unity-object операции выполняются на Unity thread. Запрос настроек возвращает applied/restart-required/unsupported/failed. Если implementation ещё нет, docs прямо называют это contract-only.
5. Crawl scope, пользовательская top-level navigation и загрузка зависимостей — разные решения. `CurrentPage` запрещает обход других документов, но не должен случайно удалить необходимые CDN assets. Host/JS bridge allowlist и resource/network limits требуют собственной enforcement boundary.
6. Same-domain использует проверенный Public Suffix resolver; при его отсутствии — явный отказ/diagnostic, без эвристики двух последних labels. Exact-host — отдельная явная опция. Для IP/localhost нужен exact-host path. Проверять IDN, trailing dot, ports, scheme, query, fragments, redirects и документные duplicates.
7. Ограничения страниц/depth/bytes/time/concurrency/redirects валидируются до операции. URL predicate не выдаётся за реализованную очередь crawler, timeout или byte limiter. Для local root нужны containment и отдельная нормализация; HTTP-only policy честно ограничивает свою область.

## 5. Lifecycle, версии, AOT и производительность

- Текущий compatibility switch для PanelRenderer уже сосредоточен в `UitkRootConnection.PanelCompatibility`; 6.6 использует versioned callback. 6.0–6.4 остаются на UIDocument, 6.5+ проверяются отдельно. Register callback может синхронно вызвать root handler: constructor/open должен выдерживать reload/reentry/disposal до возврата. [Unity callback contract](https://docs.unity.com/en-us/engine/6000.5/script-reference/unityengine/uielements/panelrenderer/registeruireloadcallback).
- Не добавлять `GetInstanceID`, editor API или reflection-only binding в новый runtime. Нужный Unity object ID — `NeoDiagnostics.StableId`. Никакая ветка `#if` не получает статус compatible до сборки на соответствующем Unity SDK.
- IL2CPP/AOT — отдельный Player receipt: runtime code generation, generic reflection и native→managed callbacks имеют ограничения; reverse P/Invoke требует поддержанного статического callback path. Обычный .NET/Mono запуск не доказывает AOT. [Unity scripting restrictions](https://docs.unity3d.com/6000.0/Documentation/Manual/scripting-restrictions.html).
- Для session обязательны late-callback/generation fencing; idempotent Dispose; release subscriptions, timers, render command buffers, textures и native view; teardown при disable/destroy, scene reload, app pause и renderer crash. Несколько документов не должны делить неподконтрольное состояние/cookies/bridge privilege. Исключение пользовательского callback не должно мешать освобождению остальных ресурсов. Это требуемые проверки, а не заявления о найденной утечке.
- `UitkActionBindings.Refresh` перевязывает все actions на attach; `BreakpointController.ApplyViewport` строит временные коллекции и обходит дерево по активным selector rules; `UitkAnimator` планирует задачи по animation/element. Их удобно переиспользовать, но не стоит вызывать полный refresh/rebuild при каждой DOM mutation. Измерить incremental updates и 1/100/1000+ элементов до назначения performance tier.
- Pinned Windows source содержит D3D11 shared texture, readback и CapturePreview paths. Нельзя выводить выбранный путь/FPS по наличию исходника или названию GPU feature. До приёмки фиксировать фактический graphics API, runtime DLL/hash, capture path, bitmap refresh cycle, frame pacing и color space. [Pinned rendering source](https://github.com/gree/unity-webview/blob/950cfbac14dbbf72e87548e77ef11ecc8843988e/plugins/Windows/WebViewPlugin.cpp#L117).
- Для UITK texture integration нужна небольшая проверенная поверхность владения texture/input, если stock `OnGUI` не подходит. Проверить двойную отрисовку, focus, IME/touch, scroll, clipping, overlay order и resize; native overlay mobile не считать texture-equivalent автоматически.
- Native browser process сам по себе не делает мост безопасным: origin + payload validation нужны после navigation и для каждого сообщения. [Microsoft WebView2 security](https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/security).

## 6. Лицензии и reuse receipt

Pinned gree — zlib: сохранять исходные notices, не приписывать себе исходный код, явно отмечать изменённые версии. [Pinned license](https://github.com/gree/unity-webview/blob/950cfbac14dbbf72e87548e77ef11ecc8843988e/LICENSE). Это не лицензия на сайт, шрифты, игровые ресурсы или Spine. При поставке отдельно фиксировать WebView2 loader/runtime/SDK и остальные native dependencies; в pinned source указан SDK `Microsoft.Web.WebView2` `1.0.3800.47` (`plugins/Windows/packages.config`), но фактический состав DLL должен подтвердить ABI-владелец.

Практический reuse-выбор: **адаптировать gree как optional browser backend**, затем расширять только доказанные пробелы policy/settings/surface. **Адаптировать существующие converter contracts/core и native helpers** для поддержанного IR. **Не включать OneJS/ReactUnity/Jint/AngleSharp/CEF как обязательные зависимости** ради наличия интерфейсов; их отдельные spikes в research остаются кандидатами, не результатами текущей интеграции.

## 7. Обязательная приёмка следующего implementation этапа

| Проверка | Наблюдаемый результат | Статус этого ревью |
|---|---|---|
| Base-only consumer, adapter install/remove, Input System present/absent | Базовый пакет компилируется без browser dependencies; понятные prerequisites optional integration | Не запускалась |
| Unity 6.0–6.6, relevant Mono/IL2CPP и platform branches | Сохранены старые API; нет editor/native binary leakage; оба compatibility paths реально собраны | Не запускалась |
| Contract unit tests | Defaults, invalid limits, unsupported settings, deterministic backend selection, cancellation state | Ожидают новых исходников |
| URL policy fixtures | PSL (`co.uk`, private suffix), IDN, exact host, IP/localhost, fragments/query, redirect outside scope, cycles; отсутствие resolver не даёт broad allow | Ожидают новых исходников |
| Local HTTP server + real backend | Redirect/top-level navigation до запроса, CDN resources, timeout/bytes/page/depth/concurrency limits; ошибки не выдаются за success | Не запускалась |
| Session ownership | Open A→B, cancel before/during ready, late load/error/message, disable/re-enable/destroy, scene reload, два документа, exception/reentry callbacks | Не запускалась |
| Native IR | Без CLI создан реальный attached tree; text/image/font/style/input/focus/actions, resize, stable node identity и reduced motion | Не реализовано в проверенной базе |
| Browser execution | Реальные JS state/click/input, CSS motion/rAF, Canvas2D/WebGL пиксели и input, Back/SPA; запрошенные выключения дают effective result | Не запускалась |
| Windows render + mobile surface | Фактический capture path, физический ввод/IME, DPI, alpha, color space, clipping, resize, overlay order | Не запускалась |
| Performance/lifetime | Заданные заранее budgets; first paint, p50/p95/p99, GC, managed/native/GPU memory, draw calls, 100 open/close и несколько views без роста retained owners | Не запускалась |
| Velora/Chicken Road | Сценарии и видео одинаковых размеров/времени; выбранный backend явно указан, игры не подменены снимками; тестовый игровой backend | Не запускалась |

В данном проходе подтверждены только прочитанные исходные контракты и перечисленные пробелы. Execution acceptance остаётся у одного Unity/Player оператора после остановки исходных writers.

## 8. Ревью новых `Runtime/Web` исходников

Это первый handoff до correction. Его первоначальные findings и hashes сохранены для traceability; текущий corrected-source verdict и принятые hashes находятся в разделе 9.

После CLI3 source-final exit 0 полностью прочитаны четыре C# файла, README, новый NUnit fixture и developer report. Во время ревью исходники и тесты не изменялись; запусков нет.

| Прочитанный файл | SHA-256 |
|---|---|
| `Runtime/Web/HtmlRuntimeOptions.cs` | `9f8ea7542057aca9697b7de5a3203b842fe71850a5b97e63e66f08e5d5609b7b` |
| `Runtime/Web/HtmlRuntimeCapabilities.cs` | `ca3c56c889e4e9b37dffbdbe41822435db164cfb42dc1d2452b226dc22bd90f0` |
| `Runtime/Web/IHtmlRuntimeBackend.cs` | `967e8c931e7a49f9655f23427b8920b656679de2d928af63ff90c6e1f878044e` |
| `Runtime/Web/HtmlUrlPolicy.cs` | `21e7b534644e60a8df900433b66a992bb4df437e0ab4ad32c63e6c9acfbd5136` |
| `Runtime/Web/README.md` | `7d6f6199c7da3eaecc7c9aa74cef6c2594cc2034a9a5c9fa6385044f37f26655` |
| `Tests/Edit/HtmlToUitk/HtmlRuntimeWebContractsTests.cs` | `4f84de83d1e5232961e43c090011dcf4d992190aafb1a4a10a0627fc803f2358` |
| `Docs/Audits/2026-11-01/RuntimeWebContracts.md` | `e57d153dfed9881cbac79a364bdc7c51132b0bc74180f2d020ab2c9ac2ef9bb9` |

Первые пять путей относительны `Assets/Neoxider/Scripts/UI/HtmlToUitk`; тест — `Assets/Neoxider`. Metadata/asmdef прочитаны точечно для ownership и discovery, не все метафайлы получили независимый полный аудит.

**Source verdict:** изоляция optional API, отсутствие обязательных browser dependencies и честное описание contract-only стадии подтверждены. Перед принятием публичной семантики требуется disposition двух конкретных API findings ниже. Реального provider по-прежнему нет; NR-01–NR-04 не исправлены этим diff и не должны считаться закрытыми.

### RWC-01 — P1: enabled-by-default превращено в безусловно required

`Assets/Neoxider/Scripts/UI/HtmlToUitk/Runtime/Web/HtmlRuntimeOptions.cs:40` задаёт `Features.All`. `HtmlRuntimeCapabilities.cs:62–64` отвергает любую включённую функцию, которой нет в backend, без учёта требований контента. Новый `HtmlRuntimeWebContractsTests.cs:44–51` намеренно закрепляет, что Native backend без Canvas отвергает default options.

**Пример:** статическая HTML/IR-страница вообще не использует Canvas/JS; нативный projector корректно поддерживает все необходимые элементы. По текущему validator default load всё равно отклоняется из-за отсутствия Canvas (а затем JS), либо вызывающий должен заранее вручную выключить незадействованные функции. `Auto` не сможет выбрать этот нативный backend с default options. Это расходится с целью «поддержанные функции включены по умолчанию» и native path для поддержанного IR.

**Минимальная коррекция:** разделить разрешённые по умолчанию функции и требуемые самим контентом/вызывающим функции. Валидировать действительные content requirements отдельно; не удалять реально использованный Canvas/JS молча. Если caller явно требует функцию, сохранить strict rejection. Добавить acceptance: default static IR → native accepted; content requiring Canvas → backend без нужного Canvas rejected/fallback; явное отключение always-on JS по-прежнему rejected. **Disposition:** первый handoff возвращён root; исправление source-closed в разделе 9, execution NotRun.

### RWC-02 — P1: scope обхода стал lifetime navigation/frame allowlist

`HtmlUrlPolicy.cs:143–144` разрешает CurrentPage только при точном совпадении seed document key. `TryResolveRedirect:92` применяет то же правило. `IHtmlRuntimeBackend.cs:32–34` требует применить policy к каждой document/frame navigation на протяжении сессии. README `47–48` и tests `235–241` прямо закрепляют отказ от redirect на другой документ.

**Наблюдаемое из кода следствие:** default `http://example.com/start` → HTTPS redirect, `/start` → canonical `/start/` и iframe `/frame.html` запрещены независимо от request budgets. Это не просто отсутствие обхода связанных страниц; семантика расширилась до запрета обычной загрузки/embedded документов и навигации. Исходная цель CurrentPage — обработать одну выбранную страницу с нужными ресурсами, не обходя link graph. Отказ документирован, поэтому это API/product mismatch, а не скрытая потеря эффекта.

**Минимальная коррекция:** явно разделить crawl admission, root canonical redirect, embedded-document/resource policy и пользовательскую навигацию; для строгого exact-document lock дать отдельный opt-in. Root redirect должен оставаться ограниченным и повторно проверяться до following; это не разрешение произвольных redirects. Тесты должны различать canonical root redirect, redirect вне разрешённой области, frame dependencies, no link prefetch и отказ от внеобластной top-level navigation. **Disposition:** первый handoff возвращён root; исправление source-closed в разделе 9, execution NotRun.

### Подтверждённые положительные границы diff

- Все новые runtime файлы используют BCL и не добавляют UnityEditor/CLI/native dependency. Старый VisualElement-based `IPageBackend` не затронут.
- Options immutable; enums/unknown feature bits и противоречивые finite limits проверяются. `SupportedFeatures` и `ConfigurableFeatures` разделены, отключение always-on feature явно rejected.
- `EnforcesNavigationPolicy` и `EnforcesLoadBounds` — обязательные provider claims для validator; текущий stock gree честно не может их заявить по NR-01–NR-04. Конструирование capabilities не объявлено verification/sandbox proof.
- SameDomain требует injected matcher либо явный ExactHost; public-suffix dependency не подменена last-two-label heuristic. IP/localhost ограничены exact host. Local files явно отвергнуты; separate resource-network policy не выдаётся за implemented sandbox.
- Контракт документирует one-load session, ownership, cleanup-before-terminal completion, canceled-before-start, timeout, stale callbacks и отсутствие автоматического Unity-thread dispatch. После success token перестаёт владеть session. Это требования к будущему provider, не доказанный lifecycle.
- Animation-off не разрешает остановить произвольный JS/rAF. URL helpers stateless, counters/dedup/atomic page reservation возложены на provider; это не выдаётся за crawler.

### Scope gaps, которые не являются проверенной реализацией

`HtmlRuntimeFeatures.Canvas:27` объединяет Canvas без определения отдельной поддержки Canvas2D/WebGL; Pixi/Spine, media и public-suffix implementation не реализованы. Перед реальным Auto selection и acceptance игры необходимы отдельные requirements/capabilities или недвусмысленное определение partial support. В этом контрактном diff реального selector нет, поэтому ложный выбранный backend не воспроизводился.

`IHtmlRuntimeBackend` предоставляет только `Capabilities`, один `LoadAsync` и Dispose. Общего typed status/diagnostic event после успешного load, истории/Back, viewport или input surface нет; поздний failure предлагается наблюдать через provider-specific documented diagnostics (`49–50`). Это честно названо в README как следующий этап, но не закрывает общий session API из плана. Не принимать отсутствие общего канала поздних ошибок за уже решённый unified runtime lifecycle.

### Проверки и покрытие: всё execution — NotRun

Новый fixture содержит source assertions на defaults, invalid flags/limits, support/configurability, URI identity, malformed references/userinfo, IDN/trailing dots, exact-host/deceptive domains, IP/IPv6/localhost, redirects и page/depth admission. Из этого не следует, что NUnit их обнаружил или они прошли на Mono/IL2CPP. Реальный PSL parser не тестируется: fixtures используют ExactHost или ручной allowlist.

Существующий `Assets/Neoxider/Tests/Edit/HtmlToUitk/Verification~/PureLogic.Tests.csproj` включает runtime `Logic/**/*.cs` и tests `PureLogic/**/*.cs`; новые `Runtime/Web/*.cs` и test в корне `HtmlToUitk` в эти globs не входят. Старый PureLogic PASS не включает новый fixture; root нужен отдельный targeted include/проект либо Unity test discovery. В Unity существующий test asmdef включает новый путь, но фактическая компиляция/обнаружение здесь не выполнялись.

Непроверены: поведение System.Uri именно в поддержанных Unity SDK (IDN/IPv6/escaping/empty userinfo), реальные HTTP redirects/frame requests, concurrency counters/bytes/time enforcement, token/Dispose races, exception cleanup, native WebView, физический ввод/рендер, perf и Player/IL2CPP. Новых тестов reviewer не писал и не запускал; изменения reviewer ограничены audit documents и ранее перечисленными offline PE evidence.

## 9. Независимое corrected-source ревью CLI03

Полностью перечитаны root-approved `D:/Temp/Neoxider-Audit-20261101/tasks/03-runtime-correction.md`, текущие четыре C# файла, README, весь fixture (455 строк) и developer report. Correction выполнена тем же CLI03 session и передана root с exit 0. **Текущий source verdict: RWC-01 и RWC-02 закрыты в проверенном исправлении; новых блокирующих дефектов в этой ограниченной области не найдено.** Это не compile/test/provider PASS. NR-01–NR-04 и все browser/Player acceptance остаются открытыми.

### Точные принятые hashes

| Файл | SHA-256 corrected source |
|---|---|
| `Assets/Neoxider/Scripts/UI/HtmlToUitk/Runtime/Web/HtmlRuntimeOptions.cs` | `1297a56d0e66fa68f36b00037923db543d4ae282a96dac126eebdbebd0046ff4` |
| `Assets/Neoxider/Scripts/UI/HtmlToUitk/Runtime/Web/HtmlRuntimeCapabilities.cs` | `9e48e32c6ad7829b40cd0506bc3307a6de4b53c8ee01322a205a3dadfa33f1f2` |
| `Assets/Neoxider/Scripts/UI/HtmlToUitk/Runtime/Web/IHtmlRuntimeBackend.cs` | `74b2b18f465aa3be4ce95b8e6f6d26ed57cadc5d43bd6e558fdddb59acda07b9` |
| `Assets/Neoxider/Scripts/UI/HtmlToUitk/Runtime/Web/HtmlUrlPolicy.cs` | `f9d5d4d1fd13392fd0a4c9d6a40a7193448d055ec1bc43f3689f9503c9a9a85c` |
| `Assets/Neoxider/Scripts/UI/HtmlToUitk/Runtime/Web/README.md` | `9f4fab4ffd5bfa1f26bb784d71e588224a90d74d3ed272fe9e57eba6b754888d` |
| `Assets/Neoxider/Tests/Edit/HtmlToUitk/HtmlRuntimeWebContractsTests.cs` | `8948df1da770301dacdf8ce610b2fd12ee94145efc68c45e0f4413aa98520f83` |
| `Docs/Audits/2026-11-01/RuntimeWebContracts.md` | `334f33ea06e2c6d53891b564c61108107b94e719391e2db7b7e9b8dc97d06ce8` |

### RWC-01 — source-closed

`HtmlRuntimeOptions` теперь явно различает `Features` preferences и `RequiredFeatures`. `ValidateOptions(options, contentRequiredFeatures)` требует явный аргумент найденных требований, объединяет его с caller requirements, отвергает требуемые unsupported/disabled функции и отключение supported always-on функции. Результат `HtmlRuntimeEffectiveSettings` содержит конкретный backend, эффективные поддержанные функции и объединённые требования. `IHtmlRuntimeBackend.EffectiveSettings` делает результат доступным caller; интерфейс требует discovery до engine/navigation effects и revalidation требований последующих документов.

Fixture `48–95` проверяет наблюдаемый публичный контракт: default Auto со статичным контентом принимается Native без JS/Canvas; реальные JS/Canvas требования отвергаются Native и принимаются подходящим Web; caller-disabled required behavior отвергается; caller/content requirements объединены; effective settings не маскируют запрошенный options объект. `103–118` сохраняет always-on-disable rejection и независимость animation/JS flags. Старое утверждение «отсутствующий Canvas всегда ломает default load» удалено из контрактного сценария. Значение `None` явно обозначено как утверждение inspector/caller, не реализация discovery.

### RWC-02 — source-closed

`HtmlUrlPolicy` разделяет `TryResolveInitialRootRedirect` (`51`), immutable `QualifyRoot` (`86`), `TryResolveCrawlPage` (`106`), `TryResolveCrawlRedirect` (`140`), `TryResolveTopLevelNavigation` (`150`), `TryResolveFrameNavigation` (`157`) и `TryResolveResource` (`167`). Default initial redirects проверяют URL/hop budget, а SameDomain — domain scope; CurrentPage больше не фиксирует преждевременно начальный document key. После commit scope якорится на итоговом root. Crawl не ограничивает browser-lifetime navigation/frames/resources.

`NavigationRestrictions` — отдельные top-level/frame flags с default None; `EnforcesNavigationPolicy` нужен лишь при явном запросе locks. Strict top-level lock намеренно распространяется на initial root redirect; capability не выдаётся notification-only backend автоматически. Request/byte/deadline/redirect budgets остаются отдельными обязательствами provider, а MaxPages/MaxDepth относятся к acquisition.

Fixture `291–335` проверяет initial HTTP→HTTPS→canonical path, квалификацию финального root, отсутствие приобретения связанных документов в CurrentPage, hop budget, invalid/userinfo/file redirects, SameDomain escape и явный strict root lock. `276–287` отдельно проверяет scope/hop limits discovered-page redirect. `338–374` проверяет, что внеобластной crawl rejected, а тот же frame/top-level/resource URL разрешён default navigation; отдельные strict locks ограничивают только заявленную область и не притворяются resource isolation. `150–158` проверяет default acceptance без enforcement claim и rejection явного strict запроса. Эти cases защищают новую семантику, а не старые restrictions.

### Оставшиеся ограничения и execution gate

Новых блокирующих изменений cancellation/ownership в интерфейсе не найдено: one-load instance, caller-owned Dispose, canceled-before-start, cleanup-before-terminal outcome и stale callback suppression остаются требованиями. Браузерной реализации, content inspector/selector и исполнения этих требований нет. `QualifyRoot`/redirect counters остаются pure helpers: provider обязан передать реально проверенный финальный hop и вести counters; API не доказывает прошедшую цепочку запросов.

Прежние scope gaps — отдельная поддержка Canvas2D/WebGL/Pixi/Spine/media, общий typed канал поздних ошибок/статуса, viewport/Back/input surface, PSL implementation, реальный resource enforcement — не закрыты и не переименованы в готовые capabilities. HTTP-only граница и отсутствие sandbox/SSRF claims сохранены. Root/API selection/provider stage должен проверять эти возможности отдельно.

Все компиляции, test discovery/execution, Unity, Player, IL2CPP, actual System.Uri platform behavior, сети, rendering/input и performance в этом recheck — **NotRun**, поскольку другой source writer ещё работает и root запретил запуск. Существующие PureLogic globs по-прежнему не включают этот fixture/Web path; будущая targeted проверка должна явно включить их. Reviewer изменил только этот audit; иных процессов/артефактов correction review не создавал.

## 10. Первый настоящий standalone run root: 57 PASS / 4 FAIL

После завершения source writers root заморозил принятые inputs из раздела 9 и выполнил отдельный реальный standalone contract run. Reviewer независимо прочитал и разобрал его TRX/JSON/freeze manifest как evidence; никаких компиляторов/тестов сам reviewer не запускал. **Компиляция успешна; total 61 / executed 61 / passed 57 / failed 4 / notExecuted 0; exit code 1.** Это исполненный FAIL, уточняющий прежнее source-only принятие. RWC-01/RWC-02 как решения API остаются source-closed, но URL-функциональность ещё не принята.

| Receipt | SHA-256 |
|---|---|
| [Raw TRX](D:/Temp/Neoxider-Audit-20261101/verification/results/web-contracts.trx) | `201ca37bdac1c63eb4edd8f92ec685a2dade9eeb3c7fd7c1ebd85862b96fb58a` |
| [Source freeze](D:/Temp/Neoxider-Audit-20261101/verification/web-contracts-source-freeze.json) | `971f7c5356960a996061ec0bbac442a11a001019c5ac338702bcaf74736ad4f2` |
| [Run summary](D:/Temp/Neoxider-Audit-20261101/verification/web-contracts-run1.json) | `c8affc0fa612d5d5702d4acfa9f20e76f0c62896d2381be6706e42a6bde463a8` |

Host: SDK `10.0.301`, target `net8.0`, NUnit `4.6.1`, adapter `4.6.0`; точная версия testhost runtime отдельно не traced, поэтому `net8.0` не выдаётся за exact runtime identity. Reviewer прочитал host project: он явно включает все `Runtime/Web/*.cs`, именно новый fixture и внешний `NUnitCompatibility.cs` с `StringAssert/CollectionAssert` legacy aliases. Этот shim находится вне Assets; Unity imports им не изменены. Post-run summary сообщает пустой список changed compiled inputs; freeze product hashes совпадают с section 9.

Четыре фактических failure:

| Test | Отклонённая допустимая reference |
|---|---|
| `CurrentPage_PreservesFragmentsButRejectsOtherDocuments` | `#new` |
| `CurrentPage_InitialHttpsAndCanonicalRedirectsQualifyOneFinalRoot` | `#section` |
| `RelativeResolution_UsesCurrentDocumentAndKeepsQueryOrder` | `../next?b=2&a=1#item` |
| `StrictTopLevelAndFrameLocks_AreIndependentOfAcquisitionAndResources` | `#fragment` |

Все четыре ожидали `true`, получили `false` и общий URL diagnostic. По freeze исходнику `HtmlUrlPolicy.TryNormalize:302–304` вызывает `Uri.IsWellFormedUriString(reference, RelativeOrAbsolute)` **до** base resolution. Root дополнительно выполнил отдельный PowerShell `.NET 10.0.11` probe: эта предпроверка вернула false для valid fragment/relative references, тогда как `Uri.TryCreate(base, reference)` и well-formed validation полученного absolute URI прошли. Probe поддерживает причину, но не приписывается точному runtime первого 61-case testhost.

**RWC-03 — P1, execution-confirmed:** корректные relative URL с fragment отвергаются до разрешения относительно базы. Root возобновил тот же CLI03 session только для исправления порядка validation/resolution согласно `tasks/03-runtime-execution-fix.md`. Требуются неизменённые assertions четырёх failing cases, сохранённые negative URL controls, независимое перечитывание final bytes, затем новый root run. На момент записи исправление ещё не handoff/reviewed; его execution статус — Pending. Не ослаблять malformed/userinfo/control/backslash/scheme/encoding checks и не игнорировать failures.

Первый run не проверяет настоящий backend, HTTP, Unity/Player/IL2CPP, browser behavior, визуальное соответствие или NR-01–NR-04. Их статусы остаются открытыми/NotRun. Исторический TRX первого FAIL должен сохраняться отдельно от результатов rerun.

## 11. Source recheck execution-backed URL fix: gate закрыт для rerun

После CLI03 exit 0 полностью перечитаны текущий `HtmlUrlPolicy.cs`, весь fixture и updated developer report. **RWC-03 source-closed; новых блокирующих source-проблем в узком исправлении не найдено. Source gate можно закрыть для нового root standalone run.** Actual corrected-source compilation/tests ещё Pending: исторические 57/4 не заменены предполагаемым PASS.

| Принятый файл | SHA-256 |
|---|---|
| `Assets/Neoxider/Scripts/UI/HtmlToUitk/Runtime/Web/HtmlUrlPolicy.cs` | `1e143356a8a4e14d9cdae20ec2fcac1075f11d0128dd2a57653453ec8d67011f` |
| `Assets/Neoxider/Tests/Edit/HtmlToUitk/HtmlRuntimeWebContractsTests.cs` | `1fe17840ce49d95ac54cdd9fb2dcc9f2ad9de89565dce6e3ae6b5d5f9e3be60f` |
| `Docs/Audits/2026-11-01/RuntimeWebContracts.md` | `f831733e802add10a93ba9e586de901c11ecfe5a76daae8c6cf13d66b2d9750f` |

`HtmlRuntimeOptions`, `HtmlRuntimeCapabilities` и `IHtmlRuntimeBackend` побайтно совпадают с run1 freeze/разделом 9. README не редактировался этим fix. Порядок validation/resolution изменён только в `TryNormalize:302–320`:

- Удалена premature `Uri.IsWellFormedUriString(reference, RelativeOrAbsolute)` до base resolution. Сохранились `Uri.TryCreate(base, reference)` и проверка well-formed полученного absolute URI.
- До resolution продолжают отклоняться null/trim differences/control/backslash; добавлены явные проверки raw ASCII space и каждого `%` через `Uri.IsHexEncoding`, чтобы resolution не чинил malformed input молча.
- После resolution сохранены HTTP(S)-only, userinfo/empty-userinfo delimiter, непустой host и IDN normalization checks. Crawl/root/strict-navigation admission, budgets, counters и cancellation interface не изменены.

Независимая read-only реконструкция старого source в памяти — обратная замена только перечисленных строк — дала точный frozen SHA `f9d5d4d1fd13392fd0a4c9d6a40a7193448d055ec1bc43f3689f9503c9a9a85c`. Это проверяет отсутствие иных скрытых source-изменений в fix, не исполняет URL-код и не доказывает прохождение tests.

Fixture сохраняет четыре исходных failing assertions для `#new`, `#section`, `../next?b=2&a=1#item`, `#fragment`. Добавлены один положительный query+fragment+valid escapes case (`192–199`) и три negative cases (`203–205`: invalid fragment percent escape, truncated query escape, raw path space with fragment). Удаление **только этих четырёх additions** в памяти даёт точный frozen fixture SHA `8948df1da770301dacdf8ce610b2fd12ee94145efc68c45e0f4413aa98520f83`: исходные 61 cases/assertions сохранены целиком, ignore/threshold weakening не добавлены.

Root следующий шаг: сохранить новый freeze/отдельный TRX и выполнить все 65 ожидаемых cases, а не только четыре прежних failures. До этого current execution verdict — Pending. Reviewer компиляторов/тестов/Unity не запускал и source не менял. NR-01–NR-04, реальные provider lifecycle/HTTP/bounds, Unity/Player/IL2CPP, input/render/performance остаются открытыми и этим source recheck не принимаются.

По дополнительному запросу root две независимо восстановленные версии run1 сохранены вне проекта: [HtmlUrlPolicy.cs](D:/Temp/Neoxider-Audit-20261101/verification/run1-sources/HtmlUrlPolicy.cs), 16,726 bytes, SHA `f9d5d4d1fd13392fd0a4c9d6a40a7193448d055ec1bc43f3689f9503c9a9a85c`; [fixture](D:/Temp/Neoxider-Audit-20261101/verification/run1-sources/HtmlRuntimeWebContractsTests.cs), 30,810 bytes, SHA `8948df1da770301dacdf8ce610b2fd12ee94145efc68c45e0f4413aa98520f83`. [Reconstruction receipt](D:/Temp/Neoxider-Audit-20261101/verification/run1-sources/reconstruction-receipt.json), SHA `84f1bda65a5f6e09f9905b1fe3ae931918f7078df84be5d258d88425154e05ae`, прямо отмечает: это post-run reconstruction с точным совпадением frozen hashes, **не оригинальные копии, сохранённые в момент запуска**. При наличии различающегося файла запись отказывалась бы его перезаписывать. Product files не изменены.

## 12. Итоговая executed acceptance: standalone profile 65/65

Root выполнил свежий run2 после закрытия source gate. Reviewer независимо прочитал `web-contracts-run2.json`, source freeze, raw TRX, console и проверил репозиторный evidence ZIP без повторного запуска тестов. **Executed result: total 65 / executed 65 / passed 65 / failed 0 / notExecuted 0, exit 0. RWC-03 закрыт для этого standalone .NET options/URL profile.** Это заменяет Pending раздела 11; первый FAIL из раздела 10 сохранён как исторический результат.

Долговременный evidence: [WebContracts-20261101.zip](Evidence/WebContracts-20261101.zip), 65,216 bytes, SHA-256 `6a9516dbde68a7a9675b38a18df15b78efd5f5b6bc6acacac583e24f049201a2`. В ZIP 25 payload members плюс `manifest.json` — 26 entries всего. Независимо пересчитаны размеры/SHA всех 25 manifest entries: 0 mismatches; CRC-проверка всех entries без ошибок. Включены оба raw TRX, receipts, freezes, console run2, dependency lockfile и обе версии семи compiled inputs.

| Run2 receipt внутри ZIP | SHA-256 |
|---|---|
| `run2/results.trx` | `f3a6355742e9423ccb59e1127f5a8a65641fe19fb823bf7f65d75b66783c6705` |
| `run2/source-freeze.json` | `37cdf3429e717d13afcb074866524b63cf26b48c329f86a3f04cc6c9b92f5115` |
| `run2/console.log` | `3ad6dc2bf691f38439d2f9fca2462cd6cbe7fe577d7fb4efe5c4b142ec102c3f` |
| `run2/receipt.json` | `6f77b9cfd2785c77e517fb7de394baab8705274a2e5c82ed743071c818885e3d` |

Независимый parse обоих TRX подтвердил 61 unique run1 test names и 65 unique run2 names; все исходные 61 сохранены. Все четыре прежних failures теперь имеют `Passed`, включая relative/query/fragment и strict-lock scenarios. Новые четыре boundary cases также Passed. Для каждого из семи compiled inputs hash archive snapshot совпал с соответствующим run freeze; текущие семь inputs совпали с run2 freeze, а post-run receipt сообщает пустой список changed inputs. Принимаются точные product hashes раздела 11 и неизменённые Options/Capabilities/Backend hashes раздела 9.

Профиль: SDK `10.0.301`, target `net8.0`, NUnit `4.6.1`, adapter `4.6.0`, внешние standalone legacy assert aliases. Exact testhost runtime version отдельно не traced; console `.NETCoreApp,Version=v8.0` описывает target, а не полный runtime identity. Run2 sources скопированы **до** исполнения. Run1 snapshots корректно помечены как post-run recovery: два файла восстановлены с точным frozen hash, пять — unchanged bytes с таким же hash. Они не выдаются за copy-at-run.

**Предел приёмки:** подтверждены только исполненные pure BCL options/capabilities/URL contract cases этого host. RWC-01/RWC-02 semantic correction и RWC-03 validation fix имеют source review и соответствующие выполненные assertions. Реальных browser/provider/HTTP callbacks, resource/network limits enforcement, physical input/rendering, Unity 6.0–6.6 compilation/tests, Player/IL2CPP, security isolation и performance этот run не проверяет. NR-01–NR-04 остаются открытыми. Результаты отдельного package checker и правки GettingStarted не пересматриваются этим reviewer и не прибавляются к числу contract passes.

Повторных запусков/компиляций, Unity или продуктовых правок reviewer не делал; изменён только этот audit. Текущий bounded review завершён, дополнительного rerun по этой области не требуется без новых изменений или failure.

## 13. Native Unity 6.3 EditMode: WebContracts 65/65 PASS

Root выполнил первый настоящий Unity batch с initialized graphics device в основном проекте. Reviewer независимо прочитал launch/log/raw NUnit XML, разобрал все leaf results Web fixture и проверил source identities, **не запуская Unity/компиляторы/тесты повторно**. **`Neo.HtmlToUitk.Tests.HtmlRuntimeWebContractsTests`: total 65 / passed 65 / failed 0 / skipped 0.** Все 65 unique test names точно совпали со standalone run2; четыре прежних URL failures также Passed. Это расширяет приёмку pure options/capabilities/URL contracts на конкретный Unity Mono EditMode profile.

**Общий batch не прошёл:** total 88 / passed 79 / failed 9 / skipped 0, process exit **2**. Второй fixture — EditorDraftTests — дал 14/23 PASS и 9 FAIL; его исправления принадлежат root/CLI05 и здесь не анализируются. Число 88 нельзя называть общим PASS или чистой native closure.

| Наблюдённый profile | Evidence |
|---|---|
| Unity | `6000.3.14f1`, engine revision `d68c3f99a318`, actual log initialization |
| Test mode | `EditMode`, filter ровно WebContracts + EditorDraftTests; assembly `Library/ScriptAssemblies/Neo.HtmlToUitk.Tests.Editor.dll` |
| NUnit/CLR | XML `engine-version=3.5.0.0`, `clr-version=4.0.30319.42000`; MonoBleedingEdge/unityjit-win32 paths в editor log |
| Graphics | Forced D3D11, actual `Direct3D 11.0 [level 11.1]`, no `-nographics` |
| Device | NVIDIA GeForce RTX3050 Laptop GPU (`0x25a2`), driver `32.0.16.2101` |
| Время | Root process 2026-11-01 20:52:29–20:53:49 UTC; Web fixture фактически исполнен 20:53:42 UTC |
| Scope owner | Launch receipt фиксирует прямое разрешение владельца на batch и `OtherEditorsControlled=false`; reviewer чужие процессы/Editor не проверял и не управлял ими |

Artifact root: `D:/Temp/Neoxider-Audit-20261101/native-20261102/edit-new-contracts-and-drafts-u63-r1`.

| Независимо прочитанный raw artifact | SHA-256 |
|---|---|
| [results.xml](D:/Temp/Neoxider-Audit-20261101/native-20261102/edit-new-contracts-and-drafts-u63-r1/results.xml) | `b714cc341f9c17fcd66f370e65a6c060e06511e4917aedca1fa6f222deb9a02f` |
| [editor.log](D:/Temp/Neoxider-Audit-20261101/native-20261102/edit-new-contracts-and-drafts-u63-r1/editor.log) | `a26270fcc2e6cf44e3a83f9ba729c87f9a8d9687ce52ba227741cb2d196568bd` |
| [launch.json](D:/Temp/Neoxider-Audit-20261101/native-20261102/edit-new-contracts-and-drafts-u63-r1/launch.json) | `b41d445b2426f1398f07715c6fcbe76c1e1f697670ae6d0cc8b558e773d991a7` |

Текущие bytes четырёх `Runtime/Web/*.cs` и fixture повторно совпали с принятыми product hashes из разделов 9/11/run2 freeze: Capabilities `9e48e32c…`, Options `1297a56d…`, Backend `74b2b18f…`, UrlPolicy `1e143356…`, tests `1fe17840…`. Root сообщил о pre/post equality всех 58 source inputs и 12 protected paths; в предоставленном native artifact folder отдельный manifest этих broad checks не найден, поэтому здесь это **root-reported**, а независимо сверенные source identities ограничены указанными пятью Web inputs. Другие Editor source edits, начинающиеся после этого run, этим отчётом не принимаются.

**Предел:** это actual Unity6.3/Mono EditMode contract execution, а не renderer/provider, PlayMode, Game View, physical input, Canvas/WebGL/RAF, Player/IL2CPP, performance или clean application teardown proof. Initialized D3D11 device не является визуальной приёмкой. Native6.6 и отдельная 6.5 compatibility branch этим run не покрыты. NR-01–NR-04 и browser WT findings остаются открытыми; девять Editor failures не скрыты успешным Web fixture. Повторный Web run без изменения его inputs/новой связанной проблемы reviewer не требует.

## 14. CTA readback r2: два реальных FAIL, причина ещё не подтверждена

Отдельный PlayMode batch `play-cta-u63-r2` исполнил ровно desktop/mobile CTA cases: **total 2 / passed 0 / failed 2 / skipped 0**, natural exit **2**, Unity6000.3.14f1/D3D11/Linear. Это отдельное evidence от 65 WebContracts PASS. Reviewer прочитал XML/launch/summary, осмотрел оба исходных PNG и независимо проверил PNG IHDR dimensions, bytes и SHA против строки capture в XML; никаких source/Unity/process/config изменений не делал.

| Exact readback PNG | Dimensions / bytes | SHA-256; совпал с XML |
|---|---|---|
| [Desktop](D:/Temp/Neoxider-Audit-20261101/native-20261102/play-cta-u63-r2/readback/velora-cta-readback-1280x900.png) | 1280×900 / 239393 | `8eb4a42380b801f60d341a679d234bf35885a1cb0b327b75bebebba376af1ef5` |
| [Mobile](D:/Temp/Neoxider-Audit-20261101/native-20261102/play-cta-u63-r2/readback/velora-cta-readback-390x844.png) | 390×844 / 116616 | `84e443099e42624056f4cc71efb1e3b65f63da78615e3eff0e88cf0c9116c7a5` |

На обоих full frames в измеренной CTA области видна белая rounded capsule без читаемой надписи. Независимое read-only декодирование pixels подтвердило **8664/8664 чисто белых RGBA(255,255,255,255)** в каждом внутреннем 228×38 crop: desktop `x424..651/y628..665`, mobile `x77..304/y584..621`. Samples `(538,661)` / `(191,617)` также белые. XML при этом сообщает resolved background `#1D4ED8FF`, target/readback `R8G8B8A8_SRGB`, 0/8664 совпадений background во всех raw/linear/gamma/mirrored controls. Поэтому различие paint именно этого target реально; значение diagnostic `glyph=5244` на белом crop не доказывает glyph/readability.

Исполненный fixture SHA-256 **`19c360a07b0eee2482ee5749d9cd33add2f494b76e24545f92817c5d478736e1`** совпал с независимо прочитанными bytes до изменения writer. Удаление только двух capture imports, capture call и нового helper в памяти дало побайтно тот же fixture, что архивирован для r1: SHA **`5a90b705abc5bf75b9d99be9e5270453a1ea114567a39c004e8a3a736f08c4cb`**. Следовательно, прежние assertions/thresholds/index/scheduling/pointer/modal path не ослаблены. Paint assertion остаётся `> total×0.45`; оба cases останавливаются на ней. Следующие glyph assertion и synthetic pointer/modal действия **не исполнились**; физический ввод не проверялся.

Raw [results.xml](D:/Temp/Neoxider-Audit-20261101/native-20261102/play-cta-u63-r2/results.xml) SHA `a323c749f4ce8b681e453a56efbad783e37df0c682f2d58dcdb17fc269316846`; editor.log SHA `4d263b517cc8f798d2f6a658fa54bed04418b10a9c50a5b2d02711314e3248a4`; launch SHA `867c56c7021c10461abaa3a4d6e0e09bbad97682d0f960d2a66828ab3ab4b819`. Root summary фиксирует 332 frozen inputs и 12 protected paths без post-run differences; reviewer не подменяет этот operator receipt отдельным аудитом всех этих путей.

**Hypothesis, не установленная причина:** исполнившийся fixture создаёт initial/resized RenderTexture с depth0; rounded overflow masks могут требовать depth/stencil. Root уже сверил exact Unity6000.3.14f1 `PanelSettings.cs:509–524` и official clearDepthStencil documentation: default clear включён, invalid depth/stencil может делать mask geometry видимой. Повторный research не выполнялся. Узкий depth24 experiment и actual depthStencilFormat diagnostics принадлежат SAME CLI02, final diff — отдельному demo reviewer; результат эксперимента Pending. Ни source-supported hypothesis, ни этот readback FAIL не доказывают дефект обычного visible Game View/продукта и не разрешают менять authored geometry/styles. Обычный Game View, visual parity, Player/IL2CPP и browser/provider остаются непроверенными.

## 15. Итоговая документация: bounded claims приняты

Полностью прочитаны текущие RU/EN `Docs{En}/UI/HtmlToUitk.md`, RU RuntimeTODO, audit README/Evidence README и operator/source-phase статусы CTAReadback/EditorWindowUX. Три устаревшие status-фразы возвращены root и исправлены: RU quick-start теперь различает выполненные native behavior tests и pending visual/physical checks; audit README больше не перечисляет уже выполненные Editor/CTA как следующую группу; EN старый source checkpoint явно помечен historical 2026-10-14, с past-tense unverified и ссылкой на текущий bounded result. **Новых блокирующих doc claims не найдено; финальная документация принята в этом scope.**

Узкий recheck трёх corrected документов зафиксирован hashes: RU guide `67305a98137b25cc724ba3ffa157f5ce98c33e1242c0bbb0526abc9f71adacc7`, EN guide `44cf8ec681195fb6de2c0d1ae2b6fbff222125cd82dfb6b2a3ba528c3c620929`, audit README `a92f8f408d9aef9e2f6410d9edcc4e549d4c73a98558919c713cedefe3f57e9a`. UTF-8 прочитан без replacement characters; prose edits не требуют нового test run.

Для проверки актуальности prose reviewer отдельно разобрал уже существующие raw XML/launch: corrected Editor **301/301**, CTA-r3 **2/2**, full Velora PlayMode **16/16**, во всех случаях failed/skipped 0 и exit0. R3 output подтверждает actual depth32 / `D32_SFloat_S8_UInt`, `clearDepthStencil=True`, прежний `R8G8B8A8_SRGB`; background/glyph counts desktop **7708/8664 + 624**, mobile **7707/8664 + 630**. Это последующий успешный framebuffer experiment в том же sampled-panel profile; Pending/hypothesis раздела 14 относится к сохранённому r2 handoff. Final fixture diff/captures/integrity ранее независимо проверены demo/closure reviewers; повторного source/ZIP audit или исполнения здесь не делалось.

Новые итоги корректно ограничены contracts, Editor behavior, sampled panel и synthetic events. Старые **79/88**, CTA depth0 failures и source-phase Pending сохранены как история и явно superseded operator results, а не переписаны в passes. Не найдено новых claims о готовом provider/crawler, обычном GUI/Game View, physical input, video/browser parity, Player/IL2CPP, measured performance, глобальном закрытии 257 действий, свежем release или merge/push в main. Архивы и статические проверки не прибавлены к contract test counts. Scope публикации текущей работы остаётся root-owned `demo`; Git действий reviewer не выполнял.

Это documentation-only acceptance. Все browser safety/NR/WT ограничения и оставшиеся visual/device gates сохраняются; никакие compiler/tests/Unity/process/config/product actions reviewer в этом проходе не запускал.
