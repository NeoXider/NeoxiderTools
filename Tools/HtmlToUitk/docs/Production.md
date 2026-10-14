# Production HTML/CSS → UI Toolkit: проверенный CLI-маршрут

Канонический CLI находится в Tools/HtmlToUitk. Unity runtime/editor — отдельный модуль Assets/Neoxider/Scripts/UI/HtmlToUitk; CLI DLL в player не копируются. Требуется .NET9 SDK; отдельные старые тесты используют .NET8 runtime. Unity6.3/6.6 native импорт, actions, reload и визуальные captures проходят отдельный операторский gate.

## Сборка и запуск из корня NeoxiderTools

```powershell
dotnet build Tools/HtmlToUitk/HtmlToUitk.sln
dotnet test Tools/HtmlToUitk/HtmlToUitk.sln
dotnet publish Tools/HtmlToUitk/src/HtmlToUitk.Cli/HtmlToUitk.Cli.csproj -c Release -o Tools/HtmlToUitk/bin
dotnet Tools/HtmlToUitk/bin/htmltouitk.dll run Tools/HtmlToUitk/fixtures/production-neutral/neutral.htujob.json
```

Проверенный neutral fixture содержит два документа, nav pages/modal/tabs, animation/reduced motion, media width/height/aspect, живые localization keys и7PNG: linear/radial/conic/45°wide+ tall/repeating hard stops/fluid horizontal. Генерируются Screens/*.uxml/*.uss/*.mq.json/*.nav.json/*.anim.json/*.runtime.json, Localization/*.newkeys.json, Generated/gradient-<SHA256>.png, generated-assets.json и diagnostics.json. Повторная конверсия сохраняет byte hashes. Имена controls стабильны; callbacks регистрируются программно через UitkScreen.RegisterAction или disposable UitkActionBindings.Bind в runtime-модуле. HTML/JS не переводится в игровой C# код.

Default runtime emulations зарегистрированы: media-breakpoints→BreakpointWatcher, page-router→UitkRouter, keyframes-animator→UitkAnimator. JSON schemas совместимы с прежними; mq class допускает optional selector для назначения класса конкретным элементам. Отсутствующая backend registration либо неподдерживаемый feature получает nonzero error, а не тихую потерю при PASS.

## Границы fidelity и budgets

Assembly capability metadata не доказывает native gradient rendering. Default использует собственный GradientBaker: CSS3 sRGB colors/stops, angle/direction, alpha, repeating hard stops и deterministic PNG. Современные interpolation spaces/hints, неподдержанные geometry/units и unresolved colors отвергаются. Локализуемый текст остаётся UXML text/key и не запекается.

Static PNG не эквивалентен произвольной responsive CSS geometry. Angled nonaxis linear, radial, conic и repeating требуют fixed pixel width/height без media size override; иначе CLI возвращает nonzero dynamic-gradient diagnostic. Fluid horizontal/vertical с процентными stops сохраняет normalized axis; reference-resolution bake помечается warning. Native visual gate обязателен, особенно для реального target aspect. Runtime/layout изменения размеров вне CSS контракта не объявляются поддержанными.

Dimension budget1..4096, aggregate32megapixels на документ. Generated path/asset identity использует полный SHA256; filenames deterministic, manifest содержит path+actual PNG SHA+width+height+kind=gradient+sRGB=true+wrap=clamp+mipmaps=false. Unity importer потребляет точные проверенные файлы, не присваивает себе всю Generated папку. Внешний converter заканчивает запись всех файлов/manifest прежде controlled Unity refresh.

Media поддерживает конъюнкции min/max width/height вpx, min/max aspect-ratio и orientation. Для первого runtime-маршрута selector — simple compounds names/classes; неподдержанные selector/query получают отказ. Normal и media selectors имеют одинаковый дополнительный class specificity, чтобы не обгонять исходную specificity по одной breakpoint-class. Browser cascade/complexCSS полная эквивалентность не заявлена.

`preview` и `compare` C# commands пока честно возвращают nonzero unsupported; не TODO с exit0. Проверенные альтернативы:

```powershell
$env:PYTHONPATH = 'Tools/HtmlToUitk/oracle/python'
python -B -m htmltouitk preview '<output>/Screens/Neutral.uxml' --out '<qa>/preview.html' --viewport 1280x720
python -B Tools/HtmlToUitk/tools/compare_shots.py '<html-capture.png>' '<unity-capture.png>' --out '<qa>/diff.png' --json '<qa>/diff.json'
```

Python preview действительно сформировал HTML из acceptance UXML. compare_shots self-diff дал0regions/exit0 — проверка маршрута, не доказательство Unity/browser parity. Сравнивать нужно независимые screenshots. Strict CLI предупреждения также дают nonzero согласно действующему exit contract; обычный успешный conversion может быть PASS WITH WARNINGS.

## Фактическая проверка

Полный suite:2559PASS/32historicalSKIP/0FAIL. Production CLI integration:19PASS/0SKIP/0FAIL, включая decoded PNG pixels/alpha,45°wide versus tall, radial center/corner, conic diversity, repeating positions, media numeric fields, nav/modal/tabs, reduced-motion spec, runtime artifacts и repeated hashes. Negative CLI: dynamic angled/radial/media geometry, invalid gradient interpolation, oversize budget, unsupported shadow и preview/compare commands.

Старые32skip не считаются PASS;29старых converter golden cases имеют incomplete fixture expectations. Новый production fixture работает через actual CLI. Native Unity/render/performance и full browser parity пока не заменяются dotnet тестами. source-final.json различает сохранённую побайтную reuse-базу и реальные изменения.
