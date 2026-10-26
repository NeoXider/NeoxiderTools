# Neo.Tools.InteractableObject — индивидуальный план реализации

Дата: 2026-10-23. Фаза 2; продолжение исходного аудита, gpt-6.1-sol / max.
Основание: `PLAN-CONTRACT.md`, собственный завершённый аудит, точная запись manifest и текущие исходники.
Это план: в этой фазе изменяется только данный файл.

Уточнение пользователя: все исправления и улучшения выполняются на ветке `demo`, исходная база
`ca198556`, целевая major-версия `11.0.0`. Состояние ветки здесь повторно не проверялось.
Обязательная последовательность: завершить ВСЕ аудиты и планы → native + root review → исходные
акторы реализуют одобренную работу → завершение ВСЕХ source writers → компиляция и ВСЕ тесты.
Внесение этой поправки не разрешает реализацию или запуск проверок сейчас.

## Текущий результат

Просмотрены все 6 принадлежащих сборке C# файлов. Исправления уже находятся в 5 исходниках:
offline-dispatch физики, проверки серверных команд и лимит соединения, разделение hover/click в
`Both`, viewport-лучи, кэш provider, hover lifecycle, установка raycaster, UI-клики, общий корень
сцены и orphan mouse-up. `ToggleObject.cs` оставлен без изменения поведения. Обновлены 6 публичных
руководств, созданы 28 регрессионных случаев в двух fixtures с префиксом manifest.

В аудитной фазе выполнены статические проверки UTF-8, объявлений API/serialized fields, ветвей MIRROR, ссылок,
GUID и `git diff --check`. Компиляция, weaving, реальные Unity callbacks и тесты не выполнялись.
Эти исправления не являются повторным backlog: ниже остаются их проверка и отдельные новые работы.

## Приоритеты и проверочный долг

### P1 — обязательный общий camera/player query, независимый от числа targets

Подтверждение root: `InteractiveObject.Update` каждого экземпляра вызывает
`UpdateMouseTargetRaycast`; `TryGetCurrentTargetHit` выполняет собственный
`Physics.RaycastNonAlloc` / `Physics2D.GetRayIntersectionNonAlloc`. `InteractionRayProvider`
создаёт только лучи. Кэшировать генерацию луча или вынести тот же вызов в per-target helper
недостаточно: N объектов по-прежнему создают N одинаковых camera physics queries.

**Обязательная коррекция для 11.0.0:** один scene-facing `InteractionController` на camera/player
источник, общий backend и snapshot каждого query-контекста на кадр. Controller один раз собирает
запросы, выполняет physics, сопоставляет collider с зарегистрированными targets, вычисляет
eligibility и доставляет hover/click/down/up transitions. `InteractiveObject` регистрируется в
OnEnable, снимается в OnDisable и потребляет результаты; его Update, pointer callbacks и повторное
чтение snapshot не запускают собственные camera casts. Один target не владеет экземпляром backend.
Нельзя добавлять target id в ключ, чтобы замаскировать повторение одинаковых запросов.

**Границы совместного использования:** ключ учитывает camera/player/input-source identity,
фактический ray и его геометрию, physics scene, тип запроса и policy. `Both` обычно требует два
контекста — center и mouse; `ScreenCenter` может разделять один между hover/click. Раздельны 3D,
2D screen-ray intersection и 2D planar raycast, layer masks, trigger inclusion, camera/display/
render target/viewport и правила source-rig exclusion. Target-specific фильтры можно применять
к общей полной выборке только при доказанной эквивалентности. Разную `InteractionDistance`
проверять отдельно; для общей геометрии допустима одна покрывающая длина с последующим соблюдением
исходного лимита каждого target, а не новый запрос на каждый distance.

Направленный LOS от отдельного `distanceCheckPoint`/`viewCheckPoint` к конкретной цели действительно
может иметь иную геометрию, obstacle mask и trigger policy. Такие запросы проходят через тот же
scheduler, объединяются только при совпадении контекста и учитываются отдельным счётчиком.
Для legacy range events по всем целям они могут оставаться O(N); это явно показывается в результате
профилирования, а не выдаётся за общий camera cast. Камерный snapshot можно повторно использовать
для LOS только при совпадении origin/direction/interval и всех фильтров. CPU-проверка расстояния
не требует physics. Не удалять range events ради результата теста производительности.

Raw physics snapshot общий и не содержит единственного заранее выбранного target. Unity-адаптер
хранит collider identity и registry bindings, включая несколько допустимых bindings на collider.
Для оценки конкретного target переиспользовать `InteractionRayHit` / `InteractionQueryMath`;
`IInteractiveTarget` остаётся typed dispatch-контрактом. Например, legacy `checkObstacles = false`
может допускать несколько целей вдоль луча: переход к одному глобальному nearest target изменил бы
поведение и не является допустимой оптимизацией. UI GraphicRaycaster остаётся отдельным каналом.

**Полнота и бюджет:** один основной native physics call на context/snapshot/frame. Если NonAlloc
заполнил буфер, допускается максимум один полный fallback с эквивалентными фильтрами на весь
контекст, а не серия повторных casts или fallback на каждого подписчика. По полному результату
увеличить переиспользуемые буферы до следующего кадра. Все потребители получают один и тот же
`IsComplete`; усечённый snapshot не может разрешить hover/click сквозь неизвестный blocker.
Если полный результат получить нельзя, вернуть явное incomplete и запретить небезопасное действие.
Счётчик включает fallback и все фактические вызовы Physics, а не только логические запросы.
Таким образом, бюджет ≤2 вызовов на холодный/overflow-контекст и 1 после прогрева; он не растёт
по числу его targets. Allocation при редком полном fallback измеряется и документируется.

**Invalidation:** snapshot принадлежит кадру и generation контекста. Изменение camera, viewport,
projection, input ray, physics scene или policy меняет контекст; новый кадр обновляет геометрию.
Registration/unregistration не запускают повторную physics query: обновляется routing и дальнейшая
доставка. Изменения из event callbacks ставятся в очередь; отключённой цели новые события не идут.
Disable/destroy источника или замена камеры снимают старый snapshot и завершают прежний hover ровно
один раз, очищают press/session согласно выбранной legacy/capture policy. При явном mid-frame
refresh после изменения physics создать новую snapshot revision и посчитать её отдельно; targets
сами такую revision не создают. Не держать stale Unity references; domain reset очищает registry.

**Будущая обязательная приёмка, после source freeze:** добавить
`Assets/Neoxider/Tests/Play/Tools/Audit20261023NeoToolsInteractableObjectSharedQueryTests.cs`.
Для 1/100/1000 зарегистрированных targets одного контекста после прогрева — ровно один native
camera query за кадр, в `Both` с действительно разными лучами — два; повторные consumers и pointer
callbacks счётчик не увеличивают. Геометрия со свободным лучом и с 17/64/1000 hits проверяет
полноту и холодный предел ≤2 на контекст. Перестановка регистрации не меняет выбранные hits или
последовательность событий. 2D/3D, разные layer/trigger policies, две камеры/viewport и разные LOS
дают отдельные ожидаемые контексты; identical LOS объединяется, разный LOS считается отдельно.
Считать вызовы через backend instrumentation и сверять с profiler, отдельно отмечая внешние
EventSystem queries. Дополнительно — отключение выбранной цели, замена камеры в кадре,
reentrant registration, obstacle/trigger/explicit collider/range/manual/network/UI semantics.
Успех по counter без сохранения поведения не закрывает P1.

### P1 — подтвердить уже внесённые сетевые исправления

Владелец: эта сборка; запуск и окружения — координатор. Источник: `InteractiveObject.cs`,
`CmdInteractDown`, `CmdInteractUp`, `CmdClick`; зависимость —
`Scripts/Network/Core/NeoNetworkComponent.cs`, `RateLimitCheck(sender)`.
Добавить `Assets/Neoxider/Tests/Play/Network/Audit20261023NeoToolsInteractableObjectNetworkCommandTests.cs`.
Два удалённых соединения: сервер выключает `interactable`/`isNetworked`, отклоняет unauthorized
и неверную кнопку; счётчики событий не меняются. Поток запросов A не подавляет допустимый запрос B;
неавторизованный запрос не расходует его бюджет. Проверить dedicated server, pure client и host:
одна принятая команда даёт ровно одно ожидаемое событие на каждом получателе.
Проверять на финальной стадии после завершения всех source writers. Минимальное исправление при
обнаруженном отказе ограничить соответствующим handler, затем снова дождаться source freeze.
Общий limiter/transport принадлежит `Neo.Network` (`neo-audit-1023-b04-05`).

### P2 — устранить неполные physics queries

Подтверждённая незакрытая проблема полноты: `InteractiveObject.cs:162–164` содержит три массива
по 16 элементов; `IsInRange`, `IsInViewForKeyboardInteraction`, `TryGetCurrentTargetHit` принимают
полный буфер за полный результат. При избытке попаданий выбор может не учитывать цель/блокер;
исполненного Unity-воспроизведения пока нет. Core-сортировка не восстанавливает отброшенные hits.

Коррекция входит в общий backend P1:
`Assets/Neoxider/Scripts/Tools/InteractableObject/Internal/InteractionPhysicsQuery.cs` и адаптация
`InteractiveObject.cs`. Буферы принадлежат контексту, а не target. Использовать описанный выше один
полный fallback при saturation; синхронно обеспечивать ёмкость raw hits и `InteractionRayHit`.
Охватить 3D, 2D, mouse/look и obstacle queries. Сохранить текущие layer/trigger/ignore правила,
не менять глобальные physics settings. Отдельный per-target helper не закрывает эту работу.

Регрессии: `Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsInteractableObjectCapacityTests.cs`.
При 17 и 64 кандидатах, разных порядках создания и посторонних trigger результаты публичных
`IsHovered`/`IsInInteractionRange` совпадают с полным запросом: ближайшая стена блокирует,
цель без стены достижима. После прогрева обычные запросы не создают новых массивов.
Владелец — эта сборка; это часть общей архитектурной коррекции, а не независимая per-target реализация.

### P2 — выполнить runtime-проверки оставшихся исправлений

После завершения всех source writers запустить существующие 16 interaction и 12 physics EditMode случаев. `SendMessage` проверяет
границу callback, но не доставку движком. Добавить
`Assets/Neoxider/Tests/Play/Tools/Audit20261023NeoToolsInteractableObjectRuntimeTests.cs`:
реальные 2D/3D trigger/collision enter/stay/exit с обоими типами подписок, disable/re-enable,
нажатие вне цели и отпускание над ней, GraphicRaycaster без collider, переходы камер,
Input System-only и нечисловые/отсутствующие координаты. Наблюдать payload, последовательность
и число событий; не ограничиваться отсутствием исключения.

Риски, пока не доказанные дефекты: порядок EventSystem/Update, зависимость текущего Both-теста
от устройства, переход между отключёнными input channels. Если потребуется управляемый input,
сначала использовать имеющийся test adapter; изменения `Neo.Tools.Input` согласовать с владельцем
`neo-audit-1023-b07-02`. Исправлять только полученное воспроизведение.

## Реализация общей query-архитектуры и C# input-сессии

Общий query P1 обязателен по требованию пользователя; расширение session/capture ниже — предложение
для native + root review. Ничего из этой архитектуры пока не реализовано. Сейчас большой MonoBehaviour одновременно выбирает камеру,
читает устройства, выполняет physics, хранит жест и вызывает сеть. Разработчику трудно подключить
AI/XR, проверить отмену без сцены или явно заменить камеру. Документированные ограничения:
`InteractionCameraResolver.Resolve(cachedCamera)` удерживает живую камеру, provider следует новой
`Camera.main`; auto-setup выполняется только в Awake; отпускание вне цели не захватывается.
Эти legacy-политики не объявляются новыми подтверждёнными багами.

Переиспользовать `IInteractiveTarget`, `InteractionRayHit`, `InteractionQueryMath`, текущую
компатибильность ввода, `NeoNetworkState` и limiter. Общий controller принадлежит camera/player,
не становится глобальным game manager. Разделение владельцев: `Neo.Tools.InteractableObject.Core`
(`neo-audit-1023-b07-04`) — независимые данные, правила и session; эта сборка — Unity physics,
registry, lifecycle, scene adapters и dispatch. Core-владельцу предложить файлы
в `Assets/Neoxider/Scripts/Tools/InteractableObject/Core/`:

| Файл | Предлагаемый контракт |
|------|-----------------------|
| `InteractionRequest.cs` | Rays с признаками наличия, source/context identity, query kind и policy; без target id в ключе общего запроса и без scene search. |
| `InteractionQuerySnapshot.cs` | Read-only unclassified hit DTO, frame/generation, completeness; collider tokens выдаёт Unity adapter, без зависимости Core от Mirror и без прямого obsolete identity API. |
| `InteractionHitCandidate.cs` | Collider token, distance/point и данные классификации; проекция в существующий `InteractionRayHit` выполняется для нужного target binding. |
| `InteractionEvaluation.cs` | Per-target hover/click hit, eligibility и `IsComplete`, полученные из общей выборки без нового physics call. |
| `InteractionSession.cs` | `Update(InteractionEvaluation evaluation, bool pressed, bool released)` возвращает typed transitions; `Cancel()` однократно завершает сессию. Capture policy по умолчанию сохраняет legacy. |
| `IInteractionHitSource.cs` | `Query(InteractionRequest request)` возвращает общий `InteractionQuerySnapshot`; backend не привязывается к одному target. |

Точные новые файлы в `Assets/Neoxider/Scripts/Tools/InteractableObject/` этой сборки:

- `InteractionController.cs` — MonoBehaviour источника, единственное автоматическое место вызова
  общей query-фазы за кадр, доставка transitions и Play Mode diagnostics counters.
- `InteractionQueryService.cs` — обычный C# сервис контекстов/snapshots; повторный запрос того же
  frame/generation возвращает готовый результат. Автоматический tick controller и ручной tick
  C# caller не должны выполнять кадр дважды.
- `Internal/InteractionPhysicsQuery.cs` — Unity backend, complete hits, reuse буферов и реальные
  native-call counters, включая overflow fallback.
- `Internal/InteractionTargetRegistry.cs` — collider/binding index, per-target policy, безопасное
  снятие регистрации и очередь изменений во время callbacks.

Изменяемые существующие файлы: `InteractiveObject.cs`, `InteractionRayProvider.cs`,
`Internal/InteractiveObjectSceneSetup.cs`. Первый прекращает per-instance camera casts;
второй остаётся ray source; третий подготавливает выбранную камеру/controller/raycasters один раз
на контекст. `PhysicsEvents2D.cs`, `PhysicsEvents3D.cs`, `ToggleObject.cs` архитектурного переноса
не требуют. Shared asmdefs/новые ссылки — только координатор после согласования с Core.

**Явный C# путь:** `InteractionController.Configure(Camera camera, Transform distancePoint,
Transform viewPoint)`; `Register(InteractiveObject target)` / `Unregister(InteractiveObject target)`;
`Tick(int frameId)`; `TryGetEvaluation(IInteractiveTarget target, out InteractionEvaluation result)`.
Регистрация и чтение сами physics не выполняют. Для проекта без MonoBehaviour driver сервис
`InteractionQueryService` получает `IInteractionHitSource` через initialization и вызывается
единственным владельцем из обычного C#. Ручные `InteractDown`, `InteractUp`, `Click` сохраняют
прямой dispatch и не превращаются в скрытый camera query.

В `InteractiveObject.cs` добавить `Configure(Camera camera, Transform distancePoint,
Transform viewPoint)` и `PrepareCamera(Camera camera)`; обновить `InteractionRayProvider.cs` и
`Internal/InteractiveObjectSceneSetup.cs`, чтобы явно выбранная камера задавала и режим, и луч,
и raycasters. Main-camera search остаётся fallback. `Configure` обновляет camera cache и checkpoints,
не перезаписывая другие настройки, и перепривязывает target к общему controller. Явный ignore-root/
набор collider заменяет догадки только opt-in. Core resolver не получает другую retention-семантику
молча; выбранная камера и provider должны относиться к одному source context.

**Scene/NoCode путь:** один controller на camera/player, назначенные Camera/View/DistancePoint,
provider с Mouse/ScreenCenter/Both; targets привязаны к controller и сохраняют свои UnityEvents,
colliders и политики. OnEnable/OnDisable обслуживают регистрацию автоматически. Для старых сцен
без ссылки допустим один общий binder/fallback на camera/player; он не создаёт backend на каждом
target и не возвращает старый polling. Если автоматическое сопоставление нескольких камер/players
неоднозначно, v11 migration явно требует выбрать controller в Inspector; это не основание молча
использовать чужую камеру. Новые сцены предпочитают явную ссылку.

Приёмка: источник A hover и источник B click не конфликтуют; одинаковый input не повторяет события;
отмена при disable/target loss приходит один раз; явно подготовленная поздняя камера работает без
пересоздания target. CaptureUntilRelease, если выбран, завершает press после ухода с цели;
legacy сохраняет прежнее правило. Сетевую отмену и новый Inspector callback определить совместно
с `Neo.Network`; до этого capture не включать в сетевом режиме. Изменять `IInteractiveTarget`
добавлением обязательного метода отмены не требуется.

Core-владельцу: новый
`Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsInteractableObjectCoreSessionTests.cs`.
Минимум — последовательности enter/down/release/exit, повторный Cancel, потеря источника и обе
capture-политики. Наша runtime fixture проверяет реальный component adapter. Оценка улучшения:
Для query context/registry добавить Core-владельцу
`Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsInteractableObjectCoreSharedQueryTests.cs`:
разделение policies, повторное чтение frame snapshot, completeness и проекция существующих правил.
Оценка обязательного shared-query переноса: 4–6 дней; optional session/capture ещё 2–3 дня,
затем общий QA. Риск средний/высокий: порядок событий, target classification, lifetime snapshot
и геометрически разные LOS. Профилирование и counters обязательны для приёмки P1.

## Совместимость, порядок и завершение

1. Завершить ВСЕ аудиты/планы. Провести native + root review полного комплекта, согласовать owner
   split, shared-query budgets, v11 compatibility/migration и перечень одобренных изменений.
   До этого source edits, компиляция и выполнение тестов по этому плану не начинаются.
2. Исходные акторы реализуют одобренные работы на `demo` от базы `ca198556`: сначала Core
   query-контракты/правила, затем общий backend, registry/controller и перевод targets в consumers.
   Overflow исправляется в общем backend. Regression fixtures и counters пишутся в этой стадии,
   но ни новые, ни существующие тесты ещё не запускаются.
3. Подключить explicit camera setup и только одобренный session/capture scope. Сетевые/input
   зависимости меняют исходные владельцы `Neo.Network` / `Neo.Tools.Input`; shared asmdefs и
   package metadata координирует root. Координатор готовит release `11.0.0`, changelog и migration
   notes; этот worker в фазе планирования их не редактирует.
4. Обновить существующие `Assets/Neoxider/Docs/Tools/InteractableObject/README.md`,
   `InteractiveObject.md`, `InteractionRayProvider.md`: общий controller, C# registration/tick/
   evaluation/cancel, бюджеты, LOS исключения и Inspector migration. Core README обновляет его
   владелец; нового параллельного entry guide не создавать. Описание v11 migration связывает
   старые serialized targets и новые scene references с примерами прежнего поведения.
5. Сохранить 28 существующих serialized fields, UnityEvents, defaults, ручные команды и повторный
   Set в ToggleObject. Не менять существующие GUID и serialized значения автоматически. Target
   registration не должна дублировать UnityEvents с EventSystem или networking. Если эквивалентность
   требует breaking change, зафиксировать конкретный v11 migration и сценарий до/после в review;
   не прятать per-target physics за режимом совместимости. Новые файлы получают GUID/meta;
   scene/sample migrations реализуют только назначенные владельцы после review.
6. Дождаться окончания ВСЕХ source writers, зафиксировать общий source freeze; только затем
   координатор запускает компиляцию, weaving, ВСЕ EditMode/PlayMode/network/performance тесты.
   Матрица: Unity 6.0 и 6.4, затем 6.5+, MIRROR on/off и доступные input backends. Новые version
   gates централизовать; identity API — через существующие StableId wrappers. При необходимости
   исправления снова завершить writers перед следующим test round.
7. После успешной общей проверки root завершает package/release validation `11.0.0` на `demo`.
   Проверить `Samples~`, developer `Samples`, новый импорт
   `Assets/Samples/NeoxiderTools/11.0.0/...` и совместимость существующего импорта `10.19.0`.
   Не переименовывать sample layout как побочный эффект query-переноса; release/config/commits
   остаются у координатора, этот этап их не выполняет.

Завершение реализации: shared camera-query бюджет доказан для 1/100/1000 targets, сохранены
API/assets либо принят и документирован конкретный v11 migration; 28 текущих и новые целевые
регрессии проходят, подтверждены реальные события и compile branches. Каждый пропуск явно
записан как непроверенный. Коррекция считается незавершённой, если общий provider/service всё ещё
вызывает camera physics из каждого target или counters не учитывают fallback/дополнительные LOS.
Без реального двухклиентного результата сетевой долг не закрывать. Решения о capture/cancel и
будущем DTO для compound collider остаются отдельными продуктовыми решениями; они не задерживают
проверку существующих исправлений и устранение неполных запросов. Native Collision по сети
по-прежнему nullable; изменение payload не входит в первый этап.

В этой поправке изменён только данный план; execution checks, Unity/build/test процессы не
запускались. Runtime, тесты, публичные docs, аудит и shared configuration не изменялись.
