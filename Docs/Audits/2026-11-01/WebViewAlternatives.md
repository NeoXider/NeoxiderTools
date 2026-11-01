# Ready Windows WebView alternatives: factual reuse pass

Проверено 2026-11-01 по primary GitHub REST metadata, pinned upstream source/docs/licenses и registry metadata. Область — gree как baseline и **две** OSS-альтернативы; список не расширялся до трёх ради количества. Никакие DLL/tarballs не скачивались и не исполнялись; packages, Unity, компиляторы, UI и порты не запускались. Product files/config не изменены.

**Результат:** найден готовый по составу Windows/UITK компонент **WebViewToolkit** с published DLL и исходниками, заслуживающий whole-component comparison до ручного gree patch/toolchain install. Source review обнаружил у него собственные AOT/lifetime/resize gaps; это не принятый безопасный renderer. **UnityWebBrowser 2.2.8** — другая реальная CEF texture surface, но upstream явно исключает IL2CPP. gree HEAD не изменился относительно проверенного pin: свежих исправлений Windows для переиспользования сейчас нет. Архитектуру выбирает root после этого evidence; quota-conformant provider — отдельный milestone.

## 1. gree freshness — более нового исправленного HEAD нет

Прямой REST query default branch на момент проверки: `master` → **`950cfbac14dbbf72e87548e77ef11ecc8843988e`**, commit 2026-10-22 03:50:35 UTC, message `update binaries`; repository не archived. Это **точно прежний pin**. `/releases/latest` вернул 404. Поэтому HEAD-vs-pin Windows diff пуст по идентичности commit; lifetime/ABI/capture findings предыдущих отчётов не закрыты новым upstream. [Pinned commit](https://github.com/gree/unity-webview/commit/950cfbac14dbbf72e87548e77ef11ecc8843988e).

Search-index README всё ещё описывает CapturePreview/default cycle 3 и отсутствие prebuilt DLL; для вывода использованы реальные pinned tree/bytes/source, где DLL есть и routes/default cycle отличаются. README freshness не подменяет byte evidence. Лицензия core — zlib, exact Unity headers — UCL; подробности в [GreeBackendInspection](GreeBackendInspection.md) и [WebBackendFeasibility](WebBackendFeasibility.md).

## 2. Краткое сравнение кандидатов

| Component | Реальная Windows surface / reuse | Upstream Unity/AOT statement | Наше execution evidence |
|---|---|---|---|
| gree, `950cfbac…` | WebView2, D3D11 WGC/shared texture, readback/CapturePreview; source + inspected prebuilt | Generic Unity5+ README; наши 6.0–6.6/IL2CPP не доказаны | Только offline PE/source, никаких renderer tests |
| WebViewToolkit, source `0a60c025…`, package `ee271a59…` / v1.3.0 | WebView2, готовые `WebViewInstance.Texture`, UITK `WebViewElement`, DX11/DX12 source, release DLL | Root README: tested Unity6000.3; package minimum2021.3; package README claims IL2CPP | Source-only; явные callback/resize/lifetime blockers, см. ниже |
| UnityWebBrowser 2.2.8, `486610f7…` | CEF separate engine process, BGRA pixels→Unity Texture2D, JS/input APIs, published Win-x64 engine package | Unity2021.3 baseline, newer versions untested; **IL2CPP not supported** | Source/metadata only; нет Unity6/browser measurements |

Все три размещают настоящий browser engine, а не DOM parser/static screenshot converter. HTML/CSS/JS engine path подтверждён source. Ни Canvas2D/WebGL contexts/pixels, ни Velora/Chicken Road здесь не исполнялись: наличие WebView2/CEF не равно доказанным capability нашего приложения.

## 3. WebViewToolkit: provenance и whole-component API

Лицензия — [MIT](https://github.com/cantetfelix/WebViewToolkit/blob/ee271a59088d67f735bc49bc025091da4d1d9fe9/LICENSE). Default source master **`0a60c025194a05e1d0994c3db53feac9ef33adbe`** от 2026-01-29; [release v1.3.0](https://github.com/cantetfelix/WebViewToolkit/releases/tag/v1.3.0) опубликован 2026-01-29. UPM branch и tag v1.3.0 resolve в **`ee271a59088d67f735bc49bc025091da4d1d9fe9`**. Все восемь runtime `.cs` blobs package совпали с соответствующими source-master blobs.

| Artifact | Primary metadata; не локально проверенные binary bytes |
|---|---|
| Release `WebViewToolkit.dll` | 303,616 bytes; GitHub asset digest `sha256:a4aece55e3cf7d7eec6d55760de91b6673ae945933f46b45298b89fdc415c05d` |
| Pinned UPM `Runtime/Plugins/x86_64/WebViewToolkit.dll` | 303,616 bytes; Git blob `fdfa19e7e52ef4f09e0f6747a12ec6587be2a583` |
| DLL в source master | 301,056 bytes — отличается от published package artifact; нельзя подменять UPM/tag произвольным master subfolder |
| Source CI run | [Build and Deploy UPM](https://github.com/cantetfelix/WebViewToolkit/actions/runs/21481710418), source SHA `0a60c025…`, API reports `completed/success` |

Workflow делает native CMake build, копирует DLL в package, subtree-publishes UPM и создаёт tag/release; это publisher-side provenance chain, не reproducible-build/ABI proof. DLL не скачана: сравнение release SHA-256 с UPM Git blob по самим bytes, exports/imports/архитектура и соответствие native source остаются отдельной offline-проверкой. [Pinned CI workflow](https://github.com/cantetfelix/WebViewToolkit/blob/0a60c025194a05e1d0994c3db53feac9ef33adbe/.github/workflows/cicd.yml).

### Public API и package shape

`WebViewManager.CreateWebView(width,height,initialUrl,enableDevTools)` возвращает `WebViewInstance`: `Texture`, `Navigate/NavigateToString`, `ExecuteScript`, Back/Forward, `Resize`, normalized mouse/wheel, virtual-key input, navigation/message events и Dispose. Text APIs используют `LPWStr`/native `wchar_t`, большинство control flags — `int`; это существенно готовее простого `LoadURL` wrapper. [Pinned instance API](https://github.com/cantetfelix/WebViewToolkit/blob/ee271a59088d67f735bc49bc025091da4d1d9fe9/Runtime/WebViewInstance.cs).

Готовый UITK element отображает texture и переводит mouse/wheel events, создаёт/уничтожает view при attach/detach. Проверенная реализация element не содержит полноценного typed text/IME/focus bridge: наличие низкоуровневого `SendKeyEvent` не доказывает его wiring в UITK. `IsReady` означает лишь ненулевой, ещё не destroyed managed instance; native environment/controller создаются асинхронно. Upstream tests даже используют Resize как readiness probe. [Element source](https://github.com/cantetfelix/WebViewToolkit/blob/ee271a59088d67f735bc49bc025091da4d1d9fe9/Runtime/UIToolkit/WebViewElement.cs).

`package.json` 1.3.0 объявляет empty dependencies, но runtime asmdef жёстко ссылается на `Unity.InputSystem`; install/remove без него требует проверки/явного prerequisite только optional integration. Runtime и UITK assemblies включают Editor и WindowsStandalone64. DLL importer явно Windows Editor x86_64 + Win64, Linux/macOS disabled — лучше проверенного gree metadata. [Runtime asmdef](https://github.com/cantetfelix/WebViewToolkit/blob/ee271a59088d67f735bc49bc025091da4d1d9fe9/Runtime/WebViewToolkit.Runtime.asmdef), [DLL importer](https://github.com/cantetfelix/WebViewToolkit/blob/ee271a59088d67f735bc49bc025091da4d1d9fe9/Runtime/Plugins/x86_64/WebViewToolkit.dll.meta).

Настройки source ограничены главным образом initial URL/size/DevTools и navigation/script calls; подтверждённых public controls JS-off, declarative animation-off, network quotas, cancel-before-navigation/redirect, bounded script termination не найдено. Не объявлять их configurable/enforced по наличию ExecuteScript. Native build использует WebView2/WIL/vcpkg; их notices/UCL Unity headers и WebView2 Runtime поставка остаются отдельными условиями поверх root MIT.

### Точный capture route

Native создаёт layered/no-activate host HWND за правым краем primary monitor, alpha1, controller visible; WinRT composition→Windows Graphics Capture frame pool. Это настоящий native window для capture, несмотря на формулировку README «no overlay windows»; UI/RAF/окклюзия и multi-monitor требуют проверки. [Native WebView source](https://github.com/cantetfelix/WebViewToolkit/blob/0a60c025194a05e1d0994c3db53feac9ef33adbe/WebViewToolkitPlugin/src/WebView.cpp#L142).

DX11 использует Unity device и GPU copy; Y-flip реализован CopySubresourceRegion по строкам. DX12 содержит D3D11On12 wrapping, **CPU staging Map/readback/upload** и WaitForGPU с `WaitForSingleObject(INFINITE)` — не сплошной zero-copy путь. Общий C# Tick hardcodes 0.033s interval (~30 update opportunities/s), независимо от display rate. Это source mechanism, не измеренные FPS/latency. [DX11](https://github.com/cantetfelix/WebViewToolkit/blob/0a60c025194a05e1d0994c3db53feac9ef33adbe/WebViewToolkitPlugin/src/RenderAPI/RenderAPI_D3D11.cpp), [DX12](https://github.com/cantetfelix/WebViewToolkit/blob/0a60c025194a05e1d0994c3db53feac9ef33adbe/WebViewToolkitPlugin/src/RenderAPI/RenderAPI_D3D12.cpp#L525).

### WT-01 — AOT/ABI: IL2CPP claim нельзя принимать без correction/Player receipt

Package README прямо заявляет IL2CPP compatibility. Однако manager регистрирует instance methods `OnNativeLog/OnNativeNavigation/OnNativeMessage`; они не имеют AOT annotations. `OnNativeDeviceEvent` помечен MonoPInvokeCallback, но тоже **instance**. Это не соответствует документированному Unity requirement static reverse-P/Invoke callback + annotation. Source-level blocker, не воспроизведённый IL2CPP crash/build failure. [Manager callbacks, строки 272–299](https://github.com/cantetfelix/WebViewToolkit/blob/ee271a59088d67f735bc49bc025091da4d1d9fe9/Runtime/WebViewManager.cs#L272), [Unity AOT restrictions](https://docs.unity3d.com/6000.0/Documentation/Manual/scripting-restrictions.html).

Дополнительно NavigationCallback marshals bool как `UnmanagedType.Bool`, native typedef использует C++ bool; log LPStr получает native UTF-8. Явные widestring APIs лучше gree, но этот callback ABI тоже требует строгой проверки/согласования. Source не доказывает безопасный cross-backend marshalling.

### WT-02 — asynchronous lifetime не решён whole-component reuse автоматически

`InitializeWebViewEnvironment` и composition-controller completion захватывают raw `[this]` (`WebView.cpp:213,254`); Create возвращается до ready. `DestroyWebView` немедленно удаляет unique_ptr (`WebViewManager.cpp:209–215`). В read completion handlers нет outstanding-create cancellation/weak lease/проверки живого owner перед обращением к `this`. При destroy-during-create возможен late use-after-free path. Это подтверждённый source lifetime gap; работу normal teardown и реальный crash здесь не запускали. [Native manager destroy](https://github.com/cantetfelix/WebViewToolkit/blob/0a60c025194a05e1d0994c3db53feac9ef33adbe/WebViewToolkitPlugin/src/WebViewManager.cpp#L209).

Глобальный Shutdown использует «abandonment» strategy; source пропускает Controller.Close/DestroyWindow во время quitting, но это не является bounded session cleanup или устранением raw async lifetime. Event-registration tokens не удержаны для явного unsubscribe. Device restore вызывает callback из native lifecycle; managed code прямо обновляет Unity textures без доказанной dispatch boundary. Нужны native failure/cancel/domain reload tests и source correction там, где lifetime не удержан.

### WT-03 — resize failure / GPU ownership

DX11 `ResizeSharedTexture:136–147` сначала Release старого texture, затем создаёт новый. При ошибке Create `WebView::Resize:440–443` не заменяет старый `m_texturePtr`, а в конце `474` возвращает Success. Это оставляет stale ownership state на allocation failure. Исправление должно быть transactional: создать replacement до release и вернуть реальную ошибку. Dimensions mismatch check при copy помогает старым кадрам, но не чинит этот failure path.

Managed Dispose сначала вызывает native destruction, затем уничтожает external Texture; native destroy/resize не показывает Unity-frame lease для старого native pointer. Render manager защищает update-all mutex, но Resize вызывает WebView после GetWebView, уже отпустив mutex. Resize/destroy/device reset/два views должны пройти проверку синхронизации GPU и Unity ownership; безопасный lifetime нельзя вывести из одного `ID3D11Texture2D::Release`. [Native resize](https://github.com/cantetfelix/WebViewToolkit/blob/0a60c025194a05e1d0994c3db53feac9ef33adbe/WebViewToolkitPlugin/src/WebView.cpp#L426).

Отдельного CPU destination-buffer copy API, как unsafe gree `_Render`, здесь нет; риск находится в pointer/texture replacement и capture-thread synchronization. Capture Shutdown закрывает session/pool и освобождает visuals, но source-only read не подтверждает отсутствие in-flight work/late callbacks.

### Compatibility evidence

Root README заявляет tested **Unity6000.3**; package minimum **2021.3** не является тестом всех новых Unity. Для **6000.6**, Mono Player, IL2CPP, actual Canvas2D/WebGL, физического ввода/IME и clean teardown в этом исследовании execution evidence нет. CI metadata подтверждает native build workflow, не Unity/Player тесты. Upstream fixtures содержат lifecycle/resize assertions, но часть navigation сценариев проверяет только отсутствие crash/returns; они не заменяют кадры/DOM-state/native input verification. Whole component полезен как кандидат для controlled comparison, но не снимает WT-01–WT-03.

## 4. UnityWebBrowser: существующий CEF texture pipeline, Mono-only предел

Лицензия core — [MIT](https://github.com/Voltstro-Studios/UnityWebBrowser/blob/486610f765e914071e47c648da99fb96b502e3b8/LICENSE.md). Latest release остаётся **2.2.8**, tag **`486610f765e914071e47c648da99fb96b502e3b8`** (2026-01-07), CEF143.0.13; master **`f49743d847688fff8f2128be3472602691e90cc6`**. REST compare показывает два дополнительных commits release относительно master без content diff (`files=[]`); source metadata/pin не смешиваются молча. [Release](https://github.com/Voltstro-Studios/UnityWebBrowser/releases/tag/2.2.8).

Есть Windows-x64 engine package через официальный VoltUPR. Primary registry version metadata дал tarball URL и `dist.integrity=sha512-OhSGeqLqTGZCqs/pzSlv+q3GG2/5IcV8bHEp6Uqdm6GUIo4ic+y4VZcEbbwzn8i7EYY1Z0amyifO343KpV1Jxg==`, shasum `0ce162ea1f46dc56583409e4c825a489691311bd`. Tarball не получен: composition/native executable hashes не проверены локально. GitHub release не имеет приложенных assets. CEF/CefGlue/native runtime notices и distribution требуют отдельного inventory.

`WebBrowserClient` exposes `BrowserTexture`, JS/input/navigation и Dispose; separate self-contained .NET10 CEF engine исполняет сайт. `CefWindowInfo.SetAsWindowless` + `OnPaint` копирует BGRA pixels, RPC/communication передаёт их Unity, NativeArray/Texture2D.Apply загружает кадр. Accelerated paint handler пуст: это CPU-pixel path. [Client](https://github.com/Voltstro-Studios/UnityWebBrowser/blob/486610f765e914071e47c648da99fb96b502e3b8/src/Packages/UnityWebBrowser/Runtime/Core/WebBrowserClient.cs#L650), [Render handler](https://github.com/Voltstro-Studios/UnityWebBrowser/blob/486610f765e914071e47c648da99fb96b502e3b8/src/UnityWebBrowser.Engine.Cef/Shared/Browser/UwbCefRenderHandler.cs#L98).

Input methods send mouse move/click/scroll/keyboard to CEF; raw-image input handler готов. Disposal отменяет operations, отправляет engine shutdown, освобождает communications, при живом engine вызывает owned-process kill и dispose, затем освобождает native arrays. CEF shutdown постится на UI thread. Это готовые механизмы, не проверенный clean teardown или process sandbox. Linux source форсирует NoSandbox; из этого нельзя делать тот же вывод о Windows default.

[Setup docs](https://github.com/Voltstro-Studios/UnityWebBrowser/blob/486610f765e914071e47c648da99fb96b502e3b8/docs/articles/user/setup.md) прямо говорят **IL2CPP NOT supported**, поскольку запуск separate process требует unsupported System.Diagnostics.Process; Unity2021.3 baseline, newer versions untested. Windows Mono/Editor — candidate profile, Unity6.3/6.6 не исполнялись. Это не замена для обязательного IL2CPP path без нового process adapter/AOT work.

Core dependencies: NativeArraySpanExtensions1.0.1, Newtonsoft3.2.1, UniTask2.5.10, VoltRPC3.2.1; engine/communication/native packages и registry scopes добавляются отдельно. Готовая CEF доставка уменьшает локальную C++ работу, но увеличивает optional dependency/runtime distribution footprint. Не навязывать их base NeoxiderTools. Имеются JS/local-storage/windowless-framerate settings; full byte quotas/strict navigation/cancel guarantees нашего backend не доказаны.

## 5. Root decision inputs и следующий безопасный шаг

Ни один кандидат здесь не объявлен ready для полного `IHtmlRuntimeBackend`: request/decoded-byte bounds и optional strict locks требуют отдельного enforcement evidence. Существующий 65/65 contract PASS не относится к browser implementations.

- **WebViewToolkit** предлагает больше готового whole-component UI/native packaging, Windows DX12 route и ready DLL; остаются точные source blockers WT-01–WT-03, input/readiness, performance и 6.6/IL2CPP verification. Наличие готового component не доказывает, что native patch вообще не понадобится.
- **gree** сохраняет интерес как платформенный family adapter; свежий HEAD тот же, найденные GB/BF gaps остались. Native fixes потребуют toolchain, готовность которого не установлена.
- **UWB** позволяет отдельно сравнить CEF/Mono surface без собственной browser implementation, но documented IL2CPP prohibition и dependency footprint являются явными ограничениями.

Самый узкий следующий reuse milestone **после решения root**: взять выбранный exact published package pin **в isolated task root**, сначала без исполнения проверить fetched binary/tarball hashes, PE/dependencies/exports и source↔asset mapping; затем вернуть результат root. Только после очереди Unity и disposition source lifetime blockers — controlled one-view fixture с DOM-state/input + Canvas2D/WebGL known pixels + RAF/presented-frame correlation, resize/early destroy/domain reload и 100 open/close; Mono и IL2CPP разделять. Это не интеграция в base package и не повод заранее устанавливать C++ SDK. Если chosen component требует correction, ограничить её подтверждёнными пробелами; не писать новый renderer.

## Research receipts и ограничения

Малые text-only notes/snapshots: `D:/Temp/Neoxider-Audit-20261101/backend-feasibility/alternatives/` — `upstream-discovery.json`, `release-provenance.json`, source receipts и pinned text files. Git blob сверка выполнена для скачанных source files, присутствующих в сохранённом filtered tree; остальные явно помечены как URL-pinned text, не binary attestation. HTTP404 на несуществовавшем предполагаемом workflow/csproj пути сохранены в receipts и не приняты как отсутствие реализации; фактические пути найдены через Git tree.

Ограниченная source coverage: Toolkit native WebView/Manager/Capture, D3D11/D3D12 route, managed Native/Instance/Manager/UITK, package/importer/CI/license; UWB setup/license/packages, client/render/process lifecycle и engine settings. Это не аудит всех third-party dependencies, DLL/ABI/allocator safety или zero-bug claim. Mobile-only WebViews/DOM interpreters и paid-only/no-source renderers не являются Windows shortlist. Никаких новых процессов, package installs, binaries или product edits этот проход не оставил.
