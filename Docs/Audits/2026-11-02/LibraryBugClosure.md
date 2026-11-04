# Закрытие багов библиотеки — 2 ноября 2026

Цель владельца: довести найденные в аудитах баги самой библиотеки до исправления и подтверждённой проверки. База — `demo`, `3e795e8c`. HTML-конвертер и новый browser backend не входят в этот проход. Реестр исходных находок — [ClosureLedger](../2026-11-01/ClosureLedger.md).

Пять source-review групп охватывают 69 областей и 246 уникальных non-HTML действий, включая пять coordinator actions. Cross-owner ссылки не считаются отдельными багами. 64 отложенных предложения не объявляются автоматически 64 дефектами: для каждого пункта отделяются исходная неисправность, реализованное исправление, обязательная приёмка и новое расширение функционала.

**Текущий срез:** после red/green независимо приняты StateMachine78, validation/Inventory82, Cards33, interaction129+Core30, UI44 и native physics8. Input29ed получил10Edit+5Play green; affected190Play PASS и16Edit PASS/1NewOnlySkip. Все12 combined performance captures прошли настоящую native byte/CPU readback-проверку:2880выбранных кадров AutomaticUpdate GC0, denseN1000 p50≈4.6–6.2мс против прежних≈955–1030мс. [Полные данные, улучшения и ухудшения хвостов](InteractionPerformanceBaseline.md) сохранены; это scoped Unity6.3 Editor, не глобальная приёмка всех246actions. Focused Input12/14-scope byte gate, остальные hot-path измерения, чистые consumer imports, network peers, Unity6.6 и interactive/manual остаются открыты. Новая library-фаза ещё не committed/pushed.

Порядок работы:

1. Зафиксировать текущие исходники и повторить библиотечные EditMode/PlayMode проверки, сохраняя raw результаты. Source reviewers в это время не меняют product/test files.
2. Сопоставить каждый action с текущей функцией и наблюдаемым тестовым поведением. Наличие файла и общий зелёный assembly сами по себе не закрывают конкретный action.
3. Выдать узкое ownership авторам подтверждённых остатков. Не менять публичные API, сериализацию, настройки пользователя или архитектуру без необходимой причины.
4. Прочитать каждый diff и независимое ревью; затем выполнить affected regressions и обязательную интеграционную проверку. Не ослаблять assertions ради зелёного результата.
5. Обновить каноническую сверку и документацию по фактическим доказательствам. Непроверенные платформы, ручной ввод и новые возможности не объявлять закрытыми.

Root — единственный оператор Unity, Git, конфигурации и запусков. Владелец разрешил hidden graphics batch при открытом другом проекте; второй Editor на том же NeoxiderTools запрещён. После bootstrap OOM перед каждым следующим Unity/build запуском требуется свежая проверка минимум **6 GiB свободного commit**. Другие проекты и приложения не меняются. 12 пользовательских путей сохраняются по исходному manifest; worktree не создаются.

Переиспользование: существующие библиотечные контракты, тестовые сборки `Neo.Editor.Tests` / `Neo.Tests.Play`, sample smoke fixtures, Profiler markers и ранее проверенный guarded batch launcher. Новая тестовая инфраструктура допускается только для реально отсутствующей приёмки.

## Первые фактические результаты

- Unity 6000.3.14f1, основной non-HTML EditMode: **4235 случаев — 4201 PASS / 11 FAIL / 23 SKIP**, exit2. Все 11 отказов пришлись на предусловия нативного захвата курсора в сфокусированном Game View; скрытый batch не даёт этой приёмки. Raw failures сохранены. Все 33 строгих `SetText`-случая прошли; E25 подтверждён для этого profile.
- Основной PlayMode с восемью новыми реальными physics cases: **748 — 718 PASS / 6 FAIL / 24 SKIP**, exit2. Четыре отказа — нативный курсор; два — новые 3D collision stay сценарии. Последние требуют разделить admission фикстуры и relay-поведение; остальные шесть новых physics cases прошли. Импортируемые сэмплы, NewOnly, HDRP и interactive end-of-frame не превращаются в PASS.
- До/после запусков совпали 5923/5925 input identities и 12 защищённых пользовательских путей. Для EditMode все 51 Neo EditorPrefs name/type/data также совпали. Значения preferences и private configuration не публикуются.
- В source review обнаружены три дополнительных runtime-boundary дефекта: успешный конструктор StateMachine, Board admission после native parenting и Drunkard initial setup после reentrant restart. Ещё два дефекта относятся к validation-host version admission и изоляции Inventory authoring test. Исправления готовятся отдельными кандидатами вне Assets с последующим red/green и независимым review.

Полное закрытие библиотеки **ещё не заявлено**. Каждый источник и каждый обязательный сценарий должны получить собственную доказательную привязку; общие totals выше не закрывают автоматически все 246 пунктов.

## Первые исправления с текущей приёмкой

- StateMachine constructor reentry: все 12 новых случаев на старом production-коде воспроизвели именно неверный CurrentState. После узкого generation/cache/entry guard прошли **78/78**, включая старые runtime, FIFO/dispatch, duration context и behaviour случаи. Raw red и green сохранены; независимый reviewer подтвердил исходники, неизменность старых случаев и точные результаты. Это Unity 6.3 Mono scope, не все платформы.
- Validation host и Inventory authoring isolation: применены четыре проверенных candidate-файла; **82/82** — provenance41 и commit41, без skips. В том числе реально прошли empty-cache/authored-icon и foreign-sentinel случаи. Нативные discovery/import profiles с новым frozen expected-version argument ещё обязательны; pure admission не подменяет импорт UPM.
- Cards F01/F02: шесть новых native callback регрессий дали **6/6 функциональных RED** на старом production. После двух ранее принятых правок полный Lifecycle fixture дал **33/33 PASS**, включая все старые 27 случаев. Независимое [ревью исполнения](NativeCardsExecutionReview.md) подтвердило raw/source, неизменность assertions, protected paths и preferences. Это приёмка Unity 6.3 Editor PlayMode; Unity 6.6 и Player остаются отдельными контекстами.

## Сбой памяти и правило последующих запусков

Попытка `cards-policy-physics-u63-red-r1` аварийно завершилась **до первого теста**: Unity не смогла выделить память при сохранении bootstrap-scene Test Framework и поиске Scene Templates. XML результатов нет; это **0 executed / NotRun**, а не доказательство Cards/physics-дефекта или успешный red. Сохранены native log, exit1073741845 и отдельный abort receipt. Memory dump/preferences остаются приватными. Единственная созданная этим запуском bootstrap-сцена с meta проверена по имени, controller, времени создания и абсолютному пути, сохранена приватно и удалена.

Перед упавшим запуском было 3.17 GiB свободного commit. Минимум для последующих library Unity запусков повышен до **6 GiB**. В 07:47 UTC ресурс восстановился до примерно 54 GiB; root возобновил узкие проверки без закрытия пользовательских приложений. Перед каждым новым запуском порог проверяется заново. Это не финальное закрытие багов.

После восстановления ресурса все **8/8 текущих native physics phase cases прошли**, включая оба 3D collision stay случая с независимыми raw MonoBehaviour enter/stay/exit controls. Assertions payload/contact/phase не ослаблены; production relay не менялся. [Независимое ревью](NativeInteractionExecutionReview.md) ограничивает результат локальным/optional-solo контекстом Unity 6.3.

Три policy probes первоначально дали **1 PASS / 2 FAIL**: оба filtered 2D сценария обнаружили устаревший Shared cache после смены `Physics2D.queriesHitTriggers` в том же кадре. После добавления отсутствующего query kind в существующий ключ кэша прошли **123/123**: все прежние 112 interaction cases, три policy и восемь native physics. Независимое ревью подтвердило точные identities, native parity и B→B cache reuse без лишнего cast/epoch advance. Настройки и механика не менялись.

Реальные UI persistence проверки первоначально дали **1 PASS / 6 FAIL**: Redo дубликата терял уникальные binding keys, Undo применения layout захватывал предыдущее отдельное изменение. После семи добавленных строк в существующей editor utility прошли **7 persistence + 37 прежних affected cases = 44/44**, без skips. [Независимое ревью](NativeUIPersistenceExecutionReview.md) подтвердило actual Undo/Redo, поздние save/reopen/rest/key/motion/reference assertions и сохранение старых identities. Это Unity 6.3 EditMode scope.

## Измеренные остатки производительности

По уточнению владельца [повторная проверка горячих путей](Performance/README.md) распространяется на все 69 областей, а не только Input/interaction. Три прежних автора завершили source follow-up по22/16/31owners; root прочитал каждый отчёт, независимые reviewer приняли coverage/source claims. Новые затраты за пределами измеренного interaction/input остаются NOT_MEASURED: это не готовая performance-приёмка всей библиотеки.

Все 12 capture cases и 12 настоящих native Profiler readbacks выполнены. Полная [исходная матрица](InteractionPerformanceBaseline.md) сохраняет измерения до оптимизации: 120 positive + 120 stable-miss кадров на каждый режим/N, калибровка GC metadata, N AutomaticUpdate scopes на кадр. Это не только query counters и не итоговый performance pass.

- В baseline плотные попадания на1000 объектов давали примерно955–1030мс Automatic CPU, преимущественно из-за повторной классификации полного H-набора для каждого потребителя. Принятый indexed fast path с живым ownership и полным legacy fallback уже применён и прошёл native129/Core30. Inspection oracle честно отражает2N вместоN×H при неизменённом workload.
- В baseline keyboard Automatic выделял ровно **3384×N байт/кадр**, camera —0. Input adapter29ed уже применён после source review и причинного native-call red. Current10Edit+5Play green сохранили mapping/live held/down/up/device/reset/fault/weak-lifetime controls; Backend не переключался ради budget. Отдельные raw focused byte-окна ещё нужны; прежний managed allocation counter4096→0 признан недействительным.

Completed combined12 повторные измерения и независимый review подтвердили scoped AutomaticUpdate GC0 на всех2880кадрах и существенное снижение dense CPU. Остальные контексты не подменяются этим результатом: External/EventSystem, Player/IL2CPP, other SDK и focused Input byte gate остаются отдельными. Ни ввод, ни число попаданий/кадров не отключались ради результата.

Первый classifier-кандидат уже применён после полного source review. Ошибка доступности internal-метода в новой фикстуре остановила первый compile до тестов; отдельная fixture-only правка перешла на эквивалентный публичный Camera API. Затем прошли **129/129** (прежние123 + шесть live-lease parity случаев) и **30/30 Core**. В одном идентичном плотном Camera3D/N1000 after-профиле медиана Automatic снизилась **955.60→7.47мс**, Classify **937.93→3.04мс**, Automatic GC остался0. Все120+120кадров,1000scopes и калибровка4128 подтверждены native readback и независимым ревью.

Этот первый classifier-only профиль сохраняет stable-miss ухудшение **3.09→4.57мс** (+48.2%) при неконтролируемой внешней нагрузке. Поздняя полная combined12 матрица после Input29ed даёт для того же camera3dN1000 miss2.66285мс; это не стирает раннее наблюдение и не доказывает его причину. В combined все miss medians ниже baseline, но отдельные small-N tails хуже и явно перечислены. Настоящий old allocation-call RED Edit7P/3F и Play2P/3F перешёл в current10/10+5/5green после state/mapping/edge/calibration controls; scoped AutomaticUpdate native bytes отдельно подтверждены всей12матрицей. Индивидуальный InputPolling12/14-scope byte gate ещё открыт.

## Сопоставление конкретных находок и поставка

Пять авторов составили карты всех **246 уникальных non-HTML actions**; независимые reviewers проверили raw/fullname/result, parameter variants, hashes, source qualifications и границы утверждений. Корректировки mapping03 затронули только реальные skip reasons и NONCOUNTED handoffs; operator amendment mapping05 фиксирует последующую prose-only правку описаний samples, сохраняя исходный snapshot. Карты и raw сохранены в [Evidence](Evidence/README.md). Это traceability, а не автоматическое превращение всех действий в closed.

Дополнительно устранена source-level ошибка переносимости: `PikupParticleSystem.prefab` ссылался на материал вне пакета. Теперь он ссылается на пакетную копию существующей конфигурации с новым уникальным GUID; её текстура уже находилась в Demo, shader встроенный. Оригинальный материал проекта не изменён. Source/reference closure независимо проверена; fresh native import/rendering остаются ожидаемой проверкой. Статический package checker после этого изменения: **5897 файлов / 71 сборка / 3213 GUID / 0 ошибок**.

Подготовленные UI7 и policy3 уже действительно исполнены с сохранёнными red/green; baseline12 и combined12 performance windows прочитаны нативным reader. Новые Grid/Selector capture adapter и module reader пока только подготавливаются внешне и не объявлены измеренными. Существующий Telepathy launcher/harness найден в репозитории; новые сетевые инструменты вместо него не создавались. Mirror optional-reference compile gate требует настоящего absent-backend profile: одна отсутствующая именованная asmdef-reference сама по себе не доказывает ошибку.

## Проверка смысла оставшихся действий root

Root дополнительно прочитал контракты, source verdicts, обязательную приёмку и supplemental claims таблиц групп 02, 03 и 05. Это проверка смысла предложенных dispositions; повторного исполнения или полного независимого чтения всех перечисленных исходников она не означает. Ранее выполненные peer reviews конкретных source hashes и raw witnesses сохранены отдельно.

При дальнейшей приёмке необходимо различать:

- **Исполненный PASS** и **отсутствующий в discovery случай**: два TimerObject UI-теста под `UNITY_TEXTMESHPRO` не были обнаружены. Для них нужен соответствующий настоящий compile profile; 11 прошедших Timer mutation случаев их не заменяют.
- **Provider и consumer**: Survivor retirement test существует, но был пропущен из-за отсутствующего импортированного Demo. Проверки pool provider не доказывают фактический teardown Survivor. Аналогично Demo cleanup, native Awake и repaired Pages prefab import требуют своего контекста.
- **Функциональный счётчик и измерение**: interaction 1/100/1000 и query counters не доказывают CPU/GC, отсутствие N×hits classification или производительность EventSystem. Grid gizmo allocation и прочие заявленные performance outcomes также требуют наблюдения в соответствующем контексте.
- **Недоступный исторический инструмент и текущий баг**: NI59 metadata utility отсутствует после пользовательских Local deletions. Её нельзя объявлять свежепроверенной или восстанавливать ради закрытия строки; конкретная текущая package identity/import проверяется действующими средствами.
- **Документированную границу и дефект**: RPG BaseMax/v3, AttackSpeed consumer, произвольные callback transactions и новый Cards zone framework остаются отдельными предложениями. Сетевой трёхпроцессный Matrix, сохранение сцен/prefabs и обе Unity compiler branches, напротив, входят в обязательную приёмку.

Cards/interaction/UI scoped red/green выполнены. Следующие native действия — focused Input bytes, другие приоритетные hot paths и оставшиеся обязательные профили. Они идут по собственным admission controls и raw результатам; OOM, capability failures, skips и undiscovered cases сохраняются явно. В первом пустом clean-consumer host Unity возвратила exit0, но native Input System UXML import дал ошибки на шести существующих путях длиной260–272символа; это не clean import PASS и не подтверждённый баг Neo. Новый короткий host создан официальным createProject без копирования Library: те же6файлов импортированы на путях205–217символов, dependency run завершён0CS/0exception/0asset-import-failure. [Независимое ревью](NativeConsumerContextReview.md) принимает только этот NoNeo dependency context; Neo import/discovery/sample/loaded-Pro proof остаются открыты. Прежний raw/lock/host сохранён.
