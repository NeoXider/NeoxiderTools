# Neo.Shop — индивидуальный план реализации

Статус: план, реализация второй фазы не начата. Продолжена исходная сессия аудитора; контракт `PLAN-CONTRACT.md`, завершённый `Neo.Shop.md` и актуальная запись manifest прочитаны полностью. Источник границ — manifest: 23 C# файла `Neo.Shop`, без вложенной `Neo.Shop.Bridges`.

## 1. Уже выполнено

В первой фазе изменены 11 исходников и 10 английских страниц `Assets/Neoxider/Docs/Shop/`. Реализованы S01–S14: общий откат бесплатных/платных покупок, возврат сверх лимита без роста `AllMoney`, числовые/профильные проверки, начальные уведомления баланса, восстановление экипировки и исправления подписок/состояния UI. Добавлены `ShopItem.OnViewChanged`, внутренний `Money.RefundSpend` и 31 NUnit-сценарий в `Audit20261023NeoShopRegressionTests.cs` с отдельным memory-provider.

Инвентарь 23/23, UTF-8, статическая структура C#, сохранение деклараций, ссылки, GUID и scoped `git diff --check` проверены. Unity-компиляция, weaving, EditMode/PlayMode не выполнялись. Эти исправления не являются будущими предложениями; первым этапом остаётся их интеграционная проверка.

Пути далее относительно `Assets/Neoxider/`, если не указано иначе. Текущие purchase/refund/authority методы, TextMoney/SetText, bridge, Inventory API, тесты и публичные инструкции перечитаны как контекст; чужие файлы не изменялись.

## 2. Приоритеты оставшейся работы

| Приоритет / статус | Доказательство и минимальное действие | Владелец / наблюдаемый результат |
| --- | --- | --- |
| **P0: долг проверки** | Централизованно запустить 31 новый сценарий, существующие Shop/Money/Equipment EditMode, `Tests/Play/ShopPurchasePlayModeTests.cs`, `Tests/Play/Network/MoneyNetworkTests.cs`. | Координатор после завершения source-аудитов. При cap=100, balance=150, price=50 неудачная покупка восстанавливает 150 и прежний `AllMoney`; UI реагирует на новую валюту, старые подписки отключены. |
| **P1: подтверждённая реентерабельность** | `Scripts/Shop/Shop.cs:357,447,996`: callback покупки A покупает B, затем бросает исключение; внешний snapshot удаляет успешное ownership B. При 200 монет и двух ценах 50 остаётся 150, B уже не owned. Добавить общий guard для `Buy`/`BuyBundle` до списания, освобождать через `finally`; вложенный вызов не должен списывать/выдавать товар. | Neo.Shop: `Shop.cs`; новый `Tests/Edit/Shop/Audit20261023NeoShopReentrancyTests.cs` + `.meta`; `Docs/Shop/Shop.md`. Воспроизведение пока исходниковое. После отката A баланс 200, B не выдан; следующий независимый вызов работает. Проверить все сочетания item/bundle и исключения. |
| **P1: подтверждённое ограничение выдачи** | `Shop.cs:504,996` компенсирует только профиль/дебет. `Scripts/Shop/Bridges/ShopInventoryGrantBridge.cs:120,142` не передаёт результат `AddItemData` обратно покупке: нулевая/частичная выдача не отклоняет её; позднее исключение не отменяет ранние grants. | Neo.Shop.Bridges + Neo.Tools.Inventory после контрактов Neo.Shop. Нужны резервирование и receipts из раздела 3. Обязательность полной выдачи — явная политика нового режима; существующий optional bridge автоматически не ужесточать. |
| **P1: подтверждённый сетевой пробел** | `Shop.cs:1106` возвращает pending до `TrySpend`, без purchase-request/ack. `Money.cs:685` допускает command без object authority, проверяя числа/баланс, но не разрешение клиента на reward/set. | Neo.Shop совместно с Neo.Network. Добавить опциональный request/ack адаптер и серверную политику операций. Требование server-authoritative экономики должно быть зафиксировано; numeric guard из аудита уже реализован. Один request приводит к одному подтверждённому списанию/выдаче либо отказу. |

Неподтверждённые риски, не готовые исправления: `TextMoney.cs:40,48` и базовый `Scripts/Tools/Text/SetText.cs:29,37` объявляют собственные lifecycle callbacks; нужен PlayMode-repro tween pause/resume. Владелец Neo.Tools.Text сначала проверяет/при необходимости предоставляет protected virtual hooks, затем Neo.Shop вызывает базовый lifecycle. Будущий тест: `Tests/Play/Audit20261023NeoShopTextLifecycleTests.cs`. Отдельно проверить refund RPC, late join и порядок startup/save загрузки; локальные EditMode callbacks этого не доказывают.

## 3. Существенное предложение: типизированная транзакция покупки

**Предложение, не реализовано.** Разработчику нужен результат покупки и компенсируемая выдача без зависимости от цепочки UnityEvents. Существующие `IMoneySpendWithResult`, `IMoneySpendAuthority`, `ShopProfileData`, `SaveProvider`, `Money.RefundSpend` переиспользуются через адаптеры. `ShopResolver`, reactive properties и `UnityEventDelegateCache` остаются средствами сценового слоя. Inventory предоставляет фактическое количество через `InventoryComponent.AddItemData` (`Runtime/InventoryComponent.Operations.cs:68`); `InventoryTransferService` покрывает перенос слотов, для выдачи необходим отдельный receipt-контракт.

Новые файлы внутри существующей Neo.Shop, без обязательной новой сборки:

- `Scripts/Shop/Runtime/ShopPurchaseContracts.cs`: обычные C# `ShopOffer`, `ShopPurchaseRequest` (requestId, offerId, kind), `ShopPurchaseResult` (id, status, price, currency, reason). Статусы: Succeeded, Rejected, Busy, PendingAuthority, CompensationFailed.
- `Scripts/Shop/Runtime/ShopTransactionContracts.cs`: wallet debit receipt с идемпотентными commit/refund; `IShopGrantParticipant.TryPrepare(context, out receipt)` и grant receipt с commit/rollback; контракт profile-store.
- `Scripts/Shop/Runtime/ShopRuntime.cs`: `ShopPurchaseResult TryPurchase(ShopPurchaseRequest request)`. Проверка каталога → prepare участников → резерв дебета → commit выдачи/профиля → post-commit уведомления; при ошибке компенсация в обратном порядке. Guard и документированное окно дедупликации requestId.
- `Scripts/Shop/Runtime/ShopMoneyAdapter.cs`, `Scripts/Shop/Runtime/ShopProfileStoreAdapter.cs`: использовать существующие wallet/profile/save API. Неподдерживаемый компенсацией custom wallet отклоняется до дебета; сбой компенсации возвращается явно.

`Scripts/Shop/Shop.cs` получает опциональный transactional режим и C# `TryPurchase(request)`/уведомление результата. Он проецирует asset-каталог в обычные offer-данные; core не ищет сценовые объекты. Старые кнопки/UnityEvents остаются сценовым API. В transactional режиме уведомления происходят после commit: исключение analytics/UI не отменяет состоявшуюся покупку.

Зависимые изменения выполняют соответствующие владельцы: Inventory добавляет резервирование/receipt в `Scripts/Tools/Inventory/Runtime/InventoryComponent.Operations.cs` и новом `Scripts/Tools/Inventory/Runtime/InventoryGrantReceipt.cs`; Bridges адаптирует `Scripts/Shop/Bridges/ShopInventoryGrantBridge.cs`, отключая legacy event-grant при регистрации участника, чтобы избежать двойной выдачи. Neo.Shop не получает ссылки на Inventory. После локального core добавить `Scripts/Shop/Runtime/ShopNetworkPurchaseAdapter.cs` в Neo.Shop: сервер определяет цену/валюту, проверяет полномочия, подтверждает requestId и отправляет authoritative snapshot. Neo.Network предоставляет существующий state/transport; обратную зависимость Network→Shop не создавать.

## 4. Порядок, совместимость и приёмка

1. Закрыть P0 и добавить reentrancy-регрессии; затем минимальный guard и повтор затронутых тестов.
2. Реализовать contracts/core/адаптеры; новые `Tests/Edit/Shop/Audit20261023NeoShopTransactionTests.cs` + `.meta` проверяют публичный API без GameObjects: free/paid, bundle с отказом второго участника, обратную компенсацию, ошибку сохранения/refund, повтор requestId и Busy. Сбой post-commit listener сохраняет успешную покупку.
3. Подключить bridge после готовности Inventory receipts; его владелец добавляет `Tests/Play/Audit20261023NeoShopBridgesTransactionTests.cs`. При заполненном inventory строгая покупка не списывает деньги; откат не удаляет прежние вещи/параллельные изменения inventory.
4. Добавить network adapter и `Tests/Play/Network/Audit20261023NeoShopAuthorityTests.cs`: pending не выдаёт товар, server rejection не списывает, повтор ack не дублирует результат, поддельный reward/set отклоняется новой политикой; host/client/late-join согласуют balance, totals, ownership.
5. Обновить существующие `Docs/Shop/README.md`, `Shop.md`, `InterfaceMoney.md`, `Money.md`, `ShopPurchaseButtonView.md`; Bridges/Inventory/Network обновляют свои страницы. Дать C# пример и Inspector/UnityEvent настройку, включая порядок grants/notifications и границы legacy режима.

Сохранить profile v1, serialized имена/defaults, stable ids, `_purchaseFlow`, int-прокси, `MoneyOp` 0–7. Новый режим выключен по умолчанию; strict missing-currency policy опциональна. Legacy callbacks сохраняют документированные ограничения. Asset-поля не получают scene references. Новым package-файлам нужны уникальные `.meta`; существующие сцены/префабы не требуют перепаковки. README-пример достаточен для первого этапа; release сохраняет `Samples~`, validation также учитывает developer `Samples` и импортированный `Assets/Samples/NeoxiderTools/<version>/...`.

Release gate: координатор компилирует Unity 6.0–6.4 и 6.5+ с/без Mirror, проверяет weaving и все перечисленные EditMode/PlayMode сценарии. Прямая ссылка `Mirror` в `Neo.Shop.asmdef` требует отдельной проверки closure; shared asmdef/package исправляет координатор только по реальной ошибке. Версионные Object API остаются за `NeoDiagnostics.StableId`/`NeoComponentHealth.StableId`; локальные Unity-version `#if` не добавлять.

Оценка: guard/регрессии 0,5–1 день; core/адаптеры/документация 5–8 дней; сеть ещё 3–5. Риск средний/высокий: callback ordering, пользовательские wallets, устойчивость save/compensation. Гарантия exactly-once после перезапуска не обещается без отдельного journal-контракта с Neo.Save. При failed compile/test, частичной компенсации или нерешённой authority/required-grant политике соответствующий этап не считается принятым; независимые локальные исправления продолжаются. Рутинные обратимые исправления дополнительного разрешения не требуют.

## Проверка этого планирования

Вторая фаза меняет только данный файл. Проверяются UTF-8, отсутствие trailing whitespace и полнота плана. Новых Unity/build/test запусков и результатов нет; рабочий аудит и публичные материалы не переписываются.
