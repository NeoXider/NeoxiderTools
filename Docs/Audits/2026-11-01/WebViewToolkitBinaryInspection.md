# WebViewToolkit v1.3.0: exact artifact / offline PE inspection

Дата локально: 2026-11-02, Asia/Yekaterinburg. Fetch UTC: 2026-11-01 19:13:46. Область разрешена root только для bounded download двух публичных DLL и byte/source inspection. **DLL не загружалась в процесс и не исполнялась. Unity, Player, Mono/IL2CPP, compiler/tests, установки и product/config changes — NotRun.**

**Итог:** release DLL и DLL exact pinned UPM package побайтно одинаковы, ожидаемые SHA-256/Git blob подтверждены. AMD64 DLL содержит все 23 Windows P/Invoke entry points. Это закрывает identity/named-entry-point проверку, но не доказывает безопасный lifecycle, полный ABI или source↔binary semantic equivalence. WT-01–WT-03 повторно подтверждены в exact source-build pin и остаются открытыми для correction/controlled execution; adoption/запуск этим отчётом не одобряются автоматически.

## 1. Fetch receipt и ограничения

Использованы только официальные URLs из сохранённых release/tree metadata:

- [Release v1.3.0](https://github.com/cantetfelix/WebViewToolkit/releases/tag/v1.3.0), asset `WebViewToolkit.dll`.
- [Pinned UPM binary](https://github.com/cantetfelix/WebViewToolkit/blob/ee271a59088d67f735bc49bc025091da4d1d9fe9/Runtime/Plugins/x86_64/WebViewToolkit.dll).

Получено **607,232 bytes** response payload суммарно при cap **2,097,152 bytes**. Оба HTTP 200; tarballs/CEF/SDK не скачивались. Release перенаправился на `release-assets.githubusercontent.com`; подписанные query/fragment URL удалены перед сохранением/выводом. В receipts нет signed query credentials.

| Проверка | Release | Pinned UPM |
|---|---|---|
| Bytes | 303,616 | 303,616 |
| SHA-256 | `a4aece55e3cf7d7eec6d55760de91b6673ae945933f46b45298b89fdc415c05d` | тот же |
| Git blob SHA-1 | `fdfa19e7e52ef4f09e0f6747a12ec6587be2a583` | тот же |
| Expected publisher digest / pinned Git tree match | Да / да | Да / да |
| Полное byte equality | **да**, сравнены сами локальные bytes | **да** |

Локальные binaries находятся только в task root: `D:/Temp/Neoxider-Audit-20261101/backend-feasibility/toolkit-binary/release/WebViewToolkit.dll` и `upm/WebViewToolkit.dll`. Изменение существующих отличающихся artifacts запрещено самим fetch script. Никакого копирования DLL в Assets/Packages не было.

## 2. PE results

Переиспользованы byte-only readers прежнего stdlib PE inspector; toolkit-specific часть только сопоставляет его exports с source. Нет ctypes/LoadLibrary/native calls. Parser читает структуры PE и C strings по file offsets; полный disassembler/loader не использовался.

| Поле | Локально прочитано |
|---|---|
| Machine | `0x8664` AMD64 |
| Format | PE32+ `0x20b`, DLL characteristic установлен |
| Linker | 14.44 |
| OS/subsystem version header | 6.0 / 6.0; не runtime minimum Windows |
| DLL characteristics | `0x160` |
| COFF timestamp raw | `0x697b6ce5`; не provenance certificate/доказанная дата build |
| Named exports | **27**, forwarders 0 |
| Windows C# P/Invokes | **23**, missing exports 0 |
| Native C exports в PluginExports.cpp | **23**, missing binary names 0 |
| Дополнительные symbols | `UnityPluginLoad`, `UnityPluginUnload`, `GetRenderEventFunc`, `GetRenderEventAndDataFunc` |
| Render aliases | prefixed/unprefixed render getters имеют одинаковые RVA `0x4040` / `0x4050` |
| Certificate directory | offset 0, size 0 — встроенная certificate table отсутствует; detached/catalog signing не проверялся |
| Delay import directory | 0 / 0 |
| Debug | одна запись type13 / 1080 bytes; CodeView/RSDS/PDB path record не обнаружен |
| Version resource scan | `VS_FIXEDFILEINFO` не найден ограниченным `.rsrc` scan; полного resource-tree аудита не было |

PE interpretation опирается на [Microsoft PE specification](https://learn.microsoft.com/en-us/windows/win32/debug/pe-format). Отсутствие PDB/version info не отменяет exact hashes, но не позволяет получить из DLL source revision по символам. Label `v1.3.0` подтверждён package/release provenance, а не встроенным FileVersion.

## 3. Imports и прямые runtime dependencies

Прочитаны **36 direct import descriptors**, полный список функций сохранён в JSON. Именованные OS/graphics DLL: `d3d11.dll`, `dxgi.dll`, `dcomp.dll`, `CoreMessaging.dll`, `USER32.dll`, `OLEAUT32.dll`; остальные — Windows API sets для loader/COM/WinRT/threading/memory/files/registry и прочих OS функций.

Конкретные graphics entry points: `D3D11CreateDevice`, `CreateDirect3D11DeviceFromDXGIDevice`, `D3D11On12CreateDevice`, `CreateDXGIFactory1`, `DCompositionCreateDevice`, `CreateDispatcherQueueController`. Это согласуется с reviewed composition/WGC/D3D11/D3D11On12 source; наличие imports не подтверждает успех route на устройстве.

**Не импортируются напрямую** `MSVCP140.dll`, `VCRUNTIME140.dll/_1`, `WebView2Loader.dll`. CMake source явно выбирает static CRT `MultiThreaded` и native WebView2/WIL linking. Это отличается от изученного gree DLL footprint, но не означает «нет runtime dependencies»: Windows APIs/WinRT и browser **WebView2 Runtime** всё ещё нужны. Transitive/dynamic dependencies, actual OS API-set resolution и loader behavior не исполнялись. [Exact CMake source](https://github.com/cantetfelix/WebViewToolkit/blob/0a60c025194a05e1d0994c3db53feac9ef33adbe/WebViewToolkitPlugin/CMakeLists.txt#L38).

## 4. ABI: что совпало и что остаётся открытым

Полный manifest содержит 23 пары managed/native declarations. В прямой C API surface source types/порядок параметров согласуются: `int↔int32_t`, `uint/out uint↔uint32_t/uint32_t*`, `float↔float`, `IntPtr↔void*`, `[LPWStr] string↔const wchar_t*`. Calls явно `CallingConvention.Cdecl`; Windows x64 ABI не даёт отдельного cdecl/stdcall stack-cleanup mismatch. x86/ARM DLL не исследовались.

Совпали entry points callback setters, Initialize/Shutdown/SignalApplicationQuit, Create/Destroy/GetTexturePtr/Resize, URL/HTML/script/history, mouse/key input и оба render getters. Имена в PE **не кодируют** types, marshalling, ownership, callback lifetime или thread affinity; это source-shape consistency, не executable ABI pass.

Не закрыты reverse callbacks: managed NavigationCallback использует `UnmanagedType.Bool`, native typedef — C++ bool; managed log LPStr получает native UTF-8 diagnostics. Широкие строковые document/script APIs выражены яснее gree, но эти исключения остаются на согласование/Mono+IL2CPP tests. Наличие всех exports не исправляет WT-01 ниже. [Exact managed declarations](https://github.com/cantetfelix/WebViewToolkit/blob/ee271a59088d67f735bc49bc025091da4d1d9fe9/Runtime/Native/WebViewNative.cs).

## 5. Source↔release provenance и повторная проверка WT-01–WT-03

Native source-build pin **`0a60c025194a05e1d0994c3db53feac9ef33adbe`**, [publisher CI run](https://github.com/cantetfelix/WebViewToolkit/actions/runs/21481710418) reports success. Release/UPM package pin **`ee271a59088d67f735bc49bc025091da4d1d9fe9`**. Предыдущая primary metadata сверка показала совпадение всех восьми managed runtime blobs source→package. В текущем проходе **11 критических source файлов** независимо пересчитаны по Git blob против exact source-build tree, все совпали; сохранены immutable copies и receipt.

Речь идёт именно о source pin CI, связанном с опубликованным 303,616-byte artifact. Старый 301,056-byte DLL из master-folder в этой работе не скачивался/не разбирался, findings к нему не приписываются. Сохранённая workflow chain + matching artifact hashes усиливает provenance, но без reproducible native build/PDB/full semantic analysis **не доказывает полное соответствие всех машинных instructions исходнику**.

| Finding | Повторно подтверждено в exact pinned source | Disposition |
|---|---|---|
| **WT-01 AOT/callback ABI** | Managed manager `106–109` регистрирует instance callback methods; `272/282/290` без MonoPInvokeCallback, `299` тоже instance, хотя annotated. Native callback typedef bool не равен явному I1 managed contract | Source blocker для IL2CPP claim остаётся. Static callback trampolines/annotations и ABI agreement нужны; actual Mono/IL2CPP NotRun |
| **WT-02 async lifetime** | WebView.cpp `213/254` callbacks захватывают raw `[this]`; native manager `209–215` сразу erases unique_ptr при Destroy. Нет outstanding-create lease/cancel handshake, делающего owner живым до completions | Source early-destroy/late-callback hazard остаётся. Publisher binary не был выполнен и crash не воспроизводился |
| **WT-03 resize/GPU ownership** | D3D11 ResizeSharedTexture `136–147` Release старого pointer до Create; WebView::Resize `440–443` обновляет field лишь при success, но `474` возвращает Success даже при allocation failure | Source stale-owner/failure propagation blocker остаётся. Нужны transactional replacement, actual error и render/external-texture ownership checks |

Sources: [managed manager](https://github.com/cantetfelix/WebViewToolkit/blob/ee271a59088d67f735bc49bc025091da4d1d9fe9/Runtime/WebViewManager.cs#L272), [native create/resize](https://github.com/cantetfelix/WebViewToolkit/blob/0a60c025194a05e1d0994c3db53feac9ef33adbe/WebViewToolkitPlugin/src/WebView.cpp#L203), [native destroy](https://github.com/cantetfelix/WebViewToolkit/blob/0a60c025194a05e1d0994c3db53feac9ef33adbe/WebViewToolkitPlugin/src/WebViewManager.cpp#L209), [DX11 replacement](https://github.com/cantetfelix/WebViewToolkit/blob/0a60c025194a05e1d0994c3db53feac9ef33adbe/WebViewToolkitPlugin/src/RenderAPI/RenderAPI_D3D11.cpp#L136). Unity требует static/annotated native→managed callbacks на AOT; README IL2CPP statement этим binary inspection не подтверждается. [Unity restrictions](https://docs.unity3d.com/6000.0/Documentation/Manual/scripting-restrictions.html).

Дополнительный GPU ownership путь остаётся unaccepted: native texture destruction/replacement выполняется до managed external Texture disposal; manager returns raw WebView после mutex release, а render update имеет отдельную lock scope. DX12 source содержит CPU staging и infinite fence wait. Gree-style caller pixel-buffer/capacity API здесь отсутствует; это не основание переносить на Toolkit gree `_Render` finding, но texture/resize/late-work risks требуют своих tests.

## 6. Лицензии, artifacts и последующий gate

Оригинальный pinned MIT notice сохранён рядом с binaries как `MIT-LICENSE.txt` (SHA `4be55cb237ec49b18fc37a8573f1a80699d85098e449b2fa9f90c37d373f2294`); source notices не удалены. Это не перелицензирование Unity UCL headers, WebView2/WIL или browser Runtime. Их notice/distribution inventory требуется перед redistributing optional integration. Никаких dependency установок/изменений не было.

Evidence root: `D:/Temp/Neoxider-Audit-20261101/backend-feasibility/toolkit-binary`:

| Receipt | SHA-256 |
|---|---|
| `fetch-receipt.json` | `4a071df2a6f3e1369270b629fc9831b5f521f7e63773f9978b3cab4a9781c06e` |
| `pe-inspection.json` | `55225d2cd326b57019a3421206fe1a38dcddea7d3b8ecd2c8371987eedeae594` |
| `critical-source-receipt.json` | `4f3276417946df926af1c02c9162cc5bdb76c9a925b16b6056ce69b2aca1a732` |
| `inspect_pe.py` | `a787efa4f49c2e07e0d3cea90e3ef8c52c6ace6e84c006f4030a5c431692c7e4` |

Fetch/raw PE/source checks **завершены**; runtime safety и adoption **не приняты**. Следующее решение принадлежит root: bounded correction или controlled isolated spike после disposition известных source blockers, либо другой backend. До этого не заявлять clean teardown, Canvas/WebGL/input/RAF/render/FPS, Unity6.3/6.6/Mono/IL2CPP или `EnforcesLoadBounds`/strict navigation enforcement. Сам факт скачанного готового DLL не устраняет эти gates. Reviewer не создавал других agents/портов/UI/worktrees и не оставил активных процессов.
