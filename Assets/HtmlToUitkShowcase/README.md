# Velora UI · HTML → UI Toolkit showcase

[Original SaaS template](https://velora.colorlib.com/templates/saas) · [MIT source](https://github.com/ColorlibHQ/velora-ui)

Одна сцена показывает четыре настоящие страницы Velora: SaaS landing, Pricing, About и Contact. Кнопка **Original ↗** в верхней панели и ссылки в footer открывают точный оригинал SaaS. Текст, композиция hero, dashboard, карточек, тарифов и формы взяты из MIT-исходников Velora. Авторские права и лицензия сохранены в `LICENSE-Velora.txt`; исходная ревизия и snapshots находятся в `Source~/UPSTREAM.json` и `Source~/Original`.

Это явная адаптация React/Tailwind в поддерживаемый HTML/CSS, после которой **настоящий HtmlToUitk CLI** создаёт UXML, USS, media/navigation/animation metadata и градиентные PNG. UXML/USS вручную не рисовались и после конвертации не редактировались. Next.js, React, Tailwind и Motion не входят в Unity player.

## Сцена и запуск

Открыть `VeloraShowcase.unity` и нажать Play. Для ручной сборки сцены нужен один GameObject с `UIDocument`, `Neo.HtmlToUitk.Navigation.UitkScreen` и `VeloraShowcaseController`. Назначить `PanelSettings` с Default Runtime Theme, **Constant Pixel Size / scale 1**. Для честной проверки responsive размеров panel coordinates должны совпадать с Game View pixels.

Controller автоматически загружает включённые assets из `Resources/Velora`; его поля можно оставить пустыми. При явном назначении:

| Поле | Asset |
|---|---|
| `document` | UIDocument на том же объекте |
| `screen` | UitkScreen на том же объекте |
| `visualTree` | `Resources/Velora/Screens/Velora.uxml` |
| `navigation` | `Resources/Velora/Screens/Velora.nav.json` |
| `animations` | `Resources/Velora/Screens/Velora.anim.json` |
| `mediaQueries` | `Resources/Velora/Screens/Velora.mq.json` |
| `localization` | `Resources/Velora/Velora.strings.json` |
| `interfaceFont` | `Resources/Velora/Fonts/Geist-Regular.ttf` (автоматически) |

Зависимости: существующий `Neo.HtmlToUitk.Runtime` и Unity UI Toolkit. Нет дополнительных packages, Editor builders, браузера или сетевых запросов для локальных UI-функций. Оригинал открывается только по явному нажатию ссылки.

Основной шрифт — официальный **Geist Regular** от Vercel / basement.studio. Включены исходный статический TTF и `LICENSE-Geist.txt` (SIL OFL 1.1); публичный `FontDefinition.FromFont` назначается текущему root при каждой привязке. Generated USS для этого не изменяется.

## Что проверить в Play Mode

- В hero движутся три мягких aurora слоя, меняется слово, мигает cursor и проходит shimmer по CTA. Настоящие CLI radial/linear baked gradients используются в фоне и графике.
- Pricing, About и Contact переключают локальные страницы. Header Components, Blocks, Templates, Themes и Changelog сохраняют исходные назначения `/components`, `/blocks`, `/templates`, `/themes`, `/changelog`: они явно помечены **↗** и открывают соответствующую страницу Velora в браузере. Иконка темы отдельно меняет локальную тему. Hero Browse components сохраняет исходный переход к `#features`; Browse components на Pricing открывает оригинальный каталог **↗**.
- CTA открывает dialog. Copy install command копирует реальную команду в clipboard; Close и Escape закрывают dialog.
- Search открывает локальный поиск: `aurora`, `text`, `marquee`, `chart` фильтруют результаты, результат прокручивает к соответствующему элементу.
- На узком экране поиск доступен через меню **☰ → Search components** (`vl-mobile-search`). Motion остаётся доступен в шапке, включая 901–1100 px; с 1100 px шапка использует компактное меню.
- Last 30 days переключает dashboard на 90 дней и повторно запускает native keyframes графика.
- FAQ раскрывается по одному ответу. Theme переключает светлую/тёмную тему, Motion on/off включает reduced-motion режим.
- Contact проверяет обязательные поля и показывает локальный результат, Send another возвращает форму. Pricing проверяет email и показывает локальное состояние waitlist. Email проверяется `MailAddress` с запретом повторного `@`; `ada@@example.com` отклоняется в обеих формах. **Данные никуда не отправляются.**
- Resize Game View: `>1100`, `900`, `640`, `390` px. Layout меняет штатный `BreakpointController` на основе CLI `.mq.json`, без собственного C# resize layout.

Primary CTA явно сбрасывают native theme background image и задают контрастные blue/white цвета для normal, hover, focus, active и disabled. Shimmer ограничен 10% opacity; wrapper также имеет blue fallback. При переходе карточек row → column их media rules меняют `flex-basis:0` на `auto`, отключают grow/shrink и задают безопасный minimum height: Pricing, Testimonials и Contact используют высоту по содержимому.

Полоса прокрутки остаётся native `ScrollView`: typed adapter оформляет её как 8 px track с 6 px thumb без стрелок. Он меняет только style properties публичных `verticalScroller` / `slider` и их стандартных chrome nodes; wheel, touch и native thumb drag handlers сохраняются. Это явная Unity-адаптация: внутренние элементы не получают converter `.htu-media-base`, поэтому обычный конвертированный CSS для них не сработал бы.

## Честные границы адаптации

1. Конвертер не выполняет React/JS. Typed adapter вызывает `UitkScreen.RegisterAction`, `Router.Navigate/OpenModal/CloseModal`, `UitkAnimator.PlayManual`; он реализует локальные формы, search, theme и typewriter. Общий header находится вне page, поэтому его navigation подключается typed C# actions. Навигация внутри страниц и dialogs создаётся из HTML data-атрибутов. Полный каталог из 100 компонентов/31 блока и его страницы не перенесены в Unity: обозначенные **↗** пункты сохраняют точные URL оригинала и не подменяются локальными действиями.
2. Aurora в оригинале — размытые CSS blobs. Здесь это три фиксированных мягких radial-gradient слоя, реально запечённых CLI и анимированных `UitkAnimator`. Геометрия текстуры не меняется при resize; viewport обрезает слои.
3. `background-clip:text`, SVG masks, arbitrary blur, Motion springs, 3D tilt, SVG beam paths и IntersectionObserver не поддерживаются этим маршрутом. Слово остаётся живым TextElement с native color animation и typed typewriter; оно не запекается в картинку. **Полная browser pixel parity и gradient-clipped text не заявляются.**
4. Logo cloud использует исходные названия технологий. Lucide/brand SVG заменены простыми текстовыми знаками. Orbit, beam и meteors адаптированы к поддерживаемым rotate/translate/opacity channels. Произвольные scroll/3D эффекты оригинала не воспроизводятся.
5. Для компактного showcase перенесены основные секции SaaS и четыре страницы; весь 100-component каталог, blog, auth и платёжная интеграция не включены. Testimonials сохранены как исходные **example customers**. Demo формы оригинала тоже frontend-only.
6. Gradient baker требует одинаковую геометрию в одном CSS rule. Поэтому разные высоты dashboard bars представлены отдельными HTML CSS classes. CSS media использует простые class selectors, соответствующие ограничениям runtime.

## Автоматические проверки

Текущее состояние на 27 октября 2026: в последнем графическом Play Mode-прогоне Unity 6000.3.14f1 прошли **14 из 16** проверок; две pixel-проверки CTA завершились с ошибкой. Причина ещё выясняется. Повторная проверка обычного Game View с добавленной камерой, проверка физического ввода и видеосравнение с оригиналом ещё не выполнены.

В Unity Test Runner выберите **Play Mode** и сборку `VeloraShowcase.QA.PlayMode`. Проверки используют настоящий графический backend и панель UI Toolkit; при запуске с `-nographics` они пропускаются. Они охватывают страницы, формы, анимацию, адаптивные размеры, пиксели главной кнопки и синтетические события указателя и прокрутки. Синтетические события не заменяют ручную проверку физической мышью или touch-устройством.

## Повторная конвертация

Все исходные inputs находятся в `Source~`. Каталог с `~` Unity не импортирует. Для воспроизводимого conversion из PowerShell (полный CLI publish рядом с DLL):

```powershell
python D:\Path\To\Assets\HtmlToUitkShowcase\Source~\sync_styles.py
dotnet D:\Path\To\HtmlToUitk\htmltouitk.dll run D:\Path\To\Assets\HtmlToUitkShowcase\Source~\showcase.htujob.json
python D:\Path\To\Assets\HtmlToUitkShowcase\Source~\verify_outputs.py
```

Ожидаемый результат: `PASS WITH WARNINGS`, без FAIL. Warnings явно сообщают о keyframes/media runtime emulations, baked gradients и reference geometry. `generated-assets.json` содержит SHA256 и importer policy каждого PNG. Файлы `Resources/Velora` перемещают вместе. `Velora.strings.json` — копия source English localization table, используемая через `ILocalizer`.

HTML содержит embedded CSS, поскольку текущий dialect validator отвергает даже local stylesheet links (D010). `sync_styles.py` переносит authoring CSS в HTML перед конвертацией. Изменения текста и его source localization table делают согласованно. `verify_outputs.py` проверяет уникальные имена, существование route targets, hash каждого PNG, localization coverage и отсутствие кнопок без runtime/C# обработчика; это структурный smoke check, а не замена ручной Play Mode проверки.

`VeloraShowcaseController.RefreshBindings()` перепривязывает только собственные actions, callbacks и scheduled items к текущим `screen.Root`/`screen.Router`. Метод автоматически вызывается из `Update` и перед действиями; host может вызвать его сразу после `UitkScreen.ReloadRoot`, `ConfigureRoot` или замены UIDocument. Синхронизация не вызывает `Configure`, `Initialize` или `OnEnable`; явный `Initialize()` остаётся операцией первоначальной/повторной настройки документа. При disable/enable адаптер сохраняет внешнюю проекцию и не заменяет её своим документом. Старые Button/field callbacks не изменяют новое дерево, а старый delayed scroll и typewriter снимаются при rebind.

## English

This single-scene showcase adapts the real MIT Velora SaaS, pricing, about and contact pages. Supported HTML/CSS is converted by the actual HtmlToUitk CLI; generated UXML/USS is not manually authored or edited. Neo.HtmlToUitk supplies routing, modal ownership, keyframes and responsive media classes. A small typed adapter supplies the React interaction boundary: typewriter, theme, local search, clipboard and frontend-only forms. The Original button opens the exact upstream SaaS URL. The scope and unsupported browser effects above are intentional and explicit; this is not a claim of arbitrary React/JavaScript conversion or browser pixel parity.

The latest graphics-enabled Unity 6000.3.14f1 Play Mode run passed 14 of 16 tests; two CTA pixel tests failed and their cause remains under investigation. The camera in the normal Game View, physical input and real-time video comparison have yet to be verified.

Run `VeloraShowcase.QA.PlayMode` in Unity Test Runner with a graphics backend; `-nographics` causes these panel tests to be skipped. Synthetic pointer and scroll events do not replace testing with a physical mouse or touch device.
