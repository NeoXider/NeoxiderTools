# gree/unity-webview: offline PE / API inspection

Дата: 2026-11-01. Проверяющий: независимый native runtime reviewer. Этот проход заменяет неисполненную CLI4-задачу после OOM; CLI4 не перезапускался. Проверены только локальные байты, metadata и исходники. **DLL не загружалась и не исполнялась. Unity/Player/IL2CPP, установка, компиляторы и тесты — NotRun.**

**Вывод:** Windows x64 DLL существует, совпадает с pinned Git blob и содержит все Windows entry points C# wrapper. Это подтверждает полноту имён экспортов, но не общую ABI-совместимость и не работоспособность backend. Есть конкретные пробелы marshalling, URL enforcement и lifecycle. Повторное использование через отдельный optional typed adapter целесообразно; базовый NeoxiderTools не должен получать обязательную browser dependency.

## 1. Проверяемый объект и воспроизводимые доказательства

| Поле | Получено локально |
|---|---|
| Upstream | `gree/unity-webview`, commit `950cfbac14dbbf72e87548e77ef11ecc8843988e` |
| Commit tree | `83e03c46efcd8fa15ff99679a1dacbb771dbbb81` |
| DLL | `snapshot/dist/package/Assets/Plugins/Windows/x64/WebView.dll` |
| Размер | 146,944 bytes |
| SHA-256 | `2f9031885bba4c02720de14401e372f1ac7d4d9e6953453cdd6e2719e46046d3` |
| Git blob SHA-1 | `f346acf03d4d4b0737b87d81808d9452523401ca` |
| Provenance | Все 13 локальных файлов повторно проверены по SHA-256 и `SHA1("blob <size>\\0" + bytes)`; совпали с `provenance.json.expectedGitBlobSHA1` |

Исходный каталог: `D:/Temp/Neoxider-HtmlDemo/spikes/gree-inspection`. Новые task-owned артефакты:

- [Python stdlib parser](D:/Temp/Neoxider-HtmlDemo/spikes/gree-inspection/pe_static_20261101.py), SHA-256 `6b8952a8bab7b4d51d651309dbf887ee623f64fd72da62f8095c5e60e46f87fc`.
- [Полный PE / imports / exports / signature manifest](D:/Temp/Neoxider-HtmlDemo/spikes/gree-inspection/pe-static-20261101.json), SHA-256 `a340727b7fb2b660fc240df23a27ff211c549a5ac6fbb73add3446d4b37e7f83`.

Парсер использует `pathlib/struct/hashlib/json/re/uuid`, читает файлы как bytes и не содержит `ctypes`, `LoadLibrary` или запуска native/subprocess. Выполнен только этот лёгкий Python-процесс; он завершился с exit 0. Существующие исторические `REPORT.md` и `inspection-result.json` не переписаны: их прежний `NotRun` описывает отдельный старый проход и не является текущим PE-verdict.

## 2. Что установлено из PE

| Проверка | Результат |
|---|---|
| Machine | `0x8664`, AMD64 |
| Format | PE32+ `0x20b`, DLL characteristic установлен |
| Linker version | 14.51 |
| OS/subsystem header version | 6.0 / 6.0; это заголовки PE, не доказанный минимум ОС для WinRT/WGC/WebView2 |
| Named exports | 38, без forwarders |
| Windows wrapper P/Invoke declarations | 36 |
| Имена wrapper, отсутствующие в DLL | 0 |
| Native C API exports, отсутствующие в DLL | 0 |
| Дополнительные exports | `UnityPluginLoad`, `UnityPluginUnload` |
| Delay import directory | отсутствует |
| PE certificate directory | offset 0, size 0 — встроенная certificate table отсутствует; каталоговые подписи не проверялись |
| PDB record | CodeView RSDS GUID `4928695b-8f22-4ba0-add1-22856caf34d5`, age 2; PDB не получен |
| VERSIONINFO | Сигнатура `VS_FIXEDFILEINFO` в `.rsrc` не найдена ограниченным scan; это не полная проверка resource tree |

Формат RVA/import/export разобран согласно [Microsoft PE specification](https://learn.microsoft.com/en-us/windows/win32/debug/pe-format). COFF timestamp сохранён как raw `0x6ad095dc`; по нему не приписывается дата сборки или воспроизводимость.

**Статическая проверка конкретных stubs:** `_CWebViewPlugin_SetURLPattern`, RVA `0xc460`, начинается `b0 01 c3` (`mov al,1; ret`). Имена `InitStatic`, `ClearCookie`, `ClearCookies`, `SaveCookies`, `GetCookies` указывают на один RVA `0xbe90`, bytes `c2 00 00` (`ret 0`). Это согласуется с пустыми/constant-return исходными реализациями. Наличие этих exports не означает наличие соответствующей функции.

Нельзя по PE-именам установить параметры/return types, корректность threading, COM/apartment ownership, работу GPU-захвата, ввод, compatibility Unity/IL2CPP, отсутствие memory corruption или соответствие всей DLL всему исходнику. Полной декомпиляции, PDB-сверки и reproducible native build не было.

## 3. Прямые зависимости и toolchain

Всего 18 import descriptors. Из DLL непосредственно получены:

| Группа | DLL |
|---|---|
| Win32/COM | `KERNEL32.dll`, `USER32.dll`, `ADVAPI32.dll`, `ole32.dll`, `OLEAUT32.dll`, `urlmon.dll` |
| Graphics/dispatcher | `d3d11.dll`, `CoreMessaging.dll` (`CreateDispatcherQueueController`) |
| MSVC | `MSVCP140.dll`, `VCRUNTIME140.dll`, `VCRUNTIME140_1.dll` |
| API sets | `api-ms-win-crt-runtime-l1-1-0.dll`, `api-ms-win-crt-heap-l1-1-0.dll`, `api-ms-win-crt-string-l1-1-0.dll`, `api-ms-win-crt-stdio-l1-1-0.dll`, `api-ms-win-crt-convert-l1-1-0.dll`, `api-ms-win-core-winrt-error-l1-1-1.dll`, `api-ms-win-core-winrt-l1-1-0.dll` |

`WebView2Loader.dll` не импортируется. `WebViewPlugin.vcxproj` ссылается на `WebView2LoaderStatic.lib`, SDK `Microsoft.Web.WebView2` `1.0.3800.47`, toolset `v145`, C++20 и Windows SDK `10.0`. Это декларации исходного проекта, не полная реконструкция команды, создавшей DLL. README говорит о Windows 10+ и необходимом WebView2 Runtime; старые инструкции про VS2019/2022 нельзя считать доказательством доступности указанного `v145` toolset.

Static loader не включает сам browser Runtime в DLL. На чистом Windows consumer отдельно проверить MSVC runtime, WebView2 Runtime, WinRT/dispatcher/WGC availability и корректный failure result при их отсутствии. Не делать вывод «нет зависимостей» из отсутствия отдельного loader import. Transitive imports, API-set resolution, catalog signing и состояние целевой машины не проверены.

## 4. Сопоставление Windows C# / C API

Полная таблица всех 36 пар находится в JSON (`windowsPInvokes`, `nativeSourceExports`). Количество и имена совпали. На уровне исходных деклараций `IntPtr↔void*`, `int↔int`, `ushort↔unsigned short`, `float↔float` и порядок параметров согласуются; structs/managed delegates в этой поверхности отсутствуют. `GetRenderEventFunc` возвращает указатель, а не вызываемый из managed делегат.

### GB-01 — P1: UTF-8 в native API не выражен в Windows P/Invoke

Windows блок `WebViewObject.cs:594–666` использует `[DllImport("WebView")]` без CharSet/MarshalAs и сам содержит TODO о UTF-8. Native `LoadURL/LoadHTML/EvaluateJS`, UA и keyboard strings преобразуются с `CP_UTF8`; native messages формируются как UTF-8. Default C# P/Invoke CharSet соответствует ANSI; Windows ANSI code page не гарантированно UTF-8. [Microsoft charset rules](https://learn.microsoft.com/en-us/dotnet/standard/native-interop/charset).

Риск затрагивает русские строки, emoji, не-ASCII HTML/JS, ввод и callback messages. ASCII fixture его не обнаружит. Исправление должно задавать подтверждённый UTF-8 marshalling для Windows и явное владение возвращаемыми strings либо использовать явные byte/pointer wrappers; проверить отдельно Mono и IL2CPP. Объявлять весь bridge Unicode-correct по совпадению имён exports нельзя.

### GB-02 — ABI review required: C++ bool / managed bool

Native использует C++ `bool`; Windows imports/returns не имеют `[MarshalAs(UnmanagedType.I1)]`. Стандартное managed bool marshalling не является явным описанием однобайтного C++ bool. Требуются явные согласованные annotations или C API с фиксированными integer widths и Player false/true controls. [Microsoft boolean interop guidance](https://learn.microsoft.com/en-us/dotnet/standard/native-interop/best-practices#boolean-parameters-and-fields).

В x64 используется стандартный Windows ABI; отсутствие cdecl/stdcall override само по себе здесь не доказывает stack mismatch. [Microsoft x64 calling convention](https://learn.microsoft.com/en-us/cpp/build/x64-calling-convention?view=msvc-170). x86 DLL этим проходом не проверялась; x64 результат на неё не переносится.

### GB-03 — returned string ownership различается

`GetMessage` и `GetCustomHeaderValue` выделяют результат через `CoTaskMemAlloc`; `GetAppPath` возвращает borrowed `static std::string.c_str()`, но C# объявляет все три как `string`. Для borrowed pointer нельзя предполагать то же освобождение, что для allocated result. Windows путь wrapper сейчас не вызывает `GetAppPath` (использование находится в macOS init), поэтому это dormant ABI hazard, а не доказанный текущий Windows crash. До расширения adapter не начинать использовать этот import без явного ownership contract.

## 5. Возможности, которые нельзя обещать через stock Windows API

| API / setting | Исходник и/или статические байты | Требование adapter |
|---|---|---|
| `SetURLPattern` | Source `1535–1541`; binary constant-true stub | Capability enforcement=false; нельзя считать успешный bool применённой policy |
| `NavigationStarting` | Source `852–864`: сообщает URL, не отменяет | Нужен cancel-before-request extension для обязательной navigation/redirect policy |
| JS off | Native `764` принудительно `IsScriptEnabled(TRUE)`; общего toggle wrapper нет | Unsupported/restart-required до реальной реализации; CSS injection не отключает JS |
| Animation off / reduced motion | Общего browser API нет | Отдельная настройка; не выключать весь rAF, теряя игровую логику |
| `Pause/Resume` | Wrapper `958–994`: Windows/macOS TODO; iOS — только media | Не выдавать за JS/event-loop pause |
| `SetInteractionEnabled` | Wrapper `1220–1237`: Windows/macOS TODO/no-op | Нужен input-owner extension либо explicit unsupported |
| `Init.transparent/separated` | Native `1452–1453` игнорирует | Не заявлять alpha/separate-window capability по наличию аргумента |
| Cookies clear/save/get | Source `1754–1768` и общий binary ret stub | Не обещать session profile/cookie isolation или очистку |
| Draw/input surface | Private texture, `Update` + `OnGUI` | Для UITK — минимальный явный texture/input adapter без второй draw/input цепочки |
| Refresh cadence | Wrapper `2345`: default `bitmapRefreshCycle=10`; GPU и fallback paths различаются | Измерять фактический path/кадры; не обещать одинаковый FPS |

Строки относятся к локальным UTF-8 файлам pinned snapshot. Возможности иных ОС требуют их собственных исходников и Player-тестов; текущий ограниченный snapshot не является готовым полным UPM-пакетом.

### GB-04 — P1 source lifecycle blocker: timeout/failure инициализации

`_CWebViewPlugin_Init:1462` создаёт `CreateParams` на стеке, `CreateThread:1474` получает `&params`; WebView2 completion lambdas захватывают `params` (`722`, `740`). `WaitForSingleObject:1481` ограничен 10s, его результат игнорируется, `readyEvent` закрывается. При ошибке/timeout код ждёт STA thread ещё максимум 5s, затем возвращает и уничтожает `inst`/stack state без подтверждённой отмены callbacks/завершения потока. STA создаёт окно и продолжает message loop (`1248–1319`); asynchronous callbacks могут прийти позже.

Это подтверждённый небезопасный source path: при медленном/ошибочном init потенциально остаются ссылки на освобождённые `params/instance` и закрытый event. Код содержит соответствующий TODO. Реального воспроизведения падения нет. Нужна ownership-safe heap/shared init state, проверка wait result и согласованное завершение/cancellation; generic C# adapter не исправляет lifetime внутренних указателей.

Дополнительно `Destroy:1505–1524` на timeout удерживает instance вместо erase, но закрывает event, адрес которого отправлен STA; успешный return `1` не означает фактически завершённое освобождение. Проверить позднюю доставку и event lifetime, а также retained instances после повторных open/close. До исправления не объявлять teardown bounded/safe по самому return value.

## 6. Поставка, лицензии и минимальная интеграция

`unity-webview.asmdef` называется `unity-webview`, public namespace — `Gree.UnityWebView`; typed интеграция не требует reflection. Но asmdef ссылается на `Unity.InputSystem`, а `package.json` не объявляет dependency. Проверить consumer без Input System. В Windows x64 `WebView.dll.meta` `Linux64.enabled=1` и `OSXUniversal.enabled=1`; исправить importer filters только в поставляемой optional integration, сохранив provenance/изменения. В Assets/Packages этого проекта ничего не копировалось.

Pinned `LICENSE` — zlib: сохранять notice, не искажать происхождение, явно отмечать изменённые sources. Это не выдаёт права на Chromium/WebView2 runtime, сайт, шрифты, Chicken Road или Spine assets. Перед поставкой зафиксировать native dependency/runtime notices и источник каждой части; текущий осмотр ничего не устанавливает и не перераспределяет.

Минимальный путь adapter:

1. Отдельный опциональный пакет/assembly с reference на `unity-webview` и базовые backend contracts; base Neo compile без него.
2. Владеющий session создаёт отдельный `WebViewObject`, использует `Init`, `LoadURL/LoadHTML`, `CanGoBack/GoBack`, viewport `SetMargins`, `SetVisibility`, `ld/err/httpErr/started` callbacks. Цель — использовать штатный renderer, не писать браузер.
3. Generation/closed guards отсекают late callbacks. Отмена до init completion возвращается явно; фактическую native cancellation нельзя обещать до GB-04. Никакого автоматического повторного исполнения форм/раунда при fallback.
4. EvaluateJS/Unity bridge доступен лишь через узкий typed host protocol с origin/payload limits. Stock callback не передаёт origin; при требовании проверяемого происхождения нужен узкий native/upstream extension, а не payload-самодекларация сайта.
5. Effective options и diagnostics объясняют unsupported настройки. Для первого проверяемого spike допустим строго ограниченный trusted fixture с перечисленными пробелами; это не принятие backend для произвольного remote content.
6. Подтверждённые недостающие части — UTF-8/ABI annotations, init/teardown lifetime, enforceable navigation/settings, texture/input surface. Нужен небольшой отслеживаемый patch upstream wrapper/native API либо другой backend; переименование stubs не решает контракт.

## 7. Последующие acceptance checks

Не выполнены: загрузка DLL на чистой ОС; Mono/IL2CPP и Unity 6.0–6.6 compilation/runtime; x86/ARM и остальные платформы; отсутствующие WebView2/MSVC deps; slow/fail init/late callbacks/cancel/destroy; русские строки/emoji/IME round-trip; true/false bool controls; redirect cancel-before-request и bridge origin; cookie/profile isolation; JS/animation/input settings; physical input/alpha/DPI/resize; D3D11 vs other graphics API/capture paths; GPU/CPU/native memory/FPS; Velora и Chicken Road.

Это source/bytes receipt, не тестовый PASS. Новые runtime/backend implementations должны пройти отдельное независимое ревью и Player acceptance. Связанный [NativeRuntimeReview.md](NativeRuntimeReview.md) описывает общую границу и критерии.

Изменены только этот отчёт и два новых task-specific offline артефакта вне Assets. Долгоживущих процессов, установок, импорта, compiler/test запусков и native execution у проверяющего нет.
