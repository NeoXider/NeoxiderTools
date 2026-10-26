# Neo.Cards — индивидуальный план реализации

Статус: только план второй фазы; новых исправлений или выполненных Unity-проверок нет. Основания: [завершённый аудит](Neo.Cards.md), [контракт](PLAN-CONTRACT.md), актуальная запись `Neo.Cards` в `manifest.json`. Продолжена исходная сессия аудитора; модель/режим не переключались.

Пути ниже точные относительно обозначений: `R = Assets/Neoxider/Scripts/Cards`, `D = Assets/Neoxider/Docs/Cards`, `E = Assets/Neoxider/Tests/Edit/Cards`, `P = Assets/Neoxider/Tests/Play`.

## Выполнено и повторно проверено

Первая фаза проверила 45 файлов, исправила F01–F13 в 17 runtime-файлах, обновила десять публичных страниц и добавила 40 EditMode-сценариев и один PlayMode-сценарий. Уже исправлены snapshot/reset колоды, custom-ID и генерация, bulk-add, вместимость, повторные экземпляры, порядок presenter/view, состояние стороны, устаревшие спрайты, изначально полная рука, недопустимые poker-карты, резервы/переполнение Drunkard. Повторять эти изменения не требуется.

Текущий fingerprint 45 исходников совпадает с аудитом: `227dcf4b204e564f3251761149476a75469f3ed9cbec27f1314fc9475d3c25b1`. Перечитаны актуальные участки transfer/layout/restart/tween, контракты views, тестовый сценарий Drunkard, тестовые зависимости и соответствующие инструкции. Ранее прошли UTF-8, структурные проверки и scoped `git diff --check`. Это не компиляция: все 41 сценарий, визуальная проверка и Unity-совместимость остаются долгом.

## Приоритетные исправления и долг проверки

**V0, P0 — обеспечить проверяемую интеграцию.** Координатор совместно с владельцами `Neo.Editor.Tests` и `Neo.Tests.Play` добавляет соответственно `UnityEngine.UI` в `Assets/Neoxider/Tests/Edit/Neo.Editor.Tests.asmdef` и `Neo.Cards`, `UniTask` в `Assets/Neoxider/Tests/Play/Neo.Tests.Play.asmdef`. Повторное чтение подтверждает отсутствие ссылок. Затем оператор запускает существующие Cards suites и три файла `Audit20261023NeoCardsModelTests.cs`, `Audit20261023NeoCardsSceneTests.cs`, `Audit20261023NeoCardsDrunkardTests.cs` в указанных E/P. Ожидается 40+1 новых успешных сценариев, включая сохранение всех 54 карт. Наличие исходников тестов не считать результатом запуска.

**R1, P1 — отказ после начала выдачи; подтверждено кодом, runtime-воспроизведение предстоит.** `R/Components/DeckComponent.cs:346–387` проверяет руку до `MoveToAsync`; `R/Components/HandComponent.cs:164–228` позднее молча отклоняет переполнение. Заполнить последний слот во время перелёта: источник уже уменьшен, но `OnCardDealt` вызывается без добавления. Добавить `UniTask<bool> TryAddCardAsync(CardComponent card, bool animate)` с прежним `AddCardAsync` как совместимой обёрткой. При отказе вернуть точные данные наверх исходной модели и восстановить исходное представление стопки; успех выдачи публиковать только после принятия. Проверять поколение источника, чтобы откат не добавлял старую карту в переинициализированную колоду. Владелец: Neo.Cards. Acceptance: два вызова на последний слот дают один успех, полный multiset и отсутствие бесхозных объектов.

**R2, P1 — незавершённый layout переживает изменение состава; подтверждено кодом.** `R/Components/BoardComponent.cs:582–607` рассчитывает массивы один раз, но после await продолжает обход изменяемого `_cards.Count`. Добавление карты увеличивает индекс сверх массива. В этом файле и `R/Components/HandComponent.cs` применять snapshot ссылок и ревизию membership; устаревшую раскладку отменять, актуальную пересчитывать. Не применять transform к карте, сменившей владельца. Acceptance: add/remove/clear во время раскладки завершаются актуальным порядком без исключений и позднего перемещения чужих карт.

**R3, P1 — restart не отменяет предыдущий раунд; подтверждено кодом.** `R/Drunkard/DrunkardGame.cs:83–87,212–301,898–941`: destroy-token сохраняется, restart очищает очереди и снимает `IsPlaying`, старые продолжения остаются. Ввести CTS поколения игры, отменять до очистки, освобождать весь war-pile; проверять поколение после await и в finally. В `R/Components/CardComponent.cs`, `R/View/CardViewUniversal.cs`, `R/View/CardView.cs`, `R/View/CardViewAnimationTemplates.cs` связать активные tweens с уничтожением/отменой, хранить конкретную операцию вместо ожидания изменяемого `_currentTween`. Добавлять token-overloads, сохраняя прежние сигнатуры; `ICardView` не расширять обязательными членами. Acceptance: restart между половинами flip/во время war оставляет только новую раздачу, без старых winner/completion-событий и потерянных объектов. Владелец: Neo.Cards; видимые последствия проверить в PlayMode.

**R4, P2 — старый trump-preview после пустой инициализации; подтверждено условием.** В `R/Components/DeckComponent.cs:204–212` отсутствует ветка скрытия при пустом `TrumpCard`. Минимально выставлять active-state из наличия карты и `_showTrumpCard`. Новый EditMode-тест: непустая конфигурация → пустая custom-колода → display неактивен, count=0.

**Отдельные непроверенные риски:** mixed `DrawCard`/`Shuffle` со stack-dealing может рассинхронизировать данные; текущие D-инструкции уже ограничивают этот workflow. Глобальный animation-config при отключённом domain reload, ручные/generated slots, Inspector-кнопки вне PlayMode и нулевые/отрицательные durations требуют воспроизведения перед исправлением. Это не заявленные новые runtime-регрессии. Не менять семантику custom rank/suit queries без отдельного API-контракта.

## Существенное улучшение P01: атомарные зоны и упорядоченный view

Предложение, не реализация. Проблема разработчика: независимые списки модели/view и равенство `CardData` по определению мешают безопасно переносить одинаковые физические копии.

Повторно использовать `CardData`, `HandModel.TryAdd`, `ICardContainer`, текущие DeckModel return-операции, `CardLayoutCalculator`, UniTask и DOTween. `CardContainerModel.Mutable` не обеспечивает экземпляры/транзакции; `IHandView` не предоставляет пакетный reorder. Новые общие таймеры, pool, DI или зависимости не нужны.

Точные новые файлы в Neo.Cards: `R/Core/Data/CardInstanceId.cs`, `CardInstance.cs`, `CardTransferResult.cs`; `R/Core/Model/CardZoneModel.cs`, `CardTransferService.cs`; `R/Core/Interfaces/IOrderedHandView.cs`. Все новые package-файлы получают уникальные `.meta`.

Публичный C# путь: `CardInstanceId` — ненулевой caller-assigned `ulong`, отдельный от Unity Object ID; `CardInstance` связывает ID с `CardData`. `CardTransferService.TryTransfer(CardZoneModel source, CardZoneModel destination, CardInstanceId id, out CardTransferResult result)` проверяет обе зоны до mutation, затем публикует согласованное состояние. Result различает Full/Missing/Duplicate/Success. Первая версия фиксирует перенос до визуальной анимации; reservations для animation-before-commit — отдельное расширение.

`IOrderedHandView.SetOrderAsync(IReadOnlyList<ICardView> order, bool animate, CancellationToken cancellation)` реализовать в `R/View/HandView.cs`; использовать из `R/Presenter/HandPresenter.cs`, сохранив fallback для старого `IHandView`. Обновить адаптеры `R/Components/DeckComponent.cs`, `HandComponent.cs`, `BoardComponent.cs` только через opt-in путь. Сохранить сериализованные поля, enum values, UnityEvents, `CardData.Equals`, старые entry points и `Mutable`; старые saves не переписывать. Документация: `D/README.md`, `DeckComponent.md`, `BoardComponent.md`, `HandComponent.md`, `View/HandView.md`, `View/CustomCardViewGuide.md` — пример чистого C# и Inspector/UnityEvent-настройка.

Минимальные новые тесты: `E/Audit20261023NeoCardsTransferTests.cs` — отказ без mutation/events, две равные карты с разными ID, один перенос на последний слот, согласованное состояние в callbacks; `E/Audit20261023NeoCardsOrderTests.cs` — reorder без remove/add-событий, один layout и корректный indexed remove; `P/Audit20261023NeoCardsLifecycleTests.cs` — R1–R3, уничтожение и custom-view fallback. Оценка P01: 5–8 рабочих дней, средний/высокий риск event timing и custom views. Последующее выделение `R/Drunkard/DrunkardRoundModel.cs` и адаптация `DrunkardGame.cs` — отдельные 3–5 дней; в обязательные fixes не включать.

## Порядок, границы и завершение

1. Координатор закрывает V0 после завершения параллельных source-аудитов; затем безопасный R4 с регрессией, R3 lifecycle, R2 revision и R1 rollback с отдельными сценариями.
2. После локальных исправлений реализовывать P01 отдельно: pure core → EditMode → ordered view/opt-in адаптеры → PlayMode → документация. Не ждать большого redesign для устранения потерь карт.
3. `Neo.Cards.Editor` сохраняет владение `R/Editor/DeckConfigEditor.cs` и `D/Editor.md`; изменения authoring передавать этому владельцу после стабилизации runtime API. Shared asmdefs, package/settings и release-проверки принадлежат координатору. Новых зависимостей от Editor/Save/Grid не вводить.
4. Централизованно компилировать Unity 6.0–6.4 и 6.5+, включая существующие version gates; при необходимости Object ID использовать `NeoDiagnostics.StableId`/editor-owner helper, без локальных `#if`. Проверить package import и активный `Samples~`, developer `Samples` либо imported sample root без переименований в этой фазе.

Закрытие: V0 и регрессии проходят, multiset/порядок/события наблюдаемы, отменённые операции не меняют новое состояние, asset GUID/serialized API и scoped diff проверены. Unity-провалы чинить до закрытия; отсутствие запуска оставляет verification debt открытым. Стоп расширению P01 — до решения о семантике animation-before-commit, persistence новых ID и переносе custom-view side effects; это не блокирует обычные обратимые fixes. В этой фазе изменён только данный план; Unity/build/test-процессы не запускались.
