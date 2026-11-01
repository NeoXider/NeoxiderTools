# Optional platform WebView: feasibility перед реальным spike

Дата: 2026-11-01. Область: read-only toolchain/source discovery и план isolated Windows x64 spike. Product code, manifests/config, Assets/Packages, Unity и пользовательские приложения не изменялись. Компиляторы/MSBuild, DLL, Unity/Player, UI и сетевые порты не запускались; установки не выполнялись. Выполнена только readonly утилита `vswhere` и лёгкое чтение/хеширование файлов.

**Вывод:** переиспользование gree/WebView2 технически обосновано, но локальная native build readiness **не установлена**. Visual Studio/MSBuild присутствуют, зарегистрированные C++ tools и Windows 10/11 SDK не найдены. Поставленная DLL и wrapper имеют source-confirmed lifetime/ABI/bitmap/ownership пробелы. Следующий узкий этап — подготовить изолированный upstream patch/build-input manifest для этих пробелов и обеспечить подтверждённый C++ toolchain; запуск нынешней DLL не закрывает их. Собственный browser/DOM engine не нужен.

## 1. Зафиксированные inputs и reuse

- gree/unity-webview pin `950cfbac14dbbf72e87548e77ef11ecc8843988e`; локальные 13 verified файлов — `D:/Temp/Neoxider-HtmlDemo/spikes/gree-inspection/snapshot`. DLL SHA-256 `2f9031885bba4c02720de14401e372f1ac7d4d9e6953453cdd6e2719e46046d3`. [Предыдущий offline PE/ABI review](GreeBackendInspection.md) подтверждает AMD64, все 36 Windows P/Invoke exports и ограничения этих доказательств.
- Existing `WebViewObject` уже даёт load/history/visibility/margins, browser callbacks и Windows rendering/input. Его нужно адаптировать; загрузка страницы в browser surface не является конвертацией DOM в UXML.
- [Чистые runtime contracts](NativeRuntimeReview.md) уже имеют 65/65 standalone contract PASS. Они не являются renderer/provider. Для gree нельзя автоматически объявить `EnforcesLoadBounds=true`: полнота request/decoded-byte/redirect/deadline enforcement ещё не реализована.
- Не импортировать Jint/AngleSharp/CEF/OneJS как дополнительный engine ради этого spike. Объём missing glue ограничен lifetime/interop/policy/settings/surface над уже существующим WebView.

## 2. Readonly toolchain discovery

Машинный receipt: [toolchain-discovery.json](D:/Temp/Neoxider-Audit-20261101/backend-feasibility/toolchain-discovery.json), SHA-256 `0a03c5a2e3bef4ae3f3a951a2df2fc57e48c029b1e2b747efc22f4d00ccda5ba`. Содержит исходные результаты трёх `vswhere` queries, checked paths, cached SDK hashes и header-to-pinned-tree comparison. Это inventory, не compiler execution.

| Проверка | Наблюдение | Практический вывод |
|---|---|---|
| Visual Studio | Community 2026, `18.7.11925.98`, complete/launchable по vswhere; `C:/Program Files/Microsoft Visual Studio/18/Community` | IDE существует; C++ support отдельно |
| MSBuild | `MSBuild/Current/Bin/MSBuild.exe` существует | Не запускался; файл не доказывает наличие compiler/linker/SDK |
| `vswhere -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64` | `[]` | Registered MSVC x86/x64 tools не найдены |
| `vswhere -requires Microsoft.VisualStudio.Workload.NativeDesktop` | `[]` | NativeDesktop workload не найден |
| `VC/Tools/MSVC`, VCToolsVersion.default.txt | отсутствуют | `v145` compiler installation не подтверждён |
| VC metadata | `VC/Auxiliary/Build` содержит redist metadata, в том числе version `14.51.36231`; MSBuild VC folders существуют | Redistributable/targets metadata не равно `cl/link` |
| `Get-Command msbuild, cl, dumpbin` | executable не найден в текущем PATH | Не доказывает глобальное отсутствие custom toolchains; стандартный discovery не дал runnable native tools |
| Windows Kits | `C:/Program Files (x86)/Windows Kits` содержит только `NETFXSDK`; стандартные `10/Include`, `10/Lib`, `10/bin` отсутствуют | Нужные Win32/WinRT/C++/WinRT headers/libs не найдены в стандартном SDK root |
| KitsRoot10 registry | native/WOW6432Node Installed Roots не дали путь | Альтернативный SDK root не установлен этим discovery |
| WebView2 Runtime directory | `C:/Program Files (x86)/Microsoft/EdgeWebView/Application/156.0.4314.58` существует | Текущий directory receipt; runtime не запускался и installation health не проверена |

**BF-01 — build blocker:** нельзя заявить возможность локально пересобрать patched DLL на основании одного MSBuild. Требуются подтверждённые `v145` compiler/linker, Windows SDK `10.0` с WinRT/C++20 inputs и согласованный build profile. Проверять/предоставлять их отдельно; ничего не устанавливалось. Наличие C++ workload является отдельным выбором Visual Studio, что подтверждает [официальная документация Microsoft](https://learn.microsoft.com/en-us/cpp/build/vscpp-step-0-installation?view=msvc-170).

## 3. Native project, SDK и header/lib completeness

`WebViewPlugin.vcxproj` декларирует `PlatformToolset=v145`, `stdcpp20`, Windows SDK `10.0`, `WebView2LoaderStatic.lib` и `Microsoft.Web.WebView2` **1.0.3800.47**. Pinned snapshot не содержит `WebViewPlugin.sln`, SDK `packages/...1.0.3800.47`, `Unity/*.h` или всё дерево upstream. Это ограниченный inspection snapshot, не установленный UPM/build tree. Для сборки `.sln` не обязателен при прямом project build, но отсутствующие includes/libs обязательны.

| Input | Локальная доступность |
|---|---|
| Pinned SDK 1.0.3800.47 | В snapshot-relative packages и стандартном NuGet cache не найден; в проверенных spike roots тоже отсутствует |
| Cached SDK 1.0.4314.35 | Есть в `D:/Temp/Neoxider-HtmlDemo/spikes/webview2-host/packages/microsoft.web.webview2/1.0.4314.35` |
| Cached native inputs | `build/native/include/WebView2.h`, x64/x86/arm64 `WebView2LoaderStatic.lib`, package/nuspec/license/notice присутствуют |
| Нужные COM interfaces в cached header | Найдены Environment3, Controller2, ICoreWebView2_22, TrySuspend и другие versioned APIs; это наличие declarations, не runtime feature test |
| Unity headers | Все три есть в установленном Unity `6000.6.0f1/Editor/Data/PluginAPI` и **побайтно совпадают с pinned gree Git blob** |

Точное header reuse доказательство:

| Header | Bytes | Git blob, совпавший с pinned tree |
|---|---:|---|
| `IUnityInterface.h` | 8990 | `ab713ec64d64f1cf0b093de7530181fb8e6a17fe` |
| `IUnityGraphics.h` | 3576 | `721f42ea2c0ead6f6a494a8c12415fe77ce3fda8` |
| `IUnityGraphicsD3D11.h` | 1371 | `b94f6aca38c822983ad555f7fd6ea22f648716fe` |

**BF-02 — reproducibility blocker:** newer cached WebView2 SDK не подменяет pin автоматически. Для изолированного build выбрать либо точную 1.0.3800.47, либо отдельный явно зафиксированный SDK-upgrade profile с hash/license/compatibility checks. Изменение include path на cached 1.0.4314.35 без записи версии не является воспроизводимой pinned сборкой. Существующие точные Unity headers можно переиспользовать локально, сохранив notices; загрузка копий из случайного проекта не нужна.

## 4. Текущий capture/refresh route: три разных пути

Строки ниже относятся к pinned локальным `plugins/Windows/WebViewPlugin.cpp` и `dist/package/Assets/Plugins/WebViewObject.cs`.

| Путь | Native source | C# consumption / cadence |
|---|---|---|
| Unity D3D11 + WGC shared texture | UnityPluginLoad/device callback получает Unity D3D11 device (`1341–1408`); CreateCaptureDevice выбирает его (`307`); WGC pool (`545`); render-thread event drains newest frame и CopyResource в shared texture (`484–518`) | Видимый wrapper каждый Update выдаёт plugin event и читает texture pointer (`2256–2296`), создаёт `CreateExternalTexture`. `bitmapRefreshCycle=10` этот render-event path **не** ограничивает |
| WGC private D3D11 + CPU readback | Без Unity D3D11 interface — отдельный device, включая WARP fallback (`314–327`); FrameArrived→staging Map→BGRA buffer (`377–437`) | CPU upload выполняется только при `refreshBitmap` (`2298–2325`); default cycle 10 ограничивает представленную свежесть, хотя native capture может обновляться чаще |
| CapturePreview fallback | Если WGC/visual tree недоступны — PNG capture/decode; native Update постит capture лишь когда refresh requested и нет capture in progress (`1650–1662`, `1101–1150`) | CPU buffer→Texture2D.Apply на refresh interval; стоимость PNG/decode/upload измеряется отдельно |

Default `bitmapRefreshCycle=10` при Unity 60 Update/s номинально разрешает около 6 CPU upload opportunities/s; это арифметика gate, не измеренный FPS. Native zero-copy всё ещё выполняет GPU CopyResource и не означает нулевую стоимость. D3D12/Metal/Vulkan нельзя считать shared D3D11 path. Фактическая DLL/graphics API/selected path проверяются в будущем Player; текущий source trace этого не исполняет.

**BF-03 — bitmap resize memory-safety gap:** `_CWebViewPlugin_Render:1706–1714` не принимает destination capacity и копирует текущие `bitmapWidth*bitmapHeight*4` под lock. Wrapper отдельно читает width/height, выделяет/pins buffer, затем вызывает Render (`2300–2325`); FrameArrived/capture callback может заменить размеры между этими операциями. При resize больший snapshot может не поместиться в старый buffer. Source TODO прямо требует capacity и единого locked snapshot. Это подтверждённый небезопасный API path, не воспроизведённый crash. Минимальный patch — bounded copy с capacity/result dimensions/version одного snapshot и явным retry, а не ещё один independent width query.

**BF-04 — GPU lifetime gap для нескольких views:** retired shared textures хранятся на три render events, а не три фактических Unity frames (`289–302`, `1399`). Несколько instances за один кадр могут ускорить retire; source TODO это отмечает. Для release/multi-view нужен frame/fence-aware ownership. Первый spike ограничить одной view; это не исправляет/принимает multi-view lifetime. Проверить resize→draw→destroy/device reset и отсутствие render events после teardown.

Wrapper callback dispatch (`2198–2245`) не ограничен по времени/числу сообщений и не изолирует исключения пользовательских callbacks; source TODO отмечает, что это может прервать texture update. Native MessageQueue также unbounded. В adapter spike нужны ограниченный payload/queue ownership и callback exception isolation; flooded bridge не должен блокировать кадры/cleanup. Не считать добавление общего timeout доказательством этих гарантий.

## 5. HWND, controller visibility и RAF

STA создаёт layered transparent popup с `WS_EX_NOACTIVATE`, position `(primaryScreenWidth+100,0)`, alpha 1/255 и `ShowWindow(SW_SHOWNOACTIVATE)` (`1281–1303`). Controller получает bounds и `IsVisible=TRUE` (`925–927`); composition root видим. Это намеренно показанное ОС окно для DWM/WGC, а не полностью hidden controller. Position за правым краем **основного** монитора не доказывает положение за пределами всего multi-monitor desktop.

`SetVisibility(false)` wrapper вызывает native controller `IsVisible=false` и затем прекращает draw/render update; HWND при этом сам не скрывается (`1153–1193`, native `1085–1089`). Microsoft подтверждает, что скрытый controller не рисуется и Chromium может throttling animations/tasks; это может объяснить frozen motion в прежнем hidden-host fixture, но не доказывает исправление RAF в видимом Unity path. [Microsoft IsVisible remarks](https://learn.microsoft.com/en-us/dotnet/api/microsoft.web.webview2.core.corewebview2controller.isvisible?view=webview2-dotnet-1.0.3800.47).

`TrySuspend` — отдельный best-effort механизм, требующий invisible controller; он приостанавливает timers/animations и может ждать уже исполняющийся script. Его нельзя подменять точным JS cancellation или decorative-animation toggle. [Microsoft suspend contract](https://learn.microsoft.com/en-us/dotnet/api/microsoft.web.webview2.core.corewebview2.trysuspendasync?view=webview2-dotnet-1.0.3800.47). Stock gree Windows Pause/Resume этого API не вызывает.

Следовательно, будущий fixture измеряет отдельно `document.visibilityState`, RAF counter/timestamps, CSS transform и реально presented Unity frame ID при normal display, explicit hide/show, minimize/restore и capture-route switch. `IsVisible=true`, alpha/offscreen HWND или растущий JS counter сами по себе не являются доказательством живой Unity-картинки. В этом feasibility UI не открывался.

## 6. Самый маленький upstream-adaptation diff

Не копировать весь plugin в base package. Использовать отдельную optional integration и bounded fork/patch поверх pin с явными изменениями:

| Файл/область | Необходимая узкая адаптация |
|---|---|
| `plugins/Windows/WebViewPlugin.cpp` | Исправить stack CreateParams/async init и destroy/event lifetime из GB-04; bounded bitmap copy; наблюдаемые capture path/frame counters; explicit settings and optional cancel-before-request navigation; не писать browser renderer |
| `dist/package/Assets/Plugins/WebViewObject.cs` | Явные UTF-8 и bool widths/returned string ownership; error/callback fencing; validated refresh cycle≥1; bounded-copy ABI; один texture/draw/input owner и минимальный публичный texture/input bridge при нужде UITK |
| `plugins/Windows/WebViewPlugin.vcxproj`, `packages.config` | Только согласованные SDK/toolset/header inputs и выбранный version pin. Никаких незаметных глобальных retarget/install действий |
| Windows x64 DLL + `.meta` optional package | Пересобранная DLL получает новый hash/provenance; исправить Linux/macOS filters. Не выдавать старую DLL за новый patched source |
| `unity-webview.asmdef`, optional package `package.json` | Зафиксировать реальный Input System prerequisite/dependency и supported platforms, сохранив независимость base Neo |
| Новый тонкий optional adapter assembly | Reference к `unity-webview` + `Neo.HtmlToUitk.Runtime`, typed session ownership/capabilities/effective settings/diagnostics. Surface/history/viewport над штатными APIs; старый `IPageBackend` не менять |
| Новые isolated fixture/test files вне base package | Контролируемый HTML + native lifecycle/copy guards + consumer install/remove. Они не author/rebuild пользовательские сцены |

JS on/off требует native settings before navigation/reload, interaction-off — реальной остановки input owner, animations-off — отдельного поддержанного declarative mechanism. Strict nav — cancel-before-request для requested top-level/frame restrictions; existing `SetURLPattern=true` stub не использовать как enforcement. Если механизм не реализован, capability/configurability=false и явный отказ. Cookie/profile isolation, arbitrary script termination и all-request byte quotas не обещать по этим правкам.

**BF-05 — provider conformance blocker:** чистый low-level browser-surface spike и соответствующий всем `IHtmlRuntimeBackend` bounds provider — разные milestones. Stock WebView API не предоставляет подтверждённого decoded-byte контроля всех документов/frames/scripts/network requests. First spike может испытывать trusted fixture напрямую через patched upstream API, но до полного enforcement не регистрируется как рабочий provider с `EnforcesLoadBounds=true`. Не ослаблять base contracts, чтобы скрыть этот пробел.

### Input System / install-remove

Pinned `unity-webview.asmdef` ссылается на `Unity.InputSystem`, но package dependencies пусты. Узкий первый вариант — объявленный prerequisite только **optional integration**, без обязательной зависимости base Neo. При необходимости поддержки plugin без Input System её исследовать отдельной небольшой adaptation; условные `using` не делают отсутствующую asmdef reference опциональной автоматически.

Проверочная matrix: base-only consumer без Input System; base-only с Input System; optional integration с заявленными dependency pins; снятие optional integration с сохранением baseline package manifest; отсутствие native DLL/editor reference в base build. Не удалять общие зависимости, уже принадлежавшие consumer. Отсутствующий required plugin должен означать отсутствие adapter package/понятный install prerequisite, а не broken assembly base Neo.

## 7. Лицензии и provenance

gree core — zlib: notices сохранить, modified sources явно пометить. Unity headers в установленном SDK содержат **Unity Companion License**, и точные bytes совпали с pinned headers; не объявлять их zlib. Отдельный репозиторий Unity NativeRenderingPlugin имеет MIT root license, но это не переопределяет notices конкретных headers. [Official Unity example license](https://github.com/Unity-Technologies/NativeRenderingPlugin/blob/master/LICENSE).

Cached WebView2 NuGet `1.0.4314.35` содержит собственные Microsoft `LICENSE.txt` и `NOTICE.txt`; retain package notices и отдельно фиксировать SDK/loader/runtime distribution. Cached package license не является лицензией на любой browser/сайт/Spine assets. WebView2 Runtime требуется на target machine независимо от static loader. [Microsoft distribution guidance](https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/distribution). Инструменты Visual Studio и Windows SDK не копируются в package. Никакая лицензия/версия нового native build ещё не принята этим feasibility.

## 8. Изолированный spike: acceptance и rollback

Сначала фиксируется nonce-owned временный project root вне `Assets/Packages` основной библиотеки, dependency manifest/pins/hash, одна Windows x64 view, fixed viewport 1280×720/DPR1, graphics API и локальный owned fixture. Порты не обязательны для первого `LoadHTML` fixture; HTTP/redirect тесты идут отдельным следующим разрешённым шагом. Ниже — будущие проверки, все **NotRun**:

1. **Build prerequisites:** подтверждённый compiler/linker/SDK inventory, exact includes/libs/licenses; compile/export/PE checks новой DLL и явный ABI version. Отсутствие prerequisites возвращает конкретный blocker, без попытки «запустить старую и считать исправленной».
2. **Failure-safe native lifecycle:** missing Runtime, init error, artificially delayed init past deadline, cancel/destroy during create/capture/resize, повторное dispose. После завершённого cleanup нет owned STA thread/window/callback/event/view; old generation не публикует success. Native init не блокирует Unity thread на 10+5s.
3. **Interop:** ASCII/русский/emoji HTML, JS, URL, callback и keyboard round-trip; bool false/true; borrowed/allocated string ownership; bitmap resize с guard canaries и capacities, GPU destroy/reset lifetime. Ни один output buffer не перезаписан вне capacity.
4. **Browser behavior:** vanilla JS state/click/input, RAF counter, CSS animation timestamps, Canvas2D/WebGL known pixels и pointer interaction. Одновременно растут fixture frame ID и sampled presented-frame ID; frozen texture не проходит при живом JS counter.
5. **Capture matrix:** D3D11 shared texture, private-device readback и CapturePreview fallback принудительно различаются и логируются. `bitmapRefreshCycle` 1/10 и invalid0; resize/DPI/linear-vs-sRGB, minimize/restore/hide/show. Зафиксировать реальное HWND/controller/visibility состояние и multi-monitor location; capture border/alpha/input не угадываются по source.
6. **Controls:** поддержанные JS/declarative animation/input flags действительно меняют результат; unsupported settings явно rejected. Strict nav tested before request/redirect on local controlled endpoints; browser bridge payload/origin limits отдельно. Никогда не отключать всю game RAF logic ради animations-off.
7. **Performance:** для каждого path снять 10s steady sample и first-paint, delivered unique frames, RAF/present latency p50/p95/p99, CPU/GC/native/GPU memory, 100 open/close, resize и два views. До performance tier/root release зафиксировать hardware-specific budgets; отсутствие бюджета означает измерительный spike, а не «плавный/быстрый backend».
8. **Integration:** отдельный adapter возвращает реальные effective settings, поддерживает old/base-only consumers и install/remove matrix. Full backend acceptance требует доказать load-bound enforcement, не только `LoadHTML` картинку. Velora/Chicken Road — последующий controlled scenario с разрешёнными ресурсами/test backend, не первая native safety проверка.

Rollback: сохранять baseline manifests/import settings, patch и hashes до запуска; nonce output только вне user projects. Закрывать лишь созданные session/native windows/threads/processes по записанным ownership IDs, не по имени browser process; shared WebView2 Runtime/пользовательские Edge/Unity не закрывать. Удаление optional package возвращает exact baseline references; installer/toolchain changes в rollback spike не входят, поскольку их нет. Если cleanup hang/late pointer/capacity violation обнаружен — этот path FAIL, сохранить crash/log/core evidence и исправить upstream owner, не продолжать серию unsafe trials.

## Узкий следующий шаг

**Доступно сейчас без запуска:** подготовить isolated upstream patch/build-input manifest с точными локальными Unity headers, списком отсутствующих `v145`/Windows SDK/pinned WebView2 inputs и patches GB-04/BF-03/ABI; оставить product package неизменным. После предоставления/подтверждения toolchain — один isolated native build и lifecycle/capacity fixture, затем единственная видимая browser surface. Ни renderer, ни package integration здесь не объявляются готовыми; BF-01/BF-02/BF-05 требуют отдельного решения/доказательства перед соответствующими milestones.

Активных процессов feasibility reviewer не оставил. Полезные source gaps направлены root; этот отчёт и один JSON notes receipt — единственные созданные артефакты данного этапа.
