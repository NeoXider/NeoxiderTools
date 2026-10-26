# Production HTML/CSS → UI Toolkit: проверенный CLI-маршрут

Канонический CLI находится в Tools/HtmlToUitk; Unity runtime/editor — отдельный [модуль](../../../Assets/Neoxider/Docs/UI/HtmlToUitk.md). CLI DLL в player не копируются. Проверенный build SDK — **10.0.301**, target остаётся net9.0. Для отдельного **HtmlToUitk.Cli.v11.0.0.zip** нужен.NET 10 runtime: runtimeconfig LatestMajor проверен на10.0.9 вне репозитория без ENV. Runtime9 не включён; net8-тесты использовали8.0.21. [Фактическая verification](../../../Assets/Neoxider/Docs/UI/HtmlToUitkVerification11.md) разделяет source, CLI, native и OS manual scopes.

## Сборка и запуск из корня NeoxiderTools

```powershell
dotnet build Tools/HtmlToUitk/HtmlToUitk.sln
dotnet test Tools/HtmlToUitk/HtmlToUitk.sln
dotnet publish Tools/HtmlToUitk/src/HtmlToUitk.Cli/HtmlToUitk.Cli.csproj -c Release --self-contained false -p:UseAppHost=false -p:RollForward=LatestMajor -o Tools/HtmlToUitk/bin
dotnet Tools/HtmlToUitk/bin/htmltouitk.dll run Tools/HtmlToUitk/fixtures/production-neutral/neutral.htujob.json
```

Для `dotnet test` net9-проектов на машине только с.NET 10 текущая проверка использовала process-local `DOTNET_ROLL_FORWARD=Major`; прежнее значение нужно восстановить после своей verification-сессии. Portable publish использует собственный LatestMajor runtimeconfig и этого ENV не требует. Для Unity **CLI...** укажите DLL из целиком распакованного publish-каталога, а не одиночную скопированную DLL или checkout fallback.

Проверенный neutral fixture содержит два документа, nav pages/modal/tabs, animation/reduced motion, media width/height/aspect, живые localization keys и7PNG: linear/radial/conic/45°wide+ tall/repeating hard stops/fluid horizontal. Генерируются Screens/*.uxml/*.uss/*.mq.json/*.nav.json/*.anim.json/*.runtime.json, Localization/*.newkeys.json, Generated/gradient-<SHA256>.png, generated-assets.json и diagnostics.json. Повторная конверсия сохраняет byte hashes. Имена controls стабильны; callbacks регистрируются программно через UitkScreen.RegisterAction или disposable UitkActionBindings.Bind в runtime-модуле. HTML/JS не переводится в игровой C# код.

Default runtime emulations зарегистрированы: media-breakpoints→BreakpointWatcher, page-router→UitkRouter, keyframes-animator→UitkAnimator. JSON schemas совместимы с прежними; mq class допускает optional selector для назначения класса конкретным элементам. Отсутствующая backend registration либо неподдерживаемый feature получает nonzero error, а не тихую потерю при PASS.

## Границы fidelity и budgets

Assembly capability metadata не доказывает native gradient rendering. Default использует собственный GradientBaker: CSS3 sRGB colors/stops, angle/direction, alpha, repeating hard stops и deterministic PNG. Современные interpolation spaces/hints, неподдержанные geometry/units и unresolved colors отвергаются. Локализуемый текст остаётся UXML text/key и не запекается.

Static PNG не эквивалентен произвольной responsive CSS geometry. Angled nonaxis linear, radial, conic и repeating требуют fixed pixel width/height без media size override; иначе CLI возвращает nonzero dynamic-gradient diagnostic. Fluid horizontal/vertical с процентными stops сохраняет normalized axis; reference-resolution bake помечается warning. Native visual gate обязателен, особенно для реального target aspect. Runtime/layout изменения размеров вне CSS контракта не объявляются поддержанными.

Dimension budget1..4096, aggregate32megapixels на документ. Generated path/asset identity использует полный SHA256; filenames deterministic, manifest содержит path+actual PNG SHA+width+height+kind=gradient+sRGB=true+wrap=clamp+mipmaps=false. Unity importer потребляет точные проверенные файлы, не присваивает себе всю Generated папку. Внешний converter заканчивает запись всех файлов/manifest прежде controlled Unity refresh.

Actual UXML/USS filenames и shared/per-page CSS references резервируются вместе, включая suffix collisions. Slash/dot/case aliases ведут к одному canonical asset: одинаковые bytes идемпотентны, конфликт bytes/kind, reserved output или escape отвергается до Commit. Разные страницы/CSS больше не исчезают из-за одинакового stem.

Media поддерживает конъюнкции min/max width/height вpx, min/max aspect-ratio и orientation. Для первого runtime-маршрута selector — simple compounds names/classes; неподдержанные selector/query получают отказ. Normal и media selectors имеют одинаковый дополнительный class specificity, чтобы не обгонять исходную specificity по одной breakpoint-class. Browser cascade/complexCSS полная эквивалентность не заявлена.

`preview` и `compare` C# commands пока честно возвращают nonzero unsupported; не TODO с exit0. Проверенные альтернативы:

```powershell
$env:PYTHONPATH = 'Tools/HtmlToUitk/oracle/python'
python -B -m htmltouitk preview '<output>/Screens/Neutral.uxml' --out '<qa>/preview.html' --viewport 1280x720
python -B Tools/HtmlToUitk/tools/compare_shots.py '<html-capture.png>' '<unity-capture.png>' --out '<qa>/diff.png' --json '<qa>/diff.json'
```

Python preview действительно сформировал HTML из acceptance UXML. compare_shots self-diff дал0regions/exit0 — проверка маршрута, не доказательство Unity/browser parity. Сравнивать нужно независимые screenshots. Strict CLI предупреждения также дают nonzero согласно действующему exit contract; обычный успешный conversion может быть PASS WITH WARNINGS.

## Фактическая проверка

Свежий Round2 всего CLI solution: **2573 Passed / 30 NotExecuted / 0 Failed**;199 real capability cases прошли с SDK 6.6 managed/UnityEngine inputs. Root pure logic: **291/291,0 skipped**. Остаются7 validate-only,22 open-golden и1 S08 expected-diagnostic случая; они не PASS. Исторические source-final.json и19-case fixture сохраняют прежний scope и не заменяют этот полный запуск.

Round3 полной Unity assembly дал **370/370,0 skipped на каждом6000.3.14f1/6000.6.0f1**:5 новых runtime-регрессий и Root real external three external CLI runs → UXML/USS/nav/media/anim+7PNG → native ownership import/theme → Play UIDocument pixels/nav/modal/tabs/actions/disable-enable/tree replacement. Manual OSUnity 6.3 при1280×720: Settings один callback, VideoTab, Back, Quit/Cancel, public UIDocument Profile replacement, физический Save один callback; settingsClicks=1/saveClicks=1/no screen error. Четыре legacy diagnostic csproj всё ещё machine-specific. Не заявляются все разрешения, browser/JS parity, mobile/player/performance. Native export/import финальных Core/Complete архивов проверяется отдельно перед публикацией; [Verification 11](../../../Assets/Neoxider/Verification11.md) фиксирует этот статус.
